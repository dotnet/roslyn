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
                ProcessRegularInvocationOrCreation(operation.Constructor, operation.Arguments, operation);
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
                TaintedDataAbstractValue baseVisit = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);

                ProcessRegularInvocationOrCreation(method, visitedArguments, originalOperation);

                if (visitedInstance != null)
                {
                    TaintedDataAbstractValue instanceAbstractValue = this.GetCachedAbstractValue(visitedInstance);
                    if (instanceAbstractValue.Kind == TaintedDataAbstractValueKind.Tainted)
                    {
                        return instanceAbstractValue;
                    }

                    if (WebInputSources.IsTaintedMethod(this.WellKnownTypeProvider, visitedInstance, method))
                    {
                        return TaintedDataAbstractValue.CreateTainted(method, originalOperation.Syntax, this.OwningSymbol);
                    }
                }

                return baseVisit;
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
            /// <param name="arguments">Arguments to the method.</param>
            /// <param name="originalOperation">Original IOperation for the method/constructor invocation.</param>
            private void ProcessRegularInvocationOrCreation(IMethodSymbol targetMethod, ImmutableArray<IArgumentOperation> arguments, IOperation originalOperation)
            {
                IEnumerable<IArgumentOperation> taintedArguments = arguments.Where(
                    a => this.GetCachedAbstractValue(a).Kind == TaintedDataAbstractValueKind.Tainted
                         && (a.Parameter.RefKind == RefKind.None
                             || a.Parameter.RefKind == RefKind.Ref
                             || a.Parameter.RefKind == RefKind.In));
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
        }
    }
}
