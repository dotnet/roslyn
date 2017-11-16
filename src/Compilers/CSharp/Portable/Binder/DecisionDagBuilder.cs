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
        private readonly Conversions Conversions;
        private readonly TypeSymbol BooleanType;
        private readonly TypeSymbol ObjectType;
        private HashSet<DiagnosticInfo> discardedUseSiteDiagnostics;

        internal DecisionDagBuilder(CSharpCompilation compilation)
        {
            this.Conversions = compilation.Conversions;
            this.BooleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            this.ObjectType = compilation.GetSpecialType(SpecialType.System_Object);
        }

        /// <summary>
        /// Used to translate the pattern of an is-pattern expression.
        /// </summary>
        public BoundDagTemp TranslatePattern(
            BoundExpression input,
            BoundPattern pattern,
            out ImmutableArray<BoundDagDecision> decisions,
            out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings)
        {
            var rootIdentifier = new BoundDagTemp(input.Syntax, input.Type, null, 0);
            MakeAndSimplifyDecisionsAndBindings(rootIdentifier, pattern, out decisions, out bindings);
            return rootIdentifier;
        }

        /// <summary>
        /// Used to create a decision dag for a switch statement.
        /// </summary>
        /// <param name="syntax"></param>
        /// <param name="switchExpression"></param>
        /// <param name="switchSections"></param>
        /// <param name="defaultLabel"></param>
        /// <returns></returns>
        public BoundDecisionDag CreateDecisionDag(
            SyntaxNode syntax,
            BoundExpression switchExpression,
            ImmutableArray<BoundPatternSwitchSection> switchSections,
            LabelSymbol defaultLabel)
        {
            ImmutableArray<PartialCaseDecision> cases = MakeCases(switchExpression, switchSections);
            BoundDecisionDag dag = MakeDecisionDag(syntax, cases, defaultLabel);
            return dag;
        }

        private ImmutableArray<PartialCaseDecision> MakeCases(BoundExpression switchExpression, ImmutableArray<BoundPatternSwitchSection> switchSections)
        {
            var rootIdentifier = new BoundDagTemp(switchExpression.Syntax, switchExpression.Type, null, 0);
            int i = 0;
            var builder = ArrayBuilder<PartialCaseDecision>.GetInstance();
            foreach (var section in switchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    builder.Add(MakePartialCaseDecision(++i, rootIdentifier, label));
                }
            }

            return builder.ToImmutableAndFree();
        }

        private PartialCaseDecision MakePartialCaseDecision(int index, BoundDagTemp input, BoundPatternSwitchLabel label)
        {
            MakeAndSimplifyDecisionsAndBindings(input, label.Pattern, out var decisions, out var bindings);
            return new PartialCaseDecision(index, label.Syntax, decisions, bindings, label.Guard, label.Label);
        }

        private void MakeAndSimplifyDecisionsAndBindings(
            BoundDagTemp input,
            BoundPattern pattern,
            out ImmutableArray<BoundDagDecision> decisions,
            out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings)
        {
            var decisionsBuilder = ArrayBuilder<BoundDagDecision>.GetInstance();
            var bindingsBuilder = ArrayBuilder<(BoundExpression, BoundDagTemp)>.GetInstance();
            // use site diagnostics will have been produced during binding of the patterns, so can be discarded here
            HashSet<DiagnosticInfo> discardedUseSiteDiagnostics = null;
            MakeDecisionsAndBindings(input, pattern, decisionsBuilder, bindingsBuilder, ref discardedUseSiteDiagnostics);

            // Now simplify the decisions and bindings. We don't need anything in decisions that does not
            // contribute to the result. This will, for example, permit us to match `(2, 3) is (2, _)` without
            // fetching `Item2` from the input.
            var usedValues = PooledHashSet<BoundDagEvaluation>.GetInstance();
            foreach (var (_, temp) in bindingsBuilder)
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
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindings,
            ref HashSet<DiagnosticInfo> discardedUseSiteDiagnostics)
        {
            switch (pattern)
            {
                case BoundDeclarationPattern declaration:
                    MakeDecisionsAndBindings(input, declaration, decisions, bindings, ref discardedUseSiteDiagnostics);
                    break;
                case BoundConstantPattern constant:
                    MakeDecisionsAndBindings(input, constant, decisions, bindings, ref discardedUseSiteDiagnostics);
                    break;
                case BoundDiscardPattern wildcard:
                    // Nothing to do. It always matches.
                    break;
                case BoundRecursivePattern recursive:
                    MakeDecisionsAndBindings(input, recursive, decisions, bindings, ref discardedUseSiteDiagnostics);
                    break;
                default:
                    throw new NotImplementedException(pattern.Kind.ToString());
            }
        }

        private void MakeDecisionsAndBindings(
            BoundDagTemp input,
            BoundDeclarationPattern declaration,
            ArrayBuilder<BoundDagDecision> decisions,
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindings,
            ref HashSet<DiagnosticInfo> discardedUseSiteDiagnostics)
        {
            var type = declaration.DeclaredType.Type;
            var syntax = declaration.Syntax;

            // Add a null and type test if needed.
            if (!declaration.IsVar)
            {
                NullCheck(input, declaration.Syntax, decisions);
                input = ConvertToType(input, declaration.Syntax, type, decisions, ref discardedUseSiteDiagnostics);
            }

            var left = declaration.VariableAccess;
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
            ArrayBuilder<BoundDagDecision> decisions,
            ref HashSet<DiagnosticInfo> discardedUseSiteDiagnostics)
        {
            if (input.Type != type)
            {
                var inputType = input.Type.StrippedType(); // since a null check has already been done
                var conversion = Conversions.ClassifyBuiltInConversion(inputType, type, ref discardedUseSiteDiagnostics);
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
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindings,
            ref HashSet<DiagnosticInfo> discardedUseSiteDiagnostics)
        {
            input = ConvertToType(input, constant.Syntax, constant.Value.Type, decisions, ref discardedUseSiteDiagnostics);
            decisions.Add(new BoundValueDecision(constant.Syntax, constant.ConstantValue, input));
        }

        private void MakeDecisionsAndBindings(
            BoundDagTemp input,
            BoundRecursivePattern recursive,
            ArrayBuilder<BoundDagDecision> decisions,
            ArrayBuilder<(BoundExpression, BoundDagTemp)> bindings,
            ref HashSet<DiagnosticInfo> discardedUseSiteDiagnostics)
        {
            Debug.Assert(input.Type.IsErrorType() || input.Type == recursive.InputType);
            NullCheck(input, recursive.Syntax, decisions);
            if (recursive.DeclaredType != null && recursive.DeclaredType.Type != input.Type)
            {
                input = ConvertToType(input, recursive.Syntax, recursive.DeclaredType.Type, decisions, ref discardedUseSiteDiagnostics);
            }

            if (!recursive.Deconstruction.IsDefault)
            {
                // we have a "deconstruction" form, which is either an invocation of a Deconstruct method, or a disassembly of a tuple
                if (recursive.DeconstructMethodOpt != null)
                {
                    var method = recursive.DeconstructMethodOpt;
                    var evaluation = new BoundDagDeconstructEvaluation(recursive.Syntax, method, input);
                    decisions.Add(evaluation);
                    int extensionExtra = method.IsStatic ? 1 : 0;
                    int count = Math.Min(method.ParameterCount - extensionExtra, recursive.Deconstruction.Length);
                    for (int i = 0; i < count; i++)
                    {
                        var pattern = recursive.Deconstruction[i];
                        var syntax = pattern.Syntax;
                        var output = new BoundDagTemp(syntax, method.Parameters[i + extensionExtra].Type, evaluation, i);
                        MakeDecisionsAndBindings(output, pattern, decisions, bindings, ref discardedUseSiteDiagnostics);
                    }
                }
                else if (input.Type.IsTupleType)
                {
                    var elements = input.Type.TupleElements;
                    var elementTypes = input.Type.TupleElementTypes;
                    int count = Math.Min(elementTypes.Length, recursive.Deconstruction.Length);
                    for (int i = 0; i < count; i++)
                    {
                        var pattern = recursive.Deconstruction[i];
                        var syntax = pattern.Syntax;
                        var field = elements[i];
                        var evaluation = new BoundDagFieldEvaluation(syntax, field, input); // fetch the ItemN field
                        decisions.Add(evaluation);
                        var output = new BoundDagTemp(syntax, field.Type, evaluation, 0);
                        MakeDecisionsAndBindings(output, pattern, decisions, bindings, ref discardedUseSiteDiagnostics);
                    }
                }
                else
                {
                    // TODO(patterns2): This should not occur except in error cases. Perhaps this will be used to handle the ITuple case.
                    Debug.Assert(recursive.HasAnyErrors);
                }
            }

            if (recursive.PropertiesOpt != null)
            {
                // we have a "property" form
                for (int i = 0; i < recursive.PropertiesOpt.Length; i++)
                {
                    var prop = recursive.PropertiesOpt[i];
                    var symbol = prop.symbol;
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
                            Debug.Assert(prop.pattern.HasAnyErrors);
                            continue;
                    }
                    decisions.Add(evaluation);
                    var output = new BoundDagTemp(prop.pattern.Syntax, prop.symbol.GetTypeOrReturnType(), evaluation, 0);
                    MakeDecisionsAndBindings(output, prop.pattern, decisions, bindings, ref discardedUseSiteDiagnostics);
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

            var first = cases[0];
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
                        return new BoundWhereClause(first.Syntax, first.Bindings, null, new BoundDecision(first.Syntax, first.CaseLabel), null);
                    }
                }
                else
                {
                    // in case the where clause fails, we prepare for the remaining cases.
                    var remainingCases = cases.WhereAsArray(d => d != first);
                    var whereFails = MakeDecisionDag(syntax, remainingCases, defaultLabel);
                    return new BoundWhereClause(first.Syntax, first.Bindings, first.WhereClause, new BoundDecision(first.Syntax, first.CaseLabel), whereFails);
                }
            }
            else
            {
                switch (first.Decisions[0])
                {
                    case BoundDagEvaluation e:
                        var tail = MakeDecisionDag(syntax, RemoveEvaluation(cases, e), defaultLabel);
                        return new BoundEvaluationPoint(syntax, e, tail);
                    case BoundDagDecision d:
                        SplitCases(cases, d, out var whenTrueDecisions, out var whenFalseDecisions);
                        var whenTrue = MakeDecisionDag(syntax, whenTrueDecisions, defaultLabel);
                        var whenFalse = MakeDecisionDag(syntax, whenFalseDecisions, defaultLabel);
                        return new BoundDecisionPoint(syntax, d, whenTrue, whenFalse);
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
            foreach (var c in cases)
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
            foreach (var dd in c.Decisions)
            {
                CheckConsistentDecision(d: d, other: dd,
                    permitsTrue: out bool permitsTrue, permitsFalse: out bool permitsFalse,
                    killsDecisionOnTrueBranch: out bool killsDecisionOnTrueBranch,
                    killsDecisionOnFalseBranch: out bool killsDecisionOnFalseBranch);
                if (permitsTrue)
                {
                    if (d != dd && !killsDecisionOnTrueBranch) trueBuilder?.Add(dd);
                }
                else
                {
                    trueBuilder?.Free();
                    trueBuilder = null;
                }
                if (permitsFalse)
                {
                    Debug.Assert(d != dd);
                    if (!killsDecisionOnFalseBranch) falseBuilder?.Add(dd);
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
        /// set permitsTrue if a true decision on d would permit other to succeed.
        /// set permitsFalse if a false decision on d would permit other to succeed.
        /// sets killsFalseDecision when d being false means other has been proven true
        /// </summary>
        private void CheckConsistentDecision(
            BoundDagDecision d,
            BoundDagDecision other,
            out bool permitsTrue,
            out bool permitsFalse,
            out bool killsDecisionOnTrueBranch,
            out bool killsDecisionOnFalseBranch)
        {
            // innocent until proven guilty
            permitsTrue = true;
            permitsFalse = true;
            killsDecisionOnTrueBranch = false;
            killsDecisionOnFalseBranch = false;

            // if decisions test unrelated things, there is no implication from one to the other
            if (d.Input != other.Input)
            {
                return;
            }

            // a test cannot be both true and false
            if (d == other)
            {
                permitsFalse = false;
                killsDecisionOnTrueBranch = true;
                return;
            }

            switch (d)
            {
                case BoundNonNullDecision n1:
                    switch (other)
                    {
                        case BoundValueDecision v2:
                            if (v2.Value == ConstantValue.Null)
                            {
                                // once v!=null is false, we know v==null is true and do not need to test it
                                permitsTrue = false;
                                killsDecisionOnFalseBranch = true;
                            }
                            else
                            {
                                // Given that v!=null fails, v==K might succeed
                                permitsFalse = false;
                            }
                            break;
                        default:
                            // Once v!=null fails, it must fail a type test
                            permitsFalse = false;
                            break;
                    }
                    break;
                case BoundTypeDecision t1:
                    switch (other)
                    {
                        case BoundNonNullDecision n2:
                            // Once `v is T` is true, v!=null cannot fail
                            permitsFalse = false;
                            killsDecisionOnTrueBranch = true;
                            break;
                        case BoundTypeDecision t2:
                            // If T1 could never be T2, then success of T1 implies failure of T2.
                            var matchPossible = Binder.ExpressionOfTypeMatchesPatternType(Conversions, t1.Type, t2.Type, ref discardedUseSiteDiagnostics, out _);
                            if (matchPossible == false) permitsTrue = false;
                            // If every T2 is a T1, then failure of T1 implies failure of T2.
                            matchPossible = Binder.ExpressionOfTypeMatchesPatternType(Conversions, t2.Type, t1.Type, ref discardedUseSiteDiagnostics, out _);
                            if (matchPossible == true)
                            {
                                // Once we know it is of the subtype, we do not need to test for the supertype.
                                permitsFalse = false;
                                killsDecisionOnTrueBranch = true;
                            }
                            break;
                        case BoundValueDecision v2:
                            break;
                    }
                    break;
                case BoundValueDecision v1:
                    switch (other)
                    {
                        case BoundNonNullDecision n2:
                            if (v1.Value == ConstantValue.Null)
                            {
                                // once v==null is false, we know v!=null is true and do not need to test it
                                permitsTrue = false;
                                killsDecisionOnFalseBranch = true;
                            }
                            else
                            {
                                // once v==K is true, we know v!=null is true and do not need to test it
                                permitsFalse = false;
                                killsDecisionOnTrueBranch = true;
                            }
                            break;
                        case BoundTypeDecision t2:
                            if (v1.Value == ConstantValue.Null) permitsTrue = false;
                            break;
                        case BoundValueDecision v2:
                            Debug.Assert(v1.Value != v2.Value);
                            permitsTrue = false;
                            if (v1.Input.Type.SpecialType == SpecialType.System_Boolean)
                            {
                                // As a special case, we note that boolean values can only ever be true or false.
                                // However, in order to exclude bad constant values, we check the values.
                                if (v1.Value == ConstantValue.True && v2.Value == ConstantValue.False ||
                                    v1.Value == ConstantValue.False && v2.Value == ConstantValue.True)
                                {
                                    killsDecisionOnFalseBranch = true;
                                }
                            }
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

    partial class BoundDagEvaluation
    {
        public override bool Equals(object obj) => obj is BoundDagEvaluation other && this.Equals(other);
        public bool Equals(BoundDagEvaluation other)
        {
            return other != (object)null && this.Kind == other.Kind && this.Input.Equals(other.Input) && this.Symbol == other.Symbol;
        }
        private Symbol Symbol
        {
            get
            {
                switch (this)
                {
                    case BoundDagFieldEvaluation e: return e.Field;
                    case BoundDagPropertyEvaluation e: return e.Property;
                    case BoundDagTypeEvaluation e: return e.Type;
                    case BoundDagDeconstructEvaluation e: return e.DeconstructMethod;
                    default: throw ExceptionUtilities.UnexpectedValue(this.Kind);
                }
            }
        }
        public override int GetHashCode()
        {
            return this.Input.GetHashCode() ^ (this.Symbol?.GetHashCode() ?? 0);
        }
        public static bool operator ==(BoundDagEvaluation left, BoundDagEvaluation right)
        {
            return (left == (object)null) ? right == (object)null : left.Equals(right);
        }
        public static bool operator !=(BoundDagEvaluation left, BoundDagEvaluation right)
        {
            return !(left == right);
        }
    }

    partial class BoundDagTemp
    {
        public override bool Equals(object obj) => obj is BoundDagTemp other && this.Equals(other);
        public bool Equals(BoundDagTemp other)
        {
            return other != (object)null && this.Type == other.Type && object.Equals(this.Source, other.Source) && this.Index == other.Index;
        }
        public override int GetHashCode()
        {
            return this.Type.GetHashCode() ^ (this.Source?.GetHashCode() ?? 0) ^ this.Index;
        }
        public static bool operator ==(BoundDagTemp left, BoundDagTemp right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(BoundDagTemp left, BoundDagTemp right)
        {
            return !left.Equals(right);
        }
    }
}
