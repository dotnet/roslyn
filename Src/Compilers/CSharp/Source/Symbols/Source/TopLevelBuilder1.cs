using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp
{
    internal class TopLevelBuilder1 : NamespaceBuilder1
    {
        private readonly DiagnosticBag diagnosticBag;

        internal TopLevelBuilder1(DiagnosticBag bag, Symbol owner, SingleDeclaration declaration, BinderContext enclosingContext)
            : base(owner, declaration, enclosingContext)
        {
            this.diagnosticBag = bag;
        }

        internal override DiagnosticInfo ReportDiagnostic(Diagnostic diagnostic)
        {
            diagnosticBag.Add(diagnostic);
            return diagnostic.Info;
        }
    }
}