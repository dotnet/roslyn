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

namespace Roslyn.Compilers.CSharp.InternalSyntax
{
    internal abstract class SyntaxNonTerminal : SyntaxNode
    {
        internal SyntaxNonTerminal(SyntaxKind kind, DiagnosticInfo[] diagnostics = null)
            : base(kind, diagnostics: diagnostics)
        {
        }

        public override int Width
        {
            get { return this.FullWidth - this.LeadingWidth - this.TrailingWidth; }
        }

        internal override int LeadingWidth
        {
            get
            {
                var first = this.GetFirstToken();
                return first != null ? first.LeadingWidth : 0;
            }
        }

        internal override int TrailingWidth
        {
            get
            {
                var last = this.GetLastToken();
                return last != null ? last.TrailingWidth : 0;
            }
        }
    }
}