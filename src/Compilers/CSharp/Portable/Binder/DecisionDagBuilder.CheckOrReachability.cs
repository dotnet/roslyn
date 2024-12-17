// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // TODO2 after review, rename file to use underscore separator
    internal sealed partial class DecisionDagBuilder
    {
        /// <summary>
        /// For patterns that contain a disjunction `... or ...` we're going to perform reachability analysis for each branch of the `or`.
        /// We effectively pick each analyzable `or` sequence in turn and expand it to top-level cases.
        ///
        /// For example, `A and (B or C)` is expanded to two cases: `A and B` and `A and C`.
        ///
        /// Similarly, `(A or B) and (C or D)` is expanded to two sets of two cases:
        ///   1. { `case A`, `case B` } (we can truncate later test since we only care about the reachability of `A` and `B` here)
        ///   2. { `case (A or B) and C`, `case (A or B) and D` }
        /// We then check the reachability for each of those cases in different sets.
        /// </summary>
        internal static void CheckRedundantPatternsForIsPattern(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression inputExpression,
            BoundPattern pattern,
            BindingDiagnosticBag diagnostics)
        {
            if (pattern.HasErrors)
            {
                return;
            }

            LabelSymbol defaultLabel = new GeneratedLabelSymbol("isPatternFailure");
            var builder = new DecisionDagBuilder(compilation, defaultLabel: defaultLabel, forLowering: false, BindingDiagnosticBag.Discarded);
            BoundDagTemp rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);

            var noPreviousCases = ArrayBuilder<StateForCase>.GetInstance(0);
            CheckOrAndAndReachability(noPreviousCases, patternIndex: 0, pattern: pattern, builder: builder, rootIdentifier: rootIdentifier, syntax: syntax, diagnostics: diagnostics);
            noPreviousCases.Free();
        }

        /// <summary>
        /// <see cref="CheckRedundantPatternsForIsPattern"/>
        /// </summary>
        internal static void CheckRedundantPatternsForSwitchExpression(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression inputExpression,
            ImmutableArray<BoundSwitchExpressionArm> switchArms,
            BindingDiagnosticBag diagnostics)
        {
            LabelSymbol defaultLabel = new GeneratedLabelSymbol("isPatternFailure");
            var builder = new DecisionDagBuilder(compilation, defaultLabel: defaultLabel, forLowering: false, BindingDiagnosticBag.Discarded);
            BoundDagTemp rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);

            var existingCases = ArrayBuilder<StateForCase>.GetInstance(switchArms.Length);
            int index = 0;
            foreach (var switchArm in switchArms)
            {
                if (switchArm.Pattern.HasErrors)
                {
                    return;
                }

                existingCases.Add(builder.MakeTestsForPattern(++index, switchArm.Syntax, rootIdentifier, switchArm.Pattern, whenClause: switchArm.WhenClause, label: switchArm.Label));
            }

            for (int patternIndex = 0; patternIndex < switchArms.Length; patternIndex++)
            {
                CheckOrAndAndReachability(existingCases, patternIndex, switchArms[patternIndex].Pattern, builder, rootIdentifier, syntax, diagnostics);
            }

            existingCases.Free();
        }

        /// <summary>
        /// <see cref="CheckRedundantPatternsForIsPattern"/>
        /// </summary>
        internal static void CheckRedundantPatternsForSwitchStatement(
            CSharpCompilation compilation,
            SyntaxNode syntax,
            BoundExpression inputExpression,
            ImmutableArray<BoundSwitchSection> switchSections,
            BindingDiagnosticBag diagnostics)
        {
            LabelSymbol defaultLabel = new GeneratedLabelSymbol("isPatternFailure");
            var builder = new DecisionDagBuilder(compilation, defaultLabel: defaultLabel, forLowering: false, BindingDiagnosticBag.Discarded);
            BoundDagTemp rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);

            var existingCases = ArrayBuilder<StateForCase>.GetInstance();
            int index = 0;
            foreach (BoundSwitchSection section in switchSections)
            {
                foreach (BoundSwitchLabel label in section.SwitchLabels)
                {
                    if (label.Syntax.Kind() != SyntaxKind.DefaultSwitchLabel)
                    {
                        if (label.Pattern.HasErrors)
                        {
                            return;
                        }

                        existingCases.Add(builder.MakeTestsForPattern(++index, label.Syntax, rootIdentifier, label.Pattern, label.WhenClause, label.Label));
                    }
                }
            }

            int patternIndex = 0;
            foreach (BoundSwitchSection section in switchSections)
            {
                foreach (BoundSwitchLabel label in section.SwitchLabels)
                {
                    if (label.Syntax.Kind() != SyntaxKind.DefaultSwitchLabel)
                    {
                        CheckOrAndAndReachability(existingCases, patternIndex, label.Pattern, builder, rootIdentifier, syntax, diagnostics);
                        patternIndex++;
                    }
                }
            }

            existingCases.Free();
        }

        private static void CheckOrAndAndReachability(
          ArrayBuilder<StateForCase> previousCases,
          int patternIndex,
          BoundPattern pattern,
          DecisionDagBuilder builder,
          BoundDagTemp rootIdentifier,
          SyntaxNode syntax,
          BindingDiagnosticBag diagnostics)
        {
            CheckOrReachability(previousCases, patternIndex, pattern,
                builder, rootIdentifier, syntax, diagnostics);

            var negated = new BoundNegatedPattern(pattern.Syntax, negated: pattern, pattern.InputType, narrowedType: pattern.InputType);
            CheckOrReachability(previousCases, patternIndex, negated,
                builder, rootIdentifier, syntax, diagnostics);
        }

        private static void CheckOrReachability(
            ArrayBuilder<StateForCase> previousCases,
            int patternIndex,
            BoundPattern pattern,
            DecisionDagBuilder builder,
            BoundDagTemp rootIdentifier,
            SyntaxNode syntax,
            BindingDiagnosticBag diagnostics)
        {
            var normalizedPattern = PatternNormalizer.Rewrite(pattern, rootIdentifier.Type);

            SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(normalizedPattern, builder._conversions);
            if (setOfOrCases.IsDefault)
            {
                return;
            }

            // We construct a DAG and analyze reachability of branches once per `or` sequence
            Debug.Assert(!setOfOrCases.IsDefault);
            foreach (SetOfOrCases orCases in setOfOrCases.Set)
            {
                using var casesBuilder = TemporaryArray<StateForCase>.GetInstance(orCases.Cases.Count);
                var labelsToIgnore = PooledHashSet<LabelSymbol>.GetInstance();
                populateStateForCases(builder, rootIdentifier, previousCases, patternIndex, orCases, labelsToIgnore, syntax, ref casesBuilder.AsRef());
                BoundDecisionDag dag = builder.MakeBoundDecisionDag(syntax, ref casesBuilder.AsRef());

                foreach (StateForCase @case in casesBuilder)
                {
                    if (!dag.ReachableLabels.Contains(@case.CaseLabel) && !labelsToIgnore.Contains(@case.CaseLabel))
                    {
                        ErrorCode errorCode = detectNotOrPattern(@case.Syntax) ? ErrorCode.WRN_RedundantPattern : ErrorCode.HDN_RedundantPattern;
                        diagnostics.Add(errorCode, @case.Syntax);
                    }
                }

                labelsToIgnore.Free();
            }

            return;

            static void populateStateForCases(DecisionDagBuilder builder, BoundDagTemp rootIdentifier, ArrayBuilder<StateForCase> previousCases, int patternIndex,
                SetOfOrCases set, PooledHashSet<LabelSymbol> labelsToIgnore, SyntaxNode nodeSyntax, ref TemporaryArray<StateForCase> casesBuilder)
            {
                for (int i = 0; i < patternIndex; i++)
                {
                    casesBuilder.Add(previousCases[i]);
                }

                int index = patternIndex;
                foreach ((BoundPattern pattern, SyntaxNode? syntax) in set.Cases)
                {
                    var label = new GeneratedLabelSymbol("orCase");
                    SyntaxNode? diagSyntax = syntax;
                    if (diagSyntax is null)
                    {
                        labelsToIgnore.Add(label);
                        diagSyntax = nodeSyntax;
                    }

                    Debug.Assert(diagSyntax is not null);
                    casesBuilder.Add(builder.MakeTestsForPattern(++index, diagSyntax, rootIdentifier, pattern, whenClause: null, label: label));
                }
            }

            // We need to reduce the break introduced by flagging redundant patterns
            // and we never want to affect people who express their patterns thoroughly (but correctly)
            // such as `switch { < 0 => -1, 0 => 0, > 0 => 1 }`
            // So we're only reporting a warning for situations that syntactically look hazardous
            // At the moment, we're only interested in patterns involved in a `not ... or ...` pattern

            // If the pattern is on the right of an `or` pattern, we walk up and
            // if any of the preceding patterns is a `not` pattern we've detected the error-prone not/or situation.
            // For example: `not A or <redundant>` or `(A or not B) or <redundant>` or `not B or (A or <redundant>)`
            static bool detectNotOrPattern(SyntaxNode syntax)
            {
start:
                syntax = unwrap(syntax);

                if (isRightOfOrPattern(syntax, out var binary)
                    && detectNotPatternOnLeftOfOrPattern(binary))
                {
                    return true;
                }

                Debug.Assert(syntax.Parent is not null);
                if (isLeftOfOrPattern(syntax, out _)
                    && isRightOfOrPattern(unwrap(syntax.Parent), out BinaryPatternSyntax? parentBinary)
                    && detectNotPatternOnLeftOfOrPattern(parentBinary))
                {
                    return true;
                }

                // If the syntax is the whole sub-pattern, we walk up to the recursive pattern.
                // For example: `not A or { Prop: <redundant> }`
                if (syntax.Parent is SubpatternSyntax subpatternSyntax
                    && subpatternSyntax.Parent is (PropertyPatternClauseSyntax or PositionalPatternClauseSyntax) and var patternClause
                    && patternClause.Parent is RecursivePatternSyntax recursive)
                {
                    syntax = recursive;
                    goto start;
                }

                return false;
            }

            // Detect a `not` at top-level or inside a tree of `or` patterns
            // Note: we don't dig into parenthesized patterns as the meaning of `not` is not problematic then
            static bool detectNotPatternInPattern(SyntaxNode syntax)
            {
                if (syntax.Kind() == SyntaxKind.NotPattern)
                {
                    return true;
                }

                if (syntax is BinaryPatternSyntax binarySyntax && binarySyntax.Kind() == SyntaxKind.OrPattern)
                {
                    if (detectNotPatternInPattern(binarySyntax.Left))
                    {
                        return true;
                    }

                    if (detectNotPatternInPattern(binarySyntax.Right))
                    {
                        return true;
                    }
                }

                return false;
            }

            static bool isRightOfOrPattern(SyntaxNode syntax, [NotNullWhen(true)] out BinaryPatternSyntax? binaryPattern)
            {
                if (syntax.Parent is BinaryPatternSyntax foundBinaryPattern
                    && foundBinaryPattern.Kind() == SyntaxKind.OrPattern
                    && foundBinaryPattern.Right == syntax)
                {
                    binaryPattern = foundBinaryPattern;
                    return true;
                }

                binaryPattern = null;
                return false;
            }

            static bool isLeftOfOrPattern(SyntaxNode syntax, [NotNullWhen(true)] out BinaryPatternSyntax? binaryPattern)
            {
                if (syntax.Parent is BinaryPatternSyntax foundBinaryPattern
                    && foundBinaryPattern.Kind() == SyntaxKind.OrPattern
                    && foundBinaryPattern.Left == syntax)
                {
                    binaryPattern = foundBinaryPattern;
                    return true;
                }

                binaryPattern = null;
                return false;
            }

            static bool detectNotPatternOnLeftOfOrPattern(BinaryPatternSyntax binarySyntax)
            {
                if (binarySyntax.Kind() != SyntaxKind.OrPattern)
                {
                    return false;
                }

                if (detectNotPatternInPattern(binarySyntax.Left))
                {
                    return true;
                }

                Debug.Assert(binarySyntax.Parent is not null);
                if (isRightOfOrPattern(unwrap(binarySyntax.Parent), out BinaryPatternSyntax? parentBinary)
                    && detectNotPatternOnLeftOfOrPattern(parentBinary))
                {
                    return true;
                }

                return false;
            }

            static SyntaxNode unwrap(SyntaxNode syntax)
            {
                while (syntax.Parent is ParenthesizedPatternSyntax parens)
                {
                    syntax = parens;
                }

                return syntax;
            }
        }

        /// <summary>
        /// The purpose of this method is to bring `or` sequences to the top-level (so they can be used as separate cases).
        /// Each set handles one `or`.
        /// </summary>
        private static SetsOfOrCases RewriteToSetsOfOrCases(BoundPattern? pattern, Conversions conversions)
        {
            return pattern switch
            {
                BoundBinaryPattern binary => rewriteBinary(binary, conversions),
                BoundRecursivePattern => default,
                BoundListPattern => default,
                BoundSlicePattern => default,
                BoundITuplePattern => default,
                BoundNegatedPattern => default,
                BoundTypePattern => default,
                BoundDeclarationPattern => default,
                BoundConstantPattern => default,
                BoundDiscardPattern => default,
                BoundRelationalPattern => default,
                null => default,
                _ => throw ExceptionUtilities.UnexpectedValue(pattern)
            };

            static SetsOfOrCases rewriteBinary(BoundBinaryPattern binaryPattern, Conversions conversions)
            {
                SetsOfOrCases result = default;

                if (binaryPattern.Disjunction)
                {
                    var patterns = ArrayBuilder<BoundPattern>.GetInstance();
                    addPatternsFromOrTree(binaryPattern, patterns);

                    // In `A1 or ... or An`, we produce an expansion set: `case A1`, ..., `case An`
                    SetOfOrCases resultOrSet1 = result.StartNewOrCases();
                    var inputType = binaryPattern.InputType;
                    foreach (var pattern in patterns)
                    {
                        resultOrSet1.Add(pattern, pattern.Syntax);
                    }

                    // In `A1 or ... or An or B or ...` where `B` has an expansion set: `case B1`, ..., `case Bn`
                    // we produce an expansion set: `case A1:`, ... `case An:`, `case B1:`, ... `case Bn:`
                    for (int i = 0; i < patterns.Count; i++)
                    {
                        BoundPattern pattern = patterns[i];
                        using SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(pattern, conversions);
                        if (!setOfOrCases.IsDefault)
                        {
                            foreach (SetOfOrCases orSet in setOfOrCases.Set)
                            {
                                SetOfOrCases resultOrSet = result.StartNewOrCases();

                                for (int j = 0; j < i; j++)
                                {
                                    resultOrSet.Add(patterns[j], syntax: null); // already checked reachability in the previous set
                                }

                                foreach ((BoundPattern resultPattern, SyntaxNode? syntax) in orSet.Cases)
                                {
                                    resultOrSet.Add(resultPattern, syntax);
                                }
                            }
                        }
                    }

                    patterns.Free();
                }
                else
                {
                    // In `A and B`, when found multiple `or` patterns in `A` to be expanded, we drop the `and B`
                    result = RewriteToSetsOfOrCases(binaryPattern.Left, conversions);

                    using SetsOfOrCases rightSetOfOrCases = RewriteToSetsOfOrCases(binaryPattern.Right, conversions);
                    if (!rightSetOfOrCases.IsDefault)
                    {
                        // In `A and B`, when found multiple `or` patterns in `B` to be expanded
                        // For each such nested expansion, we'll produce a `A and ...` expansion
                        foreach (SetOfOrCases expandedRightSet in rightSetOfOrCases.Set)
                        {
                            SetOfOrCases resultOrSet = result.StartNewOrCases();
                            foreach ((BoundPattern rewrittenPattern, SyntaxNode? syntax) in expandedRightSet.Cases)
                            {
                                var rewrittenBinary = new BoundBinaryPattern(binaryPattern.Syntax, disjunction: false, binaryPattern.Left, rewrittenPattern, binaryPattern.InputType, rewrittenPattern.NarrowedType);
                                resultOrSet.Add(rewrittenBinary, syntax);
                            }
                        }
                    }
                }

                return result;
            }

            static void addPatternsFromOrTree(BoundPattern pattern, ArrayBuilder<BoundPattern> builder)
            {
                if (pattern is BoundBinaryPattern { Disjunction: true } orPattern)
                {
                    var stack = ArrayBuilder<BoundBinaryPattern>.GetInstance();

                    BoundBinaryPattern? current = orPattern;
                    do
                    {
                        stack.Push(current);
                        current = current.Left as BoundBinaryPattern;
                    }
                    while (current != null && current.Disjunction);

                    current = stack.Pop();

                    Debug.Assert(current.Left is not BoundBinaryPattern { Disjunction: true });
                    builder.Add(current.Left);

                    do
                    {
                        addPatternsFromOrTree(current.Right, builder);
                    }
                    while (stack.TryPop(out current));

                    stack.Free();
                }
                else
                {
                    builder.Add(pattern);
                }
            }
        }

        /// <summary>
        /// When there are multiple `or` patterns, such as `(A or B) and (C or D)`
        /// we'll expand then two sets:
        /// 1. { `case A`, `case B` }
        /// 2. { `case (A or B) and C`, `case (A or B) and D` }
        /// Each set will be analyzed for reachability.
        /// A `default` <see cref="SetsOfOrCases"/> indicates that no `or` patterns were found (so there are no expansions).
        /// </summary>
        private struct SetsOfOrCases : IDisposable
        {
            public ArrayBuilder<SetOfOrCases>? Set;

            [MemberNotNullWhen(false, nameof(Set))]
            public bool IsDefault => Set is null;

            internal SetOfOrCases StartNewOrCases()
            {
                var orCases = new SetOfOrCases();
                Set ??= ArrayBuilder<SetOfOrCases>.GetInstance();
                Set.Add(orCases);
                return orCases;
            }

            public void Dispose()
            {
                if (Set is not null)
                {
                    foreach (SetOfOrCases set in Set)
                    {
                        set.Dispose();
                    }

                    Set.Free();
                }
            }

#if DEBUG
            public string Dump()
            {
                if (IsDefault)
                    return "DEFAULT";

                var builder = new StringBuilder();
                foreach (var set in Set)
                {
                    builder.AppendLine("Set:");
                    foreach (var @case in set.Cases)
                    {
                        builder.Append(@case.pattern.DumpSource());
                        if (@case.syntax is { } syntax)
                        {
                            builder.Append("  => ");
                            builder.Append(syntax.ToString());
                            builder.Append(", ");
                        }

                        builder.AppendLine();
                    }

                    builder.AppendLine();
                }

                return builder.ToString();
            }
#endif
        }

        /// <summary>
        /// When we have a single sequence of `or` patterns, such as `A or B or C`
        /// we can expand it to separate cases: `A`, `B` and `C`.
        /// This composes. So a nested `or` sequence can also be expanded: `X and (A or B or C)`
        /// can be expanded to `X and A`, `X and B` and `X and C`.
        /// </summary>
        private struct SetOfOrCases : IDisposable
        {
            public ArrayBuilder<(BoundPattern pattern, SyntaxNode? syntax)> Cases;

            public SetOfOrCases()
            {
                Cases = ArrayBuilder<(BoundPattern pattern, SyntaxNode? syntax)>.GetInstance();
            }

            public void Add(BoundPattern pattern, SyntaxNode? syntax)
            {
                Cases.Add((pattern, pattern.WasCompilerGenerated ? null : syntax));
            }

            public void Dispose()
            {
                Cases.Free();
            }
        }

        // The purpose of this rewriter is to push `not` patterns down the pattern tree and
        // pull all the `and` and `or` patterns up the pattern tree.
        // It needs to expand composite patterns in the process.
        // It also erases/simplifies some patterns (variable declarations).
        //
        // For example, given `not { Prop: 42 or 43 }`
        // it produces `not null or ({ Prop: not 42 } and { Prop: not 43 })`.
        //
        // A Visit should produce an eval result using the original InputType. When adjustments are needed,
        //   that is the responsibility of the caller of Visit.
        //
        // Some synthesized patterns are marked as compiler-generated so they will not be reported as redundant.
        private class PatternNormalizer : BoundTreeWalkerWithStackGuard
        {
            /// <summary>
            /// Set whenever we request Visit to negate the given pattern.
            /// </summary>
            private bool _negated;

            /// <summary>
            /// Set to true when the evaluations yielded by Visit will be combined with `or`.
            /// Set to false when the evaluations yielded by Visit will be combined with `and`.
            /// Set to null otherwise.
            ///
            /// This lets us drop values that are skipped: discard (always true) in `and` and negated discard (always false) in `or`.
            /// </summary>
            private bool? _expectingOperandOfDisjunction;

            /// <summary>
            /// We visiting a composite pattern such as `{ Prop: A or B }`
            /// we'll want to push operand `{ Prop: A }`, operand `{ Prop: B }` and operation `or`
            /// in the eval sequence.
            /// This delegate is set before calling Visit on the nested pattern. The delegate returns
            /// the operand that will go into the eval stack.
            ///
            /// For example, when visiting recursive property pattern `Prop:`, we'll use a delegate
            /// that transforms pattern `A` into pattern `{ Prop: A }`.
            /// </summary>
            private Func<BoundPattern, BoundPattern>? _makeEvaluationSequenceOperand;

            /// <summary>
            /// The eval sequence contains operands and binary operations.
            /// It is populated by Visit.
            /// It is turned back into a BoundPattern by <see cref="GetResult(TypeSymbol)"/>.
            ///
            /// For example, if Visit populated:
            /// [pattern1, pattern2, `or`, pattern3, `and`]
            /// which is used by GetResult to produce:
            ///     new BoundBinaryPattern(disjunction: false,
            ///         new BoundBinaryPattern(disjunction: true,
            ///             pattern1,
            ///             pattern2),
            ///         pattern3)
            /// </summary>
            private readonly ArrayBuilder<OperandOrOperation> _evalSequence = ArrayBuilder<OperandOrOperation>.GetInstance();

            private struct OperandOrOperation
            {
                private readonly BoundPattern? _operand;
                private readonly bool? _disjunction;
                private readonly SyntaxNode? _operationSyntax;

                /// <summary>
                /// When we find that an operand or operation is on the left of a binary operation
                /// we record whether the binary operation is a disjunction.
                /// This makes it easier to track input types while processing the eval sequence.
                /// </summary>
                public bool? OnTheLeftOfDisjunction { get; set { Debug.Assert(field is null); field = value; } }

                private OperandOrOperation(BoundPattern? operand, bool? disjunction, SyntaxNode? operationSyntax)
                {
                    _operand = operand;
                    _disjunction = disjunction;
                    _operationSyntax = operationSyntax;
                }

                public static OperandOrOperation CreateOperation(bool disjunction, SyntaxNode operationSyntax)
                {
                    return new OperandOrOperation(null, disjunction, operationSyntax);
                }

                public static OperandOrOperation CreateOperand(BoundPattern operand)
                {
                    return new OperandOrOperation(operand, null, null);
                }

                public bool IsOperand([NotNullWhen(true)] out BoundPattern? operand)
                {
                    if (_operand is not null)
                    {
                        operand = _operand;
                        return true;
                    }

                    operand = null;
                    return false;
                }

                public bool IsOperation(out bool disjunction, [NotNullWhen(true)] out SyntaxNode? operationSyntax)
                {
                    if (_operand is null)
                    {
                        Debug.Assert(_disjunction.HasValue);
                        disjunction = _disjunction.Value;
                        Debug.Assert(_operationSyntax is not null);
                        operationSyntax = _operationSyntax;
                        return true;
                    }

                    disjunction = false;
                    operationSyntax = null;
                    return false;
                }
            }

            private PatternNormalizer()
            {
            }

            internal static BoundPattern Rewrite(BoundPattern pattern, TypeSymbol inputType)
            {
                var patternNormalizer = new PatternNormalizer();
                patternNormalizer.Visit(pattern);
                return patternNormalizer.GetResult(inputType);
            }

            private BoundPattern GetResult(TypeSymbol inputType)
            {
                Debug.Assert(_evalSequence is [var first, ..] && first.IsOperand(out _));

                var stack = ArrayBuilder<BoundPattern>.GetInstance();

                int evalPosition = 0;
                do
                {
                    OperandOrOperation operandOrOperation = _evalSequence[evalPosition];

                    if (operandOrOperation.IsOperation(out bool disjunction, out SyntaxNode? operationSyntax))
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();
                        TypeSymbol narrowedType = narrowedTypeForBinary(left, right, disjunction);
                        stack.Push(new BoundBinaryPattern(operationSyntax, disjunction, left, right, left.InputType, narrowedType));
                    }
                    else if (operandOrOperation.IsOperand(out BoundPattern? operand))
                    {
                        stack.Push(WithInputTypeCheckIfNeeded(operand, inputType));
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(operandOrOperation);
                    }

                    if (operandOrOperation.OnTheLeftOfDisjunction is bool onTheLeftOfDisjunction)
                    {
                        if (onTheLeftOfDisjunction)
                        {
                            inputType = stack.Peek().InputType;
                        }
                        else
                        {
                            inputType = stack.Peek().NarrowedType;
                        }
                    }

                    evalPosition++;
                }
                while (evalPosition < _evalSequence.Count);

                var result = stack.Single();
                stack.Free();
                _evalSequence.Free();
                return result;

                static TypeSymbol narrowedTypeForBinary(BoundPattern resultLeft, BoundPattern resultRight, bool resultDisjunction)
                {
                    TypeSymbol narrowedType;
                    if (resultDisjunction)
                    {
                        if (resultRight.NarrowedType.Equals(resultLeft.NarrowedType, TypeCompareKind.AllIgnoreOptions))
                        {
                            return resultLeft.NarrowedType;
                        }

                        return resultLeft.InputType;
                    }
                    else
                    {
                        narrowedType = resultRight.NarrowedType;
                    }

                    return narrowedType;
                }
            }

            public override BoundNode? Visit(BoundNode? node)
            {
                Debug.Assert(node is BoundBinaryPattern
                    or BoundRecursivePattern
                    or BoundListPattern
                    or BoundSlicePattern
                    or BoundITuplePattern
                    or BoundConstantPattern
                    or BoundDeclarationPattern
                    or BoundDiscardPattern
                    or BoundTypePattern
                    or BoundRelationalPattern
                    or BoundNegatedPattern, $"This rewriter doesn't support pattern {node} yet.");

                return base.Visit(node);
            }

            // Updates the eval sequence from [...eval sequence...]
            // to [...eval sequence..., ...eval for left..., ...eval for right..., binaryOperation]
            // in the general case.
            //
            // If the left is a skipped, then update it to
            // [...eval sequence..., ...eval for right...]
            //
            // If the right is a skipped, then update it to
            // [...eval sequence..., ...eval for left...]
            //
            // If both are skipped, then no change occurs.
            public override BoundNode? VisitBinaryPattern(BoundBinaryPattern node)
            {
                bool disjunction = node.Disjunction;

                if (_negated)
                {
                    disjunction = !disjunction;
                }

                var stack = ArrayBuilder<BoundBinaryPattern>.GetInstance();

                BoundBinaryPattern? current = node;
                do
                {
                    stack.Push(current);
                    current = current.Left as BoundBinaryPattern;
                }
                while (current != null && current.Disjunction == node.Disjunction);

                var saveExpectingOperandOfDisjunction = _expectingOperandOfDisjunction;
                _expectingOperandOfDisjunction = disjunction;

                current = stack.Pop();
                Debug.Assert((current.Left is BoundBinaryPattern binary && binary.Disjunction != node.Disjunction) || current.Left is not BoundBinaryPattern);

                bool skippedAllLeft = true;

                int startOfLeft = _evalSequence.Count;
                Visit(current.Left);
                int endOfLeft = _evalSequence.Count - 1;

                if (startOfLeft <= endOfLeft)
                {
                    skippedAllLeft = false;
                }

                do
                {
                    if (skippedAllLeft && stack.IsEmpty)
                    {
                        _expectingOperandOfDisjunction = saveExpectingOperandOfDisjunction;
                    }

                    int startOfRight = _evalSequence.Count;
                    Visit(current.Right);
                    int endOfRight = _evalSequence.Count - 1;

                    if (!skippedAllLeft && startOfRight <= endOfRight)
                    {
                        PushBinaryOperation(node.Syntax, endOfLeft, disjunction);
                    }

                    startOfLeft = startOfRight;
                    endOfLeft = endOfRight;

                    if (startOfLeft <= endOfLeft)
                    {
                        skippedAllLeft = false;
                    }
                }
                while (stack.TryPop(out current));

                stack.Free();

                _expectingOperandOfDisjunction = saveExpectingOperandOfDisjunction;

                return null;
            }

            // Updates the eval sequence from [..., endOfLeft, ...]
            // to [..., endOfLeft /*marked on the left of <disjunction>*/, ..., operation]
            private void PushBinaryOperation(SyntaxNode syntax, int endOfLeft, bool disjunction)
            {
                var left = _evalSequence[endOfLeft];
                left.OnTheLeftOfDisjunction = disjunction;
                _evalSequence[endOfLeft] = left;

                _evalSequence.Push(OperandOrOperation.CreateOperation(disjunction, syntax));
            }

            // Updates the eval sequence from [...sequence...]
            // to [...sequence..., pattern]
            // unless the pattern is considered no-op.
            private void TryPushOperand(BoundPattern pattern)
            {
                switch (_expectingOperandOfDisjunction)
                {
                    case true:
                        if (pattern is BoundNegatedPattern { Negated: BoundDiscardPattern })
                        {
                            return;
                        }
                        break;

                    case false:
                        if (pattern is BoundDiscardPattern)
                        {
                            return;
                        }
                        break;
                }

                _evalSequence.Push(OperandOrOperation.CreateOperand(_makeEvaluationSequenceOperand?.Invoke(pattern) ?? pattern));
            }

            public override BoundNode? VisitNegatedPattern(BoundNegatedPattern node)
            {
                var savedNegated = _negated;
                _negated = !_negated;
                this.Visit(node.Negated);
                _negated = savedNegated;
                return null;
            }

            public BoundPattern NegateIfNeeded(BoundPattern node)
            {
                if (!_negated)
                {
                    return node;
                }

                if (node is BoundNegatedPattern { Negated: var negated })
                {
                    return negated;
                }

                var result = new BoundNegatedPattern(node.Syntax, node, node.InputType, narrowedType: node.InputType);
                if (node.WasCompilerGenerated)
                {
                    result.MakeCompilerGenerated();
                }

                return result;
            }

            public override BoundNode? VisitTypePattern(BoundTypePattern node)
            {
                TryPushOperand(NegateIfNeeded(node));
                return null;
            }

            public override BoundNode? VisitConstantPattern(BoundConstantPattern node)
            {
                TryPushOperand(NegateIfNeeded(node));
                return null;
            }

            public override BoundNode? VisitDiscardPattern(BoundDiscardPattern node)
            {
                TryPushOperand(NegateIfNeeded(node));
                return null;
            }

            public override BoundNode? VisitRelationalPattern(BoundRelationalPattern node)
            {
                TryPushOperand(NegateIfNeeded(node));
                return null;
            }

            // Given a pattern that expects `pattern.InputType` as input type, produce
            // one that expects `inputType.
            //
            // For example, when splitting a pattern `{ Prop: A and B }` into `{ Prop: A } and/or { Prop: B }`
            // the original `B` pattern expects an input type that is the narrowed type from `A`
            // but the rewritten `B` pattern expects an input type that is the type of `Prop`.
            private static BoundPattern WithInputTypeCheckIfNeeded(BoundPattern pattern, TypeSymbol inputType)
            {
                if (pattern.InputType.Equals(inputType, TypeCompareKind.AllIgnoreOptions))
                {
                    return pattern;
                }

                if (pattern is BoundTypePattern typePattern1)
                {
                    return typePattern1.Update(typePattern1.DeclaredType, typePattern1.IsExplicitNotNullTest, inputType, typePattern1.NarrowedType);
                }

                if (pattern is BoundRecursivePattern recursivePattern)
                {
                    return recursivePattern.Update(
                        recursivePattern.DeclaredType ??
                            new BoundTypeExpression(recursivePattern.Syntax, aliasOpt: null, recursivePattern.InputType.StrippedType()),
                        recursivePattern.DeconstructMethod, recursivePattern.Deconstruction,
                        recursivePattern.Properties, recursivePattern.IsExplicitNotNullTest,
                        recursivePattern.Variable, recursivePattern.VariableAccess,
                        inputType, recursivePattern.NarrowedType);
                }

                if (pattern is BoundDiscardPattern discardPattern)
                {
                    return discardPattern.Update(inputType, inputType);
                }

                if (pattern is BoundNegatedPattern negatedPattern)
                {
                    return negatedPattern.Update(
                        WithInputTypeCheckIfNeeded(negatedPattern.Negated, inputType),
                        inputType, inputType);
                }

                Debug.Assert(pattern is BoundConstantPattern or BoundRelationalPattern or BoundITuplePattern or BoundListPattern);

                // Produce `PatternInputType and pattern` given a new input type

                BoundPattern typePattern = new BoundTypePattern(pattern.Syntax,
                    new BoundTypeExpression(pattern.Syntax, aliasOpt: null, pattern.InputType),
                    isExplicitNotNullTest: false, inputType, narrowedType: pattern.InputType).MakeCompilerGenerated();

                var result = new BoundBinaryPattern(pattern.Syntax, disjunction: false, left: typePattern, right: pattern, inputType, pattern.NarrowedType);

                if (pattern.WasCompilerGenerated)
                {
                    result = result.MakeCompilerGenerated();
                }

                return result;
            }

            // If the type of the declaration pattern implies a meaningful type test
            // then we keep it.
            // Otherwise, we push a discard.
            public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
            {
                TryPushOperand(NegateIfNeeded(MakeDiscardPattern(node).MakeCompilerGenerated()));
                return null;
            }

            public override BoundNode? VisitRecursivePattern(BoundRecursivePattern node)
            {
                // If we're starting with `Type (D1, D2, ...) { Prop1: P1, Prop2: P2, ... } x`
                // - if we are not negating, we can expand it to 
                //   `Type and Type (D1, _, ...) and Type (_, D2, ...) and Type { Prop1: P1 } and Type { Prop2: P2 } ...` 
                //
                // - if we are negating, we can expand it to 
                //   `not Type or Type (not D1, _, ...) or Type (_, not D2, ...) or Type { Prop1: not P1 } or Type { Prop2: not P2 } or ...`
                //   and the `and` and `or` patterns in the sub-patterns can then be lifted out further.
                //   For example, if `not D1` resolves to `E1 or F1`, the `Type (not D1, _, ...)` component can be normalized to
                //   `Type (E1, _, ...) or `Type (F1, _, ...)`.
                //   If there's not Type, we substitute a null check

                var saveExpectingOperandOfDisjunction = _expectingOperandOfDisjunction;
                int startOfLeft = _evalSequence.Count; // all the operands we push from here will be combined in `or` for negation and `and` otherwise

                bool isEmptyPropertyPattern = node.Deconstruction.IsDefault && node.Properties is { IsDefault: false, IsEmpty: true };
                if (_negated)
                {
                    _expectingOperandOfDisjunction = true;

                    if (node.DeclaredType is not null)
                    {
                        // `not Type`
                        TryPushOperand(new BoundNegatedPattern(node.Syntax,
                            new BoundTypePattern(node.Syntax, node.DeclaredType, node.IsExplicitNotNullTest, node.InputType, node.NarrowedType, node.HasErrors),
                            node.InputType, narrowedType: node.InputType, node.HasErrors).MakeCompilerGenerated());
                    }
                    else if (node.InputType.CanContainNull())
                    {
                        // `null`
                        BoundConstantPattern nullPattern = new BoundConstantPattern(node.Syntax,
                            new BoundLiteral(node.Syntax, constantValueOpt: ConstantValue.Null, type: null, hasErrors: false),
                            ConstantValue.Null, node.InputType, node.InputType, hasErrors: false);

                        if (!isEmptyPropertyPattern)
                        {
                            nullPattern = nullPattern.MakeCompilerGenerated();
                        }

                        TryPushOperand(nullPattern);
                    }
                }
                else
                {
                    _expectingOperandOfDisjunction = false;
                    if (node.DeclaredType is not null)
                    {
                        // `Type`
                        TryPushOperand(new BoundTypePattern(node.Syntax, node.DeclaredType, node.IsExplicitNotNullTest, node.InputType, node.NarrowedType, node.HasErrors));
                    }
                }

                ImmutableArray<BoundPositionalSubpattern> deconstruction = node.Deconstruction;
                if (!deconstruction.IsDefault)
                {
                    var discards = deconstruction.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d.Syntax, d.Pattern.InputType)));
                    var saveMakeEvaluationSequenceOperand = _makeEvaluationSequenceOperand;

                    int i = 0;

                    // Given `newPattern`, produce `(..., _, newPattern, _, ...)`
                    _makeEvaluationSequenceOperand = (BoundPattern newPattern) =>
                    {
                        newPattern = WithInputTypeCheckIfNeeded(newPattern, deconstruction[i].Pattern.InputType);
                        ImmutableArray<BoundPositionalSubpattern> newSubPatterns = discards.SetItem(i, deconstruction[i].WithPattern(newPattern));

                        BoundPattern newRecursive = new BoundRecursivePattern(
                            newPattern.Syntax, declaredType: node.DeclaredType, deconstructMethod: node.DeconstructMethod,
                            deconstruction: newSubPatterns,
                            properties: default, isExplicitNotNullTest: false, variable: null, variableAccess: null,
                            node.InputType, node.NarrowedType, node.HasErrors);

                        if (newPattern.WasCompilerGenerated)
                        {
                            newRecursive = newRecursive.MakeCompilerGenerated();
                        }

                        return saveMakeEvaluationSequenceOperand?.Invoke(newRecursive) ?? newRecursive;
                    };

                    for (; i < deconstruction.Length; i++)
                    {
                        VisitPatternAndCombine(node.Syntax, deconstruction[i].Pattern, startOfLeft);
                    }

                    _makeEvaluationSequenceOperand = saveMakeEvaluationSequenceOperand;
                }

                if (!node.Properties.IsDefault)
                {
                    var saveMakeEvaluationSequenceOperand = _makeEvaluationSequenceOperand;
                    BoundPropertySubpattern? property = null;

                    // Given `newPattern`, produce `{ Prop: newPattern }`
                    _makeEvaluationSequenceOperand = (BoundPattern newPattern) =>
                    {
                        newPattern = WithInputTypeCheckIfNeeded(newPattern, property!.Pattern.InputType);
                        ImmutableArray<BoundPropertySubpattern> newSubPatterns = [property.WithPattern(newPattern)];

                        BoundPattern newRecursive = new BoundRecursivePattern(
                            newPattern.Syntax, declaredType: node.DeclaredType, deconstructMethod: null, deconstruction: default,
                            properties: newSubPatterns,
                            isExplicitNotNullTest: false, variable: null, variableAccess: null,
                            node.InputType, node.NarrowedType, node.HasErrors);

                        if (newPattern.WasCompilerGenerated)
                        {
                            newRecursive = newRecursive.MakeCompilerGenerated();
                        }

                        return saveMakeEvaluationSequenceOperand?.Invoke(newRecursive) ?? newRecursive;
                    };

                    foreach (BoundPropertySubpattern subPattern in node.Properties)
                    {
                        property = subPattern;
                        VisitPatternAndCombine(node.Syntax, property.Pattern, startOfLeft);
                    }

                    _makeEvaluationSequenceOperand = saveMakeEvaluationSequenceOperand;
                }

                _expectingOperandOfDisjunction = saveExpectingOperandOfDisjunction;

                if (_evalSequence.Count - 1 < startOfLeft)
                {
                    // Everything was skipped
                    if (node.Variable is null)
                    {
                        TryPushOperand(node);
                    }
                    else
                    {
                        TryPushOperand(MakeDefaultPattern(node.Syntax, node.InputType));
                    }
                }

                return null;
            }

            private void VisitPatternAndCombine(SyntaxNode syntax, BoundPattern pattern, int startOfLeft)
            {
                int endOfLeft = _evalSequence.Count - 1;
                int startOfRight = _evalSequence.Count;
                Visit(pattern);
                int endOfRight = _evalSequence.Count - 1;

                if (endOfLeft < startOfLeft || endOfRight < startOfRight)
                {
                    // Left or right or both are skipped
                    return;
                }

                PushBinaryOperation(syntax, endOfLeft, disjunction: _negated);
            }

            private void TryPushOperandAndCombine(SyntaxNode syntax, BoundPattern pattern, int startOfLeft)
            {
                int endOfLeft = _evalSequence.Count - 1;
                TryPushOperand(pattern);

                if (endOfLeft < startOfLeft)
                {
                    // Left is are skipped
                    return;
                }

                PushBinaryOperation(syntax, endOfLeft, disjunction: _negated);
            }

            private BoundPattern MakeDefaultPattern(SyntaxNode syntax, TypeSymbol inputType)
            {
                BoundDiscardPattern discard = MakeDiscardPattern(syntax, inputType).MakeCompilerGenerated();
                if (_negated)
                {
                    return new BoundNegatedPattern(syntax, discard, inputType, narrowedType: inputType).MakeCompilerGenerated();
                }
                else
                {
                    return discard;
                }
            }

            public override BoundNode? VisitPropertySubpattern(BoundPropertySubpattern node)
            {
                throw ExceptionUtilities.Unreachable();
            }

            public override BoundNode? VisitPositionalSubpattern(BoundPositionalSubpattern node)
            {
                throw ExceptionUtilities.Unreachable();
            }

            public override BoundNode? VisitITuplePattern(BoundITuplePattern ituplePattern)
            {
                var saveExpectingOperandOfDisjunction = _expectingOperandOfDisjunction;
                int startOfLeft = _evalSequence.Count; // all the operands we push from here will be combined in `or` for negation and `and` otherwise
                var subpatterns = ituplePattern.Subpatterns;
                var discards = subpatterns.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d.Syntax, d.Pattern.InputType)));

                if (_negated)
                {
                    _expectingOperandOfDisjunction = true;

                    if (ituplePattern.InputType.CanContainNull())
                    {
                        // `null`
                        TryPushOperand(new BoundConstantPattern(ituplePattern.Syntax,
                            new BoundLiteral(ituplePattern.Syntax, constantValueOpt: null, type: null, hasErrors: false),
                            ConstantValue.Null, ituplePattern.InputType, ituplePattern.InputType, hasErrors: false).MakeCompilerGenerated());
                    }

                    int leftEndBeforeLengthCheck = _evalSequence.Count - 1;

                    // `not (_, ..., _)` (a Length check)
                    var notLengthPattern = new BoundNegatedPattern(ituplePattern.Syntax,
                        new BoundITuplePattern(ituplePattern.Syntax, ituplePattern.GetLengthMethod, ituplePattern.GetItemMethod, discards,
                            ituplePattern.InputType, ituplePattern.NarrowedType),
                        ituplePattern.InputType, ituplePattern.InputType).MakeCompilerGenerated();

                    TryPushOperandAndCombine(ituplePattern.Syntax, notLengthPattern, startOfLeft);
                }
                else
                {
                    _expectingOperandOfDisjunction = false;
                }

                var saveMakeEvaluationSequenceOperand = _makeEvaluationSequenceOperand;
                int i = 0;

                _makeEvaluationSequenceOperand = (BoundPattern newPattern) =>
                {
                    newPattern = WithInputTypeCheckIfNeeded(newPattern, subpatterns[i].Pattern.InputType);
                    ImmutableArray<BoundPositionalSubpattern> newSubpatterns = discards.SetItem(i, subpatterns[i].WithPattern(newPattern));

                    BoundPattern newITuple = new BoundITuplePattern(newPattern.Syntax, ituplePattern.GetLengthMethod,
                        ituplePattern.GetItemMethod, newSubpatterns, ituplePattern.InputType, ituplePattern.NarrowedType);

                    if (newPattern.WasCompilerGenerated)
                    {
                        newITuple = newITuple.MakeCompilerGenerated();
                    }

                    return saveMakeEvaluationSequenceOperand?.Invoke(newITuple) ?? newITuple;
                };

                for (; i < subpatterns.Length; i++)
                {
                    VisitPatternAndCombine(ituplePattern.Syntax, subpatterns[i].Pattern, startOfLeft);
                }

                _makeEvaluationSequenceOperand = saveMakeEvaluationSequenceOperand;
                _expectingOperandOfDisjunction = saveExpectingOperandOfDisjunction;

                if (_evalSequence.Count - 1 < startOfLeft)
                {
                    // Everything was skipped
                    TryPushOperand(MakeDefaultPattern(ituplePattern.Syntax, ituplePattern.InputType));
                }

                return null;
            }

            public override BoundNode? VisitListPattern(BoundListPattern listPattern)
            {
                // If we're starting with `[L1, L2, ...]`
                // - if we are not negating, we can expand it to `[L1, _, ...] and [_, L2, ...] and ...`
                //   and the `and` and `or` patterns in the element patterns can then be lifted out further.
                // 
                // - if we are negating, we can expand it to `null or not [_, _, ...] or [not L1, _, ...] or [_, not L2, ...] or ...`
                //   and the `and` and `or` patterns in the resulting element patterns can then be lifted out further.

                var saveExpectingOperandOfDisjunction = _expectingOperandOfDisjunction;
                int startOfLeft = _evalSequence.Count; // all the operands we push from here will be combined in `or` for negation and `and` otherwise

                ImmutableArray<BoundPattern> equivalentDefaultPatterns = listPattern.Subpatterns.SelectAsArray(makeEquivalentDefaultPattern);

                if (_negated)
                {
                    _expectingOperandOfDisjunction = true;

                    if (listPattern.InputType.CanContainNull())
                    {
                        // `null`
                        TryPushOperand(new BoundConstantPattern(listPattern.Syntax,
                            new BoundLiteral(listPattern.Syntax, constantValueOpt: null, type: null, hasErrors: false),
                            ConstantValue.Null, listPattern.InputType, listPattern.InputType).MakeCompilerGenerated());
                    }

                    // `not [_, _, ..., .._]` (effectively a Length test)
                    BoundListPattern lengthTest = listPattern.WithSubpatterns(equivalentDefaultPatterns);
                    if (!ListPatternHasOnlyEmptySlice(lengthTest))
                    {
                        int endOfLeft = _evalSequence.Count - 1;

                        TryPushOperandAndCombine(listPattern.Syntax,
                            new BoundNegatedPattern(listPattern.Syntax,
                                lengthTest, listPattern.InputType, listPattern.InputType, listPattern.HasErrors).MakeCompilerGenerated(),
                            startOfLeft);
                    }
                }
                else
                {
                    _expectingOperandOfDisjunction = false;

                    if (equivalentDefaultPatterns.IsEmpty)
                    {
                        TryPushOperand(listPattern);
                    }
                }

                var saveMakeEvaluationSequenceOperand = _makeEvaluationSequenceOperand;

                int i = 0;

                Func<BoundPattern, BoundPattern> makeListPattern = (BoundPattern newPattern) =>
                {
                    newPattern = WithInputTypeCheckIfNeeded(newPattern, equivalentDefaultPatterns[i].InputType);
                    ImmutableArray<BoundPattern> newSubpatterns = equivalentDefaultPatterns.SetItem(i, newPattern);

                    BoundPattern newList = new BoundListPattern(
                        newPattern.Syntax, newSubpatterns, hasSlice: newSubpatterns.Any(p => p is BoundSlicePattern), listPattern.LengthAccess, listPattern.IndexerAccess,
                        listPattern.ReceiverPlaceholder, listPattern.ArgumentPlaceholder, listPattern.Variable, listPattern.VariableAccess,
                        listPattern.InputType, listPattern.NarrowedType);

                    if (newPattern.WasCompilerGenerated)
                    {
                        newList = newList.MakeCompilerGenerated();
                    }

                    return saveMakeEvaluationSequenceOperand?.Invoke(newList) ?? newList;
                };

                Func<BoundPattern, BoundPattern> makeListPatternWithSlice = (BoundPattern newPattern) =>
                {
                    var slice = (BoundSlicePattern)listPattern.Subpatterns[i];
                    Debug.Assert(slice.Pattern is not null);

                    newPattern = WithInputTypeCheckIfNeeded(newPattern, slice.Pattern.InputType);
                    newPattern = new BoundSlicePattern(newPattern.Syntax, newPattern, slice.IndexerAccess,
                        slice.ReceiverPlaceholder, slice.ArgumentPlaceholder, slice.InputType, slice.NarrowedType);

                    ImmutableArray<BoundPattern> newSubpatterns = equivalentDefaultPatterns.SetItem(i, newPattern);

                    BoundPattern newList = new BoundListPattern(
                        newPattern.Syntax, newSubpatterns, hasSlice: true, listPattern.LengthAccess, listPattern.IndexerAccess,
                        listPattern.ReceiverPlaceholder, listPattern.ArgumentPlaceholder, listPattern.Variable, listPattern.VariableAccess,
                        listPattern.InputType, listPattern.NarrowedType);

                    if (newPattern.WasCompilerGenerated)
                    {
                        newList = newList.MakeCompilerGenerated();
                    }

                    return saveMakeEvaluationSequenceOperand?.Invoke(newList) ?? newList;
                };

                for (; i < equivalentDefaultPatterns.Length; i++)
                {
                    if (listPattern.Subpatterns[i] is BoundSlicePattern slicePattern)
                    {
                        if (slicePattern.Pattern is null)
                        {
                            continue;
                        }

                        _makeEvaluationSequenceOperand = makeListPatternWithSlice;
                        VisitPatternAndCombine(listPattern.Syntax, slicePattern.Pattern, startOfLeft);
                    }
                    else
                    {
                        _makeEvaluationSequenceOperand = makeListPattern;
                        VisitPatternAndCombine(listPattern.Syntax, listPattern.Subpatterns[i], startOfLeft);
                    }
                }

                _makeEvaluationSequenceOperand = saveMakeEvaluationSequenceOperand;
                _expectingOperandOfDisjunction = saveExpectingOperandOfDisjunction;

                if (_evalSequence.Count - 1 < startOfLeft)
                {
                    // Everything was skipped
                    TryPushOperand(MakeDefaultPattern(listPattern.Syntax, listPattern.InputType));
                }

                return null;

                static BoundPattern makeEquivalentDefaultPattern(BoundPattern pattern)
                {
                    if (pattern is BoundSlicePattern slice)
                    {
                        return slice.WithPattern(null);
                    }

                    return MakeDiscardPattern(pattern.Syntax, pattern.InputType);
                }
            }

            public override BoundNode VisitSlicePattern(BoundSlicePattern node)
            {
                throw ExceptionUtilities.Unreachable();
            }

            private BoundDiscardPattern MakeDiscardPattern(BoundPattern node)
            {
                return MakeDiscardPattern(node.Syntax, node.InputType);
            }

            private static BoundDiscardPattern MakeDiscardPattern(SyntaxNode syntax, TypeSymbol inputType)
            {
                return new BoundDiscardPattern(syntax, inputType, inputType);
            }
        }
    }
}
