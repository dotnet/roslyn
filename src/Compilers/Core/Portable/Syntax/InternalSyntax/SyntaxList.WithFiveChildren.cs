// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial class SyntaxList
    {
        internal class WithFiveChildren : SyntaxList
        {
            private readonly GreenNode _child0;
            private readonly GreenNode _child1;
            private readonly GreenNode _child2;
            private readonly GreenNode _child3;
            private readonly GreenNode _child4;

            internal WithFiveChildren(GreenNode child0, GreenNode child1, GreenNode child2, GreenNode child3, GreenNode child4)
            {
                this.SlotCount = 5;
                this.AdjustFlagsAndWidth(child0);
                _child0 = child0;
                this.AdjustFlagsAndWidth(child1);
                _child1 = child1;
                this.AdjustFlagsAndWidth(child2);
                _child2 = child2;
                this.AdjustFlagsAndWidth(child3);
                _child3 = child3;
                this.AdjustFlagsAndWidth(child4);
                _child4 = child4;
            }

            internal WithFiveChildren(DiagnosticInfo[]? diagnostics, SyntaxAnnotation[]? annotations, GreenNode child0, GreenNode child1, GreenNode child2, GreenNode child3, GreenNode child4)
                : base(diagnostics, annotations)
            {
                this.SlotCount = 5;
                this.AdjustFlagsAndWidth(child0);
                _child0 = child0;
                this.AdjustFlagsAndWidth(child1);
                _child1 = child1;
                this.AdjustFlagsAndWidth(child2);
                _child2 = child2;
                this.AdjustFlagsAndWidth(child3);
                _child3 = child3;
                this.AdjustFlagsAndWidth(child4);
                _child4 = child4;
            }

            internal override GreenNode? GetSlot(int index)
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
                    case 4:
                        return _child4;
                    default:
                        return null;
                }
            }

            internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
                array[offset + 2].Value = _child2;
                array[offset + 3].Value = _child3;
                array[offset + 4].Value = _child4;
            }

            internal override SyntaxNode CreateRed(SyntaxNode? parent, int position)
            {
                return new Syntax.SyntaxList.WithFiveChildren(this, parent, position);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? errors)
            {
                return new WithFiveChildren(errors, this.GetAnnotations(), _child0, _child1, _child2, _child3, _child4);
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[]? annotations)
            {
                return new WithFiveChildren(GetDiagnostics(), annotations, _child0, _child1, _child2, _child3, _child4);
            }
        }
    }
}
