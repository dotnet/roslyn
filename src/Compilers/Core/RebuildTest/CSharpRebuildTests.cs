// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
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

namespace Rebuild.UnitTests
{
    //[CompilerTrait(CompilerFeature.)]
    public class CSharpRebuildTests : CSharpTestBase
    {
        [Theory]
        [InlineData(Platform.Arm64)]
        [InlineData(Platform.Arm)]
        [InlineData(Platform.X64)]
        [InlineData(Platform.Itanium)]
        [InlineData(Platform.X86)]
        [InlineData(Platform.AnyCpu)]
        [InlineData(Platform.AnyCpu32BitPreferred)]
        public void Platform_RoundTrip(Platform platform)
        {
            var original = CreateCompilation(
                ";",
                options: TestOptions.DebugExe.WithPlatform(platform));

            original.VerifyDiagnostics();

            var image = original.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            var peReader = new PEReader(image);
            Assert.True(peReader.TryOpenAssociatedPortablePdb("test", path => null, out var provider, out _));
            var pdbReader = provider!.GetMetadataReader();

            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger("test");
            // TODO: shouldn't need to pass a logger.
            var bc = new BuildConstructor(logger);

            var optionsReader = new CompilationOptionsReader(logger, pdbReader, peReader);

            // TODO: SourceFileInfo?
            var sources = original.SyntaxTrees.Select(st => new ResolvedSource(OnDiskPath: null, st.GetText(), new SourceFileInfo())).ToImmutableArray();
            var references = original.References.ToImmutableArray();
            var (compilation, isError) = bc.CreateCompilation(optionsReader, "test", sources, references);

            Assert.False(isError);
            Assert.Equal(platform, compilation!.Options.Platform);
        }
    }
}
