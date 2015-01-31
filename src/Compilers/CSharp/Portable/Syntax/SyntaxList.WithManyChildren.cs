// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal partial class SyntaxList
    {
        internal class WithManyChildren : SyntaxList
        {
            private readonly ArrayElement<SyntaxNode>[] _children;

            internal WithManyChildren(Syntax.InternalSyntax.SyntaxList green, SyntaxNode parent, int position)
                : base(green, parent, position)
            {
                _children = new ArrayElement<SyntaxNode>[green.SlotCount];
            }

            internal override SyntaxNode GetNodeSlot(int index)
            {
                return this.GetRedElement(ref _children[index].Value, index);
            }

            internal override SyntaxNode GetCachedSlot(int index)
            {
                return _children[index];
            }

            public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }

            public override void Accept(CSharpSyntaxVisitor visitor)
            {
                throw new NotImplementedException();
            }
        }
    }
}
