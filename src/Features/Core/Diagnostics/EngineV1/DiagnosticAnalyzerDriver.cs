﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal class DiagnosticAnalyzerDriver
    {
        private readonly Document _document;

        // The root of the document.  May be null for documents without a root.
        private readonly SyntaxNode _root;

        // The span of the documents we want diagnostics for.  If null, then we want diagnostics 
        // for the entire file.
        private readonly TextSpan? _span;
        private readonly Project _project;
        private readonly CancellationToken _cancellationToken;
        private readonly ISyntaxNodeAnalyzerService _syntaxNodeAnalyzerService;
        private readonly Dictionary<SyntaxNode, ImmutableArray<SyntaxNode>> _descendantExecutableNodesMap;
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly IGeneratedCodeRecognitionService _generatedCodeService;
        private readonly IAnalyzerDriverService _analyzerDriverService;
        private readonly bool _testOnly_DonotCatchAnalyzerExceptions;

        private LogAggregator _logAggregator;

        private ImmutableArray<DeclarationInfo> _lazyDeclarationInfos;
        private ImmutableArray<ISymbol> _lazySymbols;
        private ImmutableArray<SyntaxNode> _lazyAllSyntaxNodesToAnalyze;

        private AnalyzerOptions _analyzerOptions = null;

        public DiagnosticAnalyzerDriver(Document document, TextSpan? span, SyntaxNode root, LogAggregator logAggregator, CancellationToken cancellationToken)
            : this(document, span, root, document.Project.LanguageServices.GetService<ISyntaxNodeAnalyzerService>(), cancellationToken)
        {
            _logAggregator = logAggregator;
        }

        public DiagnosticAnalyzerDriver(Project project, LogAggregator logAggregator, CancellationToken cancellationToken)
            : this(project, project.LanguageServices.GetService<ISyntaxNodeAnalyzerService>(), cancellationToken)
        {
            _logAggregator = logAggregator;
        }

        // internal for testing purposes
        internal DiagnosticAnalyzerDriver(
            Document document,
            TextSpan? span,
            SyntaxNode root,
            ISyntaxNodeAnalyzerService syntaxNodeAnalyzerService,
            CancellationToken cancellationToken,
            bool testOnly_DonotCatchAnalyzerExceptions = false)
        {
            _document = document;
            _span = span;
            _root = root;
            _project = document.Project;
            _syntaxNodeAnalyzerService = syntaxNodeAnalyzerService;
            _cancellationToken = cancellationToken;
            _descendantExecutableNodesMap = new Dictionary<SyntaxNode, ImmutableArray<SyntaxNode>>();
            _syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            _generatedCodeService = document.Project.Solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>();
            _analyzerDriverService = document.Project.LanguageServices.GetService<IAnalyzerDriverService>();
            _analyzerOptions = new WorkspaceAnalyzerOptions(_project.AnalyzerOptions, _project.Solution.Workspace);
            _testOnly_DonotCatchAnalyzerExceptions = testOnly_DonotCatchAnalyzerExceptions;
        }

        // internal for testing purposes
        internal DiagnosticAnalyzerDriver(Project project, ISyntaxNodeAnalyzerService syntaxNodeAnalyzerService, CancellationToken cancellationToken)
        {
            _project = project;
            _cancellationToken = cancellationToken;
            _syntaxNodeAnalyzerService = syntaxNodeAnalyzerService;
            _generatedCodeService = project.Solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>();
            _analyzerDriverService = project.LanguageServices.GetService<IAnalyzerDriverService>();
            _descendantExecutableNodesMap = null;
            _analyzerOptions = _project.AnalyzerOptions;
        }

        public Document Document
        {
            get
            {
                Contract.ThrowIfNull(_document);
                return _document;
            }
        }

        public TextSpan? Span
        {
            get
            {
                Contract.ThrowIfNull(_document);
                return _span;
            }
        }

        public Project Project
        {
            get
            {
                Contract.ThrowIfNull(_project);
                return _project;
            }
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return _cancellationToken;
            }
        }

        public ISyntaxNodeAnalyzerService SyntaxNodeAnalyzerService
        {
            get
            {
                return _syntaxNodeAnalyzerService;
            }
        }

        private ImmutableArray<DeclarationInfo> GetDeclarationInfos(SemanticModel model)
        {
            if (_lazyDeclarationInfos == null)
            {
                ImmutableArray<DeclarationInfo> declarations;
                if (_span == null)
                {
                    declarations = ImmutableArray<DeclarationInfo>.Empty;
                }
                else
                {
                    declarations = _analyzerDriverService.GetDeclarationsInSpan(model, _span.Value, getSymbol: true, cancellationToken: _cancellationToken);
                }

                _lazyDeclarationInfos = declarations;
            }

            return _lazyDeclarationInfos;
        }

        private ImmutableArray<ISymbol> GetSymbolsToAnalyze(SemanticModel model)
        {
            if (_lazySymbols == null)
            {
                var declarationInfos = GetDeclarationInfos(model);

                // Avoid duplicate callbacks for partial symbols by analyzing symbol only for the document with first declaring syntaxref.
                var builder = ImmutableHashSet.CreateBuilder<ISymbol>();

                for (var i = 0; i < declarationInfos.Length; i++)
                {
                    var symbol = declarationInfos[i].DeclaredSymbol;
                    if (symbol == null || symbol.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    if (symbol.DeclaringSyntaxReferences.Length > 1 &&
                        ShouldExcludePartialSymbol(symbol))
                    {
                        continue;
                    }

                    builder.Add(symbol);
                }

                ImmutableInterlocked.InterlockedInitialize(ref _lazySymbols, builder.ToImmutableArrayOrEmpty());
            }

            return _lazySymbols;
        }

        private bool ShouldExcludePartialSymbol(ISymbol symbol)
        {
            // Get the first partial definition in a non-generated file.
            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault(r => !IsInGeneratedCode(r));
            if (reference == null)
            {
                // All definitions in generated code, so just pick the first one.
                reference = symbol.DeclaringSyntaxReferences[0];
            }

            return reference.SyntaxTree != _root.SyntaxTree;
        }

        private bool IsInGeneratedCode(SyntaxReference reference)
        {
            if (_generatedCodeService == null)
            {
                // Test code might take this path.
                return false;
            }

            var tree = reference.SyntaxTree;
            var document = _project.GetDocument(reference.SyntaxTree);
            if (document == null)
            {
                return false;
            }

            return _generatedCodeService.IsGeneratedCode(document);
        }

        private ImmutableArray<SyntaxNode> GetSyntaxNodesToAnalyze()
        {
            if (_lazyAllSyntaxNodesToAnalyze == null)
            {
                if (_root == null || _span == null)
                {
                    return ImmutableArray<SyntaxNode>.Empty;
                }

                var node = _root.FindNode(_span.Value, findInsideTrivia: true);
                var descendantNodes = ImmutableArray.CreateRange(node.DescendantNodesAndSelf(descendIntoTrivia: true));
                ImmutableInterlocked.InterlockedInitialize(ref _lazyAllSyntaxNodesToAnalyze, descendantNodes);
            }

            return _lazyAllSyntaxNodesToAnalyze;
        }

        public async Task<ImmutableArray<Diagnostic>> GetSyntaxDiagnosticsAsync(DiagnosticAnalyzer analyzer)
        {
            var compilation = _document.Project.SupportsCompilation ? await _document.Project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false) : null;

            Contract.ThrowIfNull(_document);

            using (var pooledObject = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
            {
                var diagnostics = pooledObject.Object;

                _cancellationToken.ThrowIfCancellationRequested();
                var documentAnalyzer = analyzer as DocumentDiagnosticAnalyzer;
                if (documentAnalyzer != null)
                {
                    try
                    {
                        await documentAnalyzer.AnalyzeSyntaxAsync(_document, diagnostics.Add, _cancellationToken).ConfigureAwait(false);
                        return diagnostics.ToImmutableArrayOrEmpty();
                    }
                    catch (Exception e) when (CatchAnalyzerException(e, analyzer))
                    {
                        var exceptionDiagnostic = AnalyzerExceptionToDiagnostic(analyzer, e, _cancellationToken);
                        if (exceptionDiagnostic != null)
                        {
                            ReportAnalyzerExceptionDiagnostic(analyzer, exceptionDiagnostic, compilation);
                        }

                        return ImmutableArray<Diagnostic>.Empty;
                    }
                }

                var handler = AbstractHostDiagnosticUpdateSource.RegisterAnalyzerExceptionDiagnosticHandler(analyzer, this.Project);

                var analyzerActions = await this.GetAnalyzerActionsCoreAsync(analyzer).ConfigureAwait(false);

                DiagnosticAnalyzerLogger.UpdateAnalyzerTypeCount(analyzer, analyzerActions, (DiagnosticLogAggregator)_logAggregator);

                if (analyzerActions != null)
                {
                    if (_document.SupportsSyntaxTree)
                    {
                        AnalyzerDriverHelper.ExecuteSyntaxTreeActions(analyzerActions, _root.SyntaxTree,
                                _analyzerOptions, diagnostics.Add, CatchAnalyzerException, _cancellationToken);
                    }
                }

                if (diagnostics.Count == 0)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                AbstractHostDiagnosticUpdateSource.UnregisterAnalyzerExceptionDiagnosticHandler(handler);
                return GetFilteredDocumentDiagnostics(diagnostics, compilation).ToImmutableArray();
            }
        }

        private IEnumerable<Diagnostic> GetFilteredDocumentDiagnostics(IEnumerable<Diagnostic> diagnostics, Compilation compilation)
        {
            if (_root == null)
            {
                return diagnostics;
            }

            return GetFilteredDocumentDiagnosticsCore(diagnostics, compilation);
        }

        private IEnumerable<Diagnostic> GetFilteredDocumentDiagnosticsCore(IEnumerable<Diagnostic> diagnostics, Compilation compilation)
        {
            var diagsFilteredByLocation = diagnostics.Where(diagnostic => (diagnostic.Location == Location.None) ||
                        (diagnostic.Location.SourceTree == _root.SyntaxTree &&
                         (_span == null || diagnostic.Location.SourceSpan.IntersectsWith(_span.Value))));

            return compilation == null
                ? diagnostics
                : CompilationWithAnalyzers.GetEffectiveDiagnostics(diagsFilteredByLocation, compilation);
        }

        internal void ReportAnalyzerExceptionDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic exceptionDiagnostic, Compilation compilation)
        {
            Contract.ThrowIfFalse(AnalyzerDriverHelper.IsAnalyzerExceptionDiagnostic(exceptionDiagnostic));

            if (compilation != null)
            {
                var effectiveDiagnostic = CompilationWithAnalyzers.GetEffectiveDiagnostics(ImmutableArray.Create(exceptionDiagnostic), compilation).SingleOrDefault();
                if (effectiveDiagnostic == null)
                {
                    return;
                }
                else
                {
                    exceptionDiagnostic = effectiveDiagnostic;
                }
            }

            AbstractHostDiagnosticUpdateSource.ReportAnalyzerExceptionDiagnostic(this, analyzer, exceptionDiagnostic, this.Project);
        }

        public async Task<AnalyzerActions> GetAnalyzerActionsAsync(DiagnosticAnalyzer analyzer)
        {
            var handler = AbstractHostDiagnosticUpdateSource.RegisterAnalyzerExceptionDiagnosticHandler(analyzer, this.Project);
            var actions = await GetAnalyzerActionsCoreAsync(analyzer).ConfigureAwait(false);
            AbstractHostDiagnosticUpdateSource.UnregisterAnalyzerExceptionDiagnosticHandler(handler);
            return actions;
        }

        private async Task<AnalyzerActions> GetAnalyzerActionsCoreAsync(DiagnosticAnalyzer analyzer)
        {
            var compilation = _project.SupportsCompilation ?
                await _project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false) :
                null;
            var analyzerActions = await AnalyzerManager.Default.GetAnalyzerActionsAsync(analyzer, compilation, _analyzerOptions, CatchAnalyzerException, _cancellationToken).ConfigureAwait(false);
            DiagnosticAnalyzerLogger.UpdateAnalyzerTypeCount(analyzer, analyzerActions, (DiagnosticLogAggregator)_logAggregator);
            return analyzerActions;
        }

        public async Task<ImmutableArray<Diagnostic>> GetSemanticDiagnosticsAsync(DiagnosticAnalyzer analyzer)
        {
            var model = await _document.GetSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
            var compilation = model?.Compilation;

            Contract.ThrowIfNull(_document);

            using (var pooledObject = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
            {
                var diagnostics = pooledObject.Object;

                // Stateless semantic analyzers:
                //  1) ISemanticModelAnalyzer/IDocumentBasedDiagnosticAnalyzer
                //  2) ISymbolAnalyzer
                //  3) ISyntaxNodeAnalyzer

                _cancellationToken.ThrowIfCancellationRequested();

                var documentAnalyzer = analyzer as DocumentDiagnosticAnalyzer;
                if (documentAnalyzer != null)
                {
                    try
                    {
                        await documentAnalyzer.AnalyzeSemanticsAsync(_document, diagnostics.Add, _cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (CatchAnalyzerException(e, analyzer))
                    {
                        var exceptionDiagnostic = AnalyzerExceptionToDiagnostic(analyzer, e, _cancellationToken);
                        if (exceptionDiagnostic != null)
                        {
                            ReportAnalyzerExceptionDiagnostic(analyzer, exceptionDiagnostic, compilation);
                        }

                        return ImmutableArray<Diagnostic>.Empty;
                    }
                }
                else
                {
                    var handler = AbstractHostDiagnosticUpdateSource.RegisterAnalyzerExceptionDiagnosticHandler(analyzer, this.Project);

                    var analyzerActions = await GetAnalyzerActionsCoreAsync(analyzer).ConfigureAwait(false);
                    if (analyzerActions != null)
                    {
                        // SemanticModel actions.
                        if (analyzerActions.SemanticModelActionsCount > 0)
                        {
                            AnalyzerDriverHelper.ExecuteSemanticModelActions(analyzerActions, model, _analyzerOptions,
                                diagnostics.Add, CatchAnalyzerException, _cancellationToken);
                        }

                        // Symbol actions.
                        if (analyzerActions.SymbolActionsCount > 0)
                        {
                            var symbols = this.GetSymbolsToAnalyze(model);
                            AnalyzerDriverHelper.ExecuteSymbolActions(analyzerActions, symbols, compilation,
                                    _analyzerOptions, diagnostics.Add, CatchAnalyzerException, _cancellationToken);
                        }

                        if (this.SyntaxNodeAnalyzerService != null)
                        {
                            // SyntaxNode actions.
                            if (analyzerActions.SyntaxNodeActionsCount > 0)
                            {
                                this.SyntaxNodeAnalyzerService.ExecuteSyntaxNodeActions(analyzerActions, GetSyntaxNodesToAnalyze(), model,
                                    _analyzerOptions, diagnostics.Add, CatchAnalyzerException, _cancellationToken);
                            }

                            // CodeBlockStart, CodeBlockEnd, and generated SyntaxNode actions.
                            if (analyzerActions.CodeBlockStartActionsCount > 0 || analyzerActions.CodeBlockEndActionsCount > 0)
                            {
                                this.SyntaxNodeAnalyzerService.ExecuteCodeBlockActions(analyzerActions, this.GetDeclarationInfos(model), model,
                                    _analyzerOptions, diagnostics.Add, CatchAnalyzerException, _cancellationToken);
                            }
                        }
                    }

                    AbstractHostDiagnosticUpdateSource.UnregisterAnalyzerExceptionDiagnosticHandler(handler);
                }

                return GetFilteredDocumentDiagnostics(diagnostics, compilation).ToImmutableArray();
            }
        }

        public async Task<ImmutableArray<Diagnostic>> GetProjectDiagnosticsAsync(DiagnosticAnalyzer analyzer, Action<Project, DiagnosticAnalyzer, CancellationToken> forceAnalyzeAllDocuments)
        {
            Contract.ThrowIfNull(_project);
            Contract.ThrowIfFalse(_document == null);
            
            using (var diagnostics = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
            {
                if (_project.SupportsCompilation)
                {
                    await this.GetCompilationDiagnosticsAsync(analyzer, diagnostics.Object, forceAnalyzeAllDocuments).ConfigureAwait(false);
                }

                await this.GetProjectDiagnosticsWorkerAsync(analyzer, diagnostics.Object).ConfigureAwait(false);

                return diagnostics.Object.ToImmutableArray();
            }
        }

        private async Task GetProjectDiagnosticsWorkerAsync(DiagnosticAnalyzer analyzer, List<Diagnostic> diagnostics)
        {
            var projectAnalyzer = analyzer as ProjectDiagnosticAnalyzer;
            if (projectAnalyzer == null)
            {
                return;
            }

            try
            {
                await projectAnalyzer.AnalyzeProjectAsync(_project, diagnostics.Add, _cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (CatchAnalyzerException(e, analyzer))
            {
                var exceptionDiagnostic = AnalyzerExceptionToDiagnostic(analyzer, e, _cancellationToken);
                if (exceptionDiagnostic != null)
                {
                    var compilation = await _project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false);
                    ReportAnalyzerExceptionDiagnostic(analyzer, exceptionDiagnostic, compilation);
                }
            }
        }

        private async Task GetCompilationDiagnosticsAsync(DiagnosticAnalyzer analyzer, List<Diagnostic> diagnostics, Action<Project, DiagnosticAnalyzer, CancellationToken> forceAnalyzeAllDocuments)
        {
            Contract.ThrowIfFalse(_project.SupportsCompilation);

            using (var pooledObject = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
            {
                var localDiagnostics = pooledObject.Object;

                var handler = AbstractHostDiagnosticUpdateSource.RegisterAnalyzerExceptionDiagnosticHandler(analyzer, this.Project);

                // Get all the analyzer actions, including the per-compilation actions.
                var analyzerActions = await GetAnalyzerActionsCoreAsync(analyzer).ConfigureAwait(false);
                
                if (analyzerActions.CompilationEndActionsCount > 0 && analyzerActions.CompilationStartActionsCount > 0 && forceAnalyzeAllDocuments != null)
                {
                    if (analyzerActions.CodeBlockEndActionsCount > 0 ||
                        analyzerActions.CodeBlockStartActionsCount > 0 ||
                        analyzerActions.SemanticModelActionsCount > 0 ||
                        analyzerActions.SymbolActionsCount > 0 ||
                        analyzerActions.SyntaxNodeActionsCount > 0 ||
                        analyzerActions.SyntaxTreeActionsCount > 0)
                    {
                        // Analyzer registered a compilation end action and at least one other analyzer action during it's compilation start action.
                        // We need to ensure that we have force analyzed all documents in this project for this analyzer before executing the end actions.
                        forceAnalyzeAllDocuments(_project, analyzer, _cancellationToken);
                    }
                }

                // CompilationEnd actions.
                var compilation = await _project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false);
                AnalyzerDriverHelper.ExecuteCompilationEndActions(analyzerActions, compilation, _analyzerOptions, localDiagnostics.Add, CatchAnalyzerException, _cancellationToken);
                AbstractHostDiagnosticUpdateSource.UnregisterAnalyzerExceptionDiagnosticHandler(handler);

                var filteredDiagnostics = CompilationWithAnalyzers.GetEffectiveDiagnostics(localDiagnostics, compilation);
                diagnostics.AddRange(filteredDiagnostics);
            }
        }

        private static Diagnostic AnalyzerExceptionToDiagnostic(DiagnosticAnalyzer analyzer, Exception e, CancellationToken cancellationToken)
        {
            if (!IsCanceled(e, cancellationToken))
            {
                // Create a info diagnostic saying that the analyzer failed
                return AnalyzerDriverHelper.GetAnalyzerDiagnostic(analyzer, e);
            }

            return null;
        }

        private static bool IsCanceled(Exception e, CancellationToken cancellationToken)
        {
            var canceled = e as OperationCanceledException;
            return canceled != null && canceled.CancellationToken == cancellationToken;
        }

        private bool CatchAnalyzerException(Exception e, DiagnosticAnalyzer analyzer)
        {
            return CatchAnalyzerException(e, analyzer, _testOnly_DonotCatchAnalyzerExceptions);
        }

        internal bool CatchAnalyzerExceptionHandler(Exception e, DiagnosticAnalyzer analyzer)
        {
            return CatchAnalyzerException(e, analyzer, testOnly_DonotCatchAnalyzerExceptions: false);
        }

        private bool CatchAnalyzerException(Exception e, DiagnosticAnalyzer analyzer, bool testOnly_DonotCatchAnalyzerExceptions)
        {
            DiagnosticAnalyzerLogger.LogAnalyzerCrashCount(analyzer, e, _logAggregator);

            if (testOnly_DonotCatchAnalyzerExceptions)
            {
                return false;
            }

            if (AnalyzerHelper.IsBuiltInAnalyzer(analyzer))
            {
                return FatalError.ReportWithoutCrashUnlessCanceled(e);
            }

            return true;
        }
    }
}
