// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    // Shared code for rewriting Object and Collection initializer expressions

    internal sealed partial class LocalRewriter
    {
        private static BoundObjectInitializerExpressionBase UpdateInitializers(BoundObjectInitializerExpressionBase initializerExpression, ImmutableArray<BoundExpression> newInitializers)
        {
            switch (initializerExpression)
            {
                case BoundObjectInitializerExpression objectInitializer:
                    return objectInitializer.Update(objectInitializer.Placeholder, newInitializers, initializerExpression.Type);
                case BoundCollectionInitializerExpression collectionInitializer:
                    return collectionInitializer.Update(collectionInitializer.Placeholder, newInitializers, initializerExpression.Type);
                default:
                    throw ExceptionUtilities.UnexpectedValue(initializerExpression.Kind);
            }
        }

        private void AddObjectOrCollectionInitializers(
            ref ArrayBuilder<BoundExpression>? dynamicSiteInitializers,
            ref ArrayBuilder<LocalSymbol>? temps,
            ArrayBuilder<BoundExpression> result,
            BoundExpression rewrittenReceiver,
            BoundExpression initializerExpression)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(rewrittenReceiver != null);

            switch (initializerExpression)
            {
                case BoundObjectInitializerExpression objectInitializer:
                    {
                        var placeholder = objectInitializer.Placeholder;
                        AddPlaceholderReplacement(placeholder, rewrittenReceiver);
                        AddObjectInitializers(ref dynamicSiteInitializers, ref temps, result, rewrittenReceiver, objectInitializer.Initializers);
                        RemovePlaceholderReplacement(placeholder);
                    }
                    return;

                case BoundCollectionInitializerExpression collectionInitializer:
                    {
                        var placeholder = collectionInitializer.Placeholder;
                        AddPlaceholderReplacement(placeholder, rewrittenReceiver);
                        AddCollectionInitializers(result, rewrittenReceiver, collectionInitializer.Initializers);
                        RemovePlaceholderReplacement(placeholder);
                    }
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(initializerExpression.Kind);
            }
        }

        private ImmutableArray<BoundExpression> MakeObjectOrCollectionInitializersForExpressionTree(BoundExpression initializerExpression)
        {
            Debug.Assert(_inExpressionLambda);

            switch (initializerExpression.Kind)
            {
                case BoundKind.ObjectInitializerExpression:
                    return VisitList(((BoundObjectInitializerExpression)initializerExpression).Initializers);

                case BoundKind.CollectionInitializerExpression:
                    var result = ArrayBuilder<BoundExpression>.GetInstance();
                    addCollectionInitializersForExpressionTree(result, ((BoundCollectionInitializerExpression)initializerExpression).Initializers);
                    return result.ToImmutableAndFree();

                default:
                    throw ExceptionUtilities.UnexpectedValue(initializerExpression.Kind);
            }

            void addCollectionInitializersForExpressionTree(ArrayBuilder<BoundExpression> result, ImmutableArray<BoundExpression> initializers)
            {
                foreach (var initializer in initializers)
                {
                    // In general bound initializers may contain bad expressions or element initializers.
                    // We don't lower them if they contain errors, so it's safe to assume an element initializer.

                    if (initializer.Kind != BoundKind.CollectionElementInitializer)
                    {
                        throw ExceptionUtilities.UnexpectedValue(initializer.Kind);
                    }

                    var elementInitializer = (BoundCollectionElementInitializer)initializer;

                    // NOTE: Calls cannot be omitted within an expression tree (CS0765); this should already
                    // have been checked.
                    Debug.Assert(!elementInitializer.AddMethod.CallsAreOmitted(initializer.SyntaxTree));

                    Debug.Assert(!elementInitializer.InvokedAsExtensionMethod);
                    Debug.Assert(!elementInitializer.AddMethod.IsExtensionMethod);
                    Debug.Assert(!elementInitializer.AddMethod.IsExtensionBlockMember());
                    Debug.Assert(elementInitializer.Arguments.Length == elementInitializer.AddMethod.ParameterCount);
                    Debug.Assert(elementInitializer.ImplicitReceiverOpt is BoundObjectOrCollectionValuePlaceholder);

                    result.Add(
                        VisitExpression(
                            elementInitializer.Update(
                                elementInitializer.AddMethod,
                                elementInitializer.Arguments,
                                implicitReceiverOpt: null,
                                elementInitializer.Expanded,
                                elementInitializer.ArgsToParamsOpt,
                                elementInitializer.DefaultArguments,
                                elementInitializer.InvokedAsExtensionMethod,
                                elementInitializer.ResultKind,
                                elementInitializer.Type)));
                }
            }
        }

        // Rewrite collection initializer add method calls:
        // 2) new List<int> { 1 };
        //                    ~
        private void AddCollectionInitializers(ArrayBuilder<BoundExpression> result, BoundExpression rewrittenReceiver, ImmutableArray<BoundExpression> initializers)
        {
            Debug.Assert(rewrittenReceiver is { } || _inExpressionLambda);

            foreach (var initializer in initializers)
            {
                // In general bound initializers may contain bad expressions or element initializers.
                // We don't lower them if they contain errors, so it's safe to assume an element initializer.

                BoundExpression? rewrittenInitializer;
                if (initializer.Kind == BoundKind.CollectionElementInitializer)
                {
                    rewrittenInitializer = MakeCollectionInitializer((BoundCollectionElementInitializer)initializer);
                }
                else
                {
                    Debug.Assert(!_inExpressionLambda);
                    Debug.Assert(initializer.Kind == BoundKind.DynamicCollectionElementInitializer);

                    rewrittenInitializer = MakeDynamicCollectionInitializer(rewrittenReceiver!, (BoundDynamicCollectionElementInitializer)initializer);
                }

                // the call to Add may be omitted
                if (rewrittenInitializer != null)
                {
                    result.Add(rewrittenInitializer);
                }
            }
        }

        private BoundExpression MakeDynamicCollectionInitializer(BoundExpression rewrittenReceiver, BoundDynamicCollectionElementInitializer initializer)
        {
            var rewrittenArguments = VisitList(initializer.Arguments);

            // If we are calling a method on a NoPIA type, we need to embed all methods/properties
            // with the matching name of this dynamic invocation.
            EmbedIfNeedTo(rewrittenReceiver, initializer.ApplicableMethods, initializer.Syntax);

            return _dynamicFactory.MakeDynamicMemberInvocation(
                WellKnownMemberNames.CollectionInitializerAddMethodName,
                rewrittenReceiver,
                ImmutableArray<TypeWithAnnotations>.Empty,
                rewrittenArguments,
                default(ImmutableArray<string?>),
                default(ImmutableArray<RefKind>),
                hasImplicitReceiver: false,
                resultDiscarded: true).ToExpression();
        }

        // Rewrite collection initializer element Add method call:
        //  new List<int> { 1, 2, 3 };  OR  new List<int> { { 1, 2 }, 3 }; OR [1, 2, 3]
        //                  ~                               ~~~~~~~~
        private BoundExpression? MakeCollectionInitializer(BoundCollectionElementInitializer initializer)
        {
            MethodSymbol addMethod = initializer.AddMethod;

            Debug.Assert(addMethod.Name == "Add");
            Debug.Assert(addMethod.Parameters
                .Skip(addMethod.IsExtensionMethod ? 1 : 0)
                .All(p => p.RefKind is RefKind.None or RefKind.In or RefKind.RefReadOnlyParameter));
            Debug.Assert(initializer.Arguments.Any());
            Debug.Assert(!_inExpressionLambda);

            var syntax = initializer.Syntax;

            if (_allowOmissionOfConditionalCalls)
            {
                if (addMethod.CallsAreOmitted(initializer.SyntaxTree))
                {
                    return null;
                }
            }

            BoundExpression? rewrittenReceiver = VisitExpression(initializer.ImplicitReceiverOpt);

            var argumentRefKindsOpt = default(ImmutableArray<RefKind>);
            if (initializer.InvokedAsExtensionMethod && addMethod.Parameters[0].RefKind == RefKind.Ref)
            {
                // If the Add method is an extension which takes a `ref this` as the first parameter, implicitly add a `ref` to the argument
                // Initializer element syntax cannot have `ref`, `in`, or `out` keywords.
                // Arguments to `in` parameters will be converted to have RefKind.In later on.
                var builder = ArrayBuilder<RefKind>.GetInstance(addMethod.Parameters.Length, RefKind.None);
                builder[0] = RefKind.Ref;
                argumentRefKindsOpt = builder.ToImmutableAndFree();
            }

            // The receiver for a collection initializer is already a temp, so we don't need to preserve any additional temp stores beyond this method.
            ArrayBuilder<LocalSymbol>? temps = null;
            ImmutableArray<BoundExpression> rewrittenArguments = VisitArgumentsAndCaptureReceiverIfNeeded(
                ref rewrittenReceiver,
                forceReceiverCapturing: false,
                initializer.Arguments,
                addMethod,
                initializer.ArgsToParamsOpt,
                argumentRefKindsOpt,
                storesOpt: null,
                ref temps);
            rewrittenArguments = MakeArguments(rewrittenArguments, addMethod, initializer.Expanded, initializer.ArgsToParamsOpt, ref argumentRefKindsOpt, ref temps);

            var rewrittenType = VisitType(initializer.Type);

#if DEBUG
            if (initializer.InvokedAsExtensionMethod)
            {
                Debug.Assert(addMethod.IsStatic);
                Debug.Assert(addMethod.IsExtensionMethod);
                Debug.Assert(rewrittenReceiver is null);
            }
#endif

            if (Instrument)
            {
                Instrumenter.InterceptCallAndAdjustArguments(ref addMethod, ref rewrittenReceiver, ref rewrittenArguments, ref argumentRefKindsOpt);
            }

            return MakeCall(null, syntax, rewrittenReceiver, addMethod, rewrittenArguments, argumentRefKindsOpt, initializer.ResultKind, temps.ToImmutableAndFree());
        }

        private BoundExpression VisitObjectInitializerMember(BoundObjectInitializerMember node, ref BoundExpression rewrittenReceiver, ArrayBuilder<BoundExpression> sideEffects, ref ArrayBuilder<LocalSymbol>? temps)
        {
            if (node.MemberSymbol is null)
            {
                return (BoundExpression)base.VisitObjectInitializerMember(node)!;
            }

            var originalReceiver = rewrittenReceiver;
            ArrayBuilder<LocalSymbol>? constructionTemps = null;
            var rewrittenArguments = VisitArgumentsAndCaptureReceiverIfNeeded(ref rewrittenReceiver, forceReceiverCapturing: false, node.Arguments, node.MemberSymbol, node.ArgsToParamsOpt, node.ArgumentRefKindsOpt,
                storesOpt: null, ref constructionTemps);

            if (constructionTemps != null)
            {
                if (temps == null)
                {
                    temps = constructionTemps;
                }
                else
                {
                    temps.AddRange(constructionTemps);
                    constructionTemps.Free();
                }
            }

            if (originalReceiver != rewrittenReceiver && rewrittenReceiver is BoundSequence sequence)
            {
                Debug.Assert(temps != null);
                temps.AddRange(sequence.Locals);
                sideEffects.AddRange(sequence.SideEffects);
                rewrittenReceiver = sequence.Value;
            }

            return node.Update(node.MemberSymbol, rewrittenArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.DefaultArguments, node.ResultKind, node.AccessorKind, node.UnderlyingAccess, node.ReceiverType, node.Type);
        }

        // Rewrite object initializer member assignments and add them to the result.
        private void AddObjectInitializers(
            ref ArrayBuilder<BoundExpression>? dynamicSiteInitializers,
            ref ArrayBuilder<LocalSymbol>? temps,
            ArrayBuilder<BoundExpression> result,
            BoundExpression rewrittenReceiver,
            ImmutableArray<BoundExpression> initializers)
        {
            Debug.Assert(!_inExpressionLambda);

            foreach (var initializer in initializers)
            {
                // Bound initializers may be simple assignments (`Prop = v`), compound assignments
                // (`Prop += v`), null-coalescing assignments (`Prop ??= v`), or event assignments
                // (`E += h`). We don't lower them if they contain errors, so below we assume
                // well-formed shapes.
                switch (initializer.Kind)
                {
                    case BoundKind.AssignmentOperator:
                        AddObjectInitializer(ref dynamicSiteInitializers, ref temps, result, rewrittenReceiver, (BoundAssignmentOperator)initializer);
                        break;
                    case BoundKind.CompoundAssignmentOperator:
                    case BoundKind.NullCoalescingAssignmentOperator:
                        AddCompoundOrCoalesceObjectInitializer(ref temps, result, rewrittenReceiver, initializer);
                        break;
                    case BoundKind.EventAssignmentOperator:
                        AddEventObjectInitializer(result, rewrittenReceiver, (BoundEventAssignmentOperator)initializer);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(initializer.Kind);
                }
            }
        }

        /// <summary>
        /// Lowers a compound (`Prop += v` / `Prop |= v` / etc.) or null-coalescing (`Prop ??= v`)
        /// member initializer on an object initializer by substituting the placeholder receiver on
        /// the target access with the real <paramref name="rewrittenReceiver"/>, then handing the
        /// rebuilt assignment op to the corresponding general lowering pipeline
        /// (<see cref="VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator, bool)"/> or
        /// <see cref="VisitNullCoalescingAssignmentOperator(BoundNullCoalescingAssignmentOperator)"/>)
        /// to emit the read-op-write / get-if-null-then-set sequence. Accepts every Left shape that
        /// <c>BindObjectInitializerMemberCommon</c> can produce for a non-dynamic target: the
        /// <see cref="BoundObjectInitializerMember"/> wrapper (field / property / indexer / event)
        /// plus the three bare accesses it hands back directly without a wrapper
        /// (<see cref="BoundImplicitIndexerAccess"/> for <c>Index</c>/<c>Range</c>-pattern targets,
        /// <see cref="BoundArrayAccess"/>, <see cref="BoundPointerElementAccess"/>).
        /// </summary>
        private void AddCompoundOrCoalesceObjectInitializer(
            ref ArrayBuilder<LocalSymbol>? temps,
            ArrayBuilder<BoundExpression> result,
            BoundExpression rewrittenReceiver,
            BoundExpression initializer)
        {
            Debug.Assert(rewrittenReceiver != null);
            Debug.Assert(!_inExpressionLambda);

            // Build the concrete member access with the real receiver, matching the simple-assignment
            // path. Then rebuild the assignment op with that access as Left; the type matches, so
            // conversions and the operator signature remain valid. The Right is still in unlowered
            // form so the visitor visits it during lowering.
            //
            // used: false on the compound arm — in an object initializer each member initializer is a
            // statement-expression whose value is discarded, so VisitCompoundAssignmentOperator can
            // skip emitting the final dup/stloc that would preserve the RHS value on the stack. The
            // simple-assignment path calls MakeStaticAssignmentOperator with used: false for the same
            // reason; VisitNullCoalescingAssignmentOperator doesn't take a `used` flag.
            BoundExpression lowered = initializer switch
            {
                BoundCompoundAssignmentOperator compound
                    => VisitCompoundAssignmentOperator(
                        compound.Update(
                            compound.Operator,
                            RewriteInitializerMemberLeftOperand(compound.Left, ref rewrittenReceiver, result, ref temps),
                            compound.Right,
                            compound.LeftPlaceholder,
                            compound.LeftConversion,
                            compound.FinalPlaceholder,
                            compound.FinalConversion,
                            compound.ResultKind,
                            compound.OriginalUserDefinedOperatorsOpt,
                            compound.Type),
                        used: false),
                BoundNullCoalescingAssignmentOperator coalesce
                    => (BoundExpression)VisitNullCoalescingAssignmentOperator(
                        coalesce.Update(
                            RewriteInitializerMemberLeftOperand(coalesce.LeftOperand, ref rewrittenReceiver, result, ref temps),
                            coalesce.RightOperand,
                            coalesce.Type)),
                _ => throw ExceptionUtilities.UnexpectedValue(initializer.Kind),
            };

            result.Add(lowered);
        }

        /// <summary>
        /// Shared dispatch for both the compound-assignment and null-coalescing-assignment initializer
        /// paths. Takes a bound <paramref name="left"/> produced by <c>BindObjectInitializerMemberCommon</c>
        /// — which may be a <see cref="BoundObjectInitializerMember"/> wrapper, one of the bare accesses
        /// (<see cref="BoundImplicitIndexerAccess"/>, <see cref="BoundArrayAccess"/>,
        /// <see cref="BoundPointerElementAccess"/>), or a <see cref="BoundDynamicObjectInitializerMember"/>
        /// — and returns the concrete access with the real <paramref name="rewrittenReceiver"/> in place
        /// of the object-initializer placeholder. Side-effecting indexer arguments are lifted to temps
        /// via <paramref name="result"/> / <paramref name="temps"/> along the way.
        /// </summary>
        private BoundExpression RewriteInitializerMemberLeftOperand(
            BoundExpression left,
            ref BoundExpression rewrittenReceiver,
            ArrayBuilder<BoundExpression> result,
            ref ArrayBuilder<LocalSymbol>? temps)
        {
            // The dynamic-indexer sub-case of BoundObjectInitializerMember (`MemberSymbol == null &&
            // Type.IsDynamic()`, carrying a BoundDynamicIndexerAccess in UnderlyingAccess) can't go
            // through MakeObjectInitializerMemberAccess — it asserts a non-null member — so we
            // unwrap to the dynamic indexer and let TransformDynamicIndexerAccess handle the get/set
            // call-site pair, mirroring the non-initializer `d[0] += 1` path.
            return left switch
            {
                BoundObjectInitializerMember { MemberSymbol: null } w when w.Type.IsDynamic() => RewriteDynamicIndexerInitializerAccess(w, ref rewrittenReceiver, result, ref temps),
                BoundObjectInitializerMember w => RewriteObjectInitializerMemberAccess(w, ref rewrittenReceiver, result, ref temps, isRhsNestedInitializer: false),
                BoundImplicitIndexerAccess i => RewriteImplicitIndexerInitializerAccess(i, result, ref temps),
                BoundArrayAccess a => RewriteArrayInitializerAccess(a, rewrittenReceiver, result, ref temps),
                BoundPointerElementAccess p => RewritePointerElementInitializerAccess(p, rewrittenReceiver, ref temps, result),
                BoundDynamicObjectInitializerMember d => RewriteDynamicObjectInitializerMemberAccess(d, rewrittenReceiver),
                _ => throw ExceptionUtilities.UnexpectedValue(left.Kind),
            };
        }

        /// <summary>
        /// Normalizes a <see cref="BoundImplicitIndexerAccess"/> initializer-member target (e.g.
        /// <c>[^1]</c> / <c>[..n]</c> on a type with <c>int Length</c> + indexer) to the concrete
        /// <see cref="BoundIndexerAccess"/> (Index) or <c>GetSubArray</c>-style <see cref="BoundCall"/>
        /// (Range) that compound lowering operates on. Mirrors the shape built inline by the
        /// simple-assignment path in <see cref="AddObjectInitializer"/>.
        /// </summary>
        private BoundExpression RewriteImplicitIndexerInitializerAccess(
            BoundImplicitIndexerAccess implicitIndexer,
            ArrayBuilder<BoundExpression> result,
            ref ArrayBuilder<LocalSymbol>? temps)
        {
            temps ??= ArrayBuilder<LocalSymbol>.GetInstance();

            if (TypeSymbol.Equals(implicitIndexer.Argument.Type, _compilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.ConsiderEverything))
            {
                var rewritten = VisitIndexPatternIndexerAccess(
                    implicitIndexer,
                    isLeftOfAssignment: true,
                    isRegularAssignment: true,
                    cacheAllArgumentsOnly: true,
                    result, temps,
                    receiverIsKnownToBeCaptured: out _);

                if (rewritten is BoundIndexerAccess indexerAccess)
                {
                    rewritten = TransformIndexerAccessContinued(indexerAccess, indexerAccess.ReceiverOpt!, indexerAccess.Arguments, result, temps);
                }

                return rewritten;
            }

            return VisitRangePatternIndexerAccess(implicitIndexer, temps, result, cacheAllArgumentsOnly: true);
        }

        /// <summary>
        /// Normalizes a dynamic-indexer initializer-member target — a <see cref="BoundObjectInitializerMember"/>
        /// with <c>MemberSymbol == null</c> and a dynamic Type, carrying its originating
        /// <see cref="BoundDynamicIndexerAccess"/> in <see cref="BoundObjectInitializerMember.UnderlyingAccess"/> —
        /// into a <see cref="BoundDynamicIndexerAccess"/> with the real receiver, so the compound
        /// lowering pipeline (<c>VisitCompoundAssignmentOperator</c> → <c>TransformDynamicIndexerAccess</c>)
        /// can emit the runtime GetIndex/SetIndex call-site pair, matching the non-initializer
        /// <c>d[0] += 1</c> path.
        /// </summary>
        private BoundExpression RewriteDynamicIndexerInitializerAccess(
            BoundObjectInitializerMember wrapper,
            ref BoundExpression rewrittenReceiver,
            ArrayBuilder<BoundExpression> result,
            ref ArrayBuilder<LocalSymbol>? temps)
        {
            var originalIndexer = (BoundDynamicIndexerAccess)wrapper.UnderlyingAccess;
            var visitedWrapper = (BoundObjectInitializerMember)VisitObjectInitializerMember(wrapper, ref rewrittenReceiver, result, ref temps);

            var liftedArgs = visitedWrapper.Arguments.IsDefaultOrEmpty
                ? visitedWrapper.Arguments
                : EvaluateSideEffectingArgumentsToTemps(visitedWrapper.Arguments, paramRefKindsOpt: default, result, ref temps);

            return originalIndexer.Update(
                rewrittenReceiver,
                liftedArgs,
                visitedWrapper.ArgumentNamesOpt,
                visitedWrapper.ArgumentRefKindsOpt,
                originalIndexer.ApplicableIndexers,
                originalIndexer.Type);
        }

        /// <summary>
        /// Normalizes a <see cref="BoundArrayAccess"/> initializer-member target to a concrete array
        /// element access with the real receiver, lifting side-effecting index arguments into temps.
        /// Mirrors the shape built inline by the simple-assignment path.
        /// </summary>
        private BoundExpression RewriteArrayInitializerAccess(
            BoundArrayAccess arrayAccess,
            BoundExpression rewrittenReceiver,
            ArrayBuilder<BoundExpression> result,
            ref ArrayBuilder<LocalSymbol>? temps)
        {
            Debug.Assert(!arrayAccess.Indices.Any(a => a.IsParamsArrayOrCollection));

            var indices = EvaluateSideEffectingArgumentsToTemps(
                arrayAccess.Indices,
                paramRefKindsOpt: default,
                result,
                ref temps);

            return arrayAccess.Update(rewrittenReceiver, indices, arrayAccess.Type);
        }

        /// <summary>
        /// Converts a <see cref="BoundDynamicObjectInitializerMember"/> compound target into a
        /// <see cref="BoundDynamicMemberAccess"/> with the real <paramref name="rewrittenReceiver"/>.
        /// This lets the general compound-assignment lowering pipeline's dynamic path
        /// (<c>VisitCompoundAssignmentOperator</c> → <c>TransformDynamicMemberAccess</c>) take over and
        /// emit the get/set runtime call-site pair, exactly as it does for the non-initializer
        /// <c>dyn.X += 1</c> shape.
        /// </summary>
        private static BoundExpression RewriteDynamicObjectInitializerMemberAccess(
            BoundDynamicObjectInitializerMember member,
            BoundExpression rewrittenReceiver)
        {
            return new BoundDynamicMemberAccess(
                member.Syntax,
                rewrittenReceiver,
                typeArgumentsOpt: default,
                member.MemberName,
                invoked: false,
                indexed: false,
                member.Type);
        }

        /// <summary>
        /// Normalizes a <see cref="BoundPointerElementAccess"/> initializer-member target with the real
        /// receiver, lifting a side-effecting index expression into a temp. Mirrors the shape built
        /// inline by the simple-assignment path.
        /// </summary>
        private BoundExpression RewritePointerElementInitializerAccess(
            BoundPointerElementAccess pointerAccess,
            BoundExpression rewrittenReceiver,
            ref ArrayBuilder<LocalSymbol>? temps,
            ArrayBuilder<BoundExpression> result)
        {
            var rewrittenIndex = VisitExpression(pointerAccess.Index);

            if (CanChangeValueBetweenReads(rewrittenIndex))
            {
                var temp = _factory.StoreToTemp(rewrittenIndex, out BoundAssignmentOperator store);
                rewrittenIndex = temp;
                temps ??= ArrayBuilder<LocalSymbol>.GetInstance();
                temps.Add(temp.LocalSymbol);
                result.Add(store);
            }

            return RewritePointerElementAccess(pointerAccess, rewrittenReceiver, rewrittenIndex);
        }

        /// <summary>
        /// Normalizes a <see cref="BoundObjectInitializerMember"/> wrapper from an initializer-member
        /// target to the concrete <see cref="BoundFieldAccess"/> / <see cref="BoundPropertyAccess"/> /
        /// <see cref="BoundIndexerAccess"/> / <see cref="BoundEventAccess"/> that lowering operates on:
        /// substitutes the object-initializer placeholder receiver with the real
        /// <paramref name="rewrittenReceiver"/> (via <see cref="VisitObjectInitializerMember"/>), lifts
        /// any side-effecting indexer arguments into temps, and hands the result to
        /// <see cref="MakeObjectInitializerMemberAccess"/>. Shared between the simple-assignment and
        /// compound-assignment paths.
        /// </summary>
        private BoundExpression RewriteObjectInitializerMemberAccess(
            BoundObjectInitializerMember memberInit,
            ref BoundExpression rewrittenReceiver,
            ArrayBuilder<BoundExpression> result,
            ref ArrayBuilder<LocalSymbol>? temps,
            bool isRhsNestedInitializer)
        {
            memberInit = NormalizeObjectInitializerMember(memberInit, ref rewrittenReceiver, result, ref temps);
            return MakeObjectInitializerMemberAccess(rewrittenReceiver, memberInit, isRhsNestedInitializer);
        }

        /// <summary>
        /// Visits the wrapper's arguments, substitutes the object-initializer placeholder receiver
        /// with <paramref name="rewrittenReceiver"/>, and lifts any side-effecting indexer arguments
        /// into temps so a compound read-modify-write (or a dynamic get/set pair) evaluates them
        /// exactly once. Shared between <see cref="RewriteObjectInitializerMemberAccess"/> and the
        /// dynamic-indexer simple-assignment branch in <see cref="AddObjectInitializer"/>; the
        /// caller then decides whether to hand the normalized wrapper to
        /// <see cref="MakeObjectInitializerMemberAccess"/> or to the DLR `MakeDynamicGet/SetIndex`
        /// factories.
        /// </summary>
        private BoundObjectInitializerMember NormalizeObjectInitializerMember(
            BoundObjectInitializerMember memberInit,
            ref BoundExpression rewrittenReceiver,
            ArrayBuilder<BoundExpression> result,
            ref ArrayBuilder<LocalSymbol>? temps)
        {
            memberInit = (BoundObjectInitializerMember)VisitObjectInitializerMember(
                memberInit, ref rewrittenReceiver, result, ref temps);

            Debug.Assert(memberInit is { });

            if (!memberInit.Arguments.IsDefaultOrEmpty)
            {
                Debug.Assert(memberInit.Arguments.Count(a => a.IsParamsArrayOrCollection) <= (memberInit.Expanded ? 1 : 0));

                var args = EvaluateSideEffectingArgumentsToTemps(
                    memberInit.Arguments,
                    memberInit.MemberSymbol?.GetParameterRefKinds() ?? default(ImmutableArray<RefKind>),
                    result,
                    ref temps);

                memberInit = memberInit.Update(
                    memberInit.MemberSymbol,
                    args,
                    memberInit.ArgumentNamesOpt,
                    memberInit.ArgumentRefKindsOpt,
                    memberInit.Expanded,
                    memberInit.ArgsToParamsOpt,
                    memberInit.DefaultArguments,
                    memberInit.ResultKind,
                    memberInit.AccessorKind,
                    memberInit.UnderlyingAccess,
                    memberInit.ReceiverType,
                    memberInit.Type);
            }

            return memberInit;
        }

        /// <summary>
        /// Lowers an event member initializer (`E += handler` / `E -= handler`) on an object
        /// initializer by substituting the placeholder receiver on <see cref="BoundEventAssignmentOperator.ReceiverOpt"/>
        /// with the real <paramref name="rewrittenReceiver"/>, then handing the result to
        /// <see cref="VisitEventAssignmentOperator(BoundEventAssignmentOperator)"/> to emit the
        /// add_/remove_ accessor call.
        /// </summary>
        private void AddEventObjectInitializer(
            ArrayBuilder<BoundExpression> result,
            BoundExpression rewrittenReceiver,
            BoundEventAssignmentOperator eventAssign)
        {
            Debug.Assert(rewrittenReceiver != null);
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(eventAssign.ReceiverOpt is BoundObjectOrCollectionValuePlaceholder, "Event compound initializer's receiver should be the object-initializer placeholder.");

            var transformed = eventAssign.Update(
                eventAssign.Event,
                eventAssign.IsAddition,
                eventAssign.IsDynamic,
                rewrittenReceiver,
                eventAssign.Argument,
                eventAssign.Type);

            result.Add((BoundExpression)VisitEventAssignmentOperator(transformed));
        }

        // Rewrite object initializer member assignment and add it to the result.
        //  new SomeType { Member = 0 };
        //                 ~~~~~~~~~~
        private void AddObjectInitializer(
            ref ArrayBuilder<BoundExpression>? dynamicSiteInitializers,
            ref ArrayBuilder<LocalSymbol>? temps,
            ArrayBuilder<BoundExpression> result,
            BoundExpression rewrittenReceiver,
            BoundAssignmentOperator assignment)
        {
            Debug.Assert(rewrittenReceiver != null);
            Debug.Assert(!_inExpressionLambda);

            BoundExpression left = assignment.Left;
            BoundExpression right = assignment.Right;
            bool isRhsNestedInitializer = right.Kind is BoundKind.ObjectInitializerExpression or BoundKind.CollectionInitializerExpression;

            if (isRhsNestedInitializer && onlyContainsEmptyLeafNestedInitializers(assignment))
            {
                // If we only have nested object initializers and the leaves are empty initializers,
                // then we optimize, skip calling the indexers and properties in the chain, and only evaluate the indexes
                addIndexes(result, assignment);
                return;
            }

            BoundExpression rewrittenAccess;
            switch (left.Kind)
            {
                case BoundKind.ObjectInitializerMember:
                    {
                        // Receiver + arg normalization is identical on both branches; extract it and
                        // then dispatch on the dynamic-indexer shape. The dynamic path
                        // (`MemberSymbol == null && Type.IsDynamic()`) routes through the DLR
                        // `MakeDynamicSet/GetIndex` factories and plumbs the hoisted SiteInitialization
                        // into `dynamicSiteInitializers`; the non-dynamic path builds a concrete
                        // field/property/indexer access via MakeObjectInitializerMemberAccess.
                        var memberInit = NormalizeObjectInitializerMember(
                            (BoundObjectInitializerMember)left, ref rewrittenReceiver, result, ref temps);

                        if (memberInit.MemberSymbol == null && memberInit.Type.IsDynamic())
                        {
                            Debug.Assert(!memberInit.Expanded);
                            dynamicSiteInitializers ??= ArrayBuilder<BoundExpression>.GetInstance();

                            if (!isRhsNestedInitializer)
                            {
                                var rewrittenRight = VisitExpression(right);
                                var setMember = _dynamicFactory.MakeDynamicSetIndex(
                                    rewrittenReceiver,
                                    memberInit.Arguments,
                                    memberInit.ArgumentNamesOpt,
                                    memberInit.ArgumentRefKindsOpt,
                                    rewrittenRight);

                                Debug.Assert(setMember.SiteInitialization is { });
                                dynamicSiteInitializers.Add(setMember.SiteInitialization);
                                result.Add(setMember.SiteInvocation);
                                return;
                            }

                            var getMember = _dynamicFactory.MakeDynamicGetIndex(
                                rewrittenReceiver,
                                memberInit.Arguments,
                                memberInit.ArgumentNamesOpt,
                                memberInit.ArgumentRefKindsOpt);

                            Debug.Assert(getMember.SiteInitialization is { });
                            dynamicSiteInitializers.Add(getMember.SiteInitialization);
                            rewrittenAccess = getMember.SiteInvocation;
                        }
                        else
                        {
                            rewrittenAccess = MakeObjectInitializerMemberAccess(rewrittenReceiver, memberInit, isRhsNestedInitializer);

                            if (!isRhsNestedInitializer)
                            {
                                // Rewrite simple assignment to field/property.
                                var rewrittenRight = VisitExpression(right);
                                Debug.Assert(assignment.Type.IsDynamic() || TypeSymbol.Equals(rewrittenAccess.Type, assignment.Type, TypeCompareKind.AllIgnoreOptions));
                                result.Add(MakeStaticAssignmentOperator(assignment.Syntax, rewrittenAccess, rewrittenRight, isRef: assignment.IsRef, used: false, AssignmentKind.SimpleAssignment, receiverIsKnownToBeCaptured: true));
                                return;
                            }
                        }
                        break;
                    }

                case BoundKind.DynamicObjectInitializerMember:
                    {
                        var initializerMember = (BoundDynamicObjectInitializerMember?)VisitDynamicObjectInitializerMember((BoundDynamicObjectInitializerMember)left);
                        Debug.Assert(initializerMember is { });
                        if (dynamicSiteInitializers == null)
                        {
                            dynamicSiteInitializers = ArrayBuilder<BoundExpression>.GetInstance();
                        }

                        if (!isRhsNestedInitializer)
                        {
                            var rewrittenRight = VisitExpression(right);
                            var setMember = _dynamicFactory.MakeDynamicSetMember(rewrittenReceiver, initializerMember.MemberName, rewrittenRight);
                            Debug.Assert(setMember.SiteInitialization is { });
                            dynamicSiteInitializers.Add(setMember.SiteInitialization);
                            result.Add(setMember.SiteInvocation);
                            return;
                        }

                        var getMember = _dynamicFactory.MakeDynamicGetMember(rewrittenReceiver, initializerMember.MemberName, resultIndexed: false);
                        Debug.Assert(getMember.SiteInitialization is { });
                        dynamicSiteInitializers.Add(getMember.SiteInitialization);
                        rewrittenAccess = getMember.SiteInvocation;
                        break;
                    }

                case BoundKind.ArrayAccess:
                    {
                        var rewrittenArrayAccess = VisitArrayAccess((BoundArrayAccess)left);
                        Debug.Assert(rewrittenArrayAccess is { });

                        if (rewrittenArrayAccess is BoundArrayAccess arrayAccess)
                        {
                            Debug.Assert(!arrayAccess.Indices.Any(a => a.IsParamsArrayOrCollection));

                            var indices = EvaluateSideEffectingArgumentsToTemps(
                                arrayAccess.Indices,
                                paramRefKindsOpt: default,
                                result,
                                ref temps);
                            rewrittenAccess = arrayAccess.Update(rewrittenReceiver, indices, arrayAccess.Type);
                        }
                        else if (rewrittenArrayAccess is BoundCall getSubArrayCall)
                        {
                            Debug.Assert(getSubArrayCall.Arguments.Length == 2);
                            var rangeArgument = getSubArrayCall.Arguments[1];
                            Debug.Assert(Binder.IsWellKnownSystemRange(rangeArgument.Type, _compilation));

                            var rangeTemp = _factory.StoreToTemp(rangeArgument, out BoundAssignmentOperator rangeStore);
                            temps ??= ArrayBuilder<LocalSymbol>.GetInstance();
                            temps.Add(rangeTemp.LocalSymbol);
                            result.Add(rangeStore);

                            rewrittenAccess = getSubArrayCall.Update(ImmutableArray.Create(getSubArrayCall.Arguments[0], rangeTemp));
                        }
                        else
                        {
                            throw ExceptionUtilities.UnexpectedValue(rewrittenArrayAccess.Kind);
                        }

                        if (!isRhsNestedInitializer)
                        {
                            // Rewrite simple assignment to field/property.
                            var rewrittenRight = VisitExpression(right);
                            Debug.Assert(TypeSymbol.Equals(rewrittenAccess.Type, assignment.Type, TypeCompareKind.AllIgnoreOptions));
                            result.Add(MakeStaticAssignmentOperator(assignment.Syntax, rewrittenAccess, rewrittenRight, false, used: false, AssignmentKind.SimpleAssignment, receiverIsKnownToBeCaptured: false));
                            return;
                        }

                        break;
                    }

                case BoundKind.PointerElementAccess:
                    {
                        rewrittenAccess = RewritePointerElementInitializerAccess(
                            (BoundPointerElementAccess)left, rewrittenReceiver, ref temps, result);

                        if (!isRhsNestedInitializer)
                        {
                            // Rewrite as simple assignment.
                            var rewrittenRight = VisitExpression(right);
                            Debug.Assert(TypeSymbol.Equals(rewrittenAccess.Type, assignment.Type, TypeCompareKind.AllIgnoreOptions));
                            result.Add(MakeStaticAssignmentOperator(assignment.Syntax, rewrittenAccess, rewrittenRight, false, used: false, AssignmentKind.SimpleAssignment, receiverIsKnownToBeCaptured: false));
                            return;
                        }

                        break;
                    }

                case BoundKind.ImplicitIndexerAccess:
                    var implicitIndexer = (BoundImplicitIndexerAccess)left;
                    temps ??= ArrayBuilder<LocalSymbol>.GetInstance();

                    if (TypeSymbol.Equals(implicitIndexer.Argument.Type, _compilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.ConsiderEverything))
                    {
                        rewrittenAccess = VisitIndexPatternIndexerAccess(
                            implicitIndexer,
                            isLeftOfAssignment: !isRhsNestedInitializer,
                            isRegularAssignment: true,
                            cacheAllArgumentsOnly: true,
                            result, temps,
                            receiverIsKnownToBeCaptured: out _);

                        if (rewrittenAccess is BoundIndexerAccess indexerAccess)
                        {
                            rewrittenAccess = TransformIndexerAccessContinued(indexerAccess, indexerAccess.ReceiverOpt!, indexerAccess.Arguments, result, temps);
                        }
                    }
                    else
                    {
                        rewrittenAccess = VisitRangePatternIndexerAccess(implicitIndexer, temps, result, cacheAllArgumentsOnly: true);
                    }

                    if (!isRhsNestedInitializer)
                    {
                        var rewrittenRight = VisitExpression(right);
                        Debug.Assert(TypeSymbol.Equals(rewrittenAccess.Type, assignment.Type, TypeCompareKind.AllIgnoreOptions));
                        result.Add(MakeStaticAssignmentOperator(assignment.Syntax, rewrittenAccess, rewrittenRight, isRef: false, used: false, AssignmentKind.SimpleAssignment, receiverIsKnownToBeCaptured: true));
                        return;
                    }

                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(left.Kind);
            }

            AddObjectOrCollectionInitializers(ref dynamicSiteInitializers, ref temps, result, rewrittenAccess, right);
            return;

            static bool onlyContainsEmptyLeafNestedInitializers(BoundAssignmentOperator assignment)
            {
                // Guard on the cases understood by addIndexes below
                if (assignment.Left is BoundObjectInitializerMember
                    or BoundImplicitIndexerAccess
                    or BoundArrayAccess
                    or BoundPointerElementAccess)
                {
                    return assignment.Right is BoundObjectInitializerExpression initializer
                        && initializer.Initializers.All(e => e is BoundAssignmentOperator nestedAssignment && onlyContainsEmptyLeafNestedInitializers(nestedAssignment));
                }

                return false;
            }

            void addIndexes(ArrayBuilder<BoundExpression> result, BoundAssignmentOperator assignment)
            {
                // If we have an element access of the form `[arguments] = { ... }`, we'll evaluate `arguments` only
                var lhs = assignment.Left;
                if (lhs is BoundObjectInitializerMember initializerMember)
                {
                    foreach (var argument in initializerMember.Arguments)
                    {
                        if (argument is BoundArrayCreation { IsParamsArrayOrCollection: true, InitializerOpt: var initializers })
                        {
                            Debug.Assert(initializers is not null);
                            foreach (var element in initializers.Initializers)
                            {
                                result.Add(VisitExpression(element));
                            }
                        }
                        else
                        {
                            result.Add(VisitExpression(argument));
                        }
                    }
                }
                else if (lhs is BoundImplicitIndexerAccess implicitIndexerAccess)
                {
                    result.Add(VisitExpression(implicitIndexerAccess.Argument));
                }
                else if (lhs is BoundArrayAccess arrayAccess)
                {
                    foreach (var index in arrayAccess.Indices)
                    {
                        result.Add(VisitExpression(index));
                    }
                }
                else if (lhs is BoundPointerElementAccess pointerElementAccess)
                {
                    result.Add(VisitExpression(pointerElementAccess.Index));
                }
                else
                {
                    // We only bind to a BoundDynamicCollectionElementInitializer in a situation like:
                    // D = { ..., <identifier> = <expr>, ... }, where D : dynamic
                    throw ExceptionUtilities.UnexpectedValue(lhs.Kind);
                }

                // And any nested indexes
                foreach (var initializer in ((BoundObjectInitializerExpression)assignment.Right).Initializers)
                {
                    addIndexes(result, (BoundAssignmentOperator)initializer);
                }
            }
        }

        private ImmutableArray<BoundExpression> EvaluateSideEffectingArgumentsToTemps(
                                                 ImmutableArray<BoundExpression> args,
                                                 ImmutableArray<RefKind> paramRefKindsOpt,
                                                 ArrayBuilder<BoundExpression> sideeffects,
                                                 ref ArrayBuilder<LocalSymbol>? temps)
        {
            ArrayBuilder<BoundExpression>? newArgs = null;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                BoundExpression replacement;

                if (arg.IsParamsArrayOrCollection)
                {
                    // Capturing the array instead is going to lead to an observable behavior difference. Not just an IL difference,
                    // see Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.ObjectAndCollectionInitializerTests.DictionaryInitializerTestSideeffects001param for example.
                    (LocalRewriter rewriter, ArrayBuilder<BoundExpression> sideeffects, ArrayBuilder<LocalSymbol>? temps) elementArg = (rewriter: this, sideeffects, temps);
                    replacement = RewriteParamsArray(
                                      arg,
                                      static (BoundExpression element, ref (LocalRewriter rewriter, ArrayBuilder<BoundExpression> sideeffects, ArrayBuilder<LocalSymbol>? temps) elementArg) =>
                                          elementArg.rewriter.EvaluateSideEffects(element, RefKind.None, elementArg.sideeffects, ref elementArg.temps),
                                      ref elementArg);
                    temps = elementArg.temps;
                }
                else
                {
                    replacement = EvaluateSideEffects(arg, paramRefKindsOpt.RefKinds(i), sideeffects, ref temps);
                }

                if (replacement != arg)
                {
                    if (newArgs == null)
                    {
                        newArgs = ArrayBuilder<BoundExpression>.GetInstance(args.Length);
                        newArgs.AddRange(args, i);
                    }

                    newArgs.Add(replacement);
                }
                else if (newArgs != null)
                {
                    newArgs.Add(arg);
                }
            }

            return newArgs?.ToImmutableAndFree() ?? args;
        }

        private BoundExpression EvaluateSideEffects(BoundExpression arg, RefKind refKind, ArrayBuilder<BoundExpression> sideeffects, ref ArrayBuilder<LocalSymbol>? temps)
        {
            if (CanChangeValueBetweenReads(arg))
            {
                BoundAssignmentOperator store;
                var temp = _factory.StoreToTemp(arg, out store, refKind);

                if (temps == null)
                {
                    temps = ArrayBuilder<LocalSymbol>.GetInstance();
                }
                temps.Add(temp.LocalSymbol);
                sideeffects.Add(store);

                return temp;
            }

            return arg;
        }

        private BoundExpression MakeObjectInitializerMemberAccess(
            BoundExpression rewrittenReceiver,
            BoundObjectInitializerMember rewrittenLeft,
            bool isRhsNestedInitializer)
        {
            var memberSymbol = rewrittenLeft.MemberSymbol;
            Debug.Assert(memberSymbol is object);

#if DEBUG
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            Debug.Assert(_compilation.Conversions.ClassifyConversionFromType(rewrittenReceiver.Type, memberSymbol.ContainingType, isChecked: false, ref discardedUseSiteInfo).IsImplicit ||
                         (memberSymbol.IsExtensionBlockMember() && !memberSymbol.IsStatic && ConversionsBase.IsValidExtensionMethodThisArgConversion(_compilation.Conversions.ClassifyConversionFromType(rewrittenReceiver.Type, memberSymbol.ContainingType.ExtensionParameter!.Type, isChecked: false, ref discardedUseSiteInfo))) ||
                         _compilation.Conversions.HasImplicitConversionToOrImplementsVarianceCompatibleInterface(rewrittenReceiver.Type, memberSymbol.ContainingType, ref discardedUseSiteInfo, out _));
            // It is possible there are use site diagnostics from the above, but none that we need report as we aren't generating code for the conversion
#endif
            // Tracked by https://github.com/dotnet/roslyn/issues/78827 : MQ, Consider preserving the BoundConversion from initial binding instead of using markAsChecked here
            rewrittenReceiver = this.ConvertReceiverForExtensionMemberIfNeeded(memberSymbol, rewrittenReceiver, markAsChecked: true);

            switch (memberSymbol.Kind)
            {
                case SymbolKind.Field:
                    var fieldSymbol = (FieldSymbol)memberSymbol;
                    return MakeFieldAccess(rewrittenLeft.Syntax, rewrittenReceiver, fieldSymbol, null, rewrittenLeft.ResultKind, fieldSymbol.Type);

                case SymbolKind.Property:
                    var propertySymbol = (PropertySymbol)memberSymbol;
                    var arguments = rewrittenLeft.Arguments;
                    if (!arguments.IsEmpty || propertySymbol.IsIndexedProperty)
                    {
                        return MakeIndexerAccess(
                            rewrittenLeft.Syntax,
                            rewrittenReceiver,
                            propertySymbol,
                            rewrittenLeft.Arguments,
                            rewrittenLeft.ArgumentNamesOpt,
                            rewrittenLeft.ArgumentRefKindsOpt,
                            rewrittenLeft.Expanded,
                            rewrittenLeft.ArgsToParamsOpt,
                            rewrittenLeft.DefaultArguments,
                            rewrittenLeft,
                            isLeftOfAssignment: !isRhsNestedInitializer,
                            receiverIsKnownToBeCaptured: false);
                    }
                    else
                    {
                        return MakePropertyAccess(
                            rewrittenLeft.Syntax,
                            rewrittenReceiver,
                            propertySymbol,
                            rewrittenLeft.ResultKind,
                            propertySymbol.Type,
                            isLeftOfAssignment: !isRhsNestedInitializer);
                    }

                case SymbolKind.Event:
                    var eventSymbol = (EventSymbol)memberSymbol;
                    return MakeEventAccess(rewrittenLeft.Syntax, rewrittenReceiver, eventSymbol, null, rewrittenLeft.ResultKind, eventSymbol.Type);

                default:
                    throw ExceptionUtilities.UnexpectedValue(memberSymbol.Kind);
            }
        }
    }
}
