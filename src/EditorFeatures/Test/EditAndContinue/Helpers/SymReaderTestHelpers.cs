// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.DiaSymReader.PortablePdb;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal static class SymReaderTestHelpers
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

        public static (ImmutableArray<byte> PEImage, ISymUnmanagedReader5 SymReader) EmitAndOpenDummySymReader(Compilation compilation, DebugInformationFormat pdbFormat)
        {
            var symBinder = new SymBinder();
            var metadataImportProvider = new DummyMetadataImportProvider();

            var pdbStream = new MemoryStream();
            var peImage = compilation.EmitToArray(new EmitOptions(debugInformationFormat: pdbFormat), pdbStream: pdbStream);
            pdbStream.Position = 0;

            var pdbStreamCom = SymUnmanagedStreamFactory.CreateStream(pdbStream);

            ISymUnmanagedReader5 symReader5;
            if (pdbFormat == DebugInformationFormat.PortablePdb)
            {
                int hr = symBinder.GetReaderFromPdbStream(metadataImportProvider, pdbStreamCom, out var symReader);
                Assert.Equal(0, hr);
                symReader5 = (ISymUnmanagedReader5)symReader;
            }
            else
            {
                symReader5 = SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(pdbStream, new DummySymReaderMetadataProvider());
            }

            return (peImage, symReader5);
        }


    }
}
