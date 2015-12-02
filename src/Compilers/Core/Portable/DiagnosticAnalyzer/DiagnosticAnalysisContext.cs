// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Semantics;

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
    public abstract class AnalysisContext
    {
        /// <summary>
        /// Register an action to be executed at compilation start.
        /// A compilation start action can register other actions and/or collect state information to be used in diagnostic analysis,
        /// but cannot itself report any <see cref="Diagnostic"/>s.
        /// </summary>
        /// <param name="action">Action to be executed at compilation start.</param>
        public abstract void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed for a complete compilation.
        /// A compilation action reports <see cref="Diagnostic"/>s about the <see cref="Compilation"/>.
        /// </summary>
        /// <param name="action">Action to be executed at compilation end.</param>
        public abstract void RegisterCompilationAction(Action<CompilationAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a document,
        /// which will operate on the <see cref="SemanticModel"/> of the document. A semantic model action
        /// reports <see cref="Diagnostic"/>s about the model.
        /// </summary>
        /// <param name="action">Action to be executed for a document's <see cref="SemanticModel"/>.</param>
        public abstract void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.>
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds)
        {
            this.RegisterSymbolAction(action, symbolKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.>
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public abstract void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds);

        /// <summary>
        /// Register an action to be executed at the start of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block start action can register other actions and/or collect state information to be used in diagnostic analysis,
        /// but cannot itself report any <see cref="Diagnostic"/>s.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at the start of semantic analysis of a code block.</param>
        public abstract void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct;

        /// <summary> 
        /// Register an action to be executed after semantic analysis of a method body or an expression appearing outside a method body. 
        /// A code block action reports <see cref="Diagnostic"/>s about code blocks. 
        /// </summary> 
        /// <param name="action">Action to be executed for a code block.</param> 
        public abstract void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of parsing of a code document.
        /// A syntax tree action reports <see cref="Diagnostic"/>s about the <see cref="SyntaxTree"/> of a document.
        /// </summary>
        /// <param name="action">Action to be executed at completion of parsing of a document.</param>
        public abstract void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action);

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
            this.RegisterSyntaxNodeAction(action, syntaxKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/> with an appropriate Kind.
        /// A syntax node action can report <see cref="Diagnostic"/>s about <see cref="SyntaxNode"/>s, and can also collect
        /// state information to be used by other syntax node actions or code block end actions.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/>.</param>
        /// <param name="syntaxKinds">Action will be executed only if a <see cref="SyntaxNode"/>'s Kind matches one of the syntax kind values.</param>
        public abstract void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct;

        /// <summary>
        /// Register an action to be executed at the start of semantic analysis of a method body or an expression appearing outside a method body.
        /// An operation block start action can register other actions and/or collect state information to be used in diagnostic analysis,
        /// but cannot itself report any <see cref="Diagnostic"/>s.
        /// </summary>
        /// <param name="action">Action to be executed at the start of semantic analysis of an operation block.</param>
        public abstract void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action);

        /// <summary> 
        /// Register an action to be executed after semantic analysis of a method body or an expression appearing outside a method body. 
        /// An operation block action reports <see cref="Diagnostic"/>s about operation blocks. 
        /// </summary> 
        /// <param name="action">Action to be executed for an operation block.</param> 
        public abstract void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="IOperation"/> with an appropriate Kind.
        /// An operation action can report <see cref="Diagnostic"/>s about <see cref="IOperation"/>s, and can also collect
        /// state information to be used by other operation actions or code block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of an <see cref="IOperation"/>.</param>
        /// <param name="operationKinds">Action will be executed only if an <see cref="IOperation"/>'s Kind matches one of the operation kind values.</param>
        public void RegisterOperationAction(Action<OperationAnalysisContext> action, params OperationKind[] operationKinds)
        {
            this.RegisterOperationAction(action, operationKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="IOperation"/> with an appropriate Kind.
        /// An operation action can report <see cref="Diagnostic"/>s about <see cref="IOperation"/>s, and can also collect
        /// state information to be used by other operation actions or code block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of an <see cref="IOperation"/>.</param>
        /// <param name="operationKinds">Action will be executed only if an <see cref="IOperation"/>'s Kind matches one of the operation kind values.</param>
        public abstract void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds);

        /// <summary>
        /// Register for concurrent execution of analyzer actions registered by this analyzer.
        /// An analyzer that registers for concurrent execution should be more performant then a non-concurrent analyzer.
        /// However, such analyzers must ensure that its actions are implemented in a thread-safe manner.
        /// </summary>
        /// <remarks>
        /// Even when an analyzer registers for concurrent execution, certain related actions are *never* executed concurrently.
        /// For example, end actions registered on any analysis unit (compilation, code block, operation block, etc.) are by definition semantically dependent on analysis from non-end actions registered on the same analysis unit.
        /// Hence, end actions are never executed concurrently with non-end actions operating on the same analysis unit.
        /// </remarks>
        public abstract void RegisterConcurrentExecution();
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
    public abstract class CompilationStartAnalysisContext
    {
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> that is the subject of the analysis.
        /// </summary>
        public Compilation Compilation { get { return _compilation; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        protected CompilationStartAnalysisContext(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _options = options;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Register an action to be executed at compilation end.
        /// A compilation end action reports <see cref="Diagnostic"/>s about the <see cref="CodeAnalysis.Compilation"/>.
        /// </summary>
        /// <param name="action">Action to be executed at compilation end.</param>
        public abstract void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a document,
        /// which will operate on the <see cref="SemanticModel"/> of the document. A semantic model action
        /// reports <see cref="Diagnostic"/>s about the model.
        /// </summary>
        /// <param name="action">Action to be executed for a document's <see cref="SemanticModel"/>.</param>
        public abstract void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.>
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds)
        {
            this.RegisterSymbolAction(action, symbolKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.>
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public abstract void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds);

        /// <summary>
        /// Register an action to be executed at the start of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block start action can register other actions and/or collect state information to be used in diagnostic analysis,
        /// but cannot itself report any <see cref="Diagnostic"/>s.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at the start of semantic analysis of a code block.</param>
        public abstract void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct;

        /// <summary> 
        /// Register an action to be executed at the end of semantic analysis of a method body or an expression appearing outside a method body. 
        /// A code block action reports <see cref="Diagnostic"/>s about code blocks. 
        /// </summary> 
        /// <param name="action">Action to be executed for a code block.</param> 
        public abstract void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at the start of semantic analysis of a method body or an expression appearing outside a method body.
        /// An operation block start action can register other actions and/or collect state information to be used in diagnostic analysis,
        /// but cannot itself report any <see cref="Diagnostic"/>s.
        /// </summary>
        /// <param name="action">Action to be executed at the start of semantic analysis of an operation block.</param>
        public abstract void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action);

        /// <summary> 
        /// Register an action to be executed after semantic analysis of a method body or an expression appearing outside a method body. 
        /// An operation block action reports <see cref="Diagnostic"/>s about operation blocks. 
        /// </summary> 
        /// <param name="action">Action to be executed for an operation block.</param> 
        public abstract void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of parsing of a code document.
        /// A syntax tree action reports <see cref="Diagnostic"/>s about the <see cref="SyntaxTree"/> of a document.
        /// </summary>
        /// <param name="action">Action to be executed at completion of parsing of a document.</param>
        public abstract void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action);

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
            this.RegisterSyntaxNodeAction(action, syntaxKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/> with an appropriate Kind.
        /// A syntax node action can report <see cref="Diagnostic"/>s about <see cref="SyntaxNode"/>s, and can also collect
        /// state information to be used by other syntax node actions or code block end actions.
        /// </summary>
        /// <typeparam name="TLanguageKindEnum">Enum type giving the syntax node kinds of the source language for which the action applies.</typeparam>
        /// <param name="action">Action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/>.</param>
        /// <param name="syntaxKinds">Action will be executed only if a <see cref="SyntaxNode"/>'s Kind matches one of the syntax kind values.</param>
        public abstract void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct;

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="IOperation"/> with an appropriate Kind.
        /// An operation action can report <see cref="Diagnostic"/>s about <see cref="IOperation"/>s, and can also collect
        /// state information to be used by other operation actions or code block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of an <see cref="IOperation"/>.</param>
        /// <param name="operationKinds">Action will be executed only if an <see cref="IOperation"/>'s Kind matches one of the operation kind values.</param>
        public void RegisterOperationAction(Action<OperationAnalysisContext> action, params OperationKind[] operationKinds)
        {
            this.RegisterOperationAction(action, operationKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="IOperation"/> with an appropriate Kind.
        /// An operation action can report <see cref="Diagnostic"/>s about <see cref="IOperation"/>s, and can also collect
        /// state information to be used by other operation actions or code block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of an <see cref="IOperation"/>.</param>
        /// <param name="operationKinds">Action will be executed only if an <see cref="IOperation"/>'s Kind matches one of the operation kind values.</param>
        public abstract void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds);
    }

    /// <summary>
    /// Context for a compilation action or compilation end action.
    /// A compilation action or compilation end action can use a <see cref="CompilationAnalysisContext"/> to report <see cref="Diagnostic"/>s about a <see cref="CodeAnalysis.Compilation"/>.
    /// </summary>
    public struct CompilationAnalysisContext
    {
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> that is the subject of the analysis.
        /// </summary>
        public Compilation Compilation { get { return _compilation; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        public CompilationAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="CodeAnalysis.Compilation"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _isSupportedDiagnostic);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Context for a semantic model action.
    /// A semantic model action operates on the <see cref="CodeAnalysis.SemanticModel"/> of a code document, and can use a <see cref="SemanticModelAnalysisContext"/> to report <see cref="Diagnostic"/>s about the model.
    /// </summary>
    public struct SemanticModelAnalysisContext
    {
        private readonly SemanticModel _semanticModel;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that is the subject of the analysis.
        /// </summary>
        public SemanticModel SemanticModel { get { return _semanticModel; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="CodeAnalysis.SemanticModel"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _isSupportedDiagnostic);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Context for a symbol action.
    /// A symbol action can use a <see cref="SymbolAnalysisContext"/> to report <see cref="Diagnostic"/>s about an <see cref="ISymbol"/>.
    /// </summary>
    public struct SymbolAnalysisContext
    {
        private readonly ISymbol _symbol;
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="ISymbol"/> that is the subject of the analysis.
        /// </summary>
        public ISymbol Symbol { get { return _symbol; } }

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> containing the <see cref="ISymbol"/>.
        /// </summary>
        public Compilation Compilation { get { return _compilation; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            _symbol = symbol;
            _compilation = compilation;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about an <see cref="ISymbol"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _isSupportedDiagnostic);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
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
    public abstract class CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private readonly SyntaxNode _codeBlock;
        private readonly ISymbol _owningSymbol;
        private readonly SemanticModel _semanticModel;
        private readonly AnalyzerOptions _options;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Method body or expression subject to analysis.
        /// </summary>
        public SyntaxNode CodeBlock { get { return _codeBlock; } }

        /// <summary>
        /// <see cref="ISymbol"/> for which the code block provides a definition or value.
        /// </summary>
        public ISymbol OwningSymbol { get { return _owningSymbol; } }

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that can provide semantic information about the <see cref="SyntaxNode"/>s in the code block.
        /// </summary>
        public SemanticModel SemanticModel { get { return _semanticModel; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        protected CodeBlockStartAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            _codeBlock = codeBlock;
            _owningSymbol = owningSymbol;
            _semanticModel = semanticModel;
            _options = options;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Register an action to be executed at the end of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block end action reports <see cref="Diagnostic"/>s about code blocks.
        /// </summary>
        /// <param name="action">Action to be executed at the end of semantic analysis of a code block.</param>
        public abstract void RegisterCodeBlockEndAction(Action<CodeBlockAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/> with an appropriate Kind.
        /// A syntax node action can report <see cref="Diagnostic"/>s about <see cref="SyntaxNode"/>s, and can also collect
        /// state information to be used by other syntax node actions or code block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/>.</param>
        /// <param name="syntaxKinds">Action will be executed only if a <see cref="SyntaxNode"/>'s Kind matches one of the syntax kind values.</param>
        public void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds)
        {
            this.RegisterSyntaxNodeAction(action, syntaxKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/> with an appropriate Kind.
        /// A syntax node action can report <see cref="Diagnostic"/>s about <see cref="SyntaxNode"/>s, and can also collect
        /// state information to be used by other syntax node actions or code block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of a <see cref="SyntaxNode"/>.</param>
        /// <param name="syntaxKinds">Action will be executed only if a <see cref="SyntaxNode"/>'s Kind matches one of the syntax kind values.</param>
        public abstract void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds);
    }

    /// <summary>
    /// Context for a code block action or code block end action.
    /// A code block action or code block end action can use a <see cref="CodeBlockAnalysisContext"/> to report <see cref="Diagnostic"/>s about a code block.
    /// </summary>
    public struct CodeBlockAnalysisContext
    {
        private readonly SyntaxNode _codeBlock;
        private readonly ISymbol _owningSymbol;
        private readonly SemanticModel _semanticModel;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Code block that is the subject of the analysis.
        /// </summary>
        public SyntaxNode CodeBlock { get { return _codeBlock; } }

        /// <summary>
        /// <see cref="ISymbol"/> for which the code block provides a definition or value.
        /// </summary>
        public ISymbol OwningSymbol { get { return _owningSymbol; } }

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that can provide semantic information about the <see cref="SyntaxNode"/>s in the code block.
        /// </summary>
        public SemanticModel SemanticModel { get { return _semanticModel; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        public CodeBlockAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            _codeBlock = codeBlock;
            _owningSymbol = owningSymbol;
            _semanticModel = semanticModel;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a code block.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _isSupportedDiagnostic);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Context for an operation block start action.
    /// An operation block start action can use an <see cref="OperationBlockStartAnalysisContext"/> to register actions to be executed
    /// at any of:
    /// <list type="bullet">
    /// <item>
    /// <description>completion of semantic analysis of a method body or an expression appearing outside a method body, or</description>
    /// </item>
    /// <item>
    /// <description>completion of semantic analysis of an operation.</description>
    /// </item>
    /// </list>
    /// </summary>
    public abstract class OperationBlockStartAnalysisContext
    {
        private readonly ImmutableArray<IOperation> _operationBlocks;
        private readonly ISymbol _owningSymbol;
        private readonly AnalyzerOptions _options;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Method body and/or expressions subject to analysis.
        /// </summary>
        public ImmutableArray<IOperation> OperationBlocks => _operationBlocks;

        /// <summary>
        /// <see cref="ISymbol"/> for which the code block provides a definition or value.
        /// </summary>
        public ISymbol OwningSymbol => _owningSymbol;

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options => _options;

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        protected OperationBlockStartAnalysisContext(ImmutableArray<IOperation> operationBlocks, ISymbol owningSymbol, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            _operationBlocks = operationBlocks;
            _owningSymbol = owningSymbol;
            _options = options;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Register an action to be executed at the end of semantic analysis of a method body or an expression appearing outside a method body.
        /// A code block end action reports <see cref="Diagnostic"/>s about code blocks.
        /// </summary>
        /// <param name="action">Action to be executed at the end of semantic analysis of a code block.</param>
        public abstract void RegisterOperationBlockEndAction(Action<OperationBlockAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an operation with an appropriate Kind.
        /// An operation action can report <see cref="Diagnostic"/>s about <see cref="IOperation"/>s, and can also collect
        /// state information to be used by other operation actions or operation block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of an <see cref="IOperation"/>.</param>
        /// <param name="operationKinds">Action will be executed only if an <see cref="IOperation"/>'s Kind matches one of the operation kind values.</param>
        public void RegisterOperationAction(Action<OperationAnalysisContext> action, params OperationKind[] operationKinds)
        {
            this.RegisterOperationAction(action, operationKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="IOperation"/> with an appropriate Kind.
        /// An operation action can report <see cref="Diagnostic"/>s about <see cref="IOperation"/>s, and can also collect
        /// state information to be used by other operation actions or operation block end actions.
        /// </summary>
        /// <param name="action">Action to be executed at completion of semantic analysis of an <see cref="IOperation"/>.</param>
        /// <param name="operationKinds">Action will be executed only if an <see cref="IOperation"/>'s Kind matches one of the operation kind values.</param>
        public abstract void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds);
    }

    /// <summary>
    /// Context for an operation block action or operation block end action.
    /// An operation block action or operation block end action can use an <see cref="OperationAnalysisContext"/> to report <see cref="Diagnostic"/>s about an operation block.
    /// </summary>
    public struct OperationBlockAnalysisContext
    {
        private readonly ImmutableArray<IOperation> _operationBlocks;
        private readonly ISymbol _owningSymbol;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Code block that is the subject of the analysis.
        /// </summary>
        public ImmutableArray<IOperation> OperationBlocks => _operationBlocks;

        /// <summary>
        /// <see cref="ISymbol"/> for which the code block provides a definition or value.
        /// </summary>
        public ISymbol OwningSymbol => _owningSymbol;
        
        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options => _options;

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        public OperationBlockAnalysisContext(ImmutableArray<IOperation> operationBlocks, ISymbol owningSymbol, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            _operationBlocks = operationBlocks;
            _owningSymbol = owningSymbol;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a code block.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _isSupportedDiagnostic);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Context for a syntax tree action.
    /// A syntax tree action can use a <see cref="SyntaxTreeAnalysisContext"/> to report <see cref="Diagnostic"/>s about a <see cref="SyntaxTree"/> for a code document.
    /// </summary>
    public struct SyntaxTreeAnalysisContext
    {
        private readonly SyntaxTree _tree;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="SyntaxTree"/> that is the subject of the analysis.
        /// </summary>
        public SyntaxTree Tree { get { return _tree; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            _tree = tree;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="SyntaxTree"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _isSupportedDiagnostic);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Context for a syntax node action.
    /// A syntax node action can use a <see cref="SyntaxNodeAnalysisContext"/> to report <see cref="Diagnostic"/>s for a <see cref="SyntaxNode"/>.
    /// </summary>
    public struct SyntaxNodeAnalysisContext
    {
        private readonly SyntaxNode _node;
        private readonly SemanticModel _semanticModel;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="SyntaxNode"/> that is the subject of the analysis.
        /// </summary>
        public SyntaxNode Node { get { return _node; } }

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that can provide semantic information about the <see cref="SyntaxNode"/>.
        /// </summary>
        public SemanticModel SemanticModel { get { return _semanticModel; } }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            _node = node;
            _semanticModel = semanticModel;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="SyntaxNode"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _isSupportedDiagnostic);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Context for an operation action.
    /// An operation action can use an <see cref="OperationAnalysisContext"/> to report <see cref="Diagnostic"/>s for an <see cref="IOperation"/>.
    /// </summary>
    public struct OperationAnalysisContext
    {
        private readonly IOperation _operation;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="IOperation"/> that is the subject of the analysis.
        /// </summary>
        public IOperation Operation { get { return _operation; } }
        
        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get { return _options; } }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        public OperationAnalysisContext(IOperation operation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            _operation = operation;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="SyntaxNode"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _isSupportedDiagnostic);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }
}
