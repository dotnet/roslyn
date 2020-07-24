// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// <para>
    /// A utility class for making a decision dag (directed acyclic graph) for a pattern-matching construct.
    /// A decision dag is represented by
    /// the class <see cref="BoundDecisionDag"/> and is a representation of a finite state automaton that performs a
    /// sequence of binary tests. Each node is represented by a <see cref="BoundDecisionDagNode"/>. There are four
    /// kind of nodes: <see cref="BoundTestDecisionDagNode"/> performs one of the binary tests;
    /// <see cref="BoundEvaluationDecisionDagNode"/> simply performs some computation and stores it in one or more
    /// temporary variables for use in subsequent nodes (think of it as a node with a single successor);
    /// <see cref="BoundWhenDecisionDagNode"/> represents the test performed by evaluating the expression of the
    /// when-clause of a switch case; and <see cref="BoundLeafDecisionDagNode"/> represents a leaf node when we
    /// have finally determined exactly which case matches. Each test processes a single input, and there are
    /// four kinds:<see cref="BoundDagExplicitNullTest"/> tests a value for null; <see cref="BoundDagNonNullTest"/>
    /// tests that a value is not null; <see cref="BoundDagTypeTest"/> checks if the value is of a given type;
    /// and <see cref="BoundDagValueTest"/> checks if the value is equal to a given constant. Of the evaluations,
    /// there are <see cref="BoundDagDeconstructEvaluation"/> which represents an invocation of a type's
    /// "Deconstruct" method; <see cref="BoundDagFieldEvaluation"/> reads a field; <see cref="BoundDagPropertyEvaluation"/>
    /// reads a property; and <see cref="BoundDagTypeEvaluation"/> converts a value from one type to another (which
    /// is performed only after testing that the value is of that type).
    /// </para>
    /// <para>
    /// In order to build this automaton, we start (in
    /// <see cref="MakeBoundDecisionDag(SyntaxNode, ImmutableArray{DecisionDagBuilder.StateForCase})"/>
    /// by computing a description of the initial state in a <see cref="DagState"/>, and then
    /// for each such state description we decide what the test or evaluation will be at
    /// that state, and compute the successor state descriptions.
    /// A state description represented by a <see cref="DagState"/> is a collection of partially matched
    /// cases represented
    /// by <see cref="StateForCase"/>, in which some number of the tests have already been performed
    /// for each case.
    /// When we have computed <see cref="DagState"/> descriptions for all of the states, we create a new
    /// <see cref="BoundDecisionDagNode"/> for each of them, containing
    /// the state transitions (including the test to perform at each node and the successor nodes) but
    /// not the state descriptions. A <see cref="BoundDecisionDag"/> containing this
    /// set of nodes becomes part of the bound nodes (e.g. in <see cref="BoundSwitchStatement"/> and
    /// <see cref="BoundUnconvertedSwitchExpression"/>) and is used for semantic analysis and lowering.
    /// </para>
    /// </summary>
    internal sealed class DecisionDagBuilder
    {
        private readonly CSharpCompilation _compilation;
        private readonly Conversions _conversions;
        private readonly DiagnosticBag _diagnostics;
        private readonly LabelSymbol _defaultLabel;

        private DecisionDagBuilder(CSharpCompilation compilation, LabelSymbol defaultLabel, DiagnosticBag diagnostics)
        {
            this._compilation = compilation;
            this._conversions = compilation.Conversions;
            _diagnostics = diagnostics;
            _defaultLabel = defaultLabel;
        }

        /// <summary>
        /// Create a decision dag for a switch statement.
        /// </summary>
        public static BoundDecisionDag CreateDecisionDagForSwitchStatement(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression switchGoverningExpression,
            ImmutableArray<BoundSwitchSection> switchSections,
            LabelSymbol defaultLabel,
            DiagnosticBag diagnostics)
        {
            var builder = new DecisionDagBuilder(compilation, defaultLabel, diagnostics);
            return builder.CreateDecisionDagForSwitchStatement(syntax, switchGoverningExpression, switchSections);
        }

        /// <summary>
        /// Create a decision dag for a switch expression.
        /// </summary>
        public static BoundDecisionDag CreateDecisionDagForSwitchExpression(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression switchExpressionInput,
            ImmutableArray<BoundSwitchExpressionArm> switchArms,
            LabelSymbol defaultLabel,
            DiagnosticBag diagnostics)
        {
            var builder = new DecisionDagBuilder(compilation, defaultLabel, diagnostics);
            return builder.CreateDecisionDagForSwitchExpression(syntax, switchExpressionInput, switchArms);
        }

        /// <summary>
        /// Translate the pattern of an is-pattern expression.
        /// </summary>
        public static BoundDecisionDag CreateDecisionDagForIsPattern(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression inputExpression,
            BoundPattern pattern,
            LabelSymbol whenTrueLabel,
            LabelSymbol whenFalseLabel,
            DiagnosticBag diagnostics)
        {
            var builder = new DecisionDagBuilder(compilation, defaultLabel: whenFalseLabel, diagnostics);
            return builder.CreateDecisionDagForIsPattern(syntax, inputExpression, pattern, whenTrueLabel);
        }

        private BoundDecisionDag CreateDecisionDagForIsPattern(
            SyntaxNode syntax,
            BoundExpression inputExpression,
            BoundPattern pattern,
            LabelSymbol whenTrueLabel)
        {
            var rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);
            return MakeBoundDecisionDag(syntax, ImmutableArray.Create(MakeTestsForPattern(index: 1, pattern.Syntax, rootIdentifier, pattern, whenClause: null, whenTrueLabel)));
        }

        private BoundDecisionDag CreateDecisionDagForSwitchStatement(
            SyntaxNode syntax,
            BoundExpression switchGoverningExpression,
            ImmutableArray<BoundSwitchSection> switchSections)
        {
            var rootIdentifier = BoundDagTemp.ForOriginalInput(switchGoverningExpression);
            int i = 0;
            var builder = ArrayBuilder<StateForCase>.GetInstance(switchSections.Length);
            foreach (BoundSwitchSection section in switchSections)
            {
                foreach (BoundSwitchLabel label in section.SwitchLabels)
                {
                    if (label.Syntax.Kind() != SyntaxKind.DefaultSwitchLabel)
                    {
                        builder.Add(MakeTestsForPattern(++i, label.Syntax, rootIdentifier, label.Pattern, label.WhenClause, label.Label));
                    }
                }
            }

            return MakeBoundDecisionDag(syntax, builder.ToImmutableAndFree());
        }

        /// <summary>
        /// Used to create a decision dag for a switch expression.
        /// </summary>
        private BoundDecisionDag CreateDecisionDagForSwitchExpression(
            SyntaxNode syntax,
            BoundExpression switchExpressionInput,
            ImmutableArray<BoundSwitchExpressionArm> switchArms)
        {
            var rootIdentifier = BoundDagTemp.ForOriginalInput(switchExpressionInput);
            int i = 0;
            var builder = ArrayBuilder<StateForCase>.GetInstance(switchArms.Length);
            foreach (BoundSwitchExpressionArm arm in switchArms)
                builder.Add(MakeTestsForPattern(++i, arm.Syntax, rootIdentifier, arm.Pattern, arm.WhenClause, arm.Label));

            return MakeBoundDecisionDag(syntax, builder.ToImmutableAndFree());
        }

        /// <summary>
        /// Compute the set of remaining tests for a pattern.
        /// </summary>
        private StateForCase MakeTestsForPattern(
            int index,
            SyntaxNode syntax,
            BoundDagTemp input,
            BoundPattern pattern,
            BoundExpression? whenClause,
            LabelSymbol label)
        {
            Tests tests = MakeAndSimplifyTestsAndBindings(input, pattern, out ImmutableArray<BoundPatternBinding> bindings);
            return new StateForCase(index, syntax, tests, bindings, whenClause, label);
        }

        private Tests MakeAndSimplifyTestsAndBindings(
            BoundDagTemp input,
            BoundPattern pattern,
            out ImmutableArray<BoundPatternBinding> bindings)
        {
            var bindingsBuilder = ArrayBuilder<BoundPatternBinding>.GetInstance();
            Tests tests = MakeTestsAndBindings(input, pattern, bindingsBuilder);
            tests = SimplifyTestsAndBindings(tests, bindingsBuilder);
            bindings = bindingsBuilder.ToImmutableAndFree();
            return tests;
        }

        private static Tests SimplifyTestsAndBindings(
            Tests tests,
            ArrayBuilder<BoundPatternBinding> bindingsBuilder)
        {
            // Now simplify the tests and bindings. We don't need anything in tests that does not
            // contribute to the result. This will, for example, permit us to match `(2, 3) is (2, _)` without
            // fetching `Item2` from the input.
            var usedValues = PooledHashSet<BoundDagEvaluation>.GetInstance();
            foreach (BoundPatternBinding binding in bindingsBuilder)
            {
                BoundDagTemp temp = binding.TempContainingValue;
                if (temp.Source is { })
                {
                    usedValues.Add(temp.Source);
                }
            }

            var result = scanAndSimplify(tests);
            usedValues.Free();
            return result;

            Tests scanAndSimplify(Tests tests)
            {
                switch (tests)
                {
                    case Tests.SequenceTests seq:
                        var testSequence = seq.RemainingTests;
                        var length = testSequence.Length;
                        var newSequence = ArrayBuilder<Tests>.GetInstance(length);
                        newSequence.AddRange(testSequence);
                        for (int i = length - 1; i >= 0; i--)
                        {
                            newSequence[i] = scanAndSimplify(newSequence[i]);
                        }
                        return seq.Update(newSequence);
                    case Tests.True _:
                    case Tests.False _:
                        return tests;
                    case Tests.One(BoundDagEvaluation e):
                        if (usedValues.Contains(e))
                        {
                            if (e.Input.Source is { })
                                usedValues.Add(e.Input.Source);
                            return tests;
                        }
                        else
                        {
                            return Tests.True.Instance;
                        }
                    case Tests.One(BoundDagTest d):
                        if (d.Input.Source is { })
                            usedValues.Add(d.Input.Source);
                        return tests;
                    case Tests.Not n:
                        return Tests.Not.Create(scanAndSimplify(n.Negated));
                    default:
                        throw ExceptionUtilities.UnexpectedValue(tests);
                }
            }
        }

        private Tests MakeTestsAndBindings(
            BoundDagTemp input,
            BoundPattern pattern,
            ArrayBuilder<BoundPatternBinding> bindings)
        {
            return MakeTestsAndBindings(input, pattern, out _, bindings);
        }

        /// <summary>
        /// Make the tests and variable bindings for the given pattern with the given input.  The pattern's
        /// "output" value is placed in <paramref name="output"/>.  The output is defined as the input
        /// narrowed according to the pattern's *narrowed type*; see https://github.com/dotnet/csharplang/issues/2850.
        /// </summary>
        private Tests MakeTestsAndBindings(
            BoundDagTemp input,
            BoundPattern pattern,
            out BoundDagTemp output,
            ArrayBuilder<BoundPatternBinding> bindings)
        {
            Debug.Assert(pattern.HasErrors || pattern.InputType.Equals(input.Type, TypeCompareKind.AllIgnoreOptions) || pattern.InputType.IsErrorType());
            switch (pattern)
            {
                case BoundDeclarationPattern declaration:
                    return MakeTestsAndBindingsForDeclarationPattern(input, declaration, out output, bindings);
                case BoundConstantPattern constant:
                    return MakeTestsForConstantPattern(input, constant, out output);
                case BoundDiscardPattern _:
                    output = input;
                    return Tests.True.Instance;
                case BoundRecursivePattern recursive:
                    return MakeTestsAndBindingsForRecursivePattern(input, recursive, out output, bindings);
                case BoundITuplePattern iTuple:
                    return MakeTestsAndBindingsForITuplePattern(input, iTuple, out output, bindings);
                case BoundTypePattern type:
                    return MakeTestsForTypePattern(input, type, out output);
                case BoundRelationalPattern rel:
                    return MakeTestsAndBindingsForRelationalPattern(input, rel, out output);
                case BoundNegatedPattern neg:
                    output = input;
                    return MakeTestsAndBindingsForNegatedPattern(input, neg, bindings);
                case BoundBinaryPattern bin:
                    return MakeTestsAndBindingsForBinaryPattern(input, bin, out output, bindings);
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }

        private Tests MakeTestsAndBindingsForITuplePattern(
            BoundDagTemp input,
            BoundITuplePattern pattern,
            out BoundDagTemp output,
            ArrayBuilder<BoundPatternBinding> bindings)
        {
            var syntax = pattern.Syntax;
            var patternLength = pattern.Subpatterns.Length;
            var objectType = this._compilation.GetSpecialType(SpecialType.System_Object);
            var getLengthProperty = (PropertySymbol)pattern.GetLengthMethod.AssociatedSymbol;
            RoslynDebug.Assert(getLengthProperty.Type.SpecialType == SpecialType.System_Int32);
            var getItemProperty = (PropertySymbol)pattern.GetItemMethod.AssociatedSymbol;
            var iTupleType = getLengthProperty.ContainingType;
            RoslynDebug.Assert(iTupleType.Name == "ITuple");
            var tests = ArrayBuilder<Tests>.GetInstance(4 + patternLength * 2);

            tests.Add(new Tests.One(new BoundDagTypeTest(syntax, iTupleType, input)));
            var valueAsITupleEvaluation = new BoundDagTypeEvaluation(syntax, iTupleType, input);
            tests.Add(new Tests.One(valueAsITupleEvaluation));
            var valueAsITuple = new BoundDagTemp(syntax, iTupleType, valueAsITupleEvaluation);
            output = valueAsITuple;

            var lengthEvaluation = new BoundDagPropertyEvaluation(syntax, getLengthProperty, OriginalInput(valueAsITuple, getLengthProperty));
            tests.Add(new Tests.One(lengthEvaluation));
            var lengthTemp = new BoundDagTemp(syntax, this._compilation.GetSpecialType(SpecialType.System_Int32), lengthEvaluation);
            tests.Add(new Tests.One(new BoundDagValueTest(syntax, ConstantValue.Create(patternLength), lengthTemp)));

            var getItemPropertyInput = OriginalInput(valueAsITuple, getItemProperty);
            for (int i = 0; i < patternLength; i++)
            {
                var indexEvaluation = new BoundDagIndexEvaluation(syntax, getItemProperty, i, getItemPropertyInput);
                tests.Add(new Tests.One(indexEvaluation));
                var indexTemp = new BoundDagTemp(syntax, objectType, indexEvaluation);
                tests.Add(MakeTestsAndBindings(indexTemp, pattern.Subpatterns[i].Pattern, bindings));
            }

            return Tests.AndSequence.Create(tests);
        }

        /// <summary>
        /// Get the earliest input of which the symbol is a member.
        /// A BoundDagTypeEvaluation doesn't change the underlying object being pointed to.
        /// So two evaluations act on the same input so long as they have the same original input.
        /// We use this method to compute the original input for an evaluation.
        /// </summary>
        private BoundDagTemp OriginalInput(BoundDagTemp input, Symbol symbol)
        {
            while (input.Source is BoundDagTypeEvaluation source && IsDerivedType(source.Input.Type, symbol.ContainingType))
            {
                input = source.Input;
            }

            return input;
        }

        bool IsDerivedType(TypeSymbol possibleDerived, TypeSymbol possibleBase)
        {
            HashSet<DiagnosticInfo>? useSiteDiagnostics = null;
            return this._conversions.HasIdentityOrImplicitReferenceConversion(possibleDerived, possibleBase, ref useSiteDiagnostics);
        }

        private Tests MakeTestsAndBindingsForDeclarationPattern(
            BoundDagTemp input,
            BoundDeclarationPattern declaration,
            out BoundDagTemp output,
            ArrayBuilder<BoundPatternBinding> bindings)
        {
            TypeSymbol? type = declaration.DeclaredType?.Type;
            var tests = ArrayBuilder<Tests>.GetInstance(1);

            // Add a null and type test if needed.
            if (!declaration.IsVar)
                input = MakeConvertToType(input, declaration.Syntax, type!, isExplicitTest: false, tests);

            BoundExpression? variableAccess = declaration.VariableAccess;
            if (variableAccess is { })
            {
                Debug.Assert(variableAccess.Type!.Equals(input.Type, TypeCompareKind.AllIgnoreOptions) || variableAccess.Type.IsErrorType());
                bindings.Add(new BoundPatternBinding(variableAccess, input));
            }
            else
            {
                RoslynDebug.Assert(declaration.Variable == null);
            }

            output = input;
            return Tests.AndSequence.Create(tests);
        }

        private Tests MakeTestsForTypePattern(
            BoundDagTemp input,
            BoundTypePattern typePattern,
            out BoundDagTemp output)
        {
            TypeSymbol type = typePattern.DeclaredType.Type;
            var tests = ArrayBuilder<Tests>.GetInstance(4);
            output = MakeConvertToType(input: input, syntax: typePattern.Syntax, type: type, isExplicitTest: typePattern.IsExplicitNotNullTest, tests: tests);
            return Tests.AndSequence.Create(tests);
        }

        private static void MakeCheckNotNull(
            BoundDagTemp input,
            SyntaxNode syntax,
            bool isExplicitTest,
            ArrayBuilder<Tests> tests)
        {
            // Add a null test if needed
            if (input.Type.CanContainNull())
                tests.Add(new Tests.One(new BoundDagNonNullTest(syntax, isExplicitTest, input)));
        }

        /// <summary>
        /// Generate a not-null check and a type check.
        /// </summary>
        private BoundDagTemp MakeConvertToType(
            BoundDagTemp input,
            SyntaxNode syntax,
            TypeSymbol type,
            bool isExplicitTest,
            ArrayBuilder<Tests> tests)
        {
            MakeCheckNotNull(input, syntax, isExplicitTest, tests);
            if (!input.Type.Equals(type, TypeCompareKind.AllIgnoreOptions))
            {
                TypeSymbol inputType = input.Type.StrippedType(); // since a null check has already been done
                HashSet<DiagnosticInfo>? useSiteDiagnostics = null;
                Conversion conversion = _conversions.ClassifyBuiltInConversion(inputType, type, ref useSiteDiagnostics);
                _diagnostics.Add(syntax, useSiteDiagnostics);
                if (input.Type.IsDynamic() ? type.SpecialType == SpecialType.System_Object : conversion.IsImplicit)
                {
                    // type test not needed, only the type cast
                }
                else
                {
                    // both type test and cast needed
                    tests.Add(new Tests.One(new BoundDagTypeTest(syntax, type, input)));
                }

                var evaluation = new BoundDagTypeEvaluation(syntax, type, input);
                input = new BoundDagTemp(syntax, type, evaluation);
                tests.Add(new Tests.One(evaluation));
            }

            return input;
        }

        private Tests MakeTestsForConstantPattern(
            BoundDagTemp input,
            BoundConstantPattern constant,
            out BoundDagTemp output)
        {
            if (constant.ConstantValue == ConstantValue.Null)
            {
                output = input;
                return new Tests.One(new BoundDagExplicitNullTest(constant.Syntax, input));
            }
            else
            {
                var tests = ArrayBuilder<Tests>.GetInstance(2);
                var convertedInput = MakeConvertToType(input, constant.Syntax, constant.Value.Type!, isExplicitTest: false, tests);
                output = convertedInput;
                tests.Add(new Tests.One(new BoundDagValueTest(constant.Syntax, constant.ConstantValue, convertedInput)));
                return Tests.AndSequence.Create(tests);
            }
        }

        private Tests MakeTestsAndBindingsForRecursivePattern(
            BoundDagTemp input,
            BoundRecursivePattern recursive,
            out BoundDagTemp output,
            ArrayBuilder<BoundPatternBinding> bindings)
        {
            RoslynDebug.Assert(input.Type.IsErrorType() || recursive.HasErrors || recursive.InputType.IsErrorType() || input.Type.Equals(recursive.InputType, TypeCompareKind.AllIgnoreOptions));
            var inputType = recursive.DeclaredType?.Type ?? input.Type.StrippedType();
            var tests = ArrayBuilder<Tests>.GetInstance(5);
            output = input = MakeConvertToType(input, recursive.Syntax, inputType, isExplicitTest: recursive.IsExplicitNotNullTest, tests);

            if (!recursive.Deconstruction.IsDefault)
            {
                // we have a "deconstruction" form, which is either an invocation of a Deconstruct method, or a disassembly of a tuple
                if (recursive.DeconstructMethod != null)
                {
                    MethodSymbol method = recursive.DeconstructMethod;
                    var evaluation = new BoundDagDeconstructEvaluation(recursive.Syntax, method, OriginalInput(input, method));
                    tests.Add(new Tests.One(evaluation));
                    int extensionExtra = method.IsStatic ? 1 : 0;
                    int count = Math.Min(method.ParameterCount - extensionExtra, recursive.Deconstruction.Length);
                    for (int i = 0; i < count; i++)
                    {
                        BoundPattern pattern = recursive.Deconstruction[i].Pattern;
                        SyntaxNode syntax = pattern.Syntax;
                        var element = new BoundDagTemp(syntax, method.Parameters[i + extensionExtra].Type, evaluation, i);
                        tests.Add(MakeTestsAndBindings(element, pattern, bindings));
                    }
                }
                else if (Binder.IsZeroElementTupleType(inputType))
                {
                    // Work around https://github.com/dotnet/roslyn/issues/20648: The compiler's internal APIs such as `declType.IsTupleType`
                    // do not correctly treat the non-generic struct `System.ValueTuple` as a tuple type.  We explicitly perform the tests
                    // required to identify it.  When that bug is fixed we should be able to remove this if statement.

                    // nothing to do, as there are no tests for the zero elements of this tuple
                }
                else if (inputType.IsTupleType)
                {
                    ImmutableArray<FieldSymbol> elements = inputType.TupleElements;
                    ImmutableArray<TypeWithAnnotations> elementTypes = inputType.TupleElementTypesWithAnnotations;
                    int count = Math.Min(elementTypes.Length, recursive.Deconstruction.Length);
                    for (int i = 0; i < count; i++)
                    {
                        BoundPattern pattern = recursive.Deconstruction[i].Pattern;
                        SyntaxNode syntax = pattern.Syntax;
                        FieldSymbol field = elements[i];
                        var evaluation = new BoundDagFieldEvaluation(syntax, field, OriginalInput(input, field)); // fetch the ItemN field
                        tests.Add(new Tests.One(evaluation));
                        var element = new BoundDagTemp(syntax, field.Type, evaluation);
                        tests.Add(MakeTestsAndBindings(element, pattern, bindings));
                    }
                }
                else
                {
                    // This occurs in error cases.
                    RoslynDebug.Assert(recursive.HasAnyErrors);
                    // To prevent this pattern from subsuming other patterns and triggering a cascaded diagnostic, we add a test that will fail.
                    tests.Add(new Tests.One(new BoundDagTypeTest(recursive.Syntax, ErrorType(), input, hasErrors: true)));
                }
            }

            if (!recursive.Properties.IsDefault)
            {
                // we have a "property" form
                for (int i = 0; i < recursive.Properties.Length; i++)
                {
                    var subPattern = recursive.Properties[i];
                    Symbol? symbol = subPattern.Symbol;
                    BoundPattern pattern = subPattern.Pattern;
                    BoundDagEvaluation evaluation;
                    switch (symbol)
                    {
                        case PropertySymbol property:
                            evaluation = new BoundDagPropertyEvaluation(pattern.Syntax, property, OriginalInput(input, property));
                            break;
                        case FieldSymbol field:
                            evaluation = new BoundDagFieldEvaluation(pattern.Syntax, field, OriginalInput(input, field));
                            break;
                        default:
                            RoslynDebug.Assert(recursive.HasAnyErrors);
                            tests.Add(new Tests.One(new BoundDagTypeTest(recursive.Syntax, ErrorType(), input, hasErrors: true)));
                            continue;
                    }

                    tests.Add(new Tests.One(evaluation));
                    var element = new BoundDagTemp(pattern.Syntax, symbol.GetTypeOrReturnType().Type, evaluation);
                    tests.Add(MakeTestsAndBindings(element, pattern, bindings));
                }
            }

            if (recursive.VariableAccess != null)
            {
                // we have a "variable" declaration
                bindings.Add(new BoundPatternBinding(recursive.VariableAccess, input));
            }

            return Tests.AndSequence.Create(tests);
        }

        private Tests MakeTestsAndBindingsForNegatedPattern(BoundDagTemp input, BoundNegatedPattern neg, ArrayBuilder<BoundPatternBinding> bindings)
        {
            var tests = MakeTestsAndBindings(input, neg.Negated, bindings);
            return Tests.Not.Create(tests);
        }

        private Tests MakeTestsAndBindingsForBinaryPattern(
            BoundDagTemp input,
            BoundBinaryPattern bin,
            out BoundDagTemp output,
            ArrayBuilder<BoundPatternBinding> bindings)
        {
            var builder = ArrayBuilder<Tests>.GetInstance(2);
            if (bin.Disjunction)
            {
                builder.Add(MakeTestsAndBindings(input, bin.Left, bindings));
                builder.Add(MakeTestsAndBindings(input, bin.Right, bindings));
                var result = Tests.OrSequence.Create(builder);
                if (bin.InputType.Equals(bin.NarrowedType))
                {
                    output = input;
                    return result;
                }
                else
                {
                    builder = ArrayBuilder<Tests>.GetInstance(2);
                    builder.Add(result);
                    output = MakeConvertToType(input: input, syntax: bin.Syntax, type: bin.NarrowedType, isExplicitTest: false, tests: builder);
                    return Tests.AndSequence.Create(builder);
                }
            }
            else
            {
                builder.Add(MakeTestsAndBindings(input, bin.Left, out var leftOutput, bindings));
                builder.Add(MakeTestsAndBindings(leftOutput, bin.Right, out var rightOutput, bindings));
                output = rightOutput;
                Debug.Assert(bin.HasErrors || output.Type.Equals(bin.NarrowedType, TypeCompareKind.AllIgnoreOptions));
                return Tests.AndSequence.Create(builder);
            }
        }

        private Tests MakeTestsAndBindingsForRelationalPattern(
            BoundDagTemp input,
            BoundRelationalPattern rel,
            out BoundDagTemp output)
        {
            // check if the test is always true or always false
            var tests = ArrayBuilder<Tests>.GetInstance(2);
            output = MakeConvertToType(input, rel.Syntax, rel.Value.Type!, isExplicitTest: false, tests);
            var fac = ValueSetFactory.ForType(input.Type);
            var values = fac?.Related(rel.Relation.Operator(), rel.ConstantValue);
            if (values?.IsEmpty == true)
            {
                tests.Add(Tests.False.Instance);
            }
            else if (values?.Complement().IsEmpty != true)
            {
                tests.Add(new Tests.One(new BoundDagRelationalTest(rel.Syntax, rel.Relation, rel.ConstantValue, output, rel.HasErrors)));
            }

            return Tests.AndSequence.Create(tests);
        }

        private TypeSymbol ErrorType(string name = "")
        {
            return new ExtendedErrorTypeSymbol(this._compilation, name, arity: 0, errorInfo: null, unreported: false);
        }

        /// <summary>
        /// Compute and translate the decision dag, given a description of its initial state and a default
        /// decision when no decision appears to match. This implementation is nonrecursive to avoid
        /// overflowing the compiler's evaluation stack when compiling a large switch statement.
        /// </summary>
        private BoundDecisionDag MakeBoundDecisionDag(SyntaxNode syntax, ImmutableArray<StateForCase> cases)
        {
            var defaultDecision = new BoundLeafDecisionDagNode(syntax, _defaultLabel);

            // Build the state machine underlying the decision dag
            DecisionDag decisionDag = MakeDecisionDag(cases);

            // Note: It is useful for debugging the dag state table construction to view `decisionDag.Dump()` here.

            // Compute the bound decision dag corresponding to each node of decisionDag, and store
            // it in node.Dag.
            ComputeBoundDecisionDagNodes(decisionDag, defaultDecision);

            var rootDecisionDagNode = decisionDag.RootNode.Dag;
            RoslynDebug.Assert(rootDecisionDagNode != null);
            return new BoundDecisionDag(rootDecisionDagNode.Syntax, rootDecisionDagNode);
        }

        /// <summary>
        /// Make a <see cref="DecisionDag"/> (state machine) starting with the given set of cases in the root node,
        /// and return the node for the root.
        /// </summary>
        private DecisionDag MakeDecisionDag(ImmutableArray<StateForCase> casesForRootNode)
        {
            // A work list of DagStates whose successors need to be computed
            var workList = ArrayBuilder<DagState>.GetInstance();

            // A mapping used to make each DagState unique (i.e. to de-dup identical states).
            var uniqueState = new Dictionary<DagState, DagState>(DagStateEquivalence.Instance);

            // We "intern" the states, so that we only have a single object representing one
            // semantic state. Because the decision automaton may contain states that have more than one
            // predecessor, we want to represent each such state as a reference-unique object
            // so that it is processed only once. This object identity uniqueness will be important later when we
            // start mutating the DagState nodes to compute successors and BoundDecisionDagNodes
            // for each one. That is why we have to use an equivalence relation in the dictionary `uniqueState`.
            DagState uniqifyState(ImmutableArray<StateForCase> cases, ImmutableDictionary<BoundDagTemp, IValueSet> remainingValues)
            {
                var state = new DagState(cases, remainingValues);
                if (uniqueState.TryGetValue(state, out DagState? existingState))
                {
                    var newRemainingValues = existingState.RemainingValues;
                    bool changed = false;
                    foreach (var (dagTemp, valuesForTemp) in remainingValues)
                    {
                        if (newRemainingValues.TryGetValue(dagTemp, out var existingValuesForTemp))
                        {
                            var newExistingValuesForTemp = existingValuesForTemp.Union(valuesForTemp);
                            if (!newExistingValuesForTemp.Equals(existingValuesForTemp))
                            {
                                newRemainingValues = newRemainingValues.SetItem(dagTemp, newExistingValuesForTemp);
                                changed = true;
                            }
                        }
                        else
                        {
                            newRemainingValues = newRemainingValues.Add(dagTemp, valuesForTemp);
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        existingState.UpdateRemainingValues(newRemainingValues);
                        if (!workList.Contains(existingState))
                            workList.Push(existingState);
                    }

                    return existingState;
                }
                else
                {
                    // When we add a new unique state, we add it to a work list so that we
                    // will process it to compute its successors.
                    uniqueState.Add(state, state);
                    workList.Push(state);
                    return state;
                }
            }

            // Simplify the initial state based on impossible or earlier matched cases
            var rewrittenCases = ArrayBuilder<StateForCase>.GetInstance(casesForRootNode.Length);
            foreach (var state in casesForRootNode)
            {
                if (state.IsImpossible)
                    continue;
                rewrittenCases.Add(state);
                if (state.IsFullyMatched)
                    break;
            }

            var initialState = uniqifyState(rewrittenCases.ToImmutableAndFree(), ImmutableDictionary<BoundDagTemp, IValueSet>.Empty);

            // Go through the worklist of DagState nodes for which we have not yet computed
            // successor states.
            while (workList.Count != 0)
            {
                DagState state = workList.Pop();
                RoslynDebug.Assert(state.SelectedTest == null);
                RoslynDebug.Assert(state.TrueBranch == null);
                RoslynDebug.Assert(state.FalseBranch == null);
                if (state.Cases.IsDefaultOrEmpty)
                {
                    // If this state has no more cases that could possibly match, then
                    // we know there is no case that will match and this node represents a "default"
                    // decision. We do not need to compute a successor, as it is a leaf node
                    continue;
                }

                StateForCase first = state.Cases[0];

                Debug.Assert(!first.IsImpossible);
                if (first.PatternIsSatisfied)
                {
                    if (first.IsFullyMatched)
                    {
                        // The first of the remaining cases has fully matched, as there are no more tests to do.
                        // The language semantics of the switch statement and switch expression require that we
                        // execute the first matching case.  There is no when clause to evaluate here,
                        // so this is a leaf node and required no further processing.
                    }
                    else
                    {
                        // There is a when clause to evaluate.
                        // In case the when clause fails, we prepare for the remaining cases.
                        var stateWhenFails = state.Cases.RemoveAt(0);
                        state.FalseBranch = uniqifyState(stateWhenFails, state.RemainingValues);
                    }
                }
                else
                {
                    // Select the next test to do at this state, and compute successor states
                    switch (state.SelectedTest = state.ComputeSelectedTest())
                    {
                        case BoundDagEvaluation e:
                            state.TrueBranch = uniqifyState(RemoveEvaluation(state.Cases, e), state.RemainingValues);
                            // An evaluation is considered to always succeed, so there is no false branch
                            break;
                        case BoundDagTest d:
                            bool foundExplicitNullTest = false;
                            SplitCases(
                                state.Cases, state.RemainingValues, d,
                                out ImmutableArray<StateForCase> whenTrueDecisions,
                                out ImmutableArray<StateForCase> whenFalseDecisions,
                                out ImmutableDictionary<BoundDagTemp, IValueSet> whenTrueValues,
                                out ImmutableDictionary<BoundDagTemp, IValueSet> whenFalseValues,
                                ref foundExplicitNullTest);
                            state.TrueBranch = uniqifyState(whenTrueDecisions, whenTrueValues);
                            state.FalseBranch = uniqifyState(whenFalseDecisions, whenFalseValues);
                            if (foundExplicitNullTest && d is BoundDagNonNullTest { IsExplicitTest: false } t)
                            {
                                // Turn an "implicit" non-null test into an explicit one
                                state.SelectedTest = new BoundDagNonNullTest(t.Syntax, isExplicitTest: true, t.Input, t.HasErrors);
                            }
                            break;
                        case var n:
                            throw ExceptionUtilities.UnexpectedValue(n.Kind);
                    }
                }
            }

            workList.Free();
            return new DecisionDag(initialState);
        }

        /// <summary>
        /// Compute the <see cref="BoundDecisionDag"/> corresponding to each <see cref="DagState"/> of the given <see cref="DecisionDag"/>
        /// and store it in <see cref="DagState.Dag"/>.
        /// </summary>
        private void ComputeBoundDecisionDagNodes(DecisionDag decisionDag, BoundLeafDecisionDagNode defaultDecision)
        {
            Debug.Assert(_defaultLabel != null);
            Debug.Assert(defaultDecision != null);

            // Process the states in topological order, leaves first, and assign a BoundDecisionDag to each DagState.
            bool wasAcyclic = decisionDag.TryGetTopologicallySortedReachableStates(out ImmutableArray<DagState> sortedStates);
            if (!wasAcyclic)
            {
                // Since we intend the set of DagState nodes to be acyclic by construction, we do not expect
                // this to occur. Just in case it does due to bugs, we recover gracefully to avoid crashing the
                // compiler in production.  If you find that this happens (the assert fails), please modify the
                // DagState construction process to avoid creating a cyclic state graph.
                Debug.Assert(wasAcyclic); // force failure in debug builds

                // If the dag contains a cycle, return a short-circuit dag instead.
                decisionDag.RootNode.Dag = defaultDecision;
                return;
            }

            // We "intern" the dag nodes, so that we only have a single object representing one
            // semantic node. We do this because different states may end up mapping to the same
            // set of successor states. In this case we merge them when producing the bound state machine.
            var uniqueNodes = PooledDictionary<BoundDecisionDagNode, BoundDecisionDagNode>.GetInstance();
            BoundDecisionDagNode uniqifyDagNode(BoundDecisionDagNode node) => uniqueNodes.GetOrAdd(node, node);

            _ = uniqifyDagNode(defaultDecision);

            for (int i = sortedStates.Length - 1; i >= 0; i--)
            {
                var state = sortedStates[i];
                if (state.Cases.IsDefaultOrEmpty)
                {
                    state.Dag = defaultDecision;
                    continue;
                }

                StateForCase first = state.Cases[0];
                RoslynDebug.Assert(!(first.RemainingTests is Tests.False));
                if (first.PatternIsSatisfied)
                {
                    if (first.IsFullyMatched)
                    {
                        // there is no when clause we need to evaluate
                        state.Dag = finalState(first.Syntax, first.CaseLabel, first.Bindings);
                    }
                    else
                    {
                        RoslynDebug.Assert(state.TrueBranch == null);
                        RoslynDebug.Assert(state.FalseBranch is { });

                        // The final state here does not need bindings, as they will be performed before evaluating the when clause (see below)
                        BoundDecisionDagNode whenTrue = finalState(first.Syntax, first.CaseLabel, default);
                        BoundDecisionDagNode? whenFalse = state.FalseBranch.Dag;
                        RoslynDebug.Assert(whenFalse is { });
                        state.Dag = uniqifyDagNode(new BoundWhenDecisionDagNode(first.Syntax, first.Bindings, first.WhenClause, whenTrue, whenFalse));
                    }

                    BoundDecisionDagNode finalState(SyntaxNode syntax, LabelSymbol label, ImmutableArray<BoundPatternBinding> bindings)
                    {
                        BoundDecisionDagNode final = uniqifyDagNode(new BoundLeafDecisionDagNode(syntax, label));
                        return bindings.IsDefaultOrEmpty ? final : uniqifyDagNode(new BoundWhenDecisionDagNode(syntax, bindings, null, final, null));
                    }
                }
                else
                {
                    switch (state.SelectedTest)
                    {
                        case BoundDagEvaluation e:
                            {
                                BoundDecisionDagNode? next = state.TrueBranch!.Dag;
                                RoslynDebug.Assert(next is { });
                                RoslynDebug.Assert(state.FalseBranch == null);
                                state.Dag = uniqifyDagNode(new BoundEvaluationDecisionDagNode(e.Syntax, e, next));
                            }
                            break;
                        case BoundDagTest d:
                            {
                                BoundDecisionDagNode? whenTrue = state.TrueBranch!.Dag;
                                BoundDecisionDagNode? whenFalse = state.FalseBranch!.Dag;
                                RoslynDebug.Assert(whenTrue is { });
                                RoslynDebug.Assert(whenFalse is { });
                                state.Dag = uniqifyDagNode(new BoundTestDecisionDagNode(d.Syntax, d, whenTrue, whenFalse));
                            }
                            break;
                        case var n:
                            throw ExceptionUtilities.UnexpectedValue(n?.Kind);
                    }
                }
            }

            uniqueNodes.Free();
        }

        private void SplitCase(
            StateForCase stateForCase,
            BoundDagTest test,
            IValueSet? whenTrueValues,
            IValueSet? whenFalseValues,
            out StateForCase whenTrue,
            out StateForCase whenFalse,
            ref bool foundExplicitNullTest)
        {
            stateForCase.RemainingTests.Filter(this, test, whenTrueValues, whenFalseValues, out Tests whenTrueTests, out Tests whenFalseTests, ref foundExplicitNullTest);
            whenTrue = makeNext(whenTrueTests);
            whenFalse = makeNext(whenFalseTests);
            return;

            StateForCase makeNext(Tests remainingTests)
            {
                return remainingTests.Equals(stateForCase.RemainingTests)
                    ? stateForCase
                    : new StateForCase(
                        stateForCase.Index, stateForCase.Syntax, remainingTests,
                        stateForCase.Bindings, stateForCase.WhenClause, stateForCase.CaseLabel);
            }
        }

        private void SplitCases(
            ImmutableArray<StateForCase> statesForCases,
            ImmutableDictionary<BoundDagTemp, IValueSet> values,
            BoundDagTest test,
            out ImmutableArray<StateForCase> whenTrue,
            out ImmutableArray<StateForCase> whenFalse,
            out ImmutableDictionary<BoundDagTemp, IValueSet> whenTrueValues,
            out ImmutableDictionary<BoundDagTemp, IValueSet> whenFalseValues,
            ref bool foundExplicitNullTest)
        {
            var whenTrueBuilder = ArrayBuilder<StateForCase>.GetInstance(statesForCases.Length);
            var whenFalseBuilder = ArrayBuilder<StateForCase>.GetInstance(statesForCases.Length);
            bool whenTruePossible, whenFalsePossible;
            (whenTrueValues, whenFalseValues, whenTruePossible, whenFalsePossible) = SplitValues(values, test);
            // whenTruePossible means the test could possibly have succeeded.  whenFalsePossible means it could possibly have failed.
            // Tests that are either impossible or tautological (i.e. either of these false) given
            // the set of values are normally removed and replaced by the known result, so we would not normally be processing
            // a test that always succeeds or always fails, but they can occur in erroneous programs (e.g. testing for equality
            // against a non-constant value).
            foreach (var state in statesForCases)
            {
                SplitCase(
                    state, test,
                    whenTrueValues.TryGetValue(test.Input, out var v1) ? v1 : null,
                    whenFalseValues.TryGetValue(test.Input, out var v2) ? v2 : null,
                    out var whenTrueState, out var whenFalseState, ref foundExplicitNullTest);
                // whenTrueState.IsImpossible occurs when Split results in a state for a given case where the case has been ruled
                // out (because its test has failed). If not whenTruePossible, we don't want to add anything to the state.  In
                // either case, we do not want to add the current case to the state.
                if (whenTruePossible && !whenTrueState.IsImpossible && !(whenTrueBuilder.Any() && whenTrueBuilder.Last().IsFullyMatched))
                    whenTrueBuilder.Add(whenTrueState);
                // Similarly for the alternative state.
                if (whenFalsePossible && !whenFalseState.IsImpossible && !(whenFalseBuilder.Any() && whenFalseBuilder.Last().IsFullyMatched))
                    whenFalseBuilder.Add(whenFalseState);
            }

            whenTrue = whenTrueBuilder.ToImmutableAndFree();
            whenFalse = whenFalseBuilder.ToImmutableAndFree();
        }

        private static (
            ImmutableDictionary<BoundDagTemp, IValueSet> whenTrueValues,
            ImmutableDictionary<BoundDagTemp, IValueSet> whenFalseValues,
            bool truePossible,
            bool falsePossible)
            SplitValues(
            ImmutableDictionary<BoundDagTemp, IValueSet> values,
            BoundDagTest test)
        {
            switch (test)
            {
                case BoundDagEvaluation _:
                case BoundDagExplicitNullTest _:
                case BoundDagNonNullTest _:
                case BoundDagTypeTest _:
                    return (values, values, true, true);
                case BoundDagValueTest t:
                    return resultForRelation(BinaryOperatorKind.Equal, t.Value);
                case BoundDagRelationalTest t:
                    return resultForRelation(t.Relation, t.Value);
                default:
                    throw ExceptionUtilities.UnexpectedValue(test);
            }

            (
            ImmutableDictionary<BoundDagTemp, IValueSet> whenTrueValues,
            ImmutableDictionary<BoundDagTemp, IValueSet> whenFalseValues,
            bool truePossible,
            bool falsePossible)
            resultForRelation(BinaryOperatorKind relation, ConstantValue value)
            {
                var input = test.Input;
                IValueSetFactory? valueFac = ValueSetFactory.ForType(input.Type);
                if (valueFac == null || value.IsBad)
                {
                    // If it is a type we don't track yet, assume all values are possible
                    return (values, values, true, true);
                }
                IValueSet fromTestPassing = valueFac.Related(relation.Operator(), value);
                IValueSet fromTestFailing = fromTestPassing.Complement();
                if (values.TryGetValue(test.Input, out IValueSet tempValuesBeforeTest))
                {
                    fromTestPassing = fromTestPassing.Intersect(tempValuesBeforeTest);
                    fromTestFailing = fromTestFailing.Intersect(tempValuesBeforeTest);
                }
                var whenTrueValues = values.SetItem(input, fromTestPassing);
                var whenFalseValues = values.SetItem(input, fromTestFailing);
                return (whenTrueValues, whenFalseValues, !fromTestPassing.IsEmpty, !fromTestFailing.IsEmpty);
            }
        }

        private static ImmutableArray<StateForCase> RemoveEvaluation(ImmutableArray<StateForCase> cases, BoundDagEvaluation e)
        {
            var builder = ArrayBuilder<StateForCase>.GetInstance(cases.Length);
            foreach (var stateForCase in cases)
            {
                var remainingTests = stateForCase.RemainingTests.RemoveEvaluation(e);
                if (remainingTests is Tests.False)
                {
                    // This can occur in error cases like `e is not int x` where there is a trailing evaluation
                    // in a failure branch.
                }
                else
                {
                    builder.Add(new StateForCase(
                        Index: stateForCase.Index, Syntax: stateForCase.Syntax,
                        RemainingTests: remainingTests,
                        Bindings: stateForCase.Bindings, WhenClause: stateForCase.WhenClause, CaseLabel: stateForCase.CaseLabel));
                }
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Given that the test <paramref name="test"/> has occurred and produced a true/false result,
        /// set some flags indicating the implied status of the <paramref name="other"/> test.
        /// </summary>
        /// <param name="test"></param>
        /// <param name="other"></param>
        /// <param name="whenTrueValues">The possible values of test.Input when <paramref name="test"/> has succeeded.</param>
        /// <param name="whenFalseValues">The possible values of test.Input when <paramref name="test"/> has failed.</param>
        /// <param name="trueTestPermitsTrueOther">set if <paramref name="test"/> being true would permit <paramref name="other"/> to succeed</param>
        /// <param name="falseTestPermitsTrueOther">set if a false result on <paramref name="test"/> would permit <paramref name="other"/> to succeed</param>
        /// <param name="trueTestImpliesTrueOther">set if <paramref name="test"/> being true means <paramref name="other"/> has been proven true</param>
        /// <param name="falseTestImpliesTrueOther">set if <paramref name="test"/> being false means <paramref name="other"/> has been proven true</param>
        private void CheckConsistentDecision(
            BoundDagTest test,
            BoundDagTest other,
            IValueSet? whenTrueValues,
            IValueSet? whenFalseValues,
            SyntaxNode syntax,
            out bool trueTestPermitsTrueOther,
            out bool falseTestPermitsTrueOther,
            out bool trueTestImpliesTrueOther,
            out bool falseTestImpliesTrueOther,
            ref bool foundExplicitNullTest)
        {
            // innocent until proven guilty
            trueTestPermitsTrueOther = true;
            falseTestPermitsTrueOther = true;
            trueTestImpliesTrueOther = false;
            falseTestImpliesTrueOther = false;

            // if the tests are for unrelated things, there is no implication from one to the other
            if (!test.Input.Equals(other.Input))
                return;

            switch (test)
            {
                case BoundDagNonNullTest _:
                    switch (other)
                    {
                        case BoundDagValueTest _:
                            // !(v != null) --> !(v == K)
                            falseTestPermitsTrueOther = false;
                            break;
                        case BoundDagExplicitNullTest _:
                            foundExplicitNullTest = true;
                            // v != null --> !(v == null)
                            trueTestPermitsTrueOther = false;
                            // !(v != null) --> v == null
                            falseTestImpliesTrueOther = true;
                            break;
                        case BoundDagNonNullTest n2:
                            if (n2.IsExplicitTest)
                                foundExplicitNullTest = true;
                            // v != null --> v != null
                            trueTestImpliesTrueOther = true;
                            // !(v != null) --> !(v != null)
                            falseTestPermitsTrueOther = false;
                            break;
                        default:
                            // !(v != null) --> !(v is T)
                            falseTestPermitsTrueOther = false;
                            break;
                    }
                    break;
                case BoundDagTypeTest t1:
                    switch (other)
                    {
                        case BoundDagNonNullTest n2:
                            if (n2.IsExplicitTest)
                                foundExplicitNullTest = true;
                            // v is T --> v != null
                            trueTestImpliesTrueOther = true;
                            break;
                        case BoundDagTypeTest t2:
                            {
                                HashSet<DiagnosticInfo>? useSiteDiagnostics = null;
                                bool? matches = ExpressionOfTypeMatchesPatternTypeForLearningFromSuccessfulTypeTest(t1.Type, t2.Type, ref useSiteDiagnostics);
                                if (matches == false)
                                {
                                    // If T1 could never be T2
                                    // v is T1 --> !(v is T2)
                                    trueTestPermitsTrueOther = false;
                                }
                                else if (matches == true)
                                {
                                    // If T1: T2
                                    // v is T1 --> v is T2
                                    trueTestImpliesTrueOther = true;
                                }

                                // If every T2 is a T1, then failure of T1 implies failure of T2.
                                matches = Binder.ExpressionOfTypeMatchesPatternType(_conversions, t2.Type, t1.Type, ref useSiteDiagnostics, out _);
                                _diagnostics.Add(syntax, useSiteDiagnostics);
                                if (matches == true)
                                {
                                    // If T2: T1
                                    // !(v is T1) --> !(v is T2)
                                    falseTestPermitsTrueOther = false;
                                }
                            }
                            break;
                        case BoundDagValueTest _:
                            break;
                        case BoundDagExplicitNullTest _:
                            foundExplicitNullTest = true;
                            // v is T --> !(v == null)
                            trueTestPermitsTrueOther = false;
                            break;
                    }
                    break;
                case BoundDagValueTest _:
                case BoundDagRelationalTest _:
                    switch (other)
                    {
                        case BoundDagNonNullTest n2:
                            if (n2.IsExplicitTest)
                                foundExplicitNullTest = true;
                            // v == K --> v != null
                            trueTestImpliesTrueOther = true;
                            break;
                        case BoundDagTypeTest _:
                            break;
                        case BoundDagExplicitNullTest _:
                            foundExplicitNullTest = true;
                            // v == K --> !(v == null)
                            trueTestPermitsTrueOther = false;
                            break;
                        case BoundDagRelationalTest r2:
                            handleRelationWithValue(r2.Relation, r2.Value,
                                out trueTestPermitsTrueOther, out falseTestPermitsTrueOther, out trueTestImpliesTrueOther, out falseTestImpliesTrueOther);
                            break;
                        case BoundDagValueTest v2:
                            handleRelationWithValue(BinaryOperatorKind.Equal, v2.Value,
                                out trueTestPermitsTrueOther, out falseTestPermitsTrueOther, out trueTestImpliesTrueOther, out falseTestImpliesTrueOther);
                            break;

                            void handleRelationWithValue(
                                BinaryOperatorKind relation,
                                ConstantValue value,
                                out bool trueTestPermitsTrueOther,
                                out bool falseTestPermitsTrueOther,
                                out bool trueTestImpliesTrueOther,
                                out bool falseTestImpliesTrueOther)
                            {
                                // We check test.Equals(other) to handle "bad" constant values
                                bool sameTest = test.Equals(other);
                                trueTestPermitsTrueOther = whenTrueValues?.Any(relation, value) ?? true;
                                trueTestImpliesTrueOther = sameTest || trueTestPermitsTrueOther && (whenTrueValues?.All(relation, value) ?? false);
                                falseTestPermitsTrueOther = !sameTest && (whenFalseValues?.Any(relation, value) ?? true);
                                falseTestImpliesTrueOther = falseTestPermitsTrueOther && (whenFalseValues?.All(relation, value) ?? false);
                            }
                    }
                    break;
                case BoundDagExplicitNullTest _:
                    foundExplicitNullTest = true;
                    switch (other)
                    {
                        case BoundDagNonNullTest n2:
                            if (n2.IsExplicitTest)
                                foundExplicitNullTest = true;
                            // v == null --> !(v != null)
                            trueTestPermitsTrueOther = false;
                            // !(v == null) --> v != null
                            falseTestImpliesTrueOther = true;
                            break;
                        case BoundDagTypeTest _:
                            // v == null --> !(v is T)
                            trueTestPermitsTrueOther = false;
                            break;
                        case BoundDagExplicitNullTest _:
                            foundExplicitNullTest = true;
                            // v == null --> v == null
                            trueTestImpliesTrueOther = true;
                            // !(v == null) --> !(v == null)
                            falseTestPermitsTrueOther = false;
                            break;
                        case BoundDagValueTest _:
                            // v == null --> !(v == K)
                            trueTestPermitsTrueOther = false;
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Determine what we can learn from one successful runtime type test about another planned
        /// runtime type test for the purpose of building the decision tree.
        /// We accommodate a special behavior of the runtime here, which does not match the language rules.
        /// A value of type `int[]` is an "instanceof" (i.e. result of the `isinst` instruction) the type
        /// `uint[]` and vice versa.  It is similarly so for every pair of same-sized numeric types, and
        /// arrays of enums are considered to be their underlying type.  We need the dag construction to
        /// recognize this runtime behavior, so we pretend that matching one of them gives no information
        /// on whether the other will be matched.  That isn't quite correct (nothing reasonable we do
        /// could be), but it comes closest to preserving the existing C#7 behavior without undesirable
        /// side-effects, and permits the code-gen strategy to preserve the dynamic semantic equivalence
        /// of a switch (on the one hand) and a series of if-then-else statements (on the other).
        /// See, for example, https://github.com/dotnet/roslyn/issues/35661
        /// </summary>
        private bool? ExpressionOfTypeMatchesPatternTypeForLearningFromSuccessfulTypeTest(
            TypeSymbol expressionType,
            TypeSymbol patternType,
            ref HashSet<DiagnosticInfo>? useSiteDiagnostics)
        {
            bool? result = Binder.ExpressionOfTypeMatchesPatternType(_conversions, expressionType, patternType, ref useSiteDiagnostics, out Conversion conversion);
            return (!conversion.Exists && isRuntimeSimilar(expressionType, patternType))
                ? null // runtime and compile-time test behavior differ. Pretend we don't know what happens.
                : result;

            static bool isRuntimeSimilar(TypeSymbol expressionType, TypeSymbol patternType)
            {
                while (expressionType is ArrayTypeSymbol { ElementType: var e1, IsSZArray: var sz1, Rank: var r1 } &&
                       patternType is ArrayTypeSymbol { ElementType: var e2, IsSZArray: var sz2, Rank: var r2 } &&
                       sz1 == sz2 && r1 == r2)
                {
                    e1 = e1.EnumUnderlyingTypeOrSelf();
                    e2 = e2.EnumUnderlyingTypeOrSelf();
                    switch (e1.SpecialType, e2.SpecialType)
                    {
                        // The following support CLR behavior that is required by
                        // the CLI specification but violates the C# language behavior.
                        // See ECMA-335's definition of *array-element-compatible-with*.
                        case var (s1, s2) when s1 == s2:
                        case (SpecialType.System_SByte, SpecialType.System_Byte):
                        case (SpecialType.System_Byte, SpecialType.System_SByte):
                        case (SpecialType.System_Int16, SpecialType.System_UInt16):
                        case (SpecialType.System_UInt16, SpecialType.System_Int16):
                        case (SpecialType.System_Int32, SpecialType.System_UInt32):
                        case (SpecialType.System_UInt32, SpecialType.System_Int32):
                        case (SpecialType.System_Int64, SpecialType.System_UInt64):
                        case (SpecialType.System_UInt64, SpecialType.System_Int64):
                        case (SpecialType.System_IntPtr, SpecialType.System_UIntPtr):
                        case (SpecialType.System_UIntPtr, SpecialType.System_IntPtr):

                        // The following support behavior of the CLR that violates the CLI
                        // and C# specifications, but we implement them because that is the
                        // behavior on 32-bit runtimes.
                        case (SpecialType.System_Int32, SpecialType.System_IntPtr):
                        case (SpecialType.System_Int32, SpecialType.System_UIntPtr):
                        case (SpecialType.System_UInt32, SpecialType.System_IntPtr):
                        case (SpecialType.System_UInt32, SpecialType.System_UIntPtr):
                        case (SpecialType.System_IntPtr, SpecialType.System_Int32):
                        case (SpecialType.System_IntPtr, SpecialType.System_UInt32):
                        case (SpecialType.System_UIntPtr, SpecialType.System_Int32):
                        case (SpecialType.System_UIntPtr, SpecialType.System_UInt32):

                        // The following support behavior of the CLR that violates the CLI
                        // and C# specifications, but we implement them because that is the
                        // behavior on 64-bit runtimes.
                        case (SpecialType.System_Int64, SpecialType.System_IntPtr):
                        case (SpecialType.System_Int64, SpecialType.System_UIntPtr):
                        case (SpecialType.System_UInt64, SpecialType.System_IntPtr):
                        case (SpecialType.System_UInt64, SpecialType.System_UIntPtr):
                        case (SpecialType.System_IntPtr, SpecialType.System_Int64):
                        case (SpecialType.System_IntPtr, SpecialType.System_UInt64):
                        case (SpecialType.System_UIntPtr, SpecialType.System_Int64):
                        case (SpecialType.System_UIntPtr, SpecialType.System_UInt64):
                            return true;

                        default:
                            (expressionType, patternType) = (e1, e2);
                            break;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// A representation of the entire decision dag and each of its states.
        /// </summary>
        private sealed class DecisionDag
        {
            /// <summary>
            /// The starting point for deciding which case matches.
            /// </summary>
            public readonly DagState RootNode;
            public DecisionDag(DagState rootNode)
            {
                this.RootNode = rootNode;
            }

            /// <summary>
            /// A successor function used to topologically sort the DagState set.
            /// </summary>
            private static ImmutableArray<DagState> Successor(DagState state)
            {

                if (state.TrueBranch != null && state.FalseBranch != null)
                {
                    return ImmutableArray.Create(state.FalseBranch, state.TrueBranch);
                }
                else if (state.TrueBranch != null)
                {
                    return ImmutableArray.Create(state.TrueBranch);
                }
                else if (state.FalseBranch != null)
                {
                    return ImmutableArray.Create(state.FalseBranch);
                }
                else
                {
                    return ImmutableArray<DagState>.Empty;
                }
            }

            /// <summary>
            /// Produce the states in topological order.
            /// </summary>
            /// <param name="result">Topologically sorted <see cref="DagState"/> nodes.</param>
            /// <returns>True if the graph was acyclic.</returns>
            public bool TryGetTopologicallySortedReachableStates(out ImmutableArray<DagState> result)
            {
                return TopologicalSort.TryIterativeSort<DagState>(SpecializedCollections.SingletonEnumerable<DagState>(this.RootNode), Successor, out result);
            }

#if DEBUG
            /// <summary>
            /// Starting with `this` state, produce a human-readable description of the state tables.
            /// This is very useful for debugging and optimizing the dag state construction.
            /// </summary>
            internal string Dump()
            {
                if (!this.TryGetTopologicallySortedReachableStates(out var allStates))
                {
                    return "(the dag contains a cycle!)";
                }

                var stateIdentifierMap = PooledDictionary<DagState, int>.GetInstance();
                for (int i = 0; i < allStates.Length; i++)
                {
                    stateIdentifierMap.Add(allStates[i], i);
                }

                // NOTE that this numbering for temps does not work well for the invocation of Deconstruct, which produces
                // multiple values.  This would make them appear to be the same temp in the debug dump.
                int nextTempNumber = 0;
                PooledDictionary<BoundDagEvaluation, int> tempIdentifierMap = PooledDictionary<BoundDagEvaluation, int>.GetInstance();
                int tempIdentifier(BoundDagEvaluation? e)
                {
                    return (e == null) ? 0 : tempIdentifierMap.TryGetValue(e, out int value) ? value : tempIdentifierMap[e] = ++nextTempNumber;
                }

                string tempName(BoundDagTemp t)
                {
                    return $"t{tempIdentifier(t.Source)}";
                }

                var resultBuilder = PooledStringBuilder.GetInstance();
                var result = resultBuilder.Builder;

                foreach (DagState state in allStates)
                {
                    bool isFail = state.Cases.IsEmpty;
                    bool starred = isFail || state.Cases.First().PatternIsSatisfied;
                    result.Append($"{(starred ? "*" : "")}State " + stateIdentifierMap[state] + (isFail ? " FAIL" : ""));
                    var remainingValues = state.RemainingValues.Select(kvp => $"{tempName(kvp.Key)}:{kvp.Value}");
                    result.AppendLine($"{(remainingValues.Any() ? " REMAINING " + string.Join(" ", remainingValues) : "")}");

                    foreach (StateForCase cd in state.Cases)
                    {
                        result.AppendLine($"    {dumpStateForCase(cd)}");
                    }

                    if (state.SelectedTest != null)
                    {
                        result.AppendLine($"    Test: {dumpDagTest(state.SelectedTest)}");
                    }

                    if (state.TrueBranch != null)
                    {
                        result.AppendLine($"    TrueBranch: {stateIdentifierMap[state.TrueBranch]}");
                    }

                    if (state.FalseBranch != null)
                    {
                        result.AppendLine($"    FalseBranch: {stateIdentifierMap[state.FalseBranch]}");
                    }
                }

                stateIdentifierMap.Free();
                tempIdentifierMap.Free();
                return resultBuilder.ToStringAndFree();

                string dumpStateForCase(StateForCase cd)
                {
                    var instance = PooledStringBuilder.GetInstance();
                    StringBuilder builder = instance.Builder;
                    builder.Append($"{cd.Index}. [{cd.Syntax}] {(cd.PatternIsSatisfied ? "MATCH" : cd.RemainingTests.Dump(dumpDagTest))}");
                    var bindings = cd.Bindings.Select(bpb => $"{(bpb.VariableAccess is BoundLocal l ? l.LocalSymbol.Name : "<var>")}={tempName(bpb.TempContainingValue)}");
                    if (bindings.Any())
                    {
                        builder.Append(" BIND[");
                        builder.Append(string.Join("; ", bindings));
                        builder.Append("]");
                    }

                    if (cd.WhenClause is { })
                    {
                        builder.Append($" WHEN[{cd.WhenClause.Syntax}]");
                    }

                    return instance.ToStringAndFree();
                }

                string dumpDagTest(BoundDagTest d)
                {
                    switch (d)
                    {
                        case BoundDagTypeEvaluation a:
                            return $"t{tempIdentifier(a)}={a.Kind}({tempName(a.Input)} as {a.Type})";
                        case BoundDagFieldEvaluation e:
                            return $"t{tempIdentifier(e)}={e.Kind}({tempName(e.Input)}.{e.Field.Name})";
                        case BoundDagEvaluation e:
                            return $"t{tempIdentifier(e)}={e.Kind}({tempName(e.Input)})";
                        case BoundDagTypeTest b:
                            return $"?{d.Kind}({tempName(d.Input)} is {b.Type})";
                        case BoundDagValueTest v:
                            return $"?{d.Kind}({tempName(d.Input)} == {v.Value})";
                        case BoundDagRelationalTest r:
                            var operatorName = r.Relation.Operator() switch
                            {
                                BinaryOperatorKind.LessThan => "<",
                                BinaryOperatorKind.LessThanOrEqual => "<=",
                                BinaryOperatorKind.GreaterThan => ">",
                                BinaryOperatorKind.GreaterThanOrEqual => ">=",
                                _ => "??"
                            };
                            return $"?{d.Kind}({tempName(d.Input)} {operatorName} {r.Value})";
                        default:
                            return $"?{d.Kind}({tempName(d.Input)})";
                    }
                }
            }
#endif
        }

        /// <summary>
        /// The state at a given node of the decision finite state automaton. This is used during computation of the state
        /// machine (<see cref="BoundDecisionDag"/>), and contains a representation of the meaning of the state. Because we always make
        /// forward progress when a test is evaluated (the state description is monotonically smaller at each edge), the
        /// graph of states is acyclic, which is why we call it a dag (directed acyclic graph).
        /// </summary>
        private sealed class DagState
        {
            /// <summary>
            /// For each dag temp of a type for which we track such things (the integral types, floating-point types, and bool),
            /// the possible values it can take on when control reaches this state.
            /// If this dictionary is mutated after <see cref="TrueBranch"/>, <see cref="FalseBranch"/>,
            /// and <see cref="Dag"/> are computed (for example to merge states), they must be cleared and recomputed,
            /// as the set of possible values can affect successor states.
            /// A <see cref="BoundDagTemp"/> absent from this dictionary means that all values of the type are possible.
            /// </summary>
            public ImmutableDictionary<BoundDagTemp, IValueSet> RemainingValues { get; private set; }

            /// <summary>
            /// The set of cases that may still match, and for each of them the set of tests that remain to be tested.
            /// </summary>
            public readonly ImmutableArray<StateForCase> Cases;

            public DagState(ImmutableArray<StateForCase> cases, ImmutableDictionary<BoundDagTemp, IValueSet> remainingValues)
            {
                this.Cases = cases;
                this.RemainingValues = remainingValues;
            }

            // If not a leaf node or a when clause, the test that will be taken at this node of the
            // decision automaton.
            public BoundDagTest? SelectedTest;

            // We only compute the dag states for the branches after we de-dup this DagState itself.
            // If all that remains is the `when` clauses, SelectedDecision is left `null` (we can
            // build the leaf node easily during translation) and the FalseBranch field is populated
            // with the successor on failure of the when clause (if one exists).
            public DagState? TrueBranch, FalseBranch;

            // After the entire graph of DagState objects is complete, we translate each into its Dag node.
            public BoundDecisionDagNode? Dag;

            /// <summary>
            /// Decide on what test to use at this node of the decision dag. This is the principal
            /// heuristic we can change to adjust the quality of the generated decision automaton.
            /// See https://www.cs.tufts.edu/~nr/cs257/archive/norman-ramsey/match.pdf for some ideas.
            /// </summary>
            internal BoundDagTest ComputeSelectedTest()
            {
                return Cases[0].RemainingTests.ComputeSelectedTest();
            }

            internal void UpdateRemainingValues(ImmutableDictionary<BoundDagTemp, IValueSet> newRemainingValues)
            {
                this.RemainingValues = newRemainingValues;
                this.SelectedTest = null;
                this.TrueBranch = null;
                this.FalseBranch = null;
            }
        }

        /// <summary>
        /// An equivalence relation between dag states used to dedup the states during dag construction.
        /// After dag construction is complete we treat a DagState as using object equality as equivalent
        /// states have been merged.
        /// </summary>
        private sealed class DagStateEquivalence : IEqualityComparer<DagState>
        {
            public static readonly DagStateEquivalence Instance = new DagStateEquivalence();

            private DagStateEquivalence() { }

            public bool Equals(DagState? x, DagState? y)
            {
                RoslynDebug.Assert(x is { });
                RoslynDebug.Assert(y is { });
                return x == y || x.Cases.SequenceEqual(y.Cases, (a, b) => a.Equals(b));
            }

            public int GetHashCode(DagState x)
            {
                return Hash.Combine(Hash.CombineValues(x.Cases), x.Cases.Length);
            }
        }

        /// <summary>
        /// As part of the description of a node of the decision automaton, we keep track of what tests
        /// remain to be done for each case.
        /// </summary>
        private sealed class StateForCase
        {
            /// <summary>
            /// A number that is distinct for each case and monotonically increasing from earlier to later cases.
            /// Since we always keep the cases in order, this is only used to assist with debugging (e.g.
            /// see DecisionDag.Dump()).
            /// </summary>
            public readonly int Index;
            public readonly SyntaxNode Syntax;
            public readonly Tests RemainingTests;
            public readonly ImmutableArray<BoundPatternBinding> Bindings;
            public readonly BoundExpression? WhenClause;
            public readonly LabelSymbol CaseLabel;
            public StateForCase(
                int Index,
                SyntaxNode Syntax,
                Tests RemainingTests,
                ImmutableArray<BoundPatternBinding> Bindings,
                BoundExpression? WhenClause,
                LabelSymbol CaseLabel)
            {
                this.Index = Index;
                this.Syntax = Syntax;
                this.RemainingTests = RemainingTests;
                this.Bindings = Bindings;
                this.WhenClause = WhenClause;
                this.CaseLabel = CaseLabel;
            }

            /// <summary>
            /// Is the pattern in a state in which it is fully matched and there is no when clause?
            /// </summary>
            public bool IsFullyMatched => RemainingTests is Tests.True && (WhenClause is null || WhenClause.ConstantValue == ConstantValue.True);

            /// <summary>
            /// Is the pattern fully matched and ready for the when clause to be evaluated (if any)?
            /// </summary>
            public bool PatternIsSatisfied => RemainingTests is Tests.True;

            /// <summary>
            /// Is the clause impossible?  We do not consider a when clause with a constant false value to cause the branch to be impossible.
            /// Note that we do not include the possibility that a when clause is the constant false.  That is treated like any other expression.
            /// </summary>
            public bool IsImpossible => RemainingTests is Tests.False;

            public override bool Equals(object? obj)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public bool Equals(StateForCase other)
            {
                // We do not include Syntax, Bindings, WhereClause, or CaseLabel
                // because once the Index is the same, those must be the same too.
                return this == other ||
                    other != null &&
                    this.Index == other.Index &&
                    this.RemainingTests.Equals(other.RemainingTests);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(RemainingTests.GetHashCode(), Index);
            }
        }

        /// <summary>
        /// A set of tests to be performed.  This is a discriminated union; see the options (nested types) for more details.
        /// </summary>
        private abstract class Tests
        {
            private Tests() { }

            /// <summary>
            /// Take the set of tests and split them into two, one for when the test has succeeded, and one for when the test has failed.
            /// </summary>
            public abstract void Filter(
                DecisionDagBuilder builder,
                BoundDagTest test,
                IValueSet? whenTrueValues,
                IValueSet? whenFalseValues,
                out Tests whenTrue,
                out Tests whenFalse,
                ref bool foundExplicitNullTest);
            public virtual BoundDagTest ComputeSelectedTest() => throw ExceptionUtilities.Unreachable;
            public virtual Tests RemoveEvaluation(BoundDagEvaluation e) => this;
            public abstract string Dump(Func<BoundDagTest, string> dump);

            /// <summary>
            /// No tests to be performed; the result is true (success).
            /// </summary>
            public sealed class True : Tests
            {
                public static readonly True Instance = new True();
                public override string Dump(Func<BoundDagTest, string> dump) => "TRUE";
                public override void Filter(
                    DecisionDagBuilder builder,
                    BoundDagTest test,
                    IValueSet? whenTrueValues,
                    IValueSet? whenFalseValues,
                    out Tests whenTrue,
                    out Tests whenFalse,
                    ref bool foundExplicitNullTest)
                {
                    whenTrue = whenFalse = this;
                }
            }

            /// <summary>
            /// No tests to be performed; the result is false (failure).
            /// </summary>
            public sealed class False : Tests
            {
                public static readonly False Instance = new False();
                public override string Dump(Func<BoundDagTest, string> dump) => "FALSE";
                public override void Filter(
                    DecisionDagBuilder builder,
                    BoundDagTest test,
                    IValueSet? whenTrueValues,
                    IValueSet? whenFalseValues,
                    out Tests whenTrue,
                    out Tests whenFalse,
                    ref bool foundExplicitNullTest)
                {
                    whenTrue = whenFalse = this;
                }
            }

            /// <summary>
            /// A single test to be performed, described by a <see cref="BoundDagTest"/>.
            /// Note that the test might be a <see cref="BoundDagEvaluation"/>, in which case it is deemed to have
            /// succeeded after being evaluated.
            /// </summary>
            public sealed class One : Tests
            {
                public readonly BoundDagTest Test;
                public One(BoundDagTest test) => this.Test = test;
                public void Deconstruct(out BoundDagTest Test) => Test = this.Test;
                public override void Filter(
                    DecisionDagBuilder builder,
                    BoundDagTest test,
                    IValueSet? whenTrueValues,
                    IValueSet? whenFalseValues,
                    out Tests whenTrue,
                    out Tests whenFalse,
                    ref bool foundExplicitNullTest)
                {
                    builder.CheckConsistentDecision(
                        test: test,
                        other: Test,
                        whenTrueValues: whenTrueValues,
                        whenFalseValues: whenFalseValues,
                        syntax: test.Syntax,
                        trueTestPermitsTrueOther: out bool trueDecisionPermitsTrueOther,
                        falseTestPermitsTrueOther: out bool falseDecisionPermitsTrueOther,
                        trueTestImpliesTrueOther: out bool trueDecisionImpliesTrueOther,
                        falseTestImpliesTrueOther: out bool falseDecisionImpliesTrueOther,
                        foundExplicitNullTest: ref foundExplicitNullTest);
                    whenTrue = trueDecisionImpliesTrueOther ? Tests.True.Instance : trueDecisionPermitsTrueOther ? this : (Tests)Tests.False.Instance;
                    whenFalse = falseDecisionImpliesTrueOther ? Tests.True.Instance : falseDecisionPermitsTrueOther ? this : (Tests)Tests.False.Instance;
                }
                public override BoundDagTest ComputeSelectedTest() => this.Test;
                public override Tests RemoveEvaluation(BoundDagEvaluation e) => e.Equals(Test) ? Tests.True.Instance : (Tests)this;
                public override string Dump(Func<BoundDagTest, string> dump) => dump(this.Test);
                public override bool Equals(object? obj) => this == obj || obj is One other && this.Test.Equals(other.Test);
                public override int GetHashCode() => this.Test.GetHashCode();
            }

            public sealed class Not : Tests
            {
                // Negation is pushed to the level of a single test by demorgan's laws
                public readonly Tests Negated;
                private Not(Tests negated) => Negated = negated;
                public static Tests Create(Tests negated) => negated switch
                {
                    Tests.True _ => Tests.False.Instance,
                    Tests.False _ => Tests.True.Instance,
                    Tests.Not n => n.Negated, // double negative
                    Tests.AndSequence a => new Not(a),
                    Tests.OrSequence a => Tests.AndSequence.Create(NegateSequenceElements(a.RemainingTests)), // use demorgan to prefer and sequences
                    Tests.One o => new Not(o),
                    _ => throw ExceptionUtilities.UnexpectedValue(negated),
                };
                private static ArrayBuilder<Tests> NegateSequenceElements(ImmutableArray<Tests> seq)
                {
                    var builder = ArrayBuilder<Tests>.GetInstance(seq.Length);
                    foreach (var t in seq)
                        builder.Add(Not.Create(t));

                    return builder;
                }
                public override Tests RemoveEvaluation(BoundDagEvaluation e) => Create(Negated.RemoveEvaluation(e));
                public override BoundDagTest ComputeSelectedTest() => Negated.ComputeSelectedTest();
                public override string Dump(Func<BoundDagTest, string> dump) => $"Not ({Negated.Dump(dump)})";
                public override void Filter(
                    DecisionDagBuilder builder,
                    BoundDagTest test,
                    IValueSet? whenTrueValues,
                    IValueSet? whenFalseValues,
                    out Tests whenTrue,
                    out Tests whenFalse,
                    ref bool foundExplicitNullTest)
                {
                    Negated.Filter(builder, test, whenTrueValues, whenFalseValues, out var whenTestTrue, out var whenTestFalse, ref foundExplicitNullTest);
                    whenTrue = Not.Create(whenTestTrue);
                    whenFalse = Not.Create(whenTestFalse);
                }
                public override bool Equals(object? obj) => this == obj || obj is Not n && Negated.Equals(n.Negated);
                public override int GetHashCode() => Hash.Combine(Negated.GetHashCode(), typeof(Not).GetHashCode());
            }

            public abstract class SequenceTests : Tests
            {
                public readonly ImmutableArray<Tests> RemainingTests;
                protected SequenceTests(ImmutableArray<Tests> remainingTests)
                {
                    Debug.Assert(remainingTests.Length > 1);
                    this.RemainingTests = remainingTests;
                }
                public abstract Tests Update(ArrayBuilder<Tests> remainingTests);
                public override void Filter(
                    DecisionDagBuilder builder,
                    BoundDagTest test,
                    IValueSet? whenTrueValues,
                    IValueSet? whenFalseValues,
                    out Tests whenTrue,
                    out Tests whenFalse,
                    ref bool foundExplicitNullTest)
                {
                    var trueBuilder = ArrayBuilder<Tests>.GetInstance(RemainingTests.Length);
                    var falseBuilder = ArrayBuilder<Tests>.GetInstance(RemainingTests.Length);
                    foreach (var other in RemainingTests)
                    {
                        other.Filter(builder, test, whenTrueValues, whenFalseValues, out Tests oneTrue, out Tests oneFalse, ref foundExplicitNullTest);
                        trueBuilder.Add(oneTrue);
                        falseBuilder.Add(oneFalse);
                    }

                    whenTrue = Update(trueBuilder);
                    whenFalse = Update(falseBuilder);
                }
                public override Tests RemoveEvaluation(BoundDagEvaluation e)
                {
                    var builder = ArrayBuilder<Tests>.GetInstance(RemainingTests.Length);
                    foreach (var test in RemainingTests)
                        builder.Add(test.RemoveEvaluation(e));

                    return Update(builder);
                }
                public override bool Equals(object? obj) =>
                    this == obj || obj is SequenceTests other && this.GetType() == other.GetType() && RemainingTests.SequenceEqual(other.RemainingTests);
                public override int GetHashCode()
                {
                    int length = this.RemainingTests.Length;
                    int value = Hash.Combine(length, this.GetType().GetHashCode());
                    value = Hash.Combine(Hash.CombineValues(this.RemainingTests), value);
                    return value;
                }
            }

            /// <summary>
            /// A sequence of tests that must be performed, each of which must succeed.
            /// The sequence is deemed to succeed if no element fails.
            /// </summary>
            public sealed class AndSequence : SequenceTests
            {
                private AndSequence(ImmutableArray<Tests> remainingTests) : base(remainingTests) { }
                public override Tests Update(ArrayBuilder<Tests> remainingTests) => Create(remainingTests);
                public static Tests Create(ArrayBuilder<Tests> remainingTests)
                {
                    for (int i = remainingTests.Count - 1; i >= 0; i--)
                    {
                        switch (remainingTests[i])
                        {
                            case True _:
                                remainingTests.RemoveAt(i);
                                break;
                            case False f:
                                remainingTests.Free();
                                return f;
                            case AndSequence seq:
                                var testsToInsert = seq.RemainingTests;
                                remainingTests.RemoveAt(i);
                                for (int j = 0, n = testsToInsert.Length; j < n; j++)
                                    remainingTests.Insert(i + j, testsToInsert[j]);
                                break;
                        }
                    }
                    var result = remainingTests.Count switch
                    {
                        0 => True.Instance,
                        1 => remainingTests[0],
                        _ => new AndSequence(remainingTests.ToImmutable()),
                    };
                    remainingTests.Free();
                    return result;
                }
                public override BoundDagTest ComputeSelectedTest()
                {
                    // Our simple heuristic is to perform the first test of the
                    // first possible matched case, with two exceptions.

                    if (RemainingTests[0] is One { Test: { Kind: BoundKind.DagNonNullTest } planA })
                    {
                        switch (RemainingTests[1])
                        {
                            // In the specific case of a null check following by a type test, we skip the
                            // null check and perform the type test directly.  That's because the type test
                            // has the side-effect of performing the null check for us.
                            case One { Test: { Kind: BoundKind.DagTypeTest } planB1 }:
                                return (planA.Input == planB1.Input) ? planB1 : planA;

                            // In the specific case of a null check following by a value test (which occurs for
                            // pattern matching a string constant pattern), we skip the
                            // null check and perform the value test directly.  That's because the value test
                            // has the side-effect of performing the null check for us.
                            case One { Test: { Kind: BoundKind.DagValueTest } planB2 }:
                                return (planA.Input == planB2.Input) ? planB2 : planA;
                        }
                    }

                    return RemainingTests[0].ComputeSelectedTest();
                }
                public override string Dump(Func<BoundDagTest, string> dump)
                {
                    return $"AND({string.Join(", ", RemainingTests.Select(t => t.Dump(dump)))})";
                }
            }

            /// <summary>
            /// A sequence of tests that must be performed, any of which must succeed.
            /// The sequence is deemed to succeed if some element succeeds.
            /// </summary>
            public sealed class OrSequence : SequenceTests
            {
                private OrSequence(ImmutableArray<Tests> remainingTests) : base(remainingTests) { }
                public override BoundDagTest ComputeSelectedTest() => this.RemainingTests[0].ComputeSelectedTest();
                public override Tests Update(ArrayBuilder<Tests> remainingTests) => Create(remainingTests);
                public static Tests Create(ArrayBuilder<Tests> remainingTests)
                {
                    for (int i = remainingTests.Count - 1; i >= 0; i--)
                    {
                        switch (remainingTests[i])
                        {
                            case False _:
                                remainingTests.RemoveAt(i);
                                break;
                            case True t:
                                remainingTests.Free();
                                return t;
                            case OrSequence seq:
                                remainingTests.RemoveAt(i);
                                var testsToInsert = seq.RemainingTests;
                                for (int j = 0, n = testsToInsert.Length; j < n; j++)
                                    remainingTests.Insert(i + j, testsToInsert[j]);
                                break;
                        }
                    }
                    var result = remainingTests.Count switch
                    {
                        0 => False.Instance,
                        1 => remainingTests[0],
                        _ => new OrSequence(remainingTests.ToImmutable()),
                    };
                    remainingTests.Free();
                    return result;
                }
                public override string Dump(Func<BoundDagTest, string> dump)
                {
                    return $"OR({string.Join(", ", RemainingTests.Select(t => t.Dump(dump)))})";
                }
            }
        }
    }
}
