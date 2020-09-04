using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RoslynEx
{
    /// <summary>
    /// Context passed to a source transformer when <see cref="ISourceTransformer.Execute(TransformerContext)"/> is called.
    /// </summary>
    public class TransformerContext
    {
        internal List<ResourceDescription> ManifestResources { get; }

#if !ROSLYNEX_INTERFACE
        private readonly DiagnosticBag _diagnostics;

        internal TransformerContext(
            Compilation compilation, AnalyzerConfigOptions globalOptions, DiagnosticBag diagnostics)
        {
            Compilation = compilation;
            GlobalOptions = globalOptions;
            _diagnostics = diagnostics;
            ManifestResources = new List<ResourceDescription>();
        }
#endif

        /// <summary>
        /// Get the current <see cref="Compilation"/> at the time of execution.
        /// </summary>
        public Compilation Compilation { get; }

        /// <summary>
        /// Allows access to global options provided by an analyzer config
        /// </summary>
        public AnalyzerConfigOptions GlobalOptions { get; }

        public void AddManifestResource(ResourceDescription resource) => ManifestResources.Add(resource);

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
