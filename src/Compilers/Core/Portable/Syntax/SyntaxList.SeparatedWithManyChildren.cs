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
                // If the previous sibling (ignoring separator) is not cached, but the next sibling
                // (ignoring separator) is cached, use the next sibling to determine position.
                int valueIndex = (index & 1) != 0 ? index - 1 : index;
                // The check for valueIndex >= Green.SlotCount - 2 ignores the last item because the last item
                // is a separator and separators are not cached. In those cases, when the index represents
                // the last or next to last item, we still want to calculate the position from the end of
                // the list rather than the start.
                if (valueIndex > 1
                    && GetCachedSlot(valueIndex - 2) is null
                    && (valueIndex >= Green.SlotCount - 2 || GetCachedSlot(valueIndex + 2) is { }))
                {
                    return GetChildPositionFromEnd(index);
                }

                return base.GetChildPosition(index);
            }
        }
    }
}
