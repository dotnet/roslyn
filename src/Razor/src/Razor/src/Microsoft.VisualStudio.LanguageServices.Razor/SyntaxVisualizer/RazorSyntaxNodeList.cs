// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;

namespace Microsoft.VisualStudio.Razor.SyntaxVisualizer;

internal class RazorSyntaxNodeList : IEnumerable<RazorSyntaxNode>
{
    private readonly ChildSyntaxList _childSyntaxList;
    private readonly SyntaxVisualizerNode[]? _children;

    public RazorSyntaxNodeList(ChildSyntaxList childSyntaxList)
    {
        _childSyntaxList = childSyntaxList;
    }

    public RazorSyntaxNodeList(SyntaxVisualizerNode[] children)
    {
        _children = children;
    }

    public IEnumerator<RazorSyntaxNode> GetEnumerator()
    {
        if (_children is not null)
        {
            foreach (var child in _children)
            {
                yield return new RazorSyntaxNode(child);
            }
        }
        else
        {
            foreach (var nodeOrToken in _childSyntaxList)
            {
                yield return new RazorSyntaxNode(nodeOrToken);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
