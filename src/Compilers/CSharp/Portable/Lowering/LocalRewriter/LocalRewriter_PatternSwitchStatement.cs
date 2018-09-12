// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            return PatternSwitchLocalRewriter.MakeLoweredForm(this, node);
        }

        /// <summary>
        /// Helper class for rewriting a pattern switch statement by lowering it to a decision tree and
        /// then lowering the decision tree to a sequence of bound statements. We inherit <see cref="DecisionTreeBuilder"/>
        /// for the machinery to build the decision tree, and then use
        /// <see cref="PatternSwitchLocalRewriter.LowerDecisionTree(BoundExpression, DecisionTree)"/>
        /// recursively to produce bound nodes that implement it.
        /// </summary>
        private class PatternSwitchLocalRewriter : DecisionTreeBuilder
        {
            private readonly LocalRewriter _localRewriter;
            private readonly HashSet<LocalSymbol> _declaredTempSet = new HashSet<LocalSymbol>();
            private readonly ArrayBuilder<LocalSymbol> _declaredTemps = ArrayBuilder<LocalSymbol>.GetInstance();
            private readonly SyntheticBoundNodeFactory _factory;

            /// <summary>
            /// Map from switch section's syntax to the lowered code for the section.
            /// </summary>
            private readonly Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchSections = new Dictionary<SyntaxNode, ArrayBuilder<BoundStatement>>();

            private ArrayBuilder<BoundStatement> _loweredDecisionTree = ArrayBuilder<BoundStatement>.GetInstance();

            private PatternSwitchLocalRewriter(LocalRewriter localRewriter, BoundPatternSwitchStatement node)
                : base(localRewriter._factory.CurrentFunction, (SwitchStatementSyntax)node.Syntax, localRewriter._factory.Compilation.Conversions)
            {
                this._localRewriter = localRewriter;
                this._factory = localRewriter._factory;
                this._factory.Syntax = node.Syntax;
                foreach (var section in node.SwitchSections)
                {
                    _switchSections.Add(section.Syntax, ArrayBuilder<BoundStatement>.GetInstance());
                }
            }

            internal static BoundStatement MakeLoweredForm(LocalRewriter localRewriter, BoundPatternSwitchStatement node)
            {
                return new PatternSwitchLocalRewriter(localRewriter, node).MakeLoweredForm(node);
            }

            private BoundStatement MakeLoweredForm(BoundPatternSwitchStatement node)
            {
                var expression = _localRewriter.VisitExpression(node.Expression);
                var result = ArrayBuilder<BoundStatement>.GetInstance();

                // EnC: We need to insert a hidden sequence point to handle function remapping in case
                // the containing method is edited while methods invoked in the expression are being executed.
                if (!node.WasCompilerGenerated && _localRewriter.Instrument)
                {
                    var instrumentedExpression = _localRewriter._instrumenter.InstrumentSwitchStatementExpression(node, expression, _factory);
                    if (expression.ConstantValue == null)
                    {
                        expression = instrumentedExpression;
                    }
                    else
                    {
                        // If the expression is a constant, we leave it alone (the decision tree lowering code needs
                        // to see that constant). But we add an additional leading statement with the instrumented expression.
                        result.Add(_factory.ExpressionStatement(instrumentedExpression));
                    }
                }

                // output the decision tree part
                LowerPatternSwitch(expression, node, result);

                // if the endpoint is reachable, we exit the switch
                if (!node.IsComplete)
                {
                    result.Add(_factory.Goto(node.BreakLabel));
                }
                // at this point the end of result is unreachable.

                _declaredTemps.AddRange(node.InnerLocals);

                // output the sections of code
                foreach (var section in node.SwitchSections)
                {
                    // Lifetime of these locals is expanded to the entire switch body.
                    _declaredTemps.AddRange(section.Locals);

                    // Start with the part of the decision tree that is in scope of the section variables.
                    // Its endpoint is not reachable (it jumps back into the decision tree code).
                    var sectionSyntax = (SyntaxNode)section.Syntax;
                    var sectionBuilder = _switchSections[sectionSyntax];

                    // Add labels corresponding to the labels of the switch section.
                    foreach (var label in section.SwitchLabels)
                    {
                        sectionBuilder.Add(_factory.Label(label.Label));
                    }

                    // Add the translated body of the switch section
                    sectionBuilder.AddRange(_localRewriter.VisitList(section.Statements));

                    sectionBuilder.Add(_factory.Goto(node.BreakLabel));

                    ImmutableArray<BoundStatement> statements = sectionBuilder.ToImmutableAndFree();
                    if (section.Locals.IsEmpty)
                    {
                        result.Add(_factory.StatementList(statements));
                    }
                    else
                    {
                        result.Add(new BoundScope(section.Syntax, section.Locals, statements));
                    }
                    // at this point the end of result is unreachable.
                }

                result.Add(_factory.Label(node.BreakLabel));

                BoundStatement translatedSwitch = _factory.Block(_declaredTemps.ToImmutableArray(), node.InnerLocalFunctions, result.ToImmutableAndFree());

                // Only add instrumentation (such as a sequence point) if the node is not compiler-generated.
                if (!node.WasCompilerGenerated && _localRewriter.Instrument)
                {
                    translatedSwitch = _localRewriter._instrumenter.InstrumentPatternSwitchStatement(node, translatedSwitch);
                }

                return translatedSwitch;
            }

            /// <summary>
            /// Lower the given pattern switch statement into a decision tree and then to a sequence of statements into the given statement builder.
            /// </summary>
            private void LowerPatternSwitch(BoundExpression loweredExpression, BoundPatternSwitchStatement node, ArrayBuilder<BoundStatement> loweredDecisionTree)
            {
                var decisionTree = LowerToDecisionTree(loweredExpression, node);
                LowerDecisionTree(loweredExpression, decisionTree, loweredDecisionTree);
            }

            private DecisionTree LowerToDecisionTree(
                BoundExpression loweredExpression,
                BoundPatternSwitchStatement node)
            {
                var loweredDecisionTree = CreateEmptyDecisionTree(loweredExpression);
                BoundPatternSwitchLabel defaultLabel = null;
                SyntaxNode defaultSection = null;
                foreach (var section in node.SwitchSections)
                {
                    var sectionSyntax = section.Syntax;
                    foreach (var label in section.SwitchLabels)
                    {
                        var loweredLabel = LowerSwitchLabel(label);
                        if (loweredLabel.Syntax.Kind() == SyntaxKind.DefaultSwitchLabel)
                        {
                            if (defaultLabel != null)
                            {
                                // duplicate switch label will have been reported during initial binding.
                            }
                            else
                            {
                                defaultLabel = loweredLabel;
                                defaultSection = sectionSyntax;
                            }
                        }
                        else
                        {
                            AddToDecisionTree(loweredDecisionTree, sectionSyntax, loweredLabel);
                        }
                    }
                }

                if (defaultLabel != null && !loweredDecisionTree.MatchIsComplete)
                {
                    Add(loweredDecisionTree, (e, t) => new DecisionTree.Guarded(
                        expression: loweredExpression,
                        type: loweredExpression.Type,
                        bindings: default,
                        sectionSyntax: defaultSection,
                        guard: null,
                        label: defaultLabel));
                }

                // We discard use-site diagnostics, as they have been reported during initial binding.
                _useSiteDiagnostics.Clear();
                return loweredDecisionTree;
            }

            private BoundPatternSwitchLabel LowerSwitchLabel(BoundPatternSwitchLabel label)
            {
                return label.Update(label.Label, _localRewriter.LowerPattern(label.Pattern), _localRewriter.VisitExpression(label.Guard), label.IsReachable);
            }

            /// <summary>
            /// Lower the given decision tree into the given statement builder.
            /// </summary>
            private void LowerDecisionTree(BoundExpression expression, DecisionTree decisionTree, ArrayBuilder<BoundStatement> loweredDecisionTree)
            {
                // build a decision tree to dispatch the switch statement
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
                        var convertedExpression = _factory.Convert(decisionTree.Expression.Type, expression);
                        _loweredDecisionTree.Add(_factory.Assignment(decisionTree.Expression, convertedExpression));
                    }

                    // If the temp is not yet in the declared temp set, add it now
                    if (_declaredTempSet.Add(decisionTree.Temp))
                    {
                        _declaredTemps.Add(decisionTree.Temp);
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

                // three-valued: true if input known null, false if input known non-null, null if not known.
                bool? inputIsNull = inputConstant?.IsNull;

                var defaultLabel = _factory.GenerateLabel("byTypeDefault");

                if (byType.Type.CanContainNull())
                {
                    switch (inputIsNull)
                    {
                        case true:
                            {
                                // Input is known to be null. Generate code for the null case only.
                                LowerDecisionTree(byType.Expression, byType.WhenNull);
                                if (byType.WhenNull?.MatchIsComplete != true)
                                {
                                    _loweredDecisionTree.Add(_factory.Goto(defaultLabel));
                                }
                                break;
                            }
                        case false:
                            {
                                // Input is known not to be null. Don't generate any code for the null case.
                                break;
                            }
                        case null:
                            {
                                // Unknown if the input is null. First test for null
                                var notNullLabel = _factory.GenerateLabel("notNull");
                                var inputExpression = byType.Expression;
                                var objectType = _factory.SpecialType(SpecialType.System_Object);
                                var nullValue = _factory.Null(objectType);
                                BoundExpression notNull =
                                    byType.Type.IsNullableType()
                                    ? _localRewriter.RewriteNullableNullEquality(
                                            _factory.Syntax,
                                            BinaryOperatorKind.NullableNullNotEqual,
                                            byType.Expression,
                                            nullValue,
                                            _factory.SpecialType(SpecialType.System_Boolean))
                                    : _factory.ObjectNotEqual(nullValue, _factory.Convert(objectType, byType.Expression));
                                _loweredDecisionTree.Add(_factory.ConditionalGoto(notNull, notNullLabel, true));
                                LowerDecisionTree(byType.Expression, byType.WhenNull);
                                if (byType.WhenNull?.MatchIsComplete != true)
                                {
                                    _loweredDecisionTree.Add(_factory.Goto(defaultLabel));
                                }

                                _loweredDecisionTree.Add(_factory.Label(notNullLabel));
                                break;
                            }
                    }

                }
                else
                {
                    Debug.Assert(byType.WhenNull == null);
                }

                if (inputIsNull != true)
                {
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
                }

                // finally, the default for when no type matches
                _loweredDecisionTree.Add(_factory.Label(defaultLabel));
                LowerDecisionTree(byType.Expression, byType.Default);
            }

            private BoundExpression TypeTestAndCopyToTemp(BoundExpression input, BoundExpression temp)
            {
                // invariant: the input has already been tested, to ensure it is not null
                if (input == temp)
                {
                    // this may not be reachable due to https://github.com/dotnet/roslyn/issues/16878
                    return _factory.Literal(true);
                }

                Debug.Assert(temp.Kind == BoundKind.Local);
                return _localRewriter.MakeIsDeclarationPattern(_factory.Syntax, input, temp, requiresNullTest: false);
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

                // If there is a matching value among the cases, that is the only one lowered.
                if (byValue.ValueAndDecision.TryGetValue(value, out DecisionTree valueDecision))
                {
                    LowerDecisionTree(byValue.Expression, valueDecision);
                    if (valueDecision.MatchIsComplete)
                    {
                        return;
                    }
                }

                LowerDecisionTree(byValue.Expression, byValue.Default);
            }

            /// <summary>
            /// Add a branch in the lowered decision tree to a label for a matched
            /// pattern, and then produce a statement for the target of that branch
            /// that binds the pattern variables.
            /// </summary>
            /// <param name="bindings">The source/destination pairs for the assignments</param>
            /// <param name="addBindings">A builder to which the label and binding assignments are added</param>
            private void AddBindingsForCase(
                ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>> bindings,
                ArrayBuilder<BoundStatement> addBindings)
            {
                var patternMatched = _factory.GenerateLabel("patternMatched");
                _loweredDecisionTree.Add(_factory.Goto(patternMatched));

                // Hide the code that binds pattern variables in a hidden sequence point
                addBindings.Add(_factory.HiddenSequencePoint());
                addBindings.Add(_factory.Label(patternMatched));
                if (!bindings.IsDefaultOrEmpty)
                {
                    foreach (var kv in bindings)
                    {
                        var loweredRight = kv.Key;
                        var loweredLeft = kv.Value;
                        Debug.Assert(loweredLeft.Type.Equals(loweredRight.Type, TypeCompareKind.AllIgnoreOptions));
                        addBindings.Add(_factory.ExpressionStatement(
                            _localRewriter.MakeStaticAssignmentOperator(
                                _factory.Syntax, loweredLeft, loweredRight, isRef: false, type: loweredLeft.Type, used: false)));
                    }
                }
            }

            private void LowerDecisionTree(DecisionTree.Guarded guarded)
            {
                var sectionBuilder = this._switchSections[guarded.SectionSyntax];
                var targetLabel = guarded.Label.Label;
                Debug.Assert(guarded.Guard?.ConstantValue != ConstantValue.False);
                if (guarded.Guard == null || guarded.Guard.ConstantValue == ConstantValue.True)
                {
                    // unconditional
                    Debug.Assert(guarded.Default == null);
                    if (guarded.Bindings.IsDefaultOrEmpty)
                    {
                        _loweredDecisionTree.Add(_factory.Goto(targetLabel));
                    }
                    else
                    {
                        // with bindings
                        AddBindingsForCase(guarded.Bindings, sectionBuilder);
                        sectionBuilder.Add(_factory.Goto(targetLabel));
                    }
                }
                else
                {
                    AddBindingsForCase(guarded.Bindings, sectionBuilder);
                    var guardTest = _factory.ConditionalGoto(guarded.Guard, targetLabel, true);

                    // Only add instrumentation (such as a sequence point) if the node is not compiler-generated.
                    if (!guarded.Guard.WasCompilerGenerated && _localRewriter.Instrument)
                    {
                        guardTest = _localRewriter._instrumenter.InstrumentPatternSwitchWhenClauseConditionalGotoBody(guarded.Guard, guardTest);
                    }

                    sectionBuilder.Add(guardTest);

                    var guardFailed = _factory.GenerateLabel("guardFailed");
                    sectionBuilder.Add(_factory.Goto(guardFailed));
                    _loweredDecisionTree.Add(_factory.Label(guardFailed));

                    LowerDecisionTree(guarded.Expression, guarded.Default);
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

                    var section = new BoundSwitchSection(_factory.Syntax, locals: ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create(switchLabel), forValue.ToImmutableAndFree());
                    switchSections.Add(section);
                }

                var rewrittenSections = switchSections.ToImmutableAndFree();
                MethodSymbol stringEquality = null;
                if (underlyingSwitchType.SpecialType == SpecialType.System_String)
                {
                    _localRewriter.EnsureStringHashFunction(rewrittenSections, _factory.Syntax);
                    stringEquality = _localRewriter.UnsafeGetSpecialTypeMethod(_factory.Syntax, SpecialMember.System_String__op_Equality);
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
                        var literal = _localRewriter.MakeLiteral(_factory.Syntax, boundSwitchLabel.ConstantValueOpt, expression.Type);
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
                    // this location in the generated code in builder should not be reachable.
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
