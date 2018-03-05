// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        /// <summary>
        /// A common base class for lowering the pattern switch statement and the pattern switch expression.
        /// </summary>
        private class BasePatternSwitchLocalRewriter : PatternLocalRewriter
        {
            /// <summary>
            /// Map from switch section's syntax to the lowered code for the section. The code for a section
            /// includes the code to assign to the pattern variables and evaluate the when clause. Since a
            /// when clause can yield a false value, it can jump back to a label in the lowered decision dag.
            /// </summary>
            protected readonly Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchArms = new Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>>();

            /// <summary>
            /// In a switch expression, some labels may first reached by a backward branch, and
            /// it may occur when something (from the enclosing expression) is on the stack.
            /// To satisfy the verifier, the caller must arrange forward jumps to these labels. The set of
            /// labels that will need such forward jumps (if something can be on the stack) is stored in
            /// _backwardLabels. In practice, this is exclusively the set of states that are reached
            /// when a when-clause evaluates to false.
            /// PROTOTYPE(patterns2): This is a placeholder. It is not used yet for lowering the
            /// switch expression, where it will be needed.
            /// </summary>
            private readonly ArrayBuilder<BoundDecisionDag> _backwardLabels = ArrayBuilder<BoundDecisionDag>.GetInstance();

            /// <summary>
            /// The lowered decision dag. This includes all of the code to decide which pattern
            /// is matched, but not the code to assign to pattern variables and evaluate when clauses.
            /// </summary>
            protected readonly ArrayBuilder<BoundStatement> _loweredDecisionDag = ArrayBuilder<BoundStatement>.GetInstance();

            /// <summary>
            /// The label in the code for the beginning of code for each node of the dag.
            /// </summary>
            private readonly Dictionary<BoundDecisionDag, LabelSymbol> _dagNodeLabels = new Dictionary<BoundDecisionDag, LabelSymbol>();

            /// <summary>
            /// This set contains a dag once we have produced a label for its entry point.
            /// </summary>
            private readonly HashSet<BoundDecisionDag> _generated = new HashSet<BoundDecisionDag>();

            protected BasePatternSwitchLocalRewriter(LocalRewriter localRewriter, BoundExpression loweredInput, ImmutableArray<SyntaxNode> arms, BoundDecisionDag decisionDag)
                : base(localRewriter, loweredInput)
            {
                foreach (var arm in arms)
                {
                    _switchArms.Add(arm, new ArrayBuilder<BoundStatement>());
                }

                ImmutableArray<BoundDecisionDag> sortedNodes = 
                    TopologicalSort.IterativeSort<BoundDecisionDag>(SpecializedCollections.SingletonEnumerable<BoundDecisionDag>(decisionDag), d => d.Successors());

                ComputeLabelSet(sortedNodes);

                LowerDecisionDag(sortedNodes);
            }

            private void ComputeLabelSet(ImmutableArray<BoundDecisionDag> sortedNodes)
            {
                foreach (var node in sortedNodes)
                {
                    switch (node)
                    {
                        case BoundWhenClause w:
                            if (w.WhenFalse != null)
                            {
                                GetDagNodeLabel(node);
                                _backwardLabels.Add(w.WhenFalse);
                            }
                            break;
                        case BoundDecision d:
                            // Final decisions can branch directly to the target
                            _dagNodeLabels.Add(node, d.Label);
                            break;
                    }
                }
            }

            protected new void Free()
            {
                base.Free();
                _loweredDecisionDag.Free();
                _backwardLabels.Free();
            }

            private LabelSymbol GetDagNodeLabel(BoundDecisionDag dag)
            {
                if (!_dagNodeLabels.TryGetValue(dag, out LabelSymbol label))
                {
                    if (dag is BoundDecision d)
                    {
                        label = d.Label;
                        _dagNodeLabels.Add(dag, label);
                        _generated.Add(d);
                    }
                    else
                    {
                        label = _factory.GenerateLabel("dagNode");
                        _dagNodeLabels.Add(dag, label);
                    }
                }

                return label;
            }

            /// <summary>
            /// Lower the given nodes into _loweredDecisionDag.
            /// </summary>
            private void LowerDecisionDag(ImmutableArray<BoundDecisionDag> sortedNodes)
            {
                // Call LowerDecisionDagNode with each node and its following node in the generation order.
                BoundDecisionDag previous = null;
                foreach (BoundDecisionDag node in sortedNodes)
                {
                    // BoundDecision nodes do not get any code generated for them.
                    if (node is BoundDecision)
                    {
                        continue;
                    }

                    if (previous != null)
                    {
                        LowerDecisionDagNode(previous, node);
                    }

                    previous = node;
                }

                // Lower the final node
                if (previous != null)
                {
                    LowerDecisionDagNode(previous, null);
                }
            }

            /// <summary>
            /// Translate the decision tree for node, given that it will be followed by the translation for nextNode.
            /// </summary>
            private void LowerDecisionDagNode(BoundDecisionDag node, BoundDecisionDag nextNode)
            {
                if (_dagNodeLabels.TryGetValue(node, out LabelSymbol nodeLabel))
                {
                    _loweredDecisionDag.Add(_factory.Label(nodeLabel));
                }

                switch (node)
                {
                    case BoundEvaluationPoint evaluationPoint:
                        {
                            BoundExpression sideEffect = LowerEvaluation(evaluationPoint.Evaluation);
                            Debug.Assert(sideEffect != null);
                            _loweredDecisionDag.Add(_factory.ExpressionStatement(sideEffect));
                            if (nextNode != evaluationPoint.Next)
                            {
                                // We only need a goto if we would not otherwise fall through to the desired state
                                _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(evaluationPoint.Next)));
                            }
                        }

                        break;

                    case BoundDecisionPoint decisionPoint:
                        {
                            // PROTOTYPE(patterns2): should translate a chain of constant value tests into a switch instruction as before
                            BoundExpression test = base.LowerDecision(decisionPoint.Decision);

                            // Because we have already "optimized" away tests for a constant switch expression, the decision should be nontrivial.
                            Debug.Assert(test != null);

                            if (nextNode == decisionPoint.WhenFalse)
                            {
                                _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(decisionPoint.WhenTrue), jumpIfTrue: true));
                                // fall through to false decision
                            }
                            else if (nextNode == decisionPoint.WhenTrue)
                            {
                                _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(decisionPoint.WhenFalse), jumpIfTrue: false));
                                // fall through to true decision
                            }
                            else
                            {
                                _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(decisionPoint.WhenTrue), jumpIfTrue: true));
                                _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(decisionPoint.WhenFalse)));
                            }
                        }

                        break;

                    case BoundWhenClause whenClause:
                        {
                            // This node is used even when there is no when clause, to record bindings. In the case that there
                            // is no when clause, whenClause.WhenExpression and whenClause.WhenFalse are null, and the syntax for this
                            // node is the case clause.

                            // We need to assign the pattern variables in the code where they are in scope, so we produce a branch
                            // to the section where they are in scope and evaluate the when clause there.
                            var whenTrue = (BoundDecision)whenClause.WhenTrue;

                            var labelToSectionScope = _factory.GenerateLabel("where");
                            _loweredDecisionDag.Add(_factory.Goto(labelToSectionScope));

                            // We need the section syntax to get the section builder from the map. Unfortunately this is a bit awkward
                            SyntaxNode sectionSyntax;
                            switch (whenClause.Syntax)
                            {
                                case WhenClauseSyntax w:
                                    sectionSyntax = w.Parent.Parent;
                                    break;
                                case SwitchLabelSyntax l:
                                    sectionSyntax = l.Parent;
                                    break;
                                case SwitchExpressionArmSyntax a:
                                    sectionSyntax = a;
                                    break;
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(whenClause.Syntax.Kind());
                            }

                            bool foundSectionBuilder = _switchArms.TryGetValue(sectionSyntax, out ArrayBuilder<BoundStatement> sectionBuilder);
                            Debug.Assert(foundSectionBuilder);
                            sectionBuilder.Add(_factory.Label(labelToSectionScope));
                            foreach ((BoundExpression left, BoundDagTemp right) in whenClause.Bindings)
                            {
                                sectionBuilder.Add(_factory.Assignment(left, _tempAllocator.GetTemp(right)));
                            }

                            var whenFalse = whenClause.WhenFalse;
                            if (whenClause.WhenExpression != null && whenClause.WhenExpression.ConstantValue != ConstantValue.True)
                            {
                                // PROTOTYPE(patterns2): there should perhaps be a sequence point (for e.g. a breakpoint) on the when clase.
                                // However, it is not clear that is wanted for the switch expression as that would be a breakpoing where the stack is nonempty.
                                sectionBuilder.Add(_factory.ConditionalGoto(_localRewriter.VisitExpression(whenClause.WhenExpression), whenTrue.Label, jumpIfTrue: true));
                                Debug.Assert(whenFalse != null);
                                Debug.Assert(_backwardLabels.Contains(whenFalse));
                                sectionBuilder.Add(_factory.Goto(GetDagNodeLabel(whenFalse)));
                            }
                            else
                            {
                                Debug.Assert(whenFalse == null);
                                sectionBuilder.Add(_factory.Goto(whenTrue.Label));
                            }
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(node.Kind);
                }
            }
        }
    }
}
