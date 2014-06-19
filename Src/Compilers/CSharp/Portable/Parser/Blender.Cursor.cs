// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    partial struct Blender
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
        private struct Cursor
        {
            public readonly SyntaxNodeOrToken CurrentNodeOrToken;
            private readonly int indexInParent;

            private Cursor(SyntaxNodeOrToken node, int indexInParent)
            {
                this.CurrentNodeOrToken = node;
                this.indexInParent = indexInParent;
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
                        this.CurrentNodeOrToken.CSharpKind() == SyntaxKind.None ||
                        this.CurrentNodeOrToken.CSharpKind() == SyntaxKind.EndOfFileToken;
                }
            }

            private static bool IsNonZeroWidthOrIsEndOfFile(SyntaxNodeOrToken token)
            {
                return token.CSharpKind() == SyntaxKind.EndOfFileToken || token.FullWidth != 0;
            }

            public Cursor MoveToNextSibling()
            {
                if (this.CurrentNodeOrToken.Parent != null)
                {
                    // First, look to the nodes to the right of this one in our parent's child list
                    // to get the next sibling.
                    var siblings = this.CurrentNodeOrToken.Parent.ChildNodesAndTokens();
                    for (int i = indexInParent + 1, n = siblings.Count; i < n; i++)
                    {
                        var sibling = siblings[i];
                        if (IsNonZeroWidthOrIsEndOfFile(sibling))
                        {
                            return new Cursor(sibling, i);
                        }
                    }

                    // We're at the end of this sibling chain.  Walk up to the parent and see who is
                    // the next sibling of that.
                    return MoveToParent().MoveToNextSibling();
                }

                return default(Cursor);
            }

            private Cursor MoveToParent()
            {
                var parent = this.CurrentNodeOrToken.Parent;
                var index = IndexOfNodeInParent(parent);
                return new Cursor(parent, index);
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

                throw ExceptionUtilities.Unreachable;
            }

            public Cursor MoveToFirstChild()
            {
                Debug.Assert(this.CurrentNodeOrToken.IsNode);

                // Just try to get the first node directly.  This is faster than getting the list of
                // child nodes and tokens (which forces all children to be enumerated for the sake
                // of counting.  It should always be safe to index the 0th element of a node.  But
                // just to make sure that this is not a problem, we verify that the slotcount of the
                // node is greater than 0.
                var node = CurrentNodeOrToken.AsNode();
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
                    for (var node = cursor.CurrentNodeOrToken; node.CSharpKind() != SyntaxKind.None && !SyntaxFacts.IsAnyToken(node.CSharpKind()); node = cursor.CurrentNodeOrToken)
                    {
                        cursor = cursor.MoveToFirstChild();
                    }
                }

                return cursor;
            }
        }
    }
}
