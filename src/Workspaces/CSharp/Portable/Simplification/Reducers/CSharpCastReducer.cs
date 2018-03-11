// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal partial class CSharpCastReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpCastReducer() : base(s_pool)
        {
        }

        private static readonly Func<CastExpressionSyntax, SemanticModel, OptionSet, CancellationToken, ExpressionSyntax> s_simplifyCast = SimplifyCast;

        private static ExpressionSyntax SimplifyCast(CastExpressionSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
        {
            if (!node.IsUnnecessaryCast(semanticModel, cancellationToken))
            {
                return node;
            }

            var leadingTrivia = node.OpenParenToken.LeadingTrivia
                .Concat(node.OpenParenToken.TrailingTrivia)
                .Concat(node.Type.GetLeadingTrivia())
                .Concat(node.Type.GetTrailingTrivia())
                .Concat(node.CloseParenToken.LeadingTrivia)
                .Concat(node.CloseParenToken.TrailingTrivia)
                .Concat(node.Expression.GetLeadingTrivia())
                .Where(t => !t.IsElastic());

            var trailingTrivia = node.GetTrailingTrivia().Where(t => !t.IsElastic());

            var resultNode = node.Expression
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);

            resultNode = SimplificationHelpers.CopyAnnotations(from: node, to: resultNode);

            return resultNode;
        }
    }
}
