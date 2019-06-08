// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
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
                var diagnosticData = await _service._analyzerService.GetDiagnosticsAsync(document, GetAnalyzers(), kind, cancellationToken).ConfigureAwait(false);

                _service.RaiseDiagnosticsUpdated(
                    DiagnosticsUpdatedArgs.DiagnosticsCreated((_workspace.Kind, kind, document.Id),
                    _workspace, document.Project.Solution, document.Project.Id, document.Id, PredefinedBuildTools.Live, diagnosticData.ToImmutableArrayOrEmpty()));

                IEnumerable<DiagnosticAnalyzer> GetAnalyzers()
                {
                    // C# or VB document that supports compiler
                    var compilerAnalyzer = _service._analyzerService.GetCompilerDiagnosticAnalyzer(document.Project.Language);
                    if (compilerAnalyzer != null)
                    {
                        return SpecializedCollections.SingletonEnumerable(compilerAnalyzer);
                    }

                    // document that doesn't support compiler diagnostics such as fsharp or typescript
                    return _service._analyzerService.GetDiagnosticAnalyzers(document.Project);
                }
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
                    (_workspace.Kind, kind, documentId), _workspace, solution: null, documentId.ProjectId, documentId, PredefinedBuildTools.Live));
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
        }
    }
}
