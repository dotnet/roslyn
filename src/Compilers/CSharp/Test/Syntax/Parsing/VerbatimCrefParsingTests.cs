// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class VerbatimCrefParsingTests : ParsingTests
    {
        public VerbatimCrefParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            throw new NotSupportedException();
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options)
        {
            var commentText = string.Format(@"/// <see cref=""{0}""/>", text);
            var trivia = SyntaxFactory.ParseLeadingTrivia(commentText).Single();
            var structure = (DocumentationCommentTriviaSyntax)trivia.GetStructure();
            var attr = structure.DescendantNodes().OfType<XmlTextAttributeSyntax>().Single();
            return attr;
        }

        [Fact]
        public void NoEscapes()
        {
            UsingNode("T:NotARealType");

            N(SyntaxKind.XmlTextAttribute);
            {
                N(SyntaxKind.XmlName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.DoubleQuoteToken);
                N(SyntaxKind.XmlTextLiteralToken);
                N(SyntaxKind.DoubleQuoteToken);
            }
            EOF();
        }

        [Fact]
        public void EscapedKind()
        {
            UsingNode("&#84;:NotARealType");

            N(SyntaxKind.XmlTextAttribute);
            {
                N(SyntaxKind.XmlName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.DoubleQuoteToken);
                N(SyntaxKind.XmlEntityLiteralToken);
                N(SyntaxKind.XmlTextLiteralToken);
                N(SyntaxKind.DoubleQuoteToken);
            }
            EOF();
        }

        [Fact]
        public void EscapedColon()
        {
            UsingNode("T&#58;NotARealType");

            N(SyntaxKind.XmlTextAttribute);
            {
                N(SyntaxKind.XmlName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.DoubleQuoteToken);
                N(SyntaxKind.XmlTextLiteralToken);
                N(SyntaxKind.XmlEntityLiteralToken);
                N(SyntaxKind.XmlTextLiteralToken);
                N(SyntaxKind.DoubleQuoteToken);
            }
            EOF();
        }

        [Fact]
        public void EscapedKindAndColon()
        {
            UsingNode("&#84;&#58;NotARealType");

            N(SyntaxKind.XmlTextAttribute);
            {
                N(SyntaxKind.XmlName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.DoubleQuoteToken);
                N(SyntaxKind.XmlEntityLiteralToken);
                N(SyntaxKind.XmlEntityLiteralToken);
                N(SyntaxKind.XmlTextLiteralToken);
                N(SyntaxKind.DoubleQuoteToken);
            }
            EOF();
        }
    }
}
