// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;

namespace Microsoft.VisualStudio.Razor.SyntaxVisualizer;

/// <summary>
/// Wraps a syntax node for projects that don't have IVT to the compiler
/// </summary>
internal class RazorSyntaxNode : IEnumerable<RazorSyntaxNode>
{
    private readonly SyntaxNodeOrToken _nodeOrToken;
    private readonly SyntaxVisualizerNode? _node;

    public int SpanStart => _node?.SpanStart ?? _nodeOrToken.SpanStart;

    public int SpanEnd => _node?.SpanEnd ?? _nodeOrToken.Span.End;

    public int SpanLength => _node is not null
        ? _node.SpanEnd - _node.SpanStart
        : _nodeOrToken.Span.Length;

    public string Kind => _node?.Kind ?? _nodeOrToken.Kind.ToString();

    public RazorSyntaxNodeList Children { get; }

    public RazorSyntaxNode(SyntaxNodeOrToken node)
    {
        _nodeOrToken = node;
        Children = new RazorSyntaxNodeList(_nodeOrToken.ChildNodesAndTokens());
    }

    public RazorSyntaxNode(RazorSyntaxTree tree)
    {
        _nodeOrToken = tree.Root;
        Children = new RazorSyntaxNodeList(_nodeOrToken.ChildNodesAndTokens());
    }

    public RazorSyntaxNode(SyntaxVisualizerNode node)
    {
        _node = node;
        Children = new RazorSyntaxNodeList(_node.Children);
    }

    public IEnumerator<RazorSyntaxNode> GetEnumerator()
    {
        return Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return _node?.ToString() ?? _nodeOrToken.ToString();
    }
}
