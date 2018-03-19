// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public static class ParentChecker
    {
        public static void CheckParents(SyntaxNodeOrToken nodeOrToken, SyntaxTree expectedSyntaxTree)
        {
            Assert.Equal(expectedSyntaxTree, nodeOrToken.SyntaxTree);

            TextSpan span = nodeOrToken.Span;

            if (nodeOrToken.IsToken)
            {
                SyntaxToken token = nodeOrToken.AsToken();
                foreach (SyntaxTrivia trivia in token.LeadingTrivia)
                {
                    TextSpan tspan = trivia.Span;
                    SyntaxToken parentToken = trivia.Token;
                    Assert.Equal(parentToken, token);
                    if (trivia.HasStructure)
                    {
                        SyntaxNode parentTrivia = trivia.GetStructure().Parent;
                        Assert.Null(parentTrivia);
                        CheckParents((CSharpSyntaxNode)trivia.GetStructure(), expectedSyntaxTree);
                    }
                }

                foreach (SyntaxTrivia trivia in token.TrailingTrivia)
                {
                    TextSpan tspan = trivia.Span;
                    SyntaxToken parentToken = trivia.Token;
                    Assert.Equal(parentToken, token);
                    if (trivia.HasStructure)
                    {
                        SyntaxNode parentTrivia = trivia.GetStructure().Parent;
                        Assert.Null(parentTrivia);
                        CheckParents(trivia.GetStructure(), expectedSyntaxTree);
                    }
                }
            }
            else
            {
                SyntaxNode node = nodeOrToken.AsNode();
                foreach (SyntaxNodeOrToken child in node.ChildNodesAndTokens())
                {
                    SyntaxNode parent = child.Parent;
                    Assert.Equal(node, parent);
                    CheckParents(child, expectedSyntaxTree);
                }
            }
        }
    }
}

