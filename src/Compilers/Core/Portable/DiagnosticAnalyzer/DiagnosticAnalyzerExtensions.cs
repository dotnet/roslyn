// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public static class DiagnosticAnalyzerExtensions
    {
        /// <summary>
        /// Returns a new compilation with attached diagnostic analyzers.
        /// </summary>
        /// <param name="compilation">Compilation to which analyzers are to be added.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions? options = null, CancellationToken cancellationToken = default)
        {
            return new CompilationWithAnalyzers(compilation, analyzers, options, cancellationToken);
        }

        /// <summary>
        /// Returns a new compilation with attached diagnostic analyzers.
        /// </summary>
        /// <param name="compilation">Compilation to which analyzers are to be added.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="analysisOptions">Options to configure analyzer execution within <see cref="CompilationWithAnalyzers"/>.</param>
        public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions analysisOptions)
        {
            return new CompilationWithAnalyzers(compilation, analyzers, analysisOptions);
        }
    }
}
