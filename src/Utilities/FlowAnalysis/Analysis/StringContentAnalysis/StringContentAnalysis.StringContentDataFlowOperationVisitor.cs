// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.StringContentAnalysis
{
    internal partial class StringContentAnalysis : ForwardDataFlowAnalysis<StringContentAnalysisData, StringContentBlockAnalysisResult, StringContentAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the string content values across a given statement in a basic block.
        /// </summary>
        private sealed class StringContentDataFlowOperationVisitor : AnalysisEntityDataFlowOperationVisitor<StringContentAnalysisData, StringContentAbstractValue>
        {
            public StringContentDataFlowOperationVisitor(
                StringContentAbstractValueDomain valueDomain,
                ISymbol owningSymbol,
                WellKnownTypeProvider wellKnownTypeProvider,
                ControlFlowGraph cfg,
                bool pessimisticAnalysis,
                DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> copyAnalysisResultOpt,
                DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResultOpt)
                : base(valueDomain, owningSymbol, wellKnownTypeProvider, cfg, pessimisticAnalysis,
                      predicateAnalysis: true, copyAnalysisResultOpt: copyAnalysisResultOpt, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt)
            {
            }

            protected override void AddTrackedEntities(ImmutableArray<AnalysisEntity>.Builder builder) => builder.AddRange(CurrentAnalysisData.CoreAnalysisData.Keys);

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, StringContentAbstractValue value) => SetAbstractValue(CurrentAnalysisData, analysisEntity, value);

            private static void SetAbstractValue(StringContentAnalysisData analysisData, AnalysisEntity analysisEntity, StringContentAbstractValue value)
            {
                // PERF: Avoid creating an entry if the value is the default unknown value.
                if (value == StringContentAbstractValue.MayBeContainsNonLiteralState &&
                    !analysisData.HasAbstractValue(analysisEntity))
                {
                    return;
                }

                analysisData.SetAbstactValue(analysisEntity, value);
            }

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.HasAbstractValue(analysisEntity);

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity) => CurrentAnalysisData.RemoveEntries(analysisEntity);

            protected override StringContentAbstractValue GetAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

            protected override StringContentAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => StringContentAbstractValue.DoesNotContainLiteralOrNonLiteralState;

            protected override bool HasAnyAbstractValue(StringContentAnalysisData data) => data.HasAnyAbstractValue;

            protected override void ResetCurrentAnalysisData() => CurrentAnalysisData.Reset(ValueDomain.UnknownOrMayBeValue);

            #region Predicate analysis
            protected override StringContentAnalysisData GetEmptyAnalysisDataForPredicateAnalysis() => new StringContentAnalysisData();

            protected override PredicateValueKind SetValueForIsNullComparisonOperator(IOperation leftOperand, bool equals, StringContentAnalysisData targetAnalysisData) => PredicateValueKind.Unknown;

            protected override PredicateValueKind SetValueForEqualsOrNotEqualsComparisonOperator(
                IOperation leftOperand,
                IOperation rightOperand,
                bool equals,
                bool isReferenceEquality,
                StringContentAnalysisData targetAnalysisData)
            {
                var predicateValueKind = PredicateValueKind.Unknown;

                // Handle 'a == "SomeString"' and 'a != "SomeString"'
                SetValueForComparisonOperator(leftOperand, rightOperand, equals, ref predicateValueKind, targetAnalysisData);

                // Handle '"SomeString" == a' and '"SomeString" != a'
                SetValueForComparisonOperator(rightOperand, leftOperand, equals, ref predicateValueKind, targetAnalysisData);

                return predicateValueKind;
            }

            private void SetValueForComparisonOperator(IOperation target, IOperation assignedValue, bool equals, ref PredicateValueKind predicateValueKind, StringContentAnalysisData targetAnalysisData)
            {
                StringContentAbstractValue stringContentValue = GetCachedAbstractValue(assignedValue);
                if (stringContentValue.IsLiteralState &&
                    AnalysisEntityFactory.TryCreate(target, out AnalysisEntity targetEntity))
                {
                    if (CurrentAnalysisData.TryGetValue(targetEntity, out StringContentAbstractValue existingValue) &&
                        existingValue.IsLiteralState)
                    {
                        var newStringContentValue = stringContentValue.IntersectLiteralValues(existingValue);
                        if (newStringContentValue.NonLiteralState == StringContainsNonLiteralState.Invalid)
                        {
                            predicateValueKind = equals ? PredicateValueKind.AlwaysFalse : PredicateValueKind.AlwaysTrue;
                        }
                        else if (predicateValueKind != PredicateValueKind.AlwaysFalse &&
                            newStringContentValue.IsLiteralState &&
                            newStringContentValue.LiteralValues.Count == 1 &&
                            stringContentValue.LiteralValues.Count == 1 &&
                            existingValue.LiteralValues.Count == 1)
                        {
                            predicateValueKind = equals ? PredicateValueKind.AlwaysTrue : PredicateValueKind.AlwaysFalse;
                        }

                        stringContentValue = newStringContentValue;
                    }

                    if (equals)
                    {
                        CopyAbstractValue copyValue = GetCopyAbstractValue(target);
                        if (copyValue.Kind == CopyAbstractValueKind.Known)
                        {
                            Debug.Assert(copyValue.AnalysisEntities.Contains(targetEntity));
                            foreach (var analysisEntity in copyValue.AnalysisEntities)
                            {
                                SetAbstractValue(targetAnalysisData, analysisEntity, stringContentValue);
                            }
                        }
                        else
                        {
                            SetAbstractValue(targetAnalysisData, targetEntity, stringContentValue);
                        }
                    }
                }
            }

            #endregion

            protected override StringContentAnalysisData MergeAnalysisData(StringContentAnalysisData value1, StringContentAnalysisData value2)
                => s_AnalysisDomain.Merge(value1, value2);
            protected override StringContentAnalysisData GetClonedAnalysisData(StringContentAnalysisData analysisData)
                => (StringContentAnalysisData)analysisData.Clone();
            protected override bool Equals(StringContentAnalysisData value1, StringContentAnalysisData value2)
                => value1.Equals(value2);

            #region Visitor methods
            public override StringContentAbstractValue DefaultVisit(IOperation operation, object argument)
            {
                var _ = base.DefaultVisit(operation, argument);
                if (operation.Type == null)
                {
                    return StringContentAbstractValue.DoesNotContainLiteralOrNonLiteralState;
                }

                if (operation.Type.SpecialType == SpecialType.System_String)
                {
                    if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is string value)
                    {
                        return StringContentAbstractValue.Create(value);
                    }
                    else
                    {
                        switch (GetNullAbstractValue(operation))
                        {
                            case PointsToAnalysis.NullAbstractValue.Invalid:
                                return StringContentAbstractValue.InvalidState;

                            case PointsToAnalysis.NullAbstractValue.Null:
                                return StringContentAbstractValue.DoesNotContainLiteralOrNonLiteralState;

                            default:
                                return StringContentAbstractValue.MayBeContainsNonLiteralState;
                        }
                    }
                }

                return ValueDomain.UnknownOrMayBeValue;
            }

            public override StringContentAbstractValue VisitBinaryOperatorCore(IBinaryOperation operation, object argument)
            {
                switch (operation.OperatorKind)
                {
                    case BinaryOperatorKind.Add:
                    case BinaryOperatorKind.Concatenate:
                        var leftValue = Visit(operation.LeftOperand, argument);
                        var rightValue = Visit(operation.RightOperand, argument);
                        return leftValue.MergeBinaryAdd(rightValue);

                    default:
                        return base.VisitBinaryOperatorCore(operation, argument);
                }
            }

            public override StringContentAbstractValue ComputeValueForCompoundAssignment(ICompoundAssignmentOperation operation, StringContentAbstractValue targetValue, StringContentAbstractValue assignedValue)
            {
                switch (operation.OperatorKind)
                {
                    case BinaryOperatorKind.Add:
                    case BinaryOperatorKind.Concatenate:
                        return targetValue.MergeBinaryAdd(assignedValue);

                    default:
                        return base.ComputeValueForCompoundAssignment(operation, targetValue, assignedValue);
                }
            }

            public override StringContentAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
            {
                // TODO: Analyze string constructor
                // https://github.com/dotnet/roslyn-analyzers/issues/1547
                return base.VisitObjectCreation(operation, argument);
            }

            public override StringContentAbstractValue VisitFieldReference(IFieldReferenceOperation operation, object argument)
            {
                var value = base.VisitFieldReference(operation, argument);

                // Handle "string.Empty"
                if (operation.Field.Name.Equals("Empty", StringComparison.Ordinal) &&
                    operation.Field.ContainingType.SpecialType == SpecialType.System_String)
                {
                    return StringContentAbstractValue.Create(string.Empty);
                }

                return value;
            }

            public override StringContentAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IInvocationOperation operation, object argument)
            {
                // TODO: Handle invocations of string methods (Format, SubString, Replace, Concat, etc.)
                // https://github.com/dotnet/roslyn-analyzers/issues/1547
                return base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(operation, argument);
            }

            public override StringContentAbstractValue VisitInterpolatedString(IInterpolatedStringOperation operation, object argument)
            {
                if (operation.Parts.IsEmpty)
                {
                    return StringContentAbstractValue.Create(string.Empty);
                }

                StringContentAbstractValue mergedValue = Visit(operation.Parts[0], argument);
                for (int i = 1; i < operation.Parts.Length; i++)
                {
                    var newValue = Visit(operation.Parts[i], argument);
                    mergedValue = mergedValue.MergeBinaryAdd(newValue);
                }

                return mergedValue;
            }

            #endregion
        }
    }
}
