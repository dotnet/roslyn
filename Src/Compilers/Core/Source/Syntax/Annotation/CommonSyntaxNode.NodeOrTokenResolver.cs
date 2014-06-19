using System.Collections.Generic;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    public partial class CommonSyntaxNode
    {
        private class NodeOrTokenResolver : CommonSyntaxWalker<List<CommonSyntaxNodeOrToken>>
        {
            private readonly SyntaxAnnotation annotation;

            public static IEnumerable<CommonSyntaxNodeOrToken> Resolve(CommonSyntaxNode root, SyntaxAnnotation annotation)
            {
                Contract.ThrowIfNull(root);
                Contract.ThrowIfNull(annotation);

                var result = new List<CommonSyntaxNodeOrToken>();
                var resolver = new NodeOrTokenResolver(annotation);
                resolver.Visit(root, result);

                return result;
            }

            private NodeOrTokenResolver(SyntaxAnnotation annotation) :
                base(visitIntoStructuredTrivia: true)
            {
                this.annotation = annotation;
            }

            public override void Visit(CommonSyntaxNode node, List<CommonSyntaxNodeOrToken> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                var baseNode = (IBaseSyntaxNodeExt)node;
                if (!baseNode.HasAnnotations)
                {
                    return;
                }

                var annotations = baseNode.GetAnnotations();
                AddNodeIfAnnotationExist(annotations, node, results);

                base.Visit(node, results);
            }

            protected override void VisitToken(CommonSyntaxToken token, List<CommonSyntaxNodeOrToken> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                if (!token.Node.HasAnnotations)
                {
                    return;
                }

                var annotations = token.Node.GetAnnotations();
                AddNodeIfAnnotationExist(annotations, token, results);

                base.VisitToken(token, results);
            }

            private void AddNodeIfAnnotationExist(
                SyntaxAnnotation[] annotations,
                CommonSyntaxNodeOrToken nodeOrToken,
                List<CommonSyntaxNodeOrToken> results)
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
