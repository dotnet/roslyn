using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Controls overflow checks. Created for checked/unchecked blocks.
    /// </summary>
    internal sealed class CheckedUncheckedBinder : Binder
    {
        public bool Checked { get; private set; } 

        public CheckedUncheckedBinder(Binder next, bool @checked)
            : base(next)
        {
            this.Checked = @checked;
        }

        protected override OverflowChecks CheckOverflow
        {
            get
            {
                return Checked ? OverflowChecks.Enabled : OverflowChecks.Disabled;
            }
        }
    }
}
