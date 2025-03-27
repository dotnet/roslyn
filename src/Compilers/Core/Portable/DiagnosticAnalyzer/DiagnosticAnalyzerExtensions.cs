// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public static class DiagnosticAnalyzerExtensions
    {
        /// <inheritdoc cref="WithAnalyzers(Compilation, ImmutableArray{DiagnosticAnalyzer}, AnalyzerOptions?)"/>
        [Obsolete("Use WithAnalyzers overload without a cancellation token", error: false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions? options, CancellationToken cancellationToken)
        {
            return new CompilationWithAnalyzers(compilation, analyzers, options, cancellationToken);
        }

        /// <summary>
        /// Returns a new compilation with attached diagnostic analyzers.
        /// </summary>
        /// <param name="compilation">Compilation to which analyzers are to be added.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
#pragma warning disable RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads
        public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions? options = null)
#pragma warning restore RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads
        {
            return new CompilationWithAnalyzers(compilation, analyzers, options);
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
