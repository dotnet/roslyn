// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

#pragma warning disable CA1707 // Identifiers should not contain underscores

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Operation visitor to flow the abstract dataflow analysis values across a given statement in a basic block.
    /// </summary>
    public abstract class DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue> : OperationVisitor<object, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : IDataFlowAnalysisResult<TAbstractAnalysisValue>
    {
        private static readonly DiagnosticDescriptor s_dummyDataflowAnalysisDescriptor = new DiagnosticDescriptor(
            id: "InterproceduralDataflow",
            title: string.Empty,
            messageFormat: string.Empty,
            category: string.Empty,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.DataflowAndTelemetry);

        private readonly ImmutableHashSet<CaptureId> _lValueFlowCaptures;
        private readonly ImmutableDictionary<IOperation, TAbstractAnalysisValue>.Builder _valueCacheBuilder;
        private readonly ImmutableDictionary<IOperation, PredicateValueKind>.Builder _predicateValueKindCacheBuilder;
        private readonly HashSet<IArgumentOperation> _pendingArgumentsToReset;
        private readonly List<IArgumentOperation> _pendingArgumentsToPostProcess;
        private readonly HashSet<IOperation> _visitedFlowBranchConditions;
        private readonly HashSet<IOperation> _returnValueOperationsOpt;
        private ImmutableDictionary<IParameterSymbol, AnalysisEntity> _lazyParameterEntities;
        private ImmutableHashSet<IMethodSymbol> _lazyContractCheckMethodsForPredicateAnalysis;
        private TAnalysisData _currentAnalysisData;
        private int _recursionDepth;

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
        /// Dictionary from interprocedural method symbols invoked to their corresponding <see cref="ControlFlowGraph"/>.
        /// </summary>
        private readonly Dictionary<IMethodSymbol, ControlFlowGraph> _interproceduralMethodToCfgMapOpt;
        #endregion

        protected abstract TAbstractAnalysisValue GetAbstractDefaultValue(ITypeSymbol type);
        protected virtual TAbstractAnalysisValue GetAbstractDefaultValueForCatchVariable(ICatchClauseOperation catchClause) => ValueDomain.UnknownOrMayBeValue;
        protected abstract bool HasAnyAbstractValue(TAnalysisData data);
        protected abstract void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity, ArgumentInfo<TAbstractAnalysisValue> assignedValueOpt);
        protected abstract void EscapeValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity);
        protected abstract void ResetCurrentAnalysisData();
        protected bool HasPointsToAnalysisResult => DataFlowAnalysisContext.PointsToAnalysisResultOpt != null || IsPointsToAnalysis;
        protected virtual bool IsPointsToAnalysis => false;
        internal Dictionary<ThrownExceptionInfo, TAnalysisData> AnalysisDataForUnhandledThrowOperations { get; private set; }
        public ImmutableDictionary<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>> InterproceduralResultsMap => _interproceduralResultsBuilder.ToImmutable();

        /// <summary>
        /// Optional map from points to values of tasks to the underlying abstract value returned by the task.
        /// Awaiting the task produces the task wrapped value from this map.
        /// </summary>
        internal Dictionary<PointsToAbstractValue, TAbstractAnalysisValue> TaskWrappedValuesMapOpt { get; private set; }

        protected TAnalysisContext DataFlowAnalysisContext { get; }
        public AbstractValueDomain<TAbstractAnalysisValue> ValueDomain => DataFlowAnalysisContext.ValueDomain;
        protected ISymbol OwningSymbol => DataFlowAnalysisContext.OwningSymbol;
        protected WellKnownTypeProvider WellKnownTypeProvider => DataFlowAnalysisContext.WellKnownTypeProvider;
        protected Func<TAnalysisContext, TAnalysisResult> TryGetOrComputeAnalysisResult
            => DataFlowAnalysisContext.TryGetOrComputeAnalysisResult;
        internal bool ExecutingExceptionPathsAnalysisPostPass { get; set; }

        protected TAnalysisData CurrentAnalysisData
        {
            get
            {
                Debug.Assert(!_currentAnalysisData.IsDisposed);
                return _currentAnalysisData;
            }
            private set
            {
                Debug.Assert(value != null);
                Debug.Assert(!value.IsDisposed);
                _currentAnalysisData = value;
            }
        }

        protected BasicBlock CurrentBasicBlock { get; private set; }
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

        protected bool IsLValueFlowCapture(CaptureId captureId) => _lValueFlowCaptures.Contains(captureId);

        private Dictionary<BasicBlock, ThrownExceptionInfo> _exceptionPathsThrownExceptionInfoMapOpt;
        private ThrownExceptionInfo DefaultThrownExceptionInfo
        {
            get
            {
                Debug.Assert(WellKnownTypeProvider.Exception != null);

                _exceptionPathsThrownExceptionInfoMapOpt ??= new Dictionary<BasicBlock, ThrownExceptionInfo>();
                if (!_exceptionPathsThrownExceptionInfoMapOpt.TryGetValue(CurrentBasicBlock, out var info))
                {
                    info = ThrownExceptionInfo.CreateDefaultInfoForExceptionsPathAnalysis(
                        CurrentBasicBlock, WellKnownTypeProvider, DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.CallStack);
                }

                return info;
            }
        }

        protected DataFlowOperationVisitor(TAnalysisContext analysisContext)
        {
            DataFlowAnalysisContext = analysisContext;

            _lValueFlowCaptures = LValueFlowCapturesProvider.GetOrCreateLValueFlowCaptures(analysisContext.ControlFlowGraph);
            _valueCacheBuilder = ImmutableDictionary.CreateBuilder<IOperation, TAbstractAnalysisValue>();
            _predicateValueKindCacheBuilder = ImmutableDictionary.CreateBuilder<IOperation, PredicateValueKind>();
            _pendingArgumentsToReset = new HashSet<IArgumentOperation>();
            _pendingArgumentsToPostProcess = new List<IArgumentOperation>();
            _visitedFlowBranchConditions = new HashSet<IOperation>();
            _returnValueOperationsOpt = OwningSymbol is IMethodSymbol method && !method.ReturnsVoid ? new HashSet<IOperation>() : null;
            _interproceduralResultsBuilder = ImmutableDictionary.CreateBuilder<IOperation, IDataFlowAnalysisResult<TAbstractAnalysisValue>>();

            _interproceduralCallStack = new Stack<IOperation>();
            _addressSharedEntitiesProvider = new AddressSharedEntitiesProvider<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>(analysisContext);
            if (analysisContext.InterproceduralAnalysisDataOpt != null)
            {
                foreach (var argumentInfo in analysisContext.InterproceduralAnalysisDataOpt.ArgumentValuesMap.Values)
                {
                    CacheAbstractValue(argumentInfo.Operation, argumentInfo.Value);
                }

                foreach (var operation in analysisContext.InterproceduralAnalysisDataOpt.CallStack)
                {
                    _interproceduralCallStack.Push(operation);
                }

                _interproceduralMethodToCfgMapOpt = null;
            }
            else
            {
                _interproceduralMethodToCfgMapOpt = new Dictionary<IMethodSymbol, ControlFlowGraph>();
            }

            AnalysisEntity interproceduralInvocationInstanceOpt;
            if (analysisContext.InterproceduralAnalysisDataOpt?.InvocationInstanceOpt.HasValue == true)
            {
                (interproceduralInvocationInstanceOpt, ThisOrMePointsToAbstractValue) = analysisContext.InterproceduralAnalysisDataOpt.InvocationInstanceOpt.Value;
            }
            else
            {
                ThisOrMePointsToAbstractValue = GetThisOrMeInstancePointsToValue(analysisContext);
                interproceduralInvocationInstanceOpt = null;
            }

            AnalysisEntityFactory = new AnalysisEntityFactory(
                DataFlowAnalysisContext.ControlFlowGraph,
                DataFlowAnalysisContext.WellKnownTypeProvider,
                getPointsToAbstractValueOpt: (analysisContext.PointsToAnalysisResultOpt != null || IsPointsToAnalysis) ?
                    GetPointsToAbstractValue :
                    (Func<IOperation, PointsToAbstractValue>)null,
                getIsInsideAnonymousObjectInitializer: () => IsInsideAnonymousObjectInitializer,
                getIsLValueFlowCapture: IsLValueFlowCapture,
                containingTypeSymbol: analysisContext.OwningSymbol.ContainingType,
                interproceduralInvocationInstanceOpt: interproceduralInvocationInstanceOpt,
                interproceduralThisOrMeInstanceForCallerOpt: analysisContext.InterproceduralAnalysisDataOpt?.ThisOrMeInstanceForCallerOpt?.Instance,
                interproceduralCallStackOpt: analysisContext.InterproceduralAnalysisDataOpt?.CallStack,
                interproceduralCapturedVariablesMapOpt: analysisContext.InterproceduralAnalysisDataOpt?.CapturedVariablesMap,
                interproceduralGetAnalysisEntityForFlowCaptureOpt: analysisContext.InterproceduralAnalysisDataOpt?.GetAnalysisEntityForFlowCapture,
                getInterproceduralCallStackForOwningSymbol: GetInterproceduralCallStackForOwningSymbol);
        }

        protected CopyAbstractValue GetDefaultCopyValue(AnalysisEntity analysisEntity)
                => _addressSharedEntitiesProvider.GetDefaultCopyValue(analysisEntity);

        protected CopyAbstractValue TryGetAddressSharedCopyValue(AnalysisEntity analysisEntity)
            => _addressSharedEntitiesProvider.TryGetAddressSharedCopyValue(analysisEntity);

        public virtual (TAbstractAnalysisValue Value, PredicateValueKind PredicateValueKind)? GetReturnValueAndPredicateKind()
        {
            if (_returnValueOperationsOpt == null ||
                _returnValueOperationsOpt.Count == 0)
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
            foreach (var operation in _returnValueOperationsOpt)
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
                var thisOrMeLocation = AbstractLocation.CreateThisOrMeLocation(owningSymbol.ContainingType, analysisContext.InterproceduralAnalysisDataOpt?.CallStack);
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
                        MergeAnalysisDataFromUnhandledThrowOperations(caughtExceptionTypeOpt: null);
                    }
                    break;
            }

            return CurrentAnalysisData;
        }

        public TAnalysisData OnEndBlockAnalysis(BasicBlock block, TAnalysisData analysisData)
        {
            CurrentBasicBlock = block;
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
                        if (exceptionInfo.ContainingFinallyRegionOpt == null ||
                            !finallyRegion.ContainsRegionOrSelf(exceptionInfo.ContainingFinallyRegionOpt))
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

            CurrentBasicBlock = null;
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
        {
            var builder = ArrayBuilder<TKey>.GetInstance(targetAnalysisData.Count);
            try
            {
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
            finally
            {
                builder.Free();
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
                method.Parameters.Length > 0)
            {
                var builder = ImmutableDictionary.CreateBuilder<IParameterSymbol, AnalysisEntity>();
                var argumentValuesMap = DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.ArgumentValuesMap ??
                    ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>>.Empty;

                foreach (var parameter in method.Parameters)
                {
                    var result = AnalysisEntityFactory.TryCreateForSymbolDeclaration(parameter, out AnalysisEntity analysisEntity);
                    Debug.Assert(result);
                    builder.Add(parameter, analysisEntity);

                    ArgumentInfo<TAbstractAnalysisValue> assignedValueOpt = null;
                    if (argumentValuesMap.TryGetValue(parameter.OriginalDefinition, out var argumentInfo))
                    {
                        assignedValueOpt = argumentInfo;
                    }

                    _addressSharedEntitiesProvider.UpdateAddressSharedEntitiesForParameter(parameter, analysisEntity, assignedValueOpt);
                    SetValueForParameterOnEntry(parameter, analysisEntity, assignedValueOpt);
                }

                _lazyParameterEntities = builder.ToImmutable();
            }
        }

        private void OnStartExitBlockAnalysis(BasicBlock exitBlock)
        {
            Debug.Assert(exitBlock.Kind == BasicBlockKind.Exit);

            if (_lazyParameterEntities != null)
            {
                foreach (var kvp in _lazyParameterEntities)
                {
                    IParameterSymbol parameter = kvp.Key;
                    AnalysisEntity analysisEntity = kvp.Value;

                    // Escape parameter values on exit, except for ref/out parameters in interprocedural analysis.
                    if (parameter.RefKind == RefKind.None || DataFlowAnalysisContext.InterproceduralAnalysisDataOpt == null)
                    {
                        EscapeValueForParameterOnExit(parameter, analysisEntity);
                    }
                }
            }
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
            if (_lazyParameterEntities != null && DataFlowAnalysisContext.InterproceduralAnalysisDataOpt != null)
            {
                // Reset address shared entities to caller's address shared entities.
                _addressSharedEntitiesProvider.SetAddressSharedEntities(DataFlowAnalysisContext.InterproceduralAnalysisDataOpt.AddressSharedEntities);
                StopTrackingDataForParameters(_lazyParameterEntities);
            }
        }

        protected bool IsParameterEntityForCurrentMethod(AnalysisEntity analysisEntity)
            => analysisEntity.SymbolOpt is IParameterSymbol parameter &&
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
            Debug.Assert(fromBlock != null);
            Debug.Assert(input != null);

            var isFeasibleBranch = true;
            CurrentBasicBlock = fromBlock;
            CurrentAnalysisData = input;

            if (branch.BranchValueOpt != null)
            {
                FlowBranchConditionKind = branch.ControlFlowConditionKind;
                Visit(branch.BranchValueOpt, null);

                if (branch.ControlFlowConditionKind != ControlFlowConditionKind.None)
                {
                    // We visit the condition twice - once for the condition true branch, and once for the condition false branch.
                    // Below check ensures we execute AfterVisitRoot only once.
                    if (!_visitedFlowBranchConditions.Add(branch.BranchValueOpt))
                    {
                        AfterVisitRoot(branch.BranchValueOpt);
                        _visitedFlowBranchConditions.Remove(branch.BranchValueOpt);
                    }

                    if (isConditionalBranchNeverTaken())
                    {
                        isFeasibleBranch = false;
                    }
                }
                else
                {
                    AfterVisitRoot(branch.BranchValueOpt);
                }

                FlowBranchConditionKind = ControlFlowConditionKind.None;
            }

            // Special handling for return and throw branches.
            switch (branch.Kind)
            {
                case ControlFlowBranchSemantics.Return:
                    ProcessReturnValue(branch.BranchValueOpt);
                    break;

                case ControlFlowBranchSemantics.Throw:
                case ControlFlowBranchSemantics.Rethrow:
                    // Update the tracked merged analysis data at throw branches.
                    var thrownExceptionType = branch.BranchValueOpt?.Type ?? CurrentBasicBlock.GetEnclosingRegionExceptionType();
                    if (thrownExceptionType is INamedTypeSymbol exceptionType &&
                        exceptionType.DerivesFrom(WellKnownTypeProvider.Exception, baseTypesOnly: true))
                    {
                        AnalysisDataForUnhandledThrowOperations ??= new Dictionary<ThrownExceptionInfo, TAnalysisData>();
                        var info = ThrownExceptionInfo.Create(CurrentBasicBlock, exceptionType, DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.CallStack);
                        AnalysisDataForUnhandledThrowOperations[info] = GetClonedCurrentAnalysisData();
                    }

                    ProcessThrowValue(branch.BranchValueOpt);
                    break;
            }

            return (CurrentAnalysisData, isFeasibleBranch);

            bool isConditionalBranchNeverTaken()
            {
                Debug.Assert(branch.BranchValueOpt != null);
                Debug.Assert(branch.ControlFlowConditionKind != ControlFlowConditionKind.None);

                if (branch.BranchValueOpt.Type?.SpecialType == SpecialType.System_Boolean &&
                    branch.BranchValueOpt.ConstantValue.HasValue)
                {
                    var alwaysTrue = (bool)branch.BranchValueOpt.ConstantValue.Value;
                    if (alwaysTrue && branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenFalse ||
                        !alwaysTrue && branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenTrue)
                    {
                        return true;
                    }
                }

                if (PredicateAnalysis &&
                    _predicateValueKindCacheBuilder.TryGetValue(branch.BranchValueOpt, out PredicateValueKind valueKind) &&
                    isPredicateAlwaysFalseForBranch(valueKind))
                {
                    return true;
                }

                if (DataFlowAnalysisContext.PointsToAnalysisResultOpt != null &&
                    isPredicateAlwaysFalseForBranch(DataFlowAnalysisContext.PointsToAnalysisResultOpt.GetPredicateKind(branch.BranchValueOpt)))
                {
                    return true;
                }

                if (DataFlowAnalysisContext.CopyAnalysisResultOpt != null &&
                    isPredicateAlwaysFalseForBranch(DataFlowAnalysisContext.CopyAnalysisResultOpt.GetPredicateKind(branch.BranchValueOpt)))
                {
                    return true;
                }

                if (DataFlowAnalysisContext.ValueContentAnalysisResultOpt != null &&
                    isPredicateAlwaysFalseForBranch(DataFlowAnalysisContext.ValueContentAnalysisResultOpt.GetPredicateKind(branch.BranchValueOpt)))
                {
                    return true;
                }

                return false;
            }

            bool isPredicateAlwaysFalseForBranch(PredicateValueKind predicateValueKind)
            {
                Debug.Assert(branch.ControlFlowConditionKind != ControlFlowConditionKind.None);

                switch (predicateValueKind)
                {
                    case PredicateValueKind.AlwaysFalse:
                        return branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenTrue;

                    case PredicateValueKind.AlwaysTrue:
                        return branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenFalse;
                }

                return false;
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

        protected virtual void ProcessReturnValue(IOperation returnValueOperation)
        {
            if (returnValueOperation != null)
            {
                _returnValueOperationsOpt?.Add(returnValueOperation);

                _ = GetAbstractValueForReturnOperation(returnValueOperation, out var implicitTaskPointsToValueOpt);
                if (implicitTaskPointsToValueOpt != null)
                {
                    Debug.Assert(implicitTaskPointsToValueOpt.Kind == PointsToAbstractValueKind.KnownLocations);
                    SetTaskWrappedValue(implicitTaskPointsToValueOpt, GetCachedAbstractValue(returnValueOperation));
                }
            }
        }

        private TAbstractAnalysisValue GetAbstractValueForReturnOperation(IOperation returnValueOperation, out PointsToAbstractValue implicitTaskPointsToValueOpt)
        {
            Debug.Assert(returnValueOperation != null);

            implicitTaskPointsToValueOpt = null;
            var returnValue = GetCachedAbstractValue(returnValueOperation);

            // Check if returned value is wrapped in an implicitly created completed task in an async method.
            // For example, "return 0;" in an async method returning "Task<int>".
            // If so, we return the abstract value for the task wrapping the underlying return value.
            if (OwningSymbol is IMethodSymbol method &&
                method.IsAsync &&
                method.ReturnType.OriginalDefinition.Equals(WellKnownTypeProvider.GenericTask) &&
                !method.ReturnType.Equals(returnValueOperation.Type))
            {
                var location = AbstractLocation.CreateAllocationLocation(returnValueOperation, method.ReturnType, DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.CallStack);
                implicitTaskPointsToValueOpt = PointsToAbstractValue.Create(location, mayBeNull: false);
                return GetAbstractValueForImplicitWrappingTaskCreation(returnValueOperation, returnValue, implicitTaskPointsToValueOpt);
            }

            return returnValue;
        }

        protected virtual void HandlePossibleThrowingOperation(IOperation operation)
        {
            Debug.Assert(DataFlowAnalysisContext.ExceptionPathsAnalysis);
            Debug.Assert(ExecutingExceptionPathsAnalysisPostPass);

            // Bail out if we are not analyzing an interprocedural call and there is no
            // tracked analysis data.
            if (!HasAnyAbstractValue(CurrentAnalysisData) &&
                DataFlowAnalysisContext.InterproceduralAnalysisDataOpt == null)
            {
                return;
            }

            // Bail out if System.Exception is not defined.
            if (WellKnownTypeProvider.Exception == null)
            {
                return;
            }

            IOperation instanceOpt = null;
            IOperation invocationOpt = null;
            switch (operation)
            {
                case IMemberReferenceOperation memberReference:
                    instanceOpt = memberReference.Instance;
                    break;

                case IDynamicMemberReferenceOperation dynamicMemberReference:
                    instanceOpt = dynamicMemberReference.Instance;
                    break;

                case IArrayElementReferenceOperation arrayElementReference:
                    instanceOpt = arrayElementReference.ArrayReference;
                    break;

                case IInvocationOperation invocation:
                    instanceOpt = invocation.Instance;
                    invocationOpt = operation;
                    break;

                case IObjectCreationOperation objectCreation:
                    if (objectCreation.Constructor.IsImplicitlyDeclared)
                    {
                        // Implicitly generated constructor should not throw.
                        return;
                    }

                    invocationOpt = operation;
                    break;

                default:
                    // Optimististically assume the operation cannot throw.
                    return;
            }

            var invocationInstanceAccessCanThrow = instanceOpt != null &&
                instanceOpt.Kind != OperationKind.InstanceReference &&
                GetNullAbstractValue(instanceOpt) != NullAbstractValue.NotNull;
            var invocationCanThrow = invocationOpt != null && !TryGetInterproceduralAnalysisResult(operation, out _);
            if (!invocationInstanceAccessCanThrow && !invocationCanThrow)
            {
                // Cannot throw an exception from instance access and
                // interprocedural analysis already handles possible exception from invoked code.
                return;
            }

            // This operation can throw, so update the analysis data for unhandled exception with 'System.Exception' type.
            AnalysisDataForUnhandledThrowOperations ??= new Dictionary<ThrownExceptionInfo, TAnalysisData>();
            if (!AnalysisDataForUnhandledThrowOperations.TryGetValue(DefaultThrownExceptionInfo, out var data) ||
                CurrentBasicBlock.IsContainedInRegionOfKind(ControlFlowRegionKind.Finally))
            {
                data = null;
            }

            data = GetMergedAnalysisDataForPossibleThrowingOperation(data, operation);
            Debug.Assert(data != null);
            AssertValidAnalysisData(data);
            AnalysisDataForUnhandledThrowOperations[DefaultThrownExceptionInfo] = data;
        }

        protected virtual TAnalysisData GetMergedAnalysisDataForPossibleThrowingOperation(TAnalysisData existingDataOpt, IOperation operation)
        {
            Debug.Assert(DataFlowAnalysisContext.ExceptionPathsAnalysis);
            Debug.Assert(ExecutingExceptionPathsAnalysisPostPass);

            return existingDataOpt == null ?
                GetClonedCurrentAnalysisData() :
                MergeAnalysisData(CurrentAnalysisData, existingDataOpt);
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

            CurrentBasicBlock = null;
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
        {
            Debug.Assert(PredicateAnalysis);

            if (WellKnownTypeProvider.Contract != null &&
                operation.Parent is IInvocationOperation invocation &&
                Equals(invocation.TargetMethod.ContainingType, WellKnownTypeProvider.Contract) &&
                invocation.TargetMethod.IsStatic &&
                invocation.Arguments[0] == operation)
            {
                if (_lazyContractCheckMethodsForPredicateAnalysis == null)
                {
                    // Contract.Requires check.
                    var requiresMethods = WellKnownTypeProvider.Contract.GetMembers("Requires");
                    var assumeMethods = WellKnownTypeProvider.Contract.GetMembers("Assume");
                    var assertMethods = WellKnownTypeProvider.Contract.GetMembers("Assert");
                    var validationMethods = requiresMethods.Concat(assumeMethods).Concat(assertMethods).OfType<IMethodSymbol>().Where(m => m.IsStatic && m.ReturnsVoid && m.Parameters.Length >= 1 && (m.Parameters[0].Type.SpecialType == SpecialType.System_Boolean));
                    _lazyContractCheckMethodsForPredicateAnalysis = ImmutableHashSet.CreateRange(validationMethods);
                }

                return _lazyContractCheckMethodsForPredicateAnalysis.Contains(invocation.TargetMethod);
            }

            return false;
        }

        #region Helper methods to get or cache analysis data for visited operations.

        internal ImmutableDictionary<IOperation, TAbstractAnalysisValue> GetStateMap() => _valueCacheBuilder.ToImmutable();

        internal ImmutableDictionary<IOperation, PredicateValueKind> GetPredicateValueKindMap() => _predicateValueKindCacheBuilder.ToImmutable();

        public virtual TAnalysisData GetMergedDataForUnhandledThrowOperations()
        {
            if (AnalysisDataForUnhandledThrowOperations == null)
            {
                return default;
            }

            TAnalysisData mergedData = default;
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

            if (DataFlowAnalysisContext.InterproceduralAnalysisDataOpt != null)
            {
                return DataFlowAnalysisContext.InterproceduralAnalysisDataOpt.GetCachedAbstractValueFromCaller(operation);
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
            if (DataFlowAnalysisContext.CopyAnalysisResultOpt == null)
            {
                return CopyAbstractValue.Unknown;
            }
            else
            {
                return DataFlowAnalysisContext.CopyAnalysisResultOpt[operation];
            }
        }

        protected virtual PointsToAbstractValue GetPointsToAbstractValue(IOperation operation)
        {
            if (DataFlowAnalysisContext.PointsToAnalysisResultOpt == null)
            {
                return PointsToAbstractValue.Unknown;
            }
            else
            {
                return DataFlowAnalysisContext.PointsToAnalysisResultOpt[operation];
            }
        }

        protected virtual ValueContentAbstractValue GetValueContentAbstractValue(IOperation operation)
        {
            if (DataFlowAnalysisContext.ValueContentAnalysisResultOpt == null)
            {
                return ValueContentAbstractValue.MayBeContainsNonLiteralState;
            }
            else
            {
                return DataFlowAnalysisContext.ValueContentAnalysisResultOpt[operation];
            }
        }

        protected ImmutableHashSet<AbstractLocation> GetEscapedLocations(IOperation operation)
        {
            if (operation == null || DataFlowAnalysisContext.PointsToAnalysisResultOpt == null)
            {
                return ImmutableHashSet<AbstractLocation>.Empty;
            }
            else
            {
                return DataFlowAnalysisContext.PointsToAnalysisResultOpt.GetEscapedAbstractLocations(operation);
            }
        }

        protected ImmutableHashSet<AbstractLocation> GetEscapedLocations(AnalysisEntity parameterEntity)
        {
            Debug.Assert(parameterEntity.SymbolOpt?.Kind == SymbolKind.Parameter);
            if (parameterEntity == null || DataFlowAnalysisContext.PointsToAnalysisResultOpt == null)
            {
                return ImmutableHashSet<AbstractLocation>.Empty;
            }
            else
            {
                return DataFlowAnalysisContext.PointsToAnalysisResultOpt.GetEscapedAbstractLocations(parameterEntity);
            }
        }

        protected bool TryGetPointsToAbstractValueAtEntryBlockEnd(AnalysisEntity analysisEntity, out PointsToAbstractValue pointsToAbstractValue)
        {
            Debug.Assert(CurrentBasicBlock != null);
            Debug.Assert(CurrentBasicBlock.Kind == BasicBlockKind.Entry);
            Debug.Assert(DataFlowAnalysisContext.PointsToAnalysisResultOpt != null);

            var outputData = DataFlowAnalysisContext.PointsToAnalysisResultOpt.EntryBlockOutput.Data;
            return outputData.TryGetValue(analysisEntity, out pointsToAbstractValue);
        }

        protected bool TryGetNullAbstractValueAtCurrentBlockEntry(AnalysisEntity analysisEntity, out NullAbstractValue nullAbstractValue)
        {
            Debug.Assert(CurrentBasicBlock != null);
            Debug.Assert(DataFlowAnalysisContext.PointsToAnalysisResultOpt != null);
            var inputData = DataFlowAnalysisContext.PointsToAnalysisResultOpt[CurrentBasicBlock].Data;
            if (inputData.TryGetValue(analysisEntity, out PointsToAbstractValue pointsToAbstractValue))
            {
                nullAbstractValue = pointsToAbstractValue.NullState;
                return true;
            }

            nullAbstractValue = NullAbstractValue.MaybeNull;
            return false;
        }

        protected bool TryGetMergedNullAbstractValueAtUnhandledThrowOperationsInGraph(AnalysisEntity analysisEntity, out NullAbstractValue nullAbstractValue)
        {
            Debug.Assert(CurrentBasicBlock != null);
            Debug.Assert(DataFlowAnalysisContext.PointsToAnalysisResultOpt != null);
            var inputData = DataFlowAnalysisContext.PointsToAnalysisResultOpt.MergedStateForUnhandledThrowOperationsOpt?.Data;
            if (inputData == null || !inputData.TryGetValue(analysisEntity, out PointsToAbstractValue pointsToAbstractValue))
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

            TaskWrappedValuesMapOpt ??= new Dictionary<PointsToAbstractValue, TAbstractAnalysisValue>();
            TaskWrappedValuesMapOpt[pointsToValueForTask] = wrappedValue;
        }

        private protected bool TryGetTaskWrappedValue(PointsToAbstractValue pointsToAbstractValue, out TAbstractAnalysisValue wrappedValue)
        {
            if (TaskWrappedValuesMapOpt == null)
            {
                wrappedValue = default;
                return false;
            }

            return TaskWrappedValuesMapOpt.TryGetValue(pointsToAbstractValue, out wrappedValue);
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
            ITypeSymbol targetType,
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
                if (operation is IIsPatternOperation patternOperation &&
                    patternOperation.Pattern is IConstantPatternOperation constantPattern)
                {
                    if (constantPattern.Value.ConstantValue.HasValue)
                    {
                        if (constantPattern.Value.ConstantValue.Value == null)
                        {
                            switch (pointsToValue.NullState)
                            {
                                case NullAbstractValue.Null:
                                    inference.AlwaysSucceed = true;
                                    break;

                                case NullAbstractValue.NotNull:
                                    inference.AlwaysFail = true;
                                    break;
                            }
                        }

                        return true;
                    }

                    return false;
                }

                if (targetType == null)
                {
                    // Below assert fires for IDeclarationPatternOperation with null DeclaredSymbol, but non-null MatchedType.
                    // https://github.com/dotnet/roslyn-analyzers/issues/2185 tracks enabling this assert.
                    //Debug.Fail($"Unexpected 'null' target type for '{operation.Syntax.ToString()}'");
                    return false;
                }

                // Infer if a cast will always fail.
                if (!inference.IsBoxing &&
                    !inference.IsUnboxing &&
                    !IsInterfaceOrTypeParameter(targetType) &&
                    pointsToValue.Locations.All(location => location.IsNull ||
                        (!location.IsNoLocation &&
                         !IsInterfaceOrTypeParameter(location.LocationTypeOpt) &&
                         !targetType.DerivesFrom(location.LocationTypeOpt) &&
                         !location.LocationTypeOpt.DerivesFrom(targetType))))
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
                        pointsToValue.Locations.All(location => location.IsNoLocation || !location.IsNull && location.LocationTypeOpt.DerivesFrom(targetType)))
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
            static bool IsInterfaceOrTypeParameter(ITypeSymbol type) => type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.TypeParameter;
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
                (DataFlowAnalysisContext.CopyAnalysisResultOpt == null || DataFlowAnalysisContext.CopyAnalysisResultOpt[basicBlock].IsReachable) &&
                (DataFlowAnalysisContext.PointsToAnalysisResultOpt == null || DataFlowAnalysisContext.PointsToAnalysisResultOpt[basicBlock].IsReachable) &&
                (DataFlowAnalysisContext.ValueContentAnalysisResultOpt == null || DataFlowAnalysisContext.ValueContentAnalysisResultOpt[basicBlock].IsReachable);
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
            Debug.Assert(operation.Kind == OperationKind.BinaryOperator ||
                operation.Kind == OperationKind.UnaryOperator ||
                operation.Kind == OperationKind.IsNull ||
                operation.Kind == OperationKind.Invocation ||
                operation.Kind == OperationKind.Argument ||
                operation.Kind == OperationKind.FlowCaptureReference ||
                operation.Kind == OperationKind.IsPattern);

            if (FlowBranchConditionKind == ControlFlowConditionKind.None || !IsRootOfCondition())
            {
                // Operation is a predicate which is not a conditional.
                // For example, "x = operation", where operation is "a == b".
                // Check if we need to perform predicate analysis for the operation and/or set/transfer predicate data.

                // First find out if this operation is being captured.
                AnalysisEntity predicatedFlowCaptureEntityOpt = GetPredicatedFlowCaptureEntity();
                if (predicatedFlowCaptureEntityOpt == null)
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
                        var result = AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity flowCaptureReferenceEntity);
                        Debug.Assert(result);
                        Debug.Assert(flowCaptureReferenceEntity.CaptureIdOpt != null);
                        Debug.Assert(HasPredicatedDataForEntity(flowCaptureReferenceEntity));
                        TransferPredicatedData(fromEntity: flowCaptureReferenceEntity, toEntity: predicatedFlowCaptureEntityOpt);
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
                            StartTrackingPredicatedData(predicatedFlowCaptureEntityOpt, truePredicatedData, falsePredicatedData);
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

            AnalysisEntity GetPredicatedFlowCaptureEntity()
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
                                Debug.Assert(targetEntity.CaptureIdOpt != null);
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
                    //  2. Equality value check for constant pattern, i.e. "x is 1"
                    if (isPatternOperation.Pattern.Kind == OperationKind.DeclarationPattern)
                    {
                        predicateValueKind = SetValueForIsNullComparisonOperator(isPatternOperation.Pattern, equals: FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse, targetAnalysisData: targetAnalysisData);
                    }
                    else if (isPatternOperation.Pattern is IConstantPatternOperation constantPattern)
                    {
                        predicateValueKind = SetValueForEqualsOrNotEqualsComparisonOperator(isPatternOperation.Value, constantPattern.Value,
                            equals: FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue, isReferenceEquality: false, targetAnalysisData: targetAnalysisData);
                    }
                    else
                    {
                        // Below assert fires for IDiscardPatternOperation.
                        // https://github.com/dotnet/roslyn-analyzers/issues/2185 tracks enabling this assert.
                        //Debug.Fail($"Unknown pattern kind '{isPatternOperation.Kind}'");
                        predicateValueKind = PredicateValueKind.Unknown;
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

                case IFlowCaptureReferenceOperation _:
                    var result = AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity flowCaptureReferenceEntity);
                    Debug.Assert(result);
                    Debug.Assert(flowCaptureReferenceEntity.CaptureIdOpt != null);
                    if (!HasPredicatedDataForEntity(targetAnalysisData, flowCaptureReferenceEntity))
                    {
                        return;
                    }

                    predicateValueKind = ApplyPredicatedDataForEntity(targetAnalysisData, flowCaptureReferenceEntity, trueData: FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue);
                    break;

                case IInvocationOperation invocation:
                    // Predicate analysis for different equality comparison methods and argument null check methods.
                    if (invocation.Type.SpecialType != SpecialType.System_Boolean)
                    {
                        return;
                    }

                    if (invocation.TargetMethod.IsArgumentNullCheckMethod())
                    {
                        // Predicate analysis for null checks.
                        if (invocation.Arguments.Length == 1)
                        {
                            predicateValueKind = SetValueForIsNullComparisonOperator(invocation.Arguments[0].Value, equals: FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue, targetAnalysisData: targetAnalysisData);
                        }

                        break;
                    }

                    IOperation leftOperand = null;
                    IOperation rightOperand = null;
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
                if (WellKnownTypeProvider.GenericIEquatable == null)
                {
                    return false;
                }

                foreach (var interfaceType in methodSymbol.ContainingType.AllInterfaces)
                {
                    if (interfaceType.OriginalDefinition.Equals(WellKnownTypeProvider.GenericIEquatable))
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

            var leftTypeOpt = operation.LeftOperand.Type;
            var leftConstantValueOpt = operation.LeftOperand.ConstantValue;
            var rightTypeOpt = operation.RightOperand.Type;
            var rightConstantValueOpt = operation.RightOperand.ConstantValue;
            var isReferenceEquality = operation.OperatorMethod == null &&
                operation.Type.SpecialType == SpecialType.System_Boolean &&
                leftTypeOpt != null &&
                !leftTypeOpt.HasValueCopySemantics() &&
                rightTypeOpt != null &&
                !rightTypeOpt.HasValueCopySemantics() &&
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

        protected virtual void StartTrackingPredicatedData(AnalysisEntity predicatedEntity, TAnalysisData truePredicateData, TAnalysisData falsePredicateData)
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

        protected virtual void ProcessThrowValue(IOperation thrownValueOpt)
        {
        }

        #endregion

        #region Helper methods to handle initialization/assignment operations
        protected abstract void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, TAbstractAnalysisValue value);
        protected abstract void SetAbstractValueForAssignment(IOperation target, IOperation assignedValueOperation, TAbstractAnalysisValue assignedValue, bool mayBeAssignment = false);
        protected abstract void SetAbstractValueForTupleElementAssignment(AnalysisEntity tupleElementEntity, IOperation assignedValueOperation, TAbstractAnalysisValue assignedValue);
        private void HandleFlowCaptureReferenceAssignment(IFlowCaptureReferenceOperation flowCaptureReference, IOperation assignedValueOperation, TAbstractAnalysisValue assignedValue)
        {
            Debug.Assert(flowCaptureReference != null);
            Debug.Assert(IsLValueFlowCapture(flowCaptureReference.Id));

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
                            AnalysisEntity uniqueAnalysisEntity = null;
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
            Debug.Assert(operation.Type.HasValueCopySemantics());

            if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
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
            Debug.Assert(!operation.Type.HasValueCopySemantics());

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
        private void ResetInstanceAnalysisData(IOperation operation)
        {
            if (operation?.Type == null || !HasPointsToAnalysisResult || !PessimisticAnalysis)
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

        public TAnalysisData MergeAnalysisData(TAnalysisData value1, TAnalysisData value2, bool forBackEdge)
            => forBackEdge ? MergeAnalysisDataForBackEdge(value1, value2) : MergeAnalysisData(value1, value2);
        protected abstract TAnalysisData MergeAnalysisData(TAnalysisData value1, TAnalysisData value2);
        protected virtual TAnalysisData MergeAnalysisDataForBackEdge(TAnalysisData value1, TAnalysisData value2)
            => MergeAnalysisData(value1, value2);
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
            Func<TKey, bool> predicateOpt)
        {
            foreach (var (key, value) in coreCurrentAnalysisData)
            {
                if (coreDataAtException.ContainsKey(key) ||
                    predicateOpt != null && !predicateOpt(key))
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
            (AnalysisEntity InstanceOpt, PointsToAbstractValue PointsToValue)? invocationInstanceOpt,
            (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? thisOrMeInstanceForCallerOpt,
            ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>> argumentValuesMap,
            IDictionary<AnalysisEntity, PointsToAbstractValue> pointsToValuesOpt,
            IDictionary<AnalysisEntity, CopyAbstractValue> copyValuesOpt,
            IDictionary<AnalysisEntity, ValueContentAbstractValue> valueContentValuesOpt,
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
            Debug.Assert(interproceduralUnhandledThrowOperationsData != null);

            if (interproceduralUnhandledThrowOperationsData.Count == 0)
            {
                // All interprocedural exceptions were handled.
                return;
            }

            AnalysisDataForUnhandledThrowOperations ??= new Dictionary<ThrownExceptionInfo, TAnalysisData>();
            foreach (var (exceptionInfo, analysisDataAtException) in interproceduralUnhandledThrowOperationsData)
            {
                // Adjust the thrown exception info from the interprocedural context to current context.
                var adjustedExceptionInfo = exceptionInfo.With(CurrentBasicBlock, DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.CallStack);

                // Used cloned analysis data
                var clonedAnalysisDataAtException = GetClonedAnalysisData(analysisDataAtException);

                ApplyInterproceduralAnalysisDataForUnhandledThrowOperation(adjustedExceptionInfo, clonedAnalysisDataAtException);
            }

            // Local functions
            void ApplyInterproceduralAnalysisDataForUnhandledThrowOperation(ThrownExceptionInfo exceptionInfo, TAnalysisData analysisDataAtException)
            {
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

        protected bool TryGetInterproceduralAnalysisResult(IOperation operation, out TAnalysisResult analysisResult)
        {
            if (_interproceduralResultsBuilder.TryGetValue(operation, out var computedAnalysisResult))
            {
                analysisResult = (TAnalysisResult)computedAnalysisResult;
                return true;
            }

            analysisResult = default;
            return false;
        }

        private TAbstractAnalysisValue PerformInterproceduralAnalysis(
            Func<ControlFlowGraph> getCfg,
            IMethodSymbol invokedMethod,
            IOperation instanceReceiver,
            ImmutableArray<IArgumentOperation> arguments,
            IOperation originalOperation,
            TAbstractAnalysisValue defaultValue,
            bool isLambdaOrLocalFunction)
        {
            // Use the original method definition for interprocedural analysis as the ControlFlowGraph can only be created for the original definition.
            invokedMethod = invokedMethod.OriginalDefinition;

            // Bail out if configured not to execute interprocedural analysis.
            var skipInterproceduralAnalysis = !isLambdaOrLocalFunction && InterproceduralAnalysisKind == InterproceduralAnalysisKind.None ||
                DataFlowAnalysisContext.InterproceduralAnalysisPredicateOpt?.SkipInterproceduralAnalysis(invokedMethod, isLambdaOrLocalFunction) == true ||
                invokedMethod.IsConfiguredToSkipAnalysis(DataFlowAnalysisContext.AnalyzerOptions, s_dummyDataflowAnalysisDescriptor, WellKnownTypeProvider.Compilation, CancellationToken.None);

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
            var currentMethodsBeingAnalyzed = DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.MethodsBeingAnalyzed ?? ImmutableHashSet<TAnalysisContext>.Empty;
            var newMethodsBeingAnalyzed = currentMethodsBeingAnalyzed.Add(DataFlowAnalysisContext);
            if (currentMethodsBeingAnalyzed.Count == newMethodsBeingAnalyzed.Count)
            {
                return ResetAnalysisDataAndReturnDefaultValue();
            }

            // Check if we are already at the maximum allowed interprocedural call chain length.
            int currentMethodCallCount = currentMethodsBeingAnalyzed.Where(m => !((IMethodSymbol)m.OwningSymbol).IsLambdaOrLocalFunctionOrDelegate()).Count();
            int currentLambdaOrLocalFunctionCallCount = currentMethodsBeingAnalyzed.Count - currentMethodCallCount;

            if (currentMethodCallCount >= MaxInterproceduralMethodCallChain ||
                currentLambdaOrLocalFunctionCallCount >= MaxInterproceduralLambdaOrLocalFunctionCallChain)
            {
                return ResetAnalysisDataAndReturnDefaultValue();
            }

            // Compute the dependent interprocedural PointsTo and Copy analysis results, if any.
            var pointsToAnalysisResultOpt = (PointsToAnalysisResult)DataFlowAnalysisContext.PointsToAnalysisResultOpt?.TryGetInterproceduralResult(originalOperation);
            var copyAnalysisResultOpt = DataFlowAnalysisContext.CopyAnalysisResultOpt?.TryGetInterproceduralResult(originalOperation);
            var valueContentAnalysisResultOpt = DataFlowAnalysisContext.ValueContentAnalysisResultOpt?.TryGetInterproceduralResult(originalOperation);

            // Compute the CFG for the invoked method.
            var cfg = pointsToAnalysisResultOpt?.ControlFlowGraph ??
                copyAnalysisResultOpt?.ControlFlowGraph ??
                valueContentAnalysisResultOpt?.ControlFlowGraph ??
                getCfg();
            if (cfg == null || !cfg.SupportsFlowAnalysis())
            {
                return ResetAnalysisDataAndReturnDefaultValue();
            }

            var hasParameterWithDelegateType = invokedMethod.HasParameterWithDelegateType();

            // Ensure we are using the same control flow graphs across analyses.
            Debug.Assert(pointsToAnalysisResultOpt?.ControlFlowGraph == null || cfg == pointsToAnalysisResultOpt?.ControlFlowGraph);
            Debug.Assert(copyAnalysisResultOpt?.ControlFlowGraph == null || cfg == copyAnalysisResultOpt?.ControlFlowGraph);
            Debug.Assert(valueContentAnalysisResultOpt?.ControlFlowGraph == null || cfg == valueContentAnalysisResultOpt?.ControlFlowGraph);

            // Append operation to interprocedural call stack.
            _interproceduralCallStack.Push(originalOperation);

            // Compute optional interprocedural analysis data for context-sensitive analysis.
            bool isContextSensitive = isLambdaOrLocalFunction || InterproceduralAnalysisKind == InterproceduralAnalysisKind.ContextSensitive;
            var interproceduralAnalysisData = isContextSensitive ? ComputeInterproceduralAnalysisData() : null;
            TAnalysisResult analysisResult;

            try
            {
                // Create analysis context for interprocedural analysis.
                var interproceduralDataFlowAnalysisContext = DataFlowAnalysisContext.ForkForInterproceduralAnalysis(
                    invokedMethod, cfg, originalOperation, pointsToAnalysisResultOpt, copyAnalysisResultOpt, valueContentAnalysisResultOpt, interproceduralAnalysisData);

                // Check if the client configured skipping analysis for the given interprocedural analysis context.
                if (DataFlowAnalysisContext.InterproceduralAnalysisPredicateOpt?.SkipInterproceduralAnalysis(interproceduralDataFlowAnalysisContext) == true)
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

                    // Save the interprocedural result for the invocation/creation operation.
                    // Note that we Update instead of invoking .Add as we may execute the analysis multiple times for fixed point computation.
                    _interproceduralResultsBuilder[originalOperation] = analysisResult;
                }

                // Update the current analysis data based on interprocedural analysis result.
                if (isContextSensitive)
                {
                    // Apply any interprocedural analysis data for unhandled exceptions paths.
                    if (analysisResult.AnalysisDataForUnhandledThrowOperationsOpt is Dictionary<ThrownExceptionInfo, TAnalysisData> interproceduralUnhandledThrowOperationsDataOpt)
                    {
                        ApplyInterproceduralAnalysisDataForUnhandledThrowOperations(interproceduralUnhandledThrowOperationsDataOpt);
                    }

                    if (analysisResult.TaskWrappedValuesMapOpt is Dictionary<PointsToAbstractValue, TAbstractAnalysisValue> taskWrappedValuesMap)
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
                    ResetAnalysisData();
                }
            }
            finally
            {
                // Remove the operation from interprocedural call stack.
                var popped = _interproceduralCallStack.Pop();
                Debug.Assert(popped == originalOperation);

                interproceduralAnalysisData?.InitialAnalysisData?.Dispose();
            }

            Debug.Assert(invokedMethod.ReturnsVoid == !analysisResult.ReturnValueAndPredicateKindOpt.HasValue);
            if (invokedMethod.ReturnsVoid)
            {
                return defaultValue;
            }

            if (PredicateAnalysis)
            {
                SetPredicateValueKind(originalOperation, CurrentAnalysisData, analysisResult.ReturnValueAndPredicateKindOpt.Value.PredicateValueKind);
            }

            return analysisResult.ReturnValueAndPredicateKindOpt.Value.Value;

            // Local functions
            TAbstractAnalysisValue ResetAnalysisDataAndReturnDefaultValue()
            {
                ResetAnalysisData();
                return defaultValue;
            }

            void ResetAnalysisData()
            {
                // Interprocedural analysis did not succeed, so we need to conservatively reset relevant analysis data.
                if (!PessimisticAnalysis)
                {
                    // We are performing an optimistic analysis, so we should not reset any data.
                    return;
                }

                if (isLambdaOrLocalFunction)
                {
                    // For local/lambda cases, we reset all analysis data.
                    ResetCurrentAnalysisData();
                }
                else
                {
                    // For regular invocation cases, we reset instance analysis data and argument data.
                    // Note that arguments are reset later by processing '_pendingArgumentsToReset'.
                    ResetInstanceAnalysisData(instanceReceiver);
                    Debug.Assert(arguments.All(arg => _pendingArgumentsToReset.Contains(arg)));
                }
            }

            InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue> ComputeInterproceduralAnalysisData()
            {
                var invocationInstance = GetInvocationInstance();
                var thisOrMeInstance = GetThisOrMeInstance();
                var argumentValuesMap = GetArgumentValues(ref invocationInstance);
                var pointsToValuesOpt = pointsToAnalysisResultOpt?[cfg.GetEntry()].Data;
                var copyValuesOpt = copyAnalysisResultOpt?[cfg.GetEntry()].Data;
                var valueContentValuesOpt = valueContentAnalysisResultOpt?[cfg.GetEntry()].Data;
                var initialAnalysisData = GetInitialInterproceduralAnalysisData(invokedMethod, invocationInstance,
                    thisOrMeInstance, argumentValuesMap, pointsToValuesOpt, copyValuesOpt, valueContentValuesOpt, isLambdaOrLocalFunction, hasParameterWithDelegateType);

                return new InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>(
                    initialAnalysisData,
                    invocationInstance,
                    thisOrMeInstance,
                    argumentValuesMap,
                    GetCapturedVariablesMap(),
                    _addressSharedEntitiesProvider.GetAddressedSharedEntityMap(),
                    ImmutableStack.CreateRange(_interproceduralCallStack),
                    newMethodsBeingAnalyzed,
                    getCachedAbstractValueFromCaller: GetCachedAbstractValue,
                    getInterproceduralControlFlowGraph: GetInterproceduralControlFlowGraph,
                    getAnalysisEntityForFlowCapture: GetAnalysisEntityForFlowCapture,
                    getInterproceduralCallStackForOwningSymbol: GetInterproceduralCallStackForOwningSymbol);

                // Local functions.
                (AnalysisEntity, PointsToAbstractValue)? GetInvocationInstance()
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

                ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>> GetArgumentValues(ref (AnalysisEntity entity, PointsToAbstractValue pointsToValue)? invocationInstanceOpt)
                {
                    var builder = PooledDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>>.GetInstance();
                    var isExtensionMethodInvocationWithOneLessArgument = invokedMethod.IsExtensionMethod && arguments.Length == invokedMethod.Parameters.Length - 1;

                    if (isExtensionMethodInvocationWithOneLessArgument)
                    {
                        var extraArgument = new ArgumentInfo<TAbstractAnalysisValue>(
                            operation: instanceReceiver ?? originalOperation,
                            analysisEntityOpt: invocationInstanceOpt?.entity,
                            instanceLocation: invocationInstanceOpt?.pointsToValue ?? PointsToAbstractValue.Unknown,
                            value: instanceReceiver != null ? GetCachedAbstractValue(instanceReceiver) : ValueDomain.UnknownOrMayBeValue);
                        builder.Add(invokedMethod.Parameters[0], extraArgument);
                        invocationInstanceOpt = null;
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

                        builder.Add(GetMappedParameterForArgument(argument), new ArgumentInfo<TAbstractAnalysisValue>(argument, argumentEntity, instanceLocation, argumentValue));
                        _pendingArgumentsToReset.Remove(argument);
                    }

                    return builder.ToImmutableDictionaryAndFree();

                    // Local function
                    IParameterSymbol GetMappedParameterForArgument(IArgumentOperation argumentOperation)
                    {
                        if (argumentOperation.Parameter.ContainingSymbol is IMethodSymbol method &&
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

                ImmutableDictionary<ISymbol, PointsToAbstractValue> GetCapturedVariablesMap()
                {
                    if (!isLambdaOrLocalFunction)
                    {
                        return ImmutableDictionary<ISymbol, PointsToAbstractValue>.Empty;
                    }

                    var capturedVariables = cfg.OriginalOperation.GetCaptures(invokedMethod);
                    try
                    {
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

                                builder.Add(capturedVariable, capturedEntity.InstanceLocation);
                            }

                            return builder.ToImmutable();
                        }
                    }
                    finally
                    {
                        capturedVariables.Free();
                    }
                }

                AnalysisEntity GetAnalysisEntityForFlowCapture(IOperation operation)
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

        #endregion

        #region Visitor methods

        protected TAbstractAnalysisValue VisitArray(IEnumerable<IOperation> operations, object argument)
        {
            foreach (var operation in operations)
            {
                _ = Visit(operation, argument);
            }

            return ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue Visit(IOperation operation, object argument)
        {
            if (operation != null)
            {
                var value = VisitCore(operation, argument);
                CacheAbstractValue(operation, value);

                if (ExecutingExceptionPathsAnalysisPostPass)
                {
                    HandlePossibleThrowingOperation(operation);
                }

                if (_pendingArgumentsToPostProcess.Any(arg => arg.Parent == operation))
                {
                    var pendingArguments = _pendingArgumentsToPostProcess.Where(arg => arg.Parent == operation).ToImmutableArray();
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
                        _pendingArgumentsToPostProcess.Remove(argumentOperation);
                    }
                }

                return value;
            }

            return ValueDomain.UnknownOrMayBeValue;
        }

        private TAbstractAnalysisValue VisitCore(IOperation operation, object argument)
        {
            if (operation.Kind == OperationKind.None)
            {
                return DefaultVisit(operation, argument);
            }

            _recursionDepth++;
            try
            {
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                return operation.Accept(this, argument);
            }
            finally
            {
                _recursionDepth--;
            }
        }

        public override TAbstractAnalysisValue DefaultVisit(IOperation operation, object argument)
        {
            return VisitArray(operation.Children, argument);
        }

        public override TAbstractAnalysisValue VisitSimpleAssignment(ISimpleAssignmentOperation operation, object argument)
        {
            return VisitAssignmentOperation(operation, argument);
        }

        public override TAbstractAnalysisValue VisitCompoundAssignment(ICompoundAssignmentOperation operation, object argument)
        {
            TAbstractAnalysisValue targetValue = Visit(operation.Target, argument);
            TAbstractAnalysisValue assignedValue = Visit(operation.Value, argument);
            var value = ComputeValueForCompoundAssignment(operation, targetValue, assignedValue, operation.Target.Type, operation.Value.Type);
            SetAbstractValueForAssignment(operation.Target, operation.Value, value);
            return value;
        }

        public virtual TAbstractAnalysisValue ComputeValueForCompoundAssignment(
            ICompoundAssignmentOperation operation,
            TAbstractAnalysisValue targetValue,
            TAbstractAnalysisValue assignedValue,
            ITypeSymbol targetType,
            ITypeSymbol assignedValueType)
        {
            return ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, object argument)
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

        public override TAbstractAnalysisValue VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, object argument)
        {
            return VisitAssignmentOperation(operation, argument);
        }

        protected virtual TAbstractAnalysisValue VisitAssignmentOperation(IAssignmentOperation operation, object argument)
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

        public override TAbstractAnalysisValue VisitArrayInitializer(IArrayInitializerOperation operation, object argument)
        {
            var arrayCreation = operation.GetAncestor<IArrayCreationOperation>(OperationKind.ArrayCreation);
            if (arrayCreation != null)
            {
                var elementType = ((IArrayTypeSymbol)arrayCreation.Type).ElementType;
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

        public override TAbstractAnalysisValue VisitLocalReference(ILocalReferenceOperation operation, object argument)
        {
            var value = base.VisitLocalReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitParameterReference(IParameterReferenceOperation operation, object argument)
        {
            var value = base.VisitParameterReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitArrayElementReference(IArrayElementReferenceOperation operation, object argument)
        {
            var value = base.VisitArrayElementReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, object argument)
        {
            var value = base.VisitDynamicMemberReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitEventReference(IEventReferenceOperation operation, object argument)
        {
            var value = base.VisitEventReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitFieldReference(IFieldReferenceOperation operation, object argument)
        {
            var value = base.VisitFieldReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitMethodReference(IMethodReferenceOperation operation, object argument)
        {
            var value = base.VisitMethodReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitPropertyReference(IPropertyReferenceOperation operation, object argument)
        {
            var value = base.VisitPropertyReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public override TAbstractAnalysisValue VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, object argument)
        {
            var value = base.VisitFlowCaptureReference(operation, argument);
            if (!IsLValueFlowCapture(operation.Id))
            {
                PerformFlowCaptureReferencePredicateAnalysis();
                return ComputeAnalysisValueForReferenceOperation(operation, value);
            }

            return ValueDomain.UnknownOrMayBeValue;

            void PerformFlowCaptureReferencePredicateAnalysis()
            {
                if (!PredicateAnalysis)
                {
                    return;
                }

                var result = AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity flowCaptureReferenceEntity);
                Debug.Assert(result);
                Debug.Assert(flowCaptureReferenceEntity.CaptureIdOpt != null);
                if (!HasPredicatedDataForEntity(flowCaptureReferenceEntity))
                {
                    return;
                }

                PerformPredicateAnalysis(operation);
                Debug.Assert(HasPredicatedDataForEntity(flowCaptureReferenceEntity));
            }
        }

        public override TAbstractAnalysisValue VisitFlowCapture(IFlowCaptureOperation operation, object argument)
        {
            var value = Visit(operation.Value, argument);
            if (!IsLValueFlowCapture(operation.Id))
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
                    Debug.Assert(flowCaptureEntity.CaptureIdOpt != null);
                    TAnalysisData predicatedData = GetEmptyAnalysisData();
                    TAnalysisData truePredicatedData, falsePredicatedData;
                    if (constantValue)
                    {
                        truePredicatedData = predicatedData;
                        falsePredicatedData = default;
                    }
                    else
                    {
                        falsePredicatedData = predicatedData;
                        truePredicatedData = default;
                    }

                    StartTrackingPredicatedData(flowCaptureEntity, truePredicatedData, falsePredicatedData);
                }
            }
        }

        public override TAbstractAnalysisValue VisitDefaultValue(IDefaultValueOperation operation, object argument)
        {
            return GetAbstractDefaultValue(operation.Type);
        }

        public override TAbstractAnalysisValue VisitInterpolation(IInterpolationOperation operation, object argument)
        {
            var expressionValue = Visit(operation.Expression, argument);
            _ = Visit(operation.FormatString, argument);
            _ = Visit(operation.Alignment, argument);
            return expressionValue;
        }

        public override TAbstractAnalysisValue VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, object argument)
        {
            return Visit(operation.Text, argument);
        }

        public sealed override TAbstractAnalysisValue VisitArgument(IArgumentOperation operation, object argument)
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
        /// and also handles resetting the argument value for ref/out parmater.
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
            switch (operation.Parameter.RefKind)
            {
                case RefKind.Ref:
                case RefKind.Out:
                    var value = ComputeAnalysisValueForEscapedRefOrOutArgument(operation, defaultValue: ValueDomain.UnknownOrMayBeValue);
                    if (operation.Parameter.RefKind != RefKind.Out)
                    {
                        value = ValueDomain.Merge(value, GetCachedAbstractValue(operation.Value));
                    }

                    CacheAbstractValue(operation, value);
                    SetAbstractValueForAssignment(operation.Value, operation, value);
                    break;
            }
        }

        public override TAbstractAnalysisValue VisitConstantPattern(IConstantPatternOperation operation, object argument)
        {
            return Visit(operation.Value, argument);
        }

        public override TAbstractAnalysisValue VisitParenthesized(IParenthesizedOperation operation, object argument)
        {
            return Visit(operation.Operand, argument);
        }

        public override TAbstractAnalysisValue VisitTranslatedQuery(ITranslatedQueryOperation operation, object argument)
        {
            return Visit(operation.Operation, argument);
        }

        public override TAbstractAnalysisValue VisitConversion(IConversionOperation operation, object argument)
        {
            var operandValue = Visit(operation.Operand, argument);

            // Conservative for error code and user defined operator.
            return operation.Conversion.Exists && !operation.Conversion.IsUserDefined ? operandValue : ValueDomain.UnknownOrMayBeValue;
        }

        public override TAbstractAnalysisValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
        {
            Debug.Assert(operation.Initializer == null, "Object or collection initializer must have been lowered in the CFG");

            var defaultValue = base.VisitObjectCreation(operation, argument);

            var method = operation.Constructor;
            ControlFlowGraph getCfg() => GetInterproceduralControlFlowGraph(method);

            return PerformInterproceduralAnalysis(getCfg, method, instanceReceiver: null,
                operation.Arguments, operation, defaultValue, isLambdaOrLocalFunction: false);
        }

        public sealed override TAbstractAnalysisValue VisitInvocation(IInvocationOperation operation, object argument)
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

                if (operation.Arguments.Length == 1 &&
                    operation.Instance != null &&
                    operation.TargetMethod.IsTaskConfigureAwaitMethod(WellKnownTypeProvider.GenericTask))
                {
                    // ConfigureAwait invocation - just return the abstract value of the visited instance on which it is invoked.
                    value = GetCachedAbstractValue(operation.Instance);
                }
                else if (operation.Arguments.Length == 1 &&
                   operation.TargetMethod.IsTaskFromResultMethod(WellKnownTypeProvider.Task))
                {
                    // Result wrapped within a task.
                    var wrappedOperationValue = GetCachedAbstractValue(operation.Arguments[0].Value);
                    var pointsToValueOfTask = GetPointsToAbstractValue(operation);
                    SetTaskWrappedValue(pointsToValueOfTask, wrappedOperationValue);
                }

                PostVisitInvocation(operation.TargetMethod, operation.Arguments);
            }

            return value;

            // Local functions.
            void PostVisitInvocation(IMethodSymbol targetMethod, ImmutableArray<IArgumentOperation> arguments)
            {
                // Predicate analysis for different equality compare method invocations.
                if (PredicateAnalysis &&
                    operation.Type.SpecialType == SpecialType.System_Boolean &&
                    (targetMethod.Name.EndsWith("Equals", StringComparison.Ordinal) ||
                     targetMethod.IsArgumentNullCheckMethod()))
                {
                    PerformPredicateAnalysis(operation);
                }

                if (targetMethod.IsLockMethod(WellKnownTypeProvider.Monitor))
                {
                    // "System.Threading.Monitor.Enter(object)" OR "System.Threading.Monitor.Enter(object, bool)"
                    Debug.Assert(arguments.Length >= 1);

                    HandleEnterLockOperation(arguments[0].Value);
                }
            }
        }

        private TAbstractAnalysisValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IInvocationOperation operation, object argument)
        {
            var value = base.VisitInvocation(operation, argument);
            return VisitInvocation_NonLambdaOrDelegateOrLocalFunction(operation.TargetMethod, operation.Instance, operation.Arguments,
                invokedAsDelegate: false, originalOperation: operation, defaultValue: value);
        }

        private TAbstractAnalysisValue VisitInvocation_LambdaOrDelegateOrLocalFunction(
            IInvocationOperation operation,
            object argument,
            out HashSet<(IMethodSymbol method, IOperation instance)> resolvedMethodTargetsOpt)
        {
            var value = base.VisitInvocation(operation, argument);

            var knownTargetInvocations = false;
            HashSet<(IMethodSymbol method, IOperation instance)> methodTargetsOptBuilder = null;
            HashSet<IFlowAnonymousFunctionOperation> lambdaTargetsOpt = null;

            if (HasPointsToAnalysisResult)
            {
                if (operation.TargetMethod.MethodKind == MethodKind.LocalFunction)
                {
                    Debug.Assert(operation.Instance == null);

                    knownTargetInvocations = true;
                    AddMethodTarget(operation.TargetMethod, instance: null);
                }
                else if (operation.Instance != null)
                {
                    Debug.Assert(operation.TargetMethod.MethodKind == MethodKind.LambdaMethod ||
                        operation.TargetMethod.MethodKind == MethodKind.DelegateInvoke);

                    var invocationTarget = GetPointsToAbstractValue(operation.Instance);
                    if (invocationTarget.Kind == PointsToAbstractValueKind.KnownLocations)
                    {
                        knownTargetInvocations = true;
                        foreach (var location in invocationTarget.Locations)
                        {
                            if (!HandleCreationOpt(location.CreationOpt))
                            {
                                knownTargetInvocations = false;
                                break;
                            }
                        }
                    }
                }
            }

            if (knownTargetInvocations)
            {
                resolvedMethodTargetsOpt = methodTargetsOptBuilder;
                AnalyzePossibleTargetInvocations();
            }
            else
            {
                resolvedMethodTargetsOpt = null;
                if (PessimisticAnalysis)
                {
                    ResetCurrentAnalysisData();
                }
            }

            return value;

            // Local functions.
            void AddMethodTarget(IMethodSymbol method, IOperation instance)
            {
                Debug.Assert(knownTargetInvocations);

                methodTargetsOptBuilder ??= new HashSet<(IMethodSymbol method, IOperation instance)>();
                methodTargetsOptBuilder.Add((method, instance));
            }

            void AddLambdaTarget(IFlowAnonymousFunctionOperation lambda)
            {
                Debug.Assert(knownTargetInvocations);

                lambdaTargetsOpt ??= new HashSet<IFlowAnonymousFunctionOperation>();
                lambdaTargetsOpt.Add(lambda);
            }

            bool HandleCreationOpt(IOperation creationOpt)
            {
                Debug.Assert(knownTargetInvocations);

                switch (creationOpt)
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

            void AnalyzePossibleTargetInvocations()
            {
                Debug.Assert(knownTargetInvocations);
                Debug.Assert(methodTargetsOptBuilder != null || lambdaTargetsOpt != null);

                TAnalysisData mergedCurrentAnalysisData = null;
                var first = true;
                var defaultValue = value;

                using var savedCurrentAnalysisData = GetClonedCurrentAnalysisData();
                if (methodTargetsOptBuilder != null)
                {
                    foreach ((IMethodSymbol method, IOperation instance) in methodTargetsOptBuilder)
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
                }

                if (lambdaTargetsOpt != null)
                {
                    foreach (var lambda in lambdaTargetsOpt)
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
                }

                Debug.Assert(mergedCurrentAnalysisData == null || ReferenceEquals(mergedCurrentAnalysisData, CurrentAnalysisData));
            }

            TAnalysisData AnalyzePossibleTargetInvocation(Func<TAbstractAnalysisValue> computeValueForInvocation, TAnalysisData inputAnalysisData, TAnalysisData mergedAnalysisData, ref bool first)
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
        /// <param name="visitedInstance">Instance that that the method is invoked on, if any.</param>
        /// <param name="visitedArguments">Arguments to the invoked method.</param>
        /// <param name="invokedAsDelegate">Indicates that invocation is a delegate invocation.</param>
        /// <param name="originalOperation">Original invocation operation, which may be a delegate invocation.</param>
        /// <param name="defaultValue">Default abstract value to return.</param>
        /// <returns>Abstract value of return value.</returns>
        public virtual TAbstractAnalysisValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            IMethodSymbol method,
            IOperation visitedInstance,
            ImmutableArray<IArgumentOperation> visitedArguments,
            bool invokedAsDelegate,
            IOperation originalOperation,
            TAbstractAnalysisValue defaultValue)
        {
            ControlFlowGraph getCfg() => GetInterproceduralControlFlowGraph(method);

            return PerformInterproceduralAnalysis(getCfg, method, visitedInstance,
                visitedArguments, originalOperation, defaultValue, isLambdaOrLocalFunction: false);
        }

        private ControlFlowGraph GetInterproceduralControlFlowGraph(IMethodSymbol method)
        {
            if (DataFlowAnalysisContext.InterproceduralAnalysisDataOpt != null)
            {
                return DataFlowAnalysisContext.InterproceduralAnalysisDataOpt.GetInterproceduralControlFlowGraph(method);
            }

            if (!_interproceduralMethodToCfgMapOpt.TryGetValue(method, out var cfg))
            {
                var operation = method.GetTopmostOperationBlock(WellKnownTypeProvider.Compilation);
                cfg = operation?.GetEnclosingControlFlowGraph();
                _interproceduralMethodToCfgMapOpt.Add(method, cfg);
            }

            return cfg;
        }

        private ImmutableStack<IOperation> GetInterproceduralCallStackForOwningSymbol(ISymbol forOwningSymbol)
        {
            if (OwningSymbol.Equals(forOwningSymbol))
            {
                return DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.CallStack;
            }

            return DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.GetInterproceduralCallStackForOwningSymbol(forOwningSymbol);
        }

        public virtual TAbstractAnalysisValue VisitInvocation_LocalFunction(
            IMethodSymbol localFunction,
            ImmutableArray<IArgumentOperation> visitedArguments,
            IOperation originalOperation,
            TAbstractAnalysisValue defaultValue)
        {
            ControlFlowGraph getCfg() => DataFlowAnalysisContext.GetLocalFunctionControlFlowGraph(localFunction);
            return PerformInterproceduralAnalysis(getCfg, localFunction, instanceReceiver: null, arguments: visitedArguments,
                originalOperation: originalOperation, defaultValue: defaultValue, isLambdaOrLocalFunction: true);
        }

        public virtual TAbstractAnalysisValue VisitInvocation_Lambda(
            IFlowAnonymousFunctionOperation lambda,
            ImmutableArray<IArgumentOperation> visitedArguments,
            IOperation originalOperation,
            TAbstractAnalysisValue defaultValue)
        {
            ControlFlowGraph getCfg() => DataFlowAnalysisContext.GetAnonymousFunctionControlFlowGraph(lambda);
            return PerformInterproceduralAnalysis(getCfg, lambda.Symbol, instanceReceiver: null, arguments: visitedArguments,
                originalOperation: originalOperation, defaultValue: defaultValue, isLambdaOrLocalFunction: true);
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

        public override TAbstractAnalysisValue VisitTuple(ITupleOperation operation, object argument)
        {
            var elementValueBuilder = ArrayBuilder<TAbstractAnalysisValue>.GetInstance(operation.Elements.Length);

            try
            {
                foreach (var element in operation.Elements)
                {
                    elementValueBuilder.Add(Visit(element, argument));
                }

                // Set abstract value for tuple element/field assignment if the tuple is not target of a deconstruction assignment.
                // For deconstruction assignment, the value would be assigned from the computed value for the right side of the assignment.
                var deconstructionAncestorOpt = operation.GetAncestor<IDeconstructionAssignmentOperation>(OperationKind.DeconstructionAssignment);
                if (deconstructionAncestorOpt == null ||
                    !deconstructionAncestorOpt.Target.Descendants().Contains(operation))
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
            finally
            {
                elementValueBuilder.Free();
            }
        }

        public virtual TAbstractAnalysisValue VisitUnaryOperatorCore(IUnaryOperation operation, object argument)
        {
            return base.VisitUnaryOperator(operation, argument);
        }

        public sealed override TAbstractAnalysisValue VisitUnaryOperator(IUnaryOperation operation, object argument)
        {
            var value = VisitUnaryOperatorCore(operation, argument);
            if (PredicateAnalysis && operation.OperatorKind == UnaryOperatorKind.Not)
            {
                PerformPredicateAnalysis(operation);
            }

            return value;
        }

        public virtual TAbstractAnalysisValue VisitBinaryOperatorCore(IBinaryOperation operation, object argument)
        {
            return base.VisitBinaryOperator(operation, argument);
        }

        public sealed override TAbstractAnalysisValue VisitBinaryOperator(IBinaryOperation operation, object argument)
        {
            var value = VisitBinaryOperatorCore(operation, argument);
            if (PredicateAnalysis && operation.IsComparisonOperator())
            {
                PerformPredicateAnalysis(operation);
            }

            return value;
        }

        public override TAbstractAnalysisValue VisitIsNull(IIsNullOperation operation, object argument)
        {
            var value = base.VisitIsNull(operation, argument);
            if (PredicateAnalysis)
            {
                PerformPredicateAnalysis(operation);
            }
            return value;
        }

        public override TAbstractAnalysisValue VisitCaughtException(ICaughtExceptionOperation operation, object argument)
        {
            // Merge data from unhandled exception paths within try that match the caught exception type.
            if (operation.Type != null)
            {
                MergeAnalysisDataFromUnhandledThrowOperations(operation.Type);
            }

            return base.VisitCaughtException(operation, argument);
        }

        private void MergeAnalysisDataFromUnhandledThrowOperations(ITypeSymbol caughtExceptionTypeOpt)
        {
            Debug.Assert(caughtExceptionTypeOpt != null || CurrentBasicBlock.IsFirstBlockOfFinally(out _));

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
                        if (caughtExceptionTypeOpt != null)
                        {
                            AnalysisDataForUnhandledThrowOperations.Remove(pendingThrow);
                            exceptionData.Dispose();
                        }
                    }
                }
            }

            bool ShouldHandlePendingThrow(ThrownExceptionInfo pendingThrow)
            {
                if (pendingThrow.HandlingCatchRegionOpt == CurrentBasicBlock.EnclosingRegion)
                {
                    // Catch region explicitly handling the thrown exception.
                    return true;
                }

                if (caughtExceptionTypeOpt == null)
                {
                    // Check if finally region is executed for pending throw.
                    Debug.Assert(CurrentBasicBlock.IsFirstBlockOfFinally(out _));
                    var tryFinallyRegion = CurrentBasicBlock.GetContainingRegionOfKind(ControlFlowRegionKind.TryAndFinally);
                    var tryRegion = tryFinallyRegion.NestedRegions[0];
                    return tryRegion.FirstBlockOrdinal <= pendingThrow.BasicBlockOrdinal && tryRegion.LastBlockOrdinal >= pendingThrow.BasicBlockOrdinal;
                }

                return false;
            }
        }

        public override TAbstractAnalysisValue VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, object argument)
        {
            // https://github.com/dotnet/roslyn-analyzers/issues/1571 tracks adding support.
            return base.VisitFlowAnonymousFunction(operation, argument);
        }

        public override TAbstractAnalysisValue VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, object argument)
        {
            // https://github.com/dotnet/roslyn-analyzers/issues/1571 tracks adding support.
            return base.VisitStaticLocalInitializationSemaphore(operation, argument);
        }

        public override TAbstractAnalysisValue VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object argument)
        {
            var savedIsInsideAnonymousObjectInitializer = IsInsideAnonymousObjectInitializer;
            IsInsideAnonymousObjectInitializer = true;
            var value = base.VisitAnonymousObjectCreation(operation, argument);
            IsInsideAnonymousObjectInitializer = savedIsInsideAnonymousObjectInitializer;
            return value;
        }

        public sealed override TAbstractAnalysisValue VisitReturn(IReturnOperation operation, object argument)
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

        public sealed override TAbstractAnalysisValue VisitIsPattern(IIsPatternOperation operation, object argument)
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

        public override TAbstractAnalysisValue VisitAwait(IAwaitOperation operation, object argument)
        {
            var value = base.VisitAwait(operation, argument);

            var pointsToValue = GetPointsToAbstractValue(operation.Operation);
            return TryGetTaskWrappedValue(pointsToValue, out var awaitedValue) ?
                awaitedValue :
                value;
        }

        #region Overrides for lowered IOperations

        public sealed override TAbstractAnalysisValue VisitUsing(IUsingOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IUsingOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitWhileLoop(IWhileLoopOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IWhileLoopOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitForEachLoop(IForEachLoopOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IForEachLoopOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitForLoop(IForLoopOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IForLoopOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitForToLoop(IForToLoopOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IForToLoopOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitCoalesce(ICoalesceOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ICoalesceOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitConditional(IConditionalOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IConditionalOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitConditionalAccess(IConditionalAccessOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IConditionalAccessOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IConditionalAccessInstanceOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitThrow(IThrowOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IThrowOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitVariableDeclaration(IVariableDeclarationOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IVariableDeclarationOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IVariableDeclarationOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitVariableDeclarator(IVariableDeclaratorOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IVariableDeclaratorOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitTry(ITryOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ITryOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitCatchClause(ICatchClauseOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ICatchClauseOperation)}' must have been lowered in the CFG");
        }

        public override TAbstractAnalysisValue VisitLock(ILockOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ILockOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitBranch(IBranchOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IBranchOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitLabeled(ILabeledOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ILabeledOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitSwitch(ISwitchOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ISwitchOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitSwitchCase(ISwitchCaseOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ISwitchCaseOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IDefaultCaseClauseOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitPatternCaseClause(IPatternCaseClauseOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IPatternCaseClauseOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitRangeCaseClause(IRangeCaseClauseOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IRangeCaseClauseOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IRelationalCaseClauseOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ISingleValueCaseClauseOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IObjectOrCollectionInitializerOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitMemberInitializer(IMemberInitializerOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IMemberInitializerOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitBlock(IBlockOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IBlockOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitVariableInitializer(IVariableInitializerOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IVariableInitializerOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitFieldInitializer(IFieldInitializerOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IFieldInitializerOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitParameterInitializer(IParameterInitializerOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IParameterInitializerOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitPropertyInitializer(IPropertyInitializerOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IPropertyInitializerOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitEnd(IEndOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IEndOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitEmpty(IEmptyOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IEmptyOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitNameOf(INameOfOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(INameOfOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitAnonymousFunction(IAnonymousFunctionOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(IAnonymousFunctionOperation)}' must have been lowered in the CFG");
        }

        public sealed override TAbstractAnalysisValue VisitLocalFunction(ILocalFunctionOperation operation, object argument)
        {
            throw new InvalidProgramException($"'{nameof(ILocalFunctionOperation)}' must have been lowered in the CFG");
        }

        #endregion

        #endregion
    }
}