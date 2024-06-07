// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections;

internal partial class MutableIntervalTree<T>
{
    internal sealed class Node
    {
        internal T Value { get; }

        internal Node? Left { get; private set; }
        internal Node? Right { get; private set; }

        internal int Height { get; private set; }
        internal Node MaxEndNode { get; private set; }

        internal Node(T value)
        {
            this.Value = value;
            this.Height = 1;
            this.MaxEndNode = this;
        }

        internal void SetLeftRight<TIntrospector>(Node? left, Node? right, in TIntrospector introspector)
            where TIntrospector : struct, IIntervalIntrospector<T>
        {
            this.Left = left;
            this.Right = right;

            this.Height = 1 + Math.Max(Height(left), Height(right));

            // We now must store the node that produces the maximum end. Since we might have tracking spans (or
            // something similar) defining our values of "end", we can't store the int itself.
            var thisEndValue = GetEnd(this.Value, in introspector);
            var leftEndValue = MaxEndValue(left, in introspector);
            var rightEndValue = MaxEndValue(right, in introspector);

            if (thisEndValue >= leftEndValue && thisEndValue >= rightEndValue)
            {
                MaxEndNode = this;
            }
            else if ((leftEndValue >= rightEndValue) && left != null)
            {
                MaxEndNode = left.MaxEndNode;
            }
            else if (right != null)
            {
                MaxEndNode = right.MaxEndNode;
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        // Sample:
        //       1              2
        //      / \          /     \
        //     2   d        3       1
        //    / \     =>   / \     / \
        //   3   c        a   b   c   d
        //  / \
        // a   b
        internal Node RightRotation<TIntrospector>(in TIntrospector introspector)
            where TIntrospector : struct, IIntervalIntrospector<T>
        {
            RoslynDebug.AssertNotNull(Left);

            var oldLeft = this.Left;
            this.SetLeftRight(this.Left.Right, this.Right, in introspector);
            oldLeft.SetLeftRight(oldLeft.Left, this, in introspector);

            return oldLeft;
        }

        // Sample:
        //   1                  2
        //  / \              /     \
        // a   2            1       3
        //    / \     =>   / \     / \
        //   b   3        a   b   c   d
        //      / \
        //     c   d
        internal Node LeftRotation<TIntrospector>(in TIntrospector introspector)
            where TIntrospector : struct, IIntervalIntrospector<T>
        {
            RoslynDebug.AssertNotNull(Right);

            var oldRight = this.Right;
            this.SetLeftRight(this.Left, this.Right.Left, in introspector);
            oldRight.SetLeftRight(this, oldRight.Right, in introspector);
            return oldRight;
        }

        // Sample:
        //   1            1                  3
        //  / \          / \              /     \
        // a   2        a   3            1       2
        //    / \   =>     / \     =>   / \     / \
        //   3   d        b   2        a   b   c   d
        //  / \              / \
        // b   c            c   d
        internal Node InnerRightOuterLeftRotation<TIntrospector>(in TIntrospector introspector)
            where TIntrospector : struct, IIntervalIntrospector<T>
        {
            RoslynDebug.AssertNotNull(Right);
            RoslynDebug.AssertNotNull(Right.Left);

            var newTop = this.Right.Left;
            var oldRight = this.Right;

            this.SetLeftRight(this.Left, this.Right.Left.Left, in introspector);
            oldRight.SetLeftRight(oldRight.Left.Right, oldRight.Right, in introspector);
            newTop.SetLeftRight(this, oldRight, in introspector);

            return newTop;
        }

        // Sample:
        //     1              1              3
        //    / \            / \          /     \
        //   2   d          3   d        2       1
        //  / \     =>     / \     =>   / \     / \
        // a   3          2   c        a   b   c   d
        //    / \        / \
        //   b   c      a   b
        internal Node InnerLeftOuterRightRotation<TIntrospector>(in TIntrospector introspector)
            where TIntrospector : struct, IIntervalIntrospector<T>
        {
            RoslynDebug.AssertNotNull(Left);
            RoslynDebug.AssertNotNull(Left.Right);

            var newTop = this.Left.Right;
            var oldLeft = this.Left;

            this.SetLeftRight(this.Left.Right.Right, this.Right, in introspector);
            oldLeft.SetLeftRight(oldLeft.Left, oldLeft.Right.Left, in introspector);
            newTop.SetLeftRight(oldLeft, this, in introspector);

            return newTop;
        }
    }
}
