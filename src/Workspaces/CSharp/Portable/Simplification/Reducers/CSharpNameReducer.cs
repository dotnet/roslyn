// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal partial class CSharpNameReducer : AbstractCSharpReducer
{
    private static readonly ObjectPool<IReductionRewriter> s_pool = new(
        () => new Rewriter(s_pool));

    private static readonly Func<SyntaxNode, SemanticModel, CSharpSimplifierOptions, CancellationToken, SyntaxNode> s_simplifyName = SimplifyName;

    public CSharpNameReducer() : base(s_pool)
    {
    }

    protected override bool IsApplicable(CSharpSimplifierOptions options)
       => true;

    private static SyntaxNode SimplifyName(
        SyntaxNode node,
        SemanticModel semanticModel,
        CSharpSimplifierOptions options,
        CancellationToken cancellationToken)
    {
        SyntaxNode replacementNode;

        if (node is QualifiedCrefSyntax crefSyntax)
        {
            if (!QualifiedCrefSimplifier.Instance.TrySimplify(
                    crefSyntax, semanticModel, options,
                    out var crefReplacement, out _, cancellationToken))
            {
                return node;
            }

            replacementNode = crefReplacement;
        }
        else
        {
            var expressionSyntax = (ExpressionSyntax)node;
            if (!ExpressionSimplifier.Instance.TrySimplify(expressionSyntax, semanticModel, options, out var expressionReplacement, out _, cancellationToken))
            {
                return node;
            }

            replacementNode = expressionReplacement;
        }

        node = node.CopyAnnotationsTo(replacementNode).WithAdditionalAnnotations(Formatter.Annotation);
        return node.WithoutAnnotations(Simplifier.Annotation);
    }
}
