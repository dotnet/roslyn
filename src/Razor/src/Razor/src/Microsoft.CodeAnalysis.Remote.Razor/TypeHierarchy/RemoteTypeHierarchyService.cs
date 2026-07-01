// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.Handler.TypeHierarchy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;
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

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    public ValueTask<RemoteResponse<TypeHierarchyItem[]?>> PrepareTypeHierarchyAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => PrepareTypeHierarchyAsync(snapshot, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<TypeHierarchyItem[]?>> PrepareTypeHierarchyAsync(
        RemoteDocumentSnapshot snapshot,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

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

        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(positionInfo.InDeclDocument, cancellationToken).ConfigureAwait(false);
        var items = await PrepareTypeHierarchyHandler.PrepareTypeHierarchyAsync(generatedDocument, positionInfo.Position.ToLinePosition(), cancellationToken)
            .ConfigureAwait(false);

        if (items is null)
        {
            return NoFurtherHandling;
        }

        var mappedItems = await MapItemsAsync(snapshot, items, cancellationToken).ConfigureAwait(false);
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
            snapshot => ResolveSupertypesAsync(snapshot, item, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSupertypesAsync(
        RemoteDocumentSnapshot snapshot,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await TryGetGeneratedDocumentForItemAsync(snapshot, item, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return NoFurtherHandling;
        }

        var items = await TypeHierarchySupertypesHandler.ResolveSupertypesAsync(generatedDocument, item, cancellationToken)
            .ConfigureAwait(false);

        if (items is null)
        {
            return NoFurtherHandling;
        }

        var mappedItems = await MapItemsAsync(snapshot, items, cancellationToken).ConfigureAwait(false);
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
            snapshot => ResolveSubtypesAsync(snapshot, item, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSubtypesAsync(
        RemoteDocumentSnapshot snapshot,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await TryGetGeneratedDocumentForItemAsync(snapshot, item, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return NoFurtherHandling;
        }

        var items = await TypeHierarchySubtypesHandler.ResolveSubtypesAsync(generatedDocument, item, cancellationToken)
            .ConfigureAwait(false);

        if (items is null)
        {
            return NoFurtherHandling;
        }

        var mappedItems = await MapItemsAsync(snapshot, items, cancellationToken).ConfigureAwait(false);
        return Results(mappedItems);
    }

    private static async ValueTask<SourceGeneratedDocument?> TryGetGeneratedDocumentForItemAsync(
        RemoteDocumentSnapshot snapshot,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
    {
        var resolveData = TypeHierarchyHelpers.GetResolveData(item);
        var generatedDocumentUri = resolveData.TextDocument.DocumentUri.GetRequiredSystemUri();
        if (!snapshot.TextDocument.Project.Solution.TryGetSourceGeneratedDocumentIdentity(generatedDocumentUri, out var identity))
        {
            return null;
        }

        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetCSharpDocumentForHintName(identity.HintName);

        return await snapshot.GetGeneratedDocumentAsync(csharpDocument.IsDeclarationDocument, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TypeHierarchyItem[]?> MapItemsAsync(
        RemoteDocumentSnapshot snapshot,
        TypeHierarchyItem[] items,
        CancellationToken cancellationToken)
    {
        using var mappedItems = new PooledArrayBuilder<TypeHierarchyItem>(items.Length);
        foreach (var item in items)
        {
            var mappedItem = await MapItemAsync(snapshot, item, cancellationToken).ConfigureAwait(false);
            if (mappedItem is not null)
            {
                mappedItems.Add(mappedItem);
            }
        }

        return mappedItems.ToArrayAndClear();
    }

    private async Task<TypeHierarchyItem?> MapItemAsync(
        RemoteDocumentSnapshot snapshot,
        TypeHierarchyItem item,
        CancellationToken cancellationToken)
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

        return new TypeHierarchyItem
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
}
