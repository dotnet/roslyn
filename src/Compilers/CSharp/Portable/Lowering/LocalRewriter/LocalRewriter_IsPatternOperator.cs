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
            BoundExpression result;
            if (canProduceLinearSequence(decisionDag))
            {
                // If we can build a linear test sequence `(e1 && e2 && e3)` for the dag, do so.
                var isPatternRewriter = new IsPatternExpressionLinearLocalRewriter(node, this);
                result = isPatternRewriter.LowerIsPatternAsLinearTestSequence(node, decisionDag);
                isPatternRewriter.Free();
            }
            else
            {
                // We need to lower a generalized dag, so we produce a label for the true and false branches and assign to a temporary containing the result.
                var isPatternRewriter = new IsPatternExpressionGeneralLocalRewriter(node, this);
                result = isPatternRewriter.LowerGeneralIsPattern(node, decisionDag);
                isPatternRewriter.Free();
            }

            if (node.IsNegated)
            {
                result = this._factory.Not(result);
            }
            return result;

            static bool canProduceLinearSequence(BoundDecisionDag decisionDag)
            {
                foreach (BoundDecisionDagNode node in decisionDag.TopologicallySortedNodes)
                {
                    if (DecisionDagRewriter.CanGenerateSwitchDispatch(node))
                    {
                        return false;
                    }
                }

                return true;
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
                BoundIsPatternExpression node,
                LocalRewriter localRewriter) : base(node.Syntax, localRewriter, generateInstrumentation: false)
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

        private sealed class IsPatternExpressionLinearLocalRewriter : PatternLocalRewriter
        {
            /// <summary>
            /// Accumulates side-effects that come before every leaf or test node.
            /// </summary>
            private readonly ArrayBuilder<BoundExpression> _sideEffectBuilder;

            public IsPatternExpressionLinearLocalRewriter(BoundIsPatternExpression node, LocalRewriter localRewriter)
                : base(node.Syntax, localRewriter, generateInstrumentation: false)
            {
                _sideEffectBuilder = ArrayBuilder<BoundExpression>.GetInstance();
            }

            public new void Free()
            {
                _sideEffectBuilder.Free();
                base.Free();
            }

            public BoundExpression LowerIsPatternAsLinearTestSequence(
                BoundIsPatternExpression node,
                BoundDecisionDag decisionDag)
            {
                BoundExpression loweredInput = _localRewriter.VisitExpression(node.Expression);

                // The optimization of sharing pattern-matching temps with user variables can always apply to
                // an is-pattern expression because there is no when clause that could possibly intervene during
                // the execution of the pattern-matching automaton and change one of those variables.
                decisionDag = ShareTempsAndEvaluateInput(loweredInput, decisionDag, expr => _sideEffectBuilder.Add(expr), out _);
                BoundExpression result = ProduceLinearTestSequence(decisionDag.RootNode, node.WhenTrueLabel, node.WhenFalseLabel);
                return _factory.Sequence(_tempAllocator.AllTemps(), ImmutableArray<BoundExpression>.Empty, result);
            }

            /// <summary>
            /// Translate an is-pattern expression into a sequence of tests separated by the control-flow-and operator.
            /// </summary>
            private BoundExpression ProduceLinearTestSequence(
                BoundDecisionDagNode node,
                LabelSymbol whenTrueLabel,
                LabelSymbol whenFalseLabel)
            {
                return lowerNode(node);

                BoundExpression lowerNode(BoundDecisionDagNode node)
                {
                    return node switch
                    {
                        BoundEvaluationDecisionDagNode evalNode => lowerEvaluationNode(evalNode),
                        BoundTestDecisionDagNode testNode => lowerTestNode(testNode),
                        BoundWhenDecisionDagNode whenNode => lowerWhenNode(whenNode),
                        BoundLeafDecisionDagNode leafNode => MakeSequence(_factory.Literal(isSuccessNode(leafNode))),
                        _ => throw ExceptionUtilities.UnexpectedValue(node)
                    };
                }

                BoundExpression lowerEvaluationNode(BoundEvaluationDecisionDagNode evalNode)
                {
                    while (true)
                    {
                        _sideEffectBuilder.Add(LowerEvaluation(evalNode.Evaluation));
                        if (evalNode.Next is BoundEvaluationDecisionDagNode nextNode)
                        {
                            evalNode = nextNode;
                            continue;
                        }

                        break;
                    }

                    return lowerNode(evalNode.Next);
                }

                BoundExpression lowerWhenNode(BoundWhenDecisionDagNode whenNode)
                {
                    Debug.Assert(whenNode.WhenTrue is BoundLeafDecisionDagNode leafNode && isSuccessNode(leafNode));
                    Debug.Assert(whenNode.WhenFalse is null);
                    Debug.Assert(whenNode.WhenExpression is null);
                    foreach (BoundPatternBinding binding in whenNode.Bindings)
                    {
                        BoundExpression left = _localRewriter.VisitExpression(binding.VariableAccess);
                        BoundExpression right = _tempAllocator.GetTemp(binding.TempContainingValue);
                        if (left != right)
                        {
                            _sideEffectBuilder.Add(_factory.AssignmentExpression(left, right));
                        }
                    }

                    return lowerNode(whenNode.WhenTrue);
                }

                BoundExpression lowerTestNode(BoundTestDecisionDagNode testNode)
                {
                    BoundDecisionDagNode whenTrue, whenFalse = testNode.WhenFalse;
                    BoundExpression result = MakeSequence(LowerTestAndSimplify(testNode, out whenTrue));
                    while (true)
                    {
                        switch (whenTrue, whenFalse)
                        {
                            case (BoundTestDecisionDagNode t2, _) when t2.WhenFalse == whenFalse:
                                result = _factory.LogicalAnd(result, MakeSequence(LowerTestAndSimplify(t2, whenTrue: out whenTrue)));
                                continue;

                            case (_, BoundTestDecisionDagNode t2) when t2.WhenTrue == whenTrue:
                                result = _factory.LogicalOr(result, MakeSequence(LowerTest(t2.Test)));
                                whenFalse = t2.WhenFalse;
                                continue;

                            case (BoundTestDecisionDagNode t2, _) when t2.WhenTrue == whenFalse:
                                result = _factory.LogicalAnd(result, MakeSequence(_factory.Not(LowerTest(t2.Test))));
                                whenTrue = t2.WhenFalse;
                                continue;

                            case (_, BoundTestDecisionDagNode t2) when t2.WhenFalse == whenTrue:
                                result = _factory.LogicalOr(result, MakeSequence(_factory.Not(LowerTestAndSimplify(t2, whenTrue: out whenFalse))));
                                continue;

                            case (BoundLeafDecisionDagNode, BoundEvaluationDecisionDagNode e):
                                _sideEffectBuilder.Add(LowerEvaluation(e.Evaluation));
                                whenFalse = e.Next;
                                continue;

                            case (BoundEvaluationDecisionDagNode e, BoundLeafDecisionDagNode):
                                _sideEffectBuilder.Add(LowerEvaluation(e.Evaluation));
                                whenTrue = e.Next;
                                continue;

                            case (BoundLeafDecisionDagNode leafTrue, BoundLeafDecisionDagNode leafFalse):
                                return (isSuccessNode(leafTrue), isSuccessNode(leafFalse)) switch
                                {
                                    (true, true) => MakeSequence(_factory.Literal(true)),
                                    (false, false) => MakeSequence(_factory.Literal(false)),
                                    (false, true) => makeSequenceAfter(_factory.Not(result)),
                                    (true, false) => makeSequenceAfter(result),
                                };

                                BoundExpression makeSequenceAfter(BoundExpression result)
                                    => _sideEffectBuilder.IsEmpty() ? result :
                                        _factory.LogicalAnd(result, MakeSequence(_factory.Literal(true)));

                            case (BoundLeafDecisionDagNode leafNode, _):
                                return isSuccessNode(leafNode)
                                    ? _factory.LogicalOr(result, lowerNode(whenFalse))
                                    : _factory.LogicalAnd(_factory.Not(result), lowerNode(whenFalse));

                            case (_, BoundLeafDecisionDagNode leafNode):
                                return isSuccessNode(leafNode)
                                    ? _factory.LogicalOr(_factory.Not(result), lowerNode(whenTrue))
                                    : _factory.LogicalAnd(result, lowerNode(whenTrue));

                            default:
                                Debug.Assert(_sideEffectBuilder.IsEmpty());
                                return _factory.Conditional(
                                    condition: result,
                                    consequence: lowerNode(whenTrue),
                                    alternative: lowerNode(whenFalse),
                                    _factory.SpecialType(SpecialType.System_Boolean));
                        }
                    }
                }

                bool isSuccessNode(BoundLeafDecisionDagNode leafNode)
                {
                    Debug.Assert(
                        ReferenceEquals(leafNode.Label, whenTrueLabel) ||
                        ReferenceEquals(leafNode.Label, whenFalseLabel));
                    return ReferenceEquals(leafNode.Label, whenTrueLabel);
                }
            }

            private new BoundExpression LowerTest(BoundDagTest test)
            {
                var result = base.LowerTest(test);
                // When in error recovery, the generated code doesn't matter.
                if (result.Type?.IsErrorType() != false)
                    return _factory.Literal(true);
                return result;
            }

            private BoundExpression LowerTestAndSimplify(BoundTestDecisionDagNode testNode, out BoundDecisionDagNode whenTrue)
            {
                if (testNode.WhenTrue is BoundEvaluationDecisionDagNode evalNode &&
                    TryLowerTypeTestAndCast(testNode.Test, evalNode.Evaluation,
                        out BoundExpression sideEffect, out BoundExpression? result))
                {
                    _sideEffectBuilder.Add(sideEffect);
                    whenTrue = evalNode.Next;
                }
                else
                {
                    result = LowerTest(testNode.Test);
                    whenTrue = testNode.WhenTrue;
                }

                return result;
            }

            private BoundExpression MakeSequence(BoundExpression test)
            {
                return _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, _sideEffectBuilder.ToImmutableAndClear(), test);
            }
        }
    }
}
