// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial class SyntaxList
    {
        internal abstract class WithManyChildrenBase : SyntaxList
        {
            internal readonly ArrayElement<GreenNode>[] children;

            internal WithManyChildrenBase(ArrayElement<GreenNode>[] children)
            {
                this.children = children;
                this.InitializeChildren();
            }

            internal WithManyChildrenBase(DiagnosticInfo[]? diagnostics, SyntaxAnnotation[]? annotations, ArrayElement<GreenNode>[] children)
                : base(diagnostics, annotations)
            {
                this.children = children;
                this.InitializeChildren();
            }

            private void InitializeChildren()
            {
                int n = children.Length;

                // Attempt to store small lengths directly into the storage provided within GreenNode. If, however, the
                // length is too long, we will store a special value in the space, which will `SlotCount` to call back
                // into `GetSlotCount` to retrieve the true length.
                this.SlotCount = n < SlotCountTooLarge
                    ? (byte)n
                    : SlotCountTooLarge;

                for (int i = 0; i < children.Length; i++)
                {
                    this.AdjustFlagsAndWidth(children[i]);
                }
            }

            protected override int GetSlotCount()
            {
                return children.Length;
            }

            internal override GreenNode GetSlot(int index)
            {
                return this.children[index];
            }

            internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
            {
                Array.Copy(this.children, 0, array, offset, this.children.Length);
            }

            internal override SyntaxNode CreateRed(SyntaxNode? parent, int position)
            {
                var separated = this.SlotCount > 1 && HasNodeTokenPattern();
                if (parent != null && parent.ShouldCreateWeakList())
                {
                    return separated
                        ? new Syntax.SyntaxList.SeparatedWithManyWeakChildren(this, parent, position)
                        : (SyntaxNode)new Syntax.SyntaxList.WithManyWeakChildren(this, parent, position);
                }
                else
                {
                    return separated
                        ? new Syntax.SyntaxList.SeparatedWithManyChildren(this, parent, position)
                        : (SyntaxNode)new Syntax.SyntaxList.WithManyChildren(this, parent, position);
                }
            }

            private bool HasNodeTokenPattern()
            {
                for (int i = 0; i < this.SlotCount; i++)
                {
                    // even slots must not be tokens, odds slots must be tokens
                    if (this.GetSlot(i).IsToken == ((i & 1) == 0))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal sealed class WithManyChildren : WithManyChildrenBase
        {
            internal WithManyChildren(ArrayElement<GreenNode>[] children)
                : base(children)
            {
            }

            internal WithManyChildren(DiagnosticInfo[]? diagnostics, SyntaxAnnotation[]? annotations, ArrayElement<GreenNode>[] children)
                : base(diagnostics, annotations, children)
            {
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[]? errors)
            {
                return new WithManyChildren(errors, this.GetAnnotations(), children);
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[]? annotations)
            {
                return new WithManyChildren(GetDiagnostics(), annotations, children);
            }
        }
    }
}
