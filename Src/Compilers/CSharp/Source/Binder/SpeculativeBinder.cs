using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class SpeculativeBinder : Binder
    {
        public SpeculativeBinder(Binder next)
            : base(next)
        {
        }

        internal override bool IsSpeculativeBinder
        {
            get
            {
                return true;
            }
        }
    }
}