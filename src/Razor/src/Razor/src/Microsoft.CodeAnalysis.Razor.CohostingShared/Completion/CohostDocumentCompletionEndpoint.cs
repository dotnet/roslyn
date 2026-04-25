// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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

        // First of all, see if we in HTML and get HTML completions before calling OOP to get Razor completions.
        // Razor completion provider needs a set of existing HTML item labels.

        RazorVSInternalCompletionList? htmlCompletionList = null;
        var razorCompletionOptions = new RazorCompletionOptions(
            SnippetsSupported: true, // always true in non-legacy Razor, always false in legacy Razor
            AutoInsertAttributeQuotes: clientSettings.AdvancedSettings.AutoInsertAttributeQuotes,
            CommitElementsWithSpace: clientSettings.AdvancedSettings.CommitElementsWithSpace,
            UseVsCodeCompletionCommitCharacters: !_clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions);
        using var _ = HashSetPool<string>.GetPooledObject(out var existingHtmlCompletions);

        // We can just blindly call HTML LSP because if we are in C#, generated HTML seen by HTML LSP may return
        // results we don't want to show. So we want to call HTML LSP only if we know we are in HTML content.
        if (documentPositionInfo.LanguageKind == RazorLanguageKind.Html &&
            _triggerAndCommitCharacters.IsValidHtmlTrigger(completionContext))
        {
            htmlCompletionList = await GetHtmlCompletionListAsync(request, razorDocument, razorCompletionOptions, correlationId, cancellationToken).ConfigureAwait(false);

            if (htmlCompletionList is null)
            {
                // HTML server failed to respond (e.g., not yet initialized on first document open).
                // Return an incomplete empty list so the client retries, rather than continuing to
                // merge with Razor results and showing partial Razor-only items that could cause
                // the user to accidentally commit a wrong item.
                _logger.LogDebug($"HTML completion failed for {razorDocument.FilePath}, returning incomplete list");
                return new RazorVSInternalCompletionList() { IsIncomplete = true, Items = [] };
            }

            existingHtmlCompletions.UnionWith(htmlCompletionList.Items.Select(i => i.Label));
        }

        _logger.LogDebug($"Calling OOP to get completion items at {request.Position} invoked by typing '{request.Context?.TriggerCharacter}'");

        var data = await _remoteServiceInvoker.TryInvokeAsync<IRemoteCompletionService, Response>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken)
                => service.GetCompletionAsync(
                        solutionInfo,
                        razorDocument.Id,
                        completionPositionInfo,
                        completionContext,
                        razorCompletionOptions,
                        existingHtmlCompletions,
                        correlationId,
                        cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (data.StopHandling)
        {
            return null;
        }

        RazorVSInternalCompletionList? combinedCompletionList = null;
        if (data.Result is { } oopCompletionList)
        {
            combinedCompletionList = htmlCompletionList is { Items: [_, ..] }
                // If we have HTML completions, that means OOP completion list is really just Razor completion list
                ? CompletionListMerger.Merge(oopCompletionList, htmlCompletionList)
                : oopCompletionList;
        }
        else
        {
            // Didn't get anything from OOP, so just return HTML completion list or null
            combinedCompletionList = htmlCompletionList;
        }

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
