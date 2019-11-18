// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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

        public static (ImmutableArray<byte> PEImage, ImmutableArray<byte> PdbImage) EmitToArrays(this Compilation compilation, EmitOptions options)
        {
            var pdbStream = new MemoryStream();
            var peImage = compilation.EmitToArray(options, pdbStream: pdbStream);
            return (peImage, pdbStream.ToImmutable());
        }

        public static ISymUnmanagedReader5 OpenDummySymReader(ImmutableArray<byte> pdbImage)
        {
            var symBinder = new SymBinder();
            var metadataImportProvider = new DummyMetadataImportProvider();

            var pdbStream = new MemoryStream();
            pdbImage.WriteToStream(pdbStream);

            var pdbStreamCom = SymUnmanagedStreamFactory.CreateStream(pdbStream);
            if (pdbImage.Length > 4 && pdbImage[0] == 'B' && pdbImage[1] == 'S' && pdbImage[2] == 'J' && pdbImage[3] == 'B')
            {
                int hr = symBinder.GetReaderFromPdbStream(metadataImportProvider, pdbStreamCom, out var symReader);
                Assert.Equal(0, hr);
                return (ISymUnmanagedReader5)symReader;
            }
            else
            {
                return SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(pdbStream, new DummySymReaderMetadataProvider());
            }
        }
    }
}
