// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
    using Microsoft.CodeAnalysis.Operations;

    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataOperationVisitor : AbstractLocationDataFlowOperationVisitor<TaintedDataAnalysisData, TaintedDataAbstractValue>
        {
            public TaintedDataOperationVisitor(
                TaintedDataAbstractValueDomain valueDomain,
                ISymbol owningSymbol,
                WellKnownTypeProvider wellKnownTypeProvider,
                ControlFlowGraph cfg,
                DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResult,
                bool pessimisticAnalysis)
                : base(
                      valueDomain,
                      owningSymbol,
                      wellKnownTypeProvider, 
                      cfg, 
                      pessimisticAnalysis,
                      predicateAnalysis: false, 
                      copyAnalysisResultOpt: null, 
                      pointsToAnalysisResultOpt: pointsToAnalysisResult)
            {
                Debug.Assert(owningSymbol.Kind == SymbolKind.Method);
                Debug.Assert(pointsToAnalysisResult != null);

                this.TaintedSourceSinkPairsBuilder = ImmutableArray.CreateBuilder<TaintedSourceSinkPair>();
            }

            private ImmutableArray<TaintedSourceSinkPair>.Builder TaintedSourceSinkPairsBuilder;

            protected override bool Equals(TaintedDataAnalysisData value1, TaintedDataAnalysisData value2)
            {
                return value1.Equals(value2);
            }

            protected override TaintedDataAbstractValue GetAbstractDefaultValue(ITypeSymbol type)
            {
                throw new System.NotImplementedException();
            }

            protected override TaintedDataAbstractValue GetAbstractValue(AbstractLocation location)
            {
                throw new System.NotImplementedException();
            }

            protected override TaintedDataAnalysisData GetClonedAnalysisData(TaintedDataAnalysisData analysisData)
            {
                throw new System.NotImplementedException();
            }

            protected override bool HasAnyAbstractValue(TaintedDataAnalysisData data)
            {
                throw new System.NotImplementedException();
            }

            protected override TaintedDataAnalysisData MergeAnalysisData(TaintedDataAnalysisData value1, TaintedDataAnalysisData value2)
            {
                throw new System.NotImplementedException();
            }

            protected override void ResetCurrentAnalysisData()
            {
                throw new System.NotImplementedException();
            }

            protected override void SetAbstractValue(AbstractLocation location, TaintedDataAbstractValue value)
            {
                this.CurrentAnalysisData[location] = value;
            }

            protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, TaintedDataAbstractValue value)
            {
                throw new System.NotImplementedException();
            }

            protected override void SetAbstractValueForAssignment(IOperation target, IOperation assignedValueOperation, TaintedDataAbstractValue assignedValue, bool mayBeAssignment = false)
            {
                throw new System.NotImplementedException();
            }

            /// <summary>
            /// Set the abstract value for a method input parameter.
            /// </summary>
            /// <param name="parameter">The paramater symbol.</param>
            /// <param name="pointsToAbstractValue">The points-to abstract locations.</param>
            protected override void SetValueForParameterPointsToLocationOnEntry(IParameterSymbol parameter, PointsToAbstractValue pointsToAbstractValue)
            {
                // TODO: Parameters for MVC controller methods are tainted.
                this.SetAbstractValue(pointsToAbstractValue, TaintedDataAbstractValue.NotTainted);
            }

            protected override void SetValueForParameterPointsToLocationOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity, PointsToAbstractValue pointsToAbstractValue)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
