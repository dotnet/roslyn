// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class DecisionDagBuilder
    {
        // TODO2 handle switches too
        /// <summary>
        /// For patterns that contain a disjunction `... or ...` we're going to perform reachability analysis for each branch of the `or`.
        /// We effectively pick each analyzable `or` sequence in turn and expand it to top-level cases.
        ///
        /// For example, `A and (B or C)` is expanded to two cases: `A and B` and `A and C`.
        ///
        /// Similarly, `(A or B) and (C or D)` is expanded to two sets of two cases:
        ///   1. `A and (C or D)` and `B and (C or D)`
        ///   2. `(A or B) and C` and `(A or B) and D`
        /// We then check the reachability for each of those cases in different sets.
        ///
        /// Note: we do not analyze `or` sequences that are inside of a `not`, with the exception
        ///   of the simple top-level `not` in an is-pattern. Ideally, we would push every `not` down,
        ///   inverting patterns as necessary, but that is tricky for more complex patterns (recursive or list patterns)
        ///   and our goal is to catch the most common user errors.
        /// </summary>
        internal static void CheckOrReachabilityForIsPattern(
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

            // For the most part, we won't analyze an `or` that is inside a `not`.
            // But for top-level `not` in an is-pattern, we'll allow it and still analyze the pattern.
            pattern = removeTopLevelNots(pattern);

            using RewrittenOrSets sets = rewrite(pattern);
            if (sets.IsDefault)
            {
                return;
            }

            // We construct a DAG and analyze reachability of branches once per `or` sequence
            LabelSymbol whenFalseLabel = new GeneratedLabelSymbol("isPatternFailure");
            var builder = new DecisionDagBuilder(compilation, defaultLabel: whenFalseLabel, forLowering: false, BindingDiagnosticBag.Discarded);
            var rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);
            foreach (ArrayBuilder<StateForCase> cases in casesForExpandedOrSets(sets, builder, rootIdentifier))
            {
                using var casesBuilder = TemporaryArray<StateForCase>.GetInstance(cases.Count);
                casesBuilder.AddRange(cases.ToImmutable()); // TODO2 avoid copy?

                var dag = builder.MakeBoundDecisionDag(syntax, ref casesBuilder.AsRef());
                foreach (StateForCase @case in cases)
                {
                    if (!dag.ReachableLabels.Contains(@case.CaseLabel))
                    {
                        diagnostics.Add(ErrorCode.WRN_RedundantPattern, @case.Syntax);
                    }
                }
            }

            return;

            static BoundPattern removeTopLevelNots(BoundPattern pattern)
            {
                while (pattern is BoundNegatedPattern { Negated: var negated })
                {
                    pattern = negated;
                }

                return pattern;
            }

            // For each expansion set, we produce one `ArrayBuilder<StateForCase>` which we can analysis as a set of cases
            // to determine reachability.
            IEnumerable<ArrayBuilder<StateForCase>> casesForExpandedOrSets(RewrittenOrSets sets, DecisionDagBuilder builder, BoundDagTemp rootIdentifier)
            {
                Debug.Assert(!sets.IsDefault);
                foreach (RewrittenOrSet set in sets.Sets)
                {
                    var cases = ArrayBuilder<StateForCase>.GetInstance();
                    int index = 0;
                    foreach ((BoundPattern pattern, SyntaxNode syntax) in set.Set)
                    {
                        var label = new GeneratedLabelSymbol("caseForSet");
                        cases.Add(builder.MakeTestsForPattern(++index, syntax, rootIdentifier, pattern, whenClause: null, label: label));
                    }

                    yield return cases;
                    cases.Free();
                }
            }

            // TODO2 consider using a rewriter for this
            // The rewrite produces multiple sets, one for each `or` sequence within the pattern.
            // We take each `or` sequence in turn and bring it to the top-level, as long as it is not negated.
            // The caller is responsible for disposing the expansion sets
            static RewrittenOrSets rewrite(BoundPattern? pattern)
            {
                return pattern switch
                {
                    BoundBinaryPattern binary => rewriteBinary(binary),
                    BoundRecursivePattern recursive => rewriteRecursive(recursive),
                    BoundListPattern list => rewriteList(list),
                    BoundSlicePattern slice => rewriteSlice(slice),
                    BoundITuplePattern ituple => rewriteITuple(ituple),
                    BoundNegatedPattern => default, // we give up on analyzing nested patterns inside a `not` for now
                    BoundTypePattern => default,
                    BoundDeclarationPattern => default,
                    BoundConstantPattern => default,
                    BoundDiscardPattern => default,
                    BoundRelationalPattern => default,
                    _ => throw ExceptionUtilities.UnexpectedValue(pattern)
                };
            }

            static RewrittenOrSets rewriteBinary(BoundBinaryPattern binaryPattern)
            {
                RewrittenOrSets result = default;

                if (binaryPattern.Disjunction)
                {
                    var patterns = ArrayBuilder<BoundPattern>.GetInstance();
                    addPatternsFromOrTree(binaryPattern, patterns);

                    // In `A1 or ... or An`, we produce an expansion set: `A1`, ..., `An`
                    RewrittenOrSet resultOrSet1 = result.StartNewOrSet();
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
                        using RewrittenOrSets orSets = rewrite(pattern);
                        if (!orSets.IsDefault)
                        {
                            foreach (RewrittenOrSet orSet in orSets.Sets)
                            {
                                RewrittenOrSet resultOrSet = result.StartNewOrSet();
                                foreach ((BoundPattern resultPattern, SyntaxNode syntax) in orSet.Set)
                                {
                                    BoundBinaryPattern? resultBinaryPattern = updateBinaryOrSequence(binaryPattern, i, resultPattern).updated;
                                    Debug.Assert(resultBinaryPattern is not null);
                                    resultOrSet.Add(resultBinaryPattern, syntax);
                                }
                            }
                        }
                    }
                }
                else
                {
                    using RewrittenOrSets rewrittenLeft = rewrite(binaryPattern.Left);
                    if (!rewrittenLeft.IsDefault)
                    {
                        // In `A and B`, when found multiple `or` patterns in `A` to be expanded
                        // For each such nested expansion, we'll produce an expansion dropping the `and B`
                        foreach (RewrittenOrSet expandedLeftSet in rewrittenLeft.Sets)
                        {
                            RewrittenOrSet resultOrSet = result.StartNewOrSet();
                            foreach ((BoundPattern rewrittenPattern, SyntaxNode syntax) in expandedLeftSet.Set)
                            {
                                resultOrSet.Add(rewrittenPattern, syntax);
                            }
                        }
                    }

                    using RewrittenOrSets rewrittenRight = rewrite(binaryPattern.Right);
                    if (!rewrittenRight.IsDefault)
                    {
                        // In `A and B`, when found multiple `or` patterns in `B` to be expanded
                        // For each such nested expansion, we'll produce a `A and ...` expansion
                        foreach (RewrittenOrSet expandedRightSet in rewrittenRight.Sets)
                        {
                            RewrittenOrSet resultOrSet = result.StartNewOrSet();
                            foreach ((BoundPattern rewrittenPattern, SyntaxNode syntax) in expandedRightSet.Set)
                            {
                                resultOrSet.Add(binaryPattern.WithRight(rewrittenPattern), syntax);
                            }
                        }
                    }
                }

                return result;
            }

            static RewrittenOrSets rewriteRecursive(BoundRecursivePattern recursivePattern)
            {
                RewrittenOrSets result = default;

                // If any of the nested property sub-patterns can be expanded, we carry those on.
                // For example, in `{ Prop: A, ... }`, when we found multiple `or` patterns in `A` to be expanded
                // For each such nested expansion, we'll produce an `{ Prop: <expansion>, ... }` expansion
                ImmutableArray<BoundPropertySubpattern> propertySubpatterns = recursivePattern.Properties;
                if (!propertySubpatterns.IsDefault)
                {
                    for (int i = 0; i < propertySubpatterns.Length; i++)
                    {
                        BoundPattern pattern = propertySubpatterns[i].Pattern;
                        using RewrittenOrSets orSets = rewrite(pattern);
                        if (!orSets.IsDefault)
                        {
                            foreach (RewrittenOrSet orSet in orSets.Sets)
                            {
                                RewrittenOrSet resultOrSet = result.StartNewOrSet();
                                foreach ((BoundPattern resultPattern, SyntaxNode syntax) in orSet.Set)
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
                        using RewrittenOrSets orSets = rewrite(pattern);
                        if (!orSets.IsDefault)
                        {
                            foreach (RewrittenOrSet orSet in orSets.Sets)
                            {
                                RewrittenOrSet resultOrSet = result.StartNewOrSet();
                                foreach ((BoundPattern resultPattern, SyntaxNode syntax) in orSet.Set)
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

            static RewrittenOrSets rewriteList(BoundListPattern listPattern)
            {
                RewrittenOrSets result = default;

                // If any of the nested list sub-patterns can be expanded, we carry those on.
                // For example, in `[ A, ... ]`, when we found multiple `or` patterns in `A` to be expanded,
                // for each such nested expansion, we'll produce an `[ <expansion>, ... ]` expansion
                ImmutableArray<BoundPattern> subpatterns = listPattern.Subpatterns;
                for (int i = 0; i < subpatterns.Length; i++)
                {
                    BoundPattern pattern = subpatterns[i];
                    using RewrittenOrSets orSets = rewrite(pattern);
                    if (!orSets.IsDefault)
                    {
                        foreach (RewrittenOrSet orSet in orSets.Sets)
                        {
                            RewrittenOrSet resultOrSet = result.StartNewOrSet();
                            foreach ((BoundPattern resultPattern, SyntaxNode syntax) in orSet.Set)
                            {
                                BoundListPattern resultListPattern = listPattern.WithSubpatterns(subpatterns.SetItem(i, resultPattern));
                                resultOrSet.Add(resultListPattern, syntax);
                            }
                        }
                    }
                }

                return result;
            }

            static RewrittenOrSets rewriteSlice(BoundSlicePattern slicePattern)
            {
                RewrittenOrSets result = default;
                BoundPattern? pattern = slicePattern.Pattern;
                using RewrittenOrSets orSets = rewrite(pattern);
                if (!orSets.IsDefault)
                {
                    foreach (RewrittenOrSet orSet in orSets.Sets)
                    {
                        RewrittenOrSet resultOrSet = result.StartNewOrSet();
                        foreach ((BoundPattern resultPattern, SyntaxNode syntax) in orSet.Set)
                        {
                            BoundSlicePattern resultSlicePattern = slicePattern.WithPattern(resultPattern);
                            resultOrSet.Add(resultSlicePattern, syntax);
                        }
                    }
                }

                return result;
            }

            static RewrittenOrSets rewriteITuple(BoundITuplePattern ituplePattern)
            {
                RewrittenOrSets result = default;

                ImmutableArray<BoundPositionalSubpattern> positionalSubpatterns = ituplePattern.Subpatterns;
                for (int i = 0; i < positionalSubpatterns.Length; i++)
                {
                    BoundPattern pattern = positionalSubpatterns[i].Pattern;
                    using RewrittenOrSets orSets = rewrite(pattern);
                    if (!orSets.IsDefault)
                    {
                        foreach (RewrittenOrSet orSet in orSets.Sets)
                        {
                            RewrittenOrSet resultOrSet = result.StartNewOrSet();
                            foreach ((BoundPattern resultPattern, SyntaxNode syntax) in orSet.Set)
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

            // Update `pattern1 or pattern2 or ... or patternN` tree, but with the i-th pattern/leaf substituted.
            // We return either an updated node or a count of leaves found in that node.
            static (BoundBinaryPattern? updated, int leafCount) updateBinaryOrSequence(BoundBinaryPattern binaryPattern, int leafIndex, BoundPattern pattern)
            {
                int leftLeafCount;
                if (binaryPattern.Left is BoundBinaryPattern { Disjunction: true } left)
                {
                    (var updatedLeft, leftLeafCount) = updateBinaryOrSequence(left, leafIndex, pattern);
                    if (updatedLeft is not null)
                    {
                        // The index we are looking for is on the left
                        return (binaryPattern.WithLeft(updatedLeft), -1);
                    }
                }
                else
                {
                    // Reached a leaf on the left
                    leftLeafCount = 1;
                    if (leafIndex == 0)
                    {
                        // The index we are looking for is on the left
                        return (binaryPattern.WithLeft(pattern), -1);
                    }
                }

                if (binaryPattern.Right is BoundBinaryPattern { Disjunction: true } right)
                {
                    var (updatedRight, rightLeafCount) = updateBinaryOrSequence(right, leafIndex - leftLeafCount, pattern);
                    if (updatedRight is not null)
                    {
                        return (binaryPattern.WithRight(updatedRight), -1);
                    }

                    return (null, leftLeafCount + rightLeafCount);
                }

                if (leafIndex == leftLeafCount)
                {
                    // The index we are looking for is on the right
                    return (binaryPattern.WithRight(pattern), -1);
                }

                return (null, leftLeafCount + 1);
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
        /// 1. `A and (C or D)` and `B and (C or D)`
        /// 2. `(A or B) and C` and `(A or B) and D`
        /// Each set will be analyzed for reachability.
        /// A `default` <see cref="RewrittenOrSets"/> indicates that no `or` patterns were found (so there are no expansions).
        /// </summary>
        private struct RewrittenOrSets : IDisposable
        {
            public ArrayBuilder<RewrittenOrSet>? Sets;

            [MemberNotNullWhen(false, nameof(Sets))]
            public bool IsDefault => Sets is null;

            internal RewrittenOrSet StartNewOrSet()
            {
                return AddOrSet(new RewrittenOrSet());
            }

            internal RewrittenOrSet AddOrSet(RewrittenOrSet orSet)
            {
                Sets ??= ArrayBuilder<RewrittenOrSet>.GetInstance();
                Sets.Add(orSet);
                return orSet;
            }

            public void Dispose()
            {
                if (Sets is not null)
                {
                    foreach (RewrittenOrSet set in Sets)
                    {
                        set.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// When we have a single set of `or` patterns, such as `A or B or C`
        /// we can expand it to separate cases: `A`, `B` and `C`.
        /// This composes. So a nested `or` pattern can also be expanded: `X and (A or B or C)`
        /// can be expanded to `X and A`, `X and B` and `X and C`.
        /// </summary>
        private struct RewrittenOrSet : IDisposable
        {
            public ArrayBuilder<(BoundPattern pattern, SyntaxNode syntax)> Set;

            public RewrittenOrSet()
            {
                Set = ArrayBuilder<(BoundPattern pattern, SyntaxNode syntax)>.GetInstance();
            }

            public void Add(BoundPattern pattern, SyntaxNode syntax)
            {
                Set.Add((pattern, syntax));
            }

            public void Dispose()
            {
                Set.Free();
            }
        }
    }
}
