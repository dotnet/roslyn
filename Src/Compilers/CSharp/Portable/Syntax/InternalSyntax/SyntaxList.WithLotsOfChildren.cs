// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxList
    {
        internal sealed class WithLotsOfChildren : WithManyChildrenBase
        {
            private readonly int[] childOffsets;

            internal WithLotsOfChildren(ArrayElement<CSharpSyntaxNode>[] children)
                : base(children)
            {
                this.childOffsets = CalculateOffsets(children);
            }

            internal WithLotsOfChildren(ObjectReader reader)
                : base(reader)
            {
                this.childOffsets = CalculateOffsets(this.children);
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
                return this.childOffsets[index];
            }

            private static int[] CalculateOffsets(ArrayElement<CSharpSyntaxNode>[] children)
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
        }
    }
}
