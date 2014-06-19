using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Not yet implemented for namespaces.
    /// </summary>
    [Obsolete]
    public interface IDiagnosticAnalyzerDeclaration : IDiagnosticAnalyzer
    {
        [Obsolete]
        void OnDeclared(ISymbol symbol, DiagnosticSink diagnostics);
    }
}
