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
            _sawAwait = true;
            var rewrittenAwait = (BoundExpression)base.VisitAwaitExpression(node);
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
            BoundLocal replacement = _factory.StoreToTemp(
                rewrittenAwait, out BoundAssignmentOperator saveToTemp, kind: SynthesizedLocalKind.Spill, syntaxOpt: node.Syntax);
            return new BoundSpillSequence(
                syntax: node.Syntax,
                locals: ImmutableArray.Create<LocalSymbol>(replacement.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundStatement>(_factory.ExpressionStatement(saveToTemp)),
                value: replacement,
                type: replacement.Type);
        }
    }
}
