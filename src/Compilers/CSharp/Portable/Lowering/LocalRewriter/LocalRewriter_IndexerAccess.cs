// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private BoundExpression MakeDynamicIndexerAccessReceiver(BoundDynamicIndexerAccess indexerAccess, BoundExpression loweredReceiver)
        {
            BoundExpression result;

            string indexedPropertyName = indexerAccess.TryGetIndexedPropertyName();
            if (indexedPropertyName != null)
            {
                // Dev12 forces the receiver to be typed to dynamic to workaround a bug in the runtime binder.
                // See DynamicRewriter::FixupIndexedProperty:
                // "If we don't do this, then the calling object is statically typed and we pass the UseCompileTimeType to the runtime binder."
                // However, with the cast the scenarios don't work either, so we don't mimic Dev12.
                // loweredReceiver = BoundConversion.Synthesized(loweredReceiver.Syntax, loweredReceiver, Conversion.Identity, false, false, null, DynamicTypeSymbol.Instance);

                result = _dynamicFactory.MakeDynamicGetMember(loweredReceiver, indexedPropertyName, resultIndexed: true).ToExpression();
            }
            else
            {
                result = loweredReceiver;
            }

            return result;
        }

        public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            Debug.Assert(node.ReceiverOpt != null);

            var loweredReceiver = VisitExpression(node.ReceiverOpt);
            var loweredArguments = VisitList(node.Arguments);

            return MakeDynamicGetIndex(node, loweredReceiver, loweredArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt);
        }

        private BoundExpression MakeDynamicGetIndex(
            BoundDynamicIndexerAccess node,
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames,
            ImmutableArray<RefKind> refKinds)
        {
            // If we are calling a method on a NoPIA type, we need to embed all methods/properties
            // with the matching name of this dynamic invocation.
            EmbedIfNeedTo(loweredReceiver, node.ApplicableIndexers, node.Syntax);

            return _dynamicFactory.MakeDynamicGetIndex(
                MakeDynamicIndexerAccessReceiver(node, loweredReceiver),
                loweredArguments,
                argumentNames,
                refKinds).ToExpression();
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            Debug.Assert(node.Indexer.IsIndexer || node.Indexer.IsIndexedProperty);
            Debug.Assert((object)node.Indexer.GetOwnOrInheritedGetMethod() != null);

            return VisitIndexerAccess(node, isLeftOfAssignment: false);
        }

        private BoundExpression VisitIndexerAccess(BoundIndexerAccess node, bool isLeftOfAssignment)
        {
            PropertySymbol indexer = node.Indexer;
            Debug.Assert(indexer.IsIndexer || indexer.IsIndexedProperty);

            // Rewrite the receiver.
            BoundExpression rewrittenReceiver = VisitExpression(node.ReceiverOpt);

            // Rewrite the arguments.
            // NOTE: We may need additional argument rewriting such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
            // NOTE: This is done later by MakeArguments, for now we just lower each argument.
            ImmutableArray<BoundExpression> rewrittenArguments = VisitList(node.Arguments);

            return MakeIndexerAccess(
                node.Syntax,
                rewrittenReceiver,
                indexer,
                rewrittenArguments,
                node.ArgumentNamesOpt,
                node.ArgumentRefKindsOpt,
                node.Expanded,
                node.ArgsToParamsOpt,
                node.Type,
                node,
                isLeftOfAssignment);
        }

        private BoundExpression MakeIndexerAccess(
            SyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            PropertySymbol indexer,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            TypeSymbol type,
            BoundIndexerAccess oldNodeOpt,
            bool isLeftOfAssignment)
        {
            if (isLeftOfAssignment && indexer.RefKind == RefKind.None)
            {
                // This is an indexer set access. We return a BoundIndexerAccess node here.
                // This node will be rewritten with MakePropertyAssignment when rewriting the enclosing BoundAssignmentOperator.

                return oldNodeOpt != null ?
                    oldNodeOpt.Update(rewrittenReceiver, indexer, rewrittenArguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, null, isLeftOfAssignment, type) :
                    new BoundIndexerAccess(syntax, rewrittenReceiver, indexer, rewrittenArguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, null, isLeftOfAssignment, type);
            }
            else
            {
                var getMethod = indexer.GetOwnOrInheritedGetMethod();
                Debug.Assert((object)getMethod != null);

                // We have already lowered each argument, but we may need some additional rewriting for the arguments,
                // such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
                ImmutableArray<LocalSymbol> temps;
                rewrittenArguments = MakeArguments(
                    syntax,
                    rewrittenArguments,
                    indexer,
                    getMethod,
                    expanded,
                    argsToParamsOpt,
                    ref argumentRefKindsOpt,
                    out temps,
                    enableCallerInfo: ThreeState.True);

                BoundExpression call = MakePropertyGetAccess(syntax, rewrittenReceiver, indexer, rewrittenArguments, getMethod);

                if (temps.IsDefaultOrEmpty)
                {
                    return call;
                }
                else
                {
                    return new BoundSequence(
                        syntax,
                        temps,
                        ImmutableArray<BoundExpression>.Empty,
                        call,
                        type);
                }
            }
        }

        public override BoundNode VisitIndexOrRangePatternIndexerAccess(BoundIndexOrRangePatternIndexerAccess node)
        {
            return VisitIndexOrRangePatternIndexerAccess(node, isLeftOfAssignment: false);
        }

        private BoundSequence VisitIndexOrRangePatternIndexerAccess(BoundIndexOrRangePatternIndexerAccess node, bool isLeftOfAssignment)
        {
            if (TypeSymbol.Equals(
                node.Argument.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Index),
                TypeCompareKind.ConsiderEverything))
            {
                return VisitIndexPatternIndexerAccess(
                    node.Syntax,
                    node.Receiver,
                    node.LengthOrCountProperty,
                    (PropertySymbol)node.PatternSymbol,
                    node.Argument,
                    isLeftOfAssignment: isLeftOfAssignment);
            }
            else
            {
                Debug.Assert(TypeSymbol.Equals(
                    node.Argument.Type,
                    _compilation.GetWellKnownType(WellKnownType.System_Range),
                    TypeCompareKind.ConsiderEverything));
                return VisitRangePatternIndexerAccess(
                    node.Receiver,
                    node.LengthOrCountProperty,
                    (MethodSymbol)node.PatternSymbol,
                    node.Argument);
            }
        }


        private BoundSequence VisitIndexPatternIndexerAccess(
            SyntaxNode syntax,
            BoundExpression receiver,
            PropertySymbol lengthOrCountProperty,
            PropertySymbol intIndexer,
            BoundExpression argument,
            bool isLeftOfAssignment)
        {
            // Lowered code:
            // ref var receiver = receiverExpr;
            // int length = receiver.length;
            // int index = argument.GetOffset(length);
            // receiver[index];

            var F = _factory;

            var receiverLocal = F.StoreToTemp(
                VisitExpression(receiver),
                out var receiverStore,
                // Store the receiver as a ref local if it's a value type to ensure side effects are propagated
                receiver.Type.IsReferenceType ? RefKind.None : RefKind.Ref);
            var lengthLocal = F.StoreToTemp(F.Property(receiverLocal, lengthOrCountProperty), out var lengthStore);
            var indexLocal = F.StoreToTemp(
                MakePatternIndexOffsetExpression(argument, lengthLocal, out bool usedLength),
                out var indexStore);

            // Hint the array size here because the only case when the length is not needed is if the
            // user writes code like receiver[(Index)offset], as opposed to just receiver[offset]
            // and that will probably be very rare.
            var locals = ArrayBuilder<LocalSymbol>.GetInstance(3);
            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance(3);

            locals.Add(receiverLocal.LocalSymbol);
            sideEffects.Add(receiverStore);

            if (usedLength)
            {
                locals.Add(lengthLocal.LocalSymbol);
                sideEffects.Add(lengthStore);
            }

            locals.Add(indexLocal.LocalSymbol);
            sideEffects.Add(indexStore);

            return (BoundSequence)F.Sequence(
                locals.ToImmutable(),
                sideEffects.ToImmutable(),
                MakeIndexerAccess(
                    syntax,
                    receiverLocal,
                    intIndexer,
                    ImmutableArray.Create<BoundExpression>(indexLocal),
                    default,
                    default,
                    expanded: false,
                    argsToParamsOpt: default,
                    intIndexer.Type,
                    oldNodeOpt: null,
                    isLeftOfAssignment));
        }

        /// <summary>
        /// Used to construct a pattern index offset expression, of the form
        ///     `unloweredExpr.GetOffset(lengthAccess)`
        /// where unloweredExpr is an expression of type System.Index and the
        /// lengthAccess retrieves the length of the indexing target.
        /// </summary>
        /// <param name="unloweredExpr">The unlowered argument to the indexing expression</param>
        /// <param name="lengthAccess">
        /// An expression accessing the length of the indexing target. This should
        /// be a non-side-effecting operation.
        /// </param>
        /// <param name="usedLength">
        /// True if we were able to optimize the <paramref name="unloweredExpr"/>
        /// to use the <paramref name="lengthAccess"/> operation directly on the receiver, instead of
        /// using System.Index helpers.
        /// </param>
        private BoundExpression MakePatternIndexOffsetExpression(
            BoundExpression unloweredExpr,
            BoundExpression lengthAccess,
            out bool usedLength)
        {
            Debug.Assert(TypeSymbol.Equals(
                unloweredExpr.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Index),
                TypeCompareKind.ConsiderEverything));

            var F = _factory;

            if (unloweredExpr is BoundFromEndIndexExpression hatExpression)
            {
                // If the System.Index argument is `^index`, we can replace the
                // `argument.GetOffset(length)` call with `length - index`
                Debug.Assert(hatExpression.Operand.Type.SpecialType == SpecialType.System_Int32);
                usedLength = true;
                return F.IntSubtract(lengthAccess, VisitExpression(hatExpression.Operand));
            }
            else if (unloweredExpr is BoundConversion conversion && conversion.Operand.Type.SpecialType == SpecialType.System_Int32)
            {
                // If the System.Index argument is a conversion from int to Index we
                // can return the int directly
                usedLength = false;
                return VisitExpression(conversion.Operand);
            }
            else
            {
                usedLength = true;
                return F.Call(
                    VisitExpression(unloweredExpr),
                    WellKnownMember.System_Index__GetOffset,
                    lengthAccess);
            }
        }

        private BoundSequence VisitRangePatternIndexerAccess(
            BoundExpression receiver,
            PropertySymbol lengthOrCountProperty,
            MethodSymbol sliceMethod,
            BoundExpression rangeArg)
        {
            Debug.Assert(TypeSymbol.Equals(
                rangeArg.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Range),
                TypeCompareKind.ConsiderEverything));

            // Lowered code without optimizations:
            // var receiver = receiverExpr;
            // int length = receiver.length;
            // Range range = argumentExpr;
            // int start = range.Start.GetOffset(length)
            // int rangeSize = range.End.GetOffset(length) - start
            // receiver.Slice(start, rangeSize)

            var F = _factory;

            var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            var sideEffectsBuilder = ArrayBuilder<BoundExpression>.GetInstance();

            var receiverLocal = F.StoreToTemp(VisitExpression(receiver), out var receiverStore);
            var lengthLocal = F.StoreToTemp(F.Property(receiverLocal, lengthOrCountProperty), out var lengthStore);

            localsBuilder.Add(receiverLocal.LocalSymbol);
            sideEffectsBuilder.Add(receiverStore);

            BoundExpression startExpr;
            BoundExpression rangeSizeExpr;
            if (rangeArg is BoundRangeExpression rangeExpr)
            {
                // If we know that the input is a range expression, we can
                // optimize by pulling it apart inline, so
                // 
                // Range range = argumentExpr;
                // int start = range.Start.GetOffset(length)
                // int rangeSize = range.End.GetOffset(length) - start
                //
                // is, with `start..end`:
                //
                // int start = start.GetOffset(length)
                // int rangeSize = end.GetOffset(length) - start

                bool usedLength = false;

                if (rangeExpr.LeftOperandOpt is BoundExpression left)
                {
                    var startLocal = F.StoreToTemp(
                        MakePatternIndexOffsetExpression(rangeExpr.LeftOperandOpt, lengthLocal, out usedLength),
                        out var startStore);

                    localsBuilder.Add(startLocal.LocalSymbol);
                    sideEffectsBuilder.Add(startStore);
                    startExpr = startLocal;
                }
                else
                {
                    startExpr = F.Literal(0);
                }

                BoundExpression endExpr;
                if (rangeExpr.RightOperandOpt is BoundExpression right)
                {
                    endExpr = MakePatternIndexOffsetExpression(
                        right,
                        lengthLocal,
                        out bool usedLengthTemp);
                    usedLength |= usedLengthTemp;
                }
                else
                {
                    usedLength = true;
                    endExpr = lengthLocal;
                }

                if (usedLength)
                {
                    // If we used the length, it needs to be calculated after the receiver (the
                    // first bound node in the builder) and before the first use, which could be the
                    // second or third node in the builder
                    localsBuilder.Insert(1, lengthLocal.LocalSymbol);
                    sideEffectsBuilder.Insert(1, lengthStore);
                }

                var rangeSizeLocal = F.StoreToTemp(
                    F.IntSubtract(endExpr, startExpr),
                    out var rangeStore);

                localsBuilder.Add(rangeSizeLocal.LocalSymbol);
                sideEffectsBuilder.Add(rangeStore);
                rangeSizeExpr = rangeSizeLocal;
            }
            else
            {
                var rangeLocal = F.StoreToTemp(VisitExpression(rangeArg), out var rangeStore);

                localsBuilder.Add(lengthLocal.LocalSymbol);
                sideEffectsBuilder.Add(lengthStore);
                localsBuilder.Add(rangeLocal.LocalSymbol);
                sideEffectsBuilder.Add(rangeStore);

                var startLocal = F.StoreToTemp(
                    F.Call(
                        F.Call(rangeLocal, F.WellKnownMethod(WellKnownMember.System_Range__get_Start)),
                        F.WellKnownMethod(WellKnownMember.System_Index__GetOffset),
                        lengthLocal),
                    out var startStore);

                localsBuilder.Add(startLocal.LocalSymbol);
                sideEffectsBuilder.Add(startStore);
                startExpr = startLocal;

                var rangeSizeLocal = F.StoreToTemp(
                    F.IntSubtract(
                        F.Call(
                            F.Call(rangeLocal, F.WellKnownMethod(WellKnownMember.System_Range__get_End)),
                            F.WellKnownMethod(WellKnownMember.System_Index__GetOffset),
                            lengthLocal),
                        startExpr),
                    out var rangeSizeStore);

                localsBuilder.Add(rangeSizeLocal.LocalSymbol);
                sideEffectsBuilder.Add(rangeSizeStore);
                rangeSizeExpr = rangeSizeLocal;
            }

            return (BoundSequence)F.Sequence(
                localsBuilder.ToImmutableAndFree(),
                sideEffectsBuilder.ToImmutableAndFree(),
                F.Call(receiverLocal, sliceMethod, startExpr, rangeSizeExpr));
        }
    }
}
