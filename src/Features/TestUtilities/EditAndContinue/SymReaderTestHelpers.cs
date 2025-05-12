// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Microsoft.DiaSymReader.PortablePdb;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal static class SymReaderTestHelpers
{
    private sealed class DummyMetadataImportProvider : IMetadataImportProvider
    {
        public object GetMetadataImport() => throw new NotImplementedException();
    }

    private sealed class DummySymReaderMetadataProvider : ISymReaderMetadataProvider
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
        if (pdbImage is [(byte)'B', (byte)'S', (byte)'J', (byte)'B', ..])
        {
            var hr = symBinder.GetReaderFromPdbStream(metadataImportProvider, pdbStreamCom, out var symReader);
            Assert.Equal(0, hr);
            return (ISymUnmanagedReader5)symReader;
        }
        else
        {
            return SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(pdbStream, new DummySymReaderMetadataProvider());
        }
    }
}
