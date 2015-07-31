// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.PortablePdb
{
    // TODO: handle disposed objects
    // TODO: 

    internal sealed class PortablePdbReader : IDisposable
    {
        private readonly MetadataReader _metadataReader;
        private readonly GCHandle _pinnedImage;

        internal IMetadataImport MetadataImport { get; private set; }
        internal IntPtr ImagePtr => _pinnedImage.AddrOfPinnedObject();
        internal int ImageSize { get; }

        internal bool IsDisposed => MetadataImport == null;

        internal unsafe PortablePdbReader(byte[] buffer, int size, IMetadataImport metadataImporter)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(size >= 0 && size <= buffer.Length);
            Debug.Assert(metadataImporter != null);

            _pinnedImage = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _metadataReader = new MetadataReader((byte*)_pinnedImage.AddrOfPinnedObject(), size);

            ImageSize = size;
            MetadataImport = metadataImporter;
        }

        internal MetadataReader MetadataReader
        {
            get
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(nameof(SymReader));
                }

                return _metadataReader;
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                _pinnedImage.Free();
                MetadataImport = null;
            }
        }
    }
}
