// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Emit.UnitTests
{
    public class CompilationOutputFilesTests : TestBase
    {
        [Fact]
        public void OpenStream_Errors()
        {
            Assert.Throws<ArgumentException>(() => new CompilationOutputFiles(@"a.dll"));
            Assert.Throws<ArgumentException>(() => new CompilationOutputFiles(@"\a.dll", @"a.dll"));
        }

        [Fact]
        public void AssemblyAndPdb()
        {
            var source = @"class C { public static void Main() { int x = 1; } }";

            var compilation = CSharpTestBase.CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, assemblyName: "lib");
            var pdbStream = new MemoryStream();
            var peImage = compilation.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;

            var dir = Temp.CreateDirectory();
            var dllFile = dir.CreateFile("a.dll").WriteAllBytes(peImage);
            var pdbFile = dir.CreateFile("a.pdb").WriteAllBytes(pdbStream.ToArray());

            var outputs = new CompilationOutputFiles(dllFile.Path, pdbFile.Path);

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
