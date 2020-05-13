// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpParenthesizedExpressionReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpParenthesizedExpressionReducer() : base(s_pool)
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
                var resultNode = CSharpSyntaxFacts.Instance.Unparenthesize(node);
                return SimplificationHelpers.CopyAnnotations(from: node, to: resultNode);
            }

            // We don't know how to simplify this.
            return node;
        }
    }
}
