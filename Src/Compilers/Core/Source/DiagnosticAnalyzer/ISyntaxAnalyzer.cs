using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An analyzer that is invoked on each syntax tree in the compilation. Implementations
    /// should report diagnostics based primarily on the text of the program (implement <see cref="ICompilationUnitAnalyzer"/>
    /// instead if you want to use semantic information).
    /// </summary>
    public interface ISyntaxAnalyzer : IDiagnosticAnalyzer
    {
        /// <summary>
        /// Called for each tree in the compilation.
        /// </summary>
        /// <param name="tree">A tree of the compilation</param>
        /// <param name="addDiagnostic">A delegate to be used to emit diagnostics</param>
        /// <param name="cancellationToken">A token for cancelling the computation</param>
        void AnalyzeTree(SyntaxTree tree, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);
    }
}
