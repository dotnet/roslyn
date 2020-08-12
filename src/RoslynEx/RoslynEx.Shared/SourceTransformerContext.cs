using Microsoft.CodeAnalysis;

namespace RoslynEx
{
    /// <summary>
    /// Context passed to a source transformer when <see cref="ISourceTransformer.Execute(SourceTransformerContext)"/> is called.
    /// </summary>
    public readonly struct SourceTransformerContext
    {
#if !ROSLYNEX_INTERFACE
        private readonly DiagnosticBag _diagnostics;

        internal SourceTransformerContext(Compilation compilation, DiagnosticBag diagnostics)
        {
            Compilation = compilation;
            _diagnostics = diagnostics;
        }
#endif

        /// <summary>
        /// Get the current <see cref="Compilation"/> at the time of execution.
        /// </summary>
        public Compilation Compilation { get; }

        /// <summary>
        /// Adds a <see cref="Diagnostic"/> to the user's compilation.
        /// </summary>
        /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
        /// <remarks>
        /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
        /// </remarks>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
#if !ROSLYNEX_INTERFACE
            _diagnostics.Add(diagnostic);
#endif
        }
    }
}
