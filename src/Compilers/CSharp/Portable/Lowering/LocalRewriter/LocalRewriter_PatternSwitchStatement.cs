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
        private static bool UseNewTranslation(BoundPatternSwitchStatement node) => true;

        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            // Until this is all implemented, we use a dumb series of if-then-else
            // statements to translate the switch statement.
            if (!UseNewTranslation(node)) return VisitPatternSwitchStatement_Ifchain(node);

            var usedLabels = new HashSet<LabelSymbol>();
            var usedTemps = new HashSet<LocalSymbol>();  // PROTOTYPE(typeswitch): worry about deterministic ordering
            var result = ArrayBuilder<BoundStatement>.GetInstance();

            // PROTOTYPE(typeswitch): do we need to do anything with node.ConstantTargetOpt, given that we
            // have the decision tree? If not, can we remove it from the bound trees?
            var expression = VisitExpression(node.Expression);

            if (node.DecisionTree.Expression != expression)
            {
                // Store the input expression into a temp
                // PROTOTYPE(typeswitch): do we need to add the temp to the list of used temps?
                result.Add(_factory.Assignment(node.DecisionTree.Expression, expression));
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
                var nullValue = _factory.Null(byType.Type);
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
                // PROTOTYPE(typeswitch): do we need a null test here?
                return _factory.Literal(true);
            }

            Debug.Assert(temp.Kind == BoundKind.Local);
            return MakeDeclarationPattern(_factory.Syntax, input, ((BoundLocal)temp).LocalSymbol);
        }

        private void LowerDecisionTree(DecisionTree.ByValue byValue, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            switch (byValue.Type.SpecialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_SByte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    // switch on an integral type
                    LowerIntegralSwitch(byValue, usedLabels, usedTemps, result);
                    return;

                case SpecialType.System_String: // switch on a string
                    LowerStringSwitch(byValue, usedLabels, usedTemps, result);
                    return;

                case SpecialType.System_Boolean: // switch on a boolean
                    LowerBooleanSwitch(byValue, usedLabels, usedTemps, result);
                    return;

                // switch on a type requiring sequential comparisons. Note that we use constant.Equals(value), depending if
                // possible on the one from IComparable<T>. If that does not exist, we use instance method object.Equals(object)
                // with the (now boxed) constant on the left.
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                    LowerOtherSwitch(byValue, usedLabels, usedTemps, result);
                    return;

                default:
                    // There are no other types of constants that could be used as patterns.
                    throw ExceptionUtilities.UnexpectedValue(byValue.Type);
            }
        }

        private void LowerDecisionTree(DecisionTree.Guarded guarded, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            usedLabels.Add(guarded.Label.Label);
            var stmt =
                (guarded.Guard != null && guarded.Guard.ConstantValue != ConstantValue.True)
                ? _factory.ConditionalGoto(guarded.Guard, guarded.Label.Label, true)
                : _factory.Goto(guarded.Label.Label);
            result.Add(stmt);
        }

        // For switch statements, we have an option of completely rewriting the switch header
        // and switch sections into simpler constructs, i.e. we can rewrite the switch header
        // using bound conditional goto statements and the rewrite the switch sections into
        // bound labeled statements.
        //
        // However, all the logic for emitting the switch jump tables is language agnostic
        // and includes IL optimizations. Hence we delay the switch jump table generation
        // till the emit phase. This way we also get additional benefit of sharing this code
        // between both VB and C# compilers.
        private void LowerIntegralSwitch(DecisionTree.ByValue byValue, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            var switchSections = ArrayBuilder<BoundSwitchSection>.GetInstance();
            var noValueMatches = _factory.GenerateLabel("noValueMatches");
            foreach (var vd in byValue.ValueAndDecision)
            {
                var value = vd.Key;
                var decision = vd.Value;
                var constantValue = ConstantValue.Create(value, byValue.Type.SpecialType);
                var constantExpression = new BoundLiteral(_factory.Syntax, constantValue, byValue.Type);
                var label = new SourceLabelSymbol(_factory.CurrentMethod, _factory.Syntax, constantValue);
                var switchLabel = new BoundSwitchLabel(_factory.Syntax, label, constantExpression);
                var forValue = ArrayBuilder<BoundStatement>.GetInstance();
                LowerDecisionTree(decision, usedLabels, usedTemps, forValue);
                forValue.Add(_factory.Goto(noValueMatches));
                var section = new BoundSwitchSection(_factory.Syntax, ImmutableArray.Create(switchLabel), forValue.ToImmutableAndFree());
                switchSections.Add(section);
            }

            var switchStatement = new BoundSwitchStatement(_factory.Syntax, null, byValue.Expression, null, ImmutableArray<LocalSymbol>.Empty, ImmutableArray<LocalFunctionSymbol>.Empty, switchSections.ToImmutableAndFree(), noValueMatches, null);
            result.Add(switchStatement);
            result.Add(_factory.Label(noValueMatches));
            LowerDecisionTree(byValue.Default, usedLabels, usedTemps, result);
        }

        private void LowerBooleanSwitch(DecisionTree.ByValue byValue, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            switch (byValue.ValueAndDecision.Count)
            {
                case 0:
                    {
                        LowerDecisionTree(byValue.Default, usedLabels, usedTemps, result);
                        break;
                    }
                case 1:
                    {
                        DecisionTree decision;
                        bool onBoolean = byValue.ValueAndDecision.TryGetValue(true, out decision);
                        if (!onBoolean) byValue.ValueAndDecision.TryGetValue(false, out decision);
                        Debug.Assert(decision != null);
                        var onOther = _factory.GenerateLabel("on" + !onBoolean);
                        result.Add(_factory.ConditionalGoto(byValue.Expression, onOther, !onBoolean));
                        LowerDecisionTree(decision, usedLabels, usedTemps, result);
                        result.Add(_factory.Label(onOther));
                        LowerDecisionTree(byValue.Default, usedLabels, usedTemps, result);
                        break;
                    }
                case 2:
                    {
                        DecisionTree trueDecision, falseDecision;
                        bool hasTrue = byValue.ValueAndDecision.TryGetValue(true, out trueDecision);
                        bool hasFalse = byValue.ValueAndDecision.TryGetValue(false, out falseDecision);
                        Debug.Assert(hasTrue && hasFalse);
                        var tryAnother = _factory.GenerateLabel("tryAnother");
                        var onFalse = _factory.GenerateLabel("onFalse");
                        result.Add(_factory.ConditionalGoto(byValue.Expression, onFalse, false));
                        LowerDecisionTree(trueDecision, usedLabels, usedTemps, result);
                        result.Add(_factory.Goto(tryAnother));
                        result.Add(_factory.Label(onFalse));
                        LowerDecisionTree(falseDecision, usedLabels, usedTemps, result);
                        result.Add(_factory.Label(tryAnother));
                        // We ignore byValue.Default, as both true and false have been handled.
                        break;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(byValue.ValueAndDecision.Count);
            }
        }

        // For string switch statements, we need to determine if we are generating a hash
        // table based jump table or a non hash jump table, i.e. linear string comparisons
        // with each case label. We use the Dev10 Heuristic to determine this
        // (see SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch() for details).
        // If we are generating a hash table based jump table, we use a simple
        // hash function to hash the string constants corresponding to the case labels.
        // See SwitchStringJumpTableEmitter.ComputeStringHash().
        // We need to emit this same function to compute the hash value into the compiler generated
        // <PrivateImplementationDetails> class.
        // If we have at least one string switch statement in a module that needs a
        // hash table based jump table, we generate a single public string hash synthesized method
        // that is shared across the module.
        private void LowerStringSwitch(DecisionTree.ByValue byValue, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            throw new NotImplementedException();
        }

        private void LowerOtherSwitch(DecisionTree.ByValue byValue, HashSet<LabelSymbol> usedLabels, HashSet<LocalSymbol> usedTemps, ArrayBuilder<BoundStatement> result)
        {
            throw new NotImplementedException();
        }
    }
}
