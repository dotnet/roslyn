// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed partial class LocalRewriter
{
    /// <summary>
    /// Rewrites a chained relational comparison (<see cref="BoundBinaryOperator.IsChainedRelational"/>)
    /// into a short-circuit <c>&amp;&amp;</c> chain with each shared middle operand hoisted into a
    /// temp evaluated exactly once (spec §11.11.13). For example <c>a op1 b op2 c op3 d</c> lowers to:
    ///
    ///     BoundSequence(
    ///         locals:      [tempC, tempB],
    ///         sideEffects: [],
    ///         value:       (a op1 (tempB = b, tempB)) &amp;&amp;
    ///                      (tempB op2 (tempC = c, tempC)) &amp;&amp;
    ///                      (tempC op3 d),
    ///         type:        bool)
    ///
    /// Each level of the spine contributes one temp for its Y and passes an inline-assign
    /// <c>(temp = Y, temp)</c> down to the inner link's right-operand slot, so short-circuit
    /// semantics fall out of the surrounding <c>&amp;&amp;</c>s. Source-level chains beyond
    /// a handful of operands are extremely rare, so unlike <c>a + b + c + ...</c> we don't
    /// bother with an explicit stack here.
    /// </summary>
    private BoundExpression RewriteChainedRelationalOperator(BoundBinaryOperator node)
    {
        Debug.Assert(node.IsChainedRelational(out _, out _, out _));

        var locals = ArrayBuilder<LocalSymbol>.GetInstance();

        // At the top of the chain this link's right operand is simply node.Right; every
        // inner level instead receives an inline-assign injected by the level above it.
        var chain = BuildChainLink(node, VisitExpression(node.Right), locals);

        return _factory.Sequence(locals: locals.ToImmutableAndFree(), sideEffects: [], result: chain);
    }

    /// <summary>
    /// Lowers one link of a chained relational comparison whose right-operand expression is
    /// supplied by the caller as <paramref name="thisRight"/>. Any temp allocated at this level
    /// is appended to <paramref name="locals"/>.
    ///
    /// Recursion is driven here rather than through <see cref="LocalRewriter.VisitExpression"/>
    /// because the chain's single-evaluation guarantee depends on substituting each outer
    /// level's inline-assign <c>(temp = Y, temp)</c> into the inner link's right-operand slot -
    /// a substitution point several <see cref="BoundBinaryOperator"/> nodes deep that
    /// <c>VisitExpression</c> has no hook to inject at.
    /// </summary>
    private BoundExpression BuildChainLink(
        BoundBinaryOperator node,
        BoundExpression thisRight,
        ArrayBuilder<LocalSymbol> locals)
    {
        // Base (non-chained) link: thisRight is the inline-assign that captured our
        // caller's shared middle, so the whole link is just `lowered(left) op thisRight`.
        if (!node.IsChainedRelational(out BoundExpression? y, out Conversion leftConversion, out TypeSymbol? leftConvertedType))
            return buildRelationalLink(VisitExpression(node.Left));

        // Critically, the temp's type is Y's *inner-link* type (what IsChainedRelational
        // hands us), NOT the outer link's wider LeftType. That invariant is what makes
        // asymmetric chains like `short < int < long` emit verifiable IL - the inner
        // operator sees `int<int` on an `int` temp, so stack types agree.
        locals.Add(_factory.SynthesizedLocal(y.Type!, kind: SynthesizedLocalKind.LoweringTemp, syntax: y.Syntax));
        var temp = _factory.Local(locals.Last());

        // Lower this level to `loweredInner && (temp_conv op thisRight)`, where:
        //
        //   * loweredInner recurses into node.Left, threading the inline-assign
        //     `(temp = Y, temp)` down as the inner link's right operand. That idiom
        //     evaluates Y once, stores it into the temp, and yields the temp - so the
        //     inner link's operator sees Y at its inner-link type. (Same idiom is used
        //     by `?.` lowering; see LocalRewriter_ConditionalAccess.cs.)
        //
        //   * The outer link's LEFT operand is the temp wrapped in the stored
        //     LeftConversion, so the outer operator sees the temp widened to ITS chosen
        //     LeftType. MakeConversionNode short-circuits Identity conversions, so the
        //     common same-type case produces no wrapper and the temp flows through
        //     unchanged.
        return _factory.LogicalAnd(
            BuildChainLink(
                (BoundBinaryOperator)node.Left,
                _factory.Sequence(
                    locals: [],
                    sideEffects: [_factory.AssignmentExpression(temp, VisitExpression(y))],
                    result: temp),
                locals),
            buildRelationalLink(MakeConversionNode(
                oldNodeOpt: null,
                syntax: node.Syntax,
                rewrittenOperand: temp,
                conversion: leftConversion,
                @checked: false,
                explicitCastInCode: false,
                constantValueOpt: null,
                rewrittenType: leftConvertedType)));

        // Build a single relational link `leftOperand op thisRight` from node's operator
        // metadata. oldNode: null because rewritten links carry a temp-assign side effect
        // (for non-base links) and are never constants, even when the original folded.
        // BinaryOperatorMethod, not LeftTruthOperatorMethod: every chained link resolves
        // to a bool-returning `<`/`<=`/`>`/`>=` (spec §11.11.13 rule 2(b)), so
        // `operator true`/`operator false` never participate - the chain is combined
        // by _factory.LogicalAnd (= LogicalBoolAnd).
        BoundExpression buildRelationalLink(BoundExpression leftOperand) =>
            MakeBinaryOperator(
                oldNode: null,
                node.Syntax,
                node.OperatorKind,
                leftOperand,
                thisRight,
                node.Type!,
                node.BinaryOperatorMethod,
                node.ConstrainedToType,
                applyParentUnaryOperator: null);
    }
}
