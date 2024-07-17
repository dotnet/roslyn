// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntheticBoundNodeFactory;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        /// <summary>
        /// A common base class for lowering a decision dag.
        /// </summary>
        private abstract partial class DecisionDagRewriter : PatternLocalRewriter
        {
            /// <summary>
            /// Get the builder for code in the given section of the switch.
            /// For an is-pattern expression, this is a singleton.
            /// </summary>
            protected abstract ArrayBuilder<BoundStatement> BuilderForSection(SyntaxNode section);

            /// <summary>
            /// The lowered decision dag. This includes all of the code to decide which pattern
            /// is matched, but not the code to assign to pattern variables and evaluate when clauses.
            /// </summary>
            private ArrayBuilder<BoundStatement> _loweredDecisionDag;

            /// <summary>
            /// The label in the code for the beginning of code for each node of the dag.
            /// </summary>
            private readonly PooledDictionary<BoundDecisionDagNode, LabelSymbol> _dagNodeLabels = PooledDictionary<BoundDecisionDagNode, LabelSymbol>.GetInstance();

#nullable enable
            // When different branches of the DAG share `when` expressions, the
            // shared expression will be lowered as a shared section and the `when` nodes that need
            // to will jump there. After the expression is evaluated, we need to jump to different
            // labels depending on the `when` node we came from. To achieve that, each `when` node
            // gets an identifier and sets a local before jumping into the shared `when` expression.
            internal LocalSymbol? _whenNodeIdentifierLocal;
#nullable disable

            protected DecisionDagRewriter(
                SyntaxNode node,
                LocalRewriter localRewriter,
                bool generateInstrumentation)
                : base(node, localRewriter, generateInstrumentation)
            {
            }

            private void ComputeLabelSet(BoundDecisionDag decisionDag)
            {
                // Nodes with more than one predecessor are assigned a label
                var hasPredecessor = PooledHashSet<BoundDecisionDagNode>.GetInstance();
                foreach (BoundDecisionDagNode node in decisionDag.TopologicallySortedNodes)
                {
                    switch (node)
                    {
                        case BoundWhenDecisionDagNode w:
                            GetDagNodeLabel(node);
                            if (w.WhenFalse != null)
                            {
                                GetDagNodeLabel(w.WhenFalse);
                            }
                            break;
                        case BoundLeafDecisionDagNode d:
                            // Leaf can branch directly to the target
                            _dagNodeLabels[node] = d.Label;
                            break;
                        case BoundEvaluationDecisionDagNode e:
                            notePredecessor(e.Next);
                            break;
                        case BoundTestDecisionDagNode p:
                            notePredecessor(p.WhenTrue);
                            notePredecessor(p.WhenFalse);
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(node.Kind);
                    }
                }

                hasPredecessor.Free();
                return;

                void notePredecessor(BoundDecisionDagNode successor)
                {
                    if (successor != null && !hasPredecessor.Add(successor))
                    {
                        GetDagNodeLabel(successor);
                    }
                }
            }

            protected new void Free()
            {
                _dagNodeLabels.Free();
                base.Free();
            }

            protected virtual LabelSymbol GetDagNodeLabel(BoundDecisionDagNode dag)
            {
                if (!_dagNodeLabels.TryGetValue(dag, out LabelSymbol label))
                {
                    _dagNodeLabels.Add(dag, label = dag is BoundLeafDecisionDagNode d ? d.Label : _factory.GenerateLabel("dagNode"));
                }

                return label;
            }

            /// <summary>
            /// A utility class that is used to scan a when clause to determine if it might assign a pattern variable
            /// declared in that case, directly or indirectly. Used to determine if we can skip the allocation of
            /// pattern-matching temporary variables and use user-declared pattern variables instead, because we can
            /// conclude that they are not mutated by a when clause while the pattern-matching automaton is running.
            /// </summary>
            protected sealed class WhenClauseMightAssignPatternVariableWalker : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
            {
                private bool _mightAssignSomething;

                public bool MightAssignSomething(BoundExpression expr)
                {
                    if (expr == null)
                        return false;

                    this._mightAssignSomething = false;
                    this.Visit(expr);
                    return this._mightAssignSomething;
                }

                public override BoundNode Visit(BoundNode node)
                {
                    // A constant expression cannot mutate anything
                    if (node is BoundExpression { ConstantValueOpt: { } })
                        return null;

                    // Stop visiting once we determine something might get assigned
                    return this._mightAssignSomething ? null : base.Visit(node);
                }

                protected override void VisitArguments(BoundCall node)
                {
                    bool mightMutate =
                        // might be a call to a local function that assigns something
                        node.Method.MethodKind == MethodKind.LocalFunction ||
                        // or perhaps we are passing a variable by ref and mutating it that way, e.g. `int.Parse(..., out x)`
                        !node.ArgumentRefKindsOpt.IsDefault ||
                        // or perhaps we are calling a mutating method of a value type
                        MethodMayMutateReceiver(node.ReceiverOpt, node.Method);

                    if (mightMutate)
                        _mightAssignSomething = true;
                    else
                        base.VisitArguments(node);
                }

                private static bool MethodMayMutateReceiver(BoundExpression receiver, MethodSymbol method)
                {
                    return
                        method != null &&
                        !method.IsStatic &&
                        !method.IsEffectivelyReadOnly &&
                        receiver.Type?.IsReferenceType == false &&
                        // methods of primitive types do not mutate their receiver
                        !method.ContainingType.SpecialType.IsPrimitiveRecursiveStruct();
                }

                public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
                {
                    bool mightMutate =
                        // We only need to check the get accessor because an assignment would cause _mightAssignSomething to be set to true in the caller
                        MethodMayMutateReceiver(node.ReceiverOpt, node.PropertySymbol.GetMethod);

                    if (mightMutate)
                        _mightAssignSomething = true;
                    else
                        base.VisitPropertyAccess(node);

                    return null;
                }

                public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitConversion(BoundConversion node)
                {
                    visitConversion(node.Conversion);
                    if (!_mightAssignSomething)
                        base.VisitConversion(node);
                    return null;

                    void visitConversion(Conversion conversion)
                    {
                        switch (conversion.Kind)
                        {
                            case ConversionKind.MethodGroup:
                                if (conversion.Method.MethodKind == MethodKind.LocalFunction)
                                {
                                    _mightAssignSomething = true;
                                }
                                break;
                            default:
                                if (!conversion.UnderlyingConversions.IsDefault)
                                {
                                    conversion.AssertUnderlyingConversionsChecked();
                                    foreach (var underlying in conversion.UnderlyingConversions)
                                    {
                                        visitConversion(underlying);
                                        if (_mightAssignSomething)
                                            return;
                                    }
                                }
                                break;
                        }
                    }
                }

                public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
                {
                    bool mightMutate =
                        node.MethodOpt?.MethodKind == MethodKind.LocalFunction;

                    if (mightMutate)
                        _mightAssignSomething = true;
                    else
                        base.VisitDelegateCreationExpression(node);

                    return null;
                }

                public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
                {
                    _mightAssignSomething = true;
                    return null;
                }

                public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
                {
                    // perhaps we are passing a variable by ref and mutating it that way
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitDynamicInvocation(node);

                    return null;
                }

                public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
                {
                    // perhaps we are passing a variable by ref and mutating it that way
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitObjectCreationExpression(node);

                    return null;
                }

                public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
                {
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitDynamicObjectCreationExpression(node);

                    return null;
                }

                public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
                {
                    // Although ref indexers are not declarable in C#, they may be usable
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitObjectInitializerMember(node);

                    return null;
                }

                public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
                {
                    bool mightMutate =
                        !node.ArgumentRefKindsOpt.IsDefault ||
                        // We only need to check the get accessor because an assignment would cause _mightAssignSomething to be set to true in the caller
                        MethodMayMutateReceiver(node.ReceiverOpt, node.Indexer.GetMethod);

                    if (mightMutate)
                        _mightAssignSomething = true;
                    else
                        base.VisitIndexerAccess(node);

                    return null;
                }

                public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
                {
                    if (!node.ArgumentRefKindsOpt.IsDefault)
                        _mightAssignSomething = true;
                    else
                        base.VisitDynamicIndexerAccess(node);

                    return null;
                }
            }

            protected BoundDecisionDag ShareTempsIfPossibleAndEvaluateInput(
                BoundDecisionDag decisionDag,
                BoundExpression loweredSwitchGoverningExpression,
                ArrayBuilder<BoundStatement> result,
                out BoundExpression savedInputExpression)
            {
                // Note that a when-clause can contain an assignment to a
                // pattern variable declared in a different when-clause (e.g. in the same section, or
                // in a different section via the use of a local function), so we need to analyze all
                // of the when clauses to see if they are all simple enough to conclude that they do
                // not mutate pattern variables.
                var mightAssignWalker = new WhenClauseMightAssignPatternVariableWalker();
                bool canShareTemps =
                    !decisionDag.TopologicallySortedNodes
                    .Any(predicate: static (node, mightAssignWalker) => node is BoundWhenDecisionDagNode w && mightAssignWalker.MightAssignSomething(w.WhenExpression), arg: mightAssignWalker);

                if (canShareTemps)
                {
                    decisionDag = ShareTempsAndEvaluateInput(loweredSwitchGoverningExpression, decisionDag, expr => result.Add(_factory.ExpressionStatement(expr)), out savedInputExpression);
                }
                else
                {
                    // assign the input expression to its temp.
                    BoundExpression inputTemp = _tempAllocator.GetTemp(BoundDagTemp.ForOriginalInput(loweredSwitchGoverningExpression));
                    Debug.Assert(inputTemp != loweredSwitchGoverningExpression);
                    result.Add(_factory.Assignment(inputTemp, loweredSwitchGoverningExpression));
                    savedInputExpression = inputTemp;
                }

                return decisionDag;
            }

            protected ImmutableArray<BoundStatement> LowerDecisionDagCore(BoundDecisionDag decisionDag)
            {
                _loweredDecisionDag = ArrayBuilder<BoundStatement>.GetInstance();
                ComputeLabelSet(decisionDag);
                ImmutableArray<BoundDecisionDagNode> sortedNodes = decisionDag.TopologicallySortedNodes;
                var firstNode = sortedNodes[0];
                switch (firstNode)
                {
                    case BoundWhenDecisionDagNode _:
                    case BoundLeafDecisionDagNode _:
                        // If the first node is a leaf or when clause rather than the code for the
                        // lowered decision dag, jump there to start.
                        _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(firstNode)));
                        break;
                }

                // Code for each when clause goes in the separate code section for its switch section.
                LowerWhenClauses(sortedNodes);

                ImmutableArray<BoundDecisionDagNode> nodesToLower = sortedNodes.WhereAsArray(n => n.Kind != BoundKind.WhenDecisionDagNode && n.Kind != BoundKind.LeafDecisionDagNode);
                var loweredNodes = PooledHashSet<BoundDecisionDagNode>.GetInstance();
                for (int i = 0, length = nodesToLower.Length; i < length; i++)
                {
                    BoundDecisionDagNode node = nodesToLower[i];
                    // A node may have been lowered as part of a switch dispatch, but if it had a label, we'll need to lower it individually as well
                    bool alreadyLowered = loweredNodes.Contains(node);
                    if (alreadyLowered && !_dagNodeLabels.TryGetValue(node, out _))
                    {
                        continue;
                    }

                    if (_dagNodeLabels.TryGetValue(node, out LabelSymbol label))
                    {
                        _loweredDecisionDag.Add(_factory.Label(label));
                    }

                    // If we can generate an IL switch instruction, do so
                    if (!alreadyLowered && GenerateSwitchDispatch(node, loweredNodes))
                    {
                        continue;
                    }

                    // If we can generate a type test and cast more efficiently as an `is` followed by a null check, do so
                    if (GenerateTypeTestAndCast(node, loweredNodes, nodesToLower, i))
                    {
                        continue;
                    }

                    // We pass the node that will follow so we can permit a test to fall through if appropriate
                    BoundDecisionDagNode nextNode = ((i + 1) < length) ? nodesToLower[i + 1] : null;
                    if (nextNode != null && loweredNodes.Contains(nextNode))
                    {
                        nextNode = null;
                    }

                    LowerDecisionDagNode(node, nextNode);
                }

                loweredNodes.Free();
                var result = _loweredDecisionDag.ToImmutableAndFree();
                _loweredDecisionDag = null;
                return result;
            }

            /// <summary>
            /// If we have a type test followed by a cast to that type, and the types are reference types,
            /// then we can replace the pair of them by a conversion using `as` and a null check.
            /// </summary>
            /// <returns>true if we generated code for the test</returns>
            private bool GenerateTypeTestAndCast(
                BoundDecisionDagNode node,
                HashSet<BoundDecisionDagNode> loweredNodes,
                ImmutableArray<BoundDecisionDagNode> nodesToLower,
                int indexOfNode)
            {
                Debug.Assert(node == nodesToLower[indexOfNode]);
                if (node is BoundTestDecisionDagNode testNode &&
                    testNode.WhenTrue is BoundEvaluationDecisionDagNode evaluationNode &&
                    TryLowerTypeTestAndCast(testNode.Test, evaluationNode.Evaluation, out BoundExpression sideEffect, out BoundExpression test)
                    )
                {
                    var whenTrue = evaluationNode.Next;
                    var whenFalse = testNode.WhenFalse;
                    bool canEliminateEvaluationNode = !this._dagNodeLabels.ContainsKey(evaluationNode);

                    if (canEliminateEvaluationNode)
                        loweredNodes.Add(evaluationNode);

                    var nextNode =
                        (indexOfNode + 2 < nodesToLower.Length) &&
                        canEliminateEvaluationNode &&
                        nodesToLower[indexOfNode + 1] == evaluationNode &&
                        !loweredNodes.Contains(nodesToLower[indexOfNode + 2]) ? nodesToLower[indexOfNode + 2] : null;

                    _loweredDecisionDag.Add(_factory.ExpressionStatement(sideEffect));
                    GenerateTest(test, whenTrue, whenFalse, nextNode);
                    return true;
                }

                return false;
            }

            private void GenerateTest(BoundExpression test, BoundDecisionDagNode whenTrue, BoundDecisionDagNode whenFalse, BoundDecisionDagNode nextNode)
            {
                // Because we have already "optimized" away tests for a constant switch expression, the test should be nontrivial.
                _factory.Syntax = test.Syntax;
                Debug.Assert(test != null);

                if (nextNode == whenFalse)
                {
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(whenTrue), jumpIfTrue: true));
                    // fall through to false path
                }
                else if (nextNode == whenTrue)
                {
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(whenFalse), jumpIfTrue: false));
                    // fall through to true path
                }
                else
                {
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, GetDagNodeLabel(whenTrue), jumpIfTrue: true));
                    _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(whenFalse)));
                }
            }

            /// <summary>
            /// Generate a switch dispatch for a contiguous sequence of dag nodes if applicable.
            /// Returns true if it was applicable.
            /// </summary>
            private bool GenerateSwitchDispatch(BoundDecisionDagNode node, HashSet<BoundDecisionDagNode> loweredNodes)
            {
                Debug.Assert(!loweredNodes.Contains(node));
                if (!canGenerateSwitchDispatch(node))
                    return false;

                var input = ((BoundTestDecisionDagNode)node).Test.Input;
                ValueDispatchNode n = GatherValueDispatchNodes(node, loweredNodes, input);
                LowerValueDispatchNode(n, _tempAllocator.GetTemp(input));
                return true;

                bool canGenerateSwitchDispatch(BoundDecisionDagNode node)
                {
                    switch (node)
                    {
                        // These are the forms worth optimizing.
                        case BoundTestDecisionDagNode { WhenFalse: BoundTestDecisionDagNode test2 } test1:
                            return canDispatch(test1, test2);
                        case BoundTestDecisionDagNode { WhenTrue: BoundTestDecisionDagNode test2 } test1:
                            return canDispatch(test1, test2);
                        default:
                            // Other cases are just as well done with a single test.
                            return false;
                    }

                    bool canDispatch(BoundTestDecisionDagNode test1, BoundTestDecisionDagNode test2)
                    {
                        if (this._dagNodeLabels.ContainsKey(test2))
                            return false;

                        Debug.Assert(!loweredNodes.Contains(test2));
                        var t1 = test1.Test;
                        var t2 = test2.Test;
                        if (!(t1 is BoundDagValueTest || t1 is BoundDagRelationalTest))
                            return false;
                        if (!(t2 is BoundDagValueTest || t2 is BoundDagRelationalTest))
                            return false;
                        if (!t1.Input.Equals(t2.Input))
                            return false;

                        if (t1.Input.Type.SpecialType is SpecialType.System_Double or SpecialType.System_Single)
                        {
                            // The optimization (using balanced switch dispatch) breaks the semantics of NaN
                            return false;
                        }

                        return true;
                    }
                }
            }

            private ValueDispatchNode GatherValueDispatchNodes(
                BoundDecisionDagNode node,
                HashSet<BoundDecisionDagNode> loweredNodes,
                BoundDagTemp input)
            {
                IValueSetFactory fac = ValueSetFactory.ForInput(input);
                return GatherValueDispatchNodes(node, loweredNodes, input, fac);
            }

            private ValueDispatchNode GatherValueDispatchNodes(
                BoundDecisionDagNode node,
                HashSet<BoundDecisionDagNode> loweredNodes,
                BoundDagTemp input,
                IValueSetFactory fac)
            {
                if (loweredNodes.Contains(node))
                {
                    bool foundLabel = this._dagNodeLabels.TryGetValue(node, out LabelSymbol label);
                    Debug.Assert(foundLabel);
                    return new ValueDispatchNode.LeafDispatchNode(node.Syntax, label);
                }
                if (!(node is BoundTestDecisionDagNode testNode && testNode.Test.Input.Equals(input)))
                {
                    var label = GetDagNodeLabel(node);
                    return new ValueDispatchNode.LeafDispatchNode(node.Syntax, label);
                }

                switch (testNode.Test)
                {
                    case BoundDagRelationalTest relational:
                        {
                            loweredNodes.Add(testNode);
                            var whenTrue = GatherValueDispatchNodes(testNode.WhenTrue, loweredNodes, input, fac);
                            var whenFalse = GatherValueDispatchNodes(testNode.WhenFalse, loweredNodes, input, fac);
                            return ValueDispatchNode.RelationalDispatch.CreateBalanced(testNode.Syntax, relational.Value, relational.OperatorKind, whenTrue: whenTrue, whenFalse: whenFalse);
                        }
                    case BoundDagValueTest value:
                        {
                            // Gather up the (value, label) pairs, starting with the first one
                            loweredNodes.Add(testNode);
                            var cases = ArrayBuilder<(ConstantValue value, LabelSymbol label)>.GetInstance();
                            cases.Add((value: value.Value, label: GetDagNodeLabel(testNode.WhenTrue)));
                            BoundTestDecisionDagNode previous = testNode;
                            while (previous.WhenFalse is BoundTestDecisionDagNode p &&
                                p.Test is BoundDagValueTest vd &&
                                vd.Input.Equals(input) &&
                                !this._dagNodeLabels.ContainsKey(p) &&
                                !loweredNodes.Contains(p))
                            {
                                cases.Add((value: vd.Value, label: GetDagNodeLabel(p.WhenTrue)));
                                loweredNodes.Add(p);
                                previous = p;
                            }

                            var otherwise = GatherValueDispatchNodes(previous.WhenFalse, loweredNodes, input, fac);
                            return PushEqualityTestsIntoTree(value.Syntax, otherwise, cases.ToImmutableAndFree(), fac);
                        }
                    default:
                        {
                            var label = GetDagNodeLabel(node);
                            return new ValueDispatchNode.LeafDispatchNode(node.Syntax, label);
                        }
                }
            }

            /// <summary>
            /// Push the set of equality tests down to the level of the leaves in the value dispatch tree.
            /// </summary>
            private ValueDispatchNode PushEqualityTestsIntoTree(
                SyntaxNode syntax,
                ValueDispatchNode otherwise,
                ImmutableArray<(ConstantValue value, LabelSymbol label)> cases,
                IValueSetFactory fac)
            {
                if (cases.IsEmpty)
                    return otherwise;

                switch (otherwise)
                {
                    case ValueDispatchNode.LeafDispatchNode leaf:
                        return new ValueDispatchNode.SwitchDispatch(syntax, cases, leaf.Label);
                    case ValueDispatchNode.SwitchDispatch sd:
                        return new ValueDispatchNode.SwitchDispatch(sd.Syntax, sd.Cases.Concat(cases), sd.Otherwise);
                    case ValueDispatchNode.RelationalDispatch { Operator: var op, Value: var value, WhenTrue: var whenTrue, WhenFalse: var whenFalse } rel:
                        var (whenTrueCases, whenFalseCases) = splitCases(cases, op, value);
                        Debug.Assert(cases.Length == whenTrueCases.Length + whenFalseCases.Length);
                        whenTrue = PushEqualityTestsIntoTree(syntax, whenTrue, whenTrueCases, fac);
                        whenFalse = PushEqualityTestsIntoTree(syntax, whenFalse, whenFalseCases, fac);
                        var result = rel.WithTrueAndFalseChildren(whenTrue: whenTrue, whenFalse: whenFalse);
                        return result;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(otherwise);
                }

                (ImmutableArray<(ConstantValue value, LabelSymbol label)> whenTrueCases, ImmutableArray<(ConstantValue value, LabelSymbol label)> whenFalseCases)
                    splitCases(ImmutableArray<(ConstantValue value, LabelSymbol label)> cases, BinaryOperatorKind op, ConstantValue value)
                {
                    var whenTrueBuilder = ArrayBuilder<(ConstantValue value, LabelSymbol label)>.GetInstance();
                    var whenFalseBuilder = ArrayBuilder<(ConstantValue value, LabelSymbol label)>.GetInstance();
                    op = op.Operator();
                    foreach (var pair in cases)
                    {
                        (fac.Related(op, pair.value, value) ? whenTrueBuilder : whenFalseBuilder).Add(pair);
                    }

                    return (whenTrueBuilder.ToImmutableAndFree(), whenFalseBuilder.ToImmutableAndFree());
                }
            }

            private void LowerValueDispatchNode(ValueDispatchNode n, BoundExpression input)
            {
                switch (n)
                {
                    case ValueDispatchNode.LeafDispatchNode leaf:
                        _loweredDecisionDag.Add(_factory.Goto(leaf.Label));
                        return;
                    case ValueDispatchNode.SwitchDispatch eq:
                        LowerSwitchDispatchNode(eq, input);
                        return;
                    case ValueDispatchNode.RelationalDispatch rel:
                        LowerRelationalDispatchNode(rel, input);
                        return;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(n);
                }
            }

            private void LowerRelationalDispatchNode(ValueDispatchNode.RelationalDispatch rel, BoundExpression input)
            {
                var test = MakeRelationalTest(rel.Syntax, input, rel.Operator, rel.Value);
                if (rel.WhenTrue is ValueDispatchNode.LeafDispatchNode whenTrue)
                {
                    LabelSymbol trueLabel = whenTrue.Label;
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, trueLabel, jumpIfTrue: true));
                    LowerValueDispatchNode(rel.WhenFalse, input);
                }
                else if (rel.WhenFalse is ValueDispatchNode.LeafDispatchNode whenFalse)
                {
                    LabelSymbol falseLabel = whenFalse.Label;
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, falseLabel, jumpIfTrue: false));
                    LowerValueDispatchNode(rel.WhenTrue, input);
                }
                else
                {
                    LabelSymbol falseLabel = _factory.GenerateLabel("relationalDispatch");
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, falseLabel, jumpIfTrue: false));
                    LowerValueDispatchNode(rel.WhenTrue, input);
                    _loweredDecisionDag.Add(_factory.Label(falseLabel));
                    LowerValueDispatchNode(rel.WhenFalse, input);
                }
            }

            /// <summary>
            /// A comparer for sorting cases containing values of type float, double, or decimal.
            /// </summary>
            private sealed class CasesComparer : IComparer<(ConstantValue value, LabelSymbol label)>
            {
                private readonly IValueSetFactory _fac;
                public CasesComparer(TypeSymbol type)
                {
                    _fac = ValueSetFactory.ForType(type);
                    Debug.Assert(_fac is { });
                }

                int IComparer<(ConstantValue value, LabelSymbol label)>.Compare((ConstantValue value, LabelSymbol label) left, (ConstantValue value, LabelSymbol label) right)
                {
                    var x = left.value;
                    var y = right.value;
                    Debug.Assert(x.Discriminator switch
                    {
                        ConstantValueTypeDiscriminator.Decimal => true,
                        ConstantValueTypeDiscriminator.Single => true,
                        ConstantValueTypeDiscriminator.Double => true,
                        ConstantValueTypeDiscriminator.NInt => true,
                        ConstantValueTypeDiscriminator.NUInt => true,
                        _ => false
                    });
                    Debug.Assert(y.Discriminator == x.Discriminator);
                    // Sort NaN values into the "highest" position so they fall naturally into the last bucket
                    // when partitioned using less-than.
                    return
                        isNaN(x) ? 1 :
                        isNaN(y) ? -1 :
                        _fac.Related(BinaryOperatorKind.LessThanOrEqual, x, y) ?
                            (_fac.Related(BinaryOperatorKind.LessThanOrEqual, y, x) ? 0 : -1) :
                        1;

                    static bool isNaN(ConstantValue value) =>
                        (value.Discriminator == ConstantValueTypeDiscriminator.Single || value.Discriminator == ConstantValueTypeDiscriminator.Double) &&
                        double.IsNaN(value.DoubleValue);
                }
            }

            private void LowerSwitchDispatchNode(ValueDispatchNode.SwitchDispatch node, BoundExpression input)
            {
                LabelSymbol defaultLabel = node.Otherwise;

                if (input.Type.IsValidV6SwitchGoverningType() || input.Type.IsSpanOrReadOnlySpanChar())
                {
                    // If we are emitting a hash table based string switch,
                    // we need to generate a helper method for computing
                    // string hash value in <PrivateImplementationDetails> class.

                    bool isStringInput = input.Type.SpecialType == SpecialType.System_String;
                    bool isSpanInput = input.Type.IsSpanChar();
                    bool isReadOnlySpanInput = input.Type.IsReadOnlySpanChar();
                    LengthBasedStringSwitchData lengthBasedDispatchOpt = null;
                    if (isStringInput || isSpanInput || isReadOnlySpanInput)
                    {
                        var stringPatternInput = isStringInput ? StringPatternInput.String : (isSpanInput ? StringPatternInput.SpanChar : StringPatternInput.ReadOnlySpanChar);

                        if (!this._localRewriter._compilation.FeatureDisableLengthBasedSwitch &&
                            this._factory.Compilation.Options.OptimizationLevel == OptimizationLevel.Release &&
                            LengthBasedStringSwitchData.Create(node.Cases) is var lengthBasedDispatch &&
                            lengthBasedDispatch.ShouldGenerateLengthBasedSwitch(node.Cases.Length) &&
                            hasLengthBasedDispatchRequiredMembers(stringPatternInput))
                        {
                            lengthBasedDispatchOpt = lengthBasedDispatch;
                        }
                        else
                        {
                            EnsureStringHashFunction(node.Cases.Length, node.Syntax, stringPatternInput);
                        }

                        if (isStringInput)
                        {
                            // Report required missing member diagnostic
                            _localRewriter.TryGetSpecialTypeMethod(node.Syntax, SpecialMember.System_String__op_Equality, out _);
                        }
                    }

                    var dispatch = new BoundSwitchDispatch(node.Syntax, input, node.Cases, defaultLabel, lengthBasedDispatchOpt);
                    _loweredDecisionDag.Add(dispatch);
                }
                else if (input.Type.IsNativeIntegerType)
                {
                    // Native types need to be dispatched using a larger underlying type so that any
                    // possible high bits are not truncated.
                    ImmutableArray<(ConstantValue value, LabelSymbol label)> cases;
                    switch (input.Type.SpecialType)
                    {
                        case SpecialType.System_IntPtr:
                            {
                                input = _factory.Convert(_factory.SpecialType(SpecialType.System_Int64), input);
                                cases = node.Cases.SelectAsArray(p => (ConstantValue.Create((long)p.value.Int32Value), p.label));
                                break;
                            }
                        case SpecialType.System_UIntPtr:
                            {
                                input = _factory.Convert(_factory.SpecialType(SpecialType.System_UInt64), input);
                                cases = node.Cases.SelectAsArray(p => (ConstantValue.Create((ulong)p.value.UInt32Value), p.label));
                                break;
                            }
                        default:
                            throw ExceptionUtilities.UnexpectedValue(input.Type);
                    }

                    var dispatch = new BoundSwitchDispatch(node.Syntax, input, cases, defaultLabel, lengthBasedStringSwitchDataOpt: null);
                    _loweredDecisionDag.Add(dispatch);
                }
                else
                {
                    // The emitter does not know how to produce a "switch" instruction for float, double, or decimal
                    // (in part because there is no such instruction) so we fake it here by generating a decision tree.
                    var lessThanOrEqualOperator = input.Type.SpecialType switch
                    {
                        SpecialType.System_Single => BinaryOperatorKind.FloatLessThanOrEqual,
                        SpecialType.System_Double => BinaryOperatorKind.DoubleLessThanOrEqual,
                        SpecialType.System_Decimal => BinaryOperatorKind.DecimalLessThanOrEqual,
                        _ => throw ExceptionUtilities.UnexpectedValue(input.Type.SpecialType)
                    };

                    var cases = node.Cases.Sort(new CasesComparer(input.Type));
                    lowerFloatDispatch(0, cases.Length);

                    void lowerFloatDispatch(int firstIndex, int count)
                    {
                        if (count <= 3)
                        {
                            for (int i = firstIndex, limit = firstIndex + count; i < limit; i++)
                            {
                                _loweredDecisionDag.Add(_factory.ConditionalGoto(MakeValueTest(node.Syntax, input, cases[i].value), cases[i].label, jumpIfTrue: true));
                            }

                            _loweredDecisionDag.Add(_factory.Goto(defaultLabel));
                        }
                        else
                        {
                            int half = count / 2;
                            var gt = _factory.GenerateLabel("greaterThanMidpoint");
                            _loweredDecisionDag.Add(_factory.ConditionalGoto(MakeRelationalTest(node.Syntax, input, lessThanOrEqualOperator, cases[firstIndex + half - 1].value), gt, jumpIfTrue: false));
                            lowerFloatDispatch(firstIndex, half);
                            _loweredDecisionDag.Add(_factory.Label(gt));
                            lowerFloatDispatch(firstIndex + half, count - half);
                        }
                    }
                }

                return;

                bool hasLengthBasedDispatchRequiredMembers(StringPatternInput stringPatternInput)
                {
                    var compilation = _localRewriter._compilation;
                    var lengthMember = stringPatternInput switch
                    {
                        StringPatternInput.String => compilation.GetSpecialTypeMember(SpecialMember.System_String__Length),
                        StringPatternInput.SpanChar => compilation.GetWellKnownTypeMember(WellKnownMember.System_Span_T__get_Length),
                        StringPatternInput.ReadOnlySpanChar => compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__get_Length),
                        _ => throw ExceptionUtilities.UnexpectedValue(stringPatternInput),
                    };

                    if ((object)lengthMember == null || lengthMember.HasUseSiteError)
                    {
                        return false;
                    }

                    var charsMember = stringPatternInput switch
                    {
                        StringPatternInput.String => compilation.GetSpecialTypeMember(SpecialMember.System_String__Chars),
                        StringPatternInput.SpanChar => compilation.GetWellKnownTypeMember(WellKnownMember.System_Span_T__get_Item),
                        StringPatternInput.ReadOnlySpanChar => compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__get_Item),
                        _ => throw ExceptionUtilities.UnexpectedValue(stringPatternInput),
                    };

                    if ((object)charsMember == null || charsMember.HasUseSiteError)
                    {
                        return false;
                    }

                    return true;
                }
            }

            private enum StringPatternInput
            {
                String,
                SpanChar,
                ReadOnlySpanChar,
            }

            /// <summary>
            /// Checks whether we are generating a hash table based string switch and
            /// we need to generate a new helper method for computing string hash value.
            /// Creates the method if needed.
            /// </summary>
            private void EnsureStringHashFunction(int labelsCount, SyntaxNode syntaxNode, StringPatternInput stringPatternInput)
            {
                var module = _localRewriter.EmitModule;
                if (module == null)
                {
                    // we're not generating code, so we don't need the hash function
                    return;
                }

                // For string switch statements, we need to determine if we are generating a hash
                // table based jump table or a non hash jump table, i.e. linear string comparisons
                // with each case label. We use the Dev10 Heuristic to determine this
                // (see SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch() for details).
                if (!CodeAnalysis.CodeGen.SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(labelsCount))
                {
                    return;
                }

                // If we are generating a hash table based jump table, we use a simple customizable
                // hash function to hash the string constants corresponding to the case labels.
                // See SwitchStringJumpTableEmitter.ComputeStringHash().
                // We need to emit this function to compute the hash value into the compiler generated
                // <PrivateImplementationDetails> class. 
                // If we have at least one string switch statement in a module that needs a
                // hash table based jump table, we generate a single public string hash synthesized method
                // that is shared across the module.

                // If we have already generated the helper, possibly for another switch
                // or on another thread, we don't need to regenerate it.
                var privateImplClass = module.GetPrivateImplClass(syntaxNode, _localRewriter._diagnostics.DiagnosticBag);
                if (privateImplClass.PrivateImplementationDetails.GetMethod(stringPatternInput switch
                {
                    StringPatternInput.String => CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedStringHashFunctionName,
                    StringPatternInput.SpanChar => CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedReadOnlySpanHashFunctionName,
                    StringPatternInput.ReadOnlySpanChar => CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedSpanHashFunctionName,
                    _ => throw ExceptionUtilities.UnexpectedValue(stringPatternInput),
                }) != null)
                {
                    return;
                }

                // cannot emit hash method if have no access to Chars.
                var charsMember = stringPatternInput switch
                {
                    StringPatternInput.String => _localRewriter._compilation.GetSpecialTypeMember(SpecialMember.System_String__Chars),
                    StringPatternInput.SpanChar => _localRewriter._compilation.GetWellKnownTypeMember(WellKnownMember.System_Span_T__get_Item),
                    StringPatternInput.ReadOnlySpanChar => _localRewriter._compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__get_Item),
                    _ => throw ExceptionUtilities.UnexpectedValue(stringPatternInput),
                };
                if ((object)charsMember == null || charsMember.HasUseSiteError)
                {
                    return;
                }

                TypeSymbol returnType = _factory.SpecialType(SpecialType.System_UInt32);
                TypeSymbol paramType = stringPatternInput switch
                {
                    StringPatternInput.String => _factory.SpecialType(SpecialType.System_String),
                    StringPatternInput.SpanChar => _factory.WellKnownType(WellKnownType.System_Span_T)
                        .Construct(_factory.SpecialType(SpecialType.System_Char)),
                    StringPatternInput.ReadOnlySpanChar => _factory.WellKnownType(WellKnownType.System_ReadOnlySpan_T)
                        .Construct(_factory.SpecialType(SpecialType.System_Char)),
                    _ => throw ExceptionUtilities.UnexpectedValue(stringPatternInput),
                };

                SynthesizedGlobalMethodSymbol method = stringPatternInput switch
                {
                    StringPatternInput.String => new SynthesizedStringSwitchHashMethod(privateImplClass, returnType, paramType),
                    StringPatternInput.SpanChar => new SynthesizedSpanSwitchHashMethod(privateImplClass, returnType, paramType, isReadOnlySpan: false),
                    StringPatternInput.ReadOnlySpanChar => new SynthesizedSpanSwitchHashMethod(privateImplClass, returnType, paramType, isReadOnlySpan: true),
                    _ => throw ExceptionUtilities.UnexpectedValue(stringPatternInput),
                };
                privateImplClass.PrivateImplementationDetails.TryAddSynthesizedMethod(method.GetCciAdapter());
            }

