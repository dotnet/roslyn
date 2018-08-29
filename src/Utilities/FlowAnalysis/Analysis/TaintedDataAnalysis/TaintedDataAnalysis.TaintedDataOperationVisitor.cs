// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
    using Microsoft.CodeAnalysis.Operations;

    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataOperationVisitor : AnalysisEntityDataFlowOperationVisitor<TaintedDataAnalysisData, TaintedDataAbstractValue>
        {
            public TaintedDataOperationVisitor(
                TaintedDataAbstractValueDomain valueDomain,
                ISymbol owningSymbol,
                WellKnownTypeProvider wellKnownTypeProvider,
                ControlFlowGraph cfg,
                bool pessimisticAnalysis,
                bool predicateAnalysis,
                DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> copyAnalysisResultOpt,
                DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResultOpt) 
                : base(
                      valueDomain, 
                      owningSymbol, 
                      wellKnownTypeProvider, 
                      cfg, 
                      pessimisticAnalysis, 
                      predicateAnalysis, 
                      copyAnalysisResultOpt, 
                      pointsToAnalysisResultOpt)
            {
            }

            protected override TaintedDataAbstractValue ComputeAnalysisValueForReferenceOperation(IOperation operation, TaintedDataAbstractValue defaultValue)
            {
                switch (operation)
                {
                    case IPropertyReferenceOperation propertyReferenceOperation:
                        // TODO: Need a good way to identify sources.
                        // HttpRequest.Form["somestring"]
                        // NameValueCollection blah = HttpWebRequest.Form;
                        // blah["somestring"];
                        if (propertyReferenceOperation.Instance != null
                            && propertyReferenceOperation.Instance.Type == this.WellKnownTypeProvider.HttpRequest
                            && propertyReferenceOperation.Member.MetadataName == "Form")
                        {
                            if (this.AnalysisEntityFactory.TryCreate(propertyReferenceOperation, out AnalysisEntity analysisEntity))
                            {
                               // Remember that the NameValueCollection is tainted.
                               this.SetAbstractValue(analysisEntity, TaintedDataAbstractValue.Tainted);
                            }

                            return TaintedDataAbstractValue.NotTainted;
                        }
                        else if (propertyReferenceOperation.Instance != null
                            && propertyReferenceOperation.Instance.Type == this.WellKnownTypeProvider.NameValueCollection
                            && this.AnalysisEntityFactory.TryCreate(propertyReferenceOperation, out AnalysisEntity analysisEntity)
                            && this.GetAbstractValue(analysisEntity).Kind == TaintedDataAbstractValueKind.Tainted
                            )
                        {
                            return TaintedDataAbstractValue.Tainted;
                        }
                        ////else if (propertyReferenceOperation.Instance != null
                        ////    && propertyReferenceOperation.Instance is IPropertyReferenceOperation instancePropertyReferenceOperation
                        ////    && instancePropertyReferenceOperation.Instance.Type == this.WellKnownTypeProvider.HttpRequest
                        ////    && propertyReferenceOperation.Instance.Type == this.WellKnownTypeProvider.NameValueCollection
                        ////    && instancePropertyReferenceOperation.Member.MetadataName == "Form"
                        ////    && propertyReferenceOperation.Member.MetadataName == "Item")
                        ////    //&& propertyReferenceOperation.Member.Parameters.Length == 1
                        ////    //&& propertyReferenceOperation.Member.Parameters[0].Type == WellKnownTypes.String)
                        ////{
                        ////    return TaintedDataAbstractValue.Tainted;
                        ////}
                        else
                        {
                            return TaintedDataAbstractValue.NotTainted;
                        }

                    default:
                        return TaintedDataAbstractValue.NotTainted;
                }
            }

            protected override void AddTrackedEntities(ImmutableArray<AnalysisEntity>.Builder builder)
            {
                this.CurrentAnalysisData.AddTrackedEntities(builder);
            }

            protected override bool Equals(TaintedDataAnalysisData value1, TaintedDataAnalysisData value2)
            {
                return value1.Equals(value2);
            }

            protected override TaintedDataAbstractValue GetAbstractDefaultValue(ITypeSymbol type)
            {
                return TaintedDataAbstractValue.NotTainted;
            }

            protected override TaintedDataAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
            {
                return this.CurrentAnalysisData.TryGetValue(analysisEntity, out TaintedDataAbstractValue value) ? value : TaintedDataAbstractValue.Unknown;
            }

            protected override TaintedDataAnalysisData GetClonedAnalysisData(TaintedDataAnalysisData analysisData)
            {
                return (TaintedDataAnalysisData) analysisData.Clone();
            }

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
            {
                return this.CurrentAnalysisData.HasAbstractValue(analysisEntity);
            }

            protected override bool HasAnyAbstractValue(TaintedDataAnalysisData data)
            {
                return this.CurrentAnalysisData.HasAnyAbstractValue;
            }

            protected override TaintedDataAnalysisData MergeAnalysisData(TaintedDataAnalysisData value1, TaintedDataAnalysisData value2)
            {
                return TaintedDataAnalysisDomainInstance.Merge(value1, value2);
            }

            protected override void ResetCurrentAnalysisData()
            {
                this.CurrentAnalysisData.Reset(this.ValueDomain.UnknownOrMayBeValue);
            }

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, TaintedDataAbstractValue value)
            {
                if (value.Kind == TaintedDataAbstractValueKind.Tainted 
                    || this.CurrentAnalysisData.CoreAnalysisData.ContainsKey(analysisEntity))
                {
                    // Only track tainted data, or sanitized data.
                    // If it's new, and it's untainted, we don't care.
                    this.CurrentAnalysisData.SetAbstactValue(analysisEntity, value);
                }
            }

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity)
            {
                this.CurrentAnalysisData.RemoveEntries(analysisEntity);
            }
        }
    }
}
