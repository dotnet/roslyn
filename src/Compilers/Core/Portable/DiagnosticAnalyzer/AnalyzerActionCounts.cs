// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics.Telemetry
{
    /// <summary>
    /// Contains the counts of registered actions for an analyzer.
    /// </summary>
    internal class AnalyzerActionCounts
    {
        internal static readonly AnalyzerActionCounts Empty = new AnalyzerActionCounts(in AnalyzerActions.Empty);

        internal AnalyzerActionCounts(in AnalyzerActions analyzerActions) :
            this(
                analyzerActions.CompilationStartActionsCount,
                analyzerActions.CompilationEndActionsCount,
                analyzerActions.CompilationActionsCount,
                analyzerActions.SyntaxTreeActionsCount,
                analyzerActions.AdditionalFileActionsCount,
                analyzerActions.SemanticModelActionsCount,
                analyzerActions.SymbolActionsCount,
                analyzerActions.SymbolStartActionsCount,
                analyzerActions.SymbolEndActionsCount,
                analyzerActions.SyntaxNodeActionsCount,
                analyzerActions.CodeBlockStartActionsCount,
                analyzerActions.CodeBlockEndActionsCount,
                analyzerActions.CodeBlockActionsCount,
                analyzerActions.OperationActionsCount,
                analyzerActions.OperationBlockStartActionsCount,
                analyzerActions.OperationBlockEndActionsCount,
                analyzerActions.OperationBlockActionsCount,
                analyzerActions.Concurrent)
        {
        }

        internal AnalyzerActionCounts(
            int compilationStartActionsCount,
            int compilationEndActionsCount,
            int compilationActionsCount,
            int syntaxTreeActionsCount,
            int additionalFileActionsCount,
            int semanticModelActionsCount,
            int symbolActionsCount,
            int symbolStartActionsCount,
            int symbolEndActionsCount,
            int syntaxNodeActionsCount,
            int codeBlockStartActionsCount,
            int codeBlockEndActionsCount,
            int codeBlockActionsCount,
            int operationActionsCount,
            int operationBlockStartActionsCount,
            int operationBlockEndActionsCount,
            int operationBlockActionsCount,
            bool concurrent)
        {
            CompilationStartActionsCount = compilationStartActionsCount;
            CompilationEndActionsCount = compilationEndActionsCount;
            CompilationActionsCount = compilationActionsCount;
            SyntaxTreeActionsCount = syntaxTreeActionsCount;
            AdditionalFileActionsCount = additionalFileActionsCount;
            SemanticModelActionsCount = semanticModelActionsCount;
            SymbolActionsCount = symbolActionsCount;
            SymbolStartActionsCount = symbolStartActionsCount;
            SymbolEndActionsCount = symbolEndActionsCount;
            SyntaxNodeActionsCount = syntaxNodeActionsCount;
            CodeBlockStartActionsCount = codeBlockStartActionsCount;
            CodeBlockEndActionsCount = codeBlockEndActionsCount;
            CodeBlockActionsCount = codeBlockActionsCount;
            OperationActionsCount = operationActionsCount;
            OperationBlockStartActionsCount = operationBlockStartActionsCount;
            OperationBlockEndActionsCount = operationBlockEndActionsCount;
            OperationBlockActionsCount = operationBlockActionsCount;
            Concurrent = concurrent;

            HasAnyExecutableCodeActions = CodeBlockActionsCount > 0 ||
                CodeBlockStartActionsCount > 0 ||
                SyntaxNodeActionsCount > 0 ||
                OperationActionsCount > 0 ||
                OperationBlockActionsCount > 0 ||
                OperationBlockStartActionsCount > 0 ||
                SymbolStartActionsCount > 0;
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
        /// Count of registered additional file actions.
        /// </summary>
        public int AdditionalFileActionsCount { get; }

        /// <summary>
        /// Count of registered semantic model actions.
        /// </summary>
        public int SemanticModelActionsCount { get; }

        /// <summary>
        /// Count of registered symbol actions.
        /// </summary>
        public int SymbolActionsCount { get; }

        /// <summary>
        /// Count of registered symbol start actions.
        /// </summary>
        public int SymbolStartActionsCount { get; }

        /// <summary>
        /// Count of registered symbol end actions.
        /// </summary>
        public int SymbolEndActionsCount { get; }

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

        /// <summary>
        /// Gets a value indicating whether the analyzer supports concurrent execution.
        /// </summary>
        public bool Concurrent { get; }
    }
}
