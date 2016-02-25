using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Instrumentation
    {
        internal static void GenerateInstrumentationTables(MethodSymbol method, BoundBlock methodBody)
        {
            if (methodBody != null)
            {

            }
        }
    }
}
