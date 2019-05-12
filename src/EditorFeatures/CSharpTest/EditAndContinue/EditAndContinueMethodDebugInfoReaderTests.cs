// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.DiaSymReader;
using Microsoft.DiaSymReader.PortablePdb;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue
{
    public class EditAndContinueMethodDebugInfoReaderTests
    {
        private class DummyMetadataImportProvider : IMetadataImportProvider
        {
            public object GetMetadataImport() => throw new NotImplementedException();
        }

        private class DummySymReaderMetadataProvider : ISymReaderMetadataProvider
        {
            public unsafe bool TryGetStandaloneSignature(int standaloneSignatureToken, out byte* signature, out int length)
                => throw new NotImplementedException();

            public bool TryGetTypeDefinitionInfo(int typeDefinitionToken, out string namespaceName, out string typeName, out TypeAttributes attributes)
                => throw new NotImplementedException();

            public bool TryGetTypeReferenceInfo(int typeReferenceToken, out string namespaceName, out string typeName)
                => throw new NotImplementedException();
        }

        [Theory]
        [InlineData(DebugInformationFormat.PortablePdb)]
        [InlineData(DebugInformationFormat.Pdb)]
        public void DebugInfo(DebugInformationFormat format)
        {
            var symBinder = new SymBinder();
            var metadataImportProvider = new DummyMetadataImportProvider();

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

            var pdbStreamCom = SymUnmanagedStreamFactory.CreateStream(pdbStream);

            ISymUnmanagedReader5 symReader5;
            if (format == DebugInformationFormat.PortablePdb)
            {
                int hr = symBinder.GetReaderFromPdbStream(metadataImportProvider, pdbStreamCom, out var symReader);
                Assert.Equal(0, hr);
                symReader5 = (ISymUnmanagedReader5)symReader;
            }
            else
            {
                symReader5 = SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(pdbStream, new DummySymReaderMetadataProvider());
            }

            var reader = EditAndContinueMethodDebugInfoReader.Create(symReader5, version: 1);

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
