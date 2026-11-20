// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

internal readonly struct RequestContext(LspRequestContext context)
{
    [Obsolete("Use GetRequiredWorkspaceAsync instead.", error: false)]
    internal Workspace? Workspace => context.GetRequiredWorkspaceAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    [Obsolete("Use GetRequiredSolutionAsync instead.", error: false)]
    internal Solution? Solution => context.GetRequiredSolutionAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    [Obsolete("Use GetDocumentAsync instead.", error: false)]
    internal Document? Document => context.GetDocumentAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    [Obsolete("Use GetRequiredDocumentAsync instead.", error: false)]
    internal Document GetRequiredDocument() => context.GetRequiredDocumentAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();

    [Obsolete("Use GetRequiredSolutionAsync instead.", error: false)]
    internal async ValueTask<Solution?> GetSolutionAsync(CancellationToken cancellationToken)
        => await context.GetRequiredSolutionAsync(cancellationToken).ConfigureAwait(false);

    [Obsolete("Use GetRequiredWorkspaceAsync instead.", error: false)]
    internal async ValueTask<Workspace?> GetWorkspaceAsync(CancellationToken cancellationToken)
        => await context.GetRequiredWorkspaceAsync(cancellationToken).ConfigureAwait(false);

    internal ValueTask<Workspace> GetRequiredWorkspaceAsync(CancellationToken cancellationToken)
        => context.GetRequiredWorkspaceAsync(cancellationToken);

    internal ValueTask<Solution> GetRequiredSolutionAsync(CancellationToken cancellationToken)
        => context.GetRequiredSolutionAsync(cancellationToken);

    internal ValueTask<Document?> GetDocumentAsync(CancellationToken cancellationToken)
        => context.GetDocumentAsync(cancellationToken);

    internal ValueTask<Document> GetRequiredDocumentAsync(CancellationToken cancellationToken)
        => context.GetRequiredDocumentAsync(cancellationToken);

    internal T GetRequiredService<T>() where T : class => context.GetRequiredService<T>();
}
