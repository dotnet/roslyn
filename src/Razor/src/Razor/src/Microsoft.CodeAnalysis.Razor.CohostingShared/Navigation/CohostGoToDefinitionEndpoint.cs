// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Remote.GoToDefinitionResponse?>;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentDefinitionName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostGoToDefinitionEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostGoToDefinitionEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker)
    : AbstractCohostDocumentEndpoint<TextDocumentPositionParams, SumType<LspLocation, LspLocation[], DocumentLink[]>?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Definition?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.TextDocumentDefinitionName,
                RegisterOptions = new DefinitionRegistrationOptions()
            }];
        }

        return [];
    }

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(TextDocumentPositionParams request)
        => request.TextDocument;

    protected override async Task<SumType<LspLocation, LspLocation[], DocumentLink[]>?> HandleRequestAsync(TextDocumentPositionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var position = LspFactory.CreatePosition(request.Position.ToLinePosition());

        var response = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteGoToDefinitionService, Response>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.GetDefinitionsAsync(solutionInfo, razorDocument.Id, position, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (response == Response.NoFurtherHandling)
        {
            return null;
        }

        if (response == Response.CallHtml)
        {
            return await GetHtmlDefinitionsAsync(request, razorDocument, cancellationToken).ConfigureAwait(false);
        }

        // Razor OOP found definition locations it could return directly.
        if (response is { StopHandling: false, Result: { Locations: { } locations, CSharpRequest: null } })
        {
            return locations;
        }

        // Razor OOP found a navigable metadata symbol that must be resolved in the host.
        if (response is { StopHandling: false, Result: { Locations: null, CSharpRequest: { } csharpRequest } })
        {
            return await GetCSharpDefinitionsAsync(razorDocument, csharpRequest, cancellationToken).ConfigureAwait(false);
        }

        // Any other combination represents a malformed response.
        throw new InvalidOperationException($"Invalid go-to-definition response: {response}");
    }

    private static async Task<LspLocation[]?> GetCSharpDefinitionsAsync(
        TextDocument razorDocument,
        TextDocumentPositionParams request,
        CancellationToken cancellationToken)
    {
        var generatedDocument = await razorDocument.Project.Solution
            .TryGetSourceGeneratedDocumentAsync(request.TextDocument.DocumentUri, cancellationToken)
            .ConfigureAwait(false);

        if (generatedDocument is null)
        {
            return null;
        }

        var solution = generatedDocument.Project.Solution;
        var globalOptions = solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var metadataAsSourceFileService = solution.Services.ExportProvider.GetService<IMetadataAsSourceFileService>();

        // OOP already ran this helper with metadata-as-source disabled. If it found a source location,
        // it returned that result directly and we would not get this far. Repeat the lookup in the host
        // workspace with metadata-as-source enabled so the remaining metadata symbol can use host-only
        // services such as SourceLink.
        var locations = await AbstractGoToDefinitionHandler.GetDefinitionsAsync(
            globalOptions,
            metadataAsSourceFileService,
            solution.Workspace,
            generatedDocument,
            forSymbolType: false,
            request.Position.ToLinePosition(),
            cancellationToken).ConfigureAwait(false);

        return locations;
    }

    private async Task<SumType<LspLocation, LspLocation[], DocumentLink[]>?> GetHtmlDefinitionsAsync(TextDocumentPositionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var result = await _requestInvoker
            .MakeHtmlLspRequestAsync<TextDocumentPositionParams, SumType<LspLocation, LspLocation[], DocumentLink[]>>(
                razorDocument,
                Methods.TextDocumentDefinitionName,
                request,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Value is null)
        {
            return null;
        }

        if (result.TryGetFirst(out var singleLocation))
        {
            return LspFactory.CreateLocation(RemapVirtualHtmlUri(singleLocation.DocumentUri), singleLocation.Range.ToLinePositionSpan());
        }
        else if (result.TryGetSecond(out var multipleLocations))
        {
            return Array.ConvertAll(multipleLocations, l => LspFactory.CreateLocation(RemapVirtualHtmlUri(l.DocumentUri), l.Range.ToLinePositionSpan()));
        }
        else if (result.TryGetThird(out var documentLinks))
        {
            using var builder = new PooledArrayBuilder<DocumentLink>(capacity: documentLinks.Length);

            foreach (var documentLink in documentLinks)
            {
                if (documentLink.DocumentTarget is DocumentUri target)
                {
                    builder.Add(LspFactory.CreateDocumentLink(RemapVirtualHtmlUri(target), documentLink.Range.ToLinePositionSpan()));
                }
            }

            return builder.ToArray();
        }

        return null;
    }

    private DocumentUri RemapVirtualHtmlUri(DocumentUri uri)
    {
        if (uri.IsRazorHtmlDocumentUri(out var razorUri))
        {
            return razorUri;
        }

        return uri;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostGoToDefinitionEndpoint instance)
    {
        public Task<SumType<LspLocation, LspLocation[], DocumentLink[]>?> HandleRequestAsync(
            TextDocumentPositionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
