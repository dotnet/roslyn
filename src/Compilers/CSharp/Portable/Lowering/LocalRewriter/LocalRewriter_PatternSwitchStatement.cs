// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        // PROTOTYPE(typeswitch): as a temporary hack while this code is in development, we
        // only use the new translation machinery when this bool is set to true. If it is false
        // then we use the transitional code which translates a pattern switch into a series of
        // if-then-else statements. Ultimately we need the new translation to be used to generate
        // switch IL instructions for ordinary old-style switch statements.
        private static bool useNewTranslation = false;

        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            // Until this is all implemented, we use a dumb series of if-then-else
            // statements to translate the switch statement.
            if (!useNewTranslation) return VisitPatternSwitchStatement_Ifchain(node);

            var usedLabels = new HashSet<LabelSymbol>();
            var usedTemps = new HashSet<LocalSymbol>();  // PROTOTYPE(typeswitch): worry about deterministic ordering
            var result = ArrayBuilder<BoundStatement>.GetInstance();

            // PROTOTYPE(typeswitch): do we need to do anything with node.ConstantTargetOpt, given that we
            // have the decision tree? If not, can we remove it from the bound trees?

            if (node.DecisionTree.Expression != node.Expression)
            {
                // Store the input expression into a temp
                // PROTOTYPE(typeswitch): do we need to add the temp to the list of used temps?
                result.Add(_factory.Assignment(node.DecisionTree.Expression, node.Expression));
            }

            // output the decision tree part
            LowerDecisionTree(node.DecisionTree, usedLabels, usedTemps, result);
            result.Add(_factory.Goto(node.BreakLabel));

            // output the sections of code (that were reachable)
            foreach (var section in node.SwitchSections)
            {
                ArrayBuilder<BoundStatement> sectionBuilder = null;
                foreach (var label in section.SwitchLabels)
                {
                    if (usedLabels.Contains(label.Label))
                    {
                        if (sectionBuilder == null)
                        {
                            sectionBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                        }

                        sectionBuilder.Add(_factory.Label(label.Label));
                    }
                }

                if (sectionBuilder != null)
                {
                    sectionBuilder.AddRange(VisitList(section.Statements));
                    sectionBuilder.Add(_factory.Goto(node.BreakLabel));
                    result.Add(_factory.Block(section.Locals, sectionBuilder.ToImmutableAndFree()));
                }
            }

            result.Add(_factory.Label(node.BreakLabel));
            return _factory.Block(usedTemps.ToImmutableArray().Concat(node.InnerLocals), node.InnerLocalFunctions, result.ToImmutableAndFree());
        }

        private void LowerDecisionTree(DecisionTree decisionTree, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            if (decisionTree == null) return;
            switch (decisionTree.Kind)
            {
                case DecisionTree.DecisionKind.ByType:
                    {
                        LowerDecisionTree((DecisionTree.ByType)decisionTree, usedLabels, usedTemps, result);
                        return;
                    }
                case DecisionTree.DecisionKind.ByValue:
                    {
                        LowerDecisionTree((DecisionTree.ByValue)decisionTree, usedLabels, usedTemps, result);
                        return;
                    }
                case DecisionTree.DecisionKind.Guarded:
                    {
                        LowerDecisionTree((DecisionTree.Guarded)decisionTree, usedLabels, usedTemps, result);
                        return;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(decisionTree.Kind);
            }
        }

        private void LowerDecisionTree(DecisionTree.ByType byType, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            var defaultLabel = _factory.GenerateLabel("byTypeDefault");
            if (byType.Type.CanBeAssignedNull())
            {
                var notNullLabel = _factory.GenerateLabel("notNull");
                var inputExpression = byType.Expression;
                var nullValue = _factory.NullOrDefault(byType.Type);
                BoundExpression notNull = byType.Type.IsNullableType()
                    ? this.RewriteNullableNullEquality(_factory.Syntax, BinaryOperatorKind.NullableNullNotEqual, byType.Expression, nullValue, _factory.SpecialType(SpecialType.System_Boolean))
                    : _factory.ObjectNotEqual(byType.Expression, nullValue);
                result.Add(_factory.ConditionalGoto(notNull, notNullLabel, true));
                LowerDecisionTree(byType.WhenNull, usedLabels, usedTemps, result);
                result.Add(_factory.Goto(defaultLabel));
                result.Add(_factory.Label(notNullLabel));
            }
            else
            {
                Debug.Assert(byType.WhenNull == null);
            }

            foreach (var td in byType.TypeAndDecision)
            {
                var type = td.Key;
                var decision = td.Value;
                var failLabel = _factory.GenerateLabel("failedDecision");
                var testAndCopy = TypeTestAndCopyToTemp(byType.Expression, decision.Expression);
                result.Add(_factory.ConditionalGoto(testAndCopy, failLabel, false));
                LowerDecisionTree(decision, usedLabels, usedTemps, result);
                result.Add(_factory.Label(failLabel));
            }

            result.Add(_factory.Label(defaultLabel));
            LowerDecisionTree(byType.Default, usedLabels, usedTemps, result);
        }

        private BoundExpression TypeTestAndCopyToTemp(BoundExpression input, BoundExpression temp)
        {
            if (input == temp)
            {
                // if the expression is the same, no need to type test and copy to temp
                return _factory.Literal(true);
            }

            Debug.Assert(temp.Kind == BoundKind.Local);
            return MakeDeclarationPattern(_factory.Syntax, input, ((BoundLocal)temp).LocalSymbol);
        }

        private void LowerDecisionTree(DecisionTree.ByValue byValue, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            throw new NotImplementedException();
        }

        private void LowerDecisionTree(DecisionTree.Guarded guarded, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            usedLabels.Add(guarded.Label.Label);
            BoundStatement stmt = _factory.Goto(guarded.Label.Label);
            if (guarded.Guard != null)
            {
                stmt = _factory.If(VisitExpression(guarded.Guard), stmt);
            }

            result.Add(stmt);
        }

        private dynamic grep(params object[] args)
        {
            throw null;
        }
    }
}
