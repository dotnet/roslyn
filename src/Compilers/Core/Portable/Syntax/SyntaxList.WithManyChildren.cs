// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal partial class SyntaxList
    {
        internal sealed class WithManyChildren : SyntaxList
        {
            private readonly ArrayElement<SyntaxNode?>[] _children;

            internal WithManyChildren(InternalSyntax.SyntaxList green, SyntaxNode? parent, int position)
                : base(green, parent, position)
            {
                _children = new ArrayElement<SyntaxNode?>[green.SlotCount];
            }

            internal override SyntaxNode? GetNodeSlot(int index)
            {
                return this.GetRedElement(ref _children[index].Value, index);
            }

            internal override SyntaxNode? GetCachedSlot(int index)
            {
                return _children[index];
            }
        }
    }
}
