// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public abstract class AbstractLocationDataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : class, IDataFlowAnalysisResult<TAbstractAnalysisValue>
    {
        protected AbstractLocationDataFlowOperationVisitor(TAnalysisContext analysisContext)
            : base(analysisContext)
        {
            Debug.Assert(analysisContext.PointsToAnalysisResult != null);
        }

        protected abstract TAbstractAnalysisValue GetAbstractValue(AbstractLocation location);
        protected abstract void SetAbstractValue(AbstractLocation location, TAbstractAnalysisValue value);
        protected void SetAbstractValue(PointsToAbstractValue instanceLocation, TAbstractAnalysisValue value)
            => SetAbstractValue(instanceLocation.Locations, value);
        protected void SetAbstractValue(IEnumerable<AbstractLocation> locations, TAbstractAnalysisValue value)
        {
            foreach (var location in locations)
            {
                SetAbstractValue(location, value);
            }
        }

        protected abstract void StopTrackingAbstractValue(AbstractLocation location);
        protected override void StopTrackingDataForParameter(IParameterSymbol parameter, AnalysisEntity analysisEntity)
        {
            Debug.Assert(DataFlowAnalysisContext.InterproceduralAnalysisData != null);
            if (parameter.RefKind == RefKind.None)
            {
                foreach (var location in analysisEntity.InstanceLocation.Locations)
                {
                    StopTrackingAbstractValue(location);
                }
            }
        }

        protected override void ResetValueTypeInstanceAnalysisData(AnalysisEntity analysisEntity)
        {
        }

        protected override void ResetReferenceTypeInstanceAnalysisData(PointsToAbstractValue pointsToAbstractValue)
        {
        }

        protected virtual TAbstractAnalysisValue HandleInstanceCreation(IOperation creation, PointsToAbstractValue instanceLocation, TAbstractAnalysisValue defaultValue)
        {
            SetAbstractValue(instanceLocation, defaultValue);
            return defaultValue;
        }

        protected override TAbstractAnalysisValue ComputeAnalysisValueForEscapedRefOrOutArgument(IArgumentOperation operation, TAbstractAnalysisValue defaultValue)
        {
            Debug.Assert(operation.Parameter!.RefKind is RefKind.Ref or RefKind.Out);

            if (operation.Value.Type != null)
            {
                PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
                var value = HandleInstanceCreation(operation.Value, instanceLocation, defaultValue);
                if (operation.Parameter.RefKind == RefKind.Ref)
                {
                    // Escaped ref argument must be set to unknown value.
                    SetAbstractValue(instanceLocation, ValueDomain.UnknownOrMayBeValue);
                    return defaultValue;
                }
                else
                {
                    // Escaped out argument is caller's responsibility and must not be set to unknown value.
                    return value;
                }
            }

            return defaultValue;
        }

        protected abstract void SetValueForParameterPointsToLocationOnEntry(IParameterSymbol parameter, PointsToAbstractValue pointsToAbstractValue);
        protected abstract void EscapeValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, ImmutableHashSet<AbstractLocation> escapedLocations);

        protected override void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity, ArgumentInfo<TAbstractAnalysisValue>? assignedValue)
        {
            // Only set the value for non-interprocedural case.
            // For interprocedural case, we have already initialized values for the underlying locations
            // of arguments from the input analysis data.
            Debug.Assert(Equals(analysisEntity.Symbol, parameter));
            if (DataFlowAnalysisContext.InterproceduralAnalysisData == null &&
                TryGetPointsToAbstractValueAtEntryBlockEnd(analysisEntity, out var pointsToAbstractValue))
            {
                SetValueForParameterPointsToLocationOnEntry(parameter, pointsToAbstractValue);
            }
        }

        protected override void EscapeValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity)
        {
            Debug.Assert(SymbolEqualityComparer.Default.Equals(analysisEntity.Symbol, parameter));
            var escapedLocationsForParameter = GetEscapedLocations(analysisEntity);
            if (!escapedLocationsForParameter.IsEmpty)
            {
                EscapeValueForParameterPointsToLocationOnExit(parameter, analysisEntity, escapedLocationsForParameter);
            }
        }

        /// <summary>
        /// Helper method to reset analysis data for analysis locations.
        /// </summary>
        protected void ResetAnalysisData(DictionaryAnalysisData<AbstractLocation, TAbstractAnalysisValue> currentAnalysisData)
        {
            // Reset the current analysis data, while ensuring that we don't violate the monotonicity, i.e. we cannot remove any existing key from currentAnalysisData.
            // Just set the values for existing keys to ValueDomain.UnknownOrMayBeValue.
            var keys = currentAnalysisData.Keys.ToImmutableArray();
            foreach (var key in keys)
            {
                SetAbstractValue(key, ValueDomain.UnknownOrMayBeValue);
            }
        }

        protected static DictionaryAnalysisData<AbstractLocation, TAbstractAnalysisValue> GetClonedAnalysisDataHelper(IDictionary<AbstractLocation, TAbstractAnalysisValue> analysisData)
            => [.. analysisData];
        protected static DictionaryAnalysisData<AbstractLocation, TAbstractAnalysisValue> GetEmptyAnalysisDataHelper()
            => GetClonedAnalysisDataHelper(ImmutableDictionary<AbstractLocation, TAbstractAnalysisValue>.Empty);

        protected void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(
            DictionaryAnalysisData<AbstractLocation, TAbstractAnalysisValue> coreDataAtException,
            DictionaryAnalysisData<AbstractLocation, TAbstractAnalysisValue> coreCurrentAnalysisData)
        {
            base.ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(coreDataAtException, coreCurrentAnalysisData, predicate: null);
        }

        #region Visitor methods

        public override TAbstractAnalysisValue VisitObjectCreation(IObjectCreationOperation operation, object? argument)
        {
            var value = base.VisitObjectCreation(operation, argument)!;
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, object? argument)
        {
            var value = base.VisitTypeParameterObjectCreation(operation, argument)!;
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, object? argument)
        {
            var value = base.VisitDynamicObjectCreation(operation, argument)!;
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object? argument)
        {
            var value = base.VisitAnonymousObjectCreation(operation, argument)!;
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitArrayCreation(IArrayCreationOperation operation, object? argument)
        {
            var value = base.VisitArrayCreation(operation, argument)!;
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitDelegateCreation(IDelegateCreationOperation operation, object? argument)
        {
            var value = base.VisitDelegateCreation(operation, argument)!;
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation, instanceLocation, value);
        }

        public override TAbstractAnalysisValue VisitReDimClause(IReDimClauseOperation operation, object? argument)
        {
            var value = base.VisitReDimClause(operation, argument)!;
            PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(operation);
            return HandleInstanceCreation(operation, instanceLocation, value);
        }

        #endregion
    }
}
