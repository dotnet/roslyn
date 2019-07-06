// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Emit.UnitTests
{
    public class CompilationOutputsTests
    {
        private class TestCompilationOutputs : CompilationOutputs
        {
            private readonly Func<Stream> _openAssemblyStream;
            private readonly Func<Stream> _openPdbStream;

            public TestCompilationOutputs(Func<Stream> openAssemblyStream = null, Func<Stream> openPdbStream = null)
            {
                _openAssemblyStream = openAssemblyStream;
                _openPdbStream = openPdbStream;
            }

            public override string AssemblyDisplayPath => "assembly";
            public override string PdbDisplayPath => "pdb";
            protected override Stream OpenAssemblyStream() => (_openAssemblyStream ?? throw new NotImplementedException()).Invoke();
            protected override Stream OpenPdbStream() => (_openPdbStream ?? throw new NotImplementedException()).Invoke();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void OpenStream_Errors(bool canRead, bool canSeek)
        {
            var outputs = new TestCompilationOutputs(
                openAssemblyStream: () => new TestStream(canRead, canSeek, canWrite: true),
                openPdbStream: () => new TestStream(canRead, canSeek, canWrite: true));

            Assert.Throws<InvalidOperationException>(() => outputs.OpenAssemblyMetadata(prefetch: false));
            Assert.Throws<InvalidOperationException>(() => outputs.OpenPdb());
        }

        [Theory]
        [InlineData(DebugInformationFormat.PortablePdb)]
        [InlineData(DebugInformationFormat.Embedded)]
        [InlineData(DebugInformationFormat.Pdb)]
        public void AssemblyAndPdb(DebugInformationFormat format)
        {
            var source = @"class C { public static void Main() { int x = 1; } }";
            var compilation = CSharpTestBase.CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, assemblyName: "lib");

            var pdbStream = (format != DebugInformationFormat.Embedded) ? new MemoryStream() : null;
            var peImage = compilation.EmitToArray(new EmitOptions(debugInformationFormat: format), pdbStream: pdbStream);

            Stream currentPEStream = null;
            Stream currentPdbStream = null;

            var outputs = new TestCompilationOutputs(
                openAssemblyStream: () => currentPEStream = new MemoryStream(peImage.ToArray()),
                openPdbStream: () =>
                {
                    if (pdbStream == null)
                    {
                        return null;
                    }

                    currentPdbStream = new MemoryStream();
                    pdbStream.Position = 0;
                    pdbStream.CopyTo(currentPdbStream);
                    currentPdbStream.Position = 0;
                    return currentPdbStream;
                });

            using (var pdb = outputs.OpenPdb())
            {
                var encReader = pdb.CreateEditAndContinueMethodDebugInfoReader();
                Assert.Equal(format != DebugInformationFormat.Pdb, encReader.IsPortable);
                var localSig = encReader.GetLocalSignature(MetadataTokens.MethodDefinitionHandle(1));
                Assert.Equal(MetadataTokens.StandaloneSignatureHandle(1), localSig);
            }

            if (format == DebugInformationFormat.Embedded)
            {
                Assert.Throws<ObjectDisposedException>(() => currentPEStream.Length);
            }
            else
            {
                Assert.Throws<ObjectDisposedException>(() => currentPdbStream.Length);
            }

            using (var metadata = outputs.OpenAssemblyMetadata(prefetch: false))
            {
                Assert.NotEqual(0, currentPEStream.Length);

                var mdReader = metadata.GetMetadataReader();
                Assert.Equal("lib", mdReader.GetString(mdReader.GetAssemblyDefinition().Name));
            }

            Assert.Throws<ObjectDisposedException>(() => currentPEStream.Length);

            using (var metadata = outputs.OpenAssemblyMetadata(prefetch: true))
            {
                // the stream has been closed since we prefetched the metadata:
                Assert.Throws<ObjectDisposedException>(() => currentPEStream.Length);

                var mdReader = metadata.GetMetadataReader();
                Assert.Equal("lib", mdReader.GetString(mdReader.GetAssemblyDefinition().Name));
            }

            Assert.NotEqual(Guid.Empty, outputs.ReadAssemblyModuleVersionId());
        }

        [Fact]
        public void ReadAssemblyModuleVersionId_NoAssembly()
        {
            var outputs = new TestCompilationOutputs(
                openAssemblyStream: () => null,
                openPdbStream: () => null);

            Assert.Equal(Guid.Empty, outputs.ReadAssemblyModuleVersionId());
        }
    }
}
