// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Copilot;

/// <summary>
/// Context for requests handled by <see cref="AbstractCopilotLspServiceDocumentRequestHandler{TRequest, TResponse}"/>
/// </summary>
internal readonly struct CopilotRequestContext(RequestContext context)
{
    /// <summary>
    /// The solution state that the request should operate on.
    /// </summary>
    [Obsolete("Use GetSolutionAsync instead.", error: false)]
    public Solution Solution => context.GetRequiredSolutionAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    [Obsolete("Use GetDocumentAsync instead.", error: false)]
    public Document? Document => context.GetDocumentAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    public ValueTask<Solution> GetSolutionAsync(CancellationToken cancellationToken)
        => context.GetRequiredSolutionAsync(cancellationToken);

    public ValueTask<Document?> GetDocumentAsync(CancellationToken cancellationToken)
        => context.GetDocumentAsync(cancellationToken);

    public T GetRequiredService<T>() where T : class => context.GetRequiredService<T>();
}
