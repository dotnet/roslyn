// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Features.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

/// <summary>
/// Root type for both document and workspace diagnostic pull requests.
/// </summary>
/// <typeparam name="TDiagnosticsParams">The LSP input param type</typeparam>
/// <typeparam name="TReport">The LSP type that is reported via IProgress</typeparam>
/// <typeparam name="TReturn">The LSP type that is returned on completion of the request.</typeparam>
internal abstract partial class AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>(
    IDiagnosticAnalyzerService diagnosticAnalyzerService,
    IDiagnosticsRefresher diagnosticRefresher,
    IGlobalOptionService globalOptions)
    : ILspServiceRequestHandler<TDiagnosticsParams, TReturn?>
    where TDiagnosticsParams : IPartialResultParams<TReport>
{
    /// <summary>
    /// Special value we use to designate workspace diagnostics vs document diagnostics.  Document diagnostics
    /// should always <see cref="VSInternalDiagnosticReport.Supersedes"/> a workspace diagnostic as the former are 'live'
    /// while the latter are cached and may be stale.
    /// </summary>
    protected const int WorkspaceDiagnosticIdentifier = 1;
    protected const int DocumentDiagnosticIdentifier = 2;

    private readonly IDiagnosticsRefresher _diagnosticRefresher = diagnosticRefresher;

    protected readonly IGlobalOptionService GlobalOptions = globalOptions;
    protected readonly IDiagnosticAnalyzerService DiagnosticAnalyzerService = diagnosticAnalyzerService;

    /// <summary>
    /// Cache where we store the data produced by prior requests so that they can be returned if nothing of significance 
    /// changed. The <see cref="VersionStamp"/> is produced by <see cref="Project.GetDependentVersionAsync(CancellationToken)"/> while the 
    /// <see cref="Checksum"/> is produced by <see cref="Project.GetDependentChecksumAsync(CancellationToken)"/>.  The former is faster
    /// and works well for us in the normal case.  The latter still allows us to reuse diagnostics when changes happen that
    /// update the version stamp but not the content (for example, forking LSP text).
    /// </summary>
    private readonly ConcurrentDictionary<string, VersionedPullCache<(int globalStateVersion, VersionStamp? dependentVersion), (int globalStateVersion, Checksum dependentChecksum)>> _categoryToVersionedCache = [];

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    /// <summary>
    /// Retrieve the previous results we reported.  Used so we can avoid resending data for unchanged files. Also
    /// used so we can report which documents were removed and can have all their diagnostics cleared.
    /// </summary>
    protected abstract ImmutableArray<PreviousPullResult>? GetPreviousResults(TDiagnosticsParams diagnosticsParams);

    /// <summary>
    /// Returns all the documents that should be processed in the desired order to process them in.
    /// </summary>
    protected abstract ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(
        TDiagnosticsParams diagnosticsParams, string? requestDiagnosticCategory, RequestContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the appropriate LSP type to report a new set of diagnostics and resultId.
    /// </summary>
    protected abstract TReport CreateReport(TextDocumentIdentifier identifier, LSP.Diagnostic[] diagnostics, string resultId);

    /// <summary>
    /// Creates the appropriate LSP type to report unchanged diagnostics. Can return <see langword="false"/> to
    /// indicate nothing should be reported.  This should be done for workspace requests to avoiding sending a huge
    /// amount of "nothing changed" responses for most files.
    /// </summary>
    protected abstract bool TryCreateUnchangedReport(TextDocumentIdentifier identifier, string resultId, [NotNullWhen(true)] out TReport? report);

    /// <summary>
    /// Creates the appropriate LSP type to report a removed file.
    /// </summary>
    protected abstract TReport CreateRemovedReport(TextDocumentIdentifier identifier);

    protected abstract TReturn? CreateReturn(BufferedProgress<TReport> progress);

    /// <summary>
    /// Generate the right diagnostic tags for a particular diagnostic.
    /// </summary>
    protected abstract DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool isLiveSource);

    protected abstract string? GetRequestDiagnosticCategory(TDiagnosticsParams diagnosticsParams);

    /// <summary>
    /// Used by public workspace pull diagnostics to allow it to keep the connection open until
    /// changes occur to avoid the client spamming the server with requests.
    /// </summary>
    protected virtual Task WaitForChangesAsync(string? category, RequestContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<TReturn?> HandleRequestAsync(
        TDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
    {
        // The progress object we will stream reports to.
        using var progress = BufferedProgress.Create(diagnosticsParams.PartialResultToken);

        // We only support this option to disable crawling in internal speedometer and ddrit perf runs to lower
        // noise.  It is not exposed to the user.
        if (!this.GlobalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
        {
            context.TraceInformation($"{this.GetType()}. Skipping due to {nameof(SolutionCrawlerRegistrationService.EnableSolutionCrawler)}={false}");
        }
        else
        {
            Contract.ThrowIfNull(context.Solution);

            var clientCapabilities = context.GetRequiredClientCapabilities();
            var category = GetRequestDiagnosticCategory(diagnosticsParams);
            var handlerName = $"{this.GetType().Name}(category: {category})";
            context.TraceInformation($"{handlerName} started getting diagnostics");

            var versionedCache = _categoryToVersionedCache.GetOrAdd(handlerName, static handlerName => new(handlerName));

            // Get the set of results the request said were previously reported.  We can use this to determine both
            // what to skip, and what files we have to tell the client have been removed.
            var previousResults = GetPreviousResults(diagnosticsParams) ?? [];
            context.TraceInformation($"previousResults.Length={previousResults.Length}");

            // Create a mapping from documents to the previous results the client says it has for them.  That way as we
            // process documents we know if we should tell the client it should stay the same, or we can tell it what
            // the updated diagnostics are.
            using var _1 = PooledDictionary<ProjectOrDocumentId, PreviousPullResult>.GetInstance(out var documentIdToPreviousDiagnosticParams);
            using var _2 = PooledHashSet<PreviousPullResult>.GetInstance(out var removedDocuments);
            ProcessPreviousResults(context.Solution, previousResults, documentIdToPreviousDiagnosticParams, removedDocuments);

            // First, let the client know if any workspace documents have gone away.  That way it can remove those for
            // the user from squiggles or error-list.
            HandleRemovedDocuments(context, removedDocuments, progress);

            // Next process each file in priority order. Determine if diagnostics are changed or unchanged since the
            // last time we notified the client.  Report back either to the client so they can update accordingly.
            var orderedSources = await GetOrderedDiagnosticSourcesAsync(
                diagnosticsParams, category, context, cancellationToken).ConfigureAwait(false);

            context.TraceInformation($"Processing {orderedSources.Length} documents");

            // Keep track of what diagnostic sources we see this time around.  For any we do not see this time
            // around, we'll notify the client that the diagnostics for it have been removed.
            using var _3 = PooledHashSet<ProjectOrDocumentId>.GetInstance(out var seenDiagnosticSourceIds);

            foreach (var diagnosticSource in orderedSources)
            {
                seenDiagnosticSourceIds.Add(diagnosticSource.GetId());
                var globalStateVersion = _diagnosticRefresher.GlobalStateVersion;

                var project = diagnosticSource.GetProject();

                var newResultId = await versionedCache.GetNewResultIdAsync(
                    documentIdToPreviousDiagnosticParams,
                    diagnosticSource.GetId(),
                    project,
                    computeCheapVersionAsync: async () => (globalStateVersion, await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false)),
                    computeExpensiveVersionAsync: async () => (globalStateVersion, await project.GetDependentChecksumAsync(cancellationToken).ConfigureAwait(false)),
                    cancellationToken).ConfigureAwait(false);
                if (newResultId != null)
                {
                    await ComputeAndReportCurrentDiagnosticsAsync(
                        context, diagnosticSource, progress, newResultId, clientCapabilities, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    context.TraceInformation($"Diagnostics were unchanged for {diagnosticSource.ToDisplayString()}");

                    // Nothing changed between the last request and this one.  Report a (null-diagnostics,
                    // same-result-id) response to the client as that means they should just preserve the current
                    // diagnostics they have for this file.
                    //
                    // Note: if this is a workspace request, we can do nothing, as that will be interpreted by the
                    // client as nothing having been changed for that document.
                    var previousParams = documentIdToPreviousDiagnosticParams[diagnosticSource.GetId()];
                    if (TryCreateUnchangedReport(previousParams.TextDocument, previousParams.PreviousResultId, out var report))
                        progress.Report(report);
                }
            }

            // Now, for any diagnostics reported from a prior source that we do not see this time around, report its
            // diagnostics as being removed. This allows for different sets of diagnostic-sources to be computed
            // each time around, while still producing accurate diagnostic reports.
            //
            // Only do this if we haven't already created a removal report for that prior result above.
            //
            // Note: we are intentionally notifying the client that this is not a remove (vs an empty set of
            // results). As far as we and the client are concerned, this document no longer exists at this point
            // for the purposes of diagnostics.
            foreach (var (projectOrDocumentId, previousDiagnosticParams) in documentIdToPreviousDiagnosticParams)
            {
                if (!seenDiagnosticSourceIds.Contains(projectOrDocumentId) &&
                    !removedDocuments.Contains(previousDiagnosticParams))
                {
                    progress.Report(CreateRemovedReport(previousDiagnosticParams.TextDocument));
                }
            }

            // Clear out the solution context to avoid retaining memory
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1809058
            context.ClearSolutionContext();

            // Some implementations of the spec will re-open requests as soon as we close them, spamming the server.
            // In those cases, we wait for the implementation to indicate that changes have occurred, then we close the connection
            // so that the client asks us again.
            await WaitForChangesAsync(category, context, cancellationToken).ConfigureAwait(false);

            // If we had a progress object, then we will have been reporting to that.  Otherwise, take what we've been
            // collecting and return that.
            context.TraceInformation($"{this.GetType()} finished getting diagnostics");
        }

        return CreateReturn(progress);

        static void ProcessPreviousResults(
            Solution solution,
            ImmutableArray<PreviousPullResult> previousResults,
            Dictionary<ProjectOrDocumentId, PreviousPullResult> idToPreviousDiagnosticParams,
            HashSet<PreviousPullResult> removedResults)
        {
            foreach (var diagnosticParams in previousResults)
            {
                if (diagnosticParams.TextDocument != null)
                {
                    var id = GetIdForPreviousResult(diagnosticParams.TextDocument, solution);
                    if (id != null)
                    {
                        idToPreviousDiagnosticParams[id.Value] = diagnosticParams;
                    }
                    else
                    {
                        // The client previously had a result from us for this document, but we no longer have it in our solution.
                        // Record it so we can report to the client that it has been removed.
                        removedResults.Add(diagnosticParams);
                    }
                }
            }
        }

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

    private async Task ComputeAndReportCurrentDiagnosticsAsync(
        RequestContext context,
        IDiagnosticSource diagnosticSource,
        BufferedProgress<TReport> progress,
        string resultId,
        ClientCapabilities clientCapabilities,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<LSP.Diagnostic>.GetInstance(out var result);
        var diagnostics = await diagnosticSource.GetDiagnosticsAsync(context, cancellationToken).ConfigureAwait(false);

        // If we can't get a text document identifier we can't report diagnostics for this source.
        // This can happen for 'fake' projects (e.g. used for TS script blocks).
        var documentIdentifier = diagnosticSource.GetDocumentIdentifier();
        if (documentIdentifier == null)
        {
            // We are not expecting to get any diagnostics for sources that don't have a path.
            Contract.ThrowIfFalse(diagnostics.IsEmpty);
            return;
        }

        context.TraceInformation($"Found {diagnostics.Length} diagnostics for {diagnosticSource.ToDisplayString()}");

        foreach (var diagnostic in diagnostics)
            result.AddRange(ConvertDiagnostic(diagnosticSource, diagnostic, clientCapabilities));

        var report = CreateReport(documentIdentifier, result.ToArray(), resultId);
        progress.Report(report);
    }

    private void HandleRemovedDocuments(RequestContext context, HashSet<PreviousPullResult> removedPreviousResults, BufferedProgress<TReport> progress)
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
        if (!ShouldIncludeHiddenDiagnostic(diagnosticData, capabilities))
        {
            return [];
        }

        var project = diagnosticSource.GetProject();
        var diagnostic = CreateLspDiagnostic(diagnosticData, project, capabilities);

        // Check if we need to handle the unnecessary tag (fading).
        if (!diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
        {
            return [diagnostic];
        }

        // DiagnosticId supports fading, check if the corresponding VS option is turned on.
        if (!SupportsFadingOption(diagnosticData))
        {
            return [diagnostic];
        }

        // Check to see if there are specific locations marked to fade.
        if (!diagnosticData.TryGetUnnecessaryDataLocations(out var unnecessaryLocations))
        {
            // There are no specific fading locations, just mark the whole diagnostic span as unnecessary.
            // We should always have at least one tag (build or intellisense error).
            Contract.ThrowIfNull(diagnostic.Tags, $"diagnostic {diagnostic.Identifier} was missing tags");
            diagnostic.Tags = diagnostic.Tags.Append(DiagnosticTag.Unnecessary);
            return [diagnostic];
        }

        if (capabilities.HasVisualStudioLspCapability())
        {
            // Roslyn produces unnecessary diagnostics by using additional locations, however LSP doesn't support tagging
            // additional locations separately.  Instead we just create multiple hidden diagnostics for unnecessary squiggling.
            using var _ = ArrayBuilder<LSP.Diagnostic>.GetInstance(out var diagnosticsBuilder);
            diagnosticsBuilder.Add(diagnostic);
            foreach (var location in unnecessaryLocations)
            {
                var additionalDiagnostic = CreateLspDiagnostic(diagnosticData, project, capabilities);
                additionalDiagnostic.Severity = LSP.DiagnosticSeverity.Hint;
                additionalDiagnostic.Range = GetRange(location);
                additionalDiagnostic.Tags = [DiagnosticTag.Unnecessary, VSDiagnosticTags.HiddenInEditor, VSDiagnosticTags.HiddenInErrorList, VSDiagnosticTags.SuppressEditorToolTip];
                diagnosticsBuilder.Add(additionalDiagnostic);
            }

            return diagnosticsBuilder.ToImmutableArray();
        }
        else
        {
            diagnostic.Tags = diagnostic.Tags != null ? diagnostic.Tags.Append(DiagnosticTag.Unnecessary) : [DiagnosticTag.Unnecessary];
            var diagnosticRelatedInformation = unnecessaryLocations.Value.Select(l => new DiagnosticRelatedInformation
            {
                Location = new LSP.Location
                {
                    Range = GetRange(l),
                    Uri = ProtocolConversions.CreateAbsoluteUri(l.UnmappedFileSpan.Path)
                },
                Message = diagnostic.Message
            }).ToArray();
            diagnostic.RelatedInformation = diagnosticRelatedInformation;
            return [diagnostic];
        }

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
                Code = diagnosticData.Id,
                CodeDescription = ProtocolConversions.HelpLinkToCodeDescription(diagnosticData.GetValidHelpLinkUri()),
                Message = diagnosticData.Message,
                Severity = ConvertDiagnosticSeverity(diagnosticData.Severity, capabilities),
                Tags = ConvertTags(diagnosticData, diagnosticSource.IsLiveSource()),
                DiagnosticRank = ConvertRank(diagnosticData),
                Range = GetRange(diagnosticData.DataLocation)
            };

            if (capabilities.HasVisualStudioLspCapability())
            {
                var expandedMessage = string.IsNullOrEmpty(diagnosticData.Description) ? null : diagnosticData.Description;
                var informationService = project.Solution.Services.GetRequiredService<IDiagnosticProjectInformationService>();

                diagnostic.DiagnosticType = diagnosticData.Category;
                diagnostic.ExpandedMessage = expandedMessage;
                diagnostic.Projects = [informationService.GetDiagnosticProjectInformation(project)];

                // Defines an identifier used by the client for merging diagnostics across projects. We want diagnostics
                // to be merged from separate projects if they have the same code, filepath, range, and message.
                //
                // Note: LSP pull diagnostics only operates on unmapped locations.
                diagnostic.Identifier = (diagnostic.Code, diagnosticData.DataLocation.UnmappedFileSpan.Path, diagnostic.Range, diagnostic.Message)
                    .GetHashCode().ToString();
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
                    Character = dataLocation.UnmappedFileSpan.StartLinePosition.Character,
                    Line = dataLocation.UnmappedFileSpan.StartLinePosition.Line,
                },
                End = new Position
                {
                    Character = dataLocation.UnmappedFileSpan.EndLinePosition.Character,
                    Line = dataLocation.UnmappedFileSpan.EndLinePosition.Line,
                }
            };
        }

        static bool ShouldIncludeHiddenDiagnostic(DiagnosticData diagnosticData, ClientCapabilities capabilities)
        {
            // VS can handle us reporting any kind of diagnostic using VS custom tags.
            if (capabilities.HasVisualStudioLspCapability() == true)
            {
                return true;
            }

            // Diagnostic isn't hidden - we should report this diagnostic in all scenarios.
            if (diagnosticData.Severity != DiagnosticSeverity.Hidden)
            {
                return true;
            }

            // Roslyn creates these for example in remove unnecessary imports, see RemoveUnnecessaryImportsConstants.DiagnosticFixableId.
            // These aren't meant to be visible in anyway, so we can safely exclude them.
            // TODO - We should probably not be creating these as separate diagnostics or have a 'really really' hidden tag.
            if (string.IsNullOrEmpty(diagnosticData.Message))
            {
                return false;
            }

            // Hidden diagnostics that are unnecessary are visible to the user in the form of fading.
            // We can report these diagnostics.
            if (diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
            {
                return true;
            }

            // We have a hidden diagnostic that has no fading.  This diagnostic can't be visible so don't send it to the client.
            return false;
        }
    }

    private static VSDiagnosticRank? ConvertRank(DiagnosticData diagnosticData)
    {
        if (diagnosticData.Properties.TryGetValue(PullDiagnosticConstants.Priority, out var priority))
        {
            return priority switch
            {
                PullDiagnosticConstants.Low => VSDiagnosticRank.Low,
                PullDiagnosticConstants.Medium => VSDiagnosticRank.Default,
                PullDiagnosticConstants.High => VSDiagnosticRank.High,
                _ => null,
            };
        }

        return null;
    }

    private static LSP.DiagnosticSeverity ConvertDiagnosticSeverity(DiagnosticSeverity severity, ClientCapabilities clientCapabilities)
        => severity switch
        {
            // Hidden is translated in ConvertTags to pass along appropriate _ms tags
            // that will hide the item in a client that knows about those tags.
            DiagnosticSeverity.Hidden => LSP.DiagnosticSeverity.Hint,
            // VSCode shows information diagnostics as blue squiggles, and hint diagnostics as 3 dots.  We prefer the latter rendering so we return hint diagnostics in vscode.
            DiagnosticSeverity.Info => clientCapabilities.HasVisualStudioLspCapability() ? LSP.DiagnosticSeverity.Information : LSP.DiagnosticSeverity.Hint,
            DiagnosticSeverity.Warning => LSP.DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error => LSP.DiagnosticSeverity.Error,
            _ => throw ExceptionUtilities.UnexpectedValue(severity),
        };

    /// <summary>
    /// If you make change in this method, please also update the corresponding file in
    /// src\VisualStudio\Xaml\Impl\Implementation\LanguageServer\Handler\Diagnostics\AbstractPullDiagnosticHandler.cs
    /// </summary>
    protected static DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool isLiveSource, bool potentialDuplicate)
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

        if (diagnosticData.CustomTags.Contains(PullDiagnosticConstants.TaskItemCustomTag))
            result.Add(VSDiagnosticTags.TaskItem);

        // Let the host know that these errors represent potentially stale information from the past that should
        // be superseded by fresher info.
        if (potentialDuplicate)
            result.Add(VSDiagnosticTags.PotentialDuplicate);

        // Mark this also as a build error.  That way an explicitly kicked off build from a source like CPS can
        // override it.
        if (!isLiveSource)
            result.Add(VSDiagnosticTags.BuildError);

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
