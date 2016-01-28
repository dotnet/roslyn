// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics.Telemetry
{
    /// <summary>
    /// Contains telemetry info for a specific analyzer, such as count of registered actions, the total execution time, etc.
    /// </summary>
    public sealed class AnalyzerTelemetryInfo
    {
        private readonly AnalyzerActionCounts _actionCounts;

        /// <summary>
        /// Count of registered compilation start actions.
        /// </summary>
        public int CompilationStartActionsCount => _actionCounts.CompilationStartActionsCount;

        /// <summary>
        /// Count of registered compilation end actions.
        /// </summary>
        public int CompilationEndActionsCount => _actionCounts.CompilationEndActionsCount;

        /// <summary>
        /// Count of registered compilation actions.
        /// </summary>
        public int CompilationActionsCount => _actionCounts.CompilationActionsCount;

        /// <summary>
        /// Count of registered syntax tree actions.
        /// </summary>
        public int SyntaxTreeActionsCount => _actionCounts.SyntaxTreeActionsCount;

        /// <summary>
        /// Count of registered semantic model actions.
        /// </summary>
        public int SemanticModelActionsCount => _actionCounts.SemanticModelActionsCount;

        /// <summary>
        /// Count of registered symbol actions.
        /// </summary>
        public int SymbolActionsCount => _actionCounts.SymbolActionsCount;

        /// <summary>
        /// Count of registered syntax node actions.
        /// </summary>
        public int SyntaxNodeActionsCount => _actionCounts.SyntaxNodeActionsCount;

        /// <summary>
        /// Count of registered code block start actions.
        /// </summary>
        public int CodeBlockStartActionsCount => _actionCounts.CodeBlockStartActionsCount;

        /// <summary>
        /// Count of registered code block end actions.
        /// </summary>
        public int CodeBlockEndActionsCount => _actionCounts.CodeBlockEndActionsCount;
        
        /// <summary>
        /// Count of registered code block actions.
        /// </summary>
        public int CodeBlockActionsCount => _actionCounts.CodeBlockActionsCount;

        /// <summary>
        /// Count of registered operation actions.
        /// </summary>
        public int OperationActionsCount => _actionCounts.OperationActionsCount;

        /// <summary>
        /// Count of registered operation block start actions.
        /// </summary>
        public int OperationBlockStartActionsCount => _actionCounts.OperationBlockStartActionsCount;

        /// <summary>
        /// Count of registered operation block end actions.
        /// </summary>
        public int OperationBlockEndActionsCount => _actionCounts.OperationBlockEndActionsCount;

        /// <summary>
        /// Count of registered operation block actions.
        /// </summary>
        public int OperationBlockActionsCount => _actionCounts.OperationBlockActionsCount;

        /// <summary>
        /// Total execution time.
        /// </summary>
        public TimeSpan ExecutionTime { get; }

        internal AnalyzerTelemetryInfo(AnalyzerActionCounts actionCounts, TimeSpan executionTime)
        {
            _actionCounts = actionCounts;
            ExecutionTime = executionTime;
        }
    }
}
