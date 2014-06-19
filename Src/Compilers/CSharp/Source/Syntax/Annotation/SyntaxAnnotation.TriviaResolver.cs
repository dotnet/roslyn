#if false
using System.Collections.Generic;
using Roslyn.Compilers.Internal;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    public partial class SyntaxAnnotation
    {
        private class TriviaResolver : SyntaxWalker<List<SyntaxTrivia>>
        {
            private readonly SyntaxAnnotation annotation;

            public static IEnumerable<SyntaxTrivia> Resolve(SyntaxNode root, SyntaxAnnotation annotation)
            {
                Contract.ThrowIfNull(root);
                Contract.ThrowIfNull(annotation);

                var result = new List<SyntaxTrivia>();
                var resolver = new TriviaResolver(annotation);
                resolver.Visit(root, result);

                return result;
            }

            private TriviaResolver(SyntaxAnnotation annotation) :
                base(visitIntoStructuredTrivia: true)
            {
                this.annotation = annotation;
            }

            public override object Visit(SyntaxNode node, List<SyntaxTrivia> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                if (!node.HasAnnotations)
                {
                    return null;
                }

                return base.Visit(node, results);
            }

            public override void VisitToken(SyntaxToken token, List<SyntaxTrivia> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                if (!token.HasAnnotations)
                {
                    return;
                }

                base.VisitToken(token, results);
            }

            public override void VisitTrivia(SyntaxTrivia trivia, List<SyntaxTrivia> results)
            {
                // if it doesnt have annotations, don't even bother to go in.
                if (!trivia.HasAnnotations)
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
#endif