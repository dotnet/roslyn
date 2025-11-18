// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
            ControlFlowGraph cfg,
            Compilation compilation,
            InterproceduralAnalysisKind defaultInterproceduralAnalysisKind,
            uint defaultMaxInterproceduralMethodCallChain = DefaultMaxInterproceduralMethodCallChain,
            uint defaultMaxInterproceduralLambdaOrLocalFunctionCallChain = DefaultMaxInterproceduralLambdaOrLocalFunctionCallChain)
        => Create(analyzerOptions, rule, cfg.OriginalOperation.Syntax.SyntaxTree, compilation, defaultInterproceduralAnalysisKind,
                defaultMaxInterproceduralMethodCallChain, defaultMaxInterproceduralLambdaOrLocalFunctionCallChain);

        private static InterproceduralAnalysisConfiguration Create(
            AnalyzerOptions analyzerOptions,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            InterproceduralAnalysisKind defaultInterproceduralAnalysisKind,
            uint defaultMaxInterproceduralMethodCallChain = DefaultMaxInterproceduralMethodCallChain,
            uint defaultMaxInterproceduralLambdaOrLocalFunctionCallChain = DefaultMaxInterproceduralLambdaOrLocalFunctionCallChain)
        {
            var kind = analyzerOptions.GetInterproceduralAnalysisKindOption(rule, tree, compilation, defaultInterproceduralAnalysisKind);

            var maxInterproceduralMethodCallChain = analyzerOptions.GetUnsignedIntegralOptionValue(
                optionName: EditorConfigOptionNames.MaxInterproceduralMethodCallChain,
                rule: rule,
                tree,
                compilation,
                defaultValue: defaultMaxInterproceduralMethodCallChain);

            var maxInterproceduralLambdaOrLocalFunctionCallChain = analyzerOptions.GetUnsignedIntegralOptionValue(
                optionName: EditorConfigOptionNames.MaxInterproceduralLambdaOrLocalFunctionCallChain,
                rule: rule,
                tree,
                compilation,
                defaultValue: defaultMaxInterproceduralLambdaOrLocalFunctionCallChain);

            return new InterproceduralAnalysisConfiguration(
                kind, maxInterproceduralMethodCallChain, maxInterproceduralLambdaOrLocalFunctionCallChain);
        }

        public static InterproceduralAnalysisConfiguration Create(
            AnalyzerOptions analyzerOptions,
            ImmutableArray<DiagnosticDescriptor> rules,
            ControlFlowGraph cfg,
            Compilation compilation,
            InterproceduralAnalysisKind defaultInterproceduralAnalysisKind,
            uint defaultMaxInterproceduralMethodCallChain = DefaultMaxInterproceduralMethodCallChain,
            uint defaultMaxInterproceduralLambdaOrLocalFunctionCallChain = DefaultMaxInterproceduralLambdaOrLocalFunctionCallChain)
        => Create(analyzerOptions, rules, cfg.OriginalOperation, compilation, defaultInterproceduralAnalysisKind,
                defaultMaxInterproceduralMethodCallChain, defaultMaxInterproceduralLambdaOrLocalFunctionCallChain);

        internal static InterproceduralAnalysisConfiguration Create(
            AnalyzerOptions analyzerOptions,
            ImmutableArray<DiagnosticDescriptor> rules,
            IOperation operation,
            Compilation compilation,
            InterproceduralAnalysisKind defaultInterproceduralAnalysisKind,
            uint defaultMaxInterproceduralMethodCallChain = DefaultMaxInterproceduralMethodCallChain,
            uint defaultMaxInterproceduralLambdaOrLocalFunctionCallChain = DefaultMaxInterproceduralLambdaOrLocalFunctionCallChain)
        {
            var tree = operation.Syntax.SyntaxTree;
            InterproceduralAnalysisKind maxKind = InterproceduralAnalysisKind.None;
            uint maxMethodCallChain = 0;
            uint maxLambdaorLocalFunctionCallChain = 0;
            foreach (var rule in rules)
            {
                var interproceduralAnalysisConfig = Create(analyzerOptions, rule, tree, compilation, defaultInterproceduralAnalysisKind,
                    defaultMaxInterproceduralMethodCallChain, defaultMaxInterproceduralLambdaOrLocalFunctionCallChain);
                maxKind = (InterproceduralAnalysisKind)Math.Max((int)maxKind, (int)interproceduralAnalysisConfig.InterproceduralAnalysisKind);
                maxMethodCallChain = Math.Max(maxMethodCallChain, interproceduralAnalysisConfig.MaxInterproceduralMethodCallChain);
                maxLambdaorLocalFunctionCallChain = Math.Max(maxLambdaorLocalFunctionCallChain, interproceduralAnalysisConfig.MaxInterproceduralLambdaOrLocalFunctionCallChain);
            }

            return new InterproceduralAnalysisConfiguration(maxKind, maxMethodCallChain, maxLambdaorLocalFunctionCallChain);
        }

        public InterproceduralAnalysisKind InterproceduralAnalysisKind { get; }

        public uint MaxInterproceduralMethodCallChain { get; }

        public uint MaxInterproceduralLambdaOrLocalFunctionCallChain { get; }

        public override readonly bool Equals(object obj)
        {
            return obj is InterproceduralAnalysisConfiguration otherParameters &&
                Equals(otherParameters);
        }

        public readonly bool Equals(InterproceduralAnalysisConfiguration other)
        {
            return InterproceduralAnalysisKind == other.InterproceduralAnalysisKind &&
                MaxInterproceduralMethodCallChain == other.MaxInterproceduralMethodCallChain &&
                MaxInterproceduralLambdaOrLocalFunctionCallChain == other.MaxInterproceduralLambdaOrLocalFunctionCallChain;
        }

        public override readonly int GetHashCode()
        {
            return RoslynHashCode.Combine(
                ((int)InterproceduralAnalysisKind).GetHashCode(),
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
