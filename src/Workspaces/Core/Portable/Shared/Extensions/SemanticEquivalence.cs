// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SemanticEquivalence
    {
        public static bool AreEquivalent(SemanticModel semanticModel, SyntaxNode node1, SyntaxNode node2)
            => AreEquivalent(semanticModel, semanticModel, node1, node2);

        public static bool AreEquivalent(
            SemanticModel semanticModel1,
            SemanticModel semanticModel2,
            SyntaxNode node1,
            SyntaxNode node2,
            Func<SyntaxNode, bool> predicate = null)
        {
            // First check for syntactic equivalency.  If two nodes aren't structurally equivalent,
            // then they're not semantically equivalent.
            if (node1 == null && node2 == null)
            {
                return true;
            }

            if (node1 == null || node2 == null)
            {
                return false;
            }

            if (!node1.IsEquivalentTo(node2, topLevel: false))
            {
                return false;
            }

            // From this point on we can assume the tree structure is the same.  So no need to check
            // kinds, child counts or token contents.
            return AreSemanticallyEquivalentWorker(
                semanticModel1, semanticModel2, node1, node2, predicate);
        }

        private static bool AreSemanticallyEquivalentWorker(
            SemanticModel semanticModel1,
            SemanticModel semanticModel2,
            SyntaxNode node1,
            SyntaxNode node2,
            Func<SyntaxNode, bool> predicate)
        {
            if (node1 == node2)
            {
                return true;
            }

            if (predicate == null || predicate(node1))
            {
                var info1 = semanticModel1.GetSymbolInfo(node1);
                var info2 = semanticModel2.GetSymbolInfo(node2);

                if (!AreEquals(semanticModel1, semanticModel2, info1, info2))
                {
                    return false;
                }
            }

            var e1 = node1.ChildNodesAndTokens().GetEnumerator();
            var e2 = node2.ChildNodesAndTokens().GetEnumerator();

            while (true)
            {
                var b1 = e1.MoveNext();
                var b2 = e2.MoveNext();

                if (b1 != b2)
                {
                    Contract.Fail();
                    return false;
                }

                if (b1 == false)
                {
                    return true;
                }

                var c1 = e1.Current;
                var c2 = e2.Current;

                if (c1.IsNode && c2.IsNode)
                {
                    if (!AreSemanticallyEquivalentWorker(semanticModel1, semanticModel2, c1.AsNode(), c2.AsNode(), predicate))
                    {
                        return false;
                    }
                }
            }
        }

        private static bool AreEquals(
            SemanticModel semanticModel1,
            SemanticModel semanticModel2,
            SymbolInfo info1,
            SymbolInfo info2)
        {
            if (semanticModel1 == semanticModel2)
            {
                return EqualityComparer<ISymbol>.Default.Equals(info1.Symbol, info2.Symbol);
            }
            else
            {
                return SymbolEquivalenceComparer.Instance.Equals(info1.Symbol, info2.Symbol);
            }
        }
    }
}
