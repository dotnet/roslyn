// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [Shared]
    [ExportIncrementalAnalyzerProvider(WellKnownSolutionCrawlerAnalyzers.Diagnostic, workspaceKinds: null)]
    internal partial class DefaultDiagnosticAnalyzerService : IIncrementalAnalyzerProvider, IDiagnosticUpdateSource
    {
        private const int Syntax = 1;
        private const int Semantic = 2;

        [ImportingConstructor]
        public DefaultDiagnosticAnalyzerService(IDiagnosticUpdateSourceRegistrationService registrationService)
        {
            registrationService.Register(this);
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (!workspace.Options.GetOption(ServiceComponentOnOffOptions.DiagnosticProvider))
            {
                return null;
            }

            return new CompilerDiagnosticAnalyzer(this, workspace);
        }

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        public bool SupportGetDiagnostics
        {
            get
            {
                // this only support push model, pull model will be provided by DiagnosticService by caching everything this one pushed
                return false;
            }
        }

        public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            // pull model not supported
            return ImmutableArray<DiagnosticData>.Empty;
        }

        internal void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs state)
        {
            this.DiagnosticsUpdated?.Invoke(this, state);
        }

        private class CompilerDiagnosticAnalyzer : IIncrementalAnalyzer
        {
            private readonly DefaultDiagnosticAnalyzerService _service;
            private readonly Workspace _workspace;

            public CompilerDiagnosticAnalyzer(DefaultDiagnosticAnalyzerService service, Workspace workspace)
            {
                _service = service;
                _workspace = workspace;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                if (e.Option == InternalRuntimeDiagnosticOptions.Syntax ||
                    e.Option == InternalRuntimeDiagnosticOptions.Semantic)
                {
                    return true;
                }

                return false;
            }

            public async Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                // right now, there is no way to observe diagnostics for closed file.
                if (!_workspace.IsDocumentOpen(document.Id) ||
                    !_workspace.Options.GetOption(InternalRuntimeDiagnosticOptions.Syntax))
                {
                    return;
                }

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = tree.GetDiagnostics(cancellationToken);

                Contract.Requires(document.Project.Solution.Workspace == _workspace);

                var diagnosticData = diagnostics == null ? ImmutableArray<DiagnosticData>.Empty : diagnostics.Select(d => DiagnosticData.Create(document, d)).ToImmutableArrayOrEmpty();

                _service.RaiseDiagnosticsUpdated(
                    DiagnosticsUpdatedArgs.DiagnosticsCreated(new DefaultUpdateArgsId(_workspace.Kind, Syntax, document.Id),
                    _workspace, document.Project.Solution, document.Project.Id, document.Id, diagnosticData));
            }

            public async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                // right now, there is no way to observe diagnostics for closed file.
                if (!_workspace.IsDocumentOpen(document.Id) ||
                    !_workspace.Options.GetOption(InternalRuntimeDiagnosticOptions.Semantic))
                {
                    return;
                }

                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = model.GetMethodBodyDiagnostics(span: null, cancellationToken: cancellationToken).Concat(
                                    model.GetDeclarationDiagnostics(span: null, cancellationToken: cancellationToken));

                Contract.Requires(document.Project.Solution.Workspace == _workspace);

                var diagnosticData = diagnostics == null ? ImmutableArray<DiagnosticData>.Empty : diagnostics.Select(d => DiagnosticData.Create(document, d)).ToImmutableArrayOrEmpty();

                _service.RaiseDiagnosticsUpdated(
                    DiagnosticsUpdatedArgs.DiagnosticsCreated(new DefaultUpdateArgsId(_workspace.Kind, Semantic, document.Id),
                    _workspace, document.Project.Solution, document.Project.Id, document.Id, diagnosticData));
            }

            public void RemoveDocument(DocumentId documentId)
            {
                // a file is removed from misc project
                RaiseEmptyDiagnosticUpdated(Syntax, documentId);
                RaiseEmptyDiagnosticUpdated(Semantic, documentId);
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                // no closed file diagnostic and file is not opened, remove any existing diagnostics
                RemoveDocument(document.Id);
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return DocumentResetAsync(document, cancellationToken);
            }

            private void RaiseEmptyDiagnosticUpdated(int kind, DocumentId documentId)
            {
                _service.RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                    new DefaultUpdateArgsId(_workspace.Kind, kind, documentId), _workspace, null, documentId.ProjectId, documentId));
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public void RemoveProject(ProjectId projectId)
            {
            }

            private class DefaultUpdateArgsId : BuildToolId.Base<int, DocumentId>, ISupportLiveUpdate
            {
                private readonly string _workspaceKind;

                public DefaultUpdateArgsId(string workspaceKind, int type, DocumentId documentId) : base(type, documentId)
                {
                    _workspaceKind = workspaceKind;
                }

                public override string BuildTool => PredefinedBuildTools.Live;

                public override bool Equals(object obj)
                {
                    var other = obj as DefaultUpdateArgsId;
                    if (other == null)
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
