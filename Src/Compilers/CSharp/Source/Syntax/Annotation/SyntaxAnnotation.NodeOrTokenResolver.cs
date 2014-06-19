#if false
using System.Collections.Generic;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    public partial class SyntaxAnnotation
    {
        private class NodeOrTokenResolver : SyntaxWalker<List<SyntaxNodeOrToken>>
        {
            private readonly SyntaxAnnotation annotation;

            public static IEnumerable<SyntaxNodeOrToken> Resolve(SyntaxNode root, SyntaxAnnotation annotation)
            {
                Contract.ThrowIfNull(root);
                Contract.ThrowIfNull(annotation);

                var result = new List<SyntaxNodeOrToken>();
                var resolver = new NodeOrTokenResolver(annotation);
                resolver.Visit(root, result);

                return result;
            }

            private NodeOrTokenResolver(SyntaxAnnotation annotation) :
                base(visitIntoStructuredTrivia: true)
            {
                this.annotation = annotation;
            }

            public override object Visit(SyntaxNode node, List<SyntaxNodeOrToken> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                if (!node.HasAnnotations)
                {
                    return null;
                }

                var annotations = node.GetAnnotations();
                AddNodeIfAnnotationExist(annotations, node, results);

                return base.Visit(node, results);
            }

            public override void VisitToken(SyntaxToken token, List<SyntaxNodeOrToken> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                if (!token.HasAnnotations)
                {
                    return;
                }

                var annotations = token.Node.GetAnnotations();
                AddNodeIfAnnotationExist(annotations, token, results);

                base.VisitToken(token, results);
            }

            private void AddNodeIfAnnotationExist(
                SyntaxAnnotation[] annotations,
                SyntaxNodeOrToken nodeOrToken,
                List<SyntaxNodeOrToken> results)
            {
                for (int i = 0; i < annotations.Length; i++)
                {
                    if (annotations[i] == annotation)
                    {
                        results.Add(nodeOrToken);
                        return;
                    }
                }
            }
        }
    }
}
#endif