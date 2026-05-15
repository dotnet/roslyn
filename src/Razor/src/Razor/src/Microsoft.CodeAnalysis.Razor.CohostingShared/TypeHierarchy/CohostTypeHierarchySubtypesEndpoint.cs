// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.TypeHierarchy;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TypeHierarchySubtypesName)]
[ExportRazorStatelessLspService(typeof(CohostTypeHierarchySubtypesEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostTypeHierarchySubtypesEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<TypeHierarchySubtypesParams, TypeHierarchyItem[]?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(TypeHierarchySubtypesParams request)
        => RazorTypeHierarchyResolveData.Unwrap(request.Item)?.TextDocument;

    protected override async Task<TypeHierarchyItem[]?> HandleRequestAsync(TypeHierarchySubtypesParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var razorData = RazorTypeHierarchyResolveData.Unwrap(request.Item);
        if (razorData is null)
        {
            return null;
        }

        var unwrappedItem = RazorTypeHierarchyResolveData.WithData(request.Item, razorData.OriginalData);

        var response = await _remoteServiceInvoker.TryInvokeAsync<IRemoteTypeHierarchyService, RemoteResponse<TypeHierarchyItem[]?>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) =>
                service.ResolveSubtypesAsync(solutionInfo, razorDocument.Id, unwrappedItem, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (response.Result is not TypeHierarchyItem[] items)
        {
            return null;
        }

        return Array.ConvertAll(items, static item => WrapRazorItem(item));
    }

    private static TypeHierarchyItem WrapRazorItem(TypeHierarchyItem item)
    {
        var uri = item.Uri.GetRequiredSystemUri();
        return uri.GetDocumentFilePathFromUri().IsRazorFilePath()
            ? RazorTypeHierarchyResolveData.Wrap(item, new TextDocumentIdentifier { DocumentUri = item.Uri })
            : item;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostTypeHierarchySubtypesEndpoint instance)
    {
        public Task<TypeHierarchyItem[]?> HandleRequestAsync(TypeHierarchySubtypesParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
