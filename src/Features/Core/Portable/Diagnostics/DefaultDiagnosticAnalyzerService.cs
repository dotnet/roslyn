// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Shared]
    [ExportIncrementalAnalyzerProvider(WellKnownSolutionCrawlerAnalyzers.Diagnostic, workspaceKinds: null)]
    internal partial class DefaultDiagnosticAnalyzerService : IIncrementalAnalyzerProvider, IDiagnosticUpdateSource
    {
        private readonly IDiagnosticAnalyzerService _analyzerService;

        [ImportingConstructor]
        public DefaultDiagnosticAnalyzerService(
            IDiagnosticAnalyzerService analyzerService,
            IDiagnosticUpdateSourceRegistrationService registrationService)
        {
            _analyzerService = analyzerService;
            registrationService.Register(this);
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (!workspace.Options.GetOption(ServiceComponentOnOffOptions.DiagnosticProvider))
            {
                return null;
            }

            return new DefaultDiagnosticIncrementalAnalyzer(this, workspace);
        }

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;
        public event EventHandler DiagnosticsCleared { add { } remove { } }

        // this only support push model, pull model will be provided by DiagnosticService by caching everything this one pushed
        public bool SupportGetDiagnostics => false;

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            // pull model not supported
            return ImmutableArray<DiagnosticData>.Empty;
        }

        internal void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs state)
        {
            DiagnosticsUpdated?.Invoke(this, state);
        }

        private class DefaultDiagnosticIncrementalAnalyzer : IIncrementalAnalyzer
        {
            private readonly DefaultDiagnosticAnalyzerService _service;
            private readonly Workspace _workspace;

            public DefaultDiagnosticIncrementalAnalyzer(DefaultDiagnosticAnalyzerService service, Workspace workspace)
            {
                _service = service;
                _workspace = workspace;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                if (e.Option == InternalRuntimeDiagnosticOptions.Syntax ||
                    e.Option == InternalRuntimeDiagnosticOptions.Semantic ||
                    e.Option == InternalRuntimeDiagnosticOptions.ScriptSemantic)
                {
                    return true;
                }

                return false;
            }

            public async Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                Debug.Assert(document.Project.Solution.Workspace == _workspace);

                // right now, there is no way to observe diagnostics for closed file.
                if (!_workspace.IsDocumentOpen(document.Id) ||
                    !_workspace.Options.GetOption(InternalRuntimeDiagnosticOptions.Syntax))
                {
                    return;
                }

                await AnalyzeForKind(document, AnalysisKind.Syntax, cancellationToken).ConfigureAwait(false);
            }

            public async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                Debug.Assert(document.Project.Solution.Workspace == _workspace);

                if (!IsSemanticAnalysisOn())
                {
                    return;
                }

                await AnalyzeForKind(document, AnalysisKind.Semantic, cancellationToken).ConfigureAwait(false);

                bool IsSemanticAnalysisOn()
                {
                    // right now, there is no way to observe diagnostics for closed file.
                    if (!_workspace.IsDocumentOpen(document.Id))
                    {
                        return false;
                    }

                    if (_workspace.Options.GetOption(InternalRuntimeDiagnosticOptions.Semantic))
                    {
                        return true;
                    }

                    return _workspace.Options.GetOption(InternalRuntimeDiagnosticOptions.ScriptSemantic) && document.SourceCodeKind == SourceCodeKind.Script;
                }
            }

            private async Task AnalyzeForKind(Document document, AnalysisKind kind, CancellationToken cancellationToken)
            {
                var diagnosticData = await GetDiagnosticsAsync(document, kind, cancellationToken).ConfigureAwait(false);

                _service.RaiseDiagnosticsUpdated(
                    DiagnosticsUpdatedArgs.DiagnosticsCreated(new DefaultUpdateArgsId(_workspace.Kind, kind, document.Id),
                    _workspace, document.Project.Solution, document.Project.Id, document.Id, diagnosticData));
            }

            /// <summary>
            /// Get diagnostics for the given document.
            /// 
            /// This is a simple API to get all diagnostics for the given document.
            /// 
            /// The intended audience for this API is for ones that pefer simplicity over performance such as document that belong to misc project.
            /// this doesn't cache nor use cache for anything. it will re-caculate new diagnostics every time for the given document.
            /// it will not persist any data on disk nor use OOP to calcuate the data.
            /// 
            /// This should never be used when performance is a big concern. for such context, use much complex API from IDiagnosticAnalyzerService
            /// that provide all kinds of knobs/cache/persistency/OOP to get better perf over simplicity.
            /// </summary>
            private async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
               Document document, AnalysisKind kind, CancellationToken cancellationToken)
            {
                // given service must be DiagnosticAnalyzerService
                var diagnosticService = (DiagnosticAnalyzerService)_service._analyzerService;

                var analyzers = GetAnalyzers(diagnosticService, document.Project);

                var compilationWithAnalyzers = await diagnosticService.CreateCompilationWithAnalyzers(
                    document.Project, analyzers, includeSuppressedDiagnostics: false, logAggregator: null, cancellationToken).ConfigureAwait(false);

                var builder = ArrayBuilder<DiagnosticData>.GetInstance();
                foreach (var analyzer in analyzers)
                {
                    builder.AddRange(await diagnosticService.ComputeDiagnosticsAsync(
                        compilationWithAnalyzers, document, analyzer, kind, spanOpt: null, logAggregator: null, cancellationToken).ConfigureAwait(false));
                }

                return builder.ToImmutableAndFree();
            }

            private static IEnumerable<DiagnosticAnalyzer> GetAnalyzers(DiagnosticAnalyzerService service, Project project)
            {
                // C# or VB document that supports compiler
                var compilerAnalyzer = service.GetCompilerDiagnosticAnalyzer(project.Language);
                if (compilerAnalyzer != null)
                {
                    return SpecializedCollections.SingletonEnumerable(compilerAnalyzer);
                }

                // document that doesn't support compiler diagnostics such as FSharp or TypeScript
                return service.GetDiagnosticAnalyzers(project);
            }

            public void RemoveDocument(DocumentId documentId)
            {
                // a file is removed from a solution
                //
                // here syntax and semantic indicates type of errors not where it is originated from.
                // Option.Semantic or Option.ScriptSemantic indicates what kind of document we will produce semantic errors from.
                // Option.Semantic == true means we will generate semantic errors for all document type
                // Option.ScriptSemantic == true means we will generate semantic errors only for script document type
                // both of them at the end generates semantic errors
                RaiseEmptyDiagnosticUpdated(AnalysisKind.Syntax, documentId);
                RaiseEmptyDiagnosticUpdated(AnalysisKind.Semantic, documentId);
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                // no closed file diagnostic and file is not opened, remove any existing diagnostics
                RemoveDocument(document.Id);
                return Task.CompletedTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return DocumentResetAsync(document, cancellationToken);
            }

            private void RaiseEmptyDiagnosticUpdated(AnalysisKind kind, DocumentId documentId)
            {
                _service.RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                    new DefaultUpdateArgsId(_workspace.Kind, kind, documentId), _workspace, null, documentId.ProjectId, documentId));
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public void RemoveProject(ProjectId projectId)
            {
            }

            private class DefaultUpdateArgsId : BuildToolId.Base<int, DocumentId>, ISupportLiveUpdate
            {
                private readonly string _workspaceKind;

                public DefaultUpdateArgsId(string workspaceKind, AnalysisKind kind, DocumentId documentId) : base((int)kind, documentId)
                {
                    _workspaceKind = workspaceKind;
                }

                public override string BuildTool => PredefinedBuildTools.Live;

                public override bool Equals(object obj)
                {
                    if (!(obj is DefaultUpdateArgsId other))
                    {
                        return false;
                    }

                    return _workspaceKind == other._workspaceKind && base.Equals(obj);
                }

                public override int GetHashCode()
                {
                    return Hash.Combine(_workspaceKind.GetHashCode(), base.GetHashCode());
                }
            }
        }
    }
}
