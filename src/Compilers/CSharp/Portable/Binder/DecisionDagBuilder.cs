// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private readonly LabelSymbol _defaultLabel;

        private DecisionDagBuilder(CSharpCompilation compilation, LabelSymbol defaultLabel, DiagnosticBag diagnostics)
        {
            this._conversions = compilation.Conversions;
            this._booleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            this._objectType = compilation.GetSpecialType(SpecialType.System_Object);
            _diagnostics = diagnostics;
            _defaultLabel = defaultLabel;
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
            var builder = new DecisionDagBuilder(compilation, defaultLabel, diagnostics);
            var result = builder.CreateDecisionDag(syntax, switchGoverningExpression, switchSections);
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
            var builder = new DecisionDagBuilder(compilation, defaultLabel, diagnostics);
            var result = builder.CreateDecisionDag(syntax, switchExpressionInput, switchArms);
            return result;
        }

        /// <summary>
        /// Used to translate the pattern of an is-pattern expression.
        /// </summary>
        public static BoundDecisionDag CreateDecisionDag(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression inputExpression,
            BoundPattern pattern,
            LabelSymbol defaultLabel,
            DiagnosticBag diagnostics,
            out LabelSymbol successLabel)
        {
            var builder = new DecisionDagBuilder(compilation, defaultLabel, diagnostics);
            BoundDecisionDag result = builder.CreateDecisionDag(syntax, inputExpression, pattern, out successLabel);
            return result;
        }

        private BoundDecisionDag CreateDecisionDag(
            SyntaxNode syntax,
            BoundExpression inputExpression,
            BoundPattern pattern,
            out LabelSymbol successLabel)
        {
            successLabel = new GeneratedLabelSymbol("success");
            ImmutableArray<PartialCaseDecision> cases = MakeCases(inputExpression, pattern, successLabel);
            BoundDecisionDag dag = MakeDecisionDag(syntax, cases);
            return dag;
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
            ImmutableArray<BoundPatternSwitchSection> switchSections)
        {
            ImmutableArray<PartialCaseDecision> cases = MakeCases(switchGoverningExpression, switchSections);
            BoundDecisionDag dag = MakeDecisionDag(syntax, cases);
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

        private ImmutableArray<PartialCaseDecision> MakeCases(BoundExpression switchGoverningExpression, BoundPattern pattern, LabelSymbol successLabel)
        {
            var rootIdentifier = new BoundDagTemp(switchGoverningExpression.Syntax, switchGoverningExpression.Type, null, 0);
            return ImmutableArray.Create(MakePartialCaseDecision(1, rootIdentifier, pattern, successLabel));
        }

        private PartialCaseDecision MakePartialCaseDecision(int index, BoundDagTemp input, BoundPattern pattern, LabelSymbol successLabel)
        {
            MakeAndSimplifyDecisionsAndBindings(input, pattern, out ImmutableArray<BoundDagDecision> decisions, out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings);
            return new PartialCaseDecision(index, pattern.Syntax, decisions, bindings, null, successLabel);
        }

        private PartialCaseDecision MakePartialCaseDecision(int index, BoundDagTemp input, BoundPatternSwitchLabel label)
        {
            MakeAndSimplifyDecisionsAndBindings(input, label.Pattern, out ImmutableArray<BoundDagDecision> decisions, out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings);
            return new PartialCaseDecision(index, label.Syntax, decisions, bindings, label.Guard, label.Label);
        }

        /// <summary>
        /// Used to create a decision dag for a switch expression.
        /// </summary>
        private BoundDecisionDag CreateDecisionDag(
            SyntaxNode syntax,
            BoundExpression switchExpressionInput,
            ImmutableArray<BoundSwitchExpressionArm> switchArms)
        {
            ImmutableArray<PartialCaseDecision> arms = MakeArms(switchExpressionInput, switchArms);
            BoundDecisionDag dag = MakeDecisionDag(syntax, arms);
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
            SimplifyDecisionsAndBindings(decisionsBuilder, bindingsBuilder);
            decisions = decisionsBuilder.ToImmutableAndFree();
            bindings = bindingsBuilder.ToImmutableAndFree();
        }

        private void SimplifyDecisionsAndBindings(
            ArrayBuilder<BoundDagDecision> decisionsBuilder,
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindingsBuilder)
        {
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

        private BoundDecisionDag MakeDecisionDag(SyntaxNode syntax, ImmutableArray<PartialCaseDecision> cases)
        {
            var defaultDecision = new BoundDecision(syntax, _defaultLabel);
            return MakeDecisionDag(new DagState(cases), defaultDecision);
        }

        /// <summary>
        /// Compute and translate the decision tree, given a description of its initial state and a default
        /// decision when no decision appears to match. This implementation is nonrecursive to avoid
        /// overflowing the compiler's evaluation stack when compiling a large switch statement.
        /// </summary>
        private BoundDecisionDag MakeDecisionDag(DagState initialState, BoundDecision defaultDecision)
        {
            // A work list of DagStates whose successors need to be computed
            var workList = ArrayBuilder<DagState>.GetInstance();
            // A mapping used to make each DagState unique (i.e. to de-dup identical states)
            var uniqueState = new Dictionary<DagState, DagState>(DagStateEquivalence.Instance);
            DagState uniqifyState(DagState state)
            {
                if (uniqueState.TryGetValue(state, out DagState existingState))
                {
                    return existingState;
                }
                else
                {
                    uniqueState.Add(state, state);
                    workList.Push(state);
                    return state;
                }
            }

            initialState = uniqifyState(initialState);

            // Go through the worklist, preparing successor states for each dag state
            while (workList.Count != 0)
            {
                DagState state = workList.Pop();
                if (state.Cases.IsDefaultOrEmpty)
                {
                    continue;
                }
                PartialCaseDecision first = state.Cases[0];
                if (first.Decisions.IsDefaultOrEmpty)
                {
                    // The first pattern has fully matched.
                    if (first.WhenClause == null || first.WhenClause.ConstantValue == ConstantValue.True)
                    {
                        // The when clause is satisfied
                    }
                    else
                    {
                        // in case the when clause fails, we prepare for the remaining cases.
                        var stateWhenFails = state.Cases.RemoveAt(0);
                        state.FalseBranch = uniqifyState(stateWhenFails);
                    }
                }
                else
                {
                    switch (state.SelectedDecision = state.ComputeSelectedDecision())
                    {
                        case BoundDagEvaluation e:
                            state.TrueBranch = uniqifyState(RemoveEvaluation(state.Cases, e));
                            break;
                        case BoundDagDecision d:
                            SplitCases(state.Cases, d, out ImmutableArray<PartialCaseDecision> whenTrueDecisions, out ImmutableArray<PartialCaseDecision> whenFalseDecisions);
                            state.TrueBranch = uniqifyState(whenTrueDecisions);
                            state.FalseBranch = uniqifyState(whenFalseDecisions);
                            break;
                        case var n:
                            throw ExceptionUtilities.UnexpectedValue(n.Kind);
                    }
                }
            }

            workList.Free();

            // A successor function used to topologically sort the DagState set.
            IEnumerable<DagState> succ(DagState state)
            {
                if (state.TrueBranch != null)
                {
                    yield return state.TrueBranch;
                }

                if (state.FalseBranch != null)
                {
                    yield return state.FalseBranch;
                }
            }

            // Now process the states in topological order, leaves first, and assign a BoundDecisionDag to each DagState.
            ImmutableArray<DagState> sortedStates = TopologicalSort.IterativeSort<DagState>(SpecializedCollections.SingletonEnumerable<DagState>(initialState), succ);
            Debug.Assert(_defaultLabel != null);
            var finalStates = PooledDictionary<LabelSymbol, BoundDecisionDag>.GetInstance();
            finalStates.Add(_defaultLabel, defaultDecision);
            BoundDecisionDag finalState(SyntaxNode syntax, LabelSymbol label, ImmutableArray<(BoundExpression, BoundDagTemp)> bindings)
            {
                if (!finalStates.TryGetValue(label, out BoundDecisionDag final))
                {
                    if (bindings.IsDefaultOrEmpty)
                    {
                        final = new BoundDecision(syntax, label);
                    }
                    else
                    {
                        final = new BoundWhenClause(syntax, bindings, null, new BoundDecision(syntax, label), null);
                    }

                    finalStates.Add(label, final);
                }

                return final;
            }

            for (int i = sortedStates.Length - 1; i >= 0; i--)
            {
                var state = sortedStates[i];
                if (state.Cases.IsDefaultOrEmpty)
                {
                    state.Dag = defaultDecision;
                    continue;
                }

                PartialCaseDecision first = state.Cases[0];
                if (first.Decisions.IsDefaultOrEmpty)
                {
                    // The first case/pattern has fully matched
                    if (first.WhenClause == null || first.WhenClause.ConstantValue == ConstantValue.True)
                    {
                        state.Dag = finalState(first.Syntax, first.CaseLabel, first.Bindings);
                    }
                    else
                    {
                        // in case the when clause fails, we prepare for the remaining cases.
                        Debug.Assert(state.TrueBranch == null);
                        BoundDecisionDag whenTrue = finalState(first.Syntax, first.CaseLabel, default);
                        BoundDecisionDag whenFails = state.FalseBranch.Dag;
                        Debug.Assert(whenFails != null);
                        state.Dag = new BoundWhenClause(first.Syntax, first.Bindings, first.WhenClause, whenTrue, whenFails);
                    }
                }
                else
                {
                    switch (state.SelectedDecision)
                    {
                        case BoundDagEvaluation e:
                            {
                                BoundDecisionDag whenTrue = state.TrueBranch.Dag;
                                Debug.Assert(whenTrue != null);
                                Debug.Assert(state.FalseBranch == null);
                                state.Dag = new BoundEvaluationPoint(e.Syntax, e, whenTrue);
                            }
                            break;
                        case BoundDagDecision d:
                            {
                                BoundDecisionDag whenTrue = state.TrueBranch.Dag;
                                BoundDecisionDag whenFalse = state.FalseBranch.Dag;
                                Debug.Assert(whenTrue != null);
                                Debug.Assert(whenFalse != null);
                                state.Dag = new BoundDecisionPoint(d.Syntax, d, whenTrue, whenFalse);
                            }
                            break;
                        case var n:
                            throw ExceptionUtilities.UnexpectedValue(n.Kind);
                    }
                }
            }

            finalStates.Free();

            Debug.Assert(initialState.Dag != null);
            // Note: It is useful for debugging the dag state table construction to view `initialState.Dump()` here.
            return initialState.Dag;
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
                var pcd = trueBuilder.Count == c.Decisions.Length ? c : new PartialCaseDecision(c.Index, c.Syntax, trueBuilder.ToImmutableAndFree(), c.Bindings, c.WhenClause, c.CaseLabel);
                whenTrueBuilder.Add(pcd);
            }

            if (falseBuilder != null)
            {
                var pcd = falseBuilder.Count == c.Decisions.Length ? c : new PartialCaseDecision(c.Index, c.Syntax, falseBuilder.ToImmutableAndFree(), c.Bindings, c.WhenClause, c.CaseLabel);
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
        /// <param name="trueDecisionImpliesTrueOther">set if d being true means other has been proven true</param>
        /// <param name="falseDecisionImpliesTrueOther">set if d being false means other has been proven true</param>
        private void CheckConsistentDecision(
            BoundDagDecision d,
            BoundDagDecision other,
            SyntaxNode syntax,
            out bool trueDecisionPermitsTrueOther,
            out bool falseDecisionPermitsTrueOther,
            out bool trueDecisionImpliesTrueOther,
            out bool falseDecisionImpliesTrueOther)
        {
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
                        case BoundNonNullDecision n2:
                            trueDecisionImpliesTrueOther = true;
                            falseDecisionPermitsTrueOther = false;
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
                            // PROTOTYPE(patterns2): specifically, each value requires the input be a particular type, and a known type may indicate that is impossible.
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
            return cases.SelectAsArray((c, eval) => RemoveEvaluation(c, eval), e);
        }

        private static PartialCaseDecision RemoveEvaluation(PartialCaseDecision c, BoundDagEvaluation e)
        {
            return new PartialCaseDecision(
                Index: c.Index,
                Syntax: c.Syntax,
                Decisions: c.Decisions.WhereAsArray(d => !(d is BoundDagEvaluation e2) || e2 != e),
                Bindings: c.Bindings, WhenClause: c.WhenClause, CaseLabel: c.CaseLabel);
        }

        /// <summary>
        /// The state at a given node of the decision acyclic graph. This is used during computation of the state machine,
        /// and contains a representation of the meaning of the state.
        /// </summary>
        private class DagState
        {
            public readonly ImmutableArray<PartialCaseDecision> Cases;

            public DagState(ImmutableArray<PartialCaseDecision> cases)
            {
                this.Cases = cases;
            }

            public static implicit operator DagState(ImmutableArray<PartialCaseDecision> cases) => new DagState(cases);

            // We only compute the dag states for the branches after we de-dup this DagState itself.
            // If all that remains is the `when` clauses, SelectedDecision is left `null` and the
            // FalseBranch field is populated with the successor on failure of the when clause (if one exists).
            public BoundDagDecision SelectedDecision;
            public DagState TrueBranch, FalseBranch;

            // After the entire graph of DagState objects is complete, we translate each into its Dag.
            public BoundDecisionDag Dag;

            // Compute a decision to use at the root of the generated decision tree.
            internal BoundDagDecision ComputeSelectedDecision()
            {
                // Our simple heuristic is to perform the first test of the first possible matched case
                var choice = Cases[0].Decisions[0];

                // But if that test is a null check, it would be redundant with a following
                // type test. We apply this refinement only when there is exactly one case, because
                // when there are multiple cases the null check is likely to be shared.
                if (choice.Kind == BoundKind.NonNullDecision &&
                    Cases.Length == 1 &&
                    Cases[0].Decisions.Length > 1)
                {
                    var choice2 = Cases[0].Decisions[1];
                    if (choice2.Kind == BoundKind.TypeDecision)
                    {
                        return choice2;
                    }
                }

                return choice;
            }

#if DEBUG
            /// <summary>
            /// Starting with `this` state, produce a human-readable description of the state tables.
            /// This is very useful for debugging and optimizing the dag state construction.
            /// </summary>
            internal string Dump()
            {
                var printed = PooledHashSet<DagState>.GetInstance();

                int nextStateNumber = 0;
                var workQueue = ArrayBuilder<DagState>.GetInstance();
                var stateIdentifierMap = PooledDictionary<DagState, int>.GetInstance();
                int stateIdentifier(DagState state)
                {
                    if (stateIdentifierMap.TryGetValue(state, out int value))
                    {
                        return value;
                    }
                    else
                    {
                        value = stateIdentifierMap[state] = ++nextStateNumber;
                        workQueue.Push(state);
                    }

                    return value;
                }

                int nextTempNumber = 0;
                var tempIdentifierMap = PooledDictionary<BoundDagEvaluation, int>.GetInstance();
                int tempIdentifier(BoundDagEvaluation e)
                {
                    return (e == null) ? 0 : tempIdentifierMap.TryGetValue(e, out int value) ? value : tempIdentifierMap[e] = ++nextTempNumber;
                }

                string tempName(BoundDagTemp t)
                {
                    return $"t{tempIdentifier(t.Source)}{(t.Index != 0 ? $".{t.Index.ToString()}" : "")}";
                }

                var resultBuilder = PooledStringBuilder.GetInstance();
                var result = resultBuilder.Builder;
                stateIdentifier(this); // push the start node onto the work queue
                while (workQueue.Count != 0)
                {
                    var state = workQueue.Pop();
                    if (!printed.Add(state))
                    {
                        continue;
                    }

                    result.AppendLine($"State " + stateIdentifier(state));
                    foreach (PartialCaseDecision cd in state.Cases)
                    {
                        result.Append($"  [{cd.Syntax}]");
                        foreach (BoundDagDecision d in cd.Decisions)
                        {
                            result.Append($" {dump(d)}");
                        }

                        result.AppendLine();
                    }

                    if (state.SelectedDecision != null)
                    {
                        result.AppendLine($"  Decision: {dump(state.SelectedDecision)}");
                    }

                    if (state.TrueBranch != null)
                    {
                        result.AppendLine($"  TrueBranch: {stateIdentifier(state.TrueBranch)}");
                    }

                    if (state.FalseBranch != null)
                    {
                        result.AppendLine($"  FalseBranch: {stateIdentifier(state.FalseBranch)}");
                    }
                }

                workQueue.Free();
                printed.Free();
                stateIdentifierMap.Free();
                tempIdentifierMap.Free();
                return resultBuilder.ToStringAndFree();

                string dump(BoundDagDecision d)
                {
                    switch (d)
                    {
                        case BoundDagTypeEvaluation a:
                            return $"t{tempIdentifier(a)}={a.Kind}({a.Type.ToString()})";
                        case BoundDagEvaluation e:
                            return $"t{tempIdentifier(e)}={e.Kind}";
                        case BoundTypeDecision b:
                            return $"?{d.Kind}({b.Type.ToString()}, {tempName(d.Input)})";
                        case BoundNonNullValueDecision v:
                            return $"?{d.Kind}({v.Value.ToString()}, {tempName(d.Input)})";
                        default:
                            return $"?{d.Kind}({tempName(d.Input)})";
                    }
                }
            }
#endif
        }

        /// <summary>
        /// An equivalence relation between dag states used to dedup the states during dag construction.
        /// After dag construction is complete we treat a DagState as using object equality as equivalent
        /// states have been merged.
        /// </summary>
        private class DagStateEquivalence : IEqualityComparer<DagState>
        {
            public static readonly DagStateEquivalence Instance = new DagStateEquivalence();

            public bool Equals(DagState x, DagState y)
            {
                return x.Cases.SequenceEqual(y.Cases, (a, b) => a.Equals(b));
            }

            public int GetHashCode(DagState x)
            {
                return Hash.Combine(Hash.CombineValues(x.Cases), x.Cases.Length);
            }
        }

        private sealed class PartialCaseDecision
        {
            /// <summary>
            /// A number that is distinct for each case and monotinically increasing from earlier to later cases.
            /// </summary>
            public readonly int Index;
            public readonly SyntaxNode Syntax;
            public readonly ImmutableArray<BoundDagDecision> Decisions;
            public readonly ImmutableArray<(BoundExpression, BoundDagTemp)> Bindings;
            public readonly BoundExpression WhenClause;
            public readonly LabelSymbol CaseLabel;
            public PartialCaseDecision(
                int Index,
                SyntaxNode Syntax,
                ImmutableArray<BoundDagDecision> Decisions,
                ImmutableArray<(BoundExpression, BoundDagTemp)> Bindings,
                BoundExpression WhenClause,
                LabelSymbol CaseLabel)
            {
                this.Index = Index;
                this.Syntax = Syntax;
                this.Decisions = Decisions;
                this.Bindings = Bindings;
                this.WhenClause = WhenClause;
                this.CaseLabel = CaseLabel;
            }

            public override bool Equals(object obj)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public bool Equals(PartialCaseDecision other)
            {
                // We do not include Syntax, Bindings, WhereClause, or CaseLabel
                // because once the Index is the same, those must be the same too.
                return other != null && this.Index == other.Index && this.Decisions.SequenceEqual(other.Decisions, SameDecision);
            }

            private static bool SameDecision(BoundDagDecision x, BoundDagDecision y)
            {
                if (x.Input != y.Input || x.Kind != y.Kind)
                {
                    return false;
                }

                switch (x.Kind)
                {
                    case BoundKind.TypeDecision:
                        return ((BoundTypeDecision)x).Type == ((BoundTypeDecision)y).Type;

                    case BoundKind.NonNullValueDecision:
                        return ((BoundNonNullValueDecision)x).Value == ((BoundNonNullValueDecision)y).Value;

                    default:
                        return true;
                }
            }

            public override int GetHashCode()
            {
                int result = Hash.Combine(Decisions.Length, Index);
                foreach (var d in Decisions)
                {
                    result = Hash.Combine((int)d.Kind, result);
                }

                return result;
            }
        }
    }
}
