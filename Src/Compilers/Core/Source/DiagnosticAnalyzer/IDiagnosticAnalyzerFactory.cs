using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Obsolete]
    public interface IDiagnosticAnalyzerFactory
    {
        // should probably include additional properties in here such as the diagnostic categories
        // and the diagnostics that this is capable of producing.

        /// <summary>
        /// Returns list of diagnostics that can be produced by analyzers created by this factory
        /// </summary>
        IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics();

        [Obsolete]
        IDiagnosticAnalyzer OnCompilationStarted(Compilation compilation);
    }
}
