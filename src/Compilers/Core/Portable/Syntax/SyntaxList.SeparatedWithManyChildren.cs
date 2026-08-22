// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Syntax
{
    internal partial class SyntaxList
    {
        internal sealed class SeparatedWithManyChildren : SyntaxList
        {
            private readonly ArrayElement<SyntaxNode?>[] _children;

            internal SeparatedWithManyChildren(InternalSyntax.SyntaxList green, SyntaxNode? parent, int position)
                : base(green, parent, position)
            {
                _children = new ArrayElement<SyntaxNode?>[(green.SlotCount + 1) >> 1];
            }

            internal override SyntaxNode? GetNodeSlot(int i)
            {
                if ((i & 1) != 0)
                {
                    //separator
                    return null;
                }

                return this.GetRedElement(ref _children[i >> 1].Value, i);
            }

            internal override SyntaxNode? GetCachedSlot(int i)
            {
                if ((i & 1) != 0)
                {
                    //separator
                    return null;
                }

                return _children[i >> 1].Value;
            }

            internal override int GetChildPosition(int index)
            {
                int leftNodeIndex = (index >> 1) - 1;
                var children = _children;

                // If the closest node on the left is uncached
                if (unchecked((uint)leftNodeIndex < (uint)children.Length) && children[leftNodeIndex].Value is null)
                {
                    int rightNodeIndex = leftNodeIndex + 2;

                    // If there is no node on the right or if the closest node on the right is cached
                    if (unchecked((uint)rightNodeIndex >= (uint)children.Length) || children[rightNodeIndex].Value is not null)
                    {
                        // Uses the node on the right (or the end of the parent) to determine position.
                        return GetChildPositionFromEnd(index);
                    }

                }

                // Since the checks above have minimal impact and the following method is inlined
                // we avoid a perf regression while fixing https://github.com/dotnet/roslyn/issues/66475

                // Uses siblings on the left (and/or the start of the parent) to determine position.
                return GetChildPositionFromStart(index);
            }
        }
    }
}
