// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
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
            if (node.IsNullConditional)
            {
                return RewriteNullConditionalAwaitExpression(node, used);
            }

            return RewriteAwaitExpression((BoundExpression)base.VisitAwaitExpression(node)!, used);
        }

        private BoundExpression RewriteAwaitExpression(SyntaxNode syntax, BoundExpression rewrittenExpression, BoundAwaitableInfo awaitableInfo, TypeSymbol type, BoundAwaitExpressionDebugInfo debugInfo, bool used)
        {
            return RewriteAwaitExpression(new BoundAwaitExpression(syntax, rewrittenExpression, awaitableInfo, debugInfo, isNullConditional: false, type) { WasCompilerGenerated = true }, used);
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
            // such as jumps and labels, which we can only emit with an empty stack, so we require
            // that the await expression itself is produced only when the stack is empty.
            // Therefore it is represented by a BoundSpillSequence.  The resulting nodes will be "spilled" to move
            // such statements to the top level (i.e. into the enclosing statement list).  Here we ensure
            // that the await result itself is stored into a temp at the statement level, as that is
            // the form handled by async lowering.
            _needsSpilling = true;
            var tempAccess = _factory.StoreToTemp(rewrittenAwait, out BoundAssignmentOperator tempAssignment, syntaxOpt: rewrittenAwait.Syntax,
                kind: SynthesizedLocalKind.Spill);
            return new BoundSpillSequence(
                syntax: rewrittenAwait.Syntax,
                locals: ImmutableArray.Create<LocalSymbol>(tempAccess.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: tempAccess,
                type: tempAccess.Type);
        }

        /// <summary>
        /// Lower <c>await? e</c>. Produces the same shape as the regular <c>e?.M()</c> lowering: a
        /// <see cref="BoundLoweredConditionalAccess"/> whose receiver is the operand, with a null check
        /// (reference-type null comparison or <c>Nullable&lt;V&gt;.HasValue</c>) guarding a non-null branch
        /// that performs a regular <c>await</c> of the (possibly unwrapped) receiver. The subsequent
        /// <see cref="SpillSequenceSpiller"/> pass expands this into an <c>if (receiver != null) tmp = await ...; else tmp = default;</c>
        /// statement form, after which <see cref="AsyncMethodToStateMachineRewriter"/> or
        /// <see cref="RuntimeAsyncRewriter"/> consume the inner (now regular) await normally. No
        /// <see cref="BoundAwaitExpression"/> with <see cref="BoundAwaitExpression.IsNullConditional"/> = true
        /// survives past this method.
        /// </summary>
        private BoundExpression RewriteNullConditionalAwaitExpression(BoundAwaitExpression node, bool used)
        {
            Debug.Assert(node.IsNullConditional);

            // Rewrite the operand and AwaitableInfo through the base visitor so everything inside is
            // lowered exactly as it would have been for a regular await.
            var rewritten = (BoundAwaitExpression)base.VisitAwaitExpression(node)!;
            var loweredReceiver = rewritten.Expression;
            Debug.Assert(loweredReceiver.Type is { });
            var receiverType = loweredReceiver.Type;

            // Fresh id shared between the lowered conditional access and the receiver placeholder —
            // the spiller uses this to substitute the real (spilled) receiver into WhenNotNull.
            var currentConditionalAccessID = ++_currentConditionalAccessID;

            var placeholder = new BoundConditionalReceiver(
                loweredReceiver.Syntax,
                currentConditionalAccessID,
                receiverType);

            // When the operand is Nullable<V>, the awaitable pattern was resolved against V in the
            // binder, so the non-null branch must supply a value of type V. MakeOptimizedGetValueOrDefault
            // produces `placeholder.GetValueOrDefault()` of type V; for non-nullable operands it just
            // returns the placeholder unchanged.
            BoundExpression unwrappedReceiver = MakeOptimizedGetValueOrDefault(loweredReceiver.Syntax, placeholder);

            // R is the raw GetResult return type. node.Type is the lifted X (R, Nullable<R>, or R
            // unchanged depending on spec §11.8.8.3). Binder-side error paths fall back to dynamic
            // (matching Binder_Await's awaitExpressionType fallback).
            TypeSymbol resultType = (rewritten.AwaitableInfo.GetResult ?? rewritten.AwaitableInfo.RuntimeAsyncAwaitCall?.Method)?.ReturnType
                                    ?? _compilation.DynamicType;

            BoundExpression innerAwait = new BoundAwaitExpression(
                node.Syntax,
                unwrappedReceiver,
                rewritten.AwaitableInfo,
                debugInfo: rewritten.DebugInfo,
                isNullConditional: false,
                type: resultType)
            { WasCompilerGenerated = true };

            // Three shapes based on (used, R):
            //
            //   [void or !used]  Wrap the inner await in a BoundSpillSequence whose value IS the
            //                    await (no StoreToTemp, no Nullable<R> wrap), and declare the outer
            //                    BoundLoweredConditionalAccess.Type = void. The spiller's void path
            //                    then emits `if (HasValue/NotNull) ExpressionStatement(await);` —
            //                    the await ends up at statement level with its result `pop`ped (or
            //                    elided by the state machine's statement-form recognizer). No tmp.
            //
            //   [used, non-void] Route the inner await through RewriteAwaitExpression(used: true)
            //                    for the standard `tmp = await; tmp` spill shape, lift to
            //                    Nullable<R> if needed, and keep the outer Type = X. The spiller's
            //                    non-void path then emits the tmp-based if/else that produces X.
            //
            // The unused-non-void case goes through the same void-shaped path as the void case
            // specifically so the bare-statement form `await? taskOfInt;` doesn't leave a dead
            // Nullable<int>/int temp in the IL.
            BoundExpression whenNotNull;
            TypeSymbol nodeType;
            if (!used || resultType.IsVoidType())
            {
                _sawAwait = true;
                _needsSpilling = true;
                whenNotNull = new BoundSpillSequence(
                    innerAwait.Syntax,
                    locals: ImmutableArray<LocalSymbol>.Empty,
                    sideEffects: ImmutableArray<BoundStatement>.Empty,
                    value: innerAwait,
                    type: resultType);
                nodeType = _compilation.GetSpecialType(SpecialType.System_Void);
            }
            else
            {
                whenNotNull = RewriteAwaitExpression(innerAwait, used: true);
                nodeType = rewritten.Type;

                // BoundLoweredConditionalAccess requires that WhenNotNull already has the same type as
                // the overall expression — codegen picks its result slot from node.Type and just stores
                // WhenNotNull into it. For `await? Task<int>` the binder's result-type rule lifts R
                // (int) to X (int?), but the inner await we synthesized above still has type int.
                // Without this wrap codegen would try to store an int into an int? slot and fail the
                // type equality invariant. The null branch of BoundLoweredConditionalAccess already
                // emits `default(int?)`, so we only need to lift the non-null branch here.
                //
                // No wrap is needed when R is a reference type (`Task<string>` → R = X = string, with
                // an NRT annotation that doesn't change the underlying TypeSymbol), `dynamic`, or an
                // already-nullable Nullable<V> (e.g. `Task<int?>` → R = X = int?).
                if (!TypeSymbol.Equals(resultType, nodeType, TypeCompareKind.ConsiderEverything2) && nodeType.IsNullableType())
                {
                    Debug.Assert(TypeSymbol.Equals(resultType, nodeType.GetNullableUnderlyingType(), TypeCompareKind.ConsiderEverything2));
                    whenNotNull = _factory.New((NamedTypeSymbol)nodeType, whenNotNull);
                }
            }

            return new BoundLoweredConditionalAccess(
                node.Syntax,
                loweredReceiver,
                // HasValueMethodOpt selects the null-check shape codegen emits. For Nullable<V>
                // we must use Nullable<V>.HasValue (the struct is always on the stack; a
                // brtrue/brfalse on the struct itself would be meaningless). For a reference-type
                // receiver there is no HasValue to call, and a brtrue against the reference is
                // the standard null check — we pass null to signal codegen to use that path.
                // EmitLoweredConditionalAccessExpression branches on this field exactly this way.
                //
                // `UnsafeGetNullableMethod` is used here to match the precedent in
                // LocalRewriter_ConditionalAccess.cs around line 154, which resolves
                // `Nullable<T>.get_HasValue` the same way for the regular `?.` lowering. The
                // method's "unsafe" naming warns that it returns an error-typed symbol without
                // special handling when the special member is missing. That's acceptable here:
                // it internally reports a missing-member diagnostic via TryGetSpecialTypeMember,
                // and the missing-Nullable<T> case is unreachable from this path in practice —
                // the binder could never have produced a Nullable<V> operand to begin with.
                hasValueMethodOpt: receiverType.IsNullableType()
                    ? UnsafeGetNullableMethod(node.Syntax, receiverType, SpecialMember.System_Nullable_T_get_HasValue)
                    : null,
                whenNotNull: whenNotNull,
                whenNullOpt: null,
                id: currentConditionalAccessID,
                // For Nullable<V> receivers codegen otherwise shares the receiver's address across
                // the HasValue check and the subsequent GetValueOrDefault call (loaded via ldloca).
                // If the receiver is a mutable storage location (instance field on a ref-returning
                // property, `ref` local, array element, etc.) a write between the two can flip
                // HasValue to true while GetValueOrDefault returns a default value. Forcing a copy
                // decouples the two reads and preserves "evaluate the operand once" semantics for
                // `await?` just as it does for `?.`. Ignored for non-Nullable receivers.
                forceCopyOfNullableValueType: true,
                type: nodeType);
        }
    }
}
