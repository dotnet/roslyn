// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    public sealed class PortablePdbReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly GCHandle _pinnedMetadata;
        private readonly MetadataReader _metadataReader;
        private IMetadataImport _metadataImporter;

        // TODO: add GetContent to MetadataReader?
        private readonly byte[] _image;

        public unsafe PortablePdbReader(Stream stream, object metadataImporter)
        {
            _stream = stream;
            _metadataImporter = (IMetadataImport)metadataImporter;

            // TODO:
            stream.Seek(0, SeekOrigin.Begin);
            _image = new byte[stream.Length];
            stream.Read(_image, 0, _image.Length);

            _pinnedMetadata = GCHandle.Alloc(_image, GCHandleType.Pinned);
            _metadataReader = new MetadataReader((byte*)_pinnedMetadata.AddrOfPinnedObject(), _image.Length);
        }

        internal MetadataReader MetadataReader => _metadataReader;
        internal IMetadataImport MetadataImport => _metadataImporter;
        internal byte[] Image => _image;

        public void Dispose()
        {
            _stream.Dispose();
            _pinnedMetadata.Free();
            _metadataImporter = null;
        }
    }
}
