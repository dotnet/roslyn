// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <returns>true if there is a top-level explicit null check</returns>
        private void LearnFromAnyNullPatterns(
            BoundExpression expression,
            BoundPattern pattern)
        {
            int slot = MakeSlot(expression);
            LearnFromAnyNullPatterns(slot, expression.Type, pattern);
        }

        private void VisitPatternForRewriting(BoundPattern pattern)
        {
            // Don't let anything under the pattern actually affect current state,
            // as we're only visiting for nullable information.
            Debug.Assert(!IsConditionalState);
            var currentState = State;
            VisitWithoutDiagnostics(pattern);
            SetState(currentState);
        }

        public override BoundNode VisitSubpattern(BoundSubpattern node)
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
            Visit(node.Value);
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
        /// <returns>true if there is a top-level explicit null check</returns>
        private void LearnFromAnyNullPatterns(
            int inputSlot,
            TypeSymbol inputType,
            BoundPattern pattern)
        {
            if (inputSlot <= 0)
                return;

            // https://github.com/dotnet/roslyn/issues/35041 We only need to do this when we're rewriting, so we
            // can get information for any nodes in the pattern.
            VisitPatternForRewriting(pattern);

            switch (pattern)
            {
                case BoundConstantPattern cp:
                    bool isExplicitNullCheck = cp.Value.ConstantValue == ConstantValue.Null;
                    if (isExplicitNullCheck)
                    {
                        LearnFromNullTest(inputSlot, inputType, ref this.State);
                    }
                    break;
                case BoundDeclarationPattern _:
                case BoundDiscardPattern _:
                case BoundITuplePattern _:
                    break; // nothing to learn
                case BoundRecursivePattern rp:
                    {
                        // for positional part: we only learn from tuples (not Deconstruct)
                        if (rp is
                        {
                            DeconstructMethod: null,
                            Deconstruction: { IsDefault: false }
                        })
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
                            for (int i = 0, n = rp.Properties.Length; i < n; i++)
                            {
                                BoundSubpattern item = rp.Properties[i];
                                Symbol symbol = item.Symbol;
                                if (symbol?.ContainingType.Equals(inputType, TypeCompareKind.AllIgnoreOptions) == true)
                                {
                                    LearnFromAnyNullPatterns(GetOrCreateSlot(symbol, inputSlot), symbol.GetTypeOrReturnType().Type, item.Pattern);
                                }
                            }
                        }
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern);
            }
        }

        protected override LocalState VisitSwitchStatementDispatch(BoundSwitchStatement node)
        {
            // first, learn from any null tests in the patterns
            int slot = MakeSlot(node.Expression);
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

            // visit switch header
            var expressionState = VisitRvalueWithState(node.Expression);
            LocalState initialState = this.State.Clone();

            DeclareLocals(node.InnerLocals);
            foreach (var section in node.SwitchSections)
            {
                // locals can be alive across jumps in the switch sections, so we declare them early.
                DeclareLocals(section.Locals);
            }

            var labelStateMap = LearnFromDecisionDag(node.Syntax, node.DecisionDag, node.Expression, expressionState, ref initialState);
            foreach (var section in node.SwitchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    var labelResult = labelStateMap.TryGetValue(label.Label, out var s1) ? s1 : (state: UnreachableState(), believedReachable: false);
                    SetState(labelResult.state);
                    PendingBranches.Add(new PendingBranch(label, this.State, label.Label));
                }
            }

            labelStateMap.Free();
            return initialState;
        }

        protected override void VisitSwitchSection(BoundSwitchSection node, bool isLastSection)
        {
            TakeIncrementalSnapshot(node);
            SetState(UnreachableState());
            foreach (var label in node.SwitchLabels)
            {
                TakeIncrementalSnapshot(label);
                VisitPatternForRewriting(label.Pattern);
                VisitLabel(label.Label, node);
            }

            VisitStatementList(node);
        }

        private PooledDictionary<LabelSymbol, (LocalState state, bool believedReachable)>
            LearnFromDecisionDag(
            SyntaxNode node,
            BoundDecisionDag decisionDag,
            BoundExpression expression,
            TypeWithState expressionType,
            ref LocalState initialState)
        {
            // We reuse the slot at the beginning of a switch (or is-pattern expression), pretending that we are
            // not copying the input to evaluate the patterns.  In this way we infer non-nullability of the original
            // variable's parts based on matched pattern parts.  Mutations in `when` clauses can show the inaccuracy
            // of analysis based on this choice.
            var rootTemp = BoundDagTemp.ForOriginalInput(expression);
            int originalInputSlot = MakeSlot(expression);
            if (originalInputSlot <= 0)
            {
                originalInputSlot = makeDagTempSlot(expressionType.ToTypeWithAnnotations(), rootTemp);
                initialState[originalInputSlot] = expressionType.State;
            }

            var tempMap = PooledDictionary<BoundDagTemp, (int slot, TypeSymbol type)>.GetInstance();
            Debug.Assert(originalInputSlot > 0);
            tempMap.Add(rootTemp, (originalInputSlot, expressionType.Type));

            var nodeStateMap = PooledDictionary<BoundDecisionDagNode, (LocalState state, bool believedReachable)>.GetInstance();
            nodeStateMap.Add(decisionDag.RootNode, (state: initialState.Clone(), believedReachable: true));

            var labelStateMap = PooledDictionary<LabelSymbol, (LocalState state, bool believedReachable)>.GetInstance();

            foreach (var dagNode in decisionDag.TopologicallySortedNodes)
            {
                bool found = nodeStateMap.TryGetValue(dagNode, out var nodeStateAndBelievedReachable);
                Debug.Assert(found); // the topologically sorted nodes should contain only reachable nodes
                (LocalState nodeState, bool nodeBelievedReachable) = nodeStateAndBelievedReachable;
                SetState(nodeState);

                switch (dagNode)
                {
                    case BoundEvaluationDecisionDagNode p:
                        {
                            var evaluation = p.Evaluation;
                            (int inputSlot, TypeSymbol inputType) = tempMap.TryGetValue(evaluation.Input, out var slotAndType) ? slotAndType : throw ExceptionUtilities.Unreachable;
                            Debug.Assert(inputSlot > 0);
                            var inputState = this.State[inputSlot];

                            switch (evaluation)
                            {
                                case BoundDagDeconstructEvaluation e:
                                    {
                                        // https://github.com/dotnet/roslyn/issues/34232
                                        // We may need to recompute the Deconstruct method for a deconstruction if
                                        // the receiver type has changed (e.g. its nested nullability).
                                        var method = e.DeconstructMethod;
                                        int extensionExtra = method.RequiresInstanceReceiver ? 0 : 1;
                                        for (int i = 0; i < method.ParameterCount - extensionExtra; i++)
                                        {
                                            var parameterType = method.Parameters[i + extensionExtra].TypeWithAnnotations;
                                            var output = new BoundDagTemp(e.Syntax, parameterType.Type, e, i);
                                            int outputSlot = makeDagTempSlot(parameterType, output);
                                            Debug.Assert(outputSlot > 0);
                                            addToTempMap(output, outputSlot, parameterType.Type);
                                        }
                                        break;
                                    }
                                case BoundDagTypeEvaluation e:
                                    {
                                        var output = new BoundDagTemp(e.Syntax, e.Type, e);
                                        HashSet<DiagnosticInfo> discardedDiagnostics = null;
                                        int outputSlot;
                                        switch (_conversions.WithNullability(false).ClassifyConversionFromType(inputType, e.Type, ref discardedDiagnostics).Kind)
                                        {
                                            case ConversionKind.Identity:
                                            case ConversionKind.ImplicitReference:
                                            case ConversionKind.NoConversion:
                                            case ConversionKind.ExplicitReference:
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
                                        State[outputSlot] = NullableFlowState.NotNull;
                                        addToTempMap(output, outputSlot, e.Type);
                                        break;
                                    }
                                case BoundDagFieldEvaluation e:
                                    {
                                        Debug.Assert(inputSlot > 0);
                                        var field = (FieldSymbol)AsMemberOfType(inputType, e.Field);
                                        int outputSlot = GetOrCreateSlot(field, inputSlot, forceSlotEvenIfEmpty: true);
                                        Debug.Assert(outputSlot > 0);
                                        var type = field.Type;
                                        var output = new BoundDagTemp(e.Syntax, type, e);
                                        addToTempMap(output, outputSlot, type);
                                        break;
                                    }
                                case BoundDagPropertyEvaluation e:
                                    {
                                        Debug.Assert(inputSlot > 0);
                                        var property = (PropertySymbol)AsMemberOfType(inputType, e.Property);
                                        var type = property.TypeWithAnnotations;
                                        var output = new BoundDagTemp(e.Syntax, type.Type, e);
                                        int outputSlot = GetOrCreateSlot(property, inputSlot, forceSlotEvenIfEmpty: true);
                                        if (outputSlot <= 0)
                                        {
                                            // This is needed due to https://github.com/dotnet/roslyn/issues/29619
                                            outputSlot = makeDagTempSlot(type, output);
                                        }
                                        Debug.Assert(outputSlot > 0);
                                        addToTempMap(output, outputSlot, type.Type);
                                        break;
                                    }
                                case BoundDagIndexEvaluation e:
                                    {
                                        var type = TypeWithAnnotations.Create(e.Property.Type, NullableAnnotation.Annotated);
                                        var output = new BoundDagTemp(e.Syntax, type.Type, e);
                                        int outputSlot = makeDagTempSlot(type, output);
                                        Debug.Assert(outputSlot > 0);
                                        addToTempMap(output, outputSlot, type.Type);
                                        break;
                                    }
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(p.Evaluation.Kind);
                            }
                            gotoNode(p.Next, this.State, nodeBelievedReachable);
                            break;
                        }
                    case BoundTestDecisionDagNode p:
                        {
                            var test = p.Test;
                            bool foundTemp = tempMap.TryGetValue(test.Input, out var slotAndType);
                            Debug.Assert(foundTemp);

                            (int inputSlot, TypeSymbol inputType) = slotAndType;
                            var inputState = this.State[inputSlot];
                            Split();
                            switch (test)
                            {
                                case BoundDagTypeTest t:
                                    if (inputSlot > 0)
                                    {
                                        learnFromNonNullTest(inputSlot, ref this.StateWhenTrue);
                                    }
                                    gotoNode(p.WhenTrue, this.StateWhenTrue, nodeBelievedReachable);
                                    gotoNode(p.WhenFalse, this.StateWhenFalse, nodeBelievedReachable);
                                    break;
                                case BoundDagNonNullTest t:
                                    if (inputSlot > 0)
                                    {
                                        learnFromNonNullTest(inputSlot, ref this.StateWhenTrue);
                                    }
                                    gotoNode(p.WhenTrue, this.StateWhenTrue, nodeBelievedReachable);
                                    gotoNode(p.WhenFalse, this.StateWhenFalse, nodeBelievedReachable & inputState.MayBeNull());
                                    break;
                                case BoundDagExplicitNullTest t:
                                    if (inputSlot > 0)
                                    {
                                        LearnFromNullTest(inputSlot, inputType, ref this.StateWhenTrue);
                                        learnFromNonNullTest(inputSlot, ref this.StateWhenFalse);
                                    }
                                    gotoNode(p.WhenTrue, this.StateWhenTrue, nodeBelievedReachable);
                                    gotoNode(p.WhenFalse, this.StateWhenFalse, nodeBelievedReachable);
                                    break;
                                case BoundDagValueTest t:
                                    Debug.Assert(t.Value != ConstantValue.Null);
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
                        labelStateMap.Add(d.Label, (this.State, nodeBelievedReachable));
                        break;
                    case BoundWhenDecisionDagNode w:
                        // bind the pattern variables, inferring their types as well
                        foreach (var binding in w.Bindings)
                        {
                            var variableAccess = binding.VariableAccess;
                            var tempSource = binding.TempContainingValue;
                            var foundTemp = tempMap.TryGetValue(tempSource, out var tempSlotAndType);
                            Debug.Assert(foundTemp);
                            var (tempSlot, tempType) = tempSlotAndType;
                            var tempState = this.State[tempSlot];
                            if (variableAccess is BoundLocal { LocalSymbol: SourceLocalSymbol local })
                            {
                                var inferredType = TypeWithState.Create(tempType, tempState).ToTypeWithAnnotations();
                                if (_variableTypes.TryGetValue(local, out var existingType))
                                {
                                    // merge inferred nullable annotation from different branches of the decision tree
                                    _variableTypes[local] = TypeWithAnnotations.Create(existingType.Type, existingType.NullableAnnotation.Join(inferredType.NullableAnnotation));
                                }
                                else
                                {
                                    _variableTypes[local] = inferredType;
                                }

                                int localSlot = GetOrCreateSlot(local, forceSlotEvenIfEmpty: true);
                                this.State[localSlot] = tempState;
                            }
                            else
                            {
                                // https://github.com/dotnet/roslyn/issues/34144 perform inference for top-level var-declared fields in scripts
                            }
                        }

                        if (w.WhenExpression != null && w.WhenExpression.ConstantValue != ConstantValue.True)
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
            tempMap.Free();
            nodeStateMap.Free();
            return labelStateMap;

            void learnFromNonNullTest(int inputSlot, ref LocalState state)
            {
                LearnFromNonNullTest(inputSlot, ref state);
                if (inputSlot == originalInputSlot)
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
                    Debug.Assert(outputSlotAndType.type.Equals(type, TypeCompareKind.AllIgnoreOptions));
                }
                else
                {
                    tempMap.Add(output, (slot, type));
                }
            }

            void gotoNode(BoundDecisionDagNode node, LocalState state, bool believedReachable)
            {
                if (nodeStateMap.TryGetValue(node, out var stateAndReachable))
                {
                    Join(ref state, ref stateAndReachable.state);
                    believedReachable |= stateAndReachable.believedReachable;
                }

                nodeStateMap[node] = (state, believedReachable);
            }

            int makeDagTempSlot(TypeWithAnnotations type, BoundDagTemp temp)
            {
                object slotKey = (node, temp);
                return GetOrCreatePlaceholderSlot(slotKey, type);
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
            VisitSwitchExpressionCore(node, inferType: true);
            return null;
        }

        private void VisitSwitchExpressionCore(BoundSwitchExpression node, bool inferType)
        {
            // first, learn from any null tests in the patterns
            int slot = MakeSlot(node.Expression);
            if (slot > 0)
            {
                var originalInputType = node.Expression.Type;
                foreach (var arm in node.SwitchArms)
                {
                    LearnFromAnyNullPatterns(slot, originalInputType, arm.Pattern);
                }
            }

            var expressionState = VisitRvalueWithState(node.Expression);
            var labelStateMap = LearnFromDecisionDag(node.Syntax, node.DecisionDag, node.Expression, expressionState, ref this.State);
            var endState = UnreachableState();

            if (!node.ReportedNotExhaustive && node.DefaultLabel != null &&
                labelStateMap.TryGetValue(node.DefaultLabel, out var defaultLabelState) && defaultLabelState.believedReachable)
            {
                SetState(defaultLabelState.state);
                ReportDiagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, ((SwitchExpressionSyntax)node.Syntax).SwitchKeyword.GetLocation());
            }

            // collect expressions, conversions and result types
            int numSwitchArms = node.SwitchArms.Length;
            var conversions = ArrayBuilder<Conversion>.GetInstance(numSwitchArms);
            var resultTypes = ArrayBuilder<TypeWithState>.GetInstance(numSwitchArms);
            var expressions = ArrayBuilder<BoundExpression>.GetInstance(numSwitchArms);
            var placeholderBuilder = ArrayBuilder<BoundExpression>.GetInstance(numSwitchArms);

            foreach (var arm in node.SwitchArms)
            {
                SetState(!arm.Pattern.HasErrors && labelStateMap.TryGetValue(arm.Label, out var labelState) ? labelState.state : UnreachableState());
                // https://github.com/dotnet/roslyn/issues/35836 Is this where we want to take the snapshot?
                TakeIncrementalSnapshot(arm);
                VisitPatternForRewriting(arm.Pattern);
                (BoundExpression expression, Conversion conversion) = RemoveConversion(arm.Value, includeExplicitConversions: false);
                SnapshotWalkerThroughConversionGroup(arm.Value, expression);
                expressions.Add(expression);
                conversions.Add(conversion);
                var armType = VisitRvalueWithState(expression);
                resultTypes.Add(armType);
                Join(ref endState, ref this.State);

                // Build placeholders for inference in order to preserve annotations.
                placeholderBuilder.Add(CreatePlaceholderIfNecessary(expression, armType.ToTypeWithAnnotations()));
            }

            var placeholders = placeholderBuilder.ToImmutableAndFree();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            TypeSymbol inferredType =
                (inferType ? BestTypeInferrer.InferBestType(placeholders, _conversions, ref useSiteDiagnostics) : null)
                    ?? node.Type?.SetUnknownNullabilityForReferenceTypes()
                    ?? new ExtendedErrorTypeSymbol(this.compilation, "", arity: 0, errorInfo: null, unreported: false);

            var inferredTypeWithAnnotations = TypeWithAnnotations.Create(inferredType);

            // Convert elements to best type to determine element top-level nullability and to report nested nullability warnings
            for (int i = 0; i < numSwitchArms; i++)
            {
                var expression = expressions[i];
                resultTypes[i] = VisitConversion(conversionOpt: null, expression, conversions[i], inferredTypeWithAnnotations, resultTypes[i], checkConversion: true,
                    fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Assignment, reportRemainingWarnings: true, reportTopLevelWarnings: false);
            }

            var inferredState = BestTypeInferrer.GetNullableState(resultTypes);
            var resultType = TypeWithState.Create(inferredType, inferredState);
            inferredTypeWithAnnotations = resultType.ToTypeWithAnnotations();

            for (int i = 0; i < numSwitchArms; i++)
            {
                var nodeForSyntax = expressions[i];
                var conversionOpt = node.SwitchArms[i].Value switch { BoundConversion c when c != nodeForSyntax => c, _ => null };
                // Report top-level warnings
                _ = VisitConversion(conversionOpt, conversionOperand: nodeForSyntax, conversions[i], targetTypeWithNullability: inferredTypeWithAnnotations, operandType: resultTypes[i],
                    checkConversion: true, fromExplicitCast: false, useLegacyWarnings: false, AssignmentKind.Assignment, reportRemainingWarnings: false, reportTopLevelWarnings: true);
            }

            conversions.Free();
            resultTypes.Free();
            expressions.Free();
            labelStateMap.Free();
            SetState(endState);
            SetResult(node, resultType, inferredTypeWithAnnotations);
        }

        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            Debug.Assert(!IsConditionalState);
            LearnFromAnyNullPatterns(node.Expression, node.Pattern);
            VisitPatternForRewriting(node.Pattern);
            var expressionState = VisitRvalueWithState(node.Expression);
            var labelStateMap = LearnFromDecisionDag(node.Syntax, node.DecisionDag, node.Expression, expressionState, ref this.State);
            var trueState = labelStateMap.TryGetValue(node.WhenTrueLabel, out var s1) ? s1.state : UnreachableState();
            var falseState = labelStateMap.TryGetValue(node.WhenFalseLabel, out var s2) ? s2.state : UnreachableState();
            labelStateMap.Free();
            SetConditionalState(trueState, falseState);
            SetNotNullResult(node);
            return null;
        }
    }
}
