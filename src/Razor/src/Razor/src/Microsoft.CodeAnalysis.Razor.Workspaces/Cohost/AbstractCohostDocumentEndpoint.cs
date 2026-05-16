// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
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

    public Task<TResponse?> HandleRequestAsync(TRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        if (context.TextDocument is null)
        {
            _incompatibleProjectService.HandleMissingDocument(GetRazorTextDocumentIdentifier(request), context);

            return SpecializedTasks.Default<TResponse>();
        }

        return HandleRequestAsync(request, context, context.TextDocument, cancellationToken);
    }

    protected virtual Task<TResponse?> HandleRequestAsync(TRequest request, RequestContext context, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(request, razorDocument, cancellationToken);

    protected abstract TextDocumentIdentifier? GetRazorTextDocumentIdentifier(TRequest request);

    protected abstract Task<TResponse?> HandleRequestAsync(TRequest request, TextDocument razorDocument, CancellationToken cancellationToken);
}
