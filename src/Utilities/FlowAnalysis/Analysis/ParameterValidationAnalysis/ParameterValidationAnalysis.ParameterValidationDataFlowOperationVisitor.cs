// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using ParameterValidationAnalysisData = IDictionary<AbstractLocation, ParameterValidationAbstractValue>;

    internal partial class ParameterValidationAnalysis : ForwardDataFlowAnalysis<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationBlockAnalysisResult, ParameterValidationAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the location validation values across a given statement in a basic block.
        /// </summary>
        private sealed class ParameterValidationDataFlowOperationVisitor :
            AbstractLocationDataFlowOperationVisitor<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationAbstractValue>
        {
            private readonly ImmutableDictionary<IParameterSymbol, SyntaxNode>.Builder _hazardousParameterUsageBuilderOpt;

            public ParameterValidationDataFlowOperationVisitor(ParameterValidationAnalysisContext analysisContext)
                : base(analysisContext)
            {
                Debug.Assert(analysisContext.OwningSymbol.Kind == SymbolKind.Method);
                Debug.Assert(analysisContext.PointsToAnalysisResultOpt != null);

                if (analysisContext.TrackHazardousParameterUsages)
                {
                    _hazardousParameterUsageBuilderOpt = ImmutableDictionary.CreateBuilder<IParameterSymbol, SyntaxNode>();
                }
            }

            public override int GetHashCode()
            {
                return HashUtilities.Combine(_hazardousParameterUsageBuilderOpt?.GetHashCode() ?? 0, base.GetHashCode());
            }

            public ImmutableDictionary<IParameterSymbol, SyntaxNode> HazardousParameterUsages
            {
                get
                {
                    Debug.Assert(_hazardousParameterUsageBuilderOpt != null);
                    return _hazardousParameterUsageBuilderOpt.ToImmutable();
                }
            }

            protected override ParameterValidationAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => ValueDomain.Bottom;

            protected override bool HasAnyAbstractValue(ParameterValidationAnalysisData data) => data.Count > 0;

            protected override ParameterValidationAbstractValue GetAbstractValue(AbstractLocation location)
                => CurrentAnalysisData.TryGetValue(location, out var value) ? value : ValueDomain.Bottom;

            protected override void ResetCurrentAnalysisData() => ResetAnalysisData(CurrentAnalysisData);

            private bool IsTrackedLocation(AbstractLocation location) =>
                location.SymbolOpt is IParameterSymbol parameter && parameter.Type.IsReferenceType && parameter.ContainingSymbol == OwningSymbol;

            private bool IsNotOrMaybeValidatedLocation(AbstractLocation location) =>
                CurrentAnalysisData.TryGetValue(location, out var value) &&
                (value == ParameterValidationAbstractValue.NotValidated || value == ParameterValidationAbstractValue.MayBeValidated);

            protected override void SetAbstractValue(AbstractLocation location, ParameterValidationAbstractValue value)
            {
                if (IsTrackedLocation(location))
                {
                    CurrentAnalysisData[location] = value;
                }
            }

            private void SetAbstractValue(IEnumerable<AbstractLocation> locations, ParameterValidationAbstractValue value)
            {
                foreach (var location in locations)
                {
                    SetAbstractValue(location, value);
                }
            }

            protected override void SetAbstractValueForAssignment(IOperation target, IOperation assignedValueOperation, ParameterValidationAbstractValue assignedValue, bool mayBeAssignment = false)
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
                    var value = HasValidatedNotNullAttribute(parameter) ? ParameterValidationAbstractValue.Validated : ParameterValidationAbstractValue.NotValidated;
                    SetAbstractValue(pointsToAbstractValue.Locations, value);
                }
            }

            private static bool HasValidatedNotNullAttribute(IParameterSymbol parameter)
                => parameter.GetAttributes().Any(attr => attr.AttributeClass.Name.Equals("ValidatedNotNullAttribute", StringComparison.OrdinalIgnoreCase));

            protected override void SetValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, PointsToAbstractValue pointsToAbstractValue)
            {
                // Mark parameters as validated if they are non-null at all non-exception return paths and null at one of the unhandled throw operations.
                var notValidatedLocations = pointsToAbstractValue.Locations.Where(IsNotOrMaybeValidatedLocation);
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

                switch (operation.Parent)
                {
                    case IMemberReferenceOperation memberReference:
                        return memberReference.Instance == operation;

                    case IArrayElementReferenceOperation arrayElementReference:
                        return arrayElementReference.ArrayReference == operation;

                    case IInvocationOperation invocation:
                        return invocation.Instance == operation;
                }

                return false;
            }

            private void HandlePotentiallyHazardousOperation(IOperation operation, IEnumerable<AbstractLocation> nonValidatedLocations)
            {
                Debug.Assert(_hazardousParameterUsageBuilderOpt != null);

                if (GetNullAbstractValue(operation) == NullAbstractValue.NotNull)
                {
                    // We are sure the value is non-null, so cannot be hazardous.
                    return;
                }

                foreach (var location in nonValidatedLocations)
                {
                    Debug.Assert(IsNotOrMaybeValidatedLocation(location));

                    var parameter = (IParameterSymbol)location.SymbolOpt;
                    SyntaxNode syntaxNode = operation.Syntax;
                    if (!_hazardousParameterUsageBuilderOpt.TryGetValue(parameter, out SyntaxNode currentSyntaxNode) ||
                        syntaxNode.SpanStart < currentSyntaxNode.SpanStart)
                    {
                        _hazardousParameterUsageBuilderOpt[parameter] = syntaxNode;
                    }
                }
            }

            protected override ParameterValidationAnalysisData MergeAnalysisData(ParameterValidationAnalysisData value1, ParameterValidationAnalysisData value2)
                => ParameterValidationAnalysisDomainInstance.Merge(value1, value2);
            protected override ParameterValidationAnalysisData GetClonedAnalysisData(ParameterValidationAnalysisData analysisData)
                => GetClonedAnalysisDataHelper(analysisData);
            protected override bool Equals(ParameterValidationAnalysisData value1, ParameterValidationAnalysisData value2)
                => EqualsHelper(value1, value2);

            #region Visit overrides
            public override ParameterValidationAbstractValue Visit(IOperation operation, object argument)
            {
                var value = base.Visit(operation, argument);
                if (operation != null)
                {
                    if (_hazardousParameterUsageBuilderOpt != null &&
                        IsHazardousIfNull(operation))
                    {
                        var notValidatedLocations = GetNotValidatedLocations(operation);
                        HandlePotentiallyHazardousOperation(operation, notValidatedLocations);
                    }
                }

                return value;
            }

            public override ParameterValidationAbstractValue VisitArgumentCore(IArgumentOperation operation, object argument)
            {
                var value = base.VisitArgumentCore(operation, argument);

                // Arguments to validation methods must be marked as validated.
                var notValidatedLocations = GetNotValidatedLocations(operation);
                if (notValidatedLocations.Any())
                {
                    if (GetOrComputeAnalysisResultForInvokedMethod != null)
                    {
                        IMethodSymbol targetMethod = null;
                        if (operation.Parent is IInvocationOperation invocation)
                        {
                            targetMethod = invocation.TargetMethod.OriginalDefinition;
                        }
                        else if (operation.Parent is IObjectCreationOperation objectCreation)
                        {
                            targetMethod = objectCreation.Constructor.OriginalDefinition;
                        }

                        if (targetMethod != null &&
                            targetMethod != OwningSymbol)
                        {
                            if (targetMethod.ContainingType.SpecialType == SpecialType.System_String)
                            {
                                if (targetMethod.IsStatic &&
                                    targetMethod.Name.StartsWith("IsNull", StringComparison.Ordinal) &&
                                    targetMethod.Parameters.Length == 1)
                                {
                                    // string.IsNullOrXXX check.
                                    SetAbstractValue(notValidatedLocations, ParameterValidationAbstractValue.Validated);
                                }
                            }
                            else if (WellKnownTypeProvider.Exception != null && targetMethod.ContainingType.DerivesFrom(WellKnownTypeProvider.Exception))
                            {
                                // FxCop compat: special cases handled by FxCop.
                                //  1. First argument of type System.Runtime.Serialization.SerializationInfo to System.Exception.GetObjectData or its override is validated.
                                //  2. First argument of type System.Runtime.Serialization.SerializationInfo to constructor of System.Exception or its subtype is validated.
                                if (operation.Parameter.Type == WellKnownTypeProvider.SerializationInfo)
                                {
                                    switch (targetMethod.MethodKind)
                                    {
                                        case MethodKind.Ordinary:
                                            if (targetMethod.Name.Equals("GetObjectData", StringComparison.OrdinalIgnoreCase))
                                            {
                                                SetAbstractValue(notValidatedLocations, ParameterValidationAbstractValue.Validated);
                                            }
                                            break;

                                        case MethodKind.Constructor:
                                            SetAbstractValue(notValidatedLocations, ParameterValidationAbstractValue.Validated);
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                var methodTopmostBlock = targetMethod.GetTopmostOperationBlock(WellKnownTypeProvider.Compilation);
                                if (methodTopmostBlock != null)
                                {
                                    ParameterValidationAnalysisResult invokedMethodAnalysisResult = ComputeAnalysisResultForInvokedMethod(methodTopmostBlock, targetMethod);
                                    if (invokedMethodAnalysisResult != null)
                                    {
                                        var hazardousParameterUsagesInInvokedMethod = invokedMethodAnalysisResult.HazardousParameterUsages;
                                        Debug.Assert(hazardousParameterUsagesInInvokedMethod != null);
                                        var operationParameter = operation.Parameter.OriginalDefinition;

                                        // Non-validated argument passed to private/internal methods might be hazardous.
                                        if (hazardousParameterUsagesInInvokedMethod.ContainsKey(operationParameter))
                                        {
                                            if (_hazardousParameterUsageBuilderOpt != null && !targetMethod.IsExternallyVisible())
                                            {
                                                HandlePotentiallyHazardousOperation(operation, notValidatedLocations);
                                            }
                                        }
                                        else if (!targetMethod.IsVirtual && !targetMethod.IsOverride)
                                        {
                                            // Check if this is a non-virtual non-override method that validates the argument.
                                            BasicBlock invokedMethodExitBlock = invokedMethodAnalysisResult.ControlFlowGraph.GetExit();
                                            foreach (var kvp in invokedMethodAnalysisResult[invokedMethodExitBlock].OutputData)
                                            {
                                                AbstractLocation parameterLocation = kvp.Key;
                                                ParameterValidationAbstractValue parameterValue = kvp.Value;
                                                Debug.Assert(parameterLocation.SymbolOpt is IParameterSymbol invokedMethodParameter && invokedMethodParameter.ContainingSymbol == targetMethod);

                                                // Check if the matching parameter was validated by the invoked method.
                                                if ((IParameterSymbol)parameterLocation.SymbolOpt == operationParameter &&
                                                    parameterValue == ParameterValidationAbstractValue.Validated)
                                                {
                                                    SetAbstractValue(notValidatedLocations, ParameterValidationAbstractValue.Validated);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return value;

                // Local functions
                ParameterValidationAnalysisResult ComputeAnalysisResultForInvokedMethod(IOperation methodTopmostBlock, IMethodSymbol targetMethod)
                {
                    if (GetOrComputeAnalysisResultForInvokedMethod == null)
                    {
                        return null;
                    }

                    var cfg = methodTopmostBlock.GetEnclosingControlFlowGraph();
                    var pointsToAnalysisResult = ComputePointsToAnalysisResultForParameterValidationAnalysis(cfg, targetMethod, WellKnownTypeProvider);

                    // We do not want to analyze more than one level down the call stack.
                    // Hence, we pass 'getOrComputeAnalysisResultForInvokedMethod = null' for the invoked method analysis.
                    var analysisContext = new ParameterValidationAnalysisContext(ValueDomain, WellKnownTypeProvider, cfg,
                        targetMethod, PessimisticAnalysis, pointsToAnalysisResult, getOrComputeAnalysisResultForInvokedMethod: null,
                        trackHazardousParameterUsages: _hazardousParameterUsageBuilderOpt != null);

                    return GetOrComputeAnalysisResultForInvokedMethod(analysisContext);
                }
            }

            #endregion
        }
    }
}
