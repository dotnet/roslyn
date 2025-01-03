// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal partial class SyntaxList
    {
        internal sealed class WithFourChildren : SyntaxList
        {
            private SyntaxNode? _child0;
            private SyntaxNode? _child1;
            private SyntaxNode? _child2;
            private SyntaxNode? _child3;

            internal WithFourChildren(InternalSyntax.SyntaxList green, SyntaxNode? parent, int position)
                : base(green, parent, position)
            {
            }

            internal override SyntaxNode? GetNodeSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return this.GetRedElement(ref _child0, 0);
                    case 1:
                        return this.GetRedElementIfNotToken(ref _child1, 1);
                    case 2:
                        return this.GetRedElement(ref _child2, 2);
                    case 3:
                        return this.GetRedElementIfNotToken(ref _child3, 3);
                    default:
                        return null;
                }
            }

            internal override SyntaxNode? GetCachedSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    case 2:
                        return _child2;
                    case 3:
                        return _child3;
                    default:
                        return null;
                }
            }
        }
    }
}
