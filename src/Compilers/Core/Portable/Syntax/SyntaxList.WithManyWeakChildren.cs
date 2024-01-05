// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal partial class SyntaxList
    {
        internal sealed class WithManyWeakChildren : SyntaxList
        {
            private readonly ArrayElement<WeakReference<SyntaxNode>?>[] _children;

            // We calculate and store the positions of all children here. This way, getting the position
            // of all children is O(N) [N being the list size], otherwise it is O(N^2) because getting
            // the position of a child later requires traversing all previous siblings.
            private readonly int[] _childPositions;

            internal WithManyWeakChildren(InternalSyntax.SyntaxList.WithManyChildrenBase green, SyntaxNode parent, int position)
                : base(green, parent, position)
            {
                int count = green.SlotCount;
                _children = new ArrayElement<WeakReference<SyntaxNode>?>[count];

                var childOffsets = new int[count];

                int childPosition = position;
                var greenChildren = green.children;
                for (int i = 0; i < childOffsets.Length; ++i)
                {
                    childOffsets[i] = childPosition;
                    childPosition += greenChildren[i].Value.FullWidth;
                }

                _childPositions = childOffsets;
            }

            internal override int GetChildPosition(int index)
            {
                return _childPositions[index];
            }

            internal override SyntaxNode GetNodeSlot(int index)
            {
                return GetWeakRedElement(ref _children[index].Value, index);
            }

            internal override SyntaxNode? GetCachedSlot(int index)
            {
                SyntaxNode? value = null;
                _children[index].Value?.TryGetTarget(out value);
                return value;
            }
        }
    }
}
