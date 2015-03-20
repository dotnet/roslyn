// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.DiaSymReader;

namespace Roslyn.Test.PdbUtilities
{
    public sealed class TempPdbReader : IDisposable
    {
        private ISymUnmanagedReader _rawReader;

        private TempPdbReader(ISymUnmanagedReader rawReader)
        {
            _rawReader = rawReader;
        }

        public static object CreateUnmanagedReader(Stream pdb)
        {
            return CreateReader(pdb);
        }

        public static TempPdbReader Create(Stream pdb)
        {
            return new TempPdbReader(CreateReader(pdb));
        }

        internal static ISymUnmanagedReader CreateReader(Stream pdb)
        {
            return SymUnmanagedReaderTestExtensions.CreateReader(pdb, DummyMetadataImport.Instance);
        }

        public void Dispose()
        {
            // If the underlying symbol reader supports an explicit dispose interface to release
            // it's resources, then call it.
            (_rawReader as ISymUnmanagedDispose)?.Destroy();
            _rawReader = null;
        }

        internal ISymUnmanagedReader SymbolReader
        {
            get
            {
                if (_rawReader == null)
                {
                    throw new ObjectDisposedException("SymReader");
                }

                return _rawReader;
            }
        }
    }
}
