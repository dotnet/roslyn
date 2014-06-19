// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    public partial class Syntax
    {
        /// <summary>
        /// Returns the nodes in the new tree that do not share the same underlying 
        /// representation in the old tree. These may be entirely new nodes or rebuilt nodes.
        /// </summary>
        /// <param name="oldNode"></param>
        /// <param name="newNode"></param>
        /// <returns></returns>
        internal static IEnumerable<SyntaxNodeOrToken> GetRebuiltNodes(SyntaxNodeOrToken oldNode, SyntaxNodeOrToken newNode)
        {
            var hashSet = new HashSet<InternalSyntax.SyntaxNode>();
            GatherNodes(oldNode.UnderlyingNode.ToGreen(), hashSet);

            var stack = new Stack<ChildSyntaxList.Enumerator>();
            if (!hashSet.Contains(newNode.UnderlyingNode.ToGreen()))
            {
                yield return newNode;
                stack.Push(newNode.Children.GetEnumerator());
            }

            while (stack.Count > 0)
            {
                var newc = stack.Pop();
                if (newc.MoveNext())
                {
                    stack.Push(newc); // put enumerator changes back on stack..
                    newNode = newc.Current;
                    if (!hashSet.Contains(newNode.UnderlyingNode.ToGreen()))
                    {
                        yield return newNode;
                        stack.Push(newNode.Children.GetEnumerator());
                    }
                }
            }
        }

        private static void GatherNodes(InternalSyntax.SyntaxNode node, HashSet<InternalSyntax.SyntaxNode> hashset)
        {
            hashset.Add(node);
            foreach (var child in node.Children)
            {
                GatherNodes(child, hashset);
            }
        }

#if false
        internal static IEnumerable<SyntaxNodeOrToken> GetRebuiltNodes(SyntaxNodeOrToken oldNode, SyntaxNodeOrToken newNode)
        {
            var stack = new Stack<DifferenceInfo>();
            if (oldNode.UnderlyingNode.ToGreen() != newNode.UnderlyingNode.ToGreen())
            {
                yield return newNode;
                stack.Push(new DifferenceInfo(oldNode.Children.GetEnumerator(), newNode.Children.GetEnumerator()));
            }
            while (stack.Count > 0)
            {
                var top = stack.Pop();
                var oldc = top.oldChildren;
                var newc = top.newChildren;
                if (oldc.MoveNext())
                {
                    if (newc.MoveNext())
                    {
                        stack.Push(new DifferenceInfo(oldc, newc)); // put changes back on stack..

                        oldNode = oldc.Current;
                        newNode = newc.Current;

                        if (oldNode.UnderlyingNode.ToGreen() != newNode.UnderlyingNode.ToGreen())
                        {
                            yield return newNode;
                            stack.Push(new DifferenceInfo(oldNode.Children.GetEnumerator(), newNode.Children.GetEnumerator()));
                        }
                    }
                    else
                    {
                        // no more new children.... I guess we are done here
                    }
                }
                else
                {
                    // yield any extra nodes in newChildren list
                    while (newc.MoveNext())
                    {
                        yield return newc.Current;
                    }
                }
            }
        }

        struct DifferenceInfo
        {
            public readonly ChildSyntaxList.Enumerator oldChildren;
            public readonly ChildSyntaxList.Enumerator newChildren;
            public DifferenceInfo(ChildSyntaxList.Enumerator oldChildren, ChildSyntaxList.Enumerator newChildren)
            {
                this.oldChildren = oldChildren;
                this.newChildren = newChildren;
            }
        }
#endif
    }
}