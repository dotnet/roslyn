// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal partial class SyntaxList
    {
        internal class SeparatedWithManyChildren : SyntaxList
        {
            private readonly ArrayElement<SyntaxNode>[] _children;

            internal SeparatedWithManyChildren(InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
                : base(green, parent, position)
            {
                _children = new ArrayElement<SyntaxNode>[(green.SlotCount + 1) >> 1];
            }

            internal override SyntaxNode GetNodeSlot(int i)
            {
                if ((i & 1) != 0)
                {
                    //separator
                    return null;
                }

                return this.GetRedElement(ref _children[i >> 1].Value, i);
            }

            internal override SyntaxNode GetCachedSlot(int i)
            {
                if ((i & 1) != 0)
                {
                    //separator
                    return null;
                }

                return _children[i >> 1].Value;
            }
        }
    }
}
