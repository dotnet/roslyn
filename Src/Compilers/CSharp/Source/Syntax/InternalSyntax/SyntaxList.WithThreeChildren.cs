// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxList
    {
        internal class WithThreeChildren : SyntaxList
        {
            private readonly CSharpSyntaxNode child0;
            private readonly CSharpSyntaxNode child1;
            private readonly CSharpSyntaxNode child2;

            internal WithThreeChildren(CSharpSyntaxNode child0, CSharpSyntaxNode child1, CSharpSyntaxNode child2)
            {
                this.SlotCount = 3;
                this.AdjustFlagsAndWidth(child0);
                this.child0 = child0;
                this.AdjustFlagsAndWidth(child1);
                this.child1 = child1;
                this.AdjustFlagsAndWidth(child2);
                this.child2 = child2;
            }

            internal WithThreeChildren(ObjectReader reader)
                : base(reader)
            {
                this.SlotCount = 3;
                this.child0 = (CSharpSyntaxNode)reader.ReadValue();
                this.AdjustFlagsAndWidth(child0);
                this.child1 = (CSharpSyntaxNode)reader.ReadValue();
                this.AdjustFlagsAndWidth(child1);
                this.child2 = (CSharpSyntaxNode)reader.ReadValue();
                this.AdjustFlagsAndWidth(child2);
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(this.child0);
                writer.WriteValue(this.child1);
                writer.WriteValue(this.child2);
            }

            internal override Func<ObjectReader, object> GetReader()
            {
                return r => new WithThreeChildren(r);
            }

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return child0;
                    case 1:
                        return child1;
                    case 2:
                        return child2;
                    default:
                        return null;
                }
            }

            internal override void CopyTo(ArrayElement<CSharpSyntaxNode>[] array, int offset)
            {
                array[offset].Value = child0;
                array[offset + 1].Value = child1;
                array[offset + 2].Value = child2;
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
            {
                return new CSharp.Syntax.SyntaxList.WithThreeChildren(this, parent, position);
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