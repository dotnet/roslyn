// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            return VisitAwaitExpression(node, true);
        }

        public BoundExpression VisitAwaitExpression(BoundAwaitExpression node, bool used)
        {
            return RewriteAwaitExpression((BoundExpression)base.VisitAwaitExpression(node), used);
        }

        private BoundExpression RewriteAwaitExpression(SyntaxNode syntax, BoundExpression rewrittenExpression, AwaitableInfo awaitableInfo, TypeSymbol type, bool used)
        {
            return RewriteAwaitExpression(new BoundAwaitExpression(syntax, rewrittenExpression, awaitableInfo, type) { WasCompilerGenerated = true }, used);
        }

        /// <summary>
        /// Lower an await expression that has already had its components rewritten.
        /// </summary>
        private BoundExpression RewriteAwaitExpression(BoundExpression rewrittenAwait, bool used)
        {
            _sawAwait = true;
            if (!used)
            {
                // Await expression is already at the statement level.
                return rewrittenAwait;
            }

            // The await expression will be lowered to code that involves the use of side-effects
            // such as jumps and labels, therefore it is represented by a BoundSpillSequence.
            // The resulting nodes will be "spilled" to move such statements to the top
            // level (i.e. into the enclosing statement list).
            _needsSpilling = true;
            return new BoundSpillSequence(
                syntax: rewrittenAwait.Syntax,
                locals: ImmutableArray<LocalSymbol>.Empty,
                sideEffects: ImmutableArray<BoundStatement>.Empty,
                value: rewrittenAwait,
                type: rewrittenAwait.Type);
        }
    }
}
