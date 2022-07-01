// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument
{
    public class PdbSourceDocumentLoaderServiceTests : AbstractPdbSourceDocumentTests
    {
        [Fact]
        public async Task ReturnsSourceFileFromSourceLink()
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

                // Move the source file to a path that only our fake debugger service knows about
                var sourceFilePath = Path.Combine(path, "SourceLink.cs");
                File.Move(GetSourceFilePath(path), sourceFilePath);

                var sourceLinkService = new Lazy<ISourceLinkService>(() => new TestSourceLinkService(sourceFilePath: sourceFilePath));
                var service = new PdbSourceDocumentLoaderService(sourceLinkService, logger: null);

                using var hash = SHA256.Create();
                var fileHash = hash.ComputeHash(File.ReadAllBytes(sourceFilePath));

                var sourceDocument = new SourceDocument("goo.cs", Text.SourceHashAlgorithm.Sha256, fileHash.ToImmutableArray(), null, "https://sourcelink");
                var result = await service.LoadSourceDocumentAsync(path, sourceDocument, Encoding.UTF8, new TelemetryMessage(CancellationToken.None), useExtendedTimeout: false, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equal(sourceFilePath, result!.FilePath);
                Assert.True(result.FromRemoteLocation);
            });
        }

        [Fact]
        public async Task NoUrlFoundReturnsNull()
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

                // Move the source file to a path that only our fake debugger service knows about
                var sourceFilePath = Path.Combine(path, "SourceLink.cs");
                File.Move(GetSourceFilePath(path), sourceFilePath);

                var sourceLinkService = new Lazy<ISourceLinkService>(() => new TestSourceLinkService(sourceFilePath: sourceFilePath));
                var service = new PdbSourceDocumentLoaderService(sourceLinkService, logger: null);

                var sourceDocument = new SourceDocument("goo.cs", Text.SourceHashAlgorithm.None, default, null, SourceLinkUrl: null);
                var result = await service.LoadSourceDocumentAsync(path, sourceDocument, Encoding.UTF8, new TelemetryMessage(CancellationToken.None), useExtendedTimeout: false, CancellationToken.None);

                Assert.Null(result);
            });
        }
    }
}
