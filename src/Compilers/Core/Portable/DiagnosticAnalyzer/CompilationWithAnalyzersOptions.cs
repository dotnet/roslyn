// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Options to configure analyzer execution within <see cref="CompilationWithAnalyzers"/>.
    /// </summary>
    public sealed class CompilationWithAnalyzersOptions
    {
        private readonly AnalyzerOptions _options;
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;
        private readonly bool _concurrentAnalysis;
        private readonly bool _logAnalyzerExecutionTime;
        private readonly bool _reportSuppressedDiagnostics;

        /// <summary>
        /// Options passed to <see cref="DiagnosticAnalyzer"/>s.
        /// </summary>
        public AnalyzerOptions Options => _options;

        /// <summary>
        /// An optional delegate to be invoked when an analyzer throws an exception.
        /// </summary>
        public Action<Exception, DiagnosticAnalyzer, Diagnostic> OnAnalyzerException => _onAnalyzerException;

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
        /// Creates a new <see cref="CompilationWithAnalyzersOptions"/>.
        /// </summary>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="onAnalyzerException">Action to invoke if an analyzer throws an exception.</param>
        /// <param name="concurrentAnalysis">Flag indicating whether analysis can be performed concurrently on multiple threads.</param>
        /// <param name="logAnalyzerExecutionTime">Flag indicating whether analyzer execution time should be logged.</param>
        public CompilationWithAnalyzersOptions(AnalyzerOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, bool concurrentAnalysis, bool logAnalyzerExecutionTime)
            : this (options, onAnalyzerException, concurrentAnalysis, logAnalyzerExecutionTime, reportSuppressedDiagnostics: false)
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
        public CompilationWithAnalyzersOptions(AnalyzerOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, bool concurrentAnalysis, bool logAnalyzerExecutionTime, bool reportSuppressedDiagnostics)
        {
            _options = options;
            _onAnalyzerException = onAnalyzerException;
            _concurrentAnalysis = concurrentAnalysis;
            _logAnalyzerExecutionTime = logAnalyzerExecutionTime;
            _reportSuppressedDiagnostics = reportSuppressedDiagnostics;
        }
    }
}
