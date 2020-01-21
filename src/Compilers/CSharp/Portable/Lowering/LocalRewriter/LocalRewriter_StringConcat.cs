// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        /// <summary>
        /// The strategy of this rewrite is to do rewrite "locally".
        /// We analyze arguments of the concat in a shallow fashion assuming that 
        /// lowering and optimizations (including this one) is already done for the arguments.
        /// Based on the arguments we select the most appropriate pattern for the current node.
        /// 
        /// NOTE: it is not guaranteed that the node that we chose will be the most optimal since we have only 
        ///       local information - i.e. we look at the arguments, but we do not know about siblings.
        ///       When we move to the parent, the node may be rewritten by this or some another optimization.
        ///       
        /// Example:
        ///     result = ( "abc" + "def" + null ?? expr1 + "moo" + "baz" ) + expr2
        /// 
        /// Will rewrite into:
        ///     result = Concat("abcdef", expr2)
        ///     
        /// However there will be transient nodes like  Concat(expr1 + "moo")  that will not be present in the
        /// resulting tree.
        ///
        /// </summary>
        private BoundExpression RewriteStringConcatenation(SyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type)
        {
            Debug.Assert(
                operatorKind == BinaryOperatorKind.StringConcatenation ||
                operatorKind == BinaryOperatorKind.StringAndObjectConcatenation ||
                operatorKind == BinaryOperatorKind.ObjectAndStringConcatenation);

            if (_inExpressionLambda)
            {
                return RewriteStringConcatInExpressionLambda(syntax, operatorKind, loweredLeft, loweredRight, type);
            }

            // Convert both sides to a string (calling ToString if necessary)
            loweredLeft = ConvertConcatExprToString(syntax, loweredLeft);
            loweredRight = ConvertConcatExprToString(syntax, loweredRight);

            Debug.Assert(loweredLeft.Type.IsStringType() || loweredLeft.ConstantValue?.IsNull == true || loweredLeft.Type.IsErrorType());
            Debug.Assert(loweredRight.Type.IsStringType() || loweredRight.ConstantValue?.IsNull == true || loweredRight.Type.IsErrorType());

            // try fold two args without flattening.
            var folded = TryFoldTwoConcatOperands(syntax, loweredLeft, loweredRight);
            if (folded != null)
            {
                return folded;
            }

            // flatten and merge -  ( expr1 + "A" ) + ("B" + expr2) ===> (expr1 + "AB" + expr2)
            ArrayBuilder<BoundExpression> leftFlattened = ArrayBuilder<BoundExpression>.GetInstance();
            ArrayBuilder<BoundExpression> rightFlattened = ArrayBuilder<BoundExpression>.GetInstance();

            FlattenConcatArg(loweredLeft, leftFlattened);
            FlattenConcatArg(loweredRight, rightFlattened);

            if (leftFlattened.Any() && rightFlattened.Any())
            {
                folded = TryFoldTwoConcatOperands(syntax, leftFlattened.Last(), rightFlattened.First());
                if (folded != null)
                {
                    rightFlattened[0] = folded;
                    leftFlattened.RemoveLast();
                }
            }

            leftFlattened.AddRange(rightFlattened);
            rightFlattened.Free();

            BoundExpression result;

            switch (leftFlattened.Count)
            {
                case 0:
                    result = _factory.StringLiteral(string.Empty);
                    break;

                case 1:
                    // All code paths which reach here (through TryFoldTwoConcatOperands) have already called
                    // RewriteStringConcatenationOneExpr if necessary
                    result = leftFlattened[0];
                    break;

                case 2:
                    var left = leftFlattened[0];
                    var right = leftFlattened[1];
                    result = RewriteStringConcatenationTwoExprs(syntax, left, right);
                    break;

                case 3:
                    {
                        var first = leftFlattened[0];
                        var second = leftFlattened[1];
                        var third = leftFlattened[2];
                        result = RewriteStringConcatenationThreeExprs(syntax, first, second, third);
                    }
                    break;

                case 4:
                    {
                        var first = leftFlattened[0];
                        var second = leftFlattened[1];
                        var third = leftFlattened[2];
                        var fourth = leftFlattened[3];
                        result = RewriteStringConcatenationFourExprs(syntax, first, second, third, fourth);
                    }
                    break;

                default:
                    result = RewriteStringConcatenationManyExprs(syntax, leftFlattened.ToImmutable());
                    break;
            }

            leftFlattened.Free();
            return result;
        }

        /// <summary>
        /// digs into known concat operators and unwraps their arguments
        /// otherwise returns the expression as-is
        /// 
        /// Generally we only need to recognize same node patterns that we create as a result of concatenation rewrite.
        /// </summary>
        private void FlattenConcatArg(BoundExpression lowered, ArrayBuilder<BoundExpression> flattened)
        {
            if (TryExtractStringConcatArgs(lowered, out var arguments))
            {
                flattened.AddRange(arguments);
            }
            else
            {
                // fallback - if nothing above worked, leave arg as-is
                flattened.Add(lowered);
            }
        }

        /// <summary>
        /// Determines whether an expression is a known string concat operator (with or without a subsequent ?? ""), and extracts
        /// its args if so.
        /// </summary>
        /// <returns>True if this is a call to a known string concat operator, false otherwise</returns>
        private bool TryExtractStringConcatArgs(BoundExpression lowered, out ImmutableArray<BoundExpression> arguments)
        {
            switch (lowered.Kind)
            {
                case BoundKind.Call:
                    var boundCall = (BoundCall)lowered;
                    var method = boundCall.Method;
                    if (method.IsStatic && method.ContainingType.SpecialType == SpecialType.System_String)
                    {
                        if ((object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringString) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringStringString))
                        {
                            arguments = boundCall.Arguments;
                            return true;
                        }

                        if ((object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringArray))
                        {
                            var args = boundCall.Arguments[0] as BoundArrayCreation;
                            if (args != null)
                            {
                                var initializer = args.InitializerOpt;
                                if (initializer != null)
                                {
                                    arguments = initializer.Initializers;
                                    return true;
                                }
                            }
                        }
                    }
                    break;

                case BoundKind.NullCoalescingOperator:
                    var boundCoalesce = (BoundNullCoalescingOperator)lowered;

                    if (boundCoalesce.LeftConversion.IsIdentity)
                    {
                        // The RHS may be a constant value with an identity conversion to string even
                        // if it is not a string: in particular, the null literal behaves this way.
                        // To be safe, check that the constant value is actually a string before
                        // attempting to access its value as a string.

                        var rightConstant = boundCoalesce.RightOperand.ConstantValue;
                        if (rightConstant != null && rightConstant.IsString && rightConstant.StringValue.Length == 0)
                        {
                            arguments = ImmutableArray.Create(boundCoalesce.LeftOperand);
                            return true;
                        }
                    }
                    break;
            }

            arguments = default;
            return false;
        }

        /// <summary>
        /// folds two concat operands into one expression if possible
        /// otherwise returns null
        /// </summary>
        private BoundExpression TryFoldTwoConcatOperands(SyntaxNode syntax, BoundExpression loweredLeft, BoundExpression loweredRight)
        {
            // both left and right are constants
            var leftConst = loweredLeft.ConstantValue;
            var rightConst = loweredRight.ConstantValue;

            if (leftConst != null && rightConst != null)
            {
                // const concat may fail to fold if strings are huge. 
                // This would be unusual.
                ConstantValue concatenated = TryFoldTwoConcatConsts(leftConst, rightConst);
                if (concatenated != null)
                {
                    return _factory.StringLiteral(concatenated);
                }
            }

            // one or another is null. 
            if (IsNullOrEmptyStringConstant(loweredLeft))
            {
                if (IsNullOrEmptyStringConstant(loweredRight))
                {
                    return _factory.Literal((string)null + (string)null);
                }

                return RewriteStringConcatenationOneExpr(syntax, loweredRight);
            }
            else if (IsNullOrEmptyStringConstant(loweredRight))
            {
                return RewriteStringConcatenationOneExpr(syntax, loweredLeft);
            }

            return null;
        }

        private static bool IsNullOrEmptyStringConstant(BoundExpression operand)
        {
            return (operand.ConstantValue != null && string.IsNullOrEmpty(operand.ConstantValue.StringValue)) ||
                    operand.IsDefaultValue();
        }

        /// <summary>
        /// folds two concat constants into one if possible
        /// otherwise returns null.
        /// It is generally always possible to concat constants, unless resulting string would be too large.
        /// </summary>
        private static ConstantValue TryFoldTwoConcatConsts(ConstantValue leftConst, ConstantValue rightConst)
        {
            var leftVal = leftConst.StringValue;
            var rightVal = rightConst.StringValue;

            if (!leftConst.IsDefaultValue && !rightConst.IsDefaultValue)
            {
                if (leftVal.Length + rightVal.Length < 0)
                {
                    return null;
                }
            }

            // TODO: if transient string allocations are an issue, consider introducing constants that contain builders.
            //       it may be not so easy to even get here though, since typical
            //       "A" + "B" + "C" + ... cases should be folded in the binder as spec requires so.
            //       we would be mostly picking here edge cases like "A" + (object)null + "B" + (object)null + ...
            return ConstantValue.Create(leftVal + rightVal);
        }

        /// <summary>
        /// Strangely enough there is such a thing as unary concatenation and it must be rewritten.
        /// </summary>
        private BoundExpression RewriteStringConcatenationOneExpr(SyntaxNode syntax, BoundExpression loweredOperand)
        {
            // If it's a call to 'string.Concat' (or is something which ends in '?? ""', which this method also extracts),
            // we know the result cannot be null. Otherwise return loweredOperand ?? ""
            if (TryExtractStringConcatArgs(loweredOperand, out _))
            {
                return loweredOperand;
            }
            else
            {
                return _factory.Coalesce(loweredOperand, _factory.Literal(""));
            }
        }

        private BoundExpression RewriteStringConcatenationTwoExprs(SyntaxNode syntax, BoundExpression loweredLeft, BoundExpression loweredRight)
        {
            Debug.Assert(loweredLeft.HasAnyErrors || loweredLeft.Type.IsStringType());
            Debug.Assert(loweredRight.HasAnyErrors || loweredRight.Type.IsStringType());

            var method = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatStringString);
            Debug.Assert((object)method != null);

            return (BoundExpression)BoundCall.Synthesized(syntax, null, method, loweredLeft, loweredRight);
        }

        private BoundExpression RewriteStringConcatenationThreeExprs(SyntaxNode syntax, BoundExpression loweredFirst, BoundExpression loweredSecond, BoundExpression loweredThird)
        {
            Debug.Assert(loweredFirst.HasAnyErrors || loweredFirst.Type.IsStringType());
            Debug.Assert(loweredSecond.HasAnyErrors || loweredSecond.Type.IsStringType());
            Debug.Assert(loweredThird.HasAnyErrors || loweredThird.Type.IsStringType());

            var method = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatStringStringString);
            Debug.Assert((object)method != null);

            return BoundCall.Synthesized(syntax, null, method, ImmutableArray.Create(loweredFirst, loweredSecond, loweredThird));
        }

        private BoundExpression RewriteStringConcatenationFourExprs(SyntaxNode syntax, BoundExpression loweredFirst, BoundExpression loweredSecond, BoundExpression loweredThird, BoundExpression loweredFourth)
        {
            Debug.Assert(loweredFirst.HasAnyErrors || loweredFirst.Type.IsStringType());
            Debug.Assert(loweredSecond.HasAnyErrors || loweredSecond.Type.IsStringType());
            Debug.Assert(loweredThird.HasAnyErrors || loweredThird.Type.IsStringType());
            Debug.Assert(loweredFourth.HasAnyErrors || loweredFourth.Type.IsStringType());

            var method = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatStringStringStringString);
            Debug.Assert((object)method != null);

            return BoundCall.Synthesized(syntax, null, method, ImmutableArray.Create(loweredFirst, loweredSecond, loweredThird, loweredFourth));
        }

        private BoundExpression RewriteStringConcatenationManyExprs(SyntaxNode syntax, ImmutableArray<BoundExpression> loweredArgs)
        {
            Debug.Assert(loweredArgs.Length > 4);
            Debug.Assert(loweredArgs.All(a => a.HasErrors || a.Type.IsStringType()));

            var method = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatStringArray);
            Debug.Assert((object)method != null);

            var array = _factory.ArrayOrEmpty(_factory.SpecialType(SpecialType.System_String), loweredArgs);

            return (BoundExpression)BoundCall.Synthesized(syntax, null, method, array);
        }

        /// <summary>
        /// Most of the above optimizations are not applicable in expression trees as the operator
        /// must stay a binary operator. We cannot do much beyond constant folding which is done in binder.
        /// </summary>
        private BoundExpression RewriteStringConcatInExpressionLambda(SyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type)
        {
            SpecialMember member = (operatorKind == BinaryOperatorKind.StringConcatenation) ?
                SpecialMember.System_String__ConcatStringString :
                SpecialMember.System_String__ConcatObjectObject;

            var method = UnsafeGetSpecialTypeMethod(syntax, member);
            Debug.Assert((object)method != null);

            return new BoundBinaryOperator(syntax, operatorKind, default(ConstantValue), method, default(LookupResultKind), loweredLeft, loweredRight, type);
        }

        /// <summary>
        /// Returns an expression which converts the given expression into a string (or null).
        /// If necessary, this invokes .ToString() on the expression, to avoid boxing value types.
        /// </summary>
        private BoundExpression ConvertConcatExprToString(SyntaxNode syntax, BoundExpression expr)
        {
            // If it's a value type, it'll have been boxed by the +(string, object) or +(object, string)
            // operator. Undo that.
            if (expr.Kind == BoundKind.Conversion)
            {
                BoundConversion conv = (BoundConversion)expr;
                if (conv.ConversionKind == ConversionKind.Boxing)
                {
                    expr = conv.Operand;
                }
            }

            // Is the expression a literal char?  If so, we can
            // simply make it a literal string instead and avoid any 
            // allocations for converting the char to a string at run time.
            // Similarly if it's a literal null, don't do anything special.
            if (expr.Kind == BoundKind.Literal)
            {
                ConstantValue cv = ((BoundLiteral)expr).ConstantValue;
                if (cv != null)
                {
                    if (cv.SpecialType == SpecialType.System_Char)
                    {
                        return _factory.StringLiteral(cv.CharValue.ToString());
                    }
                    else if (cv.IsNull)
                    {
                        return expr;
                    }
                }
            }

            // If it's a string already, just return it
            if (expr.Type.IsStringType())
            {
                return expr;
            }

            // Evaluate toString at the last possible moment, to avoid spurious diagnostics if it's missing.
            // All code paths below here use it.
            var objectToStringMethod = UnsafeGetSpecialTypeMethod(syntax, SpecialMember.System_Object__ToString);

            // If it's a struct which has overridden ToString, find that method. Note that we might fail to
            // find it, e.g. if object.ToString is missing
            MethodSymbol structToStringMethod = null;
            if (expr.Type.IsValueType && !expr.Type.IsTypeParameter())
            {
                var type = (NamedTypeSymbol)expr.Type;
                var typeToStringMembers = type.GetMembers(objectToStringMethod.Name);
                foreach (var member in typeToStringMembers)
                {
                    if (member is MethodSymbol toStringMethod &&
                        toStringMethod.GetLeastOverriddenMethod(type) == (object)objectToStringMethod)
                    {
                        structToStringMethod = toStringMethod;
                        break;
                    }
                }
            }

            // If it's a special value type (and not a field of a MarshalByRef object), it should have its own ToString method (but we might fail to find
            // it if object.ToString is missing). Assume that this won't be removed, and emit a direct call rather
            // than a constrained virtual call. This keeps in the spirit of #7079, but expands the range of
            // types to all special value types.
            if (structToStringMethod != null && (expr.Type.SpecialType != SpecialType.None && !isFieldOfMarshalByRef(expr, _compilation)))
            {
                return BoundCall.Synthesized(expr.Syntax, expr, structToStringMethod);
            }

            // - It's a reference type (excluding unconstrained generics): no copy
            // - It's a constant: no copy
            // - The type definitely doesn't have its own ToString method (i.e. we're definitely calling 
            //   object.ToString on a struct type, not type parameter): no copy (yes this is a versioning issue,
            //   but that doesn't matter)
            // - We're calling the type's own ToString method, and it's effectively readonly (the method or the whole
            //   type is readonly): no copy
            // - Otherwise: copy
            // This is to mimic the old behaviour, where value types would be boxed before ToString was called on them,
            // but with optimizations for readonly methods.
            bool callWithoutCopy = expr.Type.IsReferenceType ||
                expr.ConstantValue != null ||
                (structToStringMethod == null && !expr.Type.IsTypeParameter()) ||
                structToStringMethod?.IsEffectivelyReadOnly == true;

            // No need for a conditional access if it's a value type - we know it's not null.
            if (expr.Type.IsValueType)
            {
                if (!callWithoutCopy)
                {
                    expr = new BoundPassByCopy(expr.Syntax, expr, expr.Type);
                }
                return BoundCall.Synthesized(expr.Syntax, expr, objectToStringMethod);
            }

            if (callWithoutCopy)
            {
                return makeConditionalAccess(expr);
            }
            else
            {
                // If we do conditional access on a copy, we need a proper BoundLocal rather than a
                // BoundPassByCopy (as it's accessed multiple times). If we don't do this, and the
                // receiver is an unconstrained generic parameter, BoundLoweredConditionalAccess has
                // to generate a lot of code to ensure it only accesses the copy once (which is pointless).
                var temp = _factory.StoreToTemp(expr, out var store);
                return _factory.Sequence(
                    ImmutableArray.Create(temp.LocalSymbol),
                    ImmutableArray.Create<BoundExpression>(store),
                    makeConditionalAccess(temp));
            }

            BoundExpression makeConditionalAccess(BoundExpression receiver)
            {
                int currentConditionalAccessID = ++_currentConditionalAccessID;

                return new BoundLoweredConditionalAccess(
                    syntax,
                    receiver,
                    hasValueMethodOpt: null,
                    whenNotNull: BoundCall.Synthesized(
                        syntax,
                        new BoundConditionalReceiver(syntax, currentConditionalAccessID, expr.Type),
                        objectToStringMethod),
                    whenNullOpt: null,
                    id: currentConditionalAccessID,
                    type: _compilation.GetSpecialType(SpecialType.System_String));
            }

            static bool isFieldOfMarshalByRef(BoundExpression expr, CSharpCompilation compilation)
            {
                if (expr is BoundFieldAccess fieldAccess)
                {
                    return DiagnosticsPass.IsNonAgileFieldAccess(fieldAccess, compilation);
                }
                return false;
            }
        }
    }
}
