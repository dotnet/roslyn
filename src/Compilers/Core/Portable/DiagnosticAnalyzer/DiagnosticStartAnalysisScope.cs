// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Scope for setting up analyzers for an entire session, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerAnalysisContext : AnalysisContext
    {
        private readonly DiagnosticAnalyzer _analyzer;
        private readonly HostSessionStartAnalysisScope _scope;
        
        private readonly bool _isIOperationFeatureEnabled;

        public AnalyzerAnalysisContext(DiagnosticAnalyzer analyzer, HostSessionStartAnalysisScope scope, bool isIOperationFeatureEnabled = false)
        {
            _analyzer = analyzer;
            _scope = scope;
            _isIOperationFeatureEnabled = isIOperationFeatureEnabled;
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

        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyIOperationFeatureFlag(_isIOperationFeatureEnabled);
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, operationKinds);
            _scope.RegisterOperationAction(_analyzer, action, operationKinds);
        }

        public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyIOperationFeatureFlag(_isIOperationFeatureEnabled);
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockStartAction(_analyzer, action);
        }

        public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyIOperationFeatureFlag(_isIOperationFeatureEnabled);
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockAction(_analyzer, action);
        }

        public override void EnableConcurrentExecution()
        {
            _scope.EnableConcurrentExecution(_analyzer);
        }

        public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags mode)
        {
            _scope.ConfigureGeneratedCodeAnalysis(_analyzer, mode);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a compilation, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerCompilationStartAnalysisContext : CompilationStartAnalysisContext
    {
        private readonly DiagnosticAnalyzer _analyzer;
        private readonly HostCompilationStartAnalysisScope _scope;
        private readonly CompilationAnalysisValueProviderFactory _compilationAnalysisValueProviderFactory;
        
        private readonly bool _isIOperationFeatureEnabled;

        public AnalyzerCompilationStartAnalysisContext(
            DiagnosticAnalyzer analyzer,
            HostCompilationStartAnalysisScope scope,
            Compilation compilation,
            AnalyzerOptions options,
            CompilationAnalysisValueProviderFactory compilationAnalysisValueProviderFactory,
            CancellationToken cancellationToken)
            : base(compilation, options, cancellationToken)
        {
            _analyzer = analyzer;
            _scope = scope;
            _compilationAnalysisValueProviderFactory = compilationAnalysisValueProviderFactory;
            _isIOperationFeatureEnabled = compilation.IsIOperationFeatureEnabled();
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

        public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyIOperationFeatureFlag(_isIOperationFeatureEnabled);
            _scope.RegisterOperationBlockStartAction(_analyzer, action);
        }

        public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyIOperationFeatureFlag(_isIOperationFeatureEnabled);
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockAction(_analyzer, action);
        }

        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyIOperationFeatureFlag(_isIOperationFeatureEnabled);
            _scope.RegisterOperationAction(_analyzer, action, operationKinds);
        }

        internal override bool TryGetValueCore<TKey, TValue>(TKey key, AnalysisValueProvider<TKey, TValue> valueProvider, out TValue value)
        {
            var compilationAnalysisValueProvider = _compilationAnalysisValueProviderFactory.GetValueProvider(valueProvider);
            return compilationAnalysisValueProvider.TryGetValue(key, out value);
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
    /// Scope for setting up analyzers for an operation block, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerOperationBlockStartAnalysisContext : OperationBlockStartAnalysisContext
    {
        private readonly DiagnosticAnalyzer _analyzer;
        private readonly HostOperationBlockStartAnalysisScope _scope;

        internal AnalyzerOperationBlockStartAnalysisContext(DiagnosticAnalyzer analyzer,
                                                            HostOperationBlockStartAnalysisScope scope,
                                                            ImmutableArray<IOperation> operationBlocks,
                                                            ISymbol owningSymbol,
                                                            Compilation compilation,
                                                            AnalyzerOptions options,
                                                            CancellationToken cancellationToken)
            : base(operationBlocks, owningSymbol, compilation, options, cancellationToken)
        {
            _analyzer = analyzer;
            _scope = scope;
        }

        public override void RegisterOperationBlockEndAction(Action<OperationBlockAnalysisContext> action)
        {
            _scope.RegisterOperationBlockEndAction(_analyzer, action);
        }

        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            _scope.RegisterOperationAction(_analyzer, action, operationKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for an entire session, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostSessionStartAnalysisScope : HostAnalysisScope
    {
        private ImmutableArray<CompilationStartAnalyzerAction> _compilationStartActions = ImmutableArray<CompilationStartAnalyzerAction>.Empty;
        private ImmutableHashSet<DiagnosticAnalyzer> _concurrentAnalyzers = ImmutableHashSet<DiagnosticAnalyzer>.Empty;
        private ConcurrentDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> _generatedCodeConfigurationMap = new ConcurrentDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags>();

        public ImmutableArray<CompilationStartAnalyzerAction> CompilationStartActions
        {
            get { return _compilationStartActions; }
        }

        public bool IsConcurrentAnalyzer(DiagnosticAnalyzer analyzer)
        {
            return _concurrentAnalyzers.Contains(analyzer);
        }

        public GeneratedCodeAnalysisFlags GetGeneratedCodeAnalysisFlags(DiagnosticAnalyzer analyzer)
        {
            GeneratedCodeAnalysisFlags mode;
            return _generatedCodeConfigurationMap.TryGetValue(analyzer, out mode) ? mode : AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags;
        }

        public void RegisterCompilationStartAction(DiagnosticAnalyzer analyzer, Action<CompilationStartAnalysisContext> action)
        {
            CompilationStartAnalyzerAction analyzerAction = new CompilationStartAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddCompilationStartAction(analyzerAction);
            _compilationStartActions = _compilationStartActions.Add(analyzerAction);
        }

        public void EnableConcurrentExecution(DiagnosticAnalyzer analyzer)
        {
            _concurrentAnalyzers = _concurrentAnalyzers.Add(analyzer);
        }

        public void ConfigureGeneratedCodeAnalysis(DiagnosticAnalyzer analyzer, GeneratedCodeAnalysisFlags mode)
        {
            _generatedCodeConfigurationMap.AddOrUpdate(analyzer, addValue: mode, updateValueFactory: (a, c) => mode);
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

        public override ImmutableArray<OperationAnalyzerAction> OperationActions
        {
            get { return base.OperationActions.AddRange(_sessionScope.OperationActions); }
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

    internal sealed class HostOperationBlockStartAnalysisScope
    {
        private ImmutableArray<OperationBlockAnalyzerAction> _operationBlockEndActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
        private ImmutableArray<OperationAnalyzerAction> _operationActions = ImmutableArray<OperationAnalyzerAction>.Empty;

        public ImmutableArray<OperationBlockAnalyzerAction> OperationBlockEndActions => _operationBlockEndActions;

        public ImmutableArray<OperationAnalyzerAction> OperationActions => _operationActions;

        internal HostOperationBlockStartAnalysisScope()
        {
        }

        public void RegisterOperationBlockEndAction(DiagnosticAnalyzer analyzer, Action<OperationBlockAnalysisContext> action)
        {
            _operationBlockEndActions = _operationBlockEndActions.Add(new OperationBlockAnalyzerAction(action, analyzer));
        }

        public void RegisterOperationAction(DiagnosticAnalyzer analyzer, Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            _operationActions = _operationActions.Add(new OperationAnalyzerAction(action, operationKinds, analyzer));
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
        private ImmutableArray<OperationBlockStartAnalyzerAction> _operationBlockStartActions = ImmutableArray<OperationBlockStartAnalyzerAction>.Empty;
        private ImmutableArray<OperationBlockAnalyzerAction> _operationBlockEndActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
        private ImmutableArray<OperationBlockAnalyzerAction> _operationBlockActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
        private ImmutableArray<OperationAnalyzerAction> _operationActions = ImmutableArray<OperationAnalyzerAction>.Empty;
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

        public virtual ImmutableArray<OperationAnalyzerAction> OperationActions
        {
            get { return _operationActions; }
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

        public void RegisterOperationBlockStartAction(DiagnosticAnalyzer analyzer, Action<OperationBlockStartAnalysisContext> action)
        {
            OperationBlockStartAnalyzerAction analyzerAction = new OperationBlockStartAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddOperationBlockStartAction(analyzerAction);
            _operationBlockStartActions = _operationBlockStartActions.Add(analyzerAction);
        }

        public void RegisterOperationBlockEndAction(DiagnosticAnalyzer analyzer, Action<OperationBlockAnalysisContext> action)
        {
            OperationBlockAnalyzerAction analyzerAction = new OperationBlockAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddOperationBlockEndAction(analyzerAction);
            _operationBlockEndActions = _operationBlockEndActions.Add(analyzerAction);
        }

        public void RegisterOperationBlockAction(DiagnosticAnalyzer analyzer, Action<OperationBlockAnalysisContext> action)
        {
            OperationBlockAnalyzerAction analyzerAction = new OperationBlockAnalyzerAction(action, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddOperationBlockAction(analyzerAction);
            _operationBlockActions = _operationBlockActions.Add(analyzerAction);
        }

        public void RegisterOperationAction(DiagnosticAnalyzer analyzer, Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            OperationAnalyzerAction analyzerAction = new OperationAnalyzerAction(action, operationKinds, analyzer);
            this.GetOrCreateAnalyzerActions(analyzer).AddOperationAction(analyzerAction);
            _operationActions = _operationActions.Add(analyzerAction);
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
        private ImmutableArray<OperationBlockStartAnalyzerAction> _operationBlockStartActions = ImmutableArray<OperationBlockStartAnalyzerAction>.Empty;
        private ImmutableArray<OperationBlockAnalyzerAction> _operationBlockEndActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
        private ImmutableArray<OperationBlockAnalyzerAction> _operationBlockActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
        private ImmutableArray<AnalyzerAction> _syntaxNodeActions = ImmutableArray<AnalyzerAction>.Empty;
        private ImmutableArray<OperationAnalyzerAction> _operationActions = ImmutableArray<OperationAnalyzerAction>.Empty;

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
        public int OperationActionsCount { get { return _operationActions.Length; } }
        public int OperationBlockStartActionsCount { get { return _operationBlockStartActions.Length; } }
        public int OperationBlockEndActionsCount { get { return _operationBlockEndActions.Length; } }
        public int OperationBlockActionsCount { get { return _operationBlockActions.Length; } }
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

        internal ImmutableArray<OperationBlockAnalyzerAction> OperationBlockActions
        {
            get { return _operationBlockActions; }
        }

        internal ImmutableArray<OperationBlockAnalyzerAction> OperationBlockEndActions
        {
            get { return _operationBlockEndActions; }
        }

        internal ImmutableArray<OperationBlockStartAnalyzerAction> OperationBlockStartActions
        {
            get { return _operationBlockStartActions; }
        }

        internal ImmutableArray<OperationAnalyzerAction> OperationActions
        {
            get { return _operationActions; }
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

        internal void AddOperationBlockStartAction(OperationBlockStartAnalyzerAction action)
        {
            _operationBlockStartActions = _operationBlockStartActions.Add(action);
        }

        internal void AddOperationBlockAction(OperationBlockAnalyzerAction action)
        {
            _operationBlockActions = _operationBlockActions.Add(action);
        }

        internal void AddOperationBlockEndAction(OperationBlockAnalyzerAction action)
        {
            _operationBlockEndActions = _operationBlockEndActions.Add(action);
        }

        internal void AddOperationAction(OperationAnalyzerAction action)
        {
            _operationActions = _operationActions.Add(action);
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
            actions._operationActions = _operationActions.AddRange(otherActions._operationActions);
            actions._operationBlockStartActions = _operationBlockStartActions.AddRange(otherActions._operationBlockStartActions);
            actions._operationBlockEndActions = _operationBlockEndActions.AddRange(otherActions._operationBlockEndActions);
            actions._operationBlockActions = _operationBlockActions.AddRange(otherActions._operationBlockActions);

            return actions;
        }
    }
}
