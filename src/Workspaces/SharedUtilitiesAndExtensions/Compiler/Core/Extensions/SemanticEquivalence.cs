// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

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

        // Original expression and current node being semantically equivalent isn't enough when the original expression 
        // is a member access via instance reference (either implicit or explicit), the check only ensures that the expression
        // and current node are both backed by the same member symbol. So in this case, in addition to SemanticEquivalence check, 
        // we also check if expression and current node are both instance member access.
        //
        // For example, even though the first `c` binds to a field and we are introducing a local for it,
        // we don't want other references to that field to be replaced as well (i.e. the second `c` in the expression).
        //
        //  class C
        //  {
        //      C c;
        //      void Test()
        //      {
        //          var x = [|c|].c;
        //      }
        //  }
        var originalOperation = semanticModel1.GetOperation(node1);
        if (originalOperation != null && IsInstanceMemberReference(originalOperation))
        {
            var currentOperation = semanticModel2.GetOperation(node2);

            if (currentOperation is null || !IsInstanceMemberReference(currentOperation))
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

            Contract.ThrowIfTrue(b1 != b2);

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

    private static bool IsInstanceMemberReference(IOperation operation)
        => operation is IMemberReferenceOperation { Instance.Kind: OperationKind.InstanceReference };

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
