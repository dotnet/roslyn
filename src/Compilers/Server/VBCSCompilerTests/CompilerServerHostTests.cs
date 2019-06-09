using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.CodeAnalysis.CommandLine.BuildResponse;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public sealed class CompilerServerHostTests
    {
        internal sealed class TestableCompilerServerHost : CompilerServerHost
        {
            public override IAnalyzerAssemblyLoader AnalyzerAssemblyLoader { get; }

            public override Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider { get; }

            public TestableCompilerServerHost(
                IAnalyzerAssemblyLoader loader = null,
                Func<string, MetadataReferenceProperties, PortableExecutableReference> assemblyReferenceProvider = null) :
                base(ServerUtil.DefaultClientDirectory, ServerUtil.DefaultSdkDirectory)
            {
                AnalyzerAssemblyLoader = loader;
                AssemblyReferenceProvider = assemblyReferenceProvider;
            }

            public override bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers)
            {
                return true;
            }
        }

        [WorkItem(13995, "https://github.com/dotnet/roslyn/issues/13995")]
        [Fact]
        public void RejectEmptyTempPath()
        {
            using (var temp = new TempRoot())
            {
                var host = new TestableCompilerServerHost();
                var request = new RunRequest(LanguageNames.CSharp, currentDirectory: temp.CreateDirectory().Path, tempDirectory: null, libDirectory: null, arguments: Array.Empty<string>());
                var response = host.RunCompilation(request, CancellationToken.None);
                Assert.Equal(ResponseType.Rejected, response.Type);
            }
        }
    }
}
