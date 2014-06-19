using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers;

namespace Roslyn.Compilers.CSharp
{
    internal interface ISourceSymbol
    {
        bool IsDeclaredBy(SyntaxNode node);
    }
}