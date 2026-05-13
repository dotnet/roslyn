// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol.Folding;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentFoldingRangeName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostFoldingRangeEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostFoldingRangeEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<FoldingRangeParams, FoldingRange[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostFoldingRangeEndpoint>();

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.FoldingRange?.DynamicRegistration is true)
        {
            return [new Registration()
            {
                Method = Methods.TextDocumentFoldingRangeName,
                RegisterOptions = new FoldingRangeRegistrationOptions()
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(FoldingRangeParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override Task<FoldingRange[]?> HandleRequestAsync(FoldingRangeParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(razorDocument, cancellationToken);

    private async Task<FoldingRange[]?> HandleRequestAsync(TextDocument razorDocument, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Getting folding ranges for {razorDocument.FilePath}");
        // TODO: Should we have a separate method/service for getting C# ranges, so we can kick off both tasks in parallel? Or are we better off transition to OOP once?
        var htmlRangesResult = await GetHtmlFoldingRangesAsync(razorDocument, cancellationToken).ConfigureAwait(false);

        if (htmlRangesResult is not { } htmlRanges)
        {
            // We prefer to return null, so the client will try again
            _logger.LogDebug($"Didn't get any ranges back from Html");
            return null;
        }

        _logger.LogDebug($"Calling OOP with the {htmlRanges.Length} html ranges, so it can fill in the rest");
        var data = await _remoteServiceInvoker.TryInvokeAsync<IRemoteFoldingRangeService, ImmutableArray<RemoteFoldingRange>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetFoldingRangesAsync(solutionInfo, razorDocument.Id, htmlRanges, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (data is [_, ..] allRanges)
        {
            _logger.LogDebug($"Got a total of {allRanges.Length} ranges back from OOP");

            return allRanges.SelectAsPlainArray(RemoteFoldingRange.ToLspFoldingRange);
        }

        return null;
    }

    private async Task<ImmutableArray<RemoteFoldingRange>?> GetHtmlFoldingRangesAsync(TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var foldingRangeParams = new FoldingRangeParams
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = razorDocument.CreateDocumentUri() }
        };

        var result = await _requestInvoker.MakeHtmlLspRequestAsync<FoldingRangeParams, FoldingRange[]>(
            razorDocument,
            Methods.TextDocumentFoldingRangeName,
            foldingRangeParams,
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            _logger.LogDebug($"Didn't get any ranges back from Html. Returning null so we can abandon the whole thing");
            return null;
        }

        return result.SelectAsArray(RemoteFoldingRange.FromLspFoldingRange);
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostFoldingRangeEndpoint instance)
    {
        public Task<FoldingRange[]?> HandleRequestAsync(TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(razorDocument, cancellationToken);
    }
}

