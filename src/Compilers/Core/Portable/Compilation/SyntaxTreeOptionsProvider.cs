// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract class SyntaxTreeOptionsProvider
    {
        public abstract bool? IsGenerated(SyntaxTree tree, CancellationToken cancellationToken);

        /// <summary>
        /// Get diagnostic severity setting or a given diagnostic identifier in a given tree.
        /// </summary>
        public abstract bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity);
    }

    internal sealed class CompilerSyntaxTreeOptionsProvider : SyntaxTreeOptionsProvider
    {
        private readonly struct Options
        {
            public readonly bool? IsGenerated;
            public readonly ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions;

            public Options(AnalyzerConfigOptionsResult? result)
            {
                if (result is AnalyzerConfigOptionsResult r)
                {
                    DiagnosticOptions = r.TreeOptions;
                    IsGenerated = GeneratedCodeUtilities.GetIsGeneratedCodeFromOptions(r.AnalyzerOptions);
                }
                else
                {
                    DiagnosticOptions = SyntaxTree.EmptyDiagnosticOptions;
                    IsGenerated = null;
                }
            }
        }

        private readonly ImmutableDictionary<SyntaxTree, Options> _options;

        public CompilerSyntaxTreeOptionsProvider(
            SyntaxTree?[] trees,
            ImmutableArray<AnalyzerConfigOptionsResult> results)
        {
            var builder = ImmutableDictionary.CreateBuilder<SyntaxTree, Options>();
            for (int i = 0; i < trees.Length; i++)
            {
                if (trees[i] != null)
                {
                    builder.Add(
                        trees[i]!,
                        new Options(results.IsDefault ? null : (AnalyzerConfigOptionsResult?)results[i]));
                }
            }
            _options = builder.ToImmutableDictionary();
        }

        public override bool? IsGenerated(SyntaxTree tree, CancellationToken _)
            => _options.TryGetValue(tree, out var value) ? value.IsGenerated : null;

        public override bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, CancellationToken _, out ReportDiagnostic severity)
        {
            if (_options.TryGetValue(tree, out var value))
            {
                return value.DiagnosticOptions.TryGetValue(diagnosticId, out severity);
            }
            severity = ReportDiagnostic.Default;
            return false;
        }
    }
}
