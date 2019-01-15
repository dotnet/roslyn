// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Interprocedural analysis configuration parameters.
    /// </summary>
    internal struct InterproceduralAnalysisConfiguration : IEquatable<InterproceduralAnalysisConfiguration>
    {
        /// <summary>
        /// Defines the max length for method call chain (call stack size) for interprocedural analysis.
        /// This is done for performance reasons for analyzing methods with extremely large call trees.
        /// https://github.com/dotnet/roslyn-analyzers/issues/1809 tracks improving this heuristic.
        /// </summary>
        private const uint DefaultMaxInterproceduralMethodCallChain = 3;

        /// <summary>
        /// Defines the max length for lambda/local function method call chain (call stack size) for interprocedural analysis.
        /// This is done for performance reasons for analyzing methods with extremely large call trees.
        /// https://github.com/dotnet/roslyn-analyzers/issues/1809 tracks improving this heuristic.
        /// </summary>
        private const uint DefaultMaxInterproceduralLambdaorLocalFunctionCallChain = 10;

        private InterproceduralAnalysisConfiguration(
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            uint maxInterproceduralMethodCallChain,
            uint maxInterproceduralLambdaorLocalFunctionCallChain)
        {
            InterproceduralAnalysisKind = interproceduralAnalysisKind;
            MaxInterproceduralMethodCallChain = maxInterproceduralMethodCallChain;
            MaxInterproceduralLambdaorLocalFunctionCallChain = maxInterproceduralLambdaorLocalFunctionCallChain;
        }

        public static InterproceduralAnalysisConfiguration Create(
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            InterproceduralAnalysisKind defaultInterproceduralAnalysisKind,
            CancellationToken cancellationToken,
            uint defaultMaxInterproceduralMethodCallChain = DefaultMaxInterproceduralMethodCallChain,
            uint defaultMaxInterproceduralLambdaorLocalFunctionCallChain = DefaultMaxInterproceduralLambdaorLocalFunctionCallChain)
        {
            var kind = analyzerOptions.GetInterproceduralAnalysisKindOption(rule, defaultInterproceduralAnalysisKind, cancellationToken);

            var maxInterproceduralMethodCallChain = analyzerOptions.GetUnsignedIntegralOptionValue(
                optionName: EditorConfigOptionNames.MaxInterproceduralMethodCallChain,
                rule: rule,
                defaultValue: defaultMaxInterproceduralMethodCallChain,
                cancellationToken: cancellationToken);

            var maxInterproceduralLambdaorLocalFunctionCallChain = analyzerOptions.GetUnsignedIntegralOptionValue(
                optionName: EditorConfigOptionNames.MaxInterproceduralLambdaOrLocalFunctionCallChain,
                rule: rule,
                defaultValue: defaultMaxInterproceduralLambdaorLocalFunctionCallChain,
                cancellationToken: cancellationToken);

            return new InterproceduralAnalysisConfiguration(
                kind, maxInterproceduralMethodCallChain, maxInterproceduralLambdaorLocalFunctionCallChain);
        }

        public static InterproceduralAnalysisConfiguration Create(
            AnalyzerOptions analyzerOptions,
            ImmutableArray<DiagnosticDescriptor> rules,
            InterproceduralAnalysisKind defaultInterproceduralAnalysisKind,
            CancellationToken cancellationToken,
            uint defaultMaxInterproceduralMethodCallChain = DefaultMaxInterproceduralMethodCallChain,
            uint defaultMaxInterproceduralLambdaorLocalFunctionCallChain = DefaultMaxInterproceduralLambdaorLocalFunctionCallChain)
        {
            InterproceduralAnalysisKind maxKind = InterproceduralAnalysisKind.None;
            uint maxMethodCallChain = 0;
            uint maxLambdaorLocalFunctionCallChain = 0;
            foreach (var rule in rules)
            {
                var interproceduralAnalysisConfig = Create(analyzerOptions, rule, defaultInterproceduralAnalysisKind,
                    cancellationToken, defaultMaxInterproceduralMethodCallChain, defaultMaxInterproceduralLambdaorLocalFunctionCallChain);
                maxKind = (InterproceduralAnalysisKind)Math.Max((int)maxKind, (int)interproceduralAnalysisConfig.InterproceduralAnalysisKind);
                maxMethodCallChain = Math.Max(maxMethodCallChain, interproceduralAnalysisConfig.MaxInterproceduralMethodCallChain);
                maxLambdaorLocalFunctionCallChain = Math.Max(maxLambdaorLocalFunctionCallChain, interproceduralAnalysisConfig.MaxInterproceduralLambdaorLocalFunctionCallChain);
            }

            return new InterproceduralAnalysisConfiguration(maxKind, maxMethodCallChain, maxLambdaorLocalFunctionCallChain);
        }

        public InterproceduralAnalysisKind InterproceduralAnalysisKind { get; }

        public uint MaxInterproceduralMethodCallChain { get; }

        public uint MaxInterproceduralLambdaorLocalFunctionCallChain { get; }

        public override bool Equals(object obj)
        {
            return obj is InterproceduralAnalysisConfiguration otherParameters &&
                Equals(otherParameters);
        }

        public bool Equals(InterproceduralAnalysisConfiguration other)
        {
            return InterproceduralAnalysisKind == other.InterproceduralAnalysisKind &&
                MaxInterproceduralMethodCallChain == other.MaxInterproceduralMethodCallChain &&
                MaxInterproceduralLambdaorLocalFunctionCallChain == other.MaxInterproceduralLambdaorLocalFunctionCallChain;
        }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(InterproceduralAnalysisKind.GetHashCode(),
                HashUtilities.Combine(MaxInterproceduralMethodCallChain.GetHashCode(),
                                      MaxInterproceduralLambdaorLocalFunctionCallChain.GetHashCode()));
        }
    }
}
