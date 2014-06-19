using System.Collections.Generic;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    public partial class CommonSyntaxNode
    {
        private class TriviaResolver : CommonSyntaxWalker<List<CommonSyntaxTrivia>>
        {
            private readonly SyntaxAnnotation annotation;

            public static IEnumerable<CommonSyntaxTrivia> Resolve(CommonSyntaxNode root, SyntaxAnnotation annotation)
            {
                Contract.ThrowIfNull(root);
                Contract.ThrowIfNull(annotation);

                var result = new List<CommonSyntaxTrivia>();
                var resolver = new TriviaResolver(annotation);
                resolver.Visit(root, result);

                return result;
            }

            private TriviaResolver(SyntaxAnnotation annotation) :
                base(visitIntoStructuredTrivia: true)
            {
                this.annotation = annotation;
            }

            public override void Visit(CommonSyntaxNode node, List<CommonSyntaxTrivia> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                var baseNode = (IBaseSyntaxNodeExt)node;
                if (!baseNode.HasAnnotations)
                {
                    return;
                }

                base.Visit(node, results);
            }

            protected override void VisitToken(CommonSyntaxToken token, List<CommonSyntaxTrivia> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                if (!token.Node.HasAnnotations)
                {
                    return;
                }

                base.VisitToken(token, results);
            }

            protected override void VisitTrivia(CommonSyntaxTrivia trivia, List<CommonSyntaxTrivia> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                if (!trivia.UnderlyingNode.HasAnnotations)
                {
                    return;
                }

                var annotations = trivia.UnderlyingNode.GetAnnotations();
                for (int i = 0; i < annotations.Length; i++)
                {
                    if (annotations[i] == annotation)
                    {
                        results.Add(trivia);
                        break;
                    }
                }

                base.VisitTrivia(trivia, results);
            }
        }
    }
}