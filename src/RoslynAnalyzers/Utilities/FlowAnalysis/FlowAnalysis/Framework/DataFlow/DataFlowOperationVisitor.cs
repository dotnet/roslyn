// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#pragma warning disable CA1707 // Identifiers should not contain underscores

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Operation visitor to flow the abstract dataflow analysis values across a given statement in a basic block.
    /// </summary>
    public abstract class DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> : OperationVisitor<object?, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : class, IDataFlowAnalysisResult<TAbstractAnalysisValue>
    {
#pragma warning disable RS0030 // The symbol 'DiagnosticDescriptor.DiagnosticDescriptor.#ctor' is banned in this project: Use 'DiagnosticDescriptorHelper.Create' instead
#pragma warning disable RS2000 // Add analyzer diagnostic IDs to analyzer release
        private static readonly DiagnosticDescriptor s_dummyDataflowAnalysisDescriptor = new(
            id: "InterproceduralDataflow",
            title: string.Empty,
            messageFormat: string.Empty,
            category: string.Empty,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.DataflowAndTelemetry);
#pragma warning restore RS2000 // Add analyzer diagnostic IDs to analyzer release
#pragma warning restore RS0030

        private readonly ImmutableHashSet<CaptureId> _lValueFlowCaptures;
        private readonly ImmutableDictionary<IOperation, TAbstractAnalysisValue>.Builder _valueCacheBuilder;
        private readonly ImmutableDictionary<IOperation, PredicateValueKind>.Builder _predicateValueKindCacheBuilder;
        private readonly HashSet<IArgumentOperation> _pendingArgumentsToReset;
        private readonly List<IArgumentOperation> _pendingArgumentsToPostProcess;
        private readonly HashSet<IOperation> _visitedFlowBranchConditions;
        private readonly HashSet<IFlowAnonymousFunctionOperation> _visitedLambdas;
        private readonly HashSet<IOperation>? _returnValueOperations;
        private ImmutableDictionary<IParameterSymbol, AnalysisEntity>? _lazyParameterEntities;
        private ImmutableHashSet<IMethodSymbol>? _lazyContractCheckMethods;
        private TAnalysisData? _currentAnalysisData;
        private BasicBlock? _currentBasicBlock;
        private int _recursionDepth;

        #region Fields specific to lambda/local function analysis

        /// <summary>
        /// Local functions that escaped from this method.
        /// </summary>
        private readonly ImmutableHashSet<IMethodSymbol>.Builder _escapedLocalFunctions;

        /// <summary>
        /// Local functions for which interprocedural analysis was performed at least once in this method.
        /// </summary>
        private readonly ImmutableHashSet<IMethodSymbol>.Builder _analyzedLocalFunctions;

        /// <summary>
        /// Lambda methods that escaped from this method.
        /// </summary>
        private readonly ImmutableHashSet<IFlowAnonymousFunctionOperation>.Builder _escapedLambdas;

        /// <summary>
        /// Lambda methods for which interprocedural analysis was performed at least once in this method.
        /// </summary>
        private readonly ImmutableHashSet<IFlowAnonymousFunctionOperation>.Builder _analyzedLambdas;

        #endregion

        #region Fields specific to Interprocedural analysis

        private InterproceduralAnalysisKind InterproceduralAnalysisKind
            => DataFlowAnalysisContext.InterproceduralAnalysisConfiguration.InterproceduralAnalysisKind;

        /// <summary>
        /// Defines the max length for method call chain (call stack size) for interprocedural analysis.
        /// This is done for performance reasons for analyzing methods with extremely large call trees.
        /// </summary>
        private uint MaxInterproceduralMethodCallChain
            => DataFlowAnalysisContext.InterproceduralAnalysisConfiguration.MaxInterproceduralMethodCallChain;

        /// <summary>
        /// Defines the max length for lambda/local function method call chain (call stack size) for interprocedural analysis.
        /// This is done for performance reasons for analyzing methods with extremely large call trees.
        /// </summary>
        private uint MaxInterproceduralLambdaOrLocalFunctionCallChain
            => DataFlowAnalysisContext.InterproceduralAnalysisConfiguration.MaxInterproceduralLambdaOrLocalFunctionCallChain;

        /// <summary>
        /// Stores a map from entity to set of entities that share the same instance location.
        /// Primarily used for ref arguments for context sensitive interprocedural analysis
        /// to ensure that PointsTo value updates to any of the mapped entities is reflected in the others in the set.
        /// </summary>
        private readonly AddressSharedEntitiesProvider<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> _addressSharedEntitiesProvider;

        /// <summary>
        /// Current interprocedural operation call stack.
        /// </summary>
        private readonly Stack<IOperation> _interproceduralCallStack;

        /// <summary>
        /// Dictionary storing context sensitive interprocedural analysis results for each callsite.
        /// </summary>
        private readonly ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>>.Builder _interproceduralResultsBuilder;

        /// <summary>
        /// Dictionary storing context insensitive interprocedural analysis results for escaped local function.
        /// </summary>
        private readonly ImmutableDictionary<IMethodSymbol, IDataFlowAnalysisResult<TAbstractAnalysisValue>>.Builder _standaloneLocalFunctionAnalysisResultsBuilder;

        /// <summary>
        /// Dictionary from interprocedural method symbols invoked to their corresponding <see cref="ControlFlowGraph"/>.
        /// </summary>
        private readonly Dictionary<IMethodSymbol, ControlFlowGraph?>? _interproceduralMethodToCfgMap;
        #endregion

        protected abstract TAbstractAnalysisValue GetAbstractDefaultValue(ITypeSymbol? type);
        protected virtual TAbstractAnalysisValue GetAbstractDefaultValueForCatchVariable(ICatchClauseOperation catchClause) => ValueDomain.UnknownOrMayBeValue;
        protected abstract bool HasAnyAbstractValue(TAnalysisData data);
        protected abstract void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity, ArgumentInfo<TAbstractAnalysisValue>? assignedValue);
        protected abstract void EscapeValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity);
        protected abstract void ResetCurrentAnalysisData();

        /// <summary>
        /// Indicates if we have any points to analysis data, with or without tracking for fields and properties, i.e. either
        /// <see cref="PointsToAnalysisKind.PartialWithoutTrackingFieldsAndProperties"/> or <see cref="PointsToAnalysisKind.Complete"/>
        /// </summary>
        protected bool HasPointsToAnalysisResult { get; }

        /// <summary>
        /// Indicates if we have complete points to analysis data with <see cref="PointsToAnalysisKind.Complete"/>.
        /// </summary>
        protected bool HasCompletePointsToAnalysisResult { get; }

        internal virtual bool IsPointsToAnalysis => false;

        internal Dictionary<ThrownExceptionInfo, TAnalysisData>? AnalysisDataForUnhandledThrowOperations { get; private set; }
        public ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>> InterproceduralResultsMap => _interproceduralResultsBuilder.ToImmutable();
        public ImmutableDictionary<IMethodSymbol, IDataFlowAnalysisResult<TAbstractAnalysisValue>> StandaloneLocalFunctionAnalysisResultsMap => _standaloneLocalFunctionAnalysisResultsBuilder.ToImmutable();
        internal LambdaAndLocalFunctionAnalysisInfo LambdaAndLocalFunctionAnalysisInfo =>
            new(_escapedLocalFunctions, _analyzedLocalFunctions, _escapedLambdas, _analyzedLambdas);

        /// <summary>
        /// Optional map from points to values of tasks to the underlying abstract value returned by the task.
        /// Awaiting the task produces the task wrapped value from this map.
        /// </summary>
        internal Dictionary<PointsToAbstractValue, TAbstractAnalysisValue>? TaskWrappedValuesMap { get; private set; }

        protected TAnalysisContext DataFlowAnalysisContext { get; }
        public AbstractValueDomain<TAbstractAnalysisValue> ValueDomain => DataFlowAnalysisContext.ValueDomain;
        protected ISymbol OwningSymbol => DataFlowAnalysisContext.OwningSymbol;
        protected WellKnownTypeProvider WellKnownTypeProvider => DataFlowAnalysisContext.WellKnownTypeProvider;
        protected Func<TAnalysisContext, TAnalysisResult?> TryGetOrComputeAnalysisResult
            => DataFlowAnalysisContext.TryGetOrComputeAnalysisResult;
        internal bool ExecutingExceptionPathsAnalysisPostPass { get; set; }
        internal virtual bool SkipExceptionPathsAnalysisPostPass => false;

        protected TAnalysisData CurrentAnalysisData
        {
            get
            {
                RoslynDebug.Assert(_currentAnalysisData != null);
                Debug.Assert(!_currentAnalysisData.IsDisposed);
                return _currentAnalysisData;
            }
            private set
            {
                Debug.Assert(!value.IsDisposed);
                _currentAnalysisData = value;
            }
        }

        protected BasicBlock CurrentBasicBlock
        {
            get
            {
                Debug.Assert(_currentBasicBlock != null);
                return _currentBasicBlock!;
            }
            private set => _currentBasicBlock = value;
        }
        protected ControlFlowConditionKind FlowBranchConditionKind { get; private set; }
        protected PointsToAbstractValue ThisOrMePointsToAbstractValue { get; }
        protected AnalysisEntityFactory AnalysisEntityFactory { get; }

        /// <summary>
        /// This boolean field determines if the caller requires an optimistic OR a pessimistic analysis for such cases.
        /// For example, invoking an instance method may likely invalidate all the instance field analysis state, i.e.
        /// reference type fields might be re-assigned to point to different objects in the called method.
        /// An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
        /// A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
        /// </summary>
        /// <remarks>
        /// For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.
        /// For value content analysis, we want to perform a pessimistic points to analysis to be conservative and avoid missing out true violations.
        /// </remarks>
        protected bool PessimisticAnalysis => DataFlowAnalysisContext.PessimisticAnalysis;

        /// <summary>
        /// Indicates if we this visitor needs to analyze predicates of conditions.
        /// </summary>
        protected bool PredicateAnalysis => DataFlowAnalysisContext.PredicateAnalysis;

        /// <summary>
        /// PERF: Track if we are within an <see cref="IAnonymousObjectCreationOperation"/>.
        /// </summary>
        protected bool IsInsideAnonymousObjectInitializer { get; private set; }

        protected bool IsLValueFlowCapture(IFlowCaptureOperation flowCapture)
            => _lValueFlowCaptures.Contains(flowCapture.Id);

        protected bool IsLValueFlowCaptureReference(IFlowCaptureReferenceOperation flowCaptureReference)
            => flowCaptureReference.IsLValueFlowCaptureReference();

        private Dictionary<BasicBlock, ThrownExceptionInfo>? _exceptionPathsThrownExceptionInfoMap;
        private ThrownExceptionInfo DefaultThrownExceptionInfo
        {
            get
            {
                Debug.Assert(ExceptionNamedType != null);

                _exceptionPathsThrownExceptionInfoMap ??= [];
                if (!_exceptionPathsThrownExceptionInfoMap.TryGetValue(CurrentBasicBlock, out var info))
                {
                    info = ThrownExceptionInfo.CreateDefaultInfoForExceptionsPathAnalysis(
                        CurrentBasicBlock, WellKnownTypeProvider, DataFlowAnalysisContext.InterproceduralAnalysisData?.CallStack);
                }

                return info;
            }
        }

        protected DataFlowOperationVisitor(TAnalysisContext analysisContext)
        {
            DataFlowAnalysisContext = analysisContext;

            // All of these named type are very commonly accessed in the dataflow analysis so we want to ensure
            // the fastest access (even though we know we have a cached access).
            ExceptionNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemException);
            ContractNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemDiagnosticContractsContract);
            IDisposableNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIDisposable);
            IAsyncDisposableNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIAsyncDisposable);
            ConfiguredAsyncDisposable = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesConfiguredAsyncDisposable);
            ConfiguredValueTaskAwaitable = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesConfiguredValueTaskAwaitable);
            TaskNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask);
            TaskAsyncEnumerableExtensions = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTaskAsyncEnumerableExtensions);
            MemoryStreamNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIOMemoryStream);
            ValueTaskNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksValueTask);
            GenericTaskNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask1);
            MonitorNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingMonitor);
            InterlockedNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingInterlocked);
            SerializationInfoNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationSerializationInfo);
            StreamingContextNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeSerializationStreamingContext);
            GenericIEquatableNamedType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIEquatable1);
            StringReaderType = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIOStringReader);
            CollectionNamedTypes = GetWellKnownCollectionTypes();
            DebugAssertMethod = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemDiagnosticsDebug)?.GetMembers("Assert")
                    .OfType<IMethodSymbol>().FirstOrDefault(HasDebugAssertSignature);

            _lValueFlowCaptures = LValueFlowCapturesProvider.GetOrCreateLValueFlowCaptures(analysisContext.ControlFlowGraph);
            _valueCacheBuilder = ImmutableDictionary.CreateBuilder<IOperation, TAbstractAnalysisValue>();
            _predicateValueKindCacheBuilder = ImmutableDictionary.CreateBuilder<IOperation, PredicateValueKind>();
            _pendingArgumentsToReset = [];
            _pendingArgumentsToPostProcess = [];
            _visitedFlowBranchConditions = [];
            _visitedLambdas = [];
            _returnValueOperations = OwningSymbol is IMethodSymbol method && !method.ReturnsVoid ? new HashSet<IOperation>() : null;
            _interproceduralResultsBuilder = ImmutableDictionary.CreateBuilder<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>>();
            _standaloneLocalFunctionAnalysisResultsBuilder = ImmutableDictionary.CreateBuilder<IMethodSymbol, IDataFlowAnalysisResult<TAbstractAnalysisValue>>();
            _escapedLocalFunctions = ImmutableHashSet.CreateBuilder<IMethodSymbol>();
            _analyzedLocalFunctions = ImmutableHashSet.CreateBuilder<IMethodSymbol>();
            _escapedLambdas = ImmutableHashSet.CreateBuilder<IFlowAnonymousFunctionOperation>();
            _analyzedLambdas = ImmutableHashSet.CreateBuilder<IFlowAnonymousFunctionOperation>();

            _interproceduralCallStack = new Stack<IOperation>();
            _addressSharedEntitiesProvider = new AddressSharedEntitiesProvider<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>(analysisContext);
            if (analysisContext.InterproceduralAnalysisData != null)
            {
                foreach (var argumentInfo in analysisContext.InterproceduralAnalysisData.ArgumentValuesMap.Values)
                {
                    CacheAbstractValue(argumentInfo.Operation, argumentInfo.Value);
                }

                foreach (var operation in analysisContext.InterproceduralAnalysisData.CallStack)
                {
                    _interproceduralCallStack.Push(operation);
                }

                _interproceduralMethodToCfgMap = null;
            }
            else
            {
                _interproceduralMethodToCfgMap = [];
            }

            AnalysisEntity? interproceduralInvocationInstance;
            if (analysisContext.InterproceduralAnalysisData?.InvocationInstance.HasValue == true)
            {
                (interproceduralInvocationInstance, ThisOrMePointsToAbstractValue) = analysisContext.InterproceduralAnalysisData.InvocationInstance!.Value;
            }
            else
            {
                ThisOrMePointsToAbstractValue = GetThisOrMeInstancePointsToValue(analysisContext);
                interproceduralInvocationInstance = null;
            }

            var pointsToAnalysisKind = analysisContext is PointsToAnalysisContext pointsToAnalysisContext
                ? pointsToAnalysisContext.PointsToAnalysisKind
                : analysisContext.PointsToAnalysisResult?.PointsToAnalysisKind ?? PointsToAnalysisKind.None;
            HasPointsToAnalysisResult = pointsToAnalysisKind != PointsToAnalysisKind.None;
            HasCompletePointsToAnalysisResult = pointsToAnalysisKind == PointsToAnalysisKind.Complete;

            AnalysisEntityFactory = new AnalysisEntityFactory(
                DataFlowAnalysisContext.ControlFlowGraph,
                DataFlowAnalysisContext.WellKnownTypeProvider,
                getPointsToAbstractValue: HasPointsToAnalysisResult ?
                    GetPointsToAbstractValue :
                    null,
                getIsInsideAnonymousObjectInitializer: () => IsInsideAnonymousObjectInitializer,
                getIsLValueFlowCapture: IsLValueFlowCapture,
                containingTypeSymbol: analysisContext.OwningSymbol.ContainingType,
                interproceduralInvocationInstance: interproceduralInvocationInstance,
                interproceduralThisOrMeInstanceForCaller: analysisContext.InterproceduralAnalysisData?.ThisOrMeInstanceForCaller?.Instance,
                interproceduralCallStack: analysisContext.InterproceduralAnalysisData?.CallStack,
                interproceduralCapturedVariablesMap: analysisContext.InterproceduralAnalysisData?.CapturedVariablesMap,
                interproceduralGetAnalysisEntityForFlowCapture: analysisContext.InterproceduralAnalysisData?.GetAnalysisEntityForFlowCapture,
                getInterproceduralCallStackForOwningSymbol: GetInterproceduralCallStackForOwningSymbol);

            return;

            static bool HasDebugAssertSignature(IMethodSymbol method)
            {
                return method.IsStatic &&
                method.ReturnsVoid &&
                method.Parameters.Length == 1 &&
                method.Parameters[0].Type.SpecialType == SpecialType.System_Boolean;
            }
        }

        protected CopyAbstractValue GetDefaultCopyValue(AnalysisEntity analysisEntity)
                => _addressSharedEntitiesProvider.GetDefaultCopyValue(analysisEntity);

        protected CopyAbstractValue? TryGetAddressSharedCopyValue(AnalysisEntity analysisEntity)
            => _addressSharedEntitiesProvider.TryGetAddressSharedCopyValue(analysisEntity);

        public virtual (TAbstractAnalysisValue Value, PredicateValueKind PredicateValueKind)? GetReturnValueAndPredicateKind()
        {
            if (_returnValueOperations == null ||
                _returnValueOperations.Count == 0)
            {
                if (OwningSymbol is IMethodSymbol method &&
                    !method.ReturnsVoid)
                {
                    // Non-void method without any return statements
                    return (GetAbstractDefaultValue(method.ReturnType), PredicateValueKind.Unknown);
                }

                return null;
            }

            TAbstractAnalysisValue mergedValue = ValueDomain.Bottom;
            PredicateValueKind? mergedPredicateValueKind = null;
            foreach (var operation in _returnValueOperations)
            {
                mergedValue = ValueDomain.Merge(mergedValue, GetAbstractValueForReturnOperation(operation, out _));
                if (PredicateAnalysis)
                {
                    if (!_predicateValueKindCacheBuilder.TryGetValue(operation, out var predicateValueKind))
                    {
                        predicateValueKind = PredicateValueKind.Unknown;
                    }

                    if (!mergedPredicateValueKind.HasValue)
                    {
                        mergedPredicateValueKind = predicateValueKind;
                    }
                    else if (mergedPredicateValueKind.Value != predicateValueKind)
                    {
                        mergedPredicateValueKind = PredicateValueKind.Unknown;
                    }
                }
            }

            return (mergedValue, mergedPredicateValueKind ?? PredicateValueKind.Unknown);
        }

        private static PointsToAbstractValue GetThisOrMeInstancePointsToValue(TAnalysisContext analysisContext)
        {
            var owningSymbol = analysisContext.OwningSymbol;
            if (!owningSymbol.IsStatic &&
                !owningSymbol.ContainingType.HasValueCopySemantics())
            {
                var thisOrMeLocation = AbstractLocation.CreateThisOrMeLocation(owningSymbol.ContainingType, analysisContext.InterproceduralAnalysisData?.CallStack);
                return PointsToAbstractValue.Create(thisOrMeLocation, mayBeNull: false);
            }
            else
            {
                return PointsToAbstractValue.NoLocation;
            }
        }

        /// <summary>
        /// Primary method that flows analysis data through the given statement.
        /// </summary>
        public virtual TAnalysisData Flow(IOperation statement, BasicBlock block, TAnalysisData input)
        {
            CurrentAnalysisData = input;
            Visit(statement, null);
            AfterVisitRoot(statement);
            return CurrentAnalysisData;
        }

        [Conditional("DEBUG")]
        private void AfterVisitRoot(IOperation operation)
        {
            Debug.Assert(_pendingArgumentsToReset.Count == 0);
            Debug.Assert(_pendingArgumentsToPostProcess.Count == 0);

            // Ensure that we visited and cached values for all operation descendants.
            foreach (var descendant in operation.DescendantsAndSelf())
            {
                // GetState will throw an InvalidOperationException if the visitor did not visit the operation or cache it's abstract value.
                var _ = GetCachedAbstractValue(descendant);
            }
        }

        public TAnalysisData OnStartBlockAnalysis(BasicBlock block, TAnalysisData input)
        {
            CurrentBasicBlock = block;
            CurrentAnalysisData = input;

            if (PredicateAnalysis && IsReachableBlockData(input))
            {
                UpdateReachability(block, input, isReachable: GetBlockReachability(block));
            }

            switch (block.Kind)
            {
                case BasicBlockKind.Entry:
                    OnStartEntryBlockAnalysis(block);
                    break;

                case BasicBlockKind.Exit:
                    OnStartExitBlockAnalysis(block);
                    break;

                default:
                    if (AnalysisDataForUnhandledThrowOperations != null && block.IsFirstBlockOfFinally(out _))
                    {
                        MergeAnalysisDataFromUnhandledThrowOperations(caughtExceptionType: null);
                    }

                    break;
            }

            return CurrentAnalysisData;
        }

        public TAnalysisData OnEndBlockAnalysis(BasicBlock block, TAnalysisData analysisData)
        {
            _currentBasicBlock = block;
            CurrentAnalysisData = analysisData;

            if (block.EnclosingRegion != null &&
                block.EnclosingRegion.LastBlockOrdinal == block.Ordinal)
            {
                // Update analysis data for unhandled throw exceptions if we are at the end of a finally region.
                // Note: We must do this before invoking OnLeavingRegion below as that might remove tracking data
                // for keys that are out of scope.
                if (AnalysisDataForUnhandledThrowOperations != null && block.IsLastBlockOfFinally(out var finallyRegion))
                {
                    foreach (var (exceptionInfo, dataAtException) in AnalysisDataForUnhandledThrowOperations)
                    {
                        if (exceptionInfo.ContainingFinallyRegion == null ||
                            !finallyRegion.ContainsRegionOrSelf(exceptionInfo.ContainingFinallyRegion))
                        {
                            AssertValidAnalysisData(dataAtException);
                            UpdateValuesForAnalysisData(dataAtException);
                            AssertValidAnalysisData(dataAtException);
                        }
                    }
                }
            }

            if (block.Kind == BasicBlockKind.Exit)
            {
                OnEndExitBlockAnalysis(block);
            }

            _currentBasicBlock = null;
            return CurrentAnalysisData;
        }

        /// <summary>
        /// Updates values for existing entries in <paramref name="targetAnalysisData"/> with newer values from CurrentAnalysisData.
        /// </summary>
        protected abstract void UpdateValuesForAnalysisData(TAnalysisData targetAnalysisData);

        /// <summary>
        /// Helper method to update analysis data for existing entries in <paramref name="targetAnalysisData"/>
        /// with newer values from <paramref name="newAnalysisData"/>.
        /// </summary>
        protected void UpdateValuesForAnalysisData<TKey>(
            DictionaryAnalysisData<TKey, TAbstractAnalysisValue> targetAnalysisData,
            DictionaryAnalysisData<TKey, TAbstractAnalysisValue> newAnalysisData)
            where TKey : notnull
        {
            using var _ = ArrayBuilder<TKey>.GetInstance(targetAnalysisData.Count, out var builder);
            builder.AddRange(targetAnalysisData.Keys);

            for (int i = 0; i < builder.Count; i++)
            {
                var key = builder[i];
                if (newAnalysisData.TryGetValue(key, out var newValue))
                {
                    targetAnalysisData[key] = newValue;
                }
            }
        }

        protected abstract void StopTrackingDataForParameter(IParameterSymbol parameter, AnalysisEntity analysisEntity);

        protected virtual void StopTrackingDataForParameters(ImmutableDictionary<IParameterSymbol, AnalysisEntity> parameterEntities)
        {
            foreach (var kvp in parameterEntities)
            {
                IParameterSymbol parameter = kvp.Key;
                AnalysisEntity analysisEntity = kvp.Value;

                // Stop tracking parameter values on exit.
                StopTrackingDataForParameter(parameter, analysisEntity);
            }
        }

        private void OnStartEntryBlockAnalysis(BasicBlock entryBlock)
        {
            Debug.Assert(entryBlock.Kind == BasicBlockKind.Entry);

            if (_lazyParameterEntities == null &&
                OwningSymbol is IMethodSymbol method &&
                !method.Parameters.IsEmpty)
            {
                var builder = ImmutableDictionary.CreateBuilder<IParameterSymbol, AnalysisEntity>();
                var argumentValuesMap = DataFlowAnalysisContext.InterproceduralAnalysisData?.ArgumentValuesMap ??
                    ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>>.Empty;

                foreach (var parameter in method.Parameters)
                {
                    var result = AnalysisEntityFactory.TryCreateForSymbolDeclaration(parameter, out var analysisEntity);
                    Debug.Assert(result);
                    RoslynDebug.Assert(analysisEntity != null);
                    builder.Add(parameter, analysisEntity);

                    ArgumentInfo<TAbstractAnalysisValue>? assignedValue = null;
                    if (argumentValuesMap.TryGetValue(parameter.OriginalDefinition, out var argumentInfo))
                    {
                        assignedValue = argumentInfo;
                    }

                    _addressSharedEntitiesProvider.UpdateAddressSharedEntitiesForParameter(parameter, analysisEntity, assignedValue);
                    SetValueForParameterOnEntry(parameter, analysisEntity, assignedValue);
                }

                _lazyParameterEntities = builder.ToImmutable();
            }
        }

        private void OnStartExitBlockAnalysis(BasicBlock exitBlock)
        {
            Debug.Assert(exitBlock.Kind == BasicBlockKind.Exit);

            PerformStandaloneLambdaOrLocalFunctionAnalysisOnExit();

            if (_lazyParameterEntities != null)
            {
                foreach (var kvp in _lazyParameterEntities)
                {
                    IParameterSymbol parameter = kvp.Key;
                    AnalysisEntity analysisEntity = kvp.Value;

                    // Escape parameter values on exit, except for ref/out parameters in interprocedural analysis.
                    if (parameter.RefKind == RefKind.None || DataFlowAnalysisContext.InterproceduralAnalysisData == null)
                    {
                        EscapeValueForParameterOnExit(parameter, analysisEntity);
                    }
                }
            }
        }

        private void PerformStandaloneLambdaOrLocalFunctionAnalysisOnExit()
        {
            // First append the escaped local functions and lambdas from points to result, if any.
            if (DataFlowAnalysisContext.PointsToAnalysisResult is { } pointsToAnalysisResult)
            {
                _escapedLocalFunctions.AddRange(pointsToAnalysisResult.LambdaAndLocalFunctionAnalysisInfo.EscapedLocalFunctions);
                _escapedLambdas.AddRange(pointsToAnalysisResult.LambdaAndLocalFunctionAnalysisInfo.EscapedLambdas);
            }

            // Perform standalone analysis for local functions, if required.
            foreach (var localFunction in DataFlowAnalysisContext.ControlFlowGraph.LocalFunctions)
            {
                if (IsStandaloneAnalysisRequiredForLocalFunction(localFunction))
                {
                    PerformStandaloneLocalFunctionInterproceduralAnalysis(localFunction);
                }
            }

            // Perform standalone analysis for lambdas, if required.
            foreach (var lambda in _visitedLambdas)
            {
                if (IsStandaloneAnalysisRequiredForLambda(lambda))
                {
                    PerformStandaloneLambdaInterproceduralAnalysis(lambda);
                }
            }
        }

        private bool IsStandaloneAnalysisRequiredForLocalFunction(IMethodSymbol localFunction)
        {
            Debug.Assert(localFunction.MethodKind == MethodKind.LocalFunction);
            Debug.Assert(DataFlowAnalysisContext.ControlFlowGraph.LocalFunctions.Contains(localFunction));

            // Perform standalone analysis for local functions that escaped or were not analyzed.
            return _escapedLocalFunctions.Contains(localFunction) || !_analyzedLocalFunctions.Contains(localFunction);
        }

        private bool IsStandaloneAnalysisRequiredForLambda(IFlowAnonymousFunctionOperation lambda)
        {
            Debug.Assert(_visitedLambdas.Contains(lambda));

            // Perform standalone analysis for lambdas that escaped or were not analyzed.
            return _escapedLambdas.Contains(lambda) || !_analyzedLambdas.Contains(lambda);
        }

        private void OnEndExitBlockAnalysis(BasicBlock exitBlock)
        {
            Debug.Assert(exitBlock.Kind == BasicBlockKind.Exit);

            if (DataFlowAnalysisContext.ExceptionPathsAnalysis && !ExecutingExceptionPathsAnalysisPostPass)
            {
                // We are going to perform another analysis pass for computing data on exception paths.
                // So delay all the exit block analysis until that is done.
                return;
            }

            // For context-sensitive interprocedural analysis, we need to stop tracking data for the parameters
            // as they will no longer be in caller's analysis scope.
            if (_lazyParameterEntities != null && DataFlowAnalysisContext.InterproceduralAnalysisData != null)
            {
                // Reset address shared entities to caller's address shared entities.
                _addressSharedEntitiesProvider.SetAddressSharedEntities(DataFlowAnalysisContext.InterproceduralAnalysisData.AddressSharedEntities);
                StopTrackingDataForParameters(_lazyParameterEntities);
            }
        }

        protected bool IsParameterEntityForCurrentMethod(AnalysisEntity analysisEntity)
            => analysisEntity.Symbol is IParameterSymbol parameter &&
            _lazyParameterEntities != null &&
            _lazyParameterEntities.TryGetValue(parameter, out var parameterEntity) &&
            parameterEntity == analysisEntity;

        /// <summary>
        /// Primary method that flows analysis data through the given flow edge/branch.
        /// Returns false if the branch is conditional and the branch value always evaluates to false.
        /// </summary>
        public virtual (TAnalysisData output, bool isFeasibleBranch) FlowBranch(
            BasicBlock fromBlock,
            BranchWithInfo branch,
            TAnalysisData input)
        {
            var isFeasibleBranch = true;
            CurrentBasicBlock = fromBlock;
            CurrentAnalysisData = input;

            if (branch.BranchValue != null)
            {
                FlowBranchConditionKind = branch.ControlFlowConditionKind;
                Visit(branch.BranchValue, null);

                if (branch.ControlFlowConditionKind != ControlFlowConditionKind.None)
                {
                    // We visit the condition twice - once for the condition true branch, and once for the condition false branch.
                    // Below check ensures we execute AfterVisitRoot only once.
                    if (!_visitedFlowBranchConditions.Add(branch.BranchValue))
                    {
                        AfterVisitRoot(branch.BranchValue);
                        _visitedFlowBranchConditions.Remove(branch.BranchValue);
                    }

                    if (isConditionalBranchNeverTaken())
                    {
                        isFeasibleBranch = false;
                    }
                }
                else
                {
                    AfterVisitRoot(branch.BranchValue);
                }

                FlowBranchConditionKind = ControlFlowConditionKind.None;
            }

            // Special handling for return and throw branches.
            switch (branch.Kind)
            {
                case ControlFlowBranchSemantics.Return:
                    ProcessReturnValue(branch.BranchValue);
                    break;

                case ControlFlowBranchSemantics.Throw:
                case ControlFlowBranchSemantics.Rethrow:
                    // Update the tracked merged analysis data at throw branches.
                    var thrownExceptionType = branch.BranchValue?.Type ?? CurrentBasicBlock.GetEnclosingRegionExceptionType();
                    if (thrownExceptionType is INamedTypeSymbol exceptionType &&
                        exceptionType.DerivesFrom(ExceptionNamedType, baseTypesOnly: true))
                    {
                        AnalysisDataForUnhandledThrowOperations ??= [];
                        var info = ThrownExceptionInfo.Create(CurrentBasicBlock, exceptionType, DataFlowAnalysisContext.InterproceduralAnalysisData?.CallStack);
                        AnalysisDataForUnhandledThrowOperations[info] = GetClonedCurrentAnalysisData();
                    }

                    ProcessThrowValue(branch.BranchValue);
                    break;
            }

            return (CurrentAnalysisData, isFeasibleBranch);

            bool isConditionalBranchNeverTaken()
            {
                RoslynDebug.Assert(branch.BranchValue != null);
                Debug.Assert(branch.ControlFlowConditionKind != ControlFlowConditionKind.None);

                if (branch.BranchValue.Type?.SpecialType == SpecialType.System_Boolean &&
                    branch.BranchValue.ConstantValue.HasValue)
                {
                    var alwaysTrue = (bool)branch.BranchValue.ConstantValue.Value!;
                    if (alwaysTrue && branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenFalse ||
                        !alwaysTrue && branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenTrue)
                    {
                        return true;
                    }
                }

                if (PredicateAnalysis &&
                    _predicateValueKindCacheBuilder.TryGetValue(branch.BranchValue, out PredicateValueKind valueKind) &&
                    isPredicateAlwaysFalseForBranch(valueKind))
                {
                    return true;
                }

                if (DataFlowAnalysisContext.PointsToAnalysisResult != null &&
                    isPredicateAlwaysFalseForBranch(DataFlowAnalysisContext.PointsToAnalysisResult.GetPredicateKind(branch.BranchValue)))
                {
                    return true;
                }

                if (DataFlowAnalysisContext.CopyAnalysisResult != null &&
                    isPredicateAlwaysFalseForBranch(DataFlowAnalysisContext.CopyAnalysisResult.GetPredicateKind(branch.BranchValue)))
                {
                    return true;
                }

                if (DataFlowAnalysisContext.ValueContentAnalysisResult != null &&
                    isPredicateAlwaysFalseForBranch(DataFlowAnalysisContext.ValueContentAnalysisResult.GetPredicateKind(branch.BranchValue)))
                {
                    return true;
                }

                return false;
            }

            bool isPredicateAlwaysFalseForBranch(PredicateValueKind predicateValueKind)
            {
                Debug.Assert(branch.ControlFlowConditionKind != ControlFlowConditionKind.None);

                return predicateValueKind switch
                {
                    PredicateValueKind.AlwaysFalse => branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenTrue,

                    PredicateValueKind.AlwaysTrue => branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenFalse,

                    _ => false,
                };
            }
        }

        /// <summary>
        /// Get analysis value for an implicitly created completed task wrapping a returned value in an async method.
        /// For example, "return 0;" in an async method returning "Task(Of int)".
        /// </summary>
        private protected virtual TAbstractAnalysisValue GetAbstractValueForImplicitWrappingTaskCreation(
            IOperation returnValueOperation,
            TAbstractAnalysisValue returnValue,
            PointsToAbstractValue implicitTaskPointsToValue)
        {
            // Conservatively assume default unknown value for implicit task.
            return ValueDomain.UnknownOrMayBeValue;
        }

        protected virtual void ProcessReturnValue(IOperation? returnValue)
        {
            if (returnValue != null)
            {
                _returnValueOperations?.Add(returnValue);

                _ = GetAbstractValueForReturnOperation(returnValue, out var implicitTaskPointsToValueOpt);
                if (implicitTaskPointsToValueOpt != null)
                {
                    Debug.Assert(implicitTaskPointsToValueOpt.Kind == PointsToAbstractValueKind.KnownLocations);
                    SetTaskWrappedValue(implicitTaskPointsToValueOpt, GetCachedAbstractValue(returnValue));
                }
            }
        }

        private TAbstractAnalysisValue GetAbstractValueForReturnOperation(IOperation returnValueOperation, out PointsToAbstractValue? implicitTaskPointsToValue)
        {
            implicitTaskPointsToValue = null;
            var returnValue = GetCachedAbstractValue(returnValueOperation);

            // Check if returned value is wrapped in an implicitly created completed task in an async method.
            // For example, "return 0;" in an async method returning "Task<int>".
            // If so, we return the abstract value for the task wrapping the underlying return value.
            if (OwningSymbol is IMethodSymbol method &&
                method.IsAsync &&
                SymbolEqualityComparer.Default.Equals(method.ReturnType.OriginalDefinition, GenericTaskNamedType) &&
                !SymbolEqualityComparer.Default.Equals(method.ReturnType, returnValueOperation.Type))
            {
                var location = AbstractLocation.CreateAllocationLocation(returnValueOperation, method.ReturnType, DataFlowAnalysisContext.InterproceduralAnalysisData?.CallStack);
                implicitTaskPointsToValue = PointsToAbstractValue.Create(location, mayBeNull: false);
                return GetAbstractValueForImplicitWrappingTaskCreation(returnValueOperation, returnValue, implicitTaskPointsToValue);
            }

            return returnValue;
        }

        protected virtual void HandlePossibleThrowingOperation(IOperation operation)
        {
            Debug.Assert(ExecutingExceptionPathsAnalysisPostPass);
            Debug.Assert(!SkipExceptionPathsAnalysisPostPass);

            // Bail out if we are not analyzing an interprocedural call and there is no
            // tracked analysis data.
            if (!HasAnyAbstractValue(CurrentAnalysisData) &&
                DataFlowAnalysisContext.InterproceduralAnalysisData == null)
            {
                return;
            }

            // Bail out if System.Exception is not defined.
            if (ExceptionNamedType == null)
            {
                return;
            }

            IOperation? instance = null;
            IOperation? invocation = null;
            switch (operation)
            {
                case IMemberReferenceOperation memberReference:
                    instance = memberReference.Instance;
                    break;

                case IDynamicMemberReferenceOperation dynamicMemberReference:
                    instance = dynamicMemberReference.Instance;
                    break;

                case IArrayElementReferenceOperation arrayElementReference:
                    instance = arrayElementReference.ArrayReference;
                    break;

                case IInvocationOperation invocationOp:
                    instance = invocationOp.Instance;
                    invocation = operation;
                    break;

                case IObjectCreationOperation objectCreation:
                    if (objectCreation.Constructor?.IsImplicitlyDeclared == true)
                    {
                        // Implicitly generated constructor should not throw.
                        return;
                    }

                    invocation = operation;
                    break;

                default:
                    // Optimistically assume the operation cannot throw.
                    return;
            }

            var invocationInstanceAccessCanThrow = instance != null &&
                instance.Kind != OperationKind.InstanceReference &&
                GetNullAbstractValue(instance) != NullAbstractValue.NotNull;
            var invocationCanThrow = invocation != null && !TryGetInterproceduralAnalysisResult(operation, out _);
            if (!invocationInstanceAccessCanThrow && !invocationCanThrow)
            {
                // Cannot throw an exception from instance access and
                // interprocedural analysis already handles possible exception from invoked code.
                return;
            }

            // This operation can throw, so update the analysis data for unhandled exception with 'System.Exception' type.
            AnalysisDataForUnhandledThrowOperations ??= [];
            if (!AnalysisDataForUnhandledThrowOperations.TryGetValue(DefaultThrownExceptionInfo, out TAnalysisData? data) ||
                CurrentBasicBlock.IsContainedInRegionOfKind(ControlFlowRegionKind.Finally))
            {
                data = null;
            }

            data = GetMergedAnalysisDataForPossibleThrowingOperation(data, operation);
            RoslynDebug.Assert(data != null);
            AssertValidAnalysisData(data);
            AnalysisDataForUnhandledThrowOperations[DefaultThrownExceptionInfo] = data!;
        }

        protected virtual TAnalysisData GetMergedAnalysisDataForPossibleThrowingOperation(TAnalysisData? existingData, IOperation operation)
        {
            Debug.Assert(ExecutingExceptionPathsAnalysisPostPass);
            Debug.Assert(!SkipExceptionPathsAnalysisPostPass);

            return existingData == null ?
                GetClonedCurrentAnalysisData() :
                MergeAnalysisData(CurrentAnalysisData, existingData);
        }

        public TAnalysisData OnLeavingRegions(
            IEnumerable<ILocalSymbol> leavingRegionLocals,
            IEnumerable<CaptureId> leavingRegionFlowCaptures,
            BasicBlock currentBasicBlock,
            TAnalysisData input)
        {
            if (!leavingRegionLocals.Any() && !leavingRegionFlowCaptures.Any())
            {
                return input;
            }

            CurrentBasicBlock = currentBasicBlock;
            CurrentAnalysisData = input;

            ProcessOutOfScopeLocalsAndFlowCaptures(leavingRegionLocals, leavingRegionFlowCaptures);

            _currentBasicBlock = null;
            return CurrentAnalysisData;
        }

        protected virtual void ProcessOutOfScopeLocalsAndFlowCaptures(IEnumerable<ILocalSymbol> locals, IEnumerable<CaptureId> flowCaptures)
        {
            Debug.Assert(locals.Any() || flowCaptures.Any());

            if (PredicateAnalysis)
            {
                foreach (var captureId in flowCaptures)
                {
                    if (AnalysisEntityFactory.TryGetForFlowCapture(captureId, out var analysisEntity) &&
                        HasPredicatedDataForEntity(analysisEntity))
                    {
                        StopTrackingPredicatedData(analysisEntity);
                    }
                }
            }
        }

        private bool IsContractCheckArgument(IArgumentOperation operation)
            => operation.Parent is IInvocationOperation invocation &&
               invocation.Arguments[0] == operation &&
               (IsAnyDebugAssertMethod(invocation.TargetMethod) || IsContractCheckMethod(invocation.TargetMethod));

        private bool IsContractCheckMethod(IMethodSymbol method)
        {
            if (Equals(method.ContainingType, ContractNamedType) &&
                method.IsStatic)
            {
                if (_lazyContractCheckMethods == null)
                {
                    // Contract.Requires check.
                    var requiresMethods = ContractNamedType.GetMembers("Requires");
                    var assumeMethods = ContractNamedType.GetMembers("Assume");
                    var assertMethods = ContractNamedType.GetMembers("Assert");
                    var validationMethods = requiresMethods.Concat(assumeMethods).Concat(assertMethods).OfType<IMethodSymbol>().Where(m => m.IsStatic && m.ReturnsVoid && !m.Parameters.IsEmpty && (m.Parameters[0].Type.SpecialType == SpecialType.System_Boolean));
                    _lazyContractCheckMethods = ImmutableHashSet.CreateRange(validationMethods);
                }

                return _lazyContractCheckMethods.Contains(method);
            }

            return false;
        }

        /// <summary>
        /// Checks if the method is an overload of the <see cref="Debug.Assert(bool)"/> method.
        /// </summary>
        /// <param name="method">The IMethodSymbol to test.</param>
        /// <returns>True if the method is an overlaod of the <see cref="Debug.Assert(bool)"/> method.</returns>
        private bool IsAnyDebugAssertMethod(IMethodSymbol method) =>
            DebugAssertMethod != null &&
            method.ContainingSymbol.Equals(DebugAssertMethod.ContainingSymbol, SymbolEqualityComparer.Default) &&
            method.Name == DebugAssertMethod.Name &&
            method.ReturnType == DebugAssertMethod.ReturnType;

        protected bool IsAnyAssertMethod(IMethodSymbol method)
            => IsAnyDebugAssertMethod(method) || IsContractCheckMethod(method);

        #region Helper methods to get or cache analysis data for visited operations.

        internal ImmutableDictionary<IOperation, TAbstractAnalysisValue> GetStateMap() => _valueCacheBuilder.ToImmutable();

        internal ImmutableDictionary<IOperation, PredicateValueKind> GetPredicateValueKindMap() => _predicateValueKindCacheBuilder.ToImmutable();

        public virtual TAnalysisData? GetMergedDataForUnhandledThrowOperations()
        {
            if (AnalysisDataForUnhandledThrowOperations == null)
            {
                return null;
            }

            TAnalysisData? mergedData = null;
            foreach (TAnalysisData data in AnalysisDataForUnhandledThrowOperations.Values)
            {
                mergedData = mergedData != null ? MergeAnalysisData(mergedData, data) : data;
            }

#if DEBUG
            if (mergedData != null)
            {
                AssertValidAnalysisData(mergedData);
            }
#endif
            return mergedData;
        }

        public TAbstractAnalysisValue GetCachedAbstractValue(IOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (_valueCacheBuilder.TryGetValue(operation, out var state))
            {
                return state;
            }

            if (DataFlowAnalysisContext.InterproceduralAnalysisData != null)
            {
                return DataFlowAnalysisContext.InterproceduralAnalysisData.GetCachedAbstractValueFromCaller(operation);
            }

            // We were unable to find cached abstract value for requested operation.
            // We should never reach this path, except for one known case.
            // For interprocedural analysis, we might reach here when attempting to access abstract value for instance receiver
            // of a method delegate which was saved in a prior interprocedural call chain, that returned back to the root caller
            // and a different interprocedural call tree indirectly invokes that saved method delegate.
            // We correctly resolve the method delegate target, but would need pretty complicated implementation to reach that
            // operation's analysis result.
            // See unit test 'DisposeObjectsBeforeLosingScopeTests.InvocationOfMethodDelegate_PriorInterproceduralCallChain' for an example.
            // For now, we just gracefully return an unknown abstract value for this case.
            if (DataFlowAnalysisContext.InterproceduralAnalysisConfiguration.InterproceduralAnalysisKind != InterproceduralAnalysisKind.None)
            {
                return ValueDomain.UnknownOrMayBeValue;
            }

            throw new InvalidOperationException();
        }

        protected void CacheAbstractValue(IOperation operation, TAbstractAnalysisValue value)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            _valueCacheBuilder[operation] = value;
        }

        protected NullAbstractValue GetNullAbstractValue(IOperation operation) => GetPointsToAbstractValue(operation).NullState;

        protected virtual CopyAbstractValue GetCopyAbstractValue(IOperation operation)
        {
            if (DataFlowAnalysisContext.CopyAnalysisResult == null)
            {
                return CopyAbstractValue.Unknown;
            }
            else
            {
                return DataFlowAnalysisContext.CopyAnalysisResult[operation];
            }
        }

        protected virtual PointsToAbstractValue GetPointsToAbstractValue(IOperation operation)
        {
            if (DataFlowAnalysisContext.PointsToAnalysisResult == null)
            {
                return PointsToAbstractValue.Unknown;
            }
            else
            {
                return DataFlowAnalysisContext.PointsToAnalysisResult[operation];
            }
        }

        protected virtual ValueContentAbstractValue GetValueContentAbstractValue(IOperation operation)
        {
            if (DataFlowAnalysisContext.ValueContentAnalysisResult == null)
            {
                return ValueContentAbstractValue.MayBeContainsNonLiteralState;
            }
            else
            {
                return DataFlowAnalysisContext.ValueContentAnalysisResult[operation];
            }
        }

        protected ImmutableHashSet<AbstractLocation> GetEscapedLocations(IOperation operation)
        {
            if (operation == null || DataFlowAnalysisContext.PointsToAnalysisResult == null)
            {
                return ImmutableHashSet<AbstractLocation>.Empty;
            }
            else
            {
                return DataFlowAnalysisContext.PointsToAnalysisResult.GetEscapedAbstractLocations(operation);
            }
        }

        protected ImmutableHashSet<AbstractLocation> GetEscapedLocations(AnalysisEntity parameterEntity)
        {
            Debug.Assert(parameterEntity.Symbol?.Kind == SymbolKind.Parameter);
            if (parameterEntity == null || DataFlowAnalysisContext.PointsToAnalysisResult == null)
            {
                return ImmutableHashSet<AbstractLocation>.Empty;
            }
            else
            {
                return DataFlowAnalysisContext.PointsToAnalysisResult.GetEscapedAbstractLocations(parameterEntity);
            }
        }

        protected bool TryGetPointsToAbstractValueAtEntryBlockEnd(AnalysisEntity analysisEntity, [NotNullWhen(true)] out PointsToAbstractValue? pointsToAbstractValue)
        {
            Debug.Assert(CurrentBasicBlock.Kind == BasicBlockKind.Entry);
            RoslynDebug.Assert(DataFlowAnalysisContext.PointsToAnalysisResult != null);

            var outputData = DataFlowAnalysisContext.PointsToAnalysisResult.EntryBlockOutput.Data;
            return outputData.TryGetValue(analysisEntity, out pointsToAbstractValue);
        }

        protected bool TryGetNullAbstractValueAtCurrentBlockEntry(AnalysisEntity analysisEntity, out NullAbstractValue nullAbstractValue)
        {
            RoslynDebug.Assert(DataFlowAnalysisContext.PointsToAnalysisResult != null);
            var inputData = DataFlowAnalysisContext.PointsToAnalysisResult[CurrentBasicBlock].Data;
            if (inputData.TryGetValue(analysisEntity, out PointsToAbstractValue? pointsToAbstractValue))
            {
                nullAbstractValue = pointsToAbstractValue.NullState;
                return true;
            }

            nullAbstractValue = NullAbstractValue.MaybeNull;
            return false;
        }

        protected bool TryGetMergedNullAbstractValueAtUnhandledThrowOperationsInGraph(AnalysisEntity analysisEntity, out NullAbstractValue nullAbstractValue)
        {
            RoslynDebug.Assert(DataFlowAnalysisContext.PointsToAnalysisResult != null);
            var inputData = DataFlowAnalysisContext.PointsToAnalysisResult.MergedStateForUnhandledThrowOperations?.Data;
            if (inputData == null || !inputData.TryGetValue(analysisEntity, out PointsToAbstractValue? pointsToAbstractValue))
            {
                nullAbstractValue = NullAbstractValue.MaybeNull;
                return false;
            }

            nullAbstractValue = pointsToAbstractValue.NullState;
            return true;
        }

        private protected void SetTaskWrappedValue(PointsToAbstractValue pointsToValueForTask, TAbstractAnalysisValue wrappedValue)
        {
            if (pointsToValueForTask.Kind == PointsToAbstractValueKind.Unknown)
            {
                return;
            }

            TaskWrappedValuesMap ??= [];
            TaskWrappedValuesMap[pointsToValueForTask] = wrappedValue;
        }

        private protected bool TryGetTaskWrappedValue(PointsToAbstractValue pointsToAbstractValue, out TAbstractAnalysisValue wrappedValue)
        {
            if (TaskWrappedValuesMap == null)
            {
                wrappedValue = ValueDomain.UnknownOrMayBeValue;
                return false;
            }

            return TaskWrappedValuesMap.TryGetValue(pointsToAbstractValue, out wrappedValue);
        }

        protected virtual TAbstractAnalysisValue ComputeAnalysisValueForReferenceOperation(IOperation operation, TAbstractAnalysisValue defaultValue)
        {
            return defaultValue;
        }

        protected virtual TAbstractAnalysisValue ComputeAnalysisValueForEscapedRefOrOutArgument(IArgumentOperation operation, TAbstractAnalysisValue defaultValue)
        {
            return defaultValue;
        }

        internal bool TryInferConversion(IConversionOperation operation, out ConversionInference inference)
        {
            inference = ConversionInference.Create(operation);

            // Bail out for user defined conversions.
            if (operation.Conversion.IsUserDefined)
            {
                return true;
            }

            // Bail out if conversion does not exist (error code).
            if (!operation.Conversion.Exists)
            {
                return false;
            }

            return TryInferConversion(operation.Operand, operation.Type, operation.IsTryCast, operation, out inference);
        }

        internal bool TryInferConversion(IIsPatternOperation operation, out ConversionInference inference)
        {
            var targetType = operation.Pattern.GetPatternType();
            return TryInferConversion(operation.Value, targetType, isTryCast: true, operation, out inference);
        }

        private bool TryInferConversion(
            IOperation sourceOperand,
            ITypeSymbol? targetType,
            bool isTryCast,
            IOperation operation,
            out ConversionInference inference)
        {
            inference = ConversionInference.Create(targetType, sourceOperand.Type, isTryCast);

            // Bail out for throw expression conversion.
            if (sourceOperand.Kind == OperationKind.Throw)
            {
                return true;
            }

            // Analyze if cast might always succeed or fail based on points to analysis result.
            var pointsToValue = GetPointsToAbstractValue(sourceOperand);
            if (pointsToValue.Kind == PointsToAbstractValueKind.KnownLocations)
            {
                // Bail out if we have a possible null location for direct cast.
                if (!isTryCast && pointsToValue.Locations.Any(location => location.IsNull))
                {
                    return true;
                }

                // Handle is pattern operations with constant pattern.
                if (operation is IIsPatternOperation isPatternOperation)
                {
                    IPatternOperation patternOperation = isPatternOperation.Pattern;
                    bool direct = true;

                    if (patternOperation is INegatedPatternOperation negatedPattern)
                    {
                        patternOperation = negatedPattern.Pattern;
                        direct = false;
                    }

                    if (patternOperation is IConstantPatternOperation { Value.ConstantValue: { HasValue: true, Value: null } })
                    {
                        switch (pointsToValue.NullState)
                        {
                            case NullAbstractValue.Null:
                                inference.AlwaysSucceed = direct;
                                break;

                            case NullAbstractValue.NotNull:
                                inference.AlwaysFail = direct;
                                break;
                        }

                        return true;
                    }
                }

                if (targetType == null)
                {
                    Debug.Fail($"Unexpected 'null' target type for '{operation.Syntax}'");
                    return false;
                }

                // Infer if a cast will always fail.
                if (!inference.IsBoxing &&
                    !inference.IsUnboxing &&
                    !IsInterfaceOrTypeParameter(targetType) &&
                    pointsToValue.Locations.All(location => location.IsNull ||
                        (!location.IsNoLocation &&
                         !IsInterfaceOrTypeParameter(location.LocationType) &&
                         !targetType.DerivesFrom(location.LocationType) &&
                         !location.LocationType.DerivesFrom(targetType))))
                {
                    if (PredicateAnalysis)
                    {
                        _predicateValueKindCacheBuilder[operation] = PredicateValueKind.AlwaysFalse;
                    }

                    // We only set the alwaysFail flag for TryCast as direct casts that are guaranteed to fail will throw an exception and subsequent code will not execute.
                    if (isTryCast)
                    {
                        inference.AlwaysFail = true;
                    }
                }
                else
                {
                    // Infer if a TryCast will always succeed.
                    if (isTryCast &&
                        pointsToValue.Locations.All(location => location.IsNoLocation || !location.IsNull && location.LocationType.DerivesFrom(targetType)))
                    {
                        // TryCast which is guaranteed to succeed, and potentially can be changed to DirectCast.
                        if (PredicateAnalysis)
                        {
                            _predicateValueKindCacheBuilder[operation] = PredicateValueKind.AlwaysTrue;
                        }

                        inference.AlwaysSucceed = true;
                    }
                }

                return true;
            }

            return false;

            // We are currently bailing out if an interface or type parameter is involved.
            static bool IsInterfaceOrTypeParameter(ITypeSymbol? type) => type?.TypeKind is TypeKind.Interface or TypeKind.TypeParameter;
        }

        #endregion

        #region Predicate analysis

        protected virtual void UpdateReachability(BasicBlock basicBlock, TAnalysisData analysisData, bool isReachable)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        protected virtual bool IsReachableBlockData(TAnalysisData analysisData)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        private bool GetBlockReachability(BasicBlock basicBlock)
        {
            return basicBlock.IsReachable &&
                (DataFlowAnalysisContext.CopyAnalysisResult == null || DataFlowAnalysisContext.CopyAnalysisResult[basicBlock].IsReachable) &&
                (DataFlowAnalysisContext.PointsToAnalysisResult == null || DataFlowAnalysisContext.PointsToAnalysisResult[basicBlock].IsReachable) &&
                (DataFlowAnalysisContext.ValueContentAnalysisResult == null || DataFlowAnalysisContext.ValueContentAnalysisResult[basicBlock].IsReachable);
        }

        protected bool IsCurrentBlockReachable()
        {
            if (PredicateAnalysis)
            {
                return IsReachableBlockData(CurrentAnalysisData);
            }

            return GetBlockReachability(CurrentBasicBlock);
        }

        private void PerformPredicateAnalysis(IOperation operation)
        {
            Debug.Assert(PredicateAnalysis);
            Debug.Assert(operation.Kind is OperationKind.BinaryOperator or
                OperationKind.UnaryOperator or
                OperationKind.IsNull or
                OperationKind.Invocation or
                OperationKind.Argument or
                OperationKind.FlowCaptureReference or
                OperationKind.IsPattern);

            if (FlowBranchConditionKind == ControlFlowConditionKind.None || !IsRootOfCondition())
            {
                // Operation is a predicate which is not a conditional.
                // For example, "x = operation", where operation is "a == b".
                // Check if we need to perform predicate analysis for the operation and/or set/transfer predicate data.

                // First find out if this operation is being captured.
                AnalysisEntity? predicatedFlowCaptureEntity = GetPredicatedFlowCaptureEntity();
                if (predicatedFlowCaptureEntity == null)
                {
                    // Operation is not being flow captured, we may have to perform predicate analysis.
                    if (operation.Kind == OperationKind.FlowCaptureReference)
                    {
                        // "x = FCR0"
                        // No predicate analysis required.
                        return;
                    }

                    // Perform predicate analysis with dummy CurrentAnalysisData to set predicate value kind (always true/false/null).
#if DEBUG
                    TAnalysisData savedCurrentAnalysisData = GetClonedCurrentAnalysisData();
#endif
                    FlowBranchConditionKind = ControlFlowConditionKind.WhenTrue;
                    var dummyTargetPredicateData = GetClonedCurrentAnalysisData();
                    PerformPredicateAnalysisCore(operation, dummyTargetPredicateData);
                    FlowBranchConditionKind = ControlFlowConditionKind.None;
#if DEBUG
                    Debug.Assert(Equals(savedCurrentAnalysisData, CurrentAnalysisData), "Expected no updates to CurrentAnalysisData");
                    savedCurrentAnalysisData.Dispose();
#endif
                    dummyTargetPredicateData.Dispose();
                }
                else
                {
                    // Operation is being flow captured, i.e. "FC = operation"
                    if (operation.Kind == OperationKind.FlowCaptureReference)
                    {
                        // FC = FCR0
                        var result = AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity? flowCaptureReferenceEntity);
                        Debug.Assert(result);
                        RoslynDebug.Assert(flowCaptureReferenceEntity != null);
                        RoslynDebug.Assert(flowCaptureReferenceEntity.CaptureId != null);
                        Debug.Assert(HasPredicatedDataForEntity(flowCaptureReferenceEntity));
                        TransferPredicatedData(fromEntity: flowCaptureReferenceEntity, toEntity: predicatedFlowCaptureEntity);
                    }
                    else
                    {
                        //  "FC = (a == b)"
                        // Perform predicate analysis for both true/false result and start tracking data predicated on flow capture variable.

#if DEBUG
                        TAnalysisData savedCurrentAnalysisData = GetClonedCurrentAnalysisData();
#endif
                        TAnalysisData truePredicatedData = GetEmptyAnalysisData();
                        FlowBranchConditionKind = ControlFlowConditionKind.WhenTrue;
                        PerformPredicateAnalysisCore(operation, truePredicatedData);
                        Debug.Assert(!ReferenceEquals(truePredicatedData, CurrentAnalysisData));

                        TAnalysisData falsePredicatedData = GetEmptyAnalysisData();
                        FlowBranchConditionKind = ControlFlowConditionKind.WhenFalse;
                        PerformPredicateAnalysisCore(operation, falsePredicatedData);
                        Debug.Assert(!ReferenceEquals(falsePredicatedData, CurrentAnalysisData));
                        FlowBranchConditionKind = ControlFlowConditionKind.None;

#if DEBUG
                        Debug.Assert(Equals(savedCurrentAnalysisData, CurrentAnalysisData), "Expected no updates to CurrentAnalysisData");
                        savedCurrentAnalysisData.Dispose();
#endif

                        if (HasAnyAbstractValue(truePredicatedData) || HasAnyAbstractValue(falsePredicatedData))
                        {
                            StartTrackingPredicatedData(predicatedFlowCaptureEntity, truePredicatedData, falsePredicatedData);
                        }
                        else
                        {
                            truePredicatedData.Dispose();
                            falsePredicatedData.Dispose();
                        }
                    }
                }
            }
            else
            {
                PerformPredicateAnalysisCore(operation, CurrentAnalysisData);
            }

            return;

            // local functions
            bool IsRootOfCondition()
            {
                // Special case for contract check argument
                if (operation.Kind == OperationKind.Argument)
                {
                    Debug.Assert(IsContractCheckArgument((IArgumentOperation)operation));
                    return true;
                }

                var current = operation.Parent;
                while (current != null)
                {
                    switch (current.Kind)
                    {
                        case OperationKind.Conversion:
                        case OperationKind.Parenthesized:
                            current = current.Parent;
                            continue;

                        default:
                            return false;
                    }
                }

                return current == null;
            }

            AnalysisEntity? GetPredicatedFlowCaptureEntity()
            {
                var current = operation.Parent;
                while (current != null)
                {
                    switch (current.Kind)
                    {
                        case OperationKind.Conversion:
                        case OperationKind.Parenthesized:
                            current = current.Parent;
                            continue;

                        case OperationKind.FlowCapture:
                            if (AnalysisEntityFactory.TryCreate(current, out var targetEntity) &&
                                targetEntity.IsCandidatePredicateEntity())
                            {
                                Debug.Assert(targetEntity.CaptureId != null);
                                return targetEntity;
                            }

                            return null;

                        default:
                            return null;
                    }
                }

                return null;
            }
        }

        private void PerformPredicateAnalysisCore(IOperation operation, TAnalysisData targetAnalysisData)
        {
            Debug.Assert(PredicateAnalysis);
            Debug.Assert(FlowBranchConditionKind != ControlFlowConditionKind.None);

            PredicateValueKind predicateValueKind = PredicateValueKind.Unknown;
            switch (operation)
            {
                case IIsNullOperation isNullOperation:
                    // Predicate analysis for null checks.
                    predicateValueKind = SetValueForIsNullComparisonOperator(isNullOperation.Operand, equals: FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue, targetAnalysisData: targetAnalysisData);
                    break;

                case IIsPatternOperation isPatternOperation:
                    // Predicate analysis for "is pattern" checks:
                    //  1. Non-null value check for declaration pattern, i.e. "c is D d"
                    //  2. Non-null value check for discard pattern, i.e. "c is D _"
                    //  3. Non-null value check for recursive pattern, i.e. "c is D { SomeProperty: 0 }"
                    //  4. Equality value check for constant pattern, i.e. "x is 1"
                    switch (isPatternOperation.Pattern.Kind)
                    {
                        case OperationKind.DeclarationPattern:
                            if (!((IDeclarationPatternOperation)isPatternOperation.Pattern).MatchesNull)
                            {
                                // Set predicated null/non-null value for declared pattern variable, i.e. for 'd' in "c is D d".
                                predicateValueKind = SetValueForIsNullComparisonOperator(isPatternOperation.Pattern, equals: FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse, targetAnalysisData: targetAnalysisData);
                            }

                            // Also set the predicated value for pattern value for true branch, i.e. for 'c' in "c is D d",
                            // while explicitly ignore the returned 'predicateValueKind'.
                            if (FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue)
                            {
                                _ = SetValueForIsNullComparisonOperator(isPatternOperation.Value, equals: false, targetAnalysisData: targetAnalysisData);
                            }

                            break;

                        case OperationKind.DiscardPattern:
                        case OperationKind.RecursivePattern:
                            // For the true branch, set the pattern operation value to NotNull.
                            if (FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue)
                            {
                                predicateValueKind = SetValueForIsNullComparisonOperator(isPatternOperation.Value, equals: false, targetAnalysisData: targetAnalysisData);
                            }

                            break;

                        case OperationKind.ConstantPattern:
                            var constantPattern = (IConstantPatternOperation)isPatternOperation.Pattern;
                            predicateValueKind = SetValueForEqualsOrNotEqualsComparisonOperator(isPatternOperation.Value, constantPattern.Value,
                                equals: FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue, isReferenceEquality: false, targetAnalysisData: targetAnalysisData);
                            break;

                        case OperationKind.NegatedPattern:
                            var negatedPattern = (INegatedPatternOperation)isPatternOperation.Pattern;
                            if (negatedPattern.Pattern is IConstantPatternOperation negatedConstantPattern)
                            {
                                predicateValueKind = SetValueForEqualsOrNotEqualsComparisonOperator(isPatternOperation.Value, negatedConstantPattern.Value,
                                    equals: FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse, isReferenceEquality: false, targetAnalysisData: targetAnalysisData);
                            }

                            break;

                        case OperationKind.RelationalPattern:
                            // For the true branch, set the pattern operation value to NotNull.
                            if (FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue)
                            {
                                predicateValueKind = SetValueForIsNullComparisonOperator(isPatternOperation.Value, equals: false, targetAnalysisData: targetAnalysisData);
                            }

                            break;

                        case OperationKind.BinaryPattern:
                            // These high level patterns should not be present in the lowered CFG: https://github.com/dotnet/roslyn/issues/47068
                            predicateValueKind = PredicateValueKind.Unknown;

                            // We special case common null check to reduce false positives. But this implementation for BinaryPattern is very incomplete.
                            if (FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse)
                            {
                                var binaryPattern = (IBinaryPatternOperation)isPatternOperation.Pattern;
                                if (IsNotNullWhenFalse(binaryPattern))
                                {
                                    predicateValueKind = SetValueForIsNullComparisonOperator(isPatternOperation.Value, equals: false, targetAnalysisData: targetAnalysisData);
                                }
                            }
                            else if (FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue)
                            {
                                var binaryPattern = (IBinaryPatternOperation)isPatternOperation.Pattern;
                                if (IsNotNullWhenTrue(binaryPattern))
                                {
                                    predicateValueKind = SetValueForIsNullComparisonOperator(isPatternOperation.Value, equals: false, targetAnalysisData: targetAnalysisData);
                                }
                            }

                            break;

                        default:
                            Debug.Fail($"Unknown pattern kind '{isPatternOperation.Pattern.Kind}'");
                            predicateValueKind = PredicateValueKind.Unknown;
                            break;
                    }

                    break;

                case IBinaryOperation binaryOperation:
                    // Predicate analysis for different equality comparison operators.
                    if (!binaryOperation.IsComparisonOperator())
                    {
                        return;
                    }

                    predicateValueKind = SetValueForComparisonOperator(binaryOperation, targetAnalysisData);
                    break;

                case IUnaryOperation unaryOperation:
                    // Predicate analysis for unary not operator.
                    if (unaryOperation.OperatorKind == UnaryOperatorKind.Not)
                    {
                        FlowBranchConditionKind = FlowBranchConditionKind.Negate();
                        PerformPredicateAnalysisCore(unaryOperation.Operand, targetAnalysisData);
                        FlowBranchConditionKind = FlowBranchConditionKind.Negate();
                    }

                    break;

                case IArgumentOperation argument:
                    Debug.Assert(IsContractCheckArgument(argument));
                    PerformPredicateAnalysisCore(argument.Value, targetAnalysisData);
                    return;

                case IConversionOperation conversion:
                    PerformPredicateAnalysisCore(conversion.Operand, targetAnalysisData);
                    return;

                case IParenthesizedOperation parenthesizedOperation:
                    PerformPredicateAnalysisCore(parenthesizedOperation.Operand, targetAnalysisData);
                    return;

                case IFlowCaptureReferenceOperation:
                    var result = AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity? flowCaptureReferenceEntity);
                    Debug.Assert(result);
                    RoslynDebug.Assert(flowCaptureReferenceEntity != null);
                    RoslynDebug.Assert(flowCaptureReferenceEntity.CaptureId != null);
                    if (!HasPredicatedDataForEntity(targetAnalysisData, flowCaptureReferenceEntity))
                    {
                        return;
                    }

                    predicateValueKind = ApplyPredicatedDataForEntity(targetAnalysisData, flowCaptureReferenceEntity, trueData: FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue);
                    break;

                case IInvocationOperation invocation:
                    // Predicate analysis for different equality comparison methods and argument null check methods.
                    if (invocation.Type?.SpecialType != SpecialType.System_Boolean)
                    {
                        return;
                    }

                    if (invocation.TargetMethod.IsArgumentNullCheckMethod())
                    {
                        // Predicate analysis for null checks, e.g. 'IsNullOrEmpty', 'IsNullOrWhiteSpace', etc.
                        // The method guarantees non-null value on 'WhenFalse' path, but does not guarantee null value on 'WhenTrue' path.
                        // Additionally, predicateValueKind cannot be determined to be AlwaysTrue or AlwaysFalse on either of these paths.
                        if (invocation.Arguments.Length == 1 && FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse)
                        {
                            _ = SetValueForIsNullComparisonOperator(invocation.Arguments[0].Value, equals: false, targetAnalysisData: targetAnalysisData);
                        }

                        break;
                    }

                    IOperation? leftOperand = null;
                    IOperation? rightOperand = null;
                    bool isReferenceEquality = false;
                    if (invocation.Arguments.Length == 2 &&
                        invocation.TargetMethod.IsStaticObjectEqualsOrReferenceEquals())
                    {
                        // 1. "static bool object.ReferenceEquals(o1, o2)"
                        // 2. "static bool object.Equals(o1, o2)"
                        leftOperand = invocation.Arguments[0].Value;
                        rightOperand = invocation.Arguments[1].Value;
                        isReferenceEquality = invocation.TargetMethod.Name == "ReferenceEquals" ||
                            (AnalysisEntityFactory.TryCreate(invocation.Arguments[0].Value, out var analysisEntity) &&
                             !analysisEntity.Type.HasValueCopySemantics() &&
                             (analysisEntity.Type as INamedTypeSymbol)?.OverridesEquals() == false);
                    }
                    else
                    {
                        // 1. "bool virtual object.Equals(other)"
                        // 2. "bool override Equals(other)"
                        // 3. "bool IEquatable<T>.Equals(other)"
                        if (invocation.Arguments.Length == 1 &&
                            (invocation.TargetMethod.IsObjectEquals() ||
                             invocation.TargetMethod.IsObjectEqualsOverride() ||
                             IsOverrideOrImplementationOfEquatableEquals(invocation.TargetMethod)))
                        {
                            leftOperand = invocation.Instance;
                            rightOperand = invocation.Arguments[0].Value;
                            isReferenceEquality = invocation.TargetMethod.IsObjectEquals();
                        }
                    }

                    if (leftOperand != null && rightOperand != null)
                    {
                        predicateValueKind = SetValueForEqualsOrNotEqualsComparisonOperator(
                            leftOperand,
                            rightOperand,
                            equals: FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue,
                            isReferenceEquality: isReferenceEquality,
                            targetAnalysisData: targetAnalysisData);
                    }

                    break;

                default:
                    return;
            }

            SetPredicateValueKind(operation, targetAnalysisData, predicateValueKind);
            return;

            // local functions.
            bool IsOverrideOrImplementationOfEquatableEquals(IMethodSymbol methodSymbol)
            {
                if (GenericIEquatableNamedType == null)
                {
                    return false;
                }

                foreach (var interfaceType in methodSymbol.ContainingType.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, GenericIEquatableNamedType))
                    {
                        var equalsMember = interfaceType.GetMembers("Equals").OfType<IMethodSymbol>().FirstOrDefault();
                        if (equalsMember != null && methodSymbol.IsOverrideOrImplementationOfInterfaceMember(equalsMember))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            bool IsNotNullWhenFalse(IOperation operation)
            {
                if (operation is IConstantPatternOperation constant && constant.Value.ConstantValue.HasValue && constant.Value.ConstantValue.Value is null)
                {
                    // This is a null check. So we are not null on the false branch.
                    return true;
                }

                if (operation is IBinaryPatternOperation { OperatorKind: BinaryOperatorKind.Or } binaryOrOperation)
                {
                    // Example: if (c is null or "")
                    // The whole operation is not null when false, because one of the OR branches is not null when false.
                    return IsNotNullWhenFalse(binaryOrOperation.LeftPattern) || IsNotNullWhenFalse(binaryOrOperation.RightPattern);
                }

                return false;
            }

            bool IsNotNullWhenTrue(IOperation operation)
            {
                if (operation is INegatedPatternOperation negated && negated.Pattern is IConstantPatternOperation constant && constant.Value.ConstantValue.HasValue && constant.Value.ConstantValue.Value is null)
                {
                    // This is a not null check. So we are not null on the true branch.
                    return true;
                }

                if (operation is IBinaryPatternOperation { OperatorKind: BinaryOperatorKind.And } binaryOrOperation)
                {
                    // Example: if (c is not null and "")
                    // The whole operation is not null when true, because one of the AND branches is not null when true.
                    return IsNotNullWhenTrue(binaryOrOperation.LeftPattern) || IsNotNullWhenTrue(binaryOrOperation.RightPattern);
                }

                return false;
            }
        }

        protected virtual void SetPredicateValueKind(IOperation operation, TAnalysisData analysisData, PredicateValueKind predicateValueKind)
        {
            Debug.Assert(PredicateAnalysis);

            if (predicateValueKind != PredicateValueKind.Unknown ||
                _predicateValueKindCacheBuilder.ContainsKey(operation))
            {
                if (FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse)
                {
                    switch (predicateValueKind)
                    {
                        case PredicateValueKind.AlwaysFalse:
                            predicateValueKind = PredicateValueKind.AlwaysTrue;
                            break;

                        case PredicateValueKind.AlwaysTrue:
                            predicateValueKind = PredicateValueKind.AlwaysFalse;
                            break;
                    }
                }

                _predicateValueKindCacheBuilder[operation] = predicateValueKind;
            }
        }

        protected virtual PredicateValueKind SetValueForComparisonOperator(IBinaryOperation operation, TAnalysisData targetAnalysisData)
        {
            Debug.Assert(PredicateAnalysis);
            Debug.Assert(operation.IsComparisonOperator());
            Debug.Assert(FlowBranchConditionKind != ControlFlowConditionKind.None);

            var leftType = operation.LeftOperand.Type;
            var leftConstantValueOpt = operation.LeftOperand.ConstantValue;
            var rightType = operation.RightOperand.Type;
            var rightConstantValueOpt = operation.RightOperand.ConstantValue;
            var isReferenceEquality = operation.OperatorMethod == null &&
                operation.Type?.SpecialType == SpecialType.System_Boolean &&
                leftType != null &&
                !leftType.HasValueCopySemantics() &&
                rightType != null &&
                !rightType.HasValueCopySemantics() &&
                (!leftConstantValueOpt.HasValue || leftConstantValueOpt.Value != null) &&
                (!rightConstantValueOpt.HasValue || rightConstantValueOpt.Value != null);

            bool equals;
            switch (operation.OperatorKind)
            {
                case BinaryOperatorKind.Equals:
                case BinaryOperatorKind.ObjectValueEquals:
                    equals = true;
                    break;

                case BinaryOperatorKind.NotEquals:
                case BinaryOperatorKind.ObjectValueNotEquals:
                    equals = false;
                    break;

                default:
                    return PredicateValueKind.Unknown;
            }

            if (FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse)
            {
                equals = !equals;
            }

            return SetValueForEqualsOrNotEqualsComparisonOperator(operation.LeftOperand, operation.RightOperand, equals, isReferenceEquality, targetAnalysisData);
        }

        protected virtual PredicateValueKind SetValueForEqualsOrNotEqualsComparisonOperator(IOperation leftOperand, IOperation rightOperand, bool equals, bool isReferenceEquality, TAnalysisData targetAnalysisData)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        protected virtual PredicateValueKind SetValueForIsNullComparisonOperator(IOperation leftOperand, bool equals, TAnalysisData targetAnalysisData)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        protected virtual void StartTrackingPredicatedData(AnalysisEntity predicatedEntity, TAnalysisData? truePredicateData, TAnalysisData? falsePredicateData)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        protected virtual void StopTrackingPredicatedData(AnalysisEntity predicatedEntity)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        private bool HasPredicatedDataForEntity(AnalysisEntity predicatedEntity)
            => HasPredicatedDataForEntity(CurrentAnalysisData, predicatedEntity);

        protected virtual bool HasPredicatedDataForEntity(TAnalysisData analysisData, AnalysisEntity predicatedEntity)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        protected virtual void TransferPredicatedData(AnalysisEntity fromEntity, AnalysisEntity toEntity)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        protected virtual PredicateValueKind ApplyPredicatedDataForEntity(TAnalysisData analysisData, AnalysisEntity predicatedEntity, bool trueData)
        {
            Debug.Assert(PredicateAnalysis);
            throw new NotImplementedException();
        }

        protected virtual void ProcessThrowValue(IOperation? thrownValue)
        {
        }

        #endregion

        #region Helper methods to handle initialization/assignment operations
        protected abstract void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, TAbstractAnalysisValue value);
        protected abstract void SetAbstractValueForAssignment(IOperation target, IOperation? assignedValueOperation, TAbstractAnalysisValue assignedValue, bool mayBeAssignment = false);
        protected abstract void SetAbstractValueForTupleElementAssignment(AnalysisEntity tupleElementEntity, IOperation assignedValueOperation, TAbstractAnalysisValue assignedValue);
        private void HandleFlowCaptureReferenceAssignment(IFlowCaptureReferenceOperation flowCaptureReference, IOperation assignedValueOperation, TAbstractAnalysisValue assignedValue)
        {
            Debug.Assert(IsLValueFlowCaptureReference(flowCaptureReference));

            var pointsToValue = GetPointsToAbstractValue(flowCaptureReference);
            if (pointsToValue.Kind == PointsToAbstractValueKind.KnownLValueCaptures)
            {
                switch (pointsToValue.LValueCapturedOperations.Count)
                {
                    case 0:
                        throw new InvalidProgramException();

                    case 1:
                        var target = pointsToValue.LValueCapturedOperations.First();
                        SetAbstractValueForAssignment(target, assignedValueOperation, assignedValue);
                        break;

                    default:
                        if (HasUniqueCapturedEntity())
                        {
                            goto case 1;
                        }
                        else
                        {
                            foreach (var capturedOperation in pointsToValue.LValueCapturedOperations)
                            {
                                SetAbstractValueForAssignment(capturedOperation, assignedValueOperation, assignedValue, mayBeAssignment: true);
                            }
                        }

                        break;

                        bool HasUniqueCapturedEntity()
                        {
                            AnalysisEntity? uniqueAnalysisEntity = null;
                            foreach (var capturedOperation in pointsToValue.LValueCapturedOperations)
                            {
                                if (AnalysisEntityFactory.TryCreate(capturedOperation, out var entity))
                                {
                                    if (uniqueAnalysisEntity == null)
                                    {
                                        uniqueAnalysisEntity = entity;
                                    }
                                    else if (entity != uniqueAnalysisEntity)
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }

                            return uniqueAnalysisEntity != null;
                        }
                }
            }
        }
        #endregion

        #region Helper methods for reseting/transfer instance analysis data when PointsTo analysis results are available
        /// <summary>
        /// Resets all the analysis data for all <see cref="AnalysisEntity"/> instances that share the same <see cref="AnalysisEntity.InstanceLocation"/>
        /// as the given <paramref name="analysisEntity"/>.
        /// </summary>
        /// <param name="analysisEntity"></param>
        protected abstract void ResetValueTypeInstanceAnalysisData(AnalysisEntity analysisEntity);

        /// <summary>
        /// Resets all the analysis data for all <see cref="AnalysisEntity"/> instances that share the same <see cref="AnalysisEntity.InstanceLocation"/>
        /// as the given <paramref name="pointsToAbstractValue"/>.
        /// </summary>
        protected abstract void ResetReferenceTypeInstanceAnalysisData(PointsToAbstractValue pointsToAbstractValue);

        private void ResetValueTypeInstanceAnalysisData(IOperation operation)
        {
            Debug.Assert(HasPointsToAnalysisResult);
            Debug.Assert(operation.Type!.HasValueCopySemantics());

            if (AnalysisEntityFactory.TryCreate(operation, out var analysisEntity))
            {
                if (analysisEntity.Type.HasValueCopySemantics())
                {
                    ResetValueTypeInstanceAnalysisData(analysisEntity);
                }
            }
        }

        /// <summary>
        /// Resets all the analysis data for all <see cref="AnalysisEntity"/> instances that share the same <see cref="AnalysisEntity.InstanceLocation"/>
        /// as pointed to by given reference type <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation"></param>
        private void ResetReferenceTypeInstanceAnalysisData(IOperation operation)
        {
            Debug.Assert(HasPointsToAnalysisResult);
            Debug.Assert(!operation.Type!.HasValueCopySemantics());

            var pointsToValue = GetPointsToAbstractValue(operation);
            if (pointsToValue.Locations.IsEmpty)
            {
                return;
            }

            ResetReferenceTypeInstanceAnalysisData(pointsToValue);
        }

        /// <summary>
        /// Reset all the instance analysis data if <see cref="HasPointsToAnalysisResult"/> is true and <see cref="PessimisticAnalysis"/> is also true.
        /// If we are using or performing points to analysis, certain operations can invalidate all the analysis data off the containing instance.
        /// </summary>
        private void ResetInstanceAnalysisData(IOperation? operation)
        {
            if (operation == null || operation.Type == null || !HasPointsToAnalysisResult || !PessimisticAnalysis)
            {
                return;
            }

            if (operation.Type.HasValueCopySemantics())
            {
                ResetValueTypeInstanceAnalysisData(operation);
            }
            else
            {
                ResetReferenceTypeInstanceAnalysisData(operation);
            }
        }

        #endregion

        public TAnalysisData MergeAnalysisData(TAnalysisData value1, TAnalysisData value2, BasicBlock forBlock, bool forBackEdge)
            => forBackEdge ? MergeAnalysisDataForBackEdge(value1, value2, forBlock) : MergeAnalysisData(value1, value2, forBlock);
        protected abstract TAnalysisData MergeAnalysisData(TAnalysisData value1, TAnalysisData value2);
        protected virtual TAnalysisData MergeAnalysisData(TAnalysisData value1, TAnalysisData value2, BasicBlock forBlock)
            => MergeAnalysisData(value1, value2);
        protected virtual TAnalysisData MergeAnalysisDataForBackEdge(TAnalysisData value1, TAnalysisData value2, BasicBlock forBlock)
            => MergeAnalysisData(value1, value2, forBlock);
        protected abstract TAnalysisData GetClonedAnalysisData(TAnalysisData analysisData);
        protected TAnalysisData GetClonedCurrentAnalysisData() => GetClonedAnalysisData(CurrentAnalysisData);
        public abstract TAnalysisData GetEmptyAnalysisData();
        protected abstract TAnalysisData GetExitBlockOutputData(TAnalysisResult analysisResult);
        protected abstract bool Equals(TAnalysisData value1, TAnalysisData value2);
        protected static bool EqualsHelper<TKey, TValue>(IDictionary<TKey, TValue> dict1, IDictionary<TKey, TValue> dict2)
            => dict1.Count == dict2.Count &&
               dict1.Keys.All(key => dict2.TryGetValue(key, out TValue value2) && EqualityComparer<TValue>.Default.Equals(dict1[key], value2));
        protected abstract void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(TAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType);
        protected virtual void AssertValidAnalysisData(TAnalysisData analysisData)
        {
            // No validation by default, all the subtypes can override for validation.
        }

        protected void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData<TKey>(
            DictionaryAnalysisData<TKey, TAbstractAnalysisValue> coreDataAtException,
            DictionaryAnalysisData<TKey, TAbstractAnalysisValue> coreCurrentAnalysisData,
            Func<TKey, bool>? predicate)
            where TKey : notnull
        {
            foreach (var (key, value) in coreCurrentAnalysisData)
            {
                if (coreDataAtException.ContainsKey(key) ||
                    predicate != null && !predicate(key))
                {
                    continue;
                }

                coreDataAtException.Add(key, value);
            }
        }

        #region Interprocedural analysis

        /// <summary>
        /// Gets a new instance of analysis data that should be passed as initial analysis data
        /// for interprocedural analysis.
        /// The default implementation returns cloned CurrentAnalysisData.
        /// </summary>
        protected virtual TAnalysisData GetInitialInterproceduralAnalysisData(
            IMethodSymbol invokedMethod,
            (AnalysisEntity? Instance, PointsToAbstractValue PointsToValue)? invocationInstance,
            (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? thisOrMeInstanceForCaller,
            ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>> argumentValuesMap,
            IDictionary<AnalysisEntity, PointsToAbstractValue>? pointsToValues,
            IDictionary<AnalysisEntity, CopyAbstractValue>? copyValues,
            IDictionary<AnalysisEntity, ValueContentAbstractValue>? valueContentValues,
            bool isLambdaOrLocalFunction,
            bool hasParameterWithDelegateType)
            => GetClonedCurrentAnalysisData();

        /// <summary>
        /// Apply the result data from interprocedural analysis to CurrentAnalysisData.
        /// Default implementation is designed for the default implementation of GetInitialInterproceduralAnalysisData.
        /// and overwrites the CurrentAnalysisData with the given <paramref name="resultData"/>.
        /// </summary>
        protected virtual void ApplyInterproceduralAnalysisResult(TAnalysisData resultData, bool isLambdaOrLocalFunction, bool hasDelegateTypeArgument, TAnalysisResult analysisResult)
            => CurrentAnalysisData = resultData;

        private void ApplyInterproceduralAnalysisDataForUnhandledThrowOperations(Dictionary<ThrownExceptionInfo, TAnalysisData> interproceduralUnhandledThrowOperationsData)
        {
            if (interproceduralUnhandledThrowOperationsData.Count == 0)
            {
                // All interprocedural exceptions were handled.
                return;
            }

            AnalysisDataForUnhandledThrowOperations ??= [];
            foreach (var (exceptionInfo, analysisDataAtException) in interproceduralUnhandledThrowOperationsData)
            {
                // Adjust the thrown exception info from the interprocedural context to current context.
                var adjustedExceptionInfo = exceptionInfo.With(CurrentBasicBlock, DataFlowAnalysisContext.InterproceduralAnalysisData?.CallStack);

                // Used cloned analysis data
                var clonedAnalysisDataAtException = GetClonedAnalysisData(analysisDataAtException);

                ApplyInterproceduralAnalysisDataForUnhandledThrowOperation(adjustedExceptionInfo, clonedAnalysisDataAtException);
            }

            // Local functions
            void ApplyInterproceduralAnalysisDataForUnhandledThrowOperation(ThrownExceptionInfo exceptionInfo, TAnalysisData analysisDataAtException)
            {
                RoslynDebug.Assert(AnalysisDataForUnhandledThrowOperations != null);
                AssertValidAnalysisData(analysisDataAtException);
                ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(analysisDataAtException, exceptionInfo);
                AssertValidAnalysisData(analysisDataAtException);

                if (!AnalysisDataForUnhandledThrowOperations.TryGetValue(exceptionInfo, out var existingAnalysisDataAtException))
                {
                    AnalysisDataForUnhandledThrowOperations.Add(exceptionInfo, analysisDataAtException);
                }
                else
                {
                    var mergedAnalysisDataAtException = MergeAnalysisData(existingAnalysisDataAtException, analysisDataAtException);
                    AssertValidAnalysisData(mergedAnalysisDataAtException);
                    AnalysisDataForUnhandledThrowOperations[exceptionInfo] = mergedAnalysisDataAtException;
                    existingAnalysisDataAtException.Dispose();
                    analysisDataAtException.Dispose();
                }
            }
        }

        protected bool TryGetInterproceduralAnalysisResult(IOperation operation, [NotNullWhen(returnValue: true)] out TAnalysisResult? analysisResult)
        {
            if (_interproceduralResultsBuilder.TryGetValue(operation, out var computedAnalysisResult))
            {
                analysisResult = (TAnalysisResult)computedAnalysisResult;
                return true;
            }

            analysisResult = null;
            return false;
        }

        private TAbstractAnalysisValue PerformInterproceduralAnalysis(
            Func<ControlFlowGraph?> getCfg,
            IMethodSymbol invokedMethod,
            IOperation? instanceReceiver,
            ImmutableArray<IArgumentOperation> arguments,
            IOperation originalOperation,
            TAbstractAnalysisValue defaultValue,
            bool isLambdaOrLocalFunction,
            out bool wasAnalyzed)
        {
            wasAnalyzed = false;

            // Use the original method definition for interprocedural analysis as the ControlFlowGraph can only be created for the original definition.
            invokedMethod = invokedMethod.OriginalDefinition;

            // Bail out if configured not to execute interprocedural analysis.
            var skipInterproceduralAnalysis = !isLambdaOrLocalFunction && InterproceduralAnalysisKind == InterproceduralAnalysisKind.None ||
                DataFlowAnalysisContext.InterproceduralAnalysisPredicate?.SkipInterproceduralAnalysis(invokedMethod, isLambdaOrLocalFunction) == true ||
                DataFlowAnalysisContext.AnalyzerOptions.IsConfiguredToSkipAnalysis(s_dummyDataflowAnalysisDescriptor, invokedMethod, OwningSymbol, WellKnownTypeProvider.Compilation);

            // Also bail out for non-source methods and methods where we are not sure about the actual runtime target method.
            if (skipInterproceduralAnalysis ||
                invokedMethod.Locations.All(l => !l.IsInSource) ||
                invokedMethod.IsAbstract ||
                invokedMethod.IsVirtual ||
                invokedMethod.IsOverride ||
                invokedMethod.IsImplicitlyDeclared)
            {
                return ResetAnalysisDataAndReturnDefaultValue();
            }

            // Bail out if we are already analyzing the current context.
            var currentMethodsBeingAnalyzed = DataFlowAnalysisContext.InterproceduralAnalysisData?.MethodsBeingAnalyzed ?? ImmutableHashSet<TAnalysisContext>.Empty;
            var newMethodsBeingAnalyzed = currentMethodsBeingAnalyzed.Add(DataFlowAnalysisContext);
            if (currentMethodsBeingAnalyzed.Count == newMethodsBeingAnalyzed.Count)
            {
                return ResetAnalysisDataAndReturnDefaultValue();
            }

            // Check if we are already at the maximum allowed interprocedural call chain length.
            int currentMethodCallCount = currentMethodsBeingAnalyzed.Count(m => !(m.OwningSymbol is IMethodSymbol ms && ms.IsLambdaOrLocalFunctionOrDelegate()));
            int currentLambdaOrLocalFunctionCallCount = currentMethodsBeingAnalyzed.Count - currentMethodCallCount;

            if (currentMethodCallCount >= MaxInterproceduralMethodCallChain ||
                currentLambdaOrLocalFunctionCallCount >= MaxInterproceduralLambdaOrLocalFunctionCallChain)
            {
                return ResetAnalysisDataAndReturnDefaultValue();
            }

            // Compute the dependent interprocedural PointsTo and Copy analysis results, if any.
            var pointsToAnalysisResult = (PointsToAnalysisResult?)DataFlowAnalysisContext.PointsToAnalysisResult?.TryGetInterproceduralResult(originalOperation);
            var copyAnalysisResult = DataFlowAnalysisContext.CopyAnalysisResult?.TryGetInterproceduralResult(originalOperation);
            var valueContentAnalysisResult = DataFlowAnalysisContext.ValueContentAnalysisResult?.TryGetInterproceduralResult(originalOperation);

            // Compute the CFG for the invoked method.
            var cfg = pointsToAnalysisResult?.ControlFlowGraph ??
                copyAnalysisResult?.ControlFlowGraph ??
                valueContentAnalysisResult?.ControlFlowGraph ??
                getCfg();
            if (cfg == null || !cfg.SupportsFlowAnalysis())
            {
                return ResetAnalysisDataAndReturnDefaultValue();
            }

            var hasParameterWithDelegateType = invokedMethod.HasParameterWithDelegateType();

            // Ensure we are using the same control flow graphs across analyses.
            Debug.Assert(pointsToAnalysisResult?.ControlFlowGraph == null || cfg == pointsToAnalysisResult?.ControlFlowGraph);
            Debug.Assert(copyAnalysisResult?.ControlFlowGraph == null || cfg == copyAnalysisResult?.ControlFlowGraph);
            Debug.Assert(valueContentAnalysisResult?.ControlFlowGraph == null || cfg == valueContentAnalysisResult?.ControlFlowGraph);

            // Append operation to interprocedural call stack.
            _interproceduralCallStack.Push(originalOperation);

            // Compute optional interprocedural analysis data for context-sensitive analysis.
            bool isContextSensitive = isLambdaOrLocalFunction || InterproceduralAnalysisKind == InterproceduralAnalysisKind.ContextSensitive;
            var interproceduralAnalysisData = isContextSensitive ? ComputeInterproceduralAnalysisData() : null;
            TAnalysisResult? analysisResult;

            try
            {
                // Create analysis context for interprocedural analysis.
                var interproceduralDataFlowAnalysisContext = DataFlowAnalysisContext.ForkForInterproceduralAnalysis(
                    invokedMethod, cfg, pointsToAnalysisResult, copyAnalysisResult, valueContentAnalysisResult, interproceduralAnalysisData);

                // Check if the client configured skipping analysis for the given interprocedural analysis context.
                if (DataFlowAnalysisContext.InterproceduralAnalysisPredicate?.SkipInterproceduralAnalysis(interproceduralDataFlowAnalysisContext) == true)
                {
                    return ResetAnalysisDataAndReturnDefaultValue();
                }
                else
                {
                    // Execute interprocedural analysis and get result.
                    analysisResult = TryGetOrComputeAnalysisResult(interproceduralDataFlowAnalysisContext);
                    if (analysisResult == null)
                    {
                        return defaultValue;
                    }

                    wasAnalyzed = true;

                    // Save the interprocedural result for the invocation/creation operation.
                    // Note that we Update instead of invoking .Add as we may execute the analysis multiple times for fixed point computation.
                    _interproceduralResultsBuilder[originalOperation] = analysisResult;

                    _escapedLocalFunctions.AddRange(analysisResult.LambdaAndLocalFunctionAnalysisInfo.EscapedLocalFunctions);
                    _analyzedLocalFunctions.AddRange(analysisResult.LambdaAndLocalFunctionAnalysisInfo.AnalyzedLocalFunctions);
                    _escapedLambdas.AddRange(analysisResult.LambdaAndLocalFunctionAnalysisInfo.EscapedLambdas);
                    _analyzedLambdas.AddRange(analysisResult.LambdaAndLocalFunctionAnalysisInfo.AnalyzedLambdas);
                }

                // Update the current analysis data based on interprocedural analysis result.
                if (isContextSensitive)
                {
                    // Apply any interprocedural analysis data for unhandled exceptions paths.
                    if (analysisResult.AnalysisDataForUnhandledThrowOperations is Dictionary<ThrownExceptionInfo, TAnalysisData> interproceduralUnhandledThrowOperationsDataOpt)
                    {
                        ApplyInterproceduralAnalysisDataForUnhandledThrowOperations(interproceduralUnhandledThrowOperationsDataOpt);
                    }

                    if (analysisResult.TaskWrappedValuesMap is Dictionary<PointsToAbstractValue, TAbstractAnalysisValue> taskWrappedValuesMap)
                    {
                        foreach (var (key, value) in taskWrappedValuesMap)
                        {
                            SetTaskWrappedValue(key, value);
                        }
                    }

                    // Apply interprocedural result analysis data for non-exception paths.
                    var resultData = GetExitBlockOutputData(analysisResult);
                    ApplyInterproceduralAnalysisResult(resultData, isLambdaOrLocalFunction, hasParameterWithDelegateType, analysisResult);

                    Debug.Assert(arguments.All(arg => !_pendingArgumentsToReset.Contains(arg)));
                }
                else
                {
                    // TODO: https://github.com/dotnet/roslyn-analyzers/issues/1810
                    // Implement Non-context sensitive interprocedural analysis to
                    // merge the relevant data from invoked method's analysis result into CurrentAnalysisData.
                    // For now, retain the original logic of resetting the analysis data.
                    ResetAnalysisData(hasEscapedLambdaOrLocalFunctions: false);
                }
            }
            finally
            {
                // Remove the operation from interprocedural call stack.
                var popped = _interproceduralCallStack.Pop();
                Debug.Assert(popped == originalOperation);

                interproceduralAnalysisData?.InitialAnalysisData?.Dispose();
            }

            RoslynDebug.Assert(invokedMethod.ReturnsVoid == !analysisResult.ReturnValueAndPredicateKind.HasValue);
            if (invokedMethod.ReturnsVoid)
            {
                return defaultValue;
            }

            RoslynDebug.Assert(analysisResult.ReturnValueAndPredicateKind != null);

            if (PredicateAnalysis)
            {
                SetPredicateValueKind(originalOperation, CurrentAnalysisData, analysisResult.ReturnValueAndPredicateKind.Value.PredicateValueKind);
            }

            return analysisResult.ReturnValueAndPredicateKind.Value.Value;

            // Local functions
            TAbstractAnalysisValue ResetAnalysisDataAndReturnDefaultValue()
            {
                var hasEscapes = MarkEscapedLambdasAndLocalFunctionsFromArguments();
                ResetAnalysisData(hasEscapes);
                return defaultValue;
            }

            void ResetAnalysisData(bool hasEscapedLambdaOrLocalFunctions)
            {
                // Interprocedural analysis did not succeed, so we need to conservatively reset relevant analysis data.
                if (!PessimisticAnalysis)
                {
                    // We are performing an optimistic analysis, so we should not reset any data.
                    return;
                }

                if (isLambdaOrLocalFunction || hasEscapedLambdaOrLocalFunctions)
                {
                    // For local/lambda cases, we reset all analysis data.
                    ResetCurrentAnalysisData();
                }
                else
                {
                    // For regular invocation cases, we reset instance analysis data and argument data.
                    // Note that arguments are reset later by processing '_pendingArgumentsToReset'.
                    ResetInstanceAnalysisData(instanceReceiver);
                    Debug.Assert(arguments.All(_pendingArgumentsToReset.Contains));
                }
            }

            bool MarkEscapedLambdasAndLocalFunctionsFromArguments()
            {
                var hasEscapes = false;
                foreach (var argument in arguments)
                {
                    if (argument.Parameter?.Type.TypeKind == TypeKind.Delegate ||
                        argument.Parameter?.Type.SpecialType == SpecialType.System_Object)
                    {
                        if (!IsPointsToAnalysis)
                        {
                            // For non-points to analysis, pessimistically assume delegate arguments
                            // lead to escaped lambda or local function target which may get invoked.
                            if (argument.Parameter.Type.TypeKind == TypeKind.Delegate)
                            {
                                hasEscapes = true;
                                break;
                            }
                        }
                        else
                        {
                            // For points to analysis, we try to compute the target lambda or local function
                            // to determine if we have an escape.
                            var pointsToValue = GetPointsToAbstractValue(argument);
                            if (MarkEscapedLambdasAndLocalFunctions(pointsToValue))
                                hasEscapes = true;
                        }
                    }
                }

                return hasEscapes;
            }

            InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue> ComputeInterproceduralAnalysisData()
            {
                RoslynDebug.Assert(cfg != null);

                var invocationInstance = GetInvocationInstance();
                var thisOrMeInstance = GetThisOrMeInstance();
                var argumentValuesMap = GetArgumentValues(ref invocationInstance);
                var pointsToValues = pointsToAnalysisResult?[cfg.GetEntry()].Data;
                var copyValues = copyAnalysisResult?[cfg.GetEntry()].Data;
                var valueContentValues = valueContentAnalysisResult?[cfg.GetEntry()].Data;
                var initialAnalysisData = GetInitialInterproceduralAnalysisData(invokedMethod, invocationInstance,
                    thisOrMeInstance, argumentValuesMap, pointsToValues, copyValues, valueContentValues, isLambdaOrLocalFunction, hasParameterWithDelegateType);

                return new InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>(
                    initialAnalysisData,
                    invocationInstance,
                    thisOrMeInstance,
                    argumentValuesMap,
                    GetCapturedVariablesMap(cfg, invokedMethod, isLambdaOrLocalFunction),
                    _addressSharedEntitiesProvider.GetAddressedSharedEntityMap(),
                    ImmutableStack.CreateRange(_interproceduralCallStack),
                    newMethodsBeingAnalyzed,
                    getCachedAbstractValueFromCaller: GetCachedAbstractValue,
                    getInterproceduralControlFlowGraph: GetInterproceduralControlFlowGraph,
                    getAnalysisEntityForFlowCapture: GetAnalysisEntityForFlowCapture,
                    getInterproceduralCallStackForOwningSymbol: GetInterproceduralCallStackForOwningSymbol);

                // Local functions.
                (AnalysisEntity?, PointsToAbstractValue)? GetInvocationInstance()
                {
                    if (isLambdaOrLocalFunction)
                    {
                        return (AnalysisEntityFactory.ThisOrMeInstance, ThisOrMePointsToAbstractValue);
                    }
                    else if (instanceReceiver != null)
                    {
                        if (!AnalysisEntityFactory.TryCreate(instanceReceiver, out var receiverAnalysisEntityOpt))
                        {
                            receiverAnalysisEntityOpt = null;
                        }

                        var instancePointsToValue = GetPointsToAbstractValue(instanceReceiver);
                        if (instancePointsToValue.Kind == PointsToAbstractValueKind.Undefined)
                        {
                            // Error case: Invocation through an uninitialized local.
                            // Use Unknown PointsTo value for interprocedural analysis.
                            instancePointsToValue = PointsToAbstractValue.Unknown;
                        }

                        return (receiverAnalysisEntityOpt, instancePointsToValue);
                    }
                    else if (invokedMethod.MethodKind == MethodKind.Constructor)
                    {
                        // We currently only support interprocedural constructor analysis for object creation operations.
                        Debug.Assert(originalOperation.Kind == OperationKind.ObjectCreation);

                        // Pass in the allocation location as interprocedural instance.
                        // There is no analysis entity for the allocation.
                        var instancePointsToValue = GetPointsToAbstractValue(originalOperation);
                        return (null, instancePointsToValue);
                    }

                    return null;
                }

                (AnalysisEntity, PointsToAbstractValue)? GetThisOrMeInstance()
                    => (AnalysisEntityFactory.ThisOrMeInstance, ThisOrMePointsToAbstractValue);

                ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>> GetArgumentValues(ref (AnalysisEntity? entity, PointsToAbstractValue pointsToValue)? invocationInstance)
                {
                    var builder = PooledDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>>.GetInstance();
                    var isExtensionMethodInvocationWithOneLessArgument = invokedMethod.IsExtensionMethod && arguments.Length == invokedMethod.Parameters.Length - 1;

                    if (isExtensionMethodInvocationWithOneLessArgument)
                    {
                        var extraArgument = new ArgumentInfo<TAbstractAnalysisValue>(
                            operation: instanceReceiver ?? originalOperation,
                            analysisEntity: invocationInstance?.entity,
                            instanceLocation: invocationInstance?.pointsToValue ?? PointsToAbstractValue.Unknown,
                            value: instanceReceiver != null ? GetCachedAbstractValue(instanceReceiver) : ValueDomain.UnknownOrMayBeValue);
                        builder.Add(invokedMethod.Parameters[0], extraArgument);
                        invocationInstance = null;
                    }
                    else
                    {
                        Debug.Assert(arguments.Length == invokedMethod.Parameters.Length);
                    }

                    foreach (var argument in arguments)
                    {
                        PointsToAbstractValue instanceLocation;
                        if (AnalysisEntityFactory.TryCreate(argument, out var argumentEntity))
                        {
                            instanceLocation = argumentEntity.InstanceLocation;
                        }
                        else
                        {
                            // For allocations, such as "new A()", which have no associated entity, but a valid PointsTo value.
                            instanceLocation = GetPointsToAbstractValue(argument);
                            argumentEntity = null;
                        }

                        var argumentValue = GetCachedAbstractValue(argument);
                        if (ReferenceEquals(argumentValue, ValueDomain.Bottom))
                        {
                            argumentValue = ValueDomain.UnknownOrMayBeValue;
                        }

                        if (argument.Parameter != null)
                        {
                            builder.Add(GetMappedParameterForArgument(argument), new ArgumentInfo<TAbstractAnalysisValue>(argument, argumentEntity, instanceLocation, argumentValue));
                        }

                        _pendingArgumentsToReset.Remove(argument);
                    }

                    return builder.ToImmutableDictionaryAndFree();

                    // Local function
                    IParameterSymbol GetMappedParameterForArgument(IArgumentOperation argumentOperation)
                    {
                        if (argumentOperation.Parameter!.ContainingSymbol is IMethodSymbol method &&
                            method.MethodKind == MethodKind.DelegateInvoke)
                        {
                            // Parameter associated with IArgumentOperation for delegate invocations
                            // is the DelegateInvoke method parameter.
                            // So we need to map it to the parameter of the invoked method by using ordinals.
                            Debug.Assert(invokedMethod.Parameters.Length == method.GetParameters().Length ||
                                isExtensionMethodInvocationWithOneLessArgument);

                            var ordinal = argumentOperation.Parameter.Ordinal;
                            if (isExtensionMethodInvocationWithOneLessArgument)
                            {
                                ordinal++;
                            }

                            return invokedMethod.Parameters[ordinal].OriginalDefinition;
                        }

                        return argumentOperation.Parameter.OriginalDefinition;
                    }
                }

                AnalysisEntity? GetAnalysisEntityForFlowCapture(IOperation operation)
                {
                    switch (operation.Kind)
                    {
                        case OperationKind.FlowCapture:
                        case OperationKind.FlowCaptureReference:
                            if (AnalysisEntityFactory.TryGetForInterproceduralAnalysis(operation, out var analysisEntity) &&
                                originalOperation.Descendants().Contains(operation))
                            {
                                return analysisEntity;
                            }

                            break;
                    }

                    return null;
                }
            }
        }

        private ImmutableDictionary<ISymbol, PointsToAbstractValue> GetCapturedVariablesMap(
            ControlFlowGraph cfg,
            IMethodSymbol invokedMethod,
            bool isLambdaOrLocalFunction)
        {
            RoslynDebug.Assert(cfg != null);

            if (!isLambdaOrLocalFunction)
            {
                return ImmutableDictionary<ISymbol, PointsToAbstractValue>.Empty;
            }

            using var _ = cfg.OriginalOperation.GetCaptures(invokedMethod, out var capturedVariables);
            if (capturedVariables.Count == 0)
            {
                return ImmutableDictionary<ISymbol, PointsToAbstractValue>.Empty;
            }
            else
            {
                var builder = ImmutableDictionary.CreateBuilder<ISymbol, PointsToAbstractValue>();
                foreach (var capturedVariable in capturedVariables)
                {
                    if (capturedVariable.Kind == SymbolKind.NamedType)
                    {
                        // ThisOrMeInstance capture can be skipped here
                        // as we already pass down the invocation instance through "GetInvocationInstance".
                        continue;
                    }

                    var success = AnalysisEntityFactory.TryCreateForSymbolDeclaration(capturedVariable, out var capturedEntity);
                    Debug.Assert(success);
                    RoslynDebug.Assert(capturedEntity != null);
                    builder.Add(capturedVariable, capturedEntity.InstanceLocation);
                }

                return builder.ToImmutable();
            }
        }

        private void PerformStandaloneLocalFunctionInterproceduralAnalysis(IMethodSymbol localFunction)
        {
            Debug.Assert(IsStandaloneAnalysisRequiredForLocalFunction(localFunction));

            // Compute the dependent interprocedural PointsTo and Copy analysis results, if any.
            var pointsToAnalysisResult = (PointsToAnalysisResult?)DataFlowAnalysisContext.PointsToAnalysisResult?.TryGetStandaloneLocalFunctionAnalysisResult(localFunction);
            var copyAnalysisResult = DataFlowAnalysisContext.CopyAnalysisResult?.TryGetStandaloneLocalFunctionAnalysisResult(localFunction);
            var valueContentAnalysisResult = DataFlowAnalysisContext.ValueContentAnalysisResult?.TryGetStandaloneLocalFunctionAnalysisResult(localFunction);

            // Compute the CFG for the invoked method.
            var cfg = pointsToAnalysisResult?.ControlFlowGraph ??
                copyAnalysisResult?.ControlFlowGraph ??
                valueContentAnalysisResult?.ControlFlowGraph ??
                DataFlowAnalysisContext.GetLocalFunctionControlFlowGraph(localFunction);
            if (cfg == null || !cfg.SupportsFlowAnalysis())
            {
                return;
            }

            // Ensure we are using the same control flow graphs across analyses.
            Debug.Assert(pointsToAnalysisResult?.ControlFlowGraph == null || cfg == pointsToAnalysisResult?.ControlFlowGraph);
            Debug.Assert(copyAnalysisResult?.ControlFlowGraph == null || cfg == copyAnalysisResult?.ControlFlowGraph);
            Debug.Assert(valueContentAnalysisResult?.ControlFlowGraph == null || cfg == valueContentAnalysisResult?.ControlFlowGraph);

            // Push a dummy operation for standalone local function analysis.
            _interproceduralCallStack.Push(DataFlowAnalysisContext.ControlFlowGraph.OriginalOperation);

            try
            {
                var interproceduralAnalysisData = GetInterproceduralAnalysisDataForStandaloneLambdaOrLocalFunctionAnalysis(cfg, localFunction);

                // Create analysis context for interprocedural analysis.
                var interproceduralDataFlowAnalysisContext = DataFlowAnalysisContext.ForkForInterproceduralAnalysis(
                    localFunction, cfg, pointsToAnalysisResult, copyAnalysisResult, valueContentAnalysisResult, interproceduralAnalysisData);

                // Execute interprocedural analysis and get result.
                var analysisResult = TryGetOrComputeAnalysisResult(interproceduralDataFlowAnalysisContext);
                if (analysisResult != null)
                {
                    // Save the interprocedural result for the local function.
                    // Note that we Update instead of invoking .Add as we may execute the analysis multiple times for fixed point computation.
                    _standaloneLocalFunctionAnalysisResultsBuilder[localFunction] = analysisResult;
                }
            }
            finally
            {
                _interproceduralCallStack.Pop();
            }
        }

        private void PerformStandaloneLambdaInterproceduralAnalysis(IFlowAnonymousFunctionOperation lambda)
        {
            Debug.Assert(IsStandaloneAnalysisRequiredForLambda(lambda));

            // Compute the dependent interprocedural PointsTo and Copy analysis results, if any.
            var pointsToAnalysisResult = (PointsToAnalysisResult?)DataFlowAnalysisContext.PointsToAnalysisResult?.TryGetInterproceduralResult(lambda);
            var copyAnalysisResult = DataFlowAnalysisContext.CopyAnalysisResult?.TryGetInterproceduralResult(lambda);
            var valueContentAnalysisResult = DataFlowAnalysisContext.ValueContentAnalysisResult?.TryGetInterproceduralResult(lambda);

            // Compute the CFG for the invoked method.
            var cfg = pointsToAnalysisResult?.ControlFlowGraph ??
                copyAnalysisResult?.ControlFlowGraph ??
                valueContentAnalysisResult?.ControlFlowGraph ??
                DataFlowAnalysisContext.GetAnonymousFunctionControlFlowGraph(lambda);
            if (cfg == null || !cfg.SupportsFlowAnalysis())
            {
                return;
            }

            // Ensure we are using the same control flow graphs across analyses.
            Debug.Assert(pointsToAnalysisResult?.ControlFlowGraph == null || cfg == pointsToAnalysisResult?.ControlFlowGraph);
            Debug.Assert(copyAnalysisResult?.ControlFlowGraph == null || cfg == copyAnalysisResult?.ControlFlowGraph);
            Debug.Assert(valueContentAnalysisResult?.ControlFlowGraph == null || cfg == valueContentAnalysisResult?.ControlFlowGraph);

            _interproceduralCallStack.Push(lambda);

            try
            {
                var interproceduralAnalysisData = GetInterproceduralAnalysisDataForStandaloneLambdaOrLocalFunctionAnalysis(cfg, lambda.Symbol);

                // Create analysis context for interprocedural analysis.
                var interproceduralDataFlowAnalysisContext = DataFlowAnalysisContext.ForkForInterproceduralAnalysis(
                    lambda.Symbol, cfg, pointsToAnalysisResult, copyAnalysisResult, valueContentAnalysisResult, interproceduralAnalysisData);

                // Execute interprocedural analysis and get result.
                var analysisResult = TryGetOrComputeAnalysisResult(interproceduralDataFlowAnalysisContext);
                if (analysisResult != null)
                {
                    // Save the interprocedural result for the lambda operation.
                    // Note that we Update instead of invoking .Add as we may execute the analysis multiple times for fixed point computation.
                    _interproceduralResultsBuilder[lambda] = analysisResult;
                }
            }
            finally
            {
                _interproceduralCallStack.Pop();
            }
        }

        private InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue> GetInterproceduralAnalysisDataForStandaloneLambdaOrLocalFunctionAnalysis(
            ControlFlowGraph cfg,
            IMethodSymbol invokedMethod)
        {
            var invocationInstance = (AnalysisEntityFactory.ThisOrMeInstance, ThisOrMePointsToAbstractValue);
            var thisOrMeInstance = invocationInstance;
            var currentMethodsBeingAnalyzed = DataFlowAnalysisContext.InterproceduralAnalysisData?.MethodsBeingAnalyzed ?? ImmutableHashSet<TAnalysisContext>.Empty;
            var newMethodsBeingAnalyzed = currentMethodsBeingAnalyzed.Add(DataFlowAnalysisContext);

            return new InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>(
                initialAnalysisData: null,
                invocationInstance,
                thisOrMeInstance,
                argumentValuesMap: ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>>.Empty,
                GetCapturedVariablesMap(cfg, invokedMethod, isLambdaOrLocalFunction: true),
                addressSharedEntities: ImmutableDictionary<AnalysisEntity, CopyAbstractValue>.Empty,
                ImmutableStack.CreateRange(_interproceduralCallStack),
                newMethodsBeingAnalyzed,
                getCachedAbstractValueFromCaller: _ => ValueDomain.UnknownOrMayBeValue,
                getInterproceduralControlFlowGraph: GetInterproceduralControlFlowGraph,
                getAnalysisEntityForFlowCapture: _ => null,
                getInterproceduralCallStackForOwningSymbol: GetInterproceduralCallStackForOwningSymbol);
        }

        #endregion

        #region Visitor methods

        protected TAbstractAnalysisValue VisitArray(IEnumerable<IOperation> operations, object? argument)
        {
            foreach (var operation in operations)
            {
                _ = Visit(operation, argument);
            }

            return ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue Visit(IOperation? operation, object? argument)
        {
            if (operation != null)
            {
                var value = VisitCore(operation, argument);
                CacheAbstractValue(operation, value);

                if (ExecutingExceptionPathsAnalysisPostPass)
                {
                    HandlePossibleThrowingOperation(operation);
                }

                var pendingArguments = _pendingArgumentsToPostProcess.ExtractAll(static (arg, operation) => arg.Parent == operation, operation);
                foreach (IArgumentOperation argumentOperation in pendingArguments)
                {
                    bool isEscaped;
                    if (_pendingArgumentsToReset.Remove(argumentOperation))
                    {
                        PostProcessEscapedArgument(argumentOperation);
                        isEscaped = true;
                    }
                    else
                    {
                        isEscaped = false;
                    }

                    PostProcessArgument(argumentOperation, isEscaped);
                }

                return value;
            }

            return ValueDomain.UnknownOrMayBeValue;
        }

        private TAbstractAnalysisValue VisitCore(IOperation operation, object? argument)
        {
            if (operation.Kind == OperationKind.None)
            {
                return DefaultVisit(operation, argument);
            }

            _recursionDepth++;
            try
            {
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                return operation.Accept(this, argument!)!;
            }
            finally
            {
                _recursionDepth--;
            }
        }

        public override TAbstractAnalysisValue DefaultVisit(IOperation operation, object? argument)
        {
            return VisitArray(operation.ChildOperations, argument);
        }

        public override TAbstractAnalysisValue VisitSimpleAssignment(ISimpleAssignmentOperation operation, object? argument)
        {
            return VisitAssignmentOperation(operation, argument);
        }

        public override TAbstractAnalysisValue VisitCompoundAssignment(ICompoundAssignmentOperation operation, object? argument)
        {
            TAbstractAnalysisValue targetValue = Visit(operation.Target, argument);
            TAbstractAnalysisValue assignedValue = Visit(operation.Value, argument);
            var value = ComputeValueForCompoundAssignment(operation, targetValue, assignedValue, operation.Target.Type, operation.Value.Type);
            if (operation.Target is IFlowCaptureReferenceOperation flowCaptureReference)
            {
                HandleFlowCaptureReferenceAssignment(flowCaptureReference, operation.Value, value);
            }
            else
            {
                SetAbstractValueForAssignment(operation.Target, operation.Value, value);
            }

            return value;
        }

        public virtual TAbstractAnalysisValue ComputeValueForCompoundAssignment(
            ICompoundAssignmentOperation operation,
            TAbstractAnalysisValue targetValue,
            TAbstractAnalysisValue assignedValue,
            ITypeSymbol? targetType,
            ITypeSymbol? assignedValueType)
        {
            return ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, object? argument)
        {
            TAbstractAnalysisValue targetValue = Visit(operation.Target, argument);
            var value = ComputeValueForIncrementOrDecrementOperation(operation, targetValue);
            SetAbstractValueForAssignment(operation.Target, assignedValueOperation: null, assignedValue: value);
            return value;
        }

        public virtual TAbstractAnalysisValue ComputeValueForIncrementOrDecrementOperation(IIncrementOrDecrementOperation operation, TAbstractAnalysisValue targetValue)
        {
            return ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, object? argument)
        {
            return VisitAssignmentOperation(operation, argument);
        }

        protected virtual TAbstractAnalysisValue VisitAssignmentOperation(IAssignmentOperation operation, object? argument)
        {
            _ = Visit(operation.Target, argument);
            TAbstractAnalysisValue assignedValue = Visit(operation.Value, argument);

            if (operation.Target is IFlowCaptureReferenceOperation flowCaptureReference)
            {
                HandleFlowCaptureReferenceAssignment(flowCaptureReference, operation.Value, assignedValue);
            }
            else
            {
                SetAbstractValueForAssignment(operation.Target, operation.Value, assignedValue);
            }

            return assignedValue;
        }

        public override TAbstractAnalysisValue VisitArrayInitializer(IArrayInitializerOperation operation, object? argument)
        {
            var arrayCreation = operation.GetAncestor<IArrayCreationOperation>(OperationKind.ArrayCreation);
            if (arrayCreation != null)
            {
                var elementType = ((IArrayTypeSymbol)arrayCreation.Type!).ElementType;
                for (int index = 0; index < operation.ElementValues.Length; index++)
                {
                    var abstractIndex = AbstractIndex.Create(index);
                    IOperation elementInitializer = operation.ElementValues[index];
                    TAbstractAnalysisValue initializerValue = Visit(elementInitializer, argument);
                    SetAbstractValueForArrayElementInitializer(arrayCreation, ImmutableArray.Create(abstractIndex), elementType, elementInitializer, initializerValue);
                }
            }
            else
            {
                _ = base.VisitArrayInitializer(operation, argument);
            }

            return ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue VisitLocalReference(ILocalReferenceOperation operation, object? argument)
        {
            var value = base.VisitLocalReference(operation, argument)!;
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitParameterReference(IParameterReferenceOperation operation, object? argument)
        {
            var value = base.VisitParameterReference(operation, argument)!;
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitArrayElementReference(IArrayElementReferenceOperation operation, object? argument)
        {
            var value = base.VisitArrayElementReference(operation, argument)!;
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, object? argument)
        {
            var value = base.VisitDynamicMemberReference(operation, argument)!;
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitEventReference(IEventReferenceOperation operation, object? argument)
        {
            var value = base.VisitEventReference(operation, argument)!;
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitFieldReference(IFieldReferenceOperation operation, object? argument)
        {
            var value = base.VisitFieldReference(operation, argument)!;
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitMethodReference(IMethodReferenceOperation operation, object? argument)
        {
            var value = base.VisitMethodReference(operation, argument)!;
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitPropertyReference(IPropertyReferenceOperation operation, object? argument)
        {
            var value = base.VisitPropertyReference(operation, argument)!;
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, object? argument)
        {
            var value = base.VisitFlowCaptureReference(operation, argument)!;
            if (!IsLValueFlowCaptureReference(operation))
            {
                if (_lValueFlowCaptures.Contains(operation.Id))
                {
                    // Special flow capture reference operation where the corresponding flow capture
                    // is an LValue flow capture but the flow capture reference is not lvalue capture reference.
                    var flowCaptureForCaptureId = DataFlowAnalysisContext.ControlFlowGraph
                                                    .DescendantOperations<IFlowCaptureOperation>(OperationKind.FlowCapture)
                                                    .FirstOrDefault(fc => fc.Id.Equals(operation.Id));
                    if (flowCaptureForCaptureId != null)
                    {
                        return GetCachedAbstractValue(flowCaptureForCaptureId.Value);
                    }
                }
                else
                {
                    PerformFlowCaptureReferencePredicateAnalysis();
                    return ComputeAnalysisValueForReferenceOperation(operation, value);
                }
            }

            return ValueDomain.UnknownOrMayBeValue;

            void PerformFlowCaptureReferencePredicateAnalysis()
            {
                if (!PredicateAnalysis)
                {
                    return;
                }

                var result = AnalysisEntityFactory.TryCreate(operation, out var flowCaptureReferenceEntity);
                Debug.Assert(result);
                RoslynDebug.Assert(flowCaptureReferenceEntity != null);
                RoslynDebug.Assert(flowCaptureReferenceEntity.CaptureId != null);
                if (!HasPredicatedDataForEntity(flowCaptureReferenceEntity))
                {
                    return;
                }

                PerformPredicateAnalysis(operation);
                Debug.Assert(HasPredicatedDataForEntity(flowCaptureReferenceEntity));
            }
        }

        public override TAbstractAnalysisValue VisitFlowCapture(IFlowCaptureOperation operation, object? argument)
        {
            var value = Visit(operation.Value, argument);
            if (!IsLValueFlowCapture(operation))
            {
                SetAbstractValueForAssignment(target: operation, assignedValueOperation: operation.Value, assignedValue: value);
                PerformFlowCapturePredicateAnalysis();
                return value;
            }

            return ValueDomain.UnknownOrMayBeValue;

            void PerformFlowCapturePredicateAnalysis()
            {
                if (!PredicateAnalysis)
                {
                    return;
                }

                if (operation.Value.TryGetBoolConstantValue(out bool constantValue) &&
                    AnalysisEntityFactory.TryCreate(operation, out var flowCaptureEntity))
                {
                    Debug.Assert(flowCaptureEntity.CaptureId != null);
                    TAnalysisData predicatedData = GetEmptyAnalysisData();
                    TAnalysisData? truePredicatedData, falsePredicatedData;
                    if (constantValue)
                    {
                        truePredicatedData = predicatedData;
                        falsePredicatedData = null;
                    }
                    else
                    {
                        falsePredicatedData = predicatedData;
                        truePredicatedData = null;
                    }

                    StartTrackingPredicatedData(flowCaptureEntity, truePredicatedData, falsePredicatedData);
                }
            }
        }

        public override TAbstractAnalysisValue VisitDefaultValue(IDefaultValueOperation operation, object? argument)
        {
            return GetAbstractDefaultValue(operation.Type);
        }

        public override TAbstractAnalysisValue VisitInterpolation(IInterpolationOperation operation, object? argument)
        {
            var expressionValue = Visit(operation.Expression, argument);
            _ = Visit(operation.FormatString, argument);
            _ = Visit(operation.Alignment, argument);
            return expressionValue;
        }

        public override TAbstractAnalysisValue VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, object? argument)
        {
            return Visit(operation.Text, argument);
        }

        public sealed override TAbstractAnalysisValue VisitArgument(IArgumentOperation operation, object? argument)
        {
            var value = Visit(operation.Value, argument);

            // Is first argument of a Contract check invocation?
            if (PredicateAnalysis && IsContractCheckArgument(operation))
            {
                Debug.Assert(FlowBranchConditionKind == ControlFlowConditionKind.None);

                // Force true branch.
                FlowBranchConditionKind = ControlFlowConditionKind.WhenTrue;
                PerformPredicateAnalysis(operation);
                FlowBranchConditionKind = ControlFlowConditionKind.None;
            }

            _pendingArgumentsToPostProcess.Add(operation);
            _pendingArgumentsToReset.Add(operation);
            return value;
        }

        /// <summary>
        /// Invoked after the parent invocation/creation operation of the given argument has been visited.
        /// </summary>
        /// <param name="operation">Argument to be post-processed.</param>
        /// <param name="isEscaped">Boolean flag indicating if the argument was escaped due to lack of interprocedural analysis or not.</param>
        protected virtual void PostProcessArgument(IArgumentOperation operation, bool isEscaped)
        {
        }

        /// <summary>
        /// Post process argument which needs to be escaped/reset after being passed to an invocation/creation target
        /// without interprocedural analysis.
        /// This method resets the analysis data for an object instance passed around as an <see cref="IArgumentOperation"/>
        /// and also handles resetting the argument value for ref/out parameter.
        /// </summary>
        private void PostProcessEscapedArgument(IArgumentOperation operation)
        {
            // For reference types passed as arguments,
            // reset all analysis data for the instance members as the content might change for them.
            if (HasPointsToAnalysisResult &&
                PessimisticAnalysis &&
                operation.Value.Type != null &&
                !operation.Value.Type.HasValueCopySemantics())
            {
                ResetReferenceTypeInstanceAnalysisData(operation.Value);
            }

            // Handle ref/out arguments as escapes.
            if (operation.Parameter?.RefKind is RefKind.Ref or RefKind.Out)
            {
                var value = ComputeAnalysisValueForEscapedRefOrOutArgument(operation, defaultValue: ValueDomain.UnknownOrMayBeValue);
                if (operation.Parameter.RefKind != RefKind.Out)
                {
                    value = ValueDomain.Merge(value, GetCachedAbstractValue(operation.Value));
                }

                CacheAbstractValue(operation, value);
                SetAbstractValueForAssignment(operation.Value, operation, value);
            }
        }

        public override TAbstractAnalysisValue VisitConstantPattern(IConstantPatternOperation operation, object? argument)
        {
            return Visit(operation.Value, argument);
        }

        public override TAbstractAnalysisValue VisitParenthesized(IParenthesizedOperation operation, object? argument)
        {
            return Visit(operation.Operand, argument);
        }

        public override TAbstractAnalysisValue VisitTranslatedQuery(ITranslatedQueryOperation operation, object? argument)
        {
            return Visit(operation.Operation, argument);
        }

        public override TAbstractAnalysisValue VisitConversion(IConversionOperation operation, object? argument)
        {
            var operandValue = Visit(operation.Operand, argument);

            // Conservative for error code and user defined operator.
            return operation.Conversion.Exists && !operation.Conversion.IsUserDefined ? operandValue : ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue VisitObjectCreation(IObjectCreationOperation operation, object? argument)
        {
            Debug.Assert(operation.Initializer == null, "Object or collection initializer must have been lowered in the CFG");

            var defaultValue = base.VisitObjectCreation(operation, argument)!;

            var method = operation.Constructor!;
            ControlFlowGraph? getCfg() => GetInterproceduralControlFlowGraph(method);

            return PerformInterproceduralAnalysis(getCfg, method, instanceReceiver: null,
                operation.Arguments, operation, defaultValue, isLambdaOrLocalFunction: false, out _);
        }

        public sealed override TAbstractAnalysisValue VisitInvocation(IInvocationOperation operation, object? argument)
        {
            TAbstractAnalysisValue value;
            if (operation.TargetMethod.IsLambdaOrLocalFunctionOrDelegate())
            {
                // Invocation of a lambda or delegate or local function.
                value = VisitInvocation_LambdaOrDelegateOrLocalFunction(operation, argument, out var resolvedMethodTargetsOpt);
                CacheAbstractValue(operation, value);

                // Check if we have known possible set of invoked methods.
                if (resolvedMethodTargetsOpt != null)
                {
                    foreach ((IMethodSymbol method, _) in resolvedMethodTargetsOpt)
                    {
                        PostVisitInvocation(method, operation.Arguments);
                    }
                }
            }
            else
            {
                value = VisitInvocation_NonLambdaOrDelegateOrLocalFunction(operation, argument);
                CacheAbstractValue(operation, value);

                switch (operation.Arguments.Length)
                {
                    case 1:
                        if (operation.Instance != null && operation.TargetMethod.IsTaskConfigureAwaitMethod(GenericTaskNamedType))
                        {
                            // ConfigureAwait invocation - just return the abstract value of the visited instance on which it is invoked.
                            value = GetCachedAbstractValue(operation.Instance);
                        }
                        else if (operation.TargetMethod.IsTaskFromResultMethod(TaskNamedType))
                        {
                            // Result wrapped within a task.
                            var wrappedOperationValue = GetCachedAbstractValue(operation.Arguments[0].Value);
                            var pointsToValueOfTask = GetPointsToAbstractValue(operation);
                            SetTaskWrappedValue(pointsToValueOfTask, wrappedOperationValue);
                        }

                        break;

                    case 2:
                        if (operation.Instance == null &&
                            operation.TargetMethod.IsAsyncDisposableConfigureAwaitMethod(IAsyncDisposableNamedType, TaskAsyncEnumerableExtensions))
                        {
                            // ConfigureAwait invocation - just return the abstract value of the visited instance on which it is invoked.
                            value = GetCachedAbstractValue(operation.Arguments.GetArgumentForParameterAtIndex(0));
                        }

                        break;
                }

                PostVisitInvocation(operation.TargetMethod, operation.Arguments);
            }

            return value;

            // Local functions.
            void PostVisitInvocation(IMethodSymbol targetMethod, ImmutableArray<IArgumentOperation> arguments)
            {
                // Predicate analysis for different equality compare method invocations.
                if (PredicateAnalysis &&
                    operation.Type?.SpecialType == SpecialType.System_Boolean &&
                    (targetMethod.Name.EndsWith("Equals", StringComparison.Ordinal) ||
                     targetMethod.IsArgumentNullCheckMethod()))
                {
                    PerformPredicateAnalysis(operation);
                }

                if (targetMethod.IsLockMethod(MonitorNamedType))
                {
                    // "System.Threading.Monitor.Enter(object)" OR "System.Threading.Monitor.Enter(object, bool)"
                    Debug.Assert(!arguments.IsEmpty);

                    HandleEnterLockOperation(arguments[0].Value);
                }
                else if (InterlockedNamedType != null &&
                    SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType.OriginalDefinition, InterlockedNamedType))
                {
                    ProcessInterlockedOperation(targetMethod, arguments, InterlockedNamedType);
                }
            }

            void ProcessInterlockedOperation(IMethodSymbol targetMethod, ImmutableArray<IArgumentOperation> arguments, INamedTypeSymbol interlockedType)
            {
                var isExchangeMethod = targetMethod.IsInterlockedExchangeMethod(interlockedType);
                var isCompareExchangeMethod = targetMethod.IsInterlockedCompareExchangeMethod(interlockedType);

                if (isExchangeMethod || isCompareExchangeMethod)
                {
                    // "System.Threading.Interlocked.Exchange(ref T, T)" OR "System.Threading.Interlocked.CompareExchange(ref T, T, T)"
                    Debug.Assert(arguments.Length >= 2);

                    SetAbstractValueForAssignment(
                        target: arguments[0].Value,
                        assignedValueOperation: arguments[1].Value,
                        assignedValue: GetCachedAbstractValue(arguments[1].Value),
                        mayBeAssignment: isCompareExchangeMethod);
                    foreach (var argument in arguments)
                    {
                        _pendingArgumentsToReset.Remove(argument);
                    }
                }
            }
        }

        private TAbstractAnalysisValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IInvocationOperation operation, object? argument)
        {
            var value = base.VisitInvocation(operation, argument)!;
            return VisitInvocation_NonLambdaOrDelegateOrLocalFunction(operation.TargetMethod, operation.Instance, operation.Arguments,
                invokedAsDelegate: false, originalOperation: operation, defaultValue: value);
        }

        private protected bool MarkEscapedLambdasAndLocalFunctions(PointsToAbstractValue pointsToAbstractValue)
        {
            Debug.Assert(IsPointsToAnalysis);

            var hasEscapes = false;
            using var _1 = PooledHashSet<(IMethodSymbol method, IOperation? instance)>.GetInstance(out var methodTargetsOptBuilder);
            using var _2 = PooledHashSet<IFlowAnonymousFunctionOperation>.GetInstance(out var lambdaTargets);
            if (ResolveLambdaOrDelegateOrLocalFunctionTargets(pointsToAbstractValue, methodTargetsOptBuilder, lambdaTargets))
            {
                foreach (var (targetMethod, _) in methodTargetsOptBuilder)
                {
                    if (targetMethod.MethodKind == MethodKind.LocalFunction)
                    {
                        _escapedLocalFunctions.Add(targetMethod);
                        hasEscapes = true;
                    }
                }

                foreach (var flowAnonymousFunctionOperation in lambdaTargets)
                {
                    _escapedLambdas.Add(flowAnonymousFunctionOperation);
                    hasEscapes = true;
                }
            }

            return hasEscapes;
        }

        private bool ResolveLambdaOrDelegateOrLocalFunctionTargets(
            PointsToAbstractValue invocationTarget,
            PooledHashSet<(IMethodSymbol method, IOperation? instance)> methodTargetsOptBuilder,
            PooledHashSet<IFlowAnonymousFunctionOperation> lambdaTargets)
        => ResolveLambdaOrDelegateOrLocalFunctionTargetsCore(operation: null, invocationTarget, methodTargetsOptBuilder, lambdaTargets);

        private bool ResolveLambdaOrDelegateOrLocalFunctionTargets(
            IOperation operation,
            PooledHashSet<(IMethodSymbol method, IOperation? instance)> methodTargetsOptBuilder,
            PooledHashSet<IFlowAnonymousFunctionOperation> lambdaTargets)
        => ResolveLambdaOrDelegateOrLocalFunctionTargetsCore(operation, invocationTarget: null, methodTargetsOptBuilder, lambdaTargets);

        private bool ResolveLambdaOrDelegateOrLocalFunctionTargetsCore(
            IOperation? operation,
            PointsToAbstractValue? invocationTarget,
            PooledHashSet<(IMethodSymbol method, IOperation? instance)> methodTargetsOptBuilder,
            PooledHashSet<IFlowAnonymousFunctionOperation> lambdaTargets)
        {
            Debug.Assert(operation != null ^ invocationTarget != null);

            var knownTargetInvocations = false;
            IOperation? instance;
            if (operation is IInvocationOperation invocation)
            {
                instance = invocation.Instance;

                var targetMethod = invocation.TargetMethod;
                if (targetMethod.MethodKind == MethodKind.LocalFunction)
                {
                    Debug.Assert(invocation.Instance == null);

                    knownTargetInvocations = true;
                    AddMethodTarget(targetMethod, instance: null);
                    return knownTargetInvocations;
                }
                else
                {
                    Debug.Assert(targetMethod.MethodKind is MethodKind.LambdaMethod or
                        MethodKind.DelegateInvoke);
                }
            }
            else
            {
                instance = operation;
            }

            if (invocationTarget == null &&
                HasPointsToAnalysisResult &&
                instance != null)
            {
                invocationTarget = GetPointsToAbstractValue(instance);
            }

            if (invocationTarget?.Kind == PointsToAbstractValueKind.KnownLocations)
            {
                knownTargetInvocations = true;
                foreach (var location in invocationTarget.Locations)
                {
                    if (!HandleCreationOpt(location.Creation))
                    {
                        knownTargetInvocations = false;
                        break;
                    }
                }
            }

            return knownTargetInvocations;

            // Local functions.
            void AddMethodTarget(IMethodSymbol method, IOperation? instance)
            {
                Debug.Assert(knownTargetInvocations);

                methodTargetsOptBuilder.Add((method, instance));
            }

            void AddLambdaTarget(IFlowAnonymousFunctionOperation lambda)
            {
                Debug.Assert(knownTargetInvocations);

                lambdaTargets.Add(lambda);
            }

            bool HandleCreationOpt(IOperation? creation)
            {
                Debug.Assert(knownTargetInvocations);

                switch (creation)
                {
                    case ILocalFunctionOperation localFunctionOperation:
                        AddMethodTarget(localFunctionOperation.Symbol, instance: null);
                        return true;

                    case IMethodReferenceOperation methodReferenceOperation:
                        AddMethodTarget(methodReferenceOperation.Method, methodReferenceOperation.Instance);
                        return true;

                    case IDelegateCreationOperation delegateCreationOperation:
                        return HandleDelegateCreationTarget(delegateCreationOperation);

                    default:
                        return false;
                }
            }

            bool HandleDelegateCreationTarget(IDelegateCreationOperation delegateCreationOperation)
            {
                Debug.Assert(knownTargetInvocations);

                switch (delegateCreationOperation.Target)
                {
                    case IFlowAnonymousFunctionOperation lambdaOperation:
                        AddLambdaTarget(lambdaOperation);
                        return true;

                    case IMethodReferenceOperation methodReferenceOperation:
                        AddMethodTarget(methodReferenceOperation.Method, methodReferenceOperation.Instance);
                        return true;

                    default:
                        return false;
                }
            }
        }

        private TAbstractAnalysisValue VisitInvocation_LambdaOrDelegateOrLocalFunction(
            IInvocationOperation operation,
            object? argument,
            out ImmutableHashSet<(IMethodSymbol method, IOperation? instance)>? resolvedMethodTargets)
        {
            var value = base.VisitInvocation(operation, argument)!;

            using var _1 = PooledHashSet<(IMethodSymbol method, IOperation? instance)>.GetInstance(out var methodTargetsOptBuilder);
            using var _2 = PooledHashSet<IFlowAnonymousFunctionOperation>.GetInstance(out var lambdaTargets);
            if (ResolveLambdaOrDelegateOrLocalFunctionTargets(operation, methodTargetsOptBuilder, lambdaTargets))
            {
                resolvedMethodTargets = methodTargetsOptBuilder.ToImmutable();
                AnalyzePossibleTargetInvocations();
            }
            else
            {
                resolvedMethodTargets = null;
                if (PessimisticAnalysis)
                {
                    ResetCurrentAnalysisData();
                }
            }

            return value;

            void AnalyzePossibleTargetInvocations()
            {
                Debug.Assert(methodTargetsOptBuilder.Count > 0 || lambdaTargets.Count > 0);

                TAnalysisData? mergedCurrentAnalysisData = null;
                var first = true;
                var defaultValue = value;

                using var savedCurrentAnalysisData = GetClonedCurrentAnalysisData();
                foreach (var (method, instance) in methodTargetsOptBuilder)
                {
                    var oldMergedAnalysisData = mergedCurrentAnalysisData;
                    mergedCurrentAnalysisData = AnalyzePossibleTargetInvocation(
                        computeValueForInvocation: () => method.MethodKind == MethodKind.LocalFunction ?
                            VisitInvocation_LocalFunction(method, operation.Arguments, operation, defaultValue) :
                            VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, instance, operation.Arguments,
                                invokedAsDelegate: true, originalOperation: operation, defaultValue: defaultValue),
                        inputAnalysisData: savedCurrentAnalysisData,
                        mergedAnalysisData: mergedCurrentAnalysisData,
                        first: ref first);
                    Debug.Assert(!ReferenceEquals(oldMergedAnalysisData, CurrentAnalysisData));
                    oldMergedAnalysisData?.Dispose();
                }

                foreach (var lambda in lambdaTargets)
                {
                    var oldMergedAnalysisData = mergedCurrentAnalysisData;
                    mergedCurrentAnalysisData = AnalyzePossibleTargetInvocation(
                        computeValueForInvocation: () => VisitInvocation_Lambda(lambda, operation.Arguments, operation, defaultValue),
                        inputAnalysisData: savedCurrentAnalysisData,
                        mergedAnalysisData: mergedCurrentAnalysisData,
                        first: ref first);
                    Debug.Assert(!ReferenceEquals(oldMergedAnalysisData, CurrentAnalysisData));
                    oldMergedAnalysisData?.Dispose();
                }

                Debug.Assert(mergedCurrentAnalysisData == null || ReferenceEquals(mergedCurrentAnalysisData, CurrentAnalysisData));
            }

            TAnalysisData AnalyzePossibleTargetInvocation(Func<TAbstractAnalysisValue> computeValueForInvocation, TAnalysisData inputAnalysisData, TAnalysisData? mergedAnalysisData, ref bool first)
            {
                CurrentAnalysisData = GetClonedAnalysisData(inputAnalysisData);
                var invocationValue = computeValueForInvocation();

                if (first)
                {
                    first = false;
                    value = invocationValue;
                }
                else
                {
                    RoslynDebug.Assert(mergedAnalysisData != null);

                    value = ValueDomain.Merge(value, invocationValue);
                    var result = MergeAnalysisData(mergedAnalysisData, CurrentAnalysisData);
                    CurrentAnalysisData.Dispose();
                    CurrentAnalysisData = result;
                }

                return CurrentAnalysisData;
            }
        }

        /// <summary>
        /// Visits an invocation, either as a direct method call, or intermediately through a delegate.
        /// </summary>
        /// <param name="method">Method that is invoked.</param>
        /// <param name="visitedInstance">Instance that the method is invoked on, if any.</param>
        /// <param name="visitedArguments">Arguments to the invoked method.</param>
        /// <param name="invokedAsDelegate">Indicates that invocation is a delegate invocation.</param>
        /// <param name="originalOperation">Original invocation operation, which may be a delegate invocation.</param>
        /// <param name="defaultValue">Default abstract value to return.</param>
        /// <returns>Abstract value of return value.</returns>
        public virtual TAbstractAnalysisValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            IMethodSymbol method,
            IOperation? visitedInstance,
            ImmutableArray<IArgumentOperation> visitedArguments,
            bool invokedAsDelegate,
            IOperation originalOperation,
            TAbstractAnalysisValue defaultValue)
        {
            ControlFlowGraph? getCfg() => GetInterproceduralControlFlowGraph(method);

            return PerformInterproceduralAnalysis(getCfg, method, visitedInstance,
                visitedArguments, originalOperation, defaultValue, isLambdaOrLocalFunction: false, out _);
        }

        private ControlFlowGraph? GetInterproceduralControlFlowGraph(IMethodSymbol method)
        {
            if (DataFlowAnalysisContext.InterproceduralAnalysisData != null)
            {
                return DataFlowAnalysisContext.InterproceduralAnalysisData.GetInterproceduralControlFlowGraph(method);
            }

            RoslynDebug.Assert(_interproceduralMethodToCfgMap != null);

            if (!_interproceduralMethodToCfgMap.TryGetValue(method, out var cfg))
            {
                var operation = method.GetTopmostOperationBlock(WellKnownTypeProvider.Compilation);
                cfg = operation?.GetEnclosingControlFlowGraph();
                _interproceduralMethodToCfgMap.Add(method, cfg);
            }

            return cfg;
        }

        private ImmutableStack<IOperation>? GetInterproceduralCallStackForOwningSymbol(ISymbol forOwningSymbol)
        {
            if (OwningSymbol.Equals(forOwningSymbol))
            {
                return DataFlowAnalysisContext.InterproceduralAnalysisData?.CallStack;
            }

            return DataFlowAnalysisContext.InterproceduralAnalysisData?.GetInterproceduralCallStackForOwningSymbol(forOwningSymbol);
        }

        public virtual TAbstractAnalysisValue VisitInvocation_LocalFunction(
            IMethodSymbol localFunction,
            ImmutableArray<IArgumentOperation> visitedArguments,
            IOperation originalOperation,
            TAbstractAnalysisValue defaultValue)
        {
            ControlFlowGraph? getCfg() => DataFlowAnalysisContext.GetLocalFunctionControlFlowGraph(localFunction);
            var value = PerformInterproceduralAnalysis(getCfg, localFunction, instanceReceiver: null, arguments: visitedArguments,
                originalOperation: originalOperation, defaultValue: defaultValue, isLambdaOrLocalFunction: true, out var wasAnalyzed);
            if (wasAnalyzed)
            {
                Debug.Assert(_interproceduralResultsBuilder.ContainsKey(originalOperation));
                _analyzedLocalFunctions.Add(localFunction);
            }

            return value;
        }

        public virtual TAbstractAnalysisValue VisitInvocation_Lambda(
            IFlowAnonymousFunctionOperation lambda,
            ImmutableArray<IArgumentOperation> visitedArguments,
            IOperation originalOperation,
            TAbstractAnalysisValue defaultValue)
        {
            ControlFlowGraph? getCfg() => DataFlowAnalysisContext.GetAnonymousFunctionControlFlowGraph(lambda);
            var value = PerformInterproceduralAnalysis(getCfg, lambda.Symbol, instanceReceiver: null, arguments: visitedArguments,
                originalOperation: originalOperation, defaultValue: defaultValue, isLambdaOrLocalFunction: true, out var wasAnalyzed);
            if (wasAnalyzed)
            {
                Debug.Assert(_interproceduralResultsBuilder.ContainsKey(originalOperation));
                _analyzedLambdas.Add(lambda);
            }

            return value;
        }

        public override TAbstractAnalysisValue VisitDelegateCreation(IDelegateCreationOperation operation, object? argument)
        {
            var value = base.VisitDelegateCreation(operation, argument)!;
            if (!HasPointsToAnalysisResult)
            {
                switch (operation.Target)
                {
                    case IFlowAnonymousFunctionOperation flowAnonymousFunction:
                        _escapedLambdas.Add(flowAnonymousFunction);
                        break;

                    case IMethodReferenceOperation methodReference:
                        if (methodReference.Method.MethodKind == MethodKind.LocalFunction)
                        {
                            _escapedLocalFunctions.Add(methodReference.Method);
                            _escapedLocalFunctions.Add(methodReference.Method);
                        }

                        break;
                }
            }

            return value;
        }

        public virtual void HandleEnterLockOperation(IOperation lockedObject)
        {
            // Multi-threaded instance method.
            // Conservatively reset all the instance analysis data for the ThisOrMeInstance.
            ResetThisOrMeInstanceAnalysisData();
        }

        /// <summary>
        /// Reset all the instance analysis data for <see cref="AnalysisEntityFactory.ThisOrMeInstance"/> if <see cref="HasPointsToAnalysisResult"/> is true and <see cref="PessimisticAnalysis"/> is also true.
        /// If we are using or performing points to analysis, certain operations can invalidate all the analysis data off the containing instance.
        /// </summary>
        private void ResetThisOrMeInstanceAnalysisData()
        {
            if (!HasPointsToAnalysisResult || !PessimisticAnalysis)
            {
                return;
            }

            if (AnalysisEntityFactory.ThisOrMeInstance.Type.HasValueCopySemantics())
            {
                ResetValueTypeInstanceAnalysisData(AnalysisEntityFactory.ThisOrMeInstance);
            }
            else
            {
                ResetReferenceTypeInstanceAnalysisData(ThisOrMePointsToAbstractValue);
            }
        }

        public override TAbstractAnalysisValue VisitTuple(ITupleOperation operation, object? argument)
        {
            using var _ = ArrayBuilder<TAbstractAnalysisValue>.GetInstance(operation.Elements.Length, out var elementValueBuilder);

            foreach (var element in operation.Elements)
            {
                elementValueBuilder.Add(Visit(element, argument));
            }

            // Set abstract value for tuple element/field assignment if the tuple is not target of a deconstruction assignment.
            // For deconstruction assignment, the value would be assigned from the computed value for the right side of the assignment.
            var deconstructionAncestor = operation.GetAncestor<IDeconstructionAssignmentOperation>(OperationKind.DeconstructionAssignment);
            if (deconstructionAncestor == null ||
                !deconstructionAncestor.Target.Descendants().Contains(operation))
            {
                if (AnalysisEntityFactory.TryCreateForTupleElements(operation, out var elementEntities))
                {
                    Debug.Assert(elementEntities.Length == elementValueBuilder.Count);
                    Debug.Assert(elementEntities.Length == operation.Elements.Length);
                    for (int i = 0; i < elementEntities.Length; i++)
                    {
                        var tupleElementEntity = elementEntities[i];
                        var assignedValueOperation = operation.Elements[i];
                        var assignedValue = elementValueBuilder[i];
                        SetAbstractValueForTupleElementAssignment(tupleElementEntity, assignedValueOperation, assignedValue);
                    }
                }
                else
                {
                    // Reset data for elements.
                    foreach (var element in operation.Elements)
                    {
                        SetAbstractValueForAssignment(element, operation, ValueDomain.UnknownOrMayBeValue);
                    }
                }
            }

            return GetAbstractDefaultValue(operation.Type);
        }

        public virtual TAbstractAnalysisValue VisitUnaryOperatorCore(IUnaryOperation operation, object? argument)
        {
            return Visit(operation.Operand, argument);
        }

        public sealed override TAbstractAnalysisValue VisitUnaryOperator(IUnaryOperation operation, object? argument)
        {
            var value = VisitUnaryOperatorCore(operation, argument);
            if (PredicateAnalysis && operation.OperatorKind == UnaryOperatorKind.Not)
            {
                PerformPredicateAnalysis(operation);
            }

            return value;
        }

        public virtual TAbstractAnalysisValue VisitBinaryOperatorCore(IBinaryOperation operation, object? argument)
        {
            return base.VisitBinaryOperator(operation, argument)!;
        }

        public sealed override TAbstractAnalysisValue VisitBinaryOperator(IBinaryOperation operation, object? argument)
        {
            var value = VisitBinaryOperatorCore(operation, argument)!;
            if (PredicateAnalysis && operation.IsComparisonOperator())
            {
                PerformPredicateAnalysis(operation);
            }

            return value;
        }

        public override TAbstractAnalysisValue VisitIsNull(IIsNullOperation operation, object? argument)
        {
            var value = base.VisitIsNull(operation, argument)!;
            if (PredicateAnalysis)
            {
                PerformPredicateAnalysis(operation);
            }

            return value;
        }

        public override TAbstractAnalysisValue VisitCaughtException(ICaughtExceptionOperation operation, object? argument)
        {
            // Merge data from unhandled exception paths within try that match the caught exception type.
            if (operation.Type != null)
            {
                MergeAnalysisDataFromUnhandledThrowOperations(operation.Type);
            }

            return base.VisitCaughtException(operation, argument)!;
        }

        private void MergeAnalysisDataFromUnhandledThrowOperations(ITypeSymbol? caughtExceptionType)
        {
            Debug.Assert(caughtExceptionType != null || CurrentBasicBlock.IsFirstBlockOfFinally(out _));

            if (AnalysisDataForUnhandledThrowOperations?.Count > 0)
            {
                foreach (ThrownExceptionInfo pendingThrow in AnalysisDataForUnhandledThrowOperations.Keys.ToArray())
                {
                    if (ShouldHandlePendingThrow(pendingThrow))
                    {
                        var previousCurrentAnalysisData = CurrentAnalysisData;
                        var exceptionData = AnalysisDataForUnhandledThrowOperations[pendingThrow];
                        CurrentAnalysisData = MergeAnalysisData(previousCurrentAnalysisData, exceptionData);
                        AssertValidAnalysisData(CurrentAnalysisData);
                        previousCurrentAnalysisData.Dispose();
                        if (caughtExceptionType != null)
                        {
                            AnalysisDataForUnhandledThrowOperations.Remove(pendingThrow);
                            exceptionData.Dispose();
                        }
                    }
                }
            }

            bool ShouldHandlePendingThrow(ThrownExceptionInfo pendingThrow)
            {
                if (pendingThrow.HandlingCatchRegion == CurrentBasicBlock.EnclosingRegion)
                {
                    // Catch region explicitly handling the thrown exception.
                    return true;
                }

                if (caughtExceptionType == null)
                {
                    // Check if finally region is executed for pending throw.
                    Debug.Assert(CurrentBasicBlock.IsFirstBlockOfFinally(out _));
                    var tryFinallyRegion = CurrentBasicBlock.GetContainingRegionOfKind(ControlFlowRegionKind.TryAndFinally)!;
                    var tryRegion = tryFinallyRegion.NestedRegions[0];
                    return tryRegion.FirstBlockOrdinal <= pendingThrow.BasicBlockOrdinal && tryRegion.LastBlockOrdinal >= pendingThrow.BasicBlockOrdinal;
                }

                return false;
            }
        }

        public override TAbstractAnalysisValue VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, object? argument)
        {
            var value = base.VisitFlowAnonymousFunction(operation, argument)!;
            _visitedLambdas.Add(operation);
            return value;
        }

        public override TAbstractAnalysisValue VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, object? argument)
        {
            // https://github.com/dotnet/roslyn-analyzers/issues/1571 tracks adding support.
            return base.VisitStaticLocalInitializationSemaphore(operation, argument)!;
        }

        public override TAbstractAnalysisValue VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object? argument)
        {
            var savedIsInsideAnonymousObjectInitializer = IsInsideAnonymousObjectInitializer;
            IsInsideAnonymousObjectInitializer = true;
            var value = base.VisitAnonymousObjectCreation(operation, argument)!;
            IsInsideAnonymousObjectInitializer = savedIsInsideAnonymousObjectInitializer;
            return value;
        }

        public sealed override TAbstractAnalysisValue VisitReturn(IReturnOperation operation, object? argument)
        {
            Debug.Assert(operation.Kind == OperationKind.YieldReturn, "IReturnOperation must have been lowered in the CFG");

            var value = Visit(operation.ReturnedValue, argument);
            ProcessReturnValue(operation.ReturnedValue);

            return value;
        }

        public virtual TAbstractAnalysisValue GetAssignedValueForPattern(IIsPatternOperation operation, TAbstractAnalysisValue operandValue)
        {
            return operandValue;
        }

        public override TAbstractAnalysisValue VisitIsPattern(IIsPatternOperation operation, object? argument)
        {
            // "c is D d" OR "x is 1"
            var operandValue = Visit(operation.Value, argument);
            _ = Visit(operation.Pattern, argument);

            var patternValue = GetAssignedValueForPattern(operation, operandValue);
            if (operation.Pattern is IDeclarationPatternOperation)
            {
                SetAbstractValueForAssignment(
                    target: operation.Pattern,
                    assignedValueOperation: operation.Value,
                    assignedValue: patternValue);
            }

            if (PredicateAnalysis)
            {
                PerformPredicateAnalysis(operation);
            }

            return ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue VisitAwait(IAwaitOperation operation, object? argument)
        {
            var value = base.VisitAwait(operation, argument)!;

            var pointsToValue = GetPointsToAbstractValue(operation.Operation);
            return TryGetTaskWrappedValue(pointsToValue, out var awaitedValue) ?
                awaitedValue :
                value;
        }

        #region Overrides for lowered IOperations

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitUsing(IUsingOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IUsingOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitWhileLoop(IWhileLoopOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IWhileLoopOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitForEachLoop(IForEachLoopOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IForEachLoopOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitForLoop(IForLoopOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IForLoopOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitForToLoop(IForToLoopOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IForToLoopOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitCoalesce(ICoalesceOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ICoalesceOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitConditional(IConditionalOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IConditionalOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitConditionalAccess(IConditionalAccessOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IConditionalAccessOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IConditionalAccessInstanceOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitThrow(IThrowOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IThrowOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitVariableDeclaration(IVariableDeclarationOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IVariableDeclarationOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IVariableDeclarationOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitVariableDeclarator(IVariableDeclaratorOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IVariableDeclaratorOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitTry(ITryOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ITryOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitCatchClause(ICatchClauseOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ICatchClauseOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public override TAbstractAnalysisValue VisitLock(ILockOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ILockOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitBranch(IBranchOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IBranchOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitLabeled(ILabeledOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ILabeledOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitSwitch(ISwitchOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ISwitchOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitSwitchCase(ISwitchCaseOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ISwitchCaseOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IDefaultCaseClauseOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitPatternCaseClause(IPatternCaseClauseOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IPatternCaseClauseOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitRangeCaseClause(IRangeCaseClauseOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IRangeCaseClauseOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IRelationalCaseClauseOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ISingleValueCaseClauseOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IObjectOrCollectionInitializerOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitMemberInitializer(IMemberInitializerOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IMemberInitializerOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitBlock(IBlockOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IBlockOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitVariableInitializer(IVariableInitializerOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IVariableInitializerOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitFieldInitializer(IFieldInitializerOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IFieldInitializerOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitParameterInitializer(IParameterInitializerOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IParameterInitializerOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitPropertyInitializer(IPropertyInitializerOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IPropertyInitializerOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitEnd(IEndOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IEndOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitEmpty(IEmptyOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IEmptyOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitNameOf(INameOfOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(INameOfOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitAnonymousFunction(IAnonymousFunctionOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(IAnonymousFunctionOperation)}' must have been lowered in the CFG");
        }

        [ExcludeFromCodeCoverage]
        public sealed override TAbstractAnalysisValue VisitLocalFunction(ILocalFunctionOperation operation, object? argument)
        {
            throw new NotSupportedException($"'{nameof(ILocalFunctionOperation)}' must have been lowered in the CFG");
        }

        #endregion

        #endregion

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Exception"/>
        /// </summary>
        protected INamedTypeSymbol? ExceptionNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for 'System.Diagnostics.Contracts.Contract' type. />
        /// </summary>
        protected INamedTypeSymbol? ContractNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IDisposable"/>
        /// </summary>
        protected INamedTypeSymbol? IDisposableNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for "System.IAsyncDisposable"
        /// </summary>
        private INamedTypeSymbol? IAsyncDisposableNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for "System.Runtime.CompilerServices.ConfiguredAsyncDisposable"
        /// </summary>
        private INamedTypeSymbol? ConfiguredAsyncDisposable { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable"
        /// </summary>
        private INamedTypeSymbol? ConfiguredValueTaskAwaitable { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Tasks.Task"/>
        /// </summary>
        protected INamedTypeSymbol? TaskNamedType { get; }

#pragma warning disable CA1200 // Avoid using cref tags with a prefix - cref prefix required for one of the project contexts
        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="T:System.Threading.Tasks.TaskAsyncEnumerableExtensions"/>
        /// </summary>
        private INamedTypeSymbol? TaskAsyncEnumerableExtensions { get; }
#pragma warning restore CA1200 // Avoid using cref tags with a prefix

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IO.MemoryStream"/>
        /// </summary>
        protected INamedTypeSymbol? MemoryStreamNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for System.Threading.Tasks.ValueTask/>
        /// </summary>
        private INamedTypeSymbol? ValueTaskNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Tasks.Task{TResult}"/>
        /// </summary>
        protected INamedTypeSymbol? GenericTaskNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Monitor"/>
        /// </summary>
        protected INamedTypeSymbol? MonitorNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.Threading.Interlocked"/>
        /// </summary>
        protected INamedTypeSymbol? InterlockedNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for 'System.Runtime.Serialization.SerializationInfo' type />
        /// </summary>
        protected INamedTypeSymbol? SerializationInfoNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for 'System.Runtime.Serialization.StreamingContext' type />
        /// </summary>
        protected INamedTypeSymbol? StreamingContextNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IEquatable{T}"/>
        /// </summary>
        protected INamedTypeSymbol? GenericIEquatableNamedType { get; }

        /// <summary>
        /// <see cref="INamedTypeSymbol"/> for <see cref="System.IO.StringReader"/>
        /// </summary>
        protected INamedTypeSymbol? StringReaderType { get; }

        /// <summary>
        /// Set containing following named types, if not null:
        /// 1. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.ICollection"/>
        /// 2. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.ICollection{T}"/>
        /// 3. <see cref="INamedTypeSymbol"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>
        /// </summary>
        protected ImmutableHashSet<INamedTypeSymbol> CollectionNamedTypes { get; }

        private IMethodSymbol? DebugAssertMethod { get; }

        private ImmutableHashSet<INamedTypeSymbol> GetWellKnownCollectionTypes()
        {
            var builder = PooledHashSet<INamedTypeSymbol>.GetInstance();
            var iCollection = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsICollection);
            if (iCollection != null)
            {
                builder.Add(iCollection);
            }

            var genericICollection = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericICollection1);
            if (genericICollection != null)
            {
                builder.Add(genericICollection);
            }

            var genericIReadOnlyCollection = WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIReadOnlyCollection1);
            if (genericIReadOnlyCollection != null)
            {
                builder.Add(genericIReadOnlyCollection);
            }

            return builder.ToImmutableAndFree();
        }

        private protected bool IsDisposable([NotNullWhen(returnValue: true)] ITypeSymbol? type)
            => type != null && type.IsDisposable(IDisposableNamedType, IAsyncDisposableNamedType, ConfiguredAsyncDisposable);

        private protected DisposeMethodKind GetDisposeMethodKind(IMethodSymbol method)
            => method.GetDisposeMethodKind(IDisposableNamedType, IAsyncDisposableNamedType, ConfiguredAsyncDisposable, TaskNamedType, ValueTaskNamedType, ConfiguredValueTaskAwaitable);
    }
}
