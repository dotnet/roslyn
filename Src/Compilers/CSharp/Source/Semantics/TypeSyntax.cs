using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Compilers.CSharp.Symbols.Source;

namespace Roslyn.Compilers.CSharp
{
    public abstract partial class TypeSyntax : ExpressionSyntax
    {
        public bool IsVar { get { return this.GetText() == "var"; } }
    }
}