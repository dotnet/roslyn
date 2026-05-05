// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Completion.Delegation;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.RazorVSInternalCompletionList?>;
using CompletionResponse = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Protocol.Completion.CompletionResult>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteCompletionService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteCompletionService
{
    internal sealed class Factory : FactoryBase<IRemoteCompletionService>
    {
        protected override IRemoteCompletionService CreateService(in ServiceArgs args)
            => new RemoteCompletionService(in args);
    }

    private readonly RazorCompletionListProvider _razorCompletionListProvider = args.ExportProvider.GetExportedValue<RazorCompletionListProvider>();
    private readonly CompletionListCache _completionListCache = args.ExportProvider.GetExportedValue<CompletionListCache>();
    private readonly CompletionListCacheWrapperProvder _cacheWrapperProvider = args.ExportProvider.GetExportedValue<CompletionListCacheWrapperProvder>();
    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();
    private readonly CompletionTriggerAndCommitCharacters _triggerAndCommitCharacters = args.ExportProvider.GetExportedValue<CompletionTriggerAndCommitCharacters>();
    private readonly IRazorFormattingService _formattingService = args.ExportProvider.GetExportedValue<IRazorFormattingService>();
    private readonly IDocumentMappingService _documentMappingService = args.ExportProvider.GetExportedValue<IDocumentMappingService>();
    private readonly ITelemetryReporter _telemetryReporter = args.ExportProvider.GetExportedValue<ITelemetryReporter>();
    private readonly IClientSettingsManager _clientSettingsManager = args.ExportProvider.GetExportedValue<IClientSettingsManager>();

    public ValueTask<CompletionPositionInfo?> GetPositionInfoAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        VSInternalCompletionContext completionContext,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetPositionInfoAsync(context, completionContext, position, cancellationToken),
            cancellationToken);

    private async ValueTask<CompletionPositionInfo?> GetPositionInfoAsync(
        RemoteDocumentContext remoteDocumentContext,
        VSInternalCompletionContext completionContext,
        Position position,
        CancellationToken cancellationToken)
    {
        var sourceText = await remoteDocumentContext.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);
        if (!sourceText.TryGetAbsoluteIndex(position, out var index))
        {
            return null;
        }

        var codeDocument = await remoteDocumentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var positionInfo = GetPositionInfo(codeDocument, index);

        if (positionInfo.LanguageKind != RazorLanguageKind.Razor
            && DelegatedCompletionHelper.TryGetProvisionalCompletionInfo(
                codeDocument, completionContext, positionInfo, DocumentMappingService, out var provisionalCompletionInfo))
        {
            return provisionalCompletionInfo with { ShouldIncludeDelegationSnippets = false };
        }

        var shouldIncludeSnippets = positionInfo.LanguageKind == RazorLanguageKind.Html
            && DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, index);

        return new CompletionPositionInfo(ProvisionalTextEdit: null, positionInfo, shouldIncludeSnippets);
    }

    public ValueTask<CompletionResponse> GetCompletionAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        CompletionPositionInfo positionInfo,
        VSInternalCompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        Guid correlationId,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetCompletionAsync(
                context,
                positionInfo,
                completionContext,
                razorCompletionOptions,
                correlationId,
                cancellationToken),
            cancellationToken);

    private async ValueTask<CompletionResponse> GetCompletionAsync(
        RemoteDocumentContext remoteDocumentContext,
        CompletionPositionInfo positionInfo,
        VSInternalCompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var documentPositionInfo = positionInfo.DocumentPositionInfo;

        var isCSharpTrigger = documentPositionInfo.LanguageKind == RazorLanguageKind.CSharp &&
                              _triggerAndCommitCharacters.IsValidCSharpTrigger(completionContext);

        var isRazorTrigger = _triggerAndCommitCharacters.IsValidRazorTrigger(completionContext);

        if (!isCSharpTrigger && !isRazorTrigger)
        {
            // We don't have a valid trigger, so we can't provide completions.
            return CompletionResults.CallHtml;
        }

        var documentSnapshot = remoteDocumentContext.Snapshot;

        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        RazorVSInternalCompletionList? csharpCompletionList = null;
        if (isCSharpTrigger)
        {
            var mappedPosition = documentPositionInfo.Position;

            var csharpGeneratedDocument = await GetCSharpGeneratedDocumentAsync(
                documentSnapshot, positionInfo.ProvisionalTextEdit, cancellationToken).ConfigureAwait(false);

            csharpCompletionList = await GetCSharpCompletionAsync(
                csharpGeneratedDocument,
                codeDocument,
                documentPositionInfo.HostDocumentIndex,
                mappedPosition,
                completionContext,
                razorCompletionOptions,
                correlationId,
                positionInfo.ProvisionalTextEdit,
                cancellationToken)
                .ConfigureAwait(false);
        }

        RazorVSInternalCompletionList? razorCompletionList = null;
        var needsHtmlDependentPhase = false;

        if (isRazorTrigger)
        {
            (razorCompletionList, needsHtmlDependentPhase) = _razorCompletionListProvider.GetCompletionList(
                codeDocument,
                documentPositionInfo.HostDocumentIndex,
                completionContext,
                _clientCapabilitiesService.ClientCapabilities,
                razorCompletionOptions);
        }

        return CompletionResults.Create(
            CompletionListMerger.Merge(razorCompletionList, csharpCompletionList),
            needsHtmlDependentPhase);
    }

    public ValueTask<Response> GetHtmlDependentCompletionsAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        CompletionPositionInfo positionInfo,
        VSInternalCompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        string[] htmlLabels,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetHtmlDependentCompletionsAsync(
                context,
                positionInfo,
                completionContext,
                razorCompletionOptions,
                htmlLabels,
                cancellationToken),
            cancellationToken);

    private async ValueTask<Response> GetHtmlDependentCompletionsAsync(
        RemoteDocumentContext remoteDocumentContext,
        CompletionPositionInfo positionInfo,
        VSInternalCompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        string[] htmlLabels,
        CancellationToken cancellationToken)
    {
        var documentPositionInfo = positionInfo.DocumentPositionInfo;

        if (!_triggerAndCommitCharacters.IsValidRazorTrigger(completionContext))
        {
            return Response.CallHtml;
        }

        var documentSnapshot = remoteDocumentContext.Snapshot;
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        using var _ = HashSetPool<string>.GetPooledObject(out var existingCompletions);
        existingCompletions.UnionWith(htmlLabels);

        var razorCompletionList = _razorCompletionListProvider.GetHtmlDependentCompletionList(
            codeDocument,
            documentPositionInfo.HostDocumentIndex,
            completionContext,
            _clientCapabilitiesService.ClientCapabilities,
            razorCompletionOptions,
            existingCompletions);

        if (razorCompletionList is null)
        {
            return Response.CallHtml;
        }

        return Response.Results(razorCompletionList);
    }

    private async ValueTask<RazorVSInternalCompletionList?> GetCSharpCompletionAsync(
        SourceGeneratedDocument generatedDocument,
        RazorCodeDocument codeDocument,
        int documentIndex,
        Position mappedPosition,
        CompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        Guid correlationId,
        TextEdit? provisionalTextEdit,
        CancellationToken cancellationToken)
    {
        var clientCapabilities = _clientCapabilitiesService.ClientCapabilities;
        if (clientCapabilities.TextDocument?.Completion is not { } completionSetting)
        {
            Debug.Fail("Unable to convert VS to Roslyn LSP completion setting");
            return null;
        }

        var mappedLinePosition = mappedPosition.ToLinePosition();

        VSInternalCompletionList? completionList = null;
        using (_telemetryReporter.TrackLspRequest(Methods.TextDocumentCompletionName, Constants.ExternalAccessServerName, TelemetryThresholds.CompletionSubLSPTelemetryThreshold, correlationId))
        {
            completionList = await ExternalAccess.Razor.Cohost.Handlers.Completion.GetCompletionListAsync(
                generatedDocument,
                mappedLinePosition,
                completionContext,
                clientCapabilities.SupportsVisualStudioExtensions,
                completionSetting,
                _cacheWrapperProvider.GetCache(),
                cancellationToken)
                .ConfigureAwait(false);
        }

        if (completionList is null)
        {
            // If we don't get a response from the delegated server, we have to make sure to return an incomplete completion
            // list. When a user is typing quickly, the delegated request from the first keystroke will fail to synchronize,
            // so if we return a "complete" list then the query won't re-query us for completion once the typing stops/slows
            // so we'd only ever return Razor completion items.
            return new RazorVSInternalCompletionList()
            {
                Items = [],
                IsIncomplete = true
            };
        }

        var rewrittenResponse = DelegatedCompletionHelper.RewriteCSharpResponse(
            new(completionList),
            documentIndex,
            codeDocument,
            mappedPosition,
            razorCompletionOptions);

        var resolutionContext = new DelegatedCompletionResolutionContext(RazorLanguageKind.CSharp, rewrittenResponse.Data ?? rewrittenResponse.ItemDefaults?.Data, provisionalTextEdit);
        var resultId = _completionListCache.Add(rewrittenResponse, resolutionContext);
        rewrittenResponse.SetResultId(resultId, clientCapabilities);

        return rewrittenResponse;
    }

    private static async Task<SourceGeneratedDocument> GetCSharpGeneratedDocumentAsync(RemoteDocumentSnapshot documentSnapshot, TextEdit? provisionalTextEdit, CancellationToken cancellationToken)
    {
        var generatedDocument = await documentSnapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

        if (provisionalTextEdit is not null)
        {
            var generatedText = await generatedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var change = generatedText.GetTextChange(provisionalTextEdit);
            generatedText = generatedText.WithChanges([change]);
            generatedDocument = (SourceGeneratedDocument)generatedDocument.WithText(generatedText);
        }

        return generatedDocument;
    }

    public ValueTask<VSInternalCompletionItem> ResolveCompletionItemAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        VSInternalCompletionItem request,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => ResolveCompletionItemAsync(context, request, cancellationToken),
            cancellationToken);

    private ValueTask<VSInternalCompletionItem> ResolveCompletionItemAsync(
        RemoteDocumentContext context,
        VSInternalCompletionItem request,
        CancellationToken cancellationToken)
    {
        if (!_completionListCache.TryGetOriginalRequestData(request, out var containingCompletionList, out var originalRequestContext))
        {
            // Couldn't find an associated completion list
            return new(request);
        }

        if (originalRequestContext is DelegatedCompletionResolutionContext resolutionContext)
        {
            return ResolveCSharpCompletionItemAsync(context, request, containingCompletionList, resolutionContext, cancellationToken);
        }
        else if (originalRequestContext is RazorCompletionResolveContext razorResolutionContext)
        {
            return ResolveRazorCompletionItemAsync(context, request, razorResolutionContext, cancellationToken);
        }

        // We don't know how to resolve this completion item, so just return it as-is.
        Logger.LogWarning("Did not recognize completion item, so unable to resolve.");
        return new(request);
    }

    private async ValueTask<VSInternalCompletionItem> ResolveRazorCompletionItemAsync(RemoteDocumentContext context, VSInternalCompletionItem request, RazorCompletionResolveContext razorResolutionContext, CancellationToken cancellationToken)
    {
        var componentAvailabilityService = new ComponentAvailabilityService(context.Snapshot.ProjectSnapshot.SolutionSnapshot);

        var result = await RazorCompletionItemResolver.ResolveAsync(
            request,
            _clientCapabilitiesService.ClientCapabilities,
            componentAvailabilityService,
            razorResolutionContext,
            cancellationToken).ConfigureAwait(false);

        // If we couldn't resolve, fall back to what we were passed in
        return result ?? request;
    }

    private async ValueTask<VSInternalCompletionItem> ResolveCSharpCompletionItemAsync(RemoteDocumentContext context, VSInternalCompletionItem request, VSInternalCompletionList containingCompletionList, DelegatedCompletionResolutionContext resolutionContext, CancellationToken cancellationToken)
    {
        var oldData = request.Data;
        try
        {
            request.Data = DelegatedCompletionHelper.GetOriginalCompletionItemData(request, containingCompletionList, resolutionContext.OriginalCompletionListData);

            // Roslyn expects data to be JsonElement because we're calling into their LSP handler, but we cache their data as its underlying object, so lets
            // make sure to serialize if we need to.
            if (request.Data is not JsonElement)
            {
                request.Data = JsonSerializer.SerializeToElement(request.Data, JsonHelpers.JsonSerializerOptions);
            }

            var documentSnapshot = context.Snapshot;
            var generatedDocument = await GetCSharpGeneratedDocumentAsync(documentSnapshot, resolutionContext.ProvisionalTextEdit, cancellationToken).ConfigureAwait(false);

            var clientCapabilities = _clientCapabilitiesService.ClientCapabilities;
            var completionListSetting = clientCapabilities.TextDocument?.Completion;
            var result = await ExternalAccess.Razor.Cohost.Handlers.Completion.ResolveCompletionItemAsync(
                request,
                generatedDocument,
                clientCapabilities.SupportsVisualStudioExtensions,
                completionListSetting ?? new(),
                _cacheWrapperProvider.GetCache(),
                cancellationToken).ConfigureAwait(false);

            var item = JsonHelpers.Convert<CompletionItem, VSInternalCompletionItem>(result).AssumeNotNull();

            var formattingOptions = _clientSettingsManager.GetClientSettings().ToRazorFormattingOptions();

            item = await DelegatedCompletionHelper.FormatCSharpCompletionItemAsync(
                item,
                context,
                formattingOptions,
                _formattingService,
                _documentMappingService,
                clientCapabilities.SupportsVisualStudioExtensions,
                Logger,
                cancellationToken).ConfigureAwait(false);

            return item;
        }
        finally
        {
            request.Data = oldData; // Restore original data to avoid side effects, as it may have come from the cache
        }
    }
}
