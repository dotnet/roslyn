// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteCallHierarchyService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteCallHierarchyService
{
    internal sealed class Factory : FactoryBase<IRemoteCallHierarchyService>
    {
        protected override IRemoteCallHierarchyService CreateService(in ServiceArgs args)
            => new RemoteCallHierarchyService(in args);
    }

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    public ValueTask<RemoteResponse<CallHierarchyItem[]?>> PrepareCallHierarchyAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => PrepareCallHierarchyAsync(snapshot, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<CallHierarchyItem[]?>> PrepareCallHierarchyAsync(
        RemoteDocumentSnapshot snapshot,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            return RemoteResponse<CallHierarchyItem[]?>.NoFurtherHandling;
        }

        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);
        if (positionInfo.LanguageKind is not RazorLanguageKind.CSharp)
        {
            return RemoteResponse<CallHierarchyItem[]?>.NoFurtherHandling;
        }

        var generatedDocument = await snapshot
            .GetGeneratedDocumentAsync(positionInfo.InDeclDocument, cancellationToken)
            .ConfigureAwait(false);

        var linePosition = positionInfo.Position.ToLinePosition();
        var items = await PrepareCallHierarchyHandler.PrepareCallHierarchyAsync(generatedDocument, linePosition, generatedDocument.Id, cancellationToken)
            .ConfigureAwait(false);

        if (items is null && !positionInfo.InDeclDocument)
        {
            // Razor implementation generated documents can contain call sites for members whose declarations live in the
            // paired declaration generated document. In that case Roslyn's prepare path can't create an item for the
            // implementation document, so retry while explicitly preferring the declaration document.
            var declarationGeneratedDocument = await snapshot.TryGetGeneratedDocumentAsync(declarationDocument: true, cancellationToken).ConfigureAwait(false);
            if (declarationGeneratedDocument is null)
            {
                return RemoteResponse<CallHierarchyItem[]?>.NoFurtherHandling;
            }

            items = await PrepareCallHierarchyHandler.PrepareCallHierarchyAsync(generatedDocument, linePosition, declarationGeneratedDocument.Id, cancellationToken).ConfigureAwait(false);
        }

        if (items is null)
        {
            return RemoteResponse<CallHierarchyItem[]?>.NoFurtherHandling;
        }

        var mappedItems = await MapItemsAsync(snapshot, items, cancellationToken).ConfigureAwait(false);
        return RemoteResponse<CallHierarchyItem[]?>.Results(mappedItems);
    }

    public ValueTask<RemoteResponse<CallHierarchyIncomingCall[]?>> GetIncomingCallsAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => GetIncomingCallsAsync(snapshot, item, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<CallHierarchyIncomingCall[]?>> GetIncomingCallsAsync(
        RemoteDocumentSnapshot snapshot,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await TryGetGeneratedDocumentForItemAsync(snapshot, item, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return RemoteResponse<CallHierarchyIncomingCall[]?>.NoFurtherHandling;
        }

        var incomingCalls = await CallHierarchyIncomingCallsHandler.GetIncomingCallsAsync(generatedDocument, item, allowRazorSourceGeneratedDocuments: true, cancellationToken)
            .ConfigureAwait(false);

        if (incomingCalls is null)
        {
            return RemoteResponse<CallHierarchyIncomingCall[]?>.NoFurtherHandling;
        }

        using var builder = new PooledArrayBuilder<CallHierarchyIncomingCall>(incomingCalls.Length);
        foreach (var incomingCall in incomingCalls)
        {
            var originalFromUri = incomingCall.From.Uri.GetRequiredSystemUri();
            var mappedFromItem = await MapItemAsync(snapshot, incomingCall.From, cancellationToken).ConfigureAwait(false);
            if (mappedFromItem is null)
            {
                continue;
            }

            var mappedRanges = await MapRangesAsync(snapshot, originalFromUri, incomingCall.FromRanges, cancellationToken).ConfigureAwait(false);
            builder.Add(new CallHierarchyIncomingCall
            {
                From = mappedFromItem,
                FromRanges = mappedRanges,
            });
        }

        return RemoteResponse<CallHierarchyIncomingCall[]?>.Results(builder.ToArray());
    }

    public ValueTask<RemoteResponse<CallHierarchyOutgoingCall[]?>> GetOutgoingCallsAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => GetOutgoingCallsAsync(snapshot, item, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<CallHierarchyOutgoingCall[]?>> GetOutgoingCallsAsync(
        RemoteDocumentSnapshot snapshot,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await TryGetGeneratedDocumentForItemAsync(snapshot, item, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return RemoteResponse<CallHierarchyOutgoingCall[]?>.NoFurtherHandling;
        }

        var outgoingCalls = await CallHierarchyOutgoingCallsHandler.GetOutgoingCallsAsync(generatedDocument, item, cancellationToken)
            .ConfigureAwait(false);

        if (outgoingCalls is null)
        {
            return RemoteResponse<CallHierarchyOutgoingCall[]?>.NoFurtherHandling;
        }

        var callerUri = generatedDocument.CreateSystemUri();
        using var builder = new PooledArrayBuilder<CallHierarchyOutgoingCall>(outgoingCalls.Length);
        foreach (var outgoingCall in outgoingCalls)
        {
            var mappedToItem = await MapItemAsync(snapshot, outgoingCall.To, cancellationToken).ConfigureAwait(false);
            if (mappedToItem is null)
            {
                continue;
            }

            var mappedRanges = await MapRangesAsync(snapshot, callerUri, outgoingCall.FromRanges, cancellationToken).ConfigureAwait(false);
            builder.Add(new CallHierarchyOutgoingCall
            {
                To = mappedToItem,
                FromRanges = mappedRanges,
            });
        }

        return RemoteResponse<CallHierarchyOutgoingCall[]?>.Results(builder.ToArray());
    }

    private static async ValueTask<SourceGeneratedDocument?> TryGetGeneratedDocumentForItemAsync(
        RemoteDocumentSnapshot snapshot,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var resolveData = CallHierarchyHelpers.GetResolveData(item);
        var generatedDocumentUri = resolveData.TextDocument.DocumentUri.GetRequiredSystemUri();
        return await snapshot.TextDocument.Project.Solution.TryGetSourceGeneratedDocumentAsync(generatedDocumentUri, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CallHierarchyItem[]?> MapItemsAsync(RemoteDocumentSnapshot snapshot, CallHierarchyItem[] items, CancellationToken cancellationToken)
    {
        using var builder = new PooledArrayBuilder<CallHierarchyItem>(items.Length);
        foreach (var item in items)
        {
            var mappedItem = await MapItemAsync(snapshot, item, cancellationToken).ConfigureAwait(false);
            if (mappedItem is not null)
            {
                builder.Add(mappedItem);
            }
        }

        return builder.ToArray();
    }

    private async Task<CallHierarchyItem?> MapItemAsync(RemoteDocumentSnapshot snapshot, CallHierarchyItem item, CancellationToken cancellationToken)
    {
        var uri = item.Uri.GetRequiredSystemUri();

        var (mappedDocumentUri, mappedRange) = await DocumentMappingService
            .MapToHostDocumentUriAndRangeAsync(snapshot, uri, item.Range, cancellationToken)
            .ConfigureAwait(false);

        var documentUri = mappedDocumentUri.CreateDocumentUriFromSystemUri();
        if (documentUri.IsRazorCSharpDocumentUri(snapshot.TextDocument.Project.Solution))
        {
            return null;
        }

        var (mappedSelectionUri, mappedSelectionRange) = await DocumentMappingService
            .MapToHostDocumentUriAndRangeAsync(snapshot, uri, item.SelectionRange, cancellationToken)
            .ConfigureAwait(false);

        if (mappedSelectionUri.CreateDocumentUriFromSystemUri().IsRazorCSharpDocumentUri(snapshot.TextDocument.Project.Solution))
        {
            return null;
        }

        return new CallHierarchyItem
        {
            Name = item.Name,
            Kind = item.Kind,
            Tags = item.Tags,
            Detail = item.Detail,
            Uri = documentUri,
            Range = mappedRange,
            SelectionRange = mappedSelectionRange,
            Data = item.Data,
        };
    }

    private async Task<LspRange[]> MapRangesAsync(RemoteDocumentSnapshot snapshot, Uri documentUri, LspRange[] ranges, CancellationToken cancellationToken)
    {
        using var builder = new PooledArrayBuilder<LspRange>(ranges.Length);
        foreach (var range in ranges)
        {
            var (mappedDocumentUri, mappedRange) = await DocumentMappingService
                .MapToHostDocumentUriAndRangeAsync(snapshot, documentUri, range, cancellationToken)
                .ConfigureAwait(false);

            if (mappedDocumentUri.CreateDocumentUriFromSystemUri().IsRazorCSharpDocumentUri(snapshot.TextDocument.Project.Solution))
            {
                continue;
            }

            builder.Add(mappedRange);
        }

        return builder.ToArray();
    }
}
