// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpNameReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpNameReducer() : base(s_pool)
        {
        }

        private static readonly Func<SyntaxNode, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyName = SimplifyName;

        private static SyntaxNode SimplifyName(
            SyntaxNode node,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            SyntaxNode replacementNode;

            if (node.IsKind(SyntaxKind.QualifiedCref, out QualifiedCrefSyntax crefSyntax))
            {
                if (!QualifiedCrefSimplifier.Instance.TrySimplify(
                        crefSyntax, semanticModel, optionSet,
                        out var crefReplacement, out _, cancellationToken))
                {
                    return node;
                }

                replacementNode = crefReplacement;
            }
            else
            {
                var expressionSyntax = (ExpressionSyntax)node;
                if (!ExpressionSimplifier.Instance.TrySimplify(expressionSyntax, semanticModel, optionSet, out var expressionReplacement, out _, cancellationToken))
                {
                    return node;
                }

                replacementNode = expressionReplacement;
            }

            node = node.CopyAnnotationsTo(replacementNode).WithAdditionalAnnotations(Formatter.Annotation);
            return node.WithoutAnnotations(Simplifier.Annotation);
        }
    }
}
