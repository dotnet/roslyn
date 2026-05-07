// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol.InlayHints;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.InlayHintResolveName)]
[ExportRazorStatelessLspService(typeof(CohostInlayHintResolveEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal class CohostInlayHintResolveEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<InlayHint, InlayHint?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostInlayHintResolveEndpoint>();

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(InlayHint request)
        => GetTextDocumentIdentifier(request)?.ToRazorTextDocumentIdentifier() ?? null;

    private TextDocumentIdentifier? GetTextDocumentIdentifier(InlayHint request)
    {
        var data = GetInlayHintResolveData(request);
        if (data is null)
        {
            _logger.LogError($"Got a resolve request for an inlay hint but couldn't extract the data object. Raw data is: {request.Data}");
            return null;
        }

        return data.TextDocument;
    }

    protected override async Task<InlayHint?> HandleRequestAsync(InlayHint request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var razorData = GetInlayHintResolveData(request).AssumeNotNull();
        var razorPosition = request.Position;
        request.Data = razorData.OriginalData;
        request.Position = razorData.OriginalPosition;

        var hint = await _remoteServiceInvoker.TryInvokeAsync<IRemoteInlayHintService, InlayHint>(
           razorDocument.Project.Solution,
           (service, solutionInfo, cancellationToken) => service.ResolveHintAsync(solutionInfo, razorDocument.Id, request, cancellationToken),
           cancellationToken).ConfigureAwait(false);

        if (hint is null)
        {
            return null;
        }

        Debug.Assert(request.Position == hint.Position, "Resolving inlay hints should not change the position of them.");
        hint.Position = razorPosition;

        return hint;
    }

    private static InlayHintDataWrapper? GetInlayHintResolveData(InlayHint inlayHint)
    {
        if (inlayHint.Data is InlayHintDataWrapper wrapper)
        {
            return wrapper;
        }

        if (inlayHint.Data is JsonElement json)
        {
            return JsonSerializer.Deserialize<InlayHintDataWrapper>(json);
        }

        return null;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostInlayHintResolveEndpoint instance)
    {
        public TextDocumentIdentifier? GetTextDocumentIdentifier(InlayHint request)
            => instance.GetTextDocumentIdentifier(request);

        public Task<InlayHint?> HandleRequestAsync(InlayHint request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
