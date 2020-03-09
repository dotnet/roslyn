// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        /// <summary>
        /// A common base class for lowering the pattern switch statement and the pattern switch expression.
        /// </summary>
        private abstract class BaseSwitchLocalRewriter : PatternLocalRewriter
        {
            /// <summary>
            /// Map from switch section's syntax to the lowered code for the section. The code for a section
            /// includes the code to assign to the pattern variables and evaluate the when clause. Since a
            /// when clause can yield a false value, it can jump back to a label in the lowered decision dag.
            /// </summary>
            private readonly PooledDictionary<SyntaxNode, ArrayBuilder<BoundStatement>> _switchArms = PooledDictionary<SyntaxNode, ArrayBuilder<BoundStatement>>.GetInstance();

            /// <summary>
            /// The lowered decision dag. This includes all of the code to decide which pattern
            /// is matched, but not the code to assign to pattern variables and evaluate when clauses.
            /// </summary>
            private readonly ArrayBuilder<BoundStatement> _loweredDecisionDag = ArrayBuilder<BoundStatement>.GetInstance();

            /// <summary>
            /// The label in the code for the beginning of code for each node of the dag.
            /// </summary>
            private readonly PooledDictionary<BoundDecisionDagNode, LabelSymbol> _dagNodeLabels = PooledDictionary<BoundDecisionDagNode, LabelSymbol>.GetInstance();

            protected BaseSwitchLocalRewriter(
                SyntaxNode node,
                LocalRewriter localRewriter,
                ImmutableArray<SyntaxNode> arms)
                : base(node, localRewriter)
            {
                foreach (var arm in arms)
                {
                    var armBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                    // We start each switch block of a switch statement with a hidden sequence point so that
                    // we do not appear to be in the previous switch block when we begin.
                    if (IsSwitchStatement)
                        armBuilder.Add(_factory.HiddenSequencePoint());

                    _switchArms.Add(arm, armBuilder);
                }
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
                _switchArms.Free();
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
            /// A utility class that is used to scan a when clause to determine if it might assign a variable,
            /// directly or indirectly. Used to determine if we can skip the allocation of pattern-matching
            /// temporary variables and use user-declared variables instead, because we can conclude that they
            /// are not mutated while the pattern-matching automaton is running.
            /// </summary>
            protected class WhenClauseMightAssignWalker : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
            {
                private bool _mightAssignSomething;

                public bool MightAssignSomething(BoundExpression expr)
                {
                    if (expr == null || expr.ConstantValue != null)
                    {
                        return false;
                    }

                    this._mightAssignSomething = false;
                    this.Visit(expr);
                    return this._mightAssignSomething;
                }

                public override BoundNode Visit(BoundNode node)
                {
                    // Stop visiting once we determine something might get assigned
                    return this._mightAssignSomething ? null : base.Visit(node);
                }

                public override BoundNode VisitCall(BoundCall node)
                {
                    bool mightMutate =
                        // might be a call to a local function that assigns something
                        node.Method.MethodKind == MethodKind.LocalFunction ||
                        // or perhaps we are passing a variable by ref and mutating it that way
                        !node.ArgumentRefKindsOpt.IsDefault;

                    if (mightMutate)
                        _mightAssignSomething = true;
                    else
                        base.VisitCall(node);

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
                    // Although property arguments with ref indexers are not declarable in C#, they may be usable
                    if (!node.ArgumentRefKindsOpt.IsDefault)
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
                var mightAssignWalker = new WhenClauseMightAssignWalker();
                bool canShareTemps =
                    !decisionDag.TopologicallySortedNodes
                    .Any(node => node is BoundWhenDecisionDagNode w && mightAssignWalker.MightAssignSomething(w.WhenExpression));

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

                // In a switch statement, there is a hidden sequence point after evaluating the input at the start of
                // the code to handle the decision dag. This is necessary so that jumps back from a `when` clause into
                // the decision dag do not appear to jump back up to the enclosing construct.
                if (IsSwitchStatement)
                    result.Add(_factory.HiddenSequencePoint());

                return decisionDag;
            }

            /// <summary>
            /// Lower the given nodes into _loweredDecisionDag. Should only be called once per instance of this.
            /// </summary>
            protected (ImmutableArray<BoundStatement> loweredDag, ImmutableDictionary<SyntaxNode, ImmutableArray<BoundStatement>> switchSections) LowerDecisionDag(BoundDecisionDag decisionDag)
            {
                Debug.Assert(this._loweredDecisionDag.IsEmpty());
                ComputeLabelSet(decisionDag);
                LowerDecisionDagCore(decisionDag);
                ImmutableArray<BoundStatement> loweredDag = _loweredDecisionDag.ToImmutableAndFree();
                var switchSections = _switchArms.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.ToImmutableAndFree());
                _switchArms.Clear();
                return (loweredDag, switchSections);
            }

            private void LowerDecisionDagCore(BoundDecisionDag decisionDag)
            {
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
                foreach (BoundDecisionDagNode node in sortedNodes)
                {
                    if (node is BoundWhenDecisionDagNode w)
                    {
                        LowerWhenClause(w);
                    }
                }

                ImmutableArray<BoundDecisionDagNode> nodesToLower = sortedNodes.WhereAsArray(n => n.Kind != BoundKind.WhenDecisionDagNode && n.Kind != BoundKind.LeafDecisionDagNode);
                var loweredNodes = PooledHashSet<BoundDecisionDagNode>.GetInstance();
                for (int i = 0, length = nodesToLower.Length; i < length; i++)
                {
                    BoundDecisionDagNode node = nodesToLower[i];
                    if (loweredNodes.Contains(node))
                    {
                        Debug.Assert(!_dagNodeLabels.TryGetValue(node, out _));
                        continue;
                    }

                    if (_dagNodeLabels.TryGetValue(node, out LabelSymbol label))
                    {
                        _loweredDecisionDag.Add(_factory.Label(label));
                    }

                    // If we can generate an IL switch instruction, do so
                    if (GenerateSwitchDispatch(node, loweredNodes))
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
            /// A node in a tree representing the form of a generated decision tree for classifying an input value.
            /// </summary>
            private abstract class ValueDispatchNode
            {
                protected virtual int Height => 1;
                public readonly SyntaxNode Syntax;

                public ValueDispatchNode(SyntaxNode syntax) => Syntax = syntax;

                /// <summary>
                /// A node representing the dispatch by value (equality). This corresponds to a classical C switch
                /// statement, except that it also handles values of type float, double, decimal, and string.
                /// </summary>
                internal class SwitchDispatch : ValueDispatchNode
                {
                    public readonly ImmutableArray<(ConstantValue value, LabelSymbol label)> Cases;
                    public readonly LabelSymbol Otherwise;
                    public SwitchDispatch(SyntaxNode syntax, ImmutableArray<(ConstantValue value, LabelSymbol label)> dispatches, LabelSymbol otherwise) : base(syntax)
                    {
                        this.Cases = dispatches;
                        this.Otherwise = otherwise;
                    }
                    public override string ToString() => "[" + string.Join(",", Cases.Select(c => c.value)) + "]";
                }

                /// <summary>
                /// A node representing a final destination that requires no further dispatch.
                /// </summary>
                internal class LeafDispatchNode : ValueDispatchNode
                {
                    public readonly LabelSymbol Label;
                    public LeafDispatchNode(SyntaxNode syntax, LabelSymbol Label) : base(syntax) => this.Label = Label;
                    public override string ToString() => "Leaf";
                }

                /// <summary>
                /// A node representing a dispatch based on a relational test of the input value by some constant.
                /// Nodes of this kind are required to be height-balanced when constructed, so that when the full
                /// decision tree is produced it generates a balanced tree of comparisons.
                /// </summary>
                internal class RelationalDispatch : ValueDispatchNode
                {
                    private int _height;
                    protected override int Height => _height;
                    public readonly ConstantValue Value;
                    public readonly BinaryOperatorKind Operator;
                    /// <summary>The side of the test handling lower values. The true side for &lt; and &lt;=, the false side for > and >=.</summary>
                    private ValueDispatchNode Left { get; set; }
                    /// <summary>The side of the test handling higher values. The false side for &lt; and &lt;=, the true side for > and >=.</summary>
                    private ValueDispatchNode Right { get; set; }
                    private RelationalDispatch(SyntaxNode syntax, ConstantValue value, BinaryOperatorKind op, ValueDispatchNode left, ValueDispatchNode right) : base(syntax)
                    {
                        this.Value = value;
                        this.Operator = op;
                        WithLeftAndRight(left, right);
                    }
                    public ValueDispatchNode WhenTrue => IsReversed(Operator) ? Right : Left;
                    public ValueDispatchNode WhenFalse => IsReversed(Operator) ? Left : Right;
                    public override string ToString() => $"RelationalDispatch.{Height}({Left} {Operator.Operator()} {Value} {Right})";
                    private static bool IsReversed(BinaryOperatorKind op) => op.Operator() switch { BinaryOperatorKind.GreaterThan => true, BinaryOperatorKind.GreaterThanOrEqual => true, _ => false };

                    private RelationalDispatch WithLeftAndRight(ValueDispatchNode left, ValueDispatchNode right)
                    {
                        // Note that this is a destructive implementation to reduce GC garbage.
                        // That requires clients to stop using the input node once this has been called.

                        int l = left.Height;
                        int r = right.Height;
                        Debug.Assert(Math.Abs(l - r) <= 1);

                        this.Left = left;
                        this.Right = right;
                        _height = Math.Max(l, r) + 1;
                        return this;
                    }

                    public RelationalDispatch WithTrueAndFalseChildren(ValueDispatchNode whenTrue, ValueDispatchNode whenFalse)
                    {
                        if (whenTrue == this.WhenTrue && whenFalse == this.WhenFalse)
                            return this;

                        Debug.Assert(whenTrue.Height == this.WhenTrue.Height);
                        Debug.Assert(whenFalse.Height == this.WhenFalse.Height);
                        var (left, right) = IsReversed(Operator.Operator()) ? (whenFalse, whenTrue) : (whenTrue, whenFalse);
                        return WithLeftAndRight(left, right);
                    }

                    public static ValueDispatchNode CreateBalanced(SyntaxNode syntax, ConstantValue value, BinaryOperatorKind op, ValueDispatchNode whenTrue, ValueDispatchNode whenFalse)
                    {
                        // Keep the lower numbers on the left and the higher numbers on the right.
                        var (left, right) = IsReversed(op.Operator()) ? (whenFalse, whenTrue) : (whenTrue, whenFalse);
                        return CreateBalancedCore(syntax, value, op, left: left, right: right);
                    }

                    private static ValueDispatchNode CreateBalancedCore(SyntaxNode syntax, ConstantValue value, BinaryOperatorKind op, ValueDispatchNode left, ValueDispatchNode right)
                    {
                        // Build a height-balanced tree node that is semantically equivalent to a node with the given parameters.
                        // See http://www.cs.ecu.edu/karl/3300/spr16/Notes/DataStructure/Tree/balance.html

                        // First, build an approximately balanced left and right bottom-up.
                        if (left.Height > (right.Height + 1))
                        {
                            var l = (RelationalDispatch)left;
                            var newRight = CreateBalancedCore(syntax, value, op, left: l.Right, right: right);
                            (syntax, value, op, left, right) = (l.Syntax, l.Value, l.Operator, l.Left, newRight);
                        }
                        else if (right.Height > (left.Height + 1))
                        {
                            var r = (RelationalDispatch)right;
                            var newLeft = CreateBalancedCore(syntax, value, op, left: left, right: r.Left);
                            (syntax, value, op, left, right) = (r.Syntax, r.Value, r.Operator, newLeft, r.Right);
                        }

                        // That should have brought the two sides within a height difference of two.
                        Debug.Assert(Math.Abs(left.Height - right.Height) <= 2);

                        // Now see if a final rotation is needed.

                        if (left.Height == right.Height + 2)
                        {
                            var leftDispatch = (RelationalDispatch)left;
                            if (leftDispatch.Left.Height == right.Height)
                            {
                                //
                                //       z
                                //      / \                 y
                                //     x   D               / \
                                //    / \        -->      x   z
                                //   A   y               /|   |\
                                //      / \             A B   C D
                                //     B   C
                                //
                                var x = leftDispatch;
                                var A = x.Left;
                                var y = (RelationalDispatch)x.Right;
                                var B = y.Left;
                                var C = y.Right;
                                var D = right;
                                return y.WithLeftAndRight(x.WithLeftAndRight(A, B), new RelationalDispatch(syntax, value, op, C, D));
                            }
                            else
                            {
                                Debug.Assert(leftDispatch.Right.Height == right.Height);
                                //
                                //       z
                                //      / \                 y
                                //     y   D               / \
                                //    / \        -->      x   z
                                //   x   C               /|   |\
                                //  / \                 A B   C D
                                // A   B
                                //
                                var y = leftDispatch;
                                var x = y.Left;
                                var C = y.Right;
                                var D = right;
                                return y.WithLeftAndRight(x, new RelationalDispatch(syntax, value, op, C, D));
                            }
                        }
                        else if (right.Height == left.Height + 2)
                        {
                            var rightDispatch = (RelationalDispatch)right;
                            if (rightDispatch.Right.Height == left.Height)
                            {
                                //
                                //     x
                                //    / \                   y
                                //   A   z                 / \
                                //      / \      -->      x   z
                                //     y   D             /|   |\
                                //    / \               A B   C D
                                //   B   C
                                //
                                var A = left;
                                var z = rightDispatch;
                                var y = (RelationalDispatch)z.Left;
                                var B = y.Left;
                                var C = y.Right;
                                var D = z.Right;
                                return y.WithLeftAndRight(new RelationalDispatch(syntax, value, op, A, B), z.WithLeftAndRight(C, D));
                            }
                            else
                            {
                                Debug.Assert(rightDispatch.Left.Height == left.Height);
                                //
                                //     x
                                //    / \                   y
                                //   A   y                 / \
                                //      / \      -->      x   z
                                //     B   z             /|   |\
                                //        / \           A B   C D
                                //       C   D
                                //
                                var A = left;
                                var y = rightDispatch;
                                var B = y.Left;
                                var z = y.Right;
                                return y.WithLeftAndRight(new RelationalDispatch(syntax, value, op, A, B), z);
                            }
                        }

                        // that should have been sufficient to balance the tree.
                        Debug.Assert(Math.Abs(left.Height - right.Height) < 2);
                        return new RelationalDispatch(syntax, value, op, left: left, right: right);
                    }
                }
            }

            private bool CanGenerateSwitchDispatch(BoundDecisionDagNode node, HashSet<BoundDecisionDagNode> loweredNodes)
            {
                switch (node)
                {
                    // These are the forms worth optimizing.
                    case BoundTestDecisionDagNode { WhenFalse: BoundTestDecisionDagNode test2 } test1:
                        return CanDispatch(test1, test2);
                    case BoundTestDecisionDagNode { WhenTrue: BoundTestDecisionDagNode test2 } test1:
                        return CanDispatch(test1, test2);
                    default:
                        // Other cases are just as well done with a single test.
                        return false;
                }

                bool CanDispatch(BoundTestDecisionDagNode test1, BoundTestDecisionDagNode test2)
                {
                    if (loweredNodes.Contains(test2))
                    {
                        Debug.Assert(this._dagNodeLabels.ContainsKey(test2));
                        return false;
                    }
                    if (this._dagNodeLabels.ContainsKey(test2))
                        return false;
                    var t1 = test1.Test;
                    var t2 = test2.Test;
                    if (!(t1 is BoundDagValueTest || t1 is BoundDagRelationalTest))
                        return false;
                    if (!(t2 is BoundDagValueTest || t2 is BoundDagRelationalTest))
                        return false;
                    if (!t1.Input.Equals(t2.Input))
                        return false;
                    return true;
                }
            }

            /// <summary>
            /// Generate a switch dispatch for a contiguous sequence of dag nodes if applicable.
            /// Returns true if it was applicable.
            /// </summary>
            private bool GenerateSwitchDispatch(BoundDecisionDagNode node, HashSet<BoundDecisionDagNode> loweredNodes)
            {
                Debug.Assert(!loweredNodes.Contains(node));
                if (!CanGenerateSwitchDispatch(node, loweredNodes))
                    return false;

                var input = ((BoundTestDecisionDagNode)node).Test.Input;
                ValueDispatchNode n = GatherValueDispatchNodes(node, loweredNodes, input);
                LowerValueDispatchNode(n, input);
                return true;
            }

            private ValueDispatchNode GatherValueDispatchNodes(
                BoundDecisionDagNode node,
                HashSet<BoundDecisionDagNode> loweredNodes,
                BoundDagTemp input)
            {
                IValueSetFactory fac = ValueSetFactory.ForSpecialType(input.Type.SpecialType);
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
                    foreach (var pair in cases)
                    {
                        (fac.Related(op.Operator(), pair.value, value) ? whenTrueBuilder : whenFalseBuilder).Add(pair);
                    }

                    return (whenTrueBuilder.ToImmutableAndFree(), whenFalseBuilder.ToImmutableAndFree());
                }
            }

            private void LowerValueDispatchNode(
                ValueDispatchNode n,
                BoundDagTemp input)
            {
                var inputExpression = _tempAllocator.GetTemp(input);
                LowerValueDispatchNodeCore(n, inputExpression);
            }

            private void LowerValueDispatchNodeCore(ValueDispatchNode n, BoundExpression input)
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
                    LowerValueDispatchNodeCore(rel.WhenFalse, input);
                }
                else if (rel.WhenFalse is ValueDispatchNode.LeafDispatchNode whenFalse)
                {
                    LabelSymbol falseLabel = whenFalse.Label;
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, falseLabel, jumpIfTrue: false));
                    LowerValueDispatchNodeCore(rel.WhenTrue, input);
                }
                else
                {
                    LabelSymbol falseLabel = _factory.GenerateLabel("sw");
                    _loweredDecisionDag.Add(_factory.ConditionalGoto(test, falseLabel, jumpIfTrue: false));
                    LowerValueDispatchNodeCore(rel.WhenTrue, input);
                    _loweredDecisionDag.Add(_factory.Label(falseLabel));
                    LowerValueDispatchNodeCore(rel.WhenFalse, input);
                }
            }

            /// <summary>
            /// A comparer for sorting cases containing values of type float, double, or decimal.
            /// </summary>
            private sealed class CasesComparer : IComparer
            {
                private readonly IValueSetFactory _fac;
                private readonly BinaryOperatorKind _lessThanOrEqualOperator;
                public CasesComparer(SpecialType type, BinaryOperatorKind lessThanOrEqualOperator)
                {
                    _fac = ValueSetFactory.ForSpecialType(type);
                    this._lessThanOrEqualOperator = lessThanOrEqualOperator.Operator();
                }

                int IComparer.Compare(object left, object right)
                {
                    var x = (((ConstantValue value, LabelSymbol label))left).value;
                    var y = (((ConstantValue value, LabelSymbol label))right).value;
                    Debug.Assert(x.Discriminator switch
                    {
                        ConstantValueTypeDiscriminator.Decimal => true,
                        ConstantValueTypeDiscriminator.Single => true,
                        ConstantValueTypeDiscriminator.Double => true,
                        _ => false
                    });
                    Debug.Assert(y.Discriminator == x.Discriminator);
                    // Sort NaN values into the "highest" position so they fall naturally into the last bucket
                    // when partitioned using less-than.
                    return
                        x.Discriminator != ConstantValueTypeDiscriminator.Decimal && double.IsNaN(x.DoubleValue) ? 1 :
                        y.Discriminator != ConstantValueTypeDiscriminator.Decimal && double.IsNaN(y.DoubleValue) ? -1 :
                        _fac.Related(_lessThanOrEqualOperator, x, y) ?
                            (_fac.Related(_lessThanOrEqualOperator, y, x) ? 0 : -1) :
                        1;
                }
            }

            private void LowerSwitchDispatchNode(ValueDispatchNode.SwitchDispatch node, BoundExpression input)
            {
                if (input.Type.IsValidV6SwitchGoverningType())
                {
                    // If we are emitting a hash table based string switch,
                    // we need to generate a helper method for computing
                    // string hash value in <PrivateImplementationDetails> class.
                    MethodSymbol stringEquality = null;
                    if (input.Type.SpecialType == SpecialType.System_String)
                    {
                        EnsureStringHashFunction(node.Cases.Length, node.Syntax);
                        stringEquality = _localRewriter.UnsafeGetSpecialTypeMethod(node.Syntax, SpecialMember.System_String__op_Equality);
                    }

                    LabelSymbol defaultLabel = node.Otherwise;
                    var dispatch = new BoundSwitchDispatch(node.Syntax, input, node.Cases, defaultLabel, stringEquality);
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

                    var cases = node.Cases.ToArray();
                    Array.Sort(cases, new CasesComparer(input.Type.SpecialType, lessThanOrEqualOperator));
                    lowerFloatDispatch(0, cases.Length);
                    void lowerFloatDispatch(int firstIndex, int count)
                    {
                        if (count <= 3)
                        {
                            for (int i = firstIndex, limit = firstIndex + count; i < limit; i++)
                            {
                                _loweredDecisionDag.Add(_factory.ConditionalGoto(MakeValueTest(node.Syntax, input, cases[i].value), cases[i].label, jumpIfTrue: true));
                            }

                            _loweredDecisionDag.Add(_factory.Goto(node.Otherwise));
                        }
                        else
                        {
                            int half = count / 2;
                            var gt = _factory.GenerateLabel("gt");
                            _loweredDecisionDag.Add(_factory.ConditionalGoto(MakeRelationalTest(node.Syntax, input, lessThanOrEqualOperator, cases[firstIndex + half - 1].value), gt, jumpIfTrue: false));
                            lowerFloatDispatch(firstIndex, half);
                            _loweredDecisionDag.Add(_factory.Label(gt));
                            lowerFloatDispatch(firstIndex + half, count - half);
                        }
                    }
                }
            }

            /// <summary>
            /// Checks whether we are generating a hash table based string switch and
            /// we need to generate a new helper method for computing string hash value.
            /// Creates the method if needed.
            /// </summary>
            private void EnsureStringHashFunction(int labelsCount, SyntaxNode syntaxNode)
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
                if (!CodeAnalysis.CodeGen.SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(module, labelsCount))
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
                var privateImplClass = module.GetPrivateImplClass(syntaxNode, _localRewriter._diagnostics);
                if (privateImplClass.GetMethod(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedStringHashFunctionName) != null)
                {
                    return;
                }

                // cannot emit hash method if have no access to Chars.
                var charsMember = _localRewriter._compilation.GetSpecialTypeMember(SpecialMember.System_String__Chars);
                if ((object)charsMember == null || charsMember.GetUseSiteDiagnostic() != null)
                {
                    return;
                }

                TypeSymbol returnType = _factory.SpecialType(SpecialType.System_UInt32);
                TypeSymbol paramType = _factory.SpecialType(SpecialType.System_String);

                var method = new SynthesizedStringSwitchHashMethod(module.SourceModule, privateImplClass, returnType, paramType);
                privateImplClass.TryAddSynthesizedMethod(method);
            }

            private void LowerWhenClause(BoundWhenDecisionDagNode whenClause)
            {
                // This node is used even when there is no when clause, to record bindings. In the case that there
                // is no when clause, whenClause.WhenExpression and whenClause.WhenFalse are null, and the syntax for this
                // node is the case clause.

                // We need to assign the pattern variables in the code where they are in scope, so we produce a branch
                // to the section where they are in scope and evaluate the when clause there.
                var whenTrue = (BoundLeafDecisionDagNode)whenClause.WhenTrue;
                LabelSymbol labelToSectionScope = GetDagNodeLabel(whenClause);

                // We need the section syntax to get the section builder from the map. Unfortunately this is a bit awkward
                SyntaxNode sectionSyntax = whenClause.Syntax is SwitchLabelSyntax l ? l.Parent : whenClause.Syntax;
                bool foundSectionBuilder = _switchArms.TryGetValue(sectionSyntax, out ArrayBuilder<BoundStatement> sectionBuilder);
                Debug.Assert(foundSectionBuilder);
                sectionBuilder.Add(_factory.Label(labelToSectionScope));
                foreach (BoundPatternBinding binding in whenClause.Bindings)
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

                var whenFalse = whenClause.WhenFalse;
                var trueLabel = GetDagNodeLabel(whenTrue);
                if (whenClause.WhenExpression != null && whenClause.WhenExpression.ConstantValue != ConstantValue.True)
                {
                    _factory.Syntax = whenClause.Syntax;
                    BoundStatement conditionalGoto = _factory.ConditionalGoto(_localRewriter.VisitExpression(whenClause.WhenExpression), trueLabel, jumpIfTrue: true);

                    // Only add instrumentation (such as a sequence point) if the node is not compiler-generated.
                    if (IsSwitchStatement && !whenClause.WhenExpression.WasCompilerGenerated && _localRewriter.Instrument)
                    {
                        conditionalGoto = _localRewriter._instrumenter.InstrumentSwitchWhenClauseConditionalGotoBody(whenClause.WhenExpression, conditionalGoto);
                    }

                    sectionBuilder.Add(conditionalGoto);

                    Debug.Assert(whenFalse != null);

                    // We hide the jump back into the decision dag, as it is not logically part of the when clause
                    BoundStatement jump = _factory.Goto(GetDagNodeLabel(whenFalse));
                    sectionBuilder.Add(IsSwitchStatement ? _factory.HiddenSequencePoint(jump) : jump);
                }
                else
                {
                    Debug.Assert(whenFalse == null);
                    sectionBuilder.Add(_factory.Goto(trueLabel));
                }
            }

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
                            if (IsSwitchStatement)
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
