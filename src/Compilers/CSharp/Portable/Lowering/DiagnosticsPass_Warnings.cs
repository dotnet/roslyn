// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This pass detects and reports diagnostics that do not affect lambda convertibility.
    /// This part of the partial class focuses on expression and operator warnings.
    /// </summary>
    internal sealed partial class DiagnosticsPass : BoundTreeWalkerWithStackGuard
    {
        private void CheckArguments(ImmutableArray<RefKind> argumentRefKindsOpt, ImmutableArray<BoundExpression> arguments, Symbol method)
        {
            if (!argumentRefKindsOpt.IsDefault)
            {
                Debug.Assert(arguments.Length == argumentRefKindsOpt.Length);
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (argumentRefKindsOpt[i] != RefKind.None && arguments[i].Kind == BoundKind.FieldAccess)
                    {
                        CheckFieldAddress((BoundFieldAccess)arguments[i], method);
                    }
                }
            }
        }

        /// <remarks>
        /// This is for when we are taking the address of a field.
        /// Distinguish from <see cref="CheckFieldAsReceiver"/>.
        /// </remarks>
        private void CheckFieldAddress(BoundFieldAccess fieldAccess, Symbol consumerOpt)
        {
            FieldSymbol fieldSymbol = fieldAccess.FieldSymbol;

            // We can safely suppress this warning when calling an Interlocked API
            if (fieldSymbol.IsVolatile && ((object)consumerOpt == null || !IsInterlockedAPI(consumerOpt)))
            {
                Error(ErrorCode.WRN_VolatileByRef, fieldAccess, fieldSymbol);
            }

            if (IsNonAgileFieldAccess(fieldAccess, _compilation))
            {
                Error(ErrorCode.WRN_ByRefNonAgileField, fieldAccess, fieldSymbol);
            }
        }

        /// <remarks>
        /// This is for when we are dotting into a field.
        /// Distinguish from <see cref="CheckFieldAddress"/>.
        /// 
        /// NOTE: dev11 also calls this on string initializers in fixed statements,
        /// but never accomplishes anything since string is a reference type.  This
        /// is probably a bug, but fixing it would be a breaking change.
        /// </remarks>
        private void CheckFieldAsReceiver(BoundFieldAccess fieldAccess)
        {
            // From ExpressionBinder.cpp:
            //   Taking the address of a field is suspect if the type is marshalbyref.
            //   REVIEW ShonK: Is this really the best way to handle this? It'd be so much more
            //   bullet proof for ilgen to error when it spits out the ldflda....

            FieldSymbol fieldSymbol = fieldAccess.FieldSymbol;

            if (IsNonAgileFieldAccess(fieldAccess, _compilation) && !fieldSymbol.Type.IsReferenceType)
            {
                Error(ErrorCode.WRN_CallOnNonAgileField, fieldAccess, fieldSymbol);
            }
        }

        private void CheckReceiverIfField(BoundExpression receiverOpt)
        {
            if (receiverOpt != null && receiverOpt.Kind == BoundKind.FieldAccess)
            {
                CheckFieldAsReceiver((BoundFieldAccess)receiverOpt);
            }
        }

        /// <remarks>
        /// Based on OutputContext::IsNonAgileField.
        /// </remarks>
        internal static bool IsNonAgileFieldAccess(BoundFieldAccess fieldAccess, CSharpCompilation compilation)
        {
            // Warn if taking the address of a non-static field with a receiver other than this (possibly cast)
            // and a type that descends from System.MarshalByRefObject.
            if (IsInstanceFieldAccessWithNonThisReceiver(fieldAccess))
            {
                // NOTE: We're only trying to produce a warning, so there's no point in producing an
                // error if the well-known type we need for the check is missing.
                NamedTypeSymbol marshalByRefType = compilation.GetWellKnownType(WellKnownType.System_MarshalByRefObject);

                TypeSymbol baseType = fieldAccess.FieldSymbol.ContainingType;
                while ((object)baseType != null)
                {
                    if (baseType == marshalByRefType)
                    {
                        return true;
                    }

                    // NOTE: We're only trying to produce a warning, so there's no point in producing a
                    // use site diagnostic if we can't walk up the base type hierarchy.
                    baseType = baseType.BaseTypeNoUseSiteDiagnostics;
                }
            }

            return false;
        }

        private static bool IsInstanceFieldAccessWithNonThisReceiver(BoundFieldAccess fieldAccess)
        {
            BoundExpression receiver = fieldAccess.ReceiverOpt;
            if (receiver == null || fieldAccess.FieldSymbol.IsStatic)
            {
                return false;
            }

            while (receiver.Kind == BoundKind.Conversion)
            {
                BoundConversion conversion = (BoundConversion)receiver;
                if (conversion.ExplicitCastInCode) break;
                receiver = conversion.Operand;
            }

            return receiver.Kind != BoundKind.ThisReference && receiver.Kind != BoundKind.BaseReference;
        }

        private bool IsInterlockedAPI(Symbol method)
        {
            var interlocked = _compilation.GetWellKnownType(WellKnownType.System_Threading_Interlocked);
            if ((object)interlocked != null && interlocked == method.ContainingType)
                return true;

            return false;
        }

        private static BoundExpression StripImplicitCasts(BoundExpression expr)
        {
            BoundExpression current = expr;
            while (true)
            {
                // CONSIDER: Dev11 doesn't strip conversions to float or double.
                BoundConversion conversion = current as BoundConversion;
                if (conversion == null || !conversion.ConversionKind.IsImplicitConversion())
                {
                    return current;
                }

                current = conversion.Operand;
            }
        }

        private static bool IsSameLocalOrField(BoundExpression expr1, BoundExpression expr2)
        {
            if (expr1 == null && expr2 == null)
            {
                return true;
            }

            if (expr1 == null || expr2 == null)
            {
                return false;
            }

            if (expr1.HasAnyErrors || expr2.HasAnyErrors)
            {
                return false;
            }

            expr1 = StripImplicitCasts(expr1);
            expr2 = StripImplicitCasts(expr2);

            if (expr1.Kind != expr2.Kind)
            {
                return false;
            }

            switch (expr1.Kind)
            {
                case BoundKind.Local:
                    var local1 = expr1 as BoundLocal;
                    var local2 = expr2 as BoundLocal;
                    return local1.LocalSymbol == local2.LocalSymbol;
                case BoundKind.FieldAccess:
                    var field1 = expr1 as BoundFieldAccess;
                    var field2 = expr2 as BoundFieldAccess;
                    return field1.FieldSymbol == field2.FieldSymbol &&
                        (field1.FieldSymbol.IsStatic || IsSameLocalOrField(field1.ReceiverOpt, field2.ReceiverOpt));
                case BoundKind.EventAccess:
                    var event1 = expr1 as BoundEventAccess;
                    var event2 = expr2 as BoundEventAccess;
                    return event1.EventSymbol == event2.EventSymbol &&
                        (event1.EventSymbol.IsStatic || IsSameLocalOrField(event1.ReceiverOpt, event2.ReceiverOpt));
                case BoundKind.Parameter:
                    var param1 = expr1 as BoundParameter;
                    var param2 = expr2 as BoundParameter;
                    return param1.ParameterSymbol == param2.ParameterSymbol;
                case BoundKind.RangeVariable:
                    var rangeVar1 = expr1 as BoundRangeVariable;
                    var rangeVar2 = expr2 as BoundRangeVariable;
                    return rangeVar1.RangeVariableSymbol == rangeVar2.RangeVariableSymbol;
                case BoundKind.ThisReference:
                case BoundKind.PreviousSubmissionReference:
                case BoundKind.HostObjectMemberReference:
                    Debug.Assert(expr1.Type == expr2.Type);
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsComCallWithRefOmitted(MethodSymbol method, ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> argumentRefKindsOpt)
        {
            if (method.ParameterCount != arguments.Length ||
                (object)method.ContainingType == null ||
                !method.ContainingType.IsComImport)
                return false;

            for (int i = 0; i < arguments.Length; i++)
            {
                if (method.Parameters[i].RefKind != RefKind.None && (argumentRefKindsOpt.IsDefault || argumentRefKindsOpt[i] == RefKind.None)) return true;
            }

            return false;
        }

        private void CheckBinaryOperator(BoundBinaryOperator node)
        {
            if ((object)node.MethodOpt == null)
            {
                CheckUnsafeType(node.Left);
                CheckUnsafeType(node.Right);
            }

            CheckForBitwiseOrSignExtend(node, node.OperatorKind, node.Left, node.Right);
            CheckNullableNullBinOp(node);
            CheckLiftedBinOp(node);
            CheckRelationals(node);
            CheckDynamic(node);
        }

        private void CheckCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            BoundExpression left = node.Left;

            if (!node.Operator.Kind.IsDynamic() && !node.LeftConversion.IsIdentity && node.LeftConversion.Exists)
            {
                // Need to represent the implicit conversion as a node in order to be able to produce correct diagnostics.
                left = new BoundConversion(left.Syntax, left, node.LeftConversion, node.Operator.Kind.IsChecked(),
                                           explicitCastInCode: false, constantValueOpt: null, type: node.Operator.LeftType);
            }

            CheckForBitwiseOrSignExtend(node, node.Operator.Kind, left, node.Right);
            CheckLiftedCompoundAssignment(node);

            if (_inExpressionLambda)
            {
                Error(ErrorCode.ERR_ExpressionTreeContainsAssignment, node);
            }
        }

        private void CheckRelationals(BoundBinaryOperator node)
        {
            Debug.Assert(node != null);

            if (!node.OperatorKind.IsComparison())
            {
                return;
            }

            // Don't bother to check vacuous comparisons where both sides are constant, eg, where someone
            // is doing something like "if (0xFFFFFFFFU == 0)" -- these are likely to be machine-
            // generated code. 

            if (node.Left.ConstantValue != null && node.Right.ConstantValue == null && node.Right.Kind == BoundKind.Conversion)
            {
                CheckVacuousComparisons(node, node.Left.ConstantValue, node.Right);
            }

            if (node.Right.ConstantValue != null && node.Left.ConstantValue == null && node.Left.Kind == BoundKind.Conversion)
            {
                CheckVacuousComparisons(node, node.Right.ConstantValue, node.Left);
            }

            if (node.OperatorKind == BinaryOperatorKind.ObjectEqual || node.OperatorKind == BinaryOperatorKind.ObjectNotEqual)
            {
                TypeSymbol t;
                if (node.Left.Type.SpecialType == SpecialType.System_Object && !IsExplicitCast(node.Left) && !(node.Left.ConstantValue != null && node.Left.ConstantValue.IsNull) && ConvertedHasEqual(node.OperatorKind, node.Right, out t))
                {
                    // Possible unintended reference comparison; to get a value comparison, cast the left hand side to type '{0}'
                    _diagnostics.Add(ErrorCode.WRN_BadRefCompareLeft, node.Syntax.Location, t);
                }
                else if (node.Right.Type.SpecialType == SpecialType.System_Object && !IsExplicitCast(node.Right) && !(node.Right.ConstantValue != null && node.Right.ConstantValue.IsNull) && ConvertedHasEqual(node.OperatorKind, node.Left, out t))
                {
                    // Possible unintended reference comparison; to get a value comparison, cast the right hand side to type '{0}'
                    _diagnostics.Add(ErrorCode.WRN_BadRefCompareRight, node.Syntax.Location, t);
                }
            }

            CheckSelfComparisons(node);
        }

        private static bool IsExplicitCast(BoundExpression node)
        {
            return node.Kind == BoundKind.Conversion && ((BoundConversion)node).ExplicitCastInCode;
        }

        private static bool ConvertedHasEqual(BinaryOperatorKind oldOperatorKind, BoundNode node, out TypeSymbol type)
        {
            type = null;
            if (node.Kind != BoundKind.Conversion) return false;
            var conv = (BoundConversion)node;
            if (conv.ExplicitCastInCode) return false;
            NamedTypeSymbol nt = conv.Operand.Type as NamedTypeSymbol;
            if ((object)nt == null || !nt.IsReferenceType) return false;
            string opName = (oldOperatorKind == BinaryOperatorKind.ObjectEqual) ? WellKnownMemberNames.EqualityOperatorName : WellKnownMemberNames.InequalityOperatorName;
            for (var t = nt; (object)t != null; t = t.BaseTypeNoUseSiteDiagnostics)
            {
                foreach (var sym in t.GetMembers(opName))
                {
                    MethodSymbol op = sym as MethodSymbol;
                    if ((object)op == null || op.MethodKind != MethodKind.UserDefinedOperator) continue;
                    var parameters = op.GetParameters();
                    if (parameters.Length == 2 && parameters[0].Type.TypeSymbol == t && parameters[1].Type.TypeSymbol == t)
                    {
                        type = t;
                        return true;
                    }
                }
            }

            return false;
        }

        private void CheckSelfComparisons(BoundBinaryOperator node)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.OperatorKind.IsComparison());

            if (!node.HasAnyErrors && IsSameLocalOrField(node.Left, node.Right))
            {
                Error(ErrorCode.WRN_ComparisonToSelf, node);
            }
        }

        private void CheckVacuousComparisons(BoundBinaryOperator tree, ConstantValue constantValue, BoundNode operand)
        {
            Debug.Assert(tree != null);
            Debug.Assert(constantValue != null);
            Debug.Assert(operand != null);

            // We wish to detect comparisons between integers and constants which are likely to be wrong
            // because we know at compile time whether they will be true or false. For example:
            // 
            // const short s = 1000;
            // byte b = whatever;
            // if (b < s)
            //
            // We know that this will always be true because when b and s are both converted to int for
            // the comparison, the left side will always be less than the right side. 
            //
            // We only give the warning if there is no explicit conversion involved on the operand. 
            // For example, if we had:
            //
            // const uint s = 1000;
            // sbyte b = whatever; 
            // if ((byte)b < s)
            //
            // Then we do not give a warning.
            //
            // Note that the native compiler has what looks to be some dead code. It checks to see
            // if the conversion on the operand is from an enum type. But this is unnecessary if
            // we are rejecting cases with explicit conversions. The only possible cases are:
            //
            // enum == enumConstant           -- enum types must be the same, so it must be in range.
            // enum == integralConstant       -- not legal unless the constant is zero, which is in range.
            // enum == (ENUM)anyConstant      -- if the constant is out of range then this is not legal in the first place
            //                                   unless we're in an unchecked context, in which case, the user probably does 
            //                                   not want the warning.
            // integral == enumConstant       -- never legal in the first place
            //
            // Since it seems pointless to try to check enums, we simply look for vacuous comparisons of
            // integral types here.

            for (BoundConversion conversion = operand as BoundConversion;
                conversion != null;
                conversion = conversion.Operand as BoundConversion)
            {
                if (conversion.ConversionKind != ConversionKind.ImplicitNumeric &&
                    conversion.ConversionKind != ConversionKind.ImplicitConstant)
                {
                    return;
                }

                // As in dev11, we don't dig through explicit casts (see ExpressionBinder::WarnAboutBadRelationals).
                if (conversion.ExplicitCastInCode)
                {
                    return;
                }

                if (!conversion.Operand.Type.SpecialType.IsIntegralType() || !conversion.Type.SpecialType.IsIntegralType())
                {
                    return;
                }

                if (!Binder.CheckConstantBounds(conversion.Operand.Type.SpecialType, constantValue))
                {
                    Error(ErrorCode.WRN_VacuousIntegralComp, tree, conversion.Operand.Type);
                    return;
                }
            }
        }

        private void CheckForBitwiseOrSignExtend(BoundExpression node, BinaryOperatorKind operatorKind, BoundExpression leftOperand, BoundExpression rightOperand)
        {
            // We wish to give a warning for situations where an unexpected sign extension wipes
            // out some bits. For example:
            //
            // int x = 0x0ABC0000;
            // short y = -2; // 0xFFFE
            // int z = x | y;
            //
            // The user might naively expect the result to be 0x0ABCFFFE. But the short is sign-extended
            // when it is converted to int before the bitwise or, so this is in fact the same as:
            //
            // int x = 0x0ABC0000;
            // short y = -2; // 0xFFFE
            // int ytemp = y; // 0xFFFFFFFE
            // int z = x | ytemp; 
            //
            // Which gives 0xFFFFFFFE, not 0x0ABCFFFE.
            //
            // However, we wish to suppress the warning if:
            //
            // * The sign extension is "expected" -- for instance, because there was an explicit cast 
            //   from short to int:  "int z = x | (int)y;" should not produce the warning.
            //   Note that "uint z = (uint)x | (uint)y;" should still produce the warning because 
            //   the user might not realize that converting y to uint does a sign extension.
            //   
            // * There is the same amount of sign extension on both sides. For example, when
            //   doing "short | sbyte", both sides are sign extended. The left creates two FF bytes
            //   and the right creates three, so we are potentially wiping out information from the
            //   left side. But "short | short" adds two FF bytes on both sides, so no information is lost.
            //
            // The native compiler also suppresses this warning in a bizarre and inconsistent way. If
            // the side whose bits are going to be wiped out by sign extension is a *constant*, then the
            // warning is suppressed *if the constant, when converted to a signed long, fits into the 
            // range of the type that is being sign-extended.* 
            //
            // Consider the effects of this rule:
            //
            // (uint)0xFFFF0000 | y -- gives the warning because 0xFFFF0000 as a long is not in the range of a short, 
            //                         *even though the result will not be affected by the sign extension*.
            // (ulong)0xFFFFFFFFFFFFFFFF | y -- suppresses the warning, because 0xFFFFFFFFFFFFFFFF as a signed long fits into a short.
            // (int)0x0000ABCD | y -- suppresses the warning, even though the 0000 is going to be wiped out by the sign extension.
            //
            // It seems clear that the intention of the heuristic is to *suppress the warning when the bits being hammered
            // on are either all zero, or all one.*  Therefore that is the heuristic we will *actually* implement here.
            //

            switch (operatorKind)
            {
                case BinaryOperatorKind.LiftedUIntOr:
                case BinaryOperatorKind.LiftedIntOr:
                case BinaryOperatorKind.LiftedULongOr:
                case BinaryOperatorKind.LiftedLongOr:
                case BinaryOperatorKind.UIntOr:
                case BinaryOperatorKind.IntOr:
                case BinaryOperatorKind.ULongOr:
                case BinaryOperatorKind.LongOr:
                    break;
                default:
                    return;
            }

            // The native compiler skips this warning if both sides of the operator are constants.
            //
            // CONSIDER: Is that sensible? It seems reasonable that if we would warn on int | short
            // when they are non-constants, or when one is a constant, that we would similarly warn 
            // when both are constants.

            if (node.ConstantValue != null)
            {
                return;
            }

            // Start by determining *which bits on each side are going to be unexpectedly turned on*.

            ulong left = FindSurprisingSignExtensionBits(leftOperand);
            ulong right = FindSurprisingSignExtensionBits(rightOperand);

            // If they are all the same then there's no warning to give.

            if (left == right)
            {
                return;
            }

            // Suppress the warning if one side is a constant, and either all the unexpected
            // bits are already off, or all the unexpected bits are already on.

            ConstantValue constVal = GetConstantValueForBitwiseOrCheck(leftOperand);
            if (constVal != null)
            {
                ulong val = constVal.UInt64Value;
                if ((val & right) == right || (~val & right) == right)
                {
                    return;
                }
            }

            constVal = GetConstantValueForBitwiseOrCheck(rightOperand);
            if (constVal != null)
            {
                ulong val = constVal.UInt64Value;
                if ((val & left) == left || (~val & left) == left)
                {
                    return;
                }
            }

            // CS0675: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
            Error(ErrorCode.WRN_BitwiseOrSignExtend, node);
        }

        private static ConstantValue GetConstantValueForBitwiseOrCheck(BoundExpression operand)
        {
            // We might have a nullable conversion on top of an integer constant. But only dig out
            // one level.

            if (operand.Kind == BoundKind.Conversion)
            {
                BoundConversion conv = (BoundConversion)operand;
                if (conv.ConversionKind == ConversionKind.ImplicitNullable)
                {
                    operand = conv.Operand;
                }
            }

            ConstantValue constVal = operand.ConstantValue;

            if (constVal == null || !constVal.IsIntegral)
            {
                return null;
            }

            return constVal;
        }

        // A "surprising" sign extension is:
        //
        // * a conversion with no cast in source code that goes from a smaller
        //   signed type to a larger signed or unsigned type.
        //
        // * an conversion (with or without a cast) from a smaller
        //   signed type to a larger unsigned type.

        private static ulong FindSurprisingSignExtensionBits(BoundExpression expr)
        {
            if (expr.Kind != BoundKind.Conversion)
            {
                return 0;
            }

            BoundConversion conv = (BoundConversion)expr;
            TypeSymbol from = conv.Operand.Type;
            TypeSymbol to = conv.Type;

            if ((object)from == null || (object)to == null)
            {
                return 0;
            }

            if (from.IsNullableType())
            {
                from = from.GetNullableUnderlyingType();
            }

            if (to.IsNullableType())
            {
                to = to.GetNullableUnderlyingType();
            }

            SpecialType fromSpecialType = from.SpecialType;
            SpecialType toSpecialType = to.SpecialType;

            if (!fromSpecialType.IsIntegralType() || !toSpecialType.IsIntegralType())
            {
                return 0;
            }

            int fromSize = fromSpecialType.SizeInBytes();
            int toSize = toSpecialType.SizeInBytes();

            if (fromSize == 0 || toSize == 0)
            {
                return 0;
            }

            // The operand might itself be a conversion, and might be contributing
            // surprising bits. We might have more, fewer or the same surprising bits
            // as the operand.

            ulong recursive = FindSurprisingSignExtensionBits(conv.Operand);

            if (fromSize == toSize)
            {
                // No change.
                return recursive;
            }

            if (toSize < fromSize)
            {
                // We are casting from a larger type to a smaller type, and are therefore
                // losing surprising bits. 
                switch (toSize)
                {
                    case 1: return unchecked((ulong)(byte)recursive);
                    case 2: return unchecked((ulong)(ushort)recursive);
                    case 4: return unchecked((ulong)(uint)recursive);
                }
                Debug.Assert(false, "How did we get here?");
                return recursive;
            }

            // We are converting from a smaller type to a larger type, and therefore might
            // be adding surprising bits. First of all, the smaller type has got to be signed
            // for there to be sign extension.

            bool fromSigned = fromSpecialType.IsSignedIntegralType();

            if (!fromSigned)
            {
                return recursive;
            }

            // OK, we know that the "from" type is a signed integer that is smaller than the
            // "to" type, so we are going to have sign extension. Is it surprising? The only
            // time that sign extension is *not* surprising is when we have a cast operator
            // to a *signed* type. That is, (int)myShort is not a surprising sign extension.

            if (conv.ExplicitCastInCode && toSpecialType.IsSignedIntegralType())
            {
                return recursive;
            }

            // Note that we *could* be somewhat more clever here. Consider the following edge case:
            //
            // (ulong)(int)(uint)(ushort)mySbyte
            //
            // We could reason that the sbyte-to-ushort conversion is going to add one byte of
            // unexpected sign extension. The conversion from ushort to uint adds no more bytes.
            // The conversion from uint to int adds no more bytes. Does the conversion from int
            // to ulong add any more bytes of unexpected sign extension? Well, no, because we 
            // know that the previous conversion from ushort to uint will ensure that the top bit
            // of the uint is off! 
            //
            // But we are not going to try to be that clever. In the extremely unlikely event that
            // someone does this, we will record that the unexpectedly turned-on bits are 
            // 0xFFFFFFFF0000FF00, even though we could in theory deduce that only 0x000000000000FF00
            // are the unexpected bits.

            ulong result = recursive;
            for (int i = fromSize; i < toSize; ++i)
            {
                result |= (0xFFUL) << (i * 8);
            }

            return result;
        }

        private void CheckLiftedCompoundAssignment(BoundCompoundAssignmentOperator node)
        {
            Debug.Assert(node != null);
            if (!node.Operator.Kind.IsLifted())
            {
                return;
            }

            // CS0458: The result of the expression is always 'null' of type '{0}'
            if (node.Right.NullableNeverHasValue())
            {
                Error(ErrorCode.WRN_AlwaysNull, node, node.Type);
            }
        }

        private void CheckLiftedUnaryOp(BoundUnaryOperator node)
        {
            Debug.Assert(node != null);

            if (!node.OperatorKind.IsLifted())
            {
                return;
            }

            // CS0458: The result of the expression is always 'null' of type '{0}'
            if (node.Operand.NullableNeverHasValue())
            {
                Error(ErrorCode.WRN_AlwaysNull, node, node.Type);
            }
        }

        private void CheckNullableNullBinOp(BoundBinaryOperator node)
        {
            if ((node.OperatorKind & BinaryOperatorKind.NullableNull) == 0)
            {
                return;
            }

            switch (node.OperatorKind.Operator())
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    // CS0472: The result of the expression is always '{0}' since a value of type '{1}' is never equal to 'null' of type '{2}'
                    //
                    // Produce the warning if one side is always null and the other is never null.
                    // That is, we have something like "if (myInt == null)"

                    string always = node.OperatorKind.Operator() == BinaryOperatorKind.NotEqual ? "true" : "false";

                    // we use a separate warning code for cases newly detected in later versions of the compiler
                    if (node.Right.IsLiteralNull() && node.Left.NullableAlwaysHasValue())
                    {
                        Error(ErrorCode.WRN_NubExprIsConstBool, node, always, node.Left.Type.GetNullableUnderlyingType(), node.Left.Type);
                    }
                    else if (node.Left.IsLiteralNull() && node.Right.NullableAlwaysHasValue())
                    {
                        Error(ErrorCode.WRN_NubExprIsConstBool, node, always, node.Right.Type.GetNullableUnderlyingType(), node.Right.Type);
                    }
                    break;
            }
        }

        private void CheckLiftedBinOp(BoundBinaryOperator node)
        {
            Debug.Assert(node != null);

            if (!node.OperatorKind.IsLifted())
            {
                return;
            }

            switch (node.OperatorKind.Operator())
            {
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                    // CS0464: Comparing with null of type '{0}' always produces 'false'
                    //
                    // Produce the warning if one (or both) sides are always null.
                    if (node.Right.NullableNeverHasValue())
                    {
                        Error(ErrorCode.WRN_CmpAlwaysFalse, node, GetTypeForLiftedComparisonWarning(node.Right));
                    }
                    else if (node.Left.NullableNeverHasValue())
                    {
                        Error(ErrorCode.WRN_CmpAlwaysFalse, node, GetTypeForLiftedComparisonWarning(node.Left));
                    }
                    break;
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    // CS0472: The result of the expression is always '{0}' since a value of type '{1}' is never equal to 'null' of type '{2}'
                    //
                    // Produce the warning if one side is always null and the other is never null.
                    // That is, we have something like "if (myInt == null)"

                    string always = node.OperatorKind.Operator() == BinaryOperatorKind.NotEqual ? "true" : "false";

                    if (_compilation.FeatureStrictEnabled || !node.OperatorKind.IsUserDefined())
                    {
                        if (node.Right.NullableNeverHasValue() && node.Left.NullableAlwaysHasValue())
                        {
                            Error(node.OperatorKind.IsUserDefined() ? ErrorCode.WRN_NubExprIsConstBool2 : ErrorCode.WRN_NubExprIsConstBool, node, always, node.Left.Type.GetNullableUnderlyingType(), GetTypeForLiftedComparisonWarning(node.Right));
                        }
                        else if (node.Left.NullableNeverHasValue() && node.Right.NullableAlwaysHasValue())
                        {
                            Error(node.OperatorKind.IsUserDefined() ? ErrorCode.WRN_NubExprIsConstBool2 : ErrorCode.WRN_NubExprIsConstBool, node, always, node.Right.Type.GetNullableUnderlyingType(), GetTypeForLiftedComparisonWarning(node.Left));
                        }
                    }
                    break;
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.And:
                    // CS0458: The result of the expression is always 'null' of type '{0}'
                    if ((node.Left.NullableNeverHasValue() && node.Right.IsNullableNonBoolean()) ||
                        (node.Left.IsNullableNonBoolean() && node.Right.NullableNeverHasValue()))
                        Error(ErrorCode.WRN_AlwaysNull, node, node.Type);
                    break;
                default:
                    // CS0458: The result of the expression is always 'null' of type '{0}'
                    if (node.Right.NullableNeverHasValue() || node.Left.NullableNeverHasValue())
                    {
                        Error(ErrorCode.WRN_AlwaysNull, node, node.Type);
                    }
                    break;
            }
        }

        private void CheckLiftedUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            // CS0458: The result of the expression is always 'null' of type '{0}'
            if (node.Right.NullableNeverHasValue() || node.Left.NullableNeverHasValue())
            {
                Error(ErrorCode.WRN_AlwaysNull, node, node.Type);
            }
        }

        private static TypeSymbol GetTypeForLiftedComparisonWarning(BoundExpression node)
        {
            // If we have something like "10 < new sbyte?()" we bind that as 
            // (int?)10 < (int?)(new sbyte?()) 
            // but the warning we want to produce is that the null on the right hand
            // side is of type sbyte?, not int?. 

            if ((object)node.Type == null || !node.Type.IsNullableType())
            {
                return null;
            }

            TypeSymbol type = null;

            if (node.Kind == BoundKind.Conversion)
            {
                var conv = (BoundConversion)node;
                if (conv.ConversionKind == ConversionKind.ExplicitNullable || conv.ConversionKind == ConversionKind.ImplicitNullable)
                {
                    type = GetTypeForLiftedComparisonWarning(conv.Operand);
                }
            }

            return type ?? node.Type;
        }

        private bool CheckForAssignmentToSelf(BoundAssignmentOperator node)
        {
            if (!node.HasAnyErrors && IsSameLocalOrField(node.Left, node.Right))
            {
                Error(ErrorCode.WRN_AssignmentToSelf, node);
                return true;
            }
            return false;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            CheckReceiverIfField(node.ReceiverOpt);
            return base.VisitFieldAccess(node);
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            CheckReceiverIfField(node.ReceiverOpt);
            return base.VisitPropertyAccess(node);
        }

        public override BoundNode VisitPropertyGroup(BoundPropertyGroup node)
        {
            CheckReceiverIfField(node.ReceiverOpt);
            return base.VisitPropertyGroup(node);
        }
    }
}
