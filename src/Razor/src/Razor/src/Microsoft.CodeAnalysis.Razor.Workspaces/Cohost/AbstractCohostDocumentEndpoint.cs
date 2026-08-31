// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.Razor.Cohost;

internal abstract class AbstractCohostDocumentEndpoint<TRequest, TResponse>(
    IIncompatibleProjectService incompatibleProjectService) : ILspServiceRequestHandler<TRequest, TResponse?>, ITextDocumentIdentifierHandler<TRequest, TextDocumentIdentifier?>
{
    private readonly IIncompatibleProjectService _incompatibleProjectService = incompatibleProjectService;

    bool IMethodHandler.MutatesSolutionState => MutatesSolutionState;

    bool ISolutionRequiredHandler.RequiresLSPSolution => RequiresLSPSolution;

    TextDocumentIdentifier? ITextDocumentIdentifierHandler<TRequest, TextDocumentIdentifier?>.GetTextDocumentIdentifier(TRequest request)
        => GetRazorTextDocumentIdentifier(request);

    protected abstract bool MutatesSolutionState { get; }

    protected abstract bool RequiresLSPSolution { get; }

    public async Task<TResponse?> HandleRequestAsync(TRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var textDocument = await context.GetTextDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (textDocument is null)
        {
            await _incompatibleProjectService.HandleMissingDocumentAsync(GetRazorTextDocumentIdentifier(request), context, cancellationToken).ConfigureAwait(false);

            return default;
        }

        return await HandleRequestAsync(request, context, textDocument, cancellationToken).ConfigureAwait(false);
    }

    protected virtual Task<TResponse?> HandleRequestAsync(TRequest request, RequestContext context, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(request, razorDocument, cancellationToken);

    protected abstract TextDocumentIdentifier? GetRazorTextDocumentIdentifier(TRequest request);

    protected abstract Task<TResponse?> HandleRequestAsync(TRequest request, TextDocument razorDocument, CancellationToken cancellationToken);
}
