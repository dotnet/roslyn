// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal sealed class PortablePdbReader : IDisposable
    {
        private readonly MetadataReader _metadataReader;
        private readonly GCHandle _pinnedImage;
        private LazyMetadataImport _lazyMetadataImport;

        internal IntPtr ImagePtr => _pinnedImage.AddrOfPinnedObject();
        internal int ImageSize { get; }

        internal PortablePdbReader(byte[] buffer, int size, LazyMetadataImport metadataImport)
        {
            Debug.Assert(metadataImport != null);

            _metadataReader = CreateMetadataReader(buffer, size, out _pinnedImage);
            _lazyMetadataImport = metadataImport;
            ImageSize = size;
        }

        internal unsafe static MetadataReader CreateMetadataReader(byte[] buffer, int size, out GCHandle pinnedImage)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(size >= 0 && size <= buffer.Length);

            pinnedImage = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            return new MetadataReader((byte*)pinnedImage.AddrOfPinnedObject(), size);
        }

        internal bool MatchesModule(Guid guid, uint stamp, int age)
        {
            return age == 1 && IdEquals(MetadataReader.DebugMetadataHeader.Id, guid, stamp);
        }

        internal static bool IdEquals(ImmutableArray<byte> left, Guid rightGuid, uint rightStamp)
        {
            if (left.Length != 20)
            {
                // invalid id
                return false;
            }

            byte[] guidBytes = rightGuid.ToByteArray();
            for (int i = 0; i < guidBytes.Length; i++)
            {
                if (guidBytes[i] != left[i])
                {
                    return false;
                }
            }

            byte[] stampBytes = BitConverter.GetBytes(rightStamp);
            for (int i = 0; i < stampBytes.Length; i++)
            {
                if (stampBytes[i] != left[guidBytes.Length + i])
                {
                    return false;
                }
            }

            return true;
        }

        internal IMetadataImport GetMetadataImport()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(SymReader));
            }

            return _lazyMetadataImport.GetMetadataImport();
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

        internal bool IsDisposed
        {
            get
            {
                return _lazyMetadataImport == null;
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                _pinnedImage.Free();
                _lazyMetadataImport.Dispose();
                _lazyMetadataImport = null;
            }
        }
    }
}
