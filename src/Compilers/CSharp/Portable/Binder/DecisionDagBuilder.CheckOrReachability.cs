// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class DecisionDagBuilder
    {
        /// <summary>
        /// For patterns that contain a disjunction `... or ...` we're going to perform reachability analysis for each branch of the `or`.
        /// We effectively pick each analyzable `or` sequence in turn and expand it to top-level cases.
        ///
        /// For example, `A and (B or C)` is expanded to two cases: `A and B` and `A and C`.
        ///
        /// Similarly, `(A or B) and (C or D)` is expanded to two sets of two cases:
        ///   1. `A` and `B` (we can truncate later test since we only care about the reachability of `A` and `B` here)
        ///   2. `(A or B) and C` and `(A or B) and D`
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
            var rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);

            var noPreviousCases = ArrayBuilder<StateForCase>.GetInstance(0);
            CheckOrAndAndReachability(noPreviousCases, patternIndex: 0, pattern, builder, rootIdentifier, defaultLabel, syntax, diagnostics);
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
            var rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);

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
                CheckOrAndAndReachability(existingCases, patternIndex, switchArms[patternIndex].Pattern, builder, rootIdentifier, defaultLabel, syntax, diagnostics, isSwitchExpression: true);
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
            var rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);

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
                        CheckOrAndAndReachability(existingCases, patternIndex, label.Pattern, builder, rootIdentifier, defaultLabel, syntax, diagnostics);
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
          LabelSymbol defaultLabel,
          SyntaxNode syntax,
          BindingDiagnosticBag diagnostics,
          bool isSwitchExpression = false)
        {
            CheckOrReachability(previousCases, patternIndex, pattern,
                builder, rootIdentifier, defaultLabel, syntax, diagnostics, ignoreDefaultLabel: isSwitchExpression);

            var negated = new BoundNegatedPattern(pattern.Syntax, negated: pattern, pattern.InputType, narrowedType: pattern.InputType);
            CheckOrReachability(previousCases, patternIndex, negated,
                builder, rootIdentifier, defaultLabel, syntax, diagnostics, ignoreDefaultLabel: isSwitchExpression);
        }

        private static void CheckOrReachability(
            ArrayBuilder<StateForCase> previousCases,
            int patternIndex,
            BoundPattern pattern,
            DecisionDagBuilder builder,
            BoundDagTemp rootIdentifier,
            LabelSymbol defaultLabel,
            SyntaxNode syntax,
            BindingDiagnosticBag diagnostics,
            bool ignoreDefaultLabel = false)
        {
            var normalizedPattern = PatternNormalizer.Rewrite(pattern, rootIdentifier.Type, builder._conversions);

            SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(normalizedPattern, builder._conversions);
            if (setOfOrCases.IsDefault)
            {
                return;
            }

            // We construct a DAG and analyze reachability of branches once per `or` sequence
            Debug.Assert(!setOfOrCases.IsDefault);
            foreach (OrCases orCases in setOfOrCases.Set)
            {
                using var casesBuilder = TemporaryArray<StateForCase>.GetInstance(orCases.Cases.Count);
                var labelsToIgnore = PooledHashSet<LabelSymbol>.GetInstance();
                PopulateStateForCases(builder, rootIdentifier, previousCases, patternIndex, orCases, labelsToIgnore, syntax, ref casesBuilder.AsRef());
                BoundDecisionDag dag = builder.MakeBoundDecisionDag(syntax, ref casesBuilder.AsRef());

                foreach (StateForCase @case in casesBuilder)
                {
                    if (!dag.ReachableLabels.Contains(@case.CaseLabel) && !labelsToIgnore.Contains(@case.CaseLabel))
                    {
                        diagnostics.Add(ErrorCode.WRN_RedundantPattern, @case.Syntax);
                    }
                }

                if (!ignoreDefaultLabel && !dag.ReachableLabels.Contains(defaultLabel))
                {
                    diagnostics.Add(ErrorCode.WRN_RedundantPattern, syntax);
                }

                labelsToIgnore.Free();
            }
        }

        static void PopulateStateForCases(DecisionDagBuilder builder, BoundDagTemp rootIdentifier, ArrayBuilder<StateForCase> previousCases, int patternIndex, OrCases set, PooledHashSet<LabelSymbol> labelsToIgnore,
            SyntaxNode nodeSyntax, ref TemporaryArray<StateForCase> casesBuilder)
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

        /// <summary>
        /// The purpose of this method is to bring `or` sequences to the top-level (so they can be used as separate cases).
        /// Each set handles one `or`.
        /// </summary>
        private static SetsOfOrCases RewriteToSetsOfOrCases(BoundPattern? pattern, Conversions conversions)
        {
            return pattern switch
            {
                BoundBinaryPattern binary => rewriteBinary(binary, conversions),
                BoundRecursivePattern recursive => rewriteRecursive(recursive, conversions),
                BoundListPattern list => rewriteList(list, conversions),
                BoundSlicePattern slice => rewriteSlice(slice, conversions),
                BoundITuplePattern ituple => rewriteITuple(ituple, conversions),
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

                    // In `A1 or ... or An`, we produce an expansion set: `A1`, ..., `An`
                    OrCases resultOrSet1 = result.StartNewOrCases();
                    var inputType = binaryPattern.InputType;
                    foreach (var pattern in patterns)
                    {
                        resultOrSet1.Add(pattern, pattern.Syntax);
                    }

                    // If any of the nested patterns can be expanded, we carry those on.
                    // For example, in `... or Ai or ...`, when we found multiple `or` patterns in `Ai` to be expanded
                    // For each such nested expansion, we'll produce an `... or <expansion> or ...` expansion
                    for (int i = 0; i < patterns.Count; i++)
                    {
                        BoundPattern pattern = patterns[i];
                        using SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(pattern, conversions);
                        if (!setOfOrCases.IsDefault)
                        {
                            foreach (OrCases orSet in setOfOrCases.Set)
                            {
                                OrCases resultOrSet = result.StartNewOrCases();
                                foreach ((BoundPattern resultPattern, SyntaxNode? syntax) in orSet.Cases)
                                {
                                    BoundPattern resultBinaryPattern = makeDisjunctionWithReplacement(patterns, resultPattern, i, conversions);
                                    Debug.Assert(resultBinaryPattern is not null);
                                    resultOrSet.Add(resultBinaryPattern, syntax);
                                }
                            }
                        }
                    }

                    patterns.Free();
                }
                else
                {
                    using SetsOfOrCases leftSetOfOrCases = RewriteToSetsOfOrCases(binaryPattern.Left, conversions);
                    if (!leftSetOfOrCases.IsDefault)
                    {
                        // In `A and B`, when found multiple `or` patterns in `A` to be expanded
                        // For each such nested expansion, we'll produce an expansion dropping the `and B`
                        foreach (OrCases expandedLeftSet in leftSetOfOrCases.Set)
                        {
                            OrCases resultOrSet = result.StartNewOrCases();
                            foreach ((BoundPattern rewrittenPattern, SyntaxNode? syntax) in expandedLeftSet.Cases)
                            {
                                resultOrSet.Add(rewrittenPattern, syntax);
                            }
                        }
                    }

                    using SetsOfOrCases rightSetOfOrCases = RewriteToSetsOfOrCases(binaryPattern.Right, conversions);
                    if (!rightSetOfOrCases.IsDefault)
                    {
                        // In `A and B`, when found multiple `or` patterns in `B` to be expanded
                        // For each such nested expansion, we'll produce a `A and ...` expansion
                        foreach (OrCases expandedRightSet in rightSetOfOrCases.Set)
                        {
                            OrCases resultOrSet = result.StartNewOrCases();
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

            static BoundPattern makeDisjunctionWithReplacement(ArrayBuilder<BoundPattern> builder, BoundPattern replacementNode, int index, Conversions conversions)
            {
                Debug.Assert(builder.Count != 0);

                BoundPattern result = (index == builder.Count - 1) ? replacementNode : builder.Last();
                var candidateTypes = ArrayBuilder<TypeSymbol>.GetInstance(builder.Count);
                for (int i = builder.Count - 2; i >= 0; i--)
                {
                    candidateTypes.Clear();

                    BoundPattern current = (i == index) ? replacementNode : builder[i];
                    candidateTypes.Add(current.NarrowedType);
                    candidateTypes.Add(result.NarrowedType);

                    var narrowedType = leastSpecificType(candidateTypes, conversions) ?? replacementNode.InputType;
                    result = new BoundBinaryPattern(replacementNode.Syntax, disjunction: true, current, result, replacementNode.InputType, narrowedType, replacementNode.HasErrors);
                }

                candidateTypes.Free();
                return result;
            }

            static TypeSymbol? leastSpecificType(ArrayBuilder<TypeSymbol> candidates, Conversions conversions)
            {
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                return Binder.LeastSpecificType(conversions, candidates, ref useSiteInfo);
            }

            static SetsOfOrCases rewriteRecursive(BoundRecursivePattern recursivePattern, Conversions conversions)
            {
                SetsOfOrCases result = default;

                // If any of the nested property sub-patterns can be expanded, we carry those on.
                // For example, in `{ Prop: A, ... }`, when we found multiple `or` patterns in `A` to be expanded
                // For each such nested expansion, we'll produce an `{ Prop: <expansion>, ... }` expansion
                ImmutableArray<BoundPropertySubpattern> propertySubpatterns = recursivePattern.Properties;
                if (!propertySubpatterns.IsDefault)
                {
                    for (int i = 0; i < propertySubpatterns.Length; i++)
                    {
                        BoundPattern pattern = propertySubpatterns[i].Pattern;
                        using SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(pattern, conversions);
                        if (!setOfOrCases.IsDefault)
                        {
                            foreach (OrCases orSet in setOfOrCases.Set)
                            {
                                OrCases resultOrSet = result.StartNewOrCases();
                                foreach ((BoundPattern resultPattern, SyntaxNode? syntax) in orSet.Cases)
                                {
                                    ImmutableArray<BoundPropertySubpattern> properties = recursivePattern.Properties;
                                    BoundRecursivePattern resultRecursivePattern = recursivePattern
                                        .WithProperties(properties.SetItem(i, properties[i].WithPattern(resultPattern)));

                                    resultOrSet.Add(resultRecursivePattern, syntax);
                                }
                            }
                        }
                    }
                }

                // If any of the nested positional patterns can be expanded, we carry those on.
                // For example, in `(A, ... )`, when we found multiple `or` patterns in `A` to be expanded
                // For each such nested expansion, we'll produce an `(<expansion>, ... )` expansion
                ImmutableArray<BoundPositionalSubpattern> positionalSubpatterns = recursivePattern.Deconstruction;
                if (!positionalSubpatterns.IsDefault)
                {
                    for (int i = 0; i < positionalSubpatterns.Length; i++)
                    {
                        BoundPattern pattern = positionalSubpatterns[i].Pattern;
                        using SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(pattern, conversions);
                        if (!setOfOrCases.IsDefault)
                        {
                            foreach (OrCases orSet in setOfOrCases.Set)
                            {
                                OrCases resultOrSet = result.StartNewOrCases();
                                foreach ((BoundPattern resultPattern, SyntaxNode? syntax) in orSet.Cases)
                                {
                                    ImmutableArray<BoundPositionalSubpattern> deconstruction = recursivePattern.Deconstruction;
                                    BoundRecursivePattern resultRecursivePattern = recursivePattern
                                        .WithDeconstruction(deconstruction.SetItem(i, deconstruction[i].WithPattern(resultPattern)));

                                    resultOrSet.Add(resultRecursivePattern, syntax);
                                }
                            }
                        }
                    }
                }

                return result;
            }

            static SetsOfOrCases rewriteList(BoundListPattern listPattern, Conversions conversions)
            {
                SetsOfOrCases result = default;

                // If any of the nested list sub-patterns can be expanded, we carry those on.
                // For example, in `[ A, ... ]`, when we found multiple `or` patterns in `A` to be expanded,
                // for each such nested expansion, we'll produce an `[ <expansion>, ... ]` expansion
                ImmutableArray<BoundPattern> subpatterns = listPattern.Subpatterns;
                for (int i = 0; i < subpatterns.Length; i++)
                {
                    BoundPattern pattern = subpatterns[i];
                    using SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(pattern, conversions);
                    if (!setOfOrCases.IsDefault)
                    {
                        foreach (OrCases orSet in setOfOrCases.Set)
                        {
                            OrCases resultOrSet = result.StartNewOrCases();
                            foreach ((BoundPattern resultPattern, SyntaxNode? syntax) in orSet.Cases)
                            {
                                BoundListPattern resultListPattern = listPattern.WithSubpatterns(subpatterns.SetItem(i, resultPattern));
                                resultOrSet.Add(resultListPattern, syntax);
                            }
                        }
                    }
                }

                return result;
            }

            static SetsOfOrCases rewriteSlice(BoundSlicePattern slicePattern, Conversions conversions)
            {
                SetsOfOrCases result = default;
                BoundPattern? pattern = slicePattern.Pattern;
                using SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(pattern, conversions);
                if (!setOfOrCases.IsDefault)
                {
                    foreach (OrCases orSet in setOfOrCases.Set)
                    {
                        OrCases resultOrSet = result.StartNewOrCases();
                        foreach ((BoundPattern resultPattern, SyntaxNode? syntax) in orSet.Cases)
                        {
                            BoundSlicePattern resultSlicePattern = slicePattern.WithPattern(resultPattern);
                            resultOrSet.Add(resultSlicePattern, syntax);
                        }
                    }
                }

                return result;
            }

            static SetsOfOrCases rewriteITuple(BoundITuplePattern ituplePattern, Conversions conversions)
            {
                SetsOfOrCases result = default;

                ImmutableArray<BoundPositionalSubpattern> positionalSubpatterns = ituplePattern.Subpatterns;
                for (int i = 0; i < positionalSubpatterns.Length; i++)
                {
                    BoundPattern pattern = positionalSubpatterns[i].Pattern;
                    using SetsOfOrCases setOfOrCases = RewriteToSetsOfOrCases(pattern, conversions);
                    if (!setOfOrCases.IsDefault)
                    {
                        foreach (OrCases orSet in setOfOrCases.Set)
                        {
                            OrCases resultOrSet = result.StartNewOrCases();
                            foreach ((BoundPattern resultPattern, SyntaxNode? syntax) in orSet.Cases)
                            {
                                BoundITuplePattern resultITuplePattern = ituplePattern
                                    .WithSubpatterns(positionalSubpatterns.SetItem(i, positionalSubpatterns[i].WithPattern(resultPattern)));

                                resultOrSet.Add(resultITuplePattern, syntax);
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
                    addPatternsFromOrTree(orPattern.Left, builder);
                    addPatternsFromOrTree(orPattern.Right, builder);
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
        /// 1. `A` and `B`
        /// 2. `(A or B) and C` and `(A or B) and D`
        /// Each set will be analyzed for reachability.
        /// A `default` <see cref="SetsOfOrCases"/> indicates that no `or` patterns were found (so there are no expansions).
        /// </summary>
        private struct SetsOfOrCases : IDisposable
        {
            public ArrayBuilder<OrCases>? Set;

            [MemberNotNullWhen(false, nameof(Set))]
            public bool IsDefault => Set is null;

            internal OrCases StartNewOrCases()
            {
                return AddOrSet(new OrCases());
            }

            internal OrCases AddOrSet(OrCases orCases)
            {
                Set ??= ArrayBuilder<OrCases>.GetInstance();
                Set.Add(orCases);
                return orCases;
            }

            public void Dispose()
            {
                if (Set is not null)
                {
                    foreach (OrCases set in Set)
                    {
                        set.Dispose();
                    }

                    Set.Free();
                }
            }

#if DEBUG
            public override string ToString()
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
        private struct OrCases : IDisposable
        {
            public ArrayBuilder<(BoundPattern pattern, SyntaxNode? syntax)> Cases;

            public OrCases()
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

        // The purpose of this rewriter is to push `not` patterns down the pattern tree.
        // It needs to expand composite patterns in the process.
        // It also erases/simplifies some patterns (variable declarations).
        // Throughout the process it maintains input and narrowed types discipline, to produce a valid and consistent output.
        private class PatternNormalizer : BoundTreeRewriterWithStackGuard
        {
            private bool _negated;
            private TypeSymbol _inputType;
            private readonly Conversions _conversions;

            public PatternNormalizer(Conversions conversions, TypeSymbol inputType)
            {
                _negated = false;
                _inputType = inputType;
                _conversions = conversions;
            }

            internal static BoundPattern Rewrite(BoundPattern pattern, TypeSymbol inputType, Conversions conversions)
            {
                return (BoundPattern)new PatternNormalizer(conversions, inputType).Visit(pattern);
            }

            // A negated discard node represents an always false pattern
            private bool IsNegatedDiscard(BoundPattern pattern, bool synthesized = false)
            {
                if (synthesized)
                {
                    return pattern is BoundNegatedPattern { Negated: BoundDiscardPattern { WasCompilerGenerated: true } };
                }
                else
                {
                    return pattern is BoundNegatedPattern { Negated: BoundDiscardPattern };
                }
            }

            public override BoundNode? VisitBinaryPattern(BoundBinaryPattern node)
            {
                var resultLeft = VisitWithInputType(node.Left, _inputType);
                var resultDisjunction = _negated ? !node.Disjunction : node.Disjunction;

                var rightInputType = resultDisjunction ? _inputType : resultLeft.NarrowedType;
                var resultRight = VisitWithInputType(node.Right, rightInputType);

                TypeSymbol narrowedTypeForBinary2 = narrowedTypeForBinary(resultLeft, resultRight, resultDisjunction);
                var result = new BoundBinaryPattern(node.Syntax, resultDisjunction, resultLeft, resultRight, _inputType, narrowedTypeForBinary2);
                return result;

                TypeSymbol narrowedTypeForBinary(BoundPattern resultLeft, BoundPattern resultRight, bool resultDisjunction)
                {
                    TypeSymbol narrowedType;
                    if (resultDisjunction)
                    {
                        var candidateTypes = ArrayBuilder<TypeSymbol>.GetInstance(2);
                        candidateTypes.Clear();
                        candidateTypes.Add(resultLeft.NarrowedType);
                        candidateTypes.Add(resultRight.NarrowedType);

                        narrowedType = this.LeastSpecificType(candidateTypes) ?? _inputType; // TODO2 the input type fallback is wrong here?
                        candidateTypes.Free();
                    }
                    else
                    {
                        narrowedType = resultRight.NarrowedType;
                    }

                    return narrowedType;
                }
            }

            TypeSymbol? LeastSpecificType(ArrayBuilder<TypeSymbol> candidates)
            {
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                return Binder.LeastSpecificType(_conversions, candidates, ref useSiteInfo);
            }

            public override BoundNode? VisitNegatedPattern(BoundNegatedPattern node)
            {
                var savedNegated = _negated;
                _negated = !_negated;
                var result = this.Visit(node.Negated);
                _negated = savedNegated;
                return result;
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
                return NegateIfNeeded(WithInputTypeCheckIfNeeded(node));
            }

            public override BoundNode? VisitConstantPattern(BoundConstantPattern node)
            {
                return NegateIfNeeded(WithInputTypeCheckIfNeeded(node));
            }

            public override BoundNode? VisitDiscardPattern(BoundDiscardPattern node)
            {
                return NegateIfNeeded(WithInputTypeCheckIfNeeded(node));
            }

            public override BoundNode? VisitRelationalPattern(BoundRelationalPattern node)
            {
                return NegateIfNeeded(WithInputTypeCheckIfNeeded(node));
            }

            private BoundPattern WithInputTypeCheckIfNeeded(BoundPattern pattern)
            {
                // Produce `PatternInputType and pattern` given a new input type

                if (pattern.InputType.Equals(_inputType, TypeCompareKind.AllIgnoreOptions))
                {
                    return pattern;
                }

                BoundPattern typePattern = new BoundTypePattern(pattern.Syntax,
                    new BoundTypeExpression(pattern.Syntax, aliasOpt: null, pattern.InputType),
                    isExplicitNotNullTest: false, _inputType, narrowedType: pattern.InputType);

                var result = new BoundBinaryPattern(pattern.Syntax, disjunction: false, left: typePattern, right: pattern, _inputType, pattern.NarrowedType);

                if (pattern.WasCompilerGenerated)
                {
                    result = result.MakeCompilerGenerated();
                }

                return result;
            }

            public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
            {
                BoundPattern result;
                if (node.IsVar)
                {
                    result = MakeDiscardPattern(node).MakeCompilerGenerated();
                }
                else
                {
                    if (node.InputType.Equals(node.DeclaredType.Type, TypeCompareKind.AllIgnoreOptions))
                    {
                        result = MakeDiscardPattern(node).MakeCompilerGenerated();
                    }
                    else
                    {
                        result = node;
                    }
                }

                return NegateIfNeeded(WithInputTypeCheckIfNeeded(result));
            }

            public override BoundNode? VisitRecursivePattern(BoundRecursivePattern node)
            {
                if (!_negated)
                {
                    ImmutableArray<BoundPositionalSubpattern> deconstructions = VisitListWithInputTypeFromPattern(node.Deconstruction);
                    ImmutableArray<BoundPropertySubpattern> properties = VisitListWithInputTypeFromPattern(node.Properties);
                    var result2 = node.WithDeconstruction(deconstructions).WithProperties(properties);
                    return WithInputTypeCheckIfNeeded(result2);
                }

                // `Type (D1, D2, ...) { Prop1: P1, Prop2: P2, ... } x`
                // can be expanded to `Type and Type (D1, _, ...) and Type (_, D2, ...) and Type { Prop1: P1 } and Type { Prop2: P2 } or ... or var x`
                // which can be negated to
                // `not Type or Type (not D1, _, ...) or Type (_, not D2, ...) or Type { Prop1: not P1 } or Type { Prop2: not P2 } or ...`
                // If there's not Type, we substitute a null check

                var builder = ArrayBuilder<BoundPattern>.GetInstance();

                if (node.DeclaredType is not null)
                {
                    // `not Type`
                    builder.Add(new BoundNegatedPattern(node.Syntax,
                        new BoundTypePattern(node.Syntax, node.DeclaredType, node.IsExplicitNotNullTest, _inputType, node.NarrowedType, node.HasErrors),
                        _inputType, narrowedType: _inputType, node.HasErrors).MakeCompilerGenerated());
                }
                else if (node.InputType.CanContainNull())
                {
                    // `null`
                    builder.Add(new BoundConstantPattern(node.Syntax,
                        new BoundLiteral(node.Syntax, constantValueOpt: null, type: null, hasErrors: false), // dummy expression
                        ConstantValue.Null, _inputType, _inputType, hasErrors: false).MakeCompilerGenerated());
                }

                ImmutableArray<BoundPositionalSubpattern> deconstruction = node.Deconstruction;
                if (!deconstruction.IsDefault)
                {
                    var discards = deconstruction.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d)));
                    for (int i = 0; i < deconstruction.Length; i++)
                    {
                        var negatedPattern = VisitWithInputTypeFromPattern(deconstruction[i].Pattern);

                        if (IsNegatedDiscard(negatedPattern))
                        {
                            continue;
                        }

                        // `Type ( ..., _, not DN, _, ... )`
                        var rewrittenDeconstruction = discards.SetItem(i, deconstruction[i].WithPattern(negatedPattern));

                        var inputType = node.DeclaredType is null ? node.InputType : _inputType;

                        BoundPattern rewrittenRecursive = new BoundRecursivePattern(
                            deconstruction[i].Syntax, declaredType: node.DeclaredType, deconstructMethod: node.DeconstructMethod,
                            deconstruction: rewrittenDeconstruction,
                            properties: default, isExplicitNotNullTest: false, variable: null, variableAccess: null,
                            inputType, node.NarrowedType, node.HasErrors);

                        if (node.DeclaredType is null)
                        {
                            rewrittenRecursive = WithInputTypeCheckIfNeeded(rewrittenRecursive);
                        }

                        builder.Add(rewrittenRecursive);
                    }
                }

                if (!node.Properties.IsDefault)
                {
                    foreach (BoundPropertySubpattern property in node.Properties)
                    {
                        var negatedPattern = VisitWithInputTypeFromPattern(property.Pattern);

                        if (IsNegatedDiscard(negatedPattern))
                        {
                            continue;
                        }

                        // `Type { PropN: not PN }`
                        var inputType = node.DeclaredType is null ? node.InputType : _inputType;

                        BoundPattern rewrittenRecursive = new BoundRecursivePattern(
                            property.Syntax, declaredType: node.DeclaredType, deconstructMethod: null, deconstruction: default,
                            properties: [property.WithPattern(negatedPattern)],
                            isExplicitNotNullTest: false, variable: null, variableAccess: null,
                            inputType, node.NarrowedType, node.HasErrors);

                        if (node.DeclaredType is null)
                        {
                            rewrittenRecursive = WithInputTypeCheckIfNeeded(rewrittenRecursive);
                        }

                        builder.Add(rewrittenRecursive);
                    }
                }

                BoundPattern result = MakeDisjunction(node, builder);
                builder.Free();
                return result;
            }

            public override BoundNode? VisitPropertySubpattern(BoundPropertySubpattern node)
            {
                Debug.Assert(!_negated);
                BoundPattern pattern = VisitWithInputTypeFromPattern(node.Pattern);
                return node.Update(node.Member, node.IsLengthOrCount, pattern);
            }

            public override BoundNode? VisitPositionalSubpattern(BoundPositionalSubpattern node)
            {
                Debug.Assert(!_negated);
                BoundPattern pattern = VisitWithInputTypeFromPattern(node.Pattern);
                return node.Update(node.Symbol, pattern);
            }

            [return: NotNullIfNotNull(nameof(pattern))]
            BoundPattern? VisitWithInputTypeFromPattern(BoundPattern? pattern)
            {
                if (pattern is null)
                {
                    return null;
                }

                return VisitWithInputType(pattern, pattern.InputType);
            }

            BoundPattern VisitWithInputType(BoundPattern pattern, TypeSymbol inputType)
            {
                var savedInputType = _inputType;
                _inputType = inputType;
                var result = (BoundPattern)Visit(pattern);
                _inputType = savedInputType;
                return result;
            }

            private BoundPattern MakeDisjunction(BoundPattern node, ArrayBuilder<BoundPattern> builder)
            {
                if (builder.Count == 0)
                {
                    return MakeDiscardPattern(node);
                }

                BoundPattern result = builder.Last();
                var candidateTypes = ArrayBuilder<TypeSymbol>.GetInstance(2);
                for (int i = builder.Count - 2; i >= 0; i--)
                {
                    candidateTypes.Clear();
                    candidateTypes.Add(builder[i].NarrowedType);
                    candidateTypes.Add(result.NarrowedType);

                    var narrowedType = this.LeastSpecificType(candidateTypes) ?? _inputType; // TODO2 the input type fallback is wrong here
                    result = new BoundBinaryPattern(node.Syntax, disjunction: true, builder[i], result, _inputType, narrowedType, node.HasErrors);
                }

                candidateTypes.Free();
                return result;
            }

            public override BoundNode? VisitITuplePattern(BoundITuplePattern node)
            {
                if (!_negated)
                {
                    ImmutableArray<BoundPositionalSubpattern> subpatterns2 = VisitListWithInputTypeFromPattern(node.Subpatterns);
                    var result2 = node.WithSubpatterns(subpatterns2);
                    return WithInputTypeCheckIfNeeded(result2);
                }

                // `(L1, L2, ...)`
                // can be expanded to `not null and (L1, _, ...) and (_, L2, ...) and ...`
                // which can be negated to
                // `null or (not L1, _, ...) or (_, not L2, ...) or ...`

                var builder = ArrayBuilder<BoundPattern>.GetInstance();

                if (node.InputType.CanContainNull())
                {
                    // `null`
                    builder.Add(new BoundConstantPattern(node.Syntax,
                        new BoundLiteral(node.Syntax, constantValueOpt: null, type: null, hasErrors: false), // dummy expression
                        ConstantValue.Null, node.InputType, node.InputType, hasErrors: false).MakeCompilerGenerated());
                }

                var subpatterns = node.Subpatterns;
                var discards = subpatterns.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d)));

                for (int i = 0; i < subpatterns.Length; i++)
                {
                    var negatedPattern = VisitWithInputTypeFromPattern(subpatterns[i].Pattern);

                    if (IsNegatedDiscard(negatedPattern))
                    {
                        continue;
                    }

                    // `(..., not LN, ...)`
                    var newSubpatterns = discards.SetItem(i, subpatterns[i].WithPattern(negatedPattern));

                    var newPattern = new BoundITuplePattern(subpatterns[i].Syntax, node.GetLengthMethod, node.GetItemMethod, newSubpatterns,
                        _inputType, node.NarrowedType);// TODO2 input type seems wrong

                    builder.Add(newPattern);
                }

                BoundPattern result = MakeDisjunction(node, builder);
                builder.Free();
                return result;
            }

            private ImmutableArray<BoundPattern> VisitListWithInputTypeFromPattern(ImmutableArray<BoundPattern> patterns)
            {
                if (patterns.IsDefault)
                {
                    return patterns;
                }

                return patterns.SelectAsArray(p => VisitWithInputTypeFromPattern(p));
            }

            private ImmutableArray<BoundPositionalSubpattern> VisitListWithInputTypeFromPattern(ImmutableArray<BoundPositionalSubpattern> subpatterns)
            {
                if (subpatterns.IsDefault)
                {
                    return subpatterns;
                }

                return subpatterns.SelectAsArray(p => p.WithPattern(VisitWithInputTypeFromPattern(p.Pattern)));
            }

            private ImmutableArray<BoundPropertySubpattern> VisitListWithInputTypeFromPattern(ImmutableArray<BoundPropertySubpattern> subpatterns)
            {
                if (subpatterns.IsDefault)
                {
                    return subpatterns;
                }

                return subpatterns.SelectAsArray(p => p.WithPattern(VisitWithInputTypeFromPattern(p.Pattern)));
            }

            public override BoundNode? VisitListPattern(BoundListPattern node)
            {
                if (!_negated)
                {
                    ImmutableArray<BoundPattern> subpatterns = VisitListWithInputTypeFromPattern(node.Subpatterns);
                    var result2 = node.WithSubpatterns(subpatterns);
                    return WithInputTypeCheckIfNeeded(result2);
                }

                // `[L1, L2, ...]`
                // can be expanded to `not null and [_, _, ...] and [L1, _, ...] and [_, L2, ...] and ...`
                // which can be negated to
                // `null or not [_, _, ...] or [not L1, _, ...] or [_, not L2, ...] or ...`

                var builder = ArrayBuilder<BoundPattern>.GetInstance();

                if (node.InputType.CanContainNull())
                {
                    // `null`
                    builder.Add(new BoundConstantPattern(node.Syntax,
                        new BoundLiteral(node.Syntax, constantValueOpt: null, type: null, hasErrors: false), // dummy expression
                        ConstantValue.Null, node.InputType, node.InputType, hasErrors: false).MakeCompilerGenerated());
                }

                ImmutableArray<BoundPattern> discardSubpatterns = node.Subpatterns.SelectAsArray(replaceWithDiscards);

                // `not [_, _, ..., .._]`
                BoundListPattern listOfDiscards = node.WithSubpatterns(discardSubpatterns);
                if (!ListPatternHasOnlyEmptySlice(listOfDiscards))
                {
                    builder.Add(new BoundNegatedPattern(node.Syntax,
                        listOfDiscards,
                        node.InputType, node.InputType, node.HasErrors).MakeCompilerGenerated());
                }

                for (int i = 0; i < discardSubpatterns.Length; i++)
                {
                    var negatedPattern = VisitWithInputTypeFromPattern(node.Subpatterns[i]);

                    if (IsNegatedDiscard(negatedPattern))
                    {
                        continue;
                    }

                    if (negatedPattern is BoundSlicePattern { Pattern: { } p } && IsNegatedDiscard(p))
                    {
                        continue;
                    }

                    // `[..., _, not LN, _, ...]`
                    builder.Add(node.WithSubpatterns(discardSubpatterns.SetItem(i, negatedPattern)).WithSyntax(node.Subpatterns[i].Syntax)); // TODO2 we'd like to associate this pattern with the LN syntax for clearer reporting
                }

                BoundPattern result = MakeDisjunction(node, builder);
                builder.Free();
                return result;

                BoundPattern replaceWithDiscards(BoundPattern pattern)
                {
                    if (pattern is BoundSlicePattern slice)
                    {
                        return slice.WithPattern(null);
                    }

                    return MakeDiscardPattern(pattern);
                }
            }

            public override BoundNode? VisitSlicePattern(BoundSlicePattern node)
            {
                if (!_negated)
                {
                    var pattern = VisitWithInputTypeFromPattern(node.Pattern);
                    var result2 = node.WithPattern(pattern);
                    return WithInputTypeCheckIfNeeded(result2);
                }

                if (node.Pattern is null)
                {
                    return node.WithPattern(
                        new BoundNegatedPattern(node.Syntax, MakeDiscardPattern(node), node.InputType, narrowedType: node.InputType));
                }

                var negatedPattern = VisitWithInputTypeFromPattern(node.Pattern);
                return node.WithPattern(negatedPattern);
            }

            private BoundDiscardPattern MakeDiscardPattern(BoundPattern node)
            {
                return new BoundDiscardPattern(node.Syntax, _inputType, _inputType);
            }

            private static BoundDiscardPattern MakeDiscardPattern(BoundPositionalSubpattern node)
            {
                return new BoundDiscardPattern(node.Syntax, node.Pattern.InputType, node.Pattern.InputType);
            }
        }
    }
}
