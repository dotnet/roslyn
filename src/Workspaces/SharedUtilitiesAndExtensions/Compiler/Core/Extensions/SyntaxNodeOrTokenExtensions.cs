// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class SyntaxNodeOrTokenExtensions
{
    public static IEnumerable<SyntaxNodeOrToken> DepthFirstTraversal(this SyntaxNodeOrToken node)
    {
        using var pooledStack = SharedPools.Default<Stack<SyntaxNodeOrToken>>().GetPooledObject();
        var stack = pooledStack.Object;
        stack.Push(node);

        while (!stack.IsEmpty())
        {
            var current = stack.Pop();

            yield return current;

            if (current.IsNode)
            {
                foreach (var child in current.ChildNodesAndTokens().Reverse())
                    stack.Push(child);
            }
        }
    }

    public static IEnumerable<SyntaxNode> DepthFirstTraversalNodes(this SyntaxNodeOrToken node)
    {
        foreach (var t in node.DepthFirstTraversal())
        {
            if (t.IsNode)
                yield return t.AsNode()!;
        }
    }

    public static SyntaxTrivia[] GetTrivia(params SyntaxNodeOrToken[] nodesOrTokens)
        => nodesOrTokens.SelectMany(nodeOrToken => nodeOrToken.GetLeadingTrivia().Concat(nodeOrToken.GetTrailingTrivia())).ToArray();

    public static SyntaxNodeOrToken WithAppendedTrailingTrivia(this SyntaxNodeOrToken nodeOrToken, params SyntaxTrivia[] trivia)
        => WithAppendedTrailingTrivia(nodeOrToken, (IEnumerable<SyntaxTrivia>)trivia);

    public static SyntaxNodeOrToken WithAppendedTrailingTrivia(this SyntaxNodeOrToken nodeOrToken, IEnumerable<SyntaxTrivia> trivia)
        => nodeOrToken.IsNode ? nodeOrToken.AsNode()!.WithAppendedTrailingTrivia(trivia) : nodeOrToken.AsToken().WithAppendedTrailingTrivia(trivia);
}
