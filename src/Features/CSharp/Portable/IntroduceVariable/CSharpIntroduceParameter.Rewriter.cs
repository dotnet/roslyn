// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    internal partial class CSharpIntroduceParameterService
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly SyntaxAnnotation _replacementAnnotation = new();
            private readonly SyntaxNode _replacementNode;
            private readonly ISet<ExpressionSyntax> _matches;

            private Rewriter(SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
            {
                _replacementNode = replacementNode;
                _matches = matches;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node is ExpressionSyntax expression &&
                    _matches.Contains(expression))
                {
                    return _replacementNode
                        .WithTriviaFrom(expression)
                        .WithAdditionalAnnotations(_replacementAnnotation);
                }

                return base.Visit(node);
            }

            public static SyntaxNode Visit(SyntaxNode node, SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
                => new Rewriter(replacementNode, matches).Visit(node);
        }
    }
}
