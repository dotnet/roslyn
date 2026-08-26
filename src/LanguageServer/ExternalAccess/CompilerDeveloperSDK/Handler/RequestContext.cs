// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

internal readonly struct RequestContext
{
    private readonly LspRequestContext _context;

    internal RequestContext(LspRequestContext context)
    {
        _context = context;
    }

    [Obsolete("Use GetWorkspaceAsync instead.", error: false)]
    internal Workspace? Workspace
        => _context.GetWorkspaceSynchronously();

    [Obsolete("Use GetSolutionAsync instead.", error: false)]
    internal Solution? Solution
        => _context.GetSolutionSynchronously();

    [Obsolete("Use GetDocumentAsync instead.", error: false)]
    internal Document? Document
        => _context.GetDocumentSynchronously();

    [Obsolete("Use GetRequiredDocumentAsync instead.", error: false)]
    internal Document GetRequiredDocument()
        => _context.GetRequiredDocumentSynchronously();

    internal ValueTask<Solution?> GetSolutionAsync(CancellationToken cancellationToken)
        => _context.GetSolutionAsync(cancellationToken);

    internal ValueTask<Workspace?> GetWorkspaceAsync(CancellationToken cancellationToken)
        => _context.GetWorkspaceAsync(cancellationToken);

    internal ValueTask<Workspace> GetRequiredWorkspaceAsync(CancellationToken cancellationToken)
        => _context.GetRequiredWorkspaceAsync(cancellationToken);

    internal ValueTask<Document?> GetDocumentAsync(CancellationToken cancellationToken)
        => _context.GetDocumentAsync(cancellationToken);

    internal ValueTask<Document> GetRequiredDocumentAsync(CancellationToken cancellationToken)
        => _context.GetRequiredDocumentAsync(cancellationToken);

    internal T GetRequiredService<T>() where T : class => _context.GetRequiredService<T>();
}
