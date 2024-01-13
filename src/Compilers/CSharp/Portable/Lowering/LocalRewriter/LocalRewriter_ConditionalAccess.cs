// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            // Never returns null when used is true.
            return RewriteConditionalAccess(node, used: true)!;
        }

        public override BoundNode VisitLoweredConditionalAccess(BoundLoweredConditionalAccess node)
        {
            throw ExceptionUtilities.Unreachable();
        }

        // null when currently enclosing conditional access node
        // is not supposed to be lowered.
        private BoundExpression? _currentConditionalAccessTarget;
        private int _currentConditionalAccessID;

        private enum ConditionalAccessLoweringKind
        {
            LoweredConditionalAccess,
            Conditional,
            ConditionalCaptureReceiverByVal
        }

        // IL gen can generate more compact code for certain conditional accesses 
        // by utilizing stack dup/pop instructions 
        internal BoundExpression? RewriteConditionalAccess(BoundConditionalAccess node, bool used)
        {
            Debug.Assert(!_inExpressionLambda);
            Debug.Assert(node.AccessExpression.Type is { });

            var loweredReceiver = this.VisitExpression(node.Receiver);
            Debug.Assert(loweredReceiver.Type is { });
            var receiverType = loweredReceiver.Type;

            // Check trivial case
            if (loweredReceiver.IsDefaultValue() && receiverType.IsReferenceType)
            {
                return _factory.Default(node.Type);
            }

            ConditionalAccessLoweringKind loweringKind;
            // dynamic receivers are not directly supported in codegen and need to be lowered to a conditional
            var lowerToConditional = node.AccessExpression.Type.IsDynamic();

            if (!lowerToConditional)
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
                loweringKind = ConditionalAccessLoweringKind.ConditionalCaptureReceiverByVal;
            }
            else
            {
                loweringKind = ConditionalAccessLoweringKind.Conditional;
            }

            var previousConditionalAccessTarget = _currentConditionalAccessTarget;
            var currentConditionalAccessID = ++_currentConditionalAccessID;

            LocalSymbol? temp = null;

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.LoweredConditionalAccess:
                    _currentConditionalAccessTarget = new BoundConditionalReceiver(
                        loweredReceiver.Syntax,
                        currentConditionalAccessID,
                        receiverType);

                    break;

                case ConditionalAccessLoweringKind.Conditional:
                    _currentConditionalAccessTarget = loweredReceiver;
                    break;

                case ConditionalAccessLoweringKind.ConditionalCaptureReceiverByVal:
                    temp = _factory.SynthesizedLocal(receiverType);
                    _currentConditionalAccessTarget = _factory.Local(temp);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(loweringKind);
            }

            BoundExpression? loweredAccessExpression;

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
            Debug.Assert(loweredAccessExpression.Type is { });
            _currentConditionalAccessTarget = previousConditionalAccessTarget;

            TypeSymbol type = this.VisitType(node.Type);

            TypeSymbol nodeType = node.Type;
            TypeSymbol accessExpressionType = loweredAccessExpression.Type;

            if (accessExpressionType.IsVoidType())
            {
                type = nodeType = accessExpressionType;
            }

            if (!TypeSymbol.Equals(accessExpressionType, nodeType, TypeCompareKind.ConsiderEverything2) && nodeType.IsNullableType())
            {
                Debug.Assert(TypeSymbol.Equals(accessExpressionType, nodeType.GetNullableUnderlyingType(), TypeCompareKind.ConsiderEverything2));
                loweredAccessExpression = _factory.New((NamedTypeSymbol)nodeType, loweredAccessExpression);
            }
            else
            {
                Debug.Assert(TypeSymbol.Equals(accessExpressionType, nodeType, TypeCompareKind.ConsiderEverything2) ||
                    (nodeType.IsVoidType() && !used));
            }

            BoundExpression result;
            var objectType = _compilation.GetSpecialType(SpecialType.System_Object);

            switch (loweringKind)
            {
                case ConditionalAccessLoweringKind.LoweredConditionalAccess:
                    Debug.Assert(loweredReceiver.Type is { });
                    result = new BoundLoweredConditionalAccess(
                        node.Syntax,
                        loweredReceiver,
                        receiverType.IsNullableType() ?
                                 UnsafeGetNullableMethod(node.Syntax, loweredReceiver.Type, SpecialMember.System_Nullable_T_get_HasValue) :
                                 null,
                        loweredAccessExpression,
                        null,
                        currentConditionalAccessID,
                        forceCopyOfNullableValueType: true,
                        type);

                    break;

                case ConditionalAccessLoweringKind.ConditionalCaptureReceiverByVal:
                    // capture the receiver into a temp
                    Debug.Assert(temp is { });
                    loweredReceiver = _factory.MakeSequence(
                                            _factory.AssignmentExpression(_factory.Local(temp), loweredReceiver),
                                            _factory.Local(temp));

                    goto case ConditionalAccessLoweringKind.Conditional;

                case ConditionalAccessLoweringKind.Conditional:
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
                            nodeType,
                            isRef: false);

                        if (temp != null)
                        {
                            result = _factory.MakeSequence(temp, result);
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
            Debug.Assert(newtarget is { Type: { } });

            if (newtarget.Type.IsNullableType())
            {
                newtarget = MakeOptimizedGetValueOrDefault(node.Syntax, newtarget);
            }

            return newtarget;
        }
    }
}
