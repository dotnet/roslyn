// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.DiaSymReader;
using Microsoft.DiaSymReader.PortablePdb;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public class EditAndContinueMethodDebugInfoReaderTests
    {
        private class DummyMetadataImportProvider : IMetadataImportProvider
        {
            public object GetMetadataImport() => throw new NotImplementedException();
        }

        [Fact]
        public void Create_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => EditAndContinueMethodDebugInfoReader.Create((ISymUnmanagedReader5)null));
            Assert.Throws<ArgumentNullException>(() => EditAndContinueMethodDebugInfoReader.Create((MetadataReader)null));
            Assert.Throws<ArgumentNullException>(() => EditAndContinueMethodDebugInfoReader.Create(null, 1));

            var mockSymReader = new Mock<ISymUnmanagedReader5>().Object;
            Assert.Throws<ArgumentOutOfRangeException>(() => EditAndContinueMethodDebugInfoReader.Create(mockSymReader, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => EditAndContinueMethodDebugInfoReader.Create(mockSymReader, -1));
        }

        [Theory]
        [InlineData(DebugInformationFormat.PortablePdb, true)]
        [InlineData(DebugInformationFormat.PortablePdb, false)]
        [InlineData(DebugInformationFormat.Pdb, true)]
        public void DebugInfo(DebugInformationFormat format, bool useSymReader)
        {
            var source = @"
using System;
delegate void D();
class C
{
    public static void Main()
    {
        int x = 1;
        D d = () => Console.Write(x);
        d();
    }
}
";
            var compilation = CSharpTestBase.CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            compilation.EmitToArray(new EmitOptions(debugInformationFormat: format), pdbStream: pdbStream);
            pdbStream.Position = 0;

            DebugInformationReaderProvider provider;
            EditAndContinueMethodDebugInfoReader reader;

            if (format == DebugInformationFormat.PortablePdb && useSymReader)
            {
                var pdbStreamCom = SymUnmanagedStreamFactory.CreateStream(pdbStream);
                var metadataImportProvider = new DummyMetadataImportProvider();
                Assert.Equal(0, new SymBinder().GetReaderFromPdbStream(metadataImportProvider, pdbStreamCom, out var symReader));
                reader = EditAndContinueMethodDebugInfoReader.Create((ISymUnmanagedReader5)symReader, version: 1);
            }
            else
            {
                provider = DebugInformationReaderProvider.CreateFromStream(pdbStream);
                reader = provider.CreateEditAndContinueMethodDebugInfoReader();
            }

            // Main method
            var debugInfo = reader.GetDebugInfo(MetadataTokens.MethodDefinitionHandle(5));
            Assert.Equal(0, debugInfo.GetMethodOrdinal());
            AssertEx.Equal(new[] { "Offset=0 Ordinal=0 Kind=LambdaDisplayClass", "Offset=33 Ordinal=0 Kind=UserDefined" }, debugInfo.InspectLocalSlots());
            AssertEx.Equal(new[] { "Offset=43 Id=0#0 Closure=0" }, debugInfo.InspectLambdas());
            AssertEx.Equal(new[] { "Offset=0 Id=0#0" }, debugInfo.InspectClosures());

            var localSig = reader.GetLocalSignature(MetadataTokens.MethodDefinitionHandle(5));
            Assert.Equal(MetadataTokens.StandaloneSignatureHandle(1), localSig);

            // method without debug information:
            debugInfo = reader.GetDebugInfo(MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal(-1, debugInfo.GetMethodOrdinal());
            Assert.Null(debugInfo.InspectLocalSlots());
            Assert.Null(debugInfo.InspectLambdas());
            Assert.Null(debugInfo.InspectClosures());

            localSig = reader.GetLocalSignature(MetadataTokens.MethodDefinitionHandle(1));
            Assert.Equal(default, localSig);
        }
    }
}
