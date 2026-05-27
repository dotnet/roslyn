// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.TypeHierarchy;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.PrepareTypeHierarchyName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostPrepareTypeHierarchyEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostPrepareTypeHierarchyEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<TypeHierarchyPrepareParams, TypeHierarchyItem[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.TypeHierarchy?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.PrepareTypeHierarchyName,
                RegisterOptions = new TypeHierarchyRegistrationOptions()
            }];
        }

        return [];
    }

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(TypeHierarchyPrepareParams request)
        => request.TextDocument;

    protected override async Task<TypeHierarchyItem[]?> HandleRequestAsync(TypeHierarchyPrepareParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var response = await _remoteServiceInvoker.TryInvokeAsync<IRemoteTypeHierarchyService, RemoteResponse<TypeHierarchyItem[]?>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) =>
                service.PrepareTypeHierarchyAsync(solutionInfo, razorDocument.Id, request.Position, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (response.Result is not TypeHierarchyItem[] items)
        {
            return null;
        }

        return Array.ConvertAll(items, item => WrapRazorItem(item, request.TextDocument));
    }

    private static TypeHierarchyItem WrapRazorItem(TypeHierarchyItem item, TextDocumentIdentifier textDocument)
    {
        var uri = item.Uri;
        return uri.GetDocumentFilePathFromUri().IsRazorFilePath()
            ? RazorTypeHierarchyResolveData.Wrap(item, textDocument.WithUri(uri))
            : item;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostPrepareTypeHierarchyEndpoint instance)
    {
        public Task<TypeHierarchyItem[]?> HandleRequestAsync(TypeHierarchyPrepareParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
