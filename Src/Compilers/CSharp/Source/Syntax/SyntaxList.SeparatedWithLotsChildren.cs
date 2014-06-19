using System;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal partial class SyntaxList
    {
        internal class SeparatedWithLotsOfChildren : SeparatedWithManyChildren
        {
            private readonly int[] positions;

            internal SeparatedWithLotsOfChildren(SyntaxNode parent, Syntax.InternalSyntax.SyntaxList.WithManyChildren green, int position)
                : base(parent, green, position)
            {
                var positions = new int[(green.SlotCount + 1) >> 1];
                var curPosition = position;

                for(int i = 0, cnt = green.SlotCount; i < cnt; i++)
                {
                    if ((i & 1) == 0)
                    {
                        positions[i >> 1] = curPosition;
                    }

                    var child = green.GetSlot(i);
                    if (child != null)
                    {
                        curPosition += child.FullWidth;
                    }
                }

                this.positions = positions;
            }

            internal override int GetChildPosition(int i)
            {
                var position = positions[i >> 1];

                if ((i & 1) != 0)
                {
                    //separator
                    position += this.Green.GetSlot(i - 1).FullWidth;
                }

                return position;
            }

            public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }

            public override void Accept(SyntaxVisitor visitor)
            {
                throw new NotImplementedException();
            }
        }
    }
}