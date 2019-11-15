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
            switch (node.Type, node.PositionalPatternClause, node.PropertyPatternClause)
            {
                case ({ }, _, { Subpatterns: { Count: 0 } }):
                case (_, { }, { Subpatterns: { Count: 0 } }):
                    return node.Update(node.Type, node.PositionalPatternClause, null, node.Designation);
                default:
                    return node;
            }
        }
    }
}
