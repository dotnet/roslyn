﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpParenthesesReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpParenthesesReducer() : base(s_pool)
        {
        }

        private static readonly Func<ParenthesizedExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyParentheses = SimplifyParentheses;

        private static SyntaxNode SimplifyParentheses(
            ParenthesizedExpressionSyntax node,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            if (node.CanRemoveParentheses(semanticModel))
            {
                // TODO(DustinCa): We should not be skipping elastic trivia below.
                // However, the formatter seems to mess up trailing trivia in some
                // cases if elastic trivia is there -- and it's not clear why.
                // Specifically remove the elastic trivia formatting rule doesn't
                // have any effect.

                var leadingTrivia = node.OpenParenToken.LeadingTrivia
                    .Concat(node.OpenParenToken.TrailingTrivia)
                    .Where(t => !t.IsElastic())
                    .Concat(node.Expression.GetLeadingTrivia());

                var trailingTrivia = node.Expression.GetTrailingTrivia()
                    .Concat(node.CloseParenToken.LeadingTrivia)
                    .Where(t => !t.IsElastic())
                    .Concat(node.CloseParenToken.TrailingTrivia);

                var resultNode = node.Expression
                    .WithLeadingTrivia(leadingTrivia)
                    .WithTrailingTrivia(trailingTrivia);

                resultNode = SimplificationHelpers.CopyAnnotations(from: node, to: resultNode);

                return resultNode;
            }

            // We don't know how to simplify this.
            return node;
        }
    }
}
