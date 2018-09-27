// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    internal partial class BinaryFormatterAnalysis
    {
        private sealed class BinaryFormatterOperationVisitor : AnalysisEntityDataFlowOperationVisitor<BinaryFormatterAnalysisData, BinaryFormatterAnalysisContext, BinaryFormatterAnalysisResult, BinaryFormatterAbstractValue>
        {
            private Dictionary<IOperation, BinaryFormatterAbstractValue> HazardousUsages;

            public BinaryFormatterOperationVisitor(BinaryFormatterAnalysisContext analysisContext)
                : base(analysisContext)
            {
                this.DeserializerTypeSymbol = this.WellKnownTypeProvider.BinaryFormatter;
                if (analysisContext.TrackHazardousUsages)
                {
                    this.HazardousUsages = new Dictionary<IOperation, BinaryFormatterAbstractValue>();
                }
            }

            private INamedTypeSymbol DeserializerTypeSymbol { get; }

            public ImmutableDictionary<IOperation, BinaryFormatterAbstractValue> GetHazardousUsages()
            {
                if (this.HazardousUsages == null)
                {
                    throw new InvalidOperationException($"{nameof(BinaryFormatterAnalysisContext)}.{nameof(BinaryFormatterAnalysisContext.TrackHazardousUsages)} was not specified");
                }

                return this.HazardousUsages.ToImmutableDictionary();
            }

            protected override void AddTrackedEntities(ImmutableArray<AnalysisEntity>.Builder builder)
            {
                this.CurrentAnalysisData.AddTrackedEntities(builder);
            }

            protected override bool Equals(BinaryFormatterAnalysisData value1, BinaryFormatterAnalysisData value2)
            {
                return value1.Equals(value2);
            }

            protected override BinaryFormatterAbstractValue GetAbstractDefaultValue(ITypeSymbol type)
            {
                return BinaryFormatterAbstractValue.Unflagged;
            }

            protected override BinaryFormatterAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
            {
                return
                    this.CurrentAnalysisData.TryGetValue(analysisEntity, out BinaryFormatterAbstractValue value)
                    ? value 
                    : BinaryFormatterAbstractValue.Unflagged;
            }

            protected override BinaryFormatterAnalysisData GetClonedAnalysisData(BinaryFormatterAnalysisData analysisData)
            {
                return (BinaryFormatterAnalysisData)analysisData.Clone();
            }

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
            {
                return this.CurrentAnalysisData.HasAbstractValue(analysisEntity);
            }

            protected override bool HasAnyAbstractValue(BinaryFormatterAnalysisData data)
            {
                return this.CurrentAnalysisData.HasAnyAbstractValue;
            }

            protected override BinaryFormatterAnalysisData MergeAnalysisData(BinaryFormatterAnalysisData value1, BinaryFormatterAnalysisData value2)
            {
                return BinaryFormatterAnalysisDomainInstance.Merge(value1, value2);
            }

            protected override void ResetCurrentAnalysisData()
            {
                this.CurrentAnalysisData.Reset(this.ValueDomain.UnknownOrMayBeValue);
            }

            protected override BinaryFormatterAnalysisData GetEmptyAnalysisData()
            {
                return new BinaryFormatterAnalysisData();
            }

            protected override BinaryFormatterAnalysisData GetAnalysisDataAtBlockEnd(BinaryFormatterAnalysisResult analysisResult, BasicBlock block)
            {
                return new BinaryFormatterAnalysisData(analysisResult[block].OutputData);
            }

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, BinaryFormatterAbstractValue value)
            {
                if (value != BinaryFormatterAbstractValue.Unknown
                    || this.CurrentAnalysisData.CoreAnalysisData.ContainsKey(analysisEntity))
                {
                    // Only known values.
                    // If it's new, and it's unknown, we don't care.
                    this.CurrentAnalysisData.SetAbstactValue(analysisEntity, value);
                }
            }

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity)
            {
                this.CurrentAnalysisData.RemoveEntries(analysisEntity);
            }

            // So we can hook into constructor calls.
            public override BinaryFormatterAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
            {
                var value = base.VisitObjectCreation(operation, argument);
                if (operation.Type != null && operation.Type == this.DeserializerTypeSymbol)
                {
                    if (this.AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
                    {
                        this.SetAbstractValue(analysisEntity, BinaryFormatterAbstractValue.Flagged);
                    }

                    return BinaryFormatterAbstractValue.Flagged;
                }
                
                return value;
            }

            public override BinaryFormatterAbstractValue VisitPropertyReference(IPropertyReferenceOperation operation, object argument)
            {
                BinaryFormatterAbstractValue baseValue = base.VisitPropertyReference(operation, argument);

                if (operation.Property.MatchPropertyByName(this.DeserializerTypeSymbol, "Binder")
                    && operation.Parent is IAssignmentOperation assignmentOperation
                    && assignmentOperation.Target == operation
                    && operation.Instance != null
                    && this.AnalysisEntityFactory.TryCreate(operation.Instance, out AnalysisEntity analysisEntity))
                {
                    if (assignmentOperation.Value.HasNullConstantValue())
                    {
                        this.SetAbstractValue(analysisEntity, BinaryFormatterAbstractValue.Flagged);
                    }
                    else
                    {
                        // Perhaps we could be smarter with ValueContentAnalysis.
                        this.SetAbstractValue(analysisEntity, BinaryFormatterAbstractValue.Unflagged);
                    }
                }

                return baseValue;
            }

            public override BinaryFormatterAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IMethodSymbol method, IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments, bool invokedAsDelegate, IOperation originalOperation, BinaryFormatterAbstractValue defaultValue)
            {
                BinaryFormatterAbstractValue baseValue = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);
                if (this.HazardousUsages != null
                    && visitedInstance != null
                    && visitedInstance.Type != null
                    && visitedInstance.Type == this.DeserializerTypeSymbol
                    && method.MetadataName == "Deserialize"
                    && this.AnalysisEntityFactory.TryCreate(visitedInstance, out AnalysisEntity instanceAnalysisEntity))
                {
                    BinaryFormatterAbstractValue instanceAbstractValue = this.GetAbstractValue(instanceAnalysisEntity);
                    if (instanceAbstractValue == BinaryFormatterAbstractValue.Flagged
                        || instanceAbstractValue == BinaryFormatterAbstractValue.MaybeFlagged)
                    {
                        this.HazardousUsages[originalOperation] = instanceAbstractValue;
                    }
                }

                return baseValue;
            }
        }
    }
}
