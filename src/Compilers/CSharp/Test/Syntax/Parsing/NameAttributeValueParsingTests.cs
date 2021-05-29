// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Compilers.CSharp.UnitTests
{
    public class NameAttributeValueParsingTests : ParsingTests
    {
        public NameAttributeValueParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            throw new NotSupportedException();
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options)
        {
            var commentText = string.Format(@"/// <param name=""{0}""/>", text);
            var trivia = SyntaxFactory.ParseLeadingTrivia(commentText).Single();
            var structure = (DocumentationCommentTriviaSyntax)trivia.GetStructure();
            var attr = structure.DescendantNodes().OfType<XmlNameAttributeSyntax>().Single();
            return attr.Identifier;
        }

        [Fact]
        public void Identifier()
        {
            UsingNode("A");

            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken);
            }

            EOF();
        }

        [Fact]
        public void Keyword()
        {
            UsingNode("int");

            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken);
            }

            EOF();
        }

        [Fact]
        public void Empty()
        {
            UsingNode("");

            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }

            EOF();
        }

        [Fact]
        public void Whitespace()
        {
            UsingNode(" ");

            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }

            EOF();
        }

        [Fact]
        public void Qualified()
        {
            // Everything after the first identifier is skipped.

            UsingNode("A.B");

            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken);
            }

            EOF();
        }

        [Fact]
        public void Generic()
        {
            // Everything after the first identifier is skipped.

            UsingNode("A{T}");

            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken);
            }

            EOF();
        }

        [Fact]
        public void Punctuation()
        {
            // A missing identifier is inserted and everything is skipped.

            UsingNode(".");

            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }

            EOF();
        }
    }
}
