// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            var isPatternRewriter = new IsPatternExpressionLocalRewriter(node.Syntax, this);
            BoundExpression result = isPatternRewriter.LowerIsPattern(node, node.Pattern, this._compilation, this._diagnostics);
            isPatternRewriter.Free();
            return result;
        }

        private sealed class IsPatternExpressionLocalRewriter : PatternLocalRewriter
        {
            /// <summary>
            /// Accumulates side-effects that come before the next conjunct.
            /// </summary>
            private readonly ArrayBuilder<BoundExpression> _sideEffectBuilder;

            /// <summary>
            /// Accumulates conjuncts (conditions that must all be true) for the translation. When a conjunct is added,
            /// elements of the _sideEffectBuilder, if any, should be added as part of a sequence expression for
            /// the conjunct being added.
            /// </summary>
            private readonly ArrayBuilder<BoundExpression> _conjunctBuilder;

            public IsPatternExpressionLocalRewriter(SyntaxNode node, LocalRewriter localRewriter)
                : base(node, localRewriter)
            {
                _conjunctBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                _sideEffectBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            }

            protected override bool IsSwitchStatement => false;

            public new void Free()
            {
                _conjunctBuilder.Free();
                _sideEffectBuilder.Free();
                base.Free();
            }

            private void AddConjunct(BoundExpression test)
            {
                if (_sideEffectBuilder.Count != 0)
                {
                    test = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, _sideEffectBuilder.ToImmutable(), test);
                    _sideEffectBuilder.Clear();
                }

                _conjunctBuilder.Add(test);
            }

            /// <summary>
            /// Translate the single test into _sideEffectBuilder and _conjunctBuilder.
            /// </summary>
            private void LowerOneTest(BoundDagTest test)
            {
                _factory.Syntax = test.Syntax;
                switch (test)
                {
                    case BoundDagEvaluation eval:
                        {
                            var sideEffect = LowerEvaluation(eval);
                            _sideEffectBuilder.Add(sideEffect);
                            return;
                        }
                    case var _:
                        {
                            var testExpression = LowerTest(test);
                            if (testExpression != null)
                            {
                                AddConjunct(testExpression);
                            }

                            return;
                        }
                }
            }

            public BoundExpression LowerIsPattern(
                BoundIsPatternExpression isPatternExpression, BoundPattern pattern, CSharpCompilation compilation, DiagnosticBag diagnostics)
            {
                BoundDecisionDag decisionDag = isPatternExpression.DecisionDag;
                LabelSymbol whenTrueLabel = isPatternExpression.WhenTrueLabel;
                LabelSymbol whenFalseLabel = isPatternExpression.WhenFalseLabel;
                BoundExpression loweredInput = _localRewriter.VisitExpression(isPatternExpression.Expression);

                // The optimization of sharing pattern-matching temps with user variables can always apply to
                // an is-pattern expression because there is no when clause that could possibly intervene during
                // the execution of the pattern-matching automaton and change one of those variables.
                decisionDag = ShareTempsAndEvaluateInput(loweredInput, decisionDag, expr => _sideEffectBuilder.Add(expr), out _);
                var node = decisionDag.RootNode;

                // We follow the "good" path in the decision dag. We depend on it being nicely linear in structure.
                // If we add "or" patterns that assumption breaks down.
                while (node.Kind != BoundKind.LeafDecisionDagNode && node.Kind != BoundKind.WhenDecisionDagNode)
                {
                    switch (node)
                    {
                        case BoundEvaluationDecisionDagNode evalNode:
                            {
                                LowerOneTest(evalNode.Evaluation);
                                node = evalNode.Next;
                            }
                            break;
                        case BoundTestDecisionDagNode testNode:
                            {
                                Debug.Assert(testNode.WhenFalse is BoundLeafDecisionDagNode x && x.Label == whenFalseLabel);
                                if (testNode.WhenTrue is BoundEvaluationDecisionDagNode e &&
                                    TryLowerTypeTestAndCast(testNode.Test, e.Evaluation, out BoundExpression sideEffect, out BoundExpression testExpression))
                                {
                                    _sideEffectBuilder.Add(sideEffect);
                                    AddConjunct(testExpression);
                                    node = e.Next;
                                }
                                else
                                {
                                    LowerOneTest(testNode.Test);
                                    node = testNode.WhenTrue;
                                }
                            }
                            break;
                    }
                }

                // When we get to "the end", we see if it is a success node.
                switch (node)
                {
                    case BoundLeafDecisionDagNode leafNode:
                        {
                            if (leafNode.Label == whenFalseLabel)
                            {
                                // It is not clear that this can occur given the dag "optimizations" we performed earlier.
                                AddConjunct(_factory.Literal(false));
                            }
                            else
                            {
                                Debug.Assert(leafNode.Label == whenTrueLabel);
                            }
                        }

                        break;

                    case BoundWhenDecisionDagNode whenNode:
                        {
                            Debug.Assert(whenNode.WhenExpression == null);
                            Debug.Assert(whenNode.WhenTrue is BoundLeafDecisionDagNode d && d.Label == whenTrueLabel);
                            foreach (BoundPatternBinding binding in whenNode.Bindings)
                            {
                                BoundExpression left = _localRewriter.VisitExpression(binding.VariableAccess);
                                BoundExpression right = _tempAllocator.GetTemp(binding.TempContainingValue);
                                if (left != right)
                                {
                                    _sideEffectBuilder.Add(_factory.AssignmentExpression(left, right));
                                }
                            }
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(decisionDag.Kind);
                }

                if (_sideEffectBuilder.Count > 0 || _conjunctBuilder.Count == 0)
                {
                    AddConjunct(_factory.Literal(true));
                }

                Debug.Assert(_sideEffectBuilder.Count == 0);
                BoundExpression result = null;
                foreach (BoundExpression conjunct in _conjunctBuilder)
                {
                    result = (result == null) ? conjunct : _factory.LogicalAnd(result, conjunct);
                }

                _conjunctBuilder.Clear();
                Debug.Assert(result != null);
                var allTemps = _tempAllocator.AllTemps();
                if (allTemps.Length > 0)
                {
                    result = _factory.Sequence(allTemps, ImmutableArray<BoundExpression>.Empty, result);
                }

                return result;
            }
        }
    }
}
