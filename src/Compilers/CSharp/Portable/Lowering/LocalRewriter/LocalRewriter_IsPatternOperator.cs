// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            BoundDecisionDag decisionDag = node.GetDecisionDagForLowering(_factory.Compilation);
            bool negated = node.IsNegated;
            BoundExpression result;
            if (canProduceLinearSequence(decisionDag.RootNode, whenTrueLabel: node.WhenTrueLabel, whenFalseLabel: node.WhenFalseLabel))
            {
                // If we can build a linear test sequence `(e1 && e2 && e3)` for the dag, do so.
                var isPatternRewriter = new IsPatternExpressionLinearLocalRewriter(node, this);
                result = isPatternRewriter.LowerIsPatternAsLinearTestSequence(node, decisionDag, whenTrueLabel: node.WhenTrueLabel, whenFalseLabel: node.WhenFalseLabel);
                isPatternRewriter.Free();
            }
            else if (IsFailureNode(decisionDag.RootNode, node.WhenFalseLabel))
            {
                // If the given pattern always fails due to a constant input (see comments on BoundDecisionDag.SimplifyDecisionDagIfConstantInput),
                // we build a linear test sequence with the whenTrue and whenFalse labels swapped and then negate the result, to keep the result a constant.
                // Note that the positive case will be handled by canProduceLinearSequence above, however, we avoid to produce a full inverted linear sequence here
                // because we may be able to generate better code for a sequence of `or` patterns, using a switch dispatch, for example, which is done in the general rewriter.
                negated = !negated;
                var isPatternRewriter = new IsPatternExpressionLinearLocalRewriter(node, this);
                result = isPatternRewriter.LowerIsPatternAsLinearTestSequence(node, decisionDag, whenTrueLabel: node.WhenFalseLabel, whenFalseLabel: node.WhenTrueLabel);
                isPatternRewriter.Free();
            }
            else
            {
                // We need to lower a generalized dag, so we produce a label for the true and false branches and assign to a temporary containing the result.
                var isPatternRewriter = new IsPatternExpressionGeneralLocalRewriter(node.Syntax, this);
                result = isPatternRewriter.LowerGeneralIsPattern(node, decisionDag);
                isPatternRewriter.Free();
            }

            if (negated)
            {
                result = this._factory.Not(result);
            }
            return result;

            // Can the given decision dag node, and its successors, be generated as a sequence of
            // linear tests with a single "golden" path to the true label and all other paths leading
            // to the false label?  This occurs with an is-pattern expression that uses no "or" or "not"
            // pattern forms.
            static bool canProduceLinearSequence(
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
        }

        /// <summary>
        /// A local rewriter for lowering an is-pattern expression.  This handles the general case by lowering
        /// the decision dag, and returning a "true" or "false" value as the result at the end.
        /// </summary>
        private sealed class IsPatternExpressionGeneralLocalRewriter : DecisionDagRewriter
        {
            private readonly ArrayBuilder<BoundStatement> _statements = ArrayBuilder<BoundStatement>.GetInstance();

            public IsPatternExpressionGeneralLocalRewriter(
                SyntaxNode node,
                LocalRewriter localRewriter) : base(node, localRewriter, generateInstrumentation: false)
            {
            }

            protected override ArrayBuilder<BoundStatement> BuilderForSection(SyntaxNode section) => _statements;

            public new void Free()
            {
                base.Free();
                _statements.Free();
            }

            internal BoundExpression LowerGeneralIsPattern(BoundIsPatternExpression node, BoundDecisionDag decisionDag)
            {
                _factory.Syntax = node.Syntax;
                var resultBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                var inputExpression = _localRewriter.VisitExpression(node.Expression);
                decisionDag = ShareTempsIfPossibleAndEvaluateInput(decisionDag, inputExpression, resultBuilder, out _);

                // lower the decision dag.
                ImmutableArray<BoundStatement> loweredDag = LowerDecisionDagCore(decisionDag);
                resultBuilder.Add(_factory.Block(loweredDag));
                Debug.Assert(node.Type is { SpecialType: SpecialType.System_Boolean });
                LocalSymbol resultTemp = _factory.SynthesizedLocal(node.Type, node.Syntax, kind: SynthesizedLocalKind.LoweringTemp);
                LabelSymbol afterIsPatternExpression = _factory.GenerateLabel("afterIsPatternExpression");
                LabelSymbol trueLabel = node.WhenTrueLabel;
                LabelSymbol falseLabel = node.WhenFalseLabel;
                if (_statements.Count != 0)
                    resultBuilder.Add(_factory.Block(_statements.ToArray()));
                resultBuilder.Add(_factory.Label(trueLabel));
                resultBuilder.Add(_factory.Assignment(_factory.Local(resultTemp), _factory.Literal(true)));
                resultBuilder.Add(_factory.Goto(afterIsPatternExpression));
                resultBuilder.Add(_factory.Label(falseLabel));
                resultBuilder.Add(_factory.Assignment(_factory.Local(resultTemp), _factory.Literal(false)));
                resultBuilder.Add(_factory.Label(afterIsPatternExpression));
                _localRewriter._needsSpilling = true;
                return _factory.SpillSequence(_tempAllocator.AllTemps().Add(resultTemp), resultBuilder.ToImmutableAndFree(), _factory.Local(resultTemp));
            }
        }

        private static bool IsFailureNode(BoundDecisionDagNode node, LabelSymbol whenFalseLabel)
        {
            if (node is BoundWhenDecisionDagNode w)
                node = w.WhenTrue;
            return node is BoundLeafDecisionDagNode l && l.Label == whenFalseLabel;
        }

        private sealed class IsPatternExpressionLinearLocalRewriter : PatternLocalRewriter
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

            public IsPatternExpressionLinearLocalRewriter(BoundIsPatternExpression node, LocalRewriter localRewriter)
                : base(node.Syntax, localRewriter, generateInstrumentation: false)
            {
                _conjunctBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                _sideEffectBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            }

            public new void Free()
            {
                _conjunctBuilder.Free();
                _sideEffectBuilder.Free();
                base.Free();
            }

            private void AddConjunct(BoundExpression test)
            {
                // When in error recovery, the generated code doesn't matter.
                if (test.Type?.IsErrorType() != false)
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

            public BoundExpression LowerIsPatternAsLinearTestSequence(
                BoundIsPatternExpression isPatternExpression,
                BoundDecisionDag decisionDag,
                LabelSymbol whenTrueLabel,
                LabelSymbol whenFalseLabel)
            {
                BoundExpression loweredInput = _localRewriter.VisitExpression(isPatternExpression.Expression);

                // The optimization of sharing pattern-matching temps with user variables can always apply to
                // an is-pattern expression because there is no when clause that could possibly intervene during
                // the execution of the pattern-matching automaton and change one of those variables.
                decisionDag = ShareTempsAndEvaluateInput(loweredInput, decisionDag, expr => _sideEffectBuilder.Add(expr), out _);
                var node = decisionDag.RootNode;
                return ProduceLinearTestSequence(node, whenTrueLabel, whenFalseLabel);
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
                                    TryLowerTypeTestAndCast(testNode.Test, e.Evaluation, out BoundExpression? sideEffect, out BoundExpression? testExpression))
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
                BoundExpression? result = null;
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
