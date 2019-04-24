// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
        [InlineData(true)]
        [InlineData(false)]
        public void AssemblyAndPdb(bool exactPdbPath)
        {
            var dir = Temp.CreateDirectory();
            var dllFile = dir.CreateFile("lib.dll");
            var pdbFile = dir.CreateFile("lib.pdb");

            var source = @"class C { public static void Main() { int x = 1; } }";

            var compilation = CSharpTestBase.CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, assemblyName: "lib");
            var pdbStream = new MemoryStream();
            var debugDirPdbPath = exactPdbPath ? pdbFile.Path : "a/y/z/lib.pdb";
            var peImage = compilation.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: debugDirPdbPath), pdbStream: pdbStream);
            pdbStream.Position = 0;

            dllFile.WriteAllBytes(peImage);
            pdbFile.WriteAllBytes(pdbStream.ToArray());

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

    }
}
