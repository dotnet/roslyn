// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitConvertedSwitchExpression(BoundConvertedSwitchExpression node)
        {
            // The switch expression is lowered to an expression that involves the use of side-effects
            // such as jumps and labels, therefore it is represented by a BoundSpillSequence and
            // the resulting nodes will need to be "spilled" to move such statements to the top
            // level (i.e. into the enclosing statement list).
            this._needsSpilling = true;
            return SwitchExpressionLocalRewriter.Rewrite(this, node);
        }

        private sealed class SwitchExpressionLocalRewriter : BaseSwitchLocalRewriter
        {
            private SwitchExpressionLocalRewriter(BoundConvertedSwitchExpression node, LocalRewriter localRewriter)
                : base(node.Syntax, localRewriter, node.SwitchArms.SelectAsArray(arm => arm.Syntax))
            {
            }

            protected override bool IsSwitchStatement => false;

            public static BoundExpression Rewrite(LocalRewriter localRewriter, BoundConvertedSwitchExpression node)
            {
                var rewriter = new SwitchExpressionLocalRewriter(node, localRewriter);
                BoundExpression result = rewriter.LowerSwitchExpression(node);
                rewriter.Free();
                return result;
            }

            private BoundExpression LowerSwitchExpression(BoundConvertedSwitchExpression node)
            {
                _factory.Syntax = node.Syntax;
                var result = ArrayBuilder<BoundStatement>.GetInstance();
                var outerVariables = ArrayBuilder<LocalSymbol>.GetInstance();
                var loweredSwitchGoverningExpression = _localRewriter.VisitExpression(node.Expression);
                BoundDecisionDag decisionDag = ShareTempsIfPossibleAndEvaluateInput(
                    node.DecisionDag, loweredSwitchGoverningExpression, result, out BoundExpression savedInputExpression);
                Debug.Assert(savedInputExpression != null);

                // lower the decision dag.
                (ImmutableArray<BoundStatement> loweredDag, ImmutableDictionary<SyntaxNode, ImmutableArray<BoundStatement>> switchSections) =
                    LowerDecisionDag(decisionDag);

                // then add the rest of the lowered dag that references that input
                result.Add(_factory.Block(loweredDag));
                // A branch to the default label when no switch case matches is included in the
                // decision tree, so the code in result is unreachable at this point.

                // Lower each switch expression arm
                LocalSymbol resultTemp = _factory.SynthesizedLocal(node.Type, node.Syntax, kind: SynthesizedLocalKind.LoweringTemp);
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
                    var objectType = _factory.SpecialType(SpecialType.System_Object);
                    var thrownExpression =
                        (implicitConversionExists(savedInputExpression, objectType) &&
                                _factory.WellKnownMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctorObject, isOptional: true) is MethodSymbol exception1)
                            ? _factory.New(exception1, _factory.Convert(objectType, savedInputExpression)) :
                        (_factory.WellKnownMember(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctor, isOptional: true) is MethodSymbol exception0)
                            ? _factory.New(exception0) :
                        _factory.New(_factory.WellKnownMethod(WellKnownMember.System_InvalidOperationException__ctor));
                    result.Add(_factory.Throw(thrownExpression));
                }

                result.Add(_factory.Label(afterSwitchExpression));
                outerVariables.Add(resultTemp);
                outerVariables.AddRange(_tempAllocator.AllTemps());
                return _factory.SpillSequence(outerVariables.ToImmutableAndFree(), result.ToImmutableAndFree(), _factory.Local(resultTemp));

                bool implicitConversionExists(BoundExpression expression, TypeSymbol type)
                {
                    HashSet<DiagnosticInfo> discarded = null;
                    Conversion c = _localRewriter._compilation.Conversions.ClassifyConversionFromExpression(expression, type, ref discarded);
                    return c.IsImplicit;
                }
            }
        }
    }
}
