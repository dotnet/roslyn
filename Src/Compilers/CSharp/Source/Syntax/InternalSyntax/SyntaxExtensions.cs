// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System.Linq;

namespace Roslyn.Compilers.CSharp.InternalSyntax
{
    internal static class SyntaxExtensions
    {
        public static TNode WithDiagnostics<TNode>(this TNode node, params DiagnosticInfo[] diagnostics) where TNode : SyntaxNode
        {
            return (TNode)node.SetDiagnostics(diagnostics);
        }

        public static TNode WithAdditionalDiagnostics<TNode>(this TNode node, params DiagnosticInfo[] diagnostics) where TNode : SyntaxNode
        {
            return (TNode)node.SetDiagnostics(node.GetDiagnostics().Concat(diagnostics).ToArray());
        }

        public static TNode WithoutDiagnostics<TNode>(this TNode node) where TNode : SyntaxNode
        {
            var current = node.GetDiagnostics();
            if (current == null || current.Length == 0)
            {
                return node;
            }

            return (TNode)node.SetDiagnostics(null);
        }
    }
}