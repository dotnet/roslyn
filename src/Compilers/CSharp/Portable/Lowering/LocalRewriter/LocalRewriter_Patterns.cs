// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            var expression = VisitExpression(node.Expression);
            var result = LowerPattern(node.Pattern, expression);
            return result;
        }

        // Input must be used no more than once in the result. If it is needed repeatedly store its value in a temp and use the temp.
        BoundExpression LowerPattern(BoundPattern pattern, BoundExpression input)
        {
            var syntax = _factory.Syntax = pattern.Syntax;
            switch (pattern.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        var declPattern = (BoundDeclarationPattern)pattern;
                        return LowerDeclarationPattern(declPattern, input);
                    }

                case BoundKind.WildcardPattern:
                    return _factory.Literal(true);

                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)pattern;
                        return LowerConstantPattern(constantPattern, input);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }

        private BoundExpression LowerConstantPattern(BoundConstantPattern pattern, BoundExpression input)
        {
            return CompareWithConstant(input, VisitExpression(pattern.Value));
        }

        private BoundExpression LowerDeclarationPattern(BoundDeclarationPattern pattern, BoundExpression input)
        {
            Debug.Assert(pattern.Variable.GetTypeOrReturnType() == pattern.DeclaredType.Type);
            var variableAccess = VisitExpression(pattern.VariableAccess);

            if (pattern.IsVar)
            {
                Debug.Assert(input.Type == pattern.Variable.GetTypeOrReturnType());
                var assignment = _factory.AssignmentExpression(variableAccess, input);
                var result = _factory.Literal(true);
                return _factory.MakeSequence(assignment, result);
            }

            return MakeDeclarationPattern(pattern.Syntax, input, variableAccess, requiresNullTest: true);
        }

        /// <summary>
        /// Produce a 'logical and' operation that is clearly irrefutable (<see cref="IsIrrefutablePatternTest(BoundExpression)"/>) when it can be.
        /// </summary>
        BoundExpression LogicalAndForPatterns(BoundExpression left, BoundExpression right)
        {
            return IsIrrefutablePatternTest(left) ? _factory.MakeSequence(left, right) : _factory.LogicalAnd(left, right);
        }

        /// <summary>
        /// Is the test, produced as a result of a pattern-matching operation, always true?
        /// Knowing that enables us to construct slightly more efficient code.
        /// </summary>
        private bool IsIrrefutablePatternTest(BoundExpression test)
        {
            while (true)
            {
                switch (test.Kind)
                {
                    case BoundKind.Literal:
                        {
                            var value = ((BoundLiteral)test).ConstantValue;
                            return value.IsBoolean && value.BooleanValue;
                        }
                    case BoundKind.Sequence:
                        test = ((BoundSequence)test).Value;
                        continue;
                    default:
                        return false;
                }
            }
        }

        private BoundExpression CompareWithConstant(BoundExpression input, BoundExpression boundConstant)
        {
            return _factory.StaticCall(
                _factory.SpecialType(SpecialType.System_Object),
                "Equals",
                _factory.Convert(_factory.SpecialType(SpecialType.System_Object), input),
                _factory.Convert(_factory.SpecialType(SpecialType.System_Object), boundConstant)
                );
        }

        BoundExpression MakeDeclarationPattern(SyntaxNode syntax, BoundExpression input, BoundExpression target, bool requiresNullTest)
        {
            var type = target.Type;

            // a pattern match of the form "expression is Type identifier" is equivalent to
            // an invocation of one of these helpers:
            if (type.IsReferenceType)
            {
                // bool Is<T>(object e, out T t) where T : class // reference type
                // {
                //     t = e as T;
                //     return t != null;
                // }
                if (input.Type == type)
                {
                    // CONSIDER: this can be done whenever input.Type is a subtype of type for improved code
                    var assignment = _factory.AssignmentExpression(target, input);
                    return requiresNullTest
                        ? _factory.ObjectNotEqual(assignment, _factory.Null(type))
                        : _factory.MakeSequence(assignment, _factory.Literal(true));
                }
                else
                {
                    return _factory.ObjectNotEqual(
                        _factory.AssignmentExpression(target, _factory.As(input, type)),
                        _factory.Null(type));
                }
            }
            else if (type.IsNullableType())
            {
                // While `(o is int?)` is statically an error in the binder, we can get here
                // through generic substitution. Note that (null is int?) is false.

                // bool Is<T>(object e, out T? t) where T : struct
                // {
                //     t = e as T?;
                //     return t.HasValue;
                // }
                return _factory.Call(
                    _factory.AssignmentExpression(target, _factory.As(input, type)),
                    GetNullableMethod(syntax, type, SpecialMember.System_Nullable_T_get_HasValue));
            }
            else if (type.IsValueType)
            {
                // It is possible that the input value is already of the correct type, in which case the pattern
                // is irrefutable, and we can just do the assignment and return true.
                if (input.Type == type)
                {
                    return _factory.MakeSequence(
                        _factory.AssignmentExpression(target, input),
                        _factory.Literal(true));
                }

                // It would be possible to improve this code by only assigning t when returning
                // true (avoid returning a new default value)
                // bool Is<T>(object e, out T t) where T : struct // non-Nullable value type
                // {
                //     T? tmp = e as T?;
                //     t = tmp.GetValueOrDefault();
                //     return tmp.HasValue;
                // }
                var tmpType = _factory.SpecialType(SpecialType.System_Nullable_T).Construct(type);
                if (tmpType == input.Type)
                {
                    var value = _factory.Call(
                        input,
                        GetNullableMethod(syntax, tmpType, SpecialMember.System_Nullable_T_GetValueOrDefault));
                    var asg2 = _factory.AssignmentExpression(target, value);
                    var result = requiresNullTest ? MakeNullableHasValue(syntax, input) : _factory.Literal(true);
                    return _factory.MakeSequence(asg2, result);
                }
                else
                {
                    var tmp = _factory.SynthesizedLocal(tmpType, syntax);
                    var asg1 = _factory.AssignmentExpression(_factory.Local(tmp), _factory.As(input, tmpType));
                    var value = _factory.Call(
                        _factory.Local(tmp),
                        GetNullableMethod(syntax, tmpType, SpecialMember.System_Nullable_T_GetValueOrDefault));
                    var asg2 = _factory.AssignmentExpression(target, value);
                    var result = MakeNullableHasValue(syntax, _factory.Local(tmp));
                    return _factory.MakeSequence(tmp, asg1, asg2, result);
                }
            }
            else // type parameter
            {
                Debug.Assert(type.IsTypeParameter());
                // bool Is<T>(this object i, out T o)
                // {
                //     // inefficient because it performs the type test twice.
                //     bool s = i is T;
                //     if (s) o = (T)i;
                //     return s;
                // }
                return _factory.Conditional(_factory.Is(input, type),
                    _factory.MakeSequence(_factory.AssignmentExpression(
                        target,
                        _factory.Convert(type, input)),
                        _factory.Literal(true)),
                    _factory.Literal(false),
                    _factory.SpecialType(SpecialType.System_Boolean));
            }
        }
    }
}
