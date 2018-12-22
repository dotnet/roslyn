// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            var loweredExpression = VisitExpression(node.Expression);
            var loweredPattern = LowerPattern(node.Pattern);
            return MakeIsPattern(loweredPattern, loweredExpression);
        }

        // Input must be used no more than once in the result. If it is needed repeatedly store its value in a temp and use the temp.
        BoundExpression MakeIsPattern(BoundPattern loweredPattern, BoundExpression loweredInput)
        {
            var syntax = _factory.Syntax = loweredPattern.Syntax;
            switch (loweredPattern.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        var declPattern = (BoundDeclarationPattern)loweredPattern;
                        return MakeIsDeclarationPattern(declPattern, loweredInput);
                    }

                case BoundKind.WildcardPattern:
                    return _factory.Literal(true);

                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)loweredPattern;
                        return MakeIsConstantPattern(constantPattern, loweredInput);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(loweredPattern.Kind);
            }
        }

        BoundPattern LowerPattern(BoundPattern pattern)
        {
            switch (pattern.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        var declPattern = (BoundDeclarationPattern)pattern;
                        return declPattern.Update(declPattern.Variable, VisitExpression(declPattern.VariableAccess), declPattern.DeclaredType, declPattern.IsVar);
                    }
                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)pattern;
                        return constantPattern.Update(VisitExpression(constantPattern.Value), constantPattern.ConstantValue);
                    }
                default:
                    return pattern;
            }
        }

        private BoundExpression MakeIsConstantPattern(BoundConstantPattern loweredPattern, BoundExpression loweredInput)
        {
            return CompareWithConstant(loweredInput, loweredPattern.Value);
        }

        private BoundExpression MakeIsDeclarationPattern(BoundDeclarationPattern loweredPattern, BoundExpression loweredInput)
        {
            Debug.Assert(((object)loweredPattern.Variable == null && loweredPattern.VariableAccess.Kind == BoundKind.DiscardExpression) ||
                         TypeSymbol.Equals(loweredPattern.Variable.GetTypeOrReturnType().TypeSymbol, loweredPattern.DeclaredType.Type, TypeCompareKind.ConsiderEverything2));

            if (loweredPattern.IsVar)
            {
                var result = _factory.Literal(true);

                if (loweredPattern.VariableAccess.Kind == BoundKind.DiscardExpression)
                {
                    return _factory.MakeSequence(loweredInput, result);
                }

                Debug.Assert((object)loweredPattern.Variable != null && loweredInput.Type.Equals(loweredPattern.Variable.GetTypeOrReturnType().TypeSymbol, TypeCompareKind.AllIgnoreOptions));

                var assignment = _factory.AssignmentExpression(loweredPattern.VariableAccess, loweredInput);
                return _factory.MakeSequence(assignment, result);
            }

            if (loweredPattern.VariableAccess.Kind == BoundKind.DiscardExpression)
            {
                LocalSymbol temp;
                BoundLocal discard = _factory.MakeTempForDiscard((BoundDiscardExpression)loweredPattern.VariableAccess, out temp);

                return _factory.Sequence(ImmutableArray.Create(temp),
                         sideEffects: ImmutableArray<BoundExpression>.Empty,
                         result: MakeIsDeclarationPattern(loweredPattern.Syntax, loweredInput, discard, requiresNullTest: loweredInput.Type.CanContainNull()));
            }

            return MakeIsDeclarationPattern(loweredPattern.Syntax, loweredInput, loweredPattern.VariableAccess, requiresNullTest: loweredInput.Type.CanContainNull());
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
            var systemObject = _factory.SpecialType(SpecialType.System_Object);
            if (boundConstant.ConstantValue == ConstantValue.Null)
            {
                if (input.Type.IsNonNullableValueType())
                {
                    var systemBoolean = _factory.SpecialType(SpecialType.System_Boolean);
                    return RewriteNullableNullEquality(
                        syntax: _factory.Syntax,
                        kind: BinaryOperatorKind.NullableNullEqual,
                        loweredLeft: input,
                        loweredRight: boundConstant,
                        returnType: systemBoolean);
                }
                else
                {
                    return _factory.ObjectEqual(_factory.Convert(systemObject, input), boundConstant);
                }
            }
            else if (input.Type.IsNullableType() && boundConstant.NullableNeverHasValue())
            {
                return _factory.Not(MakeNullableHasValue(_factory.Syntax, input));
            }
            else
            {
                return _factory.StaticCall(
                    systemObject,
                    "Equals",
                    _factory.Convert(systemObject, boundConstant),
                    _factory.Convert(systemObject, input)
                    );
            }
        }

        private bool? MatchConstantValue(BoundExpression source, TypeSymbol targetType, bool requiredNullTest)
        {
            // use site diagnostics will already have been reported during binding.
            HashSet<DiagnosticInfo> ignoredDiagnostics = null;
            var sourceType = source.Type.IsDynamic() ? _compilation.GetSpecialType(SpecialType.System_Object) : source.Type;
            var conversionKind = _compilation.Conversions.ClassifyBuiltInConversion(sourceType, targetType, ref ignoredDiagnostics).Kind;
            var constantResult = Binder.GetIsOperatorConstantResult(sourceType, targetType, conversionKind, source.ConstantValue, requiredNullTest);
            return
                constantResult == ConstantValue.True ? true :
                constantResult == ConstantValue.False ? false :
                constantResult == null ? (bool?)null :
                throw ExceptionUtilities.UnexpectedValue(constantResult);
        }

        BoundExpression MakeIsDeclarationPattern(SyntaxNode syntax, BoundExpression loweredInput, BoundExpression loweredTarget, bool requiresNullTest)
        {
            var type = loweredTarget.Type;
            requiresNullTest = requiresNullTest && loweredInput.Type.CanContainNull();

            // If the match is impossible, we simply evaluate the input and yield false.
            var matchConstantValue = MatchConstantValue(loweredInput, type, false);
            if (matchConstantValue == false)
            {
                return _factory.MakeSequence(loweredInput, _factory.Literal(false));
            }

            // It is possible that the input value is already of the correct type, in which case the pattern
            // is irrefutable, and we can just do the assignment and return true (or perform the null test).
            if (matchConstantValue == true)
            {
                requiresNullTest = requiresNullTest && MatchConstantValue(loweredInput, type, true) != true;
                if (loweredInput.Type.IsNullableType())
                {
                    var getValueOrDefault = _factory.SpecialMethod(SpecialMember.System_Nullable_T_GetValueOrDefault).AsMember((NamedTypeSymbol)loweredInput.Type);
                    if (requiresNullTest)
                    {
                        //bool Is<T>(T? input, out T output) where T : struct
                        //{
                        //    output = input.GetValueOrDefault();
                        //    return input.HasValue;
                        //}

                        var input = _factory.SynthesizedLocal(loweredInput.Type, syntax); // we copy the input to avoid double evaluation
                        var getHasValue = _factory.SpecialMethod(SpecialMember.System_Nullable_T_get_HasValue).AsMember((NamedTypeSymbol)loweredInput.Type);
                        return _factory.MakeSequence(input,
                            _factory.AssignmentExpression(_factory.Local(input), loweredInput),
                            _factory.AssignmentExpression(loweredTarget, _factory.Convert(type, _factory.Call(_factory.Local(input), getValueOrDefault))),
                            _factory.Call(_factory.Local(input), getHasValue)
                            );
                    }
                    else
                    {
                        var convertedInput = _factory.Convert(type, _factory.Call(loweredInput, getValueOrDefault));
                        var assignment = _factory.AssignmentExpression(loweredTarget, convertedInput);
                        return _factory.MakeSequence(assignment, _factory.Literal(true));
                    }

                }
                else
                {
                    var convertedInput = _factory.Convert(type, loweredInput);
                    var assignment = _factory.AssignmentExpression(loweredTarget, convertedInput);
                    var objectType = _factory.SpecialType(SpecialType.System_Object);
                    return requiresNullTest
                        ? _factory.ObjectNotEqual(_factory.Convert(objectType, assignment), _factory.Null(objectType))
                        : _factory.MakeSequence(assignment, _factory.Literal(true));
                }
            }

            // a pattern match of the form "expression is Type identifier" is equivalent to
            // an invocation of one of these helpers:
            if (type.IsReferenceType)
            {
                // bool Is<T>(object e, out T t) where T : class // reference type
                // {
                //     t = e as T;
                //     return t != null;
                // }

                return _factory.ObjectNotEqual(
                    _factory.AssignmentExpression(loweredTarget, _factory.As(loweredInput, type)),
                    _factory.Null(type));
            }
            else // type parameter or value type
            {
                // bool Is<T>(this object i, ref T o)
                // {
                //     // inefficient because it performs the type test twice, and also because it boxes the input.
                //     return i is T && (o = (T)i; true);
                // }

                var i = _factory.SynthesizedLocal(loweredInput.Type, syntax); // we copy the input to avoid double evaluation

                // Because a cast involving a type parameter is not necessarily a valid conversion (or, if it is, it might not
                // be of a kind appropriate for pattern-matching), we use `object` as an intermediate type for the input expression.
                var convertedInput = _factory.Convert(type, _factory.Convert(_factory.SpecialType(SpecialType.System_Object), _factory.Local(i)));

                return _factory.MakeSequence(i,
                    _factory.LogicalAnd(
                        _factory.Is(_factory.AssignmentExpression(_factory.Local(i), loweredInput), type),
                        _factory.MakeSequence(_factory.AssignmentExpression(loweredTarget, convertedInput), _factory.Literal(true))));
            }
        }
    }
}
