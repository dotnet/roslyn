// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System;
using System.Reflection.PortableExecutable;
using BuildValidator;
using Castle.Core.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Xunit;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic;
using System.Text;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public class OptionRoundTripTests : CSharpTestBase
    {
        public static readonly object[][] s_platforms = ((Platform[])Enum.GetValues(typeof(Platform))).Select(p => new[] { (object)p }).ToArray();

        [Theory]
        [MemberData(nameof(s_platforms))]
        public void Platform_RoundTrip(Platform platform)
        {
            const string path = "test";

            var original = CreateCompilation(
                "class C { static void Main() { } }",
                options: TestOptions.DebugExe.WithPlatform(platform));

            original.VerifyDiagnostics();

            var originalBytes = original.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            var peReader = new PEReader(originalBytes);
            Assert.True(peReader.TryOpenAssociatedPortablePdb(path, path => null, out var provider, out _));
            var pdbReader = provider!.GetMetadataReader();

            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger(path);
            // TODO: shouldn't need to pass a logger.
            var bc = new BuildConstructor(logger);

            var optionsReader = new CompilationOptionsReader(logger, pdbReader, peReader);

            var sources = original.SyntaxTrees.Select(st =>
            {
                var text = st.GetText();
                return new ResolvedSource(OnDiskPath: null, text, new SourceFileInfo(path, text.ChecksumAlgorithm, text.GetChecksum().ToArray(), text, embeddedCompressedHash: null));
            }).ToImmutableArray();
            var references = original.References.ToImmutableArray();
            var compilation = bc.CreateCompilation(optionsReader, path, sources, references);

            Assert.Equal(platform, compilation.Options.Platform);

            // TODO: we should be able to get byte-for-byte equality here.
            // it will probably be necessary to expose some diagnostic facility in the Rebuild API to figure out what's wrong here.

            // using var rebuildStream = new MemoryStream();
            // var result = BuildConstructor.Emit(rebuildStream, new FileInfo(path), optionsReader, compilation, logger, CancellationToken.None);
            // Assert.Empty(result.Diagnostics);
            // Assert.True(result.Success);
            // Assert.Equal(originalBytes.ToArray(), rebuildStream.ToArray());
        }

        [Theory]
        [MemberData(nameof(s_platforms))]
        public void Platform_RoundTrip_VB(Platform platform)
        {
            const string path = "test";

            var original = CreateVisualBasicCompilation(
                path,
                compilationOptions: new VisualBasicCompilationOptions(outputKind: OutputKind.ConsoleApplication, platform: platform),
                encoding: Encoding.UTF8,
                code: @"
Class C
    Shared Sub Main()
    End Sub
End Class");

            original.VerifyDiagnostics();

            var originalBytes = original.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            var peReader = new PEReader(originalBytes);
            Assert.True(peReader.TryOpenAssociatedPortablePdb(path, path => null, out var provider, out _));
            var pdbReader = provider!.GetMetadataReader();

            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger(path);
            // TODO: shouldn't need to pass a logger.
            var bc = new BuildConstructor(logger);

            var optionsReader = new CompilationOptionsReader(logger, pdbReader, peReader);

            var sources = original.SyntaxTrees.Select(st =>
            {
                var text = st.GetText();
                return new ResolvedSource(OnDiskPath: null, text, new SourceFileInfo(path, text.ChecksumAlgorithm, text.GetChecksum().ToArray(), text, embeddedCompressedHash: null));
            }).ToImmutableArray();
            var references = original.References.ToImmutableArray();
            var compilation = bc.CreateCompilation(optionsReader, path, sources, references);

            Assert.Equal(platform, compilation.Options.Platform);

            // TODO: we should be able to get byte-for-byte equality here.
            // it will probably be necessary to expose some diagnostic facility in the Rebuild API to figure out what's wrong here.

            // using var rebuildStream = new MemoryStream();
            // var result = BuildConstructor.Emit(rebuildStream, new FileInfo(path), optionsReader, compilation, logger, CancellationToken.None);
            // Assert.Empty(result.Diagnostics);
            // Assert.True(result.Success);
            // Assert.Equal(originalBytes.ToArray(), rebuildStream.ToArray());
        }
    }
}
