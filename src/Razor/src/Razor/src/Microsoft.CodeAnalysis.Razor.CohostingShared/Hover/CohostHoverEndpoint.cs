// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Roslyn.Text.Adornments;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentHoverName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostHoverEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostHoverEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker)
    : AbstractCohostDocumentEndpoint<TextDocumentPositionParams, LspHover>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Hover?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.TextDocumentHoverName,
                RegisterOptions = new HoverRegistrationOptions()
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(TextDocumentPositionParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<LspHover?> HandleRequestAsync(TextDocumentPositionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var position = LspFactory.CreatePosition(request.Position.ToLinePosition());

        var razorResponse = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteHoverService, RemoteResponse<LspHover?>>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.GetHoverAsync(solutionInfo, razorDocument.Id, position, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (razorResponse.StopHandling)
        {
            return razorResponse.Result;
        }

        var htmlHover = await _requestInvoker.MakeHtmlLspRequestAsync<TextDocumentPositionParams, LspHover>(
            razorDocument,
            Methods.TextDocumentHoverName,
            request,
            cancellationToken).ConfigureAwait(false);

        return MergeHtmlAndRazorHoverResponses(razorResponse.Result, htmlHover);
    }

    private static LspHover? MergeHtmlAndRazorHoverResponses(LspHover? razorHover, LspHover? htmlHover)
    {
        if (razorHover is null)
        {
            return htmlHover;
        }

        if (htmlHover is null
            || htmlHover.Range != razorHover.Range)
        {
            return razorHover;
        }

        var htmlStringResponse = htmlHover.Contents.Match(
            static s => s,
            static markedString => null,
            static stringOrMarkedStringArray => null,
            static markupContent => markupContent.Value
        );

        if (htmlStringResponse is null)
        {
            return razorHover;
        }

        // This logic is to prepend HTML hover content to the razor hover content if both exist.
        // The razor content comes through as a ContainerElement, while the html content comes
        // through as MarkupContent. We need to extract the html content and insert it at the
        // start of the combined ContainerElement.
        if (razorHover is VSInternalHover razorVsInternalHover
            && razorVsInternalHover.RawContent is ContainerElement razorContainerElement)
        {
            var htmlStringClassifiedTextElement = ClassifiedTextElement.CreatePlainText(htmlStringResponse);
            var verticalSpacingTextElement = ClassifiedTextElement.CreatePlainText(string.Empty);
            var htmlContainerElement = new ContainerElement(
                ContainerElementStyle.Stacked,
                [htmlStringClassifiedTextElement, verticalSpacingTextElement]);

            // Modify the existing hover's RawContent to prepend the HTML content.
            razorVsInternalHover.RawContent = new ContainerElement(razorContainerElement.Style, [htmlContainerElement, .. razorContainerElement.Elements]);
        }
        else
        {
            var razorStringResponse = razorHover.Contents.Match(
                static s => s,
                static markedString => throw new NotImplementedException(),
                static stringOrMarkedStringArray => throw new NotImplementedException(),
                static markupContent => markupContent.Value
            );

            if (razorStringResponse is not null)
            {
                razorHover.Contents = new MarkupContent()
                {
                    Kind = MarkupKind.Markdown,
                    Value = htmlStringResponse + "\n\n---\n\n" + razorStringResponse
                };
            }
        }

        return razorHover;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostHoverEndpoint instance)
    {
        public Task<LspHover?> HandleRequestAsync(
            TextDocumentPositionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
