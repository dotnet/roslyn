using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
#if REMOVE
    internal static class SyntaxNodeExtensions
    {
        public static TNode WithAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : CSharpSyntaxNode
        {
            if (annotations == null) throw new ArgumentNullException("annotations");
            return (TNode)node.SetAnnotations(annotations);
        }

        public static TNode WithAdditionalAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : CSharpSyntaxNode
        {
            if (annotations == null) throw new ArgumentNullException("annotations");
            return (TNode)node.SetAnnotations(node.GetAnnotations().Concat(annotations).ToArray());
        }

        public static TNode WithoutAnnotations<TNode>(this TNode node, params SyntaxAnnotation[] removalAnnotations) where TNode : CSharpSyntaxNode
        {
            var newAnnotations = ArrayBuilder<SyntaxAnnotation>.GetInstance();
            var annotations = node.GetAnnotations();
            foreach (var candidate in annotations)
            {
                if (Array.IndexOf(removalAnnotations, candidate) < 0)
                {
                    newAnnotations.Add(candidate);
                }
            }
            return (TNode)node.SetAnnotations(newAnnotations.ToArrayAndFree());
        }

        public static TNode WithDiagnostics<TNode>(this TNode node, params DiagnosticInfo[] diagnostics) where TNode : CSharpSyntaxNode
        {
            return (TNode)node.SetDiagnostics(diagnostics);
        }

        public static TNode WithoutDiagnostics<TNode>(this TNode node) where TNode : CSharpSyntaxNode
        {
            var current = node.GetDiagnostics();
            if (current == null || current.Length == 0)
            {
                return node;
            }

            return (TNode)node.SetDiagnostics(null);
        }
    }
#endif
}