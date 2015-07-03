﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return WithAnalyzers(compilation, analyzers, options, null, cancellationToken);
        }

        /// <summary>
        /// Returns a new compilation with attached diagnostic analyzers.
        /// </summary>
        /// <param name="compilation">Compilation to which analyzers are to be added.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="onAnalyzerException">Action to invoke if an analyzer throws an exception.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, CancellationToken cancellationToken)
        {
            return new CompilationWithAnalyzers(compilation, analyzers, options, onAnalyzerException, cancellationToken);
        }
    }
}
