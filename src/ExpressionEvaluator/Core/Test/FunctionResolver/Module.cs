// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class Module : IDisposable
    {
        private readonly string _name;
        private readonly PEReader _reader;
        private int _getMetadataCount;

        internal Module(ImmutableArray<byte> bytes, string name = null)
        {
            _name = name;
            _reader = bytes.IsDefault ? null : new PEReader(bytes);
        }

        internal string Name => _name;

        internal int GetMetadataCount => _getMetadataCount;

        internal MetadataReader GetMetadata()
        {
            _getMetadataCount++;
            return GetMetadataInternal();
        }

        internal MetadataReader GetMetadataInternal()
        {
            if (_reader == null)
            {
                return null;
            }
            unsafe
            {
                var block = _reader.GetMetadata();
                return new MetadataReader(block.Pointer, block.Length);
            }
        }

        void IDisposable.Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }
        }
    }
}
