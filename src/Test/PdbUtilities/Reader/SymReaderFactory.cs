// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias DSR;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymReader")]
        private static extern void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymReader")]
        private static extern void CreateSymReaderAmd64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport("Microsoft.DiaSymReader.Native.arm64.dll", EntryPoint = "CreateSymReader")]
        private static extern void CreateSymReaderArm64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object symReader);

        private static ISymUnmanagedReader5 CreateNativeSymReader(Stream pdbStream, object metadataImporter)
        {
            object symReader = null;

            var guid = default(Guid);
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    CreateSymReader32(ref guid, out symReader);
                    break;
                case Architecture.X64:
                    CreateSymReaderAmd64(ref guid, out symReader);
                    break;
                case Architecture.Arm64:
                    CreateSymReaderArm64(ref guid, out symReader);
                    break;
                default:
                    throw new NotSupportedException();
            }

            var reader = (ISymUnmanagedReader5)symReader;
            reader.Initialize(pdbStream, metadataImporter);
            return reader;
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
            return CreateReader(new MemoryStream(pdbImage.ToArray()), (peImageOpt.IsDefault) ? null : new PEReader(peImageOpt));
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
