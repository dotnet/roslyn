// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal sealed partial class CSharpCastReducer : AbstractCSharpReducer
{
    private static readonly ObjectPool<IReductionRewriter> s_pool = new(
        () => new Rewriter(s_pool));

    public CSharpCastReducer() : base(s_pool)
    {
    }

    protected override bool IsApplicable(CSharpSimplifierOptions options)
        => true;

    private static readonly Func<CastExpressionSyntax, SemanticModel, SimplifierOptions, CancellationToken, ExpressionSyntax> s_simplifyCast = SimplifyCast;

    private static ExpressionSyntax SimplifyCast(CastExpressionSyntax node, SemanticModel semanticModel, SimplifierOptions options, CancellationToken cancellationToken)
    {
        if (!CastSimplifier.IsUnnecessaryCast(node, semanticModel, cancellationToken))
        {
            return node;
        }

        return node.Uncast();
    }
}
