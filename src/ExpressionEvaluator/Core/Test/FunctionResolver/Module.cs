// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class Module : IDisposable
    {
        private readonly PEReader? _reader;

        public readonly string? Name;
        public int MetadataAccessCount { get; private set; }

        internal Module(ImmutableArray<byte> metadata, string? name = null)
        {
            Name = name;
            _reader = metadata.IsDefault ? null : new PEReader(metadata);
        }

        internal unsafe bool TryGetMetadata(out byte* pointer, out int length)
        {
            MetadataAccessCount++;

            if (_reader == null)
            {
                pointer = null;
                length = 0;
                return false;
            }

            var block = _reader.GetMetadata();
            pointer = block.Pointer;
            length = block.Length;
            return true;
        }

        internal unsafe MetadataReader? GetMetadataReader()
        {
            if (_reader == null)
            {
                return null;
            }

            var block = _reader.GetMetadata();
            return new MetadataReader(block.Pointer, block.Length);
        }

        void IDisposable.Dispose()
            => _reader?.Dispose();
    }
}
