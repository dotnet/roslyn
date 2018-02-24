// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitSwitchExpression(BoundSwitchExpression node)
        {
            return SwitchExpressionLocalRewriter.Rewrite(this, node);
        }

        private class SwitchExpressionLocalRewriter : BasePatternSwitchLocalRewriter
        {
            private SwitchExpressionLocalRewriter(LocalRewriter localRewriter, BoundSwitchExpression node)
                : base(localRewriter, localRewriter.VisitExpression(node.Expression), node.SwitchArms.SelectAsArray(arm => arm.Syntax), node.DecisionDag)
            {
            }

            public static BoundNode Rewrite(LocalRewriter localRewriter, BoundSwitchExpression node)
            {
                var rewriter = new SwitchExpressionLocalRewriter(localRewriter, node);
                BoundExpression result = rewriter.LowerSwitchExpression(node);
                rewriter.Free();
                return result;
            }

            private BoundExpression LowerSwitchExpression(BoundSwitchExpression node)
            {
                var result = ArrayBuilder<BoundStatement>.GetInstance();
                LocalSymbol resultTemp = _factory.SynthesizedLocal(node.Type, node.Syntax);
                LabelSymbol afterSwitchExpression = _factory.GenerateLabel("afterSwitchExpression");

                // Assign the input to a temp
                result.Add(_factory.Assignment(_tempAllocator.GetTemp(base._inputTemp), base._loweredInput));

                // then lower the rest of the dag that references that input
                result.AddRange(_loweredDecisionDag.ToImmutable());

                // A branch to the default label when no switch case matches is included in the
                // decision tree, so the code in result is unreachable at this point.

                // Lower each switch arm
                foreach (BoundSwitchExpressionArm arm in node.SwitchArms)
                {
                    ArrayBuilder<BoundStatement> sectionStatementBuilder = _switchArms[arm.Syntax];
                    result.Add(_factory.Label(arm.Label));
                    var armSequence = _factory.Sequence(arm.Locals,
                        sectionStatementBuilder.ToImmutable(),
                        _factory.AssignmentExpression(_factory.Local(resultTemp), _localRewriter.VisitExpression(arm.Value)));
                    result.Add(_factory.ExpressionStatement(armSequence));
                    result.Add(_factory.Goto(afterSwitchExpression));
                }

                if (node.DefaultLabel != null)
                {
                    result.Add(_factory.Label(node.DefaultLabel));
                    // PROTOTYPE(patterns2): Need a dedicated platform exception type to throw for input not matched.
                    result.Add(_factory.ThrowNull());
                }

                result.Add(_factory.Label(afterSwitchExpression));
                var resultValue = _factory.Local(resultTemp);

                // Dispatch temps are in scope throughout the switch expression
                return _factory.Sequence(_tempAllocator.AllTemps().Add(resultTemp), result.ToImmutableAndFree(), resultValue);
            }
        }
    }
}
