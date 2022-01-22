// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

            var span = nodeOrToken.Span;

            if (nodeOrToken.IsToken)
            {
                var token = nodeOrToken.AsToken();
                foreach (var trivia in token.LeadingTrivia)
                {
                    var tspan = trivia.Span;
                    var parentToken = trivia.Token;
                    Assert.Equal(parentToken, token);
                    if (trivia.HasStructure)
                    {
                        var parentTrivia = trivia.GetStructure().Parent;
                        Assert.Null(parentTrivia);
                        CheckParents((CSharpSyntaxNode)trivia.GetStructure(), expectedSyntaxTree);
                    }
                }

                foreach (var trivia in token.TrailingTrivia)
                {
                    var tspan = trivia.Span;
                    var parentToken = trivia.Token;
                    Assert.Equal(parentToken, token);
                    if (trivia.HasStructure)
                    {
                        var parentTrivia = trivia.GetStructure().Parent;
                        Assert.Null(parentTrivia);
                        CheckParents(trivia.GetStructure(), expectedSyntaxTree);
                    }
                }
            }
            else
            {
                var node = nodeOrToken.AsNode();
                foreach (var child in node.ChildNodesAndTokens())
                {
                    var parent = child.Parent;
                    Assert.Equal(node, parent);
                    CheckParents(child, expectedSyntaxTree);
                }
            }
        }
    }
}

