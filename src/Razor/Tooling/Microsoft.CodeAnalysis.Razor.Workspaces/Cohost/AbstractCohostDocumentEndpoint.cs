// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

namespace Microsoft.CodeAnalysis.Razor.Cohost;

internal abstract class AbstractCohostDocumentEndpoint<TRequest, TResponse>(
    IIncompatibleProjectService incompatibleProjectService) : AbstractRazorCohostDocumentRequestHandler<TRequest, TResponse?>
{
    private readonly IIncompatibleProjectService _incompatibleProjectService = incompatibleProjectService;

    protected sealed override Task<TResponse?> HandleRequestAsync(TRequest request, RazorCohostRequestContext context, CancellationToken cancellationToken)
    {
        if (context.TextDocument is null)
        {
            _incompatibleProjectService.HandleMissingDocument(GetRazorTextDocumentIdentifier(request), context);

            return SpecializedTasks.Default<TResponse>();
        }

        return HandleRequestAsync(request, context, context.TextDocument, cancellationToken);
    }

    protected virtual Task<TResponse?> HandleRequestAsync(TRequest request, RazorCohostRequestContext context, TextDocument razorDocument, CancellationToken cancellationToken)
        => HandleRequestAsync(request, razorDocument, cancellationToken);

    protected abstract Task<TResponse?> HandleRequestAsync(TRequest request, TextDocument razorDocument, CancellationToken cancellationToken);
}
