// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            string? indexedPropertyName = indexerAccess.TryGetIndexedPropertyName();
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
            var loweredReceiver = VisitExpression(node.Receiver);
            // There are no target types for dynamic expression.
            AssertNoImplicitInterpolatedStringHandlerConversions(node.Arguments);
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
            Debug.Assert((object?)node.Indexer.GetOwnOrInheritedGetMethod() != null);

            return VisitIndexerAccess(node, isLeftOfAssignment: false);
        }

        private BoundExpression VisitIndexerAccess(BoundIndexerAccess node, bool isLeftOfAssignment)
        {
            PropertySymbol indexer = node.Indexer;
            Debug.Assert(indexer.IsIndexer || indexer.IsIndexedProperty);

            // Rewrite the receiver.
            BoundExpression? rewrittenReceiver = VisitExpression(node.ReceiverOpt);
            Debug.Assert(rewrittenReceiver is { });

            return MakeIndexerAccess(
                node.Syntax,
                rewrittenReceiver,
                indexer,
                node.Arguments,
                node.ArgumentNamesOpt,
                node.ArgumentRefKindsOpt,
                node.Expanded,
                node.ArgsToParamsOpt,
                node.DefaultArguments,
                node.Type,
                node,
                isLeftOfAssignment);
        }

        private BoundExpression MakeIndexerAccess(
            SyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            PropertySymbol indexer,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            TypeSymbol type,
            BoundIndexerAccess? oldNodeOpt,
            bool isLeftOfAssignment)
        {
            if (isLeftOfAssignment && indexer.RefKind == RefKind.None)
            {
                // This is an indexer set access. We return a BoundIndexerAccess node here.
                // This node will be rewritten with MakePropertyAssignment when rewriting the enclosing BoundAssignmentOperator.

                return oldNodeOpt != null ?
                    oldNodeOpt.Update(rewrittenReceiver, indexer, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, defaultArguments, type) :
                    new BoundIndexerAccess(syntax, rewrittenReceiver, indexer, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, defaultArguments, type);
            }
            else
            {
                var getMethod = indexer.GetOwnOrInheritedGetMethod();
                Debug.Assert(getMethod is not null);

                ImmutableArray<BoundExpression> rewrittenArguments = VisitArguments(
                    arguments,
                    indexer,
                    argsToParamsOpt,
                    argumentRefKindsOpt,
                    ref rewrittenReceiver!,
                    out ArrayBuilder<LocalSymbol>? temps);

                rewrittenArguments = MakeArguments(
                    syntax,
                    rewrittenArguments,
                    indexer,
                    expanded,
                    argsToParamsOpt,
                    ref argumentRefKindsOpt,
                    ref temps);

                BoundExpression call = MakePropertyGetAccess(syntax, rewrittenReceiver, indexer, rewrittenArguments, getMethod);

                if (temps.Count == 0)
                {
                    temps.Free();
                    return call;
                }
                else
                {
                    return new BoundSequence(
                        syntax,
                        temps.ToImmutableAndFree(),
                        ImmutableArray<BoundExpression>.Empty,
                        call,
                        type);
                }
            }
        }

        public override BoundNode? VisitListPatternUnloweredIndexPlaceholder(BoundListPatternUnloweredIndexPlaceholder node)
        {
            return Visit(PlaceholderReplacement(node));
        }

        public override BoundNode? VisitListPatternReceiverPlaceholder(BoundListPatternReceiverPlaceholder node)
        {
            return PlaceholderReplacement(node);
        }

        public override BoundNode? VisitSlicePatternUnloweredRangePlaceholder(BoundSlicePatternUnloweredRangePlaceholder node)
        {
            return Visit(PlaceholderReplacement(node));
        }

        public override BoundNode? VisitSlicePatternReceiverPlaceholder(BoundSlicePatternReceiverPlaceholder node)
        {
            return PlaceholderReplacement(node);
        }

        public override BoundNode? VisitIndexOrRangeIndexerPatternReceiverPlaceholder(BoundIndexOrRangeIndexerPatternReceiverPlaceholder node)
        {
            return PlaceholderReplacement(node);
        }

        public override BoundNode? VisitIndexOrRangeIndexerPatternValuePlaceholder(BoundIndexOrRangeIndexerPatternValuePlaceholder node)
        {
            return PlaceholderReplacement(node);
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
                return VisitIndexImplicitIndexerAccess(node, isLeftOfAssignment: isLeftOfAssignment);
            }
            else
            {
                Debug.Assert(TypeSymbol.Equals(
                    node.Argument.Type,
                    _compilation.GetWellKnownType(WellKnownType.System_Range),
                    TypeCompareKind.ConsiderEverything));

                return VisitRangeImplicitIndexerAccess(node);
            }
        }

        private BoundSequence VisitIndexImplicitIndexerAccess(BoundIndexOrRangePatternIndexerAccess node, bool isLeftOfAssignment) // TODO2 we're not using isLeftOfAssignment
        {
            Debug.Assert(node.ArgumentPlaceholders.Length == 1);
            Debug.Assert(node.IndexerAccess is BoundIndexerAccess);

            Debug.Assert(TypeSymbol.Equals(
                node.Argument.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Index),
                TypeCompareKind.ConsiderEverything));

            var F = _factory;
            var receiver = node.Receiver;

            Debug.Assert(receiver.Type is { });
            var receiverLocal = F.StoreToTemp(
                VisitExpression(receiver),
                out var receiverStore,
                // Store the receiver as a ref local if it's a value type to ensure side effects are propagated
                receiver.Type.IsReferenceType ? RefKind.None : RefKind.Ref);

            var receiverPlaceholder = node.ReceiverPlaceholder;
            AddPlaceholderReplacement(receiverPlaceholder, receiverLocal);

            Debug.Assert(node.LengthOrCountAccess is not null);
            var integerArgument = MakePatternIndexOffsetExpression(node.Argument, VisitExpression(node.LengthOrCountAccess), out _);
            Debug.Assert(integerArgument.Type!.SpecialType == SpecialType.System_Int32);

            Debug.Assert(node.ArgumentPlaceholders.Length == 1);
            var argumentPlaceholder = node.ArgumentPlaceholders[0];

            AddPlaceholderReplacement(argumentPlaceholder, integerArgument);
            var rewrittenIndexerAccess = VisitExpression(node.IndexerAccess);
            RemovePlaceholderReplacement(argumentPlaceholder);
            RemovePlaceholderReplacement(receiverPlaceholder);

            return (BoundSequence)F.Sequence(
                ImmutableArray.Create<LocalSymbol>(receiverLocal.LocalSymbol),
                ImmutableArray.Create<BoundExpression>(receiverStore),
                rewrittenIndexerAccess);
        }

        /// <summary>
        /// Used to construct a pattern index offset expression (of type Int32), of the form
        ///     `unloweredExpr.GetOffset(lengthAccess)`
        /// where unloweredExpr is an expression of type System.Index and the
        /// lengthAccess retrieves the length of the indexing target.
        /// </summary>
        /// <param name="unloweredExpr">The unlowered argument to the indexing expression</param>
        /// <param name="loweredLengthAccess">
        /// An expression accessing the length of the indexing target. This should
        /// be a non-side-effecting operation.
        /// </param>
        /// <param name="usedLength">
        /// True if we were able to optimize the <paramref name="unloweredExpr"/>
        /// to use the <paramref name="loweredLengthAccess"/> operation directly on the receiver, instead of
        /// using System.Index helpers.
        /// </param>
        private BoundExpression MakePatternIndexOffsetExpression(
            BoundExpression unloweredExpr,
            BoundExpression loweredLengthAccess,
            out bool usedLength)
        {
            Debug.Assert(TypeSymbol.Equals(
                unloweredExpr.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Index),
                TypeCompareKind.ConsiderEverything));

            var F = _factory;

            // The argument value may be a non-placeholder value (in an element access scenario: `expr[^1]`or `array[^1]`)
            // or a placeholder value (in a list-pattern scenario: `expr is [_, var x]` or `array is [_, var x]`).
            unloweredExpr = UnwrapPlaceholderIfNeeded(unloweredExpr);

            if (unloweredExpr is BoundFromEndIndexExpression hatExpression)
            {
                // If the System.Index argument is `^index`, we can replace the
                // `argument.GetOffset(length)` call with `length - index`
                Debug.Assert(hatExpression.Operand is { Type: { SpecialType: SpecialType.System_Int32 } });
                usedLength = true;

                if (hatExpression.Operand.ConstantValue is { Int32Value: 0 })
                {
                    return loweredLengthAccess;
                }
                return F.IntSubtract(loweredLengthAccess, VisitExpression(hatExpression.Operand));
            }
            else if (unloweredExpr is BoundConversion { Operand: { Type: { SpecialType: SpecialType.System_Int32 } } operand })
            {
                // If the System.Index argument is a conversion from int to Index we
                // can return the int directly
                usedLength = false;
                return VisitExpression(operand);
            }
            else
            {
                usedLength = true;
                return F.Call(
                    VisitExpression(unloweredExpr),
                    WellKnownMember.System_Index__GetOffset,
                    loweredLengthAccess);
            }
        }

        private BoundSequence VisitRangeImplicitIndexerAccess(BoundIndexOrRangePatternIndexerAccess node)
        {
            Debug.Assert(node.ArgumentPlaceholders.Length == 2);
            Debug.Assert(node.IndexerAccess is BoundCall);

            Debug.Assert(TypeSymbol.Equals(
                node.Argument.Type,
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

            var receiverLocal = F.StoreToTemp(VisitExpression(node.Receiver), out var receiverStore);
            var receiverPlaceholder = node.ReceiverPlaceholder;
            AddPlaceholderReplacement(receiverPlaceholder, receiverLocal);

            Debug.Assert(node.LengthOrCountAccess is not null);

            var loweredLengthAccess = VisitExpression(node.LengthOrCountAccess);
            BoundAssignmentOperator? lengthStore = null;
            LocalSymbol? lengthLocal = null;
            // In a list-pattern, the lengthAccess will already be a a local, but in other cases we want to make one
            if (CanChangeValueBetweenReads(loweredLengthAccess, localsMayBeAssignedOrCaptured: false))
            {
                var boundLocal = F.StoreToTemp(loweredLengthAccess, out lengthStore);
                loweredLengthAccess = boundLocal;
                lengthLocal = boundLocal.LocalSymbol;
            }

            localsBuilder.Add(receiverLocal.LocalSymbol);
            sideEffectsBuilder.Add(receiverStore);

            // The argument value may be a non-placeholder value (in an element access scenario: expr[..])
            // or a placeholder value (in a slice-pattern scenario: expr is [_, .. var x]).
            var rangeArg = UnwrapPlaceholderIfNeeded(node.Argument);
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
                        MakePatternIndexOffsetExpression(rangeExpr.LeftOperandOpt, loweredLengthAccess, out usedLength),
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
                        loweredLengthAccess,
                        out bool usedLengthTemp);
                    usedLength |= usedLengthTemp;
                }
                else
                {
                    usedLength = true;
                    endExpr = loweredLengthAccess;
                }

                if (usedLength && lengthStore is not null)
                {
                    // If we used the length, it needs to be calculated after the receiver (the
                    // first bound node in the builder) and before the first use, which could be the
                    // second or third node in the builder
                    localsBuilder.Insert(1, lengthLocal!);
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

                if (lengthStore is not null)
                {
                    localsBuilder.Add(lengthLocal!);
                    sideEffectsBuilder.Add(lengthStore);
                }
                localsBuilder.Add(rangeLocal.LocalSymbol);
                sideEffectsBuilder.Add(rangeStore);

                var startLocal = F.StoreToTemp(
                    F.Call(
                        F.Call(rangeLocal, F.WellKnownMethod(WellKnownMember.System_Range__get_Start)),
                        F.WellKnownMethod(WellKnownMember.System_Index__GetOffset),
                        loweredLengthAccess),
                    out var startStore);

                localsBuilder.Add(startLocal.LocalSymbol);
                sideEffectsBuilder.Add(startStore);
                startExpr = startLocal;

                var rangeSizeLocal = F.StoreToTemp(
                    F.IntSubtract(
                        F.Call(
                            F.Call(rangeLocal, F.WellKnownMethod(WellKnownMember.System_Range__get_End)),
                            F.WellKnownMethod(WellKnownMember.System_Index__GetOffset),
                            loweredLengthAccess),
                        startExpr),
                    out var rangeSizeStore);

                localsBuilder.Add(rangeSizeLocal.LocalSymbol);
                sideEffectsBuilder.Add(rangeSizeStore);
                rangeSizeExpr = rangeSizeLocal;
            }

            AddPlaceholderReplacement(node.ArgumentPlaceholders[0], startExpr);
            AddPlaceholderReplacement(node.ArgumentPlaceholders[1], rangeSizeExpr);

            var rewrittenIndexerAccess = VisitExpression(node.IndexerAccess);

            RemovePlaceholderReplacement(receiverPlaceholder);
            RemovePlaceholderReplacement(node.ArgumentPlaceholders[0]);
            RemovePlaceholderReplacement(node.ArgumentPlaceholders[1]);

            return (BoundSequence)F.Sequence(
                localsBuilder.ToImmutableAndFree(),
                sideEffectsBuilder.ToImmutableAndFree(),
                rewrittenIndexerAccess);
        }
    }
}
