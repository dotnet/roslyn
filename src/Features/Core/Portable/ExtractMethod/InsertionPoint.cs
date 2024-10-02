// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal sealed class InsertionPoint
{
    private readonly SyntaxAnnotation _annotation;
    private readonly Lazy<SyntaxNode?> _context;

    public InsertionPoint(SemanticDocument document, SyntaxAnnotation annotation)
    {
        Contract.ThrowIfNull(document);
        Contract.ThrowIfNull(annotation);

        SemanticDocument = document;
        _annotation = annotation;
        _context = CreateLazyContextNode();
    }

    public SemanticDocument SemanticDocument { get; }

    public SyntaxNode GetRoot()
        => SemanticDocument.Root;

    public SyntaxNode? GetContext()
        => _context.Value;

    public InsertionPoint With(SemanticDocument document)
        => new(document, _annotation);

    private Lazy<SyntaxNode?> CreateLazyContextNode()
        => new(ComputeContextNode, isThreadSafe: true);

    private SyntaxNode? ComputeContextNode()
    {
        var root = SemanticDocument.Root;
        return root.GetAnnotatedNodesAndTokens(_annotation).SingleOrDefault().AsNode();
    }
}
