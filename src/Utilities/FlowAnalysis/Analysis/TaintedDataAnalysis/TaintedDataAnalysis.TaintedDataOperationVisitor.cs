// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using TaintedDataAnalysisResult = DataFlowAnalysisResult<TaintedDataBlockAnalysisResult, TaintedDataAbstractValue>;

    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataOperationVisitor : AnalysisEntityDataFlowOperationVisitor<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataAbstractValue>
        {
            public TaintedDataOperationVisitor(TaintedDataAnalysisContext analysisContext)
                : base(analysisContext)
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
                            && (propertyReferenceOperation.Member.MetadataName == "Form"
                                || propertyReferenceOperation.Member.MetadataName == "UserLanguages"))
                        {
                            // HttpRequest.Form
                            return TaintedDataAbstractValue.Tainted;
                        }
                        else if (propertyReferenceOperation.Instance != null
                            && propertyReferenceOperation.Instance.Type == this.WellKnownTypeProvider.NameValueCollection
                            && this.GetCachedAbstractValue(propertyReferenceOperation.Instance).Kind == TaintedDataAbstractValueKind.Tainted)
                        {
                            // propertyReferenceOperation.Instance is a NameValueCollection from an HttpRequest.Form
                            return TaintedDataAbstractValue.Tainted;
                        }

                        break;

                    case IArrayElementReferenceOperation arrayElementOperation:
                        if (arrayElementOperation.ArrayReference.Type is IArrayTypeSymbol arrayTypeSymbol
                            && arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_String
                            && this.GetCachedAbstractValue(arrayElementOperation.ArrayReference).Kind == TaintedDataAbstractValueKind.Tainted)
                        {
                            return TaintedDataAbstractValue.Tainted;
                        }

                        break;
                }

                if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
                {
                    return this.CurrentAnalysisData.TryGetValue(analysisEntity, out TaintedDataAbstractValue value) ? value : defaultValue;
                }
                else
                {
                    return defaultValue;
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

            protected override TaintedDataAnalysisData GetEmptyAnalysisData()
            {
                return new TaintedDataAnalysisData();
            }

            protected override TaintedDataAnalysisData GetAnalysisDataAtBlockEnd(TaintedDataAnalysisResult analysisResult, BasicBlock block)
            {
                return new TaintedDataAnalysisData(analysisResult[block].OutputData);
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

            public override TaintedDataAbstractValue VisitBinaryOperatorCore(IBinaryOperation operation, object argument)
            {
                TaintedDataAbstractValue leftAbstractValue = Visit(operation.LeftOperand, argument);
                TaintedDataAbstractValue rightAbstractValue = Visit(operation.RightOperand, argument);

                return TaintedDataAbstractValueDomain.Default.Merge(leftAbstractValue, rightAbstractValue);
            }
        }
    }
}
