// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Razor.LanguageClient.WrapWithTag;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(LanguageServerConstants.RazorWrapWithTagEndpoint)]
[ExportCohostStatelessLspService(typeof(CohostWrapWithTagEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostWrapWithTagEndpoint(
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker,
    LSPDocumentManager documentManager,
    IIncompatibleProjectService incompatibleProjectService,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<VSInternalWrapWithTagParams, VSInternalWrapWithTagResponse?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly LSPDocumentManager _documentManager = documentManager;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostWrapWithTagEndpoint>();

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(VSInternalWrapWithTagParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<VSInternalWrapWithTagResponse?> HandleRequestAsync(VSInternalWrapWithTagParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        // First, check if the position is valid for wrap with tag operation through the remote service
        var range = request.Range.ToLinePositionSpan();
        var result = await _remoteServiceInvoker.TryInvokeAsync<IRemoteWrapWithTagService, RemoteResponse<LinePositionSpan>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetValidWrappingRangeAsync(solutionInfo, razorDocument.Id, range, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        // If the remote service says it's not a valid location or we should stop handling, return null
        if (result.StopHandling)
        {
            return null;
        }

        request.Range = result.Result.ToRange();

        // The location is valid, so delegate to the HTML server
        var htmlResponse = await _requestInvoker.MakeHtmlLspRequestAsync<VSInternalWrapWithTagParams, VSInternalWrapWithTagResponse>(
            razorDocument,
            LanguageServerConstants.RazorWrapWithTagEndpoint,
            request,
            cancellationToken).ConfigureAwait(false);

        // If the Html response has ~s in it, then we need to clean them up.
        if (htmlResponse?.TextEdits is { } edits &&
            edits.Any(static e => e.NewText.Contains('~')))
        {
            // To do this we don't actually need to go to OOP, we just need a SourceText with the Html document,
            // and we already have that in a virtual buffer, because it's what the above request was made against.
            // So we can write a little bit of code to grab that, and avoid the extra OOP call.

            if (!_documentManager.TryGetDocument(razorDocument.CreateUri(), out var snapshot))
            {
                _logger.LogError($"Couldn't find document in LSPDocumentManager for {razorDocument.FilePath}");
                return null;
            }

            if (!snapshot.TryGetVirtualDocument<HtmlVirtualDocumentSnapshot>(out var htmlDocument))
            {
                _logger.LogError($"Couldn't find virtual document snapshot for {snapshot.Uri}");
                return null;
            }

            var htmlSourceText = htmlDocument.Snapshot.AsText();
            htmlResponse.TextEdits = FormattingUtilities.FixHtmlTextEdits(htmlSourceText, edits);
        }

        return htmlResponse;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostWrapWithTagEndpoint instance)
    {
        public Task<VSInternalWrapWithTagResponse?> HandleRequestAsync(VSInternalWrapWithTagParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
