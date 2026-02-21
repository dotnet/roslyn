// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract class SyntaxTreeOptionsProvider
    {
        /// <summary>
        /// Get whether the given tree is generated.
        /// </summary>
        public abstract GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken cancellationToken);

        /// <summary>
        /// Get diagnostic severity setting for a given diagnostic identifier in a given tree.
        /// </summary>
        public abstract bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity);

        /// <summary>
        /// Get diagnostic severity set globally for a given diagnostic identifier
        /// </summary>
        public abstract bool TryGetGlobalDiagnosticValue(string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity);
    }

    internal sealed class CompilerSyntaxTreeOptionsProvider : SyntaxTreeOptionsProvider
    {
        private readonly struct Options
        {
            public readonly GeneratedKind IsGenerated;
            public readonly ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions;

            public Options(AnalyzerConfigOptionsResult? result)
            {
                if (result is AnalyzerConfigOptionsResult r)
                {
                    DiagnosticOptions = r.TreeOptions;
                    IsGenerated = GeneratedCodeUtilities.GetGeneratedCodeKindFromOptions(r.AnalyzerOptions);
                }
                else
                {
                    DiagnosticOptions = SyntaxTree.EmptyDiagnosticOptions;
                    IsGenerated = GeneratedKind.Unknown;
                }
            }
        }

        private readonly AnalyzerConfigSet? _analyzerConfigSet;
        private readonly string _projectBaseDirectory;
        private readonly string _projectOutputDirectory;
        private ImmutableDictionary<SyntaxTree, Options> _options;
        private readonly AnalyzerConfigOptionsResult _globalOptions;

        public CompilerSyntaxTreeOptionsProvider(
            SyntaxTree?[] trees,
            AnalyzerConfigSet? analyzerConfigSet,
            string projectBaseDirectory,
            string projectOutputDirectory,
            ImmutableArray<AnalyzerConfigOptionsResult> results,
            AnalyzerConfigOptionsResult globalResults)
        {
            Debug.Assert(results.IsDefault || trees.Length == results.Length);

            var builder = ImmutableDictionary.CreateBuilder<SyntaxTree, Options>();
            for (int i = 0; i < trees.Length; i++)
            {
                if (trees[i] is { } tree)
                {
                    builder.Add(
                        tree,
                        new Options(results.IsDefault ? null : (AnalyzerConfigOptionsResult?)results[i]));
                }
            }
            _options = builder.ToImmutableDictionary();
            _analyzerConfigSet = analyzerConfigSet;
            _projectOutputDirectory = projectBaseDirectory;
            _projectOutputDirectory = projectOutputDirectory;
            _globalOptions = globalResults;
        }

        private Options? GetOptionsForTree(SyntaxTree tree)
        {
            if (_options.TryGetValue(tree, out var options))
            {
                return options;
            }

            if (_analyzerConfigSet is not null && tree.FilePath.StartsWith(_projectOutputDirectory))
            {
                // This is a source-generated file
                var optionsResult = _analyzerConfigSet.GetOptionsForGeneratedPath(_projectBaseDirectory, _projectOutputDirectory, tree.FilePath);
                options = new Options(optionsResult);

                do
                {
                    var oldOptions = _options;
                    if (oldOptions.ContainsKey(tree))
                    {
                        // Another thread has already added options for this tree, use those instead of trying to add again.
                        Debug.Assert(oldOptions[tree].IsGenerated == options.IsGenerated);
                        Debug.Assert(oldOptions[tree].DiagnosticOptions.SetEquals(options.DiagnosticOptions));
                        options = oldOptions[tree];
                        break;
                    }

                    var newOptions = oldOptions.Add(tree, options);
                    if (Interlocked.CompareExchange(ref _options, newOptions, oldOptions) == oldOptions)
                    {
                        // Successfully added options for this tree.
                        break;
                    }
                }
                while (true);

                return options;
            }

            return null;
        }

        public override GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken _)
            => GetOptionsForTree(tree) is { } value ? value.IsGenerated : GeneratedKind.Unknown;

        public override bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, CancellationToken _, out ReportDiagnostic severity)
        {
            if (GetOptionsForTree(tree) is { } value)
            {
                return value.DiagnosticOptions.TryGetValue(diagnosticId, out severity);
            }
            severity = ReportDiagnostic.Default;
            return false;
        }

        public override bool TryGetGlobalDiagnosticValue(string diagnosticId, CancellationToken _, out ReportDiagnostic severity)
        {
            if (_globalOptions.TreeOptions is object)
            {
                return _globalOptions.TreeOptions.TryGetValue(diagnosticId, out severity);
            }
            severity = ReportDiagnostic.Default;
            return false;
        }
    }
}
