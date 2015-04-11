// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DiaSymReader;
using Roslyn.Test.PdbUtilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue
{
    internal sealed class SymbolReaderProvider : IDisposable
    {
        private ISymUnmanagedReader _rawReader;

        private SymbolReaderProvider(ISymUnmanagedReader rawReader)
        {
            _rawReader = rawReader;
        }

        public static unsafe SymbolReaderProvider Create(byte[] pdbImage)
        {
            Guid corSymReaderSxS = new Guid("0A3976C5-4529-4ef8-B0B0-42EED37082CD");
            var rawReader = (ISymUnmanagedReader)Activator.CreateInstance(Type.GetTypeFromCLSID(corSymReaderSxS));
            int hr = rawReader.Initialize(new DummyMetadataImport(metadataReaderOpt: null), null, null, new ComStreamWrapper(new MemoryStream(pdbImage)));
            Marshal.ThrowExceptionForHR(hr);
            return new SymbolReaderProvider(rawReader);
        }

        public void Dispose()
        {
            if (_rawReader != null)
            {
                Marshal.ReleaseComObject(_rawReader);
                _rawReader = null;
            }
        }

        public ISymUnmanagedReader SymbolReader
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
