// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIsPatternExpression(BoundIsPatternExpression node)
        {
            var isPatternRewriter = new IsPatternExpressionLinearLocalRewriter(node, this);
            BoundExpression result = isPatternRewriter.LowerIsPatternAsLinearTestSequence(node);
            isPatternRewriter.Free();

            if (node.IsNegated)
            {
                result = this._factory.Not(result);
            }
            
            return result;
        }

        private sealed class IsPatternExpressionLinearLocalRewriter : PatternLocalRewriter
        {
            public IsPatternExpressionLinearLocalRewriter(BoundIsPatternExpression node, LocalRewriter localRewriter)
                : base(node.Syntax, localRewriter, generateInstrumentation: false)
            {
            }

            public BoundExpression LowerIsPatternAsLinearTestSequence(BoundIsPatternExpression isPatternExpression)
            {
                BoundDecisionDag decisionDag = isPatternExpression.GetDecisionDagForLowering(_factory.Compilation);
                LabelSymbol whenTrueLabel = isPatternExpression.WhenTrueLabel;
                BoundExpression loweredInput = _localRewriter.VisitExpression(isPatternExpression.Expression);
                
                var sideEffectBuilder = ArrayBuilder<BoundExpression>.GetInstance();
                // The optimization of sharing pattern-matching temps with user variables can always apply to
                // an is-pattern expression because there is no when clause that could possibly intervene during
                // the execution of the pattern-matching automaton and change one of those variables.
                decisionDag = ShareTempsAndEvaluateInput(loweredInput, decisionDag, expr => sideEffectBuilder.Add(expr), out _);


                BoundWhenDecisionDagNode? whenNodeOpt = null;
                BoundExpression result = decisionDag.Rewrite<BoundExpression>(makeReplacement);
                
                if (sideEffectBuilder.Any())
                {
                    result = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, sideEffectBuilder.ToImmutableAndClear(), result);
                }
                
                if (whenNodeOpt is not null)
                {
                    foreach (BoundPatternBinding binding in whenNodeOpt.Bindings)
                    {
                        BoundExpression left = _localRewriter.VisitExpression(binding.VariableAccess);
                        BoundExpression right = _tempAllocator.GetTemp(binding.TempContainingValue);
                        if (left != right)
                        {
                            sideEffectBuilder.Add(_factory.AssignmentExpression(left, right));
                        }
                    }
                }

                if (sideEffectBuilder.Any())
                {
                    result = _factory.LogicalAnd(result, _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, sideEffectBuilder.ToImmutable(), _factory.Literal(true)));
                }
                
                var allTemps = _tempAllocator.AllTemps();
                if (allTemps.Any())
                {
                    result = _factory.Sequence(allTemps, ImmutableArray<BoundExpression>.Empty, result);
                }
                
                sideEffectBuilder.Free();
                return result;

                BoundExpression makeReplacement(BoundDecisionDagNode node, IReadOnlyDictionary<BoundDecisionDagNode, BoundExpression> map)
                {
                    switch (node)
                    {
                        case BoundEvaluationDecisionDagNode evalNode:
                            BoundExpression next = map[evalNode.Next];
                            return next is BoundSequence seq
                                ? seq.Update(seq.Locals, seq.SideEffects.Insert(0, LowerEvaluation(evalNode.Evaluation)), seq.Value, seq.Type)
                                : _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create(LowerEvaluation(evalNode.Evaluation)), next);

                        case BoundLeafDecisionDagNode leafNode:
                            return _factory.Literal(leafNode.Label == whenTrueLabel);

                        case BoundTestDecisionDagNode testNode:
                            if (testNode.WhenTrue is BoundEvaluationDecisionDagNode e &&
                                TryLowerTypeTestAndCast(testNode.Test, e.Evaluation, out BoundExpression? sideEffect, out BoundExpression? testExpression))
                            {
                                return _factory.LogicalAnd(_factory.Sequence(ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create(sideEffect), testExpression), map[e.Next]);
                            }

                            BoundExpression whenTrue = map[testNode.WhenTrue];
                            BoundExpression whenFalse = map[testNode.WhenFalse];
                            return (whenTrue.ConstantValue?.BooleanValue, whenFalse.ConstantValue?.BooleanValue) switch
                            {
                                (true, true) => whenTrue,
                                (false, false) => whenFalse,
                                (true, false) => LowerTest(testNode.Test),
                                (false, true) => _factory.Not(LowerTest(testNode.Test)),
                                (null, true) => _factory.LogicalOr(_factory.Not(LowerTest(testNode.Test)), whenTrue),
                                (null, false) => _factory.LogicalAnd(LowerTest(testNode.Test), whenTrue),
                                (true, null) => _factory.LogicalOr(LowerTest(testNode.Test), whenFalse),
                                (false, null) => _factory.LogicalAnd(_factory.Not(LowerTest(testNode.Test)), whenFalse),
                                (null, null) => _factory.Conditional(LowerTest(testNode.Test), whenTrue, whenFalse, whenTrue.Type)
                            };
                        
                        case BoundWhenDecisionDagNode whenNode:
                            Debug.Assert(whenNode.WhenExpression is null);
                            Debug.Assert(whenNode.WhenTrue is BoundLeafDecisionDagNode d && d.Label == whenTrueLabel);
                            Debug.Assert(whenNode.WhenFalse is null);
                            Debug.Assert(whenNodeOpt is null);
                            whenNodeOpt = whenNode;
                            return map[whenNode.WhenTrue];
                        
                        case var v:
                            throw ExceptionUtilities.UnexpectedValue(v);
                    }
                }
            }
        }
    }
}
