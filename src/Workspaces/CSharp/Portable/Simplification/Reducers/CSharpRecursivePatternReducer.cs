// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpRecursivePatternReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpRecursivePatternReducer() : base(s_pool)
        {
        }

        private static readonly Func<RecursivePatternSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyNode = SimplifyNode;

        private static SyntaxNode SimplifyNode(RecursivePatternSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
        {
            switch (node.PropertyPatternClause, node.Type, node.PositionalPatternClause)
            {
                // Remove an empty property pattern clause if the type is present.
                case ({ Subpatterns: { Count: 0 } }, Type: { }, _):
                // Remove an empty property pattern clause if a positional pattern clause with more than a single subpattern is present.
                case ({ Subpatterns: { Count: 0 } }, Type: null, { } positional) when positional.Subpatterns.Count > 1:
                    return node.Update(node.Type, node.PositionalPatternClause, null, node.Designation);
                default:
                    return node;
            }
        }
    }
}
