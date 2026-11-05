// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias DSR;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using DSR::Microsoft.DiaSymReader;
using PortablePdb = Microsoft.DiaSymReader.PortablePdb;

namespace Roslyn.Test.PdbUtilities
{
    public static class SymReaderFactory
    {
        public static void Dispose(this ISymUnmanagedReader symReader)
            => ((ISymUnmanagedDispose)symReader)?.Destroy();

        private static ISymUnmanagedReader5 CreateNativeSymReader(Stream pdbStream, object metadataImporter)
        {
            // Let DiaSymReader create the metadata adapter so it uses the same COM marshalling as the reader.
            return SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(
                pdbStream, new NativeSymReaderMetadataProvider((PortablePdb.IMetadataImport)metadataImporter));
        }

        private static ISymUnmanagedReader5 CreatePortableSymReader(Stream pdbStream, object metadataImporter)
        {
            // Call the managed binder directly rather than converting it to a COM interface.
            var hr = new PortablePdb.SymBinder().GetReaderFromStream(
                metadataImporter, SymUnmanagedStreamFactory.CreateStream(pdbStream), out var reader);
            Marshal.ThrowExceptionForHR(hr);
            return (ISymUnmanagedReader5)reader;
        }

        private sealed class NativeSymReaderMetadataProvider(PortablePdb.IMetadataImport metadataImporter) : ISymReaderMetadataProvider
        {
            public unsafe bool TryGetStandaloneSignature(int standaloneSignatureToken, out byte* signature, out int length)
                => metadataImporter.GetSigFromToken(standaloneSignatureToken, out signature, out length) == 0;

            // Type information is only requested by portable readers.
            public bool TryGetTypeDefinitionInfo(int typeDefinitionToken, out string namespaceName, out string typeName, out TypeAttributes attributes)
                => throw new NotSupportedException();

            public bool TryGetTypeReferenceInfo(int typeReferenceToken, out string namespaceName, out string typeName)
                => throw new NotSupportedException();
        }

        public static ISymUnmanagedReader5 CreateReader(byte[] pdbImage, byte[] peImageOpt = null)
        {
            return CreateReader(new MemoryStream(pdbImage), (peImageOpt != null) ? new PEReader(new MemoryStream(peImageOpt)) : null);
        }

        public static ISymUnmanagedReader5 CreateReader(ImmutableArray<byte> pdbImage, ImmutableArray<byte> peImageOpt = default(ImmutableArray<byte>))
        {
            return CreateReader(new MemoryStream([.. pdbImage]), (peImageOpt.IsDefault) ? null : new PEReader(peImageOpt));
        }

        public static ISymUnmanagedReader5 CreateReader(Stream pdbStream, Stream peStreamOpt = null)
        {
            return CreateReader(pdbStream, (peStreamOpt != null) ? new PEReader(peStreamOpt) : null);
        }

        public static ISymUnmanagedReader5 CreateReader(Stream pdbStream, PEReader peReaderOpt)
        {
            return CreateReader(pdbStream, peReaderOpt?.GetMetadataReader(), peReaderOpt);
        }

        public static ISymUnmanagedReader5 CreateReader(Stream pdbStream, MetadataReader metadataReaderOpt, IDisposable metadataMemoryOwnerOpt)
        {
            return CreateReaderImpl(pdbStream, metadataImporter: new DummyMetadataImport(metadataReaderOpt, metadataMemoryOwnerOpt));
        }

        public static ISymUnmanagedReader5 CreateReaderImpl(Stream pdbStream, object metadataImporter)
        {
            pdbStream.Position = 0;
            bool isPortable = pdbStream.ReadByte() == 'B' && pdbStream.ReadByte() == 'S' && pdbStream.ReadByte() == 'J' && pdbStream.ReadByte() == 'B';
            pdbStream.Position = 0;

            if (isPortable)
            {
                return CreatePortableSymReader(pdbStream, metadataImporter);
            }
            else
            {
                return CreateNativeSymReader(pdbStream, metadataImporter);
            }
        }
    }
}
