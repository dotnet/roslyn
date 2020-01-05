// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
