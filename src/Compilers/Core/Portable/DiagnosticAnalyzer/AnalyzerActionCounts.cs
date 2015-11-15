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
    }
}
