// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial class SyntaxList
    {
        internal class WithTwoChildren : SyntaxList
        {
            private readonly GreenNode _child0;
            private readonly GreenNode _child1;

            internal WithTwoChildren(GreenNode child0, GreenNode child1)
            {
                this.SlotCount = 2;
                this.AdjustFlagsAndWidth(child0);
                _child0 = child0;
                this.AdjustFlagsAndWidth(child1);
                _child1 = child1;
            }

            internal WithTwoChildren(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, GreenNode child0, GreenNode child1)
                : base(diagnostics, annotations)
            {
                this.SlotCount = 2;
                this.AdjustFlagsAndWidth(child0);
                _child0 = child0;
                this.AdjustFlagsAndWidth(child1);
                _child1 = child1;
            }

            internal WithTwoChildren(ObjectReader reader)
                : base(reader)
            {
                this.SlotCount = 2;
                _child0 = (GreenNode)reader.ReadValue();
                this.AdjustFlagsAndWidth(_child0);
                _child1 = (GreenNode)reader.ReadValue();
                this.AdjustFlagsAndWidth(_child1);
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(_child0);
                writer.WriteValue(_child1);
            }

            internal override Func<ObjectReader, object> GetReader()
            {
                return r => new WithTwoChildren(r);
            }

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    default:
                        return null;
                }
            }

            internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
            {
                return new Syntax.SyntaxList.WithTwoChildren(this, parent, position);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] errors)
            {
                return new WithTwoChildren(errors, this.GetAnnotations(), _child0, _child1);
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new WithTwoChildren(GetDiagnostics(), annotations, _child0, _child1);
            }
        }
    }
}