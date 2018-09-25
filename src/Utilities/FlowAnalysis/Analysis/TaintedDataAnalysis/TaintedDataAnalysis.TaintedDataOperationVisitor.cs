// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataOperationVisitor : AnalysisEntityDataFlowOperationVisitor<TaintedDataAnalysisData, TaintedDataAnalysisContext, TaintedDataAnalysisResult, TaintedDataAbstractValue>
        {
            /// <summary>
            /// Mapping of a tainted data sinks to their originating sources.
            /// </summary>
            /// <remarks>Keys are <see cref="SymbolAccess"/> sinks where the tainted data entered, values are <see cref="SymbolAccess"/>s where the tainted data originated from.</remarks>
            private Dictionary<SymbolAccess, HashSet<SymbolAccess>> TaintedSourcesBySink { get; set; }

            public TaintedDataOperationVisitor(TaintedDataAnalysisContext analysisContext)
                : base(analysisContext)
            {
                this.TaintedSourcesBySink = new Dictionary<SymbolAccess, HashSet<SymbolAccess>>();
            }

            public ImmutableArray<TaintedDataSourceSink> GetTaintedDataSourceSinkEntries()
            {
                ImmutableArray<TaintedDataSourceSink>.Builder builder = ImmutableArray.CreateBuilder<TaintedDataSourceSink>();
                foreach (KeyValuePair<SymbolAccess, HashSet<SymbolAccess>> kvp in this.TaintedSourcesBySink)
                {
                    SymbolAccess[] sourceOrigins = kvp.Value.ToArray();

                    Array.Sort(sourceOrigins);

                    builder.Add(
                        new TaintedDataSourceSink(
                            kvp.Key,
                            SinkKind.Sql,
                            ImmutableArray.Create<SymbolAccess>(sourceOrigins)));
                }

                builder.Sort((x, y) => LocationComparer.Instance.Compare(x.Sink.SyntaxNode.GetLocation(), y.Sink.SyntaxNode.GetLocation()));
                 
                return builder.ToImmutableArray();
            }

            protected override TaintedDataAbstractValue ComputeAnalysisValueForReferenceOperation(IOperation operation, TaintedDataAbstractValue defaultValue)
            {
                if (operation is IPropertyReferenceOperation propertyReferenceOperation
                    && WebInputSources.IsTaintedProperty(this.WellKnownTypeProvider, propertyReferenceOperation))
                {
                    return TaintedDataAbstractValue.CreateTainted(propertyReferenceOperation.Member, propertyReferenceOperation.Syntax, this.OwningSymbol);
                }

                IOperation referenceeOperation = operation.GetReferenceOperationReferencee();
                if (referenceeOperation != null)
                {
                    TaintedDataAbstractValue referenceeAbstractValue = this.GetCachedAbstractValue(referenceeOperation);
                    if (referenceeAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted)
                    {
                        return referenceeAbstractValue;
                    }
                }

                if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
                {
                    return this.CurrentAnalysisData.TryGetValue(analysisEntity, out TaintedDataAbstractValue value) ? value : defaultValue;
                }

                return defaultValue;
            }

            protected override void AddTrackedEntities(ImmutableArray<AnalysisEntity>.Builder builder)
            {
                this.CurrentAnalysisData.AddTrackedEntities(builder);
            }

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
                return this.CurrentAnalysisData.TryGetValue(analysisEntity, out TaintedDataAbstractValue value) ? value : TaintedDataAbstractValue.Unknown;
            }

            protected override TaintedDataAnalysisData GetClonedAnalysisData(TaintedDataAnalysisData analysisData)
            {
                return (TaintedDataAnalysisData) analysisData.Clone();
            }

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
            {
                return this.CurrentAnalysisData.HasAbstractValue(analysisEntity);
            }

            protected override bool HasAnyAbstractValue(TaintedDataAnalysisData data)
            {
                return this.CurrentAnalysisData.HasAnyAbstractValue;
            }

            protected override TaintedDataAnalysisData MergeAnalysisData(TaintedDataAnalysisData value1, TaintedDataAnalysisData value2)
            {
                return TaintedDataAnalysisDomainInstance.Merge(value1, value2);
            }

            protected override void ResetCurrentAnalysisData()
            {
                this.CurrentAnalysisData.Reset(this.ValueDomain.UnknownOrMayBeValue);
            }

            protected override TaintedDataAnalysisData GetEmptyAnalysisData()
            {
                return new TaintedDataAnalysisData();
            }

            protected override TaintedDataAnalysisData GetAnalysisDataAtBlockEnd(TaintedDataAnalysisResult analysisResult, BasicBlock block)
            {
                return new TaintedDataAnalysisData(analysisResult[block].OutputData);
            }

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, TaintedDataAbstractValue value)
            {
                if (value.Kind == TaintedDataAbstractValueKind.Tainted
                    || this.CurrentAnalysisData.CoreAnalysisData.ContainsKey(analysisEntity))
                {
                    // Only track tainted data, or sanitized data.
                    // If it's new, and it's untainted, we don't care.
                    this.CurrentAnalysisData.SetAbstactValue(analysisEntity, value);
                }
            }

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity)
            {
                this.CurrentAnalysisData.RemoveEntries(analysisEntity);
            }

            // So we can hook into constructor calls.
            public override TaintedDataAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
            {
                var value = base.VisitObjectCreation(operation, argument);
                IEnumerable<IArgumentOperation> taintedArguments = operation.Arguments.Where(
                    a => this.GetCachedAbstractValue(a).Kind == TaintedDataAbstractValueKind.Tainted
                         && (a.Parameter.RefKind == RefKind.None
                             || a.Parameter.RefKind == RefKind.Ref
                             || a.Parameter.RefKind == RefKind.In));
                if (taintedArguments.Any())
                {
                    ProcessRegularInvocationOrCreation(operation.Constructor, taintedArguments, operation);

                    IEnumerable<TaintedDataAbstractValue> allTaintedValues = taintedArguments.Select(a => this.GetCachedAbstractValue(a));
                    if (value.Kind == TaintedDataAbstractValueKind.Tainted)
                    {
                        allTaintedValues = allTaintedValues.Concat(value);
                    }

                    return TaintedDataAbstractValue.MergeTainted(allTaintedValues);
                }

                return value;
            }

            public override TaintedDataAbstractValue VisitBinaryOperatorCore(IBinaryOperation operation, object argument)
            {
                TaintedDataAbstractValue leftAbstractValue = Visit(operation.LeftOperand, argument);
                TaintedDataAbstractValue rightAbstractValue = Visit(operation.RightOperand, argument);

                return TaintedDataAbstractValueDomain.Default.Merge(leftAbstractValue, rightAbstractValue);
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
                TaintedDataAbstractValue baseVisit = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                    method, 
                    visitedInstance,
                    visitedArguments, 
                    invokedAsDelegate, 
                    originalOperation, 
                    defaultValue);

                IEnumerable<IArgumentOperation> taintedArguments = visitedArguments.Where(
                    a => this.GetCachedAbstractValue(a).Kind == TaintedDataAbstractValueKind.Tainted
                         && (a.Parameter.RefKind == RefKind.None
                             || a.Parameter.RefKind == RefKind.Ref
                             || a.Parameter.RefKind == RefKind.In));

                if (taintedArguments.Any())
                {
                    ProcessRegularInvocationOrCreation(method, visitedArguments, originalOperation);
                }

                if (PrimitiveTypeConverterSanitizers.IsSanitizingMethod(this.WellKnownTypeProvider, method))
                {
                    return TaintedDataAbstractValue.NotTainted;
                }

                TaintedDataAbstractValue returnValue = baseVisit;
                if (visitedInstance != null)
                {
                    TaintedDataAbstractValue instanceAbstractValue = this.GetCachedAbstractValue(visitedInstance);
                    if (instanceAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted)
                    {
                        returnValue = instanceAbstractValue;
                    }
                    else if (WebInputSources.IsTaintedMethod(this.WellKnownTypeProvider, visitedInstance, method))
                    {
                        returnValue = TaintedDataAbstractValue.CreateTainted(method, originalOperation.Syntax, this.OwningSymbol);
                    }
                }

                // TODO paulming: This is too conservative, and should only apply for non-interprocedural analysis.
                // E.g. tainted arguments are passed to a method that sanitizes the data.
                if (taintedArguments.Any())
                {
                    IEnumerable<TaintedDataAbstractValue> allTaintedValues =
                        taintedArguments.Select(a => this.GetCachedAbstractValue(a));
                    if (returnValue.Kind == TaintedDataAbstractValueKind.Tainted)
                    {
                        allTaintedValues = allTaintedValues.Concat(returnValue);
                    }

                    return TaintedDataAbstractValue.MergeTainted(allTaintedValues);
                }
                else
                {
                    return returnValue;
                }
            }

            /// <summary>
            /// Computes abstract value for out or ref arguments when not performing interprocedural analysis.
            /// </summary>
            /// <param name="analysisEntity">Analysis entity.</param>
            /// <param name="operation">IArgumentOperation.</param>
            /// <param name="defaultValue">Default TaintedDataAbstractValue if we don't need to override.</param>
            /// <returns></returns>
            protected override TaintedDataAbstractValue ComputeAnalysisValueForEscapedRefOrOutArgument(
                AnalysisEntity analysisEntity, 
                IArgumentOperation operation,
                TaintedDataAbstractValue defaultValue)
            {
                if (operation.Parent is IInvocationOperation invocationOperation)
                {
                    // Treat ref or out arguments as the same as the invocation operation.
                    TaintedDataAbstractValue returnValueAbstractValue = this.GetCachedAbstractValue(invocationOperation);
                    return returnValueAbstractValue;
                }
                else
                {
                    return defaultValue;
                }
            }

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

                if (taintedAbstractValues.Any())
                {
                    return TaintedDataAbstractValue.MergeTainted(taintedAbstractValues);
                }
                else
                {
                    return baseAbstractValue;
                }
            }

            protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, TaintedDataAbstractValue value)
            {
                base.SetAbstractValueForArrayElementInitializer(arrayCreation, indices, elementType, initializer, value);
            }

            protected override TaintedDataAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object argument)
            {
                TaintedDataAbstractValue taintedDataAbstractValue = base.VisitAssignmentOperation(operation, argument);
                ProcessAssignmentOperation(operation);
                return taintedDataAbstractValue;
            }

            private void TrackTaintedDataEnteringSink(ISymbol sinkSymbol, SyntaxNode sinkSyntax, IEnumerable<SymbolAccess> sources)
            {
                SymbolAccess sink = new SymbolAccess(sinkSymbol, sinkSyntax, this.OwningSymbol);
                if (!this.TaintedSourcesBySink.TryGetValue(sink, out HashSet<SymbolAccess> sourceOrigins))
                {
                    sourceOrigins = new HashSet<SymbolAccess>();
                    this.TaintedSourcesBySink.Add(sink, sourceOrigins);
                }

                sourceOrigins.UnionWith(sources);
            }

            /// <summary>
            /// Determines if tainted data is entering a sink as a method call argument, and if so, flags it.
            /// </summary>
            /// <param name="targetMethod">Method being invoked.</param>
            /// <param name="taintedArguments">Arguments with tainted data to the method.</param>
            /// <param name="originalOperation">Original IOperation for the method/constructor invocation.</param>
            private void ProcessRegularInvocationOrCreation(IMethodSymbol targetMethod, IEnumerable<IArgumentOperation> taintedArguments, IOperation originalOperation)
            {
                if (SqlSinks.IsMethodArgumentASink(this.WellKnownTypeProvider, targetMethod, taintedArguments))
                {
                    foreach (IArgumentOperation taintedArgument in taintedArguments)
                    {
                        TaintedDataAbstractValue abstractValue = this.GetCachedAbstractValue(taintedArgument);
                        this.TrackTaintedDataEnteringSink(targetMethod, originalOperation.Syntax, abstractValue.SourceOrigins);
                    }
                }
            }

            private void ProcessAssignmentOperation(IAssignmentOperation assignmentOperation)
            {
                TaintedDataAbstractValue assignmentValueAbstractValue = this.GetCachedAbstractValue(assignmentOperation.Value);
                if (assignmentOperation.Target != null
                    && assignmentValueAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted
                    && assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation
                    && SqlSinks.IsPropertyASink(this.WellKnownTypeProvider, propertyReferenceOperation))
                {
                    this.TrackTaintedDataEnteringSink(propertyReferenceOperation.Member, propertyReferenceOperation.Syntax, assignmentValueAbstractValue.SourceOrigins);
                }
            }

            private IEnumerable<TaintedDataAbstractValue> GetTaintedValuesFromInputArguments(IEnumerable<IArgumentOperation> arguments)
            {
                foreach (IArgumentOperation argument in arguments)
                {
                    if (argument.Parameter.RefKind == RefKind.None
                        || argument.Parameter.RefKind == RefKind.Ref
                        || argument.Parameter.RefKind == RefKind.In)
                    {
                        TaintedDataAbstractValue value = this.GetCachedAbstractValue(argument);
                        if (value.Kind == TaintedDataAbstractValueKind.Tainted)
                        {
                            yield return value;
                        }
                    }
                }
            }
        }
    }
}
