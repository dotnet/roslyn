// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics.Telemetry
{
    /// <summary>
    /// Contains telemetry info for a specific analyzer, such as count of registered actions, the total execution time, etc.
    /// </summary>
    public sealed class AnalyzerTelemetryInfo
    {
        /// <summary>
        /// Count of registered compilation start actions.
        /// </summary>
        public int CompilationStartActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered compilation end actions.
        /// </summary>
        public int CompilationEndActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered compilation actions.
        /// </summary>
        public int CompilationActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered syntax tree actions.
        /// </summary>
        public int SyntaxTreeActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered semantic model actions.
        /// </summary>
        public int SemanticModelActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered symbol actions.
        /// </summary>
        public int SymbolActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered symbol start actions.
        /// </summary>
        public int SymbolStartActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered symbol end actions.
        /// </summary>
        public int SymbolEndActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered syntax node actions.
        /// </summary>
        public int SyntaxNodeActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered code block start actions.
        /// </summary>
        public int CodeBlockStartActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered code block end actions.
        /// </summary>
        public int CodeBlockEndActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered code block actions.
        /// </summary>
        public int CodeBlockActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered operation actions.
        /// </summary>
        public int OperationActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered operation block start actions.
        /// </summary>
        public int OperationBlockStartActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered operation block end actions.
        /// </summary>
        public int OperationBlockEndActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered operation block actions.
        /// </summary>
        public int OperationBlockActionsCount { get; set; } = 0;

        /// <summary>
        /// Count of registered suppression actions.
        /// This is the same as count of <see cref="DiagnosticSuppressor"/>s as each suppressor
        /// has a single suppression action, i.e. <see cref="DiagnosticSuppressor.ReportSuppressions(SuppressionAnalysisContext)"/>.
        /// </summary>
        public int SuppressionActionsCount { get; set; } = 0;

        /// <summary>
        /// Total execution time.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets a value indicating whether the analyzer supports concurrent execution.
        /// </summary>
        public bool Concurrent { get; set; }

        internal AnalyzerTelemetryInfo(AnalyzerActionCounts actionCounts, int suppressionActionCounts, TimeSpan executionTime)
        {
            CompilationStartActionsCount = actionCounts.CompilationStartActionsCount;
            CompilationEndActionsCount = actionCounts.CompilationEndActionsCount;
            CompilationActionsCount = actionCounts.CompilationActionsCount;

            SyntaxTreeActionsCount = actionCounts.SyntaxTreeActionsCount;
            SemanticModelActionsCount = actionCounts.SemanticModelActionsCount;
            SymbolActionsCount = actionCounts.SymbolActionsCount;
            SymbolStartActionsCount = actionCounts.SymbolStartActionsCount;
            SymbolEndActionsCount = actionCounts.SymbolEndActionsCount;
            SyntaxNodeActionsCount = actionCounts.SyntaxNodeActionsCount;

            CodeBlockStartActionsCount = actionCounts.CodeBlockStartActionsCount;
            CodeBlockEndActionsCount = actionCounts.CodeBlockEndActionsCount;
            CodeBlockActionsCount = actionCounts.CodeBlockActionsCount;

            OperationActionsCount = actionCounts.OperationActionsCount;
            OperationBlockStartActionsCount = actionCounts.OperationBlockStartActionsCount;
            OperationBlockEndActionsCount = actionCounts.OperationBlockEndActionsCount;
            OperationBlockActionsCount = actionCounts.OperationBlockActionsCount;

            SuppressionActionsCount = suppressionActionCounts;

            ExecutionTime = executionTime;
            Concurrent = actionCounts.Concurrent;
        }

        /// <summary>
        /// Create telemetry info for a specific analyzer, such as count of registered actions, the total execution time, etc.
        /// </summary>
        public AnalyzerTelemetryInfo()
        {
        }
    }
}
