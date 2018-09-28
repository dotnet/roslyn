// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using BinaryFormatterAnalysisData = IDictionary<AbstractLocation, BinaryFormatterAbstractValue>;

    internal partial class BinaryFormatterAnalysis
    {
        /// <summary>
        /// Operation visitor to flow the location validation values across a given statement in a basic block.
        /// </summary>
        private sealed class BinaryFormatterDataFlowOperationVisitor :
            AbstractLocationDataFlowOperationVisitor<BinaryFormatterAnalysisData, BinaryFormatterAnalysisContext, BinaryFormatterAnalysisResult, BinaryFormatterAbstractValue>
        {
            private const int MaxInterproceduralCallChain = 1;
            private readonly ImmutableDictionary<IOperation, BinaryFormatterAbstractValue>.Builder _hazardousUsageBuilderOpt;
            private INamedTypeSymbol DeserializerTypeSymbol;

            public BinaryFormatterDataFlowOperationVisitor(BinaryFormatterAnalysisContext analysisContext)
                : base(analysisContext)
            {
                Debug.Assert(analysisContext.OwningSymbol.Kind == SymbolKind.Method);
                Debug.Assert(analysisContext.PointsToAnalysisResultOpt != null);

                if (analysisContext.TrackHazardousUsages || true)
                {
                    _hazardousUsageBuilderOpt = ImmutableDictionary.CreateBuilder<IOperation, BinaryFormatterAbstractValue>();
                }

                this.DeserializerTypeSymbol = this.WellKnownTypeProvider.BinaryFormatter;
            }

            public override int GetHashCode()
            {
                return HashUtilities.Combine(_hazardousUsageBuilderOpt?.GetHashCode() ?? 0, base.GetHashCode());
            }

            public ImmutableDictionary<IOperation, BinaryFormatterAbstractValue> HazardousUsages
            {
                get
                {
                    Debug.Assert(_hazardousUsageBuilderOpt != null);
                    return _hazardousUsageBuilderOpt.ToImmutable();
                }
            }

            // We only want to track method calls one level down.
            protected override int GetAllowedInterproceduralCallChain() => MaxInterproceduralCallChain;

            protected override BinaryFormatterAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => ValueDomain.Bottom;

            protected override bool HasAnyAbstractValue(BinaryFormatterAnalysisData data) => data.Count > 0;

            protected override BinaryFormatterAbstractValue GetAbstractValue(AbstractLocation location)
                => this.CurrentAnalysisData.TryGetValue(location, out var value) ? value : ValueDomain.Bottom;

            protected override void ResetCurrentAnalysisData() => ResetAnalysisData(CurrentAnalysisData);

            protected override void StopTrackingAbstractValue(AbstractLocation location) => CurrentAnalysisData.Remove(location);

            protected override void SetAbstractValue(AbstractLocation location, BinaryFormatterAbstractValue value)
            {
                if (value != BinaryFormatterAbstractValue.NotApplicable
                    || this.CurrentAnalysisData.ContainsKey(location))
                {
                    this.CurrentAnalysisData[location] = value;
                }
            }


            protected override BinaryFormatterAnalysisData MergeAnalysisData(BinaryFormatterAnalysisData value1, BinaryFormatterAnalysisData value2)
                => BinaryFormatterAnalysisDomainInstance.Merge(value1, value2);
            protected override BinaryFormatterAnalysisData GetClonedAnalysisData(BinaryFormatterAnalysisData analysisData)
                => GetClonedAnalysisDataHelper(analysisData);
            protected override BinaryFormatterAnalysisData GetEmptyAnalysisData()
                => GetEmptyAnalysisDataHelper();
            protected override BinaryFormatterAnalysisData GetAnalysisDataAtBlockEnd(BinaryFormatterAnalysisResult analysisResult, BasicBlock block)
                => GetClonedAnalysisDataHelper(analysisResult[block].OutputData);
            protected override bool Equals(BinaryFormatterAnalysisData value1, BinaryFormatterAnalysisData value2)
                => EqualsHelper(value1, value2);

            protected override void SetValueForParameterPointsToLocationOnEntry(IParameterSymbol parameter, PointsToAbstractValue pointsToAbstractValue)
            {
            }

            protected override void EscapeValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, PointsToAbstractValue pointsToAbstractValue)
            {
            }

            protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, BinaryFormatterAbstractValue value)
            {
            }

            protected override void SetAbstractValueForAssignment(IOperation target, IOperation assignedValueOperation, BinaryFormatterAbstractValue assignedValue, bool mayBeAssignment = false)
            {
            }

            public override BinaryFormatterAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
            {
                var value = base.VisitObjectCreation(operation, argument);
                if (operation.Type != null && operation.Type == this.DeserializerTypeSymbol)
                {
                    PointsToAbstractValue pointsToAbstractValue = this.GetPointsToAbstractValue(operation);
                    this.SetAbstractValue(pointsToAbstractValue, BinaryFormatterAbstractValue.Flagged);
                    return BinaryFormatterAbstractValue.Flagged;
                }

                return value;
            }

            protected override BinaryFormatterAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object argument)
            {
                BinaryFormatterAbstractValue baseValue = base.VisitAssignmentOperation(operation, argument);
                if (operation.Target is IPropertyReferenceOperation propertyReferenceOperation
                    && propertyReferenceOperation.Property.MatchPropertyByName(this.DeserializerTypeSymbol, "Binder"))
                {
                    PointsToAbstractValue pointsToAbstractValue = GetPointsToAbstractValue(propertyReferenceOperation.Instance);
                    NullAbstractValue nullAbstractValue = this.GetNullAbstractValue(operation.Value);
                    if (nullAbstractValue == NullAbstractValue.Null)
                    {
                        this.SetAbstractValue(pointsToAbstractValue, BinaryFormatterAbstractValue.Flagged);
                    }
                    else if (nullAbstractValue == NullAbstractValue.NotNull)
                    {
                        this.SetAbstractValue(pointsToAbstractValue, BinaryFormatterAbstractValue.Unflagged);
                    }
                    else
                    {
                        this.SetAbstractValue(pointsToAbstractValue, BinaryFormatterAbstractValue.MaybeFlagged);
                    }
                }

                return baseValue;
            }

            public override BinaryFormatterAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IMethodSymbol method, IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments, bool invokedAsDelegate, IOperation originalOperation, BinaryFormatterAbstractValue defaultValue)
            {
                BinaryFormatterAbstractValue baseValue = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);
                if (this._hazardousUsageBuilderOpt != null
                    && visitedInstance != null
                    && visitedInstance.Type != null
                    && visitedInstance.Type == this.DeserializerTypeSymbol
                    && method.MetadataName == "Deserialize")
                {
                    PointsToAbstractValue pointsToAbstractValue = this.GetPointsToAbstractValue(visitedInstance);
                    bool hasFlagged = false;
                    bool hasMaybeFlagged = false;
                    foreach (AbstractLocation location in pointsToAbstractValue.Locations)
                    {
                        BinaryFormatterAbstractValue locationAbstractValue = this.GetAbstractValue(location);
                        if (locationAbstractValue == BinaryFormatterAbstractValue.Flagged)
                        {
                            hasFlagged = true;
                        }
                        else if (locationAbstractValue == BinaryFormatterAbstractValue.MaybeFlagged)
                        {
                            hasMaybeFlagged = true;
                        }
                    }


                    if (hasFlagged && !hasMaybeFlagged)
                    {
                        this._hazardousUsageBuilderOpt.Add(originalOperation, BinaryFormatterAbstractValue.Flagged);
                    }
                    else if (hasFlagged || hasMaybeFlagged)
                    {
                        this._hazardousUsageBuilderOpt.Add(originalOperation, BinaryFormatterAbstractValue.MaybeFlagged);
                    }
                }

                return baseValue;
            }

        }
    }
}
