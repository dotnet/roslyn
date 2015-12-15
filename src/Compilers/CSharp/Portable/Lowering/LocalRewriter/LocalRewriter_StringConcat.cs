// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.RuntimeMembers;

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
        private BoundExpression RewriteStringConcatenation(CSharpSyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type)
        {
            Debug.Assert(
                operatorKind == BinaryOperatorKind.StringConcatenation ||
                operatorKind == BinaryOperatorKind.StringAndObjectConcatenation ||
                operatorKind == BinaryOperatorKind.ObjectAndStringConcatenation);

            if (_inExpressionLambda)
            {
                return RewriteStringConcatInExpressionLambda(syntax, operatorKind, loweredLeft, loweredRight, type);
            }

            // avoid run time boxing and ToString operations if we can reasonably convert to a string at compile time
            loweredLeft = ConvertConcatExprToStringIfPossible(syntax, loweredLeft);
            loweredRight = ConvertConcatExprToStringIfPossible(syntax, loweredRight);

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
                    result = leftFlattened[0];
                    break;

                case 2:
                    var left = leftFlattened[0];
                    var right = leftFlattened[1];
                    result = RewriteStringConcatenationTwoExprs(syntax, left, right);
                    break;

                case 3:
                    var first = leftFlattened[0];
                    var second = leftFlattened[1];
                    var third = leftFlattened[2];
                    result = RewriteStringConcatenationThreeExprs(syntax, first, second, third);
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
            switch (lowered.Kind)
            {
                case BoundKind.Call:
                    var boundCall = (BoundCall)lowered;

                    var method = boundCall.Method;
                    if (method.IsStatic && method.ContainingType.SpecialType == SpecialType.System_String)
                    {
                        if ((object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringString) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringStringString) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatObject) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatObjectObject) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatObjectObjectObject))
                        {
                            flattened.AddRange(boundCall.Arguments);
                            return;
                        }

                        if ((object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringArray) ||
                            (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatObjectArray))
                        {
                            var args = boundCall.Arguments[0] as BoundArrayCreation;
                            if (args != null)
                            {
                                var initializer = args.InitializerOpt;
                                if (initializer != null)
                                {
                                    flattened.AddRange(initializer.Initializers);
                                    return;
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
                            flattened.Add(boundCoalesce.LeftOperand);
                            return;
                        }
                    }
                    break;
            }

            // fallback - if nothing above worked, leave arg as-is
            flattened.Add(lowered);
            return;
        }

        /// <summary>
        /// folds two concat operands into one expression if possible
        /// otherwise returns null
        /// </summary>
        private BoundExpression TryFoldTwoConcatOperands(CSharpSyntaxNode syntax, BoundExpression loweredLeft, BoundExpression loweredRight)
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
        private BoundExpression RewriteStringConcatenationOneExpr(CSharpSyntaxNode syntax, BoundExpression loweredOperand)
        {
            if (loweredOperand.Type.SpecialType == SpecialType.System_String)
            {
                // loweredOperand ?? ""
                return _factory.Coalesce(loweredOperand, _factory.Literal(""));
            }

            var method = GetSpecialTypeMethod(syntax, SpecialMember.System_String__ConcatObject);
            Debug.Assert((object)method != null);

            return (BoundExpression)BoundCall.Synthesized(syntax, null, method, loweredOperand);
        }

        private BoundExpression RewriteStringConcatenationTwoExprs(CSharpSyntaxNode syntax, BoundExpression loweredLeft, BoundExpression loweredRight)
        {
            SpecialMember member = (loweredLeft.Type.SpecialType == SpecialType.System_String && loweredRight.Type.SpecialType == SpecialType.System_String) ?
                SpecialMember.System_String__ConcatStringString :
                SpecialMember.System_String__ConcatObjectObject;

            var method = GetSpecialTypeMethod(syntax, member);
            Debug.Assert((object)method != null);

            return (BoundExpression)BoundCall.Synthesized(syntax, null, method, loweredLeft, loweredRight);
        }

        private BoundExpression RewriteStringConcatenationThreeExprs(CSharpSyntaxNode syntax, BoundExpression loweredFirst, BoundExpression loweredSecond, BoundExpression loweredThird)
        {
            SpecialMember member = (loweredFirst.Type.SpecialType == SpecialType.System_String &&
                                    loweredSecond.Type.SpecialType == SpecialType.System_String &&
                                    loweredThird.Type.SpecialType == SpecialType.System_String) ?
                SpecialMember.System_String__ConcatStringStringString :
                SpecialMember.System_String__ConcatObjectObjectObject;

            var method = GetSpecialTypeMethod(syntax, member);
            Debug.Assert((object)method != null);

            return BoundCall.Synthesized(syntax, null, method, ImmutableArray.Create(loweredFirst, loweredSecond, loweredThird));
        }

        private BoundExpression RewriteStringConcatenationManyExprs(CSharpSyntaxNode syntax, ImmutableArray<BoundExpression> loweredArgs)
        {
            Debug.Assert(loweredArgs.Length > 3);
            Debug.Assert(loweredArgs.All(a => a.Type.SpecialType == SpecialType.System_Object || a.Type.SpecialType == SpecialType.System_String));

            bool isObject = false;
            TypeSymbol elementType = null;

            foreach (var arg in loweredArgs)
            {
                elementType = arg.Type;
                if (elementType.SpecialType != SpecialType.System_String)
                {
                    isObject = true;
                    break;
                }
            }

            // Count == 4 is handled differently because there is a Concat method with 4 arguments
            // for strings, but there is no such method for objects.
            if (!isObject && loweredArgs.Length == 4)
            {
                SpecialMember member = SpecialMember.System_String__ConcatStringStringStringString;
                var method = GetSpecialTypeMethod(syntax, member);
                Debug.Assert((object)method != null);

                return (BoundExpression)BoundCall.Synthesized(syntax, null, method, loweredArgs);
            }
            else
            {
                SpecialMember member = isObject ?
                    SpecialMember.System_String__ConcatObjectArray :
                    SpecialMember.System_String__ConcatStringArray;

                var method = GetSpecialTypeMethod(syntax, member);
                Debug.Assert((object)method != null);

                var array = _factory.Array(elementType, loweredArgs);

                return (BoundExpression)BoundCall.Synthesized(syntax, null, method, array);
            }
        }

        /// <summary>
        /// Most of the above optimizations are not applicable in expression trees as the operator
        /// must stay a binary operator. We cannot do much beyond constant folding which is done in binder.
        /// </summary>
        private BoundExpression RewriteStringConcatInExpressionLambda(CSharpSyntaxNode syntax, BinaryOperatorKind operatorKind, BoundExpression loweredLeft, BoundExpression loweredRight, TypeSymbol type)
        {
            SpecialMember member = (operatorKind == BinaryOperatorKind.StringConcatenation) ?
                SpecialMember.System_String__ConcatStringString :
                SpecialMember.System_String__ConcatObjectObject;

            var method = GetSpecialTypeMethod(syntax, member);
            Debug.Assert((object)method != null);

            return new BoundBinaryOperator(syntax, operatorKind, loweredLeft, loweredRight, default(ConstantValue), method, default(LookupResultKind), type);
        }

        /// <summary>
        /// Checks whether the expression represents a boxing conversion of a special value type.
        /// If it does, it tries to return a string-based representation instead in order
        /// to avoid allocations.  If it can't, the original expression is returned.
        /// </summary>
        private BoundExpression ConvertConcatExprToStringIfPossible(CSharpSyntaxNode syntax, BoundExpression expr)
        {
            if (expr.Kind == BoundKind.Conversion)
            {
                BoundConversion conv = (BoundConversion)expr;
                if (conv.ConversionKind == ConversionKind.Boxing)
                {
                    BoundExpression operand = conv.Operand;
                    if (operand != null)
                    {
                        // Is the expression a literal char?  If so, we can
                        // simply make it a literal string instead and avoid any 
                        // allocations for converting the char to a string at run time.
                        if (operand.Kind == BoundKind.Literal)
                        {
                            ConstantValue cv = ((BoundLiteral)operand).ConstantValue;
                            if (cv != null && cv.SpecialType == SpecialType.System_Char)
                            {
                                return _factory.StringLiteral(cv.CharValue.ToString());
                            }
                        }

                        // Can the expression be optimized with a ToString call?
                        // If so, we can synthesize a ToString call to avoid boxing.
                        if (ConcatExprCanBeOptimizedWithToString(operand.Type))
                        {
                            var toString = GetSpecialTypeMethod(syntax, SpecialMember.System_Object__ToString);

                            var type = (NamedTypeSymbol)operand.Type;
                            var toStringMembers = type.GetMembers(toString.Name);
                            foreach(var member in toStringMembers)
                            {
                                var toStringMethod = member as MethodSymbol;
                                if (toStringMethod.GetLeastOverriddenMethod(type) == (object)toString)
                                {
                                    return BoundCall.Synthesized(syntax, operand, toStringMethod);
                                }
                            }
                        }
                    }
                }
            }

            // Optimization not possible; just return the original expression.
            return expr;
        }

        /// <summary>
        /// Gets whether the type of an argument used in string concatenation can
        /// be optimized by first calling ToString on it before passing the argument
        /// to the String.Concat function.
        /// </summary>
        /// <param name="symbol">The type symbol of the argument.</param>
        /// <returns>
        /// true if ToString may be used; false if using ToString could lead to observable differences in behavior.
        /// </returns>
        private static bool ConcatExprCanBeOptimizedWithToString(TypeSymbol symbol)
        {
            // There are several constraints applied here in support of backwards compatibility:
            // - This optimization potentially changes the order in which ToString is called
            //   on the arguments.  That's a a compatibility issue if one argument's ToString
            //   depends on state mutated by another, such as current culture.
            // - For value types, this optimization causes ToString to be called on the original
            //   value rather than on a boxed copy.  That means a mutating ToString implementation
            //   could change the original rather than the copy.
            // For these reasons, this optimization is currently restricted to primitives
            // known to have a non-mutating ToString implementation that is independent
            // of externally mutable state.  Common value types such as Int32 and Double
            // do not meet this bar.

            switch (symbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return true;
                default:
                    return false;
            }
        }
    }
}
