// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias DSR;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
            // Delegate to the DiaSymReader factory so the native reader is created through the
            // source-generated COM marshalling infrastructure. Creating the reader via a raw P/Invoke
            // that marshals it as a built-in COM object (UnmanagedType.IUnknown) is incompatible with the
            // [GeneratedComInterface]-based ISymUnmanagedReader* interfaces and fails when the reader is used.
            return SymUnmanagedReaderFactory.CreateReaderWithMetadataImport<ISymUnmanagedReader5>(pdbStream, metadataImporter);
        }

        private static ISymUnmanagedReader5 CreatePortableSymReader(Stream pdbStream, object metadataImporter)
        {
            return (ISymUnmanagedReader5)new PortablePdb.SymBinder().GetReaderFromStream(pdbStream, metadataImporter);
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