#nullable enable
            private void LowerWhenClauses(ImmutableArray<BoundDecisionDagNode> sortedNodes)
            {
                if (!sortedNodes.Any(static n => n.Kind == BoundKind.WhenDecisionDagNode)) return;

                // The way the DAG is prepared, it is possible for different `BoundWhenDecisionDagNode` nodes to
                // share the same `WhenExpression` (same `BoundExpression` instance).
                // So we can't just lower each `BoundWhenDecisionDagNode` separately, as that would result in duplicate blocks
                // for the same `WhenExpression` and such expressions might contains labels which must be emitted once only.

                // For a simple `BoundWhenDecisionDagNode` (with a unique `WhenExpression`), we lower to something like:
                //   labelToSectionScope;
                //   if (... logic from WhenExpression ...)
                //   {
                //     jump to whenTrue label
                //   }
                //   jump to whenFalse label

                // For a complex `BoundWhenDecisionDagNode` (where the `WhenExpression` is shared), we lower to something like:
                //   labelToSectionScope;
                //   whenNodeIdentifierLocal = whenNodeIdentifier;
                //   goto labelToWhenExpression;
                //
                // and we'll also create a section for the shared `WhenExpression` logic:
                //   labelToWhenExpression;
                //   if (... logic from WhenExpression ...)
                //   {
                //     jump to whenTrue label
                //   }
                //   switch on whenNodeIdentifierLocal with dispatches to whenFalse labels

                int nextWhenNodeIdentifier = 0;
                // Prepared maps for `when` nodes and expressions
                var whenExpressionMap = PooledDictionary<BoundExpression, (LabelSymbol LabelToWhenExpression, ArrayBuilder<BoundWhenDecisionDagNode> WhenNodes)>.GetInstance();
                var whenNodeMap = PooledDictionary<BoundWhenDecisionDagNode, (LabelSymbol LabelToWhenExpression, int WhenNodeIdentifier)>.GetInstance();
                foreach (BoundDecisionDagNode node in sortedNodes)
                {
                    if (node is BoundWhenDecisionDagNode whenNode)
                    {
                        var whenExpression = whenNode.WhenExpression;
                        if (whenExpression is not null && whenExpression.ConstantValueOpt != ConstantValue.True)
                        {
                            LabelSymbol labelToWhenExpression;
                            if (whenExpressionMap.TryGetValue(whenExpression, out var whenExpressionInfo))
                            {
                                labelToWhenExpression = whenExpressionInfo.LabelToWhenExpression;
                                whenExpressionInfo.WhenNodes.Add(whenNode);
                            }
                            else
                            {
                                labelToWhenExpression = _factory.GenerateLabel("sharedWhenExpression");
                                var list = ArrayBuilder<BoundWhenDecisionDagNode>.GetInstance();
                                list.Add(whenNode);
                                whenExpressionMap.Add(whenExpression, (labelToWhenExpression, list));
                            }

                            whenNodeMap.Add(whenNode, (labelToWhenExpression, nextWhenNodeIdentifier++));
                        }
                    }
                }

                // Lower nodes
                foreach (BoundDecisionDagNode node in sortedNodes)
                {
                    if (node is BoundWhenDecisionDagNode whenNode)
                    {
                        if (!tryLowerAsJumpToSharedWhenExpression(whenNode))
                        {
                            lowerWhenClause(whenNode);
                        }
                    }
                }

                // Lower shared `when` expressions
                foreach (var (whenExpression, (labelToWhenExpression, whenNodes)) in whenExpressionMap)
                {
                    lowerWhenExpressionIfShared(whenExpression, labelToWhenExpression, whenNodes);
                    whenNodes.Free();
                }

                whenExpressionMap.Free();
                whenNodeMap.Free();

                return;

                bool tryLowerAsJumpToSharedWhenExpression(BoundWhenDecisionDagNode whenNode)
                {
                    var whenExpression = whenNode.WhenExpression;
                    if (!isSharedWhenExpression(whenExpression))
                    {
                        return false;
                    }

                    LabelSymbol labelToSectionScope = GetDagNodeLabel(whenNode);
                    ArrayBuilder<BoundStatement> sectionBuilder = BuilderForSection(whenNode.Syntax);
                    sectionBuilder.Add(_factory.Label(labelToSectionScope));

                    _whenNodeIdentifierLocal ??= _factory.SynthesizedLocal(_factory.SpecialType(SpecialType.System_Int32));
                    var found = whenNodeMap.TryGetValue(whenNode, out var whenNodeInfo);
                    Debug.Assert(found);

                    // whenNodeIdentifierLocal = whenNodeIdentifier;
                    sectionBuilder.Add(_factory.Assignment(_factory.Local(_whenNodeIdentifierLocal), _factory.Literal(whenNodeInfo.WhenNodeIdentifier)));

                    // goto labelToWhenExpression;
                    sectionBuilder.Add(_factory.Goto(whenNodeInfo.LabelToWhenExpression));

                    return true;
                }

                void lowerWhenExpressionIfShared(BoundExpression whenExpression, LabelSymbol labelToWhenExpression, ArrayBuilder<BoundWhenDecisionDagNode> whenNodes)
                {
                    if (!isSharedWhenExpression(whenExpression))
                    {
                        return;
                    }

                    var whenClauseSyntax = whenNodes[0].Syntax;
                    var whenTrueLabel = GetDagNodeLabel(whenNodes[0].WhenTrue);
                    Debug.Assert(whenNodes.Count > 1);
                    Debug.Assert(whenNodes.All(n => n.Syntax == whenClauseSyntax));
                    Debug.Assert(whenNodes.All(n => n.WhenExpression == whenExpression));
                    Debug.Assert(whenNodes.All(n => n.Bindings == whenNodes[0].Bindings));
                    Debug.Assert(whenNodes.All(n => GetDagNodeLabel(n.WhenTrue) == whenTrueLabel));

                    ArrayBuilder<BoundStatement> sectionBuilder = BuilderForSection(whenClauseSyntax);
                    sectionBuilder.Add(_factory.Label(labelToWhenExpression));
                    lowerBindings(whenNodes[0].Bindings, sectionBuilder);
                    addConditionalGoto(whenExpression, whenClauseSyntax, whenTrueLabel, sectionBuilder);

                    var whenFalseSwitchSections = ArrayBuilder<SyntheticSwitchSection>.GetInstance();
                    foreach (var whenNode in whenNodes)
                    {
                        var (_, whenNodeIdentifier) = whenNodeMap[whenNode];
                        Debug.Assert(whenNode.WhenFalse != null);
                        whenFalseSwitchSections.Add(_factory.SwitchSection(whenNodeIdentifier, _factory.Goto(GetDagNodeLabel(whenNode.WhenFalse))));
                    }

                    // switch (whenNodeIdentifierLocal)
                    // {
                    //   case whenNodeIdentifier: goto falseLabelForWhenNode;
                    //   ...
                    // }
                    Debug.Assert(_whenNodeIdentifierLocal is not null);
                    BoundStatement jumps = _factory.Switch(_factory.Local(_whenNodeIdentifierLocal), whenFalseSwitchSections.ToImmutableAndFree());

                    // We hide the jump back into the decision dag, as it is not logically part of the when clause
                    sectionBuilder.Add(GenerateInstrumentation ? _factory.HiddenSequencePoint(jumps) : jumps);
                }

                // if (loweredWhenExpression)
                // {
                //   jump to whenTrue label
                // }
                void addConditionalGoto(BoundExpression whenExpression, SyntaxNode whenClauseSyntax, LabelSymbol whenTrueLabel, ArrayBuilder<BoundStatement> sectionBuilder)
                {
                    _factory.Syntax = whenClauseSyntax;
                    BoundStatement conditionalGoto = _factory.ConditionalGoto(_localRewriter.VisitExpression(whenExpression), whenTrueLabel, jumpIfTrue: true);

                    // Only add instrumentation (such as a sequence point) if the node is not compiler-generated.
                    if (GenerateInstrumentation && !whenExpression.WasCompilerGenerated)
                    {
                        conditionalGoto = _localRewriter.Instrumenter.InstrumentSwitchWhenClauseConditionalGotoBody(whenExpression, conditionalGoto);
                    }

                    sectionBuilder.Add(conditionalGoto);
                }

                bool isSharedWhenExpression(BoundExpression? whenExpression)
                {
                    return whenExpression is not null
                        && whenExpressionMap.TryGetValue(whenExpression, out var whenExpressionInfo)
                        && whenExpressionInfo.WhenNodes.Count > 1;
                }

                void lowerWhenClause(BoundWhenDecisionDagNode whenClause)
                {
                    // This node is used even when there is no when clause, to record bindings. In the case that there
                    // is no when clause, whenClause.WhenExpression and whenClause.WhenFalse are null, and the syntax for this
                    // node is the case clause.

                    // We need to assign the pattern variables in the code where they are in scope, so we produce a branch
                    // to the section where they are in scope and evaluate the when clause there.
                    var whenTrue = (BoundLeafDecisionDagNode)whenClause.WhenTrue;
                    LabelSymbol labelToSectionScope = GetDagNodeLabel(whenClause);

                    ArrayBuilder<BoundStatement> sectionBuilder = BuilderForSection(whenClause.Syntax);
                    sectionBuilder.Add(_factory.Label(labelToSectionScope));
                    lowerBindings(whenClause.Bindings, sectionBuilder);

                    var whenFalse = whenClause.WhenFalse;
                    var trueLabel = GetDagNodeLabel(whenTrue);
                    if (whenClause.WhenExpression != null && whenClause.WhenExpression.ConstantValueOpt != ConstantValue.True)
                    {
                        addConditionalGoto(whenClause.WhenExpression, whenClause.Syntax, trueLabel, sectionBuilder);

                        // We hide the jump back into the decision dag, as it is not logically part of the when clause
                        Debug.Assert(whenFalse != null);
                        BoundStatement jump = _factory.Goto(GetDagNodeLabel(whenFalse));
                        sectionBuilder.Add(GenerateInstrumentation ? _factory.HiddenSequencePoint(jump) : jump);
                    }
                    else
                    {
                        Debug.Assert(whenFalse == null);
                        sectionBuilder.Add(_factory.Goto(trueLabel));
                    }
                }

                void lowerBindings(ImmutableArray<BoundPatternBinding> bindings, ArrayBuilder<BoundStatement> sectionBuilder)
                {
                    foreach (BoundPatternBinding binding in bindings)
                    {
                        BoundExpression left = _localRewriter.VisitExpression(binding.VariableAccess);
                        // Since a switch does not add variables to the enclosing scope, the pattern variables
                        // are locals even in a script and rewriting them should have no effect.
                        Debug.Assert(left.Kind == BoundKind.Local && left == binding.VariableAccess);
                        BoundExpression right = _tempAllocator.GetTemp(binding.TempContainingValue);
                        if (left != right)
                        {
                            sectionBuilder.Add(_factory.Assignment(left, right));
                        }
                    }
                }
            }
