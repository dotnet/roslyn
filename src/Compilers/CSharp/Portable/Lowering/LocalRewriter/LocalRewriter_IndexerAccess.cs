// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
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
            ImmutableArray<string?> argumentNames,
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
                node,
                isLeftOfAssignment);
        }

        private BoundExpression MakeIndexerAccess(
            SyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            PropertySymbol indexer,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<string?> argumentNamesOpt,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            BitVector defaultArguments,
            BoundExpression originalIndexerAccessOrObjectInitializerMember,
            bool isLeftOfAssignment)
        {
            Debug.Assert(originalIndexerAccessOrObjectInitializerMember is BoundIndexerAccess or BoundObjectInitializerMember);
            Debug.Assert(originalIndexerAccessOrObjectInitializerMember.Type is not null);

            if (isLeftOfAssignment && indexer.RefKind == RefKind.None)
            {
                TypeSymbol type = indexer.Type;
                Debug.Assert(originalIndexerAccessOrObjectInitializerMember.Type.Equals(type, TypeCompareKind.ConsiderEverything));

                // This is an indexer set access. We return a BoundIndexerAccess node here.
                // This node will be rewritten with MakePropertyAssignment when rewriting the enclosing BoundAssignmentOperator.

                return originalIndexerAccessOrObjectInitializerMember is BoundIndexerAccess indexerAccess ?
                    indexerAccess.Update(rewrittenReceiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, indexer, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, defaultArguments, type) :
                    new BoundIndexerAccess(syntax, rewrittenReceiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, indexer, arguments, argumentNamesOpt, argumentRefKindsOpt, expanded, argsToParamsOpt, defaultArguments, type);
            }
            else
            {
                var getMethod = indexer.GetOwnOrInheritedGetMethod();
                Debug.Assert(getMethod is not null);

                ArrayBuilder<LocalSymbol>? temps = null;
                ImmutableArray<BoundExpression> rewrittenArguments = VisitArgumentsAndCaptureReceiverIfNeeded(
                    ref rewrittenReceiver,
                    captureReceiverMode: ReceiverCaptureMode.Default,
                    arguments,
                    indexer,
                    argsToParamsOpt,
                    argumentRefKindsOpt,
                    storesOpt: null,
                    ref temps);

                rewrittenArguments = MakeArguments(
                    rewrittenArguments,
                    indexer,
                    expanded,
                    argsToParamsOpt,
                    ref argumentRefKindsOpt,
                    ref temps);

                BoundExpression call = MakePropertyGetAccess(syntax, rewrittenReceiver, indexer, rewrittenArguments, argumentRefKindsOpt, getMethod);

                if (originalIndexerAccessOrObjectInitializerMember.Type.IsDynamic() == true && !indexer.Type.IsDynamic())
                {
                    Debug.Assert(call.Type is not null);
                    Debug.Assert(!call.Type.IsDynamic());
                    Debug.Assert(!getMethod.ReturnsByRef);
                    call = _factory.Convert(originalIndexerAccessOrObjectInitializerMember.Type, call);
                }

                Debug.Assert(call.Type is not null);

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
                        call.Type);
                }
            }
        }

        public override BoundNode? VisitInlineArrayAccess(BoundInlineArrayAccess node)
        {
            Debug.Assert(_factory.ModuleBuilderOpt is { });
            Debug.Assert(_diagnostics.DiagnosticBag is { });
            Debug.Assert(node.Expression.Type is object);
            Debug.Assert(node.Argument.Type is object);

            var rewrittenReceiver = VisitExpression(node.Expression);
            BoundAssignmentOperator? receiverStore = null;

            if (node.IsValue && node.GetItemOrSliceHelper == WellKnownMember.System_ReadOnlySpan_T__get_Item)
            {
                // Clone receiver because it is a value, but we need a variable to be able to get a span
                rewrittenReceiver = _factory.StoreToTemp(rewrittenReceiver, out receiverStore);
            }

            var getItemOrSliceHelper = (MethodSymbol?)_compilation.GetWellKnownTypeMember(node.GetItemOrSliceHelper);
            Debug.Assert(getItemOrSliceHelper is object);

            BoundExpression result;
            _ = node.Expression.Type.HasInlineArrayAttribute(out int length);

            if (node.Argument.Type.SpecialType == SpecialType.System_Int32)
            {
                result = getElementRef(node, rewrittenReceiver, index: VisitExpression(node.Argument), getItemOrSliceHelper, length);
            }
            else
            {
                Debug.Assert(length > 0);

                if (TypeSymbol.Equals(node.Argument.Type, _compilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.AllIgnoreOptions))
                {
                    BoundExpression makeOffsetInput = DetermineMakePatternIndexOffsetExpressionStrategy(node.Argument, out PatternIndexOffsetLoweringStrategy strategy);
                    BoundExpression integerArgument = makePatternIndexOffsetExpression(makeOffsetInput, length, strategy);

                    result = getElementRef(node, rewrittenReceiver, index: integerArgument, getItemOrSliceHelper, length);
                }
                else
                {
                    // createSpan(ref receiver, length).Slice(range converted to start, range converted to size)

                    Debug.Assert(receiverStore is null);
                    Debug.Assert(TypeSymbol.Equals(node.Argument.Type, _compilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.AllIgnoreOptions));

                    MethodSymbol createSpan = getCreateSpanHelper(node, spanType: getItemOrSliceHelper.ContainingType, intType: (NamedTypeSymbol)getItemOrSliceHelper.Parameters[0].Type);
                    getItemOrSliceHelper = getItemOrSliceHelper.AsMember((NamedTypeSymbol)createSpan.ReturnType);

                    BoundRangeExpression? rangeExpr;
                    BoundExpression? startMakeOffsetInput, endMakeOffsetInput, rewrittenRangeArg;
                    PatternIndexOffsetLoweringStrategy startStrategy, endStrategy;
                    RewriteRangeParts(node.Argument, out rangeExpr, out startMakeOffsetInput, out startStrategy, out endMakeOffsetInput, out endStrategy, out rewrittenRangeArg);

                    var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
                    var sideEffectsBuilder = ArrayBuilder<BoundExpression>.GetInstance();

                    BoundExpression startExpr;
                    BoundExpression rangeSizeExpr;
                    if (rangeExpr is not null)
                    {

                        startExpr = makePatternIndexOffsetExpression(startMakeOffsetInput, length, startStrategy);
                        BoundExpression endExpr = makePatternIndexOffsetExpression(endMakeOffsetInput, length, endStrategy);
                        rangeSizeExpr = MakeRangeSize(ref startExpr, endExpr, localsBuilder, sideEffectsBuilder);
                    }
                    else
                    {
                        Debug.Assert(rewrittenRangeArg is not null);
                        DeconstructRange(rewrittenRangeArg, _factory.Literal(length), localsBuilder, sideEffectsBuilder, out startExpr, out rangeSizeExpr);
                    }

                    BoundExpression possiblyRefCapturedReceiver = rewrittenReceiver;

                    if (sideEffectsBuilder.Count != 0)
                    {
                        possiblyRefCapturedReceiver = _factory.StoreToTemp(possiblyRefCapturedReceiver, out var refCapture, createSpan.Parameters[0].RefKind == RefKind.In ? RefKindExtensions.StrictIn : RefKind.Ref);
                        localsBuilder.Insert(0, ((BoundLocal)possiblyRefCapturedReceiver).LocalSymbol);
                        sideEffectsBuilder.Insert(0, refCapture);
                    }

                    if (startExpr.ConstantValueOpt is { SpecialType: SpecialType.System_Int32, Int32Value: 0 } &&
                        rangeSizeExpr.ConstantValueOpt is { SpecialType: SpecialType.System_Int32, Int32Value: >= 0 and int rangeSizeConst } &&
                        rangeSizeConst <= length)
                    {
                        // No need to call Slice, we can create a Span of the right length from the start.
                        result = _factory.Call(null, createSpan, possiblyRefCapturedReceiver, rangeSizeExpr, useStrictArgumentRefKinds: true);
                    }
                    else
                    {
                        result = _factory.Call(_factory.Call(null, createSpan, possiblyRefCapturedReceiver, _factory.Literal(length), useStrictArgumentRefKinds: true),
                                           getItemOrSliceHelper, startExpr, rangeSizeExpr);
                    }

                    result = _factory.Sequence(localsBuilder.ToImmutableAndFree(), sideEffectsBuilder.ToImmutableAndFree(), result);
                }
            }

            if (receiverStore is not null)
            {
                result = _factory.Sequence(ImmutableArray.Create(((BoundLocal)rewrittenReceiver).LocalSymbol),
                                           ImmutableArray.Create((BoundExpression)receiverStore),
                                           result);
            }

            return result;

            BoundExpression makePatternIndexOffsetExpression(BoundExpression? makeOffsetInput, int length, PatternIndexOffsetLoweringStrategy strategy)
            {
                BoundExpression integerArgument;

                if (strategy == PatternIndexOffsetLoweringStrategy.SubtractFromLength &&
                    makeOffsetInput is { ConstantValueOpt.Int32Value: var offset })
                {
                    integerArgument = _factory.Literal(length - offset);
                }
                else
                {
                    integerArgument = MakePatternIndexOffsetExpression(makeOffsetInput, _factory.Literal(length), strategy);
                }

                return integerArgument;
            }

            MethodSymbol getCreateSpanHelper(BoundInlineArrayAccess node, NamedTypeSymbol spanType, NamedTypeSymbol intType)
            {
                Debug.Assert(node.Expression.Type is object);

                MethodSymbol createSpan;
                if (node.GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int or WellKnownMember.System_ReadOnlySpan_T__get_Item)
                {
                    createSpan = _factory.ModuleBuilderOpt.EnsureInlineArrayAsReadOnlySpanExists(node.Syntax, spanType, intType, _diagnostics.DiagnosticBag);
                }
                else
                {
                    createSpan = _factory.ModuleBuilderOpt.EnsureInlineArrayAsSpanExists(node.Syntax, spanType, intType, _diagnostics.DiagnosticBag);
                }

                return createSpan.Construct(node.Expression.Type, node.Expression.Type.TryGetInlineArrayElementField()!.Type);
            }

            BoundExpression getElementRef(BoundInlineArrayAccess node, BoundExpression rewrittenReceiver, BoundExpression index, MethodSymbol getItemOrSliceHelper, int length)
            {
                Debug.Assert(node.Expression.Type is object);
                Debug.Assert(index.Type?.SpecialType == SpecialType.System_Int32);

                var intType = (NamedTypeSymbol)index.Type;

                if (index.ConstantValueOpt is { SpecialType: SpecialType.System_Int32, Int32Value: int constIndex })
                {
                    if (constIndex == 0)
                    {
                        // getFirstElementRef(ref receiver)
                        MethodSymbol elementRef;

                        if (node.GetItemOrSliceHelper is WellKnownMember.System_Span_T__get_Item)
                        {
                            elementRef = _factory.ModuleBuilderOpt.EnsureInlineArrayFirstElementRefExists(node.Syntax, _diagnostics.DiagnosticBag);
                        }
                        else
                        {
                            Debug.Assert(node.GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__get_Item);
                            elementRef = _factory.ModuleBuilderOpt.EnsureInlineArrayFirstElementRefReadOnlyExists(node.Syntax, _diagnostics.DiagnosticBag);
                        }

                        elementRef = elementRef.Construct(node.Expression.Type, node.Expression.Type.TryGetInlineArrayElementField()!.Type);

                        return _factory.Call(null, elementRef, rewrittenReceiver, useStrictArgumentRefKinds: true);
                    }
                    else if (constIndex > 0 && constIndex < length)
                    {
                        // getElementRef(ref receiver, index)
                        MethodSymbol elementRef;

                        if (node.GetItemOrSliceHelper is WellKnownMember.System_Span_T__get_Item)
                        {
                            elementRef = _factory.ModuleBuilderOpt.EnsureInlineArrayElementRefExists(node.Syntax, intType, _diagnostics.DiagnosticBag);
                        }
                        else
                        {
                            Debug.Assert(node.GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__get_Item);
                            elementRef = _factory.ModuleBuilderOpt.EnsureInlineArrayElementRefReadOnlyExists(node.Syntax, intType, _diagnostics.DiagnosticBag);
                        }

                        elementRef = elementRef.Construct(node.Expression.Type, node.Expression.Type.TryGetInlineArrayElementField()!.Type);

                        return _factory.Call(null, elementRef, rewrittenReceiver, index, useStrictArgumentRefKinds: true);
                    }
                }

                Debug.Assert(index.ConstantValueOpt is null); // Binder should have reported an error due to index out of bounds, or should have handled by code above.

                // createSpan(ref receiver, length)[index]

                NamedTypeSymbol spanType = getItemOrSliceHelper.ContainingType;
                MethodSymbol createSpan = getCreateSpanHelper(node, spanType, intType);
                getItemOrSliceHelper = getItemOrSliceHelper.AsMember((NamedTypeSymbol)createSpan.ReturnType);
                return _factory.Call(_factory.Call(null, createSpan, rewrittenReceiver, _factory.Literal(length), useStrictArgumentRefKinds: true), getItemOrSliceHelper, index);
            }
        }

        public override BoundNode? VisitListPatternIndexPlaceholder(BoundListPatternIndexPlaceholder node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitListPatternReceiverPlaceholder(BoundListPatternReceiverPlaceholder node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitSlicePatternRangePlaceholder(BoundSlicePatternRangePlaceholder node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitSlicePatternReceiverPlaceholder(BoundSlicePatternReceiverPlaceholder node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundNode? VisitImplicitIndexerReceiverPlaceholder(BoundImplicitIndexerReceiverPlaceholder node)
        {
            return PlaceholderReplacement(node);
        }

        public override BoundNode? VisitImplicitIndexerValuePlaceholder(BoundImplicitIndexerValuePlaceholder node)
        {
            return PlaceholderReplacement(node);
        }

        public override BoundNode VisitImplicitIndexerAccess(BoundImplicitIndexerAccess node)
        {
            return VisitImplicitIndexerAccess(node, isLeftOfAssignment: false);
        }

        private BoundExpression VisitImplicitIndexerAccess(BoundImplicitIndexerAccess node, bool isLeftOfAssignment)
        {
            if (TypeSymbol.Equals(
                node.Argument.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Index),
                TypeCompareKind.ConsiderEverything))
            {
                return VisitIndexPatternIndexerAccess(node, isLeftOfAssignment: isLeftOfAssignment);
            }
            else
            {
                Debug.Assert(TypeSymbol.Equals(
                    node.Argument.Type,
                    _compilation.GetWellKnownType(WellKnownType.System_Range),
                    TypeCompareKind.ConsiderEverything));
                Debug.Assert(!isLeftOfAssignment || node.IndexerOrSliceAccess.GetRefKind() == RefKind.Ref);

                return VisitRangePatternIndexerAccess(node);
            }
        }

        private BoundExpression VisitIndexPatternIndexerAccess(BoundImplicitIndexerAccess node, bool isLeftOfAssignment)
        {
            var locals = ArrayBuilder<LocalSymbol>.GetInstance(2);
            var sideeffects = ArrayBuilder<BoundExpression>.GetInstance(2);

            BoundExpression rewrittenIndexerAccess = GetUnderlyingIndexerOrSliceAccess(
                node, isLeftOfAssignment,
                isRegularAssignmentOrRegularCompoundAssignment: isLeftOfAssignment,
                cacheAllArgumentsOnly: false,
                sideeffects, locals);

            return _factory.Sequence(
                locals.ToImmutableAndFree(),
                sideeffects.ToImmutableAndFree(),
                rewrittenIndexerAccess);
        }

        private BoundExpression GetUnderlyingIndexerOrSliceAccess(
            BoundImplicitIndexerAccess node,
            bool isLeftOfAssignment,
            bool isRegularAssignmentOrRegularCompoundAssignment,
            bool cacheAllArgumentsOnly,
            ArrayBuilder<BoundExpression> sideeffects,
            ArrayBuilder<LocalSymbol> locals)
        {
            Debug.Assert(node.ArgumentPlaceholders.Length == 1);
            Debug.Assert(node.IndexerOrSliceAccess is BoundIndexerAccess or BoundArrayAccess);

            Debug.Assert(TypeSymbol.Equals(
                node.Argument.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Index),
                TypeCompareKind.ConsiderEverything));

            var F = _factory;
            BoundExpression makeOffsetInput = DetermineMakePatternIndexOffsetExpressionStrategy(node.Argument, out PatternIndexOffsetLoweringStrategy strategy);

            var receiver = VisitExpression(node.Receiver);

            // Do not capture receiver if we're in an initializer
            if (!cacheAllArgumentsOnly)
            {
                // Do not capture receiver if it is a local or parameter and we are evaluating a pattern
                // If length access is a local, then we are evaluating a pattern
                if (node.LengthOrCountAccess.Kind is not BoundKind.Local || receiver.Kind is not (BoundKind.Local or BoundKind.Parameter))
                {
                    Debug.Assert(receiver.Type is { });

                    var receiverLocal = F.StoreToTemp(
                        receiver,
                        out var receiverStore,
                        // Store the receiver as a ref local if it's a value type to ensure side effects are propagated
                        receiver.Type.IsReferenceType ? RefKind.None : RefKind.Ref);
                    locals.Add(receiverLocal.LocalSymbol);

                    if (receiverLocal.LocalSymbol.IsRef &&
                        CodeGenerator.IsPossibleReferenceTypeReceiverOfConstrainedCall(receiverLocal) &&
                        !CodeGenerator.ReceiverIsKnownToReferToTempIfReferenceType(receiverLocal) &&
                        ((isLeftOfAssignment && !isRegularAssignmentOrRegularCompoundAssignment) ||
                         !CodeGenerator.IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(ImmutableArray.Create(makeOffsetInput))))
                    {
                        BoundAssignmentOperator? extraRefInitialization;
                        ReferToTempIfReferenceTypeReceiver(receiverLocal, ref receiverStore, out extraRefInitialization, locals);

                        if (extraRefInitialization is object)
                        {
                            sideeffects.Add(extraRefInitialization);
                        }
                    }

                    sideeffects.Add(receiverStore);

                    receiver = receiverLocal;
                }
            }

            AddPlaceholderReplacement(node.ReceiverPlaceholder, receiver);

            BoundExpression integerArgument;

            switch (strategy)
            {
                case PatternIndexOffsetLoweringStrategy.SubtractFromLength:
                    BoundExpression lengthAccess = VisitExpression(node.LengthOrCountAccess);

                    // ensure we evaluate the input before accessing length, unless it is an array length
                    if (makeOffsetInput.ConstantValueOpt is null && lengthAccess.Kind is not BoundKind.ArrayLength)
                    {
                        makeOffsetInput = F.StoreToTemp(makeOffsetInput, out BoundAssignmentOperator inputStore);
                        locals.Add(((BoundLocal)makeOffsetInput).LocalSymbol);
                        sideeffects.Add(inputStore);
                    }

                    integerArgument = MakePatternIndexOffsetExpression(makeOffsetInput, lengthAccess, strategy);
                    break;

                case PatternIndexOffsetLoweringStrategy.UseAsIs:
                    integerArgument = MakePatternIndexOffsetExpression(makeOffsetInput, lengthAccess: null, strategy);
                    break;

                case PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI:
                    integerArgument = MakePatternIndexOffsetExpression(makeOffsetInput, VisitExpression(node.LengthOrCountAccess), strategy);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(strategy);
            }

            Debug.Assert(node.ArgumentPlaceholders.Length == 1);
            var argumentPlaceholder = node.ArgumentPlaceholders[0];
            Debug.Assert(integerArgument.Type!.SpecialType == SpecialType.System_Int32);

            BoundExpression rewrittenIndexerAccess;

            if (node.IndexerOrSliceAccess is BoundIndexerAccess indexerAccess)
            {
                Debug.Assert(indexerAccess.Arguments.Length == 1);
                if (isLeftOfAssignment && indexerAccess.GetRefKind() == RefKind.None)
                {
                    // Note: we currently don't honor cacheAllArgumentsOnly in this branch, and let
                    // callers do the caching instead
                    // Tracked by https://github.com/dotnet/roslyn/issues/71056
                    AddPlaceholderReplacement(argumentPlaceholder, integerArgument);
                    ImmutableArray<BoundExpression> rewrittenArguments = VisitArgumentsAndCaptureReceiverIfNeeded(
                        ref receiver,
                        captureReceiverMode: ReceiverCaptureMode.Default,
                        indexerAccess.Arguments,
                        indexerAccess.Indexer,
                        indexerAccess.ArgsToParamsOpt,
                        indexerAccess.ArgumentRefKindsOpt,
                        storesOpt: null,
                        ref locals!);

                    Debug.Assert(locals is not null);

                    rewrittenIndexerAccess = indexerAccess.Update(
                        receiver, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, indexerAccess.Indexer, rewrittenArguments,
                        indexerAccess.ArgumentNamesOpt, indexerAccess.ArgumentRefKindsOpt,
                        indexerAccess.Expanded,
                        indexerAccess.ArgsToParamsOpt,
                        indexerAccess.DefaultArguments,
                        indexerAccess.Type);
                }
                else
                {
                    if (cacheAllArgumentsOnly)
                    {
                        var integerTemp = F.StoreToTemp(integerArgument, out BoundAssignmentOperator integerStore);
                        locals.Add(integerTemp.LocalSymbol);
                        sideeffects.Add(integerStore);
                        integerArgument = integerTemp;
                    }

                    AddPlaceholderReplacement(argumentPlaceholder, integerArgument);
                    rewrittenIndexerAccess = VisitIndexerAccess(indexerAccess, isLeftOfAssignment);
                }
            }
            else
            {
                if (cacheAllArgumentsOnly)
                {
                    var integerTemp = F.StoreToTemp(integerArgument, out BoundAssignmentOperator integerStore);
                    locals.Add(integerTemp.LocalSymbol);
                    sideeffects.Add(integerStore);
                    integerArgument = integerTemp;
                }

                AddPlaceholderReplacement(argumentPlaceholder, integerArgument);
                rewrittenIndexerAccess = (BoundExpression)VisitArrayAccess((BoundArrayAccess)node.IndexerOrSliceAccess);
            }

            RemovePlaceholderReplacement(argumentPlaceholder);
            RemovePlaceholderReplacement(node.ReceiverPlaceholder);

            return rewrittenIndexerAccess;
        }

        /// <summary>
        /// Used to produce an expression translating <paramref name="loweredExpr"/> to an integer offset
        /// according to the <paramref name="strategy"/>.
        /// The implementation should be in sync with <see cref="DetermineMakePatternIndexOffsetExpressionStrategy"/>.
        /// </summary>
        /// <param name="loweredExpr">The lowered input for the translation</param>
        /// <param name="lengthAccess">
        /// An expression accessing the length of the indexing target. This should
        /// be a non-side-effecting operation.
        /// </param>
        /// <param name="strategy">The translation strategy</param>
        private BoundExpression MakePatternIndexOffsetExpression(
            BoundExpression? loweredExpr,
            BoundExpression? lengthAccess,
            PatternIndexOffsetLoweringStrategy strategy)
        {
            switch (strategy)
            {
                case PatternIndexOffsetLoweringStrategy.Zero:
                    return _factory.Literal(0);

                case PatternIndexOffsetLoweringStrategy.Length:
                    Debug.Assert(lengthAccess is not null);
                    return lengthAccess;

                case PatternIndexOffsetLoweringStrategy.SubtractFromLength:
                    Debug.Assert(loweredExpr is not null);
                    Debug.Assert(lengthAccess is not null);
                    Debug.Assert(loweredExpr.Type!.SpecialType == SpecialType.System_Int32);

                    if (loweredExpr.ConstantValueOpt?.Int32Value == 0)
                    {
                        return lengthAccess;
                    }

                    return _factory.IntSubtract(lengthAccess, loweredExpr);

                case PatternIndexOffsetLoweringStrategy.UseAsIs:
                    Debug.Assert(loweredExpr is not null);
                    Debug.Assert(loweredExpr.Type!.SpecialType == SpecialType.System_Int32);
                    return loweredExpr;

                case PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI:
                    Debug.Assert(loweredExpr is not null);
                    Debug.Assert(lengthAccess is not null);
                    Debug.Assert(TypeSymbol.Equals(
                        loweredExpr.Type,
                        _compilation.GetWellKnownType(WellKnownType.System_Index),
                        TypeCompareKind.ConsiderEverything));

                    return _factory.Call(
                        loweredExpr,
                        WellKnownMember.System_Index__GetOffset,
                        lengthAccess);

                default:
                    throw ExceptionUtilities.UnexpectedValue(strategy);
            }
        }

        private enum PatternIndexOffsetLoweringStrategy
        {
            Zero,
            Length,
            SubtractFromLength,
            UseAsIs,
            UseGetOffsetAPI
        }

        /// <summary>
        /// Determine the lowering strategy for translating a System.Index value to an integer offset value
        /// and prepare the lowered input for the translation process handled by <see cref="MakePatternIndexOffsetExpression"/>.
        /// The implementation should be in sync with <see cref="MakePatternIndexOffsetExpression"/>.
        /// </summary>
        private BoundExpression DetermineMakePatternIndexOffsetExpressionStrategy(
            BoundExpression unloweredExpr,
            out PatternIndexOffsetLoweringStrategy strategy)
        {
            Debug.Assert(TypeSymbol.Equals(
                unloweredExpr.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Index),
                TypeCompareKind.ConsiderEverything));

            if (unloweredExpr is BoundFromEndIndexExpression hatExpression)
            {
                // If the System.Index argument is `^index`, we can replace the
                // `argument.GetOffset(length)` call with `length - index`
                Debug.Assert(hatExpression.Operand is { Type: { SpecialType: SpecialType.System_Int32 } });
                strategy = PatternIndexOffsetLoweringStrategy.SubtractFromLength;
                return VisitExpression(hatExpression.Operand);
            }
            else if (unloweredExpr is BoundConversion { Operand: { Type: { SpecialType: SpecialType.System_Int32 } } operand })
            {
                // If the System.Index argument is a conversion from int to Index we
                // can return the int directly
                strategy = PatternIndexOffsetLoweringStrategy.UseAsIs;
                return VisitExpression(operand);
            }
            else if (unloweredExpr is BoundObjectCreationExpression { Constructor: MethodSymbol constructor, Arguments: { Length: 2 } arguments, ArgsToParamsOpt: { IsDefaultOrEmpty: true }, InitializerExpressionOpt: null } &&
                     (object)constructor == _compilation.GetWellKnownTypeMember(WellKnownMember.System_Index__ctor) &&
                     arguments[0] is { Type.SpecialType: SpecialType.System_Int32, ConstantValueOpt.Value: int _ and >= 0 } index &&
                     arguments[1] is { Type.SpecialType: SpecialType.System_Boolean, ConstantValueOpt.Value: bool fromEnd })
            {
                if (fromEnd)
                {
                    // We can replace the `argument.GetOffset(length)` call with `length - index`
                    strategy = PatternIndexOffsetLoweringStrategy.SubtractFromLength;
                }
                else
                {
                    // We can return the int directly
                    strategy = PatternIndexOffsetLoweringStrategy.UseAsIs;
                }

                return VisitExpression(index);
            }
            else
            {
                // `argument.GetOffset(length)`
                strategy = PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI;
                return VisitExpression(unloweredExpr);
            }
        }

        private BoundExpression VisitRangePatternIndexerAccess(BoundImplicitIndexerAccess node)
        {
            var F = _factory;
            var localsBuilder = ArrayBuilder<LocalSymbol>.GetInstance();
            var sideEffectsBuilder = ArrayBuilder<BoundExpression>.GetInstance();

            var rewrittenIndexerAccess = VisitRangePatternIndexerAccess(node, localsBuilder, sideEffectsBuilder, cacheAllArgumentsOnly: false);

            return F.Sequence(
                localsBuilder.ToImmutableAndFree(),
                sideEffectsBuilder.ToImmutableAndFree(),
                rewrittenIndexerAccess);
        }

        private BoundExpression VisitRangePatternIndexerAccess(BoundImplicitIndexerAccess node, ArrayBuilder<LocalSymbol> localsBuilder, ArrayBuilder<BoundExpression> sideEffectsBuilder, bool cacheAllArgumentsOnly)
        {
            Debug.Assert(node.ArgumentPlaceholders.Length == 2);
            Debug.Assert(node.IndexerOrSliceAccess is BoundCall);

            Debug.Assert(TypeSymbol.Equals(
                node.Argument.Type,
                _compilation.GetWellKnownType(WellKnownType.System_Range),
                TypeCompareKind.ConsiderEverything));

            // Lowered code without optimizations:
            // var receiver = receiverExpr;
            // Range range = argumentExpr;
            // int length = receiver.length;
            // int start = range.Start.GetOffset(length)
            // int rangeSize = range.End.GetOffset(length) - start
            // receiver.Slice(start, rangeSize)

            var F = _factory;

            var receiver = VisitExpression(node.Receiver);
            var rangeArg = node.Argument;

            BoundRangeExpression? rangeExpr;
            BoundExpression? startMakeOffsetInput, endMakeOffsetInput, rewrittenRangeArg;
            PatternIndexOffsetLoweringStrategy startStrategy, endStrategy;
            RewriteRangeParts(rangeArg, out rangeExpr, out startMakeOffsetInput, out startStrategy, out endMakeOffsetInput, out endStrategy, out rewrittenRangeArg);

            // Do not capture receiver if it is a local or parameter and we are evaluating a pattern
            // If length access is a local, then we are evaluating a pattern
            if (node.LengthOrCountAccess.Kind is not BoundKind.Local || receiver.Kind is not (BoundKind.Local or BoundKind.Parameter))
            {
                Debug.Assert(receiver.Type is { });

                var receiverLocal = F.StoreToTemp(
                    receiver,
                    out var receiverStore,
                    // Store the receiver as a ref local if it's a value type to ensure side effects are propagated
                    receiver.Type.IsReferenceType ? RefKind.None : RefKind.Ref);

                localsBuilder.Add(receiverLocal.LocalSymbol);

                if (receiverLocal.LocalSymbol.IsRef &&
                    CodeGenerator.IsPossibleReferenceTypeReceiverOfConstrainedCall(receiverLocal) &&
                    !CodeGenerator.ReceiverIsKnownToReferToTempIfReferenceType(receiverLocal))
                {
                    var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(2);

                    if (startMakeOffsetInput is not null)
                    {
                        argumentsBuilder.Add(startMakeOffsetInput);
                    }

                    if (endMakeOffsetInput is not null)
                    {
                        argumentsBuilder.Add(endMakeOffsetInput);
                    }

                    if (rewrittenRangeArg is not null)
                    {
                        argumentsBuilder.Add(rewrittenRangeArg);
                    }

                    if (!CodeGenerator.IsSafeToDereferenceReceiverRefAfterEvaluatingArguments(argumentsBuilder.ToImmutableAndFree()))
                    {
                        BoundAssignmentOperator? extraRefInitialization;
                        ReferToTempIfReferenceTypeReceiver(receiverLocal, ref receiverStore, out extraRefInitialization, localsBuilder);

                        if (extraRefInitialization is object)
                        {
                            sideEffectsBuilder.Add(extraRefInitialization);
                        }
                    }
                }

                sideEffectsBuilder.Add(receiverStore);

                receiver = receiverLocal;
            }

            AddPlaceholderReplacement(node.ReceiverPlaceholder, receiver);

            BoundExpression startExpr;
            BoundExpression rangeSizeExpr;
            if (rangeExpr is not null)
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

                const int captureStartOffset = 1 << 0;
                const int captureEndOffset = 1 << 1;
                const int useLength = 1 << 2;
                const int captureLength = 1 << 3;

                int rewriteFlags;

                switch ((startStrategy, endStrategy))
                {
                    case (PatternIndexOffsetLoweringStrategy.Zero, PatternIndexOffsetLoweringStrategy.Length):
                    case (PatternIndexOffsetLoweringStrategy.Zero, PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI):
                        rewriteFlags = useLength;
                        break;
                    case (PatternIndexOffsetLoweringStrategy.Zero, PatternIndexOffsetLoweringStrategy.SubtractFromLength):
                        rewriteFlags = captureEndOffset | useLength;
                        break;
                    case (PatternIndexOffsetLoweringStrategy.Zero, PatternIndexOffsetLoweringStrategy.UseAsIs):
                        rewriteFlags = 0;
                        break;
                    case (PatternIndexOffsetLoweringStrategy.UseAsIs, PatternIndexOffsetLoweringStrategy.Length):
                    case (PatternIndexOffsetLoweringStrategy.UseAsIs, PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI):
                        rewriteFlags = useLength;
                        break;
                    case (PatternIndexOffsetLoweringStrategy.UseAsIs, PatternIndexOffsetLoweringStrategy.SubtractFromLength):
                        rewriteFlags = captureStartOffset | captureEndOffset | useLength;
                        break;
                    case (PatternIndexOffsetLoweringStrategy.UseAsIs, PatternIndexOffsetLoweringStrategy.UseAsIs):
                        rewriteFlags = 0;
                        break;
                    case (PatternIndexOffsetLoweringStrategy.SubtractFromLength, PatternIndexOffsetLoweringStrategy.Length):
                    case (PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI, PatternIndexOffsetLoweringStrategy.Length):
                        rewriteFlags = captureStartOffset | useLength | captureLength;
                        break;
                    case (PatternIndexOffsetLoweringStrategy.SubtractFromLength, PatternIndexOffsetLoweringStrategy.SubtractFromLength):
                    case (PatternIndexOffsetLoweringStrategy.SubtractFromLength, PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI):
                    case (PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI, PatternIndexOffsetLoweringStrategy.SubtractFromLength):
                    case (PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI, PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI):
                        rewriteFlags = captureStartOffset | captureEndOffset | useLength | captureLength;
                        break;
                    case (PatternIndexOffsetLoweringStrategy.SubtractFromLength, PatternIndexOffsetLoweringStrategy.UseAsIs):
                    case (PatternIndexOffsetLoweringStrategy.UseGetOffsetAPI, PatternIndexOffsetLoweringStrategy.UseAsIs):
                        rewriteFlags = captureStartOffset | captureEndOffset | useLength;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(startStrategy);
                }

                Debug.Assert(startStrategy != PatternIndexOffsetLoweringStrategy.Zero || (rewriteFlags & captureStartOffset) == 0);
                Debug.Assert((rewriteFlags & captureEndOffset) == 0 || (rewriteFlags & captureStartOffset) != 0 || startStrategy == PatternIndexOffsetLoweringStrategy.Zero);
                Debug.Assert((rewriteFlags & captureStartOffset) == 0 || (rewriteFlags & captureEndOffset) != 0 || endStrategy == PatternIndexOffsetLoweringStrategy.Length);
                Debug.Assert(endStrategy != PatternIndexOffsetLoweringStrategy.Length || (rewriteFlags & captureEndOffset) == 0);
                Debug.Assert((rewriteFlags & captureLength) == 0 || (rewriteFlags & useLength) != 0);

                if ((rewriteFlags & captureStartOffset) != 0)
                {
                    Debug.Assert(startMakeOffsetInput is not null);
                    if (startMakeOffsetInput.ConstantValueOpt is null)
                    {
                        startMakeOffsetInput = F.StoreToTemp(startMakeOffsetInput, out BoundAssignmentOperator inputStore);
                        localsBuilder.Add(((BoundLocal)startMakeOffsetInput).LocalSymbol);
                        sideEffectsBuilder.Add(inputStore);
                    }
                }

                if ((rewriteFlags & captureEndOffset) != 0)
                {
                    Debug.Assert(endMakeOffsetInput is not null);
                    if (endMakeOffsetInput.ConstantValueOpt is null)
                    {
                        endMakeOffsetInput = F.StoreToTemp(endMakeOffsetInput, out BoundAssignmentOperator inputStore);
                        localsBuilder.Add(((BoundLocal)endMakeOffsetInput).LocalSymbol);
                        sideEffectsBuilder.Add(inputStore);
                    }
                }

                BoundExpression? lengthAccess = null;

                if ((rewriteFlags & useLength) != 0)
                {
                    lengthAccess = VisitExpression(node.LengthOrCountAccess);

                    // If length access is a local, then we are evaluating a pattern and don't need to capture the value.
                    if ((rewriteFlags & captureLength) != 0 && lengthAccess.Kind is not BoundKind.Local)
                    {
                        var lengthLocal = F.StoreToTemp(lengthAccess, out var lengthStore);
                        localsBuilder.Add(lengthLocal.LocalSymbol);
                        sideEffectsBuilder.Add(lengthStore);
                        lengthAccess = lengthLocal;
                    }
                }

                startExpr = MakePatternIndexOffsetExpression(startMakeOffsetInput, lengthAccess, startStrategy);
                BoundExpression endExpr = MakePatternIndexOffsetExpression(endMakeOffsetInput, lengthAccess, endStrategy);
                rangeSizeExpr = MakeRangeSize(ref startExpr, endExpr, localsBuilder, sideEffectsBuilder);

                if (cacheAllArgumentsOnly)
                {
                    var startLocal = F.StoreToTemp(startExpr, out var startStore);
                    localsBuilder.Add(startLocal.LocalSymbol);
                    sideEffectsBuilder.Add(startStore);
                    startExpr = startLocal;

                    var rangeSizeLocal = F.StoreToTemp(rangeSizeExpr, out var rangeSizeStore);
                    localsBuilder.Add(rangeSizeLocal.LocalSymbol);
                    sideEffectsBuilder.Add(rangeSizeStore);
                    rangeSizeExpr = startLocal;
                }
            }
            else
            {
                Debug.Assert(rewrittenRangeArg is not null);
                DeconstructRange(rewrittenRangeArg, VisitExpression(node.LengthOrCountAccess), localsBuilder, sideEffectsBuilder, out startExpr, out rangeSizeExpr);
            }

            Debug.Assert(node.ArgumentPlaceholders.Length == 2);
            AddPlaceholderReplacement(node.ArgumentPlaceholders[0], startExpr);
            AddPlaceholderReplacement(node.ArgumentPlaceholders[1], rangeSizeExpr);

            var sliceCall = (BoundCall)node.IndexerOrSliceAccess;
            var rewrittenIndexerAccess = VisitExpression(sliceCall);

            RemovePlaceholderReplacement(node.ArgumentPlaceholders[0]);
            RemovePlaceholderReplacement(node.ArgumentPlaceholders[1]);
            RemovePlaceholderReplacement(node.ReceiverPlaceholder);

            return rewrittenIndexerAccess;
        }

        private BoundExpression MakeRangeSize(ref BoundExpression startExpr, BoundExpression endExpr, ArrayBuilder<LocalSymbol> localsBuilder, ArrayBuilder<BoundExpression> sideEffectsBuilder)
        {
            var F = _factory;

            BoundExpression rangeSizeExpr;

            if (startExpr.ConstantValueOpt?.Int32Value == 0)
            {
                rangeSizeExpr = endExpr;
            }
            else if (startExpr.ConstantValueOpt is { Int32Value: var startConst } && endExpr.ConstantValueOpt is { Int32Value: var endConst })
            {
                rangeSizeExpr = F.Literal(unchecked(endConst - startConst));
            }
            else
            {
                if (startExpr.ConstantValueOpt is null &&
                    startExpr is not BoundLocal { LocalSymbol.SynthesizedKind: not SynthesizedLocalKind.UserDefined })
                {
                    var startLocal = F.StoreToTemp(startExpr, out var startStore);
                    localsBuilder.Add(startLocal.LocalSymbol);
                    sideEffectsBuilder.Add(startStore);
                    startExpr = startLocal;
                }

                rangeSizeExpr = F.IntSubtract(endExpr, startExpr);
            }

            return rangeSizeExpr;
        }

        private void DeconstructRange(BoundExpression rewrittenRangeArg, BoundExpression lengthAccess, ArrayBuilder<LocalSymbol> localsBuilder, ArrayBuilder<BoundExpression> sideEffectsBuilder, out BoundExpression startExpr, out BoundExpression rangeSizeExpr)
        {
            var F = _factory;

            var rangeLocal = F.StoreToTemp(rewrittenRangeArg, out var rangeStore);
            localsBuilder.Add(rangeLocal.LocalSymbol);
            sideEffectsBuilder.Add(rangeStore);

            if (lengthAccess.ConstantValueOpt is null)
            {
                var lengthLocal = F.StoreToTemp(lengthAccess, out var lengthStore);
                localsBuilder.Add(lengthLocal.LocalSymbol);
                sideEffectsBuilder.Add(lengthStore);
                lengthAccess = lengthLocal;
            }

            var startLocal = F.StoreToTemp(
                F.Call(
                    F.Call(rangeLocal, F.WellKnownMethod(WellKnownMember.System_Range__get_Start)),
                    F.WellKnownMethod(WellKnownMember.System_Index__GetOffset),
                    lengthAccess),
                out var startStore);

            localsBuilder.Add(startLocal.LocalSymbol);
            sideEffectsBuilder.Add(startStore);
            startExpr = startLocal;

            var rangeSizeLocal = F.StoreToTemp(
                F.IntSubtract(
                    F.Call(
                        F.Call(rangeLocal, F.WellKnownMethod(WellKnownMember.System_Range__get_End)),
                        F.WellKnownMethod(WellKnownMember.System_Index__GetOffset),
                        lengthAccess),
                    startExpr),
                out var rangeSizeStore);

            localsBuilder.Add(rangeSizeLocal.LocalSymbol);
            sideEffectsBuilder.Add(rangeSizeStore);
            rangeSizeExpr = rangeSizeLocal;
        }

        private void RewriteRangeParts(BoundExpression rangeArg, out BoundRangeExpression? rangeExpr, out BoundExpression? startMakeOffsetInput, out PatternIndexOffsetLoweringStrategy startStrategy, out BoundExpression? endMakeOffsetInput, out PatternIndexOffsetLoweringStrategy endStrategy, out BoundExpression? rewrittenRangeArg)
        {
            startMakeOffsetInput = null;
            startStrategy = default;
            endMakeOffsetInput = null;
            endStrategy = default;
            rewrittenRangeArg = null;
            rangeExpr = rangeArg as BoundRangeExpression;

            if (rangeExpr is not null)
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

                if (rangeExpr.LeftOperandOpt is BoundExpression left)
                {
                    startMakeOffsetInput = DetermineMakePatternIndexOffsetExpressionStrategy(left, out startStrategy);
                }
                else
                {
                    startStrategy = PatternIndexOffsetLoweringStrategy.Zero;
                    startMakeOffsetInput = null;
                }

                if (rangeExpr.RightOperandOpt is BoundExpression right)
                {
                    endMakeOffsetInput = DetermineMakePatternIndexOffsetExpressionStrategy(right, out endStrategy);
                }
                else
                {
                    endStrategy = PatternIndexOffsetLoweringStrategy.Length;
                    endMakeOffsetInput = null;
                }
            }
            else
            {
                rewrittenRangeArg = VisitExpression(rangeArg);
            }
        }
    }
}
