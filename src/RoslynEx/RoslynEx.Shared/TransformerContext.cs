using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#if ROSLYNEX_INTERFACE
// missing constructor causes warnings
#nullable disable
#endif

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
        private readonly IAnalyzerAssemblyLoader _assemblyLoader;

        internal TransformerContext(
            Compilation compilation, AnalyzerConfigOptions globalOptions, DiagnosticBag diagnostics, IAnalyzerAssemblyLoader assemblyLoader)
        {
            Compilation = compilation;
            GlobalOptions = globalOptions;
            _diagnostics = diagnostics;
            _assemblyLoader = assemblyLoader;
            ManifestResources = new List<ResourceDescription>();
        }
#endif

        /// <summary>
        /// Get the current <see cref="Compilation"/> at the time of execution.
        /// </summary>
        public Compilation Compilation { get; }

        /// <summary>
        /// Allows access to global options provided by an analyzer config,
        /// which can in turn come from the csproj file.
        /// </summary>
        public AnalyzerConfigOptions GlobalOptions { get; }

        /// <summary>
        /// Adds a managed resource to the assembly.
        /// </summary>
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

        public Assembly LoadReferencedAssembly(IAssemblySymbol assemblySymbol)
        {
#if ROSLYNEX_INTERFACE
            throw new InvalidOperationException("This operation works only inside RoslynEx.");
#else
            if (Compilation.GetMetadataReference(assemblySymbol) is not { } reference)
                throw new ArgumentException("Could not retrive MetadataReference for the given assembly symbol.", nameof(assemblySymbol));

            if (reference is not PortableExecutableReference peReference)
                throw new ArgumentException("The given assembly symbol does not correspond to a PE reference.", nameof(assemblySymbol));

            if (peReference.FilePath is not { } path)
                throw new ArgumentException("Could not access path for the given assembly symbol.", nameof(assemblySymbol));

            return _assemblyLoader.LoadFromPath(path);
#endif
        }
    }
}
