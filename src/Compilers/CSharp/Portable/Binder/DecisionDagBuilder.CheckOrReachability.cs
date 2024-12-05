// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Text;

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

            checkOrReachability(compilation, syntax, inputExpression, pattern, diagnostics);
            checkOrReachability(compilation, syntax, inputExpression, MoveNotPatternsDownRewriter.MakeNegatedPattern(pattern), diagnostics);

            return;

            static void checkOrReachability(CSharpCompilation compilation, SyntaxNode syntax, BoundExpression inputExpression, BoundPattern pattern, BindingDiagnosticBag diagnostics)
            {
                var normalizedPattern = MoveNotPatternsDownRewriter.Rewrite(pattern);

                SetOfOrCases setOfOrCases = rewrite(normalizedPattern);
                if (setOfOrCases.IsDefault)
                {
                    return;
                }

                // We construct a DAG and analyze reachability of branches once per `or` sequence
                LabelSymbol whenFalseLabel = new GeneratedLabelSymbol("isPatternFailure");
                var builder = new DecisionDagBuilder(compilation, defaultLabel: whenFalseLabel, forLowering: false, BindingDiagnosticBag.Discarded);
                var rootIdentifier = BoundDagTemp.ForOriginalInput(inputExpression);
                Debug.Assert(!setOfOrCases.IsDefault);
                foreach (OrCases orCases in setOfOrCases.Set)
                {
                    using var casesBuilder = TemporaryArray<StateForCase>.GetInstance(orCases.Cases.Count);
                    var labelsToIgnore = PooledHashSet<LabelSymbol>.GetInstance();
                    populateStateForCases(builder, rootIdentifier, orCases, labelsToIgnore, syntax, ref casesBuilder.AsRef());
                    BoundDecisionDag dag = builder.MakeBoundDecisionDag(syntax, ref casesBuilder.AsRef());

                    foreach (StateForCase @case in casesBuilder)
                    {
                        if (!dag.ReachableLabels.Contains(@case.CaseLabel) && !labelsToIgnore.Contains(@case.CaseLabel))
                        {
                            diagnostics.Add(ErrorCode.WRN_RedundantPattern, @case.Syntax);
                        }
                    }
                }
            }

            // The rewrite produces multiple OrCases, one for each `or` sequence within the pattern.
            // We take each `or` sequence in turn and bring it to the top-level, as long as it is not negated.
            // The caller is responsible for disposing the expansion sets
            static SetOfOrCases rewrite(BoundPattern? pattern)
            {
                return pattern switch
                {
                    BoundBinaryPattern binary => rewriteBinary(binary),
                    BoundRecursivePattern recursive => rewriteRecursive(recursive),
                    BoundListPattern list => rewriteList(list),
                    BoundSlicePattern slice => rewriteSlice(slice),
                    BoundITuplePattern ituple => rewriteITuple(ituple),
                    BoundNegatedPattern => default,
                    BoundTypePattern => default,
                    BoundDeclarationPattern => default,
                    BoundConstantPattern => default,
                    BoundDiscardPattern => default,
                    BoundRelationalPattern => default,
                    null => default,
                    _ => throw ExceptionUtilities.UnexpectedValue(pattern)
                };
            }

            static SetOfOrCases rewriteBinary(BoundBinaryPattern binaryPattern)
            {
                SetOfOrCases result = default;

                if (binaryPattern.Disjunction)
                {
                    var patterns = ArrayBuilder<BoundPattern>.GetInstance();
                    addPatternsFromOrTree(binaryPattern, patterns);

                    // In `A1 or ... or An`, we produce an expansion set: `A1`, ..., `An`
                    OrCases resultOrSet1 = result.StartNewOrCases();
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
                        using SetOfOrCases setOfOrCases = rewrite(pattern);
                        if (!setOfOrCases.IsDefault)
                        {
                            foreach (OrCases orSet in setOfOrCases.Set)
                            {
                                OrCases resultOrSet = result.StartNewOrCases();
                                foreach ((BoundPattern resultPattern, SyntaxNode? syntax) in orSet.Cases)
                                {
                                    BoundBinaryPattern? resultBinaryPattern = updateBinaryOrTree(binaryPattern, i, resultPattern).updated;
                                    Debug.Assert(resultBinaryPattern is not null);
                                    resultOrSet.Add(resultBinaryPattern, syntax);
                                }
                            }
                        }
                    }
                }
                else
                {
                    using SetOfOrCases leftSetOfOrCases = rewrite(binaryPattern.Left);
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

                    using SetOfOrCases rightSetOfOrCases = rewrite(binaryPattern.Right);
                    if (!rightSetOfOrCases.IsDefault)
                    {
                        // In `A and B`, when found multiple `or` patterns in `B` to be expanded
                        // For each such nested expansion, we'll produce a `A and ...` expansion
                        foreach (OrCases expandedRightSet in rightSetOfOrCases.Set)
                        {
                            OrCases resultOrSet = result.StartNewOrCases();
                            foreach ((BoundPattern rewrittenPattern, SyntaxNode? syntax) in expandedRightSet.Cases)
                            {
                                resultOrSet.Add(binaryPattern.WithRight(rewrittenPattern), syntax);
                            }
                        }
                    }
                }

                return result;
            }

            static SetOfOrCases rewriteRecursive(BoundRecursivePattern recursivePattern)
            {
                SetOfOrCases result = default;

                // If any of the nested property sub-patterns can be expanded, we carry those on.
                // For example, in `{ Prop: A, ... }`, when we found multiple `or` patterns in `A` to be expanded
                // For each such nested expansion, we'll produce an `{ Prop: <expansion>, ... }` expansion
                ImmutableArray<BoundPropertySubpattern> propertySubpatterns = recursivePattern.Properties;
                if (!propertySubpatterns.IsDefault)
                {
                    for (int i = 0; i < propertySubpatterns.Length; i++)
                    {
                        BoundPattern pattern = propertySubpatterns[i].Pattern;
                        using SetOfOrCases setOfOrCases = rewrite(pattern);
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
                        using SetOfOrCases setOfOrCases = rewrite(pattern);
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

            static SetOfOrCases rewriteList(BoundListPattern listPattern)
            {
                SetOfOrCases result = default;

                // If any of the nested list sub-patterns can be expanded, we carry those on.
                // For example, in `[ A, ... ]`, when we found multiple `or` patterns in `A` to be expanded,
                // for each such nested expansion, we'll produce an `[ <expansion>, ... ]` expansion
                ImmutableArray<BoundPattern> subpatterns = listPattern.Subpatterns;
                for (int i = 0; i < subpatterns.Length; i++)
                {
                    BoundPattern pattern = subpatterns[i];
                    using SetOfOrCases setOfOrCases = rewrite(pattern);
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

            static SetOfOrCases rewriteSlice(BoundSlicePattern slicePattern)
            {
                SetOfOrCases result = default;
                BoundPattern? pattern = slicePattern.Pattern;
                using SetOfOrCases setOfOrCases = rewrite(pattern);
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

            static SetOfOrCases rewriteITuple(BoundITuplePattern ituplePattern)
            {
                SetOfOrCases result = default;

                ImmutableArray<BoundPositionalSubpattern> positionalSubpatterns = ituplePattern.Subpatterns;
                for (int i = 0; i < positionalSubpatterns.Length; i++)
                {
                    BoundPattern pattern = positionalSubpatterns[i].Pattern;
                    using SetOfOrCases setOfOrCases = rewrite(pattern);
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

            // Update `pattern1 or pattern2 or ... or patternN` tree, but with the i-th pattern/leaf substituted.
            // We return either an updated node or a count of leaves found in that node.
            static (BoundBinaryPattern? updated, int leafCount) updateBinaryOrTree(BoundBinaryPattern binaryPattern, int leafIndex, BoundPattern pattern)
            {
                int leftLeafCount;
                if (binaryPattern.Left is BoundBinaryPattern { Disjunction: true } left)
                {
                    (var updatedLeft, leftLeafCount) = updateBinaryOrTree(left, leafIndex, pattern);
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
                    var (updatedRight, rightLeafCount) = updateBinaryOrTree(right, leafIndex - leftLeafCount, pattern);
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

            static void populateStateForCases(DecisionDagBuilder builder, BoundDagTemp rootIdentifier, OrCases set, PooledHashSet<LabelSymbol> labelsToIgnore,
                SyntaxNode nodeSyntax, ref TemporaryArray<StateForCase> casesBuilder)
            {
                int index = 0;
                foreach ((BoundPattern pattern, SyntaxNode? syntax) in set.Cases)
                {
                    var label = new GeneratedLabelSymbol("orCase");
                    SyntaxNode? diagSyntax = syntax;
                    if (diagSyntax is null)
                    {
                        // TODO2 comment
                        labelsToIgnore.Add(label);
                        diagSyntax = nodeSyntax;
                    }

                    Debug.Assert(diagSyntax is not null);
                    casesBuilder.Add(builder.MakeTestsForPattern(++index, diagSyntax, rootIdentifier, pattern, whenClause: null, label: label));
                }
            }
        }

        /// <summary>
        /// When there are multiple `or` patterns, such as `(A or B) and (C or D)`
        /// we'll expand then two sets:
        /// 1. `A` and `B`
        /// 2. `(A or B) and C` and `(A or B) and D`
        /// Each set will be analyzed for reachability.
        /// A `default` <see cref="SetOfOrCases"/> indicates that no `or` patterns were found (so there are no expansions).
        /// </summary>
        private struct SetOfOrCases : IDisposable
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
                        builder.AppendLine(@case.pattern.DumpSource());
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
        // TODO2 some patterns get erased (variable declarations?)
        private class MoveNotPatternsDownRewriter : BoundTreeRewriterWithStackGuard
        {
            private static readonly MoveNotPatternsDownRewriter Instance = new MoveNotPatternsDownRewriter(negated: false);
            private static readonly MoveNotPatternsDownRewriter NegatedInstance = new MoveNotPatternsDownRewriter(negated: true);

            private readonly bool _negated;

            public MoveNotPatternsDownRewriter(bool negated)
            {
                _negated = negated;
            }

            internal static BoundPattern Rewrite(BoundPattern pattern)
            {
                return (BoundPattern)Instance.Visit(pattern);
            }

            // A discard node represents an always true pattern
            private bool IsDiscard(BoundPattern pattern)
            {
                return pattern is BoundDiscardPattern;
            }

            // A negated discard node represents an always false pattern
            private bool IsNegatedDiscard(BoundPattern pattern)
            {
                return pattern is BoundNegatedPattern { Negated: BoundDiscardPattern };
            }

            public override BoundNode? VisitBinaryPattern(BoundBinaryPattern node)
            {
                var resultLeft = (BoundPattern)Visit(node.Left);
                var resultRight = (BoundPattern)Visit(node.Right);
                var resultDisjunction = _negated ? !node.Disjunction : node.Disjunction;

                // TODO2 verify that each case is hit
                // TODO2 maybe we only simplify for synthesized nodes
                if (resultDisjunction)
                {
                    if (IsNegatedDiscard(resultLeft))
                    {
                        // not _ or <right>
                        return resultRight;
                    }

                    if (IsNegatedDiscard(resultRight))
                    {
                        // <left> or not _
                        return resultLeft;
                    }

                    if (IsDiscard(resultLeft))
                    {
                        // _ or <right>
                        return resultLeft;
                    }

                    if (IsDiscard(resultRight))
                    {
                        // <left> or _
                        return resultRight;
                    }
                }
                else
                {
                    if (IsNegatedDiscard(resultLeft))
                    {
                        // not _ and <right>
                        return resultLeft;
                    }

                    if (IsNegatedDiscard(resultRight))
                    {
                        // <left> and not _
                        return resultRight;
                    }

                    if (IsDiscard(resultLeft))
                    {
                        // _ and <right>
                        return resultRight;
                    }

                    if (IsDiscard(resultRight))
                    {
                        // <left> and _
                        return resultLeft;
                    }
                }

                var result = node.WithLeft(resultLeft).WithRight(resultRight);
                return _negated ? result.WithDisjunction(!node.Disjunction) : result;
            }

            public override BoundNode? VisitNegatedPattern(BoundNegatedPattern node)
            {
                if (_negated)
                {
                    return Instance.Visit(node.Negated);
                }

                return NegatedInstance.Visit(node.Negated);
            }

            public BoundPattern NegateIfNeeded(BoundPattern node)
            {
                if (_negated)
                {
                    return new BoundNegatedPattern(node.Syntax, node, node.InputType, narrowedType: node.InputType);
                }

                return node;
            }

            public override BoundNode? VisitTypePattern(BoundTypePattern node)
            {
                return NegateIfNeeded(node);
            }

            public override BoundNode? VisitConstantPattern(BoundConstantPattern node)
            {
                return NegateIfNeeded(node);
            }

            public override BoundNode? VisitDiscardPattern(BoundDiscardPattern node)
            {
                return NegateIfNeeded(node);
            }

            public override BoundNode? VisitDeclarationPattern(BoundDeclarationPattern node)
            {
                BoundPattern result;
                if (node.IsVar)
                {
                    result = MakeDiscardPattern(node);
                }
                else
                {
                    result = node;
                    //result = new BoundTypePattern(node.Syntax, node.DeclaredType, isExplicitNotNullTest: false, node.InputType, node.NarrowedType); // TODO2

                    if (node.InputType.Equals(node.DeclaredType.Type))
                    {
                        result = MakeDiscardPattern(node);
                    }
                }

                return NegateIfNeeded(result);
            }

            public override BoundNode? VisitRelationalPattern(BoundRelationalPattern node)
            {
                return NegateIfNeeded(node);
            }

            public override BoundNode? VisitRecursivePattern(BoundRecursivePattern node)
            {
                if (!_negated)
                {
                    return base.VisitRecursivePattern(node); // TODO2 is this correct, or do we need to recognize always-true/always-false cases?
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
                        new BoundTypePattern(node.Syntax, node.DeclaredType, node.IsExplicitNotNullTest, node.InputType, node.NarrowedType, node.HasErrors),
                        node.InputType, node.NarrowedType, node.HasErrors).MakeCompilerGenerated());
                }
                else if (node.InputType.CanContainNull())
                {
                    // `null`
                    builder.Add(new BoundConstantPattern(node.Syntax,
                        new BoundLiteral(node.Syntax, constantValueOpt: null, type: null, hasErrors: false), // dummy expression
                        ConstantValue.Null, node.InputType, node.NarrowedType, hasErrors: false).MakeCompilerGenerated());
                }

                ImmutableArray<BoundPositionalSubpattern> deconstruction = node.Deconstruction;
                if (!deconstruction.IsDefault)
                {
                    var discards = deconstruction.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d)));
                    for (int i = 0; i < deconstruction.Length; i++)
                    {
                        var negatedPattern = (BoundPattern)Visit(deconstruction[i].Pattern);

                        if (IsNegatedDiscard(negatedPattern))
                        {
                            continue;
                        }

                        // `Type ( ..., _, not DN, _, ... )`
                        var rewrittenDeconstruction = discards.SetItem(i, deconstruction[i].WithPattern(negatedPattern));

                        builder.Add(new BoundRecursivePattern(
                            deconstruction[i].Syntax, declaredType: node.DeclaredType, deconstructMethod: node.DeconstructMethod,
                            deconstruction: rewrittenDeconstruction,
                            properties: default, isExplicitNotNullTest: false, variable: null, variableAccess: null,
                            node.InputType, node.NarrowedType, node.HasErrors));
                    }
                }

                if (!node.Properties.IsDefault)
                {
                    foreach (BoundPropertySubpattern property in node.Properties)
                    {
                        var negatedPattern = (BoundPattern)Visit(property.Pattern);

                        if (IsNegatedDiscard(negatedPattern))
                        {
                            continue;
                        }

                        // `Type { PropN: not PN }`
                        builder.Add(new BoundRecursivePattern(
                            property.Syntax, declaredType: node.DeclaredType, deconstructMethod: null, deconstruction: default,
                            properties: [property.WithPattern(negatedPattern)],
                            isExplicitNotNullTest: false, variable: null, variableAccess: null,
                            node.InputType, node.NarrowedType, node.HasErrors));
                    }
                }

                BoundPattern result = MakeDisjunction(node, builder);
                builder.Free();
                return result;
            }

            private static BoundPattern MakeDisjunction(BoundPattern node, ArrayBuilder<BoundPattern> builder)
            {
                if (builder.Count == 0)
                {
                    return MakeDiscardPattern(node);
                }

                //Debug.Assert(!builder.All(p => p.WasCompilerGenerated));

                BoundPattern result = builder.Last();
                for (int i = builder.Count - 2; i >= 0; i--)
                {
                    result = new BoundBinaryPattern(node.Syntax, disjunction: true, builder[i], result, node.InputType, node.NarrowedType, node.HasErrors);
                }

                return result;
            }

            public override BoundNode? VisitITuplePattern(BoundITuplePattern node)
            {
                if (!_negated)
                {
                    return base.VisitITuplePattern(node);
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
                        ConstantValue.Null, node.InputType, node.NarrowedType, hasErrors: false).MakeCompilerGenerated());
                }

                var subpatterns = node.Subpatterns;
                var discards = subpatterns.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d)));

                for (int i = 0; i < subpatterns.Length; i++)
                {
                    var negatedPattern = (BoundPattern)Visit(subpatterns[i].Pattern);

                    if (IsNegatedDiscard(negatedPattern))
                    {
                        continue;
                    }

                    // `(..., not LN, ...)`
                    builder.Add(node.WithSubpatterns(discards.SetItem(i, subpatterns[i].WithPattern(negatedPattern))));
                }

                BoundPattern result = MakeDisjunction(node, builder);
                builder.Free();
                return result;
            }

            public override BoundNode? VisitListPattern(BoundListPattern node)
            {
                if (!_negated)
                {
                    return base.VisitListPattern(node);
                }

                // `[L1, L2, ...]`
                // can be expanded to `not null and [_, _, ...] and [L1, _, ...] and [_, L2, ...] and ...`
                // which can be negated to
                // `null or not [_, _, ...] or [not L1, _, ...] or [_, not L2, ...] or ...`

                var builder = ArrayBuilder<BoundPattern>.GetInstance();

                // TODO2 not for structs
                if (node.InputType.CanContainNull())
                {
                    // `null`
                    builder.Add(new BoundConstantPattern(node.Syntax,
                        new BoundLiteral(node.Syntax, constantValueOpt: null, type: null, hasErrors: false), // dummy expression
                        ConstantValue.Null, node.InputType, node.NarrowedType, hasErrors: false).MakeCompilerGenerated());
                }

                ImmutableArray<BoundPattern> discardSubpatterns = node.Subpatterns.SelectAsArray(replaceWithDiscards);

                // `not [_, _, ..., .._]`
                BoundListPattern listOfDiscards = node.WithSubpatterns(discardSubpatterns);
                if (!ListPatternHasOnlyEmptySlice(listOfDiscards))
                {
                    builder.Add(new BoundNegatedPattern(node.Syntax,
                        listOfDiscards,
                        node.InputType, node.NarrowedType, node.HasErrors).MakeCompilerGenerated());
                }

                for (int i = 0; i < discardSubpatterns.Length; i++)
                {
                    var negatedPattern = (BoundPattern)Visit(node.Subpatterns[i]);

                    if (IsNegatedDiscard(negatedPattern))
                    {
                        continue;
                    }

                    if (negatedPattern is BoundSlicePattern { Pattern: var p } && IsNegatedDiscard(p))
                    {
                        continue;
                    }

                    // `[..., _, not LN, _, ...]`
                    builder.Add(node.WithSubpatterns(discardSubpatterns.SetItem(i, negatedPattern)));
                }

                BoundPattern result = MakeDisjunction(node, builder);
                builder.Free();
                return result;

                static BoundPattern replaceWithDiscards(BoundPattern pattern)
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
                    return base.VisitSlicePattern(node);
                }

                if (node.Pattern is null)
                {
                    return node.WithPattern(
                        new BoundNegatedPattern(node.Syntax, MakeDiscardPattern(node), node.InputType, narrowedType: node.InputType));
                }

                var negatedPattern = (BoundPattern)Visit(node.Pattern);
                return node.WithPattern(negatedPattern);
            }

            internal static BoundPattern MakeNegatedPattern(BoundPattern node)
            {
                return new BoundNegatedPattern(node.Syntax, node, node.InputType, narrowedType: node.InputType);
            }

            private static BoundDiscardPattern MakeDiscardPattern(BoundPattern node)
            {
                return new BoundDiscardPattern(node.Syntax, node.InputType, node.NarrowedType);
            }

            private static BoundDiscardPattern MakeDiscardPattern(BoundPositionalSubpattern node)
            {
                return new BoundDiscardPattern(node.Syntax, node.Pattern.InputType, node.Pattern.NarrowedType);
            }
        }
    }
}
