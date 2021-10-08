// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    /// <summary>
    /// Supports obtaining metadata readers from various types, and manages their disposal
    /// </summary>
    internal sealed class MultiMetadataReaderProvider : IDisposable
    {
        private readonly Stream _dllStream;
        private readonly Stream? _pdbStream;
        private readonly MetadataReaderProvider _pdbReaderProvider;
        private readonly PEReader _peReader;

        public MultiMetadataReaderProvider(Stream dllStream, PEReader peReader, Stream? pdbStream, MetadataReaderProvider pdbReaderProvider)
        {
            _dllStream = dllStream;
            _pdbStream = pdbStream;

            _peReader = peReader;
            _pdbReaderProvider = pdbReaderProvider;
        }

        public MetadataReader GetDllMetadataReader()
        {
            return _peReader.GetMetadataReader();
        }

        public MetadataReader GetPdbMetadataReader()
        {
            return _pdbReaderProvider.GetMetadataReader();
        }

        public void Dispose()
        {
            _dllStream.Dispose();
            _pdbStream?.Dispose();
            _pdbReaderProvider.Dispose();
            _peReader.Dispose();
        }
    }
}