#nullable disable

            /// <summary>
            /// Translate the decision dag for node, given that it will be followed by the translation for nextNode.
            /// </summary>
            private void LowerDecisionDagNode(BoundDecisionDagNode node, BoundDecisionDagNode nextNode)
            {
                _factory.Syntax = node.Syntax;
                switch (node)
                {
                    case BoundEvaluationDecisionDagNode evaluationNode:
                        {
                            BoundExpression sideEffect = LowerEvaluation(evaluationNode.Evaluation);
                            Debug.Assert(sideEffect != null);
                            _loweredDecisionDag.Add(_factory.ExpressionStatement(sideEffect));

                            // We add a hidden sequence point after the evaluation's side-effect, which may be a call out
                            // to user code such as `Deconstruct` or a property get, to permit edit-and-continue to
                            // synchronize on changes.
                            if (GenerateInstrumentation)
                                _loweredDecisionDag.Add(_factory.HiddenSequencePoint());

                            if (nextNode != evaluationNode.Next)
                            {
                                // We only need a goto if we would not otherwise fall through to the desired state
                                _loweredDecisionDag.Add(_factory.Goto(GetDagNodeLabel(evaluationNode.Next)));
                            }
                        }

                        break;

                    case BoundTestDecisionDagNode testNode:
                        {
                            BoundExpression test = base.LowerTest(testNode.Test);
                            GenerateTest(test, testNode.WhenTrue, testNode.WhenFalse, nextNode);
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(node.Kind);
                }
            }
        }
    }
}
