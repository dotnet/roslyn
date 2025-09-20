// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataOperationVisitor : AnalysisEntityDataFlowOperationVisitor<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataAbstractValue>
        {
            private readonly TaintedDataAnalysisDomain _taintedDataAnalysisDomain;

            /// <summary>
            /// Mapping of a tainted data sinks to their originating sources.
            /// </summary>
            /// <remarks>Keys are <see cref="SymbolAccess"/> sinks where the tainted data entered, values are <see cref="SymbolAccess"/>s where the tainted data originated from.</remarks>
            private Dictionary<SymbolAccess, (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SymbolAccess>.Builder SourceOrigins)> TaintedSourcesBySink { get; }

            public TaintedDataOperationVisitor(TaintedDataAnalysisDomain taintedDataAnalysisDomain, TaintedDataAnalysisContext analysisContext)
                : base(analysisContext)
            {
                _taintedDataAnalysisDomain = taintedDataAnalysisDomain;
                this.TaintedSourcesBySink = [];
            }

            public ImmutableArray<TaintedDataSourceSink> GetTaintedDataSourceSinkEntries()
            {
                ImmutableArray<TaintedDataSourceSink>.Builder builder = ImmutableArray.CreateBuilder<TaintedDataSourceSink>();
                foreach (KeyValuePair<SymbolAccess, (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SymbolAccess>.Builder SourceOrigins)> kvp in this.TaintedSourcesBySink)
                {
                    builder.Add(
                        new TaintedDataSourceSink(
                            kvp.Key,
                            kvp.Value.SinkKinds.ToImmutable(),
                            kvp.Value.SourceOrigins.ToImmutable()));
                }

                return builder.ToImmutableArray();
            }

            protected override void AddTrackedEntities(TaintedDataAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
                => analysisData.AddTrackedEntities(builder);

            protected override bool Equals(TaintedDataAnalysisData value1, TaintedDataAnalysisData value2)
            {
                return value1.Equals(value2);
            }

            protected override TaintedDataAbstractValue GetAbstractDefaultValue(ITypeSymbol? type)
            {
                return TaintedDataAbstractValue.NotTainted;
            }

            protected override TaintedDataAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
            {
                return this.CurrentAnalysisData.TryGetValue(analysisEntity, out TaintedDataAbstractValue? value) ? value : TaintedDataAbstractValue.NotTainted;
            }

            protected override TaintedDataAnalysisData GetClonedAnalysisData(TaintedDataAnalysisData analysisData)
            {
                return (TaintedDataAnalysisData)analysisData.Clone();
            }

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
            {
                return this.CurrentAnalysisData.HasAbstractValue(analysisEntity);
            }

            protected override bool HasAnyAbstractValue(TaintedDataAnalysisData data)
            {
                return data.HasAnyAbstractValue;
            }

            protected override TaintedDataAnalysisData MergeAnalysisData(TaintedDataAnalysisData value1, TaintedDataAnalysisData value2)
            {
                return _taintedDataAnalysisDomain.Merge(value1, value2);
            }

            protected override void UpdateValuesForAnalysisData(TaintedDataAnalysisData targetAnalysisData)
            {
                UpdateValuesForAnalysisData(targetAnalysisData.CoreAnalysisData, CurrentAnalysisData.CoreAnalysisData);
            }

            protected override void ResetCurrentAnalysisData()
            {
                this.CurrentAnalysisData.Reset(this.ValueDomain.UnknownOrMayBeValue);
            }

            public override TaintedDataAnalysisData GetEmptyAnalysisData()
            {
                return new TaintedDataAnalysisData();
            }

            protected override TaintedDataAnalysisData GetExitBlockOutputData(TaintedDataAnalysisResult analysisResult)
            {
                return new TaintedDataAnalysisData(analysisResult.ExitBlockOutput.Data);
            }

            protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(TaintedDataAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
            {
                base.ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException.CoreAnalysisData, CurrentAnalysisData.CoreAnalysisData, throwBranchWithExceptionType);
            }

            protected override TaintedDataAbstractValue GetDefaultValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            {
                if (this.DataFlowAnalysisContext.SourceInfos.IsSourceParameter(parameter, WellKnownTypeProvider))
                {
                    // Location of the parameter, so we can track where the tainted data appears in code.
                    // The parameter itself may not have any DeclaringSyntaxReferences, e.g. 'value' inside property setters.
                    SyntaxNode parameterSyntaxNode;
                    if (!parameter.DeclaringSyntaxReferences.IsEmpty)
                    {
                        parameterSyntaxNode = parameter.DeclaringSyntaxReferences[0].GetSyntax();
                    }
                    else if (!parameter.ContainingSymbol.DeclaringSyntaxReferences.IsEmpty)
                    {
                        parameterSyntaxNode = parameter.ContainingSymbol.DeclaringSyntaxReferences[0].GetSyntax();
                    }
                    else
                    {
                        // Unless there are others, the only case we have for parameters being tainted data sources is inside
                        // ASP.NET Core MVC controller action methods (see WebInputSources.cs), so those parameters should
                        // always be declared somewhere.
                        Debug.Fail("Can we have a tainted data parameter with no syntax references?");
                        return ValueDomain.UnknownOrMayBeValue;
                    }

                    return TaintedDataAbstractValue.CreateTainted(parameter, parameterSyntaxNode, this.OwningSymbol);
                }

                return ValueDomain.UnknownOrMayBeValue;
            }

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, TaintedDataAbstractValue value)
            {
                if (value.Kind == TaintedDataAbstractValueKind.Tainted
                    || this.CurrentAnalysisData.CoreAnalysisData.ContainsKey(analysisEntity))
                {
                    // Only track tainted data, or sanitized data.
                    // If it's new, and it's untainted, we don't care.
                    SetAbstractValueCore(CurrentAnalysisData, analysisEntity, value);
                }
            }

            private static void SetAbstractValueCore(TaintedDataAnalysisData taintedAnalysisData, AnalysisEntity analysisEntity, TaintedDataAbstractValue value)
                => taintedAnalysisData.SetAbstractValue(analysisEntity, value);

            protected override void ResetAbstractValue(AnalysisEntity analysisEntity)
            {
                this.SetAbstractValue(analysisEntity, ValueDomain.UnknownOrMayBeValue);
            }

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity, TaintedDataAnalysisData analysisData)
            {
                analysisData.RemoveEntries(analysisEntity);
            }

            public override TaintedDataAbstractValue DefaultVisit(IOperation operation, object? argument)
            {
                // This handles most cases of tainted data flowing from child operations to parent operations.
                // Examples:
                // - tainted input parameters to method calls returns, and out/ref parameters, tainted (assuming no interprocedural)
                // - adding a tainted value to something makes the result tainted
                // - instantiating an object with tainted data makes the new object tainted

                List<TaintedDataAbstractValue>? taintedValues = null;
                foreach (IOperation childOperation in operation.ChildOperations)
                {
                    TaintedDataAbstractValue childValue = Visit(childOperation, argument);
                    if (childValue.Kind == TaintedDataAbstractValueKind.Tainted)
                    {
                        taintedValues ??= [];

                        taintedValues.Add(childValue);
                    }
                }

                if (taintedValues != null)
                {
                    if (taintedValues.Count == 1)
                    {
                        return taintedValues[0];
                    }
                    else
                    {
                        return TaintedDataAbstractValue.MergeTainted(taintedValues);
                    }
                }
                else
                {
                    return ValueDomain.UnknownOrMayBeValue;
                }
            }

            public override TaintedDataAbstractValue VisitConversion(IConversionOperation operation, object? argument)
            {
                TaintedDataAbstractValue operandValue = Visit(operation.Operand, argument);

                if (!operation.Conversion.Exists)
                {
                    return ValueDomain.UnknownOrMayBeValue;
                }

                if (operation.Conversion.IsImplicit)
                {
                    return operandValue;
                }

                // Conservative for error code and user defined operator.
                return !operation.Conversion.IsUserDefined ? operandValue : ValueDomain.UnknownOrMayBeValue;
            }

            protected override TaintedDataAbstractValue ComputeAnalysisValueForReferenceOperation(IOperation operation, TaintedDataAbstractValue defaultValue)
            {
                // If the property reference itself is a tainted data source
                if (operation is IPropertyReferenceOperation propertyReferenceOperation
                    && this.DataFlowAnalysisContext.SourceInfos.IsSourceProperty(propertyReferenceOperation.Property))
                {
                    return TaintedDataAbstractValue.CreateTainted(propertyReferenceOperation.Member, propertyReferenceOperation.Syntax, this.OwningSymbol);
                }

                if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity? analysisEntity))
                {
                    return this.CurrentAnalysisData.TryGetValue(analysisEntity, out TaintedDataAbstractValue? value) ? value : defaultValue;
                }

                return defaultValue;
            }

            // So we can hook into constructor calls.
            public override TaintedDataAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object? argument)
            {
                TaintedDataAbstractValue baseValue = base.VisitObjectCreation(operation, argument);
                IEnumerable<IArgumentOperation> taintedArguments = GetTaintedArguments(operation.Arguments);
                if (taintedArguments.Any() && operation.Constructor != null)
                {
                    ProcessTaintedDataEnteringInvocationOrCreation(operation.Constructor, taintedArguments, operation);
                }

                return baseValue;
            }

            public override TaintedDataAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                IMethodSymbol method,
                IOperation? visitedInstance,
                ImmutableArray<IArgumentOperation> visitedArguments,
                bool invokedAsDelegate,
                IOperation originalOperation,
                TaintedDataAbstractValue defaultValue)
            {
                // Always invoke base visit.
                TaintedDataAbstractValue result = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                    method,
                    visitedInstance,
                    visitedArguments,
                    invokedAsDelegate,
                    originalOperation,
                    defaultValue);

                IEnumerable<IArgumentOperation> taintedArguments = GetTaintedArguments(visitedArguments);
                if (taintedArguments.Any())
                {
                    ProcessTaintedDataEnteringInvocationOrCreation(method, taintedArguments, originalOperation);
                }

                PooledHashSet<string>? taintedTargets = null;
                PooledHashSet<(string, string)>? taintedParameterPairs = null;
                PooledHashSet<(string, string)>? sanitizedParameterPairs = null;
                PooledHashSet<string>? taintedParameterNamesCached = null;
                try
                {
                    IEnumerable<string> GetTaintedParameterNames()
                    {
                        IEnumerable<string> taintedParameterNames = visitedArguments
                                .Where(s => s.Parameter != null && this.GetCachedAbstractValue(s).Kind == TaintedDataAbstractValueKind.Tainted)
                                .Select(s => s.Parameter!.Name);

                        if (visitedInstance != null && this.GetCachedAbstractValue(visitedInstance).Kind == TaintedDataAbstractValueKind.Tainted)
                        {
                            taintedParameterNames = taintedParameterNames.Concat(TaintedTargetValue.This);
                        }

                        return taintedParameterNames;
                    }

                    taintedParameterNamesCached = PooledHashSet<string>.GetInstance();
                    taintedParameterNamesCached.UnionWith(GetTaintedParameterNames());

                    if (this.DataFlowAnalysisContext.SourceInfos.IsSourceMethod(
                        method,
                        visitedArguments,
                        new Lazy<PointsToAnalysisResult?>(() => DataFlowAnalysisContext.PointsToAnalysisResult),
                        new Lazy<(PointsToAnalysisResult?, ValueContentAnalysisResult?)>(() => (DataFlowAnalysisContext.PointsToAnalysisResult, DataFlowAnalysisContext.ValueContentAnalysisResult)),
                        out taintedTargets))
                    {
                        bool rebuildTaintedParameterNames = false;

                        foreach (string taintedTarget in taintedTargets)
                        {
                            if (taintedTarget != TaintedTargetValue.Return)
                            {
                                IArgumentOperation? argumentOperation = visitedArguments.FirstOrDefault(o => o.Parameter?.Name == taintedTarget);
                                if (argumentOperation?.Parameter != null)
                                {
                                    rebuildTaintedParameterNames = true;
                                    this.CacheAbstractValue(argumentOperation, TaintedDataAbstractValue.CreateTainted(argumentOperation.Parameter, argumentOperation.Syntax, method));
                                }
                                else
                                {
                                    Debug.Fail("Are the tainted data sources misconfigured?");
                                }
                            }
                            else
                            {
                                result = TaintedDataAbstractValue.CreateTainted(method, originalOperation.Syntax, this.OwningSymbol);
                            }
                        }

                        if (rebuildTaintedParameterNames)
                        {
                            taintedParameterNamesCached.Clear();
                            taintedParameterNamesCached.UnionWith(GetTaintedParameterNames());
                        }
                    }

                    if (this.DataFlowAnalysisContext.SourceInfos.IsSourceTransferMethod(
                        method,
                        visitedArguments,
                        taintedParameterNamesCached,
                        out taintedParameterPairs))
                    {
                        foreach ((string ifTaintedParameter, string thenTaintedTarget) in taintedParameterPairs)
                        {
                            IOperation? thenTaintedTargetOperation = visitedInstance != null && thenTaintedTarget == TaintedTargetValue.This
                                ? visitedInstance
                                : visitedArguments.FirstOrDefault(o => o.Parameter?.Name == thenTaintedTarget);
                            if (thenTaintedTargetOperation != null)
                            {
                                var operation = visitedInstance != null && ifTaintedParameter == TaintedTargetValue.This
                                    ? visitedInstance
                                    : visitedArguments.FirstOrDefault(o => o.Parameter?.Name == ifTaintedParameter);
                                if (operation != null)
                                {
                                    SetTaintedForEntity(thenTaintedTargetOperation, this.GetCachedAbstractValue(operation));
                                }
                            }
                            else
                            {
                                Debug.Fail("Are the tainted data sources misconfigured?");
                            }
                        }
                    }

                    if (visitedInstance != null && this.IsSanitizingInstanceMethod(method))
                    {
                        SetTaintedForEntity(visitedInstance, TaintedDataAbstractValue.NotTainted);
                    }

                    if (this.IsSanitizingMethod(
                        method,
                        visitedArguments,
                        taintedParameterNamesCached,
                        out sanitizedParameterPairs))
                    {
                        if (sanitizedParameterPairs.Count == 0)
                        {
                            // it was either sanitizing constructor or
                            // the short form or registering sanitizer method by just the name
                            result = TaintedDataAbstractValue.NotTainted;
                        }
                        else
                        {
                            foreach ((string ifTaintedParameter, string thenSanitizedTarget) in sanitizedParameterPairs)
                            {
                                if (thenSanitizedTarget == TaintedTargetValue.Return)
                                {
                                    result = TaintedDataAbstractValue.NotTainted;
                                    continue;
                                }

                                IArgumentOperation? thenSanitizedTargetOperation = visitedArguments.FirstOrDefault(o => o.Parameter?.Name == thenSanitizedTarget);
                                if (thenSanitizedTargetOperation != null)
                                {
                                    SetTaintedForEntity(thenSanitizedTargetOperation, TaintedDataAbstractValue.NotTainted);
                                }
                                else
                                {
                                    Debug.Fail("Are the tainted data sanitizers misconfigured?");
                                }
                            }
                        }
                    }
                }
                finally
                {
                    taintedTargets?.Free();
                    taintedParameterPairs?.Free();
                    sanitizedParameterPairs?.Free();
                    taintedParameterNamesCached?.Free();
                }

                return result;
            }

            public override TaintedDataAbstractValue VisitInvocation_LocalFunction(IMethodSymbol localFunction, ImmutableArray<IArgumentOperation> visitedArguments, IOperation originalOperation, TaintedDataAbstractValue defaultValue)
            {
                // Always invoke base visit.
                TaintedDataAbstractValue baseValue = base.VisitInvocation_LocalFunction(localFunction, visitedArguments, originalOperation, defaultValue);

                IEnumerable<IArgumentOperation> taintedArguments = GetTaintedArguments(visitedArguments);
                if (taintedArguments.Any())
                {
                    ProcessTaintedDataEnteringInvocationOrCreation(localFunction, taintedArguments, originalOperation);
                }

                return baseValue;
            }

            public override TaintedDataAbstractValue VisitInvocation_Lambda(IFlowAnonymousFunctionOperation lambda, ImmutableArray<IArgumentOperation> visitedArguments, IOperation originalOperation, TaintedDataAbstractValue defaultValue)
            {
                // Always invoke base visit.
                TaintedDataAbstractValue baseValue = base.VisitInvocation_Lambda(lambda, visitedArguments, originalOperation, defaultValue);

                IEnumerable<IArgumentOperation> taintedArguments = GetTaintedArguments(visitedArguments);
                if (taintedArguments.Any())
                {
                    ProcessTaintedDataEnteringInvocationOrCreation(lambda.Symbol, taintedArguments, originalOperation);
                }

                return baseValue;
            }

            /// <summary>
            /// Computes abstract value for out or ref arguments when not performing interprocedural analysis.
            /// </summary>
            /// <param name="analysisEntity">Analysis entity.</param>
            /// <param name="operation">IArgumentOperation.</param>
            /// <param name="defaultValue">Default TaintedDataAbstractValue if we don't need to override.</param>
            /// <returns>Abstract value of the output parameter.</returns>
            protected override TaintedDataAbstractValue ComputeAnalysisValueForEscapedRefOrOutArgument(
                AnalysisEntity analysisEntity,
                IArgumentOperation operation,
                TaintedDataAbstractValue defaultValue)
            {
                // Note this method is only called when interprocedural DFA is *NOT* performed.
                if (operation.Parent is IInvocationOperation invocationOperation)
                {
                    Debug.Assert(!this.TryGetInterproceduralAnalysisResult(invocationOperation, out _));

                    // Treat ref or out arguments as the same as the invocation operation.
                    TaintedDataAbstractValue returnValueAbstractValue = this.GetCachedAbstractValue(invocationOperation);
                    return returnValueAbstractValue;
                }
                else
                {
                    return defaultValue;
                }
            }

            // So we can treat the array as tainted when it's passed to other object constructors.
            // See HttpRequest_Form_Array_List_Diagnostic and HttpRequest_Form_List_Diagnostic tests.
            public override TaintedDataAbstractValue VisitArrayInitializer(IArrayInitializerOperation operation, object? argument)
            {
                HashSet<SymbolAccess>? sourceOrigins = null;
                TaintedDataAbstractValue baseAbstractValue = base.VisitArrayInitializer(operation, argument);
                if (baseAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted)
                {
                    sourceOrigins = [.. baseAbstractValue.SourceOrigins];
                }

                IEnumerable<TaintedDataAbstractValue> taintedAbstractValues =
                    operation.ElementValues
                        .Select<IOperation, TaintedDataAbstractValue>(this.GetCachedAbstractValue)
                        .Where(v => v.Kind == TaintedDataAbstractValueKind.Tainted);
                if (baseAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted)
                {
                    taintedAbstractValues = taintedAbstractValues.Concat(baseAbstractValue);
                }

                TaintedDataAbstractValue? result = null;
                if (taintedAbstractValues.Any())
                {
                    result = TaintedDataAbstractValue.MergeTainted(taintedAbstractValues);
                }

                IArrayCreationOperation? arrayCreationOperation = operation.GetAncestor<IArrayCreationOperation>(OperationKind.ArrayCreation);
                if (arrayCreationOperation?.Type is IArrayTypeSymbol arrayTypeSymbol
                    && this.DataFlowAnalysisContext.SourceInfos.IsSourceConstantArrayOfType(arrayTypeSymbol, operation)
                    && operation.ElementValues.All(s => GetValueContentAbstractValue(s).IsLiteralState))
                {
                    TaintedDataAbstractValue taintedDataAbstractValue = TaintedDataAbstractValue.CreateTainted(arrayTypeSymbol, arrayCreationOperation.Syntax, this.OwningSymbol);
                    result = result == null ? taintedDataAbstractValue : TaintedDataAbstractValue.MergeTainted(result, taintedDataAbstractValue);
                }

                if (result != null)
                {
                    return result;
                }
                else
                {
                    return baseAbstractValue;
                }
            }

            protected override TaintedDataAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object? argument)
            {
                TaintedDataAbstractValue taintedDataAbstractValue = base.VisitAssignmentOperation(operation, argument);
                ProcessAssignmentOperation(operation);
                return taintedDataAbstractValue;
            }

            private void TrackTaintedDataEnteringSink(
                ISymbol sinkSymbol,
                Location sinkLocation,
                IEnumerable<SinkKind> sinkKinds,
                IEnumerable<SymbolAccess> sources)
            {
                SymbolAccess sink = new SymbolAccess(sinkSymbol, sinkLocation, this.OwningSymbol);
                this.TrackTaintedDataEnteringSink(sink, sinkKinds, sources);
            }

            private void TrackTaintedDataEnteringSink(SymbolAccess sink, IEnumerable<SinkKind> sinkKinds, IEnumerable<SymbolAccess> sources)
            {
                if (!this.TaintedSourcesBySink.TryGetValue(sink, out (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SymbolAccess>.Builder SourceOrigins) data))
                {
                    data = (ImmutableHashSet.CreateBuilder<SinkKind>(), ImmutableHashSet.CreateBuilder<SymbolAccess>());
                    this.TaintedSourcesBySink.Add(sink, data);
                }

                data.SinkKinds.UnionWith(sinkKinds);
                data.SourceOrigins.UnionWith(sources);
            }

            /// <summary>
            /// Determines if tainted data is entering a sink as a method call or constructor argument, and if so, flags it.
            /// </summary>
            /// <param name="targetMethod">Method being invoked.</param>
            /// <param name="taintedArguments">Arguments with tainted data to the method.</param>
            /// <param name="originalOperation">Original IOperation for the method/constructor invocation.</param>
            private void ProcessTaintedDataEnteringInvocationOrCreation(
                IMethodSymbol targetMethod,
                IEnumerable<IArgumentOperation> taintedArguments,
                IOperation originalOperation)
            {
                if (targetMethod.ContainingType != null && taintedArguments.Any())
                {
                    IEnumerable<SinkInfo>? infosForType = this.DataFlowAnalysisContext.SinkInfos.GetInfosForType(targetMethod.ContainingType);
                    if (infosForType != null)
                    {
                        foreach (IArgumentOperation taintedArgument in taintedArguments)
                        {
                            if (IsMethodArgumentASink(targetMethod, infosForType, taintedArgument, out HashSet<SinkKind>? sinkKinds))
                            {
                                TaintedDataAbstractValue abstractValue = this.GetCachedAbstractValue(taintedArgument);
                                this.TrackTaintedDataEnteringSink(targetMethod, originalOperation.Syntax.GetLocation(), sinkKinds, abstractValue.SourceOrigins);
                            }
                        }
                    }
                }

                if (this.TryGetInterproceduralAnalysisResult(originalOperation, out TaintedDataAnalysisResult? subResult)
                    && !subResult.TaintedDataSourceSinks.IsEmpty)
                {
                    foreach (TaintedDataSourceSink sourceSink in subResult.TaintedDataSourceSinks)
                    {
                        if (!this.TaintedSourcesBySink.TryGetValue(
                                sourceSink.Sink,
                                out (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SymbolAccess>.Builder SourceOrigins) data))
                        {
                            data = (ImmutableHashSet.CreateBuilder<SinkKind>(), ImmutableHashSet.CreateBuilder<SymbolAccess>());
                            this.TaintedSourcesBySink.Add(sourceSink.Sink, data);
                        }

                        data.SinkKinds.UnionWith(sourceSink.SinkKinds);
                        data.SourceOrigins.UnionWith(sourceSink.SourceOrigins);
                    }
                }
            }

            private void ProcessAssignmentOperation(IAssignmentOperation assignmentOperation)
            {
                TaintedDataAbstractValue assignmentValueAbstractValue = this.GetCachedAbstractValue(assignmentOperation.Value);
                if (assignmentOperation.Target != null
                    && assignmentValueAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted
                    && assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation)
                {
                    if (this.IsPropertyASink(propertyReferenceOperation, out HashSet<SinkKind>? sinkKinds))
                    {
                        this.TrackTaintedDataEnteringSink(
                            propertyReferenceOperation.Member,
                            propertyReferenceOperation.Syntax.GetLocation(),
                            sinkKinds,
                            assignmentValueAbstractValue.SourceOrigins);
                    }

                    if (this.DataFlowAnalysisContext.SourceInfos.IsSourceTransferProperty(propertyReferenceOperation))
                    {
                        SetTaintedForEntity(propertyReferenceOperation.Instance!, assignmentValueAbstractValue);
                    }
                }
            }

            /// <summary>
            /// Determines if the instance method call returns tainted data.
            /// </summary>
            /// <param name="method">Instance method being called.</param>
            /// <param name="arguments">Arguments passed to the method.</param>
            /// <param name="taintedParameterNames">Names of the tainted input parameters.</param>
            /// <param name="taintedParameterPairs">Matched pairs of "tainted parameter name" to "sanitized parameter name".</param>
            /// <returns>True if the method sanitizes data (returned or as an output parameter), false otherwise.</returns>
            private bool IsSanitizingMethod(
                IMethodSymbol method,
                ImmutableArray<IArgumentOperation> arguments,
                ISet<string> taintedParameterNames,
                [NotNullWhen(returnValue: true)] out PooledHashSet<(string, string)>? taintedParameterPairs)
            {
                taintedParameterPairs = null;
                foreach (SanitizerInfo sanitizerInfo in this.DataFlowAnalysisContext.SanitizerInfos.GetInfosForType(method.ContainingType))
                {
                    if (method.MethodKind == MethodKind.Constructor
                        && sanitizerInfo.IsConstructorSanitizing)
                    {
                        taintedParameterPairs = PooledHashSet<(string, string)>.GetInstance();
                        return true;
                    }

                    foreach ((MethodMatcher methodMatcher, ImmutableHashSet<(string source, string end)> sourceToEnds) in sanitizerInfo.SanitizingMethods)
                    {
                        if (methodMatcher(method.Name, arguments))
                        {
                            taintedParameterPairs ??= PooledHashSet<(string, string)>.GetInstance();

                            taintedParameterPairs.UnionWith(sourceToEnds.Where(s => taintedParameterNames.Contains(s.source)));
                        }
                    }
                }

                return taintedParameterPairs != null;
            }

            /// <summary>
            /// Determines if untaint the instance after calling the method.
            /// </summary>
            /// <param name="method">Instance method being called.</param>
            /// <returns>True if untaint the instance, false otherwise.</returns>
            private bool IsSanitizingInstanceMethod(IMethodSymbol method)
            {
                foreach (SanitizerInfo sanitizerInfo in this.DataFlowAnalysisContext.SanitizerInfos.GetInfosForType(method.ContainingType))
                {
                    if (sanitizerInfo.SanitizingInstanceMethods.Contains(method.MetadataName))
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Determines if tainted data passed as arguments to a method enters a tainted data sink.
            /// </summary>
            /// <param name="method">Method being invoked.</param>
            /// <param name="taintedArgument">Argument passed to the method invocation that is tainted.</param>
            /// <returns>True if any of the tainted data arguments enters a sink, false otherwise.</returns>
            private static bool IsMethodArgumentASink(IMethodSymbol method, IEnumerable<SinkInfo> infosForType, IArgumentOperation taintedArgument, [NotNullWhen(returnValue: true)] out HashSet<SinkKind>? sinkKinds)
            {
                sinkKinds = null;
                Lazy<HashSet<SinkKind>> lazySinkKinds = new Lazy<HashSet<SinkKind>>(() => []);
                foreach (SinkInfo sinkInfo in infosForType)
                {
                    if (lazySinkKinds.IsValueCreated && lazySinkKinds.Value.IsSupersetOf(sinkInfo.SinkKinds) ||
                        taintedArgument.Parameter == null)
                    {
                        continue;
                    }

                    if (method.MethodKind == MethodKind.Constructor
                        && sinkInfo.IsAnyStringParameterInConstructorASink
                        && taintedArgument.Parameter.Type.SpecialType == SpecialType.System_String)
                    {
                        lazySinkKinds.Value.UnionWith(sinkInfo.SinkKinds);
                    }
                    else if (sinkInfo.SinkMethodParameters.TryGetValue(method.MetadataName, out var sinkParameters)
                        && sinkParameters.Contains(taintedArgument.Parameter.MetadataName))
                    {
                        lazySinkKinds.Value.UnionWith(sinkInfo.SinkKinds);
                    }
                }

                if (lazySinkKinds.IsValueCreated)
                {
                    sinkKinds = lazySinkKinds.Value;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// Determines if a property is a sink.
            /// </summary>
            /// <param name="propertyReferenceOperation">Property to check if it's a sink.</param>
            /// <param name="sinkKinds">If the property is a sink, <see cref="HashSet{SinkInfo}"/> containing the kinds of sinks; null otherwise.</param>
            /// <returns>True if the property is a sink, false otherwise.</returns>
            private bool IsPropertyASink(IPropertyReferenceOperation propertyReferenceOperation, [NotNullWhen(returnValue: true)] out HashSet<SinkKind>? sinkKinds)
            {
                Lazy<HashSet<SinkKind>> lazySinkKinds = new Lazy<HashSet<SinkKind>>(() => []);
                foreach (SinkInfo sinkInfo in this.DataFlowAnalysisContext.SinkInfos.GetInfosForType(propertyReferenceOperation.Member.ContainingType))
                {
                    if (lazySinkKinds.IsValueCreated && lazySinkKinds.Value.IsSupersetOf(sinkInfo.SinkKinds))
                    {
                        continue;
                    }

                    if (sinkInfo.SinkProperties.Contains(propertyReferenceOperation.Member.MetadataName))
                    {
                        lazySinkKinds.Value.UnionWith(sinkInfo.SinkKinds);
                    }
                }

                if (lazySinkKinds.IsValueCreated)
                {
                    sinkKinds = lazySinkKinds.Value;
                    return true;
                }
                else
                {
                    sinkKinds = null;
                    return false;
                }
            }

            private IEnumerable<IArgumentOperation> GetTaintedArguments(ImmutableArray<IArgumentOperation> arguments)
            {
                return arguments.Where(
                    a => this.GetCachedAbstractValue(a).Kind == TaintedDataAbstractValueKind.Tainted
                         && a.Parameter != null
                         && (a.Parameter.RefKind == RefKind.None
                             || a.Parameter.RefKind == RefKind.Ref
                             || a.Parameter.RefKind == RefKind.In));
            }

            private void SetTaintedForEntity(IOperation operation, TaintedDataAbstractValue value)
            {
                if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity? analysisEntity))
                {
                    this.CurrentAnalysisData.SetAbstractValue(analysisEntity, value);
                }
            }

            protected override void ApplyInterproceduralAnalysisResultCore(TaintedDataAnalysisData resultData)
                => ApplyInterproceduralAnalysisResultHelper(resultData.CoreAnalysisData);

            protected override TaintedDataAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
                => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData.CoreAnalysisData, SetAbstractValueCore);

        }
    }
}
