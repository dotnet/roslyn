// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System.Text;

namespace Roslyn.Compilers.CSharp
{
    public partial class XmlDocCommentSyntax : StructuredTriviaSyntax
    {
        public string GetInteriorXml()
        {
            // This could be implemented in the GreenNodes only with an
            // override of WriteTo, and it would be faster.

            StringBuilder sb = new StringBuilder();
            this.Accept(new InteriorXmlWalker(), sb);
            return sb.ToString();
        }

        private class InteriorXmlWalker : SyntaxWalker<StringBuilder>
        {
            public override void VisitToken(SyntaxToken token, StringBuilder arg)
            {
                VisitLeadingTrivia(token, arg);
                arg.Append(token.GetText());
                VisitTrailingTrivia(token, arg);
            }

            public override void VisitTrivia(SyntaxTrivia trivia, StringBuilder arg)
            {
                if (trivia.Kind != SyntaxKind.XmlDocCommentExteriorTrivia)
                {
                    arg.Append(trivia.GetText());
                }
            }
        }
    }
}
