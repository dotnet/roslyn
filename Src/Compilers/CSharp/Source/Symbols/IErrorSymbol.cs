using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    public interface IErrorSymbol
    {
        DiagnosticInfo ErrorInfo { get; }
    }
}