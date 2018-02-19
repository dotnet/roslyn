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

            protected void LowerDecision(BoundDagDecision decision, out BoundExpression sideEffect, out BoundExpression test)
            {
                sideEffect = null;
                test = null;
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
                                test = _factory.Literal(decisionResult);
                            }
                        }
                        else
                        {
                            // PROTOTYPE(patterns2): combine null test and type test when possible for improved code
                            test = _localRewriter.MakeNullCheck(d.Syntax, input, input.Type.IsNullableType() ? BinaryOperatorKind.NullableNullNotEqual : BinaryOperatorKind.NotEqual);
                        }
                        return;
                    case BoundTypeDecision d:
                        {
                            test = _factory.Is(input, d.Type);
                        }
                        return;
                    case BoundValueDecision d:
                        // If the actual input is a constant, short-circuit this test
                        if (d.Input == _inputTemp && _loweredInput.ConstantValue != null)
                        {
                            bool decisionResult = _loweredInput.ConstantValue == d.Value;
                            if (!decisionResult)
                            {
                                test = _factory.Literal(decisionResult);
                            }
                        }
                        else if (d.Value == ConstantValue.Null)
                        {
                            test = _localRewriter.MakeNullCheck(d.Syntax, input, input.Type.IsNullableType() ? BinaryOperatorKind.NullableNullEqual : BinaryOperatorKind.Equal);
                        }
                        else
                        {
                            Debug.Assert(!input.Type.IsNullableType());
                            test = _localRewriter.MakeEqual(_factory.Literal(d.Value, input.Type), input);
                        }
                        return;
                    case BoundDagFieldEvaluation f:
                        {
                            // PROTOTYPE(patterns2): I believe simply using TupleUnderlyingField is not sufficient for correct handling of long tuples.
                            // Instead, we should probably use MakeFieldAccess helper rather than _factory.Field below. Please add a test that uses Item9 field, for example.
                            FieldSymbol field = f.Field;
                            field = field.TupleUnderlyingField ?? field;
                            var outputTemp = new BoundDagTemp(f.Syntax, field.Type, f, 0);
                            BoundExpression output = _tempAllocator.GetTemp(outputTemp);
                            sideEffect = _factory.AssignmentExpression(output, _factory.Field(input, field));
                        }
                        return;
                    case BoundDagPropertyEvaluation p:
                        {
                            PropertySymbol property = p.Property;
                            var outputTemp = new BoundDagTemp(p.Syntax, property.Type, p, 0);
                            BoundExpression output = _tempAllocator.GetTemp(outputTemp);
                            sideEffect = _factory.AssignmentExpression(output, _factory.Property(input, property));
                        }
                        return;
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
                            sideEffect = _factory.Call(receiver, method, refKindBuilder.ToImmutableAndFree(), argBuilder.ToImmutableAndFree());
                        }
                        return;
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
                            Conversion conversion = _factory.Compilation.ClassifyConversion(inputType, output.Type);
                            if (conversion.Exists)
                            {
                                sideEffect = _factory.AssignmentExpression(output, _factory.Convert(type, input, conversion));
                            }
                            else
                            {
                                sideEffect = _factory.AssignmentExpression(output, _factory.As(input, type));
                            }
                        }
                        return;
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

            private void LowerDecision(BoundDagDecision decision)
            {
                SyntaxNode oldSyntax = _factory.Syntax;
                _factory.Syntax = decision.Syntax;
                LowerDecisionCore(decision);
                _factory.Syntax = oldSyntax;
            }

            /// <summary>
            /// Translate the single decision into _sideEffectBuilder and _conjunctBuilder.
            /// </summary>
            private void LowerDecisionCore(BoundDagDecision decision)
            {
                LowerDecision(decision, out BoundExpression sideEffect, out BoundExpression test);
                if (sideEffect != null)
                {
                    _sideEffectBuilder.Add(sideEffect);
                }
                if (test != null)
                {
                    // PROTOTYPE(patterns2): could handle constant expressions more efficiently.
                    if (_sideEffectBuilder.Count != 0)
                    {
                        test = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, _sideEffectBuilder.ToImmutable(), test);
                        _sideEffectBuilder.Clear();
                    }

                    _conjunctBuilder.Add(test);
                }
            }

            public BoundExpression LowerIsPattern(BoundPattern pattern, CSharpCompilation compilation)
            {
                var decisionBuilder = new DecisionDagBuilder(compilation);
                BoundDagTemp inputTemp = decisionBuilder.TranslatePattern(_loweredInput, pattern, out ImmutableArray<BoundDagDecision> decisions, out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings);
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
                foreach (BoundDagDecision decision in decisions)
                {
                    LowerDecision(decision);
                }

                if (_sideEffectBuilder.Count != 0)
                {
                    _conjunctBuilder.Add(_factory.Sequence(ImmutableArray<LocalSymbol>.Empty, _sideEffectBuilder.ToImmutable(), _factory.Literal(true)));
                    _sideEffectBuilder.Clear();
                }

                BoundExpression result = null;
                foreach (BoundExpression conjunct in _conjunctBuilder)
                {
                    result = (result == null) ? conjunct : _factory.LogicalAnd(result, conjunct);
                }

                var bindingsBuilder = ArrayBuilder<BoundExpression>.GetInstance(bindings.Length);
                foreach ((BoundExpression left, BoundDagTemp right) in bindings)
                {
                    bindingsBuilder.Add(_factory.AssignmentExpression(left, _tempAllocator.GetTemp(right)));
                }

                var bindingAssignments = bindingsBuilder.ToImmutableAndFree();
                if (bindingAssignments.Length > 0)
                {
                    BoundSequence c = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, bindingAssignments, _factory.Literal(true));
                    result = (result == null) ? c : (BoundExpression)_factory.LogicalAnd(result, c);
                }
                else if (result == null)
                {
                    result = _factory.Literal(true);
                }

                return _factory.Sequence(_tempAllocator.AllTemps(), ImmutableArray<BoundExpression>.Empty, result);
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
            BoundExpression result = isPatternRewriter.LowerIsPattern(loweredPattern, this._compilation);
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
