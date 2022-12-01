// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

            // All executable code actions, symbol actions, semantic model actions and compilation end actions
            // are driven by compilation event queue in the AnalyzerDriver.
            HasAnyActionsRequiringCompilationEvents = HasAnyExecutableCodeActions ||
                SymbolActionsCount > 0 ||
                SemanticModelActionsCount > 0 ||
                CompilationEndActionsCount > 0;
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
        /// Returns true if there are any analyzer action callbacks that are driven by compilation events,
        /// such as <see cref="SymbolDeclaredCompilationEvent"/>, <see cref="CompilationUnitCompletedEvent"/>, etc.
        /// Many callbacks into the diagnostics analyzers are driven in the <see cref="AnalyzerDriver"/>
        /// by compilation events added to the <see cref="Compilation.EventQueue"/>. For these callbacks to be executed,
        /// the analyzer driver host needs to force complete the events in the relevant part of the compilation,
        /// i.e. relevant tree(s) or entire compilation. This force complete operation incurs a performance cost,
        /// which can be avoided if the analyzer(s) to be executed, such as syntax-only analyzers, do not register any
        /// actions which are driven by compilation events.
        /// Note that <see cref="CompilationStartedEvent"/> is an exception as it is *always* generated as soon as the
        /// <see cref="Compilation"/> is created. Any action callbacks driven off <see cref="CompilationStartedEvent"/>
        /// do not need any force completion and hence do not need to be accounted by this boolean flag.
        /// </summary>
        /// <remarks>This flag is primarily intended for performance improvements in certain analyzer execution code paths.</remarks>
        public bool HasAnyActionsRequiringCompilationEvents { get; }

        /// <summary>
        /// Gets a value indicating whether the analyzer supports concurrent execution.
        /// </summary>
        public bool Concurrent { get; }
    }
}
