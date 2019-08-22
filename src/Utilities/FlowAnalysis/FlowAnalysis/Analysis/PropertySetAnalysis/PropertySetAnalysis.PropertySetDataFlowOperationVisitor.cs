// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using PropertySetAnalysisData = DictionaryAnalysisData<AbstractLocation, PropertySetAbstractValue>;

    internal partial class PropertySetAnalysis
    {
        /// <summary>
        /// Operation visitor to flow the location validation values across a given statement in a basic block.
        /// </summary>
        private sealed partial class PropertySetDataFlowOperationVisitor :
            AbstractLocationDataFlowOperationVisitor<PropertySetAnalysisData, PropertySetAnalysisContext, PropertySetAnalysisResult, PropertySetAbstractValue>
        {
            /// <summary>
            /// Keeps track of hazardous usages detected.
            /// </summary>
            private readonly ImmutableDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult>.Builder _hazardousUsageBuilder;

            private readonly ImmutableHashSet<IMethodSymbol>.Builder _visitedLocalFunctions;

            private readonly ImmutableHashSet<IFlowAnonymousFunctionOperation>.Builder _visitedLambdas;

            /// <summary>
            /// When looking for initialization hazardous usages, track the assignment operations for fields and properties for the tracked type.
            /// </summary>
            /// <remarks>
            /// Mapping of AnalysisEntity (for a field or property of the tracked type) to AbstractLocation(s) to IAssignmentOperation(s)
            /// </remarks>
            private PooledDictionary<AnalysisEntity, TrackedAssignmentData> TrackedFieldPropertyAssignmentsOpt;

            /// <summary>
            /// The type containing the property set we're tracking.
            /// </summary>
            private readonly INamedTypeSymbol TrackedTypeSymbol;

            public PropertySetDataFlowOperationVisitor(PropertySetAnalysisContext analysisContext)
                : base(analysisContext)
            {
                Debug.Assert(analysisContext.PointsToAnalysisResultOpt != null);

                this._hazardousUsageBuilder = ImmutableDictionary.CreateBuilder<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult>();

                this._visitedLocalFunctions = ImmutableHashSet.CreateBuilder<IMethodSymbol>();

                this._visitedLambdas = ImmutableHashSet.CreateBuilder<IFlowAnonymousFunctionOperation>();

                this.WellKnownTypeProvider.TryGetTypeByMetadataName(analysisContext.TypeToTrackMetadataName, out this.TrackedTypeSymbol);
                Debug.Assert(this.TrackedTypeSymbol != null);

                if (this.DataFlowAnalysisContext.HazardousUsageEvaluators.TryGetInitializationHazardousUsageEvaluator(out _))
                {
                    this.TrackedFieldPropertyAssignmentsOpt = PooledDictionary<AnalysisEntity, TrackedAssignmentData>.GetInstance();
                }
            }

            public ImmutableDictionary<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> HazardousUsages
            {
                get
                {
                    return this._hazardousUsageBuilder.ToImmutable();
                }
            }

            public ImmutableHashSet<IMethodSymbol> VisitedLocalFunctions
            {
                get
                {
                    return this._visitedLocalFunctions.ToImmutable();
                }
            }

            public ImmutableHashSet<IFlowAnonymousFunctionOperation> VisitedLambdas
            {
                get
                {
                    return this._visitedLambdas.ToImmutable();
                }
            }

            protected override PropertySetAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => ValueDomain.Bottom;

            protected override bool HasAnyAbstractValue(PropertySetAnalysisData data) => data.Count > 0;

            protected override PropertySetAbstractValue GetAbstractValue(AbstractLocation location)
                => this.CurrentAnalysisData.TryGetValue(location, out var value) ? value : ValueDomain.Bottom;

            protected override void ResetCurrentAnalysisData() => ResetAnalysisData(CurrentAnalysisData);

            protected override void StopTrackingAbstractValue(AbstractLocation location) => CurrentAnalysisData.Remove(location);

            protected override void SetAbstractValue(AbstractLocation location, PropertySetAbstractValue value)
            {
                if (value != PropertySetAbstractValue.Unknown
                    || this.CurrentAnalysisData.ContainsKey(location))
                {
                    this.CurrentAnalysisData[location] = value;
                }
            }

            protected override PropertySetAnalysisData MergeAnalysisData(PropertySetAnalysisData value1, PropertySetAnalysisData value2)
                => PropertySetAnalysisDomainInstance.Merge(value1, value2);
            protected override void UpdateValuesForAnalysisData(PropertySetAnalysisData targetAnalysisData)
                => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);
            protected override PropertySetAnalysisData GetClonedAnalysisData(PropertySetAnalysisData analysisData)
                => GetClonedAnalysisDataHelper(analysisData);
            public override PropertySetAnalysisData GetEmptyAnalysisData()
                => GetEmptyAnalysisDataHelper();
            protected override PropertySetAnalysisData GetExitBlockOutputData(PropertySetAnalysisResult analysisResult)
                => GetClonedAnalysisDataHelper(analysisResult.ExitBlockOutput.Data);
            protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(PropertySetAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
                => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData);
            protected override bool Equals(PropertySetAnalysisData value1, PropertySetAnalysisData value2)
                => EqualsHelper(value1, value2);

            protected override void SetValueForParameterPointsToLocationOnEntry(IParameterSymbol parameter, PointsToAbstractValue pointsToAbstractValue)
            {
            }

            protected override void EscapeValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, ImmutableHashSet<AbstractLocation> escapedLocations)
            {
            }

            protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, PropertySetAbstractValue value)
            {
            }

            protected override void SetAbstractValueForAssignment(IOperation target, IOperation assignedValueOperation, PropertySetAbstractValue assignedValue, bool mayBeAssignment = false)
            {
            }

            protected override void SetAbstractValueForTupleElementAssignment(AnalysisEntity tupleElementEntity, IOperation assignedValueOperation, PropertySetAbstractValue assignedValue)
            {
            }

            public override PropertySetAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
            {
                PropertySetAbstractValue abstractValue = base.VisitObjectCreation(operation, argument);
                if (!Equals(operation.Type, this.TrackedTypeSymbol))
                {
                    return abstractValue;
                }

                ConstructorMapper constructorMapper = this.DataFlowAnalysisContext.ConstructorMapper;
                if (!constructorMapper.PropertyAbstractValues.IsEmpty)
                {
                    abstractValue = PropertySetAbstractValue.GetInstance(constructorMapper.PropertyAbstractValues);
                }
                else if (constructorMapper.MapFromPointsToAbstractValue != null)
                {
                    ArrayBuilder<PointsToAbstractValue> builder = ArrayBuilder<PointsToAbstractValue>.GetInstance();
                    try
                    {
                        foreach (IArgumentOperation argumentOperation in operation.Arguments)
                        {
                            builder.Add(this.GetPointsToAbstractValue(argumentOperation));
                        }

                        abstractValue = constructorMapper.MapFromPointsToAbstractValue(operation.Constructor, builder);
                    }
                    finally
                    {
                        builder.Free();
                    }
                }
                else if (constructorMapper.MapFromValueContentAbstractValue != null)
                {
                    Debug.Assert(this.DataFlowAnalysisContext.ValueContentAnalysisResultOpt != null);
                    ArrayBuilder<PointsToAbstractValue> pointsToBuilder = ArrayBuilder<PointsToAbstractValue>.GetInstance();
                    ArrayBuilder<ValueContentAbstractValue> valueContentBuilder = ArrayBuilder<ValueContentAbstractValue>.GetInstance();
                    try
                    {
                        foreach (IArgumentOperation argumentOperation in operation.Arguments)
                        {
                            pointsToBuilder.Add(this.GetPointsToAbstractValue(argumentOperation));
                            valueContentBuilder.Add(this.GetValueContentAbstractValue(argumentOperation.Value));
                        }

                        abstractValue = constructorMapper.MapFromValueContentAbstractValue(operation.Constructor, valueContentBuilder, pointsToBuilder);
                    }
                    finally
                    {
                        pointsToBuilder.Free();
                        valueContentBuilder.Free();
                    }
                }
                else
                {
                    Debug.Fail("Unhandled ConstructorMapper");
                    return abstractValue;
                }

                PointsToAbstractValue pointsToAbstractValue = this.GetPointsToAbstractValue(operation);
                this.SetAbstractValue(pointsToAbstractValue, abstractValue);
                return abstractValue;
            }

            protected override PropertySetAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object argument)
            {
                PropertySetAbstractValue baseValue = base.VisitAssignmentOperation(operation, argument);

                // If we need to evaluate hazardous usages on initializations, track assignments of properties and fields, so
                // at the end of the CFG we can figure out which assignment operations to flag.
                if (this.TrackedFieldPropertyAssignmentsOpt != null
                    && this.TrackedTypeSymbol.Equals(operation.Target.Type)
                    && (operation.Target.Kind == OperationKind.PropertyReference
                        || operation.Target.Kind == OperationKind.FieldReference
                        || operation.Target.Kind == OperationKind.FlowCaptureReference))
                {
                    AnalysisEntity targetAnalysisEntity = null;
                    if (operation.Target.Kind == OperationKind.FlowCaptureReference)
                    {
                        PointsToAbstractValue lValuePointsToAbstractValue = this.GetPointsToAbstractValue(operation.Target);
                        if (lValuePointsToAbstractValue.LValueCapturedOperations.Count == 1)
                        {
                            IOperation lValueOperation = lValuePointsToAbstractValue.LValueCapturedOperations.First();
                            if (lValueOperation.Kind == OperationKind.FieldReference
                                || lValueOperation.Kind == OperationKind.PropertyReference)
                            {
                                this.AnalysisEntityFactory.TryCreate(lValueOperation, out targetAnalysisEntity);
                            }
                        }
                        else
                        {
                            Debug.Fail("Can LValues FlowCaptureReferences have more than one operation?");
                        }
                    }
                    else
                    {
                        this.AnalysisEntityFactory.TryCreate(operation.Target, out targetAnalysisEntity);
                    }

                    if (targetAnalysisEntity != null)
                    {
                        PointsToAbstractValue pointsToAbstractValue = this.GetPointsToAbstractValue(operation.Value);
                        if (!this.TrackedFieldPropertyAssignmentsOpt.TryGetValue(
                                targetAnalysisEntity,
                                out TrackedAssignmentData trackedAssignmentData))
                        {
                            trackedAssignmentData = new TrackedAssignmentData();
                            this.TrackedFieldPropertyAssignmentsOpt.Add(targetAnalysisEntity, trackedAssignmentData);
                        }

                        if (pointsToAbstractValue.Kind == PointsToAbstractValueKind.KnownLocations)
                        {
                            foreach (AbstractLocation abstractLocation in pointsToAbstractValue.Locations)
                            {
                                trackedAssignmentData.TrackAssignmentWithAbstractLocation(operation, abstractLocation);
                            }
                        }
                        else if (pointsToAbstractValue.Kind == PointsToAbstractValueKind.Unknown
                            || pointsToAbstractValue.Kind == PointsToAbstractValueKind.UnknownNotNull)
                        {
                            trackedAssignmentData.TrackAssignmentWithUnknownLocation(operation);
                        }
                        else if (pointsToAbstractValue.NullState == NullAbstractValue.Null)
                        {
                            // Do nothing.
                        }
                        else
                        {
                            Debug.Fail($"Unhandled PointsToAbstractValue: Kind = {pointsToAbstractValue.Kind}, NullState = {pointsToAbstractValue.NullState}");
                        }
                    }
                }

                if (operation.Target is IPropertyReferenceOperation propertyReferenceOperation
                    && Equals(propertyReferenceOperation.Instance?.Type, this.TrackedTypeSymbol)
                    && this.DataFlowAnalysisContext.PropertyMappers.TryGetPropertyMapper(
                        propertyReferenceOperation.Property.Name,
                        out PropertyMapper propertyMapper,
                        out int index))
                {
                    PropertySetAbstractValueKind propertySetAbstractValueKind;

                    if (propertyMapper.MapFromPointsToAbstractValue != null)
                    {
                        propertySetAbstractValueKind = propertyMapper.MapFromPointsToAbstractValue(
                            this.GetPointsToAbstractValue(operation.Value));
                    }
                    else if (propertyMapper.MapFromValueContentAbstractValue != null)
                    {
                        Debug.Assert(this.DataFlowAnalysisContext.ValueContentAnalysisResultOpt != null);
                        propertySetAbstractValueKind = propertyMapper.MapFromValueContentAbstractValue(
                            this.GetValueContentAbstractValue(operation.Value));
                    }
                    else
                    {
                        Debug.Fail("Unhandled PropertyMapper");
                        return baseValue;
                    }

                    baseValue = null;
                    PointsToAbstractValue pointsToAbstractValue = this.GetPointsToAbstractValue(propertyReferenceOperation.Instance);
                    foreach (AbstractLocation location in pointsToAbstractValue.Locations)
                    {
                        PropertySetAbstractValue propertySetAbstractValue = this.GetAbstractValue(location);
                        propertySetAbstractValue = propertySetAbstractValue.ReplaceAt(index, propertySetAbstractValueKind);

                        if (baseValue == null)
                        {
                            baseValue = propertySetAbstractValue;
                        }
                        else
                        {
                            baseValue = this.DataFlowAnalysisContext.ValueDomain.Merge(baseValue, propertySetAbstractValue);
                        }

                        this.SetAbstractValue(location, propertySetAbstractValue);
                    }

                    return baseValue ?? PropertySetAbstractValue.Unknown.ReplaceAt(index, propertySetAbstractValueKind);
                }

                return baseValue;
            }

            /// <summary>
            /// Processes PropertySetAbstractValues at the end of the ControlFlowGraph.
            /// </summary>
            /// <param name="exitBlockOutput">Exit block output.</param>
            /// <remarks>When evaluating hazardous usages on initializations.
            /// class Class
            /// {
            ///     public static readonly Settings InsecureSettings = new Settings { AllowAnyoneAdminAccess = true };
            /// }
            /// </remarks>
            internal void ProcessExitBlock(PropertySetBlockAnalysisResult exitBlockOutput)
            {
                if (this.TrackedFieldPropertyAssignmentsOpt == null)
                {
                    return;
                }

                try
                {
                    this.DataFlowAnalysisContext.HazardousUsageEvaluators.TryGetInitializationHazardousUsageEvaluator(
                        out HazardousUsageEvaluator initializationHazardousUsageEvaluator);
                    Debug.Assert(initializationHazardousUsageEvaluator != null);

                    foreach (KeyValuePair<AnalysisEntity, TrackedAssignmentData> kvp
                        in this.TrackedFieldPropertyAssignmentsOpt)
                    {
                        if (!this.DataFlowAnalysisContext.PointsToAnalysisResultOpt.ExitBlockOutput.Data.TryGetValue(
                                kvp.Key, out PointsToAbstractValue pointsToAbstractValue))
                        {
                            continue;
                        }

                        if (pointsToAbstractValue.Kind == PointsToAbstractValueKind.KnownLocations)
                        {
                            if (kvp.Value.AbstractLocationsToAssignments != null)
                            {
                                foreach (AbstractLocation abstractLocation in pointsToAbstractValue.Locations)
                                {
                                    if (abstractLocation.IsNull || abstractLocation.IsAnalysisEntityDefaultLocation)
                                    {
                                        continue;
                                    }

                                    if (!kvp.Value.AbstractLocationsToAssignments.TryGetValue(
                                            abstractLocation,
                                            out PooledHashSet<IAssignmentOperation> assignments))
                                    {
                                        Debug.Fail("Expected to have tracked assignment operations for a given location");
                                        continue;
                                    }

                                    if (!exitBlockOutput.Data.TryGetValue(
                                            abstractLocation,
                                            out PropertySetAbstractValue propertySetAbstractValue))
                                    {
                                        propertySetAbstractValue = PropertySetAbstractValue.Unknown;
                                    }

                                    HazardousUsageEvaluationResult result =
                                        initializationHazardousUsageEvaluator.ValueEvaluator(propertySetAbstractValue);
                                    if (result != HazardousUsageEvaluationResult.Unflagged)
                                    {
                                        foreach (IAssignmentOperation assignmentOperation in assignments)
                                        {
                                            this.MergeHazardousUsageResult(
                                                assignmentOperation.Syntax,
                                                methodSymbol: null,    // No method invocation; just evaluating initialization value.
                                                result);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Debug.Fail("Expected to have tracked assignment operations with locations");
                            }
                        }
                        else if (pointsToAbstractValue.Kind == PointsToAbstractValueKind.Unknown
                            || pointsToAbstractValue.Kind == PointsToAbstractValueKind.UnknownNotNull)
                        {
                            if (kvp.Value.AssignmentsWithUnknownLocation != null)
                            {
                                HazardousUsageEvaluationResult result =
                                    initializationHazardousUsageEvaluator.ValueEvaluator(PropertySetAbstractValue.Unknown);
                                if (result != HazardousUsageEvaluationResult.Unflagged)
                                {
                                    foreach (IAssignmentOperation assignmentOperation in kvp.Value.AssignmentsWithUnknownLocation)
                                    {
                                        this.MergeHazardousUsageResult(
                                            assignmentOperation.Syntax,
                                            methodSymbol: null,    // No method invocation; just evaluating initialization value.
                                            result);
                                    }
                                }
                            }
                            else
                            {
                                Debug.Fail("Expected to have tracked assignment operations with unknown PointsTo");
                            }
                        }
                        else if (pointsToAbstractValue.NullState == NullAbstractValue.Null)
                        {
                            // Do nothing.
                        }
                        else
                        {
                            Debug.Fail($"Unhandled PointsToAbstractValue: Kind = {pointsToAbstractValue.Kind}, NullState = {pointsToAbstractValue.NullState}");
                        }
                    }
                }
                finally
                {
                    foreach (TrackedAssignmentData trackedAssignmentData in this.TrackedFieldPropertyAssignmentsOpt.Values)
                    {
                        trackedAssignmentData.Free();
                    }

                    this.TrackedFieldPropertyAssignmentsOpt.Free();
                    this.TrackedFieldPropertyAssignmentsOpt = null;
                }
            }

            public override PropertySetAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(IMethodSymbol method, IOperation visitedInstance, ImmutableArray<IArgumentOperation> visitedArguments, bool invokedAsDelegate, IOperation originalOperation, PropertySetAbstractValue defaultValue)
            {
                PropertySetAbstractValue baseValue = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);

                if (this.DataFlowAnalysisContext.HazardousUsageEvaluators.TryGetArgumentHazardousUsageEvaluator(
                            out HazardousUsageEvaluator argumentHazardousUsageEvaluator))
                {
                    foreach (IArgumentOperation visitedArgument in visitedArguments)
                    {
                        if (this.TrackedTypeSymbol.Equals(visitedArgument.Value.Type))
                        {
                            this.EvaluatePotentialHazardousUsage(
                                visitedArgument.Value.Syntax,
                                null,
                                visitedArgument.Value,
                                (PropertySetAbstractValue abstractValue) => argumentHazardousUsageEvaluator.ValueEvaluator(abstractValue));
                        }
                    }
                }

                // If we have a HazardousUsageEvaluator for a method within the tracked type,
                // or for a method within a different type.
                IOperation propertySetInstance = visitedInstance;
                if ((Equals(visitedInstance?.Type, this.TrackedTypeSymbol)
                    && this.DataFlowAnalysisContext.HazardousUsageEvaluators.TryGetHazardousUsageEvaluator(method.MetadataName, out var hazardousUsageEvaluator))
                    || TryFindNonTrackedTypeHazardousUsageEvaluator(out hazardousUsageEvaluator, out propertySetInstance))
                {
                    this.EvaluatePotentialHazardousUsage(
                        originalOperation.Syntax,
                        method,
                        propertySetInstance,
                        (PropertySetAbstractValue abstractValue) => hazardousUsageEvaluator.InvocationEvaluator(method, abstractValue));
                }
                else
                {
                    this.MergeInterproceduralResults(originalOperation);
                }

                return baseValue;

                // Local functions.
                bool TryFindNonTrackedTypeHazardousUsageEvaluator(out HazardousUsageEvaluator evaluator, out IOperation instance)
                {
                    evaluator = null;
                    instance = null;
                    if (!this.DataFlowAnalysisContext.HazardousUsageTypesToNames.TryGetValue(
                            visitedInstance?.Type as INamedTypeSymbol ?? method.ContainingType,
                            out string containingTypeName))
                    {
                        return false;
                    }

                    // This doesn't handle the case of multiple instances of the type being tracked.
                    // If that's needed one day, will need to extend this.
                    foreach (IArgumentOperation argumentOperation in visitedArguments)
                    {
                        if (Equals(argumentOperation.Value?.Type, this.TrackedTypeSymbol)
                            && this.DataFlowAnalysisContext.HazardousUsageEvaluators.TryGetHazardousUsageEvaluator(
                                    containingTypeName,
                                    method.MetadataName,
                                    argumentOperation.Parameter.MetadataName,
                                    out evaluator))
                        {
                            instance = argumentOperation.Value;
                            return true;
                        }
                    }

                    return false;
                }
            }

            /// <summary>
            /// Evaluates an operation for potentially being a hazardous usage.
            /// </summary>
            /// <param name="operationSyntax">SyntaxNode of operation that's being evaluated.</param>
            /// <param name="methodSymbol">Method symbol of the invocation operation that's being evaluated, or null if not an invocation operation.</param>
            /// <param name="propertySetInstance">IOperation of the tracked type containing the properties to be evaluated.</param>
            /// <param name="evaluationFunction">Function to evaluate a PropertySetAbstractValue to a HazardousUsageEvaluationResult.</param>
            /// <param name="locationToAbstractValueMapping">Optional function to map AbstractLocations to PropertySetAbstractValues.  If null, uses this.CurrentAnalysisData.</param>
            private void EvaluatePotentialHazardousUsage(SyntaxNode operationSyntax, IMethodSymbol methodSymbol, IOperation propertySetInstance, Func<PropertySetAbstractValue, HazardousUsageEvaluationResult> evaluationFunction, Func<AbstractLocation, PropertySetAbstractValue> locationToAbstractValueMapping = null)
            {
                if (locationToAbstractValueMapping == null)
                {
                    locationToAbstractValueMapping = this.GetAbstractValue;
                }

                PointsToAbstractValue pointsToAbstractValue = this.GetPointsToAbstractValue(propertySetInstance);
                HazardousUsageEvaluationResult result = HazardousUsageEvaluationResult.Unflagged;
                foreach (AbstractLocation location in pointsToAbstractValue.Locations)
                {
                    PropertySetAbstractValue locationAbstractValue = locationToAbstractValueMapping(location);

                    HazardousUsageEvaluationResult evaluationResult = evaluationFunction(locationAbstractValue);
                    result = MergeHazardousUsageEvaluationResult(result, evaluationResult);
                }

                this.MergeHazardousUsageResult(operationSyntax, methodSymbol, result);
            }

            private void MergeHazardousUsageResult(SyntaxNode operationSyntax, IMethodSymbol methodSymbol, HazardousUsageEvaluationResult result)
            {
                if (result != HazardousUsageEvaluationResult.Unflagged)
                {
                    (Location, IMethodSymbol) key = (operationSyntax.GetLocation(), methodSymbol);
                    if (this._hazardousUsageBuilder.TryGetValue(key, out HazardousUsageEvaluationResult existingResult))
                    {
                        this._hazardousUsageBuilder[key] = MergeHazardousUsageEvaluationResult(result, existingResult);
                    }
                    else
                    {
                        this._hazardousUsageBuilder.Add(key, result);
                    }
                }
            }

            public override PropertySetAbstractValue VisitInvocation_LocalFunction(IMethodSymbol localFunction, ImmutableArray<IArgumentOperation> visitedArguments, IOperation originalOperation, PropertySetAbstractValue defaultValue)
            {
                PropertySetAbstractValue baseValue = base.VisitInvocation_LocalFunction(localFunction, visitedArguments, originalOperation, defaultValue);
                this._visitedLocalFunctions.Add(localFunction);
                this.MergeInterproceduralResults(originalOperation);
                return baseValue;
            }

            public override PropertySetAbstractValue VisitInvocation_Lambda(IFlowAnonymousFunctionOperation lambda, ImmutableArray<IArgumentOperation> visitedArguments, IOperation originalOperation, PropertySetAbstractValue defaultValue)
            {
                PropertySetAbstractValue baseValue = base.VisitInvocation_Lambda(lambda, visitedArguments, originalOperation, defaultValue);
                this._visitedLambdas.Add(lambda);
                this.MergeInterproceduralResults(originalOperation);
                return baseValue;
            }

            protected override void ProcessReturnValue(IOperation returnValue)
            {
                base.ProcessReturnValue(returnValue);

                if (returnValue != null
                    && this.TrackedTypeSymbol.Equals(returnValue.Type)
                    && this.DataFlowAnalysisContext.HazardousUsageEvaluators.TryGetReturnHazardousUsageEvaluator(
                        out HazardousUsageEvaluator hazardousUsageEvaluator))
                {
                    this.EvaluatePotentialHazardousUsage(
                        returnValue.Syntax,
                        null,
                        returnValue,
                        (PropertySetAbstractValue abstractValue) => hazardousUsageEvaluator.ValueEvaluator(abstractValue));
                }
            }

            private void MergeInterproceduralResults(IOperation originalOperation)
            {
                if (!this.TryGetInterproceduralAnalysisResult(originalOperation, out PropertySetAnalysisResult subResult))
                {
                    return;
                }

                foreach (KeyValuePair<(Location Location, IMethodSymbol Method), HazardousUsageEvaluationResult> kvp in subResult.HazardousUsages)
                {
                    if (this._hazardousUsageBuilder.TryGetValue(kvp.Key, out HazardousUsageEvaluationResult existingValue))
                    {
                        this._hazardousUsageBuilder[kvp.Key] = MergeHazardousUsageEvaluationResult(kvp.Value, existingValue);
                    }
                    else
                    {
                        this._hazardousUsageBuilder.Add(kvp.Key, kvp.Value);
                    }
                }

                foreach (IMethodSymbol localFunctionSymbol in subResult.VisitedLocalFunctions)
                {
                    this._visitedLocalFunctions.Add(localFunctionSymbol);
                }

                foreach (IFlowAnonymousFunctionOperation lambdaOperation in subResult.VisitedLambdas)
                {
                    this._visitedLambdas.Add(lambdaOperation);
                }
            }
        }
    }
}
