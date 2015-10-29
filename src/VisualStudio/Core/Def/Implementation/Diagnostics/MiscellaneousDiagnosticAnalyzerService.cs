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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [ExportIncrementalAnalyzerProvider(WorkspaceKind.MiscellaneousFiles)]
    [Shared]
    internal partial class MiscellaneousDiagnosticAnalyzerService : IIncrementalAnalyzerProvider, IDiagnosticUpdateSource
    {
        private readonly IDiagnosticAnalyzerService _analyzerService;

        [ImportingConstructor]
        public MiscellaneousDiagnosticAnalyzerService(IDiagnosticAnalyzerService analyzerService, IDiagnosticUpdateSourceRegistrationService registrationService)
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

            return new SyntaxOnlyDiagnosticAnalyzer(this, workspace);
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
            var handler = this.DiagnosticsUpdated;
            if (handler != null)
            {
                handler(this, state);
            }
        }

        private class SyntaxOnlyDiagnosticAnalyzer : IIncrementalAnalyzer
        {
            private readonly MiscellaneousDiagnosticAnalyzerService _service;
            private readonly Workspace _workspace;

            public SyntaxOnlyDiagnosticAnalyzer(MiscellaneousDiagnosticAnalyzerService service, Workspace workspace)
            {
                _service = service;
                _workspace = workspace;
            }

            public async Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                // if closed file diagnostic is off and document is not opened, then don't do anything
                if (!_workspace.Options.GetOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, document.Project.Language) && !document.IsOpen())
                {
                    return;
                }

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = tree.GetDiagnostics(cancellationToken);

                Contract.Requires(document.Project.Solution.Workspace == _workspace);

                var diagnosticData = diagnostics == null ? ImmutableArray<DiagnosticData>.Empty : diagnostics.Select(d => DiagnosticData.Create(document, d)).ToImmutableArrayOrEmpty();
                _service.RaiseDiagnosticsUpdated(
                    new DiagnosticsUpdatedArgs(new MiscUpdateArgsId(document.Id),
                    _workspace, document.Project.Solution, document.Project.Id, document.Id, diagnosticData,
                    DiagnosticsUpdatedKind.DiagnosticsCreated));
            }

            public void RemoveDocument(DocumentId documentId)
            {
                // a file is removed from misc project
                RaiseEmptyDiagnosticUpdated(documentId);
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                // no closed file diagnostic and file is not opened, remove any existing diagnostics
                if (!_workspace.Options.GetOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, document.Project.Language) && !document.IsOpen())
                {
                    RaiseEmptyDiagnosticUpdated(document.Id);
                }

                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return DocumentResetAsync(document, cancellationToken);
            }

            private void RaiseEmptyDiagnosticUpdated(DocumentId documentId)
            {
                _service.RaiseDiagnosticsUpdated(new DiagnosticsUpdatedArgs(ValueTuple.Create(this, documentId), _workspace, null, documentId.ProjectId, documentId, ImmutableArray<DiagnosticData>.Empty,
                    DiagnosticsUpdatedKind.DiagnosticsRemoved));
            }

            // method we don't care. misc project only supports syntax errors
            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return false;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public void RemoveProject(ProjectId projectId)
            {
            }

            private class MiscUpdateArgsId : BuildToolId.Base<DocumentId>, ISupportLiveUpdate
            {
                public MiscUpdateArgsId(DocumentId documentId) : base(documentId)
                {
                }

                public override string BuildTool
                {
                    get
                    {
                        return PredefinedBuildTools.Live;
                    }
                }

                public override bool Equals(object obj)
                {
                    var other = obj as MiscUpdateArgsId;
                    if (other == null)
                    {
                        return false;
                    }

                    return base.Equals(obj);
                }

                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }
            }
        }
    }
}
