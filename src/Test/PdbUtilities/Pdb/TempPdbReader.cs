﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.DiaSymReader;

namespace Roslyn.Test.PdbUtilities
{
    public sealed class TempPdbReader : IDisposable
    {
        private ISymUnmanagedReader rawReader;

        private TempPdbReader(ISymUnmanagedReader rawReader)
        {
            this.rawReader = rawReader;
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
            return SymUnmanagedReaderExtensions.CreateReader(pdb, DummyMetadataImport.Instance);
        }

        public void Dispose()
        {
            // If the underlying symbol reader supports an explicit dispose interface to release
            // it's resources, then call it.
            (rawReader as ISymUnmanagedDispose)?.Destroy();
            this.rawReader = null;
        }

        internal ISymUnmanagedReader SymbolReader
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
