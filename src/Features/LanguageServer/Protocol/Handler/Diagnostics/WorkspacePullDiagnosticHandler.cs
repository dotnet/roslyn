// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Method(VSInternalMethods.WorkspacePullDiagnosticName)]
    internal sealed class WorkspacePullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport, VSInternalWorkspaceDiagnosticReport[]>
    {
        public WorkspacePullDiagnosticHandler(IDiagnosticAnalyzerService analyzerService, EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource, IGlobalOptionService globalOptions)
            : base(analyzerService, editAndContinueDiagnosticUpdateSource, globalOptions)
        {
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalWorkspaceDiagnosticsParams request)
            => null;

        protected override VSInternalWorkspaceDiagnosticReport CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
            => new VSInternalWorkspaceDiagnosticReport
            {
                TextDocument = identifier,
                Diagnostics = diagnostics,
                ResultId = resultId,
                // Mark these diagnostics as having come from us.  They will be superseded by any diagnostics for the
                // same file produced by the DocumentPullDiagnosticHandler.
                Identifier = WorkspaceDiagnosticIdentifier,
            };

        protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalWorkspaceDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PreviousResults?.Where(d => d.PreviousResultId != null).Select(d => new PreviousPullResult(d.PreviousResultId!, d.TextDocument!)).ToImmutableArray();

        protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
        {
            // All workspace diagnostics are potential duplicates given that they can be overridden by the diagnostics
            // produced by document diagnostics.
            return ConvertTags(diagnosticData, potentialDuplicate: true);
        }

        protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return GetWorkspacePullDocumentsAsync(context, GlobalOptions, cancellationToken);
        }

        protected override VSInternalWorkspaceDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalWorkspaceDiagnosticReport> progress)
        {
            return progress.GetValues();
        }

        internal static async ValueTask<ImmutableArray<IDiagnosticSource>> GetWorkspacePullDocumentsAsync(RequestContext context, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Solution);

            // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
            // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
            // document-diagnostics instead.
            if (context.ServerKind == WellKnownLspServerKinds.RazorLspServer)
                return ImmutableArray<IDiagnosticSource>.Empty;

            using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var result);

            var solution = context.Solution;

            var documentTrackingService = solution.Workspace.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get diagnostics for.  This will
            // prioritize the files from currently active projects, but then also include all other docs in all projects
            // (depending on current FSA settings).

            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            // Now, prioritize the projects related to the active/visible files.
            await AddDocumentsAndProject(activeDocument?.Project, context.SupportedLanguages, isOpen: true, cancellationToken).ConfigureAwait(false);
            foreach (var doc in visibleDocuments)
                await AddDocumentsAndProject(doc.Project, context.SupportedLanguages, isOpen: true, cancellationToken).ConfigureAwait(false);

            // finally, add the remainder of all documents.
            foreach (var project in solution.Projects)
                await AddDocumentsAndProject(project, context.SupportedLanguages, isOpen: false, cancellationToken).ConfigureAwait(false);

            // Ensure that we only process documents once.
            result.RemoveDuplicates();
            return result.ToImmutable();

            async Task AddDocumentsAndProject(Project? project, ImmutableArray<string> supportedLanguages, bool isOpen, CancellationToken cancellationToken)
            {
                if (project == null)
                    return;

                if (!supportedLanguages.Contains(project.Language))
                {
                    // This project is for a language not supported by the LSP server making the request.
                    // Do not report diagnostics for these projects.
                    return;
                }

                var isFSAOn = globalOptions.IsFullSolutionAnalysisEnabled(project.Language);
                var documents = ImmutableArray<Document>.Empty;
                // If FSA is on, then add all the documents in the project.  Other analysis scopes are handled by the document pull handler.
                if (isFSAOn)
                {
                    documents = documents.AddRange(project.Documents);
                }

                // If all features are enabled for source generated documents, make sure they are included when FSA is on or a file in the project is open.
                // This is done because for either scenario we've already run generators, so there shouldn't be much cost in getting the diagnostics.
                if ((isFSAOn || isOpen) && solution.Workspace.Services.GetService<IWorkspaceConfigurationService>()?.Options.EnableOpeningSourceGeneratedFiles == true)
                {
                    var sourceGeneratedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
                    documents = documents.AddRange(sourceGeneratedDocuments);
                }

                foreach (var document in documents)
                {
                    // Only consider closed documents here (and only open ones in the DocumentPullDiagnosticHandler).
                    // Each handler treats those as separate worlds that they are responsible for.
                    if (context.IsTracking(document.GetURI()))
                    {
                        context.TraceInformation($"Skipping tracked document: {document.GetURI()}");
                        continue;
                    }

                    // Do not attempt to get workspace diagnostics for Razor files, Razor will directly ask us for document diagnostics
                    // for any razor file they are interested in.
                    if (document.IsRazorDocument())
                    {
                        continue;
                    }

                    result.Add(new WorkspaceDocumentDiagnosticSource(document));
                }

                // Finally, if FSA is on we also want to check for diagnostics associated with the project itself.
                if (isFSAOn)
                {
                    result.Add(new ProjectDiagnosticSource(project));
                }
            }
        }

        private record struct ProjectDiagnosticSource(Project Project) : IDiagnosticSource
        {
            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
                IDiagnosticAnalyzerService diagnosticAnalyzerService,
                RequestContext context,
                DiagnosticMode diagnosticMode,
                CancellationToken cancellationToken)
            {
                // Directly use the IDiagnosticAnalyzerService.  This will use the actual snapshots
                // we're passing in.  If information is already cached for that snapshot, it will be returned.  Otherwise,
                // it will be computed on demand.  Because it is always accurate as per this snapshot, all spans are correct
                // and do not need to be adjusted.
                var projectDiagnostics = await diagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync(Project.Solution, Project.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
                return projectDiagnostics;
            }

            public ProjectOrDocumentId GetId() => new(Project.Id);

            public Project GetProject() => Project;

            public Uri GetUri()
            {
                Contract.ThrowIfNull(Project.FilePath);
                return ProtocolConversions.GetUriFromFilePath(Project.FilePath);
            }
        }

        private record struct WorkspaceDocumentDiagnosticSource(Document Document) : IDiagnosticSource
        {
            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
                IDiagnosticAnalyzerService diagnosticAnalyzerService,
                RequestContext context,
                DiagnosticMode diagnosticMode,
                CancellationToken cancellationToken)
            {
                if (Document is not SourceGeneratedDocument)
                {
                    // We call GetDiagnosticsForIdsAsync as we want to ensure we get the full set of diagnostics for this document
                    // including those reported as a compilation end diagnostic.  These are not included in document pull (uses GetDiagnosticsForSpan) due to cost.
                    // However we can include them as a part of workspace pull when FSA is on.
                    var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(Document.Project.Solution, Document.Project.Id, Document.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return documentDiagnostics;
                }
                else
                {
                    // Unfortunately GetDiagnosticsForIdsAsync returns nothing for source generated documents.
                    var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(Document, range: null, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return documentDiagnostics;
                }
            }

            public ProjectOrDocumentId GetId() => new(Document.Id);

            public Project GetProject() => Document.Project;

            public Uri GetUri() => Document.GetURI();
        }
    }
}
