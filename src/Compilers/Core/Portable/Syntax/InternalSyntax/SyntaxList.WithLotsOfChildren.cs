// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial class SyntaxList
    {
        internal sealed class WithLotsOfChildren : WithManyChildrenBase
        {
            private readonly int[] _childOffsets;

            internal WithLotsOfChildren(ArrayElement<GreenNode>[] children)
                : base(children)
            {
                _childOffsets = CalculateOffsets(children);
            }

            internal WithLotsOfChildren(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, ArrayElement<GreenNode>[] children, int[] childOffsets)
                : base(diagnostics, annotations, children)
            {
                _childOffsets = childOffsets;
            }

            internal WithLotsOfChildren(ObjectReader reader)
                : base(reader)
            {
                _childOffsets = CalculateOffsets(this.children);
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                // don't write offsets out, recompute them on construction
            }

            internal override Func<ObjectReader, object> GetReader()
            {
                return r => new WithLotsOfChildren(r);
            }

            public override int GetSlotOffset(int index)
            {
                return _childOffsets[index];
            }

            /// <summary>
            /// Find the slot that contains the given offset.
            /// </summary>
            /// <param name="offset">The target offset. Must be between 0 and <see cref="GreenNode.FullWidth"/>.</param>
            /// <returns>The slot index of the slot containing the given offset.</returns>
            /// <remarks>
            /// This implementation uses a binary search to find the first slot that contains
            /// the given offset.
            /// </remarks>
            public override int FindSlotIndexContainingOffset(int offset)
            {
                Debug.Assert(offset >= 0 && offset < FullWidth);
                return _childOffsets.BinarySearchUpperBound(offset) - 1;
            }

            private static int[] CalculateOffsets(ArrayElement<GreenNode>[] children)
            {
                int n = children.Length;
                var childOffsets = new int[n];
                int offset = 0;
                for (int i = 0; i < n; i++)
                {
                    childOffsets[i] = offset;
                    offset += children[i].Value.FullWidth;
                }
                return childOffsets;
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] errors)
            {
                return new WithLotsOfChildren(errors, this.GetAnnotations(), children, _childOffsets);
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new WithLotsOfChildren(GetDiagnostics(), annotations, children, _childOffsets);
            }
        }
    }
}