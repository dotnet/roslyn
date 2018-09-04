// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Operation visitor to flow the abstract dataflow analysis values for <see cref="AbstractLocation"/>s across a given statement in a basic block.
    /// </summary>
    internal abstract class AbstractLocationDataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : IDataFlowAnalysisResult
    {
        protected AbstractLocationDataFlowOperationVisitor(TAnalysisContext analysisContext)
            : base(analysisContext)
        {
            Debug.Assert(analysisContext.PointsToAnalysisResultOpt != null);
        }

        protected abstract TAbstractAnalysisValue GetAbstractValue(AbstractLocation location);
        protected abstract void SetAbstractValue(AbstractLocation location, TAbstractAnalysisValue value);
        protected virtual void SetAbstractValue(PointsToAbstractValue instanceLocation, TAbstractAnalysisValue value)
        {
            foreach (var location in instanceLocation.Locations)
            {
                SetAbstractValue(location, value);
            }
        }

        protected override void ResetValueTypeInstanceAnalysisData(AnalysisEntity analysisEntity)
        {
        }

        protected override void ResetReferenceTypeInstanceAnalysisData(PointsToAbstractValue pointsToAbstractValue)
        {
        }

        protected virtual TAbstractAnalysisValue HandleInstanceCreation(ITypeSymbol instanceType, PointsToAbstractValue instanceLocation, TAbstractAnalysisValue defaultValue)
        {
            SetAbstractValue(instanceLocation, defaultValue);
            return defaultValue;
        }

        protected override TAbstractAnalysisValue ComputeAnalysisValueForOutArgument(IArgumentOperation operation, TAbstractAnalysisValue defaultValue)
        {
            if (operation.Value.Type != null)
            {
                PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
                return HandleInstanceCreation(operation.Value.Type, instanceLocation, defaultValue);
            }

            return defaultValue;
        }

        protected abstract void SetValueForParameterPointsToLocationOnEntry(IParameterSymbol parameter, PointsToAbstractValue pointsToAbstractValue);
        protected abstract void SetValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, PointsToAbstractValue pointsToAbstractValue);

        protected override void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity)
        {
            Debug.Assert(analysisEntity.SymbolOpt == parameter);
            if (TryGetPointsToAbstractValueAtCurrentBlockExit(analysisEntity, out PointsToAbstractValue pointsToAbstractValue))
            {
                SetValueForParameterPointsToLocationOnEntry(parameter, pointsToAbstractValue);
            }
        }

        protected override void SetValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity)
        {
            Debug.Assert(analysisEntity.SymbolOpt == parameter);
            if (TryGetPointsToAbstractValueAtCurrentBlockEntry(analysisEntity, out PointsToAbstractValue pointsToAbstractValue))
            {
                SetValueForParameterPointsToLocationOnExit(parameter, analysisEntity, pointsToAbstractValue);
            }
        }

        /// <summary>
        /// Helper method to reset analysis data for analysis locations.
        /// </summary>
        protected void ResetAnalysisData(IDictionary<AbstractLocation, TAbstractAnalysisValue> currentAnalysisDataOpt)
        {
            // Reset the current analysis data, while ensuring that we don't violate the monotonicity, i.e. we cannot remove any existing key from currentAnalysisData.
            // Just set the values for existing keys to ValueDomain.UnknownOrMayBeValue.
            var keys = currentAnalysisDataOpt?.Keys.ToImmutableArray();
            foreach (var key in keys)
            {
                SetAbstractValue(key, ValueDomain.UnknownOrMayBeValue);
            }
        }

        protected IDictionary<AbstractLocation, TAbstractAnalysisValue> GetClonedAnalysisDataHelper(IDictionary<AbstractLocation, TAbstractAnalysisValue> analysisData)
            => new Dictionary<AbstractLocation, TAbstractAnalysisValue>(analysisData);

        #region Visitor methods

        public override TAbstractAnalysisValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
        {
            var value = base.VisitObjectCreation(operation, argument);
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation.Type, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, object argument)
        {
            var value = base.VisitTypeParameterObjectCreation(operation, argument);
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation.Type, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, object argument)
        {
            var value = base.VisitDynamicObjectCreation(operation, argument);
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation.Type, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object argument)
        {
            var value = base.VisitAnonymousObjectCreation(operation, argument);
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation.Type, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitArrayCreation(IArrayCreationOperation operation, object argument)
        {
            var value = base.VisitArrayCreation(operation, argument);
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation.Type, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitDelegateCreation(IDelegateCreationOperation operation, object argument)
        {
            var value = base.VisitDelegateCreation(operation, argument);
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation.Type, instanceLocation, value);
        }

        #endregion
    }
}
