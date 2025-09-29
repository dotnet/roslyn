// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Scope for setting up analyzers for an entire session, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerAnalysisContext : AnalysisContext
    {
        private readonly HostSessionStartAnalysisScope _scope;

        public AnalyzerAnalysisContext(HostSessionStartAnalysisScope scope, SeverityFilter severityFilter)
        {
            _scope = scope;
            MinimumReportedSeverity = severityFilter.GetMinimumUnfilteredSeverity();
        }

        public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCompilationStartAction(action);
        }

        public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCompilationAction(action);
        }

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSyntaxTreeAction(action);
        }

        public override void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterAdditionalFileAction(action);
        }

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSemanticModelAction(action);
        }

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, symbolKinds);
            _scope.RegisterSymbolAction(action, symbolKinds);
        }

        public override void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSymbolStartAction(action, symbolKind);
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockStartAction<TLanguageKindEnum>(action);
        }

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockAction(action);
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, syntaxKinds);
            _scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }

        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, operationKinds);
            _scope.RegisterOperationAction(action, operationKinds);
        }

        public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockStartAction(action);
        }

        public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockAction(action);
        }

        public override void EnableConcurrentExecution()
        {
            _scope.EnableConcurrentExecution();
        }

        public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags mode)
        {
            _scope.ConfigureGeneratedCodeAnalysis(mode);
        }

        public override DiagnosticSeverity MinimumReportedSeverity { get; }
    }

    /// <summary>
    /// Scope for setting up analyzers for a compilation, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerCompilationStartAnalysisContext : CompilationStartAnalysisContext
    {
        private readonly HostCompilationStartAnalysisScope _scope;
        private readonly CompilationAnalysisValueProviderFactory _compilationAnalysisValueProviderFactory;

        public AnalyzerCompilationStartAnalysisContext(
            HostCompilationStartAnalysisScope scope,
            Compilation compilation,
            AnalyzerOptions options,
            CompilationAnalysisValueProviderFactory compilationAnalysisValueProviderFactory,
            CancellationToken cancellationToken)
            : base(compilation, options, cancellationToken)
        {
            _scope = scope;
            _compilationAnalysisValueProviderFactory = compilationAnalysisValueProviderFactory;
        }

        public override void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCompilationEndAction(action);
        }

        public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSyntaxTreeAction(action);
        }

        public override void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterAdditionalFileAction(action);
        }

        public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSemanticModelAction(action);
        }

        public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, symbolKinds);
            _scope.RegisterSymbolAction(action, symbolKinds);
        }

        public override void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSymbolStartAction(action, symbolKind);
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockStartAction<TLanguageKindEnum>(action);
        }

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockAction(action);
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, syntaxKinds);
            _scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }

        public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockStartAction(action);
        }

        public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockAction(action);
        }

        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, operationKinds);
            _scope.RegisterOperationAction(action, operationKinds);
        }

        internal override bool TryGetValueCore<TKey, TValue>(TKey key, AnalysisValueProvider<TKey, TValue> valueProvider, [MaybeNullWhen(false)] out TValue value)
        {
            var compilationAnalysisValueProvider = _compilationAnalysisValueProviderFactory.GetValueProvider(valueProvider);
            return compilationAnalysisValueProvider.TryGetValue(key, out value);
        }

        public AnalyzerCompilationStartAnalysisContext WithOptions(AnalyzerOptions options)
            => this.Options == options
                ? this
                : new(_scope, this.Compilation, options, _compilationAnalysisValueProviderFactory, this.CancellationToken);
    }

    /// <summary>
    /// Scope for setting up analyzers for code within a symbol and its members.
    /// </summary>
    internal sealed class AnalyzerSymbolStartAnalysisContext : SymbolStartAnalysisContext
    {
        private readonly HostSymbolStartAnalysisScope _scope;

        internal AnalyzerSymbolStartAnalysisContext(
                                                       HostSymbolStartAnalysisScope scope,
                                                       ISymbol owningSymbol,
                                                       Compilation compilation,
                                                       AnalyzerOptions options,
                                                       bool isGeneratedCode,
                                                       SyntaxTree? filterTree,
                                                       TextSpan? filterSpan,
                                                       CancellationToken cancellationToken)
            : base(owningSymbol, compilation, options, isGeneratedCode, filterTree, filterSpan, cancellationToken)
        {
            _scope = scope;
        }

        public override void RegisterSymbolEndAction(Action<SymbolAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterSymbolEndAction(action);
        }

        public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockStartAction<TLanguageKindEnum>(action);
        }

        public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockAction(action);
        }

        public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, syntaxKinds);
            _scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }

        public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockStartAction(action);
        }

        public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockAction(action);
        }

        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, operationKinds);
            _scope.RegisterOperationAction(action, operationKinds);
        }

        public AnalyzerSymbolStartAnalysisContext WithOptions(AnalyzerOptions analyzerOptions)
            => this.Options == analyzerOptions
                ? this
                : new(_scope, this.Symbol, this.Compilation, analyzerOptions, this.IsGeneratedCode, this.FilterTree, this.FilterSpan, this.CancellationToken);
    }

    /// <summary>
    /// Scope for setting up analyzers for a code block, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum> : CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private readonly HostCodeBlockStartAnalysisScope<TLanguageKindEnum> _scope;

        internal AnalyzerCodeBlockStartAnalysisContext(
                                                       HostCodeBlockStartAnalysisScope<TLanguageKindEnum> scope,
                                                       SyntaxNode codeBlock,
                                                       ISymbol owningSymbol,
                                                       SemanticModel semanticModel,
                                                       AnalyzerOptions options,
                                                       TextSpan? filterSpan,
                                                       bool isGeneratedCode,
                                                       CancellationToken cancellationToken)
            : base(codeBlock, owningSymbol, semanticModel, options, filterSpan, isGeneratedCode, cancellationToken)
        {
            _scope = scope;
        }

        public override void RegisterCodeBlockEndAction(Action<CodeBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterCodeBlockEndAction(action);
        }

        public override void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, syntaxKinds);
            _scope.RegisterSyntaxNodeAction(action, syntaxKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for an operation block, automatically associating actions with analyzers.
    /// </summary>
    internal sealed class AnalyzerOperationBlockStartAnalysisContext : OperationBlockStartAnalysisContext
    {
        private readonly HostOperationBlockStartAnalysisScope _scope;

        internal AnalyzerOperationBlockStartAnalysisContext(
                                                            HostOperationBlockStartAnalysisScope scope,
                                                            ImmutableArray<IOperation> operationBlocks,
                                                            ISymbol owningSymbol,
                                                            Compilation compilation,
                                                            AnalyzerOptions options,
                                                            Func<IOperation, ControlFlowGraph> getControlFlowGraph,
                                                            SyntaxTree filterTree,
                                                            TextSpan? filterSpan,
                                                            bool isGeneratedCode,
                                                            CancellationToken cancellationToken)
            : base(operationBlocks, owningSymbol, compilation, options, getControlFlowGraph, filterTree, filterSpan, isGeneratedCode, cancellationToken)
        {
            _scope = scope;
        }

        public override void RegisterOperationBlockEndAction(Action<OperationBlockAnalysisContext> action)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action);
            _scope.RegisterOperationBlockEndAction(action);
        }

        public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            DiagnosticAnalysisContextHelpers.VerifyArguments(action, operationKinds);
            _scope.RegisterOperationAction(action, operationKinds);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for an entire session, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostSessionStartAnalysisScope(DiagnosticAnalyzer analyzer)
        : HostAnalysisScope(analyzer)
    {
        private bool _isConcurrent;
        private GeneratedCodeAnalysisFlags _generatedCodeConfiguration = AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags;

        public bool IsConcurrentAnalyzer()
        {
            return _isConcurrent;
        }

        public GeneratedCodeAnalysisFlags GetGeneratedCodeAnalysisFlags()
        {
            return _generatedCodeConfiguration;
        }

        public void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
        {
            CompilationStartAnalyzerAction analyzerAction = new CompilationStartAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddCompilationStartAction(analyzerAction);
        }

        public void EnableConcurrentExecution()
        {
            _isConcurrent = true;
            GetOrCreateAnalyzerActions().Value.EnableConcurrentExecution();
        }

        public void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags mode)
        {
            _generatedCodeConfiguration = mode;
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for a compilation, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostCompilationStartAnalysisScope : HostAnalysisScope
    {
        private readonly HostSessionStartAnalysisScope _sessionScope;

        public HostCompilationStartAnalysisScope(HostSessionStartAnalysisScope sessionScope)
            : base(sessionScope.Analyzer)
        {
            _sessionScope = sessionScope;
        }

        public override AnalyzerActions GetAnalyzerActions()
        {
            AnalyzerActions compilationActions = base.GetAnalyzerActions();
            AnalyzerActions sessionActions = _sessionScope.GetAnalyzerActions();

            if (sessionActions.IsEmpty)
            {
                return compilationActions;
            }

            if (compilationActions.IsEmpty)
            {
                return sessionActions;
            }

            return compilationActions.Append(in sessionActions);
        }
    }

    /// <summary>
    /// Scope for setting up analyzers for analyzing a symbol and its members.
    /// </summary>
    internal sealed class HostSymbolStartAnalysisScope(DiagnosticAnalyzer analyzer)
        : HostAnalysisScope(analyzer)
    {
    }

    /// <summary>
    /// Scope for setting up analyzers for a code block, capable of retrieving the actions.
    /// </summary>
    internal sealed class HostCodeBlockStartAnalysisScope<TLanguageKindEnum>(DiagnosticAnalyzer analyzer) where TLanguageKindEnum : struct
    {
        private ImmutableArray<CodeBlockAnalyzerAction> _codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
        private ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> _syntaxNodeActions = ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.Empty;

        private DiagnosticAnalyzer Analyzer { get; } = analyzer;

        public ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions
        {
            get { return _codeBlockEndActions; }
        }

        public ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> SyntaxNodeActions
        {
            get { return _syntaxNodeActions; }
        }

        public void RegisterCodeBlockEndAction(Action<CodeBlockAnalysisContext> action)
        {
            _codeBlockEndActions = _codeBlockEndActions.Add(new CodeBlockAnalyzerAction(action, Analyzer));
        }

        public void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
        {
            _syntaxNodeActions = _syntaxNodeActions.Add(new SyntaxNodeAnalyzerAction<TLanguageKindEnum>(action, syntaxKinds, Analyzer));
        }
    }

    internal sealed class HostOperationBlockStartAnalysisScope(DiagnosticAnalyzer analyzer)
    {
        private ImmutableArray<OperationBlockAnalyzerAction> _operationBlockEndActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
        private ImmutableArray<OperationAnalyzerAction> _operationActions = ImmutableArray<OperationAnalyzerAction>.Empty;

        private DiagnosticAnalyzer Analyzer { get; } = analyzer;

        public ImmutableArray<OperationBlockAnalyzerAction> OperationBlockEndActions => _operationBlockEndActions;

        public ImmutableArray<OperationAnalyzerAction> OperationActions => _operationActions;

        public void RegisterOperationBlockEndAction(Action<OperationBlockAnalysisContext> action)
        {
            _operationBlockEndActions = _operationBlockEndActions.Add(new OperationBlockAnalyzerAction(action, Analyzer));
        }

        public void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            _operationActions = _operationActions.Add(new OperationAnalyzerAction(action, operationKinds, Analyzer));
        }
    }

    internal abstract class HostAnalysisScope(DiagnosticAnalyzer analyzer)
    {
        private StrongBox<AnalyzerActions>? _analyzerActions;

        internal DiagnosticAnalyzer Analyzer { get; } = analyzer;

        public virtual AnalyzerActions GetAnalyzerActions()
        {
            return this.GetOrCreateAnalyzerActions().Value;
        }

        public void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
        {
            CompilationAnalyzerAction analyzerAction = new CompilationAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddCompilationAction(analyzerAction);
        }

        public void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action)
        {
            CompilationAnalyzerAction analyzerAction = new CompilationAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddCompilationEndAction(analyzerAction);
        }

        public void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action)
        {
            SemanticModelAnalyzerAction analyzerAction = new SemanticModelAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddSemanticModelAction(analyzerAction);
        }

        public void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action)
        {
            SyntaxTreeAnalyzerAction analyzerAction = new SyntaxTreeAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddSyntaxTreeAction(analyzerAction);
        }

        public void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action)
        {
            var analyzerAction = new AdditionalFileAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddAdditionalFileAction(analyzerAction);
        }

        public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            SymbolAnalyzerAction analyzerAction = new SymbolAnalyzerAction(action, symbolKinds, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddSymbolAction(analyzerAction);

            // The SymbolAnalyzerAction does not handle SymbolKind.Parameter because the compiler
            // does not make CompilationEvents for them. As a workaround, handle them specially by
            // registering further SymbolActions (for Methods) and utilize the results to construct
            // the necessary SymbolAnalysisContexts.

            if (symbolKinds.Contains(SymbolKind.Parameter))
            {
                RegisterSymbolAction(
                    context =>
                    {
                        ImmutableArray<IParameterSymbol> parameters;

                        switch (context.Symbol.Kind)
                        {
                            case SymbolKind.Method:
                                parameters = ((IMethodSymbol)context.Symbol).Parameters;
                                break;
                            case SymbolKind.Property:
                                parameters = ((IPropertySymbol)context.Symbol).Parameters;
                                break;
                            case SymbolKind.NamedType:
                                var namedType = (INamedTypeSymbol)context.Symbol;
                                if (namedType.IsExtension)
                                {
                                    parameters = namedType.ExtensionParameter is { } extensionParameter ? [extensionParameter] : [];
                                }
                                else
                                {
                                    var delegateInvokeMethod = namedType.DelegateInvokeMethod;
                                    parameters = delegateInvokeMethod?.Parameters ?? ImmutableArray.Create<IParameterSymbol>();
                                }
                                break;
                            default:
                                throw new ArgumentException($"{context.Symbol.Kind} is not supported.", nameof(context));
                        }

                        foreach (var parameter in parameters)
                        {
                            if (!parameter.IsImplicitlyDeclared)
                            {
                                action(new SymbolAnalysisContext(
                                    parameter,
                                    context.Compilation,
                                    context.Options,
                                    context.ReportDiagnostic,
                                    context.IsSupportedDiagnostic,
                                    context.IsGeneratedCode,
                                    context.FilterTree,
                                    context.FilterSpan,
                                    context.CancellationToken));
                            }
                        }
                    },
                    ImmutableArray.Create(SymbolKind.Method, SymbolKind.Property, SymbolKind.NamedType));
            }
        }

        public void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind)
        {
            var analyzerAction = new SymbolStartAnalyzerAction(action, symbolKind, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddSymbolStartAction(analyzerAction);
        }

        public void RegisterSymbolEndAction(Action<SymbolAnalysisContext> action)
        {
            var analyzerAction = new SymbolEndAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddSymbolEndAction(analyzerAction);
        }

        public void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct
        {
            CodeBlockStartAnalyzerAction<TLanguageKindEnum> analyzerAction = new CodeBlockStartAnalyzerAction<TLanguageKindEnum>(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddCodeBlockStartAction(analyzerAction);
        }

        public void RegisterCodeBlockEndAction(Action<CodeBlockAnalysisContext> action)
        {
            CodeBlockAnalyzerAction analyzerAction = new CodeBlockAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddCodeBlockEndAction(analyzerAction);
        }

        public void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action)
        {
            CodeBlockAnalyzerAction analyzerAction = new CodeBlockAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddCodeBlockAction(analyzerAction);
        }

        public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct
        {
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> analyzerAction = new SyntaxNodeAnalyzerAction<TLanguageKindEnum>(action, syntaxKinds, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddSyntaxNodeAction(analyzerAction);
        }

        public void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action)
        {
            OperationBlockStartAnalyzerAction analyzerAction = new OperationBlockStartAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddOperationBlockStartAction(analyzerAction);
        }

        public void RegisterOperationBlockEndAction(Action<OperationBlockAnalysisContext> action)
        {
            OperationBlockAnalyzerAction analyzerAction = new OperationBlockAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddOperationBlockEndAction(analyzerAction);
        }

        public void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action)
        {
            OperationBlockAnalyzerAction analyzerAction = new OperationBlockAnalyzerAction(action, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddOperationBlockAction(analyzerAction);
        }

        public void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            OperationAnalyzerAction analyzerAction = new OperationAnalyzerAction(action, operationKinds, Analyzer);
            this.GetOrCreateAnalyzerActions().Value.AddOperationAction(analyzerAction);
        }

        protected StrongBox<AnalyzerActions> GetOrCreateAnalyzerActions()
        {
            return InterlockedOperations.Initialize(ref _analyzerActions, static () => new StrongBox<AnalyzerActions>(AnalyzerActions.Empty));
        }
    }

    /// <summary>
    /// Actions registered by a particular analyzer.
    /// </summary>
    // ToDo: AnalyzerActions, and all of the mechanism around it, can be eliminated if the IDE diagnostic analyzer driver
    // moves from an analyzer-centric model to an action-centric model. For example, the driver would need to stop asking
    // if a particular analyzer can analyze syntax trees, and instead ask if any syntax tree actions are present. Also,
    // the driver needs to apply all relevant actions rather then applying the actions of individual analyzers.
    internal struct AnalyzerActions
    {
        public static readonly AnalyzerActions Empty = new AnalyzerActions(concurrent: false);

        private ImmutableArray<CompilationStartAnalyzerAction> _compilationStartActions;
        private ImmutableArray<CompilationAnalyzerAction> _compilationEndActions;
        private ImmutableArray<CompilationAnalyzerAction> _compilationActions;
        private ImmutableArray<SyntaxTreeAnalyzerAction> _syntaxTreeActions;
        private ImmutableArray<AdditionalFileAnalyzerAction> _additionalFileActions;
        private ImmutableArray<SemanticModelAnalyzerAction> _semanticModelActions;
        private ImmutableArray<SymbolAnalyzerAction> _symbolActions;
        private ImmutableArray<SymbolStartAnalyzerAction> _symbolStartActions;
        private ImmutableArray<SymbolEndAnalyzerAction> _symbolEndActions;
        private ImmutableArray<AnalyzerAction> _codeBlockStartActions;
        private ImmutableArray<CodeBlockAnalyzerAction> _codeBlockEndActions;
        private ImmutableArray<CodeBlockAnalyzerAction> _codeBlockActions;
        private ImmutableArray<OperationBlockStartAnalyzerAction> _operationBlockStartActions;
        private ImmutableArray<OperationBlockAnalyzerAction> _operationBlockEndActions;
        private ImmutableArray<OperationBlockAnalyzerAction> _operationBlockActions;
        private ImmutableArray<AnalyzerAction> _syntaxNodeActions;
        private ImmutableArray<OperationAnalyzerAction> _operationActions;
        private bool _concurrent;

        internal AnalyzerActions(bool concurrent)
        {
            _compilationStartActions = ImmutableArray<CompilationStartAnalyzerAction>.Empty;
            _compilationEndActions = ImmutableArray<CompilationAnalyzerAction>.Empty;
            _compilationActions = ImmutableArray<CompilationAnalyzerAction>.Empty;
            _syntaxTreeActions = ImmutableArray<SyntaxTreeAnalyzerAction>.Empty;
            _additionalFileActions = ImmutableArray<AdditionalFileAnalyzerAction>.Empty;
            _semanticModelActions = ImmutableArray<SemanticModelAnalyzerAction>.Empty;
            _symbolActions = ImmutableArray<SymbolAnalyzerAction>.Empty;
            _symbolStartActions = ImmutableArray<SymbolStartAnalyzerAction>.Empty;
            _symbolEndActions = ImmutableArray<SymbolEndAnalyzerAction>.Empty;
            _codeBlockStartActions = ImmutableArray<AnalyzerAction>.Empty;
            _codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
            _codeBlockActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
            _operationBlockStartActions = ImmutableArray<OperationBlockStartAnalyzerAction>.Empty;
            _operationBlockEndActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
            _operationBlockActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
            _syntaxNodeActions = ImmutableArray<AnalyzerAction>.Empty;
            _operationActions = ImmutableArray<OperationAnalyzerAction>.Empty;
            _concurrent = concurrent;

            IsEmpty = true;
        }

        public AnalyzerActions(
            ImmutableArray<CompilationStartAnalyzerAction> compilationStartActions,
            ImmutableArray<CompilationAnalyzerAction> compilationEndActions,
            ImmutableArray<CompilationAnalyzerAction> compilationActions,
            ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions,
            ImmutableArray<AdditionalFileAnalyzerAction> additionalFileActions,
            ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions,
            ImmutableArray<SymbolAnalyzerAction> symbolActions,
            ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions,
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            ImmutableArray<AnalyzerAction> codeBlockStartActions,
            ImmutableArray<CodeBlockAnalyzerAction> codeBlockEndActions,
            ImmutableArray<CodeBlockAnalyzerAction> codeBlockActions,
            ImmutableArray<OperationBlockStartAnalyzerAction> operationBlockStartActions,
            ImmutableArray<OperationBlockAnalyzerAction> operationBlockEndActions,
            ImmutableArray<OperationBlockAnalyzerAction> operationBlockActions,
            ImmutableArray<AnalyzerAction> syntaxNodeActions,
            ImmutableArray<OperationAnalyzerAction> operationActions,
            bool concurrent,
            bool isEmpty)
        {
            _compilationStartActions = compilationStartActions;
            _compilationEndActions = compilationEndActions;
            _compilationActions = compilationActions;
            _syntaxTreeActions = syntaxTreeActions;
            _additionalFileActions = additionalFileActions;
            _semanticModelActions = semanticModelActions;
            _symbolActions = symbolActions;
            _symbolStartActions = symbolStartActions;
            _symbolEndActions = symbolEndActions;
            _codeBlockStartActions = codeBlockStartActions;
            _codeBlockEndActions = codeBlockEndActions;
            _codeBlockActions = codeBlockActions;
            _operationBlockStartActions = operationBlockStartActions;
            _operationBlockEndActions = operationBlockEndActions;
            _operationBlockActions = operationBlockActions;
            _syntaxNodeActions = syntaxNodeActions;
            _operationActions = operationActions;
            _concurrent = concurrent;
            IsEmpty = isEmpty;
        }

        public readonly int CompilationStartActionsCount { get { return _compilationStartActions.Length; } }
        public readonly int CompilationEndActionsCount { get { return _compilationEndActions.Length; } }
        public readonly int CompilationActionsCount { get { return _compilationActions.Length; } }
        public readonly int SyntaxTreeActionsCount { get { return _syntaxTreeActions.Length; } }
        public readonly int AdditionalFileActionsCount { get { return _additionalFileActions.Length; } }
        public readonly int SemanticModelActionsCount { get { return _semanticModelActions.Length; } }
        public readonly int SymbolActionsCount { get { return _symbolActions.Length; } }
        public readonly int SymbolStartActionsCount { get { return _symbolStartActions.Length; } }
        public readonly int SymbolEndActionsCount { get { return _symbolEndActions.Length; } }
        public readonly int SyntaxNodeActionsCount { get { return _syntaxNodeActions.Length; } }
        public readonly int OperationActionsCount { get { return _operationActions.Length; } }
        public readonly int OperationBlockStartActionsCount { get { return _operationBlockStartActions.Length; } }
        public readonly int OperationBlockEndActionsCount { get { return _operationBlockEndActions.Length; } }
        public readonly int OperationBlockActionsCount { get { return _operationBlockActions.Length; } }
        public readonly int CodeBlockStartActionsCount { get { return _codeBlockStartActions.Length; } }
        public readonly int CodeBlockEndActionsCount { get { return _codeBlockEndActions.Length; } }
        public readonly int CodeBlockActionsCount { get { return _codeBlockActions.Length; } }
        public readonly bool Concurrent => _concurrent;
        public bool IsEmpty { readonly get; private set; }
        public readonly bool IsDefault => _compilationStartActions.IsDefault;

        internal readonly ImmutableArray<CompilationStartAnalyzerAction> CompilationStartActions
        {
            get { return _compilationStartActions; }
        }

        internal readonly ImmutableArray<CompilationAnalyzerAction> CompilationEndActions
        {
            get { return _compilationEndActions; }
        }

        internal readonly ImmutableArray<CompilationAnalyzerAction> CompilationActions
        {
            get { return _compilationActions; }
        }

        internal readonly ImmutableArray<SyntaxTreeAnalyzerAction> SyntaxTreeActions
        {
            get { return _syntaxTreeActions; }
        }

        internal readonly ImmutableArray<AdditionalFileAnalyzerAction> AdditionalFileActions
        {
            get { return _additionalFileActions; }
        }

        internal readonly ImmutableArray<SemanticModelAnalyzerAction> SemanticModelActions
        {
            get { return _semanticModelActions; }
        }

        internal readonly ImmutableArray<SymbolAnalyzerAction> SymbolActions
        {
            get { return _symbolActions; }
        }

        internal readonly ImmutableArray<SymbolStartAnalyzerAction> SymbolStartActions
        {
            get { return _symbolStartActions; }
        }

        internal readonly ImmutableArray<SymbolEndAnalyzerAction> SymbolEndActions
        {
            get { return _symbolEndActions; }
        }

        internal readonly ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions
        {
            get { return _codeBlockEndActions; }
        }

        internal readonly ImmutableArray<CodeBlockAnalyzerAction> CodeBlockActions
        {
            get { return _codeBlockActions; }
        }

        internal readonly ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> GetCodeBlockStartActions<TLanguageKindEnum>() where TLanguageKindEnum : struct
        {
            return _codeBlockStartActions.OfType<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>().ToImmutableArray();
        }

        internal readonly void AddSyntaxNodeActions<TLanguageKindEnum>(
            ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> builder) where TLanguageKindEnum : struct
        {
            foreach (var action in _syntaxNodeActions)
            {
                if (action is SyntaxNodeAnalyzerAction<TLanguageKindEnum> stronglyTypedAction)
                    builder.Add(stronglyTypedAction);
            }
        }

        internal readonly void AddSyntaxNodeActions<TLanguageKindEnum>(
            DiagnosticAnalyzer analyzer,
            ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> builder) where TLanguageKindEnum : struct
        {
            foreach (var action in _syntaxNodeActions)
            {
                if (action.Analyzer == analyzer &&
                    action is SyntaxNodeAnalyzerAction<TLanguageKindEnum> syntaxNodeAction)
                {
                    builder.Add(syntaxNodeAction);
                }
            }
        }

        internal readonly ImmutableArray<OperationBlockAnalyzerAction> OperationBlockActions
        {
            get { return _operationBlockActions; }
        }

        internal readonly ImmutableArray<OperationBlockAnalyzerAction> OperationBlockEndActions
        {
            get { return _operationBlockEndActions; }
        }

        internal readonly ImmutableArray<OperationBlockStartAnalyzerAction> OperationBlockStartActions
        {
            get { return _operationBlockStartActions; }
        }

        internal readonly ImmutableArray<OperationAnalyzerAction> OperationActions
        {
            get { return _operationActions; }
        }

        internal void AddCompilationStartAction(CompilationStartAnalyzerAction action)
        {
            _compilationStartActions = _compilationStartActions.Add(action);
            IsEmpty = false;
        }

        internal void AddCompilationEndAction(CompilationAnalyzerAction action)
        {
            _compilationEndActions = _compilationEndActions.Add(action);
            IsEmpty = false;
        }

        internal void AddCompilationAction(CompilationAnalyzerAction action)
        {
            _compilationActions = _compilationActions.Add(action);
            IsEmpty = false;
        }

        internal void AddSyntaxTreeAction(SyntaxTreeAnalyzerAction action)
        {
            _syntaxTreeActions = _syntaxTreeActions.Add(action);
            IsEmpty = false;
        }

        internal void AddAdditionalFileAction(AdditionalFileAnalyzerAction action)
        {
            _additionalFileActions = _additionalFileActions.Add(action);
            IsEmpty = false;
        }

        internal void AddSemanticModelAction(SemanticModelAnalyzerAction action)
        {
            _semanticModelActions = _semanticModelActions.Add(action);
            IsEmpty = false;
        }

        internal void AddSymbolAction(SymbolAnalyzerAction action)
        {
            _symbolActions = _symbolActions.Add(action);
            IsEmpty = false;
        }

        internal void AddSymbolStartAction(SymbolStartAnalyzerAction action)
        {
            _symbolStartActions = _symbolStartActions.Add(action);
            IsEmpty = false;
        }

        internal void AddSymbolEndAction(SymbolEndAnalyzerAction action)
        {
            _symbolEndActions = _symbolEndActions.Add(action);
            IsEmpty = false;
        }

        internal void AddCodeBlockStartAction<TLanguageKindEnum>(CodeBlockStartAnalyzerAction<TLanguageKindEnum> action) where TLanguageKindEnum : struct
        {
            _codeBlockStartActions = _codeBlockStartActions.Add(action);
            IsEmpty = false;
        }

        internal void AddCodeBlockEndAction(CodeBlockAnalyzerAction action)
        {
            _codeBlockEndActions = _codeBlockEndActions.Add(action);
            IsEmpty = false;
        }

        internal void AddCodeBlockAction(CodeBlockAnalyzerAction action)
        {
            _codeBlockActions = _codeBlockActions.Add(action);
            IsEmpty = false;
        }

        internal void AddSyntaxNodeAction<TLanguageKindEnum>(SyntaxNodeAnalyzerAction<TLanguageKindEnum> action) where TLanguageKindEnum : struct
        {
            _syntaxNodeActions = _syntaxNodeActions.Add(action);
            IsEmpty = false;
        }

        internal void AddOperationBlockStartAction(OperationBlockStartAnalyzerAction action)
        {
            _operationBlockStartActions = _operationBlockStartActions.Add(action);
            IsEmpty = false;
        }

        internal void AddOperationBlockAction(OperationBlockAnalyzerAction action)
        {
            _operationBlockActions = _operationBlockActions.Add(action);
            IsEmpty = false;
        }

        internal void AddOperationBlockEndAction(OperationBlockAnalyzerAction action)
        {
            _operationBlockEndActions = _operationBlockEndActions.Add(action);
            IsEmpty = false;
        }

        internal void AddOperationAction(OperationAnalyzerAction action)
        {
            _operationActions = _operationActions.Add(action);
            IsEmpty = false;
        }

        internal void EnableConcurrentExecution()
        {
            _concurrent = true;
        }

        /// <summary>
        /// Append analyzer actions from <paramref name="otherActions"/> to actions from this instance.
        /// </summary>
        /// <param name="otherActions">Analyzer actions to append</param>.
        public readonly AnalyzerActions Append(in AnalyzerActions otherActions, bool appendSymbolStartAndSymbolEndActions = true)
        {
            if (otherActions.IsDefault)
            {
                throw new ArgumentNullException(nameof(otherActions));
            }

            AnalyzerActions actions = new AnalyzerActions(concurrent: _concurrent || otherActions.Concurrent);
            actions._compilationStartActions = _compilationStartActions.AddRange(otherActions._compilationStartActions);
            actions._compilationEndActions = _compilationEndActions.AddRange(otherActions._compilationEndActions);
            actions._compilationActions = _compilationActions.AddRange(otherActions._compilationActions);
            actions._syntaxTreeActions = _syntaxTreeActions.AddRange(otherActions._syntaxTreeActions);
            actions._additionalFileActions = _additionalFileActions.AddRange(otherActions._additionalFileActions);
            actions._semanticModelActions = _semanticModelActions.AddRange(otherActions._semanticModelActions);
            actions._symbolActions = _symbolActions.AddRange(otherActions._symbolActions);
            actions._symbolStartActions = appendSymbolStartAndSymbolEndActions ? _symbolStartActions.AddRange(otherActions._symbolStartActions) : _symbolStartActions;
            actions._symbolEndActions = appendSymbolStartAndSymbolEndActions ? _symbolEndActions.AddRange(otherActions._symbolEndActions) : _symbolEndActions;
            actions._codeBlockStartActions = _codeBlockStartActions.AddRange(otherActions._codeBlockStartActions);
            actions._codeBlockEndActions = _codeBlockEndActions.AddRange(otherActions._codeBlockEndActions);
            actions._codeBlockActions = _codeBlockActions.AddRange(otherActions._codeBlockActions);
            actions._syntaxNodeActions = _syntaxNodeActions.AddRange(otherActions._syntaxNodeActions);
            actions._operationActions = _operationActions.AddRange(otherActions._operationActions);
            actions._operationBlockStartActions = _operationBlockStartActions.AddRange(otherActions._operationBlockStartActions);
            actions._operationBlockEndActions = _operationBlockEndActions.AddRange(otherActions._operationBlockEndActions);
            actions._operationBlockActions = _operationBlockActions.AddRange(otherActions._operationBlockActions);
            actions.IsEmpty = IsEmpty && otherActions.IsEmpty;

            return actions;
        }
    }
}
