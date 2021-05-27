// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        private abstract partial class DecisionDagRewriter
        {
            /// <summary>
            /// A node in a tree representing the form of a generated decision tree for classifying an input value.
            /// </summary>
            private abstract class ValueDispatchNode
            {
                protected virtual int Height => 1;
#if DEBUG
                protected virtual int Weight => 1;
#endif
                public readonly SyntaxNode Syntax;

                public ValueDispatchNode(SyntaxNode syntax) => Syntax = syntax;

                /// <summary>
                /// A node representing the dispatch by value (equality). This corresponds to a classical C switch
                /// statement, except that it also handles values of type float, double, decimal, and string.
                /// </summary>
                internal sealed class SwitchDispatch : ValueDispatchNode
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
                internal sealed class LeafDispatchNode : ValueDispatchNode
                {
                    public readonly LabelSymbol Label;
                    public LeafDispatchNode(SyntaxNode syntax, LabelSymbol Label) : base(syntax) => this.Label = Label;
                    public override string ToString() => "Leaf";
                }

                /// <summary>
                /// A node representing a dispatch based on a relational test of the input value by some constant.
                /// Nodes of this kind are required to be height-balanced when constructed, so that when the full
                /// decision tree is produced it generates a balanced tree of comparisons.  The shape of the tree
                /// keeps tests for lower values on the left and tests for higher values on the right:
                /// For <see cref="BinaryOperatorKind.LessThan"/> and <see cref="BinaryOperatorKind.LessThanOrEqual"/>,
                /// the <see cref="WhenTrue"/> branch is <see cref="Left"/> and the <see cref="WhenFalse"/> branch
                /// is <see cref="Right"/>; for <see cref="BinaryOperatorKind.GreaterThan"/> and
                /// <see cref="BinaryOperatorKind.GreaterThanOrEqual"/> it is reversed.
                /// See <see cref="IsReversed(BinaryOperatorKind)"/> for where that is computed.
                /// </summary>
                internal sealed class RelationalDispatch : ValueDispatchNode
                {
                    private int _height;
#if DEBUG
                    private int _weight;
#endif

                    protected override int Height => _height;
#if DEBUG
                    protected override int Weight => _weight;
#endif
                    public readonly ConstantValue Value;
                    public readonly BinaryOperatorKind Operator;
                    /// <summary>The side of the test handling lower values. The true side for &lt; and &lt;=, the false side for > and >=.</summary>
                    private ValueDispatchNode Left { get; set; }
                    /// <summary>The side of the test handling higher values. The false side for &lt; and &lt;=, the true side for > and >=.</summary>
                    private ValueDispatchNode Right { get; set; }
                    private RelationalDispatch(SyntaxNode syntax, ConstantValue value, BinaryOperatorKind op, ValueDispatchNode left, ValueDispatchNode right) : base(syntax)
                    {
                        Debug.Assert(op.OperandTypes() != 0);
                        this.Value = value;
                        this.Operator = op;
                        WithLeftAndRight(left, right);
                    }
                    public ValueDispatchNode WhenTrue => IsReversed(Operator) ? Right : Left;
                    public ValueDispatchNode WhenFalse => IsReversed(Operator) ? Left : Right;
                    public override string ToString() => $"RelationalDispatch.{Height}({Left} {Operator.Operator()} {Value} {Right})";

                    /// <summary>
                    /// Is the operator among those for which <see cref="WhenTrue"/> is <see cref="Right"/>?
                    /// </summary>
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
#if DEBUG
                        _weight = left.Weight + right.Weight + 1;
                        // Assert that the node is approximately balanced
                        Debug.Assert(_height < 2 * Math.Log(_weight));
#endif
                        return this;
                    }

                    public RelationalDispatch WithTrueAndFalseChildren(ValueDispatchNode whenTrue, ValueDispatchNode whenFalse)
                    {
                        if (whenTrue == this.WhenTrue && whenFalse == this.WhenFalse)
                            return this;

                        Debug.Assert(whenTrue.Height == this.WhenTrue.Height);
                        Debug.Assert(whenFalse.Height == this.WhenFalse.Height);
                        var (left, right) = IsReversed(Operator) ? (whenFalse, whenTrue) : (whenTrue, whenFalse);
                        return WithLeftAndRight(left, right);
                    }

                    public static ValueDispatchNode CreateBalanced(SyntaxNode syntax, ConstantValue value, BinaryOperatorKind op, ValueDispatchNode whenTrue, ValueDispatchNode whenFalse)
                    {
                        // Keep the lower numbers on the left and the higher numbers on the right.
                        var (left, right) = IsReversed(op) ? (whenFalse, whenTrue) : (whenTrue, whenFalse);
                        return CreateBalancedCore(syntax, value, op, left: left, right: right);
                    }

                    private static ValueDispatchNode CreateBalancedCore(SyntaxNode syntax, ConstantValue value, BinaryOperatorKind op, ValueDispatchNode left, ValueDispatchNode right)
                    {
                        Debug.Assert(op.OperandTypes() != 0);

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
                        #region Rebalance the top of the tree if necessary
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
                        #endregion Rebalance the top of the tree if necessary

                        // that should have been sufficient to balance the tree.
                        Debug.Assert(Math.Abs(left.Height - right.Height) < 2);
                        return new RelationalDispatch(syntax, value, op, left: left, right: right);
                    }
                }
            }
        }
    }
}
