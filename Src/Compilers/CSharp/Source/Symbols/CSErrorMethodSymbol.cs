using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers.Internal;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// An error method, used to represent the result of overload resolution when binding fails.
    /// </summary>
    internal class CSErrorMethodSymbol : ErrorMethodSymbol
    {
        private readonly string name;
        private readonly TypeSymbol returnType;
        private readonly DiagnosticInfo diagnostic;

        internal CSErrorMethodSymbol(string name, TypeSymbol returnType, DiagnosticInfo diagnostic)
        {
            this.name = name;
            this.returnType = returnType;
            this.diagnostic = diagnostic;
        }

        public override string Name
        {
            get { return this.name; }
        }

        public override TypeSymbol ReturnType
        {
            get { return this.returnType; }
        }

        public override DiagnosticInfo ErrorInfo
        {
            get { return this.diagnostic; }
        }
    }
}
