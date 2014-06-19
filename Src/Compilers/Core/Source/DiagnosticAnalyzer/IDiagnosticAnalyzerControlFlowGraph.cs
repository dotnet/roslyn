using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Not yet implemented.
    /// </summary>
    [Obsolete]
    public interface IDiagnosticAnalyzerControlFlowGraph : IDiagnosticAnalyzer
    {
        [Obsolete]
        void OnCodeBody(ISymbol symbol, /*IControlFlowGraph*/object cfg, SemanticModel model, DiagnosticSink diagnostics);
    }
}
