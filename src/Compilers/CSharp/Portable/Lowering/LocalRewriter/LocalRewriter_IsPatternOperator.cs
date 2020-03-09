// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
                // When in error recovery, the generated code doesn't matter.
                if (test.Type.IsErrorType())
                    return;

                Debug.Assert(test.Type.SpecialType == SpecialType.System_Boolean);
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
            private void LowerOneTest(BoundDagTest test, bool invert = false)
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
                                if (invert)
                                    testExpression = _factory.Not(testExpression);

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

                return
                    CanProduceLinearSequence(node, whenTrueLabel, whenFalseLabel) ? ProduceLinearTestSequence(node, whenTrueLabel, whenFalseLabel) :
                    CanProduceLinearSequence(node, whenFalseLabel, whenTrueLabel) ? _factory.Not(ProduceLinearTestSequence(node, whenFalseLabel, whenTrueLabel)) :
                    throw new NotImplementedException("Lowering of complex is-pattern expressions");
            }

            private bool IsFailureNode(BoundDecisionDagNode node, LabelSymbol whenFalseLabel)
            {
                if (node is BoundWhenDecisionDagNode w)
                    node = w.WhenTrue;
                return node is BoundLeafDecisionDagNode l && l.Label == whenFalseLabel;
            }

            private bool CanProduceLinearSequence(
                BoundDecisionDagNode node,
                LabelSymbol whenTrueLabel,
                LabelSymbol whenFalseLabel)
            {
                while (true)
                {
                    switch (node)
                    {
                        case BoundWhenDecisionDagNode w:
                            Debug.Assert(w.WhenFalse is null);
                            node = w.WhenTrue;
                            break;
                        case BoundLeafDecisionDagNode n:
                            return n.Label == whenTrueLabel;
                        case BoundEvaluationDecisionDagNode e:
                            node = e.Next;
                            break;
                        case BoundTestDecisionDagNode t:
                            bool falseFail = IsFailureNode(t.WhenFalse, whenFalseLabel);
                            if (falseFail == IsFailureNode(t.WhenTrue, whenFalseLabel))
                                return false;
                            node = falseFail ? t.WhenTrue : t.WhenFalse;
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(node);
                    }
                }
            }

            /// <summary>
            /// Translate an is-pattern expression into a sequence of tests separated by the control-flow-and operator.
            /// </summary>
            private BoundExpression ProduceLinearTestSequence(
                BoundDecisionDagNode node,
                LabelSymbol whenTrueLabel,
                LabelSymbol whenFalseLabel)
            {
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
                                if (testNode.WhenTrue is BoundEvaluationDecisionDagNode e &&
                                    TryLowerTypeTestAndCast(testNode.Test, e.Evaluation, out BoundExpression sideEffect, out BoundExpression testExpression))
                                {
                                    _sideEffectBuilder.Add(sideEffect);
                                    AddConjunct(testExpression);
                                    node = e.Next;
                                }
                                else
                                {
                                    bool invertTest = IsFailureNode(testNode.WhenTrue, whenFalseLabel);
                                    LowerOneTest(testNode.Test, invertTest);
                                    node = invertTest ? testNode.WhenFalse : testNode.WhenTrue;
                                }
                            }
                            break;
                    }
                }

                // When we get to "the end", it is a success node.
                switch (node)
                {
                    case BoundLeafDecisionDagNode leafNode:
                        Debug.Assert(leafNode.Label == whenTrueLabel);
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
                        throw ExceptionUtilities.UnexpectedValue(node.Kind);
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
