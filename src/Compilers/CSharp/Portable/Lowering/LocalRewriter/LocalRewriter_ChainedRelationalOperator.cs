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
            BoundExpression chain = BuildChainLink(node, innerRightReplacement: null, locals);

            return _factory.Sequence(
                locals: locals.ToImmutableAndFree(),
                sideEffects: [],
                result: chain);
        }

        /// <summary>
        /// Lowers a single link of a chained relational comparison, recursing into the inner
        /// link as needed and combining via short-circuit <c>&amp;&amp;</c>.
        ///
        /// When <paramref name="node"/> is a chained <see cref="BoundBinaryOperator"/>, this
        /// method allocates a temp for the Y the link contributes (the shared middle operand
        /// between the inner link and this one), forms an inline-assign expression
        /// <c>(temp = Y, temp)</c>, and recurses into the inner link with that expression as
        /// the inner link's right-operand replacement. The result is
        /// <c>loweredInner &amp;&amp; (temp op thisNodeRight)</c>.
        ///
        /// When <paramref name="node"/> is the classical (non-chained) base link, this method
        /// simply emits <c>loweredLeft op innerRightReplacement</c>, where
        /// <paramref name="innerRightReplacement"/> is the inline-assign that the chained node
        /// above this one injected. For the base case the replacement is non-null by
        /// construction; only the very top-level chained node passes a <c>null</c> replacement,
        /// meaning "use your own <c>Right</c>".
        ///
        /// Any temp allocated at this level is appended to <paramref name="locals"/>.
        /// </summary>
        private BoundExpression BuildChainLink(
            BoundBinaryOperator node,
            BoundExpression? innerRightReplacement,
            ArrayBuilder<LocalSymbol> locals)
        {
            if (node.IsChainedRelational)
            {
                // Allocate a temp for this level's shared middle operand, at the type the inner
                // link consumes for that operand. The inner link's right slot is filled with an
                // inline-assign into this temp, so the operand is evaluated exactly once and
                // the captured value is then reused as this link's left operand.
                BoundExpression y = node.ChainedRelationalLeftOperand!;
                LocalSymbol tempSym = _factory.SynthesizedLocal(y.Type!, kind: SynthesizedLocalKind.LoweringTemp, syntax: y.Syntax);
                locals.Add(tempSym);
                BoundLocal temp = _factory.Local(tempSym);

                BoundExpression innerAssign = AssignAndRead(temp, VisitExpression(y));
                BoundExpression loweredInner = BuildChainLink((BoundBinaryOperator)node.Left, innerAssign, locals);

                // The right operand of this link is either the caller-supplied replacement
                // (we are an intermediate chained node, and our parent has already hoisted our
                // right into its own temp) or our own Right (we are the outermost chain node).
                BoundExpression thisRight = innerRightReplacement ?? VisitExpression(node.Right);

                // oldNode: null so that constant values on chain nodes (and on the classical
                // base in the mutually-recursive case below) are not copied onto the rewritten
                // link; the rewritten link carries a temp-assignment side effect and therefore
                // is not a compile-time constant even when the original operand folding said it
                // was. See https://github.com/dotnet/roslyn/issues/NNNN for the failure that
                // motivated passing null here.
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

            // Classical (non-chained) base link: emit `lowered(left) op innerRightReplacement`.
            // The chained node immediately above us is responsible for supplying the
            // inline-assign expression that captures the shared middle operand.
            Debug.Assert(innerRightReplacement is not null,
                "The non-chained base link is only reached by recursion from a chained caller, which always supplies the inline-assign expression.");

            return MakeBinaryOperator(
                oldNode: null,
                node.Syntax,
                node.OperatorKind,
                VisitExpression(node.Left),
                innerRightReplacement,
                node.Type!,
                node.LeftTruthOperatorMethod ?? node.BinaryOperatorMethod,
                node.ConstrainedToType,
                applyParentUnaryOperator: null);
        }

        /// <summary>
        /// Produces the expression <c>(t = e, t)</c>: assign <paramref name="expression"/> into the
        /// local referenced by <paramref name="temp"/> and then read the local as the value.
        /// Matches the inline-assign idiom used by <c>?.</c> (see LocalRewriter_ConditionalAccess.cs).
        /// </summary>
        private BoundExpression AssignAndRead(BoundLocal temp, BoundExpression expression)
        {
            return _factory.Sequence(
                locals: [],
                sideEffects: [_factory.AssignmentExpression(temp, expression)],
                result: temp);
        }
    }
}
