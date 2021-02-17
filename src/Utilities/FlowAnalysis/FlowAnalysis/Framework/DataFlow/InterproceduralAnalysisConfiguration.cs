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
    public struct InterproceduralAnalysisConfiguration : IEquatable<InterproceduralAnalysisConfiguration>
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
        private const uint DefaultMaxInterproceduralLambdaOrLocalFunctionCallChain = 3;

        private InterproceduralAnalysisConfiguration(
            InterproceduralAnalysisKind interproceduralAnalysisKind,
            uint maxInterproceduralMethodCallChain,
            uint maxInterproceduralLambdaOrLocalFunctionCallChain)
        {
            InterproceduralAnalysisKind = interproceduralAnalysisKind;
            MaxInterproceduralMethodCallChain = maxInterproceduralMethodCallChain;
            MaxInterproceduralLambdaOrLocalFunctionCallChain = maxInterproceduralLambdaOrLocalFunctionCallChain;
        }

        public static InterproceduralAnalysisConfiguration Create(
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            InterproceduralAnalysisKind defaultInterproceduralAnalysisKind,
            CancellationToken cancellationToken,
            uint defaultMaxInterproceduralMethodCallChain = DefaultMaxInterproceduralMethodCallChain,
            uint defaultMaxInterproceduralLambdaOrLocalFunctionCallChain = DefaultMaxInterproceduralLambdaOrLocalFunctionCallChain)
        {
            var kind = analyzerOptions.GetInterproceduralAnalysisKindOption(rule, defaultInterproceduralAnalysisKind, cancellationToken);

            var maxInterproceduralMethodCallChain = analyzerOptions.GetUnsignedIntegralOptionValue(
                optionName: EditorConfigOptionNames.MaxInterproceduralMethodCallChain,
                rule: rule,
                defaultValue: defaultMaxInterproceduralMethodCallChain,
                cancellationToken: cancellationToken);

            var maxInterproceduralLambdaOrLocalFunctionCallChain = analyzerOptions.GetUnsignedIntegralOptionValue(
                optionName: EditorConfigOptionNames.MaxInterproceduralLambdaOrLocalFunctionCallChain,
                rule: rule,
                defaultValue: defaultMaxInterproceduralLambdaOrLocalFunctionCallChain,
                cancellationToken: cancellationToken);

            return new InterproceduralAnalysisConfiguration(
                kind, maxInterproceduralMethodCallChain, maxInterproceduralLambdaOrLocalFunctionCallChain);
        }

        public static InterproceduralAnalysisConfiguration Create(
            AnalyzerOptions analyzerOptions,
            ImmutableArray<DiagnosticDescriptor> rules,
            InterproceduralAnalysisKind defaultInterproceduralAnalysisKind,
            CancellationToken cancellationToken,
            uint defaultMaxInterproceduralMethodCallChain = DefaultMaxInterproceduralMethodCallChain,
            uint defaultMaxInterproceduralLambdaOrLocalFunctionCallChain = DefaultMaxInterproceduralLambdaOrLocalFunctionCallChain)
        {
            InterproceduralAnalysisKind maxKind = InterproceduralAnalysisKind.None;
            uint maxMethodCallChain = 0;
            uint maxLambdaorLocalFunctionCallChain = 0;
            foreach (var rule in rules)
            {
                var interproceduralAnalysisConfig = Create(analyzerOptions, rule, defaultInterproceduralAnalysisKind,
                    cancellationToken, defaultMaxInterproceduralMethodCallChain, defaultMaxInterproceduralLambdaOrLocalFunctionCallChain);
                maxKind = (InterproceduralAnalysisKind)Math.Max((int)maxKind, (int)interproceduralAnalysisConfig.InterproceduralAnalysisKind);
                maxMethodCallChain = Math.Max(maxMethodCallChain, interproceduralAnalysisConfig.MaxInterproceduralMethodCallChain);
                maxLambdaorLocalFunctionCallChain = Math.Max(maxLambdaorLocalFunctionCallChain, interproceduralAnalysisConfig.MaxInterproceduralLambdaOrLocalFunctionCallChain);
            }

            return new InterproceduralAnalysisConfiguration(maxKind, maxMethodCallChain, maxLambdaorLocalFunctionCallChain);
        }

        public InterproceduralAnalysisKind InterproceduralAnalysisKind { get; }

        public uint MaxInterproceduralMethodCallChain { get; }

        public uint MaxInterproceduralLambdaOrLocalFunctionCallChain { get; }

        public override bool Equals(object obj)
        {
            return obj is InterproceduralAnalysisConfiguration otherParameters &&
                Equals(otherParameters);
        }

        public bool Equals(InterproceduralAnalysisConfiguration other)
        {
            return InterproceduralAnalysisKind == other.InterproceduralAnalysisKind &&
                MaxInterproceduralMethodCallChain == other.MaxInterproceduralMethodCallChain &&
                MaxInterproceduralLambdaOrLocalFunctionCallChain == other.MaxInterproceduralLambdaOrLocalFunctionCallChain;
        }

        public override int GetHashCode()
        {
            return RoslynHashCode.Combine(
                InterproceduralAnalysisKind.GetHashCode(),
                MaxInterproceduralMethodCallChain.GetHashCode(),
                MaxInterproceduralLambdaOrLocalFunctionCallChain.GetHashCode());
        }

        public static bool operator ==(InterproceduralAnalysisConfiguration left, InterproceduralAnalysisConfiguration right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InterproceduralAnalysisConfiguration left, InterproceduralAnalysisConfiguration right)
        {
            return !(left == right);
        }
    }
}
