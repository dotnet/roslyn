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

                case BoundKind.PropertyPattern:
                    {
                        var propertyPattern = (BoundPropertyPattern)pattern;
                        return LowerPropertyPattern(propertyPattern, ref input);
                    }

                case BoundKind.WildcardPattern:
                    return _factory.Literal(true);

                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)pattern;
                        return LowerConstantPattern(constantPattern, input);
                    }

                case BoundKind.RecursivePattern:
                    {
                        var recursivePattern = (BoundRecursivePattern)pattern;
                        return LowerRecursivePattern(recursivePattern, input);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }

        private BoundExpression LowerConstantPattern(BoundConstantPattern pattern, BoundExpression input)
        {
            return CompareWithConstant(input, pattern.Value);
        }

        private BoundExpression LowerPropertyPattern(BoundPropertyPattern pattern, ref BoundExpression input)
        {
            var syntax = pattern.Syntax;
            var temp = _factory.SynthesizedLocal(pattern.Type);
            var matched = MakeDeclarationPattern(syntax, input, temp);
            input = _factory.Local(temp);
            foreach (var subpattern in pattern.Subpatterns)
            {
                // PROTOTYPE(patterns): review and test this code path.
                // https://github.com/dotnet/roslyn/issues/9542
                // e.g. Can the `as` below result in `null`?
                var subProperty = (subpattern.Member as BoundPropertyPatternMember)?.MemberSymbol;
                var subPattern = subpattern.Pattern;
                BoundExpression subExpression;
                switch (subProperty?.Kind)
                {
                    case SymbolKind.Field:
                        subExpression = _factory.Field(input, (FieldSymbol)subProperty);
                        break;
                    case SymbolKind.Property:
                        // PROTOTYPE(patterns): review and test this code path.
                        // https://github.com/dotnet/roslyn/issues/9542
                        // e.g. https://github.com/dotnet/roslyn/pull/9505#discussion_r55320220
                        subExpression = _factory.Call(input, ((PropertySymbol)subProperty).GetMethod);
                        break;
                    case SymbolKind.Event:
                    // PROTOTYPE(patterns): should a property pattern be capable of referencing an event?
                    // https://github.com/dotnet/roslyn/issues/9515
                    default:
                        throw ExceptionUtilities.Unreachable;
                }

                var partialMatch = this.LowerPattern(subPattern, subExpression);
                matched = _factory.LogicalAnd(matched, partialMatch);
            }
            return _factory.Sequence(temp, matched);
        }

        private BoundExpression LowerDeclarationPattern(BoundDeclarationPattern pattern, BoundExpression input)
        {
            Debug.Assert(pattern.IsVar || pattern.LocalSymbol.Type == pattern.DeclaredType.Type);
            if (pattern.IsVar)
            {
                Debug.Assert(input.Type == pattern.LocalSymbol.Type);
                var assignment = _factory.AssignmentExpression(_factory.Local(pattern.LocalSymbol), input);
                var result = _factory.Literal(true);
                return _factory.Sequence(assignment, result);
            }

            return MakeDeclarationPattern(pattern.Syntax, input, pattern.LocalSymbol);
        }

        private BoundExpression LowerRecursivePattern(BoundRecursivePattern recursivePattern, BoundExpression input)
        {
            // cast the input to the argument type of 'operator is'.
            var temp = _factory.SynthesizedLocal(recursivePattern.IsOperator.Parameters[0].Type, recursivePattern.Syntax);
            var test = MakeDeclarationPattern(recursivePattern.Syntax, input, temp);

            // prepare arguments to call 'operator is'
            var arguments = ArrayBuilder<BoundExpression>.GetInstance();
            var argumentTemps = ArrayBuilder<LocalSymbol>.GetInstance();
            var argumentRefKinds = ArrayBuilder<RefKind>.GetInstance();
            arguments.Add(_factory.Local(temp));
            argumentRefKinds.Add(RefKind.None);
            foreach (var p in recursivePattern.IsOperator.Parameters)
            {
                if (p.Ordinal == 0) continue;
                var argumentTemp = _factory.SynthesizedLocal(p.Type, recursivePattern.Patterns[p.Ordinal - 1].Syntax);
                argumentTemps.Add(argumentTemp);
                argumentRefKinds.Add(RefKind.Out);
                arguments.Add(_factory.Local(argumentTemp));
            }

            // invoke 'operator is' and use its result
            BoundExpression invoke = _factory.Call(null, recursivePattern.IsOperator, argumentRefKinds.ToImmutableAndFree(), arguments.ToImmutableAndFree());
            if (recursivePattern.IsOperator.ReturnsVoid)
            {
                // if the user-defined operator is irrefutable, we treat it as returning true
                invoke = _factory.Sequence(invoke, _factory.Literal(true));
            }

            test = _factory.Sequence(temp, LogicalAnd(test, invoke));

            // handle each of the nested patterns.
            for (int i = 0; i < recursivePattern.Patterns.Length; i++)
            {
                var recursiveTest = LowerPattern(recursivePattern.Patterns[i], _factory.Local(argumentTemps[i]));
                test = LogicalAnd(test, recursiveTest);
            }

            return _factory.Sequence(argumentTemps.ToImmutableAndFree(), test);
        }

        /// <summary>
        /// Produce a 'logical and' operator that is clearly irrefutable when it can be.
        /// </summary>
        BoundExpression LogicalAnd(BoundExpression left, BoundExpression right)
        {
            // PROTOTYPE(patterns): can the generated code be improved further?
            return IsIrrefutable(left) ? _factory.Sequence(left, right) : _factory.LogicalAnd(left, right);
        }

        private bool IsIrrefutable(BoundExpression test)
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
            // We currently use "exact" type semantics.
            // PROTOTYPE(patterns): We need to change this to be sensitive to conversions
            // among integral types, so that the same value of different integral types are considered matching.
            return _factory.StaticCall(
                _factory.SpecialType(SpecialType.System_Object),
                "Equals",
                _factory.Convert(_factory.SpecialType(SpecialType.System_Object), input),
                _factory.Convert(_factory.SpecialType(SpecialType.System_Object), boundConstant)
                );
        }

        BoundExpression MakeDeclarationPattern(CSharpSyntaxNode syntax, BoundExpression input, LocalSymbol target)
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
                var assignment = _factory.AssignmentExpression(_factory.Local(target), _factory.As(input, type));
                var result = _factory.ObjectNotEqual(_factory.Local(target), _factory.Null(type));
                return _factory.Sequence(assignment, result);
            }
            else if (type.IsNullableType())
            {
                // This is a semantic error in the binder, so we should not get here.
                // Not sure what the semantics should be, as (null is Nullable<int>) is false.

                // bool Is<T>(object e, out T? t) where T : struct
                // {
                //     t = e as T?;
                //     return e == null || t.HasValue;
                // }
                throw new NotImplementedException();
            }
            else if (type.IsValueType)
            {
                // It is possible that the input value is already of the correct type, in which case the pattern
                // is irrefutable, and we can just do the assignment and return true.
                if (input.Type == type)
                {
                    return _factory.Sequence(
                        _factory.AssignmentExpression(_factory.Local(target), input),
                        _factory.Literal(true));
                }

                // PROTOTYPE(patterns): only assign t when returning true (avoid returning a new default value)
                // bool Is<T>(object e, out T t) where T : struct // non-Nullable value type
                // {
                //     T? tmp = e as T?;
                //     t = tmp.GetValueOrDefault();
                //     return tmp.HasValue;
                // }
                var tmpType = _factory.SpecialType(SpecialType.System_Nullable_T).Construct(type);
                var tmp = _factory.SynthesizedLocal(tmpType, syntax);
                var asg1 = _factory.AssignmentExpression(_factory.Local(tmp), _factory.As(input, tmpType));
                var value = _factory.Call(
                    _factory.Local(tmp),
                    GetNullableMethod(syntax, tmpType, SpecialMember.System_Nullable_T_GetValueOrDefault));
                var asg2 = _factory.AssignmentExpression(_factory.Local(target), value);
                var result = MakeNullableHasValue(syntax, _factory.Local(tmp));
                return _factory.Sequence(tmp, asg1, asg2, result);
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
                    _factory.Sequence(_factory.AssignmentExpression(
                        _factory.Local(target),
                        _factory.Convert(type, input)),
                        _factory.Literal(true)),
                    _factory.Literal(false),
                    _factory.SpecialType(SpecialType.System_Boolean));
            }
        }

        public override BoundNode VisitMatchExpression(BoundMatchExpression node)
        {
            // PROTOTYPE(patterns): find a better way to preserve the scope of pattern variables in a match.

            // we translate a match expression into a sequence of conditionals.
            // However, because we have no way to express the proper scope of the pattern
            // variables, we lump them all together at the top.
            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            BoundAssignmentOperator initialStore;
            var left = VisitExpression(node.Left);
            var temp =_factory.StoreToTemp(left, out initialStore);
            locals.Add(temp.LocalSymbol);
            int n = node.Cases.Length;
            BoundExpression result = _factory.ThrowNullExpression(node.Type);
            for (int i = n-1; i >= 0; i--)
            {
                var c = node.Cases[i];
                locals.AddRange(c.Locals);
                var condition = LowerPattern(c.Pattern, temp);
                if (c.Guard != null) condition = _factory.LogicalAnd(condition, VisitExpression(c.Guard));
                var consequence = VisitExpression(c.Expression);
                _factory.Syntax = c.Syntax;
                result = _factory.Conditional(condition, consequence, result, node.Type);
            }

            return _factory.Sequence(locals.ToImmutableAndFree(), initialStore, result);
        }

        public override BoundNode VisitLetStatement(BoundLetStatement node)
        {
            BoundAssignmentOperator initialStore;
            _factory.Syntax = node.Expression.Syntax;
            var temp = _factory.StoreToTemp(VisitExpression(node.Expression), out initialStore);
            _factory.Syntax = node.Pattern.Syntax;
            var pattern = LowerPattern(node.Pattern, temp);
            _factory.Syntax = node.Expression.Syntax;
            var condition = _factory.Sequence(ImmutableArray.Create(temp.LocalSymbol), initialStore, pattern);
            if (node.Guard != null)
            {
                _factory.Syntax = node.Guard.Syntax;
                condition = _factory.LogicalAnd(condition, VisitExpression(node.Guard));
            }
            _factory.Syntax = node.Syntax;
            BoundStatement result = (node.Else == null)
                ? _factory.ExpressionStatement(condition)
                : _factory.If(_factory.Not(condition), VisitStatement(node.Else));
            return result;
        }
    }
}
