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
        private BoundExpression _currentConditionalAccessTarget;
        private int _currentConditionalAccessID;

        private enum ConditionalAccessLoweringKind
        {
            LoweredConditionalAccess,
            Ternary,
            TernaryCaptureReceiverByVal
        }

        // IL gen can generate more compact code for certain conditional accesses 
        // by utilizing stack dup/pop instructions 
        internal BoundExpression RewriteConditionalAccess(BoundConditionalAccess node, bool used)
        {
            Debug.Assert(!_inExpressionLambda);

            var loweredReceiver = this.VisitExpression(node.Receiver);
            var receiverType = loweredReceiver.Type;

            // Check trivial case
            if (loweredReceiver.IsDefaultValue())
            {
                return _factory.Default(node.Type);
            }

            ConditionalAccessLoweringKind loweringKind;
            // dynamic receivers are not directly supported in codegen and need to be lowered to a ternary
            var lowerToTernary = node.AccessExpression.Type.IsDynamic();

            if (!lowerToTernary)
            {
                // trivial cases are directly supported in IL gen
                loweringKind = ConditionalAccessLoweringKind.LoweredConditionalAccess;
            }
            else if (CanChangeValueBetweenReads(loweredReceiver))
            {
                // NOTE: dynamic operations historically do not propagate mutations
                // to the receiver if that happens to be a value type
                // so we can capture receiver by value in dynamic case regardless of 
                // the type of receiver
                // Nullable receivers are immutable so should be captured by value as well.
                loweringKind = ConditionalAccessLoweringKind.TernaryCaptureReceiverByVal;
            }
            else
            {
                loweringKind = ConditionalAccessLoweringKind.Ternary;
            }


            var previousConditionalAccessTarget = _currentConditionalAccessTarget;
            var currentConditionalAccessID = ++_currentConditionalAccessID;

            LocalSymbol temp = null;
            BoundExpression unconditionalAccess = null;

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.LoweredConditionalAccess:
                    _currentConditionalAccessTarget = new BoundConditionalReceiver(
                        loweredReceiver.Syntax,
                        currentConditionalAccessID,
                        receiverType);

                    break;

                case ConditionalAccessLoweringKind.Ternary:
                    _currentConditionalAccessTarget = loweredReceiver;
                    break;

                case ConditionalAccessLoweringKind.TernaryCaptureReceiverByVal:
                    temp = _factory.SynthesizedLocal(receiverType);
                    _currentConditionalAccessTarget = _factory.Local(temp);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(loweringKind);
            }

            BoundExpression loweredAccessExpression;
            
            if (used)
            {
                loweredAccessExpression = this.VisitExpression(node.AccessExpression);
            }
            else
            {
                loweredAccessExpression = this.VisitUnusedExpression(node.AccessExpression);
                if (loweredAccessExpression == null)
                {
                    return null;
                }
            }

            Debug.Assert(loweredAccessExpression != null);
            _currentConditionalAccessTarget = previousConditionalAccessTarget;

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

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.LoweredConditionalAccess:
                    result = new BoundLoweredConditionalAccess(
                        node.Syntax,
                        loweredReceiver,
                        receiverType.IsNullableType() ?
                                 GetNullableMethod(node.Syntax, loweredReceiver.Type, SpecialMember.System_Nullable_T_get_HasValue) :
                                 null,
                        loweredAccessExpression,
                        null,
                        currentConditionalAccessID,
                        type);

                    break;

                case ConditionalAccessLoweringKind.TernaryCaptureReceiverByVal:
                    // capture the receiver into a temp
                    loweredReceiver = _factory.Sequence(
                                            _factory.AssignmentExpression(_factory.Local(temp), loweredReceiver),
                                            _factory.Local(temp));

                    goto case ConditionalAccessLoweringKind.Ternary;

                case ConditionalAccessLoweringKind.Ternary:
                    {
                        // (object)r != null ? access : default(T)
                        var condition = _factory.ObjectNotEqual(
                                _factory.Convert(objectType, loweredReceiver),
                                _factory.Null(objectType));

                        var consequence = loweredAccessExpression;

                        result = RewriteConditionalOperator(node.Syntax,
                            condition,
                            consequence,
                            _factory.Default(nodeType),
                            null,
                            nodeType);

                        if (temp != null)
                        {
                            result = _factory.Sequence(temp, result);
                        }
                    }
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(loweringKind);
            }

            return result;
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            var newtarget = _currentConditionalAccessTarget;

            if (newtarget.Type.IsNullableType())
            {
                newtarget = MakeOptimizedGetValueOrDefault(node.Syntax, newtarget);
            }

            return newtarget;
        }
    }
}
