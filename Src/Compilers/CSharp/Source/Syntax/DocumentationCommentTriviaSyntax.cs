using System.Text;
using Roslyn.Compilers.Common;

namespace Roslyn.Compilers.CSharp
{
    public partial class DocumentationCommentTriviaSyntax : StructuredTriviaSyntax
    {
        public string GetInteriorXml()
        {
            // This could be implemented in the GreenNodes only with an override of WriteTo, and it
            // would be faster.

            StringBuilder sb = new StringBuilder();
            this.Accept(new InteriorXmlWalker(sb));
            return sb.ToString();
        }

        private class InteriorXmlWalker : SyntaxWalker
        {
            private readonly StringBuilder arg;

            public InteriorXmlWalker(StringBuilder arg)
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                this.arg = arg;
            }

            public override void VisitToken(SyntaxToken token)
            {
                if (token.Kind == SyntaxKind.XmlTextLiteralNewLineToken)
                {
                    return;
                }

                VisitLeadingTrivia(token);
                arg.Append(token.ToString());
                VisitTrailingTrivia(token);
            }

            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.Kind != SyntaxKind.DocumentationCommentExteriorTrivia)
                {
                    arg.Append(trivia.ToString());
                }
            }
        }
    }
}
