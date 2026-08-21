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
    private readonly Workspace? _initialWorkspace;
    private readonly Solution? _initialSolution;
    private readonly TextDocument? _initialTextDocument;
    private readonly Document? _initialDocument;

    internal RequestContext(LspRequestContext context)
    {
        _context = context;
        _initialWorkspace = context.GetInitialWorkspace();
        _initialSolution = context.GetInitialSolution();
        _initialTextDocument = context.GetInitialTextDocument();
        _initialDocument = _initialTextDocument as Document;
    }

    [Obsolete("Use GetWorkspaceAsync instead.", error: false)]
    internal Workspace? Workspace => _initialWorkspace;

    [Obsolete("Use GetSolutionAsync instead.", error: false)]
    internal Solution? Solution => _initialSolution;

    [Obsolete("Use GetDocumentAsync instead.", error: false)]
    internal Document? Document => _initialDocument;

    [Obsolete("Use GetRequiredDocumentAsync instead.", error: false)]
    internal Document GetRequiredDocument()
        => _initialDocument ?? throw new ArgumentNullException($"{nameof(Document)} is null when it was required for {_context.Method}");

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
