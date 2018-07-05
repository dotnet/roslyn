// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    internal partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the PointsTo values across a given statement in a basic block.
        /// </summary>
        private sealed class PointsToDataFlowOperationVisitor : AnalysisEntityDataFlowOperationVisitor<PointsToAnalysisData, PointsToAbstractValue>
        {
            private readonly DefaultPointsToValueGenerator _defaultPointsToValueGenerator;
            private readonly PointsToAnalysisDomain _pointsToAnalysisDomain;

            public PointsToDataFlowOperationVisitor(
                DefaultPointsToValueGenerator defaultPointsToValueGenerator,
                PointsToAnalysisDomain pointsToAnalysisDomain,
                PointsToAbstractValueDomain valueDomain,
                ISymbol owningSymbol,
                WellKnownTypeProvider wellKnownTypeProvider,
                ControlFlowGraph cfg,
                bool pessimisticAnalysis,
                DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue> copyAnalysisResultOpt)
                : base(valueDomain, owningSymbol, wellKnownTypeProvider, cfg, pessimisticAnalysis, predicateAnalysis: true, copyAnalysisResultOpt: copyAnalysisResultOpt, pointsToAnalysisResultOpt: null)
            {
                _defaultPointsToValueGenerator = defaultPointsToValueGenerator;
                _pointsToAnalysisDomain = pointsToAnalysisDomain;
            }

            public override PointsToAnalysisData Flow(IOperation statement, BasicBlock block, PointsToAnalysisData input)
            {
                // Ensure PointsTo value is set for the "this" or "Me" instance.
                if (input != null && !HasAbstractValue(AnalysisEntityFactory.ThisOrMeInstance))
                {
                    input.SetAbstactValue(AnalysisEntityFactory.ThisOrMeInstance, ThisOrMePointsToAbstractValue);
                }

                return base.Flow(statement, block, input);
            }

            private static bool ShouldBeTracked(ITypeSymbol typeSymbol) => typeSymbol.IsReferenceTypeOrNullableValueType();

            private bool ShouldBeTracked(AnalysisEntity analysisEntity) => ShouldBeTracked(analysisEntity.Type) ||
                analysisEntity.CaptureIdOpt.HasValue && IsLValueFlowCapture(analysisEntity.CaptureIdOpt.Value);

            protected override void AddTrackedEntities(ImmutableArray<AnalysisEntity>.Builder builder)
            {
                _defaultPointsToValueGenerator.AddTrackedEntities(builder);

                // Ensure we skip duplicates.
                var defaultPointsToEntities = builder.ToSet();
                foreach (var key in CurrentAnalysisData.CoreAnalysisData.Keys)
                {
                    if (!defaultPointsToEntities.Contains(key))
                    {
                        builder.Add(key);
                    }
                }
            }

            protected override bool IsPointsToAnalysis => true;

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.HasAbstractValue(analysisEntity);

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity) => CurrentAnalysisData.RemoveEntries(analysisEntity);

            protected override PointsToAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
            {
                if (!ShouldBeTracked(analysisEntity))
                {
                    Debug.Assert(!CurrentAnalysisData.HasAbstractValue(analysisEntity));
                    return PointsToAbstractValue.NoLocation;
                }

                if (!CurrentAnalysisData.TryGetValue(analysisEntity, out var value))
                {
                    value = _defaultPointsToValueGenerator.GetOrCreateDefaultValue(analysisEntity);
                }

                return value;
            }

            protected override PointsToAbstractValue GetPointsToAbstractValue(IOperation operation) => base.GetCachedAbstractValue(operation);

            protected override PointsToAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => !ShouldBeTracked(type) ? PointsToAbstractValue.NoLocation : PointsToAbstractValue.NullLocation;

            protected override bool HasAnyAbstractValue(PointsToAnalysisData data) => data.HasAnyAbstractValue;

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, PointsToAbstractValue value)
            {
                if (ShouldBeTracked(analysisEntity))
                {
                    CurrentAnalysisData.SetAbstactValue(analysisEntity, value);
                }
            }

            private static void SetAbstractValueFromPredicate(
                AnalysisEntity analysisEntity,
                IOperation operation,
                NullAbstractValue nullState,
                PointsToAnalysisData sourceAnalysisData,
                PointsToAnalysisData targetAnalysisData)
            {
                Debug.Assert(IsValidValueForPredicateAnalysis(nullState) || nullState == NullAbstractValue.Invalid);
                if (sourceAnalysisData.TryGetValue(analysisEntity, out PointsToAbstractValue existingValue))
                {
                    PointsToAbstractValue newPointsToValue;
                    switch (nullState)
                    {
                        case NullAbstractValue.Null:
                            newPointsToValue = existingValue.MakeNull();
                            break;

                        case NullAbstractValue.NotNull:
                            newPointsToValue = existingValue.MakeNonNull(operation);
                            break;

                        case NullAbstractValue.Invalid:
                            newPointsToValue = PointsToAbstractValue.Invalid;
                            break;

                        default:
                            throw new InvalidProgramException();
                    }

                    targetAnalysisData.SetAbstactValue(analysisEntity, newPointsToValue);
                }
            }

            protected override void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            {
                // Create a dummy PointsTo value for each reference type parameter.
                if (ShouldBeTracked(parameter.Type))
                {
                    var value = PointsToAbstractValue.Create(AbstractLocation.CreateSymbolLocation(parameter), mayBeNull: true);
                    SetAbstractValue(analysisEntity, value);
                }
            }

            protected override void SetValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            {
                // Do not escape the PointsTo value for parameter at exit.
            }

            protected override void ResetCurrentAnalysisData() => CurrentAnalysisData.Reset(ValueDomain.UnknownOrMayBeValue);

            protected override PointsToAbstractValue ComputeAnalysisValueForReferenceOperation(IOperation operation, PointsToAbstractValue defaultValue)
            {
                if (ShouldBeTracked(operation.Type) &&
                    AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
                {
                    return GetAbstractValue(analysisEntity);
                }
                else
                {
                    Debug.Assert(operation.Type == null || !operation.Type.IsNonNullableValueType() || defaultValue == PointsToAbstractValue.NoLocation);
                    return defaultValue;
                }
            }

            protected override PointsToAbstractValue ComputeAnalysisValueForOutArgument(AnalysisEntity analysisEntity, IArgumentOperation operation, PointsToAbstractValue defaultValue)
            {
                if (!ShouldBeTracked(analysisEntity))
                {
                    return PointsToAbstractValue.NoLocation;
                }

                var location = AbstractLocation.CreateAllocationLocation(operation, analysisEntity.Type);
                return PointsToAbstractValue.Create(location, mayBeNull: true);
            }

            #region Predicate analysis
            protected override PointsToAnalysisData GetEmptyAnalysisDataForPredicateAnalysis() => new PointsToAnalysisData();

            private static bool IsValidValueForPredicateAnalysis(NullAbstractValue value)
            {
                switch (value)
                {
                    case NullAbstractValue.Null:
                    case NullAbstractValue.NotNull:
                        return true;

                    default:
                        return false;
                }
            }

            protected override PredicateValueKind SetValueForEqualsOrNotEqualsComparisonOperator(
                IOperation leftOperand,
                IOperation rightOperand,
                bool equals,
                bool isReferenceEquality,
                PointsToAnalysisData targetAnalysisData)
            {
                var predicateValueKind = PredicateValueKind.Unknown;

                // Handle "a == null" and "a != null"
                if (SetValueForComparisonOperator(leftOperand, rightOperand, equals, ref predicateValueKind, targetAnalysisData))
                {
                    return predicateValueKind;
                }

                // Otherwise, handle "null == a" and "null != a"
                SetValueForComparisonOperator(rightOperand, leftOperand, equals, ref predicateValueKind, targetAnalysisData);
                return predicateValueKind;
            }

            protected override PredicateValueKind SetValueForIsNullComparisonOperator(IOperation leftOperand, bool equals, PointsToAnalysisData targetAnalysisData)
            {
                var predicateValueKind = PredicateValueKind.Unknown;
                SetValueForComparisonOperator(leftOperand, value: NullAbstractValue.Null, equals: equals, predicateValueKind: ref predicateValueKind, targetAnalysisData: targetAnalysisData);
                return predicateValueKind;
            }

            private bool SetValueForComparisonOperator(
                IOperation target,
                IOperation assignedValueOperation,
                bool equals,
                ref PredicateValueKind predicateValueKind,
                PointsToAnalysisData targetAnalysisData)
            {
                NullAbstractValue value = GetNullAbstractValue(assignedValueOperation);
                return SetValueForComparisonOperator(target, value, equals, ref predicateValueKind, targetAnalysisData);
            }

            private bool SetValueForComparisonOperator(
                IOperation target,
                NullAbstractValue value,
                bool equals,
                ref PredicateValueKind predicateValueKind,
                PointsToAnalysisData targetAnalysisData)
            {
                if (IsValidValueForPredicateAnalysis(value) &&
                    AnalysisEntityFactory.TryCreate(target, out AnalysisEntity targetEntity))
                {
                    // Comparison with a non-null value guarantees that we can infer result in only one of the branches.
                    // For example, predicate "a == c", where we know 'c' is non-null, guarantees 'a' is non-null in CurrentAnalysisData,
                    bool inferInTargetAnalysisData = !(value == NullAbstractValue.NotNull && !equals);

                    CopyAbstractValue copyValue = GetCopyAbstractValue(target);
                    if (copyValue.Kind == CopyAbstractValueKind.Known)
                    {
                        foreach (var analysisEntity in copyValue.AnalysisEntities)
                        {
                            SetValueFromPredicate(analysisEntity, value, equals, inferInTargetAnalysisData,
                                target, ref predicateValueKind, sourceAnalysisData:CurrentAnalysisData, targetAnalysisData: targetAnalysisData);
                        }
                    }
                    else
                    {
                        SetValueFromPredicate(targetEntity, value, equals, inferInTargetAnalysisData,
                            target, ref predicateValueKind, sourceAnalysisData: CurrentAnalysisData, targetAnalysisData: targetAnalysisData);
                    }

                    return true;
                }

                return false;
            }

            private static void SetValueFromPredicate(
                AnalysisEntity key,
                NullAbstractValue value,
                bool equals,
                bool inferInTargetAnalysisData,
                IOperation target,
                ref PredicateValueKind predicateValueKind,
                PointsToAnalysisData sourceAnalysisData,
                PointsToAnalysisData targetAnalysisData)
            {
                // Compute the negated value.
                NullAbstractValue negatedValue = NegatePredicateValue(value);

                // Check if the key already has an existing "Null" or "NotNull" NullState that would make the condition always true or false.
                // If so, set the predicateValueKind to always true/false, set the value in branch that can never be taken to NullAbstractValue.Invalid
                // and turn off value inference in one of the branch.
                if (sourceAnalysisData.TryGetValue(key, out PointsToAbstractValue existingPointsToValue))
                {
                    NullAbstractValue existingNullValue = existingPointsToValue.NullState;
                    if (IsValidValueForPredicateAnalysis(existingNullValue) &&
                        (existingNullValue == NullAbstractValue.Null || value == NullAbstractValue.Null))
                    {
                        if (value == existingNullValue && equals ||
                            negatedValue == existingNullValue && !equals)
                        {
                            predicateValueKind = PredicateValueKind.AlwaysTrue;
                            negatedValue = NullAbstractValue.Invalid;
                            inferInTargetAnalysisData = false;
                        }

                        if (negatedValue == existingNullValue && equals ||
                            value == existingNullValue && !equals)
                        {
                            predicateValueKind = PredicateValueKind.AlwaysFalse;
                            value = NullAbstractValue.Invalid;
                        }
                    }
                }

                // Swap value and negatedValue if we are processing not-equals operator.
                if (!equals)
                {
                    if (value != NullAbstractValue.Invalid && negatedValue != NullAbstractValue.Invalid)
                    {
                        var temp = value;
                        value = negatedValue;
                        negatedValue = temp;
                    }
                }

                if (inferInTargetAnalysisData)
                {
                    // Set value for the CurrentAnalysisData.
                    SetAbstractValueFromPredicate(key, target, value, sourceAnalysisData, targetAnalysisData);
                }
            }

            private static NullAbstractValue NegatePredicateValue(NullAbstractValue value)
            {
                Debug.Assert(IsValidValueForPredicateAnalysis(value));

                switch (value)
                {
                    case NullAbstractValue.Null:
                        return NullAbstractValue.NotNull;

                    case NullAbstractValue.NotNull:
                        return NullAbstractValue.Null;

                    default:
                        throw new InvalidProgramException();
                }
            }
            #endregion

            protected override PointsToAnalysisData MergeAnalysisData(PointsToAnalysisData value1, PointsToAnalysisData value2)
                => _pointsToAnalysisDomain.Merge(value1, value2);
            protected override PointsToAnalysisData GetClonedAnalysisData(PointsToAnalysisData analysisData)
                => (PointsToAnalysisData)analysisData.Clone();
            protected override bool Equals(PointsToAnalysisData value1, PointsToAnalysisData value2)
                => value1.Equals(value2);

            #region Visitor methods

            public override PointsToAbstractValue DefaultVisit(IOperation operation, object argument)
            {
                var value = base.DefaultVisit(operation, argument);

                // Special handling for:
                //  1. Null value: NullLocation
                //  2. Constants and value types do not point to any location.
                if (operation.ConstantValue.HasValue)
                {
                    if (operation.Type == null ||
                        operation.ConstantValue.Value == null)
                    {
                        return PointsToAbstractValue.NullLocation;
                    }
                    else
                    {
                        return PointsToAbstractValue.NoLocation;
                    }
                }
                else if (operation.Type.IsNonNullableValueType())
                {
                    return PointsToAbstractValue.NoLocation;
                }

                return ValueDomain.UnknownOrMayBeValue;
            }

            public override PointsToAbstractValue VisitAwait(IAwaitOperation operation, object argument)
            {
                var _ = base.VisitAwait(operation, argument);
                return PointsToAbstractValue.Unknown;
            }

            public override PointsToAbstractValue VisitIsType(IIsTypeOperation operation, object argument)
            {
                var _ = base.VisitIsType(operation, argument);
                return PointsToAbstractValue.NoLocation;
            }

            public override PointsToAbstractValue VisitInstanceReference(IInstanceReferenceOperation operation, object argument)
            {
                var _ = base.VisitInstanceReference(operation, argument);
                IOperation currentInstanceOperation = operation.GetInstance(IsInsideAnonymousObjectInitializer);
                var value = currentInstanceOperation != null ?
                    GetCachedAbstractValue(currentInstanceOperation) :
                    ThisOrMePointsToAbstractValue;
                Debug.Assert(value.NullState == NullAbstractValue.NotNull);
                return value;
            }

            private PointsToAbstractValue VisitTypeCreationWithArgumentsAndInitializer(IEnumerable<IOperation> arguments, IObjectOrCollectionInitializerOperation initializer, IOperation operation, object argument)
            {
                AbstractLocation location = AbstractLocation.CreateAllocationLocation(operation, operation.Type);
                var pointsToAbstractValue = PointsToAbstractValue.Create(location, mayBeNull: false);
                CacheAbstractValue(operation, pointsToAbstractValue);

                var unusedArray = VisitArray(arguments, argument);
                var initializerValue = Visit(initializer, argument);
                Debug.Assert(initializer == null || initializerValue == pointsToAbstractValue);
                return pointsToAbstractValue;
            }

            public override PointsToAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
            {
                return VisitTypeCreationWithArgumentsAndInitializer(operation.Arguments, operation.Initializer, operation, argument);
            }

            public override PointsToAbstractValue VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, object argument)
            {
                return VisitTypeCreationWithArgumentsAndInitializer(operation.Arguments, operation.Initializer, operation, argument);
            }

            public override PointsToAbstractValue VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object argument)
            {
                AbstractLocation location = AbstractLocation.CreateAllocationLocation(operation, operation.Type);
                var pointsToAbstractValue = PointsToAbstractValue.Create(location, mayBeNull: false);
                CacheAbstractValue(operation, pointsToAbstractValue);

                var _ = base.VisitAnonymousObjectCreation(operation, argument);
                return pointsToAbstractValue;
            }

            public override PointsToAbstractValue VisitDelegateCreation(IDelegateCreationOperation operation, object argument)
            {
                var _ = base.VisitDelegateCreation(operation, argument);
                AbstractLocation location = AbstractLocation.CreateAllocationLocation(operation, operation.Type);
                return PointsToAbstractValue.Create(location, mayBeNull: false);
            }

            public override PointsToAbstractValue VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, object argument)
            {
                var arguments = ImmutableArray<IOperation>.Empty;
                return VisitTypeCreationWithArgumentsAndInitializer(arguments, operation.Initializer, operation, argument);
            }

            public override PointsToAbstractValue VisitArrayInitializer(IArrayInitializerOperation operation, object argument)
            {
                var _ = base.VisitArrayInitializer(operation, argument);

                // We should have created a new PointsTo value for the associated array creation operation.
                return GetCachedAbstractValue(operation.GetAncestor<IArrayCreationOperation>(OperationKind.ArrayCreation));
            }

            public override PointsToAbstractValue VisitArrayCreation(IArrayCreationOperation operation, object argument)
            {
                var pointsToAbstractValue = PointsToAbstractValue.Create(AbstractLocation.CreateAllocationLocation(operation, operation.Type), mayBeNull: false);
                CacheAbstractValue(operation, pointsToAbstractValue);

                var unusedDimensionsValue = VisitArray(operation.DimensionSizes, argument);
                var initializerValue = Visit(operation.Initializer, argument);
                Debug.Assert(operation.Initializer == null || initializerValue == pointsToAbstractValue);
                return pointsToAbstractValue;
            }

            public override PointsToAbstractValue VisitIsPattern(IIsPatternOperation operation, object argument)
            {
                // TODO: Handle patterns
                // https://github.com/dotnet/roslyn-analyzers/issues/1571
                return base.VisitIsPattern(operation, argument);
            }

            public override PointsToAbstractValue VisitDeclarationPattern(IDeclarationPatternOperation operation, object argument)
            {
                // TODO: Handle patterns
                // https://github.com/dotnet/roslyn-analyzers/issues/1571
                return base.VisitDeclarationPattern(operation, argument);
            }

            public override PointsToAbstractValue VisitInterpolatedString(IInterpolatedStringOperation operation, object argument)
            {
                var _ = base.VisitInterpolatedString(operation, argument);
                return PointsToAbstractValue.NoLocation;
            }

            public override PointsToAbstractValue VisitBinaryOperatorCore(IBinaryOperation operation, object argument)
            {
                var _ = base.VisitBinaryOperatorCore(operation, argument);
                return PointsToAbstractValue.Unknown;
            }

            public override PointsToAbstractValue VisitSizeOf(ISizeOfOperation operation, object argument)
            {
                var _ = base.VisitSizeOf(operation, argument);
                return PointsToAbstractValue.NoLocation;
            }

            public override PointsToAbstractValue VisitTypeOf(ITypeOfOperation operation, object argument)
            {
                var _ = base.VisitTypeOf(operation, argument);
                return PointsToAbstractValue.NoLocation;
            }

            private PointsToAbstractValue VisitInvocationCommon(IOperation operation, IOperation instance)
            {
                if (ShouldBeTracked(operation.Type))
                {
                    AbstractLocation location = AbstractLocation.CreateAllocationLocation(operation, operation.Type);
                    var pointsToAbstractValue = PointsToAbstractValue.Create(location, mayBeNull: true);
                    return GetValueBasedOnInstanceOrReferenceValue(referenceOrInstance: instance, operation: operation, defaultValue: pointsToAbstractValue);
                }
                else
                {
                    return PointsToAbstractValue.NoLocation;
                }
            }

            public override PointsToAbstractValue VisitInvocation_LambdaOrDelegateOrLocalFunction(IInvocationOperation operation, object argument)
            {
                var _ = base.VisitInvocation_LambdaOrDelegateOrLocalFunction(operation, argument);
                return VisitInvocationCommon(operation, operation.Instance);
            }

            public override PointsToAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IInvocationOperation operation, object argument)
            {
                var _ = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(operation, argument);
                return VisitInvocationCommon(operation, operation.Instance);
            }

            public override PointsToAbstractValue VisitDynamicInvocation(IDynamicInvocationOperation operation, object argument)
            {
                var _ = base.VisitDynamicInvocation(operation, argument);
                return VisitInvocationCommon(operation, operation.Operation);
            }

            private NullAbstractValue GetNullStateBasedOnInstanceOrReferenceValue(IOperation referenceOrInstance, ITypeSymbol operationType, NullAbstractValue defaultValue)
            {
                if (operationType.IsNonNullableValueType())
                {
                    return NullAbstractValue.NotNull;
                }

                NullAbstractValue referenceOrInstanceValue = referenceOrInstance != null ? GetNullAbstractValue(referenceOrInstance) : NullAbstractValue.NotNull;
                switch (referenceOrInstanceValue)
                {
                    case NullAbstractValue.Invalid:
                    case NullAbstractValue.Null:
                        return referenceOrInstanceValue;

                    default:
                        return defaultValue;
                }
            }

            private PointsToAbstractValue GetValueBasedOnInstanceOrReferenceValue(IOperation referenceOrInstance, IOperation operation, PointsToAbstractValue defaultValue)
            {
                NullAbstractValue nullState = GetNullStateBasedOnInstanceOrReferenceValue(referenceOrInstance, operation.Type, defaultValue.NullState);
                switch (nullState)
                {
                    case NullAbstractValue.NotNull:
                        return defaultValue.MakeNonNull(operation);

                    case NullAbstractValue.Null:
                        return defaultValue.MakeNull();

                    case NullAbstractValue.Invalid:
                        return PointsToAbstractValue.Invalid;

                    default:
                        return defaultValue;
                }
            }

            public override PointsToAbstractValue VisitFieldReference(IFieldReferenceOperation operation, object argument)
            {
                var value = base.VisitFieldReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitPropertyReference(IPropertyReferenceOperation operation, object argument)
            {
                var value = base.VisitPropertyReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, object argument)
            {
                var value = base.VisitDynamicMemberReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitMethodReferenceCore(IMethodReferenceOperation operation, object argument)
            {
                var value = base.VisitMethodReferenceCore(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitEventReference(IEventReferenceOperation operation, object argument)
            {
                var value = base.VisitEventReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitArrayElementReference(IArrayElementReferenceOperation operation, object argument)
            {
                var value = base.VisitArrayElementReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.ArrayReference, operation, value);
            }

            public override PointsToAbstractValue VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, object argument)
            {
                var value = base.VisitDynamicIndexerAccess(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Operation, operation, value);
            }

            public override PointsToAbstractValue VisitConversion(IConversionOperation operation, object argument)
            {
                var value = base.VisitConversion(operation, argument);
                if (value.NullState == NullAbstractValue.NotNull)
                {
                    if (TryInferConversion(operation, out bool alwaysSucceed, out bool alwaysFail))
                    {
                        Debug.Assert(!alwaysSucceed || !alwaysFail);
                        if (alwaysFail)
                        {
                            value = value.MakeNull();
                        }
                        else if (operation.IsTryCast && !alwaysSucceed)
                        {
                            // TryCast which may or may not succeed.
                            value = value.MakeMayBeNull();
                        }
                    }
                    else
                    {
                        value = value.MakeMayBeNull();
                    }
                }

                return value;
            }

            public override PointsToAbstractValue VisitFlowCapture(IFlowCaptureOperation operation, object argument)
            {
                var value = base.VisitFlowCapture(operation, argument);
                if (IsLValueFlowCapture(operation.Id) &&
                    AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity flowCaptureEntity))
                {
                    value = PointsToAbstractValue.Create(operation.Value);
                    SetAbstractValue(flowCaptureEntity, value);
                }

                return value;
            }

            public override PointsToAbstractValue VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, object argument)
            {
                var value = base.VisitFlowCaptureReference(operation, argument);
                if (IsLValueFlowCapture(operation.Id) &&
                    AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity flowCaptureEntity))
                {
                    return GetAbstractValue(flowCaptureEntity);
                }

                return value;
            }

            #endregion
        }
    }
}
