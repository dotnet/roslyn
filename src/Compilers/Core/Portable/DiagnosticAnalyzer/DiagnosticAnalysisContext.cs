// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Text;

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
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds)
        {
            this.RegisterSymbolAction(action, symbolKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public abstract void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds);

        /// <summary>
        /// Register an action to be executed at start of semantic analysis of an <see cref="ISymbol"/> and its members with an appropriate Kind.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        /// <param name="symbolKind">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches the given <see cref="SymbolKind"/>.</param>
        public virtual void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind)
        {
            throw new NotImplementedException();
        }

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
        /// Register an action to be executed for each non-code document.
        /// An additional file action reports <see cref="Diagnostic"/>s about the <see cref="AdditionalText"/> of a document.
        /// </summary>
        /// <param name="action">Action to be executed for each non-code document.</param>
        public virtual void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action)
        {
            throw new NotImplementedException();
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
        public virtual void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <summary> 
        /// Register an action to be executed after semantic analysis of a method body or an expression appearing outside a method body. 
        /// An operation block action reports <see cref="Diagnostic"/>s about operation blocks. 
        /// </summary> 
        /// <param name="action">Action to be executed for an operation block.</param> 
        public virtual void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

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
        public virtual void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enable concurrent execution of analyzer actions registered by this analyzer.
        /// An analyzer that registers for concurrent execution can have better performance than a non-concurrent analyzer.
        /// However, such an analyzer must ensure that its actions can execute correctly in parallel.
        /// </summary>
        /// <remarks>
        /// Even when an analyzer registers for concurrent execution, certain related actions are *never* executed concurrently.
        /// For example, end actions registered on any analysis unit (compilation, code block, operation block, etc.) are by definition semantically dependent on analysis from non-end actions registered on the same analysis unit.
        /// Hence, end actions are never executed concurrently with non-end actions operating on the same analysis unit.
        /// </remarks>
        public virtual void EnableConcurrentExecution()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Configure analysis mode of generated code for this analyzer.
        /// Non-configured analyzers will default to an appropriate default mode for generated code.
        /// It is recommended for the analyzer to invoke this API with the required <see cref="GeneratedCodeAnalysisFlags"/> setting.
        /// </summary>
        public virtual void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Indicates the minimum reported diagnostic severity for this analysis context.
        /// Analyzer diagnostics with severity lesser than this severity are not reported.
        /// </summary>
        public virtual DiagnosticSeverity MinimumReportedSeverity => DiagnosticSeverity.Hidden;

        /// <summary>
        /// Attempts to compute or get the cached value provided by the given <paramref name="valueProvider"/> for the given <paramref name="text"/>.
        /// Note that the pair {<paramref name="valueProvider"/>, <paramref name="text"/>} acts as the key.
        /// Reusing the same <paramref name="valueProvider"/> instance across analyzer actions and/or analyzer instances can improve the overall analyzer performance by avoiding recomputation of the values.
        /// </summary>
        /// <typeparam name="TValue">The type of the value associated with the key.</typeparam>
        /// <param name="text"><see cref="SourceText"/> for which the value is queried.</param>
        /// <param name="valueProvider">Provider that computes the underlying value.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool TryGetValue<TValue>(SourceText text, SourceTextValueProvider<TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValue(text, valueProvider.CoreValueProvider, out value);
        }

        /// <summary>
        /// Attempts to compute or get the cached value provided by the given <paramref name="valueProvider"/> for the given <paramref name="text"/>.
        /// Note that the pair {<paramref name="valueProvider"/>, <paramref name="text"/>} acts as the key.
        /// Reusing the same <paramref name="valueProvider"/> instance across analyzer actions and/or analyzer instances can improve the overall analyzer performance by avoiding recomputation of the values.
        /// </summary>
        /// <typeparam name="TValue">The type of the value associated with the key.</typeparam>
        /// <param name="text"><see cref="AdditionalText"/> for which the value is queried.</param>
        /// <param name="valueProvider">Provider that computes the underlying value.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool TryGetValue<TValue>(AdditionalText text, AdditionalTextValueProvider<TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValue(text, valueProvider.CoreValueProvider, out value);
        }

        private bool TryGetValue<TKey, TValue>(TKey key, AnalysisValueProvider<TKey, TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
            where TKey : class
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(key, valueProvider);
            return valueProvider.TryGetValue(key, out value);
        }
    }

    /// <summary>
    /// Flags to configure mode of generated code analysis.
    /// </summary>
    [Flags]
    public enum GeneratedCodeAnalysisFlags
    {
        /// <summary>
        /// Disable analyzer action callbacks and diagnostic reporting for generated code.
        /// Analyzer driver will not make callbacks into the analyzer for entities (source files, symbols, etc.) that it classifies as generated code.
        /// Additionally, any diagnostic reported by the analyzer with location in generated code will not be reported.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Enable analyzer action callbacks for generated code.
        /// Analyzer driver will make callbacks into the analyzer for all entities (source files, symbols, etc.) in the compilation, including generated code.
        /// </summary>
        Analyze = 0x01,

        /// <summary>
        /// Enable reporting diagnostics on generated code.
        /// Analyzer driver will not suppress any analyzer diagnostic based on whether or not it's location is in generated code.
        /// </summary>
        ReportDiagnostics = 0x02,
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
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, params SymbolKind[] symbolKinds)
        {
            this.RegisterSymbolAction(action, symbolKinds.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Register an action to be executed at completion of semantic analysis of an <see cref="ISymbol"/> with an appropriate Kind.
        /// A symbol action reports <see cref="Diagnostic"/>s about <see cref="ISymbol"/>s.
        /// </summary>
        /// <param name="action">Action to be executed for an <see cref="ISymbol"/>.</param>
        /// <param name="symbolKinds">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches one of the <see cref="SymbolKind"/> values.</param>
        public abstract void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds);

        /// <summary>
        /// Register an action to be executed at start of semantic analysis of an <see cref="ISymbol"/> and its members with an appropriate Kind.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        /// <param name="symbolKind">Action will be executed only if an <see cref="ISymbol"/>'s Kind matches the given <see cref="SymbolKind"/>.</param>
        public virtual void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind)
        {
            throw new NotImplementedException();
        }

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
        public virtual void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <summary> 
        /// Register an action to be executed after semantic analysis of a method body or an expression appearing outside a method body. 
        /// An operation block action reports <see cref="Diagnostic"/>s about operation blocks. 
        /// </summary> 
        /// <param name="action">Action to be executed for an operation block.</param> 
        public virtual void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register an action to be executed at completion of parsing of a code document.
        /// A syntax tree action reports <see cref="Diagnostic"/>s about the <see cref="SyntaxTree"/> of a document.
        /// </summary>
        /// <param name="action">Action to be executed at completion of parsing of a document.</param>
        public abstract void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action);

        /// <summary>
        /// Register an action to be executed for each non-code document.
        /// An additional file action reports <see cref="Diagnostic"/>s about the <see cref="AdditionalText"/> of a document.
        /// </summary>
        /// <param name="action">Action to be executed for each non-code document.</param>
        public virtual void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action)
        {
            throw new NotImplementedException();
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
        public virtual void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to compute or get the cached value provided by the given <paramref name="valueProvider"/> for the given <paramref name="text"/>.
        /// Note that the pair {<paramref name="valueProvider"/>, <paramref name="text"/>} acts as the key.
        /// Reusing the same <paramref name="valueProvider"/> instance across analyzer actions and/or analyzer instances can improve the overall analyzer performance by avoiding recomputation of the values.
        /// </summary>
        /// <typeparam name="TValue">The type of the value associated with the key.</typeparam>
        /// <param name="text"><see cref="SourceText"/> for which the value is queried.</param>
        /// <param name="valueProvider">Provider that computes the underlying value.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool TryGetValue<TValue>(SourceText text, SourceTextValueProvider<TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValue(text, valueProvider.CoreValueProvider, out value);
        }

        /// <summary>
        /// Attempts to compute or get the cached value provided by the given <paramref name="valueProvider"/> for the given <paramref name="text"/>.
        /// Note that the pair {<paramref name="valueProvider"/>, <paramref name="text"/>} acts as the key.
        /// Reusing the same <paramref name="valueProvider"/> instance across analyzer actions and/or analyzer instances can improve the overall analyzer performance by avoiding recomputation of the values.
        /// </summary>
        /// <typeparam name="TValue">The type of the value associated with the key.</typeparam>
        /// <param name="text"><see cref="AdditionalText"/> instance for which the value is queried.</param>
        /// <param name="valueProvider">Provider that computes the underlying value.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool TryGetValue<TValue>(AdditionalText text, AdditionalTextValueProvider<TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValue(text, valueProvider.CoreValueProvider, out value);
        }

        /// <summary>
        /// Attempts to compute or get the cached value provided by the given <paramref name="valueProvider"/> for the given <paramref name="tree"/>.
        /// Note that the pair {<paramref name="valueProvider"/>, <paramref name="tree"/>} acts as the key.
        /// Reusing the same <paramref name="valueProvider"/> instance across analyzer actions and/or analyzer instances can improve the overall analyzer performance by avoiding recomputation of the values.
        /// </summary>
        /// <typeparam name="TValue">The type of the value associated with the key.</typeparam>
        /// <param name="tree"><see cref="SyntaxTree"/> instance for which the value is queried.</param>
        /// <param name="valueProvider">Provider that computes the underlying value.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool TryGetValue<TValue>(SyntaxTree tree, SyntaxTreeValueProvider<TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValue(tree, valueProvider.CoreValueProvider, out value);
        }

        private bool TryGetValue<TKey, TValue>(TKey key, AnalysisValueProvider<TKey, TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
            where TKey : class
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(key, valueProvider);
            return TryGetValueCore(key, valueProvider, out value);
        }

        internal virtual bool TryGetValueCore<TKey, TValue>(TKey key, AnalysisValueProvider<TKey, TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
            where TKey : class
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Context for a compilation action or compilation end action.
    /// A compilation action or compilation end action can use a <see cref="CompilationAnalysisContext"/> to report <see cref="Diagnostic"/>s about a <see cref="CodeAnalysis.Compilation"/>.
    /// </summary>
    public readonly struct CompilationAnalysisContext
    {
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;
        private readonly CompilationAnalysisValueProviderFactory? _compilationAnalysisValueProviderFactoryOpt;
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

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public CompilationAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
            : this(compilation, options, reportDiagnostic, isSupportedDiagnostic: (d, c) => isSupportedDiagnostic(d), null, cancellationToken)
        {
        }

        internal CompilationAnalysisContext(
            Compilation compilation,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            CompilationAnalysisValueProviderFactory? compilationAnalysisValueProviderFactoryOpt,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _compilationAnalysisValueProviderFactoryOpt = compilationAnalysisValueProviderFactoryOpt;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="CodeAnalysis.Compilation"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _compilation, _isSupportedDiagnostic, _cancellationToken);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Attempts to compute or get the cached value provided by the given <paramref name="valueProvider"/> for the given <paramref name="text"/>.
        /// Note that the pair {<paramref name="valueProvider"/>, <paramref name="text"/>} acts as the key.
        /// Reusing the same <paramref name="valueProvider"/> instance across analyzer actions and/or analyzer instances can improve the overall analyzer performance by avoiding recomputation of the values.
        /// </summary>
        /// <typeparam name="TValue">The type of the value associated with the key.</typeparam>
        /// <param name="text"><see cref="SourceText"/> for which the value is queried.</param>
        /// <param name="valueProvider">Provider that computes the underlying value.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool TryGetValue<TValue>(SourceText text, SourceTextValueProvider<TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValue(text, valueProvider.CoreValueProvider, out value);
        }

        /// <summary>
        /// Attempts to compute or get the cached value provided by the given <paramref name="valueProvider"/> for the given <paramref name="text"/>.
        /// Note that the pair {<paramref name="valueProvider"/>, <paramref name="text"/>} acts as the key.
        /// Reusing the same <paramref name="valueProvider"/> instance across analyzer actions and/or analyzer instances can improve the overall analyzer performance by avoiding recomputation of the values.
        /// </summary>
        /// <typeparam name="TValue">The type of the value associated with the key.</typeparam>
        /// <param name="text"><see cref="AdditionalText"/> for which the value is queried.</param>
        /// <param name="valueProvider">Provider that computes the underlying value.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool TryGetValue<TValue>(AdditionalText text, AdditionalTextValueProvider<TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValue(text, valueProvider.CoreValueProvider, out value);
        }

        /// <summary>
        /// Attempts to compute or get the cached value provided by the given <paramref name="valueProvider"/> for the given <paramref name="tree"/>.
        /// Note that the pair {<paramref name="valueProvider"/>, <paramref name="tree"/>} acts as the key.
        /// Reusing the same <paramref name="valueProvider"/> instance across analyzer actions and/or analyzer instances can improve the overall analyzer performance by avoiding recomputation of the values.
        /// </summary>
        /// <typeparam name="TValue">The type of the value associated with the key.</typeparam>
        /// <param name="tree"><see cref="SyntaxTree"/> for which the value is queried.</param>
        /// <param name="valueProvider">Provider that computes the underlying value.</param>
        /// <param name="value">Value associated with the key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool TryGetValue<TValue>(SyntaxTree tree, SyntaxTreeValueProvider<TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValue(tree, valueProvider.CoreValueProvider, out value);
        }

        private bool TryGetValue<TKey, TValue>(TKey key, AnalysisValueProvider<TKey, TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
            where TKey : class
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(key, valueProvider);

            if (_compilationAnalysisValueProviderFactoryOpt != null)
            {
                var compilationAnalysisValueProvider = _compilationAnalysisValueProviderFactoryOpt.GetValueProvider(valueProvider);
                return compilationAnalysisValueProvider.TryGetValue(key, out value);
            }

            return valueProvider.TryGetValue(key, out value);
        }
    }

    /// <summary>
    /// Context for a semantic model action.
    /// A semantic model action operates on the <see cref="CodeAnalysis.SemanticModel"/> of a code document, and can use a <see cref="SemanticModelAnalysisContext"/> to report <see cref="Diagnostic"/>s about the model.
    /// </summary>
    public readonly struct SemanticModelAnalysisContext
    {
        private readonly SemanticModel _semanticModel;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;
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

        /// <summary>
        /// Syntax tree for the <see cref="SemanticModel"/> being analyzed.
        /// </summary>
        public SyntaxTree FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Indicates if the underlying <see cref="SemanticModel.SyntaxTree"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
            : this(semanticModel, options, reportDiagnostic, isSupportedDiagnostic: (d, _) => isSupportedDiagnostic(d), filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        internal SemanticModelAnalysisContext(
            SemanticModel semanticModel,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            FilterTree = semanticModel.SyntaxTree;
            FilterSpan = filterSpan;
            IsGeneratedCode = isGeneratedCode;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="CodeAnalysis.SemanticModel"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _semanticModel.Compilation, _isSupportedDiagnostic, _cancellationToken);
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
    public readonly struct SymbolAnalysisContext
    {
        private readonly ISymbol _symbol;
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;
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
        /// Optional filter tree being analyzed.
        /// <see langword="null"/> if we are analyzing the entire compilation.
        /// </summary>
        public SyntaxTree? FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        /// <remarks>This property is guaranteed to be <see langword="null"/> if <see cref="FilterTree"/> is <see langword="null"/>.</remarks>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        internal Func<Diagnostic, CancellationToken, bool> IsSupportedDiagnostic => _isSupportedDiagnostic;

        /// <summary>
        /// Indicates if the <see cref="Symbol"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
            : this(symbol, compilation, options, reportDiagnostic, isSupportedDiagnostic: (d, _) => isSupportedDiagnostic(d), isGeneratedCode: false, filterTree: null, filterSpan: null, cancellationToken)
        {
        }

        internal SymbolAnalysisContext(
            ISymbol symbol,
            Compilation compilation,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            bool isGeneratedCode,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!filterSpan.HasValue || filterTree != null);

            _symbol = symbol;
            _compilation = compilation;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            IsGeneratedCode = isGeneratedCode;
            FilterTree = filterTree;
            FilterSpan = filterSpan;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about an <see cref="ISymbol"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _compilation, _isSupportedDiagnostic, _cancellationToken);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Context for a symbol start action to analyze a symbol and its members.
    /// A symbol start/end action can use a <see cref="SymbolStartAnalysisContext"/> to report <see cref="Diagnostic"/>s about code within a <see cref="ISymbol"/> and its members.
    /// </summary>
    public abstract class SymbolStartAnalysisContext
    {
        /// <summary>
        /// <see cref="ISymbol"/> that is the subject of the analysis.
        /// </summary>
        public ISymbol Symbol { get; }

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> containing the <see cref="ISymbol"/>.
        /// </summary>
        public Compilation Compilation { get; }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get; }

        /// <summary>
        /// Indicates if the <see cref="Symbol"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        /// <summary>
        /// Optional filter tree being analyzed.
        /// <see langword="null"/> if we are analyzing the entire compilation.
        /// </summary>
        public SyntaxTree? FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        /// <remarks>This property is guaranteed to be <see langword="null"/> if <see cref="FilterTree"/> is <see langword="null"/>.</remarks>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public SymbolStartAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
            : this(symbol, compilation, options, isGeneratedCode: false, filterTree: null, filterSpan: null, cancellationToken)
        {
        }

        internal SymbolStartAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, bool isGeneratedCode, SyntaxTree? filterTree, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            Debug.Assert(!filterSpan.HasValue || filterTree != null);

            Symbol = symbol;
            Compilation = compilation;
            Options = options;
            IsGeneratedCode = isGeneratedCode;
            FilterTree = filterTree;
            FilterSpan = filterSpan;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Register an action to be executed at end of semantic analysis of an <see cref="ISymbol"/> and its members.
        /// A symbol end action reports <see cref="Diagnostic"/>s about the code within a <see cref="Symbol"/> and its members.
        /// </summary>
        /// <param name="action">Action to be executed at compilation end.</param>
        public abstract void RegisterSymbolEndAction(Action<SymbolAnalysisContext> action);

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
        /// Syntax tree corresponding to the code block being analyzed.
        /// </summary>
        public SyntaxTree FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Indicates if the <see cref="CodeBlock"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        protected CodeBlockStartAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
            : this(codeBlock, owningSymbol, semanticModel, options, filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        private protected CodeBlockStartAnalysisContext(
            SyntaxNode codeBlock,
            ISymbol owningSymbol,
            SemanticModel semanticModel,
            AnalyzerOptions options,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            _codeBlock = codeBlock;
            _owningSymbol = owningSymbol;
            _semanticModel = semanticModel;
            _options = options;
            FilterTree = codeBlock.SyntaxTree;
            FilterSpan = filterSpan;
            IsGeneratedCode = isGeneratedCode;
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
    public readonly struct CodeBlockAnalysisContext
    {
        private readonly SyntaxNode _codeBlock;
        private readonly ISymbol _owningSymbol;
        private readonly SemanticModel _semanticModel;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;
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
        /// Syntax tree for the code block being analyzed.
        /// </summary>
        public SyntaxTree FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Indicates if the <see cref="CodeBlock"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get { return _cancellationToken; } }

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public CodeBlockAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
            : this(codeBlock, owningSymbol, semanticModel, options, reportDiagnostic, isSupportedDiagnostic: (d, _) => isSupportedDiagnostic(d), filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        internal CodeBlockAnalysisContext(
            SyntaxNode codeBlock,
            ISymbol owningSymbol,
            SemanticModel semanticModel,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            _codeBlock = codeBlock;
            _owningSymbol = owningSymbol;
            _semanticModel = semanticModel;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            FilterTree = codeBlock.SyntaxTree;
            FilterSpan = filterSpan;
            IsGeneratedCode = isGeneratedCode;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a code block.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _semanticModel.Compilation, _isSupportedDiagnostic, _cancellationToken);
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
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly Func<IOperation, ControlFlowGraph>? _getControlFlowGraph;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// One or more operation blocks that are the subject of the analysis.
        /// This includes all blocks associated with the <see cref="OwningSymbol"/>,
        /// such as method body, field/property/constructor/parameter initializer(s), attributes, etc.
        /// </summary>
        /// <remarks>Note that the operation blocks are not in any specific order.</remarks>
        public ImmutableArray<IOperation> OperationBlocks => _operationBlocks;

        /// <summary>
        /// <see cref="ISymbol"/> for which the <see cref="OperationBlocks"/> provides a definition or value.
        /// </summary>
        public ISymbol OwningSymbol => _owningSymbol;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> containing the <see cref="OperationBlocks"/>.
        /// </summary>
        public Compilation Compilation => _compilation;

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options => _options;

        /// <summary>
        /// Syntax tree for the <see cref="OperationBlocks"/> being analyzed.
        /// </summary>
        public SyntaxTree FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Indicates if the <see cref="OperationBlocks"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        protected OperationBlockStartAnalysisContext(
            ImmutableArray<IOperation> operationBlocks,
            ISymbol owningSymbol,
            Compilation compilation,
            AnalyzerOptions options,
            CancellationToken cancellationToken)
            : this(operationBlocks, owningSymbol, compilation, options, getControlFlowGraph: null,
                  filterTree: operationBlocks[0].Syntax.SyntaxTree, filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        internal OperationBlockStartAnalysisContext(
            ImmutableArray<IOperation> operationBlocks,
            ISymbol owningSymbol,
            Compilation compilation,
            AnalyzerOptions options,
            Func<IOperation, ControlFlowGraph>? getControlFlowGraph,
            SyntaxTree filterTree,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            _operationBlocks = operationBlocks;
            _owningSymbol = owningSymbol;
            _compilation = compilation;
            _options = options;
            _getControlFlowGraph = getControlFlowGraph;
            FilterTree = filterTree;
            FilterSpan = filterSpan;
            IsGeneratedCode = isGeneratedCode;
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

        /// <summary>
        /// Gets a <see cref="ControlFlowGraph"/> for a given <paramref name="operationBlock"/> from this analysis context's <see cref="OperationBlocks"/>.
        /// </summary>
        /// <param name="operationBlock">Operation block.</param>
        public ControlFlowGraph GetControlFlowGraph(IOperation operationBlock)
        {
            if (operationBlock == null)
            {
                throw new ArgumentNullException(nameof(operationBlock));
            }

            if (!OperationBlocks.Contains(operationBlock))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidOperationBlockForAnalysisContext, nameof(operationBlock));
            }

            return DiagnosticAnalysisContextHelpers.GetControlFlowGraph(operationBlock, _getControlFlowGraph, _cancellationToken);
        }
    }

    /// <summary>
    /// Context for an operation block action or operation block end action.
    /// An operation block action or operation block end action can use an <see cref="OperationAnalysisContext"/> to report <see cref="Diagnostic"/>s about an operation block.
    /// </summary>
    public readonly struct OperationBlockAnalysisContext
    {
        private readonly ImmutableArray<IOperation> _operationBlocks;
        private readonly ISymbol _owningSymbol;
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;
        private readonly Func<IOperation, ControlFlowGraph>? _getControlFlowGraph;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// One or more operation blocks that are the subject of the analysis.
        /// This includes all blocks associated with the <see cref="OwningSymbol"/>,
        /// such as method body, field/property/constructor/parameter initializer(s), attributes, etc.
        /// </summary>
        /// <remarks>Note that the operation blocks are not in any specific order.</remarks>
        public ImmutableArray<IOperation> OperationBlocks => _operationBlocks;

        /// <summary>
        /// <see cref="ISymbol"/> for which the <see cref="OperationBlocks"/> provides a definition or value.
        /// </summary>
        public ISymbol OwningSymbol => _owningSymbol;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> containing the <see cref="OperationBlocks"/>.
        /// </summary>
        public Compilation Compilation => _compilation;

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options => _options;

        /// <summary>
        /// Syntax tree for the <see cref="OperationBlocks"/> being analyzed.
        /// </summary>
        public SyntaxTree FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Indicates if the <see cref="OperationBlocks"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public OperationBlockAnalysisContext(
            ImmutableArray<IOperation> operationBlocks,
            ISymbol owningSymbol,
            Compilation compilation,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            CancellationToken cancellationToken)
            : this(operationBlocks, owningSymbol, compilation, options, reportDiagnostic, isSupportedDiagnostic: (d, _) => isSupportedDiagnostic(d), getControlFlowGraph: null,
                  filterTree: operationBlocks[0].Syntax.SyntaxTree, filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        internal OperationBlockAnalysisContext(
            ImmutableArray<IOperation> operationBlocks,
            ISymbol owningSymbol,
            Compilation compilation,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            Func<IOperation, ControlFlowGraph>? getControlFlowGraph,
            SyntaxTree filterTree,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            _operationBlocks = operationBlocks;
            _owningSymbol = owningSymbol;
            _compilation = compilation;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _getControlFlowGraph = getControlFlowGraph;
            FilterTree = filterTree;
            FilterSpan = filterSpan;
            IsGeneratedCode = isGeneratedCode;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a code block.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, Compilation, _isSupportedDiagnostic, _cancellationToken);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Gets a <see cref="ControlFlowGraph"/> for a given <paramref name="operationBlock"/> from this analysis context's <see cref="OperationBlocks"/>.
        /// </summary>
        /// <param name="operationBlock">Operation block.</param>
        public ControlFlowGraph GetControlFlowGraph(IOperation operationBlock)
        {
            if (operationBlock == null)
            {
                throw new ArgumentNullException(nameof(operationBlock));
            }

            if (!OperationBlocks.Contains(operationBlock))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidOperationBlockForAnalysisContext, nameof(operationBlock));
            }

            return DiagnosticAnalysisContextHelpers.GetControlFlowGraph(operationBlock, _getControlFlowGraph, _cancellationToken);
        }
    }

    /// <summary>
    /// Context for a syntax tree action.
    /// A syntax tree action can use a <see cref="SyntaxTreeAnalysisContext"/> to report <see cref="Diagnostic"/>s about a <see cref="SyntaxTree"/> for a code document.
    /// </summary>
    public readonly struct SyntaxTreeAnalysisContext
    {
        private readonly SyntaxTree _tree;
        private readonly Compilation? _compilationOpt;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="SyntaxTree"/> that is the subject of the analysis.
        /// </summary>
        public SyntaxTree Tree => _tree;

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options => _options;

        /// <summary>
        /// Optional filter span within the <see cref="Tree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="Tree"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Indicates if the <see cref="Tree"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        internal Compilation? Compilation => _compilationOpt;

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
            : this(tree, options, reportDiagnostic, isSupportedDiagnostic: (d, _) => isSupportedDiagnostic(d), compilation: null, filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        internal SyntaxTreeAnalysisContext(
            SyntaxTree tree,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            Compilation? compilation,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            _tree = tree;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _compilationOpt = compilation;
            FilterSpan = filterSpan;
            IsGeneratedCode = isGeneratedCode;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="SyntaxTree"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _compilationOpt, _isSupportedDiagnostic, _cancellationToken);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Context for an additional file action.
    /// An additional file action can use an <see cref="AdditionalFileAnalysisContext"/> to report <see cref="Diagnostic"/>s about a non-source <see cref="AdditionalText"/> document.
    /// </summary>
    public readonly struct AdditionalFileAnalysisContext
    {
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;

        /// <summary>
        /// <see cref="AdditionalText"/> that is the subject of the analysis.
        /// </summary>
        public AdditionalText AdditionalFile { get; }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get; }

        /// <summary>
        /// Optional filter span within the <see cref="AdditionalFile"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="AdditionalFile"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Compilation being analyzed.
        /// </summary>
        public Compilation Compilation { get; }

        internal AdditionalFileAnalysisContext(
            AdditionalText additionalFile,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            Compilation compilation,
            TextSpan? filterSpan,
            CancellationToken cancellationToken)
        {
            AdditionalFile = additionalFile;
            Options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            Compilation = compilation;
            FilterSpan = filterSpan;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a diagnostic for the given <see cref="AdditionalFile"/>.
        /// A diagnostic in a non-source document should be created with a non-source <see cref="Location"/>,
        /// which can be created using <see cref="Location.Create(string, TextSpan, LinePositionSpan)"/> API.
        /// </summary>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, Compilation, _isSupportedDiagnostic, CancellationToken);
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
    public readonly struct SyntaxNodeAnalysisContext
    {
        private readonly SyntaxNode _node;
        private readonly ISymbol? _containingSymbol;
        private readonly SemanticModel _semanticModel;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="SyntaxNode"/> that is the subject of the analysis.
        /// </summary>
        public SyntaxNode Node => _node;

        /// <summary>
        /// <see cref="ISymbol"/> for the declaration containing the syntax node.
        /// </summary>
        public ISymbol? ContainingSymbol => _containingSymbol;

        /// <summary>
        /// <see cref="CodeAnalysis.SemanticModel"/> that can provide semantic information about the <see cref="SyntaxNode"/>.
        /// </summary>
        public SemanticModel SemanticModel => _semanticModel;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> containing the <see cref="SyntaxNode"/>.
        /// </summary>
        public Compilation Compilation => _semanticModel?.Compilation ?? throw new InvalidOperationException();

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options => _options;

        /// <summary>
        /// Syntax tree for the <see cref="Node"/> being analyzed.
        /// </summary>
        public SyntaxTree FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Indicates if the <see cref="Node"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public SyntaxNodeAnalysisContext(SyntaxNode node, ISymbol? containingSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
            : this(node, containingSymbol, semanticModel, options, reportDiagnostic, isSupportedDiagnostic: (d, _) => isSupportedDiagnostic(d), filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
           : this(node, null, semanticModel, options, reportDiagnostic, isSupportedDiagnostic: (d, _) => isSupportedDiagnostic(d), filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        internal SyntaxNodeAnalysisContext(
            SyntaxNode node,
            ISymbol? containingSymbol,
            SemanticModel semanticModel,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            _node = node;
            _containingSymbol = containingSymbol;
            _semanticModel = semanticModel;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            FilterTree = node.SyntaxTree;
            FilterSpan = filterSpan;
            IsGeneratedCode = isGeneratedCode;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="SyntaxNode"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _semanticModel.Compilation, _isSupportedDiagnostic, _cancellationToken);
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
    public readonly struct OperationAnalysisContext
    {
        private readonly IOperation _operation;
        private readonly ISymbol _containingSymbol;
        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _options;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Diagnostic, CancellationToken, bool> _isSupportedDiagnostic;
        private readonly Func<IOperation, ControlFlowGraph>? _getControlFlowGraph;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// <see cref="IOperation"/> that is the subject of the analysis.
        /// </summary>
        public IOperation Operation => _operation;

        /// <summary>
        /// <see cref="ISymbol"/> for the declaration containing the operation.
        /// </summary>
        public ISymbol ContainingSymbol => _containingSymbol;

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> containing the <see cref="IOperation"/>.
        /// </summary>
        public Compilation Compilation => _compilation;

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options => _options;

        /// <summary>
        /// Syntax tree for the <see cref="Operation"/> being analyzed.
        /// </summary>
        public SyntaxTree FilterTree { get; }

        /// <summary>
        /// Optional filter span within the <see cref="FilterTree"/> for which to compute diagnostics.
        /// <see langword="null"/> if we are analyzing the entire <see cref="FilterTree"/>
        /// or the entire compilation.
        /// </summary>
        public TextSpan? FilterSpan { get; }

        /// <summary>
        /// Indicates if the <see cref="Operation"/> is generated code.
        /// </summary>
        public bool IsGeneratedCode { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        [Obsolete("Use CompilationWithAnalyzers instead. See https://github.com/dotnet/roslyn/issues/63440 for more details.")]
        public OperationAnalysisContext(
            IOperation operation,
            ISymbol containingSymbol,
            Compilation compilation,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            CancellationToken cancellationToken)
            : this(operation, containingSymbol, compilation, options, reportDiagnostic, isSupportedDiagnostic: (d, _) => isSupportedDiagnostic(d), getControlFlowGraph: null, filterSpan: null, isGeneratedCode: false, cancellationToken)
        {
        }

        internal OperationAnalysisContext(
            IOperation operation,
            ISymbol containingSymbol,
            Compilation compilation,
            AnalyzerOptions options,
            Action<Diagnostic> reportDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            Func<IOperation, ControlFlowGraph>? getControlFlowGraph,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            _operation = operation;
            _containingSymbol = containingSymbol;
            _compilation = compilation;
            _options = options;
            _reportDiagnostic = reportDiagnostic;
            _isSupportedDiagnostic = isSupportedDiagnostic;
            _getControlFlowGraph = getControlFlowGraph;
            FilterTree = operation.Syntax.SyntaxTree;
            FilterSpan = filterSpan;
            IsGeneratedCode = isGeneratedCode;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Diagnostic"/> about a <see cref="SyntaxNode"/>.
        /// </summary>
        /// <param name="diagnostic"><see cref="Diagnostic"/> to be reported.</param>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(diagnostic, _compilation, _isSupportedDiagnostic, _cancellationToken);
            lock (_reportDiagnostic)
            {
                _reportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Gets a <see cref="ControlFlowGraph"/> for the operation block containing the <see cref="Operation"/>.
        /// </summary>
        public ControlFlowGraph GetControlFlowGraph() => DiagnosticAnalysisContextHelpers.GetControlFlowGraph(Operation, _getControlFlowGraph, _cancellationToken);
    }

    /// <summary>
    /// Context for suppressing analyzer and/or compiler non-error diagnostics reported for the compilation.
    /// </summary>
    public readonly struct SuppressionAnalysisContext
    {
        private readonly Action<Suppression> _addSuppression;
        private readonly Func<SuppressionDescriptor, bool> _isSupportedSuppressionDescriptor;
        private readonly Func<SyntaxTree, SemanticModel> _getSemanticModel;

        /// <summary>
        /// Analyzer and/or compiler non-error diagnostics reported for the compilation.
        /// Each <see cref="DiagnosticSuppressor"/> only receives diagnostics whose IDs were declared suppressible in its <see cref="DiagnosticSuppressor.SupportedSuppressions"/>.
        /// This may be a subset of the full set of reported diagnostics, as an optimization for
        /// supporting incremental and partial analysis scenarios.
        /// A diagnostic is considered suppressible by a DiagnosticSuppressor if *all* of the following conditions are met:
        ///     1. Diagnostic is not already suppressed in source via pragma/suppress message attribute.
        ///     2. Diagnostic's <see cref="Diagnostic.DefaultSeverity"/> is not <see cref="DiagnosticSeverity.Error"/>.
        ///     3. Diagnostic is not tagged with <see cref="WellKnownDiagnosticTags.NotConfigurable"/> custom tag.
        /// </summary>
        public ImmutableArray<Diagnostic> ReportedDiagnostics { get; }

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> for the context.
        /// </summary>
        public Compilation Compilation { get; }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        internal SuppressionAnalysisContext(
            Compilation compilation,
            AnalyzerOptions options,
            ImmutableArray<Diagnostic> reportedDiagnostics,
            Action<Suppression> suppressDiagnostic,
            Func<SuppressionDescriptor, bool> isSupportedSuppressionDescriptor,
            Func<SyntaxTree, SemanticModel> getSemanticModel,
            CancellationToken cancellationToken)
        {
            Compilation = compilation;
            Options = options;
            ReportedDiagnostics = reportedDiagnostics;
            _addSuppression = suppressDiagnostic;
            _isSupportedSuppressionDescriptor = isSupportedSuppressionDescriptor;
            _getSemanticModel = getSemanticModel;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Report a <see cref="Suppression"/> for a reported diagnostic.
        /// </summary>
        public void ReportSuppression(Suppression suppression)
        {
            if (!ReportedDiagnostics.Contains(suppression.SuppressedDiagnostic))
            {
                // Non-reported diagnostic with ID '{0}' cannot be suppressed.
                var message = string.Format(CodeAnalysisResources.NonReportedDiagnosticCannotBeSuppressed, suppression.SuppressedDiagnostic.Id);
                throw new ArgumentException(message);
            }

            if (!_isSupportedSuppressionDescriptor(suppression.Descriptor))
            {
                // Reported suppression with ID '{0}' is not supported by the suppressor.
                var message = string.Format(CodeAnalysisResources.UnsupportedSuppressionReported, suppression.Descriptor.Id);
                throw new ArgumentException(message);
            }

            if (suppression.Descriptor.IsDisabled(Compilation.Options))
            {
                // Suppression has been disabled by the end user through compilation options.
                return;
            }

            _addSuppression(suppression);
        }

        /// <summary>
        /// Gets a <see cref="SemanticModel"/> for the given <see cref="SyntaxTree"/>, which is shared across all analyzers.
        /// </summary>
        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree) => _getSemanticModel(syntaxTree);
    }
}
