// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            /// _backwardLabels.
            /// PROTOTYPE(patterns2): This is a placeholder. It is not populated or used yet.
            /// </summary>
            private readonly ArrayBuilder<LabelSymbol> _backwardLabels = ArrayBuilder<LabelSymbol>.GetInstance();

            /// <summary>
            /// Not all labels are needed in the generated state machine. If there is only one branch to a label
            /// and it is generated immediately where the branch would appear, we can eliminate both the branch
            /// and the label. This set is prepopulated with the set of decision dag nodes that have more than one
            /// predecessor.
            /// </summary>
            private readonly HashSet<BoundDecisionDag> _labelRequired = new HashSet<BoundDecisionDag>();

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

                this.LowerDecisionDag(decisionDag);
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

            private void LowerDecisionDag(BoundDecisionDag decisionDag)
            {
                LabelSymbol label = GetDagNodeLabel(decisionDag);
                if (_generated.Contains(decisionDag))
                {
                    // we want to generate each node only once (since it is a dag), so we branch if we lowered it before.
                    _loweredDecisionDag.Add(_factory.Goto(label));
                    return;
                }

                _loweredDecisionDag.Add(_factory.Label(label));
                _generated.Add(decisionDag);

                switch (decisionDag)
                {
                    case BoundEvaluationPoint evaluation:
                        {
                            base.LowerDecision(evaluation.Evaluation, out BoundExpression sideEffect, out BoundExpression test);
                            Debug.Assert(test == null);
                            Debug.Assert(sideEffect != null);
                            _loweredDecisionDag.Add(_factory.ExpressionStatement(sideEffect));
                            LowerDecisionDag(evaluation.Next);
                            return;
                        }

                    case BoundDecisionPoint decision:
                        {
                            base.LowerDecision(decision.Decision, out BoundExpression sideEffect, out BoundExpression test);
                            Debug.Assert(sideEffect == null);
                            Debug.Assert(test != null);
                            _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(decision.WhenFalse), jumpIfTrue: false));
                            LowerDecisionDag(decision.WhenTrue);
                            if (!_generated.Contains(decision.WhenFalse))
                            {
                                LowerDecisionDag(decision.WhenFalse);
                            }
                            return;
                        }

                    case BoundWhenClause whenClause:
                        {
                            // This node is used even when there is no when clause, to record the bindings. In the case that there
                            // is no when clause, whenClause.WhenExpression and whenClause.WhenFalse are null, and the syntax for this
                            // node is the case clause.

                            // We need to assign the pattern variables in the code where they are in scope, so we produce a branch
                            // to the section where they are in scope and evaluate the when clause there.
                            var whenTrue = (BoundDecision)whenClause.WhenTrue;

                            var labelToSectionScope = _factory.GenerateLabel("where");
                            _loweredDecisionDag.Add(_factory.Goto(labelToSectionScope));

                            // The direct parent of the where clause (if present) is the case clause. The parent of the case clause is the switch section.
                            SyntaxNode sectionSyntax = whenClause.Syntax is WhenClauseSyntax s ? whenClause.Syntax.Parent.Parent : whenClause.Syntax.Parent;

                            bool foundSectionBuilder = _switchArms.TryGetValue(sectionSyntax, out ArrayBuilder<BoundStatement> sectionBuilder);
                            Debug.Assert(foundSectionBuilder);
                            sectionBuilder.Add(_factory.Label(labelToSectionScope));
                            foreach ((BoundExpression left, BoundDagTemp right) in whenClause.Bindings)
                            {
                                sectionBuilder.Add(_factory.Assignment(left, _tempAllocator.GetTemp(right)));
                            }

                            var whenFalse = whenClause.WhenFalse;
                            if (whenClause.WhenExpression != null)
                            {
                                // PROTOTYPE(patterns2): there should be a sequence point (for e.g. a breakpoint) on the when clase
                                sectionBuilder.Add(_factory.ConditionalGoto(_localRewriter.VisitExpression(whenClause.WhenExpression), whenTrue.Label, jumpIfTrue: true));
                                Debug.Assert(whenFalse != null);
                                sectionBuilder.Add(_factory.Goto(GetDagNodeLabel(whenFalse)));
                                if (!_generated.Contains(whenFalse))
                                {
                                    LowerDecisionDag(whenFalse);
                                }
                            }
                            else
                            {
                                Debug.Assert(whenFalse == null);
                                sectionBuilder.Add(_factory.Goto(whenTrue.Label));
                            }

                            return;
                        }

                    case BoundDecision decision:
                        {
                            _loweredDecisionDag.Add(_factory.Goto(decision.Label));
                            return;
                        }

                    default:
                        throw ExceptionUtilities.UnexpectedValue(decisionDag.Kind);
                }
            }
        }
    }
}
