// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Completion.Delegation;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using CompletionResponse = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Protocol.Completion.CompletionResult>;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.RazorVSInternalCompletionList?>;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentCompletionName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostDocumentCompletionEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostDocumentCompletionEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IClientSettingsManager clientSettingsManager,
    IClientCapabilitiesService clientCapabilitiesService,
#pragma warning disable RS0030 // Do not use banned APIs
    [Import(AllowDefault = true)] ISnippetCompletionItemProvider? snippetCompletionItemProvider,
#pragma warning restore RS0030 // Do not use banned APIs
    IHtmlRequestInvoker requestInvoker,
    CompletionListCache completionListCache,
    ITelemetryReporter telemetryReporter,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<RazorVSInternalCompletionParams, RazorVSInternalCompletionList?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly ISnippetCompletionItemProvider? _snippetCompletionItemProvider = snippetCompletionItemProvider;
    private readonly CompletionTriggerAndCommitCharacters _triggerAndCommitCharacters = new(clientCapabilitiesService);
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly CompletionListCache _completionListCache = completionListCache;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostDocumentCompletionEndpoint>();

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Completion?.DynamicRegistration is true)
        {
            return [new Registration()
            {
                Method = Methods.TextDocumentCompletionName,
                RegisterOptions = new CompletionRegistrationOptions()
                {
                    ResolveProvider = true,
                    TriggerCharacters = _triggerAndCommitCharacters.AllTriggerCharacters,
                    AllCommitCharacters = _triggerAndCommitCharacters.AllCommitCharacters
                }
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(RazorVSInternalCompletionParams request)
        => request.TextDocument?.ToRazorTextDocumentIdentifier();

    protected override async Task<RazorVSInternalCompletionList?> HandleRequestAsync(RazorVSInternalCompletionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        if (request.Context is not { } completionContext)
        {
            _logger.LogError("Completion request context is null");
            return null;
        }

        // Save as it may be modified if we forward request to HTML language server
        var originalTextDocumentIdentifier = request.TextDocument;

        // Return immediately if this is auto-shown completion but auto-shown completion is disallowed in settings
        var clientSettings = _clientSettingsManager.GetClientSettings();
        var autoShownCompletion = completionContext.TriggerKind != CompletionTriggerKind.Invoked;
        if (autoShownCompletion && !clientSettings.ClientCompletionSettings.AutoShowCompletion)
        {
            return null;
        }

        _logger.LogDebug($"Invoking completion for {razorDocument.FilePath}");

        var correlationId = Guid.NewGuid();
        using var _1 = _telemetryReporter.TrackLspRequest(Methods.TextDocumentCompletionName, LanguageServerConstants.RazorLanguageServerName, TelemetryThresholds.CompletionRazorTelemetryThreshold, correlationId);

        if (await _remoteServiceInvoker.TryInvokeAsync<IRemoteCompletionService, CompletionPositionInfo?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken)
                => service.GetPositionInfoAsync(
                        solutionInfo,
                        razorDocument.Id,
                        completionContext,
                        request.Position,
                        cancellationToken),
            cancellationToken).ConfigureAwait(false) is not { } completionPositionInfo)
        {
            // If we can't figure out position info for request position we can't return completions
            return null;
        }

        var documentPositionInfo = completionPositionInfo.DocumentPositionInfo;
        if (documentPositionInfo.LanguageKind != RazorLanguageKind.Razor &&
            DelegatedCompletionHelper.RewriteContext(completionContext, documentPositionInfo.LanguageKind, _triggerAndCommitCharacters) is { } rewrittenContext)
        {
            completionContext = rewrittenContext;
        }

        var razorCompletionOptions = new RazorCompletionOptions(
            SnippetsSupported: true, // always true in non-legacy Razor, always false in legacy Razor
            AutoInsertAttributeQuotes: clientSettings.AdvancedSettings.AutoInsertAttributeQuotes,
            CommitElementsWithSpace: clientSettings.AdvancedSettings.CommitElementsWithSpace,
            UseVsCodeCompletionCommitCharacters: !_clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions);

        _logger.LogDebug($"Calling OOP to get completion items at {request.Position} invoked by typing '{request.Context?.TriggerCharacter}'");

        var razorCompletionTask = _remoteServiceInvoker.TryInvokeAsync<IRemoteCompletionService, CompletionResponse>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken)
                => service.GetCompletionAsync(
                        solutionInfo,
                        razorDocument.Id,
                        completionPositionInfo,
                        completionContext,
                        razorCompletionOptions,
                        correlationId,
                        cancellationToken),
            cancellationToken).AsTask();

        RazorVSInternalCompletionList? htmlCompletionList = null;
        if (documentPositionInfo.LanguageKind == RazorLanguageKind.Html &&
            _triggerAndCommitCharacters.IsValidHtmlTrigger(completionContext))
        {
            // Fire HTML request concurrently with the OOP request already in flight.
            // Phase 1 OOP call excludes providers that need HTML labels (e.g., tag helper
            // element completions). After HTML completes, a lightweight phase 2 OOP call
            // runs only those providers with the HTML labels available.
            var htmlTask = GetHtmlCompletionListAsync(request, razorDocument, razorCompletionOptions, correlationId, cancellationToken);

            await Task.WhenAll(htmlTask, razorCompletionTask).ConfigureAwait(false);

            htmlCompletionList = await htmlTask.ConfigureAwait(false);

            if (htmlCompletionList is null)
            {
                // HTML server failed to respond (e.g., not yet initialized on first document open).
                // Return an incomplete empty list so the client retries, rather than continuing to
                // merge with Razor results and showing partial Razor-only items that could cause
                // the user to accidentally commit a wrong item.
                _logger.LogDebug($"HTML completion failed for {razorDocument.FilePath}, returning incomplete list");
                return new RazorVSInternalCompletionList() { IsIncomplete = true, Items = [] };
            }
        }

        var razorCompletionResponse = await razorCompletionTask.ConfigureAwait(false);

        if (razorCompletionResponse.StopHandling)
        {
            return null;
        }

        var razorCompletionResult = razorCompletionResponse.Result;

        // If HTML completed successfully and the initial Razor call indicated that some providers
        // were skipped because they need HTML labels, run those providers now.
        RazorVSInternalCompletionList? razorHtmlDependentCompletionList = null;
        if (htmlCompletionList is not null && razorCompletionResult.NeedsHtmlDependentPhase)
        {
            razorHtmlDependentCompletionList = await GetHtmlDependentCompletionsAsync(
                htmlCompletionList, razorDocument, completionPositionInfo,
                completionContext, razorCompletionOptions, cancellationToken).ConfigureAwait(false);
        }

        var combinedCompletionList = MergeCompletionLists(htmlCompletionList, razorCompletionResult.CompletionList, razorHtmlDependentCompletionList);

        if (completionPositionInfo.ShouldIncludeDelegationSnippets &&
            _snippetCompletionItemProvider is not null)
        {
            combinedCompletionList = AddSnippets(
                combinedCompletionList,
                documentPositionInfo.LanguageKind,
                completionContext.InvokeKind,
                completionContext.TriggerCharacter);
        }

        if (combinedCompletionList is null)
        {
            return null;
        }

        RazorCompletionResolveData.Wrap(combinedCompletionList, originalTextDocumentIdentifier, _clientCapabilitiesService.ClientCapabilities);

        return combinedCompletionList;
    }

    /// <summary>
    /// Merges up to three completion lists: HTML, Razor (C# + Razor items), and Razor HTML-dependent
    /// (tag helper element completions).
    /// </summary>
    /// <remarks>
    /// Both Razor lists are merged first, then merged once against HTML. This ensures Razor
    /// commit characters take precedence at the list level, matching the pre-parallel behavior
    /// where the Razor list was always the first argument to <see cref="CompletionListMerger.Merge"/>.
    /// </remarks>
    private static RazorVSInternalCompletionList? MergeCompletionLists(
        RazorVSInternalCompletionList? htmlCompletionList,
        RazorVSInternalCompletionList? razorCompletionList,
        RazorVSInternalCompletionList? razorHtmlDependentCompletionList)
    {
        var combinedRazorList = CompletionListMerger.Merge(razorCompletionList, razorHtmlDependentCompletionList);
        return CompletionListMerger.Merge(combinedRazorList, htmlCompletionList);
    }

    /// <summary>
    /// Phase 2: runs only the HTML-dependent completion providers (e.g., tag helper element
    /// completions) with the HTML labels available for deduplication.
    /// </summary>
    private async Task<RazorVSInternalCompletionList?> GetHtmlDependentCompletionsAsync(
        RazorVSInternalCompletionList htmlCompletionList,
        TextDocument razorDocument,
        CompletionPositionInfo completionPositionInfo,
        VSInternalCompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        CancellationToken cancellationToken)
    {
        var htmlLabels = new string[htmlCompletionList.Items.Length];
        for (var i = 0; i < htmlCompletionList.Items.Length; i++)
        {
            htmlLabels[i] = htmlCompletionList.Items[i].Label;
        }

        var htmlDependentResponse = await _remoteServiceInvoker.TryInvokeAsync<IRemoteCompletionService, Response>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken)
                => service.GetHtmlDependentCompletionsAsync(
                        solutionInfo,
                        razorDocument.Id,
                        completionPositionInfo,
                        completionContext,
                        razorCompletionOptions,
                        htmlLabels,
                        cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (htmlDependentResponse is { StopHandling: false, Result: { } htmlDependentCompletionList })
        {
            return htmlDependentCompletionList;
        }

        return null;
    }

    private async Task<RazorVSInternalCompletionList?> GetHtmlCompletionListAsync(
        RazorVSInternalCompletionParams completionParams,
        TextDocument razorDocument,
        RazorCompletionOptions razorCompletionOptions,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var result = await _requestInvoker.MakeHtmlLspRequestAsync<RazorVSInternalCompletionParams, RazorVSInternalCompletionList>(
            razorDocument,
            Methods.TextDocumentCompletionName,
            completionParams,
            TelemetryThresholds.CompletionSubLSPTelemetryThreshold,
            correlationId,
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            // HTML server didn't respond (e.g., not yet initialized, sync failure, or checksum mismatch).
            return null;
        }

        var rewrittenResponse = DelegatedCompletionHelper.RewriteHtmlResponse(result, razorCompletionOptions);

        var resolutionContext = new DelegatedCompletionResolutionContext(RazorLanguageKind.Html, rewrittenResponse.Data ?? rewrittenResponse.ItemDefaults?.Data, ProvisionalTextEdit: null);
        var resultId = _completionListCache.Add(rewrittenResponse, resolutionContext);
        rewrittenResponse.SetResultId(resultId, _clientCapabilitiesService.ClientCapabilities);

        return rewrittenResponse;
    }

    private RazorVSInternalCompletionList? AddSnippets(
        RazorVSInternalCompletionList? completionList,
        RazorLanguageKind languageKind,
        VSInternalCompletionInvokeKind invokeKind,
        string? triggerCharacter)
    {
        using var builder = new PooledArrayBuilder<VSInternalCompletionItem>();
        _snippetCompletionItemProvider.AssumeNotNull().AddSnippetCompletions(
            ref builder.AsRef(),
            languageKind,
            invokeKind,
            triggerCharacter);

        // If there were no snippets, just return the original list
        if (builder.Count == 0)
        {
            return completionList;
        }

        // We create a list here to put in our cache. It doesn't really matter if its not the one that is sent to the client,
        // we'll still be able to pull it out again when the client sends us back an item. The SetResultId method associates
        // the resolution context with each item.
        var snippetCompletionList = new RazorVSInternalCompletionList { IsIncomplete = true, Items = builder.ToArray() };
        var resolutionContext = new SnippetCompletionResolutionContext();
        var resultId = _completionListCache.Add(snippetCompletionList, resolutionContext);
        snippetCompletionList.SetResultId(resultId, _clientCapabilitiesService.ClientCapabilities);

        if (completionList is null)
        {
            // If there were no Html completion items, just use our snippet list
            completionList = snippetCompletionList;
        }
        else
        {
            // There were Html completion items, so combine them with our snippet list
            completionList.Items = [.. snippetCompletionList.Items, .. completionList.Items];
        }

        return completionList;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostDocumentCompletionEndpoint instance)
    {
        public Task<RazorVSInternalCompletionList?> HandleRequestAsync(
            RazorVSInternalCompletionParams request,
            TextDocument razorDocument,
            CancellationToken cancellationToken)
                => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
