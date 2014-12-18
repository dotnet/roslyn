// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Samples.Debugging.SymbolStore;
using Roslyn.Utilities;
using Microsoft.VisualStudio.SymReaderInterop;
using System.Runtime.InteropServices;

namespace Roslyn.Test.PdbUtilities
{
    public sealed class TempPdbReader : IDisposable
    {
        private ISymbolReader symReader;
        private ISymUnmanagedReader rawReader;

        private TempPdbReader(ISymbolReader reader, ISymUnmanagedReader rawReader)
        {
            this.symReader = reader;
            this.rawReader = rawReader;
        }

        public static object CreateUnmanagedReader(Stream pdb)
        {
            return CreateRawReader(pdb);
        }

        internal static ISymUnmanagedReader CreateRawReader(Stream pdb)
        {
            Guid CLSID_CorSymReaderSxS = new Guid("0A3976C5-4529-4ef8-B0B0-42EED37082CD");
            var rawReader = (ISymUnmanagedReader)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_CorSymReaderSxS));
            int hr = rawReader.Initialize(DummyMetadataImport.Instance, null, null, new ComStreamWrapper(pdb));
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
            return rawReader;
        }

        public static TempPdbReader Create(Stream pdb)
        {
            var rawReader = CreateRawReader(pdb);
            return new TempPdbReader(Microsoft.Samples.Debugging.CorSymbolStore.SymbolBinder.GetReaderFromCOM(rawReader), rawReader);
        }

        public void Dispose()
        {
            if (this.symReader != null)
            {
                ((IDisposable)this.symReader).Dispose();
                this.symReader = null;
                this.rawReader = null;
            }
        }

        public ISymbolReader SymbolReader
        {
            get
            {
                if (symReader == null)
                {
                    throw new ObjectDisposedException("SymReader");
                }

                return symReader;
            }
        }

        internal ISymUnmanagedReader RawSymbolReader
        {
            get 
            {
                if (rawReader == null)
                {
                    throw new ObjectDisposedException("SymReader");
                }

                return rawReader; 
            }
        }
    }
}
