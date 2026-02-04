// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class NullableWalker
    {
        /// <summary>
        /// Learn something about the input from a test of a given expression against a given pattern.  The given
        /// state is updated to note that any slots that are tested against `null` may be null.
        /// </summary>
        private void LearnFromAnyNullPatterns(
            BoundExpression expression,
            BoundPattern pattern)
        {
            int slot = MakeSlot(expression);
            LearnFromAnyNullPatterns(slot, expression.Type, pattern);
        }

        private void VisitForRewriting(BoundNode node)
        {
            // Don't let anything under the node actually affect current state,
            // as we're only visiting for nullable information.
            Debug.Assert(!IsConditionalState);
            var currentState = State;
            VisitWithoutDiagnostics(node);
            SetState(currentState);
        }

        public override BoundNode VisitPositionalSubpattern(BoundPositionalSubpattern node)
        {
            Visit(node.Pattern);
            return null;
        }

        public override BoundNode VisitPropertySubpattern(BoundPropertySubpattern node)
        {
            Visit(node.Pattern);
            return null;
        }

        public override BoundNode VisitRecursivePattern(BoundRecursivePattern node)
        {
            Visit(node.DeclaredType);
            VisitAndUnsplitAll(node.Deconstruction);
            VisitAndUnsplitAll(node.Properties);
            Visit(node.VariableAccess);
            return null;
        }

        public override BoundNode VisitConstantPattern(BoundConstantPattern node)
        {
            VisitRvalue(node.Value);
            return null;
        }

        public override BoundNode VisitDeclarationPattern(BoundDeclarationPattern node)
        {
            Visit(node.VariableAccess);
            Visit(node.DeclaredType);
            return null;
        }

        public override BoundNode VisitDiscardPattern(BoundDiscardPattern node)
        {
            return null;
        }

        public override BoundNode VisitSlicePattern(BoundSlicePattern node)
        {
            Visit(node.Pattern);
            return null;
        }

        public override BoundNode VisitListPattern(BoundListPattern node)
        {
            VisitAndUnsplitAll(node.Subpatterns);
            Visit(node.VariableAccess);
            return null;
        }

        public override BoundNode VisitTypePattern(BoundTypePattern node)
        {
            Visit(node.DeclaredType);
            return null;
        }

        public override BoundNode VisitRelationalPattern(BoundRelationalPattern node)
        {
            Visit(node.Value);
            return null;
        }

        public override BoundNode VisitNegatedPattern(BoundNegatedPattern node)
        {
            Visit(node.Negated);
            return null;
        }

        public override BoundNode VisitBinaryPattern(BoundBinaryPattern node)
        {
            // Users (such as ourselves) can have many, many nested binary patterns. To avoid crashing, do left recursion manually.

            var stack = ArrayBuilder<BoundBinaryPattern>.GetInstance();
            BoundBinaryPattern current = node;
            do
            {
                stack.Push(current);
                current = current.Left as BoundBinaryPattern;
            } while (current != null);

            current = stack.Pop();
            // We don't need to snapshot on the way down because the left spine of the tree will always have the same span start, and each
            // call to TakeIncrementalSnapshot would overwrite the previous one with the new state. This can be a _significant_ performance
            // improvement for deeply nested binary patterns; over 10x faster in some pathological cases.
            TakeIncrementalSnapshot(current);
            Debug.Assert(current.Left is not BoundBinaryPattern);
            Visit(current.Left);

            do
            {
                Visit(current.Right);
            } while (stack.TryPop(out current));

            stack.Free();
            return null;
        }

        public override BoundNode VisitITuplePattern(BoundITuplePattern node)
        {
            VisitAndUnsplitAll(node.Subpatterns);
            return null;
        }

        /// <summary>
        /// Learn from any constant null patterns appearing in the pattern.
        /// </summary>
        /// <param name="inputType">Type type of the input expression (before nullable analysis).
        /// Used to determine which types can contain null.</param>
        private void LearnFromAnyNullPatterns(
            int inputSlot,
            TypeSymbol inputType,
            BoundPattern pattern)
        {
            if (inputSlot <= 0)
                return;

            // https://github.com/dotnet/roslyn/issues/35041 We only need to do this when we're rewriting, so we
            // can get information for any nodes in the pattern.
            VisitForRewriting(pattern);

            switch (pattern)
            {
                case BoundConstantPattern cp:
                    bool isExplicitNullCheck = cp.Value.ConstantValueOpt == ConstantValue.Null;
                    if (isExplicitNullCheck)
                    {
                        // Since we're not branching on this null test here, we just infer the top level
                        // nullability.  We'll branch on it later.
                        LearnFromNullTest(inputSlot, inputType, ref this.State, markDependentSlotsNotNull: false);
                    }
                    break;
                case BoundDeclarationPattern _:
                case BoundDiscardPattern _:
                case BoundITuplePattern _:
                case BoundRelationalPattern _:
                case BoundSlicePattern _:
                case BoundListPattern lp:
                    break; // nothing to learn
                case BoundTypePattern tp:
                    if (tp.IsExplicitNotNullTest)
                    {
                        LearnFromNullTest(inputSlot, inputType, ref this.State, markDependentSlotsNotNull: false);
                    }
                    break;
                case BoundRecursivePattern rp:
                    {
                        if (rp.IsExplicitNotNullTest)
                        {
                            LearnFromNullTest(inputSlot, inputType, ref this.State, markDependentSlotsNotNull: false);
                        }

                        // for positional part: we only learn from tuples (not Deconstruct)
                        if (rp.DeconstructMethod is null && !rp.Deconstruction.IsDefault)
                        {
                            var elements = inputType.TupleElements;
                            for (int i = 0, n = Math.Min(rp.Deconstruction.Length, elements.IsDefault ? 0 : elements.Length); i < n; i++)
                            {
                                BoundSubpattern item = rp.Deconstruction[i];
                                FieldSymbol element = elements[i];
                                LearnFromAnyNullPatterns(GetOrCreateSlot(element, inputSlot), element.Type, item.Pattern);
                            }
                        }

                        // for property part
                        if (!rp.Properties.IsDefault)
                        {
                            foreach (BoundPropertySubpattern subpattern in rp.Properties)
                            {
                                if (subpattern.Member is BoundPropertySubpatternMember member)
                                {
                                    LearnFromAnyNullPatterns(getExtendedPropertySlot(member, inputSlot), member.Type, subpattern.Pattern);
                                }
                            }
                        }
                    }
                    break;
                case BoundNegatedPattern p:
                    LearnFromAnyNullPatterns(inputSlot, inputType, p.Negated);
                    break;
                case BoundBinaryPattern p:
                    // Do not use left recursion because we can have many nested binary patterns.
                    var current = p;
                    while (true)
                    {
                        // We don't need to visit in order here because we're only moving analysis in one direction:
                        // towards MaybeNull. Visiting the right or left first has no impact on the final state.
                        LearnFromAnyNullPatterns(inputSlot, inputType, current.Right);
                        if (current.Left is BoundBinaryPattern left)
                        {
                            current = left;
                            VisitForRewriting(current);
                        }
                        else
                        {
                            LearnFromAnyNullPatterns(inputSlot, inputType, current.Left);
                            break;
                        }
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern);
            }

            int getExtendedPropertySlot(BoundPropertySubpatternMember member, int inputSlot)
            {
                if (member.Symbol is null)
                {
                    return -1;
                }

                if (member.Receiver is not null)
                {
                    inputSlot = getExtendedPropertySlot(member.Receiver, inputSlot);
                }

                if (inputSlot < 0)
                {
                    return inputSlot;
                }

                if (member.Symbol.Kind is not (SymbolKind.Property or SymbolKind.Field))
                {
                    return -1;
                }

                return GetOrCreateSlot(member.Symbol, inputSlot);
            }
        }

        protected override LocalState VisitSwitchStatementDispatch(BoundSwitchStatement node)
        {
            // first, learn from any null tests in the patterns
            int slot = GetSlotForSwitchInputValue(node.Expression);
            if (slot > 0)
            {
                var originalInputType = node.Expression.Type;
                foreach (var section in node.SwitchSections)
                {
                    foreach (var label in section.SwitchLabels)
                    {
                        LearnFromAnyNullPatterns(slot, originalInputType, label.Pattern);
                    }
                }
            }

            DeclareLocals(node.InnerLocals);
            foreach (var section in node.SwitchSections)
            {
                // locals can be alive across jumps in the switch sections, so we declare them early.
                DeclareLocals(section.Locals);
            }

            // visit switch header
            Visit(node.Expression);
            var expressionState = ResultType;

            var labelStateMap = LearnFromDecisionDag(node.Syntax, node.ReachabilityDecisionDag, node.Expression, expressionState, stateWhenNotNullOpt: null);
            foreach (var section in node.SwitchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    var labelResult = labelStateMap.TryGetValue(label.Label, out var s1) ? s1 : (state: UnreachableState(), believedReachable: false);
                    SetState(labelResult.state);
                    PendingBranches.Add(new PendingBranch(label, this.State, label.Label));
                }
            }

            var afterSwitchState = labelStateMap.TryGetValue(node.BreakLabel, out var stateAndReachable) ? stateAndReachable.state : UnreachableState();
            labelStateMap.Free();
            return afterSwitchState;
        }

        protected override void VisitSwitchSection(BoundSwitchSection node, bool isLastSection)
        {
            TakeIncrementalSnapshot(node);
            SetState(UnreachableState());
            foreach (var label in node.SwitchLabels)
            {
                TakeIncrementalSnapshot(label);
                VisitForRewriting(label.Pattern);

                if (!State.Reachable && label.WhenClause != null)
                {
                    // Unreachable when clauses are not visited in `LearnFromDecisionDag`.
                    VisitForRewriting(label.WhenClause);
                }

                VisitLabel(label.Label, node);
            }

            VisitStatementList(node);
        }

        private struct PossiblyConditionalState
        {
            public LocalState State;
            public LocalState StateWhenTrue;
            public LocalState StateWhenFalse;
            public bool IsConditionalState;

            public PossiblyConditionalState(LocalState stateWhenTrue, LocalState stateWhenFalse)
            {
                StateWhenTrue = stateWhenTrue.Clone();
                StateWhenFalse = stateWhenFalse.Clone();
                IsConditionalState = true;
                State = default;
            }

            public PossiblyConditionalState(LocalState state)
            {
                StateWhenTrue = StateWhenFalse = default;
                IsConditionalState = false;
                State = state.Clone();
            }

            public static PossiblyConditionalState Create(NullableWalker nullableWalker)
            {
                return nullableWalker.IsConditionalState
                    ? new PossiblyConditionalState(nullableWalker.StateWhenTrue, nullableWalker.StateWhenFalse)
                    : new PossiblyConditionalState(nullableWalker.State);
            }

            public PossiblyConditionalState Clone()
            {
                return IsConditionalState
                    ? new PossiblyConditionalState(StateWhenTrue, StateWhenFalse)
                    : new PossiblyConditionalState(State);
            }
        }

        private PooledDictionary<LabelSymbol, (LocalState state, bool believedReachable)> LearnFromDecisionDag(
            SyntaxNode node,
            BoundDecisionDag decisionDag,
            BoundExpression expression,
            TypeWithState expressionTypeWithState,
            PossiblyConditionalState? stateWhenNotNullOpt)
        {
            // We reuse the slot at the beginning of a switch (or is-pattern expression), pretending that we are
            // not copying the input to evaluate the patterns.  In this way we infer non-nullability of the original
            // variable's parts based on matched pattern parts.  Mutations in `when` clauses can show the inaccuracy
            // of analysis based on this choice.
            var rootTemp = BoundDagTemp.ForOriginalInput(expression);
            int originalInputSlot = MakeSlot(expression);
            var expressionTypeWithAnnotations = expressionTypeWithState.ToTypeWithAnnotations(compilation);
            if (originalInputSlot <= 0)
            {
                originalInputSlot = makeDagTempSlot(expressionTypeWithAnnotations, rootTemp);
                if (!IsConditionalState)
                {
                    TrackNullableStateForAssignment(valueOpt: null, expressionTypeWithAnnotations, originalInputSlot, expressionTypeWithState);
                }
            }
            Debug.Assert(originalInputSlot > 0);

            // If the input of the switch (or is-pattern expression) is a tuple literal, we reuse the slots of
            // those expressions (when possible), pretending that we are not copying them into a temporary ValueTuple instance
            // to evaluate the patterns.  In this way we infer non-nullability of the original element's parts.
            // We do not extend such courtesy to nested tuple literals.
            var originalInputElementSlots = expression is BoundTupleExpression tuple
                ? tuple.Arguments.SelectAsArray(static (a, w) => w.GetSlotForSwitchInputValue(a), this)
                : default;
            var originalInputMap = PooledDictionary<int, BoundExpression>.GetInstance();
            originalInputMap.Add(originalInputSlot, expression);

            // Note we customize equality in BoundDagTemp
            var tempMap = PooledDictionary<BoundDagTemp, (int slot, TypeSymbol type)>.GetInstance();
            var reinferredPropertyMap = PooledDictionary<BoundDagPropertyEvaluation, PropertySymbol>.GetInstance();
            Debug.Assert(isDerivedType(NominalSlotType(originalInputSlot), expressionTypeWithState.Type));
            tempMap.Add(rootTemp, (originalInputSlot, expressionTypeWithState.Type));

            var nodeStateMap = PooledDictionary<BoundDecisionDagNode, (PossiblyConditionalState state, bool believedReachable)>.GetInstance();
            nodeStateMap.Add(decisionDag.RootNode, (state: PossiblyConditionalState.Create(this), believedReachable: true));

            var labelStateMap = PooledDictionary<LabelSymbol, (LocalState state, bool believedReachable)>.GetInstance();

            foreach (var dagNode in decisionDag.TopologicallySortedNodes)
            {
                bool found = nodeStateMap.TryGetValue(dagNode, out var nodeStateAndBelievedReachable);
                Debug.Assert(found); // the topologically sorted nodes should contain only reachable nodes
                (PossiblyConditionalState nodeState, bool nodeBelievedReachable) = nodeStateAndBelievedReachable;
                if (nodeState.IsConditionalState)
                {
                    SetConditionalState(nodeState.StateWhenTrue, nodeState.StateWhenFalse);
                }
                else
                {
                    SetState(nodeState.State);
                }

                switch (dagNode)
                {
                    case BoundEvaluationDecisionDagNode p:
                        {
                            var evaluation = p.Evaluation;
                            (int inputSlot, TypeSymbol inputType) = tempMap.TryGetValue(evaluation.Input, out var slotAndType) ? slotAndType : throw ExceptionUtilities.Unreachable();
                            Debug.Assert(inputSlot > 0);

                            switch (evaluation)
                            {
                                case BoundDagDeconstructEvaluation e:
                                    {
                                        // https://github.com/dotnet/roslyn/issues/34232
                                        // We may need to recompute the Deconstruct method for a deconstruction if
                                        // the receiver type has changed (e.g. its nested nullability).
                                        ArrayBuilder<BoundDagTemp> outParamTemps = e.MakeOutParameterTemps();
                                        foreach (var output in outParamTemps)
                                        {
                                            int outputSlot = getOrMakeAndRegisterDagTempSlot(output);
                                        }
                                        outParamTemps.Free();
                                        break;
                                    }
                                case BoundDagTypeEvaluation e:
                                    {
                                        var output = e.MakeResultTemp();
                                        int outputSlot = getOrMakeAndRegisterDagTempSlot(output);
                                        Debug.Assert(!IsConditionalState);
                                        Unsplit();
                                        SetState(ref State, outputSlot, NullableFlowState.NotNull);
                                        break;
                                    }
                                case BoundDagFieldEvaluation e:
                                    {
                                        var output = e.MakeResultTemp();
                                        int outputSlot = getOrMakeAndRegisterDagTempSlot(output);
                                        Debug.Assert(outputSlot > 0);
                                        break;
                                    }
                                case BoundDagPropertyEvaluation e:
                                    {
                                        Debug.Assert(inputSlot > 0);
                                        var property = getReInferredProperty(inputType, e);
                                        var type = property.TypeWithAnnotations;

                                        var output = e.MakeResultTemp();
                                        int outputSlot = getOrMakeAndRegisterDagTempSlot(output);
                                        Debug.Assert(outputSlot > 0);

                                        if (property.GetMethod is not null)
                                        {
                                            // A property evaluation splits the state if MemberNotNullWhen is used
                                            ApplyMemberPostConditions(inputSlot, property.GetMethod);
                                        }

                                        break;
                                    }
                                case BoundDagIndexEvaluation e:
                                    {
                                        var output = e.MakeResultTemp();
                                        int outputSlot = getOrMakeAndRegisterDagTempSlot(output);
                                        Debug.Assert(outputSlot > 0);
                                        break;
                                    }
                                case BoundDagIndexerEvaluation e:
                                    {
                                        // tDest = tSource[index]
                                        TypeWithAnnotations type = getIndexerOutputType(inputType, e.IndexerAccess, isSlice: false);
                                        var output = e.MakeResultTemp();
                                        var outputSlot = getOrMakeAndRegisterDagTempSlot(output);
                                        Debug.Assert(outputSlot > 0);
                                        TrackNullableStateForAssignment(valueOpt: null, type, outputSlot, type.ToTypeWithState());
                                        break;
                                    }
                                case BoundDagSliceEvaluation e:
                                    {
                                        // tDest = tSource[range]
                                        TypeWithAnnotations type = getIndexerOutputType(inputType, e.IndexerAccess, isSlice: true);
                                        var output = e.MakeResultTemp();
                                        var outputSlot = getOrMakeAndRegisterDagTempSlot(output);
                                        Debug.Assert(outputSlot > 0);
                                        SetState(ref this.State, outputSlot, NullableFlowState.NotNull); // Slice value is assumed to be never null
                                        break;
                                    }
                                case BoundDagAssignmentEvaluation e:
                                    {
                                        int outputSlot = getOrMakeAndRegisterDagTempSlot(e.Target);
                                        if (outputSlot > 0)
                                        {
                                            var inputState = GetState(ref this.State, inputSlot);
                                            var inputTypeWithState = TypeWithState.Create(inputType, inputState);
                                            TrackNullableStateForAssignment(valueOpt: null, inputTypeWithState.ToTypeWithAnnotations(compilation), outputSlot, inputTypeWithState, inputSlot);
                                        }

                                        break;
                                    }
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(p.Evaluation.Kind);
                            }
                            gotoNodeWithCurrentState(p.Next, nodeBelievedReachable);
                            break;
                        }
                    case BoundTestDecisionDagNode p:
                        {
                            var test = p.Test;
                            bool foundTemp = tempMap.TryGetValue(test.Input, out var slotAndType);
                            Debug.Assert(foundTemp);

                            (int inputSlot, TypeSymbol inputType) = slotAndType;
                            Split();
                            switch (test)
                            {
                                case BoundDagTypeTest:
                                    if (inputSlot > 0)
                                    {
                                        learnFromNonNullTest(inputSlot, ref this.StateWhenTrue);
                                    }
                                    gotoNode(p.WhenTrue, this.StateWhenTrue, nodeBelievedReachable);
                                    gotoNode(p.WhenFalse, this.StateWhenFalse, nodeBelievedReachable);
                                    break;
                                case BoundDagNonNullTest t:
                                    var inputMaybeNull = GetState(ref this.StateWhenTrue, inputSlot).MayBeNull();

                                    if (inputSlot > 0)
                                    {
                                        MarkDependentSlotsNotNull(inputSlot, inputType, ref this.StateWhenFalse);
                                        if (t.IsExplicitTest)
                                        {
                                            LearnFromNullTest(inputSlot, inputType, ref this.StateWhenFalse, markDependentSlotsNotNull: false);
                                        }
                                        learnFromNonNullTest(inputSlot, ref this.StateWhenTrue);
                                    }
                                    gotoNode(p.WhenTrue, this.StateWhenTrue, nodeBelievedReachable);
                                    gotoNode(p.WhenFalse, this.StateWhenFalse, nodeBelievedReachable & inputMaybeNull);
                                    break;
                                case BoundDagExplicitNullTest _:
                                    if (inputSlot > 0)
                                    {
                                        LearnFromNullTest(inputSlot, inputType, ref this.StateWhenTrue, markDependentSlotsNotNull: true);
                                        learnFromNonNullTest(inputSlot, ref this.StateWhenFalse);
                                    }
                                    gotoNode(p.WhenTrue, this.StateWhenTrue, nodeBelievedReachable);
                                    gotoNode(p.WhenFalse, this.StateWhenFalse, nodeBelievedReachable);
                                    break;
                                case BoundDagValueTest t:
                                    Debug.Assert(t.Value != ConstantValue.Null);
                                    // When we compare `bool?` inputs to bool constants, we follow a graph roughly like the following:
                                    // [0]: t0 != null ? [1] : [5]
                                    // [1]: t1 = (bool)t0; [2]
                                    // [2] (this node): t1 == boolConstant ? [3] : [4]
                                    // ...(remaining states)
                                    if (stateWhenNotNullOpt is { } stateWhenNotNull
                                        && t.Input.Source is BoundDagTypeEvaluation { Input: { IsOriginalInput: true } })
                                    {
                                        SetPossiblyConditionalState(stateWhenNotNull);
                                        Split();
                                    }
                                    else if (inputSlot > 0)
                                    {
                                        learnFromNonNullTest(inputSlot, ref this.StateWhenTrue);
                                    }
                                    bool isFalseTest = t.Value == ConstantValue.False;
                                    gotoNode(p.WhenTrue, isFalseTest ? this.StateWhenFalse : this.StateWhenTrue, nodeBelievedReachable);
                                    gotoNode(p.WhenFalse, isFalseTest ? this.StateWhenTrue : this.StateWhenFalse, nodeBelievedReachable);
                                    break;
                                case BoundDagRelationalTest _:
                                    if (inputSlot > 0)
                                    {
                                        learnFromNonNullTest(inputSlot, ref this.StateWhenTrue);
                                    }
                                    gotoNode(p.WhenTrue, this.StateWhenTrue, nodeBelievedReachable);
                                    gotoNode(p.WhenFalse, this.StateWhenFalse, nodeBelievedReachable);
                                    break;
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(test.Kind);
                            }
                            break;
                        }
                    case BoundLeafDecisionDagNode d:
                        // We have one leaf decision dag node per reachable label
                        Unsplit(); // Could be split in pathological cases like `false switch { ... }`
                        labelStateMap.Add(d.Label, (this.State, nodeBelievedReachable));
                        break;
                    case BoundWhenDecisionDagNode w:
                        // bind the pattern variables, inferring their types as well
                        Unsplit();
                        foreach (var binding in w.Bindings)
                        {
                            var variableAccess = binding.VariableAccess;
                            var tempSource = binding.TempContainingValue;
                            var foundTemp = tempMap.TryGetValue(tempSource, out var tempSlotAndType);
                            if (foundTemp) // in erroneous programs, we might not have seen a temp defined.
                            {
                                var (tempSlot, tempType) = tempSlotAndType;
                                var tempState = GetState(ref this.State, tempSlot);
                                if (variableAccess is BoundLocal { LocalSymbol: SourceLocalSymbol local } boundLocal)
                                {
                                    var value = TypeWithState.Create(tempType, tempState);
                                    var inferredType = value.ToTypeWithAnnotations(compilation, asAnnotatedType: boundLocal.DeclarationKind == BoundLocalDeclarationKind.WithInferredType);
                                    if (_variables.TryGetType(local, out var existingType))
                                    {
                                        // merge inferred nullable annotation from different branches of the decision tree
                                        inferredType = TypeWithAnnotations.Create(inferredType.Type, existingType.NullableAnnotation.Join(inferredType.NullableAnnotation));
                                    }
                                    _variables.SetType(local, inferredType);

                                    int localSlot = GetOrCreateSlot(local, forceSlotEvenIfEmpty: true);
                                    if (localSlot > 0)
                                    {
                                        TrackNullableStateForAssignment(valueOpt: null, inferredType, localSlot, TypeWithState.Create(tempType, tempState), tempSlot);
                                    }
                                }
                                else
                                {
                                    // https://github.com/dotnet/roslyn/issues/34144 perform inference for top-level var-declared fields in scripts
                                }
                            }
                        }

                        if (w.WhenExpression != null && w.WhenExpression.ConstantValueOpt != ConstantValue.True)
                        {
                            VisitCondition(w.WhenExpression);
                            Debug.Assert(this.IsConditionalState);
                            gotoNode(w.WhenTrue, this.StateWhenTrue, nodeBelievedReachable);
                            gotoNode(w.WhenFalse, this.StateWhenFalse, nodeBelievedReachable);
                        }
                        else
                        {
                            Debug.Assert(w.WhenFalse is null);
                            gotoNode(w.WhenTrue, this.State, nodeBelievedReachable);
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(dagNode.Kind);
                }
            }

            SetUnreachable(); // the decision dag is always complete (no fall-through)
            originalInputMap.Free();
            tempMap.Free();
            reinferredPropertyMap.Free();
            nodeStateMap.Free();
            return labelStateMap;

            int getOrMakeAndRegisterDagTempSlot(BoundDagTemp output)
            {
                if (tempMap.TryGetValue(output, out var targetSlotAndType))
                {
                    return targetSlotAndType.slot;
                }

                var evaluation = output.Source;
                getOrMakeAndRegisterDagTempSlot(evaluation.Input);
                (int inputSlot, TypeSymbol inputType) = tempMap.TryGetValue(evaluation.Input, out var slotAndType) ? slotAndType : throw ExceptionUtilities.Unreachable();
                Debug.Assert(inputSlot > 0);

                switch (evaluation)
                {
                    case BoundDagDeconstructEvaluation e:
                        {
                            // https://github.com/dotnet/roslyn/issues/34232
                            // We may need to recompute the Deconstruct method for a deconstruction if
                            // the receiver type has changed (e.g. its nested nullability).
                            var method = e.DeconstructMethod;
                            int extensionExtra = method.RequiresInstanceReceiver ? 0 : 1;
                            var parameterType = method.Parameters[output.Index + extensionExtra].TypeWithAnnotations;
                            int outputSlot = makeDagTempSlot(parameterType, output);
                            Debug.Assert(outputSlot > 0);
                            addToTempMap(output, outputSlot, parameterType.Type);
                            return outputSlot;
                        }
                    case BoundDagTypeEvaluation e:
                        {
                            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                            int outputSlot;
                            switch (_conversions.WithNullability(false).ClassifyConversionFromType(e.Input.Type, e.Type, isChecked: false, ref discardedUseSiteInfo).Kind)
                            {
                                case ConversionKind.Identity:
                                case ConversionKind.ImplicitReference:
                                    outputSlot = inputSlot;
                                    break;
                                case ConversionKind.ExplicitNullable when AreNullableAndUnderlyingTypes(inputType, e.Type, out _):
                                    outputSlot = GetNullableOfTValueSlot(inputType, inputSlot, out _, forceSlotEvenIfEmpty: true);
                                    if (outputSlot < 0)
                                        goto default;
                                    break;
                                default:
                                    outputSlot = makeDagTempSlot(TypeWithAnnotations.Create(e.Type, NullableAnnotation.NotAnnotated), output);
                                    break;
                            }

                            addToTempMap(output, outputSlot, e.Type);
                            return outputSlot;
                        }
                    case BoundDagFieldEvaluation e:
                        {
                            Debug.Assert(inputSlot > 0);
                            var field = (FieldSymbol)AsMemberOfType(inputType, e.Field);
                            var type = field.TypeWithAnnotations;
                            int outputSlot = -1;
                            var originalTupleElement = e.Input.IsOriginalInput && !originalInputElementSlots.IsDefault
                                ? field
                                : null;
                            if (originalTupleElement is not null)
                            {
                                // Re-use the slot of the element/expression if possible
                                outputSlot = originalInputElementSlots[originalTupleElement.TupleElementIndex];
                            }
                            if (outputSlot <= 0)
                            {
                                outputSlot = GetOrCreateSlot(field, inputSlot, forceSlotEvenIfEmpty: true);

                                if (originalTupleElement is not null && outputSlot > 0)
                                {
                                    // The expression in the tuple could not be assigned a slot (for example, `a?.b`),
                                    // so we had to create a slot for the tuple element instead.
                                    // We'll remember that so that we can apply any learnings to the expression.
#pragma warning disable CA1854 //Prefer a 'TryGetValue' call over a Dictionary indexer access guarded by a 'ContainsKey' check to avoid double lookup
                                    if (!originalInputMap.ContainsKey(outputSlot))
#pragma warning restore CA1854
                                    {
                                        originalInputMap.Add(outputSlot,
                                            ((BoundTupleExpression)expression).Arguments[originalTupleElement.TupleElementIndex]);
                                    }
                                    else
                                    {
                                        Debug.Assert(originalInputMap[outputSlot] == ((BoundTupleExpression)expression).Arguments[originalTupleElement.TupleElementIndex]);
                                    }
                                }
                            }
                            if (outputSlot <= 0)
                            {
                                outputSlot = makeDagTempSlot(type, output);
                            }

                            Debug.Assert(outputSlot > 0);
                            addToTempMap(output, outputSlot, type.Type);
                            return outputSlot;
                        }
                    case BoundDagPropertyEvaluation e:
                        {
                            Debug.Assert(inputSlot > 0);
                            var property = getReInferredProperty(inputType, e);
                            var type = property.TypeWithAnnotations;

                            int outputSlot = GetOrCreateSlot(property, inputSlot, forceSlotEvenIfEmpty: true);
                            if (outputSlot <= 0)
                            {
                                outputSlot = makeDagTempSlot(type, output);
                            }
                            Debug.Assert(outputSlot > 0);
                            addToTempMap(output, outputSlot, type.Type);
                            return outputSlot;
                        }
                    case BoundDagIndexEvaluation e:
                        {
                            var type = TypeWithAnnotations.Create(e.Property.Type, NullableAnnotation.Annotated);
                            int outputSlot = makeDagTempSlot(type, output);
                            Debug.Assert(outputSlot > 0);
                            addToTempMap(output, outputSlot, type.Type);
                            return outputSlot;
                        }
                    case BoundDagIndexerEvaluation e:
                        {
                            // tDest = tSource[index]
                            Debug.Assert(inputSlot > 0);
                            TypeWithAnnotations type = getIndexerOutputType(inputType, e.IndexerAccess, isSlice: false);

                            var outputSlot = makeDagTempSlot(type, output);
                            Debug.Assert(outputSlot > 0);
                            addToTempMap(output, outputSlot, type.Type);
                            return outputSlot;
                        }
                    case BoundDagSliceEvaluation e:
                        {
                            // tDest = tSource[range]
                            Debug.Assert(inputSlot > 0);
                            TypeWithAnnotations type = getIndexerOutputType(inputType, e.IndexerAccess, isSlice: true);

                            var outputSlot = makeDagTempSlot(type, output);
                            Debug.Assert(outputSlot > 0);
                            addToTempMap(output, outputSlot, type.Type);
                            return outputSlot;
                        }
                    case BoundDagAssignmentEvaluation e:
                    default:
                        throw ExceptionUtilities.UnexpectedValue(evaluation.Kind);
                }
            }

            PropertySymbol getReInferredProperty(TypeSymbol inputType, BoundDagPropertyEvaluation e)
            {
                if (reinferredPropertyMap.TryGetValue(e, out PropertySymbol property))
                {
                    return property;
                }

                property = e.Property.IsExtensionBlockMember()
                               ? ReInferAndVisitExtensionPropertyAccess(e, e.Property, new BoundExpressionWithNullability(e.Syntax, expression, NullableAnnotation.NotAnnotated, inputType)).Member
                               : (PropertySymbol)AsMemberOfType(inputType, e.Property);

                reinferredPropertyMap.Add(e, property);
                return property;
            }

            void learnFromNonNullTest(int inputSlot, ref LocalState state)
            {
                if (stateWhenNotNullOpt is { } stateWhenNotNull && inputSlot == originalInputSlot)
                {
                    state = CloneAndUnsplit(ref stateWhenNotNull);
                }
                LearnFromNonNullTest(inputSlot, ref state);
                if (originalInputMap.TryGetValue(inputSlot, out var expression))
                    LearnFromNonNullTest(expression, ref state);
            }

            void addToTempMap(BoundDagTemp output, int slot, TypeSymbol type)
            {
                // We need to track all dag temps, so there should be a slot
                Debug.Assert(slot > 0);
                if (tempMap.TryGetValue(output, out var outputSlotAndType))
                {
                    // The dag temp has already been allocated on another branch of the dag
                    Debug.Assert(outputSlotAndType.slot == slot);
                    Debug.Assert(isDerivedType(outputSlotAndType.type, type));
                }
                else
                {
                    Debug.Assert(NominalSlotType(slot) is var slotType && (slotType.IsErrorType() || isDerivedType(slotType, type)));
                    tempMap.Add(output, (slot, type));
                }
            }

            bool isDerivedType(TypeSymbol derivedType, TypeSymbol baseType)
            {
                if (derivedType.IsErrorType() || baseType.IsErrorType())
                    return true;

                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                return _conversions.WithNullability(false).ClassifyConversionFromType(derivedType, baseType, isChecked: false, ref discardedUseSiteInfo).Kind switch
                {
                    ConversionKind.Identity => true,
                    ConversionKind.ImplicitReference => true,
                    ConversionKind.Boxing => true,
                    _ => false,
                };
            }

            void gotoNodeWithCurrentState(BoundDecisionDagNode node, bool believedReachable)
            {
                if (nodeStateMap.TryGetValue(node, out var stateAndReachable))
                {
                    switch (IsConditionalState, stateAndReachable.state.IsConditionalState)
                    {
                        case (true, true):
                            Debug.Assert(false);
                            Join(ref this.StateWhenTrue, ref stateAndReachable.state.StateWhenTrue);
                            Join(ref this.StateWhenFalse, ref stateAndReachable.state.StateWhenFalse);
                            break;
                        case (true, false):
                            Debug.Assert(false);
                            Join(ref this.StateWhenTrue, ref stateAndReachable.state.State);
                            Join(ref this.StateWhenFalse, ref stateAndReachable.state.State);
                            break;
                        case (false, true):
                            Debug.Assert(false);
                            Split();
                            Join(ref this.StateWhenTrue, ref stateAndReachable.state.StateWhenTrue);
                            Join(ref this.StateWhenFalse, ref stateAndReachable.state.StateWhenFalse);
                            break;
                        case (false, false):
                            Join(ref this.State, ref stateAndReachable.state.State);
                            break;
                    }
                    believedReachable |= stateAndReachable.believedReachable;
                }

                nodeStateMap[node] = (PossiblyConditionalState.Create(this), believedReachable);
            }

            void gotoNode(BoundDecisionDagNode node, LocalState state, bool believedReachable)
            {
                PossiblyConditionalState result;
                if (nodeStateMap.TryGetValue(node, out var stateAndReachable))
                {
                    result = stateAndReachable.state;
                    switch (result.IsConditionalState)
                    {
                        case true:
                            Debug.Assert(false);
                            Join(ref result.StateWhenTrue, ref state);
                            Join(ref result.StateWhenFalse, ref state);
                            break;
                        case false:
                            Join(ref result.State, ref state);
                            break;
                    }
                    believedReachable |= stateAndReachable.believedReachable;
                }
                else
                {
                    result = new PossiblyConditionalState(state);
                }

                nodeStateMap[node] = (result, believedReachable);
            }

            int makeDagTempSlot(TypeWithAnnotations type, BoundDagTemp temp)
            {
                object slotKey = (node, temp);
                return GetOrCreatePlaceholderSlot(slotKey, type);
            }

            static TypeWithAnnotations getIndexerOutputType(TypeSymbol inputType, BoundExpression e, bool isSlice)
            {
                return e switch
                {
                    BoundIndexerAccess indexerAccess => AsMemberOfType(inputType, indexerAccess.Indexer).GetTypeOrReturnType(),
                    BoundCall call => AsMemberOfType(inputType, call.Method).GetTypeOrReturnType(),

                    BoundArrayAccess arrayAccess => isSlice
                        ? TypeWithAnnotations.Create(isNullableEnabled: true, inputType, isAnnotated: false)
                        : ((ArrayTypeSymbol)inputType).ElementTypeWithAnnotations,

                    BoundImplicitIndexerAccess implicitIndexerAccess => getIndexerOutputType(inputType, implicitIndexerAccess.IndexerOrSliceAccess, isSlice),
                    _ => throw ExceptionUtilities.UnexpectedValue(e.Kind)
                };
            }
        }

        public override BoundNode VisitConvertedSwitchExpression(BoundConvertedSwitchExpression node)
        {
            bool inferType = !node.WasTargetTyped;
            VisitSwitchExpressionCore(node, inferType);
            return null;
        }

        public override BoundNode VisitUnconvertedSwitchExpression(BoundUnconvertedSwitchExpression node)
        {
            // This method is only involved in method inference with unbound lambdas.
            VisitSwitchExpressionCore(node, inferType: true);
            return null;
        }

        private void VisitSwitchExpressionCore(BoundSwitchExpression node, bool inferType)
        {
            // first, learn from any null tests in the patterns
            int slot = GetSlotForSwitchInputValue(node.Expression);
            if (slot > 0)
            {
                var originalInputType = node.Expression.Type;
                foreach (var arm in node.SwitchArms)
                {
                    LearnFromAnyNullPatterns(slot, originalInputType, arm.Pattern);
                }
            }

            Visit(node.Expression);
            var expressionState = ResultType;
            var labelStateMap = LearnFromDecisionDag(node.Syntax, node.ReachabilityDecisionDag, node.Expression, expressionState, stateWhenNotNullOpt: null);
            var endState = UnreachableState();

            if (!node.ReportedNotExhaustive && node.DefaultLabel != null &&
                labelStateMap.TryGetValue(node.DefaultLabel, out var defaultLabelState) &&
                defaultLabelState.believedReachable)
            {
                SetState(defaultLabelState.state);
                var nodes = node.ReachabilityDecisionDag.TopologicallySortedNodes;
                var leaf = nodes.Where(n => n is BoundLeafDecisionDagNode leaf && leaf.Label == node.DefaultLabel).First();
                var samplePattern = PatternExplainer.SamplePatternForPathToDagNode(
                    BoundDagTemp.ForOriginalInput(node.Expression), nodes, leaf, nullPaths: true, out bool requiresFalseWhenClause, out _);
                ErrorCode warningCode = requiresFalseWhenClause ? ErrorCode.WRN_SwitchExpressionNotExhaustiveForNullWithWhen : ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull;
                ReportDiagnostic(
                    warningCode,
                    ((SwitchExpressionSyntax)node.Syntax).SwitchKeyword.GetLocation(),
                    samplePattern);
            }

            // collect expressions, conversions and result types
            int numSwitchArms = node.SwitchArms.Length;
            var conversions = ArrayBuilder<Conversion>.GetInstance(numSwitchArms);
            var resultTypes = ArrayBuilder<TypeWithState>.GetInstance(numSwitchArms);
            var expressions = ArrayBuilder<BoundExpression>.GetInstance(numSwitchArms);
            var placeholderBuilder = ArrayBuilder<BoundExpression>.GetInstance(numSwitchArms);

            foreach (var arm in node.SwitchArms)
            {
                SetState(getStateForArm(arm, labelStateMap));
                // https://github.com/dotnet/roslyn/issues/35836 Is this where we want to take the snapshot?
                TakeIncrementalSnapshot(arm);
                VisitForRewriting(arm.Pattern);

                if (!State.Reachable && arm.WhenClause != null)
                {
                    // Unreachable when clauses are not visited in `LearnFromDecisionDag`.
                    VisitForRewriting(arm.WhenClause);
                }

                (BoundExpression expression, Conversion conversion) = RemoveConversion(arm.Value, includeExplicitConversions: false);
                SnapshotWalkerThroughConversionGroup(arm.Value, expression);
                expressions.Add(expression);
                conversions.Add(conversion);
                var armType = VisitRvalueWithState(expression);
                resultTypes.Add(armType);
                Join(ref endState, ref this.State);

                if (!IsTargetTypedExpression(expression))
                {
                    // Build placeholders for inference in order to preserve annotations.
                    placeholderBuilder.Add(CreatePlaceholderIfNecessary(expression, armType.ToTypeWithAnnotations(compilation)));
                }
            }

            SetState(endState);

            var placeholders = placeholderBuilder.ToImmutableAndFree();
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

            TypeSymbol inferredType =
                (inferType ? BestTypeInferrer.InferBestType(placeholders, _conversions, ref discardedUseSiteInfo, out _) : null)
                    ?? node.Type?.SetUnknownNullabilityForReferenceTypes();

            var inferredTypeWithAnnotations = TypeWithAnnotations.Create(inferredType);
            NullableFlowState inferredState;
            TypeWithState resultType;

            if (inferType && inferredType is null)
            {
                // This can happen when we're inferring the return type of a lambda or visiting a node without diagnostics like
                // BoundConvertedTupleLiteral.SourceTuple. For these cases, we don't need to do any work,
                // the unconverted switch expression can't contribute info. The conversion that should be on top of this
                // can add or remove nullability, and nested nodes aren't being publicly exposed by the semantic model.
                // See also NullableWalker.VisitConditionalOperatorCore for a similar check for conditional operators.
                Debug.Assert((node is BoundUnconvertedSwitchExpression && (_returnTypesOpt is not null || _disableDiagnostics))
                                || node is BoundSwitchExpression { SwitchArms: { Length: 0 } });
                inferredState = default;

                resultType = TypeWithState.Create(inferredType, inferredState);

                conversions.Free();
                resultTypes.Free();
                expressions.Free();
                labelStateMap.Free();
                SetResult(node, resultType, inferredTypeWithAnnotations);
                return;
            }

            resultType = convertArms(node, labelStateMap, conversions, resultTypes, expressions, inferredTypeWithAnnotations, isTargetTyped: !inferType);

            SetResult(node, resultType, inferredTypeWithAnnotations, updateAnalyzedNullability: false);
            return;

            TypeWithState convertArms(
                BoundSwitchExpression node,
                PooledDictionary<LabelSymbol, (LocalState state, bool believedReachable)> labelStateMap,
                ArrayBuilder<Conversion> conversions,
                ArrayBuilder<TypeWithState> resultTypes,
                ArrayBuilder<BoundExpression> expressions,
                TypeWithAnnotations inferredTypeWithAnnotations,
                bool isTargetTyped)
            {
                if (!isTargetTyped)
                {
                    int numSwitchArms = node.SwitchArms.Length;
                    for (int i = 0; i < numSwitchArms; i++)
                    {
                        var nodeForSyntax = expressions[i];
                        var arm = node.SwitchArms[i];
                        var armState = getStateForArm(arm, labelStateMap);
                        resultTypes[i] = ConvertConditionalOperandOrSwitchExpressionArmResult(arm.Value, nodeForSyntax, conversions[i], inferredTypeWithAnnotations, resultTypes[i], armState, armState.Reachable);
                    }
                }

                NullableFlowState inferredState = BestTypeInferrer.GetNullableState(resultTypes);

                if (!isTargetTyped)
                {
                    conversions.Free();
                    resultTypes.Free();
                    expressions.Free();
                    labelStateMap.Free();
                }
                else
                {
                    addConvertArmsAsCompletion(node, labelStateMap, conversions, resultTypes, expressions);
                }

                TypeWithState resultType = TypeWithState.Create(inferredTypeWithAnnotations.Type, inferredState);

                if (!isTargetTyped)
                {
                    SetAnalyzedNullability(node, resultType);
                }

                return resultType;
            }

            void addConvertArmsAsCompletion(
                BoundSwitchExpression node,
                PooledDictionary<LabelSymbol, (LocalState state, bool believedReachable)> labelStateMap,
                ArrayBuilder<Conversion> conversions,
                ArrayBuilder<TypeWithState> resultTypes,
                ArrayBuilder<BoundExpression> expressions)
            {
                TargetTypedAnalysisCompletion[node] =
                    (TypeWithAnnotations inferredTypeWithAnnotations) =>
                    {
                        return convertArms(node, labelStateMap, conversions, resultTypes, expressions, inferredTypeWithAnnotations, isTargetTyped: false);
                    };
            }

            LocalState getStateForArm(BoundSwitchExpressionArm arm, PooledDictionary<LabelSymbol, (LocalState state, bool believedReachable)> labelStateMap)
                => !arm.Pattern.HasErrors && labelStateMap.TryGetValue(arm.Label, out var labelState) ? labelState.state : UnreachableState();
        }

        private int GetSlotForSwitchInputValue(BoundExpression node)
        {
            return node.IsSuppressed ? GetOrCreatePlaceholderSlot(node) : MakeSlot(node);
        }

        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            Debug.Assert(!IsConditionalState);
            LearnFromAnyNullPatterns(node.Expression, node.Pattern);
            VisitForRewriting(node.Pattern);
            var hasStateWhenNotNull = VisitPossibleConditionalAccess(node.Expression, out var conditionalStateWhenNotNull);
            var expressionState = ResultType;
            var labelStateMap = LearnFromDecisionDag(node.Syntax, node.ReachabilityDecisionDag, node.Expression, expressionState, hasStateWhenNotNull ? conditionalStateWhenNotNull : null);
            var trueState = labelStateMap.TryGetValue(node.IsNegated ? node.WhenFalseLabel : node.WhenTrueLabel, out var s1) ? s1.state : UnreachableState();
            var falseState = labelStateMap.TryGetValue(node.IsNegated ? node.WhenTrueLabel : node.WhenFalseLabel, out var s2) ? s2.state : UnreachableState();
            labelStateMap.Free();
            SetConditionalState(trueState, falseState);
            SetNotNullResult(node);
            return null;
        }
    }
}
