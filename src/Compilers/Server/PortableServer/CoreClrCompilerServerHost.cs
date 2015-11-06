using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CoreClrCompilerServerHost : ICompilerServerHost
    {
        // Caches are used by C# and VB compilers, and shared here.
        private static readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> s_assemblyReferenceProvider =
            (path, properties) => new CachingMetadataReference(path, properties);

        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader = new CoreClrAnalyzerAssemblyLoader();

        public IAnalyzerAssemblyLoader AnalyzerAssemblyLoader => _analyzerAssemblyLoader;

        public Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider => s_assemblyReferenceProvider;

        public bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
        {
            return false;
        }

        public string GetSdkDirectory()
        {
            return null;
        }

        public void Log(string message)
        {
            // BTODO: delete this interface method
        }
    }
}
