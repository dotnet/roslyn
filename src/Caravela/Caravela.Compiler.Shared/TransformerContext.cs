using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#if CARAVELA_COMPILER_INTERFACE
// missing constructor causes warnings
#nullable disable
#endif

namespace Caravela.Compiler
{
    /// <summary>
    /// Context passed to a source transformer when <see cref="ISourceTransformer.Execute(TransformerContext)"/> is called.
    /// </summary>
    public class TransformerContext
    {
#if !CARAVELA_COMPILER_INTERFACE
        private readonly DiagnosticBag _diagnostics;
        private readonly IAnalyzerAssemblyLoader _assemblyLoader;

        internal TransformerContext(
            Compilation compilation, AnalyzerConfigOptions globalOptions, IList<ResourceDescription> manifestResources, DiagnosticBag diagnostics,
            IAnalyzerAssemblyLoader assemblyLoader)
        {
            Compilation = compilation;
            GlobalOptions = globalOptions;
            ManifestResources = manifestResources;
            _diagnostics = diagnostics;
            _assemblyLoader = assemblyLoader;
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
        /// Can be used to inspect or modify (usually, add) resources of the assembly.
        /// 
        /// To inspect existing resources, use extension method from <see cref="ResourceDescriptionExtensions "/>.
        /// </summary>
        public IList<ResourceDescription> ManifestResources { get; }

        /// <summary>
        /// Adds a <see cref="Diagnostic"/> to the user's compilation.
        /// </summary>
        /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
        /// <remarks>
        /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
        /// </remarks>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
#if !CARAVELA_COMPILER_INTERFACE
            _diagnostics.Add(diagnostic);
#endif
        }

        public Assembly LoadReferencedAssembly(IAssemblySymbol assemblySymbol)
        {
#if CARAVELA_COMPILER_INTERFACE
            throw new InvalidOperationException("This operation works only inside Caravela.");
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
