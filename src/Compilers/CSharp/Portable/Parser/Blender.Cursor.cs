// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial struct Blender
    {
        /// <summary>
        /// THe cursor represents a location in the tree that we can move around to indicate where
        /// we are in the original tree as we're incrementally parsing.  When it is at a node or
        /// token, it can either move forward to that entity's next sibling.  It can also move down
        /// to a node's first child or first token.
        /// 
        /// Once the cursor hits the end of file, it's done.  Note: the cursor will skip any other
        /// zero length nodes in the tree.
        /// </summary>
        private readonly struct Cursor
        {
            public readonly SyntaxNodeOrToken CurrentNodeOrToken;
            private readonly int _indexInParent;

            private Cursor(SyntaxNodeOrToken node, int indexInParent)
            {
                this.CurrentNodeOrToken = node;
                _indexInParent = indexInParent;
            }

            public static Cursor FromRoot(CSharp.CSharpSyntaxNode node)
            {
                return new Cursor(node, indexInParent: 0);
            }

            public bool IsFinished
            {
                get
                {
                    return
                        this.CurrentNodeOrToken.Kind() == SyntaxKind.None ||
                        this.CurrentNodeOrToken.Kind() == SyntaxKind.EndOfFileToken;
                }
            }

            private static bool IsNonZeroWidthOrIsEndOfFile(SyntaxNodeOrToken token)
            {
                return token.Kind() == SyntaxKind.EndOfFileToken || token.FullWidth != 0;
            }

            /// <summary>
            /// Returns the cursor of our next non-empty (or EOF) sibling in our parent if one exists, or `default` if
            /// if doesn't.
            /// </summary>
            private Cursor TryFindNextNonZeroWidthOrIsEndOfFileSibling()
            {
                if (this.CurrentNodeOrToken.Parent != null)
                {
                    // First, look to the nodes to the right of this one in our parent's child list
                    // to get the next sibling.
                    var siblings = this.CurrentNodeOrToken.Parent.ChildNodesAndTokens();
                    for (int i = _indexInParent + 1, n = siblings.Count; i < n; i++)
                    {
                        var sibling = siblings[i];
                        if (IsNonZeroWidthOrIsEndOfFile(sibling))
                        {
                            return new Cursor(sibling, i);
                        }
                    }
                }

                return default(Cursor);
            }

            private Cursor MoveToParent()
            {
                var parent = this.CurrentNodeOrToken.Parent;
                var index = IndexOfNodeInParent(parent);
                return new Cursor(parent, index);
            }

            public static Cursor MoveToNextSibling(Cursor cursor)
            {
                // Iteratively walk over the tree so that we don't stack overflow trying to recurse into anything.
                while (cursor.CurrentNodeOrToken.UnderlyingNode != null)
                {
                    var nextSibling = cursor.TryFindNextNonZeroWidthOrIsEndOfFileSibling();

                    // If we got a valid sibling, return it.
                    if (nextSibling.CurrentNodeOrToken.UnderlyingNode != null)
                        return nextSibling;

                    // We're at the end of this sibling chain.  Walk up to the parent and see who is
                    // the next sibling of that.
                    cursor = cursor.MoveToParent();
                }

                // Couldn't find anything, bail out.
                return default;
            }

            private static int IndexOfNodeInParent(SyntaxNode node)
            {
                if (node.Parent == null)
                {
                    return 0;
                }

                var children = node.Parent.ChildNodesAndTokens();
                var index = SyntaxNodeOrToken.GetFirstChildIndexSpanningPosition(children, ((CSharp.CSharpSyntaxNode)node).Position);
                for (int i = index, n = children.Count; i < n; i++)
                {
                    var child = children[i];
                    if (child == node)
                    {
                        return i;
                    }
                }

                throw ExceptionUtilities.Unreachable();
            }

            public Cursor MoveToFirstChild()
            {
                Debug.Assert(this.CurrentNodeOrToken.IsNode);

                // Just try to get the first node directly.  This is faster than getting the list of
                // child nodes and tokens (which forces all children to be enumerated for the sake
                // of counting.  It should always be safe to index the 0th element of a node.  But
                // just to make sure that this is not a problem, we verify that the slot count of the
                // node is greater than 0.
                var node = CurrentNodeOrToken.AsNode();

                // Interpolated strings cannot be scanned or parsed incrementally. Instead they must be
                // turned into and then reparsed from the single InterpolatedStringToken.  We therefore
                // do not break interpolated string nodes down into their constituent tokens, but
                // instead replace the whole parsed interpolated string expression with its pre-parsed
                // interpolated string token.
                if (node.Kind() == SyntaxKind.InterpolatedStringExpression)
                {
                    var greenToken = Lexer.RescanInterpolatedString((InterpolatedStringExpressionSyntax)node.Green);
                    var redToken = new CodeAnalysis.SyntaxToken(node.Parent, greenToken, node.Position, _indexInParent);
                    return new Cursor(redToken, _indexInParent);
                }

                if (node.SlotCount > 0)
                {
                    var child = Microsoft.CodeAnalysis.ChildSyntaxList.ItemInternal(node, 0);
                    if (IsNonZeroWidthOrIsEndOfFile(child))
                    {
                        return new Cursor(child, 0);
                    }
                }

                // Fallback to enumerating all children.
                int index = 0;
                foreach (var child in this.CurrentNodeOrToken.ChildNodesAndTokens())
                {
                    if (IsNonZeroWidthOrIsEndOfFile(child))
                    {
                        return new Cursor(child, index);
                    }

                    index++;
                }

                return new Cursor();
            }

            public Cursor MoveToFirstToken()
            {
                var cursor = this;
                if (!cursor.IsFinished)
                {
                    for (var node = cursor.CurrentNodeOrToken; node.Kind() != SyntaxKind.None && !SyntaxFacts.IsAnyToken(node.Kind()); node = cursor.CurrentNodeOrToken)
                    {
                        cursor = cursor.MoveToFirstChild();
                    }
                }

                return cursor;
            }
        }
    }
}
