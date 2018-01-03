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

        internal DecisionDagBuilder(CSharpCompilation compilation)
        {
            this.Conversions = compilation.Conversions;
            this.BooleanType = compilation.GetSpecialType(SpecialType.System_Boolean);
            this.ObjectType = compilation.GetSpecialType(SpecialType.System_Object);
        }

        public BoundDecisionDag CreateDecisionDag(BoundPatternSwitchStatement statement)
        {
            ImmutableArray<PartialCaseDecision> cases = MakeCases(statement);
            BoundDecisionDag dag = MakeDecisionDag(cases);
            return dag;
        }

        public BoundDagTemp LowerPattern(
            BoundExpression input,
            BoundPattern pattern,
            out ImmutableArray<BoundDagDecision> decisions,
            out ImmutableArray<(BoundExpression, BoundDagTemp)> bindings)
        {
            var rootIdentifier = new BoundDagTemp(input.Syntax, input.Type, null, 0);
            MakeAndSimplifyDecisionsAndBindings(rootIdentifier, pattern, out decisions, out bindings);
            return rootIdentifier;
        }

        private ImmutableArray<PartialCaseDecision> MakeCases(BoundPatternSwitchStatement statement)
        {
            var rootIdentifier = new BoundDagTemp(statement.Expression.Syntax, statement.Expression.Type, null, 0);
            int i = 0;
            var builder = ArrayBuilder<PartialCaseDecision>.GetInstance();
            foreach (var section in statement.SwitchSections)
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
            return new PartialCaseDecision(index, decisions, bindings, label.Guard, label.Label);
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
            Debug.Assert(input.Type == recursive.InputType);
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
                    throw new NotImplementedException();
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
                            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
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

        private static BoundDecisionDag MakeDecisionDag(ImmutableArray<PartialCaseDecision> cases)
        {
            throw new NotImplementedException();
        }
    }

    internal class PartialCaseDecision
    {
        public readonly int Index;
        public readonly ImmutableArray<BoundDagDecision> Decisions;
        public readonly ImmutableArray<(BoundExpression, BoundDagTemp)> Bindings;
        public readonly BoundExpression WhereClause;
        public readonly LabelSymbol CaseLabel;
        public PartialCaseDecision(
            int Index,
            ImmutableArray<BoundDagDecision> Decisions,
            ImmutableArray<(BoundExpression, BoundDagTemp)> Bindings,
            BoundExpression WhereClause,
            LabelSymbol CaseLabel)
        {
            this.Index = Index;
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
            return !left.Equals(right);
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
