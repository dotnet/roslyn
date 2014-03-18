using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An analyzer that is invoked on each compilation unit. Analyzers that implement this interface
    /// may report diagnostics based on the syntax and semantics of the compilation unit. For performance,
    /// you should consider performing analyses inside of method bodies using <see cref="ISyntaxNodeAnalyzer{T}"/>.
    /// </summary>
    public interface ICompilationUnitAnalyzer : IDiagnosticAnalyzer
    {
        /// <summary>
        /// Called for each compilation unit in the compilation.
        /// </summary>
        /// <param name="compilationUnit">A syntax tree of the compilation unit</param>
        /// <param name="semanticModel">A SemanticModel for the compilation unit</param>
        /// <param name="addDiagnostic">A delegate to be used to emit diagnostics</param>
        /// <param name="cancellationToken">A token for cancelling the computation</param>
        void AnalyzeCompilationUnit(SyntaxTree compilationUnit, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);
    }
}
