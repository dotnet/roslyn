// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Contains the caller's analysis context data passed to context sensitive interprocedural analysis, <see cref="InterproceduralAnalysisKind.ContextSensitive"/>.
    /// This includes the following:
    /// 1. Caller's tracked analysis data at the call site, which is the initial analysis data for callee.
    /// 2. Information about the invocation instance on which the method is invoked.
    /// 3. Information about arguments for the invocation.
    /// 4. Captured variables (for lambda/local function invocations).
    /// 5. Information about ref/out parameter entities that share address with callee's parameters.
    /// 6. Operation call stack for the current interprocedural analysis.
    /// 7. Set of analysis contexts currently being analyzed.
    /// </summary>
    public sealed class InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>
        : CacheBasedEquatable<InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>>
        where TAnalysisContext : class, IDataFlowAnalysisContext
        where TAnalysisData : AbstractAnalysisData
    {
        public InterproceduralAnalysisData(
            TAnalysisData? initialAnalysisData,
            (AnalysisEntity?, PointsToAbstractValue)? invocationInstance,
            (AnalysisEntity, PointsToAbstractValue)? thisOrMeInstanceForCaller,
            ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>> argumentValuesMap,
            ImmutableDictionary<ISymbol, PointsToAbstractValue> capturedVariablesMap,
            ImmutableDictionary<AnalysisEntity, CopyAbstractValue> addressSharedEntities,
            ImmutableStack<IOperation> callStack,
            ImmutableHashSet<TAnalysisContext> methodsBeingAnalyzed,
            Func<IOperation, TAbstractAnalysisValue> getCachedAbstractValueFromCaller,
            Func<IMethodSymbol, ControlFlowGraph?> getInterproceduralControlFlowGraph,
            Func<IOperation, AnalysisEntity?> getAnalysisEntityForFlowCapture,
            Func<ISymbol, ImmutableStack<IOperation>?> getInterproceduralCallStackForOwningSymbol)
        {
            InitialAnalysisData = initialAnalysisData;
            InvocationInstance = invocationInstance;
            ThisOrMeInstanceForCaller = thisOrMeInstanceForCaller;
            ArgumentValuesMap = argumentValuesMap;
            CapturedVariablesMap = capturedVariablesMap;
            AddressSharedEntities = addressSharedEntities;
            CallStack = callStack;
            MethodsBeingAnalyzed = methodsBeingAnalyzed;
            GetCachedAbstractValueFromCaller = getCachedAbstractValueFromCaller;
            GetInterproceduralControlFlowGraph = getInterproceduralControlFlowGraph;
            GetAnalysisEntityForFlowCapture = getAnalysisEntityForFlowCapture;
            GetInterproceduralCallStackForOwningSymbol = getInterproceduralCallStackForOwningSymbol;
        }

        public TAnalysisData? InitialAnalysisData { get; }
        public (AnalysisEntity? Instance, PointsToAbstractValue PointsToValue)? InvocationInstance { get; }
        public (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? ThisOrMeInstanceForCaller { get; }
        public ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>> ArgumentValuesMap { get; }
        public ImmutableDictionary<ISymbol, PointsToAbstractValue> CapturedVariablesMap { get; }
        public ImmutableDictionary<AnalysisEntity, CopyAbstractValue> AddressSharedEntities { get; }
        public ImmutableStack<IOperation> CallStack { get; }
        public ImmutableHashSet<TAnalysisContext> MethodsBeingAnalyzed { get; }
        public Func<IOperation, TAbstractAnalysisValue> GetCachedAbstractValueFromCaller { get; }
        public Func<IMethodSymbol, ControlFlowGraph?> GetInterproceduralControlFlowGraph { get; }
        public Func<IOperation, AnalysisEntity?> GetAnalysisEntityForFlowCapture { get; }
        public Func<ISymbol, ImmutableStack<IOperation>?> GetInterproceduralCallStackForOwningSymbol { get; }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(InitialAnalysisData.GetHashCodeOrDefault());
            AddHashCodeParts(InvocationInstance, ref hashCode);
            AddHashCodeParts(ThisOrMeInstanceForCaller, ref hashCode);
            hashCode.Add(HashUtilities.Combine(ArgumentValuesMap));
            hashCode.Add(HashUtilities.Combine(CapturedVariablesMap));
            hashCode.Add(HashUtilities.Combine(AddressSharedEntities));
            hashCode.Add(HashUtilities.Combine(CallStack));
            hashCode.Add(HashUtilities.Combine(MethodsBeingAnalyzed));
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>> obj)
        {
            var other = (InterproceduralAnalysisData<TAnalysisData, TAnalysisContext, TAbstractAnalysisValue>)obj;
            return InitialAnalysisData.GetHashCodeOrDefault() == other.InitialAnalysisData.GetHashCodeOrDefault()
                && EqualsByHashCodeParts(InvocationInstance, other.InvocationInstance)
                && EqualsByHashCodeParts(ThisOrMeInstanceForCaller, other.ThisOrMeInstanceForCaller)
                && HashUtilities.Combine(ArgumentValuesMap) == HashUtilities.Combine(other.ArgumentValuesMap)
                && HashUtilities.Combine(CapturedVariablesMap) == HashUtilities.Combine(other.CapturedVariablesMap)
                && HashUtilities.Combine(AddressSharedEntities) == HashUtilities.Combine(other.AddressSharedEntities)
                && HashUtilities.Combine(CallStack) == HashUtilities.Combine(other.CallStack)
                && HashUtilities.Combine(MethodsBeingAnalyzed) == HashUtilities.Combine(other.MethodsBeingAnalyzed);
        }

        private static void AddHashCodeParts(
            (AnalysisEntity? Instance, PointsToAbstractValue PointsToValue)? instanceAndPointsToValue,
            ref RoslynHashCode hashCode)
        {
            if (instanceAndPointsToValue.HasValue)
            {
                hashCode.Add(instanceAndPointsToValue.Value.Instance.GetHashCodeOrDefault());
                hashCode.Add(instanceAndPointsToValue.Value.PointsToValue.GetHashCode());
            }
            else
            {
                hashCode.Add(0);
            }
        }

        private static bool EqualsByHashCodeParts(
            (AnalysisEntity? Instance, PointsToAbstractValue PointsToValue)? left,
            (AnalysisEntity? Instance, PointsToAbstractValue PointsToValue)? right)
        {
            if (left is null)
                return right is null;
            else if (right is null)
                return false;

            return left.Value.Instance.GetHashCodeOrDefault() == right.Value.Instance.GetHashCodeOrDefault()
                && left.Value.PointsToValue.GetHashCode() == right.Value.PointsToValue.GetHashCode();
        }
    }
}
