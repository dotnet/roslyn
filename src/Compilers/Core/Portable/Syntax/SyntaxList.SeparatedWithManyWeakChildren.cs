// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal partial class SyntaxList
    {
        internal sealed class SeparatedWithManyWeakChildren : SyntaxList
        {
            private readonly ArrayElement<WeakReference<SyntaxNode>?>[] _children;

            internal SeparatedWithManyWeakChildren(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
                : base(green, parent, position)
            {
                _children = new ArrayElement<WeakReference<SyntaxNode>?>[(((green.SlotCount + 1) >> 1) - 1)];
            }

            internal override SyntaxNode? GetNodeSlot(int i)
            {
                SyntaxNode? result = null;

                if ((i & 1) == 0)
                {
                    // not a separator
                    result = GetWeakRedElement(ref this._children[i >> 1].Value, i);
                }

                return result;
            }

            internal override SyntaxNode? GetCachedSlot(int i)
            {
                SyntaxNode? result = null;

                if ((i & 1) == 0)
                {
                    // not a separator
                    var weak = this._children[i >> 1].Value;
                    if (weak != null)
                    {
                        weak.TryGetTarget(out result);
                    }
                }

                return result;
            }
        }
    }
}
