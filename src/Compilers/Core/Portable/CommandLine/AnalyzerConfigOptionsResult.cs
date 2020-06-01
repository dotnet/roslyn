// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using AnalyzerOptions = System.Collections.Immutable.ImmutableDictionary<string, string>;
using TreeOptions = System.Collections.Immutable.ImmutableDictionary<string, Microsoft.CodeAnalysis.ReportDiagnostic>;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Holds results from <see cref="AnalyzerConfigSet.GetOptionsForSourcePath(string)"/>.
    /// </summary>
    public readonly struct AnalyzerConfigOptionsResult
    {
        /// <summary>
        /// Options that customize diagnostic severity as reported by the compiler.
        /// </summary>
        public TreeOptions TreeOptions { get; }

        /// <summary>
        /// Options that do not have any special compiler behavior and are passed to analyzers as-is.
        /// </summary>
        public AnalyzerOptions AnalyzerOptions { get; }

        /// <summary>
        /// Options in the form of <c>build_property.[name]</c> can be accessed here by using a key of <c>[name]</c>
        /// </summary>
        /// <remarks>
        /// These are typically added automatically by the build system 
        /// </remarks>
        public AnalyzerOptions BuildProperties { get; }

        /// <summary>
        /// Options in the form of <c>build_metadata.[name]</c> can be accessed here by using a key of <c>[name]</c>
        /// </summary>
        /// <remarks>
        /// These are typically added automatically by the build system, often in the form <c>build_metadata.[itemtype].[metadataname]</c>
        /// The parsing does not distinguish the <c>[itemtype]</c> or <c>[metadataname]</c>, and treats everything after <c>build_metadata.</c> as an opaque key
        /// For instance <c>build_metadata.compile.fullpath = value</c> would be accessed here with a key of <c>compile.fullpath</c>
        /// </remarks>
        public AnalyzerOptions BuildMetadata { get; }

        /// <summary>
        /// Any produced diagnostics while applying analyzer configuration.
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        internal AnalyzerConfigOptionsResult(
            TreeOptions treeOptions,
            AnalyzerOptions analyzerOptions,
            AnalyzerOptions buildProperties,
            AnalyzerOptions buildMetadata,
            ImmutableArray<Diagnostic> diagnostics)
        {
            Debug.Assert(treeOptions != null);
            Debug.Assert(analyzerOptions != null);

            TreeOptions = treeOptions;
            AnalyzerOptions = analyzerOptions;
            BuildProperties = buildProperties;
            BuildMetadata = buildMetadata;
            Diagnostics = diagnostics;
        }
    }
}
