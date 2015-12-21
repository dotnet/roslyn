// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics.Telemetry
{
    /// <summary>
    /// Contains the counts of registered actions for an analyzer.
    /// </summary>
    internal class AnalyzerActionCounts
    {
        internal static AnalyzerActionCounts Empty = new AnalyzerActionCounts(AnalyzerActions.Empty);

        internal static AnalyzerActionCounts Create(AnalyzerActions analyzerActions)
        {
            if (analyzerActions == null)
            {
                return Empty;
            }

            return new AnalyzerActionCounts(analyzerActions);
        }

        private AnalyzerActionCounts(AnalyzerActions analyzerActions)
        {
            CompilationStartActionsCount = analyzerActions.CompilationStartActionsCount;
            CompilationEndActionsCount = analyzerActions.CompilationEndActionsCount;
            CompilationActionsCount = analyzerActions.CompilationActionsCount;
            SyntaxTreeActionsCount = analyzerActions.SyntaxTreeActionsCount;
            SemanticModelActionsCount = analyzerActions.SemanticModelActionsCount;
            SymbolActionsCount = analyzerActions.SymbolActionsCount;
            SyntaxNodeActionsCount = analyzerActions.SyntaxNodeActionsCount;
            CodeBlockStartActionsCount = analyzerActions.CodeBlockStartActionsCount;
            CodeBlockEndActionsCount = analyzerActions.CodeBlockEndActionsCount;
            CodeBlockActionsCount = analyzerActions.CodeBlockActionsCount;
            OperationActionsCount = analyzerActions.OperationActionsCount;
            OperationBlockStartActionsCount = analyzerActions.OperationBlockStartActionsCount;
            OperationBlockEndActionsCount = analyzerActions.OperationBlockEndActionsCount;
            OperationBlockActionsCount = analyzerActions.OperationBlockActionsCount;

            HasAnyExecutableCodeActions = CodeBlockActionsCount > 0 ||
                CodeBlockStartActionsCount > 0 ||
                SyntaxNodeActionsCount > 0 ||
                OperationActionsCount > 0 ||
                OperationBlockActionsCount > 0 ||
                OperationBlockStartActionsCount > 0;
        }

        /// <summary>
        /// Count of registered compilation start actions.
        /// </summary>
        public int CompilationStartActionsCount { get; }

        /// <summary>
        /// Count of registered compilation end actions.
        /// </summary>
        public int CompilationEndActionsCount { get; }

        /// <summary>
        /// Count of registered compilation actions.
        /// </summary>
        public int CompilationActionsCount { get; }

        /// <summary>
        /// Count of registered syntax tree actions.
        /// </summary>
        public int SyntaxTreeActionsCount { get; }

        /// <summary>
        /// Count of registered semantic model actions.
        /// </summary>
        public int SemanticModelActionsCount { get; }

        /// <summary>
        /// Count of registered symbol actions.
        /// </summary>
        public int SymbolActionsCount { get; }

        /// <summary>
        /// Count of registered syntax node actions.
        /// </summary>
        public int SyntaxNodeActionsCount { get; }

        /// <summary>
        /// Count of code block start actions.
        /// </summary>
        public int CodeBlockStartActionsCount { get; }

        /// <summary>
        /// Count of code block end actions.
        /// </summary>
        public int CodeBlockEndActionsCount { get; }

        /// <summary>
        /// Count of code block actions.
        /// </summary>
        public int CodeBlockActionsCount { get; }

        /// <summary>
        /// Count of Operation actions.
        /// </summary>
        public int OperationActionsCount { get; }

        /// <summary>
        /// Count of Operation block start actions.
        /// </summary>
        public int OperationBlockStartActionsCount { get; }

        /// <summary>
        /// Count of Operation block end actions.
        /// </summary>
        public int OperationBlockEndActionsCount { get; }

        /// <summary>
        /// Count of Operation block actions.
        /// </summary>
        public int OperationBlockActionsCount { get; }

        /// <summary>
        /// Returns true if there are any actions that need to run on executable code.
        /// </summary>
        public bool HasAnyExecutableCodeActions { get; }
    }
}
