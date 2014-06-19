// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    public abstract class SyntaxNonTerminal : SyntaxNode
    {
        internal SyntaxNonTerminal(SyntaxNode parent, InternalSyntax.SyntaxNode green, int position)
            : base(parent, green, position)
        {
        }

        internal IBaseSyntaxNodeExt GetRed(ref IBaseSyntaxNodeExt field, int slot)
        {
            if (field == null)
            {
                var green = this.Green.GetSlot(slot);
                if (green != null)
                {
                    if (green is InternalSyntax.SyntaxToken)
                    {
                        field = green;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref field, green.ToRed(this), null);
                    }
                }
            }

            return field;
        }

        protected T GetRed<T>(ref T field, int slot) where T : SyntaxNode
        {
            if (field == null)
            {
                var green = this.Green.GetSlot(slot);
                if (green != null)
                {
                    T newRed = (T)(object)green.ToRed(this);
                    Interlocked.CompareExchange(ref field, newRed, (T)null);
                }
            }

            return field;
        }
    }
}