// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class SyntaxDifferences
    {
        /// <summary>
        /// Returns the nodes in the new tree that do not share the same underlying 
        /// representation in the old tree. These may be entirely new nodes or rebuilt nodes.
        /// </summary>
        public static ImmutableArray<SyntaxNodeOrToken> GetRebuiltNodes(SyntaxTree oldTree, SyntaxTree newTree)
        {
            var hashSet = new HashSet<GreenNode>();
            GatherNodes(oldTree.GetCompilationUnitRoot(), hashSet);

            var nodes = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();
            GetRebuiltNodes(newTree.GetCompilationUnitRoot(), hashSet, nodes);
            return nodes.ToImmutableAndFree();
        }

        private static void GetRebuiltNodes(SyntaxNodeOrToken newNode, HashSet<GreenNode> hashSet, ArrayBuilder<SyntaxNodeOrToken> nodes)
        {
            if (hashSet.Contains(newNode.UnderlyingNode))
            {
                return;
            }

            nodes.Add(newNode);

            foreach (var child in newNode.ChildNodesAndTokens())
            {
                GetRebuiltNodes(child, hashSet, nodes);
            }
        }

        private static void GatherNodes(SyntaxNodeOrToken node, HashSet<GreenNode> hashSet)
        {
            hashSet.Add(node.UnderlyingNode);
            foreach (var child in node.ChildNodesAndTokens())
            {
                GatherNodes(child, hashSet);
            }
        }
    }
}
