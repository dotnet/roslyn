// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.Handler.TypeHierarchy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.TypeHierarchyItem[]?>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteTypeHierarchyService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteTypeHierarchyService
{
    internal sealed class Factory : FactoryBase<IRemoteTypeHierarchyService>
    {
        protected override IRemoteTypeHierarchyService CreateService(in ServiceArgs args)
            => new RemoteTypeHierarchyService(in args);
    }

    private readonly IFilePathService _filePathService = args.ExportProvider.GetExportedValue<IFilePathService>();

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    public ValueTask<RemoteResponse<TypeHierarchyItem[]?>> PrepareTypeHierarchyAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => PrepareTypeHierarchyAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<TypeHierarchyItem[]?>> PrepareTypeHierarchyAsync(
        RemoteDocumentContext context,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            return NoFurtherHandling;
        }

        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);
        if (positionInfo.LanguageKind is not RazorLanguageKind.CSharp)
        {
            return NoFurtherHandling;
        }

        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);
        var items = await PrepareTypeHierarchyHandler.PrepareTypeHierarchyAsync(generatedDocument, positionInfo.Position.ToLinePosition(), cancellationToken)
            .ConfigureAwait(false);

        if (items is null)
        {
            return NoFurtherHandling;
        }

        var mappedItems = await MapItemsAsync(context, items, cancellationToken).ConfigureAwait(false);
        return Results(mappedItems);
    }

    public ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSupertypesAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => ResolveSupertypesAsync(context, item, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSupertypesAsync(
        RemoteDocumentContext context,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);
        var items = await TypeHierarchySupertypesHandler.ResolveSupertypesAsync(generatedDocument, item, cancellationToken)
            .ConfigureAwait(false);

        if (items is null)
        {
            return NoFurtherHandling;
        }

        var mappedItems = await MapItemsAsync(context, items, cancellationToken).ConfigureAwait(false);
        return Results(mappedItems);
    }

    public ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSubtypesAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => ResolveSubtypesAsync(context, item, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSubtypesAsync(
        RemoteDocumentContext context,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);
        var items = await TypeHierarchySubtypesHandler.ResolveSubtypesAsync(generatedDocument, item, cancellationToken)
            .ConfigureAwait(false);

        if (items is null)
        {
            return NoFurtherHandling;
        }

        var mappedItems = await MapItemsAsync(context, items, cancellationToken).ConfigureAwait(false);
        return Results(mappedItems);
    }
    private async Task<TypeHierarchyItem[]?> MapItemsAsync(
        RemoteDocumentContext context,
        TypeHierarchyItem[] items,
        CancellationToken cancellationToken)
    {
        using var mappedItems = new PooledArrayBuilder<TypeHierarchyItem>(items.Length);
        foreach (var item in items)
        {
            var mappedItem = await MapItemAsync(context, item, cancellationToken).ConfigureAwait(false);
            if (mappedItem is not null)
            {
                mappedItems.Add(mappedItem);
            }
        }

        return mappedItems.ToArrayAndClear();
    }

    private async Task<TypeHierarchyItem?> MapItemAsync(
        RemoteDocumentContext context,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var uri = item.Uri.GetRequiredSystemUri();

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

        return new TypeHierarchyItem
        {
            Name = item.Name,
            Kind = item.Kind,
            Tags = item.Tags,
            Detail = item.Detail,
            Uri = mappedDocumentUri.CreateDocumentUriFromSystemUri(),
            Range = mappedRange,
            SelectionRange = mappedSelectionRange,
            Data = item.Data,
        };
    }
}
