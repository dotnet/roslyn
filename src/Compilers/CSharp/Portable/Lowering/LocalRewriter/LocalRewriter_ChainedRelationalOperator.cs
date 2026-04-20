// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        ///         locals:      [tempC, tempB],
        ///         sideEffects: [],
        ///         value:       (a op1 (tempB = b, tempB)) &amp;&amp;
        ///                      (tempB op2 (tempC = c, tempC)) &amp;&amp;
        ///                      (tempC op3 d),
        ///         type:        bool)
        ///
        /// Each shared middle operand is assigned inline at the point of its first use, so
        /// evaluation follows the <c>&amp;&amp;</c> short-circuit semantics exactly.
        ///
        /// The rewrite is written recursively on the bound-tree structure: each chained
        /// <see cref="BoundBinaryOperator"/> allocates one temp for the Y it contributes and
        /// passes an inline-assign expression down to its inner link's right-operand slot.
        /// Source-level chains beyond a handful of operands are extremely rare, so unlike
        /// <c>a + b + c + ...</c> we do not bother with an explicit stack here.
        /// </summary>
        private BoundExpression RewriteChainedRelationalOperator(BoundBinaryOperator node)
        {
            Debug.Assert(node.IsChainedRelational);

            var locals = ArrayBuilder<LocalSymbol>.GetInstance();

            // At the top of the chain, this link's right operand is simply node.Right; any
            // chained level below will instead receive an inline-assign injected by the level
            // above it (see BuildChainLink).
            BoundExpression chain = BuildChainLink(node, VisitExpression(node.Right), locals);

            return _factory.Sequence(
                locals: locals.ToImmutableAndFree(),
                sideEffects: [],
                result: chain);
        }

        /// <summary>
        /// Lowers one link of a chained relational comparison whose right-operand expression is
        /// supplied by the caller as <paramref name="thisRight"/>.
        ///
        /// This method must drive the recursion into <paramref name="node"/>'s left operand
        /// itself (rather than delegating to the generic <see cref="LocalRewriter.VisitExpression"/>
        /// machinery) because the only way to preserve the chain's single-evaluation guarantee
        /// is to substitute each outer level's inline-assign <c>(temp = Y, temp)</c> into the
        /// inner link's right-operand slot. That substitution point is several
        /// <see cref="BoundBinaryOperator"/> nodes deep for long chains, and <c>VisitExpression</c>
        /// offers no hook to inject a caller-supplied expression at that position. So each
        /// chained level lowers its own left operand directly, passing the next-level-down's
        /// inline-assign along as that level's <paramref name="thisRight"/>.
        ///
        /// When <paramref name="node"/> is a chained <see cref="BoundBinaryOperator"/>: allocate
        /// a temp for this level's shared middle operand <c>Y</c>, recurse into the inner link
        /// with <c>(temp = Y, temp)</c> as its <paramref name="thisRight"/>, and return
        /// <c>loweredInner &amp;&amp; (temp op <paramref name="thisRight"/>)</c>.
        ///
        /// When <paramref name="node"/> is the classical (non-chained) base link: emit
        /// <c>lowered(node.Left) op <paramref name="thisRight"/></c> directly.
        ///
        /// Any temp allocated at this level is appended to <paramref name="locals"/>.
        /// </summary>
        private BoundExpression BuildChainLink(
            BoundBinaryOperator node,
            BoundExpression thisRight,
            ArrayBuilder<LocalSymbol> locals)
        {
            if (node.IsChainedRelational)
            {
                // Allocate a temp for this level's shared middle operand, at the type the inner
                // link consumes for that operand. The inner link's right slot is filled with
                // an inline-assign into this temp, so the operand is evaluated exactly once and
                // the captured value is then reused as this link's left operand.
                BoundExpression y = node.ChainedRelationalLeftOperand!;
                LocalSymbol tempSym = _factory.SynthesizedLocal(y.Type!, kind: SynthesizedLocalKind.LoweringTemp, syntax: y.Syntax);
                locals.Add(tempSym);
                BoundLocal temp = _factory.Local(tempSym);

                // Inline-assign idiom `(temp = Y, temp)`: evaluates Y once, stores into temp,
                // and yields temp as the value of the expression. Used by `?.` lowering too;
                // see LocalRewriter_ConditionalAccess.cs.
                BoundExpression innerAssign = _factory.Sequence(
                    locals: [],
                    sideEffects: [_factory.AssignmentExpression(temp, VisitExpression(y))],
                    result: temp);
                BoundExpression loweredInner = BuildChainLink((BoundBinaryOperator)node.Left, innerAssign, locals);

                // oldNode: null so that constant values on the original chain node are not
                // copied onto this rewritten link; the rewritten link carries a temp-assignment
                // side effect and therefore is not a compile-time constant even when the
                // original operand folding said it was.
                BoundExpression thisLink = MakeBinaryOperator(
                    oldNode: null,
                    node.Syntax,
                    node.OperatorKind,
                    temp,
                    thisRight,
                    node.Type!,
                    node.LeftTruthOperatorMethod ?? node.BinaryOperatorMethod,
                    node.ConstrainedToType,
                    applyParentUnaryOperator: null);

                return _factory.LogicalAnd(loweredInner, thisLink);
            }

            // Classical (non-chained) base link: emit `lowered(left) op thisRight`. thisRight
            // is the inline-assign expression that the chained node immediately above us
            // supplied, which captures the shared middle operand into the temp allocated there.
            return MakeBinaryOperator(
                oldNode: null,
                node.Syntax,
                node.OperatorKind,
                VisitExpression(node.Left),
                thisRight,
                node.Type!,
                node.LeftTruthOperatorMethod ?? node.BinaryOperatorMethod,
                node.ConstrainedToType,
                applyParentUnaryOperator: null);
        }
    }
}
