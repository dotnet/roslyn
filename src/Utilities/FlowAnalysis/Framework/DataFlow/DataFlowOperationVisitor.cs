// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Operation visitor to flow the abstract dataflow analysis values across a given statement in a basic block.
    /// </summary>
    internal abstract class DataFlowOperationVisitor<TAnalysisData, TAbstractAnalysisValue> : OperationVisitor<object, TAbstractAnalysisValue>
    {
        private readonly DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> _copyAnalysisResultOpt;
        private readonly DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> _pointsToAnalysisResultOpt;
        private readonly ImmutableHashSet<CaptureId> _lValueFlowCaptures;
        private readonly ImmutableDictionary<IOperation, TAbstractAnalysisValue>.Builder _valueCacheBuilder;
        private readonly ImmutableDictionary<IOperation, PredicateValueKind>.Builder _predicateValueKindCacheBuilder;
        private readonly List<IArgumentOperation> _pendingArgumentsToReset;
        private readonly HashSet<AnalysisEntity> _flowCaptureReferencesWithPredicatedData;
        private readonly HashSet<IOperation> _visitedFlowBranchConditions;
        private ImmutableDictionary<IParameterSymbol, AnalysisEntity> _lazyParameterEntities;
        private ImmutableHashSet<IMethodSymbol> _lazyContractCheckMethodsForPredicateAnalysis;
        private TAnalysisData _currentAnalysisData;
        private int _recursionDepth;

        protected abstract TAbstractAnalysisValue GetAbstractDefaultValue(ITypeSymbol type);
        protected virtual TAbstractAnalysisValue GetAbstractDefaultValueForCatchVariable(ICatchClauseOperation catchClause) => ValueDomain.UnknownOrMayBeValue;
        protected abstract bool HasAnyAbstractValue(TAnalysisData data);
        protected abstract void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity);
        protected abstract void SetValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity);
        protected abstract void ResetCurrentAnalysisData();
        protected bool HasPointsToAnalysisResult => _pointsToAnalysisResultOpt != null || IsPointsToAnalysis;
        protected virtual bool IsPointsToAnalysis => false;
        public Dictionary<ThrowBranchWithExceptionType, TAnalysisData> AnalysisDataForUnhandledThrowOperations { get; private set; }

        public AbstractValueDomain<TAbstractAnalysisValue> ValueDomain { get; }
        protected ISymbol OwningSymbol { get; }
        protected TAnalysisData CurrentAnalysisData
        {
            get => _currentAnalysisData;
            private set
            {
                Debug.Assert(value != null);
                _currentAnalysisData = value;
            }
        }

        protected BasicBlock CurrentBasicBlock { get; private set; }
        protected ControlFlowConditionKind FlowBranchConditionKind { get; private set; }
        protected PointsToAbstractValue ThisOrMePointsToAbstractValue { get; }
        protected AnalysisEntityFactory AnalysisEntityFactory { get; }
        protected WellKnownTypeProvider WellKnownTypeProvider { get; }

        /// <summary>
        /// This boolean field determines if the caller requires an optimistic OR a pessimistic analysis for such cases.
        /// For example, invoking an instance method may likely invalidate all the instance field analysis state, i.e.
        /// reference type fields might be re-assigned to point to different objects in the called method.
        /// An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
        /// A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
        /// </summary>
        /// <remarks>
        /// For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.
        /// For string content analysis, we want to perform a pessimistic points to analysis to be conservative and avoid missing out true violations.
        /// </remarks>
        protected bool PessimisticAnalysis { get; }

        /// <summary>
        /// Indicates if we this visitor needs to analyze predicates of conditions.
        /// </summary>
        protected bool PredicateAnalysis { get; }

        /// <summary>
        /// PERF: Track if we are within an <see cref="IAnonymousObjectCreationOperation"/>.
        /// </summary>
        protected bool IsInsideAnonymousObjectInitializer { get; private set; }

        protected bool IsLValueFlowCapture(CaptureId captureId) => _lValueFlowCaptures.Contains(captureId);

        protected DataFlowOperationVisitor(
            AbstractValueDomain<TAbstractAnalysisValue> valueDomain,
            ISymbol owningSymbol,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph cfg,
            bool pessimisticAnalysis,
            bool predicateAnalysis,
            DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> copyAnalysisResultOpt,
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt)
        {
            Debug.Assert(owningSymbol != null);
            Debug.Assert(owningSymbol.Kind == SymbolKind.Method ||
                owningSymbol.Kind == SymbolKind.Field ||
                owningSymbol.Kind == SymbolKind.Property ||
                owningSymbol.Kind == SymbolKind.Event);
            Debug.Assert(wellKnownTypeProvider != null);

            ValueDomain = valueDomain;
            OwningSymbol = owningSymbol;
            WellKnownTypeProvider = wellKnownTypeProvider;
            PessimisticAnalysis = pessimisticAnalysis;
            PredicateAnalysis = predicateAnalysis;
            _copyAnalysisResultOpt = copyAnalysisResultOpt;
            _pointsToAnalysisResultOpt = pointsToAnalysisResultOpt;
            _lValueFlowCaptures = LValueFlowCapturesProvider.GetOrCreateLValueFlowCaptures(cfg);
            _valueCacheBuilder = ImmutableDictionary.CreateBuilder<IOperation, TAbstractAnalysisValue>();
            _predicateValueKindCacheBuilder = ImmutableDictionary.CreateBuilder<IOperation, PredicateValueKind>();
            _pendingArgumentsToReset = new List<IArgumentOperation>();
            _flowCaptureReferencesWithPredicatedData = new HashSet<AnalysisEntity>();
            _visitedFlowBranchConditions = new HashSet<IOperation>();
            ThisOrMePointsToAbstractValue = GetThisOrMeInstancePointsToValue(owningSymbol);

            AnalysisEntityFactory = new AnalysisEntityFactory(
                getPointsToAbstractValueOpt: (pointsToAnalysisResultOpt != null || IsPointsToAnalysis) ?
                    GetPointsToAbstractValue :
                    (Func<IOperation, PointsToAbstractValue>)null,
                getIsInsideAnonymousObjectInitializer: () => IsInsideAnonymousObjectInitializer,
                containingTypeSymbol: owningSymbol.ContainingType);
        }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(GetType().GetHashCode(),
                HashUtilities.Combine(ValueDomain.GetHashCode(),
                HashUtilities.Combine(OwningSymbol.GetHashCode(),
                HashUtilities.Combine(WellKnownTypeProvider.Compilation.GetHashCode(),
                HashUtilities.Combine(PessimisticAnalysis.GetHashCode(),
                HashUtilities.Combine(PredicateAnalysis.GetHashCode(),
                HashUtilities.Combine(_copyAnalysisResultOpt?.GetHashCode() ?? 0,
                    _pointsToAnalysisResultOpt?.GetHashCode() ?? 0)))))));
        }

        private static PointsToAbstractValue GetThisOrMeInstancePointsToValue(ISymbol owningSymbol)
        {
            if (!owningSymbol.IsStatic &&
                !owningSymbol.ContainingType.HasValueCopySemantics())
            {
                var thisOrMeLocation = AbstractLocation.CreateThisOrMeLocation(owningSymbol.ContainingType);
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
            if (PredicateAnalysis)
            {
                // Perf: Stop tracking predicated data for flow captures after the first reference.
                foreach (AnalysisEntity flowCaptureEntity in _flowCaptureReferencesWithPredicatedData)
                {
                    Debug.Assert(HasPredicatedDataForEntity(flowCaptureEntity));
                    StopTrackingPredicatedData(flowCaptureEntity);
                }

                _flowCaptureReferencesWithPredicatedData.Clear();
            }

            Debug.Assert(_pendingArgumentsToReset.Count == 0);
            Debug.Assert(_flowCaptureReferencesWithPredicatedData.Count == 0);

            // Ensure that we visited and cached values for all operation descendants.
            foreach (var descendant in operation.DescendantsAndSelf())
            {
                // GetState will throw an InvalidOperationException if the visitor did not visit the operation or cache it's abstract value.
                var _ = GetCachedAbstractValue(descendant);
            }
        }

        public void OnStartBlockAnalysis(BasicBlock block, TAnalysisData input)
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
            }
        }

        public void OnEndBlockAnalysis(BasicBlock block)
        {
            if (block.EnclosingRegion != null &&
                block.EnclosingRegion.LastBlockOrdinal == block.Ordinal &&
                (block.EnclosingRegion.Kind == ControlFlowRegionKind.Finally ||
                 block.EnclosingRegion.Kind == ControlFlowRegionKind.Catch ||
                 block.EnclosingRegion.Kind == ControlFlowRegionKind.Filter))
            {
                OnLeavingRegion(block.EnclosingRegion);
            }

            CurrentBasicBlock = null;
        }

        private void OnStartEntryBlockAnalysis(BasicBlock entryBlock)
        {
            Debug.Assert(entryBlock.Kind == BasicBlockKind.Entry);

            if (_lazyParameterEntities == null &&
                OwningSymbol is IMethodSymbol method &&
                method.Parameters.Length > 0)
            {
                var builder = ImmutableDictionary.CreateBuilder<IParameterSymbol, AnalysisEntity>();
                foreach (var parameter in method.Parameters)
                {
                    var result = AnalysisEntityFactory.TryCreateForSymbolDeclaration(parameter, out AnalysisEntity analysisEntity);
                    Debug.Assert(result);
                    builder.Add(parameter, analysisEntity);
                    SetValueForParameterOnEntry(parameter, analysisEntity);
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
                    SetValueForParameterOnExit(parameter, analysisEntity);
                }
            }
        }

        /// <summary>
        /// Primary method that flows analysis data through the given flow edge/branch.
        /// </summary>
        public virtual TAnalysisData FlowBranch(
            BasicBlock fromBlock,
            BranchWithInfo branch,
            TAnalysisData input)
        {
            Debug.Assert(fromBlock != null);
            Debug.Assert(input != null);

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
                    var exceptionType = branch.BranchValueOpt?.GetThrowExceptionType(CurrentBasicBlock) as INamedTypeSymbol;
                    if (exceptionType != null &&
                        exceptionType.DerivesFrom(WellKnownTypeProvider.Exception, baseTypesOnly: true))
                    {
                        AnalysisDataForUnhandledThrowOperations = AnalysisDataForUnhandledThrowOperations ?? new Dictionary<ThrowBranchWithExceptionType, TAnalysisData>();
                        var branchWithPredecessor = new ThrowBranchWithExceptionType(branch, exceptionType);
                        AnalysisDataForUnhandledThrowOperations[branchWithPredecessor] = GetClonedCurrentAnalysisData();
                    }

                    ProcessThrowValue(branch.BranchValueOpt);
                    break;
            }

            return CurrentAnalysisData;
        }

        protected virtual void ProcessReturnValue(IOperation returnValue)
        {
        }

        public TAnalysisData OnLeavingRegions(ImmutableArray<ControlFlowRegion> regions, BasicBlock currentBasicBlock, TAnalysisData input)
        {
            CurrentBasicBlock = currentBasicBlock;
            CurrentAnalysisData = input;

            foreach (var region in regions)
            {
                OnLeavingRegion(region);
            }

            CurrentBasicBlock = null;
            return CurrentAnalysisData;
        }

        protected virtual void OnLeavingRegion(ControlFlowRegion region)
        {
        }

        private bool IsContractCheckArgument(IArgumentOperation operation)
        {
            Debug.Assert(PredicateAnalysis);

            if (WellKnownTypeProvider.Contract != null &&
                operation.Parent is IInvocationOperation invocation &&
                invocation.TargetMethod.ContainingType == WellKnownTypeProvider.Contract &&
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

        public ImmutableDictionary<IOperation, TAbstractAnalysisValue> GetStateMap() => _valueCacheBuilder.ToImmutable();

        public ImmutableDictionary<IOperation, PredicateValueKind> GetPredicateValueKindMap() => _predicateValueKindCacheBuilder.ToImmutable();

        public TAnalysisData GetMergedDataForUnhandledThrowOperations()
        {
            if (AnalysisDataForUnhandledThrowOperations == null)
            {
                return default(TAnalysisData);
            }

            TAnalysisData mergedData = default(TAnalysisData);
            foreach (TAnalysisData data in AnalysisDataForUnhandledThrowOperations.Values)
            {
                mergedData = mergedData != null ? MergeAnalysisData(mergedData, data) : data;
            }

            return mergedData;
        }

        public TAbstractAnalysisValue GetCachedAbstractValue(IOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            TAbstractAnalysisValue state;
            if (!_valueCacheBuilder.TryGetValue(operation, out state))
            {
                throw new InvalidOperationException();
            }

            return state;
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
            if (_copyAnalysisResultOpt == null)
            {
                return CopyAbstractValue.Unknown;
            }
            else
            {
                return _copyAnalysisResultOpt[operation];
            }
        }

        protected virtual PointsToAbstractValue GetPointsToAbstractValue(IOperation operation)
        {
            if (_pointsToAnalysisResultOpt == null)
            {
                return PointsToAbstractValue.Unknown;
            }
            else
            {
                return _pointsToAnalysisResultOpt[operation];
            }
        }

        protected bool TryGetPointsToAbstractValueAtCurrentBlockEntry(AnalysisEntity analysisEntity, out PointsToAbstractValue pointsToAbstractValue)
        {
            Debug.Assert(CurrentBasicBlock != null);
            Debug.Assert(_pointsToAnalysisResultOpt != null);
            var inputData = _pointsToAnalysisResultOpt[CurrentBasicBlock].InputData;
            return inputData.TryGetValue(analysisEntity, out pointsToAbstractValue);
        }

        protected bool TryGetPointsToAbstractValueAtCurrentBlockExit(AnalysisEntity analysisEntity, out PointsToAbstractValue pointsToAbstractValue)
        {
            Debug.Assert(CurrentBasicBlock != null);
            Debug.Assert(_pointsToAnalysisResultOpt != null);
            var outputData = _pointsToAnalysisResultOpt[CurrentBasicBlock].OutputData;
            return outputData.TryGetValue(analysisEntity, out pointsToAbstractValue);
        }

        protected bool TryGetNullAbstractValueAtCurrentBlockEntry(AnalysisEntity analysisEntity, out NullAbstractValue nullAbstractValue)
        {
            Debug.Assert(CurrentBasicBlock != null);
            Debug.Assert(_pointsToAnalysisResultOpt != null);
            var inputData = _pointsToAnalysisResultOpt[CurrentBasicBlock].InputData;
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
            Debug.Assert(_pointsToAnalysisResultOpt != null);
            var inputData = _pointsToAnalysisResultOpt.MergedStateForUnhandledThrowOperationsOpt?.InputData;
            if (inputData == null || !inputData.TryGetValue(analysisEntity, out PointsToAbstractValue pointsToAbstractValue))
            {
                nullAbstractValue = NullAbstractValue.MaybeNull;
                return false;
            }

            nullAbstractValue = pointsToAbstractValue.NullState;
            return true;
        }

        protected virtual TAbstractAnalysisValue ComputeAnalysisValueForReferenceOperation(IOperation operation, TAbstractAnalysisValue defaultValue)
        {
            return defaultValue;
        }

        protected virtual TAbstractAnalysisValue ComputeAnalysisValueForOutArgument(IArgumentOperation operation, TAbstractAnalysisValue defaultValue)
        {
            return defaultValue;
        }

        protected bool TryInferConversion(IConversionOperation operation, out bool alwaysSucceed, out bool alwaysFail)
        {
            // For direct cast, we assume the cast will always succeed.
            alwaysSucceed = !operation.IsTryCast;
            alwaysFail = false;

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

            // Bail out for throw expression conversion.
            if (operation.Operand.Kind == OperationKind.Throw)
            {
                return true;
            }

            // Analyze if cast might always succeed or fail based on points to analysis result.
            var pointsToValue = GetPointsToAbstractValue(operation.Operand);
            if (pointsToValue.Kind == PointsToAbstractValueKind.KnownLocations)
            {
                // Bail out if we have a possible null location for direct cast.
                if (!operation.IsTryCast && pointsToValue.Locations.Any(location => location.IsNull))
                {
                    return true;
                }

                // Infer if a cast will always fail.
                // We are currently bailing out if an interface or type parameter is involved.
                bool IsInterfaceOrTypeParameter(ITypeSymbol type) => type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.TypeParameter;
                if (!IsInterfaceOrTypeParameter(operation.Type) &&
                    pointsToValue.Locations.All(location => location.IsNull ||
                        location.IsNoLocation ||
                        (!IsInterfaceOrTypeParameter(location.LocationTypeOpt) &&
                         !operation.Type.DerivesFrom(location.LocationTypeOpt) &&
                         !location.LocationTypeOpt.DerivesFrom(operation.Type))))
                {
                    if (PredicateAnalysis)
                    {
                        _predicateValueKindCacheBuilder[operation] = PredicateValueKind.AlwaysFalse;
                    }

                    // We only set the alwaysFail flag for TryCast as direct casts that are guaranteed to fail will throw an exception and subsequent code will not execute.
                    if (operation.IsTryCast)
                    {
                        alwaysFail = true;
                    }
                }
                else
                {
                    // Infer if a TryCast will always succeed.
                    if (operation.IsTryCast &&
                        pointsToValue.Locations.All(location => location.IsNoLocation || !location.IsNull && location.LocationTypeOpt.DerivesFrom(operation.Type)))
                    {
                        // TryCast which is guaranteed to succeed, and potentially can be changed to DirectCast.
                        if (PredicateAnalysis)
                        {
                            _predicateValueKindCacheBuilder[operation] = PredicateValueKind.AlwaysTrue;
                        }

                        alwaysSucceed = true;
                    }
                }

                return true;
            }

            return false;
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
            Debug.Assert(PredicateAnalysis);

            return basicBlock.IsReachable &&
                (_copyAnalysisResultOpt == null || _copyAnalysisResultOpt[basicBlock].IsReachable) &&
                (_pointsToAnalysisResultOpt == null || _pointsToAnalysisResultOpt[basicBlock].IsReachable);
        }

        private void PerformPredicateAnalysis(IOperation operation)
        {
            Debug.Assert(PredicateAnalysis);
            Debug.Assert(operation.Kind == OperationKind.BinaryOperator ||
                operation.Kind == OperationKind.UnaryOperator ||
                operation.Kind == OperationKind.IsNull ||
                operation.Kind == OperationKind.Invocation ||
                operation.Kind == OperationKind.Argument ||
                operation.Kind == OperationKind.FlowCaptureReference);

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
                    PerformPredicateAnalysisCore(operation, GetClonedCurrentAnalysisData());
                    FlowBranchConditionKind = ControlFlowConditionKind.None;
#if DEBUG
                    Debug.Assert(Equals(savedCurrentAnalysisData, CurrentAnalysisData), "Expected no updates to CurrentAnalysisData");
#endif
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
                        TAnalysisData truePredicatedData = GetEmptyAnalysisDataForPredicateAnalysis();
                        FlowBranchConditionKind = ControlFlowConditionKind.WhenTrue;
                        PerformPredicateAnalysisCore(operation, truePredicatedData);

                        TAnalysisData falsePredicatedData = GetEmptyAnalysisDataForPredicateAnalysis();
                        FlowBranchConditionKind = ControlFlowConditionKind.WhenFalse;
                        PerformPredicateAnalysisCore(operation, falsePredicatedData);
                        FlowBranchConditionKind = ControlFlowConditionKind.None;

#if DEBUG
                        Debug.Assert(Equals(savedCurrentAnalysisData, CurrentAnalysisData), "Expected no updates to CurrentAnalysisData");
#endif

                        if (HasAnyAbstractValue(truePredicatedData) || HasAnyAbstractValue(falsePredicatedData))
                        {
                            StartTrackingPredicatedData(predicatedFlowCaptureEntityOpt, truePredicatedData, falsePredicatedData);
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
                            if (AnalysisEntityFactory.TryCreate(current, out var targetEntity))
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

                case IBinaryOperation binaryOperation:
                    // Predicate analysis for different equality comparison operators.
                    Debug.Assert(binaryOperation.IsComparisonOperator());
                    predicateValueKind = SetValueForComparisonOperator(binaryOperation, targetAnalysisData);
                    break;

                case IUnaryOperation unaryOperation:
                    // Predicate analysis for unary not operator.
                    Debug.Assert(unaryOperation.OperatorKind == UnaryOperatorKind.Not);
                    FlowBranchConditionKind = FlowBranchConditionKind.Negate();
                    PerformPredicateAnalysisCore(unaryOperation.Operand, targetAnalysisData);
                    FlowBranchConditionKind = FlowBranchConditionKind.Negate();
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

                case IFlowCaptureReferenceOperation flowCaptureReference:
                    var result = AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity flowCaptureReferenceEntity);
                    Debug.Assert(result);
                    Debug.Assert(flowCaptureReferenceEntity.CaptureIdOpt != null);
                    Debug.Assert(HasPredicatedDataForEntity(flowCaptureReferenceEntity));
                    predicateValueKind = ApplyPredicatedDataForEntity(targetAnalysisData, flowCaptureReferenceEntity, trueData: FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue);
                    break;

                case IInvocationOperation invocation:
                    // Predicate analysis for different equality comparison methods.
                    Debug.Assert(invocation.Type.SpecialType == SpecialType.System_Boolean);

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

            var isReferenceEquality = operation.OperatorMethod == null && !operation.Type.HasValueCopySemantics();
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

        protected virtual bool HasPredicatedDataForEntity(AnalysisEntity predicatedEntity)
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

        protected virtual TAnalysisData GetEmptyAnalysisDataForPredicateAnalysis()
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
        /// <param name="operation"></param>
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

        /// <summary>
        /// Resets the analysis data for an object instance passed around as an <see cref="IArgumentOperation"/>.
        /// </summary>
        private void ResetInstanceAnalysisDataForArgument(IArgumentOperation operation)
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
            if (operation.Parameter.RefKind != RefKind.None)
            {
                var value = GetCachedAbstractValue(operation);
                if (operation.Parameter.RefKind != RefKind.Out)
                {
                    value = ValueDomain.Merge(value, GetCachedAbstractValue(operation.Value));
                }

                SetAbstractValueForAssignment(operation.Value, operation, value);
            }
        }

        #endregion

        protected abstract TAnalysisData MergeAnalysisData(TAnalysisData value1, TAnalysisData value2);
        protected abstract TAnalysisData GetClonedAnalysisData(TAnalysisData analysisData);
        protected TAnalysisData GetClonedCurrentAnalysisData() => GetClonedAnalysisData(CurrentAnalysisData);
        protected abstract bool Equals(TAnalysisData value1, TAnalysisData value2);
        protected static bool EqualsHelper<TKey, TValue>(IDictionary<TKey, TValue> dict1, IDictionary<TKey, TValue> dict2)
            => dict1.Count == dict2.Count &&
               dict1.Keys.All(key => dict2.TryGetValue(key, out TValue value2) && EqualityComparer<TValue>.Default.Equals(dict1[key], value2));

        #region Visitor methods

        internal TAbstractAnalysisValue VisitArray(IEnumerable<IOperation> operations, object argument)
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

                if (_pendingArgumentsToReset.Any(arg => arg.Parent == operation))
                {
                    var pendingArguments = _pendingArgumentsToReset.Where(arg => arg.Parent == operation).ToImmutableArray();
                    foreach (IArgumentOperation argumentOperation in pendingArguments)
                    {
                        ResetInstanceAnalysisDataForArgument(argumentOperation);
                        _pendingArgumentsToReset.Remove(argumentOperation);
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
            var value = ComputeValueForCompoundAssignment(operation, targetValue, assignedValue);
            SetAbstractValueForAssignment(operation.Target, operation.Value, value);
            return value;
        }

        public virtual TAbstractAnalysisValue ComputeValueForCompoundAssignment(ICompoundAssignmentOperation operation, TAbstractAnalysisValue targetValue, TAbstractAnalysisValue assignedValue)
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
            TAbstractAnalysisValue _ = Visit(operation.Target, argument);
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
            var elementType = ((IArrayTypeSymbol)arrayCreation.Type).ElementType;
            for (int index = 0; index < operation.ElementValues.Length; index++)
            {
                var abstractIndex = AbstractIndex.Create(index);
                IOperation elementInitializer = operation.ElementValues[index];
                TAbstractAnalysisValue initializerValue = Visit(elementInitializer, argument);
                SetAbstractValueForArrayElementInitializer(arrayCreation, ImmutableArray.Create(abstractIndex), elementType, elementInitializer, initializerValue);
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

        public virtual TAbstractAnalysisValue VisitMethodReferenceCore(IMethodReferenceOperation operation, object argument)
        {
            var value = base.VisitMethodReference(operation, argument);
            return ComputeAnalysisValueForReferenceOperation(operation, value);
        }

        public sealed override TAbstractAnalysisValue VisitMethodReference(IMethodReferenceOperation operation, object argument)
        {
            var value = VisitMethodReferenceCore(operation, argument);
            if (operation.IsLambdaOrLocalFunctionOrDelegateReference())
            {
                // Reference to a lambda or local function or delegate.
                // This might be passed around as an argument, which can later be invoked from other methods.

                // Currently, we are not performing flow analysis for invocations of lambda or delegate or local function.
                // Pessimistically assume that all the current state could change and reset all our current analysis data.
                // TODO: Analyze lambda and local functions and flow the values from it's exit block to CurrentAnalysisData.
                // https://github.com/dotnet/roslyn-analyzers/issues/1547
                ResetCurrentAnalysisData();
            }
            return value;
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
                _flowCaptureReferencesWithPredicatedData.Add(flowCaptureReferenceEntity);
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
                    TAnalysisData predicatedData = GetEmptyAnalysisDataForPredicateAnalysis();
                    TAnalysisData truePredicatedData, falsePredicatedData;
                    if (constantValue)
                    {
                        truePredicatedData = predicatedData;
                        falsePredicatedData = default(TAnalysisData);
                    }
                    else
                    {
                        falsePredicatedData = predicatedData;
                        truePredicatedData = default(TAnalysisData);
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
            var formatValue = Visit(operation.FormatString, argument);
            var alignmentValue = Visit(operation.Alignment, argument);
            return expressionValue;
        }

        public override TAbstractAnalysisValue VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, object argument)
        {
            return Visit(operation.Text, argument);
        }

        public virtual TAbstractAnalysisValue VisitArgumentCore(IArgumentOperation operation, object argument)
        {
            return Visit(operation.Value, argument);
        }

        public sealed override TAbstractAnalysisValue VisitArgument(IArgumentOperation operation, object argument)
        {
            var value = VisitArgumentCore(operation, argument);
            if (operation.Parameter.RefKind != RefKind.None)
            {
                value = ComputeAnalysisValueForOutArgument(operation, defaultValue: ValueDomain.UnknownOrMayBeValue);
            }

            // Is first argument of a Contract check invocation?
            if (PredicateAnalysis && IsContractCheckArgument(operation))
            {
                Debug.Assert(FlowBranchConditionKind == ControlFlowConditionKind.None);
                
                // Force true branch.
                FlowBranchConditionKind = ControlFlowConditionKind.WhenTrue;
                PerformPredicateAnalysis(operation);
                FlowBranchConditionKind = ControlFlowConditionKind.None;
            }

            _pendingArgumentsToReset.Add(operation);
            return value;
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

        public sealed override TAbstractAnalysisValue VisitInvocation(IInvocationOperation operation, object argument)
        {
            TAbstractAnalysisValue value;
            if (operation.TargetMethod.IsLambdaOrLocalFunctionOrDelegate())
            {
                // Invocation of a lambda or local function.
                value = VisitInvocation_LambdaOrDelegateOrLocalFunction(operation, argument);
            }
            else
            {
                value = VisitInvocation_NonLambdaOrDelegateOrLocalFunction(operation, argument);

                // Predicate analysis for different equality compare method invocations.
                if (PredicateAnalysis &&
                    operation.Type.SpecialType == SpecialType.System_Boolean &&
                    operation.TargetMethod.Name.EndsWith("Equals", StringComparison.Ordinal))
                {
                    PerformPredicateAnalysis(operation);
                }
            }

            // Invocation might invalidate all the analysis data on the invoked instance.
            // Conservatively reset all the instance analysis data.
            ResetInstanceAnalysisData(operation.Instance);

            return value;
        }

        public virtual TAbstractAnalysisValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IInvocationOperation operation, object argument)
        {
            return base.VisitInvocation(operation, argument);
        }

        public virtual TAbstractAnalysisValue VisitInvocation_LambdaOrDelegateOrLocalFunction(IInvocationOperation operation, object argument)
        {
            var value = base.VisitInvocation(operation, argument);

            // Currently, we are not performing flow analysis for invocations of lambda or delegate or local function.
            // Pessimistically assume that all the current state could change and reset all our current analysis data.
            // TODO: Analyze lambda and local functions and flow the values from it's exit block to CurrentAnalysisData.
            // https://github.com/dotnet/roslyn-analyzers/issues/1547
            ResetCurrentAnalysisData();
            return value;
        }

        public override TAbstractAnalysisValue VisitTuple(ITupleOperation operation, object argument)
        {
            // TODO: Handle tuples.
            // https://github.com/dotnet/roslyn-analyzers/issues/1571
            // Until the above is implemented, we pessimistically reset the current state of tuple elements.
            var value = base.VisitTuple(operation, argument);
            CacheAbstractValue(operation, value);
            foreach (var element in operation.Elements)
            {
                SetAbstractValueForAssignment(element, operation, ValueDomain.UnknownOrMayBeValue);
            }
            return value;
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
            // Merge data from explicit throw statements within try that match the caught exception type.
            if (operation.Type != null && AnalysisDataForUnhandledThrowOperations?.Count > 0)
            {
                foreach (ThrowBranchWithExceptionType pendingThrow in AnalysisDataForUnhandledThrowOperations.Keys.ToArray())
                {
                    Debug.Assert(pendingThrow.ExceptionType.DerivesFrom(WellKnownTypeProvider.Exception, baseTypesOnly: true));

                    if (pendingThrow.ExceptionType.DerivesFrom(operation.Type, baseTypesOnly: true))
                    {
                        CurrentAnalysisData = MergeAnalysisData(CurrentAnalysisData, AnalysisDataForUnhandledThrowOperations[pendingThrow]);
                        AnalysisDataForUnhandledThrowOperations.Remove(pendingThrow);
                    }
                }
            }

            return base.VisitCaughtException(operation, argument);
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