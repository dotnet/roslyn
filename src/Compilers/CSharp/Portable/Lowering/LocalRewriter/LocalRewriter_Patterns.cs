// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        private struct IsPatternExpressionLocalRewriter : IDisposable
        {
            private readonly LocalRewriter _localRewriter;
            private readonly SyntheticBoundNodeFactory _factory;

            private readonly ArrayBuilder<BoundExpression> _conjunctBuilder;
            private readonly ArrayBuilder<BoundExpression> _sideEffectBuilder;
            private readonly DagTempAllocator _tempAllocator;

            private readonly BoundExpression _loweredInput;
            private readonly BoundDagTemp _inputTemp;

            public IsPatternExpressionLocalRewriter(LocalRewriter localRewriter, BoundExpression loweredInput)
            {
                this._localRewriter = localRewriter;
                this._factory = localRewriter._factory;
                this._tempAllocator = new DagTempAllocator(localRewriter._factory);
                this._loweredInput = loweredInput;
                this._inputTemp = new BoundDagTemp(loweredInput.Syntax, loweredInput.Type, null, 0);
                this._conjunctBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                this._sideEffectBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            }

            public void Dispose()
            {
                _conjunctBuilder.Free();
                _sideEffectBuilder.Free();
                _tempAllocator.Dispose();
            }

            public class DagTempAllocator : IDisposable
            {
                private readonly SyntheticBoundNodeFactory _factory;
                private readonly PooledDictionary<BoundDagTemp, BoundExpression> _map = PooledDictionary<BoundDagTemp, BoundExpression>.GetInstance();
                private readonly ArrayBuilder<LocalSymbol> _temps = ArrayBuilder<LocalSymbol>.GetInstance();

                public DagTempAllocator(SyntheticBoundNodeFactory factory)
                {
                    this._factory = factory;
                }

                public void Dispose()
                {
                    _temps.Free();
                    _map.Free();
                }

                public BoundExpression GetTemp(BoundDagTemp dagTemp)
                {
                    if (!_map.TryGetValue(dagTemp, out var result))
                    {
                        // PROTOTYPE(patterns2): Not sure what temp kind should be used for `is pattern`.
                        var temp = _factory.SynthesizedLocal(dagTemp.Type, syntax: _factory.Syntax, kind: SynthesizedLocalKind.SwitchCasePatternMatching);
                        _map.Add(dagTemp, _factory.Local(temp));
                        _temps.Add(temp);
                        result = _factory.Local(temp);
                    }

                    return result;
                }

                public ImmutableArray<LocalSymbol> AllTemps()
                {
                    return _temps.ToImmutableArray();
                }

                public void AssignTemp(BoundDagTemp dagTemp, BoundExpression value)
                {
                    _map.Add(dagTemp, value);
                }
            }

            private bool LowerDecision(BoundDagDecision decision)
            {
                var oldSyntax = _factory.Syntax;
                _factory.Syntax = decision.Syntax;
                var result = LowerDecisionCore(decision);
                _factory.Syntax = oldSyntax;
                return result;
            }

            /// <summary>
            /// Translate the single decision into _sideEffectBuilder and _conjunctBuilder.
            /// Returns true if no further decisions need to be translated (e.g. when producing a `false` conjunct).
            /// </summary>
            private bool LowerDecisionCore(BoundDagDecision decision)
            {
                void addConjunct(ref IsPatternExpressionLocalRewriter self, BoundExpression expression)
                {
                    // PROTOTYPE(patterns2): could handle constant expressions more efficiently.
                    if (self._sideEffectBuilder.Count != 0)
                    {
                        expression = self._factory.Sequence(ImmutableArray<LocalSymbol>.Empty, self._sideEffectBuilder.ToImmutable(), expression);
                        self._sideEffectBuilder.Clear();
                    }

                    self._conjunctBuilder.Add(expression);
                }

                var input = _tempAllocator.GetTemp(decision.Input);
                switch (decision)
                {
                    case BoundNonNullDecision d:
                        // If the actual input is a constant, short-circuit this test
                        if (d.Input == _inputTemp && _loweredInput.ConstantValue != null)
                        {
                            var decisionResult = _loweredInput.ConstantValue != ConstantValue.Null;
                            if (!decisionResult)
                            {
                                _conjunctBuilder.Add(_factory.Literal(decisionResult));
                                return true;
                            }
                        }
                        else
                        {
                            // PROTOTYPE(patterns2): combine null test and type test when possible for improved code
                            addConjunct(ref this, _localRewriter.MakeNullCheck(d.Syntax, input, input.Type.IsNullableType() ? BinaryOperatorKind.NullableNullNotEqual : BinaryOperatorKind.NotEqual));
                        }
                        break;
                    case BoundTypeDecision d:
                        {
                            addConjunct(ref this, _factory.Is(input, d.Type));
                        }
                        break;
                    case BoundValueDecision d:
                        // If the actual input is a constant, short-circuit this test
                        if (d.Input == _inputTemp && _loweredInput.ConstantValue != null)
                        {
                            var decisionResult = _loweredInput.ConstantValue == d.Value;
                            if (!decisionResult)
                            {
                                _conjunctBuilder.Add(_factory.Literal(decisionResult));
                                return true;
                            }
                        }
                        else if (d.Value == ConstantValue.Null)
                        {
                            addConjunct(ref this, _localRewriter.MakeNullCheck(d.Syntax, input, input.Type.IsNullableType() ? BinaryOperatorKind.NullableNullEqual : BinaryOperatorKind.Equal));
                        }
                        else
                        {
                            var systemObject = _factory.SpecialType(SpecialType.System_Object);
                            addConjunct(ref this, _localRewriter.MakeEqual(_factory.Literal(d.Value, input.Type), input));
                        }
                        break;
                    case BoundDagFieldEvaluation f:
                        {
                            var field = f.Field;
                            field = field.TupleUnderlyingField ?? field;
                            var outputTemp = new BoundDagTemp(f.Syntax, field.Type, f, 0);
                            var output = _tempAllocator.GetTemp(outputTemp);
                            _sideEffectBuilder.Add(_factory.AssignmentExpression(output, _factory.Field(input, field)));
                        }
                        break;
                    case BoundDagPropertyEvaluation p:
                        {
                            var property = p.Property;
                            property = property.TupleUnderlyingProperty ?? property;
                            var outputTemp = new BoundDagTemp(p.Syntax, property.Type, p, 0);
                            var output = _tempAllocator.GetTemp(outputTemp);
                            _sideEffectBuilder.Add(_factory.AssignmentExpression(output, _factory.Property(input, property)));
                        }
                        break;
                    case BoundDagDeconstructEvaluation d:
                        {
                            var method = d.DeconstructMethod;
                            var refKindBuilder = ArrayBuilder<RefKind>.GetInstance();
                            var argBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                            BoundExpression receiver;
                            void addArg(RefKind refKind, BoundExpression expression)
                            {
                                refKindBuilder.Add(refKind);
                                argBuilder.Add(expression);
                            }
                            Debug.Assert(method.Name == "Deconstruct");
                            int extensionExtra;
                            if (method.IsStatic)
                            {
                                Debug.Assert(method.IsExtensionMethod);
                                receiver = _factory.Type(method.ContainingType);
                                addArg(RefKind.None, input);
                                extensionExtra = 1;
                            }
                            else
                            {
                                receiver = input;
                                extensionExtra = 0;
                            }
                            for (int i = extensionExtra; i < method.ParameterCount; i++)
                            {
                                var parameter = method.Parameters[i];
                                Debug.Assert(parameter.RefKind == RefKind.Out);
                                var outputTemp = new BoundDagTemp(d.Syntax, parameter.Type, d, i - extensionExtra);
                                addArg(RefKind.Out, _tempAllocator.GetTemp(outputTemp));
                            }
                            _sideEffectBuilder.Add(_factory.Call(receiver, method, refKindBuilder.ToImmutableAndFree(), argBuilder.ToImmutableAndFree()));
                        }
                        break;
                    case BoundDagTypeEvaluation t:
                        {
                            var inputType = input.Type;
                            if (inputType.IsDynamic())
                            {
                                inputType = _factory.SpecialType(SpecialType.System_Object);
                            }

                            var type = t.Type;
                            var outputTemp = new BoundDagTemp(t.Syntax, type, t, 0);
                            var output = _tempAllocator.GetTemp(outputTemp);
                            var conversion = _factory.Compilation.ClassifyConversion(inputType, output.Type);
                            if (conversion.Exists)
                            {
                                _sideEffectBuilder.Add(_factory.AssignmentExpression(output, _factory.Convert(type, input, conversion)));
                            }
                            else
                            {
                                _sideEffectBuilder.Add(_factory.AssignmentExpression(output, _factory.As(input, type)));
                            }
                        }
                        break;
                }

                return false;
            }

            public BoundExpression LowerIsPattern(BoundPattern pattern, CSharpCompilation compilation)
            {
                var decisionBuilder = new DecisionDagBuilder(compilation);
                var inputTemp = decisionBuilder.TranslatePattern(_loweredInput, pattern, out var decisions, out var bindings);
                Debug.Assert(inputTemp == _inputTemp);

                // first, copy the input expression into the input temp
                if (pattern.Kind != BoundKind.RecursivePattern &&
                    (_loweredInput.Kind == BoundKind.Local || _loweredInput.Kind == BoundKind.Parameter || _loweredInput.ConstantValue != null))
                {
                    // Since non-recursive patterns cannot have side-effects on locals, we reuse an existing local
                    // if present. A recursive pattern, on the other hand, may mutate a local through a captured lambda
                    // when a `Deconstruct` method is invoked.
                    _tempAllocator.AssignTemp(inputTemp, _loweredInput);
                }
                else
                {
                    // Even if subsequently unused (e.g. `GetValue() is _`), we assign to a temp to evaluate the side-effect
                    _sideEffectBuilder.Add(_factory.AssignmentExpression(_tempAllocator.GetTemp(inputTemp), _loweredInput));
                }

                // then process all of the individual decisions
                foreach (var decision in decisions)
                {
                    if (LowerDecision(decision))
                    {
                        break;
                    }
                }

                if (_sideEffectBuilder.Count != 0)
                {
                    _conjunctBuilder.Add(_factory.Sequence(ImmutableArray<LocalSymbol>.Empty, _sideEffectBuilder.ToImmutable(), _factory.Literal(true)));
                    _sideEffectBuilder.Clear();
                }

                BoundExpression result = null;
                foreach (var conjunct in _conjunctBuilder)
                {
                    result = (result == null) ? conjunct : _factory.LogicalAnd(result, conjunct);
                }

                var bindingsBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                foreach (var (left, right) in bindings)
                {
                    bindingsBuilder.Add(_factory.AssignmentExpression(left, _tempAllocator.GetTemp(right)));
                }

                if (bindingsBuilder.Count > 0)
                {
                    var c = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, bindingsBuilder.ToImmutableAndFree(), _factory.Literal(true));
                    result = (result == null) ? c : (BoundExpression)_factory.LogicalAnd(result, c);
                }
                else if (result == null)
                {
                    result = _factory.Literal(true);
                }

                return _factory.Sequence(_tempAllocator.AllTemps(), ImmutableArray<BoundExpression>.Empty, result);
            }
        }

        //private BoundExpression MakeTypeTestAndAssignment(BoundExpression loweredTarget, BoundExpression loweredInput, TypeSymbol type)
        //{
        //    Debug.Assert(type == loweredTarget.Type);

        //    // If the match is impossible, we simply evaluate the input and yield false.
        //    var matchConstantValue = MatchConstantValue(loweredInput, type, false);
        //    if (matchConstantValue == false)
        //    {
        //        return _factory.MakeSequence(loweredInput, _factory.Literal(false));
        //    }

        //    // It is possible that the input value is already of the correct type, in which case the pattern
        //    // is irrefutable, and we can just do the assignment and return true (or perform the null test).
        //    if (matchConstantValue == true)
        //    {
        //        BoundExpression convertedInput;
        //        if (loweredInput.Type.IsNullableType())
        //        {
        //            var getValueOrDefault = _factory.SpecialMethod(SpecialMember.System_Nullable_T_GetValueOrDefault).AsMember((NamedTypeSymbol)loweredInput.Type);
        //            convertedInput = _factory.Convert(type, _factory.Call(loweredInput, getValueOrDefault));
        //        }
        //        else
        //        {
        //            convertedInput = _factory.Convert(type, loweredInput);
        //        }
        //        var assignment = _factory.AssignmentExpression(loweredTarget, convertedInput);
        //        return _factory.MakeSequence(assignment, _factory.Literal(true));
        //    }

        //    // a pattern match of the form "expression is Type identifier" is equivalent to
        //    // an invocation of one of these helpers:
        //    if (type.IsReferenceType)
        //    {
        //        // bool Is<T>(object e, out T t) where T : class // reference type
        //        // {
        //        //     t = e as T;
        //        //     return t != null;
        //        // }

        //        return _factory.ObjectNotEqual(
        //            _factory.AssignmentExpression(loweredTarget, _factory.As(loweredInput, type)),
        //            _factory.Null(type));
        //    }
        //    else // type parameter or value type
        //    {
        //        // bool Is<T>(this object i, out T o)
        //        // {
        //        //     // inefficient because it performs the type test twice, and also because it boxes the input.
        //        //     bool s;
        //        //     o = (s = i is T) ? (T)i : default(T);
        //        //     return s;
        //        // }

        //        // Because a cast involving a type parameter is not necessarily a valid conversion (or, if it is, it might not
        //        // be of a kind appropriate for pattern-matching), we use `object` as an intermediate type for the input expression.
        //        var objectType = _factory.SpecialType(SpecialType.System_Object);
        //        var s = _factory.SynthesizedLocal(_factory.SpecialType(SpecialType.System_Boolean), loweredTarget.Syntax);
        //        var i = _factory.SynthesizedLocal(objectType, loweredTarget.Syntax); // we copy the input to avoid double evaluation
        //        return _factory.Sequence(
        //            ImmutableArray.Create(s, i),
        //            ImmutableArray.Create<BoundExpression>(
        //                _factory.AssignmentExpression(_factory.Local(i), _factory.Convert(objectType, loweredInput)),
        //                _factory.AssignmentExpression(loweredTarget, _factory.Conditional(
        //                    _factory.AssignmentExpression(_factory.Local(s), _factory.Is(_factory.Local(i), type)),
        //                    _factory.Convert(type, _factory.Local(i)),
        //                    _factory.Default(type), type))
        //                ),
        //            _factory.Local(s)
        //            );
        //    }
        //}

        private BoundExpression MakeEqual(BoundExpression loweredLiteral, BoundExpression input)
        {
            if (loweredLiteral.Type.SpecialType == SpecialType.System_Double && Double.IsNaN(loweredLiteral.ConstantValue.DoubleValue) ||
                loweredLiteral.Type.SpecialType == SpecialType.System_Single && Single.IsNaN(loweredLiteral.ConstantValue.SingleValue))
            {
                // NaN must be treated specially, as operator== and .Equals() disagree.
                Debug.Assert(loweredLiteral.Type == input.Type);
                var condition = _factory.InstanceCall(loweredLiteral, "Equals", input);
                if (!condition.HasErrors && condition.Type.SpecialType != SpecialType.System_Boolean)
                {
                    // Diagnose some kinds of broken core APIs
                    var call = (BoundCall)condition;
                    // '{1} {0}' has the wrong return type
                    _factory.Diagnostics.Add(ErrorCode.ERR_BadRetType, loweredLiteral.Syntax.GetLocation(), call.Method, call.Type);
                }

                return condition;
            }

            var booleanType = _factory.SpecialType(SpecialType.System_Boolean);
            var intType = _factory.SpecialType(SpecialType.System_Int32);
            switch (loweredLiteral.Type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.BoolEqual, loweredLiteral, input, booleanType, method: null);
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Int16:
                case SpecialType.System_SByte:
                case SpecialType.System_UInt16:
                    // PROTOTYPE(patterns2): need to check that this produces efficient code
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.IntEqual, _factory.Convert(intType, loweredLiteral), _factory.Convert(intType, input), booleanType, method: null);
                case SpecialType.System_Decimal:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.DecimalEqual, loweredLiteral, input, booleanType, method: null);
                case SpecialType.System_Double:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.DoubleEqual, loweredLiteral, input, booleanType, method: null);
                case SpecialType.System_Int32:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.IntEqual, loweredLiteral, input, booleanType, method: null);
                case SpecialType.System_Int64:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.LongEqual, loweredLiteral, input, booleanType, method: null);
                case SpecialType.System_Single:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.FloatEqual, loweredLiteral, input, booleanType, method: null);
                case SpecialType.System_String:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.StringEqual, loweredLiteral, input, booleanType, method: null);
                case SpecialType.System_UInt32:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.UIntEqual, loweredLiteral, input, booleanType, method: null);
                case SpecialType.System_UInt64:
                    return MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.ULongEqual, loweredLiteral, input, booleanType, method: null);
                default:
                    // PROTOTYPE(patterns2): need more efficient code for enum test, e.g. `color is Color.Red`
                    // This is the (correct but inefficient) fallback for any type that isn't yet implemented (e.g. enums)
                    var systemObject = _factory.SpecialType(SpecialType.System_Object);
                    return _factory.StaticCall(
                        systemObject,
                        "Equals",
                        _factory.Convert(systemObject, loweredLiteral),
                        _factory.Convert(systemObject, input)
                        );
            }
        }

        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            var loweredExpression = VisitExpression(node.Expression);
            var loweredPattern = LowerPattern(node.Pattern);
            using (var x = new IsPatternExpressionLocalRewriter(this, loweredExpression))
            {
                return x.LowerIsPattern(loweredPattern, this._compilation);
            }
        }

        //public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        //{
        //    var loweredExpression = VisitExpression(node.Expression);
        //    var loweredPattern = LowerPattern(node.Pattern);
        //    return MakeIsPattern(loweredPattern, loweredExpression);
        //}

        //// Input must be used no more than once in the result. If it is needed repeatedly store its value in a temp and use the temp.
        //BoundExpression MakeIsPattern(BoundPattern loweredPattern, BoundExpression loweredInput)
        //{
        //    var syntax = _factory.Syntax = loweredPattern.Syntax;
        //    switch (loweredPattern.Kind)
        //    {
        //        case BoundKind.DeclarationPattern:
        //            {
        //                var declPattern = (BoundDeclarationPattern)loweredPattern;
        //                return MakeIsDeclarationPattern(declPattern, loweredInput);
        //            }

        //        case BoundKind.WildcardPattern:
        //            return _factory.Literal(true);

        //        case BoundKind.ConstantPattern:
        //            {
        //                var constantPattern = (BoundConstantPattern)loweredPattern;
        //                return MakeIsConstantPattern(constantPattern, loweredInput);
        //            }

        //        default:
        //            throw ExceptionUtilities.UnexpectedValue(loweredPattern.Kind);
        //    }
        //}

        BoundPattern LowerPattern(BoundPattern pattern)
        {
            switch (pattern.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        var declPattern = (BoundDeclarationPattern)pattern;
                        return declPattern.Update(declPattern.Variable, VisitExpression(declPattern.VariableAccess), declPattern.DeclaredType, declPattern.IsVar);
                    }
                case BoundKind.RecursivePattern:
                    {
                        var recur = (BoundRecursivePattern)pattern;
                        return recur.Update(
                            declaredType: recur.DeclaredType,
                            inputType: recur.InputType,
                            deconstructMethodOpt: recur.DeconstructMethodOpt,
                            deconstruction: recur.Deconstruction.IsDefault ? recur.Deconstruction : recur.Deconstruction.SelectAsArray(p => LowerPattern(p)),
                            propertiesOpt: recur.PropertiesOpt.IsDefault ? recur.PropertiesOpt : recur.PropertiesOpt.SelectAsArray(p => (p.symbol, LowerPattern(p.pattern))),
                            variable: recur.Variable,
                            variableAccess: VisitExpression(recur.VariableAccess));
                    }
                case BoundKind.ConstantPattern:
                    {
                        var constantPattern = (BoundConstantPattern)pattern;
                        return constantPattern.Update(VisitExpression(constantPattern.Value), constantPattern.ConstantValue);
                    }
                case BoundKind.DiscardPattern:
                    {
                        return pattern;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }

        //private BoundExpression MakeIsConstantPattern(BoundConstantPattern loweredPattern, BoundExpression loweredInput)
        //{
        //    return CompareWithConstant(loweredInput, loweredPattern.Value);
        //}

        //private BoundExpression MakeIsDeclarationPattern(BoundDeclarationPattern loweredPattern, BoundExpression loweredInput)
        //{
        //    Debug.Assert(((object)loweredPattern.Variable == null && loweredPattern.VariableAccess.Kind == BoundKind.DiscardExpression) ||
        //                 loweredPattern.Variable.GetTypeOrReturnType() == loweredPattern.DeclaredType.Type);

        //    if (loweredPattern.IsVar)
        //    {
        //        var result = _factory.Literal(true);

        //        if (loweredPattern.VariableAccess.Kind == BoundKind.DiscardExpression)
        //        {
        //            return result;
        //        }

        //        Debug.Assert((object)loweredPattern.Variable != null && loweredInput.Type.Equals(loweredPattern.Variable.GetTypeOrReturnType(), TypeCompareKind.AllIgnoreOptions));

        //        var assignment = _factory.AssignmentExpression(loweredPattern.VariableAccess, loweredInput);
        //        return _factory.MakeSequence(assignment, result);
        //    }

        //    if (loweredPattern.VariableAccess.Kind == BoundKind.DiscardExpression)
        //    {
        //        LocalSymbol temp;
        //        BoundLocal discard = _factory.MakeTempForDiscard((BoundDiscardExpression)loweredPattern.VariableAccess, out temp);

        //        return _factory.Sequence(ImmutableArray.Create(temp),
        //                 sideEffects: ImmutableArray<BoundExpression>.Empty,
        //                 result: MakeIsDeclarationPattern(loweredPattern.Syntax, loweredInput, discard, requiresNullTest: loweredInput.Type.CanContainNull()));
        //    }

        //    return MakeIsDeclarationPattern(loweredPattern.Syntax, loweredInput, loweredPattern.VariableAccess, requiresNullTest: loweredInput.Type.CanContainNull());
        //}

        ///// <summary>
        ///// Is the test, produced as a result of a pattern-matching operation, always true?
        ///// Knowing that enables us to construct slightly more efficient code.
        ///// </summary>
        //private bool IsIrrefutablePatternTest(BoundExpression test)
        //{
        //    while (true)
        //    {
        //        switch (test.Kind)
        //        {
        //            case BoundKind.Literal:
        //                {
        //                    var value = ((BoundLiteral)test).ConstantValue;
        //                    return value.IsBoolean && value.BooleanValue;
        //                }
        //            case BoundKind.Sequence:
        //                test = ((BoundSequence)test).Value;
        //                continue;
        //            default:
        //                return false;
        //        }
        //    }
        //}

        //private BoundExpression CompareWithConstant(BoundExpression input, BoundExpression boundConstant)
        //{
        //    var systemObject = _factory.SpecialType(SpecialType.System_Object);
        //    if (boundConstant.ConstantValue == ConstantValue.Null)
        //    {
        //        if (input.Type.IsNonNullableValueType())
        //        {
        //            var systemBoolean = _factory.SpecialType(SpecialType.System_Boolean);
        //            return RewriteNullableNullEquality(
        //                syntax: _factory.Syntax,
        //                kind: BinaryOperatorKind.NullableNullEqual,
        //                loweredLeft: input,
        //                loweredRight: boundConstant,
        //                returnType: systemBoolean);
        //        }
        //        else
        //        {
        //            return _factory.ObjectEqual(_factory.Convert(systemObject, input), boundConstant);
        //        }
        //    }
        //    else if (input.Type.IsNullableType() && boundConstant.NullableNeverHasValue())
        //    {
        //        return _factory.Not(MakeNullableHasValue(_factory.Syntax, input));
        //    }
        //    else
        //    {
        //        return _factory.StaticCall(
        //            systemObject,
        //            "Equals",
        //            _factory.Convert(systemObject, boundConstant),
        //            _factory.Convert(systemObject, input)
        //            );
        //    }
        //}
    }
}
