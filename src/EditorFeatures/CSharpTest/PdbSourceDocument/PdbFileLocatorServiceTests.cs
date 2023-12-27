// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument
{
    public class PdbFileLocatorServiceTests : AbstractPdbSourceDocumentTests
    {
        [Fact]
        public async Task ReturnsPdbPathFromDebugger()
        {
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Move the PDB to a path that only our fake debugger service knows about
                var pdbFilePath = Path.Combine(path, "SourceLink.pdb");
                File.Move(GetPdbPath(path), pdbFilePath);

                var sourceLinkService = new TestSourceLinkService(pdbFilePath: pdbFilePath);
                var service = new PdbFileLocatorService(sourceLinkService, logger: null);

                using var result = await service.GetDocumentDebugInfoReaderAsync(GetDllPath(path), useDefaultSymbolServers: false, new TelemetryMessage(CancellationToken.None), CancellationToken.None);

                Assert.NotNull(result);
            });
        }

        [Fact]
        public async Task DoesntReadNonPortablePdbs()
        {
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                // Ideally we don't want to pass in true for windowsPdb here, and this is supposed to test that the service ignores non-portable PDBs when the debugger
                // tells us they're not portable, but the debugger has a bug at the moment.
                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"), windowsPdb: true);

                // Move the PDB to a path that only our fake debugger service knows about
                var pdbFilePath = Path.Combine(path, "SourceLink.pdb");
                File.Move(GetPdbPath(path), pdbFilePath);

                var sourceLinkService = new TestSourceLinkService(pdbFilePath);
                var service = new PdbFileLocatorService(sourceLinkService, logger: null);

                using var result = await service.GetDocumentDebugInfoReaderAsync(GetDllPath(path), useDefaultSymbolServers: false, new TelemetryMessage(CancellationToken.None), CancellationToken.None);

                Assert.Null(result);
            });
        }

        [Fact]
        public async Task NoPdbFoundReturnsNull()
        {
            var source = """
                public class C
                {
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
                """;

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Move the PDB to a path that only our fake debugger service knows about
                var pdbFilePath = Path.Combine(path, "SourceLink.pdb");
                File.Move(GetPdbPath(path), pdbFilePath);

                var sourceLinkService = new TestSourceLinkService(pdbFilePath: null);
                var service = new PdbFileLocatorService(sourceLinkService, logger: null);

                using var result = await service.GetDocumentDebugInfoReaderAsync(GetDllPath(path), useDefaultSymbolServers: false, new TelemetryMessage(CancellationToken.None), CancellationToken.None);

                Assert.Null(result);
            });
        }
    }
}
