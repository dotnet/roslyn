// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            // Assume value of expression is used.
            return VisitAssignmentOperator(node, used: true);
        }

        private BoundExpression VisitAssignmentOperator(BoundAssignmentOperator node, bool used)
        {
            var loweredRight = VisitExpression(node.Right);

            BoundExpression left = node.Left;
            BoundExpression loweredLeft;
            switch (left.Kind)
            {
                case BoundKind.PropertyAccess:
                    loweredLeft = VisitPropertyAccess((BoundPropertyAccess)left, isLeftOfAssignment: true);
                    break;

                case BoundKind.IndexerAccess:
                    loweredLeft = VisitIndexerAccess((BoundIndexerAccess)left, isLeftOfAssignment: true);
                    break;

                case BoundKind.IndexOrRangePatternIndexerAccess:
                    loweredLeft = VisitIndexOrRangePatternIndexerAccess(
                        (BoundIndexOrRangePatternIndexerAccess)left,
                        isLeftOfAssignment: true);
                    break;

                case BoundKind.EventAccess:
                    {
                        BoundEventAccess eventAccess = (BoundEventAccess)left;
                        if (eventAccess.EventSymbol.IsWindowsRuntimeEvent)
                        {
                            Debug.Assert(!node.IsRef);
                            return VisitWindowsRuntimeEventFieldAssignmentOperator(node.Syntax, eventAccess, loweredRight);
                        }
                        goto default;
                    }

                case BoundKind.DynamicMemberAccess:
                    {
                        // dyn.m = expr
                        var memberAccess = (BoundDynamicMemberAccess)left;
                        var loweredReceiver = VisitExpression(memberAccess.Receiver);
                        return _dynamicFactory.MakeDynamicSetMember(loweredReceiver, memberAccess.Name, loweredRight).ToExpression();
                    }

                case BoundKind.DynamicIndexerAccess:
                    {
                        // dyn[args] = expr
                        var indexerAccess = (BoundDynamicIndexerAccess)left;
                        Debug.Assert(indexerAccess.ReceiverOpt != null);
                        var loweredReceiver = VisitExpression(indexerAccess.ReceiverOpt);
                        var loweredArguments = VisitList(indexerAccess.Arguments);
                        return MakeDynamicSetIndex(
                            indexerAccess,
                            loweredReceiver,
                            loweredArguments,
                            indexerAccess.ArgumentNamesOpt,
                            indexerAccess.ArgumentRefKindsOpt,
                            loweredRight);
                    }

                default:
                    loweredLeft = VisitExpression(left);
                    break;
            }

            return MakeStaticAssignmentOperator(node.Syntax, loweredLeft, loweredRight, node.IsRef, node.Type, used);
        }

        /// <summary>
        /// Generates a lowered form of the assignment operator for the given left and right sub-expressions.
        /// Left and right sub-expressions must be in lowered form.
        /// </summary>
        private BoundExpression MakeAssignmentOperator(SyntaxNode syntax, BoundExpression rewrittenLeft, BoundExpression rewrittenRight, TypeSymbol type,
            bool used, bool isChecked, bool isCompoundAssignment)
        {
            switch (rewrittenLeft.Kind)
            {
                case BoundKind.DynamicIndexerAccess:
                    var indexerAccess = (BoundDynamicIndexerAccess)rewrittenLeft;
                    return MakeDynamicSetIndex(
                        indexerAccess,
                        indexerAccess.ReceiverOpt,
                        indexerAccess.Arguments,
                        indexerAccess.ArgumentNamesOpt,
                        indexerAccess.ArgumentRefKindsOpt,
                        rewrittenRight,
                        isCompoundAssignment, isChecked);

                case BoundKind.DynamicMemberAccess:
                    var memberAccess = (BoundDynamicMemberAccess)rewrittenLeft;
                    return _dynamicFactory.MakeDynamicSetMember(
                        memberAccess.Receiver,
                        memberAccess.Name,
                        rewrittenRight,
                        isCompoundAssignment,
                        isChecked).ToExpression();

                case BoundKind.EventAccess:
                    var eventAccess = (BoundEventAccess)rewrittenLeft;
                    Debug.Assert(eventAccess.IsUsableAsField);
                    if (eventAccess.EventSymbol.IsWindowsRuntimeEvent)
                    {
                        const bool isDynamic = false;
                        return RewriteWindowsRuntimeEventAssignmentOperator(eventAccess.Syntax,
                                                                            eventAccess.EventSymbol,
                                                                            EventAssignmentKind.Assignment,
                                                                            isDynamic,
                                                                            eventAccess.ReceiverOpt,
                                                                            rewrittenRight);
                    }

                    // Only Windows Runtime field-like events can come through here:
                    // - Assignment operation is not supported for custom (non-field like) events.
                    // - Access to regular field-like events is expected to be lowered to at least a field access
                    //   when we reach here.
                    throw ExceptionUtilities.Unreachable;

                default:
                    return MakeStaticAssignmentOperator(syntax, rewrittenLeft, rewrittenRight, isRef: false, type: type, used: used);
            }
        }

        private BoundExpression MakeDynamicSetIndex(
            BoundDynamicIndexerAccess indexerAccess,
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames,
            ImmutableArray<RefKind> refKinds,
            BoundExpression loweredRight,
            bool isCompoundAssignment = false,
            bool isChecked = false)
        {
            // If we are calling a method on a NoPIA type, we need to embed all methods/properties
            // with the matching name of this dynamic invocation.
            EmbedIfNeedTo(loweredReceiver, indexerAccess.ApplicableIndexers, indexerAccess.Syntax);

            return _dynamicFactory.MakeDynamicSetIndex(
                MakeDynamicIndexerAccessReceiver(indexerAccess, loweredReceiver),
                loweredArguments,
                argumentNames,
                refKinds,
                loweredRight,
                isCompoundAssignment, isChecked).ToExpression();
        }

        /// <summary>
        /// Generates a lowered form of the assignment operator for the given left and right sub-expressions.
        /// Left and right sub-expressions must be in lowered form.
        /// </summary>
        private BoundExpression MakeStaticAssignmentOperator(
            SyntaxNode syntax,
            BoundExpression rewrittenLeft,
            BoundExpression rewrittenRight,
            bool isRef,
            TypeSymbol type,
            bool used)
        {
            switch (rewrittenLeft.Kind)
            {
                case BoundKind.DynamicIndexerAccess:
                case BoundKind.DynamicMemberAccess:
                    throw ExceptionUtilities.UnexpectedValue(rewrittenLeft.Kind);

                case BoundKind.PropertyAccess:
                    {
                        Debug.Assert(!isRef);
                        BoundPropertyAccess propertyAccess = (BoundPropertyAccess)rewrittenLeft;
                        BoundExpression rewrittenReceiver = propertyAccess.ReceiverOpt;
                        PropertySymbol property = propertyAccess.PropertySymbol;
                        Debug.Assert(!property.IsIndexer);
                        return MakePropertyAssignment(
                            syntax,
                            rewrittenReceiver,
                            property,
                            ImmutableArray<BoundExpression>.Empty,
                            default(ImmutableArray<RefKind>),
                            false,
                            default(ImmutableArray<int>),
                            rewrittenRight,
                            type,
                            used);
                    }

                case BoundKind.IndexerAccess:
                    {
                        Debug.Assert(!isRef);
                        BoundIndexerAccess indexerAccess = (BoundIndexerAccess)rewrittenLeft;
                        BoundExpression rewrittenReceiver = indexerAccess.ReceiverOpt;
                        ImmutableArray<BoundExpression> rewrittenArguments = indexerAccess.Arguments;
                        PropertySymbol indexer = indexerAccess.Indexer;
                        Debug.Assert(indexer.IsIndexer || indexer.IsIndexedProperty);
                        return MakePropertyAssignment(
                            syntax,
                            rewrittenReceiver,
                            indexer,
                            rewrittenArguments,
                            indexerAccess.ArgumentRefKindsOpt,
                            indexerAccess.Expanded,
                            indexerAccess.ArgsToParamsOpt,
                            rewrittenRight,
                            type,
                            used);
                    }

                case BoundKind.Local:
                    {
                        Debug.Assert(!isRef || ((BoundLocal)rewrittenLeft).LocalSymbol.RefKind != RefKind.None);
                        return new BoundAssignmentOperator(
                            syntax,
                            rewrittenLeft,
                            rewrittenRight,
                            type,
                            isRef: isRef);
                    }

                case BoundKind.Parameter:
                    {
                        Debug.Assert(!isRef || rewrittenLeft.GetRefKind() != RefKind.None);
                        return new BoundAssignmentOperator(
                            syntax,
                            rewrittenLeft,
                            rewrittenRight,
                            isRef,
                            type);
                    }

                case BoundKind.DiscardExpression:
                    {
                        return rewrittenRight;
                    }

                case BoundKind.Sequence:
                    // An Index or Range pattern-based indexer produces a sequence with a nested
                    // BoundIndexerAccess. We need to lower the final expression and produce an
                    // update sequence
                    var sequence = (BoundSequence)rewrittenLeft;
                    if (sequence.Value.Kind == BoundKind.IndexerAccess)
                    {
                        return sequence.Update(
                            sequence.Locals,
                            sequence.SideEffects,
                            MakeStaticAssignmentOperator(
                                syntax,
                                sequence.Value,
                                rewrittenRight,
                                isRef,
                                type,
                                used),
                            type);
                    }
                    goto default;

                default:
                    {
                        Debug.Assert(!isRef);
                        return new BoundAssignmentOperator(
                            syntax,
                            rewrittenLeft,
                            rewrittenRight,
                            type);
                    }
            }
        }

        private BoundExpression MakePropertyAssignment(
            SyntaxNode syntax,
            BoundExpression rewrittenReceiver,
            PropertySymbol property,
            ImmutableArray<BoundExpression> rewrittenArguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            BoundExpression rewrittenRight,
            TypeSymbol type,
            bool used)
        {
            // Rewrite property assignment into call to setter.
            var setMethod = property.GetOwnOrInheritedSetMethod();

            if ((object)setMethod == null)
            {
                Debug.Assert(property is SourcePropertySymbol { IsAutoProperty: true },
                    "only autoproperties can be assignable without having setters");

                var backingField = (property as SourcePropertySymbol).BackingField;
                return _factory.AssignmentExpression(
                    _factory.Field(rewrittenReceiver, backingField),
                    rewrittenRight);
            }

            // We have already lowered each argument, but we may need some additional rewriting for the arguments,
            // such as generating a params array, re-ordering arguments based on argsToParamsOpt map, inserting arguments for optional parameters, etc.
            ImmutableArray<LocalSymbol> argTemps;
            rewrittenArguments = MakeArguments(
                syntax,
                rewrittenArguments,
                property,
                setMethod,
                expanded,
                argsToParamsOpt,
                ref argumentRefKindsOpt,
                out argTemps,
                invokedAsExtensionMethod: false,
                enableCallerInfo: ThreeState.True);

            if (used)
            {
                // Save expression value to a temporary before calling the
                // setter, and restore the temporary after the setter, so the
                // assignment can be used as an embedded expression.
                TypeSymbol exprType = rewrittenRight.Type;

                LocalSymbol rhsTemp = _factory.SynthesizedLocal(exprType);

                BoundExpression boundRhs = new BoundLocal(syntax, rhsTemp, null, exprType);

                BoundExpression rhsAssignment = new BoundAssignmentOperator(
                    syntax,
                    boundRhs,
                    rewrittenRight,
                    exprType);

                BoundExpression setterCall = BoundCall.Synthesized(
                    syntax,
                    rewrittenReceiver,
                    setMethod,
                    AppendToPossibleNull(rewrittenArguments, rhsAssignment));

                return new BoundSequence(
                    syntax,
                    AppendToPossibleNull(argTemps, rhsTemp),
                    ImmutableArray.Create(setterCall),
                    boundRhs,
                    type);
            }
            else
            {
                BoundCall setterCall = BoundCall.Synthesized(
                    syntax,
                    rewrittenReceiver,
                    setMethod,
                    AppendToPossibleNull(rewrittenArguments, rewrittenRight));

                if (argTemps.IsDefaultOrEmpty)
                {
                    return setterCall;
                }
                else
                {
                    return new BoundSequence(
                        syntax,
                        argTemps,
                        ImmutableArray<BoundExpression>.Empty,
                        setterCall,
                        setMethod.ReturnType);
                }
            }
        }

        private static ImmutableArray<T> AppendToPossibleNull<T>(ImmutableArray<T> possibleNull, T newElement)
        {
            Debug.Assert(newElement != null);
            return possibleNull.NullToEmpty().Add(newElement);
        }
    }
}
