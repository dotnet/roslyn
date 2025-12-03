// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = DictionaryAnalysisData<AbstractLocation, ParameterValidationAbstractValue>;

    internal partial class ParameterValidationAnalysis : ForwardDataFlowAnalysis<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the location validation values across a given statement in a basic block.
        /// </summary>
        private sealed class ParameterValidationDataFlowOperationVisitor :
            AbstractLocationDataFlowOperationVisitor<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationAbstractValue>
        {
            private readonly ImmutableDictionary<IParameterSymbol, SyntaxNode>.Builder? _hazardousParameterUsageBuilder;
            private readonly INamedTypeSymbol? _notNullAttributeType;

            public ParameterValidationDataFlowOperationVisitor(ParameterValidationAnalysisContext analysisContext)
                : base(analysisContext)
            {
                Debug.Assert(analysisContext.OwningSymbol.Kind == SymbolKind.Method);
                Debug.Assert(analysisContext.PointsToAnalysisResult != null);

                if (analysisContext.TrackHazardousParameterUsages)
                {
                    _hazardousParameterUsageBuilder = ImmutableDictionary.CreateBuilder<IParameterSymbol, SyntaxNode>();
                }

                _notNullAttributeType = analysisContext.WellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemDiagnosticsCodeAnalysisNotNullAttribute);
            }

            public ImmutableDictionary<IParameterSymbol, SyntaxNode> HazardousParameterUsages
            {
                get
                {
                    RoslynDebug.Assert(_hazardousParameterUsageBuilder != null);
                    return _hazardousParameterUsageBuilder.ToImmutable();
                }
            }

            protected override ParameterValidationAbstractValue GetAbstractDefaultValue(ITypeSymbol? type) => ValueDomain.Bottom;

            protected override bool HasAnyAbstractValue(ParameterValidationAnalysisData data) => data.Count > 0;

            protected override ParameterValidationAbstractValue GetAbstractValue(AbstractLocation location)
                => CurrentAnalysisData.TryGetValue(location, out var value) ? value : ValueDomain.Bottom;

            protected override void ResetCurrentAnalysisData() => ResetAnalysisData(CurrentAnalysisData);

            private bool IsTrackedLocation(AbstractLocation location)
            {
                return CurrentAnalysisData.ContainsKey(location) ||
                    location.Symbol is IParameterSymbol parameter &&
                    parameter.Type.IsReferenceType &&
                    Equals(parameter.ContainingSymbol, GetBottomOfStackOwningSymbol());

                ISymbol GetBottomOfStackOwningSymbol()
                {
                    if (DataFlowAnalysisContext.InterproceduralAnalysisData == null)
                    {
                        return OwningSymbol;
                    }

                    return DataFlowAnalysisContext.InterproceduralAnalysisData.MethodsBeingAnalyzed
                        .Single(m => m.InterproceduralAnalysisData == null)
                        .OwningSymbol;
                }
            }

            private bool IsNotOrMaybeValidatedLocation(AbstractLocation location) =>
                CurrentAnalysisData.TryGetValue(location, out var value) &&
                (value == ParameterValidationAbstractValue.NotValidated || value == ParameterValidationAbstractValue.MayBeValidated);

            protected override void StopTrackingAbstractValue(AbstractLocation location) => CurrentAnalysisData.Remove(location);

            protected override void SetAbstractValue(AbstractLocation location, ParameterValidationAbstractValue value)
            {
                if (IsTrackedLocation(location))
                {
                    CurrentAnalysisData[location] = value;
                }
            }

            protected override void SetAbstractValueForAssignment(IOperation target, IOperation? assignedValueOperation, ParameterValidationAbstractValue assignedValue, bool mayBeAssignment = false)
            {
                // If we are assigning to parameter, mark it as validated on this path.
                if (target is IParameterReferenceOperation)
                {
                    MarkValidatedLocations(target);
                }
            }

            protected override void SetAbstractValueForTupleElementAssignment(AnalysisEntity tupleElementEntity, IOperation assignedValueOperation, ParameterValidationAbstractValue assignedValue)
            {
                // We are only tracking default parameter locations.
            }

            protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, ParameterValidationAbstractValue value)
            {
                // We are only tracking default parameter locations.
            }

            protected override void SetValueForParameterPointsToLocationOnEntry(IParameterSymbol parameter, PointsToAbstractValue pointsToAbstractValue)
            {
                if (pointsToAbstractValue.Kind == PointsToAbstractValueKind.KnownLocations)
                {
                    var value = HasAnyNullValidationAttribute(parameter) ? ParameterValidationAbstractValue.Validated : ParameterValidationAbstractValue.NotValidated;
                    SetAbstractValue(pointsToAbstractValue.Locations, value);
                }
            }

            private bool HasAnyNullValidationAttribute(IParameterSymbol? parameter)
                => parameter != null && parameter.GetAttributes().Any(attr => attr.AttributeClass != null &&
                                                                      (attr.AttributeClass.Name.Equals("ValidatedNotNullAttribute", StringComparison.OrdinalIgnoreCase) ||
                                                                       attr.AttributeClass.Equals(_notNullAttributeType)));

            protected override void EscapeValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, ImmutableHashSet<AbstractLocation> escapedLocations)
            {
                // Mark parameters as validated if they are non-null at all non-exception return paths and null at one of the unhandled throw operations.
                var notValidatedLocations = escapedLocations.Where(IsNotOrMaybeValidatedLocation);
                if (notValidatedLocations.Any())
                {
                    if (TryGetNullAbstractValueAtCurrentBlockEntry(analysisEntity, out NullAbstractValue nullAbstractValue) &&
                        nullAbstractValue == NullAbstractValue.NotNull &&
                        TryGetMergedNullAbstractValueAtUnhandledThrowOperationsInGraph(analysisEntity, out NullAbstractValue mergedValueAtUnhandledThrowOperations) &&
                        mergedValueAtUnhandledThrowOperations != NullAbstractValue.NotNull)
                    {
                        SetAbstractValue(notValidatedLocations, ParameterValidationAbstractValue.Validated);
                    }
                }
            }

            private void MarkValidatedLocations(IOperation operation)
                => SetAbstractValue(GetNotValidatedLocations(operation), ParameterValidationAbstractValue.Validated);

            private IEnumerable<AbstractLocation> GetNotValidatedLocations(IOperation operation)
            {
                var pointsToLocation = GetPointsToAbstractValue(operation);
                return pointsToLocation.Locations.Where(IsNotOrMaybeValidatedLocation);
            }

            private static bool IsHazardousIfNull(IOperation operation)
            {
                if (operation.Kind == OperationKind.ConditionalAccessInstance)
                {
                    return false;
                }

                return operation.Parent switch
                {
                    IMemberReferenceOperation memberReference => memberReference.Instance == operation,

                    IArrayElementReferenceOperation arrayElementReference => arrayElementReference.ArrayReference == operation,

                    IInvocationOperation invocation => invocation.Instance == operation,

                    _ => false,
                };
            }

            private void HandlePotentiallyHazardousOperation(IOperation operation, IEnumerable<AbstractLocation> nonValidatedLocations)
            {
                Debug.Assert(_hazardousParameterUsageBuilder != null);

                if (GetNullAbstractValue(operation) == NullAbstractValue.NotNull)
                {
                    // We are sure the value is non-null, so cannot be hazardous.
                    return;
                }

                HandleHazardousOperation(operation.Syntax, nonValidatedLocations);
            }

            private void HandleHazardousOperation(SyntaxNode syntaxNode, IEnumerable<AbstractLocation> nonValidatedLocations)
            {
                RoslynDebug.Assert(_hazardousParameterUsageBuilder != null);

                foreach (var location in nonValidatedLocations)
                {
                    Debug.Assert(IsNotOrMaybeValidatedLocation(location));

                    var parameter = (IParameterSymbol)location.Symbol!;
                    if (!_hazardousParameterUsageBuilder.TryGetValue(parameter, out var currentSyntaxNode) ||
                        syntaxNode.SpanStart < currentSyntaxNode.SpanStart)
                    {
                        _hazardousParameterUsageBuilder[parameter] = syntaxNode;
                    }
                }
            }

            protected override ParameterValidationAnalysisData MergeAnalysisData(ParameterValidationAnalysisData value1, ParameterValidationAnalysisData value2)
                => ParameterValidationAnalysisDomainInstance.Merge(value1, value2);
            protected override void UpdateValuesForAnalysisData(ParameterValidationAnalysisData targetAnalysisData)
                => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);
            protected override ParameterValidationAnalysisData GetClonedAnalysisData(ParameterValidationAnalysisData analysisData)
                => GetClonedAnalysisDataHelper(analysisData);
            public override ParameterValidationAnalysisData GetEmptyAnalysisData()
                => GetEmptyAnalysisDataHelper();
            protected override ParameterValidationAnalysisData GetExitBlockOutputData(ParameterValidationAnalysisResult analysisResult)
                => GetClonedAnalysisDataHelper(analysisResult.ExitBlockOutput.Data);
            protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(ParameterValidationAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
                => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData);
            protected override bool Equals(ParameterValidationAnalysisData value1, ParameterValidationAnalysisData value2)
                => EqualsHelper(value1, value2);

            #region Visit overrides
            public override ParameterValidationAbstractValue Visit(IOperation? operation, object? argument)
            {
                var value = base.Visit(operation, argument);
                if (operation != null)
                {
                    if (_hazardousParameterUsageBuilder != null &&
                        IsHazardousIfNull(operation))
                    {
                        var notValidatedLocations = GetNotValidatedLocations(operation);
                        HandlePotentiallyHazardousOperation(operation, notValidatedLocations);
                    }
                }

                return value;
            }

            public override ParameterValidationAbstractValue VisitReDimClause(IReDimClauseOperation operation, object? argument)
            {
                var value = base.VisitReDimClause(operation, argument);
                MarkValidatedLocations(operation.Operand);
                return value;
            }

            public override ParameterValidationAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object? argument)
            {
                var value = base.VisitObjectCreation(operation, argument);
                ProcessRegularInvocationOrCreation(operation.Constructor, operation.Arguments, operation);
                return value;
            }

            public override ParameterValidationAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                IMethodSymbol method,
                IOperation? visitedInstance,
                ImmutableArray<IArgumentOperation> visitedArguments,
                bool invokedAsDelegate,
                IOperation originalOperation,
                ParameterValidationAbstractValue defaultValue)
            {
                var value = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);
                ProcessRegularInvocationOrCreation(method, visitedArguments, originalOperation);
                return value;
            }

            public override ParameterValidationAbstractValue VisitInvocation_Lambda(
                IFlowAnonymousFunctionOperation lambda,
                ImmutableArray<IArgumentOperation> visitedArguments,
                IOperation originalOperation,
                ParameterValidationAbstractValue defaultValue)
            {
                var value = base.VisitInvocation_Lambda(lambda, visitedArguments, originalOperation, defaultValue);
                ProcessLambdaOrLocalFunctionInvocation(lambda.Symbol, originalOperation);
                return value;
            }

            public override ParameterValidationAbstractValue VisitInvocation_LocalFunction(
                IMethodSymbol localFunction,
                ImmutableArray<IArgumentOperation> visitedArguments,
                IOperation originalOperation,
                ParameterValidationAbstractValue defaultValue)
            {
                var value = base.VisitInvocation_LocalFunction(localFunction, visitedArguments, originalOperation, defaultValue);
                ProcessLambdaOrLocalFunctionInvocation(localFunction, originalOperation);
                return value;
            }

            private void ProcessRegularInvocationOrCreation(IMethodSymbol? targetMethod, ImmutableArray<IArgumentOperation> arguments, IOperation operation)
            {
                if (targetMethod == null)
                    return;

                Debug.Assert(!targetMethod.IsLambdaOrLocalFunctionOrDelegate());

                if (targetMethod.IsArgumentNullCheckMethod())
                {
                    if (arguments.Length == 1)
                    {
                        // "static bool SomeType.IsNullXXX(obj)" check.
                        MarkValidatedLocations(arguments[0]);
                    }
                }
                else if (!targetMethod.Parameters.IsEmpty &&
                         !arguments.IsEmpty &&
                         ExceptionNamedType != null &&
                         targetMethod.ContainingType.DerivesFrom(ExceptionNamedType))
                {
                    // FxCop compat: special cases handled by FxCop.
                    //  1. First argument of type System.Runtime.Serialization.SerializationInfo to System.Exception.GetObjectData or its override is validated.
                    //  2. First argument of type System.Runtime.Serialization.SerializationInfo to constructor of System.Exception or its subtype is validated.
                    if (targetMethod.IsGetObjectData(SerializationInfoNamedType, StreamingContextNamedType) ||
                        targetMethod.IsSerializationConstructor(SerializationInfoNamedType, StreamingContextNamedType))
                    {
                        MarkValidatedLocations(arguments[0]);
                    }
                }
                else if (_hazardousParameterUsageBuilder != null &&
                         !targetMethod.IsExternallyVisible() &&
                         TryGetInterproceduralAnalysisResult(operation, out var invokedMethodAnalysisResult))
                {
                    // Check if this private/internal method that has hazardous usages of non-validated argument.
                    Debug.Assert(!targetMethod.IsVirtual && !targetMethod.IsOverride);

                    var hazardousParameterUsagesInInvokedMethod = invokedMethodAnalysisResult.HazardousParameterUsages;
                    if (!hazardousParameterUsagesInInvokedMethod.IsEmpty)
                    {
                        foreach (var argument in arguments)
                        {
                            var notValidatedLocations = GetNotValidatedLocations(argument);
                            foreach (var location in notValidatedLocations)
                            {
                                var parameter = (IParameterSymbol)location.Symbol!;
                                if (hazardousParameterUsagesInInvokedMethod.ContainsKey(parameter))
                                {
                                    HandlePotentiallyHazardousOperation(argument, notValidatedLocations);
                                    break;
                                }
                            }
                        }
                    }
                }

                // Mark arguments passed to parameters of null check validation methods as validated.
                // Also mark arguments passed to parameters with ValidatedNotNullAttribute as validated.
                var isNullCheckValidationMethod = DataFlowAnalysisContext.IsNullCheckValidationMethod(targetMethod.OriginalDefinition);
                foreach (var argument in arguments)
                {
                    var notValidatedLocations = GetNotValidatedLocations(argument);
                    if (notValidatedLocations.Any())
                    {
                        if (isNullCheckValidationMethod || HasAnyNullValidationAttribute(argument.Parameter))
                        {
                            MarkValidatedLocations(argument);
                        }
                    }
                }
            }

            private void ProcessLambdaOrLocalFunctionInvocation(IMethodSymbol targetMethod, IOperation invocation)
            {
                Debug.Assert(targetMethod.MethodKind is MethodKind.LambdaMethod or MethodKind.LocalFunction);

                // Lambda and local function invocations can access captured variables.
                if (_hazardousParameterUsageBuilder != null &&
                    TryGetInterproceduralAnalysisResult(invocation, out var invokedMethodAnalysisResult))
                {
                    var notValidatedLocations = CurrentAnalysisData.Keys.Where(IsNotOrMaybeValidatedLocation);
                    if (notValidatedLocations.Any())
                    {
                        var hazardousParameterUsagesInInvokedMethod = invokedMethodAnalysisResult.HazardousParameterUsages;
                        foreach (var kvp in hazardousParameterUsagesInInvokedMethod)
                        {
                            var parameter = kvp.Key;
                            var syntaxNode = kvp.Value;
                            if (!_hazardousParameterUsageBuilder.ContainsKey(parameter))
                            {
                                HandleHazardousOperation(syntaxNode, notValidatedLocations.Where(l => Equals(l.Symbol, parameter)));
                            }
                        }
                    }
                }
            }

            public override ParameterValidationAbstractValue VisitBinaryOperatorCore(IBinaryOperation operation, object? argument)
            {
                var value = base.VisitBinaryOperatorCore(operation, argument);

                // Mark a location as validated on paths where we know it is non-null.
                //     if (x != null)
                //     {
                //         // Validated on this path
                //     }

                // if (x != null)
                // {
                //      // This code path
                // }
                var isNullNotEqualsOnWhenTrue = FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue &&
                    (operation.OperatorKind == BinaryOperatorKind.NotEquals || operation.OperatorKind == BinaryOperatorKind.ObjectValueNotEquals);

                // if (x == null) { ... }
                // else
                // {
                //      // This code path
                // }
                var isNullEqualsOnWhenFalse = FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse &&
                    (operation.OperatorKind == BinaryOperatorKind.Equals || operation.OperatorKind == BinaryOperatorKind.ObjectValueEquals);

                if (isNullNotEqualsOnWhenTrue || isNullEqualsOnWhenFalse)
                {
                    if (GetNullAbstractValue(operation.RightOperand) == NullAbstractValue.Null)
                    {
                        // if (x != null)
                        MarkValidatedLocations(operation.LeftOperand);
                    }
                    else if (GetNullAbstractValue(operation.LeftOperand) == NullAbstractValue.Null)
                    {
                        // if (null != x)
                        MarkValidatedLocations(operation.RightOperand);
                    }
                }

                return value;
            }

            public override ParameterValidationAbstractValue VisitIsNull(IIsNullOperation operation, object? argument)
            {
                var value = base.VisitIsNull(operation, argument);

                // Mark a location as validated on paths where user has performed an IsNull check.
                // See comments in VisitBinaryOperatorCore override above for further details.
                MarkValidatedLocations(operation.Operand);

                return value;
            }

            public override ParameterValidationAbstractValue VisitIsPattern(IIsPatternOperation operation, object? argument)
            {
                var value = base.VisitIsPattern(operation, argument);

                // Mark a location as validated on false path where user has performed an IsPattern check with null on true path.
                // See comments in VisitBinaryOperatorCore override above for further details.
                if (FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse &&
                    GetNullAbstractValue(operation.Pattern) == NullAbstractValue.Null)
                {
                    MarkValidatedLocations(operation.Value);
                }

                // Mark a location as validated on true path where user has performed an IsPattern check with not null on true path.
                if (FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue &&
                    GetNullAbstractValue(operation.Pattern) == NullAbstractValue.NotNull)
                {
                    MarkValidatedLocations(operation.Value);
                }

                return value;
            }

            public override ParameterValidationAbstractValue VisitIsType(IIsTypeOperation operation, object? argument)
            {
                var value = base.VisitIsType(operation, argument);

                // Mark a location as validated on paths where user has performed an IsType check, for example 'x is object'.
                // See comments in VisitBinaryOperatorCore override above for further details.
                if (FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue)
                {
                    MarkValidatedLocations(operation.ValueOperand);
                }

                return value;
            }

            #endregion
        }
    }
}
