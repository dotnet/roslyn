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
            CheckOrAndAndReachability(noPreviousCases, patternIndex: 0, pattern: pattern, builder: builder, rootIdentifier: rootIdentifier, defaultLabel: defaultLabel, syntax: syntax, diagnostics: diagnostics);
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
                CheckOrAndAndReachability(existingCases, patternIndex, switchArms[patternIndex].Pattern, builder, rootIdentifier, defaultLabel, syntax, diagnostics);
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
          BindingDiagnosticBag diagnostics)
        {
            CheckOrReachability(previousCases, patternIndex, pattern,
                builder, rootIdentifier, defaultLabel, syntax, diagnostics);

            var negated = new BoundNegatedPattern(pattern.Syntax, negated: pattern, pattern.InputType, narrowedType: pattern.InputType);
            CheckOrReachability(previousCases, patternIndex, negated,
                builder, rootIdentifier, defaultLabel, syntax, diagnostics);
        }

        private static void CheckOrReachability(
            ArrayBuilder<StateForCase> previousCases,
            int patternIndex,
            BoundPattern pattern,
            DecisionDagBuilder builder,
            BoundDagTemp rootIdentifier,
            LabelSymbol defaultLabel,
            SyntaxNode syntax,
            BindingDiagnosticBag diagnostics)
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
                populateStateForCases(builder, rootIdentifier, previousCases, patternIndex, orCases, labelsToIgnore, defaultLabel, syntax, ref casesBuilder.AsRef());
                BoundDecisionDag dag = builder.MakeBoundDecisionDag(syntax, ref casesBuilder.AsRef());

                foreach (StateForCase @case in casesBuilder)
                {
                    if (!dag.ReachableLabels.Contains(@case.CaseLabel) && !labelsToIgnore.Contains(@case.CaseLabel))
                    {
                        diagnostics.Add(ErrorCode.WRN_RedundantPattern, @case.Syntax);
                    }
                }

                labelsToIgnore.Free();
            }

            return;

            static void populateStateForCases(DecisionDagBuilder builder, BoundDagTemp rootIdentifier, ArrayBuilder<StateForCase> previousCases, int patternIndex,
                OrCases set, PooledHashSet<LabelSymbol> labelsToIgnore, LabelSymbol defaultLabel, SyntaxNode nodeSyntax, ref TemporaryArray<StateForCase> casesBuilder)
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
                    // In `A and B`, when found multiple `or` patterns in `A` to be expanded, we drop the `and B`
                    result = RewriteToSetsOfOrCases(binaryPattern.Left, conversions);

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
        /// 1. { `case A`, `case B` }
        /// 2. { `case (A or B) and C`, `case (A or B) and D` }
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

        // The purpose of this rewriter is to push `not` patterns down the pattern tree and
        // pull all the `and` and `or` patterns up the pattern tree.
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

            public override BoundNode? VisitBinaryPattern(BoundBinaryPattern node)
            {
                var resultLeft = VisitWithInputType(node.Left, _inputType);
                var resultDisjunction = _negated ? !node.Disjunction : node.Disjunction;

                var rightInputType = resultDisjunction ? _inputType : resultLeft.NarrowedType;
                var resultRight = VisitWithInputType(node.Right, rightInputType);

                TypeSymbol narrowedTypeForBinary2 = NarrowedTypeForBinary(resultLeft, resultRight, resultDisjunction);
                var result = new BoundBinaryPattern(node.Syntax, resultDisjunction, resultLeft, resultRight, _inputType, narrowedTypeForBinary2);
                return result;
            }

            private TypeSymbol NarrowedTypeForBinary(BoundPattern resultLeft, BoundPattern resultRight, bool resultDisjunction)
            {
                TypeSymbol narrowedType;
                if (resultDisjunction)
                {
                    var candidateTypes = ArrayBuilder<TypeSymbol>.GetInstance(2);
                    candidateTypes.Clear();
                    candidateTypes.Add(resultLeft.NarrowedType);
                    candidateTypes.Add(resultRight.NarrowedType);

                    narrowedType = this.LeastSpecificType(candidateTypes) ?? _inputType;
                    candidateTypes.Free();
                }
                else
                {
                    narrowedType = resultRight.NarrowedType;
                }

                return narrowedType;
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
                return WithInputTypeCheckIfNeeded(pattern, _inputType);
            }

            private static BoundPattern WithInputTypeCheckIfNeeded(BoundPattern pattern, TypeSymbol inputType)
            {
                // Produce `PatternInputType and pattern` given a new input type

                if (pattern.InputType.Equals(inputType, TypeCompareKind.AllIgnoreOptions))
                {
                    return pattern;
                }

                BoundPattern typePattern = new BoundTypePattern(pattern.Syntax,
                    new BoundTypeExpression(pattern.Syntax, aliasOpt: null, pattern.InputType),
                    isExplicitNotNullTest: false, inputType, narrowedType: pattern.InputType);

                var result = new BoundBinaryPattern(pattern.Syntax, disjunction: false, left: typePattern, right: pattern, inputType, pattern.NarrowedType);

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
                // If we're starting with `Type (D1, D2, ...) { Prop1: P1, Prop2: P2, ... } x`
                // - if we are not negating, we can expand it to 
                //   `Type (D1, _, ...) and Type (_, D2, ...) and Type { Prop1: P1 } and Type { Prop2: P2 } ...` 
                //
                // - if we are negating, we can expand it to 
                //   `not Type or Type (not D1, _, ...) or Type (_, not D2, ...) or Type { Prop1: not P1 } or Type { Prop2: not P2 } or ...`
                //   and the `and` and `or` patterns in the sub-patterns can then be lifted out further.
                //   For example, if `not D1` resolves to `E1 or F1`, the `Type (not D1, _, ...)` component can be normalized to
                //   `Type (E1, _, ...) or `Type (F1, _, ...)`.
                //   If there's not Type, we substitute a null check

                var builder = ArrayBuilder<BoundPattern>.GetInstance();

                TypeSymbol inputType = _inputType;
                bool isEmptyPropertyPattern = node.Deconstruction.IsDefault && node.Properties is { IsDefault: false, IsEmpty: true };
                if (_negated)
                {
                    if (node.DeclaredType is not null)
                    {
                        // `not Type`
                        builder.Add(new BoundNegatedPattern(node.Syntax,
                            new BoundTypePattern(node.Syntax, node.DeclaredType, node.IsExplicitNotNullTest, inputType, node.NarrowedType, node.HasErrors),
                            inputType, narrowedType: inputType, node.HasErrors).MakeCompilerGenerated());
                    }
                    else if (node.InputType.CanContainNull())
                    {
                        // `null`
                        BoundConstantPattern nullPattern = new BoundConstantPattern(node.Syntax,
                            new BoundLiteral(node.Syntax, constantValueOpt: null, type: null, hasErrors: false),
                            ConstantValue.Null, inputType, inputType, hasErrors: false);

                        if (!isEmptyPropertyPattern)
                        {
                            nullPattern = nullPattern.MakeCompilerGenerated();
                        }

                        builder.Add(nullPattern);
                    }
                }
                else
                {
                    if (isEmptyPropertyPattern)
                    {
                        builder.Add(WithInputTypeCheckIfNeeded(node));
                    }
                }

                ImmutableArray<BoundPositionalSubpattern> deconstruction = node.Deconstruction;
                if (!deconstruction.IsDefault)
                {
                    var discards = deconstruction.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d.Syntax, d.Pattern.InputType)));
                    for (int i = 0; i < deconstruction.Length; i++)
                    {
                        BoundPattern rewrittenPattern = VisitWithInputTypeFromPattern(deconstruction[i].Pattern);
                        if (CanSkip(rewrittenPattern))
                        {
                            continue;
                        }

                        BoundPattern rewrittenRecursive = PullBinaryPatternsOut(rewrittenPattern, inputType, makeDeconstructionPattern, args: (node, deconstruction[i], discards, i));
                        builder.Add(rewrittenRecursive);

                        if (!_negated)
                        {
                            inputType = rewrittenRecursive.NarrowedType;
                        }
                    }
                }

                if (!node.Properties.IsDefault)
                {
                    foreach (BoundPropertySubpattern property in node.Properties)
                    {
                        BoundPattern rewrittenPattern = VisitWithInputTypeFromPattern(property.Pattern);

                        if (CanSkip(rewrittenPattern))
                        {
                            continue;
                        }

                        BoundPattern rewrittenRecursive = PullBinaryPatternsOut(rewrittenPattern, inputType, makePropertyPattern, args: (node, property));
                        builder.Add(rewrittenRecursive);

                        if (!_negated)
                        {
                            inputType = rewrittenRecursive.NarrowedType;
                        }
                    }
                }

                BoundPattern result = MakeBinary(disjunction: _negated, builder: builder, syntax: node.Syntax);
                builder.Free();
                return result;

                // Make `Type ( ..., _, <newPattern>, _, ... )`
                static BoundPattern makeDeconstructionPattern(BoundPattern newPattern, TypeSymbol inputType,
                    (BoundRecursivePattern recursiveTemplate, BoundPositionalSubpattern subpatternTemplate, ImmutableArray<BoundPositionalSubpattern> discards, int i) args)
                {
                    newPattern = WithInputTypeCheckIfNeeded(newPattern, args.subpatternTemplate.Pattern.InputType);
                    ImmutableArray<BoundPositionalSubpattern> newSubpatterns = args.discards.SetItem(args.i, args.subpatternTemplate.WithPattern(newPattern));

                    BoundPattern newRecursive = new BoundRecursivePattern(
                        newPattern.Syntax, declaredType: args.recursiveTemplate.DeclaredType, deconstructMethod: args.recursiveTemplate.DeconstructMethod,
                        deconstruction: newSubpatterns,
                        properties: default, isExplicitNotNullTest: false, variable: null, variableAccess: null,
                        args.recursiveTemplate.InputType, args.recursiveTemplate.NarrowedType, args.recursiveTemplate.HasErrors);

                    newRecursive = WithInputTypeCheckIfNeeded(newRecursive, inputType);

                    return newRecursive;
                }

                // Make `Type { Prop: <newPattern> }`
                static BoundPattern makePropertyPattern(BoundPattern newPattern, TypeSymbol inputType,
                    (BoundRecursivePattern recursiveTemplate, BoundPropertySubpattern subpatternTemplate) args)
                {
                    newPattern = WithInputTypeCheckIfNeeded(newPattern, args.subpatternTemplate.Pattern.InputType);
                    ImmutableArray<BoundPropertySubpattern> newSubpatterns = [args.subpatternTemplate.WithPattern(newPattern)];

                    BoundPattern newRecursive = new BoundRecursivePattern(
                        newPattern.Syntax, declaredType: args.recursiveTemplate.DeclaredType, deconstructMethod: null, deconstruction: default,
                        properties: newSubpatterns,
                        isExplicitNotNullTest: false, variable: null, variableAccess: null,
                        args.recursiveTemplate.InputType, args.recursiveTemplate.NarrowedType, args.recursiveTemplate.HasErrors);

                    newRecursive = WithInputTypeCheckIfNeeded(newRecursive, inputType);

                    return newRecursive;
                }
            }

            private bool CanSkip(BoundPattern pattern)
            {
                if (_negated)
                {
                    return pattern is BoundNegatedPattern { Negated: BoundDiscardPattern };
                }

                return pattern is BoundDiscardPattern;
            }

            delegate BoundPattern ReplaceLeafPattern<T>(BoundPattern leaf, TypeSymbol inputType, T args);

            // In a tree of `and` and `or` patterns, replace the leaves
            //
            // For example, when looking at the `(A or B) and C` in `{ Prop: (A or B) and C }`
            // we produce `({ Prop: A } or { Prop: B }) and { Prop: C }`
            // by using a replacement function (x) => `{ Prop: x }`.
            private BoundPattern PullBinaryPatternsOut<TArgs>(BoundPattern pattern, TypeSymbol newInputType, ReplaceLeafPattern<TArgs> replaceLeaf, TArgs args)
            {
                if (pattern is not BoundBinaryPattern binaryPattern)
                {
                    BoundPattern result = replaceLeaf(pattern, inputType: newInputType, args);
                    if (pattern.WasCompilerGenerated)
                    {
                        result = result.MakeCompilerGenerated();
                    }

                    return result;
                }

                BoundPattern resultLeft = PullBinaryPatternsOut(binaryPattern.Left, newInputType, replaceLeaf, args);

                var rightInputType = binaryPattern.Disjunction ? newInputType : resultLeft.NarrowedType;
                BoundPattern resultRight = PullBinaryPatternsOut(binaryPattern.Right, rightInputType, replaceLeaf, args);

                TypeSymbol narrowedTypeForBinary = NarrowedTypeForBinary(resultLeft, resultRight, binaryPattern.Disjunction);

                return new BoundBinaryPattern(binaryPattern.Syntax, binaryPattern.Disjunction,
                    left: resultLeft,
                    right: resultRight,
                    newInputType, narrowedTypeForBinary);
            }

            public override BoundNode? VisitPropertySubpattern(BoundPropertySubpattern node)
            {
                throw ExceptionUtilities.Unreachable();
            }

            public override BoundNode? VisitPositionalSubpattern(BoundPositionalSubpattern node)
            {
                throw ExceptionUtilities.Unreachable();
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

            private BoundPattern MakeBinary(bool disjunction, ArrayBuilder<BoundPattern> builder, SyntaxNode syntax)
            {
                if (builder.Count == 0)
                {
                    BoundDiscardPattern discard = MakeDiscardPattern(syntax, _inputType).MakeCompilerGenerated();
                    if (disjunction)
                    {
                        // The caller normally wants to make an `... or ...` sequence but all the parts could be skipped (discards)
                        // so it can be summed up as a discard too
                        return discard;
                    }
                    else
                    {
                        // The caller normally wants to make an `... and ...` sequence but all the parts could be skipped (negated discards)
                        // so it can be summed up as a negative discard too
                        return new BoundNegatedPattern(syntax, discard, _inputType, narrowedType: _inputType).MakeCompilerGenerated();
                    }
                }

                BoundPattern result;
                if (disjunction)
                {
                    // Produce an `or` chain
                    result = builder.Last();
                    Debug.Assert(builder.All(p => p.InputType.Equals(_inputType, TypeCompareKind.AllIgnoreOptions)));
                    var candidateTypes = ArrayBuilder<TypeSymbol>.GetInstance(2);

                    for (int i = builder.Count - 2; i >= 0; i--)
                    {
                        candidateTypes.Clear();
                        candidateTypes.Add(builder[i].NarrowedType);
                        candidateTypes.Add(result.NarrowedType);

                        var narrowedType = this.LeastSpecificType(candidateTypes) ?? _inputType;
                        result = new BoundBinaryPattern(syntax, disjunction: true, builder[i], result, _inputType, narrowedType);
                    }

                    candidateTypes.Free();
                }
                else
                {
                    // Produce an `and` chain
                    result = builder.First();
                    Debug.Assert(_inputType.Equals(result.InputType, TypeCompareKind.AllIgnoreOptions));
                    var inputType = result.InputType;
                    var narrowedType = result.NarrowedType;

                    for (int i = 1; i < builder.Count; i++)
                    {
                        narrowedType = builder[i].NarrowedType;
                        Debug.Assert(result.NarrowedType.Equals(builder[i].InputType, TypeCompareKind.AllIgnoreOptions));
                        result = new BoundBinaryPattern(syntax, disjunction: false, result, builder[i], inputType, narrowedType);
                    }
                }

                return result;
            }

            public override BoundNode? VisitITuplePattern(BoundITuplePattern ituplePattern)
            {
                var builder = ArrayBuilder<BoundPattern>.GetInstance();

                if (_negated)
                {
                    if (ituplePattern.InputType.CanContainNull())
                    {
                        // `null`
                        builder.Add(new BoundConstantPattern(ituplePattern.Syntax,
                            new BoundLiteral(ituplePattern.Syntax, constantValueOpt: null, type: null, hasErrors: false),
                            ConstantValue.Null, _inputType, _inputType, hasErrors: false).MakeCompilerGenerated());
                    }
                }

                var subpatterns = ituplePattern.Subpatterns;
                var discards = subpatterns.SelectAsArray(d => d.WithPattern(MakeDiscardPattern(d.Syntax, d.Pattern.InputType)));
                TypeSymbol inputType = _inputType;
                for (int i = 0; i < subpatterns.Length; i++)
                {
                    BoundPattern newPattern = VisitWithInputTypeFromPattern(subpatterns[i].Pattern);

                    if (CanSkip(newPattern))
                    {
                        continue;
                    }

                    BoundPattern rewrittenRecursive = PullBinaryPatternsOut(newPattern, inputType, makeITuplePattern, args: (ituplePattern, subpatterns[i], discards, i));
                    builder.Add(rewrittenRecursive);

                    if (!_negated)
                    {
                        inputType = rewrittenRecursive.InputType;
                    }
                }

                BoundPattern result = MakeBinary(disjunction: true, builder: builder, syntax: ituplePattern.Syntax);
                builder.Free();
                return result;

                // Make `( ..., _, <newPattern>, _, ... )`
                static BoundPattern makeITuplePattern(BoundPattern newPattern, TypeSymbol inputType,
                    (BoundITuplePattern itupleTemplate, BoundPositionalSubpattern positionalTemplate, ImmutableArray<BoundPositionalSubpattern> discards, int i) args)
                {
                    newPattern = WithInputTypeCheckIfNeeded(newPattern, args.positionalTemplate.Pattern.InputType);
                    ImmutableArray<BoundPositionalSubpattern> newSubpatterns = args.discards.SetItem(args.i, args.positionalTemplate.WithPattern(newPattern));

                    BoundPattern newITuple = new BoundITuplePattern(newPattern.Syntax, args.itupleTemplate.GetLengthMethod,
                        args.itupleTemplate.GetItemMethod, newSubpatterns, args.itupleTemplate.InputType, args.itupleTemplate.NarrowedType);

                    newITuple = WithInputTypeCheckIfNeeded(newITuple, inputType);

                    return newITuple;
                }
            }

            public override BoundNode? VisitListPattern(BoundListPattern listPattern)
            {
                // If we're starting with `[L1, L2, ...]`
                // - if we are not negating, we can expand it to `[L1, _, ...] and [_, L2, ...] and ...`
                //   and the `and` and `or` patterns in the element patterns can then be lifted out further.
                // 
                // - if we are negating, we can expand it to `null or not [_, _, ...] or [not L1, _, ...] or [_, not L2, ...] or ...`
                //   and the `and` and `or` patterns in the resulting element patterns can then be lifted out further.

                var builder = ArrayBuilder<BoundPattern>.GetInstance();
                ImmutableArray<BoundPattern> discards = listPattern.Subpatterns.SelectAsArray(replaceWithDiscards);

                if (_negated)
                {
                    if (listPattern.InputType.CanContainNull())
                    {
                        // `null`
                        builder.Add(new BoundConstantPattern(listPattern.Syntax,
                            new BoundLiteral(listPattern.Syntax, constantValueOpt: null, type: null, hasErrors: false),
                            ConstantValue.Null, _inputType, _inputType).MakeCompilerGenerated());
                    }

                    // `not [_, _, ..., .._]`
                    BoundListPattern listOfDiscards = listPattern.WithSubpatterns(discards);
                    if (!ListPatternHasOnlyEmptySlice(listOfDiscards))
                    {
                        builder.Add(new BoundNegatedPattern(listPattern.Syntax,
                            WithInputTypeCheckIfNeeded(listOfDiscards), _inputType, _inputType, listPattern.HasErrors).MakeCompilerGenerated());
                    }
                }
                else if (discards.IsEmpty)
                {
                    builder.Add(WithInputTypeCheckIfNeeded(listPattern));
                }

                TypeSymbol inputType = _inputType;
                for (int i = 0; i < discards.Length; i++)
                {
                    BoundPattern rewrittenList;
                    if (listPattern.Subpatterns[i] is BoundSlicePattern slicePattern)
                    {
                        BoundPattern? rewrittenPattern = VisitWithInputTypeFromPattern(slicePattern.Pattern);
                        if (rewrittenPattern is null)
                        {
                            if (!_negated)
                            {
                                continue;
                            }

                            rewrittenPattern = new BoundNegatedPattern(
                                listPattern.Syntax,
                                MakeDiscardPattern(listPattern.Syntax, inputType).MakeCompilerGenerated(),
                                inputType, narrowedType: inputType).MakeCompilerGenerated();

                            builder.Add(rewrittenPattern);
                            continue;
                        }

                        if (rewrittenPattern is BoundSlicePattern { Pattern: { } p } && CanSkip(p))
                        {
                            continue;
                        }

                        rewrittenList = PullBinaryPatternsOut(rewrittenPattern, inputType, makeListPatternWithSlice, args: (listPattern, slicePattern, discards, i));
                    }
                    else
                    {
                        BoundPattern rewrittenPattern = VisitWithInputTypeFromPattern(listPattern.Subpatterns[i]);

                        if (CanSkip(rewrittenPattern))
                        {
                            continue;
                        }

                        rewrittenList = PullBinaryPatternsOut(rewrittenPattern, inputType, makeListPattern, args: (listPattern, discards, i));
                    }

                    builder.Add(rewrittenList);

                    if (!_negated)
                    {
                        inputType = rewrittenList.NarrowedType;
                    }
                }

                BoundPattern result = MakeBinary(disjunction: _negated, builder: builder, syntax: listPattern.Syntax);
                builder.Free();
                return result;

                static BoundPattern replaceWithDiscards(BoundPattern pattern)
                {
                    if (pattern is BoundSlicePattern slice)
                    {
                        return slice.WithPattern(null);
                    }

                    return MakeDiscardPattern(pattern.Syntax, pattern.InputType);
                }

                // Make `[ ..., _, <newPattern>, _, ... ]`
                static BoundPattern makeListPattern(BoundPattern newPattern, TypeSymbol inputType,
                    (BoundListPattern listTemplate, ImmutableArray<BoundPattern> discards, int i) args)
                {
                    newPattern = WithInputTypeCheckIfNeeded(newPattern, args.discards[args.i].InputType);
                    ImmutableArray<BoundPattern> newSubpatterns = args.discards.SetItem(args.i, newPattern);

                    BoundPattern newList = new BoundListPattern(
                        newPattern.Syntax, newSubpatterns, hasSlice: newSubpatterns.Any(p => p is BoundSlicePattern), args.listTemplate.LengthAccess, args.listTemplate.IndexerAccess,
                        args.listTemplate.ReceiverPlaceholder, args.listTemplate.ArgumentPlaceholder, args.listTemplate.Variable, args.listTemplate.VariableAccess,
                        args.listTemplate.InputType, args.listTemplate.NarrowedType);

                    newList = WithInputTypeCheckIfNeeded(newList, inputType);

                    return newList;
                }

                // Make `[ ..., _, ..<newPattern>, _, ... ]`
                static BoundPattern makeListPatternWithSlice(BoundPattern newPattern, TypeSymbol inputType,
                    (BoundListPattern listTemplate, BoundSlicePattern sliceTemplate, ImmutableArray<BoundPattern> discards, int i) args)
                {
                    Debug.Assert(args.sliceTemplate.Pattern is not null);

                    newPattern = WithInputTypeCheckIfNeeded(newPattern, args.sliceTemplate.Pattern.InputType);
                    newPattern = new BoundSlicePattern(newPattern.Syntax, newPattern, args.sliceTemplate.IndexerAccess,
                        args.sliceTemplate.ReceiverPlaceholder, args.sliceTemplate.ArgumentPlaceholder, args.sliceTemplate.InputType, args.sliceTemplate.NarrowedType);

                    ImmutableArray<BoundPattern> newSubpatterns = args.discards.SetItem(args.i, newPattern);

                    BoundPattern newList = new BoundListPattern(
                        newPattern.Syntax, newSubpatterns, hasSlice: true, args.listTemplate.LengthAccess, args.listTemplate.IndexerAccess,
                        args.listTemplate.ReceiverPlaceholder, args.listTemplate.ArgumentPlaceholder, args.listTemplate.Variable, args.listTemplate.VariableAccess,
                        args.listTemplate.InputType, args.listTemplate.NarrowedType);

                    newList = WithInputTypeCheckIfNeeded(newList, inputType);

                    return newList;
                }
            }

            public override BoundNode VisitSlicePattern(BoundSlicePattern node)
            {
                throw ExceptionUtilities.Unreachable();
            }

            private BoundDiscardPattern MakeDiscardPattern(BoundPattern node)
            {
                return MakeDiscardPattern(node.Syntax, _inputType);
            }

            private static BoundDiscardPattern MakeDiscardPattern(SyntaxNode syntax, TypeSymbol inputType)
            {
                return new BoundDiscardPattern(syntax, inputType, inputType);
            }
        }
    }
}
