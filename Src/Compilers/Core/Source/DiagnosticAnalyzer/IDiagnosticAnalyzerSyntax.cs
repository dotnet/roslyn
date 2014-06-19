using Microsoft.CodeAnalysis.Text;
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
    public interface IDiagnosticAnalyzerSyntax : IDiagnosticAnalyzer
    {
        [Obsolete]
        void OnSyntaxTree(SyntaxTree tree, SemanticModel model, TextSpan span, DiagnosticSink diagnostics);
    }
}
