// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxNavigator
{
    private static Func<SyntaxToken, bool> GetPredicateFunction(bool includeZeroWidth)
    {
        return includeZeroWidth ? SyntaxToken.Any : SyntaxToken.NonZeroWidth;
    }

    private static bool Matches(Func<SyntaxToken, bool>? predicate, SyntaxToken token)
    {
        return predicate == null || ReferenceEquals(predicate, SyntaxToken.Any) || predicate(token);
    }

    internal static SyntaxToken GetFirstToken(SyntaxNode current, bool includeZeroWidth)
    {
        return GetFirstToken(current, GetPredicateFunction(includeZeroWidth));
    }

    internal static SyntaxToken GetLastToken(SyntaxNode current, bool includeZeroWidth)
    {
        return GetLastToken(current, GetPredicateFunction(includeZeroWidth));
    }

    internal static SyntaxToken GetPreviousToken(SyntaxToken current, bool includeZeroWidth)
    {
        return GetPreviousToken(current, GetPredicateFunction(includeZeroWidth));
    }

    internal static SyntaxToken GetNextToken(SyntaxToken current, bool includeZeroWidth)
    {
        return GetNextToken(current, GetPredicateFunction(includeZeroWidth));
    }

    internal static SyntaxToken GetFirstToken(SyntaxNode current, Func<SyntaxToken, bool>? predicate)
    {
        using var stack = new PooledArrayBuilder<ChildSyntaxList.Enumerator>();
        stack.Push(current.ChildNodesAndTokens().GetEnumerator());

        while (stack.Count > 0)
        {
            var en = stack.Pop();
            if (en.MoveNext())
            {
                var child = en.Current;

                if (child.IsToken)
                {
                    var token = GetFirstToken(child.AsToken(), predicate);
                    if (token.Kind != SyntaxKind.None)
                    {
                        return token;
                    }
                }

                // push this enumerator back, not done yet
                stack.Push(en);

                if (!child.IsToken)
                {
                    stack.Push(child.ChildNodesAndTokens().GetEnumerator());
                }
            }
        }

        return default;
    }

    private static SyntaxToken GetFirstToken(SyntaxToken token, Func<SyntaxToken, bool>? predicate)
    {
        if (Matches(predicate, token))
        {
            return token;
        }

        return default;
    }

    internal static SyntaxToken GetLastToken(SyntaxNode current, Func<SyntaxToken, bool> predicate)
    {
        using var stack = new PooledArrayBuilder<ChildSyntaxList.Reversed.Enumerator>();
        stack.Push(current.ChildNodesAndTokens().Reverse().GetEnumerator());

        while (stack.Count > 0)
        {
            var en = stack.Pop();

            if (en.MoveNext())
            {
                var child = en.Current;

                if (child.IsToken)
                {
                    var token = GetLastToken(child.AsToken(), predicate);
                    if (token.Kind != SyntaxKind.None)
                    {
                        return token;
                    }
                }

                // push this enumerator back, not done yet
                stack.Push(en);

                if (!child.IsToken)
                {
                    stack.Push(child.ChildNodesAndTokens().Reverse().GetEnumerator());
                }
            }
        }

        return default;
    }

    private static SyntaxToken GetLastToken(SyntaxToken token, Func<SyntaxToken, bool> predicate)
    {
        if (Matches(predicate, token))
        {
            return token;
        }

        return default;
    }

    internal static SyntaxToken GetNextToken(SyntaxToken current, Func<SyntaxToken, bool>? predicate)
    {
        if (current.Parent != null)
        {
            // walk forward in parent's child list until we find ourself
            // and then return the next token
            var returnNext = false;
            foreach (var child in current.Parent.ChildNodesAndTokens())
            {
                if (returnNext)
                {
                    if (child.IsToken)
                    {
                        var token = GetFirstToken(child.AsToken(), predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        Debug.Assert(child.IsNode);
                        var token = GetFirstToken(child.AsNode()!, predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                }
                else if (child.IsToken && child.AsToken() == current)
                {
                    returnNext = true;
                }
            }

            // otherwise get next token from the parent's parent, and so on
            return GetNextToken(current.Parent, predicate);
        }

        return default;
    }

    internal static SyntaxToken GetNextToken(SyntaxNode node, Func<SyntaxToken, bool>? predicate)
    {
        while (node.Parent != null)
        {
            // walk forward in parent's child list until we find ourselves and then return the
            // next token
            var returnNext = false;
            foreach (var child in node.Parent.ChildNodesAndTokens())
            {
                if (returnNext)
                {
                    if (child.IsToken)
                    {
                        var token = GetFirstToken(child.AsToken(), predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        Debug.Assert(child.IsNode);
                        var token = GetFirstToken(child.AsNode()!, predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                }
                else if (child == node)
                {
                    returnNext = true;
                }
            }

            // didn't find the next token in my parent's children, look up the tree
            node = node.Parent;
        }

        return default;
    }

    internal static SyntaxToken GetPreviousToken(SyntaxToken current, Func<SyntaxToken, bool> predicate)
    {
        if (current.Parent != null)
        {
            // walk backward in parent's child list until we find ourself
            // and then return the next token
            var returnPrevious = false;
            foreach (var child in current.Parent.ChildNodesAndTokens().Reverse())
            {
                if (returnPrevious)
                {
                    if (child.IsToken)
                    {
                        var token = GetLastToken(child.AsToken(), predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        Debug.Assert(child.IsNode);
                        var token = GetLastToken(child.AsNode()!, predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                }
                else if (child.IsToken && child.AsToken() == current)
                {
                    returnPrevious = true;
                }
            }

            // otherwise get next token from the parent's parent, and so on
            return GetPreviousToken(current.Parent, predicate);
        }

        return default;
    }

    internal static SyntaxToken GetPreviousToken(SyntaxNode node, Func<SyntaxToken, bool> predicate)
    {
        while (node.Parent != null)
        {
            // walk backward in parent's child list until we find ourselves and then return the
            // previous token
            var returnPrevious = false;
            foreach (var child in node.Parent.ChildNodesAndTokens().Reverse())
            {
                if (returnPrevious)
                {
                    if (child.IsToken)
                    {
                        var token = GetLastToken(child.AsToken(), predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                    else
                    {
                        Debug.Assert(child.IsNode);
                        var token = GetLastToken(child.AsNode()!, predicate);
                        if (token.Kind != SyntaxKind.None)
                        {
                            return token;
                        }
                    }
                }
                else if (child == node)
                {
                    returnPrevious = true;
                }
            }

            // didn't find the previous token in my parent's children, look up the tree
            node = node.Parent;
        }

        return default;
    }
}
