// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Context for initializing an analyzer.
    /// </summary>
    public struct AnalysisContext
    {
        private readonly SessionStartAnalysisScope scope;

        public AnalysisContext(SessionStartAnalysisScope scope)
        {
            this.scope = scope;
        }

        public void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
            this.scope.RegisterCompilationStartAction(action);
        }

        public void RegisterCompilationEndAction(Action<CompilationEndAnalysisContext> action)
        {
            this.scope.RegisterCompilationEndAction(action);
        }

        public void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            this.scope.RegisterSemanticModelAction(action);
        }

        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds)
        {
            this.scope.RegisterSymbolAction(action, symbolKinds);
        }

        public void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct
        {
            this.scope.RegisterCodeBlockStartAction(action);
        }

        public void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct
        {
            this.scope.RegisterCodeBlockEndAction<TLanguageKindEnum>(action);
        }

        public void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            this.scope.RegisterSyntaxTreeAction(action);
        }

        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct
        {
            this.scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }
    }

    /// <summary>
    /// Context for a compilation start action.
    /// </summary>
    public struct CompilationStartAnalysisContext
    {
        private readonly CompilationStartAnalysisScope scope;
        private readonly Compilation compilation;
        private readonly AnalyzerOptions options;
        private readonly CancellationToken cancellationToken;

        public Compilation Compilation { get { return this.compilation; } }
        public AnalyzerOptions Options { get { return this.options; } }
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public CompilationStartAnalysisContext(CompilationStartAnalysisScope scope, Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            this.scope = scope;
            this.compilation = compilation;
            this.options = options;
            this.cancellationToken = cancellationToken;
        }

        public void RegisterCompilationEndAction(Action<CompilationEndAnalysisContext> action)
        {
            this.scope.RegisterCompilationEndAction(action);
        }

        public void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            this.scope.RegisterSemanticModelAction(action);
        }

        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds)
        {
            this.scope.RegisterSymbolAction(action, symbolKinds);
        }

        public void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct
        {
            this.scope.RegisterCodeBlockStartAction(action);
        }

        public void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct
        {
            this.scope.RegisterCodeBlockEndAction<TLanguageKindEnum>(action);
        }

        public void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            this.scope.RegisterSyntaxTreeAction(action);
        }

        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct
        {
            this.scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }
    }

    /// <summary>
    /// Context for a compilation end action.
    /// </summary>
    public struct CompilationEndAnalysisContext
    {
        private readonly Compilation compilation;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        public Compilation Compilation { get { return this.compilation; } }
        public AnalyzerOptions Options { get { return this.options; } }
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public CompilationEndAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.compilation = compilation;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a semantic model action.
    /// </summary>
    public struct SemanticModelAnalysisContext
    {
        private readonly SemanticModel semanticModel;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        public SemanticModel SemanticModel { get { return this.semanticModel; } }
        public AnalyzerOptions Options { get { return this.options; } }
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.semanticModel = semanticModel;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a symbol action.
    /// </summary>
    public struct SymbolAnalysisContext
    {
        private readonly ISymbol symbol;
        private readonly Compilation compilation;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        public ISymbol Symbol { get { return this.symbol; } }
        public Compilation Compilation { get { return this.compilation; } }
        public AnalyzerOptions Options { get { return this.options; } }
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.symbol = symbol;
            this.compilation = compilation;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a code block start action.
    /// </summary>
    public struct CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private readonly CodeBlockStartAnalysisScope<TLanguageKindEnum> scope;
        private readonly SyntaxNode codeBlock;
        private readonly ISymbol owningSymbol;
        private readonly SemanticModel semanticModel;
        private readonly AnalyzerOptions options;
        private readonly CancellationToken cancellationToken;

        public SyntaxNode CodeBlock { get { return this.codeBlock; } }
        public ISymbol OwningSymbol { get { return this.owningSymbol; } }
        public SemanticModel SemanticModel { get { return this.semanticModel; } }
        public AnalyzerOptions Options { get { return this.options; } }
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

        public void RegisterCodeBlockEndAction(Action<CodeBlockEndAnalysisContext> action)
        {
            this.scope.RegisterCodeBlockEndAction(action);
        }

        public void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds)
        {
            this.scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }
    }

    /// <summary>
    /// Context for a code block end action.
    /// </summary>
    public struct CodeBlockEndAnalysisContext
    {
        private readonly SyntaxNode codeBlock;
        private readonly ISymbol owningSymbol;
        private readonly SemanticModel semanticModel;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        public SyntaxNode CodeBlock { get { return this.codeBlock; } }
        public ISymbol OwningSymbol { get { return this.owningSymbol; } }
        public SemanticModel SemanticModel { get { return this.semanticModel; } }
        public AnalyzerOptions Options { get { return this.options; } }
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

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a syntax tree action.
    /// </summary>
    public struct SyntaxTreeAnalysisContext
    {
        private readonly SyntaxTree tree;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        public SyntaxTree Tree { get { return this.tree; } }
        public AnalyzerOptions Options { get { return this.options; } }
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.tree = tree;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Context for a syntax node action.
    /// </summary>
    public struct SyntaxNodeAnalysisContext
    {
        private readonly SyntaxNode node;
        private readonly SemanticModel semanticModel;
        private readonly AnalyzerOptions options;
        private readonly Action<Diagnostic> reportDiagnostic;
        private readonly CancellationToken cancellationToken;

        public SyntaxNode Node { get { return this.node; } }
        public SemanticModel SemanticModel { get { return this.semanticModel; } }
        public AnalyzerOptions Options { get { return this.options; } }
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            this.node = node;
            this.semanticModel = semanticModel;
            this.options = options;
            this.reportDiagnostic = reportDiagnostic;
            this.cancellationToken = cancellationToken;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            this.reportDiagnostic(diagnostic);
        }
    }
}
