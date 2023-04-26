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
        /// <summary>
        /// Options passed to <see cref="DiagnosticAnalyzer"/>s.
        /// </summary>
        public AnalyzerOptions? Options { get; }

        /// <summary>
        /// An optional delegate to be invoked when an analyzer throws an exception.
        /// </summary>
        public Action<Exception, DiagnosticAnalyzer, Diagnostic>? OnAnalyzerException { get; }

        /// <summary>
        /// An optional delegate to be invoked when an analyzer throws an exception as an exception filter.
        /// </summary>
        public Func<Exception, bool>? AnalyzerExceptionFilter { get; }

        /// <summary>
        /// Flag indicating whether analysis can be performed concurrently on multiple threads.
        /// </summary>
        public bool ConcurrentAnalysis { get; }

        /// <summary>
        /// Flag indicating whether analyzer execution time should be logged.
        /// </summary>
        public bool LogAnalyzerExecutionTime { get; }

        /// <summary>
        /// Flag indicating whether analyzer diagnostics with <see cref="Diagnostic.IsSuppressed"/> should be reported.
        /// </summary>
        public bool ReportSuppressedDiagnostics { get; }

        /// <summary>
        /// Optional <see cref="AnalyzerConfigSet"/> from which the analyzer <see cref="Options"/> were computed.
        /// </summary>
        public AnalyzerConfigSet? AnalyzerConfigSet { get; }

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
            : this(options, onAnalyzerException, concurrentAnalysis, logAnalyzerExecutionTime, reportSuppressedDiagnostics, analyzerExceptionFilter, analyzerConfigSet: null)
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
        /// <param name="analyzerConfigSet">Optional <see cref="AnalyzerConfigSet"/> from which the <paramref name="options"/> were computed.</param>
        public CompilationWithAnalyzersOptions(
            AnalyzerOptions? options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException,
            bool concurrentAnalysis,
            bool logAnalyzerExecutionTime,
            bool reportSuppressedDiagnostics,
            Func<Exception, bool>? analyzerExceptionFilter,
            AnalyzerConfigSet? analyzerConfigSet)
        {
            Options = options;
            OnAnalyzerException = onAnalyzerException;
            AnalyzerExceptionFilter = analyzerExceptionFilter;
            ConcurrentAnalysis = concurrentAnalysis;
            LogAnalyzerExecutionTime = logAnalyzerExecutionTime;
            ReportSuppressedDiagnostics = reportSuppressedDiagnostics;
            AnalyzerConfigSet = analyzerConfigSet;
        }
    }
}
