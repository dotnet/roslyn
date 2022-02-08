// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
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
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Move the PDB to a path that only our fake debugger service knows about
                var pdbFilePath = Path.Combine(path, "SourceLink.pdb");
                File.Move(GetPdbPath(path), pdbFilePath);

                var sourceLinkService = new TestSourceLinkService(pdbFilePath: pdbFilePath);
                var service = new PdbFileLocatorService(sourceLinkService);

                using var result = await service.GetDocumentDebugInfoReaderAsync(GetDllPath(path), logger: null, CancellationToken.None);

                Assert.NotNull(result);
            });
        }

        [Fact]
        public async Task DoesntReadNonPortablePdbs()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Move the PDB to a path that only our fake debugger service knows about
                var pdbFilePath = Path.Combine(path, "SourceLink.pdb");
                File.Move(GetPdbPath(path), pdbFilePath);

                var sourceLinkService = new TestSourceLinkService(pdbFilePath: pdbFilePath, isPortablePdb: false);
                var service = new PdbFileLocatorService(sourceLinkService);

                using var result = await service.GetDocumentDebugInfoReaderAsync(GetDllPath(path), logger: null, CancellationToken.None);

                Assert.Null(result);
            });
        }

        [Fact]
        public async Task NoPdbFoundReturnsNull()
        {
            var source = @"
public class C
{
    public event System.EventHandler [|E|] { add { } remove { } }
}";

            await RunTestAsync(async path =>
            {
                MarkupTestFile.GetSpan(source, out var metadataSource, out var expectedSpan);

                var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.OnDisk, Location.OnDisk, metadataSource, c => c.GetMember("C.E"));

                // Move the PDB to a path that only our fake debugger service knows about
                var pdbFilePath = Path.Combine(path, "SourceLink.pdb");
                File.Move(GetPdbPath(path), pdbFilePath);

                var sourceLinkService = new TestSourceLinkService(pdbFilePath: null);
                var service = new PdbFileLocatorService(sourceLinkService);

                using var result = await service.GetDocumentDebugInfoReaderAsync(GetDllPath(path), logger: null, CancellationToken.None);

                Assert.Null(result);
            });
        }
    }
}
