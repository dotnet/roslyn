﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpParenthesizedPatternReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new(
            () => new Rewriter(s_pool));

        private static readonly Func<ParenthesizedPatternSyntax, SemanticModel, SimplifierOptions, CancellationToken, SyntaxNode> s_simplifyParentheses = SimplifyParentheses;

        public CSharpParenthesizedPatternReducer() : base(s_pool)
        {
        }

        protected override bool IsApplicable(CSharpSimplifierOptions options)
           => true;

        private static SyntaxNode SimplifyParentheses(
            ParenthesizedPatternSyntax node,
            SemanticModel semanticModel,
            SimplifierOptions options,
            CancellationToken cancellationToken)
        {
            if (node.CanRemoveParentheses())
            {
                var resultNode = CSharpSyntaxFacts.Instance.Unparenthesize(node);
                return SimplificationHelpers.CopyAnnotations(from: node, to: resultNode);
            }

            // We don't know how to simplify this.
            return node;
        }
    }
}
