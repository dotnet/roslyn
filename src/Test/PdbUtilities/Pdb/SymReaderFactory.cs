// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;
using PortablePdb = Microsoft.DiaSymReader.PortablePdb;

namespace Roslyn.Test.PdbUtilities
{
    public static class SymReaderFactory
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        private static ISymUnmanagedReader3 CreateNativeSymReader(Stream pdbStream, object metadataImporter)
        {
            object symReader = null;

            var guid = default(Guid);
            if (IntPtr.Size == 4)
            {
                CreateSymReader32(ref guid, out symReader);
            }
            else
            {
                CreateSymReader64(ref guid, out symReader);
            }

            var reader = (ISymUnmanagedReader3)symReader;
            int hr = reader.Initialize(metadataImporter, null, null, new ComStreamWrapper(pdbStream));
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
            return reader;
        }

        public static ISymUnmanagedReader CreateReader(Stream pdbStream, MetadataReader metadataReaderOpt)
        {
            return CreateReader(pdbStream, new DummyMetadataImport(metadataReaderOpt));
        }

        public static ISymUnmanagedReader CreateReader(Stream pdbStream, object metadataImporter)
        {
            pdbStream.Position = 0;
            bool isPortable = pdbStream.ReadByte() == 'B' && pdbStream.ReadByte() == 'S' && pdbStream.ReadByte() == 'J' && pdbStream.ReadByte() == 'B';
            pdbStream.Position = 0;

            if (isPortable)
            {
                var binder = new PortablePdb.SymBinder();

                ISymUnmanagedReader reader;
                int hr = binder.GetReaderFromStream(metadataImporter, new ComStreamWrapper(pdbStream), out reader);
                SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);

                return reader;
            }
            else
            {
                return CreateNativeSymReader(pdbStream, metadataImporter);
            }
        }
    }
}
