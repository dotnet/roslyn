// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        /// <summary>
        /// Rewrites a chained relational comparison (<see cref="BoundBinaryOperator.IsChainedRelational"/>)
        /// into a short-circuit <c>&amp;&amp;</c> chain with the shared middle operands hoisted into temps so
        /// each operand is evaluated at most once. See spec §11.11.13.
        ///
        /// For a 2-operand chain <c>a op1 b op2 c</c>, the lowered form is:
        ///
        ///     BoundSequence(
        ///         locals:      [tempB],
        ///         sideEffects: [],
        ///         value:       (a op1 (tempB = b, tempB)) &amp;&amp; (tempB op2 c),
        ///         type:        bool)
        ///
        /// For a 3-operand chain <c>a op1 b op2 c op3 d</c>:
        ///
        ///     BoundSequence(
        ///         locals:      [tempB, tempC],
        ///         sideEffects: [],
        ///         value:       (a op1 (tempB = b, tempB)) &amp;&amp;
        ///                      (tempB op2 (tempC = c, tempC)) &amp;&amp;
        ///                      (tempC op3 d),
        ///         type:        bool)
        ///
        /// Each shared middle operand is assigned inline at the point of its first use, so
        /// evaluation follows the <c>&amp;&amp;</c> short-circuit semantics exactly.
        /// </summary>
        private BoundExpression RewriteChainedRelationalOperator(BoundBinaryOperator node)
        {
            Debug.Assert(node.IsChainedRelational);

            // Collect chained-outer nodes walking down the left spine. Each chained outer node
            // supplies one shared middle operand Y (stored as ChainedRelationalLeftOperand).
            // The walk stops at the innermost classical (non-chained) relational comparison,
            // which represents the base link `e0 op1 e1`.
            var chainedOuterNodesReversed = ArrayBuilder<BoundBinaryOperator>.GetInstance();
            BoundBinaryOperator current = node;
            while (current.IsChainedRelational)
            {
                chainedOuterNodesReversed.Add(current);
                Debug.Assert(current.Left is BoundBinaryOperator);
                current = (BoundBinaryOperator)current.Left;
            }

            // `current` is now the innermost classical relational comparison (e0 op1 e1).
            BoundBinaryOperator classicalBase = current;
            Debug.Assert(!classicalBase.IsChainedRelational);

            // Reverse chainedOuterNodes into source order. For `a<b<c<d`, source order is
            // [middle-chained, outermost-chained]; each node[i] contributes temp i+1 and
            // supplies its own ChainedRelationalLeftOperand as the Y for that link.
            chainedOuterNodesReversed.ReverseContents();
            ImmutableArray<BoundBinaryOperator> chainedOuterNodes = chainedOuterNodesReversed.ToImmutableAndFree();

            // Allocate one temp per chained-outer node (i.e. one per shared middle operand).
            int tempCount = chainedOuterNodes.Length;
            var tempLocals = ArrayBuilder<BoundLocal>.GetInstance(capacity: tempCount);
            var tempSymbols = ArrayBuilder<LocalSymbol>.GetInstance(capacity: tempCount);
            for (int i = 0; i < tempCount; i++)
            {
                BoundExpression y = chainedOuterNodes[i].ChainedRelationalLeftOperand!;
                LocalSymbol tempSym = _factory.SynthesizedLocal(y.Type!, kind: SynthesizedLocalKind.LoweringTemp, syntax: y.Syntax);
                tempSymbols.Add(tempSym);
                tempLocals.Add(_factory.Local(tempSym));
            }

            // Build the base link: operator from classicalBase, left from its left, right is
            // inline-assign of Y0 into temp0. `VisitExpression` on classicalBase's left ensures
            // the deep left spine is also lowered. Pass oldNode: null so that any constant
            // value baked into classicalBase (e.g. bind-time folding of `0 <= 5` to `true`)
            // is dropped; the rewritten link has a temp-assignment side effect and is not a
            // constant expression.
            BoundExpression loweredBaseLeft = VisitExpression(classicalBase.Left);
            BoundExpression loweredY0 = VisitExpression(chainedOuterNodes[0].ChainedRelationalLeftOperand!);
            BoundExpression baseLinkExpr = MakeBinaryOperator(
                oldNode: null,
                classicalBase.Syntax,
                classicalBase.OperatorKind,
                loweredBaseLeft,
                AssignAndRead(tempLocals[0], loweredY0),
                classicalBase.Type!,
                classicalBase.LeftTruthOperatorMethod ?? classicalBase.BinaryOperatorMethod,
                classicalBase.ConstrainedToType,
                applyParentUnaryOperator: null);

            BoundExpression chainExpr = baseLinkExpr;

            // Build each subsequent link. For node i in source order:
            //   - left operand is tempLocals[i]    (= captured Y at this level)
            //   - right operand is either:
            //       * AssignAndRead(tempLocals[i+1], Y[i+1])  for interior levels, OR
            //       * the outermost node's .Right               for the last level.
            for (int i = 0; i < chainedOuterNodes.Length; i++)
            {
                BoundBinaryOperator outerNode = chainedOuterNodes[i];
                bool isLast = (i == chainedOuterNodes.Length - 1);

                BoundExpression linkLeft = tempLocals[i];
                BoundExpression linkRight;

                if (isLast)
                {
                    linkRight = VisitExpression(outerNode.Right);
                }
                else
                {
                    BoundExpression nextY = VisitExpression(chainedOuterNodes[i + 1].ChainedRelationalLeftOperand!);
                    linkRight = AssignAndRead(tempLocals[i + 1], nextY);
                }

                // oldNode: null so that the constant value baked into `outerNode` (a chain
                // node does not carry a real bool constant, but stripping it here is defensive)
                // is not propagated onto this single-link rewritten node.
                BoundExpression linkExpr = MakeBinaryOperator(
                    oldNode: null,
                    outerNode.Syntax,
                    outerNode.OperatorKind,
                    linkLeft,
                    linkRight,
                    outerNode.Type!,
                    outerNode.LeftTruthOperatorMethod ?? outerNode.BinaryOperatorMethod,
                    outerNode.ConstrainedToType,
                    applyParentUnaryOperator: null);

                chainExpr = _factory.LogicalAnd(chainExpr, linkExpr);
            }

            BoundExpression result = _factory.Sequence(
                locals: tempSymbols.ToImmutableAndFree(),
                sideEffects: ImmutableArray<BoundExpression>.Empty,
                result: chainExpr);

            tempLocals.Free();
            return result;
        }

        /// <summary>
        /// Produces the expression <c>(t = e, t)</c>: assign <paramref name="expression"/> into the
        /// local referenced by <paramref name="temp"/> and then read the local as the value.
        /// Matches the inline-assign idiom used by <c>?.</c> (see LocalRewriter_ConditionalAccess.cs).
        /// </summary>
        private BoundExpression AssignAndRead(BoundLocal temp, BoundExpression expression)
        {
            return _factory.Sequence(
                locals: ImmutableArray<LocalSymbol>.Empty,
                sideEffects: ImmutableArray.Create<BoundExpression>(_factory.AssignmentExpression(temp, expression)),
                result: temp);
        }
    }
}
