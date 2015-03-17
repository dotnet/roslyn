// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            return RewriteConditionalAccess(node, used: true);
        }

        // null when currently enclosing conditional access node
        // is not supposed to be lowered.
        private BoundExpression _currentConditionalAccessTarget = null;

        private enum ConditionalAccessLoweringKind
        {
            None,
            NoCapture,
            CaptureReceiverByVal,
            CaptureReceiverByRef,
            DuplicateCode
        }

        // IL gen can generate more compact code for certain conditional accesses 
        // by utilizing stack dup/pop instructions 
        internal BoundExpression RewriteConditionalAccess(BoundConditionalAccess node, bool used, BoundExpression rewrittenWhenNull = null)
        {
            Debug.Assert(!_inExpressionLambda);

            var loweredReceiver = this.VisitExpression(node.Receiver);
            var receiverType = loweredReceiver.Type;

            //TODO: if AccessExpression does not contain awaits, the node could be left unlowered (saves a temp),
            //      but there seem to be no way of knowing that without walking AccessExpression.
            //      For now we will just check that we are in an async method, but it would be nice
            //      to have something more precise.
            var isAsync = _factory.CurrentMethod.IsAsync;

            ConditionalAccessLoweringKind loweringKind;
            // CONSIDER: If we knew that loweredReceiver is not a captured local
            //       we could pass "false" for localsMayBeAssignedOrCaptured
            //       otherwise not capturing receiver into a temp
            //       could introduce additional races into the code if receiver is captured
            //       into a closure and is modified between null check of the receiver 
            //       and the actual access.
            //
            //       Nullable is special since we are not going to read any part of it twice
            //       we will read "HasValue" and then, conditionally will read "ValueOrDefault"
            //       that is no different than just reading both values unconditionally.
            //       As a result in the case of nullable, not reading captured local through a temp 
            //       does not introduce any additional races so it is irrelevant whether 
            //       the local is captured or not.
            var localsMayBeAssignedOrCaptured = !receiverType.IsNullableType();
            var needTemp = IntroducingReadCanBeObservable(loweredReceiver, localsMayBeAssignedOrCaptured);

            if (!isAsync && !node.AccessExpression.Type.IsDynamic() &&
                (receiverType.IsReferenceType || receiverType.IsTypeParameter() && needTemp))
            {
                // trivial cases can be handled more efficiently in IL gen
                loweringKind = ConditionalAccessLoweringKind.None;
            }
            else if (needTemp)
            {
                if (receiverType.IsReferenceType || receiverType.IsNullableType())
                {
                    loweringKind = ConditionalAccessLoweringKind.CaptureReceiverByVal;
                }
                else
                {
                    loweringKind = isAsync ?
                        ConditionalAccessLoweringKind.DuplicateCode :
                        ConditionalAccessLoweringKind.CaptureReceiverByRef;
                }
            }
            else
            {
                // locals do not need to be captured
                loweringKind = ConditionalAccessLoweringKind.NoCapture;
            }


            var previousConditionalAccesTarget = _currentConditionalAccessTarget;
            LocalSymbol temp = null;
            BoundExpression unconditionalAccess = null;

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.None:
                    _currentConditionalAccessTarget = null;
                    break;

                case ConditionalAccessLoweringKind.NoCapture:
                    _currentConditionalAccessTarget = loweredReceiver;
                    break;

                case ConditionalAccessLoweringKind.DuplicateCode:
                    _currentConditionalAccessTarget = loweredReceiver;
                    unconditionalAccess = used ?
                        this.VisitExpression(node.AccessExpression) :
                        this.VisitUnusedExpression(node.AccessExpression);

                    goto case ConditionalAccessLoweringKind.CaptureReceiverByVal;

                case ConditionalAccessLoweringKind.CaptureReceiverByVal:
                    temp = _factory.SynthesizedLocal(receiverType);
                    _currentConditionalAccessTarget = _factory.Local(temp);
                    break;

                case ConditionalAccessLoweringKind.CaptureReceiverByRef:
                    temp = _factory.SynthesizedLocal(receiverType, refKind: RefKind.Ref);
                    _currentConditionalAccessTarget = _factory.Local(temp);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(loweringKind);
            }

            BoundExpression loweredAccessExpression = used ?
                        this.VisitExpression(node.AccessExpression) :
                        this.VisitUnusedExpression(node.AccessExpression);

            _currentConditionalAccessTarget = previousConditionalAccesTarget;

            TypeSymbol type = this.VisitType(node.Type);

            TypeSymbol nodeType = node.Type;
            TypeSymbol accessExpressionType = loweredAccessExpression.Type;

            if (accessExpressionType.SpecialType == SpecialType.System_Void)
            {
                type = nodeType = accessExpressionType;
            }

            if (accessExpressionType != nodeType && nodeType.IsNullableType())
            {
                Debug.Assert(accessExpressionType == nodeType.GetNullableUnderlyingType());
                loweredAccessExpression = _factory.New((NamedTypeSymbol)nodeType, loweredAccessExpression);

                if (unconditionalAccess != null)
                {
                    unconditionalAccess = _factory.New((NamedTypeSymbol)nodeType, unconditionalAccess);
                }
            }
            else
            {
                Debug.Assert(accessExpressionType == nodeType ||
                    (nodeType.SpecialType == SpecialType.System_Void && !used));
            }

            BoundExpression result;
            var objectType = _compilation.GetSpecialType(SpecialType.System_Object);

            rewrittenWhenNull = rewrittenWhenNull ?? _factory.Default(nodeType);

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.None:
                    Debug.Assert(!receiverType.IsValueType);
                    result = new BoundLoweredConditionalAccess(
                        node.Syntax, 
                        loweredReceiver, 
                        loweredAccessExpression, 
                        rewrittenWhenNull, type);

                    break;

                case ConditionalAccessLoweringKind.CaptureReceiverByVal:
                    // capture the receiver into a temp
                    loweredReceiver = _factory.Sequence(
                                            _factory.AssignmentExpression(_factory.Local(temp), loweredReceiver),
                                            _factory.Local(temp));

                    goto case ConditionalAccessLoweringKind.NoCapture;

                case ConditionalAccessLoweringKind.NoCapture:
                    {
                        // (object)r != null ? access : default(T)
                        var condition = receiverType.IsNullableType() ?
                            MakeOptimizedHasValue(loweredReceiver.Syntax, loweredReceiver) :
                            _factory.ObjectNotEqual(
                                _factory.Convert(objectType, loweredReceiver),
                                _factory.Null(objectType));

                        var consequence = loweredAccessExpression;

                        result = RewriteConditionalOperator(node.Syntax,
                            condition,
                            consequence,
                            rewrittenWhenNull,
                            null,
                            nodeType);

                        if (temp != null)
                        {
                            result = _factory.Sequence(temp, result);
                        }
                    }
                    break;

                case ConditionalAccessLoweringKind.CaptureReceiverByRef:
                    // {ref T r; T v; 
                    //    r = ref receiver; 
                    //    (isClass && { v = r; r = ref v; v == null } ) ? 
                    //                                          null;
                    //                                          r.Foo()}

                    {
                        var v = _factory.SynthesizedLocal(receiverType);

                        BoundExpression captureRef = _factory.AssignmentExpression(_factory.Local(temp), loweredReceiver, refKind: RefKind.Ref);
                        BoundExpression isNull = _factory.LogicalAnd(
                                                IsClass(receiverType, objectType),
                                                _factory.Sequence(
                                                    _factory.AssignmentExpression(_factory.Local(v), _factory.Local(temp)),
                                                    _factory.AssignmentExpression(_factory.Local(temp), _factory.Local(v), RefKind.Ref),
                                                    _factory.ObjectEqual(_factory.Convert(objectType, _factory.Local(v)), _factory.Null(objectType)))
                                                );

                        result = RewriteConditionalOperator(node.Syntax,
                           isNull,
                           rewrittenWhenNull,
                           loweredAccessExpression,
                           null,
                           nodeType);

                        result = _factory.Sequence(
                                ImmutableArray.Create(temp, v),
                                captureRef,
                                result
                            );
                    }
                    break;

                case ConditionalAccessLoweringKind.DuplicateCode:
                    {
                        Debug.Assert(!receiverType.IsNullableType());

                        // if we have a class, do regular conditional access via a val temp
                        loweredReceiver = _factory.AssignmentExpression(_factory.Local(temp), loweredReceiver);
                        BoundExpression ifClass = RewriteConditionalOperator(node.Syntax,
                            _factory.ObjectNotEqual(
                                                    _factory.Convert(objectType, loweredReceiver),
                                                    _factory.Null(objectType)),
                            loweredAccessExpression,
                            rewrittenWhenNull,
                            null,
                            nodeType);

                        if (temp != null)
                        {
                            ifClass = _factory.Sequence(temp, ifClass);
                        }

                        // if we have a struct, then just access unconditionally
                        BoundExpression ifStruct = unconditionalAccess;

                        // (object)(default(T)) != null ? ifStruct: ifClass
                        result = RewriteConditionalOperator(node.Syntax,
                            IsClass(receiverType, objectType),
                            ifClass,
                            ifStruct,
                            null,
                            nodeType);
                    }
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            }

            return result;
        }

        private BoundBinaryOperator IsClass(TypeSymbol receiverType, NamedTypeSymbol objectType)
        {
            return _factory.ObjectEqual(
                                _factory.Convert(objectType, _factory.Default(receiverType)),
                                _factory.Null(objectType));
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            if (_currentConditionalAccessTarget == null)
            {
                return node;
            }

            var newtarget = _currentConditionalAccessTarget;
            if (newtarget.Type.IsNullableType())
            {
                newtarget = MakeOptimizedGetValueOrDefault(node.Syntax, newtarget);
            }

            return newtarget;
        }
    }
}
