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
    internal partial class LocalRewriter
    {
        public override BoundNode VisitPatternSwitchStatement2(BoundPatternSwitchStatement2 node)
        {
            var rewriter = new PatternSwitchLocalRewriter2(node, this);
            return rewriter.LowerPatternSwitchStatement2(node);
        }

        private class PatternSwitchLocalRewriter2 : PatternLocalRewriter
        {
            /// <summary>
            /// Map from switch section's syntax to the lowered code for the section. The code for a section
            /// includes the code to assign to the pattern variables and evaluate the when clause. Since a
            /// when clause can yield a false value, it can jump back to a label in the lowered decision dag.
            /// </summary>
            private readonly Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchSections;

            /// <summary>
            /// The lowered decision dag. This includes all of the code to decide which pattern
            /// is matched, but not the code to assign to pattern variables and evaluate when clauses.
            /// </summary>
            private readonly ArrayBuilder<BoundStatement> _loweredDecisionDag;

            /// <summary>
            /// The label in the code for the beginning of code for each node of the dag.
            /// </summary>
            private readonly Dictionary<BoundDecisionDag, LabelSymbol> _dagNodeLabels;

            /// <summary>
            /// This set contains a dag once we have produced a label for its entry point.
            /// </summary>
            private readonly HashSet<BoundDecisionDag> _generated;

            public LabelSymbol GetDagNodeLabel(BoundDecisionDag dag)
            {
                if (!_dagNodeLabels.TryGetValue(dag, out LabelSymbol label))
                {
                    label = _factory.GenerateLabel("dagNode");
                    _dagNodeLabels.Add(dag, label);
                }

                return label;
            }

            public PatternSwitchLocalRewriter2(BoundPatternSwitchStatement2 node, LocalRewriter localRewriter)
                : base(localRewriter, localRewriter.VisitExpression(node.Expression))
            {
                this._switchSections = new Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>>();
                this._loweredDecisionDag = ArrayBuilder<BoundStatement>.GetInstance();
                _dagNodeLabels = new Dictionary<BoundDecisionDag, LabelSymbol>();
                _generated = new HashSet<BoundDecisionDag>();
                foreach (var section in node.SwitchSections)
                {
                    _switchSections.Add(section.Syntax, ArrayBuilder<BoundStatement>.GetInstance());
                }
            }

            internal void LowerDecisionDag(BoundDecisionDag decisionDag)
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

                            bool foundSectionBuilder = _switchSections.TryGetValue(sectionSyntax, out ArrayBuilder<BoundStatement> sectionBuilder);
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

            internal BoundNode LowerPatternSwitchStatement2(BoundPatternSwitchStatement2 node)
            {
                var result = ArrayBuilder<BoundStatement>.GetInstance();

                // Assign the input to a temp
                result.Add(_factory.Assignment(_tempAllocator.GetTemp(base._inputTemp), base._loweredInput));

                // then lower the rest of the dag that references that input
                this.LowerDecisionDag(node.DecisionDag);
                result.Add(_factory.Block(this._loweredDecisionDag.ToImmutableAndFree()));
                // A branch to the default label when no switch case matches is included in the
                // decision tree, so the code in result is unreachable at this point.

                // Lower each switch section.
                foreach (BoundPatternSwitchSection section in node.SwitchSections)
                {
                    ArrayBuilder<BoundStatement> sectionStatementBuilder = _switchSections[section.Syntax];
                    foreach (BoundPatternSwitchLabel switchLabel in section.SwitchLabels)
                    {
                        sectionStatementBuilder.Add(_factory.Label(switchLabel.Label));
                    }

                    foreach (BoundStatement statement in section.Statements)
                    {
                        var stmt = (BoundStatement)_localRewriter.Visit(statement);
                        if (stmt != null) sectionStatementBuilder.Add(stmt);
                    }
                    // By the semantics of the switch statement, the end of each section is unreachable.

                    result.Add(_factory.Block(section.Locals, sectionStatementBuilder.ToImmutable()));
                }

                // Dispatch temps are in scope throughout the switch statement, as they are used
                // both in the dispatch section to hold temporary values from the translation of
                // the decision dag, and in the branches where the temp values are assigned to the
                // pattern variables of matched patterns.
                return _factory.Block(_tempAllocator.AllTemps(), result.ToImmutableAndFree());
            }
        }
    }
}
