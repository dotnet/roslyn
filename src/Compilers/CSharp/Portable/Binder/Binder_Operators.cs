// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private BoundExpression BindCompoundAssignment(AssignmentExpressionSyntax node, DiagnosticBag diagnostics)
        {
            node.Left.CheckDeconstructionCompatibleArgument(diagnostics);

            BoundExpression left = BindValue(node.Left, diagnostics, GetBinaryAssignmentKind(node.Kind()));
            ReportSuppressionIfNeeded(left, diagnostics);
            BoundExpression right = BindValue(node.Right, diagnostics, BindValueKind.RValue);
            BinaryOperatorKind kind = SyntaxKindToBinaryOperatorKind(node.Kind());

            // If either operand is bad, don't try to do binary operator overload resolution; that will just
            // make cascading errors.

            if (left.Kind == BoundKind.EventAccess)
            {
                BinaryOperatorKind kindOperator = kind.Operator();
                switch (kindOperator)
                {
                    case BinaryOperatorKind.Addition:
                    case BinaryOperatorKind.Subtraction:
                        return BindEventAssignment(node, (BoundEventAccess)left, right, kindOperator, diagnostics);

                        // fall-through for other operators, if RHS is dynamic we produce dynamic operation, otherwise we'll report an error ...
                }
            }

            if (left.HasAnyErrors || right.HasAnyErrors)
            {
                // NOTE: no overload resolution candidates.
                return new BoundCompoundAssignmentOperator(node, BinaryOperatorSignature.Error, left, right,
                    Conversion.NoConversion, Conversion.NoConversion, LookupResultKind.Empty, CreateErrorType(), hasErrors: true);
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (left.HasDynamicType() || right.HasDynamicType())
            {
                if (IsLegalDynamicOperand(right) && IsLegalDynamicOperand(left))
                {
                    left = BindToNaturalType(left, diagnostics);
                    right = BindToNaturalType(right, diagnostics);
                    var finalDynamicConversion = this.Compilation.Conversions.ClassifyConversionFromExpression(right, left.Type, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);

                    return new BoundCompoundAssignmentOperator(
                        node,
                        new BinaryOperatorSignature(
                            kind.WithType(BinaryOperatorKind.Dynamic).WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                            left.Type,
                            right.Type,
                            Compilation.DynamicType),
                        left,
                        right,
                        Conversion.NoConversion,
                        finalDynamicConversion,
                        LookupResultKind.Viable,
                        left.Type,
                        hasErrors: false);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, node.OperatorToken.Text, left.Display, right.Display);

                    // error: operator can't be applied on dynamic and a type that is not convertible to dynamic:
                    return new BoundCompoundAssignmentOperator(node, BinaryOperatorSignature.Error, left, right,
                        Conversion.NoConversion, Conversion.NoConversion, LookupResultKind.Empty, CreateErrorType(), hasErrors: true);
                }
            }

            if (left.Kind == BoundKind.EventAccess && !CheckEventValueKind((BoundEventAccess)left, BindValueKind.Assignable, diagnostics))
            {
                // If we're in a place where the event can be assigned, then continue so that we give errors
                // about the types and operator not lining up.  Otherwise, just report that the event can't
                // be used here.

                // NOTE: no overload resolution candidates.
                return new BoundCompoundAssignmentOperator(node, BinaryOperatorSignature.Error, left, right,
                    Conversion.NoConversion, Conversion.NoConversion, LookupResultKind.NotAVariable, CreateErrorType(), hasErrors: true);
            }

            // A compound operator, say, x |= y, is bound as x = (X)( ((T)x) | ((T)y) ). We must determine
            // the binary operator kind, the type conversions from each side to the types expected by
            // the operator, and the type conversion from the return type of the operand to the left hand side.
            //
            // We can get away with binding the right-hand-side of the operand into its converted form early.
            // This is convenient because first, it is never rewritten into an access to a temporary before
            // the conversion, and second, because that is more convenient for the "d += lambda" case.
            // We want to have the converted (bound) lambda in the bound tree, not the unconverted unbound lambda.

            LookupResultKind resultKind;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            BinaryOperatorAnalysisResult best = this.BinaryOperatorOverloadResolution(kind, left, right, node, diagnostics, out resultKind, out originalUserDefinedOperators);
            if (!best.HasValue)
            {
                ReportAssignmentOperatorError(node, diagnostics, left, right, resultKind);
                return new BoundCompoundAssignmentOperator(node, BinaryOperatorSignature.Error, left, right,
                    Conversion.NoConversion, Conversion.NoConversion, resultKind, originalUserDefinedOperators, CreateErrorType(), hasErrors: true);
            }

            // The rules in the spec for determining additional errors are bit confusing. In particular
            // this line is misleading:
            //
            // "for predefined operators ... x op= y is permitted if both x op y and x = y are permitted"
            //
            // That's not accurate in many cases. For example, "x += 1" is permitted if x is string or
            // any enum type, but x = 1 is not legal for strings or enums.
            //
            // The correct rules are spelled out in the spec:
            //
            // Spec §7.17.2:
            // An operation of the form x op= y is processed by applying binary operator overload
            // resolution (§7.3.4) as if the operation was written x op y.
            // Let R be the return type of the selected operator, and T the type of x. Then,
            //
            // * If an implicit conversion from an expression of type R to the type T exists,
            //   the operation is evaluated as x = (T)(x op y), except that x is evaluated only once.
            //   [no cast is inserted, unless the conversion is implicit dynamic]
            // * Otherwise, if
            //   (1) the selected operator is a predefined operator,
            //   (2) if R is explicitly convertible to T, and
            //   (3.1) if y is implicitly convertible to T or
            //   (3.2) the operator is a shift operator... [then cast the result to T]
            // * Otherwise ... a binding-time error occurs.

            // So let's tease that out. There are two possible errors: the conversion from the
            // operator result type to the left hand type could be bad, and the conversion
            // from the right hand side to the left hand type could be bad.
            //
            // We report the first error under the following circumstances:
            //
            // * The final conversion is bad, or
            // * The final conversion is explicit and the selected operator is not predefined
            //
            // We report the second error under the following circumstances:
            //
            // * The final conversion is explicit, and
            // * The selected operator is predefined, and
            // * the selected operator is not a shift, and
            // * the right-to-left conversion is not implicit

            bool hasError = false;

            BinaryOperatorSignature bestSignature = best.Signature;

            if (CheckOverflowAtRuntime)
            {
                bestSignature = new BinaryOperatorSignature(
                    bestSignature.Kind.WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                    bestSignature.LeftType,
                    bestSignature.RightType,
                    bestSignature.ReturnType,
                    bestSignature.Method);
            }

            BoundExpression rightConverted = CreateConversion(right, best.RightConversion, bestSignature.RightType, diagnostics);

            var leftType = left.Type;
            Conversion finalConversion = Conversions.ClassifyConversionFromExpressionType(bestSignature.ReturnType, leftType, ref useSiteDiagnostics);

            bool isPredefinedOperator = !bestSignature.Kind.IsUserDefined();

            if (!finalConversion.IsValid || finalConversion.IsExplicit && !isPredefinedOperator)
            {
                hasError = true;
                GenerateImplicitConversionError(diagnostics, this.Compilation, node, finalConversion, bestSignature.ReturnType, leftType);
            }
            else
            {
                ReportDiagnosticsIfObsolete(diagnostics, finalConversion, node, hasBaseReceiver: false);
            }

            if (finalConversion.IsExplicit &&
                isPredefinedOperator &&
                !kind.IsShift())
            {
                Conversion rightToLeftConversion = this.Conversions.ClassifyConversionFromExpression(right, leftType, ref useSiteDiagnostics);
                if (!rightToLeftConversion.IsImplicit || !rightToLeftConversion.IsValid)
                {
                    hasError = true;
                    GenerateImplicitConversionError(diagnostics, node, rightToLeftConversion, right, leftType);
                }
            }

            diagnostics.Add(node, useSiteDiagnostics);

            if (!hasError && leftType.IsVoidPointer())
            {
                Error(diagnostics, ErrorCode.ERR_VoidError, node);
                hasError = true;
            }

            // Any events that weren't handled above (by BindEventAssignment) are bad - we just followed this
            // code path for the diagnostics.  Make sure we don't report success.
            Debug.Assert(left.Kind != BoundKind.EventAccess || hasError);

            Conversion leftConversion = best.LeftConversion;
            ReportDiagnosticsIfObsolete(diagnostics, leftConversion, node, hasBaseReceiver: false);

            return new BoundCompoundAssignmentOperator(node, bestSignature, left, rightConverted,
                leftConversion, finalConversion, resultKind, originalUserDefinedOperators, leftType, hasError);
        }

        /// <summary>
        /// For "receiver.event += expr", produce "receiver.add_event(expr)".
        /// For "receiver.event -= expr", produce "receiver.remove_event(expr)".
        /// </summary>
        /// <remarks>
        /// Performs some validation of the accessor that couldn't be done in CheckEventValueKind, because
        /// the specific accessor wasn't known.
        /// </remarks>
        private BoundExpression BindEventAssignment(AssignmentExpressionSyntax node, BoundEventAccess left, BoundExpression right, BinaryOperatorKind opKind, DiagnosticBag diagnostics)
        {
            Debug.Assert(opKind == BinaryOperatorKind.Addition || opKind == BinaryOperatorKind.Subtraction);

            bool hasErrors = false;

            EventSymbol eventSymbol = left.EventSymbol;
            BoundExpression receiverOpt = left.ReceiverOpt;

            TypeSymbol delegateType = left.Type;

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion argumentConversion = this.Conversions.ClassifyConversionFromExpression(right, delegateType, ref useSiteDiagnostics);

            if (!argumentConversion.IsImplicit || !argumentConversion.IsValid) // NOTE: dev10 appears to allow user-defined conversions here.
            {
                hasErrors = true;
                if (delegateType.IsDelegateType()) // Otherwise, suppress cascading.
                {
                    GenerateImplicitConversionError(diagnostics, node, argumentConversion, right, delegateType);
                }
            }

            BoundExpression argument = CreateConversion(right, argumentConversion, delegateType, diagnostics);

            bool isAddition = opKind == BinaryOperatorKind.Addition;
            MethodSymbol method = isAddition ? eventSymbol.AddMethod : eventSymbol.RemoveMethod;

            TypeSymbol type;
            if ((object)method == null)
            {
                type = this.GetSpecialType(SpecialType.System_Void, diagnostics, node); //we know the return type would have been void

                // There will be a diagnostic on the declaration if it is from source.
                if (!eventSymbol.OriginalDefinition.IsFromCompilation(this.Compilation))
                {
                    // CONSIDER: better error code?  ERR_EventNeedsBothAccessors?
                    Error(diagnostics, ErrorCode.ERR_MissingPredefinedMember, node, delegateType, SourceEventSymbol.GetAccessorName(eventSymbol.Name, isAddition));
                }
            }
            else
            {
                CheckImplicitThisCopyInReadOnlyMember(receiverOpt, method, diagnostics);

                if (!this.IsAccessible(method, ref useSiteDiagnostics, this.GetAccessThroughType(receiverOpt)))
                {
                    // CONSIDER: depending on the accessibility (e.g. if it's private), dev10 might just report the whole event bogus.
                    Error(diagnostics, ErrorCode.ERR_BadAccess, node, method);
                    hasErrors = true;
                }
                else if (IsBadBaseAccess(node, receiverOpt, method, diagnostics, eventSymbol))
                {
                    hasErrors = true;
                }
                else
                {
                    CheckRuntimeSupportForSymbolAccess(node, receiverOpt, method, diagnostics);
                }

                if (eventSymbol.IsWindowsRuntimeEvent)
                {
                    // Return type is actually void because this call will be later encapsulated in a call
                    // to WindowsRuntimeMarshal.AddEventHandler or RemoveEventHandler, which has the return
                    // type of void.
                    type = this.GetSpecialType(SpecialType.System_Void, diagnostics, node);
                }
                else
                {
                    type = method.ReturnType;
                }
            }

            diagnostics.Add(node, useSiteDiagnostics);

            return new BoundEventAssignmentOperator(
                syntax: node,
                @event: eventSymbol,
                isAddition: isAddition,
                isDynamic: right.HasDynamicType(),
                receiverOpt: receiverOpt,
                argument: argument,
                type: type,
                hasErrors: hasErrors);
        }

        private static bool IsLegalDynamicOperand(BoundExpression operand)
        {
            Debug.Assert(operand != null);

            TypeSymbol type = operand.Type;

            // Literal null is a legal operand to a dynamic operation. The other typeless expressions --
            // method groups, lambdas, anonymous methods -- are not.

            // If the operand is of a class, interface, delegate, array, struct, enum, nullable
            // or type param types, it's legal to use in a dynamic expression. In short, the type
            // must be one that is convertible to object.

            if ((object)type == null)
            {
                return operand.IsLiteralNull();
            }

            // Pointer types and very special types are not convertible to object.

            return !type.IsPointerType() && !type.IsRestrictedType() && !type.IsVoidType();
        }

        private BoundExpression BindDynamicBinaryOperator(
            BinaryExpressionSyntax node,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            DiagnosticBag diagnostics)
        {
            // This method binds binary * / % + - << >> < > <= >= == != & ! ^ && || operators where one or both
            // of the operands are dynamic.
            Debug.Assert((object)left.Type != null && left.Type.IsDynamic() || (object)right.Type != null && right.Type.IsDynamic());

            bool hasError = false;
            bool leftValidOperand = IsLegalDynamicOperand(left);
            bool rightValidOperand = IsLegalDynamicOperand(right);

            if (!leftValidOperand || !rightValidOperand)
            {
                // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
                Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, node.OperatorToken.Text, left.Display, right.Display);
                hasError = true;
            }

            MethodSymbol userDefinedOperator = null;

            if (kind.IsLogical() && leftValidOperand)
            {
                // We need to make sure left is either implicitly convertible to Boolean or has user defined truth operator.
                //   left && right is lowered to {op_False|op_Implicit}(left) ? left : And(left, right)
                //   left || right is lowered to {op_True|!op_Implicit}(left) ? left : Or(left, right)
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                if (!IsValidDynamicCondition(left, isNegative: kind == BinaryOperatorKind.LogicalAnd, useSiteDiagnostics: ref useSiteDiagnostics, userDefinedOperator: out userDefinedOperator))
                {
                    // Dev11 reports ERR_MustHaveOpTF. The error was shared between this case and user-defined binary Boolean operators.
                    // We report two distinct more specific error messages.
                    Error(diagnostics, ErrorCode.ERR_InvalidDynamicCondition, node.Left, left.Type, kind == BinaryOperatorKind.LogicalAnd ? "false" : "true");

                    hasError = true;
                }
                diagnostics.Add(node, useSiteDiagnostics);
            }

            return new BoundBinaryOperator(
                syntax: node,
                operatorKind: (hasError ? kind : kind.WithType(BinaryOperatorKind.Dynamic)).WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                left: BindToNaturalType(left, diagnostics),
                right: BindToNaturalType(right, diagnostics),
                constantValueOpt: ConstantValue.NotAvailable,
                methodOpt: userDefinedOperator,
                resultKind: LookupResultKind.Viable,
                type: Compilation.DynamicType,
                hasErrors: hasError);
        }

        protected static bool IsSimpleBinaryOperator(SyntaxKind kind)
        {
            // We deliberately exclude &&, ||, ??, etc.
            switch (kind)
            {
                case SyntaxKind.AddExpression:
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.SubtractExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.BitwiseAndExpression:
                case SyntaxKind.BitwiseOrExpression:
                case SyntaxKind.ExclusiveOrExpression:
                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.RightShiftExpression:
                    return true;
            }
            return false;
        }

        private BoundExpression BindSimpleBinaryOperator(BinaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            // The simple binary operators are left-associative, and expressions of the form
            // a + b + c + d .... are relatively common in machine-generated code. The parser can handle
            // creating a deep-on-the-left syntax tree no problem, and then we promptly blow the stack during
            // semantic analysis. Here we build an explicit stack to handle the left-hand recursion.

            Debug.Assert(IsSimpleBinaryOperator(node.Kind()));

            var syntaxNodes = ArrayBuilder<BinaryExpressionSyntax>.GetInstance();

            ExpressionSyntax current = node;
            while (IsSimpleBinaryOperator(current.Kind()))
            {
                var binOp = (BinaryExpressionSyntax)current;
                syntaxNodes.Push(binOp);
                current = binOp.Left;
            }

            int compoundStringLength = 0;

            BoundExpression result = BindExpression(current, diagnostics);

            if (node.IsKind(SyntaxKind.SubtractExpression)
                && current.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                if (result.Kind == BoundKind.TypeExpression
                    && !((ParenthesizedExpressionSyntax)current).Expression.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    Error(diagnostics, ErrorCode.ERR_PossibleBadNegCast, node);
                }
                else if (result.Kind == BoundKind.BadExpression)
                {
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)current;

                    if (parenthesizedExpression.Expression.IsKind(SyntaxKind.IdentifierName)
                        && ((IdentifierNameSyntax)parenthesizedExpression.Expression).Identifier.ValueText == "dynamic")
                    {
                        Error(diagnostics, ErrorCode.ERR_PossibleBadNegCast, node);
                    }
                }
            }

            while (syntaxNodes.Count > 0)
            {
                BinaryExpressionSyntax syntaxNode = syntaxNodes.Pop();
                BindValueKind bindValueKind = GetBinaryAssignmentKind(syntaxNode.Kind());
                BoundExpression left = CheckValue(result, bindValueKind, diagnostics);
                BoundExpression right = BindValue(syntaxNode.Right, diagnostics, BindValueKind.RValue);
                BoundExpression boundOp = BindSimpleBinaryOperator(syntaxNode, diagnostics, left, right, ref compoundStringLength);
                result = boundOp;
            }

            syntaxNodes.Free();
            return result;
        }

        private BoundExpression BindSimpleBinaryOperator(BinaryExpressionSyntax node, DiagnosticBag diagnostics,
            BoundExpression left, BoundExpression right, ref int compoundStringLength)
        {
            BinaryOperatorKind kind = SyntaxKindToBinaryOperatorKind(node.Kind());

            // If either operand is bad, don't try to do binary operator overload resolution; that would just
            // make cascading errors.

            if (left.HasAnyErrors || right.HasAnyErrors)
            {
                // NOTE: no user-defined conversion candidates
                return new BoundBinaryOperator(node, kind, ConstantValue.NotAvailable, null, LookupResultKind.Empty, left, right, GetBinaryOperatorErrorType(kind, diagnostics, node), true);
            }

            TypeSymbol leftType = left.Type;
            TypeSymbol rightType = right.Type;

            if ((object)leftType != null && leftType.IsDynamic() || (object)rightType != null && rightType.IsDynamic())
            {
                return BindDynamicBinaryOperator(node, kind, left, right, diagnostics);
            }

            // SPEC OMISSION: The C# 2.0 spec had a line in it that noted that the expressions "null == null"
            // SPEC OMISSION: and "null != null" were to be automatically treated as the appropriate constant;
            // SPEC OMISSION: overload resolution was to be skipped.  That's because a strict reading
            // SPEC OMISSION: of the overload resolution spec shows that overload resolution would give an
            // SPEC OMISSION: ambiguity error for this case; the expression is ambiguous between the int?,
            // SPEC OMISSION: bool? and string versions of equality.  This line was accidentally edited
            // SPEC OMISSION: out of the C# 3 specification; we should re-insert it.

            bool leftNull = left.IsLiteralNull();
            bool rightNull = right.IsLiteralNull();
            bool isEquality = kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual;
            if (isEquality && leftNull && rightNull)
            {
                return new BoundLiteral(node, ConstantValue.Create(kind == BinaryOperatorKind.Equal), GetSpecialType(SpecialType.System_Boolean, diagnostics, node));
            }

            if (IsTupleBinaryOperation(left, right) &&
                (kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual))
            {
                CheckFeatureAvailability(node, MessageID.IDS_FeatureTupleEquality, diagnostics);
                return BindTupleBinaryOperator(node, kind, left, right, diagnostics);
            }

            // SPEC: For an operation of one of the forms x == null, null == x, x != null, null != x,
            // SPEC: where x is an expression of nullable type, if operator overload resolution
            // SPEC: fails to find an applicable operator, the result is instead computed from
            // SPEC: the HasValue property of x.

            // Note that the spec says "fails to find an applicable operator", not "fails to
            // find a unique best applicable operator." For example:
            // struct X {
            //   public static bool operator ==(X? x, double? y) {...}
            //   public static bool operator ==(X? x, decimal? y) {...}
            //
            // The comparison "x == null" should produce an ambiguity error rather
            // that being bound as !x.HasValue.
            //

            LookupResultKind resultKind;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            BinaryOperatorSignature signature;
            BinaryOperatorAnalysisResult best;
            bool foundOperator = BindSimpleBinaryOperatorParts(node, diagnostics, left, right, kind,
                out resultKind, out originalUserDefinedOperators, out signature, out best);

            BinaryOperatorKind resultOperatorKind = signature.Kind;
            bool hasErrors = false;
            if (!foundOperator)
            {
                ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, resultKind);
                resultOperatorKind &= ~BinaryOperatorKind.TypeMask;
                hasErrors = true;
            }

            switch (node.Kind())
            {
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                    break;
                default:
                    if (leftType.IsVoidPointer() || rightType.IsVoidPointer())
                    {
                        // CONSIDER: dev10 cascades this, but roslyn doesn't have to.
                        Error(diagnostics, ErrorCode.ERR_VoidError, node);
                        hasErrors = true;
                    }
                    break;
            }

            TypeSymbol resultType = signature.ReturnType;
            BoundExpression resultLeft = left;
            BoundExpression resultRight = right;
            ConstantValue resultConstant = null;

            if (foundOperator && (resultOperatorKind.OperandTypes() != BinaryOperatorKind.NullableNull))
            {
                Debug.Assert((object)signature.LeftType != null);
                Debug.Assert((object)signature.RightType != null);

                resultLeft = CreateConversion(left, best.LeftConversion, signature.LeftType, diagnostics);
                resultRight = CreateConversion(right, best.RightConversion, signature.RightType, diagnostics);
                resultConstant = FoldBinaryOperator(node, resultOperatorKind, resultLeft, resultRight, resultType.SpecialType, diagnostics, ref compoundStringLength);
            }
            else
            {
                resultLeft = BindToNaturalType(resultLeft, diagnostics);
                resultRight = BindToNaturalType(resultRight, diagnostics);
            }

            hasErrors = hasErrors || resultConstant != null && resultConstant.IsBad;

            return new BoundBinaryOperator(
                node,
                resultOperatorKind.WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                resultLeft,
                resultRight,
                resultConstant,
                signature.Method,
                resultKind,
                originalUserDefinedOperators,
                resultType,
                hasErrors);
        }

        private bool BindSimpleBinaryOperatorParts(BinaryExpressionSyntax node, DiagnosticBag diagnostics, BoundExpression left, BoundExpression right, BinaryOperatorKind kind,
            out LookupResultKind resultKind, out ImmutableArray<MethodSymbol> originalUserDefinedOperators,
            out BinaryOperatorSignature resultSignature, out BinaryOperatorAnalysisResult best)
        {
            bool foundOperator;
            best = this.BinaryOperatorOverloadResolution(kind, left, right, node, diagnostics, out resultKind, out originalUserDefinedOperators);

            // However, as an implementation detail, we never "fail to find an applicable
            // operator" during overload resolution if we have x == null, etc. We always
            // find at least the reference conversion object == object; the overload resolution
            // code does not reject that.  Therefore what we should do is only bind
            // "x == null" as a nullable-to-null comparison if overload resolution chooses
            // the reference conversion.

            if (!best.HasValue)
            {
                resultSignature = new BinaryOperatorSignature(kind, leftType: null, rightType: null, CreateErrorType());
                foundOperator = false;
            }
            else
            {
                var signature = best.Signature;

                bool isObjectEquality = signature.Kind == BinaryOperatorKind.ObjectEqual || signature.Kind == BinaryOperatorKind.ObjectNotEqual;

                bool leftNull = left.IsLiteralNull();
                bool rightNull = right.IsLiteralNull();

                TypeSymbol leftType = left.Type;
                TypeSymbol rightType = right.Type;

                bool isNullableEquality = (object)signature.Method == null &&
                    (signature.Kind.Operator() == BinaryOperatorKind.Equal || signature.Kind.Operator() == BinaryOperatorKind.NotEqual) &&
                    (leftNull && (object)rightType != null && rightType.IsNullableType() ||
                        rightNull && (object)leftType != null && leftType.IsNullableType());

                if (isNullableEquality)
                {
                    resultSignature = new BinaryOperatorSignature(kind | BinaryOperatorKind.NullableNull, leftType: null, rightType: null,
                        GetSpecialType(SpecialType.System_Boolean, diagnostics, node));

                    foundOperator = true;
                }
                else
                {
                    resultSignature = signature;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    bool leftDefault = left.IsLiteralDefault();
                    bool rightDefault = right.IsLiteralDefault();
                    foundOperator = !isObjectEquality || BuiltInOperators.IsValidObjectEquality(Conversions, leftType, leftNull || leftDefault, rightType, rightNull || rightDefault, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);
                }
            }
            return foundOperator;
        }

        private static void ReportUnaryOperatorError(CSharpSyntaxNode node, DiagnosticBag diagnostics, string operatorName, BoundExpression operand, LookupResultKind resultKind)
        {
            ErrorCode errorCode = resultKind == LookupResultKind.Ambiguous ?
                ErrorCode.ERR_AmbigUnaryOp : // Operator '{0}' is ambiguous on an operand of type '{1}'
                ErrorCode.ERR_BadUnaryOp;    // Operator '{0}' cannot be applied to operand of type '{1}'

            Error(diagnostics, errorCode, node, operatorName, operand.Display);
        }

        private void ReportAssignmentOperatorError(AssignmentExpressionSyntax node, DiagnosticBag diagnostics, BoundExpression left, BoundExpression right, LookupResultKind resultKind)
        {
            if (((SyntaxKind)node.OperatorToken.RawKind == SyntaxKind.PlusEqualsToken || (SyntaxKind)node.OperatorToken.RawKind == SyntaxKind.MinusEqualsToken) &&
                (object)left.Type != null && left.Type.TypeKind == TypeKind.Delegate)
            {
                // Special diagnostic for delegate += and -= about wrong right-hand-side
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var conversion = this.Conversions.ClassifyConversionFromExpression(right, left.Type, ref useSiteDiagnostics);
                Debug.Assert(!conversion.IsImplicit);
                GenerateImplicitConversionError(diagnostics, right.Syntax, conversion, right, left.Type);
                // discard use-site diagnostics
            }
            else
            {
                ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, resultKind);
            }
        }

        private static void ReportBinaryOperatorError(ExpressionSyntax node, DiagnosticBag diagnostics, SyntaxToken operatorToken, BoundExpression left, BoundExpression right, LookupResultKind resultKind)
        {
            bool leftDefault = left.IsLiteralDefault();
            bool rightDefault = right.IsLiteralDefault();
            if ((operatorToken.Kind() == SyntaxKind.EqualsEqualsToken || operatorToken.Kind() == SyntaxKind.ExclamationEqualsToken))
            {
                if (leftDefault && rightDefault)
                {
                    Error(diagnostics, ErrorCode.ERR_AmbigBinaryOpsOnDefault, node, operatorToken.Text);
                    return;
                }
            }
            else if (leftDefault || rightDefault)
            {
                // other than == and !=, binary operators are disallowed on `default` literal
                Error(diagnostics, ErrorCode.ERR_BadOpOnNullOrDefault, node, operatorToken.Text, "default");
                return;
            }

            ErrorCode errorCode = resultKind == LookupResultKind.Ambiguous ?
                ErrorCode.ERR_AmbigBinaryOps : // Operator '{0}' is ambiguous on operands of type '{1}' and '{2}'
                ErrorCode.ERR_BadBinaryOps;    // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'

            Error(diagnostics, errorCode, node, operatorToken.Text, left.Display, right.Display);
        }

        private BoundExpression BindConditionalLogicalOperator(BinaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            Debug.Assert(node.Kind() == SyntaxKind.LogicalOrExpression || node.Kind() == SyntaxKind.LogicalAndExpression);

            // Do not blow the stack due to a deep recursion on the left.

            BinaryExpressionSyntax binary = node;
            ExpressionSyntax child;

            while (true)
            {
                child = binary.Left;
                var childAsBinary = child as BinaryExpressionSyntax;

                if (childAsBinary == null ||
                    (childAsBinary.Kind() != SyntaxKind.LogicalOrExpression && childAsBinary.Kind() != SyntaxKind.LogicalAndExpression))
                {
                    break;
                }

                binary = childAsBinary;
            }

            BoundExpression left = BindRValueWithoutTargetType(child, diagnostics);

            do
            {
                binary = (BinaryExpressionSyntax)child.Parent;
                BoundExpression right = BindRValueWithoutTargetType(binary.Right, diagnostics);
                left = BindConditionalLogicalOperator(binary, left, right, diagnostics);
                child = binary;
            }
            while ((object)child != node);

            return left;
        }

        private BoundExpression BindConditionalLogicalOperator(BinaryExpressionSyntax node, BoundExpression left, BoundExpression right, DiagnosticBag diagnostics)
        {
            BinaryOperatorKind kind = SyntaxKindToBinaryOperatorKind(node.Kind());

            Debug.Assert(kind == BinaryOperatorKind.LogicalAnd || kind == BinaryOperatorKind.LogicalOr);

            // Let's take an easy out here. The vast majority of the time the operands will
            // both be bool. This is the only situation in which the expression can be a
            // constant expression, so do the folding now if we can.

            if ((object)left.Type != null && left.Type.SpecialType == SpecialType.System_Boolean &&
                (object)right.Type != null && right.Type.SpecialType == SpecialType.System_Boolean)
            {
                var constantValue = FoldBinaryOperator(node, kind | BinaryOperatorKind.Bool, left, right, SpecialType.System_Boolean, diagnostics);

                // NOTE: no candidate user-defined operators.
                return new BoundBinaryOperator(node, kind | BinaryOperatorKind.Bool, constantValue, methodOpt: null,
                    resultKind: LookupResultKind.Viable, left, right, type: left.Type, hasErrors: constantValue != null && constantValue.IsBad);
            }

            // If either operand is bad, don't try to do binary operator overload resolution; that will just
            // make cascading errors.

            if (left.HasAnyErrors || right.HasAnyErrors)
            {
                // NOTE: no candidate user-defined operators.
                return new BoundBinaryOperator(node, kind, ConstantValue.NotAvailable, methodOpt: null,
                    resultKind: LookupResultKind.Empty, left, right, type: GetBinaryOperatorErrorType(kind, diagnostics, node), hasErrors: true);
            }

            if (left.HasDynamicType() || right.HasDynamicType())
            {
                left = BindToNaturalType(left, diagnostics);
                right = BindToNaturalType(right, diagnostics);
                return BindDynamicBinaryOperator(node, kind, left, right, diagnostics);
            }

            LookupResultKind lookupResult;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            var best = this.BinaryOperatorOverloadResolution(kind, left, right, node, diagnostics, out lookupResult, out originalUserDefinedOperators);

            // SPEC: If overload resolution fails to find a single best operator, or if overload
            // SPEC: resolution selects one of the predefined integer logical operators, a binding-
            // SPEC: time error occurs.
            //
            // SPEC OMISSION: We should probably clarify that the enum logical operators count as
            // SPEC OMISSION: integer logical operators. Basically the rule here should actually be:
            // SPEC OMISSION: if overload resolution selects something other than a user-defined
            // SPEC OMISSION: operator or the built in not-lifted operator on bool, an error occurs.
            //

            if (!best.HasValue)
            {
                ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, lookupResult);
            }
            else
            {
                // There are two non-error possibilities. Either both operands are implicitly convertible to
                // bool, or we've got a valid user-defined operator.
                BinaryOperatorSignature signature = best.Signature;

                bool bothBool = signature.LeftType.SpecialType == SpecialType.System_Boolean &&
                        signature.RightType.SpecialType == SpecialType.System_Boolean;

                MethodSymbol trueOperator = null, falseOperator = null;

                if (!bothBool && !signature.Kind.IsUserDefined())
                {
                    ReportBinaryOperatorError(node, diagnostics, node.OperatorToken, left, right, lookupResult);
                }
                else if (bothBool || IsValidUserDefinedConditionalLogicalOperator(node, signature, diagnostics, out trueOperator, out falseOperator))
                {
                    var resultLeft = CreateConversion(left, best.LeftConversion, signature.LeftType, diagnostics);
                    var resultRight = CreateConversion(right, best.RightConversion, signature.RightType, diagnostics);
                    var resultKind = kind | signature.Kind.OperandTypes();
                    if (signature.Kind.IsLifted())
                    {
                        resultKind |= BinaryOperatorKind.Lifted;
                    }

                    if (resultKind.IsUserDefined())
                    {
                        Debug.Assert(trueOperator != null && falseOperator != null);

                        return new BoundUserDefinedConditionalLogicalOperator(
                            node,
                            resultKind,
                            resultLeft,
                            resultRight,
                            signature.Method,
                            trueOperator,
                            falseOperator,
                            lookupResult,
                            originalUserDefinedOperators,
                            signature.ReturnType);
                    }
                    else
                    {
                        Debug.Assert(bothBool);

                        return new BoundBinaryOperator(
                            node,
                            resultKind,
                            resultLeft,
                            resultRight,
                            ConstantValue.NotAvailable,
                            signature.Method,
                            lookupResult,
                            originalUserDefinedOperators,
                            signature.ReturnType);
                    }
                }
            }

            // We've already reported the error.
            return new BoundBinaryOperator(node, kind, left, right, ConstantValue.NotAvailable, null, lookupResult, originalUserDefinedOperators, CreateErrorType(), true);
        }

        private bool IsValidDynamicCondition(BoundExpression left, bool isNegative, ref HashSet<DiagnosticInfo> useSiteDiagnostics, out MethodSymbol userDefinedOperator)
        {
            userDefinedOperator = null;

            var type = left.Type;
            if ((object)type == null)
            {
                return false;
            }

            if (type.IsDynamic())
            {
                return true;
            }

            var implicitConversion = Conversions.ClassifyImplicitConversionFromExpression(left, Compilation.GetSpecialType(SpecialType.System_Boolean), ref useSiteDiagnostics);
            if (implicitConversion.Exists)
            {
                return true;
            }

            if (type.Kind != SymbolKind.NamedType)
            {
                return false;
            }

            var namedType = type as NamedTypeSymbol;
            return HasApplicableBooleanOperator(namedType, isNegative ? WellKnownMemberNames.FalseOperatorName : WellKnownMemberNames.TrueOperatorName, type, ref useSiteDiagnostics, out userDefinedOperator);
        }

        private bool IsValidUserDefinedConditionalLogicalOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorSignature signature,
            DiagnosticBag diagnostics,
            out MethodSymbol trueOperator,
            out MethodSymbol falseOperator)
        {
            Debug.Assert(signature.Kind.OperandTypes() == BinaryOperatorKind.UserDefined);

            // SPEC: When the operands of && or || are of types that declare an applicable
            // SPEC: user-defined operator & or |, both of the following must be true, where
            // SPEC: T is the type in which the selected operator is defined:

            // SPEC VIOLATION:
            //
            // The native compiler violates the specification, the native compiler allows:
            //
            // public static D? operator &(D? d1, D? d2) { ... }
            // public static bool operator true(D? d) { ... }
            // public static bool operator false(D? d) { ... }
            //
            // to be used as D? && D? or D? || D?. But if you do this:
            //
            // public static D operator &(D d1, D d2) { ... }
            // public static bool operator true(D? d) { ... }
            // public static bool operator false(D? d) { ... }
            //
            // And use the *lifted* form of the operator, this is disallowed.
            //
            // public static D? operator &(D? d1, D d2) { ... }
            // public static bool operator true(D? d) { ... }
            // public static bool operator false(D? d) { ... }
            //
            // Is not allowed because "the return type must be the same as the type of both operands"
            // which is not at all what the spec says.
            //
            // We ought not to break backwards compatibility with the native compiler. The spec
            // is plausibly in error; it is possible that this section of the specification was
            // never updated when nullable types and lifted operators were added to the language.
            // And it seems like the native compiler's behavior of allowing a nullable
            // version but not a lifted version is a bug that should be fixed.
            //
            // Therefore we will do the following in Roslyn:
            //
            // * The return and parameter types of the chosen operator, whether lifted or unlifted,
            //   must be the same.
            // * The return and parameter types must be either the enclosing type, or its corresponding
            //   nullable type.
            // * There must be an operator true/operator false that takes the left hand type of the operator.

            // Only classes and structs contain user-defined operators, so we know it is a named type symbol.
            NamedTypeSymbol t = (NamedTypeSymbol)signature.Method.ContainingType;

            // SPEC: The return type and the type of each parameter of the selected operator
            // SPEC: must be T.

            // As mentioned above, we relax this restriction. The types must all be the same.

            bool typesAreSame = TypeSymbol.Equals(signature.LeftType, signature.RightType, TypeCompareKind.ConsiderEverything2) && TypeSymbol.Equals(signature.LeftType, signature.ReturnType, TypeCompareKind.ConsiderEverything2);
            bool typeMatchesContainer = TypeSymbol.Equals(signature.ReturnType, t, TypeCompareKind.ConsiderEverything2) ||
                signature.ReturnType.IsNullableType() && TypeSymbol.Equals(signature.ReturnType.GetNullableUnderlyingType(), t, TypeCompareKind.ConsiderEverything2);

            if (!typesAreSame || !typeMatchesContainer)
            {
                // CS0217: In order to be applicable as a short circuit operator a user-defined logical
                // operator ('{0}') must have the same return type and parameter types

                Error(diagnostics, ErrorCode.ERR_BadBoolOp, syntax, signature.Method);

                trueOperator = null;
                falseOperator = null;
                return false;
            }

            // SPEC: T must contain declarations of operator true and operator false.

            // As mentioned above, we need more than just op true and op false existing; we need
            // to know that the first operand can be passed to it.

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (!HasApplicableBooleanOperator(t, WellKnownMemberNames.TrueOperatorName, signature.LeftType, ref useSiteDiagnostics, out trueOperator) ||
                !HasApplicableBooleanOperator(t, WellKnownMemberNames.FalseOperatorName, signature.LeftType, ref useSiteDiagnostics, out falseOperator))
            {
                // I have changed the wording of this error message. The original wording was:

                // CS0218: The type ('T') must contain declarations of operator true and operator false

                // I have changed that to:

                // CS0218: In order to be applicable as a short circuit operator, the declaring type
                // '{1}' of user-defined operator '{0}' must declare operator true and operator false.

                Error(diagnostics, ErrorCode.ERR_MustHaveOpTF, syntax, signature.Method, t);
                diagnostics.Add(syntax, useSiteDiagnostics);

                trueOperator = null;
                falseOperator = null;
                return false;
            }

            diagnostics.Add(syntax, useSiteDiagnostics);

            // For the remainder of this method the comments WOLOG assume that we're analyzing an &&. The
            // exact same issues apply to ||.

            // Note that the mere *existence* of operator true and operator false is sufficient.  They
            // are already constrained to take either T or T?. Since we know that the applicable
            // T.& takes (T, T), we know that both sides of the && are implicitly convertible
            // to T, and therefore the left side is implicitly convertible to T or T?.

            // SPEC: The expression x && y is evaluated as T.false(x) ? x : T.&(x,y) ... except that
            // SPEC: x is only evaluated once.
            //
            // DELIBERATE SPEC VIOLATION: The native compiler does not actually evaluate x&&y in this
            // manner. Suppose X is of type X. The code above is equivalent to:
            //
            // X temp = x, then evaluate:
            // T.false(temp) ? temp : T.&(temp, y)
            //
            // What the native compiler actually evaluates is:
            //
            // T temp = x, then evaluate
            // T.false(temp) ? temp : T.&(temp, y)
            //
            // That is a small difference but it has an observable effect. For example:
            //
            // class V { public static implicit operator T(V v) { ... } }
            // class X : V { public static implicit operator T?(X x) { ... } }
            // struct T {
            //   public static operator false(T? t) { ... }
            //   public static operator true(T? t) { ... }
            //   public static T operator &(T t1, T t2) { ... }
            // }
            //
            // Under the spec'd interpretation, if we had x of type X and y of type T then x && y is
            //
            // X temp = x;
            // T.false(temp) ? temp : T.&(temp, y)
            //
            // which would then be analyzed as:
            //
            // T.false(X.op_Implicit_To_Nullable_T(temp)) ?
            //     V.op_Implicit_To_T(temp) :
            //     T.&(op_Implicit_To_T(temp), y)
            //
            // But the native compiler actually generates:
            //
            // T temp = V.Op_Implicit_To_T(x);
            // T.false(new T?(temp)) ? temp : T.&(temp, y)
            //
            // That is, the native compiler converts the temporary to the type of the declaring operator type
            // regardless of the fact that there is a better conversion for the T.false call.
            //
            // We choose to match the native compiler behavior here; we might consider fixing
            // the spec to match the compiler.
            //
            // With this decision we need not keep track of any extra information in the bound
            // binary operator node; we need to know the left hand side converted to T, the right
            // hand side converted to T, and the method symbol of the chosen T.&(T, T) method.
            // The rewriting pass has enough information to deduce which T.false is to be called,
            // and can convert the T to T? if necessary.

            return true;
        }

        private bool HasApplicableBooleanOperator(NamedTypeSymbol containingType, string name, TypeSymbol argumentType, ref HashSet<DiagnosticInfo> useSiteDiagnostics, out MethodSymbol @operator)
        {
            for (var type = containingType; (object)type != null; type = type.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                var operators = type.GetOperators(name);
                for (var i = 0; i < operators.Length; i++)
                {
                    var op = operators[i];
                    if (op.ParameterCount == 1 && op.DeclaredAccessibility == Accessibility.Public)
                    {
                        var conversion = this.Conversions.ClassifyConversionFromType(argumentType, op.GetParameterType(0), ref useSiteDiagnostics);
                        if (conversion.IsImplicit)
                        {
                            @operator = op;
                            return true;
                        }
                    }
                }
            }

            @operator = null;
            return false;
        }

        private TypeSymbol GetBinaryOperatorErrorType(BinaryOperatorKind kind, DiagnosticBag diagnostics, CSharpSyntaxNode node)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    return GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
                default:
                    return CreateErrorType();
            }
        }

        private BinaryOperatorAnalysisResult BinaryOperatorOverloadResolution(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, CSharpSyntaxNode node, DiagnosticBag diagnostics, out LookupResultKind resultKind, out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            if (!IsDefaultLiteralAllowedInBinaryOperator(kind, left, right))
            {
                resultKind = LookupResultKind.OverloadResolutionFailure;
                originalUserDefinedOperators = default(ImmutableArray<MethodSymbol>);
                return default(BinaryOperatorAnalysisResult);
            }

            var result = BinaryOperatorOverloadResolutionResult.GetInstance();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.OverloadResolution.BinaryOperatorOverloadResolution(kind, left, right, result, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);

            var possiblyBest = result.Best;

            if (result.Results.Any())
            {
                var builder = ArrayBuilder<MethodSymbol>.GetInstance();
                foreach (var analysisResult in result.Results)
                {
                    MethodSymbol method = analysisResult.Signature.Method;
                    if ((object)method != null)
                    {
                        builder.Add(method);
                    }
                }
                originalUserDefinedOperators = builder.ToImmutableAndFree();

                if (possiblyBest.HasValue)
                {
                    resultKind = LookupResultKind.Viable;
                }
                else if (result.AnyValid())
                {
                    resultKind = LookupResultKind.Ambiguous;
                }
                else
                {
                    resultKind = LookupResultKind.OverloadResolutionFailure;
                }
            }
            else
            {
                originalUserDefinedOperators = ImmutableArray<MethodSymbol>.Empty;
                resultKind = possiblyBest.HasValue ? LookupResultKind.Viable : LookupResultKind.Empty;
            }

            if (possiblyBest.HasValue)
            {
                ReportObsoleteAndFeatureAvailabilityDiagnostics(possiblyBest.Signature.Method, node, diagnostics);
            }

            result.Free();
            return possiblyBest;
        }

        private void ReportObsoleteAndFeatureAvailabilityDiagnostics(MethodSymbol operatorMethod, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            if ((object)operatorMethod != null)
            {
                ReportDiagnosticsIfObsolete(diagnostics, operatorMethod, node, hasBaseReceiver: false);

                if (operatorMethod.ContainingType.IsInterface &&
                    operatorMethod.ContainingModule != Compilation.SourceModule)
                {
                    Binder.CheckFeatureAvailability(node, MessageID.IDS_DefaultInterfaceImplementation, diagnostics);
                }
            }
        }

        private bool IsDefaultLiteralAllowedInBinaryOperator(BinaryOperatorKind kind, BoundExpression left, BoundExpression right)
        {
            bool isEquality = kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual;
            if (isEquality)
            {
                return !left.IsLiteralDefault() || !right.IsLiteralDefault();
            }
            else
            {
                return !left.IsLiteralDefault() && !right.IsLiteralDefault();
            }
        }

        private UnaryOperatorAnalysisResult UnaryOperatorOverloadResolution(
            UnaryOperatorKind kind,
            BoundExpression operand,
            CSharpSyntaxNode node,
            DiagnosticBag diagnostics,
            out LookupResultKind resultKind,
            out ImmutableArray<MethodSymbol> originalUserDefinedOperators)
        {
            var result = UnaryOperatorOverloadResolutionResult.GetInstance();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            this.OverloadResolution.UnaryOperatorOverloadResolution(kind, operand, result, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);

            var possiblyBest = result.Best;

            if (result.Results.Any())
            {
                var builder = ArrayBuilder<MethodSymbol>.GetInstance();
                foreach (var analysisResult in result.Results)
                {
                    MethodSymbol method = analysisResult.Signature.Method;
                    if ((object)method != null)
                    {
                        builder.Add(method);
                    }
                }
                originalUserDefinedOperators = builder.ToImmutableAndFree();

                if (possiblyBest.HasValue)
                {
                    resultKind = LookupResultKind.Viable;
                }
                else if (result.AnyValid())
                {
                    // Special case: If we have the unary minus operator applied to a ulong, technically that should be
                    // an ambiguity. The ulong could be implicitly converted to float, double or decimal, and then
                    // the unary minus operator could be applied to the result. But though float is better than double,
                    // float is neither better nor worse than decimal. However it seems odd to give an ambiguity error
                    // when trying to do something such as applying a unary minus operator to an unsigned long.

                    if (kind == UnaryOperatorKind.UnaryMinus &&
                        (object)operand.Type != null &&
                        operand.Type.SpecialType == SpecialType.System_UInt64)
                    {
                        resultKind = LookupResultKind.OverloadResolutionFailure;
                    }
                    else
                    {
                        resultKind = LookupResultKind.Ambiguous;
                    }
                }
                else
                {
                    resultKind = LookupResultKind.OverloadResolutionFailure;
                }
            }
            else
            {
                originalUserDefinedOperators = ImmutableArray<MethodSymbol>.Empty;
                resultKind = possiblyBest.HasValue ? LookupResultKind.Viable : LookupResultKind.Empty;
            }

            if (possiblyBest.HasValue)
            {
                ReportObsoleteAndFeatureAvailabilityDiagnostics(possiblyBest.Signature.Method, node, diagnostics);
            }

            result.Free();
            return possiblyBest;
        }

        private static object FoldDecimalBinaryOperators(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            // Roslyn uses Decimal.operator+, operator-, etc. for both constant expressions and
            // non-constant expressions. Dev11 uses Decimal.operator+ etc. for non-constant
            // expressions only. This leads to different results between the two compilers
            // for certain constant expressions involving +/-0. (See bug #529730.) For instance,
            // +0 + -0 == +0 in Roslyn and == -0 in Dev11. Similarly, -0 - -0 == -0 in Roslyn, +0 in Dev11.
            // This is a breaking change from the native compiler but seems acceptable since
            // constant and non-constant expressions behave consistently in Roslyn.
            // (In Dev11, (+0 + -0) != (x + y) when x = +0, y = -0.)

            switch (kind)
            {
                case BinaryOperatorKind.DecimalAddition:
                    return valueLeft.DecimalValue + valueRight.DecimalValue;
                case BinaryOperatorKind.DecimalSubtraction:
                    return valueLeft.DecimalValue - valueRight.DecimalValue;
                case BinaryOperatorKind.DecimalMultiplication:
                    return valueLeft.DecimalValue * valueRight.DecimalValue;
                case BinaryOperatorKind.DecimalDivision:
                    return valueLeft.DecimalValue / valueRight.DecimalValue;
                case BinaryOperatorKind.DecimalRemainder:
                    return valueLeft.DecimalValue % valueRight.DecimalValue;
            }

            return null;
        }

        private static object FoldUncheckedIntegralBinaryOperator(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            unchecked
            {
                Debug.Assert(valueLeft != null);
                Debug.Assert(valueRight != null);

                switch (kind)
                {
                    case BinaryOperatorKind.IntAddition:
                        return valueLeft.Int32Value + valueRight.Int32Value;
                    case BinaryOperatorKind.LongAddition:
                        return valueLeft.Int64Value + valueRight.Int64Value;
                    case BinaryOperatorKind.UIntAddition:
                        return valueLeft.UInt32Value + valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongAddition:
                        return valueLeft.UInt64Value + valueRight.UInt64Value;
                    case BinaryOperatorKind.IntSubtraction:
                        return valueLeft.Int32Value - valueRight.Int32Value;
                    case BinaryOperatorKind.LongSubtraction:
                        return valueLeft.Int64Value - valueRight.Int64Value;
                    case BinaryOperatorKind.UIntSubtraction:
                        return valueLeft.UInt32Value - valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongSubtraction:
                        return valueLeft.UInt64Value - valueRight.UInt64Value;
                    case BinaryOperatorKind.IntMultiplication:
                        return valueLeft.Int32Value * valueRight.Int32Value;
                    case BinaryOperatorKind.LongMultiplication:
                        return valueLeft.Int64Value * valueRight.Int64Value;
                    case BinaryOperatorKind.UIntMultiplication:
                        return valueLeft.UInt32Value * valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongMultiplication:
                        return valueLeft.UInt64Value * valueRight.UInt64Value;

                    // even in unchecked context division may overflow:
                    case BinaryOperatorKind.IntDivision:
                        if (valueLeft.Int32Value == int.MinValue && valueRight.Int32Value == -1)
                        {
                            return int.MinValue;
                        }

                        return valueLeft.Int32Value / valueRight.Int32Value;

                    case BinaryOperatorKind.LongDivision:
                        if (valueLeft.Int64Value == long.MinValue && valueRight.Int64Value == -1)
                        {
                            return long.MinValue;
                        }

                        return valueLeft.Int64Value / valueRight.Int64Value;
                }

                return null;
            }
        }

        private static object FoldCheckedIntegralBinaryOperator(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            checked
            {
                Debug.Assert(valueLeft != null);
                Debug.Assert(valueRight != null);

                switch (kind)
                {
                    case BinaryOperatorKind.IntAddition:
                        return valueLeft.Int32Value + valueRight.Int32Value;
                    case BinaryOperatorKind.LongAddition:
                        return valueLeft.Int64Value + valueRight.Int64Value;
                    case BinaryOperatorKind.UIntAddition:
                        return valueLeft.UInt32Value + valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongAddition:
                        return valueLeft.UInt64Value + valueRight.UInt64Value;
                    case BinaryOperatorKind.IntSubtraction:
                        return valueLeft.Int32Value - valueRight.Int32Value;
                    case BinaryOperatorKind.LongSubtraction:
                        return valueLeft.Int64Value - valueRight.Int64Value;
                    case BinaryOperatorKind.UIntSubtraction:
                        return valueLeft.UInt32Value - valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongSubtraction:
                        return valueLeft.UInt64Value - valueRight.UInt64Value;
                    case BinaryOperatorKind.IntMultiplication:
                        return valueLeft.Int32Value * valueRight.Int32Value;
                    case BinaryOperatorKind.LongMultiplication:
                        return valueLeft.Int64Value * valueRight.Int64Value;
                    case BinaryOperatorKind.UIntMultiplication:
                        return valueLeft.UInt32Value * valueRight.UInt32Value;
                    case BinaryOperatorKind.ULongMultiplication:
                        return valueLeft.UInt64Value * valueRight.UInt64Value;
                    case BinaryOperatorKind.IntDivision:
                        return valueLeft.Int32Value / valueRight.Int32Value;
                    case BinaryOperatorKind.LongDivision:
                        return valueLeft.Int64Value / valueRight.Int64Value;
                }

                return null;
            }
        }

        internal static TypeSymbol GetEnumType(BinaryOperatorKind kind, BoundExpression left, BoundExpression right)
        {
            switch (kind)
            {
                case BinaryOperatorKind.EnumAndUnderlyingAddition:
                case BinaryOperatorKind.EnumAndUnderlyingSubtraction:
                case BinaryOperatorKind.EnumAnd:
                case BinaryOperatorKind.EnumOr:
                case BinaryOperatorKind.EnumXor:
                case BinaryOperatorKind.EnumEqual:
                case BinaryOperatorKind.EnumGreaterThan:
                case BinaryOperatorKind.EnumGreaterThanOrEqual:
                case BinaryOperatorKind.EnumLessThan:
                case BinaryOperatorKind.EnumLessThanOrEqual:
                case BinaryOperatorKind.EnumNotEqual:
                case BinaryOperatorKind.EnumSubtraction:
                    return left.Type;
                case BinaryOperatorKind.UnderlyingAndEnumAddition:
                case BinaryOperatorKind.UnderlyingAndEnumSubtraction:
                    return right.Type;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        internal static SpecialType GetEnumPromotedType(SpecialType underlyingType)
        {
            switch (underlyingType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                    return SpecialType.System_Int32;

                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return underlyingType;

                default:
                    throw ExceptionUtilities.UnexpectedValue(underlyingType);
            }
        }


        private ConstantValue FoldEnumBinaryOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(kind.IsEnum());
            Debug.Assert(!kind.IsLifted());

            // A built-in binary operation on constant enum operands is evaluated into an operation on
            // constants of the underlying type U of the enum type E. Comparison operators are lowered as
            // simply computing U<U. All other operators are computed as (E)(U op U) or in the case of
            // E-E, (U)(U-U).

            TypeSymbol enumType = GetEnumType(kind, left, right);
            TypeSymbol underlyingType = enumType.GetEnumUnderlyingType();

            BoundExpression newLeftOperand = CreateConversion(left, underlyingType, diagnostics);
            BoundExpression newRightOperand = CreateConversion(right, underlyingType, diagnostics);

            // If the underlying type is byte, sbyte, short, ushort or nullables of those then we'll need
            // to convert it up to int or int? because there are no + - * & | ^ < > <= >= == != operators
            // on byte, sbyte, short or ushort. They all convert to int.

            SpecialType operandSpecialType = GetEnumPromotedType(underlyingType.SpecialType);
            TypeSymbol operandType = (operandSpecialType == underlyingType.SpecialType) ?
                underlyingType :
                GetSpecialType(operandSpecialType, diagnostics, syntax);

            newLeftOperand = CreateConversion(newLeftOperand, operandType, diagnostics);
            newRightOperand = CreateConversion(newRightOperand, operandType, diagnostics);

            BinaryOperatorKind newKind = kind.Operator().WithType(newLeftOperand.Type.SpecialType);

            SpecialType operatorType = SpecialType.None;

            switch (newKind.Operator())
            {
                case BinaryOperatorKind.Addition:
                case BinaryOperatorKind.Subtraction:
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                    operatorType = operandType.SpecialType;
                    break;

                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    operatorType = SpecialType.System_Boolean;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(newKind.Operator());
            }

            var constantValue = FoldBinaryOperator(syntax, newKind, newLeftOperand, newRightOperand, operatorType, diagnostics);

            if (operatorType != SpecialType.System_Boolean && constantValue != null && !constantValue.IsBad)
            {
                TypeSymbol resultType = kind == BinaryOperatorKind.EnumSubtraction ? underlyingType : enumType;

                // We might need to convert back to the underlying type.
                return FoldConstantNumericConversion(syntax, constantValue, resultType, diagnostics);
            }

            return constantValue;
        }

        // Returns null if the operator can't be evaluated at compile time.
        private ConstantValue FoldBinaryOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            SpecialType resultType,
            DiagnosticBag diagnostics)
        {
            int compoundStringLength = 0;
            return FoldBinaryOperator(syntax, kind, left, right, resultType, diagnostics, ref compoundStringLength);
        }

        // Returns null if the operator can't be evaluated at compile time.
        private ConstantValue FoldBinaryOperator(
            CSharpSyntaxNode syntax,
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            SpecialType resultType,
            DiagnosticBag diagnostics,
            ref int compoundStringLength)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            if (left.HasAnyErrors || right.HasAnyErrors)
            {
                return null;
            }

            // SPEC VIOLATION: see method definition for details
            ConstantValue nullableEqualityResult = TryFoldingNullableEquality(kind, left, right);
            if (nullableEqualityResult != null)
            {
                return nullableEqualityResult;
            }

            var valueLeft = left.ConstantValue;
            var valueRight = right.ConstantValue;
            if (valueLeft == null || valueRight == null)
            {
                return null;
            }

            if (valueLeft.IsBad || valueRight.IsBad)
            {
                return ConstantValue.Bad;
            }

            if (kind.IsEnum() && !kind.IsLifted())
            {
                return FoldEnumBinaryOperator(syntax, kind, left, right, diagnostics);
            }

            // Divisions by zero on integral types and decimal always fail even in an unchecked context.
            if (IsDivisionByZero(kind, valueRight))
            {
                Error(diagnostics, ErrorCode.ERR_IntDivByZero, syntax);
                return ConstantValue.Bad;
            }

            object newValue = null;

            // Certain binary operations never fail; bool & bool, for example. If we are in one of those
            // cases, simply fold the operation and return.
            //
            // Although remainder and division always overflow at runtime with arguments int.MinValue/long.MinValue and -1
            // (regardless of checked context) the constant folding behavior is different.
            // Remainder never overflows at compile time while division does.
            newValue = FoldNeverOverflowBinaryOperators(kind, valueLeft, valueRight);
            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            ConstantValue concatResult = FoldStringConcatenation(kind, valueLeft, valueRight, ref compoundStringLength);
            if (concatResult != null)
            {
                if (concatResult.IsBad)
                {
                    Error(diagnostics, ErrorCode.ERR_ConstantStringTooLong, syntax);
                }

                return concatResult;
            }

            // Certain binary operations always fail if they overflow even when in an unchecked context;
            // decimal + decimal, for example. If we are in one of those cases, make the attempt. If it
            // succeeds, return the result. If not, give a compile-time error regardless of context.
            try
            {
                newValue = FoldDecimalBinaryOperators(kind, valueLeft, valueRight);
            }
            catch (OverflowException)
            {
                Error(diagnostics, ErrorCode.ERR_DecConstError, syntax);
                return ConstantValue.Bad;
            }

            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            if (CheckOverflowAtCompileTime)
            {
                try
                {
                    newValue = FoldCheckedIntegralBinaryOperator(kind, valueLeft, valueRight);
                }
                catch (OverflowException)
                {
                    Error(diagnostics, ErrorCode.ERR_CheckedOverflow, syntax);
                    return ConstantValue.Bad;
                }
            }
            else
            {
                newValue = FoldUncheckedIntegralBinaryOperator(kind, valueLeft, valueRight);
            }

            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            return null;
        }

        /// <summary>
        /// If one of the (unconverted) operands has constant value null and the other has
        /// a null constant value other than null, then they are definitely not equal
        /// and we can give a constant value for either == or !=.  This is a spec violation
        /// that we retain from Dev10.
        /// </summary>
        /// <param name="kind">The operator kind.  Nothing will happen if it is not a lifted equality operator.</param>
        /// <param name="left">The left-hand operand of the operation (possibly wrapped in a conversion).</param>
        /// <param name="right">The right-hand operand of the operation (possibly wrapped in a conversion).</param>
        /// <returns>
        /// If the operator represents lifted equality, then constant value true if both arguments have constant
        /// value null, constant value false if exactly one argument has constant value null, and null otherwise.
        /// If the operator represents lifted inequality, then constant value false if both arguments have constant
        /// value null, constant value true if exactly one argument has constant value null, and null otherwise.
        /// </returns>
        /// <remarks>
        /// SPEC VIOLATION: according to the spec (section 7.19) constant expressions cannot
        /// include implicit nullable conversions or nullable subexpressions.  However, Dev10
        /// specifically folds over lifted == and != (see ExpressionBinder::TryFoldingNullableEquality).
        /// Dev 10 does do compile-time evaluation of simple lifted operators, but it does so
        /// in a rewriting pass (see NullableRewriter) - they are not treated as constant values.
        /// </remarks>
        private static ConstantValue TryFoldingNullableEquality(BinaryOperatorKind kind, BoundExpression left, BoundExpression right)
        {
            if (kind.IsLifted())
            {
                BinaryOperatorKind op = kind.Operator();
                if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
                {
                    if (left.Kind == BoundKind.Conversion && right.Kind == BoundKind.Conversion)
                    {
                        BoundConversion leftConv = (BoundConversion)left;
                        BoundConversion rightConv = (BoundConversion)right;
                        ConstantValue leftConstant = leftConv.Operand.ConstantValue;
                        ConstantValue rightConstant = rightConv.Operand.ConstantValue;

                        if (leftConstant != null && rightConstant != null)
                        {
                            bool leftIsNull = leftConstant.IsNull;
                            bool rightIsNull = rightConstant.IsNull;
                            if (leftIsNull || rightIsNull)
                            {
                                // IMPL CHANGE: Dev10 raises WRN_NubExprIsConstBool in some cases, but that really doesn't
                                // make sense (why warn that a constant has a constant value?).
                                return (leftIsNull == rightIsNull) == (op == BinaryOperatorKind.Equal) ? ConstantValue.True : ConstantValue.False;
                            }
                        }
                    }
                }
            }

            return null;
        }

        // Some binary operators on constants never overflow, regardless of whether the context is checked or not.
        private static object FoldNeverOverflowBinaryOperators(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            // Note that we *cannot* do folding on single-precision floats as doubles to preserve precision,
            // as that would cause incorrect rounding that would be impossible to correct afterwards.
            switch (kind)
            {
                case BinaryOperatorKind.ObjectEqual:
                    if (valueLeft.IsNull) return valueRight.IsNull;
                    if (valueRight.IsNull) return false;
                    break;
                case BinaryOperatorKind.ObjectNotEqual:
                    if (valueLeft.IsNull) return !valueRight.IsNull;
                    if (valueRight.IsNull) return true;
                    break;
                case BinaryOperatorKind.DoubleAddition:
                    return valueLeft.DoubleValue + valueRight.DoubleValue;
                case BinaryOperatorKind.FloatAddition:
                    return valueLeft.SingleValue + valueRight.SingleValue;
                case BinaryOperatorKind.DoubleSubtraction:
                    return valueLeft.DoubleValue - valueRight.DoubleValue;
                case BinaryOperatorKind.FloatSubtraction:
                    return valueLeft.SingleValue - valueRight.SingleValue;
                case BinaryOperatorKind.DoubleMultiplication:
                    return valueLeft.DoubleValue * valueRight.DoubleValue;
                case BinaryOperatorKind.FloatMultiplication:
                    return valueLeft.SingleValue * valueRight.SingleValue;
                case BinaryOperatorKind.DoubleDivision:
                    return valueLeft.DoubleValue / valueRight.DoubleValue;
                case BinaryOperatorKind.FloatDivision:
                    return valueLeft.SingleValue / valueRight.SingleValue;
                case BinaryOperatorKind.DoubleRemainder:
                    return valueLeft.DoubleValue % valueRight.DoubleValue;
                case BinaryOperatorKind.FloatRemainder:
                    return valueLeft.SingleValue % valueRight.SingleValue;
                case BinaryOperatorKind.IntLeftShift:
                    return valueLeft.Int32Value << valueRight.Int32Value;
                case BinaryOperatorKind.LongLeftShift:
                    return valueLeft.Int64Value << valueRight.Int32Value;
                case BinaryOperatorKind.UIntLeftShift:
                    return valueLeft.UInt32Value << valueRight.Int32Value;
                case BinaryOperatorKind.ULongLeftShift:
                    return valueLeft.UInt64Value << valueRight.Int32Value;
                case BinaryOperatorKind.IntRightShift:
                    return valueLeft.Int32Value >> valueRight.Int32Value;
                case BinaryOperatorKind.LongRightShift:
                    return valueLeft.Int64Value >> valueRight.Int32Value;
                case BinaryOperatorKind.UIntRightShift:
                    return valueLeft.UInt32Value >> valueRight.Int32Value;
                case BinaryOperatorKind.ULongRightShift:
                    return valueLeft.UInt64Value >> valueRight.Int32Value;
                case BinaryOperatorKind.BoolAnd:
                    return valueLeft.BooleanValue & valueRight.BooleanValue;
                case BinaryOperatorKind.IntAnd:
                    return valueLeft.Int32Value & valueRight.Int32Value;
                case BinaryOperatorKind.LongAnd:
                    return valueLeft.Int64Value & valueRight.Int64Value;
                case BinaryOperatorKind.UIntAnd:
                    return valueLeft.UInt32Value & valueRight.UInt32Value;
                case BinaryOperatorKind.ULongAnd:
                    return valueLeft.UInt64Value & valueRight.UInt64Value;
                case BinaryOperatorKind.BoolOr:
                    return valueLeft.BooleanValue | valueRight.BooleanValue;
                case BinaryOperatorKind.IntOr:
                    return valueLeft.Int32Value | valueRight.Int32Value;
                case BinaryOperatorKind.LongOr:
                    return valueLeft.Int64Value | valueRight.Int64Value;
                case BinaryOperatorKind.UIntOr:
                    return valueLeft.UInt32Value | valueRight.UInt32Value;
                case BinaryOperatorKind.ULongOr:
                    return valueLeft.UInt64Value | valueRight.UInt64Value;
                case BinaryOperatorKind.BoolXor:
                    return valueLeft.BooleanValue ^ valueRight.BooleanValue;
                case BinaryOperatorKind.IntXor:
                    return valueLeft.Int32Value ^ valueRight.Int32Value;
                case BinaryOperatorKind.LongXor:
                    return valueLeft.Int64Value ^ valueRight.Int64Value;
                case BinaryOperatorKind.UIntXor:
                    return valueLeft.UInt32Value ^ valueRight.UInt32Value;
                case BinaryOperatorKind.ULongXor:
                    return valueLeft.UInt64Value ^ valueRight.UInt64Value;
                case BinaryOperatorKind.LogicalBoolAnd:
                    return valueLeft.BooleanValue && valueRight.BooleanValue;
                case BinaryOperatorKind.LogicalBoolOr:
                    return valueLeft.BooleanValue || valueRight.BooleanValue;
                case BinaryOperatorKind.BoolEqual:
                    return valueLeft.BooleanValue == valueRight.BooleanValue;
                case BinaryOperatorKind.StringEqual:
                    return valueLeft.StringValue == valueRight.StringValue;
                case BinaryOperatorKind.DecimalEqual:
                    return valueLeft.DecimalValue == valueRight.DecimalValue;
                case BinaryOperatorKind.FloatEqual:
                    return valueLeft.SingleValue == valueRight.SingleValue;
                case BinaryOperatorKind.DoubleEqual:
                    return valueLeft.DoubleValue == valueRight.DoubleValue;
                case BinaryOperatorKind.IntEqual:
                    return valueLeft.Int32Value == valueRight.Int32Value;
                case BinaryOperatorKind.LongEqual:
                    return valueLeft.Int64Value == valueRight.Int64Value;
                case BinaryOperatorKind.UIntEqual:
                    return valueLeft.UInt32Value == valueRight.UInt32Value;
                case BinaryOperatorKind.ULongEqual:
                    return valueLeft.UInt64Value == valueRight.UInt64Value;
                case BinaryOperatorKind.BoolNotEqual:
                    return valueLeft.BooleanValue != valueRight.BooleanValue;
                case BinaryOperatorKind.StringNotEqual:
                    return valueLeft.StringValue != valueRight.StringValue;
                case BinaryOperatorKind.DecimalNotEqual:
                    return valueLeft.DecimalValue != valueRight.DecimalValue;
                case BinaryOperatorKind.FloatNotEqual:
                    return valueLeft.SingleValue != valueRight.SingleValue;
                case BinaryOperatorKind.DoubleNotEqual:
                    return valueLeft.DoubleValue != valueRight.DoubleValue;
                case BinaryOperatorKind.IntNotEqual:
                    return valueLeft.Int32Value != valueRight.Int32Value;
                case BinaryOperatorKind.LongNotEqual:
                    return valueLeft.Int64Value != valueRight.Int64Value;
                case BinaryOperatorKind.UIntNotEqual:
                    return valueLeft.UInt32Value != valueRight.UInt32Value;
                case BinaryOperatorKind.ULongNotEqual:
                    return valueLeft.UInt64Value != valueRight.UInt64Value;
                case BinaryOperatorKind.DecimalLessThan:
                    return valueLeft.DecimalValue < valueRight.DecimalValue;
                case BinaryOperatorKind.FloatLessThan:
                    return valueLeft.SingleValue < valueRight.SingleValue;
                case BinaryOperatorKind.DoubleLessThan:
                    return valueLeft.DoubleValue < valueRight.DoubleValue;
                case BinaryOperatorKind.IntLessThan:
                    return valueLeft.Int32Value < valueRight.Int32Value;
                case BinaryOperatorKind.LongLessThan:
                    return valueLeft.Int64Value < valueRight.Int64Value;
                case BinaryOperatorKind.UIntLessThan:
                    return valueLeft.UInt32Value < valueRight.UInt32Value;
                case BinaryOperatorKind.ULongLessThan:
                    return valueLeft.UInt64Value < valueRight.UInt64Value;
                case BinaryOperatorKind.DecimalGreaterThan:
                    return valueLeft.DecimalValue > valueRight.DecimalValue;
                case BinaryOperatorKind.FloatGreaterThan:
                    return valueLeft.SingleValue > valueRight.SingleValue;
                case BinaryOperatorKind.DoubleGreaterThan:
                    return valueLeft.DoubleValue > valueRight.DoubleValue;
                case BinaryOperatorKind.IntGreaterThan:
                    return valueLeft.Int32Value > valueRight.Int32Value;
                case BinaryOperatorKind.LongGreaterThan:
                    return valueLeft.Int64Value > valueRight.Int64Value;
                case BinaryOperatorKind.UIntGreaterThan:
                    return valueLeft.UInt32Value > valueRight.UInt32Value;
                case BinaryOperatorKind.ULongGreaterThan:
                    return valueLeft.UInt64Value > valueRight.UInt64Value;
                case BinaryOperatorKind.DecimalLessThanOrEqual:
                    return valueLeft.DecimalValue <= valueRight.DecimalValue;
                case BinaryOperatorKind.FloatLessThanOrEqual:
                    return valueLeft.SingleValue <= valueRight.SingleValue;
                case BinaryOperatorKind.DoubleLessThanOrEqual:
                    return valueLeft.DoubleValue <= valueRight.DoubleValue;
                case BinaryOperatorKind.IntLessThanOrEqual:
                    return valueLeft.Int32Value <= valueRight.Int32Value;
                case BinaryOperatorKind.LongLessThanOrEqual:
                    return valueLeft.Int64Value <= valueRight.Int64Value;
                case BinaryOperatorKind.UIntLessThanOrEqual:
                    return valueLeft.UInt32Value <= valueRight.UInt32Value;
                case BinaryOperatorKind.ULongLessThanOrEqual:
                    return valueLeft.UInt64Value <= valueRight.UInt64Value;
                case BinaryOperatorKind.DecimalGreaterThanOrEqual:
                    return valueLeft.DecimalValue >= valueRight.DecimalValue;
                case BinaryOperatorKind.FloatGreaterThanOrEqual:
                    return valueLeft.SingleValue >= valueRight.SingleValue;
                case BinaryOperatorKind.DoubleGreaterThanOrEqual:
                    return valueLeft.DoubleValue >= valueRight.DoubleValue;
                case BinaryOperatorKind.IntGreaterThanOrEqual:
                    return valueLeft.Int32Value >= valueRight.Int32Value;
                case BinaryOperatorKind.LongGreaterThanOrEqual:
                    return valueLeft.Int64Value >= valueRight.Int64Value;
                case BinaryOperatorKind.UIntGreaterThanOrEqual:
                    return valueLeft.UInt32Value >= valueRight.UInt32Value;
                case BinaryOperatorKind.ULongGreaterThanOrEqual:
                    return valueLeft.UInt64Value >= valueRight.UInt64Value;
                case BinaryOperatorKind.UIntDivision:
                    return valueLeft.UInt32Value / valueRight.UInt32Value;
                case BinaryOperatorKind.ULongDivision:
                    return valueLeft.UInt64Value / valueRight.UInt64Value;

                // MinValue % -1 always overflows at runtime but never at compile time
                case BinaryOperatorKind.IntRemainder:
                    return (valueRight.Int32Value != -1) ? valueLeft.Int32Value % valueRight.Int32Value : 0;
                case BinaryOperatorKind.LongRemainder:
                    return (valueRight.Int64Value != -1) ? valueLeft.Int64Value % valueRight.Int64Value : 0;
                case BinaryOperatorKind.UIntRemainder:
                    return valueLeft.UInt32Value % valueRight.UInt32Value;
                case BinaryOperatorKind.ULongRemainder:
                    return valueLeft.UInt64Value % valueRight.UInt64Value;
            }

            return null;
        }

        /// <summary>
        /// Returns ConstantValue.Bad if, and only if, compound string length is out of supported limit.
        /// The <paramref name="compoundStringLength"/> parameter contains value corresponding to the
        /// left node, or zero, which will trigger inference. Upon return, it will
        /// be adjusted to correspond future result node.
        /// </summary>
        private static ConstantValue FoldStringConcatenation(BinaryOperatorKind kind, ConstantValue valueLeft, ConstantValue valueRight, ref int compoundStringLength)
        {
            Debug.Assert(valueLeft != null);
            Debug.Assert(valueRight != null);

            if (kind == BinaryOperatorKind.StringConcatenation)
            {
                string leftValue = valueLeft.StringValue ?? string.Empty;
                string rightValue = valueRight.StringValue ?? string.Empty;

                if (compoundStringLength == 0)
                {
                    // Infer. Keep it simple for now.
                    compoundStringLength = leftValue.Length;
                }

                Debug.Assert(compoundStringLength >= leftValue.Length);

                long newCompoundLength = (long)compoundStringLength + (long)leftValue.Length + (long)rightValue.Length;

                if (newCompoundLength > int.MaxValue)
                {
                    return ConstantValue.Bad;
                }

                ConstantValue result;

                try
                {
                    result = ConstantValue.Create(String.Concat(leftValue, rightValue));
                    compoundStringLength = (int)newCompoundLength;
                }
                catch (System.OutOfMemoryException)
                {
                    return ConstantValue.Bad;
                }

                return result;
            }

            return null;
        }

        private static BinaryOperatorKind SyntaxKindToBinaryOperatorKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.MultiplyExpression: return BinaryOperatorKind.Multiplication;
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.DivideExpression: return BinaryOperatorKind.Division;
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.ModuloExpression: return BinaryOperatorKind.Remainder;
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.AddExpression: return BinaryOperatorKind.Addition;
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.SubtractExpression: return BinaryOperatorKind.Subtraction;
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.RightShiftExpression: return BinaryOperatorKind.RightShift;
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.LeftShiftExpression: return BinaryOperatorKind.LeftShift;
                case SyntaxKind.EqualsExpression: return BinaryOperatorKind.Equal;
                case SyntaxKind.NotEqualsExpression: return BinaryOperatorKind.NotEqual;
                case SyntaxKind.GreaterThanExpression: return BinaryOperatorKind.GreaterThan;
                case SyntaxKind.LessThanExpression: return BinaryOperatorKind.LessThan;
                case SyntaxKind.GreaterThanOrEqualExpression: return BinaryOperatorKind.GreaterThanOrEqual;
                case SyntaxKind.LessThanOrEqualExpression: return BinaryOperatorKind.LessThanOrEqual;
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.BitwiseAndExpression: return BinaryOperatorKind.And;
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.BitwiseOrExpression: return BinaryOperatorKind.Or;
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.ExclusiveOrExpression: return BinaryOperatorKind.Xor;
                case SyntaxKind.LogicalAndExpression: return BinaryOperatorKind.LogicalAnd;
                case SyntaxKind.LogicalOrExpression: return BinaryOperatorKind.LogicalOr;
                default: throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private BoundExpression BindIncrementOperator(CSharpSyntaxNode node, ExpressionSyntax operandSyntax, SyntaxToken operatorToken, DiagnosticBag diagnostics)
        {
            operandSyntax.CheckDeconstructionCompatibleArgument(diagnostics);

            BoundExpression operand = BindToNaturalType(BindValue(operandSyntax, diagnostics, BindValueKind.IncrementDecrement), diagnostics);
            UnaryOperatorKind kind = SyntaxKindToUnaryOperatorKind(node.Kind());

            // If the operand is bad, avoid generating cascading errors.
            if (operand.HasAnyErrors)
            {
                // NOTE: no candidate user-defined operators.
                return new BoundIncrementOperator(
                    node,
                    kind,
                    operand,
                    null,
                    Conversion.NoConversion,
                    Conversion.NoConversion,
                    LookupResultKind.Empty,
                    CreateErrorType(),
                    hasErrors: true);
            }

            // The operand has to be a variable, property or indexer, so it must have a type.
            var operandType = operand.Type;
            Debug.Assert((object)operandType != null);

            if (operandType.IsDynamic())
            {
                return new BoundIncrementOperator(
                    node,
                    kind.WithType(UnaryOperatorKind.Dynamic).WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                    operand,
                    methodOpt: null,
                    operandConversion: Conversion.NoConversion,
                    resultConversion: Conversion.NoConversion,
                    resultKind: LookupResultKind.Viable,
                    originalUserDefinedOperatorsOpt: default(ImmutableArray<MethodSymbol>),
                    type: operandType,
                    hasErrors: false);
            }

            LookupResultKind resultKind;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            var best = this.UnaryOperatorOverloadResolution(kind, operand, node, diagnostics, out resultKind, out originalUserDefinedOperators);
            if (!best.HasValue)
            {
                ReportUnaryOperatorError(node, diagnostics, operatorToken.Text, operand, resultKind);
                return new BoundIncrementOperator(
                    node,
                    kind,
                    operand,
                    null,
                    Conversion.NoConversion,
                    Conversion.NoConversion,
                    resultKind,
                    originalUserDefinedOperators,
                    CreateErrorType(),
                    hasErrors: true);
            }

            var signature = best.Signature;

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var resultConversion = Conversions.ClassifyConversionFromType(signature.ReturnType, operandType, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);

            bool hasErrors = false;
            if (!resultConversion.IsImplicit || !resultConversion.IsValid)
            {
                GenerateImplicitConversionError(diagnostics, this.Compilation, node, resultConversion, signature.ReturnType, operandType);
                hasErrors = true;
            }
            else
            {
                ReportDiagnosticsIfObsolete(diagnostics, resultConversion, node, hasBaseReceiver: false);
            }

            if (!hasErrors && operandType.IsVoidPointer())
            {
                Error(diagnostics, ErrorCode.ERR_VoidError, node);
                hasErrors = true;
            }

            Conversion operandConversion = best.Conversion;

            ReportDiagnosticsIfObsolete(diagnostics, operandConversion, node, hasBaseReceiver: false);

            return new BoundIncrementOperator(
                node,
                signature.Kind.WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                operand,
                signature.Method,
                operandConversion,
                resultConversion,
                resultKind,
                originalUserDefinedOperators,
                operandType,
                hasErrors);
        }

        private BoundExpression BindSuppressNullableWarningExpression(PostfixUnaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var expr = BindExpression(node.Operand, diagnostics);
            switch (expr.Kind)
            {
                case BoundKind.NamespaceExpression:
                case BoundKind.TypeExpression:
                    Error(diagnostics, ErrorCode.ERR_IllegalSuppression, expr.Syntax);
                    break;
                default:
                    if (expr.IsSuppressed)
                    {
                        Debug.Assert(node.Operand.SkipParens().GetLastToken().Kind() == SyntaxKind.ExclamationToken);
                        Error(diagnostics, ErrorCode.ERR_DuplicateNullSuppression, expr.Syntax);
                    }
                    break;
            }

            return expr.WithSuppression();
        }

        // Based on ExpressionBinder::bindPtrIndirection.
        private BoundExpression BindPointerIndirectionExpression(PrefixUnaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression operand = BindToNaturalType(BindValue(node.Operand, diagnostics, GetUnaryAssignmentKind(node.Kind())), diagnostics);

            TypeSymbol pointedAtType;
            bool hasErrors;
            BindPointerIndirectionExpressionInternal(node, operand, diagnostics, out pointedAtType, out hasErrors);

            return new BoundPointerIndirectionOperator(node, operand, pointedAtType ?? CreateErrorType(), hasErrors);
        }

        private static void BindPointerIndirectionExpressionInternal(CSharpSyntaxNode node, BoundExpression operand, DiagnosticBag diagnostics, out TypeSymbol pointedAtType, out bool hasErrors)
        {
            var operandType = operand.Type as PointerTypeSymbol;

            hasErrors = operand.HasAnyErrors; // This would propagate automatically, but by reading it explicitly we can reduce cascading.

            if ((object)operandType == null)
            {
                pointedAtType = null;

                if (!hasErrors)
                {
                    // NOTE: Dev10 actually reports ERR_BadUnaryOp if the operand has Type == null,
                    // but this seems clearer.
                    Error(diagnostics, ErrorCode.ERR_PtrExpected, node);
                    hasErrors = true;
                }
            }
            else
            {
                pointedAtType = operandType.PointedAtType;

                if (pointedAtType.IsVoidType())
                {
                    pointedAtType = null;

                    if (!hasErrors)
                    {
                        Error(diagnostics, ErrorCode.ERR_VoidError, node);
                        hasErrors = true;
                    }
                }
            }
        }

        // Based on ExpressionBinder::bindPtrAddr.
        private BoundExpression BindAddressOfExpression(PrefixUnaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression operand = BindToNaturalType(BindValue(node.Operand, diagnostics, BindValueKind.AddressOf), diagnostics);
            ReportSuppressionIfNeeded(operand, diagnostics);

            bool hasErrors = operand.HasAnyErrors; // This would propagate automatically, but by reading it explicitly we can reduce cascading.
            bool isFixedStatementAddressOfExpression = SyntaxFacts.IsFixedStatementExpression(node);

            switch (operand.Kind)
            {
                case BoundKind.MethodGroup:
                case BoundKind.Lambda:
                case BoundKind.UnboundLambda:
                    {
                        Debug.Assert(hasErrors);
                        return new BoundAddressOfOperator(node, operand, CreateErrorType(), hasErrors: true);
                    }
            }

            TypeSymbol operandType = operand.Type;
            Debug.Assert((object)operandType != null, "BindValue should have caught a null operand type");

            bool isManagedType = operandType.IsManagedType;
            bool allowManagedAddressOf = Flags.Includes(BinderFlags.AllowManagedAddressOf);
            if (!allowManagedAddressOf)
            {
                if (!hasErrors)
                {
                    hasErrors = CheckManagedAddr(Compilation, operandType, node.Location, diagnostics);
                }

                if (!hasErrors)
                {
                    Symbol accessedLocalOrParameterOpt;
                    if (IsMoveableVariable(operand, out accessedLocalOrParameterOpt) != isFixedStatementAddressOfExpression)
                    {
                        Error(diagnostics, isFixedStatementAddressOfExpression ? ErrorCode.ERR_FixedNotNeeded : ErrorCode.ERR_FixedNeeded, node);
                        hasErrors = true;
                    }
                }
            }

            TypeSymbol pointedAtType = isManagedType && allowManagedAddressOf
                ? GetSpecialType(SpecialType.System_IntPtr, diagnostics, node)
                : operandType ?? CreateErrorType();
            TypeSymbol pointerType = new PointerTypeSymbol(TypeWithAnnotations.Create(pointedAtType));

            return new BoundAddressOfOperator(node, operand, pointerType, hasErrors);
        }

        /// <summary>
        /// Checks to see whether an expression is a "moveable" variable according to the spec. Moveable
        /// variables have underlying memory which may be moved by the runtime. The spec defines anything
        /// not fixed as moveable and specifies the expressions which are fixed.
        /// </summary>

        internal bool IsMoveableVariable(BoundExpression expr, out Symbol accessedLocalOrParameterOpt)
        {
            accessedLocalOrParameterOpt = null;

            while (true)
            {
                BoundKind exprKind = expr.Kind;
                switch (exprKind)
                {
                    case BoundKind.FieldAccess:
                    case BoundKind.EventAccess:
                        {
                            FieldSymbol fieldSymbol;
                            BoundExpression receiver;
                            if (exprKind == BoundKind.FieldAccess)
                            {
                                BoundFieldAccess fieldAccess = (BoundFieldAccess)expr;
                                fieldSymbol = fieldAccess.FieldSymbol;
                                receiver = fieldAccess.ReceiverOpt;
                            }
                            else
                            {
                                BoundEventAccess eventAccess = (BoundEventAccess)expr;
                                if (!eventAccess.IsUsableAsField || eventAccess.EventSymbol.IsWindowsRuntimeEvent)
                                {
                                    return true;
                                }
                                EventSymbol eventSymbol = eventAccess.EventSymbol;
                                fieldSymbol = eventSymbol.AssociatedField;
                                receiver = eventAccess.ReceiverOpt;
                            }

                            if ((object)fieldSymbol == null || fieldSymbol.IsStatic || (object)receiver == null)
                            {
                                return true;
                            }

                            var unusedDiagnostics = DiagnosticBag.GetInstance();
                            bool receiverIsLValue = CheckValueKind(receiver.Syntax, receiver, BindValueKind.AddressOf, checkingReceiver: false, diagnostics: unusedDiagnostics);
                            unusedDiagnostics.Free();

                            if (!receiverIsLValue)
                            {
                                return true;
                            }

                            // NOTE: type parameters will already have been weeded out, since a
                            // variable of type parameter type has to be cast to an effective
                            // base or interface type before its fields can be accessed and a
                            // conversion isn't an lvalue.
                            if (receiver.Type.IsReferenceType)
                            {
                                return true;
                            }

                            expr = receiver;
                            continue;
                        }
                    case BoundKind.RangeVariable:
                        {
                            // NOTE: there are cases where you can take the address of a range variable.
                            // e.g. from x in new int[3] select *(&x)
                            BoundRangeVariable variableAccess = (BoundRangeVariable)expr;
                            expr = variableAccess.Value; //Check the underlying expression.
                            continue;
                        }
                    case BoundKind.Parameter:
                        {
                            BoundParameter parameterAccess = (BoundParameter)expr;
                            ParameterSymbol parameterSymbol = parameterAccess.ParameterSymbol;
                            accessedLocalOrParameterOpt = parameterSymbol;
                            return parameterSymbol.RefKind != RefKind.None;
                        }
                    case BoundKind.ThisReference:
                    case BoundKind.BaseReference:
                        {
                            accessedLocalOrParameterOpt = this.ContainingMemberOrLambda.EnclosingThisSymbol();
                            return true;
                        }
                    case BoundKind.Local:
                        {
                            BoundLocal localAccess = (BoundLocal)expr;
                            LocalSymbol localSymbol = localAccess.LocalSymbol;
                            accessedLocalOrParameterOpt = localSymbol;
                            // NOTE: The spec says that this is moveable if it is captured by an anonymous function,
                            // but that will be reported separately and error-recovery is better if we say that
                            // such locals are not moveable.
                            return localSymbol.RefKind != RefKind.None;
                        }
                    case BoundKind.PointerIndirectionOperator: //Covers ->, since the receiver will be one of these.
                    case BoundKind.ConvertedStackAllocExpression:
                        {
                            return false;
                        }
                    case BoundKind.PointerElementAccess:
                        {
                            // C# 7.3:
                            // a variable resulting from a... pointer_element_access of the form P[E] [is fixed] if P
                            // is not a fixed size buffer expression, or if the expression is a fixed size buffer
                            // member_access of the form E.I and E is a fixed variable
                            BoundExpression underlyingExpr = ((BoundPointerElementAccess)expr).Expression;
                            if (underlyingExpr is BoundFieldAccess fieldAccess && fieldAccess.FieldSymbol.IsFixedSizeBuffer)
                            {
                                expr = fieldAccess.ReceiverOpt;
                                continue;
                            }

                            return false;
                        }
                    case BoundKind.PropertyAccess: // Never fixed
                    case BoundKind.IndexerAccess: // Never fixed
                    default:
                        {
                            return true;
                        }
                }
            }
        }

        private BoundExpression BindUnaryOperator(PrefixUnaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression operand = BindToNaturalType(BindValue(node.Operand, diagnostics, GetUnaryAssignmentKind(node.Kind())), diagnostics);
            BoundLiteral constant = BindIntegralMinValConstants(node, operand, diagnostics);
            return constant ?? BindUnaryOperatorCore(node, node.OperatorToken.Text, operand, diagnostics);
        }

        private void ReportSuppressionIfNeeded(BoundExpression expr, DiagnosticBag diagnostics)
        {
            if (expr.IsSuppressed)
            {
                Error(diagnostics, ErrorCode.ERR_IllegalSuppression, expr.Syntax);
            }
        }

        private BoundExpression BindUnaryOperatorCore(CSharpSyntaxNode node, string operatorText, BoundExpression operand, DiagnosticBag diagnostics)
        {
            UnaryOperatorKind kind = SyntaxKindToUnaryOperatorKind(node.Kind());

            bool isOperandTypeNull = operand.IsLiteralNull() || operand.IsLiteralDefault();
            if (isOperandTypeNull)
            {
                // Dev10 does not allow unary prefix operators to be applied to the null literal
                // (or other typeless expressions).
                Error(diagnostics, ErrorCode.ERR_BadOpOnNullOrDefault, node, operatorText, operand.Display);
            }

            // If the operand is bad, avoid generating cascading errors.
            if (operand.HasAnyErrors || isOperandTypeNull)
            {
                // Note: no candidate user-defined operators.
                return new BoundUnaryOperator(node, kind, operand, ConstantValue.NotAvailable,
                    methodOpt: null,
                    resultKind: LookupResultKind.Empty,
                    type: CreateErrorType(),
                    hasErrors: true);
            }

            // If the operand is dynamic then we do not attempt to do overload resolution at compile
            // time; we defer that until runtime. If we did overload resolution then the dynamic
            // operand would be implicitly convertible to the parameter type of each operator
            // signature, and therefore every operator would be an applicable candidate. Instead
            // of changing overload resolution to handle dynamic, we just handle it here and let
            // overload resolution implement the specification.

            if (operand.HasDynamicType())
            {
                return new BoundUnaryOperator(
                    syntax: node,
                    operatorKind: kind.WithType(UnaryOperatorKind.Dynamic).WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                    operand: operand,
                    constantValueOpt: ConstantValue.NotAvailable,
                    methodOpt: null,
                    resultKind: LookupResultKind.Viable,
                    type: operand.Type);
            }

            LookupResultKind resultKind;
            ImmutableArray<MethodSymbol> originalUserDefinedOperators;
            var best = this.UnaryOperatorOverloadResolution(kind, operand, node, diagnostics, out resultKind, out originalUserDefinedOperators);
            if (!best.HasValue)
            {
                ReportUnaryOperatorError(node, diagnostics, operatorText, operand, resultKind);
                return new BoundUnaryOperator(
                    node,
                    kind,
                    operand,
                    ConstantValue.NotAvailable,
                    null,
                    resultKind,
                    originalUserDefinedOperators,
                    CreateErrorType(),
                    hasErrors: true);
            }

            var signature = best.Signature;

            var resultOperand = CreateConversion(operand.Syntax, operand, best.Conversion, isCast: false, conversionGroupOpt: null, signature.OperandType, diagnostics);
            var resultType = signature.ReturnType;
            UnaryOperatorKind resultOperatorKind = signature.Kind;
            var resultMethod = signature.Method;
            var resultConstant = FoldUnaryOperator(node, resultOperatorKind, resultOperand, resultType.SpecialType, diagnostics);

            return new BoundUnaryOperator(
                node,
                resultOperatorKind.WithOverflowChecksIfApplicable(CheckOverflowAtRuntime),
                resultOperand,
                resultConstant,
                resultMethod,
                resultKind,
                resultType);
        }

        private ConstantValue FoldEnumUnaryOperator(
            CSharpSyntaxNode syntax,
            UnaryOperatorKind kind,
            BoundExpression operand,
            DiagnosticBag diagnostics)
        {
            var underlyingType = operand.Type.GetEnumUnderlyingType();

            BoundExpression newOperand = CreateConversion(operand, underlyingType, diagnostics);

            // We may have to upconvert the type if it is a byte, sbyte, short, ushort
            // or nullable of those, because there is no ~ operator
            var upconvertSpecialType = GetEnumPromotedType(underlyingType.SpecialType);
            var upconvertType = upconvertSpecialType == underlyingType.SpecialType ?
                underlyingType :
                GetSpecialType(upconvertSpecialType, diagnostics, syntax);

            newOperand = CreateConversion(newOperand, upconvertType, diagnostics);

            UnaryOperatorKind newKind = kind.Operator().WithType(upconvertSpecialType);

            var constantValue = FoldUnaryOperator(syntax, newKind, operand, upconvertType.SpecialType, diagnostics);

            // Convert back to the underlying type
            if (!constantValue.IsBad)
            {
                // Do an unchecked conversion if bitwise complement
                var binder = kind.Operator() == UnaryOperatorKind.BitwiseComplement ?
                    this.WithCheckedOrUncheckedRegion(@checked: false) : this;
                return binder.FoldConstantNumericConversion(syntax, constantValue, underlyingType, diagnostics);
            }

            return constantValue;
        }

        private ConstantValue FoldUnaryOperator(
            CSharpSyntaxNode syntax,
            UnaryOperatorKind kind,
            BoundExpression operand,
            SpecialType resultType,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(operand != null);
            // UNDONE: report errors when in a checked context.

            if (operand.HasAnyErrors)
            {
                return null;
            }

            var value = operand.ConstantValue;
            if (value == null || value.IsBad)
            {
                return value;
            }

            if (kind.IsEnum() && !kind.IsLifted())
            {
                return FoldEnumUnaryOperator(syntax, kind, operand, diagnostics);
            }

            var newValue = FoldNeverOverflowUnaryOperator(kind, value);
            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            if (CheckOverflowAtCompileTime)
            {
                try
                {
                    newValue = FoldCheckedIntegralUnaryOperator(kind, value);
                }
                catch (OverflowException)
                {
                    Error(diagnostics, ErrorCode.ERR_CheckedOverflow, syntax);
                    return ConstantValue.Bad;
                }
            }
            else
            {
                newValue = FoldUncheckedIntegralUnaryOperator(kind, value);
            }

            if (newValue != null)
            {
                return ConstantValue.Create(newValue, resultType);
            }

            return null;
        }

        private static object FoldNeverOverflowUnaryOperator(UnaryOperatorKind kind, ConstantValue value)
        {
            // Note that we do operations on single-precision floats as double-precision.
            switch (kind)
            {
                case UnaryOperatorKind.DecimalUnaryMinus:
                    return -value.DecimalValue;
                case UnaryOperatorKind.DoubleUnaryMinus:
                case UnaryOperatorKind.FloatUnaryMinus:
                    return -value.DoubleValue;
                case UnaryOperatorKind.DecimalUnaryPlus:
                    return +value.DecimalValue;
                case UnaryOperatorKind.FloatUnaryPlus:
                case UnaryOperatorKind.DoubleUnaryPlus:
                    return +value.DoubleValue;
                case UnaryOperatorKind.LongUnaryPlus:
                    return +value.Int64Value;
                case UnaryOperatorKind.ULongUnaryPlus:
                    return +value.UInt64Value;
                case UnaryOperatorKind.IntUnaryPlus:
                    return +value.Int32Value;
                case UnaryOperatorKind.UIntUnaryPlus:
                    return +value.UInt32Value;
                case UnaryOperatorKind.BoolLogicalNegation:
                    return !value.BooleanValue;
                case UnaryOperatorKind.IntBitwiseComplement:
                    return ~value.Int32Value;
                case UnaryOperatorKind.LongBitwiseComplement:
                    return ~value.Int64Value;
                case UnaryOperatorKind.UIntBitwiseComplement:
                    return ~value.UInt32Value;
                case UnaryOperatorKind.ULongBitwiseComplement:
                    return ~value.UInt64Value;
            }

            return null;
        }

        private static object FoldUncheckedIntegralUnaryOperator(UnaryOperatorKind kind, ConstantValue value)
        {
            unchecked
            {
                switch (kind)
                {
                    case UnaryOperatorKind.LongUnaryMinus:
                        return -value.Int64Value;
                    case UnaryOperatorKind.IntUnaryMinus:
                        return -value.Int32Value;
                }
            }

            return null;
        }

        private static object FoldCheckedIntegralUnaryOperator(UnaryOperatorKind kind, ConstantValue value)
        {
            checked
            {
                switch (kind)
                {
                    case UnaryOperatorKind.LongUnaryMinus:
                        return -value.Int64Value;
                    case UnaryOperatorKind.IntUnaryMinus:
                        return -value.Int32Value;
                }
            }

            return null;
        }

        private static UnaryOperatorKind SyntaxKindToUnaryOperatorKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PreIncrementExpression: return UnaryOperatorKind.PrefixIncrement;
                case SyntaxKind.PostIncrementExpression: return UnaryOperatorKind.PostfixIncrement;
                case SyntaxKind.PreDecrementExpression: return UnaryOperatorKind.PrefixDecrement;
                case SyntaxKind.PostDecrementExpression: return UnaryOperatorKind.PostfixDecrement;
                case SyntaxKind.UnaryPlusExpression: return UnaryOperatorKind.UnaryPlus;
                case SyntaxKind.UnaryMinusExpression: return UnaryOperatorKind.UnaryMinus;
                case SyntaxKind.LogicalNotExpression: return UnaryOperatorKind.LogicalNegation;
                case SyntaxKind.BitwiseNotExpression: return UnaryOperatorKind.BitwiseComplement;
                default: throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private static BindValueKind GetBinaryAssignmentKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.SimpleAssignmentExpression:
                    return BindValueKind.Assignable;
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.CoalesceAssignmentExpression:
                    return BindValueKind.CompoundAssignment;
                default:
                    return BindValueKind.RValue;
            }
        }

        private static BindValueKind GetUnaryAssignmentKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PreDecrementExpression:
                case SyntaxKind.PreIncrementExpression:
                case SyntaxKind.PostDecrementExpression:
                case SyntaxKind.PostIncrementExpression:
                    return BindValueKind.IncrementDecrement;
                case SyntaxKind.AddressOfExpression:
                    Debug.Assert(false, "Should be handled separately.");
                    goto default;
                default:
                    return BindValueKind.RValue;
            }
        }

        private BoundLiteral BindIntegralMinValConstants(PrefixUnaryExpressionSyntax node, BoundExpression operand, DiagnosticBag diagnostics)
        {
            // SPEC: To permit the smallest possible int and long values to be written as decimal integer
            // SPEC: literals, the following two rules exist:

            // SPEC: When a decimal-integer-literal with the value 2147483648 and no integer-type-suffix
            // SPEC: appears as the token immediately following a unary minus operator token, the result is a
            // SPEC: constant of type int with the value −2147483648.

            // SPEC: When a decimal-integer-literal with the value 9223372036854775808 and no integer-type-suffix
            // SPEC: or the integer-type-suffix L or l appears as the token immediately following a unary minus
            // SPEC: operator token, the result is a constant of type long with the value −9223372036854775808.

            if (node.Kind() != SyntaxKind.UnaryMinusExpression)
            {
                return null;
            }

            if (node.Operand != operand.Syntax || operand.Syntax.Kind() != SyntaxKind.NumericLiteralExpression)
            {
                return null;
            }

            var literal = (LiteralExpressionSyntax)operand.Syntax;
            var token = literal.Token;
            if (token.Value is uint)
            {
                uint value = (uint)token.Value;
                if (value != 2147483648U)
                {
                    return null;
                }

                if (token.Text.Contains("u") || token.Text.Contains("U") || token.Text.Contains("l") || token.Text.Contains("L"))
                {
                    return null;
                }

                return new BoundLiteral(node, ConstantValue.Create((int)-2147483648), GetSpecialType(SpecialType.System_Int32, diagnostics, node));
            }
            else if (token.Value is ulong)
            {
                var value = (ulong)token.Value;
                if (value != 9223372036854775808UL)
                {
                    return null;
                }

                if (token.Text.Contains("u") || token.Text.Contains("U"))
                {
                    return null;
                }

                return new BoundLiteral(node, ConstantValue.Create(-9223372036854775808), GetSpecialType(SpecialType.System_Int64, diagnostics, node));
            }

            return null;
        }

        private static bool IsDivisionByZero(BinaryOperatorKind kind, ConstantValue valueRight)
        {
            Debug.Assert(valueRight != null);

            switch (kind)
            {
                case BinaryOperatorKind.DecimalDivision:
                case BinaryOperatorKind.DecimalRemainder:
                    return valueRight.DecimalValue == 0.0m;
                case BinaryOperatorKind.IntDivision:
                case BinaryOperatorKind.IntRemainder:
                    return valueRight.Int32Value == 0;
                case BinaryOperatorKind.LongDivision:
                case BinaryOperatorKind.LongRemainder:
                    return valueRight.Int64Value == 0;
                case BinaryOperatorKind.UIntDivision:
                case BinaryOperatorKind.UIntRemainder:
                    return valueRight.UInt32Value == 0;
                case BinaryOperatorKind.ULongDivision:
                case BinaryOperatorKind.ULongRemainder:
                    return valueRight.UInt64Value == 0;
            }

            return false;
        }

        private bool IsOperandErrors(CSharpSyntaxNode node, ref BoundExpression operand, DiagnosticBag diagnostics)
        {
            switch (operand.Kind)
            {
                case BoundKind.UnboundLambda:
                case BoundKind.Lambda:
                case BoundKind.MethodGroup:  // New in Roslyn - see DevDiv #864740.
                    // operand for an is or as expression cannot be a lambda expression or method group
                    if (!operand.HasAnyErrors)
                    {
                        Error(diagnostics, ErrorCode.ERR_LambdaInIsAs, node);
                    }

                    operand = BadExpression(node, operand).MakeCompilerGenerated();
                    return true;

                default:
                    if ((object)operand.Type == null && !operand.IsLiteralNull())
                    {
                        if (!operand.HasAnyErrors)
                        {
                            // Operator 'is' cannot be applied to operand of type '(int, <null>)'
                            Error(diagnostics, ErrorCode.ERR_BadUnaryOp, node, SyntaxFacts.GetText(SyntaxKind.IsKeyword), operand.Display);
                        }

                        operand = BadExpression(node, operand).MakeCompilerGenerated();
                        return true;
                    }

                    break;
            }

            return operand.HasAnyErrors;
        }

        private bool IsOperatorErrors(CSharpSyntaxNode node, TypeSymbol operandType, BoundTypeExpression typeExpression, DiagnosticBag diagnostics)
        {
            var targetType = typeExpression.Type;
            var targetTypeKind = targetType.TypeKind;

            // The native compiler allows "x is C" where C is a static class. This
            // is strictly illegal according to the specification (see the section
            // called "Referencing Static Class Types".) To retain compatibility we
            // allow it, but when /feature:strict is enabled we break with the native
            // compiler and turn this into an error, as it should be.
            if (targetType.IsStatic && Compilation.FeatureStrictEnabled)
            {
                Error(diagnostics, ErrorCode.ERR_StaticInAsOrIs, node, targetType);
                return true;
            }

            if ((object)operandType != null && operandType.TypeKind == TypeKind.Pointer || targetTypeKind == TypeKind.Pointer)
            {
                // operand for an is or as expression cannot be of pointer type
                Error(diagnostics, ErrorCode.ERR_PointerInAsOrIs, node);
                return true;
            }

            return targetTypeKind == TypeKind.Error;
        }

        private BoundExpression BindIsOperator(BinaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var resultType = (TypeSymbol)GetSpecialType(SpecialType.System_Boolean, diagnostics, node);
            var operand = BindRValueWithoutTargetType(node.Left, diagnostics);
            var operandHasErrors = IsOperandErrors(node, ref operand, diagnostics);
            // try binding as a type, but back off to binding as an expression if that does not work.
            AliasSymbol alias;
            var isTypeDiagnostics = DiagnosticBag.GetInstance();
            TypeWithAnnotations targetTypeWithAnnotations = BindType(node.Right, isTypeDiagnostics, out alias);
            TypeSymbol targetType = targetTypeWithAnnotations.Type;

            bool wasUnderscore = node.Right is IdentifierNameSyntax name && name.Identifier.ContextualKind() == SyntaxKind.UnderscoreToken;
            if (!wasUnderscore && targetType?.IsErrorType() == true && isTypeDiagnostics.HasAnyResolvedErrors() &&
                ((CSharpParseOptions)node.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeaturePatternMatching))
            {
                // it did not bind as a type; try binding as a constant expression pattern
                bool wasExpression;
                var isPatternDiagnostics = DiagnosticBag.GetInstance();
                if ((object)operand.Type == null)
                {
                    if (!operandHasErrors)
                    {
                        isPatternDiagnostics.Add(ErrorCode.ERR_BadPatternExpression, node.Left.Location, operand.Display);
                    }

                    operand = ToBadExpression(operand);
                }

                var boundConstantPattern = BindConstantPattern(
                    node.Right, operand.Type, node.Right, node.Right.HasErrors, isPatternDiagnostics, out wasExpression);
                boundConstantPattern.WasCompilerGenerated = true;
                if (wasExpression)
                {
                    isTypeDiagnostics.Free();
                    diagnostics.AddRangeAndFree(isPatternDiagnostics);
                    return MakeIsPatternExpression(node, operand, boundConstantPattern, resultType, operandHasErrors, diagnostics);
                }

                isPatternDiagnostics.Free();
            }

            diagnostics.AddRangeAndFree(isTypeDiagnostics);
            if (targetType.IsReferenceType && targetTypeWithAnnotations.NullableAnnotation.IsAnnotated())
            {
                Error(diagnostics, ErrorCode.ERR_IsNullableType, node.Right, targetType);
                operandHasErrors = true;
            }

            var typeExpression = new BoundTypeExpression(node.Right, alias, targetTypeWithAnnotations);
            var targetTypeKind = targetType.TypeKind;
            if (operandHasErrors || IsOperatorErrors(node, operand.Type, typeExpression, diagnostics))
            {
                return new BoundIsOperator(node, operand, typeExpression, Conversion.NoConversion, resultType, hasErrors: true);
            }

            if (wasUnderscore && ((CSharpParseOptions)node.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeatureRecursivePatterns))
            {
                diagnostics.Add(ErrorCode.WRN_IsTypeNamedUnderscore, node.Right.Location, alias ?? (Symbol)targetType);
            }

            // Is and As operator should have null ConstantValue as they are not constant expressions.
            // However we perform analysis of is/as expressions at bind time to detect if the expression
            // will always evaluate to a constant to generate warnings (always true/false/null).
            // We also need this analysis result during rewrite to optimize away redundant isinst instructions.
            // We store the conversion from expression's operand type to target type to enable these
            // optimizations during is/as operator rewrite.

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (operand.ConstantValue == ConstantValue.Null ||
                operand.Kind == BoundKind.MethodGroup ||
                operand.Type.IsVoidType())
            {
                // warning for cases where the result is always false:
                // (a) "null is TYPE" OR operand evaluates to null
                // (b) operand is a MethodGroup
                // (c) operand is of void type

                // NOTE:    Dev10 violates the SPEC for case (c) above and generates
                // NOTE:    an error ERR_NoExplicitBuiltinConv if the target type
                // NOTE:    is an open type. According to the specification, the result
                // NOTE:    is always false, but no compile time error occurs.
                // NOTE:    We follow the specification and generate WRN_IsAlwaysFalse
                // NOTE:    instead of an error.
                // NOTE:    See Test SyntaxBinderTests.TestIsOperatorWithTypeParameter

                Error(diagnostics, ErrorCode.WRN_IsAlwaysFalse, node, targetType);
                Conversion conv = Conversions.ClassifyConversionFromExpression(operand, targetType, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
                return new BoundIsOperator(node, operand, typeExpression, conv, resultType);
            }

            if (targetTypeKind == TypeKind.Dynamic)
            {
                // warning for dynamic target type
                Error(diagnostics, ErrorCode.WRN_IsDynamicIsConfusing,
                    node, node.OperatorToken.Text, targetType.Name,
                    GetSpecialType(SpecialType.System_Object, diagnostics, node).Name // a pretty way of getting the string "Object"
                    );
            }

            var operandType = operand.Type;
            Debug.Assert((object)operandType != null);
            if (operandType.TypeKind == TypeKind.Dynamic)
            {
                // if operand has a dynamic type, we do the same thing as though it were an object
                operandType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
            }

            Conversion conversion = Conversions.ClassifyBuiltInConversion(operandType, targetType, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            ReportIsOperatorConstantWarnings(node, diagnostics, operandType, targetType, conversion.Kind, operand.ConstantValue);
            return new BoundIsOperator(node, operand, typeExpression, conversion, resultType);
        }

        private static void ReportIsOperatorConstantWarnings(
            CSharpSyntaxNode syntax,
            DiagnosticBag diagnostics,
            TypeSymbol operandType,
            TypeSymbol targetType,
            ConversionKind conversionKind,
            ConstantValue operandConstantValue)
        {
            // NOTE:    Even though BoundIsOperator and BoundAsOperator will always have no ConstantValue
            // NOTE:    (they are non-constant expressions according to Section 7.19 of the specification),
            // NOTE:    we want to perform constant analysis of is/as expressions to generate warnings if the
            // NOTE:    expression will always be true/false/null.

            ConstantValue constantValue = GetIsOperatorConstantResult(operandType, targetType, conversionKind, operandConstantValue);
            if (constantValue != null)
            {
                Debug.Assert(constantValue == ConstantValue.True || constantValue == ConstantValue.False);

                ErrorCode errorCode = constantValue == ConstantValue.True ? ErrorCode.WRN_IsAlwaysTrue : ErrorCode.WRN_IsAlwaysFalse;
                Error(diagnostics, errorCode, syntax, targetType);
            }
        }

        internal static ConstantValue GetIsOperatorConstantResult(
            TypeSymbol operandType,
            TypeSymbol targetType,
            ConversionKind conversionKind,
            ConstantValue operandConstantValue,
            bool operandCouldBeNull = true)
        {
            Debug.Assert((object)targetType != null);

            // SPEC:    The result of the operation depends on D and T as follows:
            // SPEC:    1)      If T is a reference type, the result is true if D and T are the same type, if D is a reference type and
            // SPEC:        an implicit reference conversion from D to T exists, or if D is a value type and a boxing conversion from D to T exists.
            // SPEC:    2)      If T is a nullable type, the result is true if D is the underlying type of T.
            // SPEC:    3)      If T is a non-nullable value type, the result is true if D and T are the same type.
            // SPEC:    4)      Otherwise, the result is false.

            // NOTE:    The language specification talks about the runtime evaluation of the is operation.
            // NOTE:    However, we are interested in computing the compile time constant value for the expression.
            // NOTE:    Even though BoundIsOperator and BoundAsOperator will always have no ConstantValue
            // NOTE:    (they are non-constant expressions according to Section 7.19 of the specification),
            // NOTE:    we want to perform constant analysis of is/as expressions during binding to generate warnings
            // NOTE:    (always true/false/null) and during rewriting for optimized codegen.
            // NOTE:
            // NOTE:    Because the heuristic presented here is used to change codegen, it must be conservative. It is acceptable
            // NOTE:    for us to fail to report a warning in cases where humans could logically deduce that the operator will
            // NOTE:    always return false. It is not acceptable to inaccurately warn that the operator will always return false
            // NOTE:    if there are cases where it might succeed.
            // NOTE:
            // NOTE:    These same heuristics are also used in pattern-matching to determine if an expression of the form
            // NOTE:    `e is T x` is permitted. It is an error if `e` cannot be of type `T` according to this method
            // NOTE:    returning ConstantValue.False.
            // NOTE:    The heuristics are also used to determine if a `case T1 x1:` is subsumed by
            // NOTE:    some previous `case T2 x2:` in a switch statement. For that purpose operandType is T1, targetType is T2,
            // NOTE:    and operandCouldBeNull is false; the former subsumes the latter if this method returns ConstantValue.True.
            // NOTE:    Since the heuristic is now used to produce errors in pattern-matching, making it more accurate in the
            // NOTE:    future could be a breaking change.

            // To begin our heuristic: if the operand is literal null then we automatically return that the
            // result is false. You might think that we can simply check to see if the conversion is
            // ConversionKind.NullConversion, but "null is T" for a type parameter T is actually classified
            // as an implicit reference conversion if T is constrained to reference types. Rather
            // than deal with all those special cases we can simply bail out here.

            if (operandConstantValue == ConstantValue.Null)
            {
                return ConstantValue.False;
            }

            Debug.Assert((object)operandType != null);

            operandCouldBeNull =
                operandCouldBeNull &&
                operandType.CanContainNull() && // a non-nullable value type is never null
                (operandConstantValue == null || operandConstantValue == ConstantValue.Null); // a non-null constant is never null

            switch (conversionKind)
            {
                case ConversionKind.NoConversion:
                    // Oddly enough, "x is T" can be true even if there is no conversion from x to T!
                    //
                    // Scenario 1: Type parameter compared to System.Enum.
                    //
                    // bool M1<X>(X x) where X : struct { return x is Enum; }
                    //
                    // There is no conversion from X to Enum, not even an explicit conversion. But
                    // nevertheless, X could be constructed as an enumerated type.
                    // However, we can sometimes know that the result will be false.
                    //
                    // Scenario 2a: Constrained type parameter compared to reference type.
                    //
                    // bool M2a<X>(X x) where X : struct { return x is string; }
                    //
                    // We know that X, constrained to struct, will never be string.
                    //
                    // Scenario 2b: Reference type compared to constrained type parameter.
                    //
                    // bool M2b<X>(string x) where X : struct { return x is X; }
                    //
                    // We know that string will never be X, constrained to struct.
                    //
                    // Scenario 3: Value type compared to type parameter.
                    //
                    // bool M3<T>(int x) { return x is T; }
                    //
                    // There is no conversion from int to T, but T could nevertheless be int.
                    //
                    // Scenario 4: Constructed type compared to open type
                    //
                    // bool M4<T>(C<int> x) { return x is C<T>; }
                    //
                    // There is no conversion from C<int> to C<T>, but nevertheless, T might be int.
                    //
                    // Scenario 5: Open type compared to constructed type:
                    //
                    // bool M5<X>(C<X> x) { return x is C<int>);
                    //
                    // Again, X could be int.
                    //
                    // We could then go on to get more complicated. For example,
                    //
                    // bool M6<X>(C<X> x) where X : struct { return x is C<string>; }
                    //
                    // We know that C<X> is never convertible to C<string> no matter what
                    // X is. Or:
                    //
                    // bool M7<T>(Dictionary<int, int> x) { return x is List<T>; }
                    //
                    // We know that no matter what T is, the conversion will never succeed.
                    //
                    // As noted above, we must be conservative. We follow the lead of the native compiler,
                    // which uses the following algorithm:
                    //
                    // * If neither type is open and there is no conversion then the result is always false:

                    if (!operandType.ContainsTypeParameter() && !targetType.ContainsTypeParameter())
                    {
                        return ConstantValue.False;
                    }

                    // * Otherwise, at least one of them is of an open type. If the operand is of value type
                    //   and the target is a class type other than System.Enum, or vice versa, then we are
                    //   in scenario 2, not scenario 1, and can correctly deduce that the result is false.

                    if (operandType.IsValueType && targetType.IsClassType() && targetType.SpecialType != SpecialType.System_Enum ||
                        targetType.IsValueType && operandType.IsClassType() && operandType.SpecialType != SpecialType.System_Enum)
                    {
                        return ConstantValue.False;
                    }

                    // * Otherwise, if the other type is a restricted type, we know no conversion is possible.
                    if (targetType.IsRestrictedType() || operandType.IsRestrictedType())
                    {
                        return ConstantValue.False;
                    }

                    // * Otherwise, we give up. Though there are other situations in which we can deduce that
                    //   the result will always be false, such as scenarios 6 and 7, but we do not attempt
                    //   to deduce this.

                    // CONSIDER: we could use TypeUnification.CanUnify to do additional compile-time checking.

                    return null;

                case ConversionKind.ImplicitNumeric:
                case ConversionKind.ExplicitNumeric:
                case ConversionKind.ImplicitEnumeration:
                // case ConversionKind.ExplicitEnumeration: // Handled separately below.
                case ConversionKind.ImplicitConstant:
                case ConversionKind.ImplicitUserDefined:
                case ConversionKind.ExplicitUserDefined:
                case ConversionKind.IntPtr:
                case ConversionKind.ExplicitTuple:
                case ConversionKind.ImplicitTuple:

                    // Consider all the cases where we know that "x is T" must be false just from
                    // the conversion classification.
                    //
                    // If we have "x is T" and the conversion from x to T is numeric or enum then the result must be false.
                    //
                    // If we have "null is T" then obviously that must be false.
                    //
                    // If we have "1 is long" then that must be false. (If we have "1 is int" then it is an identity conversion,
                    // not an implicit constant conversion.
                    //
                    // User-defined and IntPtr conversions are always false for "is".

                    return ConstantValue.False;

                case ConversionKind.ExplicitEnumeration:
                    // Enum-to-enum conversions should be treated the same as unsuccessful struct-to-struct
                    // conversions (i.e. make allowances for type unification, etc)
                    if (operandType.IsEnumType() && targetType.IsEnumType())
                    {
                        goto case ConversionKind.NoConversion;
                    }

                    return ConstantValue.False;

                case ConversionKind.ExplicitNullable:

                    // An explicit nullable conversion is a conversion of one of the following forms:
                    //
                    // 1) X? --> Y?, where X --> Y is an explicit conversion.  (If X --> Y is an implicit
                    //    conversion then X? --> Y? is an implicit nullable conversion.) In this case we
                    //    know that "X? is Y?" must be false because either X? is null, or we have an
                    //    explicit conversion from struct type X to struct type Y, and so X is never of type Y.)
                    //
                    // 2) X --> Y?, where again, X --> Y is an explicit conversion. By the same reasoning
                    //    as in case 1, this must be false.

                    if (targetType.IsNullableType())
                    {
                        return ConstantValue.False;
                    }

                    Debug.Assert(operandType.IsNullableType());

                    // 3) X? --> X. In this case, this is just a different way of writing "x != null".
                    //    We only know what the result will be if the input is known not to be null.
                    if (Conversions.HasIdentityConversion(operandType.GetNullableUnderlyingType(), targetType))
                    {
                        return operandCouldBeNull ? null : ConstantValue.True;
                    }

                    // 4) X? --> Y where the conversion X --> Y is an implicit or explicit value type conversion.
                    //    "X? is Y" again must be false.

                    return ConstantValue.False;

                case ConversionKind.ImplicitReference:
                    return operandCouldBeNull ? null : ConstantValue.True;

                case ConversionKind.ExplicitReference:
                case ConversionKind.Unboxing:
                    // In these three cases, the expression type must be a reference type. Therefore,
                    // the result cannot be determined. The expression could be null or of the wrong type,
                    // resulting in false, or it could be a non-null reference to the appropriate type,
                    // resulting in true.
                    return null;

                case ConversionKind.Identity:
                    // The result of "x is T" can be statically determined to be true if x is an expression
                    // of non-nullable value type T. If x is of reference or nullable value type then
                    // we cannot know, because again, the expression value could be null or it could be good.
                    // If it is of pointer type then we have already given an error.
                    return operandCouldBeNull ? null : ConstantValue.True;

                case ConversionKind.Boxing:

                    // A boxing conversion might be a conversion:
                    //
                    // * From a non-nullable value type to a reference type
                    // * From a nullable value type to a reference type
                    // * From a type parameter that *could* be a value type under construction
                    //   to a reference type
                    //
                    // In the first case we know that the conversion will always succeed and that the
                    // operand is never null, and therefore "is" will always result in true.
                    //
                    // In the second two cases we do not know; either the nullable value type could be
                    // null, or the type parameter could be constructed with a reference type, and it
                    // could be null.
                    return operandCouldBeNull ? null : ConstantValue.True;

                case ConversionKind.ImplicitNullable:
                    // We have "x is T" in one of the following situations:
                    // 1) x is of type X and T is X?.  The value is always true.
                    // 2) x is of type X and T is Y? where X is convertible to Y via an implicit numeric conversion. Eg,
                    //    x is of type int and T is decimal?.  The value is always false.
                    // 3) x is of type X? and T is Y? where X is convertible to Y via an implicit numeric conversion.
                    //    The value is always false.

                    Debug.Assert(targetType.IsNullableType());
                    return operandType.Equals(targetType.GetNullableUnderlyingType(), TypeCompareKind.AllIgnoreOptions)
                        ? ConstantValue.True : ConstantValue.False;

                default:
                case ConversionKind.ImplicitDynamic:
                case ConversionKind.ExplicitDynamic:
                case ConversionKind.PointerToInteger:
                case ConversionKind.PointerToPointer:
                case ConversionKind.PointerToVoid:
                case ConversionKind.IntegerToPointer:
                case ConversionKind.NullToPointer:
                case ConversionKind.AnonymousFunction:
                case ConversionKind.DefaultOrNullLiteral:
                case ConversionKind.MethodGroup:
                    // We've either replaced Dynamic with Object, or already bailed out with an error.
                    throw ExceptionUtilities.UnexpectedValue(conversionKind);
            }
        }

        private BoundExpression BindAsOperator(BinaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var operand = BindRValueWithoutTargetType(node.Left, diagnostics);
            AliasSymbol alias;
            TypeWithAnnotations targetTypeWithAnnotations = BindType(node.Right, diagnostics, out alias);
            TypeSymbol targetType = targetTypeWithAnnotations.Type;
            var typeExpression = new BoundTypeExpression(node.Right, alias, targetTypeWithAnnotations);
            var targetTypeKind = targetType.TypeKind;
            var resultType = targetType;

            // Is and As operator should have null ConstantValue as they are not constant expressions.
            // However we perform analysis of is/as expressions at bind time to detect if the expression
            // will always evaluate to a constant to generate warnings (always true/false/null).
            // We also need this analysis result during rewrite to optimize away redundant isinst instructions.
            // We store the conversion kind from expression's operand type to target type to enable these
            // optimizations during is/as operator rewrite.

            switch (operand.Kind)
            {
                case BoundKind.UnboundLambda:
                case BoundKind.Lambda:
                case BoundKind.MethodGroup:  // New in Roslyn - see DevDiv #864740.
                    // operand for an is or as expression cannot be a lambda expression or method group
                    if (!operand.HasAnyErrors)
                    {
                        Error(diagnostics, ErrorCode.ERR_LambdaInIsAs, node);
                    }

                    return new BoundAsOperator(node, operand, typeExpression, Conversion.NoConversion, resultType, hasErrors: true);

                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    if ((object)operand.Type == null)
                    {
                        Error(diagnostics, ErrorCode.ERR_TypelessTupleInAs, node);
                        return new BoundAsOperator(node, operand, typeExpression, Conversion.NoConversion, resultType, hasErrors: true);
                    }
                    break;
            }

            if (operand.HasAnyErrors || targetTypeKind == TypeKind.Error)
            {
                // If either operand is bad or target type has errors, bail out preventing more cascading errors.
                return new BoundAsOperator(node, operand, typeExpression, Conversion.NoConversion, resultType, hasErrors: true);
            }

            if (targetType.IsReferenceType && targetTypeWithAnnotations.NullableAnnotation.IsAnnotated())
            {
                Error(diagnostics, ErrorCode.ERR_AsNullableType, node.Right, targetType);

                return new BoundAsOperator(node, operand, typeExpression, Conversion.NoConversion, resultType, hasErrors: true);
            }
            else if (!targetType.IsReferenceType && !targetType.IsNullableType())
            {
                // SPEC:    In an operation of the form E as T, E must be an expression and T must be a
                // SPEC:    reference type, a type parameter known to be a reference type, or a nullable type.
                if (targetTypeKind == TypeKind.TypeParameter)
                {
                    Error(diagnostics, ErrorCode.ERR_AsWithTypeVar, node, targetType);
                }
                else if (targetTypeKind == TypeKind.Pointer)
                {
                    Error(diagnostics, ErrorCode.ERR_PointerInAsOrIs, node);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_AsMustHaveReferenceType, node, targetType);
                }

                return new BoundAsOperator(node, operand, typeExpression, Conversion.NoConversion, resultType, hasErrors: true);
            }

            // The C# specification states in the section called
            // "Referencing Static Class Types" that it is always
            // illegal to use "as" with a static type. The
            // native compiler actually allows "null as C" for
            // a static type C to be an expression of type C.
            // It also allows "someObject as C" if "someObject"
            // is of type object. To retain compatibility we
            // allow it, but when /feature:strict is enabled we break with the native
            // compiler and turn this into an error, as it should be.
            if (targetType.IsStatic && Compilation.FeatureStrictEnabled)
            {
                Error(diagnostics, ErrorCode.ERR_StaticInAsOrIs, node, targetType);
                return new BoundAsOperator(node, operand, typeExpression, Conversion.NoConversion, resultType, hasErrors: true);
            }

            if (operand.IsLiteralNull())
            {
                // We do not want to warn for the case "null as TYPE" where the null
                // is a literal, because the user might be saying it to cause overload resolution
                // to pick a particular method
                return new BoundAsOperator(node, operand, typeExpression, Conversion.DefaultOrNullLiteral, resultType);
            }

            if (operand.IsLiteralDefault())
            {
                var defaultLiteral = (BoundDefaultExpression)operand;
                Debug.Assert((object)defaultLiteral.TargetType == null);
                Debug.Assert((object)defaultLiteral.Type == null);
                Debug.Assert((object)defaultLiteral.ConstantValueOpt == null);

                operand = new BoundDefaultExpression(defaultLiteral.Syntax, targetType: null, constantValueOpt: ConstantValue.Null,
                    type: GetSpecialType(SpecialType.System_Object, diagnostics, node));
            }

            var operandType = operand.Type;
            Debug.Assert((object)operandType != null);
            var operandTypeKind = operandType.TypeKind;

            Debug.Assert(targetTypeKind != TypeKind.Pointer, "Should have been caught above");
            if (operandTypeKind == TypeKind.Pointer)
            {
                // operand for an is or as expression cannot be of pointer type
                Error(diagnostics, ErrorCode.ERR_PointerInAsOrIs, node);
                return new BoundAsOperator(node, operand, typeExpression, Conversion.NoConversion, resultType, hasErrors: true);
            }

            if (operandTypeKind == TypeKind.Dynamic)
            {
                // if operand has a dynamic type, we do the same thing as though it were an object
                operandType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
                operandTypeKind = operandType.TypeKind;
            }

            if (targetTypeKind == TypeKind.Dynamic)
            {
                // for "as dynamic", we do the same thing as though it were an "as object"
                targetType = GetSpecialType(SpecialType.System_Object, diagnostics, node);
                targetTypeKind = targetType.TypeKind;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            Conversion conversion = Conversions.ClassifyBuiltInConversion(operandType, targetType, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            bool hasErrors = ReportAsOperatorConversionDiagnostics(node, diagnostics, this.Compilation, operandType, targetType, conversion.Kind, operand.ConstantValue);
            return new BoundAsOperator(node, operand, typeExpression, conversion, resultType, hasErrors);
        }

        private static bool ReportAsOperatorConversionDiagnostics(
            CSharpSyntaxNode node,
            DiagnosticBag diagnostics,
            Compilation compilation,
            TypeSymbol operandType,
            TypeSymbol targetType,
            ConversionKind conversionKind,
            ConstantValue operandConstantValue)
        {
            // SPEC:    In an operation of the form E as T, E must be an expression and T must be a reference type,
            // SPEC:    a type parameter known to be a reference type, or a nullable type.
            // SPEC:    Furthermore, at least one of the following must be true, or otherwise a compile-time error occurs:
            // SPEC:    •	An identity (§6.1.1), implicit nullable (§6.1.4), implicit reference (§6.1.6), boxing (§6.1.7),
            // SPEC:        explicit nullable (§6.2.3), explicit reference (§6.2.4), or unboxing (§6.2.5) conversion exists
            // SPEC:        from E to T.
            // SPEC:    •	The type of E or T is an open type.
            // SPEC:    •	E is the null literal.

            // SPEC VIOLATION:  The specification contains an error in the list of legal conversions above.
            // SPEC VIOLATION:  If we have "class C<T, U> where T : U where U : class" then there is
            // SPEC VIOLATION:  an implicit conversion from T to U, but it is not an identity, reference or
            // SPEC VIOLATION:  boxing conversion. It will be one of those at runtime, but at compile time
            // SPEC VIOLATION:  we do not know which, and therefore cannot classify it as any of those.
            // SPEC VIOLATION:  See Microsoft.CodeAnalysis.CSharp.UnitTests.SyntaxBinderTests.TestAsOperator_SpecErrorCase() test for an example.

            // SPEC VIOLATION:  The specification also unintentionally allows the case where requirement 2 above:
            // SPEC VIOLATION:  "The type of E or T is an open type" is true, but type of E is void type, i.e. T is an open type.
            // SPEC VIOLATION:  Dev10 compiler correctly generates an error for this case and we will maintain compatibility.

            bool hasErrors = false;
            switch (conversionKind)
            {
                case ConversionKind.ImplicitReference:
                case ConversionKind.Boxing:
                case ConversionKind.ImplicitNullable:
                case ConversionKind.Identity:
                case ConversionKind.ExplicitNullable:
                case ConversionKind.ExplicitReference:
                case ConversionKind.Unboxing:
                    break;

                default:
                    // Generate an error if there is no possible legal conversion and both the operandType
                    // and the targetType are closed types OR operandType is void type, otherwise we need a runtime check
                    if (!operandType.ContainsTypeParameter() && !targetType.ContainsTypeParameter() ||
                        operandType.IsVoidType())
                    {
                        SymbolDistinguisher distinguisher = new SymbolDistinguisher(compilation, operandType, targetType);
                        Error(diagnostics, ErrorCode.ERR_NoExplicitBuiltinConv, node, distinguisher.First, distinguisher.Second);
                        hasErrors = true;
                    }

                    break;
            }

            if (!hasErrors)
            {
                ReportAsOperatorConstantWarnings(node, diagnostics, operandType, targetType, conversionKind, operandConstantValue);
            }

            return hasErrors;
        }

        private static void ReportAsOperatorConstantWarnings(
            CSharpSyntaxNode node,
            DiagnosticBag diagnostics,
            TypeSymbol operandType,
            TypeSymbol targetType,
            ConversionKind conversionKind,
            ConstantValue operandConstantValue)
        {
            // NOTE:    Even though BoundIsOperator and BoundAsOperator will always have no ConstantValue
            // NOTE:    (they are non-constant expressions according to Section 7.19 of the specification),
            // NOTE:    we want to perform constant analysis of is/as expressions to generate warnings if the
            // NOTE:    expression will always be true/false/null.

            ConstantValue constantValue = GetAsOperatorConstantResult(operandType, targetType, conversionKind, operandConstantValue);
            if (constantValue != null)
            {
                Debug.Assert(constantValue.IsNull);
                Error(diagnostics, ErrorCode.WRN_AlwaysNull, node, targetType);
            }
        }

        internal static ConstantValue GetAsOperatorConstantResult(TypeSymbol operandType, TypeSymbol targetType, ConversionKind conversionKind, ConstantValue operandConstantValue)
        {
            // NOTE:    Even though BoundIsOperator and BoundAsOperator will always have no ConstantValue
            // NOTE:    (they are non-constant expressions according to Section 7.19 of the specification),
            // NOTE:    we want to perform constant analysis of is/as expressions during binding to generate warnings (always true/false/null)
            // NOTE:    and during rewriting for optimized codegen.

            ConstantValue isOperatorConstantResult = GetIsOperatorConstantResult(operandType, targetType, conversionKind, operandConstantValue);
            if (isOperatorConstantResult != null && !isOperatorConstantResult.BooleanValue)
            {
                return ConstantValue.Null;
            }

            return null;
        }

        private BoundExpression GenerateNullCoalescingBadBinaryOpsError(BinaryExpressionSyntax node, BoundExpression leftOperand, BoundExpression rightOperand, Conversion leftConversion, DiagnosticBag diagnostics)
        {
            Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, SyntaxFacts.GetText(node.OperatorToken.Kind()), leftOperand.Display, rightOperand.Display);

            return new BoundNullCoalescingOperator(node, leftOperand, rightOperand,
                leftConversion, BoundNullCoalescingOperatorResultKind.NoCommonType, CreateErrorType(), hasErrors: true);
        }

        private BoundExpression BindNullCoalescingOperator(BinaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var leftOperand = BindValue(node.Left, diagnostics, BindValueKind.RValue);
            leftOperand = BindToNaturalType(leftOperand, diagnostics);
            var rightOperand = BindValue(node.Right, diagnostics, BindValueKind.RValue);

            // If either operand is bad, bail out preventing more cascading errors
            if (leftOperand.HasAnyErrors || rightOperand.HasAnyErrors)
            {
                return new BoundNullCoalescingOperator(node, leftOperand, rightOperand,
                    Conversion.NoConversion, BoundNullCoalescingOperatorResultKind.NoCommonType, CreateErrorType(), hasErrors: true);
            }

            // The specification does not permit the left hand side to be a default literal
            if (leftOperand.IsLiteralDefault())
            {
                Error(diagnostics, ErrorCode.ERR_BadOpOnNullOrDefault, node, node.OperatorToken.Text, "default");

                return new BoundNullCoalescingOperator(node, leftOperand, rightOperand,
                    Conversion.NoConversion, BoundNullCoalescingOperatorResultKind.NoCommonType, CreateErrorType(), hasErrors: true);
            }

            // SPEC: The type of the expression a ?? b depends on which implicit conversions are available
            // SPEC: between the types of the operands. In order of preference, the type of a ?? b is A0, A, or B,
            // SPEC: where A is the type of a, B is the type of b (provided that b has a type),
            // SPEC: and A0 is the underlying type of A if A is a nullable type, or A otherwise.

            TypeSymbol optLeftType = leftOperand.Type;   // "A"
            TypeSymbol optRightType = rightOperand.Type; // "B"
            bool isLeftNullable = (object)optLeftType != null && optLeftType.IsNullableType();
            TypeSymbol optLeftType0 = isLeftNullable ?  // "A0"
                optLeftType.GetNullableUnderlyingType() :
                optLeftType;

            // SPEC: The left hand side must be either the null literal or it must have a type. Lambdas and method groups do not have a type,
            // SPEC: so using one is an error.
            if (leftOperand.Kind == BoundKind.UnboundLambda || leftOperand.Kind == BoundKind.MethodGroup)
            {
                return GenerateNullCoalescingBadBinaryOpsError(node, leftOperand, rightOperand, Conversion.NoConversion, diagnostics);
            }

            // SPEC: Otherwise, if A exists and is a non-nullable value type, a compile-time error occurs. First we check for the pre-C# 8.0
            // SPEC: condition, to ensure that we don't allow previously illegal code in old language versions.
            if ((object)optLeftType != null && !optLeftType.IsReferenceType && !isLeftNullable)
            {
                // Prior to C# 8.0, the spec said that the left type must be either a reference type or a nullable value type. This was relaxed
                // with C# 8.0, so if the feature is not enabled then issue a diagnostic and return
                if (!optLeftType.IsValueType)
                {
                    CheckFeatureAvailability(node, MessageID.IDS_FeatureUnconstrainedTypeParameterInNullCoalescingOperator, diagnostics);
                }
                else
                {
                    return GenerateNullCoalescingBadBinaryOpsError(node, leftOperand, rightOperand, Conversion.NoConversion, diagnostics);
                }
            }

            // SPEC:    If b is a dynamic expression, the result is dynamic. At runtime, a is first
            // SPEC:    evaluated. If a is not null, a is converted to a dynamic type, and this becomes
            // SPEC:    the result. Otherwise, b is evaluated, and the outcome becomes the result.
            //
            // Note that there is no runtime dynamic dispatch since comparison with null is not a dynamic operation.
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if ((object)optRightType != null && optRightType.IsDynamic())
            {
                var leftConversion = Conversions.ClassifyConversionFromExpression(leftOperand, GetSpecialType(SpecialType.System_Object, diagnostics, node), ref useSiteDiagnostics);
                rightOperand = BindToNaturalType(rightOperand, diagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
                return new BoundNullCoalescingOperator(node, leftOperand, rightOperand,
                    leftConversion, BoundNullCoalescingOperatorResultKind.RightDynamicType, optRightType);
            }

            // SPEC:    Otherwise, if A exists and is a nullable type and an implicit conversion exists from b to A0,
            // SPEC:    the result type is A0. At run-time, a is first evaluated. If a is not null,
            // SPEC:    a is unwrapped to type A0, and this becomes the result.
            // SPEC:    Otherwise, b is evaluated and converted to type A0, and this becomes the result.

            if (isLeftNullable)
            {
                var rightConversion = Conversions.ClassifyImplicitConversionFromExpression(rightOperand, optLeftType0, ref useSiteDiagnostics);
                if (rightConversion.Exists)
                {
                    var leftConversion = Conversions.ClassifyConversionFromExpression(leftOperand, optLeftType0, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);
                    var convertedRightOperand = CreateConversion(rightOperand, rightConversion, optLeftType0, diagnostics);
                    return new BoundNullCoalescingOperator(node, leftOperand, convertedRightOperand,
                        leftConversion, BoundNullCoalescingOperatorResultKind.LeftUnwrappedType, optLeftType0);
                }
            }

            // SPEC:    Otherwise, if A exists and an implicit conversion exists from b to A, the result type is A.
            // SPEC:    At run-time, a is first evaluated. If a is not null, a becomes the result.
            // SPEC:    Otherwise, b is evaluated and converted to type A, and this becomes the result.

            if ((object)optLeftType != null)
            {
                var rightConversion = Conversions.ClassifyImplicitConversionFromExpression(rightOperand, optLeftType, ref useSiteDiagnostics);
                if (rightConversion.Exists)
                {
                    var convertedRightOperand = CreateConversion(rightOperand, rightConversion, optLeftType, diagnostics);
                    var leftConversion = Conversion.Identity;
                    diagnostics.Add(node, useSiteDiagnostics);
                    return new BoundNullCoalescingOperator(node, leftOperand, convertedRightOperand,
                        leftConversion, BoundNullCoalescingOperatorResultKind.LeftType, optLeftType);
                }
            }

            // SPEC:    Otherwise, if b has a type B and an implicit conversion exists from a to B,
            // SPEC:    the result type is B. At run-time, a is first evaluated. If a is not null,
            // SPEC:    a is unwrapped to type A0 (if A exists and is nullable) and converted to type B,
            // SPEC:    and this becomes the result. Otherwise, b is evaluated and becomes the result.

            // SPEC VIOLATION:  Native compiler violates the specification here and implements this part based on
            // SPEC VIOLATION:  whether A is a nullable type or not.
            // SPEC VIOLATION:  We will maintain compatibility with the native compiler and do the same.
            // SPEC VIOLATION:  Following SPEC PROPOSAL states the current implementations in both compilers:

            // SPEC PROPOSAL:    Otherwise, if A exists and is a nullable type and if b has a type B and
            // SPEC PROPOSAL:    an implicit conversion exists from A0 to B, the result type is B.
            // SPEC PROPOSAL:    At run-time, a is first evaluated. If a is not null, a is unwrapped to type A0
            // SPEC PROPOSAL:    and converted to type B, and this becomes the result.
            // SPEC PROPOSAL:    Otherwise, b is evaluated and becomes the result.

            // SPEC PROPOSAL:    Otherwise, if A does not exist or is a non-nullable type and if b has a type B and
            // SPEC PROPOSAL:    an implicit conversion exists from a to B, the result type is B.
            // SPEC PROPOSAL:    At run-time, a is first evaluated. If a is not null, a is converted to type B,
            // SPEC PROPOSAL:    and this becomes the result. Otherwise, b is evaluated and becomes the result.

            // See test CodeGenTests.TestNullCoalescingOperatorWithNullableConversions for an example.

            if ((object)optRightType != null)
            {
                rightOperand = BindToNaturalType(rightOperand, diagnostics);
                Conversion leftConversion;
                BoundNullCoalescingOperatorResultKind resultKind;

                if (isLeftNullable)
                {
                    // This is the SPEC VIOLATION case.
                    // Note that at runtime we need two conversions on the left operand:
                    //      1) Explicit nullable conversion from leftOperand to optLeftType0 and
                    //      2) Implicit conversion from optLeftType0 to optRightType.
                    // We just store the second conversion in the bound node and insert the first conversion during rewriting
                    // the null coalescing operator. See method LocalRewriter.GetConvertedLeftForNullCoalescingOperator.

                    leftConversion = Conversions.ClassifyImplicitConversionFromType(optLeftType0, optRightType, ref useSiteDiagnostics);
                    resultKind = BoundNullCoalescingOperatorResultKind.LeftUnwrappedRightType;
                }
                else
                {
                    leftConversion = Conversions.ClassifyImplicitConversionFromExpression(leftOperand, optRightType, ref useSiteDiagnostics);
                    resultKind = BoundNullCoalescingOperatorResultKind.RightType;
                }

                if (leftConversion.Exists)
                {
                    if (!leftConversion.IsValid)
                    {
                        // CreateConversion here to generate diagnostics.
                        if (isLeftNullable)
                        {
                            var conversion = Conversion.MakeNullableConversion(ConversionKind.ExplicitNullable, leftConversion);
                            var strippedLeftOperand = CreateConversion(leftOperand, conversion, optLeftType0, diagnostics);
                            leftOperand = CreateConversion(strippedLeftOperand, leftConversion, optRightType, diagnostics);
                        }
                        else
                        {
                            leftOperand = CreateConversion(leftOperand, leftConversion, optRightType, diagnostics);
                        }

                        Debug.Assert(leftOperand.HasAnyErrors);
                    }
                    else
                    {
                        ReportDiagnosticsIfObsolete(diagnostics, leftConversion, node, hasBaseReceiver: false);
                    }

                    diagnostics.Add(node, useSiteDiagnostics);
                    return new BoundNullCoalescingOperator(node, leftOperand, rightOperand, leftConversion, resultKind, optRightType);
                }
            }

            // SPEC:    Otherwise, a and b are incompatible, and a compile-time error occurs.
            diagnostics.Add(node, useSiteDiagnostics);
            return GenerateNullCoalescingBadBinaryOpsError(node, leftOperand, rightOperand, Conversion.NoConversion, diagnostics);
        }

        private BoundExpression BindNullCoalescingAssignmentOperator(AssignmentExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression leftOperand = BindValue(node.Left, diagnostics, BindValueKind.CompoundAssignment);
            ReportSuppressionIfNeeded(leftOperand, diagnostics);
            BoundExpression rightOperand = BindValue(node.Right, diagnostics, BindValueKind.RValue);

            // If either operand is bad, bail out preventing more cascading errors
            if (leftOperand.HasAnyErrors || rightOperand.HasAnyErrors)
            {
                return new BoundNullCoalescingAssignmentOperator(node, leftOperand, rightOperand, CreateErrorType(), hasErrors: true);
            }

            // Given a ??= b, the type of a is A, the type of B is b, and if A is a nullable value type, the underlying
            // non-nullable value type of A is A0.
            TypeSymbol leftType = leftOperand.Type;
            Debug.Assert((object)leftType != null);

            // If A is a non-nullable value type, a compile-time error occurs
            if (leftType.IsValueType && !leftType.IsNullableType())
            {
                return GenerateNullCoalescingAssignmentBadBinaryOpsError(node, leftOperand, rightOperand, diagnostics);
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            // If A0 exists and B is implicitly convertible to A0, then the result type of this expression is A0, except if B is dynamic.
            // This differs from most assignments such that you cannot directly replace a with (a ??= b).
            // The exception for dynamic is called out in the spec, it's the same behavior that ?? has with respect to dynamic.
            if (leftType.IsNullableType())
            {
                var underlyingLeftType = leftType.GetNullableUnderlyingType();
                var underlyingRightConversion = Conversions.ClassifyImplicitConversionFromExpression(rightOperand, underlyingLeftType, ref useSiteDiagnostics);
                if (underlyingRightConversion.Exists && rightOperand.Type?.IsDynamic() != true)
                {
                    diagnostics.Add(node, useSiteDiagnostics);
                    var convertedRightOperand = CreateConversion(rightOperand, underlyingRightConversion, underlyingLeftType, diagnostics);
                    return new BoundNullCoalescingAssignmentOperator(node, leftOperand, convertedRightOperand, underlyingLeftType);
                }
            }

            // If an implicit conversion exists from B to A, we store that conversion. At runtime, a is first evaluated. If
            // a is not null, b is not evaluated. If a is null, b is evaluated and converted to type A, and is stored in a.
            // Reset useSiteDiagnostics because they could have been used populated incorrectly from attempting to bind
            // as the nullable underlying value type case.
            useSiteDiagnostics = null;
            var rightConversion = Conversions.ClassifyImplicitConversionFromExpression(rightOperand, leftType, ref useSiteDiagnostics);
            diagnostics.Add(node, useSiteDiagnostics);
            if (rightConversion.Exists)
            {
                var convertedRightOperand = CreateConversion(rightOperand, rightConversion, leftType, diagnostics);
                return new BoundNullCoalescingAssignmentOperator(node, leftOperand, convertedRightOperand, leftType);
            }

            // a and b are incompatible and a compile-time error occurs
            return GenerateNullCoalescingAssignmentBadBinaryOpsError(node, leftOperand, rightOperand, diagnostics);
        }

        private BoundExpression GenerateNullCoalescingAssignmentBadBinaryOpsError(AssignmentExpressionSyntax node, BoundExpression leftOperand, BoundExpression rightOperand, DiagnosticBag diagnostics)
        {
            Error(diagnostics, ErrorCode.ERR_BadBinaryOps, node, SyntaxFacts.GetText(node.OperatorToken.Kind()), leftOperand.Display, rightOperand.Display);
            return new BoundNullCoalescingAssignmentOperator(node, leftOperand, rightOperand, CreateErrorType(), hasErrors: true);
        }

        /// <remarks>
        /// From ExpressionBinder::EnsureQMarkTypesCompatible:
        ///
        /// The v2.0 specification states that the types of the second and third operands T and S of a conditional operator
        /// must be TT and TS such that either (a) TT==TS, or (b), TT->TS or TS->TT but not both.
        ///
        /// Unfortunately that is not what we implemented in v2.0.  Instead, we implemented
        /// that either (a) TT=TS or (b) T->TS or S->TT but not both.  That is, we looked at the
        /// convertibility of the expressions, not the types.
        ///
        ///
        /// Changing that to the algorithm in the standard would be a breaking change.
        ///
        /// b ? (Func&lt;int&gt;)(delegate(){return 1;}) : (delegate(){return 2;})
        ///
        /// and
        ///
        /// b ? 0 : myenum
        ///
        /// would suddenly stop working.  (The first because o2 has no type, the second because 0 goes to
        /// any enum but enum doesn't go to int.)
        ///
        /// It gets worse.  We would like the 3.0 language features which require type inference to use
        /// a consistent algorithm, and that furthermore, the algorithm be smart about choosing the best
        /// of a set of types.  However, the language committee has decided that this algorithm will NOT
        /// consume information about the convertibility of expressions. Rather, it will gather up all
        /// the possible types and then pick the "largest" of them.
        ///
        /// To maintain backwards compatibility while still participating in the spirit of consistency,
        /// we implement an algorithm here which picks the type based on expression convertibility, but
        /// if there is a conflict, then it chooses the larger type rather than producing a type error.
        /// This means that b?0:myshort will have type int rather than producing an error (because 0->short,
        /// myshort->int).
        /// </remarks>
        private BoundExpression BindConditionalOperator(ConditionalExpressionSyntax node, DiagnosticBag diagnostics)
        {
            var whenTrue = node.WhenTrue.CheckAndUnwrapRefExpression(diagnostics, out var whenTrueRefKind);
            var whenFalse = node.WhenFalse.CheckAndUnwrapRefExpression(diagnostics, out var whenFalseRefKind);

            var isRef = whenTrueRefKind == RefKind.Ref && whenFalseRefKind == RefKind.Ref;

            if (!isRef)
            {
                if (whenFalseRefKind == RefKind.Ref)
                {
                    diagnostics.Add(ErrorCode.ERR_RefConditionalNeedsTwoRefs, whenFalse.GetFirstToken().GetLocation());
                }

                if (whenTrueRefKind == RefKind.Ref)
                {
                    diagnostics.Add(ErrorCode.ERR_RefConditionalNeedsTwoRefs, whenTrue.GetFirstToken().GetLocation());
                }
            }
            else
            {
                CheckFeatureAvailability(node, MessageID.IDS_FeatureRefConditional, diagnostics);
            }

            BoundExpression condition = BindBooleanExpression(node.Condition, diagnostics);

            var valKind = BindValueKind.RValue;
            if (isRef)
            {
                valKind |= BindValueKind.RefersToLocation;
            }

            BoundExpression trueExpr = BindValue(whenTrue, diagnostics, valKind);
            BoundExpression falseExpr = BindValue(whenFalse, diagnostics, valKind);

            TypeSymbol trueType = trueExpr.Type;
            TypeSymbol falseType = falseExpr.Type;

            TypeSymbol type;
            bool hasErrors = false;

            if (TypeSymbol.Equals(trueType, falseType, TypeCompareKind.ConsiderEverything2))
            {
                // NOTE: Dev10 lets the type inferrer handle this case (presumably, for maximum consistency),
                // but it seems like a worthwhile short-circuit for a common case.

                if ((object)trueType == null)
                {
                    // If trueExpr and falseExpr both have type null, then we don't have any symbols
                    // to pass to a SymbolDistinguisher (which ERR_InvalidQM would usually require).
                    diagnostics.Add(ErrorCode.ERR_InvalidQM, node.Location, trueExpr.Display, falseExpr.Display);
                    type = CreateErrorType();
                    hasErrors = true;
                }
                else
                {
                    // <expr> ? T : T
                    type = trueType;
                    trueExpr = BindToNaturalType(trueExpr, diagnostics);
                    falseExpr = BindToNaturalType(falseExpr, diagnostics);
                }
            }
            else
            {
                bool hadMultipleCandidates;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                TypeSymbol bestType = BestTypeInferrer.InferBestTypeForConditionalOperator(trueExpr, falseExpr, this.Conversions, out hadMultipleCandidates, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);

                if ((object)bestType == null)
                {
                    // CONSIDER: Dev10 suppresses ERR_InvalidQM unless the following is true for both trueType and falseType
                    // (!T->type->IsErrorType() || T->type->AsErrorType()->HasTypeParent() || T->type->AsErrorType()->HasNSParent())
                    if (hadMultipleCandidates)
                    {
                        diagnostics.Add(ErrorCode.ERR_AmbigQM, node.Location, trueExpr.Display, falseExpr.Display);
                    }
                    else
                    {
                        object trueArg = trueExpr.Display;
                        object falseArg = falseExpr.Display;

                        Symbol trueSymbol = trueArg as Symbol;
                        Symbol falseSymbol = falseArg as Symbol;
                        if ((object)trueSymbol != null && (object)falseSymbol != null)
                        {
                            SymbolDistinguisher distinguisher = new SymbolDistinguisher(this.Compilation, trueSymbol, falseSymbol);
                            trueArg = distinguisher.First;
                            falseArg = distinguisher.Second;
                        }

                        diagnostics.Add(ErrorCode.ERR_InvalidQM, node.Location, trueArg, falseArg);
                    }

                    type = CreateErrorType();
                    hasErrors = true;
                }
                else if (bestType.IsErrorType())
                {
                    type = bestType;
                    hasErrors = true;
                }
                else if (isRef)
                {
                    if (!Conversions.HasIdentityConversion(trueType, falseType))
                    {
                        diagnostics.Add(ErrorCode.ERR_RefConditionalDifferentTypes, falseExpr.Syntax.Location, trueType);
                        type = CreateErrorType();
                        hasErrors = true;
                    }
                    else
                    {
                        Debug.Assert(Conversions.HasIdentityConversion(trueType, bestType));
                        Debug.Assert(Conversions.HasIdentityConversion(falseType, bestType));
                        type = bestType;
                    }
                }
                else
                {
                    trueExpr = GenerateConversionForAssignment(bestType, trueExpr, diagnostics);
                    falseExpr = GenerateConversionForAssignment(bestType, falseExpr, diagnostics);

                    if (trueExpr.HasAnyErrors || falseExpr.HasAnyErrors)
                    {
                        // If one of the conversions went wrong (e.g. return type of method group being converted
                        // didn't match), then we don't want to use bestType because it's not accurate.
                        type = CreateErrorType();
                        hasErrors = true;
                    }
                    else
                    {
                        type = bestType;
                    }
                }
            }

            if (!hasErrors && isRef)
            {
                var currentScope = this.LocalScopeDepth;

                // val-escape must agree on both branches.
                uint whenTrueEscape = GetValEscape(trueExpr, currentScope);
                uint whenFalseEscape = GetValEscape(falseExpr, currentScope);

                if (whenTrueEscape != whenFalseEscape)
                {
                    // ask the one with narrower escape, for the wider - hopefully the errors will make the violation easier to fix.
                    if (whenTrueEscape < whenFalseEscape)
                    {
                        CheckValEscape(falseExpr.Syntax, falseExpr, currentScope, whenTrueEscape, checkingReceiver: false, diagnostics: diagnostics);
                    }
                    else
                    {
                        CheckValEscape(trueExpr.Syntax, trueExpr, currentScope, whenFalseEscape, checkingReceiver: false, diagnostics: diagnostics);
                    }

                    diagnostics.Add(ErrorCode.ERR_MismatchedRefEscapeInTernary, node.Location);
                    hasErrors = true;
                }
            }

            ConstantValue constantValue = null;

            if (!hasErrors)
            {
                constantValue = FoldConditionalOperator(condition, trueExpr, falseExpr);
                hasErrors = constantValue != null && constantValue.IsBad;
            }

            return new BoundConditionalOperator(node, isRef, condition, trueExpr, falseExpr, constantValue, type, hasErrors);
        }

        /// <summary>
        /// Constant folding for conditional (aka ternary) operators.
        /// </summary>
        private static ConstantValue FoldConditionalOperator(BoundExpression condition, BoundExpression trueExpr, BoundExpression falseExpr)
        {
            ConstantValue trueValue = trueExpr.ConstantValue;
            if (trueValue == null || trueValue.IsBad)
            {
                return trueValue;
            }

            ConstantValue falseValue = falseExpr.ConstantValue;
            if (falseValue == null || falseValue.IsBad)
            {
                return falseValue;
            }

            ConstantValue conditionValue = condition.ConstantValue;
            if (conditionValue == null || conditionValue.IsBad)
            {
                return conditionValue;
            }
            else if (conditionValue == ConstantValue.True)
            {
                return trueValue;
            }
            else if (conditionValue == ConstantValue.False)
            {
                return falseValue;
            }
            else
            {
                return ConstantValue.Bad;
            }
        }
    }
}
