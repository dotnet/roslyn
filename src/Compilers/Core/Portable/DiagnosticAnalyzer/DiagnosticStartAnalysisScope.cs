// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Scope for setting up analyzers for an entire session, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerAnalysisContext : AnalysisContext
    {
        private readonly DiagnosticAnalyzer _analyzer;
        private readonly HostSessionStartAnalysisScope _scope;

        public AnalyzerAnalysisContext(DiagnosticAnalyzer analyzer, HostSessionStartAnalysisScope scope)
        {
            _analyzer = analyzer;
            _scope = scope;
        }

        public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCompilationStartAction(_analyzer, action);
        }

        public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCompilationAction(_analyzer, action);
        }

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSyntaxTreeAction(_analyzer, action);
        }

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSemanticModelAction(_analyzer, action);
        }

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, symbolKinds);
            _scope.RegisterSymbolAction(_analyzer, action, symbolKinds);
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockStartAction<TLanguageKindEnum>(_analyzer, action);
        }

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockAction(_analyzer, action);
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, syntaxKinds);
            _scope.RegisterSyntaxNodeAction(_analyzer, action, syntaxKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a compilation, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerCompilationStartAnalysisContext : CompilationStartAnalysisContext
    {
        private readonly DiagnosticAnalyzer _analyzer;
        private readonly HostCompilationStartAnalysisScope _scope;

        public AnalyzerCompilationStartAnalysisContext(DiagnosticAnalyzer analyzer, HostCompilationStartAnalysisScope scope, Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
            : base(compilation, options, cancellationToken)
        {
            _analyzer = analyzer;
            _scope = scope;
        }

        public override void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action)
        {
            _scope.RegisterCompilationEndAction(_analyzer, action);
        }

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            _scope.RegisterSyntaxTreeAction(_analyzer, action);
        }

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            _scope.RegisterSemanticModelAction(_analyzer, action);
        }

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            _scope.RegisterSymbolAction(_analyzer, action, symbolKinds);
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            _scope.RegisterCodeBlockStartAction<TLanguageKindEnum>(_analyzer, action);
        }

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            _scope.RegisterCodeBlockAction(_analyzer, action);
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            _scope.RegisterSyntaxNodeAction(_analyzer, action, syntaxKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a code block, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum> : CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private readonly DiagnosticAnalyzer _analyzer;
        private readonly HostCodeBlockStartAnalysisScope<TLanguageKindEnum> _scope;

        internal AnalyzerCodeBlockStartAnalysisContext(DiagnosticAnalyzer analyzer,
                                                       HostCodeBlockStartAnalysisScope<TLanguageKindEnum> scope,
                                                       SyntaxNode codeBlock,
                                                       ISymbol owningSymbol,
                                                       SemanticModel semanticModel,
                                                       AnalyzerOptions options,
                                                       CancellationToken cancellationToken)
            : base(codeBlock, owningSymbol, semanticModel, options, cancellationToken)
        {
            _analyzer = analyzer;
            _scope = scope;
        }

        public override void RegisterCodeBlockEndAction(Action<CodeBlockAnalysisContext> action)
        {
            _scope.RegisterCodeBlockEndAction(_analyzer, action);
        }

        public override void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            _scope.RegisterSyntaxNodeAction(_analyzer, action, syntaxKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for an entire session, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostSessionStartAnalysisScope : HostAnalysisScope
    {
        private ImmutableArray<CompilationStartAnalyzerAction> _compilationStartActions = ImmutableArray<CompilationStartAnalyzerAction>.Empty;

        public ImmutableArray<CompilationStartAnalyzerAction> CompilationStartActions
        {
            get { return _compilationStartActions; }
        }

        public void RegisterCompilationStartAction(DiagnosticAnalyzer analyzer, Action<CompilationStartAnalysisContext> action)
        {
            CompilationStartAnalyzerAction analyzerAction = new CompilationStartAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCompilationStartAction(analyzerAction);
            _compilationStartActions = _compilationStartActions.Add(analyzerAction);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a compilation, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostCompilationStartAnalysisScope : HostAnalysisScope
    {
        private readonly HostSessionStartAnalysisScope _sessionScope;

        public HostCompilationStartAnalysisScope(HostSessionStartAnalysisScope sessionScope)
        {
            _sessionScope = sessionScope;
        }

        public override ImmutableArray<CompilationAnalyzerAction> CompilationEndActions
        {
            get { return base.CompilationEndActions.AddRange(_sessionScope.CompilationEndActions); }
        }

        public override ImmutableArray<SemanticModelAnalyzerAction> SemanticModelActions
        {
            get { return base.SemanticModelActions.AddRange(_sessionScope.SemanticModelActions); }
        }

        public override ImmutableArray<SyntaxTreeAnalyzerAction> SyntaxTreeActions
        {
            get { return base.SyntaxTreeActions.AddRange(_sessionScope.SyntaxTreeActions); }
        }

        public override ImmutableArray<SymbolAnalyzerAction> SymbolActions
        {
            get { return base.SymbolActions.AddRange(_sessionScope.SymbolActions); }
        }

        public override ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions
        {
            get { return base.CodeBlockEndActions.AddRange(_sessionScope.CodeBlockEndActions); }
        }

        public override ImmutableArray<CodeBlockAnalyzerAction> CodeBlockActions
        {
            get { return base.CodeBlockActions.AddRange(_sessionScope.CodeBlockActions); }
        }

        public override bool HasCodeBlockEndActions
        {
            get { return base.HasCodeBlockEndActions || _sessionScope.HasCodeBlockEndActions; }
        }

        public override bool HasCodeBlockActions
        {
            get { return base.HasCodeBlockActions || _sessionScope.HasCodeBlockActions; }
        }

        public override bool HasCodeBlockStartActions<TLanguageKindEnum>()
        {
            return
                base.HasCodeBlockStartActions<TLanguageKindEnum>() ||
                _sessionScope.HasCodeBlockStartActions<TLanguageKindEnum>();
        }

        public override ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> GetCodeBlockStartActions<TLanguageKindEnum>()
        {
            return base.GetCodeBlockStartActions<TLanguageKindEnum>().AddRange(_sessionScope.GetCodeBlockStartActions<TLanguageKindEnum>());
        }

        public override ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> GetSyntaxNodeActions<TLanguageKindEnum>()
        {
            return base.GetSyntaxNodeActions<TLanguageKindEnum>().AddRange(_sessionScope.GetSyntaxNodeActions<TLanguageKindEnum>());
        }

        public override AnalyzerActions GetAnalyzerActions(DiagnosticAnalyzer analyzer)
        {
            AnalyzerActions compilationActions = base.GetAnalyzerActions(analyzer);
            AnalyzerActions sessionActions = _sessionScope.GetAnalyzerActions(analyzer);

            if (sessionActions == null)
            {
                return compilationActions;
            }

            if (compilationActions == null)
            {
                return sessionActions;
            }

            return compilationActions.Append(sessionActions);
        }

        public AnalyzerActions GetCompilationOnlyAnalyzerActions(DiagnosticAnalyzer analyzer)
        {
            return base.GetAnalyzerActions(analyzer);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a code block, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostCodeBlockStartAnalysisScope<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private ImmutableArray<CodeBlockAnalyzerAction> _codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
        private ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> _syntaxNodeActions = ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.Empty;

        public ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions
        {
            get { return _codeBlockEndActions; }
        }

        public ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> SyntaxNodeActions
        {
            get { return _syntaxNodeActions; }
        }

        internal HostCodeBlockStartAnalysisScope()
        {
        }

        public void RegisterCodeBlockEndAction(DiagnosticAnalyzer analyzer, Action<CodeBlockAnalysisContext> action)
        {
            _codeBlockEndActions = _codeBlockEndActions.Add(new CodeBlockAnalyzerAction(action, analyzer));
        }

        public void RegisterSyntaxNodeAction(DiagnosticAnalyzer analyzer, Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            _syntaxNodeActions = _syntaxNodeActions.Add(new SyntaxNodeAnalyzerAction<TLanguageKindEnum>(action, syntaxKinds, analyzer));
        }
    }

    internal abstract class HostAnalysisScope
    {
        private ImmutableArray<CompilationAnalyzerAction> _compilationActions = ImmutableArray<CompilationAnalyzerAction>.Empty;
        private ImmutableArray<CompilationAnalyzerAction> _compilationEndActions = ImmutableArray<CompilationAnalyzerAction>.Empty;
        private ImmutableArray<SemanticModelAnalyzerAction> _semanticModelActions = ImmutableArray<SemanticModelAnalyzerAction>.Empty;
        private ImmutableArray<SyntaxTreeAnalyzerAction> _syntaxTreeActions = ImmutableArray<SyntaxTreeAnalyzerAction>.Empty;
        private ImmutableArray<SymbolAnalyzerAction> _symbolActions = ImmutableArray<SymbolAnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> _codeBlockStartActions = ImmutableArray<AnalyzerAction>.Empty;
        private ImmutableArray<CodeBlockAnalyzerAction> _codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
        private ImmutableArray<CodeBlockAnalyzerAction> _codeBlockActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> _syntaxNodeActions = ImmutableArray<AnalyzerAction>.Empty;
        private readonly Dictionary<DiagnosticAnalyzer, AnalyzerActions> _analyzerActions = new Dictionary<DiagnosticAnalyzer, AnalyzerActions>();

        public ImmutableArray<CompilationAnalyzerAction> CompilationActions
        {
            get { return _compilationActions; }
        }

        public virtual ImmutableArray<CompilationAnalyzerAction> CompilationEndActions
        {
            get { return _compilationEndActions; }
        }

        public virtual ImmutableArray<SemanticModelAnalyzerAction> SemanticModelActions
        {
            get { return _semanticModelActions; }
        }

        public virtual ImmutableArray<SyntaxTreeAnalyzerAction> SyntaxTreeActions
        {
            get { return _syntaxTreeActions; }
        }

        public virtual ImmutableArray<SymbolAnalyzerAction> SymbolActions
        {
            get { return _symbolActions; }
        }

        public virtual ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions
        {
            get { return _codeBlockEndActions; }
        }

        public virtual ImmutableArray<CodeBlockAnalyzerAction> CodeBlockActions
        {
            get { return _codeBlockActions; }
        }

        public virtual bool HasCodeBlockEndActions
        {
            get { return _codeBlockEndActions.Any(); }
        }

        public virtual bool HasCodeBlockActions
        {
            get { return _codeBlockActions.Any(); }
        }

        public virtual bool HasCodeBlockStartActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return _codeBlockStartActions.OfType<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>().Any();
        }

        public virtual ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> GetCodeBlockStartActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return _codeBlockStartActions.OfType<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>().AsImmutable();
        }

        public virtual ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> GetSyntaxNodeActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return _syntaxNodeActions.OfType<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>().AsImmutable();
        }

        public virtual AnalyzerActions GetAnalyzerActions(DiagnosticAnalyzer analyzer)
        {
            AnalyzerActions actions;
            _analyzerActions.TryGetValue(analyzer, out actions);
            return actions;
        }

        public void RegisterCompilationAction(DiagnosticAnalyzer analyzer, Action<CompilationAnalysisContext> action)
        {
            CompilationAnalyzerAction analyzerAction = new CompilationAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCompilationAction(analyzerAction);
            _compilationActions = _compilationActions.Add(analyzerAction);
        }

        public void RegisterCompilationEndAction(DiagnosticAnalyzer analyzer, Action<CompilationAnalysisContext> action)
        {
            CompilationAnalyzerAction analyzerAction = new CompilationAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCompilationEndAction(analyzerAction);
            _compilationEndActions = _compilationEndActions.Add(analyzerAction);
        }

        public void RegisterSemanticModelAction(DiagnosticAnalyzer analyzer, Action<SemanticModelAnalysisContext> action)
        {
            SemanticModelAnalyzerAction analyzerAction = new SemanticModelAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddSemanticModelAction(analyzerAction);
            _semanticModelActions = _semanticModelActions.Add(analyzerAction);
        }

        public void RegisterSyntaxTreeAction(DiagnosticAnalyzer analyzer, Action<SyntaxTreeAnalysisContext> action)
        {
            SyntaxTreeAnalyzerAction analyzerAction = new SyntaxTreeAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddSyntaxTreeAction(analyzerAction);
            _syntaxTreeActions = _syntaxTreeActions.Add(analyzerAction);
        }

        public void RegisterSymbolAction(DiagnosticAnalyzer analyzer, Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            SymbolAnalyzerAction analyzerAction = new SymbolAnalyzerAction(action, symbolKinds, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddSymbolAction(analyzerAction);
            _symbolActions = _symbolActions.Add(analyzerAction);
        }

        public void RegisterCodeBlockStartAction<TLanguageKindEnum>(DiagnosticAnalyzer analyzer, Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct
        {
            CodeBlockStartAnalyzerAction<TLanguageKindEnum> analyzerAction = new CodeBlockStartAnalyzerAction<TLanguageKindEnum>(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCodeBlockStartAction(analyzerAction);
            _codeBlockStartActions = _codeBlockStartActions.Add(analyzerAction);
        }

        public void RegisterCodeBlockEndAction(DiagnosticAnalyzer analyzer, Action<CodeBlockAnalysisContext> action)
        {
            CodeBlockAnalyzerAction analyzerAction = new CodeBlockAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCodeBlockEndAction(analyzerAction);
            _codeBlockEndActions = _codeBlockEndActions.Add(analyzerAction);
        }

        public void RegisterCodeBlockAction(DiagnosticAnalyzer analyzer, Action<CodeBlockAnalysisContext> action)
        {
            CodeBlockAnalyzerAction analyzerAction = new CodeBlockAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCodeBlockAction(analyzerAction);
            _codeBlockActions = _codeBlockActions.Add(analyzerAction);
        }

        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(DiagnosticAnalyzer analyzer, Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct
        {
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> analyzerAction = new SyntaxNodeAnalyzerAction<TLanguageKindEnum>(action, syntaxKinds, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddSyntaxNodeAction(analyzerAction);
            _syntaxNodeActions = _syntaxNodeActions.Add(analyzerAction);
        }

        protected AnalyzerActions GetOrCreateAnalyzerActions(DiagnosticAnalyzer analyzer)
        {
            AnalyzerActions actions;
            if (!_analyzerActions.TryGetValue(analyzer, out actions))
            {
                actions = new AnalyzerActions();
                _analyzerActions[analyzer] = actions;
            }

            return actions;
        }
    }

    /// <summary>
    /// Actions registered by a particular analyzer.
    /// </summary>
    // ToDo: AnalyzerActions, and all of the mechanism around it, can be eliminated if the IDE diagnostic analyzer driver
    // moves from an analyzer-centric model to an action-centric model. For example, the driver would need to stop asking
    // if a particular analyzer can analyze syntax trees, and instead ask if any syntax tree actions are present. Also,
    // the driver needs to apply all relevant actions rather then applying the actions of individual analyzers.
    internal sealed class AnalyzerActions
    {
        private ImmutableArray<CompilationStartAnalyzerAction> _compilationStartActions = ImmutableArray<CompilationStartAnalyzerAction>.Empty;
        private ImmutableArray<CompilationAnalyzerAction> _compilationEndActions = ImmutableArray<CompilationAnalyzerAction>.Empty;
        private ImmutableArray<CompilationAnalyzerAction> _compilationActions = ImmutableArray<CompilationAnalyzerAction>.Empty;
        private ImmutableArray<SyntaxTreeAnalyzerAction> _syntaxTreeActions = ImmutableArray<SyntaxTreeAnalyzerAction>.Empty;
        private ImmutableArray<SemanticModelAnalyzerAction> _semanticModelActions = ImmutableArray<SemanticModelAnalyzerAction>.Empty;
        private ImmutableArray<SymbolAnalyzerAction> _symbolActions = ImmutableArray<SymbolAnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> _codeBlockStartActions = ImmutableArray<AnalyzerAction>.Empty;
        private ImmutableArray<CodeBlockAnalyzerAction> _codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
        private ImmutableArray<CodeBlockAnalyzerAction> _codeBlockActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> _syntaxNodeActions = ImmutableArray<AnalyzerAction>.Empty;

        internal static readonly AnalyzerActions Empty = new AnalyzerActions();

        internal AnalyzerActions()
        {
        }

        public int CompilationStartActionsCount { get { return _compilationStartActions.Length; } }
        public int CompilationEndActionsCount { get { return _compilationEndActions.Length; } }
        public int CompilationActionsCount { get { return _compilationActions.Length; } }
        public int SyntaxTreeActionsCount { get { return _syntaxTreeActions.Length; } }
        public int SemanticModelActionsCount { get { return _semanticModelActions.Length; } }
        public int SymbolActionsCount { get { return _symbolActions.Length; } }
        public int SyntaxNodeActionsCount { get { return _syntaxNodeActions.Length; } }
        public int CodeBlockStartActionsCount { get { return _codeBlockStartActions.Length; } }
        public int CodeBlockEndActionsCount { get { return _codeBlockEndActions.Length; } }
        public int CodeBlockActionsCount { get { return _codeBlockActions.Length; } }

        internal ImmutableArray<CompilationStartAnalyzerAction> CompilationStartActions
        {
            get { return _compilationStartActions; }
        }

        internal ImmutableArray<CompilationAnalyzerAction> CompilationEndActions
        {
            get { return _compilationEndActions; }
        }

        internal ImmutableArray<CompilationAnalyzerAction> CompilationActions
        {
            get { return _compilationActions; }
        }

        internal ImmutableArray<SyntaxTreeAnalyzerAction> SyntaxTreeActions
        {
            get { return _syntaxTreeActions; }
        }

        internal ImmutableArray<SemanticModelAnalyzerAction> SemanticModelActions
        {
            get { return _semanticModelActions; }
        }

        internal ImmutableArray<SymbolAnalyzerAction> SymbolActions
        {
            get { return _symbolActions; }
        }

        internal ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions
        {
            get { return _codeBlockEndActions; }
        }

        internal ImmutableArray<CodeBlockAnalyzerAction> CodeBlockActions
        {
            get { return _codeBlockActions; }
        }

        internal ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> GetCodeBlockStartActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return _codeBlockStartActions.OfType<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>().ToImmutableArray();
        }

        internal ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> GetSyntaxNodeActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return _syntaxNodeActions.OfType<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>().ToImmutableArray();
        }

        internal void AddCompilationStartAction(CompilationStartAnalyzerAction action)
        {
            _compilationStartActions = _compilationStartActions.Add(action);
        }

        internal void AddCompilationEndAction(CompilationAnalyzerAction action)
        {
            _compilationEndActions = _compilationEndActions.Add(action);
        }

        internal void AddCompilationAction(CompilationAnalyzerAction action)
        {
            _compilationActions = _compilationActions.Add(action);
        }

        internal void AddSyntaxTreeAction(SyntaxTreeAnalyzerAction action)
        {
            _syntaxTreeActions = _syntaxTreeActions.Add(action);
        }

        internal void AddSemanticModelAction(SemanticModelAnalyzerAction action)
        {
            _semanticModelActions = _semanticModelActions.Add(action);
        }

        internal void AddSymbolAction(SymbolAnalyzerAction action)
        {
            _symbolActions = _symbolActions.Add(action);
        }

        internal void AddCodeBlockStartAction<TLanguageKindEnum>(CodeBlockStartAnalyzerAction<TLanguageKindEnum> action) where TLanguageKindEnum : struct
        {
            _codeBlockStartActions = _codeBlockStartActions.Add(action);
        }

        internal void AddCodeBlockEndAction(CodeBlockAnalyzerAction action)
        {
            _codeBlockEndActions = _codeBlockEndActions.Add(action);
        }

        internal void AddCodeBlockAction(CodeBlockAnalyzerAction action)
        {
            _codeBlockActions = _codeBlockActions.Add(action);
        }

        internal void AddSyntaxNodeAction<TLanguageKindEnum>(SyntaxNodeAnalyzerAction<TLanguageKindEnum> action) where TLanguageKindEnum : struct
        {
            _syntaxNodeActions = _syntaxNodeActions.Add(action);
        }

        /// <summary>
        /// Append analyzer actions from <paramref name="otherActions"/> to actions from this instance.
        /// </summary>
        /// <param name="otherActions">Analyzer actions to append</param>.
        public AnalyzerActions Append(AnalyzerActions otherActions)
        {
            if (otherActions == null)
            {
                throw new ArgumentNullException(nameof(otherActions));
            }

            AnalyzerActions actions = new AnalyzerActions();
            actions._compilationStartActions = _compilationStartActions.AddRange(otherActions._compilationStartActions);
            actions._compilationEndActions = _compilationEndActions.AddRange(otherActions._compilationEndActions);
            actions._compilationActions = _compilationActions.AddRange(otherActions._compilationActions);
            actions._syntaxTreeActions = _syntaxTreeActions.AddRange(otherActions._syntaxTreeActions);
            actions._semanticModelActions = _semanticModelActions.AddRange(otherActions._semanticModelActions);
            actions._symbolActions = _symbolActions.AddRange(otherActions._symbolActions);
            actions._codeBlockStartActions = _codeBlockStartActions.AddRange(otherActions._codeBlockStartActions);
            actions._codeBlockEndActions = _codeBlockEndActions.AddRange(otherActions._codeBlockEndActions);
            actions._codeBlockActions = _codeBlockActions.AddRange(otherActions._codeBlockActions);
            actions._syntaxNodeActions = _syntaxNodeActions.AddRange(otherActions._syntaxNodeActions);

            return actions;
        }
    }
}
