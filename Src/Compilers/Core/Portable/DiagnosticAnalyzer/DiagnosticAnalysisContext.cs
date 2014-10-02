// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Context for initializing an analyzer.
    /// Analyzer initialization can use an <see cref="AnalysisContext"/> to register actions to be executed at any of:
    /// <list type="bullet">
    /// <item>
    /// <description>compilation start,</description>
    /// </item>
    /// <item>
    /// <description>compilation end,</description>
    /// </item>
    /// <item>
    /// <description>completion of parsing a code document,</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a code document,</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a symbol,</description>
    /// </item>
    /// <item>
    /// <description>start of semantic analysis of a method body or an expression appearing outside a method body,</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a method body or an expression appearing outside a method body, or</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a syntax node.</description>
    /// </item>
    /// </list>
    /// </summary>
    public struct AnalysisContext
    {
        private readonly SessionStartAnalysisScope scope;

        public AnalysisContext(SessionStartAnalysisScope scope)
        {
            this.scope = scope;
        }

        /// <summary>
        /// Register an action to be executed at compilation start.
        /// A compilation start action can register other actions and/or collect state information to be used in diagnostic analysis,
        /// but cannot itself report any <see cref="Diagnostic"/>s.
        /// </summary>
        /// <param name="action">Action to be executed at compilation start.</param>
        public void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
            this.scope.RegisterCompilationStartAction(action);
        }

        /// <summary>
        /// Register an action to be executed at compilation end.
        /// A compilation end action reports <see cref="Diagnostic"/>s about the <see cref="Compilation"/>.
        /// </summary>
        /// <param name="action">Action to be executed at compilation end.</param>
        public void RegisterCompilationEndAction(Action<CompilationEndAnalysisContext> action)
        {
            this.scope.RegisterCompilationEndAction(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a document,
        /// which will operate on the <see cref="SemanticModel"/> of the document. A semantic model action
        /// reports <see cref="Diagnostic"/>s about the model.
        /// </summary>
        /// <param name="action">Action to be executed for a document's <see cref="SemanticModel"/>.</param>
        public void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            this.scope.RegisterSemanticModelAction(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.>
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds)
        {
            this.scope.RegisterSymbolAction(action, symbolKinds);
        }

        /// <summary>
        /// Register an action to be executed at the start of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block start action can register other actions and/or collect state information to be used in diagnostic analysis,
        /// but cannot itself report any <see cref="Diagnostic"/>s.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at the start of semantic analysis of a code block.</param>
        public void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct
        {
            this.scope.RegisterCodeBlockStartAction(action);
        }

        /// <summary>
        /// Register an action to be executed at the end of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block end action reports <see cref="Diagnostic"/>s about code blocks.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at the end of semantic analysis of a code block.</param>
        public void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct
        {
            this.scope.RegisterCodeBlockEndAction<TLanguageKindEnum>(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of parsing of a code document.
        /// A syntax tree action reports <see cref="Diagnostic"/>s about the <see cref="SyntaxTree"/> of a document.
        /// </summary>
        /// <param name="action">Action to be executed at completion of parsing of a document.</param>
        public void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            this.scope.RegisterSyntaxTreeAction(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/> with an appropriate Kind.
        /// A syntax node action can report <see cref="Diagnostic"/>s about <see cref="SyntaxNode"/>s, and can also collect
        /// state information to be used by other syntax node actions or code block end actions.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/>.</param>
        /// <param name="syntaxKinds">Action will be executed only if a <see cref="SyntaxNode"/>'s Kind matches one of the syntax kind values.</param>
        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct
        {
            this.scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }
    }

    /// <summary>
    /// Context for a compilation start action.
    /// A compilation start action can use a <see cref="CompilationStartAnalysisContext"/> to register actions to be executed at any of:
    /// <list type="bullet">
    /// <item>
    /// <description>compilation end,</description>
    /// </item>
    /// <item>
    /// <description>completion of parsing a code document,</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a code document,</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a symbol,</description>
    /// </item>
    /// <item>
    /// <description>start of semantic analysis of a method body or an expression appearing outside a method body,</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a method body or an expression appearing outside a method body, or</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a syntax node.</description>
    /// </item>
    /// </list>
    /// </summary>
    public struct CompilationStartAnalysisContext
    {
        private readonly CompilationStartAnalysisScope scope;
        private readonly Compilation compilation;
        private readonly AnalyzerOptions options;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> that is the subject of the analysis.
        /// </summary>
        public Compilation Compilation { get { return this.compilation; } }
        
        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return this.options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public CompilationStartAnalysisContext(CompilationStartAnalysisScope scope, Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            this.scope = scope;
            this.compilation = compilation;
            this.options = options;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Register an action to be executed at compilation end.
        /// A compilation end action reports <see cref="Diagnostic"/>s about the <see cref="CodeAnalysis.Compilation"/>.
        /// </summary>
        /// <param name="action">Action to be executed at compilation end.</param>
        public void RegisterCompilationEndAction(Action<CompilationEndAnalysisContext> action)
        {
            this.scope.RegisterCompilationEndAction(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a document,
        /// which will operate on the <see cref="SemanticModel"/> of the document. A semantic model action
        /// reports <see cref="Diagnostic"/>s about the model.
        /// </summary>
        /// <param name="action">Action to be executed for a document's <see cref="SemanticModel"/>.</param>
        public void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            this.scope.RegisterSemanticModelAction(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.>
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds)
        {
            this.scope.RegisterSymbolAction(action, symbolKinds);
        }

        /// <summary>
        /// Register an action to be executed at the start of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block start action can register other actions and/or collect state information to be used in diagnostic analysis,
        /// but cannot itself report any <see cref="Diagnostic"/>s.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at the start of semantic analysis of a code block.</param>
        public void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct
        {
            this.scope.RegisterCodeBlockStartAction(action);
        }

        /// <summary>
        /// Register an action to be executed at the end of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block end action reports <see cref="Diagnostic"/>s about code blocks.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at the end of semantic analysis of a code block.</param>
        public void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct
        {
            this.scope.RegisterCodeBlockEndAction<TLanguageKindEnum>(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of parsing of a code document.
        /// A syntax tree action reports <see cref="Diagnostic"/>s about the <see cref="SyntaxTree"/> of a document.
        /// </summary>
        /// <param name="action">Action to be executed at completion of parsing of a document.</param>
        public void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            this.scope.RegisterSyntaxTreeAction(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/> with an appropriate Kind.
        /// A syntax node action can report <see cref="Diagnostic"/>s about <see cref="SyntaxNode"/>s, and can also collect
        /// state information to be used by other syntax node actions or code block end actions.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/>.</param>
        /// <param name="syntaxKinds">Action will be executed only if a <see cref="SyntaxNode"/>'s Kind matches one of the syntax kind values.</param>
        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct
        {
            this.scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }
    }

    /// <summary>
    /// Context for a compilation end action.
    /// A compilation end action can use a <see cref="CompilationEndAnalysisContext"/> to report <see cref="Diagnostic"/>s about a <see cref="CodeAnalysis.Compilation"/>.
    /// </summary>
    public struct CompilationEndAnalysisContext
    {
        private readonly Compilation compilation;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> that is the subject of the analysis.
        /// </summary>
        public Compilation Compilation { get { return this.compilation; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return this.options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public CompilationEndAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.compilation = compilation;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="CodeAnalysis.Compilation"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a semantic model action.
    /// A semantic model action operates on the <see cref="CodeAnalysis.SemanticModel"/> of a code document, and can use a <see cref="SemanticModelAnalysisContext"/> to report <see cref="Diagnostic"/>s about the model.
    /// </summary>
    public struct SemanticModelAnalysisContext
    {
        private readonly SemanticModel semanticModel;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that is the subject of the analysis.
        /// </summary>
        public SemanticModel SemanticModel { get { return this.semanticModel; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return this.options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.semanticModel = semanticModel;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="CodeAnalysis.SemanticModel"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a symbol action.
    /// A symbol action can use a <see cref="SymbolAnalysisContext"/> to report <see cref="Diagnostic"/>s about an <see cref="ISymbol"/>.
    /// </summary>
    public struct SymbolAnalysisContext
    {
        private readonly ISymbol symbol;
        private readonly Compilation compilation;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// <see cref="ISymbol"/> that is the subject of the analysis.
        /// </summary>
        public ISymbol Symbol { get { return this.symbol; } }

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> containing the <see cref="ISymbol"/>.
        /// </summary>
        public Compilation Compilation { get { return this.compilation; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return this.options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.symbol = symbol;
            this.compilation = compilation;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about an <see cref="ISymbol"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a code block start action.
    /// A code block start action can use a <see cref="CodeBlockStartAnalysisContext{TLanguageKindEnum}"/> to register actions to be executed
    /// at any of:
    /// <list type="bullet">
    /// <item>
    /// <description>completion of semantic analysis of a method body or an expression appearing outside a method body, or</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of a syntax node.</description>
    /// </item>
    /// </list>
    /// </summary>
    public struct CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private readonly CodeBlockStartAnalysisScope<TLanguageKindEnum> scope;
        private readonly SyntaxNode codeBlock;
        private readonly ISymbol owningSymbol;
        private readonly SemanticModel semanticModel;
        private readonly AnalyzerOptions options;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Method body or expression subjext to analysis.
        /// </summary>
        public SyntaxNode CodeBlock { get { return this.codeBlock; } }

        /// <summary>
        /// <see cref="ISymbol"/> for which the code block provides a definition or value.
        /// </summary>
        public ISymbol OwningSymbol { get { return this.owningSymbol; } }

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that can provide semantic information about the <see cref="SyntaxNode"/>s in the code block.
        /// </summary>
        public SemanticModel SemanticModel { get { return this.semanticModel; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return this.options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        internal CodeBlockStartAnalysisContext(CodeBlockStartAnalysisScope<TLanguageKindEnum> scope, SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            this.scope = scope;
            this.codeBlock = codeBlock;
            this.owningSymbol = owningSymbol;
            this.semanticModel = semanticModel;
            this.options = options;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Register an action to be executed at the end of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block end action reports <see cref="Diagnostic"/>s about code blocks.
        /// </summary>
        /// <param name="action">Action to be executed at the end of semantic analysis of a code block.</param>
        public void RegisterCodeBlockEndAction(Action<CodeBlockEndAnalysisContext> action)
        {
            this.scope.RegisterCodeBlockEndAction(action);
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/> with an appropriate Kind.
        /// A syntax node action can report <see cref="Diagnostic"/>s about <see cref="SyntaxNode"/>s, and can also collect
        /// state information to be used by other syntax node actions or code block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/>.</param>
        /// <param name="syntaxKinds">Action will be executed only if a <see cref="SyntaxNode"/>'s Kind matches one of the syntax kind values.</param>
        public void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds)
        {
            this.scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }
    }

    /// <summary>
    /// Context for a code block end action.
    /// A code block end action can use a <see cref="CodeBlockEndAnalysisContext"/> to report <see cref="Diagnostic"/>s about a code block.
    /// </summary>
    public struct CodeBlockEndAnalysisContext
    {
        private readonly SyntaxNode codeBlock;
        private readonly ISymbol owningSymbol;
        private readonly SemanticModel semanticModel;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Code block that is the subject of the analysis.
        /// </summary>
        public SyntaxNode CodeBlock { get { return this.codeBlock; } }

        /// <summary>
        /// <see cref="ISymbol"/> for which the code block provides a definition or value.
        /// </summary>
        public ISymbol OwningSymbol { get { return this.owningSymbol; } }

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that can provide semantic information about the <see cref="SyntaxNode"/>s in the code block.
        /// </summary>
        public SemanticModel SemanticModel { get { return this.semanticModel; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return this.options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        internal CodeBlockEndAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.codeBlock = codeBlock;
            this.owningSymbol = owningSymbol;
            this.semanticModel = semanticModel;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a code block.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a syntax tree action.
    /// A syntax tree action can use a <see cref="SyntaxTreeAnalysisContext"/> to report <see cref="Diagnostic"/>s about a <see cref="SyntaxTree"/> for a code document.
    /// </summary>
    public struct SyntaxTreeAnalysisContext
    {
        private readonly SyntaxTree tree;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// <see cref="SyntaxTree"/> that is the subject of the analysis.
        /// </summary>
        public SyntaxTree Tree { get { return this.tree; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return this.options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.tree = tree;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="SyntaxTree"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a syntax node action.
    /// A syntax node action can use a <see cref="SyntaxNodeAnalysisContext"/> to report <see cref="Diagnostic"/>s for a <see cref="SyntaxNode"/>.
    /// </summary>
    public struct SyntaxNodeAnalysisContext
    {
        private readonly SyntaxNode node;
        private readonly SemanticModel semanticModel;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// <see cref="SyntaxNode"/> that is the subject of the analysis.
        /// </summary>
        public SyntaxNode Node { get { return this.node; } }

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that can provide semantic information about the <see cref="SyntaxNode"/>.
        /// </summary>
        public SemanticModel SemanticModel { get { return this.semanticModel; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return this.options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.node = node;
            this.semanticModel = semanticModel;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="SyntaxNode"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }
}
