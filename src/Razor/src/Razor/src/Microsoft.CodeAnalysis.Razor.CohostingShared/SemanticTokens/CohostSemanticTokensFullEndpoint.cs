// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentSemanticTokensFullName)]
[ExportRazorStatelessLspService(typeof(CohostSemanticTokensFullEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostSemanticTokensFullEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker)
    : AbstractCohostDocumentEndpoint<SemanticTokensFullParams, SemanticTokens?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    protected override bool MutatesSolutionState => false;
    protected override bool RequiresLSPSolution => true;

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(SemanticTokensFullParams request)
        => request.TextDocument;

    protected override async Task<SemanticTokens?> HandleRequestAsync(SemanticTokensFullParams request, RequestContext context, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var result = await HandleRequestAsync(razorDocument, cancellationToken).ConfigureAwait(false);

        if (result is not null)
        {
            // Roslyn uses frozen semantics for semantic tokens, so it could return results from an older project state.
            // Every time they get a request they queue up a refresh, which will check the project checksums, and if there
            // hasn't been any changes, will no-op. We call into that same logic here to ensure everything is up to date.
            // See: https://github.com/dotnet/roslyn/blob/bb57f4643bb3d52eb7626f9863da177d9e219f1e/src/LanguageServer/Protocol/Handler/SemanticTokens/SemanticTokensHelpers.cs#L48-L52
            var semanticTokensWrapperService = context.GetRequiredService<IRazorSemanticTokensRefreshQueue>();
            await semanticTokensWrapperService.TryEnqueueRefreshComputationAsync(razorDocument.Project, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    protected override Task<SemanticTokens?> HandleRequestAsync(SemanticTokensFullParams request, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(razorDocument, cancellationToken);

    private async Task<SemanticTokens?> HandleRequestAsync(TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var sourceText = await razorDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // Full semantic tokens requests always cover the entire document.
        var span = new LinePositionSpan(new(0, 0), new(sourceText.Lines.Count, 0));

        var tokens = await _remoteServiceInvoker.TryInvokeAsync<IRemoteSemanticTokensService, int[]?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetSemanticTokensDataAsync(solutionInfo, razorDocument.Id, span, correlationId: Guid.Empty, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (tokens is not null)
        {
            return new SemanticTokens
            {
                Data = tokens
            };
        }

        return null;
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostSemanticTokensFullEndpoint instance)
    {
        public Task<SemanticTokens?> HandleRequestAsync(TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(razorDocument, cancellationToken);
    }
}
