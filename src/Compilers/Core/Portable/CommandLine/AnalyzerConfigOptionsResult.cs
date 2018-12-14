// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using AnalyzerOptions = System.Collections.Immutable.ImmutableDictionary<string, string>;
using TreeOptions = System.Collections.Immutable.ImmutableDictionary<string, Microsoft.CodeAnalysis.ReportDiagnostic>;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Holds results from <see cref="AnalyzerConfig.GetAnalyzerConfigOptions{TStringList, TACList}(TStringList, TACList)"/>.
    /// </summary>
    public readonly struct AnalyzerConfigOptionsResult
    {
        /// <summary>
        /// Options that customize diagnostic severity as reported by the compiler. If there
        /// are no options for a given source path, that entry in the array is null.
        /// </summary>
        /// <remarks>
        /// The item index corresponds to the input source path to
        /// <see cref="AnalyzerConfig.GetAnalyzerConfigOptions{TStringList, TACList}(TStringList, TACList)" />
        /// </remarks>
        public ImmutableArray<TreeOptions> TreeOptions { get; }
        /// <summary>
        /// Options that do not have any special compiler behavior and are passed to analyzers as-is.
        /// If there are no options for a given source path, that entry in the array is null.
        /// </summary>
        /// <remarks>
        /// The item index corresponds to the input source path to
        /// <see cref="AnalyzerConfig.GetAnalyzerConfigOptions{TStringList, TACList}(TStringList, TACList)"/>.
        /// </remarks>
        public ImmutableArray<AnalyzerOptions> AnalyzerOptions { get; }

        /// <summary>
        /// Any produced diagnostics while processing AnalyzerConfig files.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        internal AnalyzerConfigOptionsResult(
            ImmutableArray<TreeOptions> treeOptions,
            ImmutableArray<AnalyzerOptions> analyzerOptions,
            ImmutableArray<Diagnostic> diagnostics)
        {
            TreeOptions = treeOptions;
            AnalyzerOptions = analyzerOptions;
            Diagnostics = diagnostics;
        }
    }
}
