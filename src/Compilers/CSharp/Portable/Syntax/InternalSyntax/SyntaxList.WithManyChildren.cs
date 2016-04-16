// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxList
    {
        internal abstract class WithManyChildrenBase : SyntaxList
        {
            internal readonly ArrayElement<CSharpSyntaxNode>[] children;

            internal WithManyChildrenBase(ArrayElement<CSharpSyntaxNode>[] children)
            {
                this.children = children;
                this.InitializeChildren();
            }

            private void InitializeChildren()
            {
                int n = children.Length;
                if (n < byte.MaxValue)
                {
                    this.SlotCount = (byte)n;
                }
                else
                {
                    this.SlotCount = byte.MaxValue;
                }

                for (int i = 0; i < children.Length; i++)
                {
                    this.AdjustFlagsAndWidth(children[i]);
                }
            }

            internal WithManyChildrenBase(ObjectReader reader)
                : base(reader)
            {
                var length = reader.ReadInt32();

                this.children = new ArrayElement<CSharpSyntaxNode>[length];
                for (var i = 0; i < length; i++)
                {
                    this.children[i].Value = (CSharpSyntaxNode)reader.ReadValue();
                }

                this.InitializeChildren();
            }

            internal override void WriteTo(ObjectWriter writer)
            {
                base.WriteTo(writer);

                // PERF: Write the array out manually.Profiling shows that this is cheaper than converting to 
                // an array in order to use writer.WriteValue.
                writer.WriteInt32(this.children.Length);

                for (var i = 0; i < this.children.Length; i++)
                {
                    writer.WriteValue(this.children[i].Value);
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

            internal override void CopyTo(ArrayElement<CSharpSyntaxNode>[] array, int offset)
            {
                Array.Copy(this.children, 0, array, offset, this.children.Length);
            }

            internal override SyntaxNode CreateRed(SyntaxNode parent, int position)
            {
                var p = parent;
                if (p != null && p is CSharp.Syntax.BlockSyntax)
                {
                    var gp = p.Parent;
                    if (gp != null && (gp is CSharp.Syntax.MemberDeclarationSyntax || gp is CSharp.Syntax.AccessorDeclarationSyntax))
                    {
                        Debug.Assert(!this.GetSlot(0).IsToken);
                        return new CSharp.Syntax.SyntaxList.WithManyWeakChildren(this, parent, position);
                    }
                }

                if (this.SlotCount > 1 && HasNodeTokenPattern())
                {
                    return new CSharp.Syntax.SyntaxList.SeparatedWithManyChildren(this, parent, position);
                }
                else
                {
                    return new CSharp.Syntax.SyntaxList.WithManyChildren(this, parent, position);
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

            public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor)
            {
                throw new NotImplementedException();
            }

            public override void Accept(CSharpSyntaxVisitor visitor)
            {
                throw new NotImplementedException();
            }
        }

        internal sealed class WithManyChildren : WithManyChildrenBase
        {
            internal WithManyChildren(ArrayElement<CSharpSyntaxNode>[] children)
                : base(children)
            {
            }

            internal WithManyChildren(ObjectReader reader)
                : base(reader)
            {
            }

            internal override Func<ObjectReader, object> GetReader()
            {
                return r => new WithManyChildren(r);
            }
        }
    }
}
