﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
            private SwitchExpressionLocalRewriter(BoundSwitchExpression node, LocalRewriter localRewriter)
                : base(node.Syntax, localRewriter, node.SwitchArms.SelectAsArray(arm => arm.Syntax), isSwitchStatement: false)
            {
            }

            public static BoundExpression Rewrite(LocalRewriter localRewriter, BoundSwitchExpression node)
            {
                var rewriter = new SwitchExpressionLocalRewriter(node, localRewriter);
                BoundExpression result = rewriter.LowerSwitchExpression(node);
                rewriter.Free();
                return result;
            }

            private BoundExpression LowerSwitchExpression(BoundSwitchExpression node)
            {
                _factory.Syntax = node.Syntax;
                var result = ArrayBuilder<BoundStatement>.GetInstance();
                var outerVariables = ArrayBuilder<LocalSymbol>.GetInstance();
                var loweredSwitchGoverningExpression = _localRewriter.VisitExpression(node.Expression);
                BoundDecisionDag decisionDag = ShareTempsIfPossibleAndEvaluateInput(node.DecisionDag, loweredSwitchGoverningExpression, result);

                // lower the decision dag.
                (ImmutableArray<BoundStatement> loweredDag, ImmutableDictionary<SyntaxNode, ImmutableArray<BoundStatement>> switchSections) =
                    LowerDecisionDag(decisionDag);

                // then add the rest of the lowered dag that references that input
                result.Add(_factory.Block(loweredDag));
                // A branch to the default label when no switch case matches is included in the
                // decision tree, so the code in result is unreachable at this point.

                // Lower each switch expression arm
                LocalSymbol resultTemp = _factory.SynthesizedLocal(node.Type, node.Syntax, kind: SynthesizedLocalKind.SwitchCasePatternMatching);
                LabelSymbol afterSwitchExpression = _factory.GenerateLabel("afterSwitchExpression");
                foreach (BoundSwitchExpressionArm arm in node.SwitchArms)
                {
                    _factory.Syntax = arm.Syntax;
                    var sectionBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                    sectionBuilder.AddRange(switchSections[arm.Syntax]);
                    sectionBuilder.Add(_factory.Label(arm.Label));
                    sectionBuilder.Add(_factory.Assignment(_factory.Local(resultTemp), _localRewriter.VisitExpression(arm.Value)));
                    sectionBuilder.Add(_factory.Goto(afterSwitchExpression));
                    var statements = sectionBuilder.ToImmutableAndFree();
                    if (arm.Locals.IsEmpty)
                    {
                        result.Add(_factory.StatementList(statements));
                    }
                    else
                    {
                        // Lifetime of these locals is expanded to the entire switch body, as it is possible to
                        // share them as temps in the decision dag.
                        outerVariables.AddRange(arm.Locals);

                        // Note the language scope of the locals, even though they are included for the purposes of
                        // lifetime analysis in the enclosing scope.
                        result.Add(new BoundScope(arm.Syntax, arm.Locals, statements));
                    }
                }

                _factory.Syntax = node.Syntax;
                if (node.DefaultLabel != null)
                {
                    result.Add(_factory.Label(node.DefaultLabel));
                    // PROTOTYPE(patterns2): Need a dedicated platform exception type to throw for input not matched.
                    result.Add(_factory.ThrowNull());
                }

                result.Add(_factory.Label(afterSwitchExpression));
                outerVariables.Add(resultTemp);
                outerVariables.AddRange(_tempAllocator.AllTemps());
                return _factory.SpillSequence(outerVariables.ToImmutableAndFree(), result.ToImmutableAndFree(), _factory.Local(resultTemp));
            }
        }
    }
}
