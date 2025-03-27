// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Diagnostics.Telemetry
{
    /// <summary>
    /// Contains telemetry info for a specific analyzer, such as count of registered actions, the total execution time, etc.
    /// </summary>
    [DataContract]
    public sealed class AnalyzerTelemetryInfo
    {
        /// <summary>
        /// Count of registered compilation start actions.
        /// </summary>
        [DataMember(Order = 0)]
        public int CompilationStartActionsCount { get; set; }

        /// <summary>
        /// Count of registered compilation end actions.
        /// </summary>
        [DataMember(Order = 1)]
        public int CompilationEndActionsCount { get; set; }

        /// <summary>
        /// Count of registered compilation actions.
        /// </summary>
        [DataMember(Order = 2)]
        public int CompilationActionsCount { get; set; }

        /// <summary>
        /// Count of registered syntax tree actions.
        /// </summary>
        [DataMember(Order = 3)]
        public int SyntaxTreeActionsCount { get; set; }

        /// <summary>
        /// Count of registered additional file actions.
        /// </summary>
        [DataMember(Order = 4)]
        public int AdditionalFileActionsCount { get; set; }

        /// <summary>
        /// Count of registered semantic model actions.
        /// </summary>
        [DataMember(Order = 5)]
        public int SemanticModelActionsCount { get; set; }

        /// <summary>
        /// Count of registered symbol actions.
        /// </summary>
        [DataMember(Order = 6)]
        public int SymbolActionsCount { get; set; }

        /// <summary>
        /// Count of registered symbol start actions.
        /// </summary>
        [DataMember(Order = 7)]
        public int SymbolStartActionsCount { get; set; }

        /// <summary>
        /// Count of registered symbol end actions.
        /// </summary>
        [DataMember(Order = 8)]
        public int SymbolEndActionsCount { get; set; }

        /// <summary>
        /// Count of registered syntax node actions.
        /// </summary>
        [DataMember(Order = 9)]
        public int SyntaxNodeActionsCount { get; set; }

        /// <summary>
        /// Count of registered code block start actions.
        /// </summary>
        [DataMember(Order = 10)]
        public int CodeBlockStartActionsCount { get; set; }

        /// <summary>
        /// Count of registered code block end actions.
        /// </summary>
        [DataMember(Order = 11)]
        public int CodeBlockEndActionsCount { get; set; }

        /// <summary>
        /// Count of registered code block actions.
        /// </summary>
        [DataMember(Order = 12)]
        public int CodeBlockActionsCount { get; set; }

        /// <summary>
        /// Count of registered operation actions.
        /// </summary>
        [DataMember(Order = 13)]
        public int OperationActionsCount { get; set; }

        /// <summary>
        /// Count of registered operation block start actions.
        /// </summary>
        [DataMember(Order = 14)]
        public int OperationBlockStartActionsCount { get; set; }

        /// <summary>
        /// Count of registered operation block end actions.
        /// </summary>
        [DataMember(Order = 15)]
        public int OperationBlockEndActionsCount { get; set; }

        /// <summary>
        /// Count of registered operation block actions.
        /// </summary>
        [DataMember(Order = 16)]
        public int OperationBlockActionsCount { get; set; }

        /// <summary>
        /// Count of registered suppression actions.
        /// This is the same as count of <see cref="DiagnosticSuppressor"/>s as each suppressor
        /// has a single suppression action, i.e. <see cref="DiagnosticSuppressor.ReportSuppressions(SuppressionAnalysisContext)"/>.
        /// </summary>
        [DataMember(Order = 17)]
        public int SuppressionActionsCount { get; set; }

        /// <summary>
        /// Total execution time.
        /// </summary>
        [DataMember(Order = 18)]
        public TimeSpan ExecutionTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets a value indicating whether the analyzer supports concurrent execution.
        /// </summary>
        [DataMember(Order = 19)]
        public bool Concurrent { get; set; }

        internal AnalyzerTelemetryInfo(AnalyzerActionCounts actionCounts, int suppressionActionCounts, TimeSpan executionTime)
        {
            CompilationStartActionsCount = actionCounts.CompilationStartActionsCount;
            CompilationEndActionsCount = actionCounts.CompilationEndActionsCount;
            CompilationActionsCount = actionCounts.CompilationActionsCount;

            SyntaxTreeActionsCount = actionCounts.SyntaxTreeActionsCount;
            AdditionalFileActionsCount = actionCounts.AdditionalFileActionsCount;
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
