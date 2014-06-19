using System;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal partial class SyntaxList
    {
        internal class WithLotsOfChildren : WithManyChildren
        {
            private readonly int[] childPositions;

            internal WithLotsOfChildren(SyntaxNode parent, Syntax.InternalSyntax.SyntaxList.WithManyChildren green, int position)
                : base(parent, green, position)
            {
                var childPositions = new int[green.SlotCount];

                int childPosition = position;
                var greenChildren = green.children;
                for (int i = 0; i < childPositions.Length; ++i)
                {
                    childPositions[i] = childPosition;
                    childPosition += greenChildren[i].Value.FullWidth;
                }

                this.childPositions = childPositions;
            }

            internal override int GetChildPosition(int index)
            {
                return childPositions[index];
            }
        }
    }
}