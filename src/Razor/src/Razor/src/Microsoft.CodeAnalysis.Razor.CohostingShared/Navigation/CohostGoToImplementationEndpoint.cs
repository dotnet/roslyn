// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentImplementationName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostGoToImplementationEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostGoToImplementationEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker,
    IFilePathService filePathService)
    : AbstractCohostDocumentEndpoint<TextDocumentPositionParams, SumType<LspLocation[], VSInternalReferenceItem[]>?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly IFilePathService _filePathService = filePathService;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Implementation?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.TextDocumentImplementationName,
                RegisterOptions = new ImplementationRegistrationOptions()
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(TextDocumentPositionParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<SumType<LspLocation[], VSInternalReferenceItem[]>?> HandleRequestAsync(TextDocumentPositionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var position = LspFactory.CreatePosition(request.Position.ToLinePosition());

        var response = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteGoToImplementationService, RemoteResponse<LspLocation[]?>>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.GetImplementationAsync(solutionInfo, razorDocument.Id, position, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (response.Result is LspLocation[] locations)
        {
            return locations;
        }

        if (response.StopHandling)
        {
            return null;
        }

        return await GetHtmlImplementationsAsync(request, razorDocument, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SumType<LspLocation[], VSInternalReferenceItem[]>?> GetHtmlImplementationsAsync(TextDocumentPositionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var result = await _requestInvoker
            .MakeHtmlLspRequestAsync<TextDocumentPositionParams, SumType<LspLocation[], VSInternalReferenceItem[]>>(
                razorDocument,
                Methods.TextDocumentImplementationName,
                request,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Value is null)
        {
            return null;
        }

        if (result.TryGetFirst(out var locations))
        {
            foreach (var location in locations)
            {
                RemapVirtualHtmlUri(location);
            }

            return locations;
        }
        else if (result.TryGetSecond(out var referenceItems))
        {
            foreach (var referenceItem in referenceItems)
            {
                RemapVirtualHtmlUri(referenceItem.Location);
            }

            return referenceItems;
        }

        return null;
    }

    private void RemapVirtualHtmlUri(LspLocation? location)
    {
        if (location?.DocumentUri.ParsedUri is { } uri &&
            _filePathService.IsVirtualHtmlFile(uri))
        {
            location.DocumentUri = new(_filePathService.GetRazorDocumentUri(uri));
        }
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostGoToImplementationEndpoint instance)
    {
        public Task<SumType<LspLocation[], VSInternalReferenceItem[]>?> HandleRequestAsync(
            TextDocumentPositionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
