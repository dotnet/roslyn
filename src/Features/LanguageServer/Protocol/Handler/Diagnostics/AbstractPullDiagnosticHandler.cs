// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.LanguageServer.Features.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    /// <summary>
    /// Root type for both document and workspace diagnostic pull requests.
    /// </summary>
    /// <typeparam name="TDiagnosticsParams">The LSP input param type</typeparam>
    /// <typeparam name="TReport">The LSP type that is reported via IProgress</typeparam>
    /// <typeparam name="TReturn">The LSP type that is returned on completion of the request.</typeparam>
    internal abstract class AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn> : IRequestHandler<TDiagnosticsParams, TReturn?> where TDiagnosticsParams : IPartialResultParams<TReport[]>
    {
        /// <summary>
        /// Diagnostic mode setting for Razor.  This should always be <see cref="DiagnosticMode.Pull"/> as there is no push support in Razor.
        /// This option is only for passing to the diagnostics service and can be removed when we switch all of Roslyn to LSP pull.
        /// </summary>
        private static readonly Option2<DiagnosticMode> s_razorDiagnosticMode = new(nameof(InternalDiagnosticsOptions), "RazorDiagnosticMode", defaultValue: DiagnosticMode.Pull);

        /// <summary>
        /// Diagnostic mode setting for Live Share.  This should always be <see cref="DiagnosticMode.Pull"/> as there is no push support in Live Share.
        /// This option is only for passing to the diagnostics service and can be removed when we switch all of Roslyn to LSP pull.
        /// </summary>
        private static readonly Option2<DiagnosticMode> s_liveShareDiagnosticMode = new(nameof(InternalDiagnosticsOptions), "LiveShareDiagnosticMode", defaultValue: DiagnosticMode.Pull);

        /// <summary>
        /// Special value we use to designate workspace diagnostics vs document diagnostics.  Document diagnostics
        /// should always <see cref="VSInternalDiagnosticReport.Supersedes"/> a workspace diagnostic as the former are 'live'
        /// while the latter are cached and may be stale.
        /// </summary>
        protected const int WorkspaceDiagnosticIdentifier = 1;
        protected const int DocumentDiagnosticIdentifier = 2;

        private readonly EditAndContinueDiagnosticUpdateSource _editAndContinueDiagnosticUpdateSource;
        protected readonly IGlobalOptionService GlobalOptions;

        protected readonly IDiagnosticAnalyzerService DiagnosticAnalyzerService;

        /// <summary>
        /// Cache where we store the data produced by prior requests so that they can be returned if nothing of significance 
        /// changed. The VersionStamp is produced by <see cref="Project.GetDependentVersionAsync(CancellationToken)"/> while the 
        /// Checksum is produced by <see cref="Project.GetDependentChecksumAsync(CancellationToken)"/>.  The former is faster
        /// and works well for us in the normal case.  The latter still allows us to reuse diagnostics when changes happen that
        /// update the version stamp but not the content (for example, forking LSP text).
        /// </summary>
        private readonly VersionedPullCache<(int, VersionStamp?), (int, Checksum)> _versionedCache;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        protected AbstractPullDiagnosticHandler(
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource,
            IGlobalOptionService globalOptions)
        {
            DiagnosticAnalyzerService = diagnosticAnalyzerService;
            _editAndContinueDiagnosticUpdateSource = editAndContinueDiagnosticUpdateSource;
            GlobalOptions = globalOptions;
            _versionedCache = new(this.GetType().Name);
        }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Retrieve the previous results we reported.  Used so we can avoid resending data for unchanged files. Also
        /// used so we can report which documents were removed and can have all their diagnostics cleared.
        /// </summary>
        protected abstract ImmutableArray<PreviousPullResult>? GetPreviousResults(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Returns all the documents that should be processed in the desired order to process them in.
        /// </summary>
        protected abstract ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the appropriate LSP type to report a new set of diagnostics and resultId.
        /// </summary>
        protected abstract TReport CreateReport(TextDocumentIdentifier identifier, LSP.Diagnostic[] diagnostics, string resultId);

        /// <summary>
        /// Creates the appropriate LSP type to report unchanged diagnostics.
        /// </summary>
        protected abstract TReport CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId);

        /// <summary>
        /// Creates the appropriate LSP type to report a removed file.
        /// </summary>
        protected abstract TReport CreateRemovedReport(TextDocumentIdentifier identifier);

        protected abstract TReturn? CreateReturn(BufferedProgress<TReport> progress);

        /// <summary>
        /// Generate the right diagnostic tags for a particular diagnostic.
        /// </summary>
        protected abstract DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData);

        public async Task<TReturn?> HandleRequestAsync(
            TDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
        {
            context.TraceInformation($"{this.GetType()} started getting diagnostics");

            var diagnosticMode = GetDiagnosticMode(context);
            // For this handler to be called, we must have already checked the diagnostic mode
            // and set the appropriate capabilities.
            Contract.ThrowIfFalse(diagnosticMode == DiagnosticMode.Pull, $"{diagnosticMode} is not pull");

            // The progress object we will stream reports to.
            using var progress = BufferedProgress.Create(diagnosticsParams.PartialResultToken);

            // Get the set of results the request said were previously reported.  We can use this to determine both
            // what to skip, and what files we have to tell the client have been removed.
            var previousResults = GetPreviousResults(diagnosticsParams) ?? ImmutableArray<PreviousPullResult>.Empty;
            context.TraceInformation($"previousResults.Length={previousResults.Length}");

            // Create a mapping from documents to the previous results the client says it has for them.  That way as we
            // process documents we know if we should tell the client it should stay the same, or we can tell it what
            // the updated diagnostics are.
            var documentToPreviousDiagnosticParams = GetIdToPreviousDiagnosticParams(context, previousResults, out var removedResults);

            // First, let the client know if any workspace documents have gone away.  That way it can remove those for
            // the user from squiggles or error-list.
            HandleRemovedDocuments(context, removedResults, progress);

            // Next process each file in priority order. Determine if diagnostics are changed or unchanged since the
            // last time we notified the client.  Report back either to the client so they can update accordingly.
            var orderedSources = await GetOrderedDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);
            context.TraceInformation($"Processing {orderedSources.Length} documents");

            foreach (var diagnosticSource in orderedSources)
            {
                var encVersion = _editAndContinueDiagnosticUpdateSource.Version;

                var project = diagnosticSource.GetProject();
                var newResultId = await _versionedCache.GetNewResultIdAsync(
                    documentToPreviousDiagnosticParams,
                    diagnosticSource.GetId(),
                    project,
                    computeCheapVersionAsync: async () => (encVersion, await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false)),
                    computeExpensiveVersionAsync: async () => (encVersion, await project.GetDependentChecksumAsync(cancellationToken).ConfigureAwait(false)),
                    cancellationToken).ConfigureAwait(false);
                if (newResultId != null)
                {
                    progress.Report(await ComputeAndReportCurrentDiagnosticsAsync(context, diagnosticSource, newResultId, context.ClientCapabilities, diagnosticMode, cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    context.TraceInformation($"Diagnostics were unchanged for document: {diagnosticSource.GetUri()}");

                    // Nothing changed between the last request and this one.  Report a (null-diagnostics,
                    // same-result-id) response to the client as that means they should just preserve the current
                    // diagnostics they have for this file.
                    var previousParams = documentToPreviousDiagnosticParams[diagnosticSource.GetId()];
                    progress.Report(CreateUnchangedReport(previousParams.TextDocument, previousParams.PreviousResultId));
                }
            }

            // If we had a progress object, then we will have been reporting to that.  Otherwise, take what we've been
            // collecting and return that.
            context.TraceInformation($"{this.GetType()} finished getting diagnostics");
            return CreateReturn(progress);
        }

        private static Dictionary<ProjectOrDocumentId, PreviousPullResult> GetIdToPreviousDiagnosticParams(
            RequestContext context, ImmutableArray<PreviousPullResult> previousResults, out ImmutableArray<PreviousPullResult> removedDocuments)
        {
            Contract.ThrowIfNull(context.Solution);

            var result = new Dictionary<ProjectOrDocumentId, PreviousPullResult>();
            using var _ = ArrayBuilder<PreviousPullResult>.GetInstance(out var removedDocumentsBuilder);
            foreach (var diagnosticParams in previousResults)
            {
                if (diagnosticParams.TextDocument != null)
                {
                    var id = GetIdForPreviousResult(diagnosticParams.TextDocument, context.Solution);
                    if (id != null)
                    {
                        result[id.Value] = diagnosticParams;
                    }
                    else
                    {
                        // The client previously had a result from us for this document, but we no longer have it in our solution.
                        // Record it so we can report to the client that it has been removed.
                        removedDocumentsBuilder.Add(diagnosticParams);
                    }
                }
            }

            removedDocuments = removedDocumentsBuilder.ToImmutable();
            return result;

            static ProjectOrDocumentId? GetIdForPreviousResult(TextDocumentIdentifier textDocumentIdentifier, Solution solution)
            {
                var document = solution.GetDocument(textDocumentIdentifier);
                if (document != null)
                {
                    return new ProjectOrDocumentId(document.Id);
                }

                var project = solution.GetProject(textDocumentIdentifier);
                if (project != null)
                {
                    return new ProjectOrDocumentId(project.Id);
                }

                var additionalDocument = solution.GetAdditionalDocument(textDocumentIdentifier);
                if (additionalDocument != null)
                {
                    return new ProjectOrDocumentId(additionalDocument.Id);
                }

                return null;
            }
        }

        private DiagnosticMode GetDiagnosticMode(RequestContext context)
        {
            var diagnosticModeOption = context.ServerKind switch
            {
                WellKnownLspServerKinds.LiveShareLspServer => s_liveShareDiagnosticMode,
                WellKnownLspServerKinds.RazorLspServer => s_razorDiagnosticMode,
                _ => InternalDiagnosticsOptions.NormalDiagnosticMode,
            };

            var diagnosticMode = GlobalOptions.GetDiagnosticMode(diagnosticModeOption);
            return diagnosticMode;
        }

        private async Task<TReport> ComputeAndReportCurrentDiagnosticsAsync(
            RequestContext context,
            IDiagnosticSource diagnosticSource,
            string resultId,
            ClientCapabilities clientCapabilities,
            DiagnosticMode diagnosticMode,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<LSP.Diagnostic>.GetInstance(out var result);
            var diagnostics = await diagnosticSource.GetDiagnosticsAsync(DiagnosticAnalyzerService, context, diagnosticMode, cancellationToken).ConfigureAwait(false);
            context.TraceInformation($"Found {diagnostics.Length} diagnostics for {diagnosticSource.GetUri()}");

            foreach (var diagnostic in diagnostics)
                result.AddRange(ConvertDiagnostic(diagnosticSource, diagnostic, clientCapabilities));

            return CreateReport(new LSP.TextDocumentIdentifier { Uri = diagnosticSource.GetUri() }, result.ToArray(), resultId);
        }

        private void HandleRemovedDocuments(RequestContext context, ImmutableArray<PreviousPullResult> removedPreviousResults, BufferedProgress<TReport> progress)
        {
            foreach (var removedResult in removedPreviousResults)
            {
                context.TraceInformation($"Clearing diagnostics for removed document: {removedResult.TextDocument.Uri}");

                // Client is asking server about a document that no longer exists (i.e. was removed/deleted from
                // the workspace). Report a (null-diagnostics, null-result-id) response to the client as that
                // means they should just consider the file deleted and should remove all diagnostics
                // information they've cached for it.
                progress.Report(CreateRemovedReport(removedResult.TextDocument));
            }
        }

        private ImmutableArray<LSP.Diagnostic> ConvertDiagnostic(IDiagnosticSource diagnosticSource, DiagnosticData diagnosticData, ClientCapabilities capabilities)
        {
            var project = diagnosticSource.GetProject();
            var diagnostic = CreateLspDiagnostic(diagnosticData, project, capabilities);

            // Check if we need to handle the unnecessary tag (fading).
            if (!diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
            {
                return ImmutableArray.Create<LSP.Diagnostic>(diagnostic);
            }

            // DiagnosticId supports fading, check if the corresponding VS option is turned on.
            if (!SupportsFadingOption(diagnosticData))
            {
                return ImmutableArray.Create<LSP.Diagnostic>(diagnostic);
            }

            // Check to see if there are specific locations marked to fade.
            if (!diagnosticData.TryGetUnnecessaryDataLocations(out var unnecessaryLocations))
            {
                // There are no specific fading locations, just mark the whole diagnostic span as unnecessary.
                // We should always have at least one tag (build or intellisense error).
                Contract.ThrowIfNull(diagnostic.Tags, $"diagnostic {diagnostic.Identifier} was missing tags");
                diagnostic.Tags = diagnostic.Tags.Append(DiagnosticTag.Unnecessary);
                return ImmutableArray.Create<LSP.Diagnostic>(diagnostic);
            }

            // Roslyn produces unnecessary diagnostics by using additional locations, however LSP doesn't support tagging
            // additional locations separately.  Instead we just create multiple hidden diagnostics for unnecessary squiggling.
            using var _ = ArrayBuilder<LSP.Diagnostic>.GetInstance(out var diagnosticsBuilder);
            diagnosticsBuilder.Add(diagnostic);
            foreach (var location in unnecessaryLocations)
            {
                var additionalDiagnostic = CreateLspDiagnostic(diagnosticData, project, capabilities);
                additionalDiagnostic.Severity = LSP.DiagnosticSeverity.Hint;
                additionalDiagnostic.Range = GetRange(location);
                additionalDiagnostic.Tags = new DiagnosticTag[] { DiagnosticTag.Unnecessary, VSDiagnosticTags.HiddenInEditor, VSDiagnosticTags.HiddenInErrorList, VSDiagnosticTags.SuppressEditorToolTip };
                diagnosticsBuilder.Add(additionalDiagnostic);
            }

            return diagnosticsBuilder.ToImmutableArray();

            LSP.VSDiagnostic CreateLspDiagnostic(
                DiagnosticData diagnosticData,
                Project project,
                ClientCapabilities capabilities)
            {
                Contract.ThrowIfNull(diagnosticData.Message, $"Got a document diagnostic that did not have a {nameof(diagnosticData.Message)}");

                // We can just use VSDiagnostic as it doesn't have any default properties set that
                // would get automatically serialized.
                var diagnostic = new LSP.VSDiagnostic
                {
                    Source = "Roslyn",
                    Code = diagnosticData.Id,
                    CodeDescription = ProtocolConversions.HelpLinkToCodeDescription(diagnosticData.GetValidHelpLinkUri()),
                    Message = diagnosticData.Message,
                    Severity = ConvertDiagnosticSeverity(diagnosticData.Severity),
                    Tags = ConvertTags(diagnosticData),
                };
                if (diagnosticData.DataLocation != null)
                {
                    diagnostic.Range = GetRange(diagnosticData.DataLocation);
                }

                if (capabilities.HasVisualStudioLspCapability())
                {
                    diagnostic.DiagnosticType = diagnosticData.Category;
                    diagnostic.Projects = new[]
                    {
                        new VSDiagnosticProjectInformation
                        {
                            ProjectIdentifier = project.Id.Id.ToString(),
                            ProjectName = project.Name,
                        },
                    };
                }

                return diagnostic;
            }

            static LSP.Range GetRange(DiagnosticDataLocation dataLocation)
            {
                // We currently do not map diagnostics spans as
                //   1.  Razor handles span mapping for razor files on their side.
                //   2.  LSP does not allow us to report document pull diagnostics for a different file path.
                //   3.  The VS LSP client does not support document pull diagnostics for files outside our content type.
                //   4.  This matches classic behavior where we only squiggle the original location anyway.

                // We also do not adjust the diagnostic locations to ensure they are in bounds because we've
                // explicitly requested up to date diagnostics as of the snapshot we were passed in.
                return new LSP.Range
                {
                    Start = new Position
                    {
                        Character = dataLocation.OriginalStartColumn,
                        Line = dataLocation.OriginalStartLine,
                    },
                    End = new Position
                    {
                        Character = dataLocation.OriginalEndColumn,
                        Line = dataLocation.OriginalEndLine,
                    }
                };
            }
        }

        private static LSP.DiagnosticSeverity ConvertDiagnosticSeverity(DiagnosticSeverity severity)
            => severity switch
            {
                // Hidden is translated in ConvertTags to pass along appropriate _ms tags
                // that will hide the item in a client that knows about those tags.
                DiagnosticSeverity.Hidden => LSP.DiagnosticSeverity.Hint,
                DiagnosticSeverity.Info => LSP.DiagnosticSeverity.Hint,
                DiagnosticSeverity.Warning => LSP.DiagnosticSeverity.Warning,
                DiagnosticSeverity.Error => LSP.DiagnosticSeverity.Error,
                _ => throw ExceptionUtilities.UnexpectedValue(severity),
            };

        /// <summary>
        /// If you make change in this method, please also update the corresponding file in
        /// src\VisualStudio\Xaml\Impl\Implementation\LanguageServer\Handler\Diagnostics\AbstractPullDiagnosticHandler.cs
        /// </summary>
        protected static DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool potentialDuplicate)
        {
            using var _ = ArrayBuilder<DiagnosticTag>.GetInstance(out var result);

            if (diagnosticData.Severity == DiagnosticSeverity.Hidden)
            {
                result.Add(VSDiagnosticTags.HiddenInEditor);
                result.Add(VSDiagnosticTags.HiddenInErrorList);
                result.Add(VSDiagnosticTags.SuppressEditorToolTip);
            }
            else
            {
                result.Add(VSDiagnosticTags.VisibleInErrorList);
            }

            if (potentialDuplicate)
                result.Add(VSDiagnosticTags.PotentialDuplicate);

            result.Add(diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Build)
                ? VSDiagnosticTags.BuildError
                : VSDiagnosticTags.IntellisenseError);

            if (diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.EditAndContinue))
                result.Add(VSDiagnosticTags.EditAndContinueError);

            return result.ToArray();
        }

        private bool SupportsFadingOption(DiagnosticData diagnosticData)
        {
            if (IDEDiagnosticIdToOptionMappingHelper.TryGetMappedFadingOption(diagnosticData.Id, out var fadingOption))
            {
                Contract.ThrowIfNull(diagnosticData.Language, $"diagnostic {diagnosticData.Id} is missing a language");
                return GlobalOptions.GetOption(fadingOption, diagnosticData.Language);
            }

            return true;
        }
    }
}
