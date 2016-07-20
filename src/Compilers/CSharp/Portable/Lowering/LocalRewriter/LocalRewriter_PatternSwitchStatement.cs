// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            _factory.Syntax = node.Syntax;
            var pslr = new PatternSwitchLocalRewriter(this, node);
            var expression = VisitExpression(node.Expression);
            var result = ArrayBuilder<BoundStatement>.GetInstance();

            // output the decision tree part
            pslr.LowerDecisionTree(expression, node.DecisionTree, result);

            // if the endpoint is reachable, we exit the switch
            if (!node.DecisionTree.MatchIsComplete)
            {
                result.Add(_factory.Goto(node.BreakLabel));
            }
            // at this point the end of result is unreachable.

            // output the sections of code
            foreach (var section in node.SwitchSections)
            {
                // Start with the part of the decision tree that is in scope of the section variables.
                // Its endpoint is not reachable (it jumps back into the decision tree code).
                var sectionBuilder = pslr.SwitchSections[section];

                // Add labels corresponding to the labels of the switch section.
                foreach (var label in section.SwitchLabels)
                {
                    sectionBuilder.Add(_factory.Label(label.Label));
                }

                // Add the translated body of the switch section
                sectionBuilder.AddRange(VisitList(section.Statements));
                sectionBuilder.Add(_factory.Goto(node.BreakLabel));
                result.Add(_factory.Block(section.Locals, sectionBuilder.ToImmutableAndFree()));
                // at this point the end of result is unreachable.
            }

            result.Add(_factory.Label(node.BreakLabel));
            var translatedSwitch = _factory.Block(pslr.DeclaredTemps.ToImmutableArray().Concat(node.InnerLocals), node.InnerLocalFunctions, result.ToImmutableAndFree());
            return translatedSwitch;
        }

        private class PatternSwitchLocalRewriter
        {
            public readonly LocalRewriter LocalRewriter;
            public readonly HashSet<LocalSymbol> DeclaredTempSet = new HashSet<LocalSymbol>();
            public readonly ArrayBuilder<LocalSymbol> DeclaredTemps = ArrayBuilder<LocalSymbol>.GetInstance();
            public readonly Dictionary<BoundPatternSwitchSection, ArrayBuilder<BoundStatement>> SwitchSections = new Dictionary<BoundPatternSwitchSection, ArrayBuilder<BoundStatement>>();

            private ArrayBuilder<BoundStatement> _loweredDecisionTree = ArrayBuilder<BoundStatement>.GetInstance();
            private readonly SyntheticBoundNodeFactory _factory;

            public PatternSwitchLocalRewriter(LocalRewriter localRewriter, BoundPatternSwitchStatement node)
            {
                this.LocalRewriter = localRewriter;
                this._factory = localRewriter._factory;
                foreach (var section in node.SwitchSections)
                {
                    SwitchSections.Add(section, ArrayBuilder<BoundStatement>.GetInstance());
                }
            }

            /// <summary>
            /// Lower the given decision tree into the given statement builder.
            /// </summary>
            public void LowerDecisionTree(BoundExpression expression, DecisionTree decisionTree, ArrayBuilder<BoundStatement> loweredDecisionTree)
            {
                var oldLoweredDecisionTree = this._loweredDecisionTree;
                this._loweredDecisionTree = loweredDecisionTree;
                LowerDecisionTree(expression, decisionTree);
                this._loweredDecisionTree = oldLoweredDecisionTree;
            }

            private void LowerDecisionTree(BoundExpression expression, DecisionTree decisionTree)
            {
                if (decisionTree == null)
                {
                    return;
                }

                // If the input expression was a constant or a simple read of a local, then that is the
                // decision tree's expression. Otherwise it is a newly created temp, to which we must
                // assign the switch expression.
                if (decisionTree.Temp != null)
                {
                    // Store the input expression into a temp
                    if (decisionTree.Expression != expression)
                    {
                        _loweredDecisionTree.Add(_factory.Assignment(decisionTree.Expression, expression));
                    }

                    if (DeclaredTempSet.Add(decisionTree.Temp))
                    {
                        DeclaredTemps.Add(decisionTree.Temp);
                    }
                    else
                    {
                        // we should only attempt to declare each temp once.
                        throw ExceptionUtilities.Unreachable;
                    }
                }

                switch (decisionTree.Kind)
                {
                    case DecisionTree.DecisionKind.ByType:
                        {
                            LowerDecisionTree((DecisionTree.ByType)decisionTree);
                            return;
                        }
                    case DecisionTree.DecisionKind.ByValue:
                        {
                            LowerDecisionTree((DecisionTree.ByValue)decisionTree);
                            return;
                        }
                    case DecisionTree.DecisionKind.Guarded:
                        {
                            LowerDecisionTree((DecisionTree.Guarded)decisionTree);
                            return;
                        }
                    default:
                        throw ExceptionUtilities.UnexpectedValue(decisionTree.Kind);
                }
            }

            private void LowerDecisionTree(DecisionTree.ByType byType)
            {
                var inputConstant = byType.Expression.ConstantValue;
                if (inputConstant != null)
                {
                    if (inputConstant.IsNull)
                    {
                        // input is the constant null
                        LowerDecisionTree(byType.Expression, byType.WhenNull);
                        if (byType.WhenNull?.MatchIsComplete != true)
                        {
                            LowerDecisionTree(byType.Expression, byType.Default);
                        }
                    }
                    else
                    {
                        // input is a non-null constant
                        foreach (var kvp in byType.TypeAndDecision)
                        {
                            LowerDecisionTree(byType.Expression, kvp.Value);
                            if (kvp.Value.MatchIsComplete)
                            {
                                return;
                            }
                        }

                        LowerDecisionTree(byType.Expression, byType.Default);
                    }
                }
                else
                {
                    var defaultLabel = _factory.GenerateLabel("byTypeDefault");

                    // input is not a constant
                    if (byType.Type.CanBeAssignedNull())
                    {
                        // first test for null
                        var notNullLabel = _factory.GenerateLabel("notNull");
                        var inputExpression = byType.Expression;
                        var nullValue = _factory.Null(byType.Type);
                        BoundExpression notNull = byType.Type.IsNullableType()
                            ? LocalRewriter.RewriteNullableNullEquality(_factory.Syntax, BinaryOperatorKind.NullableNullNotEqual, byType.Expression, nullValue, _factory.SpecialType(SpecialType.System_Boolean))
                            : _factory.ObjectNotEqual(byType.Expression, nullValue);
                        _loweredDecisionTree.Add(_factory.ConditionalGoto(notNull, notNullLabel, true));
                        LowerDecisionTree(byType.Expression, byType.WhenNull);
                        if (byType.WhenNull?.MatchIsComplete != true)
                        {
                            _loweredDecisionTree.Add(_factory.Goto(defaultLabel));
                        }

                        _loweredDecisionTree.Add(_factory.Label(notNullLabel));
                    }
                    else
                    {
                        Debug.Assert(byType.WhenNull == null);
                    }

                    foreach (var td in byType.TypeAndDecision)
                    {
                        // then test for each type, sequentially
                        var type = td.Key;
                        var decision = td.Value;
                        var failLabel = _factory.GenerateLabel("failedDecision");
                        var testAndCopy = TypeTestAndCopyToTemp(byType.Expression, decision.Expression);
                        _loweredDecisionTree.Add(_factory.ConditionalGoto(testAndCopy, failLabel, false));
                        LowerDecisionTree(decision.Expression, decision);
                        _loweredDecisionTree.Add(_factory.Label(failLabel));
                    }

                    // finally, the default for when no type matches
                    _loweredDecisionTree.Add(_factory.Label(defaultLabel));
                    LowerDecisionTree(byType.Expression, byType.Default);
                }
            }

            private BoundExpression TypeTestAndCopyToTemp(BoundExpression input, BoundExpression temp)
            {
                // invariant: the input has already been tested, to ensure it is not null
                if (input == temp)
                {
                    return _factory.Literal(true);
                }

                Debug.Assert(temp.Kind == BoundKind.Local);
                return LocalRewriter.MakeDeclarationPattern(_factory.Syntax, input, ((BoundLocal)temp).LocalSymbol, requiresNullTest: false);
            }

            private void LowerDecisionTree(DecisionTree.ByValue byValue)
            {
                if (byValue.Expression.ConstantValue != null)
                {
                    LowerConstantValueDecision(byValue);
                    return;
                }

                if (byValue.ValueAndDecision.Count == 0)
                {
                    LowerDecisionTree(byValue.Expression, byValue.Default);
                    return;
                }

                if (byValue.Type.SpecialType == SpecialType.System_Boolean)
                {
                    LowerBooleanSwitch(byValue);
                }
                else
                {
                    LowerBasicSwitch(byValue);
                }
            }

            private void LowerConstantValueDecision(DecisionTree.ByValue byValue)
            {
                var value = byValue.Expression.ConstantValue.Value;
                Debug.Assert(value != null);
                DecisionTree onValue;
                if (byValue.ValueAndDecision.TryGetValue(value, out onValue))
                {
                    LowerDecisionTree(byValue.Expression, onValue);
                    if (onValue.MatchIsComplete)
                    {
                        return;
                    }
                }

                LowerDecisionTree(byValue.Expression, byValue.Default);
            }

            private void LowerDecisionTree(DecisionTree.Guarded guarded)
            {
                var sectionBuilder = this.SwitchSections[guarded.Section];
                var targetLabel = guarded.Label.Label;
                Debug.Assert(guarded.Guard?.ConstantValue != ConstantValue.False);
                if (guarded.Guard == null || guarded.Guard.ConstantValue == ConstantValue.True)
                {
                    // unconditional
                    if (guarded.Bindings.IsDefaultOrEmpty)
                    {
                        _loweredDecisionTree.Add(_factory.Goto(targetLabel));
                    }
                    else
                    {
                        // with bindings
                        var matched = _factory.GenerateLabel("matched");
                        _loweredDecisionTree.Add(_factory.Goto(matched));
                        sectionBuilder.Add(_factory.Label(matched));
                        AddBindings(sectionBuilder, guarded.Bindings);
                        sectionBuilder.Add(_factory.Goto(targetLabel));
                    }
                }
                else
                {
                    var checkGuard = _factory.GenerateLabel("checkGuard");
                    _loweredDecisionTree.Add(_factory.Goto(checkGuard));
                    sectionBuilder.Add(_factory.Label(checkGuard));
                    AddBindings(sectionBuilder, guarded.Bindings);
                    sectionBuilder.Add(_factory.ConditionalGoto(LocalRewriter.VisitExpression(guarded.Guard), targetLabel, true));
                    var guardFailed = _factory.GenerateLabel("guardFailed");
                    sectionBuilder.Add(_factory.Goto(guardFailed));
                    _loweredDecisionTree.Add(_factory.Label(guardFailed));
                }
            }

            private void AddBindings(ArrayBuilder<BoundStatement> sectionBuilder, ImmutableArray<KeyValuePair<BoundExpression, LocalSymbol>> bindings)
            {
                if (bindings.IsDefaultOrEmpty)
                {
                    return;
                }

                foreach (var kv in bindings)
                {
                    var source = kv.Key;
                    var dest = kv.Value;
                    sectionBuilder.Add(_factory.Assignment(_factory.Local(dest), source));
                }
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
            //
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
            private void LowerBasicSwitch(DecisionTree.ByValue byValue)
            {
                var switchSections = ArrayBuilder<BoundSwitchSection>.GetInstance();
                var noValueMatches = _factory.GenerateLabel("noValueMatches");
                var underlyingSwitchType = byValue.Type.IsEnumType() ? byValue.Type.GetEnumUnderlyingType() : byValue.Type;
                foreach (var vd in byValue.ValueAndDecision)
                {
                    var value = vd.Key;
                    var decision = vd.Value;
                    var constantValue = ConstantValue.Create(value, underlyingSwitchType.SpecialType);
                    var constantExpression = new BoundLiteral(_factory.Syntax, constantValue, underlyingSwitchType);
                    var label = _factory.GenerateLabel("case+" + value);
                    var switchLabel = new BoundSwitchLabel(_factory.Syntax, label, constantExpression, constantValue);
                    var forValue = ArrayBuilder<BoundStatement>.GetInstance();
                    LowerDecisionTree(byValue.Expression, decision, forValue);
                    if (!decision.MatchIsComplete)
                    {
                        forValue.Add(_factory.Goto(noValueMatches));
                    }

                    var section = new BoundSwitchSection(_factory.Syntax, ImmutableArray.Create(switchLabel), forValue.ToImmutableAndFree());
                    switchSections.Add(section);
                }

                var rewrittenSections = switchSections.ToImmutableAndFree();
                MethodSymbol stringEquality = null;
                if (underlyingSwitchType.SpecialType == SpecialType.System_String)
                {
                    LocalRewriter.EnsureStringHashFunction(rewrittenSections, _factory.Syntax);
                    stringEquality = LocalRewriter.GetSpecialTypeMethod(_factory.Syntax, SpecialMember.System_String__op_Equality);
                }

                // The BoundSwitchStatement requires a constant target when there are no sections, so we accomodate that here.
                var constantTarget = rewrittenSections.IsEmpty ? noValueMatches : null;
                var switchStatement = new BoundSwitchStatement(
                    _factory.Syntax, null, _factory.Convert(underlyingSwitchType, byValue.Expression),
                    constantTarget,
                    ImmutableArray<LocalSymbol>.Empty, ImmutableArray<LocalFunctionSymbol>.Empty,
                    rewrittenSections, noValueMatches, stringEquality);
                // The bound switch statement implicitly defines the label noValueMatches at the end, so we do not add it explicitly.

                switch (underlyingSwitchType.SpecialType)
                {
                    case SpecialType.System_Boolean:
                        // boolean switch is handled in LowerBooleanSwitch, not here.
                        throw ExceptionUtilities.Unreachable;

                    case SpecialType.System_String:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Char:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_SByte:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                        {
                            // emit knows how to efficiently generate code for these kinds of switches.
                            _loweredDecisionTree.Add(switchStatement);
                            break;
                        }

                    default:
                        {
                            // other types, such as float, double, and decimal, are not currently
                            // handled by emit and must be lowered here.
                            _loweredDecisionTree.Add(LowerNonprimitiveSwitch(switchStatement));
                            break;
                        }
                }

                LowerDecisionTree(byValue.Expression, byValue.Default);
            }

            private BoundStatement LowerNonprimitiveSwitch(BoundSwitchStatement switchStatement)
            {
                // Here we handle "other" types, such as float, double, and decimal, by
                // lowering the BoundSwitchStatement.
                // We compare the constant values using value.Equals(input), using ordinary
                // overload resolution. Note that we cannot and do not rely on switching
                // on the hash code, as it may not be consistent with the behavior of Equals;
                // see https://github.com/dotnet/coreclr/issues/6237. Also, the hash code is
                // not guaranteed to be the same on the compilation platform as the runtime
                // platform.
                // CONSIDER: can we improve the quality of the code using comparisons, like
                //           we do for other numeric types, by generating a series of tests
                //           that use divide-and-conquer to efficiently find a matching value?
                //           If so, we should use the BoundSwitchStatement and do that in emit.
                //           Moreover, we should be able to use `==` rather than `.Equals`
                //           for cases (such as non-NaN) where we know the result to be the same.
                var rewrittenSections = switchStatement.SwitchSections;
                var expression = switchStatement.Expression;
                var noValueMatches = switchStatement.BreakLabel;
                Debug.Assert(switchStatement.LoweredPreambleOpt == null);
                Debug.Assert(switchStatement.InnerLocals.IsDefaultOrEmpty);
                Debug.Assert(switchStatement.InnerLocalFunctions.IsDefaultOrEmpty);
                Debug.Assert(switchStatement.StringEquality == null);
                LabelSymbol nextLabel = null;
                var builder = ArrayBuilder<BoundStatement>.GetInstance();
                foreach (var section in rewrittenSections)
                {
                    foreach (var boundSwitchLabel in section.SwitchLabels)
                    {
                        if (nextLabel != null)
                        {
                            builder.Add(_factory.Label(nextLabel));
                        }
                        nextLabel = _factory.GenerateLabel("failcase+" + section.SwitchLabels[0].ConstantValueOpt.Value);

                        Debug.Assert(boundSwitchLabel.ConstantValueOpt != null);
                        // generate (if (value.Equals(input)) goto label;
                        var literal = LocalRewriter.MakeLiteral(_factory.Syntax, boundSwitchLabel.ConstantValueOpt, expression.Type);
                        var condition = _factory.InstanceCall(literal, "Equals", expression);
                        if (!condition.HasErrors && condition.Type.SpecialType != SpecialType.System_Boolean)
                        {
                            var call = (BoundCall)condition;
                            // '{1} {0}' has the wrong return type
                            _factory.Diagnostics.Add(ErrorCode.ERR_BadRetType, boundSwitchLabel.Syntax.GetLocation(), call.Method, call.Type);
                        }

                        builder.Add(_factory.ConditionalGoto(condition, boundSwitchLabel.Label, true));
                        builder.Add(_factory.Goto(nextLabel));
                    }

                    foreach (var boundSwitchLabel in section.SwitchLabels)
                    {
                        builder.Add(_factory.Label(boundSwitchLabel.Label));
                    }

                    builder.Add(_factory.Block(section.Statements));
                    // this location should not be reachable.
                }

                Debug.Assert(nextLabel != null);
                builder.Add(_factory.Label(nextLabel));
                builder.Add(_factory.Label(noValueMatches));
                return _factory.Block(builder.ToImmutableAndFree());
            }

            private void LowerBooleanSwitch(DecisionTree.ByValue byValue)
            {
                switch (byValue.ValueAndDecision.Count)
                {
                    case 0:
                        {
                            // this should have been handled in the caller.
                            throw ExceptionUtilities.Unreachable;
                        }
                    case 1:
                        {
                            DecisionTree decision;
                            bool onBoolean = byValue.ValueAndDecision.TryGetValue(true, out decision);
                            if (!onBoolean)
                            {
                                byValue.ValueAndDecision.TryGetValue(false, out decision);
                            }

                            Debug.Assert(decision != null);
                            var onOther = _factory.GenerateLabel("on" + !onBoolean);
                            _loweredDecisionTree.Add(_factory.ConditionalGoto(byValue.Expression, onOther, !onBoolean));
                            LowerDecisionTree(byValue.Expression, decision);
                            // if we fall through here, that means the match was not complete and we invoke the default part
                            _loweredDecisionTree.Add(_factory.Label(onOther));
                            LowerDecisionTree(byValue.Expression, byValue.Default);
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
                            _loweredDecisionTree.Add(_factory.ConditionalGoto(byValue.Expression, onFalse, false));
                            LowerDecisionTree(byValue.Expression, trueDecision);
                            _loweredDecisionTree.Add(_factory.Goto(tryAnother));
                            _loweredDecisionTree.Add(_factory.Label(onFalse));
                            LowerDecisionTree(byValue.Expression, falseDecision);
                            _loweredDecisionTree.Add(_factory.Label(tryAnother));
                            // if both true and false (i.e. all values) are fully handled, there should be no default.
                            Debug.Assert(!trueDecision.MatchIsComplete || !falseDecision.MatchIsComplete || byValue.Default == null);
                            LowerDecisionTree(byValue.Expression, byValue.Default);
                            break;
                        }
                    default:
                        throw ExceptionUtilities.UnexpectedValue(byValue.ValueAndDecision.Count);
                }
            }
        }
    }
}
