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
            // The switch expression is lowered to an expression that involves the use of side-effects
            // such as jumps and labels, therefore it is represented by a BoundSpillSequence and
            // the resulting nodes will need to be "spilled" to move such statements to the top
            // level (i.e. into the enclosing statement list).
            this._needsSpilling = true;
            return SwitchExpressionLocalRewriter.Rewrite(this, node);
        }

        private class SwitchExpressionLocalRewriter : BasePatternSwitchLocalRewriter
        {
            private SwitchExpressionLocalRewriter(LocalRewriter localRewriter, BoundSwitchExpression node)
                : base(localRewriter, localRewriter.VisitExpression(node.Expression), node.SwitchArms.SelectAsArray(arm => arm.Syntax), node.DecisionDag)
            {
            }

            public static BoundExpression Rewrite(LocalRewriter localRewriter, BoundSwitchExpression node)
            {
                var rewriter = new SwitchExpressionLocalRewriter(localRewriter, node);
                BoundExpression result = rewriter.LowerSwitchExpression(node);
                rewriter.Free();
                return result;
            }

            private BoundExpression LowerSwitchExpression(BoundSwitchExpression node)
            {
                var result = ArrayBuilder<BoundStatement>.GetInstance();
                var locals = ArrayBuilder<LocalSymbol>.GetInstance();
                LocalSymbol resultTemp = _factory.SynthesizedLocal(node.Type, node.Syntax, kind: SynthesizedLocalKind.SwitchCasePatternMatching);
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
                    sectionStatementBuilder.Add(_factory.Assignment(_factory.Local(resultTemp), _localRewriter.VisitExpression(arm.Value)));
                    var statements = sectionStatementBuilder.ToImmutableAndFree();
                    if (arm.Locals.IsEmpty)
                    {
                        result.Add(_factory.StatementList(statements));
                    }
                    else
                    {
                        // even though the whole switch expression will be lifted to the statement level, we
                        // want the locals of each section to see a section-specific scope.
                        result.Add(new BoundScope(arm.Syntax, arm.Locals, statements));
                        locals.AddRange(arm.Locals);
                    }

                    result.Add(_factory.Goto(afterSwitchExpression));
                }

                if (node.DefaultLabel != null)
                {
                    result.Add(_factory.Label(node.DefaultLabel));
                    // PROTOTYPE(patterns2): Need a dedicated platform exception type to throw for input not matched.
                    result.Add(_factory.ThrowNull());
                }

                result.Add(_factory.Label(afterSwitchExpression));
                locals.AddRange(_tempAllocator.AllTemps());
                locals.Add(resultTemp);
                return _factory.SpillSequence(locals.ToImmutableAndFree(), result.ToImmutableAndFree(), _factory.Local(resultTemp));
            }
        }
    }
}
