// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.UnitTests
{
    public class VisualStudioCompilationOutputFilesTests : TestBase
    {
        [Fact]
        public void OpenStream_Errors()
        {
            Assert.Throws<ArgumentException>(() => new CompilationOutputFilesWithImplicitPdbPath(@"a.dll"));
        }

        [Theory]
        [InlineData(DebugInformationFormat.PortablePdb, true)]
        [InlineData(DebugInformationFormat.PortablePdb, false)]
        [InlineData(DebugInformationFormat.Embedded, false)]
        public void AssemblyAndPdb(DebugInformationFormat pdbFormat, bool exactPdbPath)
        {
            var dir = Temp.CreateDirectory();
            var dllFile = dir.CreateFile("lib.dll");
            var pdbFile = (pdbFormat == DebugInformationFormat.Embedded) ? null : dir.CreateFile("lib.pdb");

            var source = @"class C { public static void Main() { int x = 1; } }";

            var compilation = CSharpTestBase.CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, assemblyName: "lib");
            var pdbStream = (pdbFile != null) ? new MemoryStream() : null;
            var debugDirPdbPath = exactPdbPath ? pdbFile.Path : "a/y/z/lib.pdb";
            var peImage = compilation.EmitToArray(new EmitOptions(debugInformationFormat: pdbFormat, pdbFilePath: debugDirPdbPath), pdbStream: pdbStream);
            dllFile.WriteAllBytes(peImage);

            if (pdbFile != null)
            {
                pdbStream.Position = 0;
                pdbFile.WriteAllBytes(pdbStream.ToArray());
            }

            var outputs = new CompilationOutputFilesWithImplicitPdbPath(dllFile.Path);

            using (var pdb = outputs.OpenPdb())
            {
                var encReader = pdb.CreateEditAndContinueMethodDebugInfoReader();
                Assert.True(encReader.IsPortable);
                var localSig = encReader.GetLocalSignature(MetadataTokens.MethodDefinitionHandle(1));
                Assert.Equal(MetadataTokens.StandaloneSignatureHandle(1), localSig);
            }

            using (var metadata = outputs.OpenAssemblyMetadata(prefetch: false))
            {
                var mdReader = metadata.GetMetadataReader();
                Assert.Equal("lib", mdReader.GetString(mdReader.GetAssemblyDefinition().Name));
            }

            // make sure all files are closed and can be deleted
            Directory.Delete(dir.Path, recursive: true);
        }

        [Fact]
        public void AssemblyFileNotFound()
        {
            var dir = Temp.CreateDirectory();
            var outputs = new CompilationOutputFilesWithImplicitPdbPath(Path.Combine(dir.Path, "nonexistent.dll"));
            Assert.Null(outputs.OpenPdb());
            Assert.Null(outputs.OpenAssemblyMetadata(prefetch: false));
        }

        [Fact]
        public void PdbFileNotFound()
        {
            var dir = Temp.CreateDirectory();
            var dllFile = dir.CreateFile("lib.dll");

            var source = @"class C { public static void Main() { int x = 1; } }";

            var compilation = CSharpTestBase.CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, assemblyName: "lib");
            var pdbStream = new MemoryStream();
            var debugDirPdbPath = Path.Combine(dir.Path, "nonexistent.pdb");
            var peImage = compilation.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: debugDirPdbPath), pdbStream: pdbStream);
            pdbStream.Position = 0;

            dllFile.WriteAllBytes(peImage);

            var outputs = new CompilationOutputFilesWithImplicitPdbPath(dllFile.Path);

            Assert.Null(outputs.OpenPdb());

            using (var metadata = outputs.OpenAssemblyMetadata(prefetch: false))
            {
                var mdReader = metadata.GetMetadataReader();
                Assert.Equal("lib", mdReader.GetString(mdReader.GetAssemblyDefinition().Name));
            }

            // make sure all files are closed and can be deleted
            Directory.Delete(dir.Path, recursive: true);
        }
    }
}
