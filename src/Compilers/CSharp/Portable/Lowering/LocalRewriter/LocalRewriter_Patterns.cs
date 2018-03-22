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
        /// <summary>
        /// A common base class for lowering constructs that use pattern-matching.
        /// </summary>
        private class PatternLocalRewriter
        {
            protected readonly LocalRewriter _localRewriter;
            protected readonly BoundExpression _loweredInput;
            protected readonly SyntheticBoundNodeFactory _factory;
            protected readonly DagTempAllocator _tempAllocator;
            protected readonly BoundDagTemp _inputTemp;

            public PatternLocalRewriter(LocalRewriter localRewriter, BoundExpression loweredInput)
            {
                this._loweredInput = loweredInput;
                this._localRewriter = localRewriter;
                this._factory = localRewriter._factory;
                this._factory.Syntax = _loweredInput.Syntax;
                this._tempAllocator = new DagTempAllocator(_factory);
                this._inputTemp = new BoundDagTemp(loweredInput.Syntax, loweredInput.Type, null, 0);
            }

            public void Free()
            {
                _tempAllocator.Free();
            }

            public class DagTempAllocator
            {
                private readonly SyntheticBoundNodeFactory _factory;
                private readonly PooledDictionary<BoundDagTemp, BoundExpression> _map = PooledDictionary<BoundDagTemp, BoundExpression>.GetInstance();
                private readonly ArrayBuilder<LocalSymbol> _temps = ArrayBuilder<LocalSymbol>.GetInstance();

                public DagTempAllocator(SyntheticBoundNodeFactory factory)
                {
                    this._factory = factory;
                }

                public void Free()
                {
                    _temps.Free();
                    _map.Free();
                }

                public BoundExpression GetTemp(BoundDagTemp dagTemp)
                {
                    if (!_map.TryGetValue(dagTemp, out BoundExpression result))
                    {
                        // PROTOTYPE(patterns2): Not sure what temp kind should be used for `is pattern`.
                        LocalSymbol temp = _factory.SynthesizedLocal(dagTemp.Type, syntax: _factory.Syntax, kind: SynthesizedLocalKind.SwitchCasePatternMatching);
                        result = _factory.Local(temp);
                        _map.Add(dagTemp, result);
                        _temps.Add(temp);
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

            /// <summary>
            /// Return the side-effect expression corresponding to an evaluation.
            /// </summary>
            protected BoundExpression LowerEvaluation(BoundDagEvaluation evaluation)
            {
                BoundExpression input = _tempAllocator.GetTemp(evaluation.Input);
                switch (evaluation)
                {
                    case BoundDagFieldEvaluation f:
                        {
                            FieldSymbol field = f.Field;
                            var outputTemp = new BoundDagTemp(f.Syntax, field.Type, f, 0);
                            BoundExpression output = _tempAllocator.GetTemp(outputTemp);
                            BoundExpression access = _localRewriter.MakeFieldAccess(f.Syntax, input, field, null, LookupResultKind.Viable, field.Type);
                            access.WasCompilerGenerated = true;
                            return _factory.AssignmentExpression(output, access);
                        }

                    case BoundDagPropertyEvaluation p:
                        {
                            PropertySymbol property = p.Property;
                            var outputTemp = new BoundDagTemp(p.Syntax, property.Type, p, 0);
                            BoundExpression output = _tempAllocator.GetTemp(outputTemp);
                            return _factory.AssignmentExpression(output, _factory.Property(input, property));
                        }

                    case BoundDagDeconstructEvaluation d:
                        {
                            MethodSymbol method = d.DeconstructMethod;
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
                                addArg(method.ParameterRefKinds[0], input);
                                extensionExtra = 1;
                            }
                            else
                            {
                                receiver = input;
                                extensionExtra = 0;
                            }

                            for (int i = extensionExtra; i < method.ParameterCount; i++)
                            {
                                ParameterSymbol parameter = method.Parameters[i];
                                Debug.Assert(parameter.RefKind == RefKind.Out);
                                var outputTemp = new BoundDagTemp(d.Syntax, parameter.Type, d, i - extensionExtra);
                                addArg(RefKind.Out, _tempAllocator.GetTemp(outputTemp));
                            }

                            return _factory.Call(receiver, method, refKindBuilder.ToImmutableAndFree(), argBuilder.ToImmutableAndFree());
                        }

                    case BoundDagTypeEvaluation t:
                        {
                            TypeSymbol inputType = input.Type;
                            if (inputType.IsDynamic())
                            {
                                inputType = _factory.SpecialType(SpecialType.System_Object);
                            }

                            TypeSymbol type = t.Type;
                            var outputTemp = new BoundDagTemp(t.Syntax, type, t, 0);
                            BoundExpression output = _tempAllocator.GetTemp(outputTemp);
                            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                            Conversion conversion = _factory.Compilation.Conversions.ClassifyBuiltInConversion(inputType, output.Type, ref useSiteDiagnostics);
                            _localRewriter._diagnostics.Add(t.Syntax, useSiteDiagnostics);
                            BoundExpression evaluated;
                            if (conversion.Exists)
                            {
                                if (conversion.Kind == ConversionKind.ExplicitNullable &&
                                    inputType.GetNullableUnderlyingType().Equals(output.Type, TypeCompareKind.AllIgnoreOptions) &&
                                    _localRewriter.TryGetNullableMethod(t.Syntax, inputType, SpecialMember.System_Nullable_T_GetValueOrDefault, out MethodSymbol getValueOrDefault))
                                {
                                    // As a special case, since the null test has already been done we can use Nullable<T>.GetValueOrDefault
                                    evaluated = _factory.Call(input, getValueOrDefault);
                                }
                                else
                                {
                                    evaluated = _factory.Convert(type, input, conversion);
                                }
                            }
                            else
                            {
                                evaluated = _factory.As(input, type);
                            }

                            return _factory.AssignmentExpression(output, evaluated);
                        }

                    default:
                        throw ExceptionUtilities.UnexpectedValue(evaluation);
                }
            }

            /// <summary>
            /// Return the boolean test required for the given decision. Returns `null` if the decision is trivially true.
            /// </summary>
            protected BoundExpression LowerDecision(BoundDagDecision decision)
            {
                BoundExpression input = _tempAllocator.GetTemp(decision.Input);
                switch (decision)
                {
                    case BoundNonNullDecision d:
                        // If the actual input is a constant, short-circuit this test
                        if (d.Input == _inputTemp && _loweredInput.ConstantValue != null)
                        {
                            bool decisionResult = _loweredInput.ConstantValue != ConstantValue.Null;
                            if (!decisionResult)
                            {
                                return _factory.Literal(decisionResult);
                            }
                        }
                        else
                        {
                            // PROTOTYPE(patterns2): combine null test and type test when possible for improved code
                            return _localRewriter.MakeNullCheck(d.Syntax, input, input.Type.IsNullableType() ? BinaryOperatorKind.NullableNullNotEqual : BinaryOperatorKind.NotEqual);
                        }

                        return null;

                    case BoundTypeDecision d:
                        {
                            return _factory.Is(input, d.Type);
                        }

                    case BoundNullValueDecision d:
                        if (d.Input == _inputTemp && _loweredInput.ConstantValue != null)
                        {
                            bool decisionResult = _loweredInput.ConstantValue == ConstantValue.Null;
                            if (!decisionResult)
                            {
                                return _factory.Literal(decisionResult);
                            }
                        }
                        else
                        {
                            return _localRewriter.MakeNullCheck(d.Syntax, input, input.Type.IsNullableType() ? BinaryOperatorKind.NullableNullEqual : BinaryOperatorKind.Equal);
                        }

                        return null;

                    case BoundNonNullValueDecision d:
                        // If the actual input is a constant, short-circuit this test
                        if (d.Input == _inputTemp && _loweredInput.ConstantValue != null)
                        {
                            bool decisionResult = _loweredInput.ConstantValue == d.Value;
                            if (!decisionResult)
                            {
                                return _factory.Literal(decisionResult);
                            }
                        }
                        else
                        {
                            Debug.Assert(!input.Type.IsNullableType());
                            return _localRewriter.MakeEqual(_localRewriter.MakeLiteral(d.Syntax, d.Value, input.Type), input);
                        }

                        return null;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(decision);
                }
            }
        }

        private class IsPatternExpressionLocalRewriter : PatternLocalRewriter
        {
            /// <summary>
            /// Accumulates side-effects that come before the next conjunct.
            /// </summary>
            private readonly ArrayBuilder<BoundExpression> _sideEffectBuilder;

            /// <summary>
            /// Accumulates conjuncts (conditions that must all be true) for the translation. When a conjunct is added,
            /// elements of the _sideEffectBuilder, if any, should be added as part of a sequence expression for
            /// the conjunct being added.
            /// </summary>
            private readonly ArrayBuilder<BoundExpression> _conjunctBuilder;

            public IsPatternExpressionLocalRewriter(LocalRewriter localRewriter, BoundExpression loweredInput)
                : base(localRewriter, loweredInput)
            {
                this._conjunctBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                this._sideEffectBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            }

            public new void Free()
            {
                _conjunctBuilder.Free();
                _sideEffectBuilder.Free();
                base.Free();
            }

            private void AddConjunct(BoundExpression test)
            {
                if (_sideEffectBuilder.Count != 0)
                {
                    test = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, _sideEffectBuilder.ToImmutable(), test);
                    _sideEffectBuilder.Clear();
                }

                _conjunctBuilder.Add(test);
            }

            /// <summary>
            /// Translate the single decision into _sideEffectBuilder and _conjunctBuilder.
            /// </summary>
            private void LowerOneDecision(BoundDagDecision decision)
            {
                _factory.Syntax = decision.Syntax;
                switch (decision)
                {
                    case BoundDagEvaluation e:
                        {
                            var sideEffect = LowerEvaluation(e);
                            _sideEffectBuilder.Add(sideEffect);
                            return;
                        }
                    case var d:
                        {
                            var test = LowerDecision(d);
                            if (test != null) // PROTOTYPE(patterns2): could handle constant expressions more efficiently.
                            {
                                AddConjunct(test);
                            }

                            return;
                        }
                }
            }

            public BoundExpression LowerIsPattern(BoundPattern pattern, CSharpCompilation compilation, DiagnosticBag diagnostics)
            {
                LabelSymbol failureLabel = new GeneratedLabelSymbol("failure");
                BoundDecisionDag dag = DecisionDagBuilder.CreateDecisionDag(compilation, pattern.Syntax, this._loweredInput, pattern, failureLabel, diagnostics, out LabelSymbol successLabel);
                if (_loweredInput.ConstantValue != null)
                {
                    dag = dag.SimplifyDecisionDagForConstantInput(_loweredInput, _localRewriter._compilation.Conversions, diagnostics);
                }

                // first, copy the input expression into the input temp
                if (pattern.Kind != BoundKind.RecursivePattern &&
                    (_loweredInput.Kind == BoundKind.Local || _loweredInput.Kind == BoundKind.Parameter || _loweredInput.ConstantValue != null))
                {
                    // Since non-recursive patterns cannot have side-effects on locals, we reuse an existing local
                    // if present. A recursive pattern, on the other hand, may mutate a local through a captured lambda
                    // when a `Deconstruct` method is invoked.
                    _tempAllocator.AssignTemp(_inputTemp, _loweredInput);
                }
                else
                {
                    // Even if subsequently unused (e.g. `GetValue() is _`), we assign to a temp to evaluate the side-effect
                    _sideEffectBuilder.Add(_factory.AssignmentExpression(_tempAllocator.GetTemp(_inputTemp), _loweredInput));
                }

                // We follow the "good" path in the decision dag. We depend on it being nicely linear in structure.
                while (dag.Kind != BoundKind.Decision && dag.Kind != BoundKind.WhenClause)
                {
                    switch (dag)
                    {
                        case BoundEvaluationPoint e:
                            LowerOneDecision(e.Evaluation);
                            dag = e.Next;
                            break;
                        case BoundDecisionPoint d:
                            LowerOneDecision(d.Decision);
                            Debug.Assert(d.WhenFalse is BoundDecision x && x.Label == failureLabel);
                            dag = d.WhenTrue;
                            break;
                    }
                }

                // When we get to "the end", we see if it is a success node.
                switch (dag)
                {
                    case BoundDecision d:
                        {
                            if (d.Label == failureLabel)
                            {
                                // It is not clear that this can occur given the dag "optimizations" we performed earlier.
                                AddConjunct(_factory.Literal(false));
                            }
                            else
                            {
                                Debug.Assert(d.Label == successLabel);
                            }
                        }

                        break;

                    case BoundWhenClause w:
                        {
                            Debug.Assert(w.WhenExpression == null);
                            Debug.Assert(w.WhenTrue is BoundDecision d && d.Label == successLabel);
                            foreach ((BoundExpression left, BoundDagTemp right) in w.Bindings)
                            {
                                _sideEffectBuilder.Add(_factory.AssignmentExpression(left, _tempAllocator.GetTemp(right)));
                            }
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(dag.Kind);
                }

                if (_sideEffectBuilder.Count > 0 || _conjunctBuilder.Count == 0)
                {
                    AddConjunct(_factory.Literal(true));
                }

                Debug.Assert(_sideEffectBuilder.Count == 0);
                BoundExpression result = null;
                foreach (BoundExpression conjunct in _conjunctBuilder)
                {
                    result = (result == null) ? conjunct : _factory.LogicalAnd(result, conjunct);
                }

                _conjunctBuilder.Clear();
                Debug.Assert(result != null);
                var allTemps = _tempAllocator.AllTemps();
                if (allTemps.Length > 0)
                {
                    result = _factory.Sequence(allTemps, ImmutableArray<BoundExpression>.Empty, result);
                }

                return result;
            }
        }

        private BoundExpression MakeEqual(BoundExpression loweredLiteral, BoundExpression input)
        {
            Debug.Assert(loweredLiteral.Type == input.Type);

            if (loweredLiteral.Type.SpecialType == SpecialType.System_Double && double.IsNaN(loweredLiteral.ConstantValue.DoubleValue))
            {
                // produce double.IsNaN(input)
                return _factory.StaticCall(SpecialMember.System_Double__IsNaN, input);
            }
            else if (loweredLiteral.Type.SpecialType == SpecialType.System_Single && float.IsNaN(loweredLiteral.ConstantValue.SingleValue))
            {
                // produce float.IsNaN(input)
                return _factory.StaticCall(SpecialMember.System_Single__IsNaN, input);
            }

            NamedTypeSymbol booleanType = _factory.SpecialType(SpecialType.System_Boolean);
            NamedTypeSymbol intType = _factory.SpecialType(SpecialType.System_Int32);
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
                    NamedTypeSymbol systemObject = _factory.SpecialType(SpecialType.System_Object);
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
            BoundExpression loweredExpression = VisitExpression(node.Expression);
            BoundPattern loweredPattern = LowerPattern(node.Pattern);
            var isPatternRewriter = new IsPatternExpressionLocalRewriter(this, loweredExpression);
            BoundExpression result = isPatternRewriter.LowerIsPattern(loweredPattern, this._compilation, this._diagnostics);
            isPatternRewriter.Free();
            return result;
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
    }
}
