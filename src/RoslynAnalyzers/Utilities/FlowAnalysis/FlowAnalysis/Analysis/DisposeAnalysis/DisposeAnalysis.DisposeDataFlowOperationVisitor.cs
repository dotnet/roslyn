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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    using DisposeAnalysisData = DictionaryAnalysisData<AbstractLocation, DisposeAbstractValue>;

    public partial class DisposeAnalysis : ForwardDataFlowAnalysis<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeBlockAnalysisResult, DisposeAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the dispose values across a given statement in a basic block.
        /// </summary>
        private sealed class DisposeDataFlowOperationVisitor : AbstractLocationDataFlowOperationVisitor<DisposeAnalysisData, DisposeAnalysisContext, DisposeAnalysisResult, DisposeAbstractValue>
        {
            private readonly Dictionary<IFieldSymbol, PointsToAbstractValue>? _trackedInstanceFieldLocations;
            private ImmutableHashSet<INamedTypeSymbol> DisposeOwnershipTransferLikelyTypes => DataFlowAnalysisContext.DisposeOwnershipTransferLikelyTypes;
            private bool DisposeOwnershipTransferAtConstructor => DataFlowAnalysisContext.DisposeOwnershipTransferAtConstructor;
            private bool DisposeOwnershipTransferAtMethodCall => DataFlowAnalysisContext.DisposeOwnershipTransferAtMethodCall;

            public DisposeDataFlowOperationVisitor(DisposeAnalysisContext analysisContext)
                : base(analysisContext)
            {
                Debug.Assert(IDisposableNamedType != null);
                Debug.Assert(CollectionNamedTypes.All(ct => ct.TypeKind == TypeKind.Interface));
                Debug.Assert(analysisContext.DisposeOwnershipTransferLikelyTypes != null);
                Debug.Assert(analysisContext.PointsToAnalysisResult != null);

                if (analysisContext.TrackInstanceFields)
                {
                    _trackedInstanceFieldLocations = [];
                }
            }

            public ImmutableDictionary<IFieldSymbol, PointsToAbstractValue> TrackedInstanceFieldPointsToMap
            {
                get
                {
                    if (_trackedInstanceFieldLocations == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return _trackedInstanceFieldLocations.ToImmutableDictionary();
                }
            }

            protected override DisposeAbstractValue GetAbstractDefaultValue(ITypeSymbol? type) => DisposeAbstractValue.NotDisposable;

            protected override DisposeAbstractValue GetAbstractValue(AbstractLocation location) => CurrentAnalysisData.TryGetValue(location, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

            protected override bool HasAnyAbstractValue(DisposeAnalysisData data) => data.Count > 0;

            protected override void SetAbstractValue(AbstractLocation location, DisposeAbstractValue value)
            {
                if (!location.IsNull &&
                    location.LocationType != null &&
                    (!location.LocationType.IsValueType || location.LocationType.IsRefLikeType) &&
                    IsDisposable(location.LocationType))
                {
                    CurrentAnalysisData[location] = value;
                }
            }

            protected override void StopTrackingAbstractValue(AbstractLocation location) => CurrentAnalysisData.Remove(location);

            protected override void ResetCurrentAnalysisData() => ResetAnalysisData(CurrentAnalysisData);

            protected override DisposeAbstractValue HandleInstanceCreation(IOperation creation, PointsToAbstractValue instanceLocation, DisposeAbstractValue defaultValue)
            {
                if (ExecutingExceptionPathsAnalysisPostPass)
                {
                    base.HandlePossibleThrowingOperation(creation);
                }

                defaultValue = DisposeAbstractValue.NotDisposable;
                var instanceType = creation.Type;

                if (!IsDisposable(instanceType) ||
                    !IsCurrentBlockReachable())
                {
                    return defaultValue;
                }

                // Handle special cases where we don't want to track the type although we know
                // it is disposable (user option, special types...)
                if (DataFlowAnalysisContext.IsDisposableTypeNotRequiringToBeDisposed(instanceType))
                {
                    return defaultValue;
                }

                SetAbstractValue(instanceLocation, DisposeAbstractValue.NotDisposed);
                return DisposeAbstractValue.NotDisposed;
            }

            private void HandleDisposingOperation(IOperation disposingOperation, IOperation? disposedInstance)
            {
                if (disposedInstance == null || !IsDisposable(disposedInstance.Type))
                {
                    return;
                }

                PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(disposedInstance);
                foreach (AbstractLocation location in instanceLocation.Locations)
                {
                    if (CurrentAnalysisData.TryGetValue(location, out var currentDisposeValue))
                    {
                        DisposeAbstractValue disposeValue = currentDisposeValue.WithNewDisposingOperation(disposingOperation);
                        SetAbstractValue(location, disposeValue);
                    }
                }
            }

            private void HandlePossibleInvalidatingOperation(IOperation invalidatedInstance)
            {
                PointsToAbstractValue instanceLocation = GetPointsToAbstractValue(invalidatedInstance);
                foreach (AbstractLocation location in instanceLocation.Locations)
                {
                    if (CurrentAnalysisData.TryGetValue(location, out var currentDisposeValue) &&
                        currentDisposeValue.Kind != DisposeAbstractValueKind.NotDisposable)
                    {
                        SetAbstractValue(location, DisposeAbstractValue.Invalid);
                    }
                }
            }

            private void HandlePossibleEscapingOperation(IOperation escapingOperation, ImmutableHashSet<AbstractLocation> escapedLocations)
            {
                foreach (AbstractLocation escapedLocation in escapedLocations)
                {
                    if (CurrentAnalysisData.TryGetValue(escapedLocation, out var currentDisposeValue) &&
                        currentDisposeValue.Kind != DisposeAbstractValueKind.Unknown)
                    {
                        DisposeAbstractValue newDisposeValue = currentDisposeValue.WithNewEscapingOperation(escapingOperation);
                        SetAbstractValue(escapedLocation, newDisposeValue);
                    }
                }
            }

            protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, DisposeAbstractValue value)
            {
                // Escaping from array element assignment is handled in PointsTo analysis.
                // We do not need to do anything here.
            }

            protected override void SetAbstractValueForAssignment(IOperation target, IOperation? assignedValueOperation, DisposeAbstractValue assignedValue, bool mayBeAssignment = false)
            {
                // Assignments should automatically transfer PointsTo value.
                // We do not need to do anything here.
            }

            protected override void SetAbstractValueForTupleElementAssignment(AnalysisEntity tupleElementEntity, IOperation assignedValueOperation, DisposeAbstractValue assignedValue)
            {
                // Assigning to tuple elements should automatically transfer PointsTo value.
                // We do not need to do anything here.
            }

            protected override void SetValueForParameterPointsToLocationOnEntry(IParameterSymbol parameter, PointsToAbstractValue pointsToAbstractValue)
            {
                if (DisposeOwnershipTransferLikelyTypes.Contains(parameter.Type) ||
                    (DisposeOwnershipTransferAtConstructor && parameter.ContainingSymbol.IsConstructor()))
                {
                    SetAbstractValue(pointsToAbstractValue, DisposeAbstractValue.NotDisposed);
                }
            }

            protected override void EscapeValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, ImmutableHashSet<AbstractLocation> escapedLocations)
            {
                Debug.Assert(!escapedLocations.IsEmpty);
                Debug.Assert(parameter.RefKind != RefKind.None);
                var escapedDisposableLocations =
                    escapedLocations.Where(l => IsDisposable(l.LocationType));
                SetAbstractValue(escapedDisposableLocations, ValueDomain.UnknownOrMayBeValue);
            }

            protected override DisposeAbstractValue ComputeAnalysisValueForEscapedRefOrOutArgument(IArgumentOperation operation, DisposeAbstractValue defaultValue)
            {
                Debug.Assert(operation.Parameter!.RefKind is RefKind.Ref or RefKind.Out);

                // Special case: don't flag "out" arguments for "bool TryGetXXX(..., out value)" invocations.
                if (operation.Parent is IInvocationOperation invocation &&
                    invocation.TargetMethod.ReturnType.SpecialType == SpecialType.System_Boolean &&
                    invocation.TargetMethod.Name.StartsWith("TryGet", StringComparison.Ordinal) &&
                    invocation.Arguments[^1] == operation)
                {
                    return DisposeAbstractValue.NotDisposable;
                }

                return base.ComputeAnalysisValueForEscapedRefOrOutArgument(operation, defaultValue);
            }

            protected override DisposeAnalysisData MergeAnalysisData(DisposeAnalysisData value1, DisposeAnalysisData value2)
                => DisposeAnalysisDomainInstance.Merge(value1, value2);
            protected override void UpdateValuesForAnalysisData(DisposeAnalysisData targetAnalysisData)
                => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);
            protected override DisposeAnalysisData GetClonedAnalysisData(DisposeAnalysisData analysisData)
                => GetClonedAnalysisDataHelper(CurrentAnalysisData);
            public override DisposeAnalysisData GetEmptyAnalysisData()
                => GetEmptyAnalysisDataHelper();
            protected override DisposeAnalysisData GetExitBlockOutputData(DisposeAnalysisResult analysisResult)
                => GetClonedAnalysisDataHelper(analysisResult.ExitBlockOutput.Data);
            protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(DisposeAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
                => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData);
            protected override bool Equals(DisposeAnalysisData value1, DisposeAnalysisData value2)
                => EqualsHelper(value1, value2);

            #region Visitor methods
            public override DisposeAbstractValue DefaultVisit(IOperation operation, object? argument)
            {
                _ = base.DefaultVisit(operation, argument);
                return DisposeAbstractValue.NotDisposable;
            }

            public override DisposeAbstractValue Visit(IOperation? operation, object? argument)
            {
                var value = base.Visit(operation, argument);

                if (operation != null)
                    HandlePossibleEscapingOperation(operation, GetEscapedLocations(operation));

                return value;
            }

            protected override void HandlePossibleThrowingOperation(IOperation operation)
            {
                // Handle possible throwing operation.
                // Note that we handle the cases for object creation and method invocation
                // separately as these also lead to NotDisposed allocations, but
                // they should not be considered as part of current state when possible exception occurs.
                if (operation != null &&
                    operation.Kind != OperationKind.ObjectCreation &&
                    (operation is not IInvocationOperation invocation ||
                       invocation.TargetMethod.IsLambdaOrLocalFunctionOrDelegate()))
                {
                    base.HandlePossibleThrowingOperation(operation);
                }
            }

            // FxCop compat: Catches things like static calls to File.Open() and Create()
            private static bool IsDisposableCreationSpecialCase(IMethodSymbol targetMethod)
                => targetMethod.IsStatic &&
                   (targetMethod.Name.StartsWith("create", StringComparison.OrdinalIgnoreCase) ||
                    targetMethod.Name.StartsWith("open", StringComparison.OrdinalIgnoreCase));

            public override DisposeAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                IMethodSymbol method,
                IOperation? visitedInstance,
                ImmutableArray<IArgumentOperation> visitedArguments,
                bool invokedAsDelegate,
                IOperation originalOperation,
                DisposeAbstractValue defaultValue)
            {
                var value = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance,
                    visitedArguments, invokedAsDelegate, originalOperation, defaultValue);

                var disposeMethodKind = GetDisposeMethodKind(method);
                switch (disposeMethodKind)
                {
                    case DisposeMethodKind.Dispose:
                    case DisposeMethodKind.DisposeBool:
                    case DisposeMethodKind.DisposeAsync:
                        HandleDisposingOperation(originalOperation, visitedInstance);
                        return value;

                    case DisposeMethodKind.Close:
                        // FxCop compat: Calling "this.Close" shouldn't count as disposing the object within the implementation of Dispose.
                        if (visitedInstance?.Kind != OperationKind.InstanceReference)
                        {
                            goto case DisposeMethodKind.Dispose;
                        }

                        break;

                    default:
                        // FxCop compat: Catches things like static calls to File.Open() and Create()
                        if (IsDisposableCreationSpecialCase(method))
                        {
                            var instanceLocation = GetPointsToAbstractValue(originalOperation);
                            return HandleInstanceCreation(originalOperation, instanceLocation, value);
                        }

                        break;
                }

                if (ExecutingExceptionPathsAnalysisPostPass)
                {
                    base.HandlePossibleThrowingOperation(originalOperation);
                }

                return value;
            }

            protected override void ApplyInterproceduralAnalysisResult(
                DisposeAnalysisData resultData,
                bool isLambdaOrLocalFunction,
                bool hasDelegateTypeArgument,
                DisposeAnalysisResult analysisResult)
            {
                base.ApplyInterproceduralAnalysisResult(resultData, isLambdaOrLocalFunction, hasDelegateTypeArgument, analysisResult);

                // Apply the tracked instance field locations from interprocedural analysis.
                if (_trackedInstanceFieldLocations != null &&
                    analysisResult.TrackedInstanceFieldPointsToMap != null)
                {
                    foreach (var (field, pointsToValue) in analysisResult.TrackedInstanceFieldPointsToMap)
                    {
                        if (!_trackedInstanceFieldLocations.ContainsKey(field))
                        {
                            _trackedInstanceFieldLocations.Add(field, pointsToValue);
                        }
                    }
                }
            }

            protected override void PostProcessArgument(IArgumentOperation operation, bool isEscaped)
            {
                base.PostProcessArgument(operation, isEscaped);
                if (isEscaped)
                {
                    // Discover if a disposable object is being passed into the creation method for this new disposable object
                    // and if the new disposable object assumes ownership of that passed in disposable object.
                    if (IsDisposeOwnershipTransfer())
                    {
                        var pointsToValue = GetPointsToAbstractValue(operation.Value);
                        HandlePossibleEscapingOperation(operation, pointsToValue.Locations);
                        return;
                    }
                    else if (FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse &&
                        operation.Parameter?.RefKind == RefKind.Out &&
                        operation.Parent is IInvocationOperation invocation &&
                        invocation.TargetMethod.ReturnType.SpecialType == SpecialType.System_Boolean)
                    {
                        // Case 1:
                        //      // Assume 'obj' is not a valid object on the 'else' path.
                        //      if (TryXXX(out IDisposable obj))
                        //      {
                        //          obj.Dispose();
                        //      }
                        //
                        //      return;

                        // Case 2:
                        //      if (!TryXXX(out IDisposable obj))
                        //      {
                        //          return; // Assume 'obj' is not a valid object on this path.
                        //      }
                        //
                        //      obj.Dispose();

                        HandlePossibleInvalidatingOperation(operation);
                        return;
                    }
                }

                // Ref or out argument values from callee might be escaped by assigning to field.
                if (operation.Parameter?.RefKind is RefKind.Out or RefKind.Ref)
                {
                    HandlePossibleEscapingOperation(operation, GetEscapedLocations(operation));
                }

                return;

                // Local functions.
                bool IsDisposeOwnershipTransfer()
                {
                    if (operation.Parameter == null ||
                        operation.Parameter.RefKind == RefKind.Out)
                    {
                        // Out arguments are always owned by the caller.
                        return false;
                    }

                    return operation.Parent switch
                    {
                        IObjectCreationOperation => DisposeOwnershipTransferAtConstructor ||
                            DisposeOwnershipTransferLikelyTypes.Contains(operation.Parameter.Type),

                        IInvocationOperation invocation => DisposeOwnershipTransferAtMethodCall ||
                            IsDisposableCreationSpecialCase(invocation.TargetMethod) && DisposeOwnershipTransferLikelyTypes.Contains(operation.Parameter.Type),

                        _ => false,
                    };
                }
            }

            public override DisposeAbstractValue VisitFieldReference(IFieldReferenceOperation operation, object? argument)
            {
                var value = base.VisitFieldReference(operation, argument);
                if (_trackedInstanceFieldLocations != null &&
                    !operation.Field.IsStatic &&
                    operation.Instance?.Kind == OperationKind.InstanceReference)
                {
                    var pointsToAbstractValue = GetPointsToAbstractValue(operation);
                    if (pointsToAbstractValue.Kind == PointsToAbstractValueKind.KnownLocations &&
                        pointsToAbstractValue.Locations.Count == 1)
                    {
                        var location = pointsToAbstractValue.Locations.Single();
                        if (location.IsAnalysisEntityDefaultLocation)
                        {
                            if (!_trackedInstanceFieldLocations.TryGetValue(operation.Field, out _))
                            {
                                // First field reference on any control flow path.
                                // Create a default instance to represent the object referenced by the field at start of the method and
                                // check if the instance has NotDisposed state, indicating it is a disposable field that must be tracked.
                                if (HandleInstanceCreation(operation, pointsToAbstractValue, defaultValue: DisposeAbstractValue.NotDisposable) == DisposeAbstractValue.NotDisposed)
                                {
                                    _trackedInstanceFieldLocations.Add(operation.Field, pointsToAbstractValue);
                                }
                            }
                            else if (!CurrentAnalysisData.ContainsKey(location))
                            {
                                // This field has already started being tracked on a different control flow path.
                                // Process the default instance creation on this control flow path as well.
                                var disposedState = HandleInstanceCreation(operation, pointsToAbstractValue, DisposeAbstractValue.NotDisposable);
                                Debug.Assert(disposedState == DisposeAbstractValue.NotDisposed);
                            }
                        }
                    }
                }

                return value;
            }

            public override DisposeAbstractValue VisitBinaryOperatorCore(IBinaryOperation operation, object? argument)
            {
                var value = base.VisitBinaryOperatorCore(operation, argument);

                // Handle null-check for a disposable symbol on a control flow branch.
                //     var x = flag ? new Disposable() : null;
                //     if (x == null)
                //     {
                //         // Disposable allocation above cannot exist on this code path.
                //     }
                //

                // if (x == null)
                // {
                //      // This code path
                // }
                var isNullEqualsOnWhenTrue = FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue &&
                    (operation.OperatorKind == BinaryOperatorKind.Equals || operation.OperatorKind == BinaryOperatorKind.ObjectValueEquals);

                // if (x != null) { ... }
                // else
                // {
                //      // This code path
                // }
                var isNullNotEqualsOnWhenFalse = FlowBranchConditionKind == ControlFlowConditionKind.WhenFalse &&
                    (operation.OperatorKind == BinaryOperatorKind.NotEquals || operation.OperatorKind == BinaryOperatorKind.ObjectValueNotEquals);

                if (isNullEqualsOnWhenTrue || isNullNotEqualsOnWhenFalse)
                {
                    if (GetNullAbstractValue(operation.RightOperand) == NullAbstractValue.Null)
                    {
                        // if (x == null)
                        HandlePossibleInvalidatingOperation(operation.LeftOperand);
                    }
                    else if (GetNullAbstractValue(operation.LeftOperand) == NullAbstractValue.Null)
                    {
                        // if (null == x)
                        HandlePossibleInvalidatingOperation(operation.RightOperand);
                    }
                }

                return value;
            }

            public override DisposeAbstractValue VisitIsNull(IIsNullOperation operation, object? argument)
            {
                var value = base.VisitIsNull(operation, argument);

                // Handle null-check for a disposable symbol on a control flow branch.
                // See comments in VisitBinaryOperatorCore override above for further details.
                if (FlowBranchConditionKind == ControlFlowConditionKind.WhenTrue)
                {
                    HandlePossibleInvalidatingOperation(operation.Operand);
                }

                return value;
            }

            #endregion
        }
    }
}
