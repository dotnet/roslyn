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

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = IDictionary<AbstractLocation, DisposeAbstractValue>;

    internal partial class DisposeAnalysis : ForwardDataFlowAnalysis<DisposeAnalysisData, DisposeBlockAnalysisResult, DisposeAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the dispose values across a given statement in a basic block.
        /// </summary>
        private sealed class DisposeDataFlowOperationVisitor : AbstractLocationDataFlowOperationVisitor<DisposeAnalysisData, DisposeAbstractValue>
        {
            private readonly ImmutableHashSet<INamedTypeSymbol> _disposeOwnershipTransferLikelyTypes;
            private readonly Dictionary<IFieldSymbol, PointsToAbstractValue> _trackedInstanceFieldLocationsOpt;

            // Invoking an instance method may likely invalidate all the instance field analysis state, i.e.
            // reference type fields might be re-assigned to point to different objects in the called method.
            // An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
            // A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
            // For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.
            private const bool pessimisticAnalysis = false;

            public DisposeDataFlowOperationVisitor(
                DisposeAbstractValueDomain valueDomain,
                ISymbol owningSymbol,
                WellKnownTypeProvider wellKnownTypeProvider,
                ControlFlowGraph cfg,
                ImmutableHashSet<INamedTypeSymbol> disposeOwnershipTransferLikelyTypes,
                bool trackInstanceFields,
                DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResult)
                : base(valueDomain, owningSymbol, wellKnownTypeProvider, cfg, pessimisticAnalysis,
                      predicateAnalysis: false, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResult)
            {
                Debug.Assert(wellKnownTypeProvider.IDisposable != null);
                Debug.Assert(wellKnownTypeProvider.CollectionTypes.All(ct => ct.TypeKind == TypeKind.Interface));
                Debug.Assert(disposeOwnershipTransferLikelyTypes != null);
                Debug.Assert(pointsToAnalysisResult != null);

                _disposeOwnershipTransferLikelyTypes = disposeOwnershipTransferLikelyTypes;
                if (trackInstanceFields)
                {
                    _trackedInstanceFieldLocationsOpt = new Dictionary<IFieldSymbol, PointsToAbstractValue>();
                }
            }

            public override int GetHashCode()
            {
                return HashUtilities.Combine(_trackedInstanceFieldLocationsOpt?.GetHashCode() ?? 0, base.GetHashCode());
            }

            public ImmutableDictionary<IFieldSymbol, PointsToAbstractValue> TrackedInstanceFieldPointsToMap
            {
                get
                {
                    if (_trackedInstanceFieldLocationsOpt == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return _trackedInstanceFieldLocationsOpt.ToImmutableDictionary();
                }
            }

            protected override DisposeAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => DisposeAbstractValue.NotDisposable;

            protected override DisposeAbstractValue GetAbstractValue(AbstractLocation location) => CurrentAnalysisData.TryGetValue(location, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

            protected override bool HasAnyAbstractValue(DisposeAnalysisData data) => data.Count > 0;

            protected override void SetAbstractValue(AbstractLocation location, DisposeAbstractValue value)
            {
                Debug.Assert(location.IsNull || location.LocationTypeOpt.IsDisposable(WellKnownTypeProvider.IDisposable));

                if (!location.IsNull)
                {
                    CurrentAnalysisData[location] = value;
                }
            }

            protected override void ResetCurrentAnalysisData() => ResetAnalysisData(CurrentAnalysisData);

            protected override DisposeAbstractValue HandleInstanceCreation(ITypeSymbol instanceType, PointsToAbstractValue instanceLocation, DisposeAbstractValue defaultValue)
            {
                defaultValue = DisposeAbstractValue.NotDisposable;

                if (!instanceType.IsDisposable(WellKnownTypeProvider.IDisposable))
                {
                    return defaultValue;
                }

                // Special case: Do not track System.Threading.Tasks.Task as you are not required to dispose them.
                if (WellKnownTypeProvider.Task != null && instanceType.DerivesFrom(WellKnownTypeProvider.Task, baseTypesOnly: true))
                {
                    return defaultValue;
                }

                SetAbstractValue(instanceLocation, DisposeAbstractValue.NotDisposed);
                return DisposeAbstractValue.NotDisposed;
            }

            private void HandleDisposingOperation(IOperation disposingOperation, IOperation disposedInstance)
            {
                if (disposedInstance.Type?.IsDisposable(WellKnownTypeProvider.IDisposable) == false)
                {
                    return;
                }

                PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(disposedInstance);
                foreach (AbstractLocation location in instanceLocation.Locations)
                {
                    if (CurrentAnalysisData.TryGetValue(location, out DisposeAbstractValue currentDisposeValue))
                    {
                        DisposeAbstractValue disposeValue = currentDisposeValue.WithNewDisposingOperation(disposingOperation);
                        SetAbstractValue(location, disposeValue);
                    }
                }
            }

            private void HandlePossibleEscapingOperation(IOperation escapingOperation, IOperation escapedInstance)
            {
                Debug.Assert(escapingOperation != null);
                Debug.Assert(escapedInstance != null);

                PointsToAbstractValue pointsToValue = GetPointsToAbstractValue(escapedInstance);
                foreach (AbstractLocation location in pointsToValue.Locations)
                {
                    if (CurrentAnalysisData.TryGetValue(location, out DisposeAbstractValue currentDisposeValue))
                    {
                        DisposeAbstractValue newDisposeValue = currentDisposeValue.WithNewEscapingOperation(escapingOperation);
                        SetAbstractValue(location, newDisposeValue);
                    }
                }
            }

            private void HandlePossibleEscapingForAssignment(IOperation target, IOperation value, IOperation operation)
            {
                // FxCop compat: The object assigned to a field or a property or an array element is considered escaped.
                // TODO: Perform better analysis for array element assignments as we already track element locations.
                // https://github.com/dotnet/roslyn-analyzers/issues/1577
                // Also consider arguments passed ByRef as escaped.
                if (target is IMemberReferenceOperation ||
                    target.Kind == OperationKind.ArrayElementReference ||
                    (value is IArgumentOperation argument && argument.Parameter.RefKind == RefKind.Ref))
                {
                    HandlePossibleEscapingOperation(operation, value);
                }
            }

            protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, DisposeAbstractValue value)
            {
                HandlePossibleEscapingOperation(arrayCreation, initializer);
            }

            protected override void SetAbstractValueForAssignment(IOperation target, IOperation assignedValueOperation, DisposeAbstractValue assignedValue, bool mayBeAssignment = false)
            {
                if (assignedValueOperation == null)
                {
                    return;
                }

                // Temporary workaround for missing dataflow support for tuples
                // https://github.com/dotnet/roslyn-analyzers/issues/1571
                if (assignedValueOperation?.Kind == OperationKind.Tuple)
                {
                    HandlePossibleEscapingOperation(escapingOperation: assignedValueOperation, escapedInstance: target);
                    return;
                }

                HandlePossibleEscapingForAssignment(target, assignedValueOperation, assignedValueOperation);
            }

            protected override void SetValueForParameterPointsToLocationOnEntry(IParameterSymbol parameter, PointsToAbstractValue pointsToAbstractValue)
            {
                if (_disposeOwnershipTransferLikelyTypes.Contains(parameter.Type))
                {
                    SetAbstractValue(pointsToAbstractValue, DisposeAbstractValue.NotDisposed);
                }
            }

            protected override void SetValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, PointsToAbstractValue pointsToAbstractValue)
            {
                if (parameter.RefKind != RefKind.None &&
                    !pointsToAbstractValue.Locations.IsEmpty &&
                    parameter.Type.IsDisposable(WellKnownTypeProvider.IDisposable))
                {
                    SetAbstractValue(pointsToAbstractValue, ValueDomain.UnknownOrMayBeValue);
                }
            }

            protected override DisposeAbstractValue ComputeAnalysisValueForOutArgument(IArgumentOperation operation, DisposeAbstractValue defaultValue)
            {
                // Special case: don't flag "out" arguments for "bool TryGetXXX(..., out value)" invocations.
                if (operation.Parent is IInvocationOperation invocation &&
                    invocation.TargetMethod.ReturnType.SpecialType == SpecialType.System_Boolean &&
                    invocation.TargetMethod.Name.StartsWith("TryGet", StringComparison.Ordinal) &&
                    invocation.Arguments[invocation.Arguments.Length - 1] == operation)
                {
                    return DisposeAbstractValue.NotDisposable;
                }

                return base.ComputeAnalysisValueForOutArgument(operation, defaultValue);
            }

            protected override DisposeAnalysisData MergeAnalysisData(DisposeAnalysisData value1, DisposeAnalysisData value2)
                => DisposeAnalysisDomainInstance.Merge(value1, value2);
            protected override DisposeAnalysisData GetClonedAnalysisData(DisposeAnalysisData analysisData)
                => GetClonedAnalysisDataHelper(CurrentAnalysisData);
            protected override bool Equals(DisposeAnalysisData value1, DisposeAnalysisData value2)
                => EqualsHelper(value1, value2);

            #region Visitor methods
            public override DisposeAbstractValue DefaultVisit(IOperation operation, object argument)
            {
                var value = base.DefaultVisit(operation, argument);
                return DisposeAbstractValue.NotDisposable;
            }

            // FxCop compat: Catches things like static calls to File.Open() and Create()
            private static bool IsDisposableCreationSpecialCase(IInvocationOperation operation)
                => operation.TargetMethod.IsStatic &&
                   (operation.TargetMethod.Name.StartsWith("create", StringComparison.OrdinalIgnoreCase) ||
                    operation.TargetMethod.Name.StartsWith("open", StringComparison.OrdinalIgnoreCase));

            public override DisposeAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IInvocationOperation operation, object argument)
            {
                var value = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(operation, argument);
                var disposeMethodKind = operation.TargetMethod.GetDisposeMethodKind(WellKnownTypeProvider.IDisposable);
                switch (disposeMethodKind)
                {
                    case DisposeMethodKind.Dispose:
                    case DisposeMethodKind.DisposeBool:
                        HandleDisposingOperation(operation, operation.Instance);
                        break;

                    case DisposeMethodKind.Close:
                        // FxCop compat: Calling "this.Close" shouldn't count as disposing the object within the implementation of Dispose.
                        if (operation.Instance?.Kind != OperationKind.InstanceReference)
                        {
                            goto case DisposeMethodKind.Dispose;
                        }
                        break;

                    default:
                        // FxCop compat: Catches things like static calls to File.Open() and Create()
                        if (IsDisposableCreationSpecialCase(operation))
                        {
                            var instanceLocation = GetPointsToAbstractValue(operation);
                            return HandleInstanceCreation(operation.Type, instanceLocation, value);
                        }
                        else if (operation.Arguments.Length > 0 &&
                            operation.TargetMethod.IsCollectionAddMethod(WellKnownTypeProvider.CollectionTypes))
                        {
                            // FxCop compat: The object added to a collection is considered escaped.
                            var lastArgument = operation.Arguments[operation.Arguments.Length - 1];
                            HandlePossibleEscapingOperation(operation, lastArgument.Value);
                        }

                        break;
                }

                return value;
            }

            protected override DisposeAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object argument)
            {
                var value = base.VisitAssignmentOperation(operation, argument);
                HandlePossibleEscapingForAssignment(operation.Target, operation.Value, operation);
                return value;
            }

            public override DisposeAbstractValue VisitArgumentCore(IArgumentOperation operation, object argument)
            {
                var value = base.VisitArgumentCore(operation, argument);
                var possibleEscape = false;

                if (operation.Parameter.Type.IsDisposable(WellKnownTypeProvider.IDisposable))
                {
                    // Discover if a disposable object is being passed into the creation method for this new disposable object
                    // and if the new disposable object assumes ownership of that passed in disposable object.
                    if ((operation.Parent is IObjectCreationOperation objectCreation ||
                         operation.Parent is IInvocationOperation invocation && IsDisposableCreationSpecialCase(invocation)) &&
                        _disposeOwnershipTransferLikelyTypes.Contains(operation.Parameter.Type))
                    {
                        possibleEscape = true;
                    }
                    else if (operation.Parameter.RefKind == RefKind.Ref)
                    {
                        // Argument passed by ref is considered escaped.
                        possibleEscape = true;
                    }
                }

                if (possibleEscape)
                {
                    HandlePossibleEscapingOperation(operation, operation.Value);
                }

                return value;
            }

            protected override void ProcessReturnValue(IOperation returnValue)
            {
                if (returnValue != null)
                {
                    HandlePossibleEscapingOperation(escapingOperation: returnValue, escapedInstance: returnValue);
                }
            }

            public override DisposeAbstractValue VisitConversion(IConversionOperation operation, object argument)
            {
                var value = base.VisitConversion(operation, argument);
                if (operation.OperatorMethod != null)
                {
                    // Conservatively handle user defined conversions.
                    HandlePossibleEscapingOperation(operation, operation.Operand);
                }

                return value;
            }

            public override DisposeAbstractValue VisitFieldReference(IFieldReferenceOperation operation, object argument)
            {
                var value = base.VisitFieldReference(operation, argument);
                if (_trackedInstanceFieldLocationsOpt != null &&
                    !operation.Field.IsStatic &&
                    operation.Instance?.Kind == OperationKind.InstanceReference)
                {
                    if (!_trackedInstanceFieldLocationsOpt.TryGetValue(operation.Field, out PointsToAbstractValue pointsToAbstractValue))
                    {
                        pointsToAbstractValue = GetPointsToAbstractValue(operation);
                        if (HandleInstanceCreation(operation.Type, pointsToAbstractValue, DisposeAbstractValue.NotDisposable) != DisposeAbstractValue.NotDisposable)
                        {
                            _trackedInstanceFieldLocationsOpt.Add(operation.Field, pointsToAbstractValue);
                        }
                    }
                }

                return value;
            }

            #endregion
        }
    }
}
