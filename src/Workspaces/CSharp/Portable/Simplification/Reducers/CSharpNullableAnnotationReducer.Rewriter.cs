﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal sealed partial class CSharpNullableAnnotationReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool) : base(pool)
            {
            }

            public override SyntaxNode VisitNullableType(NullableTypeSyntax node)
            {
                return SimplifyExpression(
                    node,
                    base.VisitNullableType(node),
                    simplifier: s_simplifyNullableType);
            }
        }
    }
}
