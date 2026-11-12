// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public class CSharpRebuildTests : CSharpTestBase
    {
        [Fact]
        public void TopLevelStatements()
        {
            var original = CreateCompilation(
                @"System.Console.WriteLine(""I'm using top-level statements!"");",
                options: TestOptions.DebugExe);
            original.VerifyDiagnostics();

            var originalBytes = original.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            var originalPeReader = new PEReader(originalBytes);
            var originalPdbReader = originalPeReader.GetEmbeddedPdbMetadataReader()!;
            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("Test");

            var optionsReader = new CompilationOptionsReader(logger, originalPdbReader, originalPeReader);
            var compilationFactory = CompilationFactory.Create("test.exe", optionsReader);

            var sources = original
                .SyntaxTrees
                .Select(x => compilationFactory.CreateSyntaxTree(x.FilePath, x.GetText()))
                .ToImmutableArray();
            var references = original.References.ToImmutableArray();
            var rebuild = compilationFactory.CreateCompilation(sources, original.References.ToImmutableArray());
            rebuild.VerifyEmitDiagnostics();
        }

        [Theory]
        [InlineData(256)]
        [InlineData(1024)]
        public void EmbeddedSourceCompression(int lineCount)
        {
            var sourceBuilder = new StringBuilder("internal static class C { }");

            // Deterministic xorshift data makes the comments non-trivial to compress,
            // while the two line counts exercise the StringText and LargeText embedded-source paths.
            uint value = 0x12345678;
            for (var i = 0; i < lineCount; i++)
            {
                sourceBuilder.AppendLine();
                sourceBuilder.Append("// ");
                for (var j = 0; j < 8; j++)
                {
                    value ^= value << 13;
                    value ^= value >> 17;
                    value ^= value << 5;
                    sourceBuilder.Append(value.ToString("X8"));
                }
            }

            var sourceText = SourceText.From(sourceBuilder.ToString(), Encoding.UTF8, SourceHashAlgorithm.Sha256);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: "Generated.g.cs");
            var original = CreateCompilation(
                syntaxTree,
                options: TestOptions.DebugDll.WithDeterministic(true));
            original.VerifyDiagnostics();

            using var peStream = new MemoryStream();
            var emitResult = original.Emit(
                peStream,
                options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded),
                embeddedTexts: [EmbeddedText.FromSource(syntaxTree.FilePath, sourceText)]);
            emitResult.Diagnostics.Verify();
            Assert.True(emitResult.Success);

            peStream.Position = 0;
            RoundTripUtil.VerifyRoundTrip(
                peStream,
                pdbStream: null,
                assemblyFileName: original.AssemblyName + ".dll",
                new CompilationRebuildArtifactResolver(original));
        }
    }
}
