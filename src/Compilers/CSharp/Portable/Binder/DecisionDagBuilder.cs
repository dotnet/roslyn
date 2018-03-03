// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class DecisionDagBuilder
    {
        private readonly Conversions _conversions;
        private readonly TypeSymbol _booleanType;
        private readonly TypeSymbol _objectType;
        private readonly DiagnosticBag _diagnostics;

        private DecisionDagBuilder(CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            this._conversions = compilation.Conversions;
            this._booleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            this._objectType = compilation.GetSpecialType(SpecialType.System_Object);
            _diagnostics = diagnostics;
        }

        /// <summary>
        /// Used to create a decision dag for a switch statement.
        /// </summary>
        public static BoundDecisionDag CreateDecisionDag(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression switchGoverningExpression,
            ImmutableArray<BoundPatternSwitchSection> switchSections,
            LabelSymbol defaultLabel,
            DiagnosticBag diagnostics)
        {
            var builder = new DecisionDagBuilder(compilation, diagnostics);
            var result = builder.CreateDecisionDag(syntax, switchGoverningExpression, switchSections, defaultLabel);
            return result;
        }

        /// <summary>
        /// Used to create a decision dag for a switch expression.
        /// </summary>
        public static BoundDecisionDag CreateDecisionDag(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression switchExpressionInput,
            ImmutableArray<BoundSwitchExpressionArm> switchArms,
            LabelSymbol defaultLabel,
            DiagnosticBag diagnostics)
        {
            var builder = new DecisionDagBuilder(compilation, diagnostics);
            var result = builder.CreateDecisionDag(syntax, switchExpressionInput, switchArms, defaultLabel);
            return result;
        }

        /// <summary>
        /// Used to translate the pattern of an is-pattern expression. Returns the BoundDagTemp used to represent the root (input).
        /// </summary>
        public static BoundDagTemp TranslatePattern(
            CSharpCompilation compilation,
            BoundExpression loweredInput,
            BoundPattern pattern,
            DiagnosticBag diagnostics,
            out ImmutableArray<BoundDagDecision> decisions,
            out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings)
        {
            DecisionDagBuilder builder = new DecisionDagBuilder(compilation, diagnostics);
            var result = builder.TranslatePattern(loweredInput, pattern, out decisions, out bindings);
            return result;
        }

        private BoundDagTemp TranslatePattern(
            BoundExpression input,
            BoundPattern pattern,
            out ImmutableArray<BoundDagDecision> decisions,
            out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings)
        {
            var rootIdentifier = new BoundDagTemp(input.Syntax, input.Type, null, 0);
            MakeAndSimplifyDecisionsAndBindings(rootIdentifier, pattern, out decisions, out bindings);
            return rootIdentifier;
        }

        private BoundDecisionDag CreateDecisionDag(
            SyntaxNode syntax,
            BoundExpression switchGoverningExpression,
            ImmutableArray<BoundPatternSwitchSection> switchSections,
            LabelSymbol defaultLabel)
        {
            ImmutableArray<PartialCaseDecision> cases = MakeCases(switchGoverningExpression, switchSections);
            BoundDecisionDag dag = MakeDecisionDag(syntax, cases, defaultLabel);
            return dag;
        }

        private ImmutableArray<PartialCaseDecision> MakeCases(BoundExpression switchGoverningExpression, ImmutableArray<BoundPatternSwitchSection> switchSections)
        {
            var rootIdentifier = new BoundDagTemp(switchGoverningExpression.Syntax, switchGoverningExpression.Type, null, 0);
            int i = 0;
            var builder = ArrayBuilder<PartialCaseDecision>.GetInstance(switchSections.Length);
            foreach (BoundPatternSwitchSection section in switchSections)
            {
                foreach (BoundPatternSwitchLabel label in section.SwitchLabels)
                {
                    if (label.Syntax.Kind() != SyntaxKind.DefaultSwitchLabel)
                    {
                        builder.Add(MakePartialCaseDecision(++i, rootIdentifier, label));
                    }
                }
            }

            return builder.ToImmutableAndFree();
        }

        private PartialCaseDecision MakePartialCaseDecision(int index, BoundDagTemp input, BoundPatternSwitchLabel label)
        {
            MakeAndSimplifyDecisionsAndBindings(input, label.Pattern, out ImmutableArray<BoundDagDecision> decisions, out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings);
            return new PartialCaseDecision(index, label.Syntax, decisions, bindings, label.Guard, label.Label);
        }

        /// <summary>
        /// Used to create a decision dag for a switch expression.
        /// </summary>
        /// <param name="syntax"></param>
        /// <param name="switchExpressionInput"></param>
        /// <param name="switchArms"></param>
        /// <param name="defaultLabel"></param>
        /// <returns></returns>
        private BoundDecisionDag CreateDecisionDag(
            SyntaxNode syntax,
            BoundExpression switchExpressionInput,
            ImmutableArray<BoundSwitchExpressionArm> switchArms,
            LabelSymbol defaultLabel)
        {
            ImmutableArray<PartialCaseDecision> arms = MakeArms(switchExpressionInput, switchArms);
            BoundDecisionDag dag = MakeDecisionDag(syntax, arms, defaultLabel);
            return dag;
        }

        private ImmutableArray<PartialCaseDecision> MakeArms(BoundExpression switchExpressionInput, ImmutableArray<BoundSwitchExpressionArm> switchArms)
        {
            var rootIdentifier = new BoundDagTemp(switchExpressionInput.Syntax, switchExpressionInput.Type, null, 0);
            int i = 0;
            var builder = ArrayBuilder<PartialCaseDecision>.GetInstance(switchArms.Length);
            foreach (BoundSwitchExpressionArm arm in switchArms)
            {
                builder.Add(MakePartialCaseDecision(++i, rootIdentifier, arm));
            }

            return builder.ToImmutableAndFree();
        }

        private PartialCaseDecision MakePartialCaseDecision(int index, BoundDagTemp input, BoundSwitchExpressionArm arm)
        {
            MakeAndSimplifyDecisionsAndBindings(input, arm.Pattern, out ImmutableArray<BoundDagDecision> decisions, out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings);
            return new PartialCaseDecision(index, arm.Syntax, decisions, bindings, arm.Guard, arm.Label);
        }

        private void MakeAndSimplifyDecisionsAndBindings(
            BoundDagTemp input,
            BoundPattern pattern,
            out ImmutableArray<BoundDagDecision> decisions,
            out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings)
        {
            var decisionsBuilder = ArrayBuilder<BoundDagDecision>.GetInstance();
            var bindingsBuilder = ArrayBuilder<(BoundExpression, BoundDagTemp)>.GetInstance();
            MakeDecisionsAndBindings(input, pattern, decisionsBuilder, bindingsBuilder);

            // Now simplify the decisions and bindings. We don't need anything in decisions that does not
            // contribute to the result. This will, for example, permit us to match `(2, 3) is (2, _)` without
            // fetching `Item2` from the input.
            var usedValues = PooledHashSet<BoundDagEvaluation>.GetInstance();
            foreach ((var _, BoundDagTemp temp) in bindingsBuilder)
            {
                if (temp.Source != (object)null)
                {
                    usedValues.Add(temp.Source);
                }
            }
            for (int i = decisionsBuilder.Count - 1; i >= 0; i--)
            {
                switch (decisionsBuilder[i])
                {
                    case BoundDagEvaluation e:
                        {
                            if (usedValues.Contains(e))
                            {
                                if (e.Input.Source != (object)null)
                                {
                                    usedValues.Add(e.Input.Source);
                                }
                            }
                            else
                            {
                                decisionsBuilder.RemoveAt(i);
                            }
                        }
                        break;
                    case BoundDagDecision d:
                        {
                            usedValues.Add(d.Input.Source);
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(decisionsBuilder[i]);
                }
            }

            // We also do not need to compute any result more than once. This will permit us to fetch
            // a property once even if it is used more than once, e.g. `o is { X: P1, X: P2 }`
            usedValues.Clear();
            usedValues.Add(input.Source);
            for (int i = 0; i < decisionsBuilder.Count; i++)
            {
                switch (decisionsBuilder[i])
                {
                    case BoundDagEvaluation e:
                        if (usedValues.Contains(e))
                        {
                            decisionsBuilder.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            usedValues.Add(e);
                        }
                        break;
                }
            }

            usedValues.Free();
            decisions = decisionsBuilder.ToImmutableAndFree();
            bindings = bindingsBuilder.ToImmutableAndFree();
        }

        private void MakeDecisionsAndBindings(
            BoundDagTemp input,
            BoundPattern pattern,
            ArrayBuilder<BoundDagDecision> decisions,
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindings)
        {
            switch (pattern)
            {
                case BoundDeclarationPattern declaration:
                    MakeDecisionsAndBindings(input, declaration, decisions, bindings);
                    break;
                case BoundConstantPattern constant:
                    MakeDecisionsAndBindings(input, constant, decisions, bindings);
                    break;
                case BoundDiscardPattern wildcard:
                    // Nothing to do. It always matches.
                    break;
                case BoundRecursivePattern recursive:
                    MakeDecisionsAndBindings(input, recursive, decisions, bindings);
                    break;
                default:
                    throw new NotImplementedException(pattern.Kind.ToString());
            }
        }

        private void MakeDecisionsAndBindings(
            BoundDagTemp input,
            BoundDeclarationPattern declaration,
            ArrayBuilder<BoundDagDecision> decisions,
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindings)
        {
            TypeSymbol type = declaration.DeclaredType.Type;
            SyntaxNode syntax = declaration.Syntax;

            // Add a null and type test if needed.
            if (!declaration.IsVar)
            {
                NullCheck(input, declaration.Syntax, decisions);
                input = ConvertToType(input, declaration.Syntax, type, decisions);
            }

            BoundExpression left = declaration.VariableAccess;
            if (left != null)
            {
                Debug.Assert(left.Type == input.Type);
                bindings.Add((left, input));
            }
            else
            {
                Debug.Assert(declaration.Variable == null);
            }
        }

        private void NullCheck(
            BoundDagTemp input,
            SyntaxNode syntax,
            ArrayBuilder<BoundDagDecision> decisions)
        {
            if (input.Type.CanContainNull())
            {
                // Add a null test
                decisions.Add(new BoundNonNullDecision(syntax, input));
            }
        }

        private BoundDagTemp ConvertToType(
            BoundDagTemp input,
            SyntaxNode syntax,
            TypeSymbol type,
            ArrayBuilder<BoundDagDecision> decisions)
        {
            if (input.Type != type)
            {
                TypeSymbol inputType = input.Type.StrippedType(); // since a null check has already been done
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                Conversion conversion = _conversions.ClassifyBuiltInConversion(inputType, type, ref useSiteDiagnostics);
                _diagnostics.Add(syntax, useSiteDiagnostics);
                if (input.Type.IsDynamic() ? type.SpecialType == SpecialType.System_Object : conversion.IsImplicit)
                {
                    // type test not needed, only the type cast
                }
                else
                {
                    // both type test and cast needed
                    decisions.Add(new BoundTypeDecision(syntax, type, input));
                }

                var evaluation = new BoundDagTypeEvaluation(syntax, type, input);
                input = new BoundDagTemp(syntax, type, evaluation, 0);
                decisions.Add(evaluation);
            }

            return input;
        }

        private void MakeDecisionsAndBindings(
            BoundDagTemp input,
            BoundConstantPattern constant,
            ArrayBuilder<BoundDagDecision> decisions,
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindings)
        {
            if (constant.ConstantValue == ConstantValue.Null)
            {
                decisions.Add(new BoundNullValueDecision(constant.Syntax, input));
            }
            else
            {
                if (input.Type.CanContainNull())
                {
                    decisions.Add(new BoundNonNullDecision(constant.Syntax, input));
                }

                var convertedInput = ConvertToType(input, constant.Syntax, constant.Value.Type, decisions);
                decisions.Add(new BoundNonNullValueDecision(constant.Syntax, constant.ConstantValue, convertedInput));
            }
        }

        private void MakeDecisionsAndBindings(
            BoundDagTemp input,
            BoundRecursivePattern recursive,
            ArrayBuilder<BoundDagDecision> decisions,
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindings)
        {
            Debug.Assert(input.Type.IsErrorType() || input.Type == recursive.InputType);
            NullCheck(input, recursive.Syntax, decisions);
            if (recursive.DeclaredType != null && recursive.DeclaredType.Type != input.Type)
            {
                input = ConvertToType(input, recursive.Syntax, recursive.DeclaredType.Type, decisions);
            }

            if (!recursive.Deconstruction.IsDefault)
            {
                // we have a "deconstruction" form, which is either an invocation of a Deconstruct method, or a disassembly of a tuple
                if (recursive.DeconstructMethodOpt != null)
                {
                    MethodSymbol method = recursive.DeconstructMethodOpt;
                    var evaluation = new BoundDagDeconstructEvaluation(recursive.Syntax, method, input);
                    decisions.Add(evaluation);
                    int extensionExtra = method.IsStatic ? 1 : 0;
                    int count = Math.Min(method.ParameterCount - extensionExtra, recursive.Deconstruction.Length);
                    for (int i = 0; i < count; i++)
                    {
                        BoundPattern pattern = recursive.Deconstruction[i];
                        SyntaxNode syntax = pattern.Syntax;
                        var output = new BoundDagTemp(syntax, method.Parameters[i + extensionExtra].Type, evaluation, i);
                        MakeDecisionsAndBindings(output, pattern, decisions, bindings);
                    }
                }
                else if (input.Type.IsTupleType)
                {
                    ImmutableArray<FieldSymbol> elements = input.Type.TupleElements;
                    ImmutableArray<TypeSymbol> elementTypes = input.Type.TupleElementTypes;
                    int count = Math.Min(elementTypes.Length, recursive.Deconstruction.Length);
                    for (int i = 0; i < count; i++)
                    {
                        BoundPattern pattern = recursive.Deconstruction[i];
                        SyntaxNode syntax = pattern.Syntax;
                        FieldSymbol field = elements[i];
                        var evaluation = new BoundDagFieldEvaluation(syntax, field, input); // fetch the ItemN field
                        decisions.Add(evaluation);
                        var output = new BoundDagTemp(syntax, field.Type, evaluation, 0);
                        MakeDecisionsAndBindings(output, pattern, decisions, bindings);
                    }
                }
                else
                {
                    // PROTOTYPE(patterns2): This should not occur except in error cases. Perhaps this will be used to handle the ITuple case.
                    Debug.Assert(recursive.HasAnyErrors);
                }
            }

            if (recursive.PropertiesOpt != null)
            {
                // we have a "property" form
                for (int i = 0; i < recursive.PropertiesOpt.Length; i++)
                {
                    (Symbol symbol, BoundPattern pattern) prop = recursive.PropertiesOpt[i];
                    Symbol symbol = prop.symbol;
                    BoundDagEvaluation evaluation;
                    switch (symbol)
                    {
                        case PropertySymbol property:
                            evaluation = new BoundDagPropertyEvaluation(prop.pattern.Syntax, property, input);
                            break;
                        case FieldSymbol field:
                            evaluation = new BoundDagFieldEvaluation(prop.pattern.Syntax, field, input);
                            break;
                        default:
                            Debug.Assert(recursive.HasAnyErrors);
                            continue;
                    }
                    decisions.Add(evaluation);
                    var output = new BoundDagTemp(prop.pattern.Syntax, prop.symbol.GetTypeOrReturnType(), evaluation, 0);
                    MakeDecisionsAndBindings(output, prop.pattern, decisions, bindings);
                }
            }

            if (recursive.VariableAccess != null)
            {
                // we have a "variable" declaration
                bindings.Add((recursive.VariableAccess, input));
            }
        }

        private BoundDecisionDag MakeDecisionDag(SyntaxNode syntax, ImmutableArray<PartialCaseDecision> cases, LabelSymbol defaultLabel)
        {
            if (cases.IsDefaultOrEmpty)
            {
                return new BoundDecision(syntax, defaultLabel);
            }

            PartialCaseDecision first = cases[0];
            if (first.Decisions.IsDefaultOrEmpty)
            {
                // The first pattern has fully matched
                if (first.WhereClause == null || first.WhereClause.ConstantValue == ConstantValue.True)
                {
                    // no (or constant true) where clause. The decision is finalized.
                    if (first.Bindings.IsDefaultOrEmpty)
                    {
                        return new BoundDecision(first.Syntax, first.CaseLabel);
                    }
                    else
                    {
                        return new BoundWhenClause(first.Syntax, first.Bindings, null, new BoundDecision(first.Syntax, first.CaseLabel), null);
                    }
                }
                else
                {
                    // in case the where clause fails, we prepare for the remaining cases.
                    ImmutableArray<PartialCaseDecision> remainingCases = cases.RemoveAt(0);
                    BoundDecisionDag whereFails = MakeDecisionDag(syntax, remainingCases, defaultLabel);
                    return new BoundWhenClause(first.Syntax, first.Bindings, first.WhereClause,
                        new BoundDecision(first.Syntax, first.CaseLabel), whereFails);
                }
            }
            else
            {
                switch (first.Decisions[0])
                {
                    case BoundDagEvaluation e:
                        BoundDecisionDag tail = MakeDecisionDag(e.Syntax, RemoveEvaluation(cases, e), defaultLabel);
                        return new BoundEvaluationPoint(e.Syntax, e, tail);
                    case BoundDagDecision d:
                        SplitCases(cases, d, out ImmutableArray<PartialCaseDecision> whenTrueDecisions, out ImmutableArray<PartialCaseDecision> whenFalseDecisions);
                        BoundDecisionDag whenTrue = MakeDecisionDag(d.Syntax, whenTrueDecisions, defaultLabel);
                        BoundDecisionDag whenFalse = MakeDecisionDag(d.Syntax, whenFalseDecisions, defaultLabel);
                        return new BoundDecisionPoint(d.Syntax, d, whenTrue, whenFalse);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(first.Decisions[0].Kind);
                }
            }
        }

        private void SplitCases(
            ImmutableArray<PartialCaseDecision> cases,
            BoundDagDecision d,
            out ImmutableArray<PartialCaseDecision> whenTrue,
            out ImmutableArray<PartialCaseDecision> whenFalse)
        {
            var whenTrueBuilder = ArrayBuilder<PartialCaseDecision>.GetInstance();
            var whenFalseBuilder = ArrayBuilder<PartialCaseDecision>.GetInstance();
            foreach (PartialCaseDecision c in cases)
            {
                FilterCase(c, d, whenTrueBuilder, whenFalseBuilder);
            }

            whenTrue = whenTrueBuilder.ToImmutableAndFree();
            whenFalse = whenFalseBuilder.ToImmutableAndFree();
        }

        private void FilterCase(
            PartialCaseDecision c,
            BoundDagDecision d,
            ArrayBuilder<PartialCaseDecision> whenTrueBuilder,
            ArrayBuilder<PartialCaseDecision> whenFalseBuilder)
        {
            var trueBuilder = ArrayBuilder<BoundDagDecision>.GetInstance();
            var falseBuilder = ArrayBuilder<BoundDagDecision>.GetInstance();
            foreach (BoundDagDecision other in c.Decisions)
            {
                CheckConsistentDecision(
                    d: d,
                    other: other,
                    syntax: d.Syntax,
                    trueDecisionPermitsTrueOther: out bool trueDecisionPermitsTrueOther,
                    falseDecisionPermitsTrueOther: out bool falseDecisionPermitsTrueOther,
                    trueDecisionImpliesTrueOther: out bool trueDecisionImpliesTrueOther,
                    falseDecisionImpliesTrueOther: out bool falseDecisionImpliesTrueOther);
                if (trueDecisionPermitsTrueOther)
                {
                    if (!trueDecisionImpliesTrueOther)
                    {
                        Debug.Assert(d != other);
                        trueBuilder?.Add(other);
                    }
                }
                else
                {
                    trueBuilder?.Free();
                    trueBuilder = null;
                }
                if (falseDecisionPermitsTrueOther)
                {
                    if (!falseDecisionImpliesTrueOther)
                    {
                        Debug.Assert(d != other);
                        falseBuilder?.Add(other);
                    }
                }
                else
                {
                    falseBuilder?.Free();
                    falseBuilder = null;
                }
            }

            if (trueBuilder != null)
            {
                var pcd = trueBuilder.Count == c.Decisions.Length ? c : new PartialCaseDecision(c.Index, c.Syntax, trueBuilder.ToImmutableAndFree(), c.Bindings, c.WhereClause, c.CaseLabel);
                whenTrueBuilder.Add(pcd);
            }

            if (falseBuilder != null)
            {
                var pcd = falseBuilder.Count == c.Decisions.Length ? c : new PartialCaseDecision(c.Index, c.Syntax, falseBuilder.ToImmutableAndFree(), c.Bindings, c.WhereClause, c.CaseLabel);
                whenFalseBuilder.Add(pcd);
            }
        }

        /// <summary>
        /// Given that the decision d has occurred and produced a true/false result,
        /// set some flags indicating the implied status of the other decision.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="other"></param>
        /// <param name="trueDecisionPermitsTrueOther">set if d being true would permit other to succeed</param>
        /// <param name="falseDecisionPermitsTrueOther">set if a false decision on d would permit other to succeed</param>
        /// <param name="trueDecisionImpliesTrueOther">set if d being false means other has been proven true</param>
        /// <param name="falseDecisionImpliesTrueOther">set if d being true means other has been proven true</param>
        private void CheckConsistentDecision(
            BoundDagDecision d,
            BoundDagDecision other,
            SyntaxNode syntax,
            out bool trueDecisionPermitsTrueOther,
            out bool falseDecisionPermitsTrueOther,
            out bool trueDecisionImpliesTrueOther,
            out bool falseDecisionImpliesTrueOther)
        {
            // PROTOTYPE(patterns2): the names and API shape are confusing. Perhaps
            // could be renamed to couldBeTrueIfTrue, couldBeFalseIfFalse, isDefinitelyTrueIfTrue, and isDefinitelyFalseIfFalse.
            // Possibly also use a tuple for returning the result.

            // innocent until proven guilty
            trueDecisionPermitsTrueOther = true;
            falseDecisionPermitsTrueOther = true;

            trueDecisionImpliesTrueOther = false;
            falseDecisionImpliesTrueOther = false;

            // if decisions test unrelated things, there is no implication from one to the other
            if (d.Input != other.Input)
            {
                return;
            }

            // a test is consistent with itself
            if (d == other)
            {
                trueDecisionImpliesTrueOther = true;
                falseDecisionPermitsTrueOther = false;
                return;
            }

            switch (d)
            {
                case BoundNonNullDecision n1:
                    switch (other)
                    {
                        case BoundNonNullValueDecision v2:
                            // Given that v!=null fails, v is null and v==K cannot succeed
                            falseDecisionPermitsTrueOther = false;
                            break;
                        case BoundNullValueDecision v2:
                            trueDecisionPermitsTrueOther = false; // if v!=null is true, then v==null cannot succeed
                            falseDecisionImpliesTrueOther = true; // if v!=null is false, then v==null has been proven true
                            break;
                        default:
                            // Once v!=null fails, it must fail a type test
                            falseDecisionPermitsTrueOther = false;
                            break;
                    }
                    break;
                case BoundTypeDecision t1:
                    switch (other)
                    {
                        case BoundNonNullDecision n2:
                            // Once `v is T` is true, v!=null cannot fail
                            trueDecisionImpliesTrueOther = true;
                            break;
                        case BoundTypeDecision t2:
                            // If T1 could never be T2, then success of T1 implies failure of T2.
                            {
                                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                                bool? matches = Binder.ExpressionOfTypeMatchesPatternType(_conversions, t1.Type, t2.Type, ref useSiteDiagnostics, out _);
                                if (matches == false)
                                {
                                    trueDecisionPermitsTrueOther = false;
                                }

                                // If every T2 is a T1, then failure of T1 implies failure of T2.
                                matches = Binder.ExpressionOfTypeMatchesPatternType(_conversions, t2.Type, t1.Type, ref useSiteDiagnostics, out _);
                                _diagnostics.Add(syntax, useSiteDiagnostics);
                                if (matches == true)
                                {
                                    // Once we know it is of the subtype, we do not need to test for the supertype.
                                    falseDecisionPermitsTrueOther = false;
                                }
                            }
                            break;
                        case BoundNonNullValueDecision v2:
                            // PROTOTYPE(patterns2): what can knowing that the type is/isn't T1 imply about knowing the value is v2?
                            break;
                        case BoundNullValueDecision v2:
                            trueDecisionPermitsTrueOther = false; // if v is T1 is true, then v==null cannot succeed
                            break;
                    }
                    break;
                case BoundNonNullValueDecision v1:
                    switch (other)
                    {
                        case BoundNonNullDecision n2:
                            // v==K implies v!=null
                            trueDecisionImpliesTrueOther = true;
                            break;
                        case BoundTypeDecision t2:
                            break;
                        case BoundNullValueDecision v2:
                            trueDecisionPermitsTrueOther = false;
                            break;
                        case BoundNonNullValueDecision v2:
                            if (v1.Value == v2.Value)
                            {
                                trueDecisionImpliesTrueOther = true;
                                falseDecisionPermitsTrueOther = false;
                            }
                            else
                            {
                                trueDecisionPermitsTrueOther = false;
                                if (v1.Input.Type.SpecialType == SpecialType.System_Boolean)
                                {
                                    // As a special case, we note that boolean values can only ever be true or false.
                                    falseDecisionImpliesTrueOther = true;
                                }
                            }

                            break;
                    }
                    break;
                case BoundNullValueDecision v1:
                    switch (other)
                    {
                        case BoundNonNullDecision n2:
                            trueDecisionPermitsTrueOther = false; // v==null being true does not permit v!=null to be true
                            falseDecisionImpliesTrueOther = true; // v==null being false implies v!=null to be true
                            break;
                        case BoundTypeDecision t2:
                            // v==null does not permit v is T
                            trueDecisionPermitsTrueOther = false;
                            break;
                        case BoundNullValueDecision v2:
                            trueDecisionImpliesTrueOther = true;
                            falseDecisionPermitsTrueOther = false;
                            break;
                        case BoundNonNullValueDecision v2:
                            trueDecisionPermitsTrueOther = false;
                            break;
                    }
                    break;
            }
        }

        private static ImmutableArray<PartialCaseDecision> RemoveEvaluation(ImmutableArray<PartialCaseDecision> cases, BoundDagEvaluation e)
        {
            return cases.SelectAsArray(c => RemoveEvaluation(c, e));
        }

        private static PartialCaseDecision RemoveEvaluation(PartialCaseDecision c, BoundDagEvaluation e)
        {
            return new PartialCaseDecision(
                Index: c.Index,
                Syntax: c.Syntax,
                Decisions: c.Decisions.WhereAsArray(d => !(d is BoundDagEvaluation e2) || e2 != e),
                Bindings: c.Bindings, WhereClause: c.WhereClause, CaseLabel: c.CaseLabel);
        }

        internal class PartialCaseDecision
        {
            public readonly int Index;
            public readonly SyntaxNode Syntax;
            public readonly ImmutableArray<BoundDagDecision> Decisions;
            public readonly ImmutableArray<(BoundExpression, BoundDagTemp)> Bindings;
            public readonly BoundExpression WhereClause;
            public readonly LabelSymbol CaseLabel;
            public PartialCaseDecision(
                int Index,
                SyntaxNode Syntax,
                ImmutableArray<BoundDagDecision> Decisions,
                ImmutableArray<(BoundExpression, BoundDagTemp)> Bindings,
                BoundExpression WhereClause,
                LabelSymbol CaseLabel)
            {
                this.Index = Index;
                this.Syntax = Syntax;
                this.Decisions = Decisions;
                this.Bindings = Bindings;
                this.WhereClause = WhereClause;
                this.CaseLabel = CaseLabel;
            }
        }
    }
}
