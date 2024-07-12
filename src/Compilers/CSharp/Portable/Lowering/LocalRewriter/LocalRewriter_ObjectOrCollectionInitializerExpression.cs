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
                    AddCollectionInitializers(result, null, ((BoundCollectionInitializerExpression)initializerExpression).Initializers);
                    return result.ToImmutableAndFree();

                default:
                    throw ExceptionUtilities.UnexpectedValue(initializerExpression.Kind);
            }
        }

        // Rewrite collection initializer add method calls:
        // 2) new List<int> { 1 };
        //                    ~
        private void AddCollectionInitializers(ArrayBuilder<BoundExpression> result, BoundExpression? rewrittenReceiver, ImmutableArray<BoundExpression> initializers)
        {
            Debug.Assert(rewrittenReceiver is { } || _inExpressionLambda);

            foreach (var initializer in initializers)
            {
                // In general bound initializers may contain bad expressions or element initializers.
                // We don't lower them if they contain errors, so it's safe to assume an element initializer.

                BoundExpression? rewrittenInitializer;
                if (initializer.Kind == BoundKind.CollectionElementInitializer)
                {
                    rewrittenInitializer = MakeCollectionInitializer(rewrittenReceiver, (BoundCollectionElementInitializer)initializer);
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
        private BoundExpression? MakeCollectionInitializer(BoundExpression? rewrittenReceiver, BoundCollectionElementInitializer initializer)
        {
            MethodSymbol addMethod = initializer.AddMethod;

            Debug.Assert(addMethod.Name == "Add");
            Debug.Assert(addMethod.Parameters
                .Skip(addMethod.IsExtensionMethod ? 1 : 0)
                .All(p => p.RefKind is RefKind.None or RefKind.In or RefKind.RefReadOnlyParameter));
            Debug.Assert(initializer.Arguments.Any());
            Debug.Assert(rewrittenReceiver != null || _inExpressionLambda);

            var syntax = initializer.Syntax;

            if (_allowOmissionOfConditionalCalls)
            {
                // NOTE: Calls cannot be omitted within an expression tree (CS0765); this should already
                // have been checked.
                if (addMethod.CallsAreOmitted(initializer.SyntaxTree))
                {
                    return null;
                }
            }

            var argumentRefKindsOpt = default(ImmutableArray<RefKind>);
            if (addMethod.Parameters[0].RefKind == RefKind.Ref)
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
                captureReceiverMode: ReceiverCaptureMode.Default,
                initializer.Arguments,
                addMethod,
                initializer.ArgsToParamsOpt,
                argumentRefKindsOpt,
                storesOpt: null,
                ref temps);
            rewrittenArguments = MakeArguments(rewrittenArguments, addMethod, initializer.Expanded, initializer.ArgsToParamsOpt, ref argumentRefKindsOpt, ref temps);

            var rewrittenType = VisitType(initializer.Type);

            if (initializer.InvokedAsExtensionMethod)
            {
                Debug.Assert(addMethod.IsStatic);
                Debug.Assert(addMethod.IsExtensionMethod);
                Debug.Assert(!_inExpressionLambda, "Expression trees do not support extension Add");
                rewrittenReceiver = null;
            }

            if (_inExpressionLambda)
            {
                Debug.Assert(temps.Count == 0);
                temps.Free();
                return initializer.Update(addMethod, rewrittenArguments, rewrittenReceiver, expanded: false, argsToParamsOpt: default, defaultArguments: default, invokedAsExtensionMethod: false, initializer.ResultKind, rewrittenType);
            }

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
            var rewrittenArguments = VisitArgumentsAndCaptureReceiverIfNeeded(ref rewrittenReceiver, captureReceiverMode: ReceiverCaptureMode.Default, node.Arguments, node.MemberSymbol, node.ArgsToParamsOpt, node.ArgumentRefKindsOpt,
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

            return node.Update(node.MemberSymbol, rewrittenArguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.Expanded, node.ArgsToParamsOpt, node.DefaultArguments, node.ResultKind, node.ReceiverType, node.Type);
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
                // In general bound initializers may contain bad expressions or assignments.
                // We don't lower them if they contain errors, so it's safe to assume an assignment.
                AddObjectInitializer(ref dynamicSiteInitializers, ref temps, result, rewrittenReceiver, (BoundAssignmentOperator)initializer);
            }
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
                        var memberInit = (BoundObjectInitializerMember)VisitObjectInitializerMember(
                            (BoundObjectInitializerMember)left, ref rewrittenReceiver, result, ref temps);

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
                                memberInit.ReceiverType,
                                memberInit.Type);
                        }

                        if (memberInit.MemberSymbol == null && memberInit.Type.IsDynamic())
                        {
                            Debug.Assert(!memberInit.Expanded);

                            if (dynamicSiteInitializers == null)
                            {
                                dynamicSiteInitializers = ArrayBuilder<BoundExpression>.GetInstance();
                            }

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
                                result.Add(MakeStaticAssignmentOperator(assignment.Syntax, rewrittenAccess, rewrittenRight, isRef: assignment.IsRef, used: false));
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
                            Debug.Assert(TypeSymbol.Equals(rangeArgument.Type, _compilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.ConsiderEverything));

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
                            result.Add(MakeStaticAssignmentOperator(assignment.Syntax, rewrittenAccess, rewrittenRight, false, used: false));
                            return;
                        }

                        break;
                    }

                case BoundKind.PointerElementAccess:
                    {
                        var pointerAccess = (BoundPointerElementAccess)left;
                        var rewrittenIndex = VisitExpression(pointerAccess.Index);

                        if (CanChangeValueBetweenReads(rewrittenIndex))
                        {
                            BoundAssignmentOperator store;
                            var temp = _factory.StoreToTemp(rewrittenIndex, out store);
                            rewrittenIndex = temp;

                            if (temps == null)
                            {
                                temps = ArrayBuilder<LocalSymbol>.GetInstance();
                            }
                            temps.Add(temp.LocalSymbol);
                            result.Add(store);
                        }

                        rewrittenAccess = RewritePointerElementAccess(pointerAccess, rewrittenReceiver, rewrittenIndex);

                        if (!isRhsNestedInitializer)
                        {
                            // Rewrite as simple assignment.
                            var rewrittenRight = VisitExpression(right);
                            Debug.Assert(TypeSymbol.Equals(rewrittenAccess.Type, assignment.Type, TypeCompareKind.AllIgnoreOptions));
                            result.Add(MakeStaticAssignmentOperator(assignment.Syntax, rewrittenAccess, rewrittenRight, false, used: false));
                            return;
                        }

                        break;
                    }

                case BoundKind.ImplicitIndexerAccess:
                    var implicitIndexer = (BoundImplicitIndexerAccess)left;
                    temps ??= ArrayBuilder<LocalSymbol>.GetInstance();

                    if (TypeSymbol.Equals(implicitIndexer.Argument.Type, _compilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.ConsiderEverything))
                    {
                        rewrittenAccess = GetUnderlyingIndexerOrSliceAccess(
                            implicitIndexer,
                            isLeftOfAssignment: !isRhsNestedInitializer,
                            isRegularAssignmentOrRegularCompoundAssignment: true,
                            cacheAllArgumentsOnly: true,
                            result, temps);

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
                        result.Add(MakeStaticAssignmentOperator(assignment.Syntax, rewrittenAccess, rewrittenRight, isRef: false, used: false));
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
                                      elementRewriter: static (BoundExpression element, ref (LocalRewriter rewriter, ArrayBuilder<BoundExpression> sideeffects, ArrayBuilder<LocalSymbol>? temps) elementArg) =>
                                          elementArg.rewriter.EvaluateSideEffects(element, RefKind.None, elementArg.sideeffects, ref elementArg.temps),
                                      arg: ref elementArg);
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
                         _compilation.Conversions.HasImplicitConversionToOrImplementsVarianceCompatibleInterface(rewrittenReceiver.Type, memberSymbol.ContainingType, ref discardedUseSiteInfo, out _));
            // It is possible there are use site diagnostics from the above, but none that we need report as we aren't generating code for the conversion
#endif

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
                            oldNodeOpt: null,
                            isLeftOfAssignment: !isRhsNestedInitializer);
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
