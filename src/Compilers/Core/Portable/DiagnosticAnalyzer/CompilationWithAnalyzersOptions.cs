// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Options to configure analyzer execution within <see cref="CompilationWithAnalyzers"/>.
    /// </summary>
    public sealed class CompilationWithAnalyzersOptions
    {
        private readonly AnalyzerOptions? _options;
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic>? _onAnalyzerException;
        private readonly Func<Exception, bool>? _analyzerExceptionFilter;
        private readonly bool _concurrentAnalysis;
        private readonly bool _logAnalyzerExecutionTime;
        private readonly bool _reportSuppressedDiagnostics;

        /// <summary>
        /// Options passed to <see cref="DiagnosticAnalyzer"/>s.
        /// </summary>
        public AnalyzerOptions? Options => _options;

        /// <summary>
        /// An optional delegate to be invoked when an analyzer throws an exception.
        /// </summary>
        public Action<Exception, DiagnosticAnalyzer, Diagnostic>? OnAnalyzerException => _onAnalyzerException;

        /// <summary>
        /// An optional delegate to be invoked when an analyzer throws an exception as an exception filter.
        /// </summary>
        public Func<Exception, bool>? AnalyzerExceptionFilter => _analyzerExceptionFilter;

        /// <summary>
        /// Flag indicating whether analysis can be performed concurrently on multiple threads.
        /// </summary>
        public bool ConcurrentAnalysis => _concurrentAnalysis;

        /// <summary>
        /// Flag indicating whether analyzer execution time should be logged.
        /// </summary>
        public bool LogAnalyzerExecutionTime => _logAnalyzerExecutionTime;

        /// <summary>
        /// Flag indicating whether analyzer diagnostics with <see cref="Diagnostic.IsSuppressed"/> should be reported.
        /// </summary>
        public bool ReportSuppressedDiagnostics => _reportSuppressedDiagnostics;

        /// <summary>
        /// Callback to allow individual analyzers to have their own <see cref="AnalyzerConfigOptionsProvider"/>
        /// distinct from the shared instance provided in <see cref="Options"/>.  If <see langword="null"/> then <see
        /// cref="Options"/> will be used for all analyzers.
        /// </summary>
        internal readonly Func<DiagnosticAnalyzer, AnalyzerConfigOptionsProvider>? GetAnalyzerConfigOptionsProvider;

        /// <summary>
        /// Creates a new <see cref="CompilationWithAnalyzersOptions"/>.
        /// </summary>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="onAnalyzerException">Action to invoke if an analyzer throws an exception.</param>
        /// <param name="concurrentAnalysis">Flag indicating whether analysis can be performed concurrently on multiple threads.</param>
        /// <param name="logAnalyzerExecutionTime">Flag indicating whether analyzer execution time should be logged.</param>
        public CompilationWithAnalyzersOptions(
            AnalyzerOptions options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException,
            bool concurrentAnalysis,
            bool logAnalyzerExecutionTime)
            : this(options, onAnalyzerException, concurrentAnalysis, logAnalyzerExecutionTime, reportSuppressedDiagnostics: false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CompilationWithAnalyzersOptions"/>.
        /// </summary>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="onAnalyzerException">Action to invoke if an analyzer throws an exception.</param>
        /// <param name="concurrentAnalysis">Flag indicating whether analysis can be performed concurrently on multiple threads.</param>
        /// <param name="logAnalyzerExecutionTime">Flag indicating whether analyzer execution time should be logged.</param>
        /// <param name="reportSuppressedDiagnostics">Flag indicating whether analyzer diagnostics with <see cref="Diagnostic.IsSuppressed"/> should be reported.</param>
        public CompilationWithAnalyzersOptions(
            AnalyzerOptions options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException,
            bool concurrentAnalysis,
            bool logAnalyzerExecutionTime,
            bool reportSuppressedDiagnostics)
            : this(options, onAnalyzerException, concurrentAnalysis, logAnalyzerExecutionTime, reportSuppressedDiagnostics, analyzerExceptionFilter: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CompilationWithAnalyzersOptions"/>.
        /// </summary>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="onAnalyzerException">Action to invoke if an analyzer throws an exception.</param>
        /// <param name="analyzerExceptionFilter">Action to invoke if an analyzer throws an exception as an exception filter.</param>
        /// <param name="concurrentAnalysis">Flag indicating whether analysis can be performed concurrently on multiple threads.</param>
        /// <param name="logAnalyzerExecutionTime">Flag indicating whether analyzer execution time should be logged.</param>
        /// <param name="reportSuppressedDiagnostics">Flag indicating whether analyzer diagnostics with <see cref="Diagnostic.IsSuppressed"/> should be reported.</param>
        public CompilationWithAnalyzersOptions(
            AnalyzerOptions? options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException,
            bool concurrentAnalysis,
            bool logAnalyzerExecutionTime,
            bool reportSuppressedDiagnostics,
            Func<Exception, bool>? analyzerExceptionFilter)
            : this(options, onAnalyzerException, concurrentAnalysis, logAnalyzerExecutionTime, reportSuppressedDiagnostics, analyzerExceptionFilter, getAnalyzerConfigOptionsProvider: null)
        {
        }

        /// <inheritdoc cref="CompilationWithAnalyzersOptions.CompilationWithAnalyzersOptions(AnalyzerOptions?, Action{Exception, DiagnosticAnalyzer, Diagnostic}?, bool, bool, bool, Func{Exception, bool}?)"/>
        /// <param name="getAnalyzerConfigOptionsProvider">Callback to allow individual analyzers to have their own <see
        /// cref="AnalyzerConfigOptionsProvider"/> distinct from the shared instance provided in <paramref
        /// name="options"/>. If <see langword="null"/> then <paramref name="options"/> will be used for all
        /// analyzers.</param>
        public CompilationWithAnalyzersOptions(
           AnalyzerOptions? options,
           Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException,
           bool concurrentAnalysis,
           bool logAnalyzerExecutionTime,
           bool reportSuppressedDiagnostics,
           Func<Exception, bool>? analyzerExceptionFilter,
           Func<DiagnosticAnalyzer, AnalyzerConfigOptionsProvider>? getAnalyzerConfigOptionsProvider)
        {
            _options = options;
            _onAnalyzerException = onAnalyzerException;
            _analyzerExceptionFilter = analyzerExceptionFilter;
            _concurrentAnalysis = concurrentAnalysis;
            _logAnalyzerExecutionTime = logAnalyzerExecutionTime;
            _reportSuppressedDiagnostics = reportSuppressedDiagnostics;
            this.GetAnalyzerConfigOptionsProvider = getAnalyzerConfigOptionsProvider;
        }
    }
}
