// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using ExternalCallHierarchy = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers.CallHierarchy;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteCallHierarchyService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteCallHierarchyService
{
    internal sealed class Factory : FactoryBase<IRemoteCallHierarchyService>
    {
        protected override IRemoteCallHierarchyService CreateService(in ServiceArgs args)
            => new RemoteCallHierarchyService(in args);
    }

    private readonly IFilePathService _filePathService = args.ExportProvider.GetExportedValue<IFilePathService>();

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    public ValueTask<RemoteResponse<CallHierarchyItem[]?>> PrepareCallHierarchyAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => PrepareCallHierarchyAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<CallHierarchyItem[]?>> PrepareCallHierarchyAsync(
        RemoteDocumentContext context,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

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

        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await ExternalCallHierarchy
            .PrepareCallHierarchyAsync(generatedDocument, positionInfo.Position.ToLinePosition(), cancellationToken)
            .ConfigureAwait(false);

        if (items is null)
        {
            return RemoteResponse<CallHierarchyItem[]?>.NoFurtherHandling;
        }

        var mappedItems = await MapItemsAsync(context, items, cancellationToken).ConfigureAwait(false);
        return RemoteResponse<CallHierarchyItem[]?>.Results(mappedItems);
    }

    public ValueTask<RemoteResponse<CallHierarchyIncomingCall[]?>> GetIncomingCallsAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetIncomingCallsAsync(context, item, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<CallHierarchyIncomingCall[]?>> GetIncomingCallsAsync(
        RemoteDocumentContext context,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var incomingCalls = await ExternalCallHierarchy
            .GetIncomingCallsAsync(generatedDocument, item, cancellationToken)
            .ConfigureAwait(false);

        if (incomingCalls is null)
        {
            return RemoteResponse<CallHierarchyIncomingCall[]?>.NoFurtherHandling;
        }

        using var builder = new PooledArrayBuilder<CallHierarchyIncomingCall>(incomingCalls.Length);
        foreach (var incomingCall in incomingCalls)
        {
            var originalFromUri = incomingCall.From.Uri.GetRequiredParsedUri();
            var mappedFromItem = await MapItemAsync(context, incomingCall.From, cancellationToken).ConfigureAwait(false);
            if (mappedFromItem is null)
            {
                continue;
            }

            var mappedRanges = await MapRangesAsync(context, originalFromUri, incomingCall.FromRanges, cancellationToken).ConfigureAwait(false);
            builder.Add(new CallHierarchyIncomingCall
            {
                From = mappedFromItem,
                FromRanges = mappedRanges,
            });
        }

        return RemoteResponse<CallHierarchyIncomingCall[]?>.Results(builder.ToArray());
    }

    public ValueTask<RemoteResponse<CallHierarchyOutgoingCall[]?>> GetOutgoingCallsAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetOutgoingCallsAsync(context, item, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<CallHierarchyOutgoingCall[]?>> GetOutgoingCallsAsync(
        RemoteDocumentContext context,
        CallHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var outgoingCalls = await ExternalCallHierarchy
            .GetOutgoingCallsAsync(generatedDocument, item, cancellationToken)
            .ConfigureAwait(false);

        if (outgoingCalls is null)
        {
            return RemoteResponse<CallHierarchyOutgoingCall[]?>.NoFurtherHandling;
        }

        var callerUri = generatedDocument.CreateUri();
        using var builder = new PooledArrayBuilder<CallHierarchyOutgoingCall>(outgoingCalls.Length);
        foreach (var outgoingCall in outgoingCalls)
        {
            var mappedToItem = await MapItemAsync(context, outgoingCall.To, cancellationToken).ConfigureAwait(false);
            if (mappedToItem is null)
            {
                continue;
            }

            var mappedRanges = await MapRangesAsync(context, callerUri, outgoingCall.FromRanges, cancellationToken).ConfigureAwait(false);
            builder.Add(new CallHierarchyOutgoingCall
            {
                To = mappedToItem,
                FromRanges = mappedRanges,
            });
        }

        return RemoteResponse<CallHierarchyOutgoingCall[]?>.Results(builder.ToArray());
    }

    private async Task<CallHierarchyItem[]?> MapItemsAsync(RemoteDocumentContext context, CallHierarchyItem[] items, CancellationToken cancellationToken)
    {
        using var builder = new PooledArrayBuilder<CallHierarchyItem>(items.Length);
        foreach (var item in items)
        {
            var mappedItem = await MapItemAsync(context, item, cancellationToken).ConfigureAwait(false);
            if (mappedItem is not null)
            {
                builder.Add(mappedItem);
            }
        }

        return builder.ToArray();
    }

    private async Task<CallHierarchyItem?> MapItemAsync(RemoteDocumentContext context, CallHierarchyItem item, CancellationToken cancellationToken)
    {
        var uri = item.Uri.GetRequiredParsedUri();

        var (mappedDocumentUri, mappedRange) = await DocumentMappingService
            .MapToHostDocumentUriAndRangeAsync(context.Snapshot, uri, item.Range, cancellationToken)
            .ConfigureAwait(false);

        if (_filePathService.IsVirtualCSharpFile(mappedDocumentUri))
        {
            return null;
        }

        var (mappedSelectionUri, mappedSelectionRange) = await DocumentMappingService
            .MapToHostDocumentUriAndRangeAsync(context.Snapshot, uri, item.SelectionRange, cancellationToken)
            .ConfigureAwait(false);

        if (_filePathService.IsVirtualCSharpFile(mappedSelectionUri))
        {
            return null;
        }

        return new CallHierarchyItem
        {
            Name = item.Name,
            Kind = item.Kind,
            Tags = item.Tags,
            Detail = item.Detail,
            Uri = new(mappedDocumentUri),
            Range = mappedRange,
            SelectionRange = mappedSelectionRange,
            Data = item.Data,
        };
    }

    private async Task<LspRange[]> MapRangesAsync(RemoteDocumentContext context, Uri documentUri, LspRange[] ranges, CancellationToken cancellationToken)
    {
        using var builder = new PooledArrayBuilder<LspRange>(ranges.Length);
        foreach (var range in ranges)
        {
            var (mappedDocumentUri, mappedRange) = await DocumentMappingService
                .MapToHostDocumentUriAndRangeAsync(context.Snapshot, documentUri, range, cancellationToken)
                .ConfigureAwait(false);

            if (_filePathService.IsVirtualCSharpFile(mappedDocumentUri))
            {
                continue;
            }

            builder.Add(mappedRange);
        }

        return builder.ToArray();
    }
}
