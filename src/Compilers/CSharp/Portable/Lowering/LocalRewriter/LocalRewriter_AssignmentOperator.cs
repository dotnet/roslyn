// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
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

                case BoundKind.ImplicitIndexerAccess:
                    loweredLeft = VisitImplicitIndexerAccess(
                        (BoundImplicitIndexerAccess)left,
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
                        var loweredReceiver = VisitExpression(indexerAccess.Receiver);
                        // Dynamic can't have created handler conversions because we don't know target types.
                        AssertNoImplicitInterpolatedStringHandlerConversions(indexerAccess.Arguments);
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

            return MakeStaticAssignmentOperator(node.Syntax, loweredLeft, loweredRight, node.IsRef, used, AssignmentKind.SimpleAssignment);
        }

        private enum AssignmentKind
        {
            SimpleAssignment,
            CompoundAssignment,
            IncrementDecrement,
            Deconstruction,
            NullCoalescingAssignment,
        }

        /// <summary>
        /// Generates a lowered form of the assignment operator for the given left and right sub-expressions.
        /// Left and right sub-expressions must be in lowered form.
        /// </summary>
        private BoundExpression MakeAssignmentOperator(SyntaxNode syntax, BoundExpression rewrittenLeft, BoundExpression rewrittenRight,
            bool used, bool isChecked, AssignmentKind assignmentKind)
        {
            switch (rewrittenLeft.Kind)
            {
                case BoundKind.DynamicIndexerAccess:
                    var indexerAccess = (BoundDynamicIndexerAccess)rewrittenLeft;
                    return MakeDynamicSetIndex(
                        indexerAccess,
                        indexerAccess.Receiver,
                        indexerAccess.Arguments,
                        indexerAccess.ArgumentNamesOpt,
                        indexerAccess.ArgumentRefKindsOpt,
                        rewrittenRight,
                        isCompoundAssignment: assignmentKind == AssignmentKind.CompoundAssignment, isChecked);

                case BoundKind.DynamicMemberAccess:
                    var memberAccess = (BoundDynamicMemberAccess)rewrittenLeft;
                    return _dynamicFactory.MakeDynamicSetMember(
                        memberAccess.Receiver,
                        memberAccess.Name,
                        rewrittenRight,
                        isCompoundAssignment: assignmentKind == AssignmentKind.CompoundAssignment,
                        isChecked).ToExpression();

                case BoundKind.EventAccess:
                    var eventAccess = (BoundEventAccess)rewrittenLeft;
                    Debug.Assert(eventAccess.IsUsableAsField);
                    if (eventAccess.EventSymbol.IsWindowsRuntimeEvent)
                    {
                        return RewriteWindowsRuntimeEventAssignmentOperator(eventAccess.Syntax,
                                                                            eventAccess.EventSymbol,
                                                                            EventAssignmentKind.Assignment,
                                                                            eventAccess.ReceiverOpt,
                                                                            rewrittenRight);
                    }

                    // Only Windows Runtime field-like events can come through here:
                    // - Assignment operation is not supported for custom (non-field like) events.
                    // - Access to regular field-like events is expected to be lowered to at least a field access
                    //   when we reach here.
                    throw ExceptionUtilities.Unreachable();

                default:
                    return MakeStaticAssignmentOperator(syntax, rewrittenLeft, rewrittenRight, isRef: false, used: used, assignmentKind);
            }
        }

        private BoundExpression MakeDynamicSetIndex(
            BoundDynamicIndexerAccess indexerAccess,
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string?> argumentNames,
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
            bool used,
            AssignmentKind assignmentKind)
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
                        BoundExpression? rewrittenReceiver = propertyAccess.ReceiverOpt;
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
                            used,
                            assignmentKind);
                    }

                case BoundKind.IndexerAccess:
                    {
                        Debug.Assert(!isRef);
                        BoundIndexerAccess indexerAccess = (BoundIndexerAccess)rewrittenLeft;
                        BoundExpression? rewrittenReceiver = indexerAccess.ReceiverOpt;
                        ImmutableArray<BoundExpression> arguments = indexerAccess.Arguments;
                        PropertySymbol indexer = indexerAccess.Indexer;
                        Debug.Assert(indexer.IsIndexer || indexer.IsIndexedProperty);
                        return MakePropertyAssignment(
                            syntax,
                            rewrittenReceiver,
                            indexer,
                            arguments,
                            indexerAccess.ArgumentRefKindsOpt,
                            indexerAccess.Expanded,
                            indexerAccess.ArgsToParamsOpt,
                            rewrittenRight,
                            used,
                            assignmentKind);
                    }

                case BoundKind.Local:
                case BoundKind.Parameter:
                case BoundKind.FieldAccess:
                    {
                        Debug.Assert(!isRef || rewrittenLeft.GetRefKind() != RefKind.None);
                        return _factory.AssignmentExpression(
                            syntax,
                            rewrittenLeft,
                            rewrittenRight,
                            isRef);
                    }

                case BoundKind.DiscardExpression:
                    {
                        return rewrittenRight;
                    }

                case BoundKind.Sequence:
                    {
                        // An Index or Range pattern-based indexer, or an interpolated string handler conversion
                        // that uses an indexer argument, produces a sequence with a BoundIndexerAccess as the value.
                        // Similarly, a ref-returning extension indexer in an object initializer produces
                        // a sequence with a BoundCall to the getter as the value.
                        // To respect requirements from later phases, we need to rewrite the assignment to the value of the sequence.
                        var sequence = (BoundSequence)rewrittenLeft;
                        return sequence.Update(
                            sequence.Locals,
                            sequence.SideEffects,
                            MakeStaticAssignmentOperator(
                                syntax,
                                sequence.Value,
                                rewrittenRight,
                                isRef,
                                used,
                                assignmentKind),
                            sequence.Type);
                    }

                default:
                    {
                        Debug.Assert(!isRef);
                        return _factory.AssignmentExpression(
                            syntax,
                            rewrittenLeft,
                            rewrittenRight);
                    }
            }
        }

        private bool IsExtensionPropertyWithByValPossiblyStructReceiverWhichHasHomeAndCanChangeValueBetweenReads(BoundExpression rewrittenReceiver, PropertySymbol property)
        {
            return CanChangeValueBetweenReads(rewrittenReceiver, localsMayBeAssignedOrCaptured: true, structThisCanChangeValueBetweenReads: true) &&
                   IsExtensionBlockMemberWithByValPossiblyStructReceiver(property) &&
                   CodeGen.CodeGenerator.HasHome(rewrittenReceiver,
                                       CodeGen.CodeGenerator.AddressKind.ReadOnlyStrict,
                                       _factory.CurrentFunction,
                                       peVerifyCompatEnabled: false,
                                       stackLocalsOpt: null);
        }

        private BoundExpression MakePropertyAssignment(
            SyntaxNode syntax,
            BoundExpression? rewrittenReceiver,
            PropertySymbol property,
            ImmutableArray<BoundExpression> arguments,
            ImmutableArray<RefKind> argumentRefKindsOpt,
            bool expanded,
            ImmutableArray<int> argsToParamsOpt,
            BoundExpression rewrittenRight,
            bool used,
            AssignmentKind assignmentKind)
        {
            // Rewrite property assignment into call to setter.
            var setMethod = property.GetOwnOrInheritedSetMethod();

            if (setMethod is null)
            {
                var autoProp = (SourcePropertySymbolBase)property.OriginalDefinition;
                Debug.Assert(autoProp.IsAutoPropertyOrUsesFieldKeyword,
                    "only autoproperties can be assignable without having setters");
                Debug.Assert(_factory.CurrentFunction.IsConstructor());
                Debug.Assert(property.Equals(autoProp, TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

                var backingField = autoProp.BackingField;
                return _factory.AssignmentExpression(
                    _factory.Field(rewrittenReceiver, backingField),
                    rewrittenRight);
            }

            ArrayBuilder<LocalSymbol>? argTempsBuilder = null;

            bool needSpecialExtensionPropertyReceiverReadOrder = false;
            ArrayBuilder<BoundExpression>? storesOpt = null;

            if (rewrittenReceiver is not null &&
                assignmentKind is not (AssignmentKind.CompoundAssignment or AssignmentKind.NullCoalescingAssignment or AssignmentKind.Deconstruction or AssignmentKind.IncrementDecrement) &&
                IsExtensionPropertyWithByValPossiblyStructReceiverWhichHasHomeAndCanChangeValueBetweenReads(rewrittenReceiver, property) &&
                (arguments.Length != 0 || !IsSafeForReordering(rewrittenRight, RefKind.None)))
            {
                // The receiver has location, but extension property/indexer takes receiver by value.
                // This means that we need to ensure that the receiver value is read after
                // any side-effecting arguments and right hand side are evaluated, so that the
                // setter receives the last value of the receiver, not the value before the
                // arguments/rhs were evaluated. Receiver side effects should be evaluated at
                // the very beginning, of course.

                needSpecialExtensionPropertyReceiverReadOrder = true;
                storesOpt = ArrayBuilder<BoundExpression>.GetInstance();
            }

#if DEBUG
            BoundExpression? rewrittenReceiverBeforePossibleCapture = rewrittenReceiver;
#endif
            arguments = VisitArgumentsAndCaptureReceiverIfNeeded(
                ref rewrittenReceiver,
                forceReceiverCapturing: needSpecialExtensionPropertyReceiverReadOrder,
                arguments,
                property,
                argsToParamsOpt,
                argumentRefKindsOpt,
                storesOpt,
                ref argTempsBuilder);

            if (needSpecialExtensionPropertyReceiverReadOrder)
            {
#if DEBUG
                Debug.Assert(rewrittenReceiverBeforePossibleCapture != (object?)rewrittenReceiver);
#endif
                Debug.Assert(storesOpt is { });
                Debug.Assert(storesOpt.Count != 0);
                Debug.Assert(argTempsBuilder is not null);

                // Store everything that is non-trivial into a temporary; record the
                // stores in storesToTemps and make the actual argument a reference to the temp.
                arguments = ExtractSideEffectsFromArguments(arguments, property, expanded, argsToParamsOpt, ref argumentRefKindsOpt, storesOpt, argTempsBuilder);

                if (!IsSafeForReordering(rewrittenRight, RefKind.None))
                {
                    BoundLocal capturedRight = _factory.StoreToTemp(rewrittenRight, out BoundAssignmentOperator assignmentToTemp, refKind: RefKind.None);
                    argTempsBuilder.Add(capturedRight.LocalSymbol);
                    storesOpt.Add(assignmentToTemp);
                    rewrittenRight = capturedRight;
                }
            }
            else
            {
                arguments = MakeArguments(
                    arguments,
                    property,
                    expanded,
                    argsToParamsOpt,
                    ref argumentRefKindsOpt,
                    ref argTempsBuilder,
                    invokedAsExtensionMethod: false);
            }

            var sideEffects = storesOpt is null ? [] : storesOpt.ToImmutableAndFree();
            var argTemps = argTempsBuilder.ToImmutableAndFree();

            if (used)
            {
                // Save expression value to a temporary before calling the
                // setter, and restore the temporary after the setter, so the
                // assignment can be used as an embedded expression.
                TypeSymbol? exprType = rewrittenRight.Type;
                Debug.Assert(exprType is object);

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
                    initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                    setMethod,
                    AppendToPossibleNull(arguments, rhsAssignment));

                return new BoundSequence(
                    syntax,
                    AppendToPossibleNull(argTemps, rhsTemp),
                    sideEffects.Add(setterCall), // https://github.com/dotnet/roslyn/issues/78829 - there is no test coverage for 'sideEffects' on this code path
                    boundRhs,
                    rhsTemp.Type);
            }
            else
            {
                BoundCall setterCall = BoundCall.Synthesized(
                    syntax,
                    rewrittenReceiver,
                    initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown,
                    setMethod,
                    AppendToPossibleNull(arguments, rewrittenRight));

                if (argTemps.IsDefaultOrEmpty)
                {
                    Debug.Assert(sideEffects.IsEmpty);
                    return setterCall;
                }
                else
                {
                    return new BoundSequence(
                        syntax,
                        argTemps,
                        sideEffects,
                        setterCall,
                        setMethod.ReturnType);
                }
            }
        }

        private static ImmutableArray<T> AppendToPossibleNull<T>(ImmutableArray<T> possibleNull, T newElement)
            where T : notnull
        {
            Debug.Assert(newElement is { });
            return possibleNull.NullToEmpty().Add(newElement);
        }
    }
}
