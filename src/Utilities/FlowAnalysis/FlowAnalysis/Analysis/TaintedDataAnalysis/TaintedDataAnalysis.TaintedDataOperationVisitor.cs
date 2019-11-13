// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataOperationVisitor : AnalysisEntityDataFlowOperationVisitor<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataAbstractValue>
        {
            /// <summary>
            /// Mapping of a tainted data sinks to their originating sources.
            /// </summary>
            /// <remarks>Keys are <see cref="SymbolAccess"/> sinks where the tainted data entered, values are <see cref="SymbolAccess"/>s where the tainted data originated from.</remarks>
            private Dictionary<SymbolAccess, (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SymbolAccess>.Builder SourceOrigins)> TaintedSourcesBySink { get; }

            public TaintedDataOperationVisitor(TaintedDataAnalysisContext analysisContext)
                : base(analysisContext)
            {
                this.TaintedSourcesBySink = new Dictionary<SymbolAccess, (ImmutableHashSet<SinkKind>.Builder SinkKinds, ImmutableHashSet<SymbolAccess>.Builder SourceOrigins)>();
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

            protected override TaintedDataAbstractValue GetAbstractDefaultValue(ITypeSymbol type)
            {
                return TaintedDataAbstractValue.NotTainted;
            }

            protected override TaintedDataAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
            {
                return this.CurrentAnalysisData.TryGetValue(analysisEntity, out TaintedDataAbstractValue value) ? value : TaintedDataAbstractValue.NotTainted;
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
                return TaintedDataAnalysisDomainInstance.Merge(value1, value2);
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

            public override TaintedDataAbstractValue DefaultVisit(IOperation operation, object argument)
            {
                // This handles most cases of tainted data flowing from child operations to parent operations.
                // Examples:
                // - tainted input parameters to method calls returns, and out/ref parameters, tainted (assuming no interprocedural)
                // - adding a tainted value to something makes the result tainted
                // - instantiating an object with tainted data makes the new object tainted

                List<TaintedDataAbstractValue> taintedValues = null;
                foreach (IOperation childOperation in operation.Children)
                {
                    TaintedDataAbstractValue childValue = Visit(childOperation, argument);
                    if (childValue.Kind == TaintedDataAbstractValueKind.Tainted)
                    {
                        if (taintedValues == null)
                        {
                            taintedValues = new List<TaintedDataAbstractValue>();
                        }

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

            protected override TaintedDataAbstractValue ComputeAnalysisValueForReferenceOperation(IOperation operation, TaintedDataAbstractValue defaultValue)
            {
                // If the property reference itself is a tainted data source
                if (operation is IPropertyReferenceOperation propertyReferenceOperation
                    && this.DataFlowAnalysisContext.SourceInfos.IsSourceProperty(propertyReferenceOperation.Property))
                {
                    return TaintedDataAbstractValue.CreateTainted(propertyReferenceOperation.Member, propertyReferenceOperation.Syntax, this.OwningSymbol);
                }

                if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
                {
                    return this.CurrentAnalysisData.TryGetValue(analysisEntity, out TaintedDataAbstractValue value) ? value : defaultValue;
                }

                return defaultValue;
            }

            // So we can hook into constructor calls.
            public override TaintedDataAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
            {
                TaintedDataAbstractValue baseValue = base.VisitObjectCreation(operation, argument);
                IEnumerable<IArgumentOperation> taintedArguments = GetTaintedArguments(operation.Arguments);
                if (taintedArguments.Any())
                {
                    ProcessTaintedDataEnteringInvocationOrCreation(operation.Constructor, taintedArguments, operation);
                }

                return baseValue;
            }

            public override TaintedDataAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                IMethodSymbol method,
                IOperation visitedInstance,
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

                PooledHashSet<string> taintedTargets = null;
                PooledHashSet<(string, string)> taintedParameterPairs = null;
                try
                {
                    if (this.IsSanitizingMethod(method))
                    {
                        result = TaintedDataAbstractValue.NotTainted;
                    }
                    else if (visitedInstance != null && this.IsSanitizingInstanceMethod(method))
                    {
                        result = TaintedDataAbstractValue.NotTainted;
                        SetTaintedForEntity(visitedInstance, result);
                    }
                    else if (this.DataFlowAnalysisContext.SourceInfos.IsSourceMethod(
                        method,
                        visitedArguments,
                        new Lazy<PointsToAnalysisResult>(() => DataFlowAnalysisContext.PointsToAnalysisResultOpt),
                        new Lazy<(PointsToAnalysisResult, ValueContentAnalysisResult)>(() => (DataFlowAnalysisContext.PointsToAnalysisResultOpt, DataFlowAnalysisContext.ValueContentAnalysisResultOpt)),
                        out taintedTargets))
                    {
                        foreach (string taintedTarget in taintedTargets)
                        {
                            if (taintedTarget != TaintedTargetValue.Return)
                            {
                                IArgumentOperation argumentOperation = visitedArguments.FirstOrDefault(o => o.Parameter.Name == taintedTarget);
                                if (argumentOperation != null)
                                {
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
                    }

                    if (this.DataFlowAnalysisContext.SourceInfos.IsSourceTransferMethod(
                        method,
                        visitedArguments,
                        visitedArguments
                            .Where(s => this.GetCachedAbstractValue(s).Kind == TaintedDataAbstractValueKind.Tainted)
                            .Select(s => s.Parameter.Name)
                            .ToImmutableArray(),
                        out taintedParameterPairs))
                    {
                        foreach ((string ifTaintedParameter, string thenTaintedTarget) in taintedParameterPairs)
                        {
                            IArgumentOperation thenTaintedTargetOperation = visitedArguments.FirstOrDefault(o => o.Parameter.Name == thenTaintedTarget);
                            if (thenTaintedTargetOperation != null)
                            {
                                SetTaintedForEntity(
                                    thenTaintedTargetOperation,
                                    this.GetCachedAbstractValue(
                                        visitedArguments.FirstOrDefault(o => o.Parameter.Name == ifTaintedParameter)));
                            }
                            else
                            {
                                Debug.Fail("Are the tainted data sources misconfigured?");
                            }
                        }
                    }
                }
                finally
                {
                    taintedTargets?.Free();
                    taintedParameterPairs?.Free();
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
                    Debug.Assert(!this.TryGetInterproceduralAnalysisResult(invocationOperation, out TaintedDataAnalysisResult _));

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
            public override TaintedDataAbstractValue VisitArrayInitializer(IArrayInitializerOperation operation, object argument)
            {
                HashSet<SymbolAccess> sourceOrigins = null;
                TaintedDataAbstractValue baseAbstractValue = base.VisitArrayInitializer(operation, argument);
                if (baseAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted)
                {
                    sourceOrigins = new HashSet<SymbolAccess>(baseAbstractValue.SourceOrigins);
                }

                IEnumerable<TaintedDataAbstractValue> taintedAbstractValues =
                    operation.ElementValues
                        .Select<IOperation, TaintedDataAbstractValue>(e => this.GetCachedAbstractValue(e))
                        .Where(v => v.Kind == TaintedDataAbstractValueKind.Tainted);
                if (baseAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted)
                {
                    taintedAbstractValues = taintedAbstractValues.Concat(baseAbstractValue);
                }

                TaintedDataAbstractValue result = null;
                if (taintedAbstractValues.Any())
                {
                    result = TaintedDataAbstractValue.MergeTainted(taintedAbstractValues);
                }

                IArrayCreationOperation arrayCreationOperation = operation.GetAncestor<IArrayCreationOperation>(OperationKind.ArrayCreation);
                if (arrayCreationOperation?.Type is IArrayTypeSymbol arrayTypeSymbol
                    && this.DataFlowAnalysisContext.SourceInfos.IsSourceConstantArrayOfType(arrayTypeSymbol)
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

            protected override TaintedDataAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object argument)
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
                if (this.IsMethodArgumentASink(targetMethod, taintedArguments, out HashSet<SinkKind> sinkKinds))
                {
                    foreach (IArgumentOperation taintedArgument in taintedArguments)
                    {
                        TaintedDataAbstractValue abstractValue = this.GetCachedAbstractValue(taintedArgument);
                        this.TrackTaintedDataEnteringSink(targetMethod, originalOperation.Syntax.GetLocation(), sinkKinds, abstractValue.SourceOrigins);
                    }
                }

                if (this.TryGetInterproceduralAnalysisResult(originalOperation, out TaintedDataAnalysisResult subResult)
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
                    && assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation
                    && this.IsPropertyASink(propertyReferenceOperation, out HashSet<SinkKind> sinkKinds))
                {
                    this.TrackTaintedDataEnteringSink(
                        propertyReferenceOperation.Member,
                        propertyReferenceOperation.Syntax.GetLocation(),
                        sinkKinds,
                        assignmentValueAbstractValue.SourceOrigins);
                }
            }

            /// <summary>
            /// Determines if the instance method call returns tainted data.
            /// </summary>
            /// <param name="method">Instance method being called.</param>
            /// <returns>True if the method returns tainted data, false otherwise.</returns>
            private bool IsSanitizingMethod(IMethodSymbol method)
            {
                foreach (SanitizerInfo sanitizerInfo in this.DataFlowAnalysisContext.SanitizerInfos.GetInfosForType(method.ContainingType))
                {
                    if (method.MethodKind == MethodKind.Constructor
                        && sanitizerInfo.IsConstructorSanitizing)
                    {
                        return true;
                    }

                    if (sanitizerInfo.SanitizingMethods.Contains(method.MetadataName))
                    {
                        return true;
                    }
                }

                return false;
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
            /// <param name="taintedArguments">Arguments passed to the method invocation that are tainted.</param>
            /// <returns>True if any of the tainted data arguments enters a sink, false otherwise.</returns>
            private bool IsMethodArgumentASink(IMethodSymbol method, IEnumerable<IArgumentOperation> taintedArguments, out HashSet<SinkKind> sinkKinds)
            {
                sinkKinds = null;

                if (method.ContainingType == null || !taintedArguments.Any())
                {
                    return false;
                }

                Lazy<HashSet<SinkKind>> lazySinkKinds = new Lazy<HashSet<SinkKind>>(() => new HashSet<SinkKind>());
                foreach (SinkInfo sinkInfo in this.DataFlowAnalysisContext.SinkInfos.GetInfosForType(method.ContainingType))
                {
                    if (lazySinkKinds.IsValueCreated && lazySinkKinds.Value.IsSupersetOf(sinkInfo.SinkKinds))
                    {
                        continue;
                    }

                    if (method.MethodKind == MethodKind.Constructor
                        && sinkInfo.IsAnyStringParameterInConstructorASink
                        && taintedArguments.Any(a => a.Parameter.Type.SpecialType == SpecialType.System_String))
                    {
                        lazySinkKinds.Value.UnionWith(sinkInfo.SinkKinds);
                    }
                    else if (sinkInfo.SinkMethodParameters.TryGetValue(method.MetadataName, out ImmutableHashSet<string> sinkParameters)
                        && taintedArguments.Any(a => sinkParameters.Contains(a.Parameter.MetadataName)))
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
            private bool IsPropertyASink(IPropertyReferenceOperation propertyReferenceOperation, out HashSet<SinkKind> sinkKinds)
            {
                Lazy<HashSet<SinkKind>> lazySinkKinds = new Lazy<HashSet<SinkKind>>(() => new HashSet<SinkKind>());
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
                         && (a.Parameter.RefKind == RefKind.None
                             || a.Parameter.RefKind == RefKind.Ref
                             || a.Parameter.RefKind == RefKind.In));
            }

            private void SetTaintedForEntity(IOperation operation, TaintedDataAbstractValue value)
            {
                if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
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
