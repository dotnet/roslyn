﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Syntax.InternalSyntax
{
    internal partial class SyntaxList
    {
        internal class WithThreeChildren : SyntaxList
        {
            static WithThreeChildren()
            {
                ObjectBinder.RegisterTypeReader(typeof(WithThreeChildren), r => new WithThreeChildren(r));
            }

            private readonly GreenNode _child0;
            private readonly GreenNode _child1;
            private readonly GreenNode _child2;

            internal WithThreeChildren(GreenNode child0, GreenNode child1, GreenNode child2)
            {
                this.SlotCount = 3;
                this.AdjustFlagsAndWidth(child0);
                _child0 = child0;
                this.AdjustFlagsAndWidth(child1);
                _child1 = child1;
                this.AdjustFlagsAndWidth(child2);
                _child2 = child2;
            }

            internal WithThreeChildren(DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, GreenNode child0, GreenNode child1, GreenNode child2)
                : base(diagnostics, annotations)
            {
                this.SlotCount = 3;
                this.AdjustFlagsAndWidth(child0);
                _child0 = child0;
                this.AdjustFlagsAndWidth(child1);
                _child1 = child1;
                this.AdjustFlagsAndWidth(child2);
                _child2 = child2;
            }

            internal WithThreeChildren(ObjectReader reader)
                : base(reader)
            {
                this.SlotCount = 3;
                _child0 = (GreenNode)reader.ReadValue();
                this.AdjustFlagsAndWidth(_child0);
                _child1 = (GreenNode)reader.ReadValue();
                this.AdjustFlagsAndWidth(_child1);
                _child2 = (GreenNode)reader.ReadValue();
                this.AdjustFlagsAndWidth(_child2);
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);
                writer.WriteValue(_child0);
                writer.WriteValue(_child1);
                writer.WriteValue(_child2);
            }

            internal override GreenNode GetSlot(int index)
            {
                switch (index)
                {
                    case 0:
                        return _child0;
                    case 1:
                        return _child1;
                    case 2:
                        return _child2;
                    default:
                        return null;
                }
            }

            internal override void CopyTo(ArrayElement<GreenNode>[] array, int offset)
            {
                array[offset].Value = _child0;
                array[offset + 1].Value = _child1;
                array[offset + 2].Value = _child2;
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
            {
                return new Syntax.SyntaxList.WithThreeChildren(this, parent, position);
            }

            internal override GreenNode SetDiagnostics(DiagnosticInfo[] errors)
            {
                return new WithThreeChildren(errors, this.GetAnnotations(), _child0, _child1, _child2);
            }

            internal override GreenNode SetAnnotations(SyntaxAnnotation[] annotations)
            {
                return new WithThreeChildren(GetDiagnostics(), annotations, _child0, _child1, _child2);
            }
        }
    }
}
