∩╗┐// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define ROSLYN_TEST_REDUNDANT_PATTERN

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;

#if ROSLYN_TEST_REDUNDANT_PATTERN
using System.Text;
#endif
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class DecisionDagBuilder
    {
        /// <summary>
        /// # High-level algorithm:
        /// 1. We normalize the pattern.
        /// 2. We analyze the normalized pattern to construct sets of "cases".
        /// 3. For each set of cases, we run reachability analysis and collect the syntax nodes to report.
        /// 4. We repeat the process with the negated pattern to collect more syntax nodes to report.
        /// 5. Once we've collected all the syntax nodes to report, we report the diagnostics.
        ///   We only report diagnostics if we detect certain syntactic structure (`not` before a redundant pattern in a binary pattern).
        ///
        /// # Normalization:
        /// In short:
        /// - composite patterns are expanded: `Type { Prop1: A, Prop2: B }` becomes `Type and { Prop1: A } and { Prop2: B }`
        /// - negated patterns are pushed down: `not (Type and { Prop: A } }` becomes `not Type or { Prop: not A }`
        ///
        /// See <see cref="PatternNormalizer"/> for details.
        ///
        /// # Identifying sets of cases to run reachability analysis on:
        /// For patterns that contain a disjunction `... or ...` we're going to perform reachability analysis for each branch of the `or`.
        /// We effectively pick each analyzable `or` sequence in turn and expand it to top-level cases.
        ///
        /// For example `A and (B or C)` we'll check the reachability of two cases: `case A and B` and `case A and C`.
        ///
        /// Similarly, for `(A or B) and (C or D)` we'll check the reachability of two sets of two cases:
        ///   1. { `case A`, `case B` } (we can truncate later test since we only care about the reachability of `A` and `B` here)
        ///   2. { `case (A or B) and C`, `case (A or B) and D` }
        ///
        /// Similarly, for `A or ((B or C) and D)` we'll check the reachability of two sets of cases:
        ///   1. { `case A:`, `case ((B or C) and D):` }
        ///   2. { `case A:`, `case B:`, `case C:` }
        ///
        /// Similarly, for `A or (B and (C or D))` we'll check the reachability of two sets of cases:
        ///   1. { `case A:`, `case (B and (C or D)):`
        ///   2. { `case A:`, `case B and C:`, `case B and D:` }
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

            LabelSymbol defaultLabel = new GeneratedLabelSymbol("defaultLabel");
            var builder = new DecisionDagBuilder(compilation, defaultLabel: defaultLabel, forLowering: false, BindingDiagnosticBag.Discarded);
            BoundDagTemp rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);
            var redundantNodes = PooledHashSet<SyntaxNode>.GetInstance();

            var noPreviousCases = ArrayBuilder<StateForCase>.GetInstance(0);
            CheckOrAndAndReachability(noPreviousCases, patternIndex: 0, pattern: pattern, builder: builder, rootIdentifier: rootIdentifier, syntax: syntax, diagnostics: diagnostics, redundantNodes);
            ReportRedundant(redundantNodes, diagnostics);

            redundantNodes.Free();
            Debug.Assert(noPreviousCases.Count == 0);
            noPreviousCases.Free();
        }

        internal static bool EnableRedundantPatternsCheck(CSharpCompilation compilation)
        {
            return compilation.LanguageVersion >= LanguageVersion.CSharp14;
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
            var redundantNodes = PooledHashSet<SyntaxNode>.GetInstance();
            var existingCases = ArrayBuilder<StateForCase>.GetInstance(switchArms.Length);

            checkRedundantPatternsForSwitchExpression(compilation, syntax, inputExpression, switchArms, diagnostics, redundantNodes, existingCases);

            existingCases.Free();
            redundantNodes.Free();

            static void checkRedundantPatternsForSwitchExpression(
                CSharpCompilation compilation,
                SyntaxNode syntax,
                BoundExpression inputExpression,
                ImmutableArray<BoundSwitchExpressionArm> switchArms,
                BindingDiagnosticBag diagnostics,
                PooledHashSet<SyntaxNode> redundantNodes,
                ArrayBuilder<StateForCase> existingCases)
            {
                LabelSymbol defaultLabel = new GeneratedLabelSymbol("defaultLabel");
                var builder = new DecisionDagBuilder(compilation, defaultLabel: defaultLabel, forLowering: false, BindingDiagnosticBag.Discarded);
                BoundDagTemp rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);
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
                    CheckOrAndAndReachability(existingCases, patternIndex, switchArms[patternIndex].Pattern, builder, rootIdentifier, syntax, diagnostics, redundantNodes);
                }

                ReportRedundant(redundantNodes, diagnostics);
            }
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
            var redundantNodes = PooledHashSet<SyntaxNode>.GetInstance();
            var existingCases = ArrayBuilder<StateForCase>.GetInstance();

            checkRedundantPatternsForSwitchStatement(compilation, syntax, inputExpression, switchSections, diagnostics, redundantNodes, existingCases);

            existingCases.Free();
            redundantNodes.Free();

            static void checkRedundantPatternsForSwitchStatement(
                CSharpCompilation compilation,
                SyntaxNode syntax,
                BoundExpression inputExpression,
                ImmutableArray<BoundSwitchSection> switchSections,
                BindingDiagnosticBag diagnostics,
                PooledHashSet<SyntaxNode> redundantNodes,
                ArrayBuilder<StateForCase> existingCases)
            {
                LabelSymbol defaultLabel = new GeneratedLabelSymbol("defaultLabel");
                var builder = new DecisionDagBuilder(compilation, defaultLabel: defaultLabel, forLowering: false, BindingDiagnosticBag.Discarded);
                BoundDagTemp rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);
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
                            CheckOrAndAndReachability(existingCases, patternIndex, label.Pattern, builder, rootIdentifier, syntax, diagnostics, redundantNodes);
                            patternIndex++;
                        }
                    }
                }

                ReportRedundant(redundantNodes, diagnostics);
            }
        }

        private static void ReportRedundant(PooledHashSet<SyntaxNode> redundantNodes, BindingDiagnosticBag diagnostics)
        {
            foreach (var node in redundantNodes)
            {
                ErrorCode errorCode = shouldWarn(node) ? ErrorCode.WRN_RedundantPattern : ErrorCode.HDN_RedundantPattern;
                diagnostics.Add(errorCode, node);
            }

            return;

            // We need to reduce the break introduced by reporting redundant patterns
            // and we never want to affect people who express their patterns thoroughly (but correctly)
            // such as `switch { < 0 => -1, 0 => 0, > 0 => 1 }`.
            // So we're only reporting a warning for situations that syntactically look hazardous.
            // Others are reported as a hidden diagnostic.
            // At the moment, we're only interested in patterns in a binary pattern with a `not` before the redundant pattern.
            static bool shouldWarn(SyntaxNode syntax)
            {
start:
                if (syntax.Parent is ParenthesizedPatternSyntax parens)
                {
                    syntax = parens;
                    goto start;
                }

                if (syntax.Parent is BinaryPatternSyntax binary)
                {
                    if (binary.Right == syntax && findNotInBinary(binary.Left))
                    {
                        return true;
                    }

                    syntax = binary;
                    goto start;
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

                // If the syntax is the whole list element pattern, we walk up to the list pattern.
                // For example: `not A or [<redundant>, ...]`
                if (syntax.Parent is ListPatternSyntax listPattern)
                {
                    syntax = listPattern;
                    goto start;
                }

                if (syntax.Parent is SlicePatternSyntax slicePattern)
                {
                    syntax = slicePattern;
                    goto start;
                }

                return false;
            }

            // Detect a `not` at top-level or inside a tree of binary patterns
            // Note: we don't dig into parenthesized patterns as the meaning of `not` is not problematic then
            static bool findNotInBinary(SyntaxNode syntax)
            {
                while (syntax is BinaryPatternSyntax binarySyntax)
                {
                    if (findNotInBinary(binarySyntax.Right))
                    {
                        return true;
                    }

                    syntax = binarySyntax.Left;
                }

                return syntax.Kind() == SyntaxKind.NotPattern;
            }
        }

        /// <summary>
        /// This type provides all the context needed to perform a reachability analysis
        /// on a binary OR pattern we're visiting inside a normalized pattern,
        /// and collect the redundant nodes that are identified.
        /// </summary>
        private readonly struct ReachabilityAnalysisContext
        {
            public readonly ArrayBuilder<StateForCase> PreviousCases;
            public readonly int PatternIndex;
            public readonly DecisionDagBuilder Builder;
            public readonly BoundDagTemp RootIdentifier;
            public readonly SyntaxNode Syntax;

            /// <summary>Collects the nodes we intend to report.</summary>
            public readonly PooledHashSet<SyntaxNode> RedundantNodes;

            public ReachabilityAnalysisContext(ArrayBuilder<StateForCase> previousCases, int patternIndex,
                DecisionDagBuilder builder, BoundDagTemp rootIdentifier, SyntaxNode syntax, PooledHashSet<SyntaxNode> redundantNodes)
            {
                PreviousCases = previousCases;
                PatternIndex = patternIndex;
                Builder = builder;
                RootIdentifier = rootIdentifier;
                Syntax = syntax;
                RedundantNodes = redundantNodes;
            }

#if ROSLYN_TEST_REDUNDANT_PATTERN
            public readonly StringBuilder Logger = new StringBuilder();

            public void Dump(BoundPattern pattern, bool wasReported)
            {
                Logger.Append(pattern.DumpSource());
                if (!pattern.WasCompilerGenerated)
                {
                    Logger.Append("  => ");
                    Logger.Append(pattern.Syntax.ToString());
                    Logger.Append(",");
                    if (wasReported)
                    {
                        Logger.Append(" [redundant]");
                    }
                }

                Logger.AppendLine();
            }
#endif
        }

        private static void CheckOrAndAndReachability(
            ArrayBuilder<StateForCase> previousCases,
            int patternIndex,
            BoundPattern pattern,
            DecisionDagBuilder builder,
            BoundDagTemp rootIdentifier,
            SyntaxNode syntax,
            BindingDiagnosticBag diagnostics,
            PooledHashSet<SyntaxNode> redundantNodes)
        {
            var context = new ReachabilityAnalysisContext(previousCases, patternIndex, builder, rootIdentifier, syntax, redundantNodes);

            try
            {
                var normalizedPattern = PatternNormalizer.Rewrite(pattern, rootIdentifier.Type);

#if ROSLYN_TEST_REDUNDANT_PATTERN
                context.Logger.AppendLine($"Pattern: {pattern.Syntax.ToString()}");
                context.Logger.AppendLine($"Normalized pattern: {normalizedPattern.DumpSource()}");
#endif

                analyze(normalizedPattern, in context);

                // The set of redundancies in pattern `A` is identical to the set of redundancies in pattern `not A`
                // but our method for detecting redundancies only detects those in `or` cases (which we bring to the top after normalization).
                // By analyzing `not A` too we can report more redundancies.
                // For example: `if (o is not (<any pattern including a redundancy>))`, `if (i is 42 and not 43)` (which is the negation of `not 42 or 43`)
                var negated = new BoundNegatedPattern(pattern.Syntax, negated: pattern, pattern.InputType, narrowedType: pattern.InputType);
                var normalizedNegatedPattern = PatternNormalizer.Rewrite(negated, rootIdentifier.Type);

#if ROSLYN_TEST_REDUNDANT_PATTERN
                context.Logger.AppendLine($"Normalized negated pattern: {normalizedNegatedPattern.DumpSource()}");
#endif

                analyze(normalizedNegatedPattern, in context);

#if ROSLYN_TEST_REDUNDANT_PATTERN
                string log = context.Logger.ToString();
                // For debugging, uncomment the preprocessing directive and set a breakpoint below
                ;
#endif
            }
            catch (InsufficientExecutionStackException)
            {
                diagnostics.Add(ErrorCode.HDN_RedundantPatternStackGuard, pattern.Syntax);
            }

            return;

            static void populateStateForCases(ArrayBuilder<BoundPattern> set, PooledHashSet<LabelSymbol> labelsToIgnore,
                ref TemporaryArray<StateForCase> casesBuilder, ref readonly ReachabilityAnalysisContext context)
            {
                int patternIndex = context.PatternIndex;
                var previousCases = context.PreviousCases;
                for (int i = 0; i < patternIndex; i++)
                {
                    casesBuilder.Add(previousCases[i]);
                }

                int index = patternIndex;
                foreach (BoundPattern pattern in set)
                {
                    var label = new GeneratedLabelSymbol("orCase");
                    SyntaxNode? diagSyntax = pattern.Syntax;
                    if (pattern.WasCompilerGenerated)
                    {
                        labelsToIgnore.Add(label);
                        diagSyntax = context.Syntax;
                    }

                    Debug.Assert(diagSyntax is not null);
                    casesBuilder.Add(context.Builder.MakeTestsForPattern(++index, diagSyntax, context.RootIdentifier, pattern, whenClause: null, label: label));
                }
            }

            // Given a normalized pattern (so there are only `and` and `or` patterns at the root of the tree)
            // we traverse the binary patterns building a set of cases and reporting reachability issues
            // on that set of cases when applicable.
            static void analyze(BoundPattern pattern, ref readonly ReachabilityAnalysisContext context)
            {
                if (pattern is BoundBinaryPattern binaryPattern)
                {
                    var currentCases = ArrayBuilder<BoundPattern>.GetInstance();
                    analyzeBinary(currentCases, binaryPattern, wrapIntoParentAndPattern: null, in context);
                    currentCases.Free();
                    return;
                }

                return;
            }

            static void analyzePattern(ArrayBuilder<BoundPattern> currentCases, BoundPattern pattern, Func<BoundPattern, BoundPattern>? wrapIntoParentAndPattern, ref readonly ReachabilityAnalysisContext context)
            {
                if (pattern is BoundBinaryPattern nestedBinary)
                {
                    analyzeBinary(currentCases, nestedBinary, wrapIntoParentAndPattern, in context);
                }
            }

            static void analyzeBinary(ArrayBuilder<BoundPattern> currentCases, BoundBinaryPattern binaryPattern, Func<BoundPattern, BoundPattern>? wrapIntoParentAndPattern, ref readonly ReachabilityAnalysisContext context)
            {
                if (binaryPattern.Disjunction)
                {
                    int savedStackCount = currentCases.Count;

                    var patterns = ArrayBuilder<BoundPattern>.GetInstance();
                    addPatternsFromOrTree(binaryPattern, patterns);

                    // In `A1 or ... or Ai or B or ...`, we analyze `B` with `case A1:`, ..., `case Ai:` (with each wrapped as indicated by caller) added to current cases.
                    // That way, if `B` can be seen as multiple cases: `case B1`, ..., `case Bn`
                    // we'll be able to check reachability on: `case A1:`, ... `case Ai:`, `case B1:`, ... `case Bn:`
                    for (int i = 0; i < patterns.Count; i++)
                    {
                        BoundPattern pattern = patterns[i];
                        analyzePattern(currentCases, pattern, wrapIntoParentAndPattern, in context);
                        BoundPattern wrappedPattern = wrapIntoParentAndPattern?.Invoke(pattern) ?? pattern;
                        currentCases.Add(wrappedPattern);
                    }

                    // For `A1 or ... or An`, we check reachability on: `case A1`, ..., `case An` (with each wrapped as indicated by caller)
                    checkReachability(currentCases, in context);

                    currentCases.Count = savedStackCount;
                    patterns.Free();
                }
                else
                {
                    var stack = ArrayBuilder<BoundBinaryPattern>.GetInstance();
                    BoundBinaryPattern? current = binaryPattern;
                    do
                    {
                        stack.Push(current);
                        current = current.Left as BoundBinaryPattern;
                    }
                    while (current != null && !current.Disjunction);

                    current = stack.Pop();
                    // In `A and B and ...`, we analyze `A` without the `and B and ...`
                    analyzePattern(currentCases, current.Left, wrapIntoParentAndPattern, in context);

                    // Given `newPattern`, produce `A and newPattern`
                    Func<BoundPattern, BoundPattern> newWrapIntoParentAndPattern = (BoundPattern newPattern) =>
                    {
                        // Note: lambda intentionally captures
                        bool wasCompilerGenerated = newPattern.WasCompilerGenerated;
                        var wrappedPattern = new BoundBinaryPattern(newPattern.Syntax, disjunction: false, current.Left, newPattern, current.InputType, newPattern.NarrowedType);
                        var result = wrapIntoParentAndPattern?.Invoke(wrappedPattern) ?? wrappedPattern;

                        if (wasCompilerGenerated)
                        {
                            result = result.MakeCompilerGenerated();
                        }

                        return result;
                    };

                    do
                    {
                        // In `A and B`, we analyze `B` but any cases `case B1:`, ..., `case Bn:` found there
                        // will be wrapped as `A and <expansion>`.
                        // So we'll check reachability on `case A and B1:`, ..., `case A and Bn:`

                        analyzePattern(currentCases, current.Right, newWrapIntoParentAndPattern, in context);
                    }
                    while (stack.TryPop(out current));

                    stack.Free();
                }
            }

            static void checkReachability(ArrayBuilder<BoundPattern> orCases, ref readonly ReachabilityAnalysisContext context)
            {
                // We construct a set of cases using the previous cases from context and the current/given cases.
                // We then construct a DAG and analyze reachability of branches.
                // Unreachable cases for patterns marked as compiler-generated will not be reported.
#if ROSLYN_TEST_REDUNDANT_PATTERN
                context.Logger.AppendLine("Set:");
#endif

                using var casesBuilder = TemporaryArray<StateForCase>.GetInstance(orCases.Count);
                var labelsToIgnore = PooledHashSet<LabelSymbol>.GetInstance();
                populateStateForCases(orCases, labelsToIgnore, ref casesBuilder.AsRef(), in context);
                BoundDecisionDag dag = context.Builder.MakeBoundDecisionDag(context.Syntax, ref casesBuilder.AsRef());

                for (int i = 0; i < casesBuilder.Count; i++)
                {
                    StateForCase @case = casesBuilder[i];
                    bool shouldReport = !dag.ReachableLabels.Contains(@case.CaseLabel) && !labelsToIgnore.Contains(@case.CaseLabel);
                    if (shouldReport)
                    {
                        context.RedundantNodes.Add(@case.Syntax);
                    }

#if ROSLYN_TEST_REDUNDANT_PATTERN
                    if (i >= context.PatternIndex)
                    {
                        context.Dump(orCases[i - context.PatternIndex], shouldReport);
                    }
#endif
                }

                labelsToIgnore.Free();
            }

            // If given an `or` pattern, gather all the patterns in this `or` sequence
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

        // The purpose of this rewriter is to push `not` patterns down the pattern tree and
        // pull all the `and` and `or` patterns up the pattern tree.
        // It needs to expand composite patterns in the process.
        // It also erases/simplifies some patterns (variable declarations).
        //
        // For example, given `not { Prop: 42 or 43 }`
        // it produces `null or ({ Prop: not 42 } and { Prop: not 43 })`.
        //
        // When visiting a pattern, the caller indicates:
        // - whether the pattern should be negated,
        // - whether the evaluations yielded by the visit will be combined in `or` or `and`,
        // - how visited patterns should be wrapped before being placed in the eval sequence.
        //
        // A Visit will push single patterns (operands) and binary operations onto the eval sequence.
        // Once we're done visiting, the eval sequence will be converted back into a pattern.
        //
        // A Visit should produce an eval result using the original InputType. When type adjustments are needed,
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
            /// When visiting a composite pattern such as `{ Prop: A or B }`
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
                        disjunction = _disjunction.GetValueOrDefault();
                        Debug.Assert(_operationSyntax is not null);
                        operationSyntax = _operationSyntax;
                        return true;
                    }

                    disjunction = false;
                    operationSyntax = null;
                    return false;
                }

                public OperandOrOperation MakeCompilerGenerated()
                {
                    Debug.Assert(_operand is not null);
                    return new OperandOrOperation(_operand.MakeCompilerGenerated(), _disjunction, _operationSyntax) { OnTheLeftOfDisjunction = this.OnTheLeftOfDisjunction };
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

            /// <summary>
            /// Reconstitutes a normalized pattern from the operands (ie. patterns) and operations (`and` or `or`)
            /// accumulated in the eval sequence.
            /// It takes care of adjusting the input type in `and` sequences.
            /// </summary>
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
                    or BoundNegatedPattern, $"This visitor doesn't support pattern {node} yet.");

                return base.Visit(node);
            }

            /// <summary>
            /// Updates the eval sequence from [...eval sequence...]
            /// to [...eval sequence..., ...eval for left..., ...eval for right..., binaryOperation]
            /// in the general case.
            ///
            /// If the left is a skipped, then update it to
            /// [...eval sequence..., ...eval for right...]
            ///
            /// If the right is a skipped, then update it to
            /// [...eval sequence..., ...eval for left...]
            ///
            /// If both are skipped, then no change occurs.
            /// </summary>
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
                Debug.Assert(!(current.Left is BoundBinaryPattern binary && binary.Disjunction == node.Disjunction));

                int startOfLeft = _evalSequence.Count;
                Visit(current.Left);
                int endOfLeft = _evalSequence.Count - 1;

                do
                {
                    if (endOfLeft < startOfLeft && stack.IsEmpty)
                    {
                        _expectingOperandOfDisjunction = saveExpectingOperandOfDisjunction;
                    }

                    int startOfRight = _evalSequence.Count;
                    Visit(current.Right);
                    int endOfRight = _evalSequence.Count - 1;

                    if (endOfLeft >= startOfLeft && startOfRight <= endOfRight)
                    {
                        PushBinaryOperation(current.Syntax, endOfLeft, disjunction);
                    }

                    endOfLeft = endOfRight;
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
            // one that expects `inputType`.
            //
            // For example, when splitting a pattern `{ Prop: A and B }` into `{ Prop: A } and { Prop: B }`
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

                if (pattern is BoundConstantPattern constantPattern)
                {
                    var narrowedType = constantPattern.ConstantValue.IsNull ? inputType : constantPattern.NarrowedType;
                    return constantPattern.Update(constantPattern.Value, constantPattern.ConstantValue, inputType, narrowedType);
                }

                if (pattern is BoundRelationalPattern relationalPattern)
                {
                    return relationalPattern.Update(relationalPattern.Relation, relationalPattern.Value, relationalPattern.ConstantValue, inputType, relationalPattern.NarrowedType);
                }

                if (pattern is BoundDeclarationPattern declarationPattern)
                {
                    // We drop the variable symbol and access to avoid input type mismtaches, resulting in a designation discard
                    return declarationPattern.Update(declarationPattern.DeclaredType, declarationPattern.IsVar,
                        variable: null, variableAccess: null, inputType, declarationPattern.NarrowedType);
                }

                Debug.Assert(pattern is BoundITuplePattern or BoundListPattern);

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

            public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
            {
                var result = new BoundDeclarationPattern(node.Syntax, node.DeclaredType, node.IsVar, node.Variable, node.VariableAccess, node.InputType, node.NarrowedType)
                    .MakeCompilerGenerated();
                TryPushOperand(NegateIfNeeded(result));
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
                //   If there's no Type, we substitute a null check

                var saveExpectingOperandOfDisjunction = _expectingOperandOfDisjunction;
                int startOfLeft = _evalSequence.Count;

                // All the operands we push from here will be combined in `or` for negation and `and` otherwise
                // Note: all the operands pushed will have the original input type, we'll let GetResult adjust the input type in an `and` sequence
                _expectingOperandOfDisjunction = _negated;

                // We need an initial check to maintain proper semantics. We only blame it if the only purpose of the pattern is that initial check.
                // So if there are some nested patterns that aren't skipped, we won't blame the initial check.
                // If there is a variable declaration, we won't blame the initial check.
                // The initial check will get marked as compiler-generated later (below) as needed.
                //
                // For example: taking `myString is not (not null and { Length: 0 or 1 })`
                // We produce cases:
                //   - `null` (for the explicit null pattern)
                //   - `null` (initial check for recursive pattern, but we don't want to blame this one)
                //   - `{ Length: not 0 } and { Length: not 1 }`
                BoundPattern initialCheck;
                if (node.DeclaredType is not null)
                {
                    // `Type`
                    initialCheck = new BoundTypePattern(node.Syntax, node.DeclaredType, node.IsExplicitNotNullTest, node.InputType, node.NarrowedType, node.HasErrors);
                }
                else if (node.InputType.CanContainNull())
                {
                    // `not null`
                    var nullCheck = new BoundConstantPattern(node.Syntax,
                        new BoundLiteral(node.Syntax, constantValueOpt: ConstantValue.Null, type: node.InputType, hasErrors: false),
                        ConstantValue.Null, node.InputType, node.InputType, hasErrors: false);
                    initialCheck = new BoundNegatedPattern(node.Syntax, nullCheck, node.InputType, narrowedType: node.InputType);
                }
                else
                {
                    // `{ }`
                    initialCheck = new BoundRecursivePattern(node.Syntax, declaredType: null, deconstructMethod: null, deconstruction: default,
                        ImmutableArray<BoundPropertySubpattern>.Empty, isExplicitNotNullTest: false, variable: null, variableAccess: null, node.InputType, node.InputType);
                }
                TryPushOperand(NegateIfNeeded(initialCheck));
                Debug.Assert(_evalSequence.Count == startOfLeft + 1);

                int startOfNestedPatterns = _evalSequence.Count;
                ImmutableArray<BoundPositionalSubpattern> deconstruction = node.Deconstruction;
                if (!deconstruction.IsDefaultOrEmpty)
                {
                    var discards = deconstruction.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d.Syntax, d.Pattern.InputType)));
                    var saveMakeEvaluationSequenceOperand = _makeEvaluationSequenceOperand;

                    int i = 0;

                    // Given `newPattern`, produce `DeclaredType (..., _, newPattern, _, ...)`
                    _makeEvaluationSequenceOperand = (BoundPattern newPattern) =>
                    {
                        // Note: lambda intentionally captures
                        bool wasCompilerGenerated = newPattern.WasCompilerGenerated;
                        newPattern = WithInputTypeCheckIfNeeded(newPattern, deconstruction[i].Pattern.InputType);
                        ImmutableArray<BoundPositionalSubpattern> newSubPatterns = discards.SetItem(i, deconstruction[i].WithPattern(newPattern));

                        BoundPattern newRecursive = new BoundRecursivePattern(
                            newPattern.Syntax, declaredType: node.DeclaredType, deconstructMethod: node.DeconstructMethod,
                            deconstruction: newSubPatterns,
                            properties: default, isExplicitNotNullTest: false, variable: null, variableAccess: null,
                            node.InputType, node.NarrowedType, node.HasErrors);

                        if (wasCompilerGenerated)
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

                if (!node.Properties.IsDefaultOrEmpty)
                {
                    var saveMakeEvaluationSequenceOperand = _makeEvaluationSequenceOperand;
                    BoundPropertySubpattern? property = null;

                    // Given `newPattern`, produce `DeclaredType { Prop: newPattern }`
                    _makeEvaluationSequenceOperand = (BoundPattern newPattern) =>
                    {
                        // Note: lambda intentionally captures
                        bool wasCompilerGenerated = newPattern.WasCompilerGenerated;
                        newPattern = WithInputTypeCheckIfNeeded(newPattern, property!.Pattern.InputType);
                        ImmutableArray<BoundPropertySubpattern> newSubPatterns = [property.WithPattern(newPattern)];

                        BoundPattern newRecursive = new BoundRecursivePattern(
                            newPattern.Syntax, declaredType: node.DeclaredType, deconstructMethod: null, deconstruction: default,
                            properties: newSubPatterns,
                            isExplicitNotNullTest: false, variable: null, variableAccess: null,
                            node.InputType, node.NarrowedType, node.HasErrors);

                        if (wasCompilerGenerated)
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

                if (_evalSequence.Count > startOfNestedPatterns || node.Variable is not null)
                {
                    _evalSequence[startOfLeft] = _evalSequence[startOfLeft].MakeCompilerGenerated();
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

                Debug.Assert(_expectingOperandOfDisjunction == _negated);
                PushBinaryOperation(syntax, endOfLeft, disjunction: _negated);
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
                int startOfLeft = _evalSequence.Count;
                var subpatterns = ituplePattern.Subpatterns;
                var discards = subpatterns.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d.Syntax, d.Pattern.InputType)));

                // all the operands we push from here will be combined in `or` for negation and `and` otherwise
                _expectingOperandOfDisjunction = _negated;

                // `(_, ..., _)` (effectively a not null and Length test)
                var lengthTest = new BoundITuplePattern(ituplePattern.Syntax, ituplePattern.GetLengthMethod, ituplePattern.GetItemMethod, discards,
                    ituplePattern.InputType, ituplePattern.NarrowedType);
                TryPushOperand(NegateIfNeeded(lengthTest));
                Debug.Assert(_evalSequence.Count == startOfLeft + 1);

                int startOfNestedPatterns = _evalSequence.Count;
                var saveMakeEvaluationSequenceOperand = _makeEvaluationSequenceOperand;
                int i = 0;

                // Given `newPattern`, produce `(..., _, newPattern, _, ...)`
                _makeEvaluationSequenceOperand = (BoundPattern newPattern) =>
                {
                    // Note: lambda intentionally captures
                    bool wasCompilerGenerated = newPattern.WasCompilerGenerated;
                    newPattern = WithInputTypeCheckIfNeeded(newPattern, subpatterns[i].Pattern.InputType);
                    ImmutableArray<BoundPositionalSubpattern> newSubpatterns = discards.SetItem(i, subpatterns[i].WithPattern(newPattern));

                    BoundPattern newITuple = new BoundITuplePattern(newPattern.Syntax, ituplePattern.GetLengthMethod,
                        ituplePattern.GetItemMethod, newSubpatterns, ituplePattern.InputType, ituplePattern.NarrowedType);

                    if (wasCompilerGenerated)
                    {
                        newITuple = newITuple.MakeCompilerGenerated();
                    }

                    return saveMakeEvaluationSequenceOperand?.Invoke(newITuple) ?? newITuple;
                };

                for (; i < subpatterns.Length; i++)
                {
                    // `(..., <visited pattern>, ...)`
                    VisitPatternAndCombine(ituplePattern.Syntax, subpatterns[i].Pattern, startOfLeft);
                }

                _makeEvaluationSequenceOperand = saveMakeEvaluationSequenceOperand;
                _expectingOperandOfDisjunction = saveExpectingOperandOfDisjunction;

                if (_evalSequence.Count > startOfNestedPatterns)
                {
                    _evalSequence[startOfLeft] = _evalSequence[startOfLeft].MakeCompilerGenerated();
                }

                return null;
            }

            public override BoundNode? VisitListPattern(BoundListPattern listPattern)
            {
                // If we're starting with `[L1, L2, ...]`
                // - if we are not negating, we can expand it to `[_, _, ...] and [L1, _, ...] and [_, L2, ...] and ...`
                //   and the `and` and `or` patterns in the element patterns can then be lifted out further.
                //
                // - if we are negating, we can expand it to `not [_, _, ...] or [not L1, _, ...] or [_, not L2, ...] or ...`
                //   and the `and` and `or` patterns in the resulting element patterns can then be lifted out further.

                var saveExpectingOperandOfDisjunction = _expectingOperandOfDisjunction;
                int startOfLeft = _evalSequence.Count;

                // All the operands we push from here will be combined in `or` for negation and `and` otherwise
                _expectingOperandOfDisjunction = _negated;

                // `[_, _, ..., .._]` (effectively a not null and Length test)
                ImmutableArray<BoundPattern> equivalentDefaultPatterns = listPattern.Subpatterns.SelectAsArray(makeEquivalentDefaultPattern);
                BoundListPattern lengthTest = listPattern.WithSubpatterns(equivalentDefaultPatterns);
                TryPushOperand(NegateIfNeeded(lengthTest));
                Debug.Assert(_evalSequence.Count == startOfLeft + 1);

                int startOfNestedPatterns = _evalSequence.Count;
                var saveMakeEvaluationSequenceOperand = _makeEvaluationSequenceOperand;

                int i = 0;
                bool hasSlice = listPattern.HasSlice;

                // Given `newPattern`, produce `[..., _, newPattern, _, ...]`
                Func<BoundPattern, BoundPattern> makeListPattern = (BoundPattern newPattern) =>
                {
                    // Note: lambda intentionally captures
                    bool wasCompilerGenerated = newPattern.WasCompilerGenerated;
                    newPattern = WithInputTypeCheckIfNeeded(newPattern, equivalentDefaultPatterns[i].InputType);
                    ImmutableArray<BoundPattern> newSubpatterns = equivalentDefaultPatterns.SetItem(i, newPattern);

                    BoundPattern newList = new BoundListPattern(
                        newPattern.Syntax, newSubpatterns, hasSlice, listPattern.LengthAccess, listPattern.IndexerAccess,
                        listPattern.ReceiverPlaceholder, listPattern.ArgumentPlaceholder, listPattern.Variable, listPattern.VariableAccess,
                        listPattern.InputType, listPattern.NarrowedType);

                    if (wasCompilerGenerated)
                    {
                        newList = newList.MakeCompilerGenerated();
                    }

                    return saveMakeEvaluationSequenceOperand?.Invoke(newList) ?? newList;
                };

                // Given `newPattern`, produce `[..., _, ..newPattern, _, ...]`
                Func<BoundPattern, BoundPattern>? makeListPatternWithSlice = null;
                if (hasSlice)
                {
                    makeListPatternWithSlice = (BoundPattern newPattern) =>
                    {
                        // Note: lambda intentionally captures
                        bool wasCompilerGenerated = newPattern.WasCompilerGenerated;
                        var slice = (BoundSlicePattern)listPattern.Subpatterns[i];
                        Debug.Assert(slice.Pattern is not null);

                        newPattern = WithInputTypeCheckIfNeeded(newPattern, slice.Pattern.InputType);

                        BoundPattern newSlice = new BoundSlicePattern(newPattern.Syntax, newPattern, slice.IndexerAccess,
                            slice.ReceiverPlaceholder, slice.ArgumentPlaceholder, slice.InputType, slice.NarrowedType);

                        ImmutableArray<BoundPattern> newSubpatterns = equivalentDefaultPatterns.SetItem(i, newSlice);

                        BoundPattern newList = new BoundListPattern(
                            newPattern.Syntax, newSubpatterns, hasSlice: true, listPattern.LengthAccess, listPattern.IndexerAccess,
                            listPattern.ReceiverPlaceholder, listPattern.ArgumentPlaceholder, listPattern.Variable, listPattern.VariableAccess,
                            listPattern.InputType, listPattern.NarrowedType);

                        if (wasCompilerGenerated)
                        {
                            newList = newList.MakeCompilerGenerated();
                        }

                        return saveMakeEvaluationSequenceOperand?.Invoke(newList) ?? newList;
                    };
                }

                for (; i < equivalentDefaultPatterns.Length; i++)
                {
                    if (listPattern.Subpatterns[i] is BoundSlicePattern slicePattern)
                    {
                        if (slicePattern.Pattern is null)
                        {
                            continue;
                        }

                        // `[..., ..<visited slice pattern>, ...]`
                        _makeEvaluationSequenceOperand = makeListPatternWithSlice;
                        VisitPatternAndCombine(listPattern.Syntax, slicePattern.Pattern, startOfLeft);
                    }
                    else
                    {
                        // `[..., <visited pattern>, ...]`
                        _makeEvaluationSequenceOperand = makeListPattern;
                        VisitPatternAndCombine(listPattern.Syntax, listPattern.Subpatterns[i], startOfLeft);
                    }
                }

                _makeEvaluationSequenceOperand = saveMakeEvaluationSequenceOperand;
                _expectingOperandOfDisjunction = saveExpectingOperandOfDisjunction;

                if (_evalSequence.Count > startOfNestedPatterns)
                {
                    _evalSequence[startOfLeft] = _evalSequence[startOfLeft].MakeCompilerGenerated();
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

            private static BoundDiscardPattern MakeDiscardPattern(SyntaxNode syntax, TypeSymbol inputType)
            {
                return new BoundDiscardPattern(syntax, inputType, inputType);
            }
        }
    }
}
