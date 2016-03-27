// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class OverloadResolution
    {
        public void BinaryOperatorOverloadResolution(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, BinaryOperatorOverloadResolutionResult result, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            Debug.Assert(result.Results.Count == 0);

            // SPEC: An operation of the form x&&y or x||y is processed by applying overload resolution
            // SPEC: as if the operation was written x&y or x|y.

            // SPEC VIOLATION: For compatibility with Dev11, do not apply this rule to built-in conversions.

            BinaryOperatorKind underlyingKind = kind & ~BinaryOperatorKind.Logical;

            // We can do a table lookup for well-known problems in overload resolution.

            BinaryOperatorEasyOut(underlyingKind, left, right, result);
            if (result.Results.Count > 0)
            {
                return;
            }

            // The following is a slight rewording of the specification to emphasize that not all
            // operands of a binary operation need to have a type.

            // SPEC: An operation of the form x op y, where op is an overloadable binary operator is processed as follows:
            // SPEC: The set of candidate user-defined operators provided by the types (if any) of x and y for the 
            // SPEC operation operator op(x, y) is determined. 

            bool hadUserDefinedCandidate = GetUserDefinedOperators(underlyingKind, left, right, result.Results, ref useSiteDiagnostics);

            // SPEC: If the set of candidate user-defined operators is not empty, then this becomes the set of candidate 
            // SPEC: operators for the operation. Otherwise, the predefined binary operator op implementations, including 
            // SPEC: their lifted forms, become the set of candidate operators for the operation. 

            // Note that the native compiler has a bug in its binary operator overload resolution involving 
            // lifted built-in operators.  The spec says that we should add the lifted and unlifted operators
            // to a candidate set, eliminate the inapplicable operators, and then choose the best of what is left.
            // The lifted operator is defined as, say int? + int? --> int?.  That is not what the native compiler
            // does. The native compiler, rather, effectively says that there are *three* lifted operators:
            // int? + int? --> int?, int + int? --> int? and int? + int --> int?, and it chooses the best operator
            // amongst those choices.  
            //
            // This is a subtle difference; most of the time all it means is that we generate better code because we
            // skip an unnecessary operand conversion to int? when adding int to int?. But some of the time it
            // means that a different user-defined conversion is chosen than the one you would expect, if the
            // operand has a user-defined conversion to both int and int?.
            //
            // Roslyn matches the specification and takes the break from the native compiler.

            if (!hadUserDefinedCandidate)
            {
                result.Results.Clear();
                GetAllBuiltInOperators(kind, left, right, result.Results, ref useSiteDiagnostics);
            }

            // SPEC: The overload resolution rules of 7.5.3 are applied to the set of candidate operators to select the best 
            // SPEC: operator with respect to the argument list (x, y), and this operator becomes the result of the overload 
            // SPEC: resolution process. If overload resolution fails to select a single best operator, a binding-time 
            // SPEC: error occurs.

            BinaryOperatorOverloadResolution(left, right, result, ref useSiteDiagnostics);
        }

        private void AddDelegateOperation(BinaryOperatorKind kind, TypeSymbol delegateType,
            ArrayBuilder<BinaryOperatorSignature> operators)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Delegate, delegateType, delegateType, Compilation.GetSpecialType(SpecialType.System_Boolean)));
                    break;

                case BinaryOperatorKind.Addition:
                case BinaryOperatorKind.Subtraction:
                default:
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Delegate, delegateType, delegateType, delegateType));
                    break;
            }
        }

        private void GetDelegateOperations(BinaryOperatorKind kind, BoundExpression left, BoundExpression right,
            ArrayBuilder<BinaryOperatorSignature> operators, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            AssertNotChecked(kind);

            switch (kind)
            {
                case BinaryOperatorKind.Multiplication:
                case BinaryOperatorKind.Division:
                case BinaryOperatorKind.Remainder:
                case BinaryOperatorKind.RightShift:
                case BinaryOperatorKind.LeftShift:
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.LogicalAnd:
                case BinaryOperatorKind.LogicalOr:
                    return;

                case BinaryOperatorKind.Addition:
                case BinaryOperatorKind.Subtraction:
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    break;

                default:
                    // Unhandled bin op kind in get delegate operation
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }

            var leftType = left.Type;
            var leftDelegate = (object)leftType != null && leftType.IsDelegateType();
            var rightType = right.Type;
            var rightDelegate = (object)rightType != null && rightType.IsDelegateType();

            // If no operands have delegate types then add nothing.
            if (!leftDelegate && !rightDelegate)
            {
                // Even though neither left nor right type is a delegate type,
                // both types might have implicit conversions to System.Delegate type.

                // Spec 7.10.8: Delegate equality operators:
                // Every delegate type implicitly provides the following predefined comparison operators:
                //     bool operator ==(System.Delegate x, System.Delegate y)
                //     bool operator !=(System.Delegate x, System.Delegate y)

                switch (OperatorKindExtensions.Operator(kind))
                {
                    case BinaryOperatorKind.Equal:
                    case BinaryOperatorKind.NotEqual:
                        TypeSymbol systemDelegateType = _binder.GetSpecialType(SpecialType.System_Delegate, _binder.Compilation.DeclarationDiagnostics, left.Syntax);

                        if (Conversions.ClassifyImplicitConversionFromExpression(left, systemDelegateType, ref useSiteDiagnostics).IsValid &&
                            Conversions.ClassifyImplicitConversionFromExpression(right, systemDelegateType, ref useSiteDiagnostics).IsValid)
                        {
                            AddDelegateOperation(kind, systemDelegateType, operators);
                        }

                        break;
                }

                return;
            }

            // We might have a situation like
            //
            // Func<string> + Func<object>
            // 
            // in which case overload resolution should consider both 
            //
            // Func<string> + Func<string>
            // Func<object> + Func<object>
            //
            // are candidates (and it will pick Func<object>). Similarly,
            // we might have something like:
            //
            // Func<object> + Func<dynamic>
            // 
            // in which case neither candidate is better than the other,
            // resulting in an error.
            //
            // We could as an optimization say that if you are adding two completely
            // dissimilar delegate types D1 and D2, that neither is added to the candidate
            // set because neither can possibly be applicable, but let's not go there.
            // Let's just add them to the set and let overload resolution (and the 
            // error recovery heuristics) have at the real candidate set.
            //
            // However, we will take a spec violation for this scenario:
            //
            // SPEC VIOLATION:
            //
            // Technically the spec implies that we ought to be able to compare 
            // 
            // Func<int> x = whatever;
            // bool y = x == ()=>1;
            //
            // The native compiler does not allow this. I see no
            // reason why we ought to allow this. However, a good question is whether
            // the violation ought to be here, where we are determining the operator
            // candidate set, or in overload resolution where we are determining applicability.
            // In the native compiler we did it during candidate set determination, 
            // so let's stick with that.

            if (leftDelegate && rightDelegate)
            {
                // They are both delegate types. Add them both if they are different types.
                AddDelegateOperation(kind, leftType, operators);

                // There is no reason why we can't compare instances of delegate types that are identity convertible.
                // We can't perform + or - operation on them since it is not clear what the return type of such operation should be.
                bool useIdentityConversion = kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual;

                if (!(useIdentityConversion ? Conversions.HasIdentityConversion(leftType, rightType) : leftType.Equals(rightType)))
                {
                    AddDelegateOperation(kind, rightType, operators);
                }

                return;
            }

            // One of them is a delegate, the other is not.
            TypeSymbol delegateType = leftDelegate ? leftType : rightType;
            BoundExpression nonDelegate = leftDelegate ? right : left;

            if ((kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual)
                && nonDelegate.Kind == BoundKind.UnboundLambda)
            {
                return;
            }

            AddDelegateOperation(kind, delegateType, operators);
        }

        private void GetEnumOperation(BinaryOperatorKind kind, TypeSymbol enumType, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorSignature> operators)
        {
            Debug.Assert((object)enumType != null);
            AssertNotChecked(kind);

            if (!enumType.IsValidEnumType())
            {
                return;
            }

            var underlying = enumType.GetEnumUnderlyingType();
            Debug.Assert((object)underlying != null);
            Debug.Assert(underlying.SpecialType != SpecialType.None);

            NamedTypeSymbol nullableEnum = null;
            NamedTypeSymbol nullableUnderlying = null;

            // PERF: avoid instantiating nullable types in common simple cases.
            var leftType = left.Type;
            var rightType = right.Type;
            var simpleCase = leftType?.IsValueType == true &&
                             rightType?.IsValueType == true &&
                             leftType.IsNullableType() == false &&
                             rightType.IsNullableType() == false;

            if (!simpleCase)
            {
                var nullable = Compilation.GetSpecialType(SpecialType.System_Nullable_T);
                nullableEnum = nullable.Construct(enumType);
                nullableUnderlying = nullable.Construct(underlying);
            }

            switch (kind)
            {
                case BinaryOperatorKind.Addition:
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.EnumAndUnderlyingAddition, enumType, underlying, enumType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.UnderlyingAndEnumAddition, underlying, enumType, enumType));
                    if (!simpleCase)
                    {
                        operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedEnumAndUnderlyingAddition, nullableEnum, nullableUnderlying, nullableEnum));
                        operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedUnderlyingAndEnumAddition, nullableUnderlying, nullableEnum, nullableEnum));
                    }
                    break;
                case BinaryOperatorKind.Subtraction:
                    if (Strict)
                    {
                        operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.EnumSubtraction, enumType, enumType, underlying));
                        operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.EnumAndUnderlyingSubtraction, enumType, underlying, enumType));
                        if (!simpleCase)
                        {
                            operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedEnumSubtraction, nullableEnum, nullableEnum, nullableUnderlying));
                            operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedEnumAndUnderlyingSubtraction, nullableEnum, nullableUnderlying, nullableEnum));
                        }
                    }
                    else
                    {
                        // SPEC VIOLATION:
                        // The native compiler has bugs in overload resolution involving binary operator- for enums,
                        // which we duplicate by hardcoding Priority values among the operators. When present on both
                        // methods being compared during overload resolution, Priority values are used to decide between
                        // two candidates (instead of the usual language-specified rules).
                        bool isExactSubtraction = right.Type?.StrippedType() == underlying;
                        operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.EnumSubtraction, enumType, enumType, underlying)
                        { Priority = 2 });
                        operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.EnumAndUnderlyingSubtraction, enumType, underlying, enumType)
                        { Priority = isExactSubtraction ? 1 : 3 });
                        if (!simpleCase)
                        {
                            operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedEnumSubtraction, nullableEnum, nullableEnum, nullableUnderlying)
                            { Priority = 12 });
                            operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedEnumAndUnderlyingSubtraction, nullableEnum, nullableUnderlying, nullableEnum)
                            { Priority = isExactSubtraction ? 11 : 13 });
                        }

                        // Due to a bug, the native compiler allows "underlying - enum", so Roslyn does as well.
                        operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.UnderlyingAndEnumSubtraction, underlying, enumType, enumType)
                        { Priority = 4 });
                        if (!simpleCase)
                        {
                            operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LiftedUnderlyingAndEnumSubtraction, nullableUnderlying, nullableEnum, nullableEnum)
                            { Priority = 14 });
                        }
                    }
                    break;
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    var boolean = Compilation.GetSpecialType(SpecialType.System_Boolean);
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Enum, enumType, enumType, boolean));
                    if (!simpleCase)
                    {
                        operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Lifted | BinaryOperatorKind.Enum, nullableEnum, nullableEnum, boolean));
                    }
                    break;
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Enum, enumType, enumType, enumType));
                    if (!simpleCase)
                    {
                        operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Lifted | BinaryOperatorKind.Enum, nullableEnum, nullableEnum, nullableEnum));
                    }
                    break;
            }
        }

        private void GetPointerArithmeticOperators(
            BinaryOperatorKind kind,
            PointerTypeSymbol pointerType,
            ArrayBuilder<BinaryOperatorSignature> operators)
        {
            Debug.Assert((object)pointerType != null);
            AssertNotChecked(kind);

            switch (kind)
            {
                case BinaryOperatorKind.Addition:
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndIntAddition, pointerType, Compilation.GetSpecialType(SpecialType.System_Int32), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndUIntAddition, pointerType, Compilation.GetSpecialType(SpecialType.System_UInt32), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndLongAddition, pointerType, Compilation.GetSpecialType(SpecialType.System_Int64), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndULongAddition, pointerType, Compilation.GetSpecialType(SpecialType.System_UInt64), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.IntAndPointerAddition, Compilation.GetSpecialType(SpecialType.System_Int32), pointerType, pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.UIntAndPointerAddition, Compilation.GetSpecialType(SpecialType.System_UInt32), pointerType, pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.LongAndPointerAddition, Compilation.GetSpecialType(SpecialType.System_Int64), pointerType, pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.ULongAndPointerAddition, Compilation.GetSpecialType(SpecialType.System_UInt64), pointerType, pointerType));
                    break;
                case BinaryOperatorKind.Subtraction:
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndIntSubtraction, pointerType, Compilation.GetSpecialType(SpecialType.System_Int32), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndUIntSubtraction, pointerType, Compilation.GetSpecialType(SpecialType.System_UInt32), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndLongSubtraction, pointerType, Compilation.GetSpecialType(SpecialType.System_Int64), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerAndULongSubtraction, pointerType, Compilation.GetSpecialType(SpecialType.System_UInt64), pointerType));
                    operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.PointerSubtraction, pointerType, pointerType, Compilation.GetSpecialType(SpecialType.System_Int64)));
                    break;
            }
        }

        private void GetPointerComparisonOperators(
            BinaryOperatorKind kind,
            ArrayBuilder<BinaryOperatorSignature> operators)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    var voidPointerType = new PointerTypeSymbol(TypeSymbolWithAnnotations.Create(Compilation.GetSpecialType(SpecialType.System_Void)));
                    operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Pointer, voidPointerType, voidPointerType, Compilation.GetSpecialType(SpecialType.System_Boolean)));
                    break;
            }
        }

        private void GetEnumOperations(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorSignature> results)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            AssertNotChecked(kind);

            // First take some easy outs:
            switch (kind)
            {
                case BinaryOperatorKind.Multiplication:
                case BinaryOperatorKind.Division:
                case BinaryOperatorKind.Remainder:
                case BinaryOperatorKind.RightShift:
                case BinaryOperatorKind.LeftShift:
                case BinaryOperatorKind.LogicalAnd:
                case BinaryOperatorKind.LogicalOr:
                    return;
            }

            var leftType = left.Type;
            if ((object)leftType != null)
            {
                leftType = leftType.StrippedType();
            }

            var rightType = right.Type;
            if ((object)rightType != null)
            {
                rightType = rightType.StrippedType();
            }

            bool useIdentityConversion;
            switch (kind)
            {
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.Xor:
                    // These operations are ambiguous on non-equal identity-convertible types - 
                    // it's not clear what the resulting type of the operation should be:
                    //   C<?>.E operator +(C<dynamic>.E x, C<object>.E y)
                    useIdentityConversion = false;
                    break;

                case BinaryOperatorKind.Addition:
                    // Addition only accepts a single enum type, so operations on non-equal identity-convertible types are not ambiguous. 
                    //   E operator +(E x, U y)
                    //   E operator +(U x, E y)
                    useIdentityConversion = true;
                    break;

                case BinaryOperatorKind.Subtraction:
                    // Subtraction either returns underlying type or only accept a single enum type, so operations on non-equal identity-convertible types are not ambiguous. 
                    //   U operator –(E x, E y)
                    //   E operator –(E x, U y)
                    useIdentityConversion = true;
                    break;

                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThanOrEqual:
                    // Relational operations return Boolean, so operations on non-equal identity-convertible types are not ambiguous. 
                    //   Boolean operator op(C<dynamic>.E, C<object>.E)
                    useIdentityConversion = true;
                    break;

                default:
                    // Unhandled bin op kind in get enum operations
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }

            if ((object)leftType != null)
            {
                GetEnumOperation(kind, leftType, left, right, results);
            }

            if ((object)rightType != null && ((object)leftType == null || !(useIdentityConversion ? Conversions.HasIdentityConversion(rightType, leftType) : rightType.Equals(leftType))))
            {
                GetEnumOperation(kind, rightType, left, right, results);
            }
        }

        private void GetPointerOperators(
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            ArrayBuilder<BinaryOperatorSignature> results)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);
            AssertNotChecked(kind);

            var leftType = left.Type as PointerTypeSymbol;
            var rightType = right.Type as PointerTypeSymbol;

            if ((object)leftType != null)
            {
                GetPointerArithmeticOperators(kind, leftType, results);
            }

            // The only arithmetic operator that is applicable on two distinct pointer types is
            //   long operator –(T* x, T* y)
            // This operator returns long and so it's not ambiguous to apply it on T1 and T2 that are identity convertible to each other.
            if ((object)rightType != null && ((object)leftType == null || !Conversions.HasIdentityConversion(rightType, leftType)))
            {
                GetPointerArithmeticOperators(kind, rightType, results);
            }

            if ((object)leftType != null || (object)rightType != null)
            {
                // The pointer comparison operators are all "void* OP void*".
                GetPointerComparisonOperators(kind, results);
            }
        }

        private void GetAllBuiltInOperators(BinaryOperatorKind kind, BoundExpression left, BoundExpression right, ArrayBuilder<BinaryOperatorAnalysisResult> results, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Strip the "checked" off; the checked-ness of the context does not affect which built-in operators
            // are applicable.
            kind = kind.OperatorWithLogical();
            var operators = ArrayBuilder<BinaryOperatorSignature>.GetInstance();
            bool isEquality = kind == BinaryOperatorKind.Equal || kind == BinaryOperatorKind.NotEqual;
            if (isEquality && UseOnlyReferenceEquality(left, right, ref useSiteDiagnostics))
            {
                // As a special case, if the reference equality operator is applicable (and it
                // is not a string or delegate) we do not check any other operators.  This patches
                // what is otherwise a flaw in the language specification.  See 11426.
                GetReferenceEquality(kind, operators);
            }
            else
            {
                this.Compilation.builtInOperators.GetSimpleBuiltInOperators(kind, operators);

                // SPEC 7.3.4: For predefined enum and delegate operators, the only operators
                // considered are those defined by an enum or delegate type that is the binding
                //-time type of one of the operands.
                GetDelegateOperations(kind, left, right, operators, ref useSiteDiagnostics);
                GetEnumOperations(kind, left, right, operators);

                // We similarly limit pointer operator candidates considered.
                GetPointerOperators(kind, left, right, operators);
            }

            CandidateOperators(operators, left, right, results, ref useSiteDiagnostics);
            operators.Free();
        }

        private bool UseOnlyReferenceEquality(BoundExpression left, BoundExpression right, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return
                BuiltInOperators.IsValidObjectEquality(Conversions, left.Type, left.IsLiteralNull(), right.Type, right.IsLiteralNull(), ref useSiteDiagnostics) &&
                ((object)left.Type == null || (!left.Type.IsDelegateType() && left.Type.SpecialType != SpecialType.System_String && left.Type.SpecialType != SpecialType.System_Delegate)) &&
                ((object)right.Type == null || (!right.Type.IsDelegateType() && right.Type.SpecialType != SpecialType.System_String && right.Type.SpecialType != SpecialType.System_Delegate));
        }

        private void GetReferenceEquality(BinaryOperatorKind kind, ArrayBuilder<BinaryOperatorSignature> operators)
        {
            var @object = Compilation.GetSpecialType(SpecialType.System_Object);
            operators.Add(new BinaryOperatorSignature(kind | BinaryOperatorKind.Object, @object, @object, Compilation.GetSpecialType(SpecialType.System_Boolean)));
        }

        private bool CandidateOperators(
            ArrayBuilder<BinaryOperatorSignature> operators,
            BoundExpression left,
            BoundExpression right,
            ArrayBuilder<BinaryOperatorAnalysisResult> results,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            bool hadApplicableCandidate = false;
            foreach (var op in operators)
            {
                var convLeft = Conversions.ClassifyConversionFromExpression(left, op.LeftType, ref useSiteDiagnostics);
                var convRight = Conversions.ClassifyConversionFromExpression(right, op.RightType, ref useSiteDiagnostics);
                if (convLeft.IsImplicit && convRight.IsImplicit)
                {
                    results.Add(BinaryOperatorAnalysisResult.Applicable(op, convLeft, convRight));
                    hadApplicableCandidate = true;
                }
                else
                {
                    results.Add(BinaryOperatorAnalysisResult.Inapplicable(op, convLeft, convRight));
                }
            }
            return hadApplicableCandidate;
        }

        // Returns an analysis of every matching user-defined binary operator, including whether the
        // operator is applicable or not.

        private bool GetUserDefinedOperators(
            BinaryOperatorKind kind,
            BoundExpression left,
            BoundExpression right,
            ArrayBuilder<BinaryOperatorAnalysisResult> results,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != null);

            // The following is a slight rewording of the specification to emphasize that not all
            // operands of a binary operation need to have a type.

            // TODO (tomat): The spec needs to be updated to use identity conversion instead of type equality.

            // Spec 7.3.4 Binary operator overload resolution:
            //   An operation of the form x op y, where op is an overloadable binary operator is processed as follows:
            //   The set of candidate user-defined operators provided by the types (if any) of x and y for the 
            //   operation operator op(x, y) is determined. The set consists of the union of the candidate operators
            //   provided by the type of x (if any) and the candidate operators provided by the type of y (if any), 
            //   each determined using the rules of 7.3.5. Candidate operators only occur in the combined set once.

            var operators = ArrayBuilder<BinaryOperatorAnalysisResult>.GetInstance();
            TypeSymbol leftType = left.Type;
            TypeSymbol strippedLeftType = leftType?.StrippedType();

            bool hadApplicableCandidate = false;

            if ((object)strippedLeftType != null && !OperatorFacts.DefinitelyHasNoUserDefinedOperators(strippedLeftType))
            {
                hadApplicableCandidate = GetUserDefinedOperators(kind, strippedLeftType, left, right, operators, ref useSiteDiagnostics);
                if (!hadApplicableCandidate)
                {
                    operators.Clear();
                }
            }

            TypeSymbol rightType = right.Type;
            TypeSymbol strippedRightType = rightType?.StrippedType();
            if ((object)strippedRightType != null && !strippedRightType.Equals(strippedLeftType) &&
                !OperatorFacts.DefinitelyHasNoUserDefinedOperators(strippedRightType))
            {
                var rightOperators = ArrayBuilder<BinaryOperatorAnalysisResult>.GetInstance();
                hadApplicableCandidate |= GetUserDefinedOperators(kind, strippedRightType, left, right, rightOperators, ref useSiteDiagnostics);
                AddDistinctOperators(operators, rightOperators);
                rightOperators.Free();
            }

            if (hadApplicableCandidate)
            {
                results.AddRange(operators);
            }

            operators.Free();

            return hadApplicableCandidate;
        }

        private static void AddDistinctOperators(ArrayBuilder<BinaryOperatorAnalysisResult> result, ArrayBuilder<BinaryOperatorAnalysisResult> additionalOperators)
        {
            int initialCount = result.Count;

            foreach (var op in additionalOperators)
            {
                bool equivalentToExisting = false;

                for (int i = 0; i < initialCount; i++)
                {
                    var existingSignature = result[i].Signature;

                    Debug.Assert(op.Signature.Kind.Operator() == existingSignature.Kind.Operator());

                    // Return types must match exactly, parameters might match modulo identity conversion.
                    if (op.Signature.Kind == existingSignature.Kind && // Easy out
                        op.Signature.ReturnType.Equals(existingSignature.ReturnType, ignoreDynamic: false) &&
                        op.Signature.LeftType.Equals(existingSignature.LeftType, ignoreDynamic: true) &&
                        op.Signature.RightType.Equals(existingSignature.RightType, ignoreDynamic: true) &&
                        op.Signature.Method.ContainingType.Equals(existingSignature.Method.ContainingType, ignoreDynamic: true))
                    {
                        equivalentToExisting = true;
                        break;
                    }
                }

                if (!equivalentToExisting)
                {
                    result.Add(op);
                }
            }
        }

        private bool GetUserDefinedOperators(
            BinaryOperatorKind kind,
            TypeSymbol type0,
            BoundExpression left,
            BoundExpression right,
            ArrayBuilder<BinaryOperatorAnalysisResult> results,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Spec 7.3.5 Candidate user-defined operators
            // SPEC: Given a type T and an operation operator op(A), where op is an overloadable 
            // SPEC: operator and A is an argument list, the set of candidate user-defined operators 
            // SPEC: provided by T for operator op(A) is determined as follows:

            // SPEC: Determine the type T0. If T is a nullable type, T0 is its underlying type, 
            // SPEC: otherwise T0 is equal to T.

            // (The caller has already passed in the stripped type.)

            // SPEC: For all operator op declarations in T0 and all lifted forms of such operators, 
            // SPEC: if at least one operator is applicable (7.5.3.1) with respect to the argument 
            // SPEC: list A, then the set of candidate operators consists of all such applicable 
            // SPEC: operators in T0. Otherwise, if T0 is object, the set of candidate operators is empty.
            // SPEC: Otherwise, the set of candidate operators provided by T0 is the set of candidate 
            // SPEC: operators provided by the direct base class of T0, or the effective base class of
            // SPEC: T0 if T0 is a type parameter.

            string name = OperatorFacts.BinaryOperatorNameFromOperatorKind(kind);
            var operators = ArrayBuilder<BinaryOperatorSignature>.GetInstance();
            bool hadApplicableCandidates = false;

            NamedTypeSymbol current = type0 as NamedTypeSymbol;
            if ((object)current == null)
            {
                current = type0.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            if ((object)current == null && type0.IsTypeParameter())
            {
                current = ((TypeParameterSymbol)type0).EffectiveBaseClass(ref useSiteDiagnostics);
            }

            for (; (object)current != null; current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                operators.Clear();
                GetUserDefinedBinaryOperatorsFromType(current, kind, name, operators);
                results.Clear();
                if (CandidateOperators(operators, left, right, results, ref useSiteDiagnostics))
                {
                    hadApplicableCandidates = true;
                    break;
                }
            }

            operators.Free();

            return hadApplicableCandidates;
        }

        private void GetUserDefinedBinaryOperatorsFromType(
            NamedTypeSymbol type,
            BinaryOperatorKind kind,
            string name,
            ArrayBuilder<BinaryOperatorSignature> operators)
        {
            foreach (MethodSymbol op in type.GetOperators(name))
            {
                // If we're in error recovery, we might have bad operators. Just ignore it.
                if (op.ParameterCount != 2 || op.ReturnsVoid)
                {
                    continue;
                }

                TypeSymbol leftOperandType = op.ParameterTypes[0];
                TypeSymbol rightOperandType = op.ParameterTypes[1];
                TypeSymbol resultType = op.ReturnType.TypeSymbol;

                operators.Add(new BinaryOperatorSignature(BinaryOperatorKind.UserDefined | kind, leftOperandType, rightOperandType, resultType, op));

                LiftingResult lifting = UserDefinedBinaryOperatorCanBeLifted(leftOperandType, rightOperandType, resultType, kind);

                if (lifting == LiftingResult.LiftOperandsAndResult)
                {
                    operators.Add(new BinaryOperatorSignature(
                        BinaryOperatorKind.Lifted | BinaryOperatorKind.UserDefined | kind,
                        MakeNullable(leftOperandType), MakeNullable(rightOperandType), MakeNullable(resultType), op));
                }
                else if (lifting == LiftingResult.LiftOperandsButNotResult)
                {
                    operators.Add(new BinaryOperatorSignature(
                        BinaryOperatorKind.Lifted | BinaryOperatorKind.UserDefined | kind,
                        MakeNullable(leftOperandType), MakeNullable(rightOperandType), resultType, op));
                }
            }
        }

        private enum LiftingResult
        {
            NotLifted,
            LiftOperandsAndResult,
            LiftOperandsButNotResult
        }

        private static LiftingResult UserDefinedBinaryOperatorCanBeLifted(TypeSymbol left, TypeSymbol right, TypeSymbol result, BinaryOperatorKind kind)
        {
            // SPEC: For the binary operators + - * / % & | ^ << >> a lifted form of the
            // SPEC: operator exists if the operand and result types are all non-nullable
            // SPEC: value types. The lifted form is constructed by adding a single ?
            // SPEC: modifier to each operand and result type. 
            //
            // SPEC: For the equality operators == != a lifted form of the operator exists
            // SPEC: if the operand types are both non-nullable value types and if the 
            // SPEC: result type is bool. The lifted form is constructed by adding
            // SPEC: a single ? modifier to each operand type.
            //
            // SPEC: For the relational operators > < >= <= a lifted form of the 
            // SPEC: operator exists if the operand types are both non-nullable value
            // SPEC: types and if the result type is bool. The lifted form is 
            // SPEC: constructed by adding a single ? modifier to each operand type.

            if (!left.IsValueType ||
                left.IsNullableType() ||
                !right.IsValueType ||
                right.IsNullableType())
            {
                return LiftingResult.NotLifted;
            }

            switch (kind)
            {
                case BinaryOperatorKind.Equal:
                case BinaryOperatorKind.NotEqual:
                    // Spec violation: can't lift unless the types match.
                    // The spec doesn't require this, but dev11 does and it reduces ambiguity in some cases.
                    if (left != right) return LiftingResult.NotLifted;
                    goto case BinaryOperatorKind.GreaterThan;
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                    return result.SpecialType == SpecialType.System_Boolean ?
                        LiftingResult.LiftOperandsButNotResult :
                        LiftingResult.NotLifted;
                default:
                    return result.IsValueType && !result.IsNullableType() ?
                        LiftingResult.LiftOperandsAndResult :
                        LiftingResult.NotLifted;
            }
        }

        // Takes a list of candidates and mutates the list to throw out the ones that are worse than
        // another applicable candidate.
        private void BinaryOperatorOverloadResolution(
            BoundExpression left,
            BoundExpression right,
            BinaryOperatorOverloadResolutionResult result,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // SPEC: Given the set of applicable candidate function members, the best function member in that set is located. 
            // SPEC: If the set contains only one function member, then that function member is the best function member. 

            if (result.GetValidCount() == 1)
            {
                return;
            }

            // SPEC: Otherwise, the best function member is the one function member that is better than all other function 
            // SPEC: members with respect to the given argument list, provided that each function member is compared to all 
            // SPEC: other function members using the rules in 7.5.3.2. If there is not exactly one function member that is 
            // SPEC: better than all other function members, then the function member invocation is ambiguous and a binding-time 
            // SPEC: error occurs.

            // UNDONE: This is a naive quadratic algorithm; there is a linear algorithm that works. Consider using it.
            var candidates = result.Results;
            for (int i = 0; i < candidates.Count; ++i)
            {
                if (candidates[i].Kind != OperatorAnalysisResultKind.Applicable)
                {
                    continue;
                }

                // Is this applicable operator better than every other applicable method?
                for (int j = 0; j < candidates.Count; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    if (candidates[j].Kind == OperatorAnalysisResultKind.Inapplicable)
                    {
                        continue;
                    }
                    var better = BetterOperator(candidates[i].Signature, candidates[j].Signature, left, right, ref useSiteDiagnostics);
                    if (better == BetterResult.Left)
                    {
                        candidates[j] = candidates[j].Worse();
                    }
                    else if (better == BetterResult.Right)
                    {
                        candidates[i] = candidates[i].Worse();
                    }
                }
            }
        }

        private bool IsApplicable(BinaryOperatorSignature binaryOperator, BoundExpression left, BoundExpression right, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return
                Conversions.ClassifyImplicitConversionFromExpression(left, binaryOperator.LeftType, ref useSiteDiagnostics).Exists &&
                Conversions.ClassifyImplicitConversionFromExpression(right, binaryOperator.RightType, ref useSiteDiagnostics).Exists;
        }

        private BetterResult BetterOperator(BinaryOperatorSignature op1, BinaryOperatorSignature op2, BoundExpression left, BoundExpression right, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(op1.Priority.HasValue == op2.Priority.HasValue);

            // We use Priority as a tie-breaker to help match native compiler bugs.
            if (op1.Priority.HasValue && op2.Priority.HasValue && op1.Priority.GetValueOrDefault() != op2.Priority.GetValueOrDefault())
            {
                return (op1.Priority.GetValueOrDefault() < op2.Priority.GetValueOrDefault()) ? BetterResult.Left : BetterResult.Right;
            }

            BetterResult leftBetter = BetterConversionFromExpression(left, op1.LeftType, op2.LeftType, ref useSiteDiagnostics);
            BetterResult rightBetter = BetterConversionFromExpression(right, op1.RightType, op2.RightType, ref useSiteDiagnostics);

            // SPEC: Mp is defined to be a better function member than Mq if:
            // SPEC: * For each argument, the implicit conversion from Ex to Qx is not better than
            // SPEC:   the implicit conversion from Ex to Px, and
            // SPEC: * For at least one argument, the conversion from Ex to Px is better than the 
            // SPEC:   conversion from Ex to Qx.

            // If that is hard to follow, consult this handy chart:
            // op1.Left vs op2.Left     op1.Right vs op2.Right    result
            // -----------------------------------------------------------
            // op1 better               op1 better                op1 better
            // op1 better               neither better            op1 better
            // op1 better               op2 better                neither better
            // neither better           op1 better                op1 better
            // neither better           neither better            neither better
            // neither better           op2 better                op2 better
            // op2 better               op1 better                neither better
            // op2 better               neither better            op2 better
            // op2 better               op2 better                op2 better

            if (leftBetter == BetterResult.Left && rightBetter != BetterResult.Right ||
                leftBetter != BetterResult.Right && rightBetter == BetterResult.Left)
            {
                return BetterResult.Left;
            }

            if (leftBetter == BetterResult.Right && rightBetter != BetterResult.Left ||
                leftBetter != BetterResult.Left && rightBetter == BetterResult.Right)
            {
                return BetterResult.Right;
            }

            // There was no better member on the basis of conversions. Go to the tiebreaking round.

            // SPEC: In case the parameter type sequences P1, P2 and Q1, Q2 are equivalent -- that is, every Pi
            // SPEC: has an identity conversion to the corresponding Qi -- the following tie-breaking rules
            // SPEC: are applied:

            if (Conversions.HasIdentityConversion(op1.LeftType, op2.LeftType) &&
                Conversions.HasIdentityConversion(op1.RightType, op2.RightType))
            {
                // NOTE: The native compiler does not follow these rules; effectively, the native 
                // compiler checks for liftedness first, and then for specificity. For example:
                // struct S<T> where T : struct {
                //   public static bool operator +(S<T> x, int y) { return true; }
                //   public static bool? operator +(S<T>? x, int? y) { return false; }
                // }
                // 
                // bool? b = new S<int>?() + new int?();
                //
                // should reason as follows: the two applicable operators are the lifted
                // form of the first operator and the unlifted second operator. The
                // lifted form of the first operator is *more specific* because int?
                // is more specific than T?.  Therefore it should win. In fact the 
                // native compiler chooses the second operator, because it is unlifted.
                // 
                // Roslyn follows the spec rules; if we decide to change the spec to match
                // the native compiler, or decide to change Roslyn to match the native
                // compiler, we should change the order of the checks here.

                // SPEC: If Mp has more specific parameter types than Mq then Mp is better than Mq.
                BetterResult result = MoreSpecificOperator(op1, op2, ref useSiteDiagnostics);
                if (result == BetterResult.Left || result == BetterResult.Right)
                {
                    return result;
                }

                // SPEC: If one member is a non-lifted operator and the other is a lifted operator,
                // SPEC: the non-lifted one is better.

                bool lifted1 = op1.Kind.IsLifted();
                bool lifted2 = op2.Kind.IsLifted();

                if (lifted1 && !lifted2)
                {
                    return BetterResult.Right;
                }
                else if (!lifted1 && lifted2)
                {
                    return BetterResult.Left;
                }
            }

            return BetterResult.Neither;
        }

        private BetterResult MoreSpecificOperator(BinaryOperatorSignature op1, BinaryOperatorSignature op2, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            TypeSymbol op1Left, op1Right, op2Left, op2Right;
            if ((object)op1.Method != null)
            {
                var p = op1.Method.OriginalDefinition.GetParameters();
                op1Left = p[0].Type.TypeSymbol;
                op1Right = p[1].Type.TypeSymbol;
                if (op1.Kind.IsLifted())
                {
                    op1Left = MakeNullable(op1Left);
                    op1Right = MakeNullable(op1Right);
                }
            }
            else
            {
                op1Left = op1.LeftType;
                op1Right = op1.RightType;
            }

            if ((object)op2.Method != null)
            {
                var p = op2.Method.OriginalDefinition.GetParameters();
                op2Left = p[0].Type.TypeSymbol;
                op2Right = p[1].Type.TypeSymbol;
                if (op2.Kind.IsLifted())
                {
                    op2Left = MakeNullable(op2Left);
                    op2Right = MakeNullable(op2Right);
                }
            }
            else
            {
                op2Left = op2.LeftType;
                op2Right = op2.RightType;
            }

            var uninst1 = ArrayBuilder<TypeSymbol>.GetInstance();
            var uninst2 = ArrayBuilder<TypeSymbol>.GetInstance();

            uninst1.Add(op1Left);
            uninst1.Add(op1Right);

            uninst2.Add(op2Left);
            uninst2.Add(op2Right);

            BetterResult result = MoreSpecificType(uninst1, uninst2, ref useSiteDiagnostics);

            uninst1.Free();
            uninst2.Free();

            return result;
        }

        [Conditional("DEBUG")]
        private static void AssertNotChecked(BinaryOperatorKind kind)
        {
            Debug.Assert((kind & ~BinaryOperatorKind.Checked) == kind, "Did not expect operator to be checked.  Consider using .Operator() to mask.");
        }
    }
}
