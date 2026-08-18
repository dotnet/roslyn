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
    private readonly Solution? _initialSolution;
    private readonly TextDocument? _initialTextDocument;

    internal RequestContext(LspRequestContext context)
    {
        _context = context;
        _initialSolution = context.GetInitialSolution();
        _initialTextDocument = context.GetInitialTextDocument();
    }

    /// <inheritdoc cref="LspRequestContext.Workspace"/>
    internal Workspace? Workspace => _context.Workspace;

    [Obsolete("Use GetSolutionAsync instead.", error: false)]
    internal Solution? Solution => _initialSolution;

    [Obsolete("Use GetDocumentAsync instead.", error: false)]
    internal Document? Document => GetInitialDocument();

    [Obsolete("Use GetRequiredDocumentAsync instead.", error: false)]
    internal Document GetRequiredDocument()
        => GetInitialDocument() ?? throw new ArgumentNullException($"{nameof(Document)} is null when it was required for {_context.Method}");

    internal ValueTask<Solution?> GetSolutionAsync(CancellationToken cancellationToken)
        => _context.GetSolutionAsync(cancellationToken);

    internal ValueTask<Document?> GetDocumentAsync(CancellationToken cancellationToken)
        => _context.GetDocumentAsync(cancellationToken);

    internal ValueTask<Document> GetRequiredDocumentAsync(CancellationToken cancellationToken)
        => _context.GetRequiredDocumentAsync(cancellationToken);

    internal T GetRequiredService<T>() where T : class => _context.GetRequiredService<T>();

    private Document? GetInitialDocument()
        => _initialTextDocument switch
        {
            null => null,
            Document document => document,
            _ => throw new InvalidOperationException("Attempted to retrieve a Document but a TextDocument was found instead."),
        };
}
