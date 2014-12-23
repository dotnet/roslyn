// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly DiagnosticAnalyzer analyzer;
        private readonly HostSessionStartAnalysisScope scope;

        public AnalyzerAnalysisContext(DiagnosticAnalyzer analyzer, HostSessionStartAnalysisScope scope)
        {
            this.analyzer = analyzer;
            this.scope = scope;
        }

        public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            this.scope.RegisterCompilationStartAction(this.analyzer, action);
        }

        public override void RegisterCompilationEndAction(Action<CompilationEndAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            this.scope.RegisterCompilationEndAction(this.analyzer, action);
        }

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            this.scope.RegisterSyntaxTreeAction(this.analyzer, action);
        }

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            this.scope.RegisterSemanticModelAction(this.analyzer, action);
        }

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, symbolKinds);
            this.scope.RegisterSymbolAction(this.analyzer, action, symbolKinds);
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            this.scope.RegisterCodeBlockStartAction<TLanguageKindEnum>(this.analyzer, action);
        }

        public override void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            this.scope.RegisterCodeBlockEndAction<TLanguageKindEnum>(this.analyzer, action);
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, syntaxKinds);
            this.scope.RegisterSyntaxNodeAction(this.analyzer, action, syntaxKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a compilation, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerCompilationStartAnalysisContext : CompilationStartAnalysisContext
    {
        private readonly DiagnosticAnalyzer analyzer;
        private readonly HostCompilationStartAnalysisScope scope;

        public AnalyzerCompilationStartAnalysisContext(DiagnosticAnalyzer analyzer, HostCompilationStartAnalysisScope scope, Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
            : base(compilation, options, cancellationToken)
        {
            this.analyzer = analyzer;
            this.scope = scope;
        }

        public override void RegisterCompilationEndAction(Action<CompilationEndAnalysisContext> action)
        {
            this.scope.RegisterCompilationEndAction(this.analyzer, action);
        }

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            this.scope.RegisterSyntaxTreeAction(this.analyzer, action);
        }

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            this.scope.RegisterSemanticModelAction(this.analyzer, action);
        }

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            this.scope.RegisterSymbolAction(this.analyzer, action, symbolKinds);
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            this.scope.RegisterCodeBlockStartAction<TLanguageKindEnum>(this.analyzer, action);
        }

        public override void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action)
        {
            this.scope.RegisterCodeBlockEndAction<TLanguageKindEnum>(this.analyzer, action);
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            this.scope.RegisterSyntaxNodeAction(this.analyzer, action, syntaxKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a code block, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum> : CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private readonly DiagnosticAnalyzer analyzer;
        private readonly HostCodeBlockStartAnalysisScope<TLanguageKindEnum> scope;

        internal AnalyzerCodeBlockStartAnalysisContext(DiagnosticAnalyzer analyzer, 
                                                       HostCodeBlockStartAnalysisScope<TLanguageKindEnum> scope,
                                                       SyntaxNode codeBlock,
                                                       ISymbol owningSymbol,
                                                       SemanticModel semanticModel,
                                                       AnalyzerOptions options,
                                                       CancellationToken cancellationToken)
            : base(codeBlock, owningSymbol, semanticModel, options, cancellationToken)
        {
            this.analyzer = analyzer;
            this.scope = scope;
        }

        public override void RegisterCodeBlockEndAction(Action<CodeBlockEndAnalysisContext> action)
        {
            this.scope.RegisterCodeBlockEndAction(this.analyzer, action);
        }

        public override void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            this.scope.RegisterSyntaxNodeAction(this.analyzer, action, syntaxKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for an entire session, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostSessionStartAnalysisScope : HostAnalysisScope
    {
        private ImmutableArray<CompilationStartAnalyzerAction> compilationStartActions = ImmutableArray<CompilationStartAnalyzerAction>.Empty;

        public ImmutableArray<CompilationStartAnalyzerAction> CompilationStartActions
        {
            get { return this.compilationStartActions; }
        }

        public void RegisterCompilationStartAction(DiagnosticAnalyzer analyzer, Action<CompilationStartAnalysisContext> action)
        {
            CompilationStartAnalyzerAction analyzerAction = new CompilationStartAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCompilationStartAction(analyzerAction);
            this.compilationStartActions = this.compilationStartActions.Add(analyzerAction);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a compilation, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostCompilationStartAnalysisScope : HostAnalysisScope
    {
        private readonly HostSessionStartAnalysisScope sessionScope;

        public HostCompilationStartAnalysisScope(HostSessionStartAnalysisScope sessionScope)
        {
            this.sessionScope = sessionScope;
        }

        public override ImmutableArray<CompilationEndAnalyzerAction> CompilationEndActions
        {
            get { return base.CompilationEndActions.AddRange(this.sessionScope.CompilationEndActions); }
        }

        public override ImmutableArray<SemanticModelAnalyzerAction> SemanticModelActions
        {
            get { return base.SemanticModelActions.AddRange(this.sessionScope.SemanticModelActions); }
        }

        public override ImmutableArray<SyntaxTreeAnalyzerAction> SyntaxTreeActions
        {
            get { return base.SyntaxTreeActions.AddRange(this.sessionScope.SyntaxTreeActions); }
        }

        public override ImmutableArray<SymbolAnalyzerAction> SymbolActions
        {
            get { return base.SymbolActions.AddRange(this.sessionScope.SymbolActions); }
        }

        public override bool HasCodeBlockStartActions<TLanguageKindEnum>()
        {
            return
                base.HasCodeBlockStartActions<TLanguageKindEnum>() ||
                this.sessionScope.HasCodeBlockStartActions<TLanguageKindEnum>();
        }

        public override ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> GetCodeBlockStartActions<TLanguageKindEnum>()
        {
            return base.GetCodeBlockStartActions<TLanguageKindEnum>().AddRange(this.sessionScope.GetCodeBlockStartActions<TLanguageKindEnum>());
        }

        public override bool HasCodeBlockEndActions<TLanguageKindEnum>()
        {
            return
                base.HasCodeBlockEndActions<TLanguageKindEnum>() ||
                this.sessionScope.HasCodeBlockEndActions<TLanguageKindEnum>();
        }

        public override ImmutableArray<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> GetCodeBlockEndActions<TLanguageKindEnum>()
        {
            return base.GetCodeBlockEndActions<TLanguageKindEnum>().AddRange(this.sessionScope.GetCodeBlockEndActions<TLanguageKindEnum>());
        }

        public override ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> GetSyntaxNodeActions<TLanguageKindEnum>()
        {
            return base.GetSyntaxNodeActions<TLanguageKindEnum>().AddRange(this.sessionScope.GetSyntaxNodeActions<TLanguageKindEnum>());
        }

        public override AnalyzerActions GetAnalyzerActions(DiagnosticAnalyzer analyzer)
        {
            AnalyzerActions compilationActions = base.GetAnalyzerActions(analyzer);
            AnalyzerActions sessionActions = this.sessionScope.GetAnalyzerActions(analyzer);

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
    }

    /// <summary>
    /// Scope for setting up analyzers for a code block, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostCodeBlockStartAnalysisScope<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private ImmutableArray<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> codeBlockEndActions = ImmutableArray<CodeBlockEndAnalyzerAction<TLanguageKindEnum>>.Empty;
        private ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> syntaxNodeActions = ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.Empty;

        public ImmutableArray<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> CodeBlockEndActions
        {
            get { return this.codeBlockEndActions; }
        }

        public ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> SyntaxNodeActions
        {
            get { return this.syntaxNodeActions; }
        }

        internal HostCodeBlockStartAnalysisScope()
        {
        }

        public void RegisterCodeBlockEndAction(DiagnosticAnalyzer analyzer, Action<CodeBlockEndAnalysisContext> action)
        {
            this.codeBlockEndActions = this.codeBlockEndActions.Add(new CodeBlockEndAnalyzerAction<TLanguageKindEnum>(action, analyzer));
        }

        public void RegisterSyntaxNodeAction(DiagnosticAnalyzer analyzer, Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            this.syntaxNodeActions = this.syntaxNodeActions.Add(new SyntaxNodeAnalyzerAction<TLanguageKindEnum>(action, syntaxKinds, analyzer));
        }
    }

    internal abstract class HostAnalysisScope
    {
        private ImmutableArray<CompilationEndAnalyzerAction> compilationEndActions = ImmutableArray<CompilationEndAnalyzerAction>.Empty;
        private ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions = ImmutableArray<SemanticModelAnalyzerAction>.Empty;
        private ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions = ImmutableArray<SyntaxTreeAnalyzerAction>.Empty;
        private ImmutableArray<SymbolAnalyzerAction> symbolActions = ImmutableArray<SymbolAnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> codeBlockStartActions = ImmutableArray<AnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> codeBlockEndActions = ImmutableArray<AnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> syntaxNodeActions = ImmutableArray<AnalyzerAction>.Empty;
        private readonly Dictionary<DiagnosticAnalyzer, AnalyzerActions> analyzerActions = new Dictionary<DiagnosticAnalyzer, AnalyzerActions>();

        public virtual ImmutableArray<CompilationEndAnalyzerAction> CompilationEndActions
        {
            get { return this.compilationEndActions; }
        }

        public virtual ImmutableArray<SemanticModelAnalyzerAction> SemanticModelActions
        {
            get { return this.semanticModelActions; }
        }

        public virtual ImmutableArray<SyntaxTreeAnalyzerAction> SyntaxTreeActions
        {
            get { return this.syntaxTreeActions; }
        }

        public virtual ImmutableArray<SymbolAnalyzerAction> SymbolActions
        {
            get { return this.symbolActions; }
        }

        public virtual bool HasCodeBlockStartActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return this.codeBlockStartActions.OfType<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>().Any();
        }

        public virtual ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> GetCodeBlockStartActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return this.codeBlockStartActions.OfType<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>().AsImmutable();
        }

        public virtual bool HasCodeBlockEndActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return this.codeBlockEndActions.OfType<CodeBlockEndAnalyzerAction<TLanguageKindEnum>>().Any();
        }

        public virtual ImmutableArray<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> GetCodeBlockEndActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return this.codeBlockEndActions.OfType<CodeBlockEndAnalyzerAction<TLanguageKindEnum>>().AsImmutable();
        }

        public virtual ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> GetSyntaxNodeActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return this.syntaxNodeActions.OfType<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>().AsImmutable();
        }

        public virtual AnalyzerActions GetAnalyzerActions(DiagnosticAnalyzer analyzer)
        {
            AnalyzerActions actions;
            this.analyzerActions.TryGetValue(analyzer, out actions);
            return actions;
        }

        public void RegisterCompilationEndAction(DiagnosticAnalyzer analyzer, Action<CompilationEndAnalysisContext> action)
        {
            CompilationEndAnalyzerAction analyzerAction = new CompilationEndAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCompilationEndAction(analyzerAction);
            this.compilationEndActions = this.compilationEndActions.Add(analyzerAction);
        }

        public void RegisterSemanticModelAction(DiagnosticAnalyzer analyzer, Action<SemanticModelAnalysisContext> action)
        {
            SemanticModelAnalyzerAction analyzerAction = new SemanticModelAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddSemanticModelAction(analyzerAction);
            this.semanticModelActions = this.semanticModelActions.Add(analyzerAction);
        }

        public void RegisterSyntaxTreeAction(DiagnosticAnalyzer analyzer, Action<SyntaxTreeAnalysisContext> action)
        {
            SyntaxTreeAnalyzerAction analyzerAction = new SyntaxTreeAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddSyntaxTreeAction(analyzerAction);
            this.syntaxTreeActions = this.syntaxTreeActions.Add(analyzerAction);
        }

        public void RegisterSymbolAction(DiagnosticAnalyzer analyzer, Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            SymbolAnalyzerAction analyzerAction = new SymbolAnalyzerAction(action, symbolKinds, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddSymbolAction(analyzerAction);
            this.symbolActions = this.symbolActions.Add(analyzerAction);
        }

        public void RegisterCodeBlockStartAction<TLanguageKindEnum>(DiagnosticAnalyzer analyzer, Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct
        {
            CodeBlockStartAnalyzerAction<TLanguageKindEnum> analyzerAction = new CodeBlockStartAnalyzerAction<TLanguageKindEnum>(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCodeBlockStartAction(analyzerAction);
            this.codeBlockStartActions = this.codeBlockStartActions.Add(analyzerAction);
        }

        public void RegisterCodeBlockEndAction<TLanguageKindEnum>(DiagnosticAnalyzer analyzer, Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct
        {
            CodeBlockEndAnalyzerAction<TLanguageKindEnum> analyzerAction = new CodeBlockEndAnalyzerAction<TLanguageKindEnum>(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCodeBlockEndAction(analyzerAction);
            this.codeBlockEndActions = this.codeBlockEndActions.Add(analyzerAction);
        }

        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(DiagnosticAnalyzer analyzer, Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct
        {
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> analyzerAction = new SyntaxNodeAnalyzerAction<TLanguageKindEnum>(action, syntaxKinds, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddSyntaxNodeAction(analyzerAction);
            this.syntaxNodeActions = this.syntaxNodeActions.Add(analyzerAction);
        }

        protected AnalyzerActions GetOrCreateAnalyzerActions(DiagnosticAnalyzer analyzer)
        {
            AnalyzerActions actions;
            if (!this.analyzerActions.TryGetValue(analyzer, out actions))
            {
                actions = new AnalyzerActions();
                this.analyzerActions[analyzer] = actions;
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
        private ImmutableArray<CompilationStartAnalyzerAction> compilationStartActions = ImmutableArray<CompilationStartAnalyzerAction>.Empty;
        private ImmutableArray<CompilationEndAnalyzerAction> compilationEndActions = ImmutableArray<CompilationEndAnalyzerAction>.Empty;
        private ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions = ImmutableArray<SyntaxTreeAnalyzerAction>.Empty;
        private ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions = ImmutableArray<SemanticModelAnalyzerAction>.Empty;
        private ImmutableArray<SymbolAnalyzerAction> symbolActions = ImmutableArray<SymbolAnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> codeBlockStartActions = ImmutableArray<AnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> codeBlockEndActions = ImmutableArray<AnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> syntaxNodeActions = ImmutableArray<AnalyzerAction>.Empty;

        internal AnalyzerActions()
        {
        }

        public int CompilationStartActionsCount { get { return this.compilationStartActions.Length; } }
        public int CompilationEndActionsCount { get { return this.compilationEndActions.Length; } }
        public int SyntaxTreeActionsCount { get { return this.syntaxTreeActions.Length; } }
        public int SemanticModelActionsCount { get { return this.semanticModelActions.Length; } }
        public int SymbolActionsCount { get { return this.symbolActions.Length; } }
        public int SyntaxNodeActionsCount { get { return this.syntaxNodeActions.Length; } }
        public int CodeBlockStartActionsCount { get { return this.codeBlockStartActions.Length; } }
        public int CodeBlockEndActionsCount { get { return this.codeBlockEndActions.Length; } }

        internal ImmutableArray<CompilationStartAnalyzerAction> CompilationStartActions
        {
            get { return this.compilationStartActions; }
        }

        internal ImmutableArray<CompilationEndAnalyzerAction> CompilationEndActions
        {
            get { return this.compilationEndActions; }
        }

        internal ImmutableArray<SyntaxTreeAnalyzerAction> SyntaxTreeActions
        {
            get { return this.syntaxTreeActions; }
        }

        internal ImmutableArray<SemanticModelAnalyzerAction> SemanticModelActions
        {
            get { return this.semanticModelActions; }
        }

        internal ImmutableArray<SymbolAnalyzerAction> SymbolActions
        {
            get { return this.symbolActions; }
        }

        internal ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> GetCodeBlockStartActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return this.codeBlockStartActions.OfType<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>().ToImmutableArray();
        }

        internal ImmutableArray<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> GetCodeBlockEndActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return this.codeBlockEndActions.OfType<CodeBlockEndAnalyzerAction<TLanguageKindEnum>>().ToImmutableArray();
        }

        internal ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> GetSyntaxNodeActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return this.syntaxNodeActions.OfType<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>().ToImmutableArray();
        }

        internal void AddCompilationStartAction(CompilationStartAnalyzerAction action)
        {
            this.compilationStartActions = this.compilationStartActions.Add(action);
        }

        internal void AddCompilationEndAction(CompilationEndAnalyzerAction action)
        {
            this.compilationEndActions = this.compilationEndActions.Add(action);
        }

        internal void AddSyntaxTreeAction(SyntaxTreeAnalyzerAction action)
        {
            this.syntaxTreeActions = this.syntaxTreeActions.Add(action);
        }

        internal void AddSemanticModelAction(SemanticModelAnalyzerAction action)
        {
            this.semanticModelActions = this.semanticModelActions.Add(action);
        }

        internal void AddSymbolAction(SymbolAnalyzerAction action)
        {
            this.symbolActions = this.symbolActions.Add(action);
        }

        internal void AddCodeBlockStartAction<TLanguageKindEnum>(CodeBlockStartAnalyzerAction<TLanguageKindEnum> action) where TLanguageKindEnum : struct
        {
            this.codeBlockStartActions = this.codeBlockStartActions.Add(action);
        }

        internal void AddCodeBlockEndAction<TLanguageKindEnum>(CodeBlockEndAnalyzerAction<TLanguageKindEnum> action) where TLanguageKindEnum : struct
        {
            this.codeBlockEndActions = this.codeBlockEndActions.Add(action);
        }

        internal void AddSyntaxNodeAction<TLanguageKindEnum>(SyntaxNodeAnalyzerAction<TLanguageKindEnum> action) where TLanguageKindEnum : struct
        {
            this.syntaxNodeActions = this.syntaxNodeActions.Add(action);
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
            actions.compilationStartActions = this.compilationStartActions.AddRange(otherActions.compilationStartActions);
            actions.compilationEndActions = this.compilationEndActions.AddRange(otherActions.compilationEndActions);
            actions.syntaxTreeActions = this.syntaxTreeActions.AddRange(otherActions.syntaxTreeActions);
            actions.semanticModelActions = this.semanticModelActions.AddRange(otherActions.semanticModelActions);
            actions.symbolActions = this.symbolActions.AddRange(otherActions.symbolActions);
            actions.codeBlockStartActions = this.codeBlockStartActions.AddRange(otherActions.codeBlockStartActions);
            actions.codeBlockEndActions = this.codeBlockEndActions.AddRange(otherActions.codeBlockEndActions);
            actions.syntaxNodeActions = this.syntaxNodeActions.AddRange(otherActions.syntaxNodeActions);

            return actions;
        }
    }
}
