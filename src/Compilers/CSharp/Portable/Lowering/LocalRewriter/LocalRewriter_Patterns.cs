// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        /// A common base class for lowering constructs that use pattern-matching.
        /// </summary>
        private abstract class PatternLocalRewriter
        {
            protected readonly LocalRewriter _localRewriter;
            protected readonly SyntheticBoundNodeFactory _factory;
            protected readonly DagTempAllocator _tempAllocator;

            public PatternLocalRewriter(SyntaxNode node, LocalRewriter localRewriter)
            {
                _localRewriter = localRewriter;
                _factory = localRewriter._factory;
                _tempAllocator = new DagTempAllocator(_factory, node, IsSwitchStatement);
            }

            /// <summary>
            /// True if this is a rewriter for a switch statement. This affects 
            /// - sequence points
            ///   When clause gets a sequence point in a switch statement, but not in a switch expression.
            /// - synthesized local variable kind
            ///   The temp variables must be long lived in a switch statement since their lifetime spans across sequence points.
            /// </summary>
            protected abstract bool IsSwitchStatement { get; }

            public void Free()
            {
                _tempAllocator.Free();
            }

            public sealed class DagTempAllocator
            {
                private readonly SyntheticBoundNodeFactory _factory;
                private readonly PooledDictionary<BoundDagTemp, BoundExpression> _map = PooledDictionary<BoundDagTemp, BoundExpression>.GetInstance();
                private readonly ArrayBuilder<LocalSymbol> _temps = ArrayBuilder<LocalSymbol>.GetInstance();
                private readonly SyntaxNode _node;

                private readonly bool _isSwitchStatement;

                public DagTempAllocator(SyntheticBoundNodeFactory factory, SyntaxNode node, bool isSwitchStatement)
                {
                    _factory = factory;
                    _node = node;
                    _isSwitchStatement = isSwitchStatement;
                }

                public void Free()
                {
                    _temps.Free();
                    _map.Free();
                }

#if DEBUG
                public string Dump()
                {
                    var poolElement = PooledStringBuilder.GetInstance();
                    var builder = poolElement.Builder;
                    foreach (var kv in _map)
                    {
                        builder.Append("Key: ");
                        builder.AppendLine(kv.Key.Dump());
                        builder.Append("Value: ");
                        builder.AppendLine(kv.Value.Dump());
                    }

                    var result = builder.ToString();
                    poolElement.Free();
                    return result;
                }
#endif

                public BoundExpression GetTemp(BoundDagTemp dagTemp)
                {
                    if (!_map.TryGetValue(dagTemp, out BoundExpression result))
                    {
                        var kind = _isSwitchStatement ? SynthesizedLocalKind.SwitchCasePatternMatching : SynthesizedLocalKind.LoweringTemp;
                        LocalSymbol temp = _factory.SynthesizedLocal(dagTemp.Type, syntax: _node, kind: kind);
                        result = _factory.Local(temp);
                        _map.Add(dagTemp, result);
                        _temps.Add(temp);
                    }

                    return result;
                }

                /// <summary>
                /// Try setting a user-declared variable (given by its accessing expression) to be
                /// used for a pattern-matching temporary variable. Returns true when not already
                /// assigned. The return value of this method is typically ignored by the caller as
                /// once we have made an assignment we can keep it (we keep the first assignment we
                /// find), but we return a success bool to emphasize that the assignment is not unconditional.
                /// </summary>
                public bool TrySetTemp(BoundDagTemp dagTemp, BoundExpression translation)
                {
                    if (!_map.ContainsKey(dagTemp))
                    {
                        _map.Add(dagTemp, translation);
                        return true;
                    }

                    return false;
                }

                public ImmutableArray<LocalSymbol> AllTemps()
                {
                    return _temps.ToImmutableArray();
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
                            var outputTemp = new BoundDagTemp(f.Syntax, field.Type, f);
                            BoundExpression output = _tempAllocator.GetTemp(outputTemp);
                            BoundExpression access = _localRewriter.MakeFieldAccess(f.Syntax, input, field, null, LookupResultKind.Viable, field.Type);
                            access.WasCompilerGenerated = true;
                            return _factory.AssignmentExpression(output, access);
                        }

                    case BoundDagPropertyEvaluation p:
                        {
                            PropertySymbol property = p.Property;
                            var outputTemp = new BoundDagTemp(p.Syntax, property.Type, p);
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

                            Debug.Assert(method.Name == WellKnownMemberNames.DeconstructMethodName);
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
                                // Avoid using dynamic conversions for pattern-matching.
                                inputType = _factory.SpecialType(SpecialType.System_Object);
                                input = _factory.Convert(inputType, input);
                            }

                            TypeSymbol type = t.Type;
                            var outputTemp = new BoundDagTemp(t.Syntax, type, t);
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

                    case BoundDagIndexEvaluation e:
                        {
                            // This is an evaluation of an indexed property with a constant int value.
                            // The input type must be ITuple, and the property must be a property of ITuple.
                            Debug.Assert(e.Property.ContainingSymbol.Equals(input.Type));
                            Debug.Assert(e.Property.GetMethod.ParameterCount == 1);
                            Debug.Assert(e.Property.GetMethod.Parameters[0].Type.SpecialType == SpecialType.System_Int32);
                            TypeSymbol type = e.Property.GetMethod.ReturnType;
                            var outputTemp = new BoundDagTemp(e.Syntax, type, e);
                            BoundExpression output = _tempAllocator.GetTemp(outputTemp);
                            return _factory.AssignmentExpression(output, _factory.Call(input, e.Property.GetMethod, _factory.Literal(e.Index)));
                        }

                    default:
                        throw ExceptionUtilities.UnexpectedValue(evaluation);
                }
            }

            /// <summary>
            /// Return the boolean expression to be evaluated for the given test. Returns `null` if the test is trivially true.
            /// </summary>
            protected BoundExpression LowerTest(BoundDagTest test)
            {
                _factory.Syntax = test.Syntax;
                BoundExpression input = _tempAllocator.GetTemp(test.Input);
                switch (test)
                {
                    case BoundDagNonNullTest d:
                        return _localRewriter.MakeNullCheck(d.Syntax, input, input.Type.IsNullableType() ? BinaryOperatorKind.NullableNullNotEqual : BinaryOperatorKind.NotEqual);

                    case BoundDagTypeTest d:
                        // Note that this tests for non-null as a side-effect. We depend on that to sometimes avoid the null check.
                        return _factory.Is(input, d.Type);

                    case BoundDagExplicitNullTest d:
                        return _localRewriter.MakeNullCheck(d.Syntax, input, input.Type.IsNullableType() ? BinaryOperatorKind.NullableNullEqual : BinaryOperatorKind.Equal);

                    case BoundDagValueTest d:
                        Debug.Assert(!input.Type.IsNullableType());
                        return MakeEqual(_localRewriter.MakeLiteral(d.Syntax, d.Value, input.Type), input);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(test);
                }
            }

            private BoundExpression MakeEqual(BoundExpression loweredLiteral, BoundExpression input)
            {
                Debug.Assert(loweredLiteral.Type.Equals(input.Type, TypeCompareKind.AllIgnoreOptions));

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
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.BoolEqual, input, loweredLiteral, booleanType, method: null);
                    case SpecialType.System_Byte:
                    case SpecialType.System_Char:
                    case SpecialType.System_Int16:
                    case SpecialType.System_SByte:
                    case SpecialType.System_UInt16:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.IntEqual, _factory.Convert(intType, input), _factory.Convert(intType, loweredLiteral), booleanType, method: null);
                    case SpecialType.System_Decimal:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.DecimalEqual, input, loweredLiteral, booleanType, method: null);
                    case SpecialType.System_Double:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.DoubleEqual, input, loweredLiteral, booleanType, method: null);
                    case SpecialType.System_Int32:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.IntEqual, input, loweredLiteral, booleanType, method: null);
                    case SpecialType.System_Int64:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.LongEqual, input, loweredLiteral, booleanType, method: null);
                    case SpecialType.System_Single:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.FloatEqual, input, loweredLiteral, booleanType, method: null);
                    case SpecialType.System_String:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.StringEqual, input, loweredLiteral, booleanType, method: null);
                    case SpecialType.System_UInt32:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.UIntEqual, input, loweredLiteral, booleanType, method: null);
                    case SpecialType.System_UInt64:
                        return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.ULongEqual, input, loweredLiteral, booleanType, method: null);
                    default:
                        if (loweredLiteral.Type.IsEnumType())
                        {
                            return _localRewriter.MakeBinaryOperator(_factory.Syntax, BinaryOperatorKind.EnumEqual, input, loweredLiteral, booleanType, method: null);
                        }

                        // This is the (correct but inefficient) fallback for any type that isn't yet implemented.
                        // However, the above should handle all types.
                        Debug.Assert(false); // don't fail in non-debug builds
                        NamedTypeSymbol systemObject = _factory.SpecialType(SpecialType.System_Object);
                        return _factory.StaticCall(
                            systemObject,
                            "Equals",
                            _factory.Convert(systemObject, loweredLiteral),
                            _factory.Convert(systemObject, input)
                            );
                }
            }

            /// <summary>
            /// Lower a test followed by an evaluation into a side-effect followed by a test. This permits us to optimize
            /// a type test followed by a cast into an `as` expression followed by a null check. Returns true if the optimization
            /// applies and the results are placed into <paramref name="sideEffect"/> and <paramref name="test"/>. The caller
            /// should place the side-effect before the test in the generated code.
            /// </summary>
            /// <param name="evaluation"></param>
            /// <param name="test"></param>
            /// <param name="sideEffect"></param>
            /// <param name="testExpression"></param>
            /// <returns>true if the optimization is applied</returns>
            protected bool TryLowerTypeTestAndCast(
                BoundDagTest test,
                BoundDagEvaluation evaluation,
                out BoundExpression sideEffect,
                out BoundExpression testExpression)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                // case 1: type test followed by cast to that type
                if (test is BoundDagTypeTest typeDecision &&
                    evaluation is BoundDagTypeEvaluation typeEvaluation1 &&
                    typeDecision.Type.IsReferenceType &&
                    typeEvaluation1.Type.Equals(typeDecision.Type, TypeCompareKind.AllIgnoreOptions) &&
                    typeEvaluation1.Input == typeDecision.Input)
                {
                    BoundExpression input = _tempAllocator.GetTemp(test.Input);
                    BoundExpression output = _tempAllocator.GetTemp(new BoundDagTemp(evaluation.Syntax, typeEvaluation1.Type, evaluation));
                    sideEffect = _factory.AssignmentExpression(output, _factory.As(input, typeEvaluation1.Type));
                    testExpression = _factory.ObjectNotEqual(output, _factory.Null(output.Type));
                    return true;
                }

                // case 2: null check followed by cast to a base type
                if (test is BoundDagNonNullTest nonNullTest &&
                    evaluation is BoundDagTypeEvaluation typeEvaluation2 &&
                    _factory.Compilation.Conversions.ClassifyBuiltInConversion(test.Input.Type, typeEvaluation2.Type, ref useSiteDiagnostics) is Conversion conv &&
                    (conv.IsIdentity || conv.Kind == ConversionKind.ImplicitReference || conv.IsBoxing) &&
                    typeEvaluation2.Input == nonNullTest.Input)
                {
                    BoundExpression input = _tempAllocator.GetTemp(test.Input);
                    var baseType = typeEvaluation2.Type;
                    BoundExpression output = _tempAllocator.GetTemp(new BoundDagTemp(evaluation.Syntax, baseType, evaluation));
                    sideEffect = _factory.AssignmentExpression(output, _factory.Convert(baseType, input));
                    testExpression = _factory.ObjectNotEqual(output, _factory.Null(baseType));
                    _localRewriter._diagnostics.Add(test.Syntax, useSiteDiagnostics);
                    return true;
                }

                sideEffect = testExpression = null;
                return false;
            }

            /// <summary>
            /// Produce assignment of the input expression. This method is also responsible for assigning
            /// variables for some pattern-matching temps that can be shared with user variables.
            /// </summary>
            protected BoundDecisionDag ShareTempsAndEvaluateInput(
                BoundExpression loweredInput,
                BoundDecisionDag decisionDag,
                Action<BoundExpression> addCode,
                out BoundExpression savedInputExpression)
            {
                var inputDagTemp = BoundDagTemp.ForOriginalInput(loweredInput);
                if (loweredInput.Kind == BoundKind.Local || loweredInput.Kind == BoundKind.Parameter)
                {
                    // If we're switching on a local variable and there is no when clause (checked by the caller),
                    // we assume the value of the local variable does not change during the execution of the
                    // decision automaton and we just reuse the local variable when we need the input expression.
                    // It is possible for this assumption to be violated by a side-effecting Deconstruct that
                    // modifies the local variable which has been captured in a lambda. Since the language assumes
                    // that functions called by pattern-matching are idempotent and not side-effecting, we feel
                    // justified in taking this assumption in the compiler too.
                    bool tempAssigned = _tempAllocator.TrySetTemp(inputDagTemp, loweredInput);
                    Debug.Assert(tempAssigned);
                }

                foreach (BoundDecisionDagNode node in decisionDag.TopologicallySortedNodes)
                {
                    if (node is BoundWhenDecisionDagNode w)
                    {
                        // We share a slot for a user-declared pattern-matching variable with a pattern temp if there
                        // is no user-written when-clause that could modify the variable before the matching
                        // automaton is done with it (checked by the caller).
                        foreach (BoundPatternBinding binding in w.Bindings)
                        {
                            if (binding.VariableAccess is BoundLocal l)
                            {
                                Debug.Assert(l.LocalSymbol.DeclarationKind == LocalDeclarationKind.PatternVariable);
                                _ = _tempAllocator.TrySetTemp(binding.TempContainingValue, binding.VariableAccess);
                            }
                        }
                    }
                }

                if (loweredInput.Type.IsTupleType &&
                    loweredInput.Syntax.Kind() == SyntaxKind.TupleExpression &&
                    loweredInput is BoundObjectCreationExpression expr &&
                    !decisionDag.TopologicallySortedNodes.Any(n => usesOriginalInput(n)))
                {
                    // If the switch governing expression is a tuple literal whose whole value is not used anywhere,
                    // (though perhaps its component parts are used), then we can save the component parts
                    // and assign them into temps (or perhaps user variables) to avoid the creation of
                    // the tuple altogether.
                    decisionDag = RewriteTupleInput(decisionDag, expr, addCode, out savedInputExpression);
                }
                else
                {
                    // Otherwise we emit an assignment of the input expression to a temporary variable.
                    BoundExpression inputTemp = _tempAllocator.GetTemp(inputDagTemp);
                    savedInputExpression = inputTemp;
                    if (inputTemp != loweredInput)
                    {
                        addCode(_factory.AssignmentExpression(inputTemp, loweredInput));
                    }
                }

                Debug.Assert(savedInputExpression != null);
                return decisionDag;

                bool usesOriginalInput(BoundDecisionDagNode node)
                {
                    switch (node)
                    {
                        case BoundWhenDecisionDagNode n:
                            return n.Bindings.Any(b => b.TempContainingValue.IsOriginalInput);
                        case BoundTestDecisionDagNode t:
                            return t.Test.Input.IsOriginalInput;
                        case BoundEvaluationDecisionDagNode e:
                            switch (e.Evaluation)
                            {
                                case BoundDagFieldEvaluation f:
                                    return f.Input.IsOriginalInput && !f.Field.IsTupleElement();
                                default:
                                    return e.Evaluation.Input.IsOriginalInput;
                            }
                        default:
                            return false;
                    }
                }
            }

            /// <summary>
            /// We have a decision dag whose input is a tuple literal, and the decision dag does not need the tuple itself.
            /// We rewrite the decision dag into one which doesn't touch the tuple, but instead works directly with the
            /// values that have been stored in temps. This permits the caller to avoid creation of the tuple object
            /// itself. We also emit assignments of the tuple values into their corresponding temps.
            /// </summary>
            /// <param name="savedInputExpression">An expression that produces the value of the original input if needed
            /// by the caller.</param>
            /// <returns>A new decision dag that does not reference the input directly</returns>
            private BoundDecisionDag RewriteTupleInput(
                BoundDecisionDag decisionDag,
                BoundObjectCreationExpression loweredInput,
                Action<BoundExpression> addCode,
                out BoundExpression savedInputExpression)
            {
                int count = loweredInput.Arguments.Length;
                var tupleElementEvaluated = new bool[count];

                // first evaluate the inputs (in order) into temps
                var originalInput = BoundDagTemp.ForOriginalInput(loweredInput.Syntax, loweredInput.Type);
                var newArguments = ArrayBuilder<BoundExpression>.GetInstance(loweredInput.Arguments.Length);
                for (int i = 0; i < count; i++)
                {
                    var field = loweredInput.Type.TupleElements[i].CorrespondingTupleField;
                    Debug.Assert(field != null);
                    var expr = loweredInput.Arguments[i];
                    var fieldFetchEvaluation = new BoundDagFieldEvaluation(expr.Syntax, field, originalInput);
                    var temp = new BoundDagTemp(expr.Syntax, expr.Type, fieldFetchEvaluation);
                    Debug.Assert(!tupleElementEvaluated[i]);
                    storeToTemp(temp, expr);
                    tupleElementEvaluated[i] = true;
                    newArguments.Add(_tempAllocator.GetTemp(temp));
                }

                var rewrittenDag = decisionDag.Rewrite(makeReplacement);
                savedInputExpression = loweredInput.Update(
                    loweredInput.Constructor, arguments: newArguments.ToImmutableAndFree(), loweredInput.ArgumentNamesOpt, loweredInput.ArgumentRefKindsOpt,
                    loweredInput.Expanded, loweredInput.ArgsToParamsOpt, loweredInput.ConstantValueOpt,
                    loweredInput.InitializerExpressionOpt, loweredInput.BinderOpt, loweredInput.Type);

                return rewrittenDag;

                void storeToTemp(BoundDagTemp temp, BoundExpression expr)
                {
                    if ((expr.Kind == BoundKind.Parameter || expr.Kind == BoundKind.Local) && _tempAllocator.TrySetTemp(temp, expr))
                    {
                        // we've arranged to use the input value from the variable it is already stored in
                    }
                    else
                    {
                        var tempToHoldInput = _tempAllocator.GetTemp(temp);
                        addCode(_factory.AssignmentExpression(tempToHoldInput, expr));
                    }
                }

                BoundDecisionDagNode makeReplacement(BoundDecisionDagNode node, Func<BoundDecisionDagNode, BoundDecisionDagNode> replacement)
                {
                    switch (node)
                    {
                        case BoundEvaluationDecisionDagNode evalNode:
                            if (evalNode.Evaluation is BoundDagFieldEvaluation eval &&
                                eval.Input.IsOriginalInput &&
                                eval.Field is var field &&
                                field.CorrespondingTupleField != null &&
                                field.TupleElementIndex is int i)
                            {
                                Debug.Assert(tupleElementEvaluated[i]);
                                return replacement(evalNode.Next);
                            }

                            Debug.Assert(!evalNode.Evaluation.Input.IsOriginalInput);
                            break;

                        case BoundTestDecisionDagNode testNode:
                            Debug.Assert(!testNode.Test.Input.IsOriginalInput);
                            break;
                    }

                    return BoundDecisionDag.TrivialReplacement(node, replacement);
                }
            }
        }
    }
}
