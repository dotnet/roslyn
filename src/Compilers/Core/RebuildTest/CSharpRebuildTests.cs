// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis.Test.Utilities;
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
    }
}
