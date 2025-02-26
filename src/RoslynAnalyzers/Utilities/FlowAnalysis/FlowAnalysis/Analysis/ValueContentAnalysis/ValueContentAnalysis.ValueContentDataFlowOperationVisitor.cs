﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    public partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the data values across a given statement in a basic block.
        /// </summary>
        private sealed class ValueContentDataFlowOperationVisitor : PredicateAnalysisEntityDataFlowOperationVisitor<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentAbstractValue>
        {
            private readonly ValueContentAnalysisDomain _valueContentAnalysisDomain;

            public ValueContentDataFlowOperationVisitor(ValueContentAnalysisDomain valueContentAnalysisDomain, ValueContentAnalysisContext analysisContext)
                : base(analysisContext)
            {
                _valueContentAnalysisDomain = valueContentAnalysisDomain;
            }

            protected override void AddTrackedEntities(ValueContentAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
                => analysisData.AddTrackedEntities(builder);

            protected override void ResetAbstractValue(AnalysisEntity analysisEntity)
                => SetAbstractValue(analysisEntity, ValueDomain.UnknownOrMayBeValue);

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, ValueContentAbstractValue value)
                => SetAbstractValue(CurrentAnalysisData, analysisEntity, value);

            private void SetAbstractValue(ValueContentAnalysisData analysisData, AnalysisEntity analysisEntity, ValueContentAbstractValue value)
                => SetAbstractValue(analysisData, analysisEntity, value, HasCompletePointsToAnalysisResult);

            private static void SetAbstractValue(ValueContentAnalysisData analysisData, AnalysisEntity analysisEntity, ValueContentAbstractValue value, bool hasCompletePointsToAnalysisResult)
            {
                // PERF: Avoid creating an entry if the value is the default unknown value.
                if (value == ValueContentAbstractValue.MayBeContainsNonLiteralState &&
                    !analysisData.HasAbstractValue(analysisEntity))
                {
                    return;
                }

                if (analysisEntity.ShouldBeTrackedForAnalysis(hasCompletePointsToAnalysisResult))
                {
                    analysisData.SetAbstractValue(analysisEntity, value);
                }
            }

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
                => CurrentAnalysisData.HasAbstractValue(analysisEntity);

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity, ValueContentAnalysisData analysisData)
                => analysisData.RemoveEntries(analysisEntity);

            protected override ValueContentAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
                => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

            protected override ValueContentAbstractValue GetAbstractDefaultValue(ITypeSymbol? type)
                => type != null && !type.CanHoldNullValue() ?
                   ValueContentAbstractValue.DoesNotContainLiteralOrNonLiteralState :
                   ValueContentAbstractValue.ContainsNullLiteralState;

            protected override bool HasAnyAbstractValue(ValueContentAnalysisData data)
                => data.HasAnyAbstractValue;

            protected override void ResetCurrentAnalysisData()
                => CurrentAnalysisData.Reset(ValueDomain.UnknownOrMayBeValue);

            protected override CopyAbstractValue GetCopyAbstractValue(IOperation operation)
            {
                if (DataFlowAnalysisContext.CopyAnalysisResult == null &&
                    AnalysisEntityFactory.TryCreate(operation, out var entity) &&
                    entity.CaptureId.HasValue &&
                    AnalysisEntityFactory.TryGetCopyValueForFlowCapture(entity.CaptureId.Value.Id, out var copyValue) &&
                    copyValue.Kind == CopyAbstractValueKind.KnownValueCopy)
                {
                    return copyValue;
                }

                return base.GetCopyAbstractValue(operation);
            }

            #region Predicate analysis
            protected override PredicateValueKind SetValueForIsNullComparisonOperator(IOperation leftOperand, bool equals, ValueContentAnalysisData targetAnalysisData)
                => PredicateValueKind.Unknown;

            protected override PredicateValueKind SetValueForEqualsOrNotEqualsComparisonOperator(
                IOperation leftOperand,
                IOperation rightOperand,
                bool equals,
                bool isReferenceEquality,
                ValueContentAnalysisData targetAnalysisData)
            {
                var predicateValueKind = PredicateValueKind.Unknown;

                // Handle 'a == "SomeValue"' and 'a != "SomeValue"'
                SetValueForComparisonOperator(leftOperand, rightOperand, equals, ref predicateValueKind, targetAnalysisData);

                // Handle '"SomeValue" == a' and '"SomeValue" != a'
                SetValueForComparisonOperator(rightOperand, leftOperand, equals, ref predicateValueKind, targetAnalysisData);

                return predicateValueKind;
            }

            private void SetValueForComparisonOperator(IOperation target, IOperation assignedValue, bool equals, ref PredicateValueKind predicateValueKind, ValueContentAnalysisData targetAnalysisData)
            {
                ValueContentAbstractValue currentAssignedValue = GetCachedAbstractValue(assignedValue);
                if (currentAssignedValue.IsLiteralState &&
                    AnalysisEntityFactory.TryCreate(target, out var targetEntity))
                {
                    if (CurrentAnalysisData.TryGetValue(targetEntity, out var existingTargetValue) &&
                        existingTargetValue.IsLiteralState)
                    {
                        var newValue = currentAssignedValue.IntersectLiteralValues(existingTargetValue);
                        if (newValue.NonLiteralState == ValueContainsNonLiteralState.Invalid)
                        {
                            predicateValueKind = equals ? PredicateValueKind.AlwaysFalse : PredicateValueKind.AlwaysTrue;
                        }
                        else if (predicateValueKind != PredicateValueKind.AlwaysFalse &&
                            newValue.IsLiteralState &&
                            newValue.LiteralValues.Count == 1 &&
                            currentAssignedValue.LiteralValues.Count == 1 &&
                            existingTargetValue.LiteralValues.Count == 1)
                        {
                            predicateValueKind = equals ? PredicateValueKind.AlwaysTrue : PredicateValueKind.AlwaysFalse;
                        }

                        currentAssignedValue = newValue;
                    }

                    if (equals)
                    {
                        CopyAbstractValue copyValue = GetCopyAbstractValue(target);
                        if (copyValue.Kind.IsKnown())
                        {
                            // https://github.com/dotnet/roslyn-analyzers/issues/2106 tracks enabling the below assert.
                            //Debug.Assert(copyValue.AnalysisEntities.Contains(targetEntity));
                            foreach (var analysisEntity in copyValue.AnalysisEntities)
                            {
                                SetAbstractValue(targetAnalysisData, analysisEntity, currentAssignedValue);
                            }
                        }
                        else
                        {
                            SetAbstractValue(targetAnalysisData, targetEntity, currentAssignedValue);
                        }
                    }
                }
            }

            #endregion

            protected override ValueContentAnalysisData MergeAnalysisData(ValueContentAnalysisData value1, ValueContentAnalysisData value2)
                => _valueContentAnalysisDomain.Merge(value1, value2);
            protected override ValueContentAnalysisData MergeAnalysisDataForBackEdge(ValueContentAnalysisData value1, ValueContentAnalysisData value2, BasicBlock forBlock)
                => _valueContentAnalysisDomain.MergeAnalysisDataForBackEdge(value1, value2);
            protected override void UpdateValuesForAnalysisData(ValueContentAnalysisData targetAnalysisData)
                => UpdateValuesForAnalysisData(targetAnalysisData.CoreAnalysisData, CurrentAnalysisData.CoreAnalysisData);
            protected override ValueContentAnalysisData GetClonedAnalysisData(ValueContentAnalysisData analysisData)
                => (ValueContentAnalysisData)analysisData.Clone();
            public override ValueContentAnalysisData GetEmptyAnalysisData()
                => new();
            protected override ValueContentAnalysisData GetExitBlockOutputData(ValueContentAnalysisResult analysisResult)
                => new(analysisResult.ExitBlockOutput.Data);
            protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(ValueContentAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
                => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException.CoreAnalysisData, CurrentAnalysisData.CoreAnalysisData, throwBranchWithExceptionType);
            protected override bool Equals(ValueContentAnalysisData value1, ValueContentAnalysisData value2)
                => value1.Equals(value2);
            protected override void ApplyInterproceduralAnalysisResultCore(ValueContentAnalysisData resultData)
                => ApplyInterproceduralAnalysisResultHelper(resultData.CoreAnalysisData);
            protected override ValueContentAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
                => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData.CoreAnalysisData, SetAbstractValue);

            #region Visitor methods
            public override ValueContentAbstractValue DefaultVisit(IOperation operation, object? argument)
            {
                _ = base.DefaultVisit(operation, argument);
                if (operation.Type == null)
                {
                    return operation.Kind == OperationKind.None ?
                        ValueContentAbstractValue.MayBeContainsNonLiteralState :
                        ValueContentAbstractValue.ContainsNullLiteralState;
                }

                if (ValueContentAbstractValue.IsSupportedType(operation.Type, out var valueTypeSymbol))
                {
                    if (operation.ConstantValue.HasValue)
                    {
                        return operation.ConstantValue.Value != null ?
                            ValueContentAbstractValue.Create(operation.ConstantValue.Value, valueTypeSymbol) :
                            ValueContentAbstractValue.ContainsNullLiteralState;
                    }
                    else
                    {
                        return GetNullAbstractValue(operation) switch
                        {
                            PointsToAnalysis.NullAbstractValue.Invalid => ValueContentAbstractValue.InvalidState,

                            PointsToAnalysis.NullAbstractValue.Null => ValueContentAbstractValue.ContainsNullLiteralState,

                            _ => ValueContentAbstractValue.MayBeContainsNonLiteralState,
                        };
                    }
                }
                else if (DataFlowAnalysisContext.GetValueForAdditionalSupportedValueTypeOperation is { } getValueFunc &&
                    operation.Type is INamedTypeSymbol namedType &&
                    DataFlowAnalysisContext.AdditionalSupportedValueTypes.Contains(namedType))
                {
                    return getValueFunc(operation);
                }

                return ValueDomain.UnknownOrMayBeValue;
            }

            public override ValueContentAbstractValue VisitBinaryOperatorCore(IBinaryOperation operation, object? argument)
            {
                var leftValue = Visit(operation.LeftOperand, argument);
                var rightValue = Visit(operation.RightOperand, argument);
                return leftValue.MergeBinaryOperation(rightValue, operation.OperatorKind, operation.LeftOperand.Type, operation.RightOperand.Type, operation.Type);
            }

            public override ValueContentAbstractValue ComputeValueForCompoundAssignment(
                ICompoundAssignmentOperation operation,
                ValueContentAbstractValue targetValue,
                ValueContentAbstractValue assignedValue,
                ITypeSymbol? targetType,
                ITypeSymbol? assignedValueType)
            {
                return targetValue.MergeBinaryOperation(assignedValue, operation.OperatorKind, targetType, assignedValueType, operation.Type);
            }

            public override ValueContentAbstractValue ComputeValueForIncrementOrDecrementOperation(IIncrementOrDecrementOperation operation, ValueContentAbstractValue targetValue)
            {
                var incrementValue = ValueContentAbstractValue.ContainsOneIntergralLiteralState;
                var incrementValueType = WellKnownTypeProvider.Compilation.GetSpecialType(SpecialType.System_Int32);
                var operationKind = operation.Kind == OperationKind.Increment ? BinaryOperatorKind.Add : BinaryOperatorKind.Subtract;
                return targetValue.MergeBinaryOperation(incrementValue, operationKind, operation.Target.Type, incrementValueType, operation.Type);
            }

            public override ValueContentAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object? argument)
            {
                // TODO: Analyze string constructor
                // https://github.com/dotnet/roslyn-analyzers/issues/1547
                return base.VisitObjectCreation(operation, argument);
            }

            public override ValueContentAbstractValue VisitFieldReference(IFieldReferenceOperation operation, object? argument)
            {
                var value = base.VisitFieldReference(operation, argument);

                // Handle "string.Empty"
                if (operation.Field.Name.Equals("Empty", StringComparison.Ordinal) &&
                    operation.Field.ContainingType.SpecialType == SpecialType.System_String)
                {
                    return ValueContentAbstractValue.ContainsEmptyStringLiteralState;
                }

                return value;
            }

            public override ValueContentAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                IMethodSymbol method,
                IOperation? visitedInstance,
                ImmutableArray<IArgumentOperation> visitedArguments,
                bool invokedAsDelegate,
                IOperation originalOperation,
                ValueContentAbstractValue defaultValue)
            {
                // TODO: Handle invocations of string methods (Format, SubString, Replace, Concat, etc.)
                // https://github.com/dotnet/roslyn-analyzers/issues/1547
                return base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);
            }

            public override ValueContentAbstractValue VisitInterpolatedString(IInterpolatedStringOperation operation, object? argument)
            {
                if (operation.Parts.IsEmpty)
                {
                    return ValueContentAbstractValue.ContainsEmptyStringLiteralState;
                }

                ValueContentAbstractValue mergedValue = Visit(operation.Parts[0], argument);
                for (int i = 1; i < operation.Parts.Length; i++)
                {
                    var newValue = Visit(operation.Parts[i], argument);
                    mergedValue = mergedValue.MergeBinaryOperation(newValue, BinaryOperatorKind.Add, leftType: operation.Type, rightType: operation.Type, resultType: operation.Type);
                }

                return mergedValue;
            }

            #endregion
        }
    }
}
