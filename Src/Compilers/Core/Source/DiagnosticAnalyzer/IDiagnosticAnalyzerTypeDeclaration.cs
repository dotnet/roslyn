using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Obsolete]
    public interface IDiagnosticAnalyzerTypeDeclaration : IDiagnosticAnalyzer
    {
        [Obsolete]
        void OnDeclared(INamedTypeSymbol symbol, DiagnosticSink diagnostics);
    }
}
