// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using BuildValidator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
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
            const string path = "test";

            var original = CreateCompilation(
                @"System.Console.WriteLine(""I'm using top-level statements!"");",
                options: TestOptions.DebugExe);

            original.VerifyDiagnostics();

            var originalBytes = original.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            var peReader = new PEReader(originalBytes);
            Assert.True(peReader.TryOpenAssociatedPortablePdb(path, path => null, out var provider, out _));
            var pdbReader = provider!.GetMetadataReader();

            var factory = LoggerFactory.Create(configure => { });
            var logger = factory.CreateLogger(path);
            var bc = new BuildConstructor(logger);

            var optionsReader = new CompilationOptionsReader(logger, pdbReader, peReader);

            var sources = original.SyntaxTrees.Select(st =>
            {
                var text = st.GetText();
                return new SyntaxTreeInfo(path, text);
            }).ToImmutableArray();
            var references = original.References.ToImmutableArray();
            var compilation = bc.CreateCompilation("test.exe", optionsReader, sources, references);
            compilation.VerifyEmitDiagnostics();
        }
    }
}
