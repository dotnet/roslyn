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
internal readonly struct CopilotRequestContext
{
    private readonly RequestContext _context;
    private readonly Solution? _initialSolution;
    private readonly TextDocument? _initialTextDocument;

    public CopilotRequestContext(RequestContext context)
    {
        _context = context;
        _initialSolution = context.GetInitialSolution();
        _initialTextDocument = context.GetInitialTextDocument();
    }

    /// <summary>
    /// The solution state that the request should operate on.
    /// </summary>
    [Obsolete("Use GetSolutionAsync instead.", error: false)]
    public Solution Solution => _initialSolution ?? throw new InvalidOperationException();

    [Obsolete("Use GetDocumentAsync instead.", error: false)]
    public Document? Document
        => _initialTextDocument switch
        {
            null => null,
            Document document => document,
            _ => throw new InvalidOperationException("Attempted to retrieve a Document but a TextDocument was found instead."),
        };

    public ValueTask<Solution> GetSolutionAsync(CancellationToken cancellationToken)
        => _context.GetRequiredSolutionAsync(cancellationToken);

    public ValueTask<Document?> GetDocumentAsync(CancellationToken cancellationToken)
        => _context.GetDocumentAsync(cancellationToken);

    public T GetRequiredService<T>() where T : class => _context.GetRequiredService<T>();
}
