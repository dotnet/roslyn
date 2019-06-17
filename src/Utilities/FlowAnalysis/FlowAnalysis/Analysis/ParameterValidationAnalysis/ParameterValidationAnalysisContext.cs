// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;
    using InterproceduralParameterValidationAnalysisData = InterproceduralAnalysisData<DictionaryAnalysisData<AbstractLocation, ParameterValidationAbstractValue>, ParameterValidationAnalysisContext, ParameterValidationAbstractValue>;
    using ParameterValidationAnalysisData = DictionaryAnalysisData<AbstractLocation, ParameterValidationAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    /// <summary>
    /// Analysis context for execution of <see cref="ParameterValidationAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class ParameterValidationAnalysisContext : AbstractDataFlowAnalysisContext<ParameterValidationAnalysisData, ParameterValidationAnalysisContext, ParameterValidationAnalysisResult, ParameterValidationAbstractValue>
    {
        private ParameterValidationAnalysisContext(
            AbstractValueDomain<ParameterValidationAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            ImmutableHashSet<string> nullCheckValidationMethodNames,
            ImmutableHashSet<IMethodSymbol> nullCheckValidationMethodSymbols,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<ParameterValidationAnalysisContext, ParameterValidationAnalysisResult> tryGetOrComputeAnalysisResult,
            ControlFlowGraph parentControlFlowGraphOpt,
            InterproceduralParameterValidationAnalysisData interproceduralAnalysisDataOpt,
            bool trackHazardousParameterUsages)
            : base(valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol, interproceduralAnalysisConfig,
                  pessimisticAnalysis, predicateAnalysis: false, exceptionPathsAnalysis: false,
                  copyAnalysisResultOpt: null, pointsToAnalysisResultOpt, valueContentAnalysisResultOpt: null,
                  tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt, interproceduralAnalysisDataOpt,
                  interproceduralAnalysisPredicateOpt: null)
        {
            TrackHazardousParameterUsages = trackHazardousParameterUsages;
            NullCheckValidationMethodNames = nullCheckValidationMethodNames;
            NullCheckValidationMethodSymbols = nullCheckValidationMethodSymbols;
        }

        public static ParameterValidationAnalysisContext Create(
            AbstractValueDomain<ParameterValidationAbstractValue> valueDomain,
            WellKnownTypeProvider wellKnownTypeProvider,
            ControlFlowGraph controlFlowGraph,
            ISymbol owningSymbol,
            ImmutableArray<string> nullCheckValidationMethods,
            InterproceduralAnalysisConfiguration interproceduralAnalysisConfig,
            bool pessimisticAnalysis,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            Func<ParameterValidationAnalysisContext, ParameterValidationAnalysisResult> tryGetOrComputeAnalysisResult)
        {
            var nullCheckValidationMethodNames = nullCheckValidationMethods.IsEmpty ?
                ImmutableHashSet<string>.Empty :
                nullCheckValidationMethods.Where(s => !s.Contains("."))
                    .ToImmutableHashSet(wellKnownTypeProvider.Compilation.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

            var nullCheckValidationMethodSymbols = nullCheckValidationMethods.IsEmpty ?
                ImmutableHashSet<IMethodSymbol>.Empty :
                nullCheckValidationMethods.Where(s => s.Contains("."))
                    .Select(s => s.StartsWith("M:", StringComparison.Ordinal) ? s : "M:" + s)
                    .SelectMany(s => DocumentationCommentId.GetSymbolsForDeclarationId(s, wellKnownTypeProvider.Compilation))
                    .OfType<IMethodSymbol>()
                    .WhereNotNull()
                    .ToImmutableHashSet();

            return new ParameterValidationAnalysisContext(
                valueDomain, wellKnownTypeProvider, controlFlowGraph, owningSymbol,
                nullCheckValidationMethodNames, nullCheckValidationMethodSymbols, interproceduralAnalysisConfig,
                pessimisticAnalysis, pointsToAnalysisResultOpt, tryGetOrComputeAnalysisResult, parentControlFlowGraphOpt: null,
                interproceduralAnalysisDataOpt: null, trackHazardousParameterUsages: false);
        }

        public override ParameterValidationAnalysisContext ForkForInterproceduralAnalysis(
            IMethodSymbol invokedMethod,
            ControlFlowGraph invokedCfg,
            IOperation operation,
            PointsToAnalysisResult pointsToAnalysisResultOpt,
            CopyAnalysisResult copyAnalysisResultOpt,
            ValueContentAnalysisResult valueContentAnalysisResultOpt,
            InterproceduralParameterValidationAnalysisData interproceduralAnalysisData)
        {
            Debug.Assert(pointsToAnalysisResultOpt != null);
            Debug.Assert(copyAnalysisResultOpt == null);
            Debug.Assert(valueContentAnalysisResultOpt == null);

            // Do not invoke any interprocedural analysis more than one level down.
            // We only care about analyzing validation methods.
            return new ParameterValidationAnalysisContext(
                ValueDomain, WellKnownTypeProvider, invokedCfg, invokedMethod,
                NullCheckValidationMethodNames, NullCheckValidationMethodSymbols, InterproceduralAnalysisConfiguration,
                PessimisticAnalysis, pointsToAnalysisResultOpt, TryGetOrComputeAnalysisResult, ControlFlowGraph,
                interproceduralAnalysisData, TrackHazardousParameterUsages);
        }

        public ParameterValidationAnalysisContext WithTrackHazardousParameterUsages()
            => new ParameterValidationAnalysisContext(
                ValueDomain, WellKnownTypeProvider, ControlFlowGraph,
                OwningSymbol, NullCheckValidationMethodNames, NullCheckValidationMethodSymbols,
                InterproceduralAnalysisConfiguration, PessimisticAnalysis,
                PointsToAnalysisResultOpt, TryGetOrComputeAnalysisResult, ParentControlFlowGraphOpt,
                InterproceduralAnalysisDataOpt, trackHazardousParameterUsages: true);

        public bool TrackHazardousParameterUsages { get; }

        private ImmutableHashSet<string> NullCheckValidationMethodNames { get; }
        private ImmutableHashSet<IMethodSymbol> NullCheckValidationMethodSymbols { get; }
        public bool IsNullCheckValidationMethod(IMethodSymbol method)
            => NullCheckValidationMethodSymbols.Contains(method) || NullCheckValidationMethodNames.Contains(method.Name);
        protected override void ComputeHashCodePartsSpecific(ArrayBuilder<int> builder)
        {
            builder.Add(TrackHazardousParameterUsages.GetHashCode());
            builder.Add(HashUtilities.Combine(NullCheckValidationMethodNames));
            builder.Add(HashUtilities.Combine(NullCheckValidationMethodSymbols));
        }
    }
}
