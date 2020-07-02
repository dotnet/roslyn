// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpRecursivePatternReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool)
                : base(pool)
            {
            }

            public override SyntaxNode? VisitRecursivePattern(RecursivePatternSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitRecursivePattern(node),
                    parentNode: node.Parent,
                    simplifier: s_simplifyNode);
            }
        }
    }
}
