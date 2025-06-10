// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Text
{
    internal class StringTextWriter : SourceTextWriter
    {
        private StringBuilder _builder;
        private readonly PooledStringBuilder _pooledBuilder;
        private readonly Encoding? _encoding;
        private readonly SourceHashAlgorithm _checksumAlgorithm;

        public StringTextWriter(Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, int capacity)
        {
            _pooledBuilder = PooledStringBuilder.GetInstance();
            _builder = _pooledBuilder.Builder;
            _builder.EnsureCapacity(capacity);
            _encoding = encoding;
            _checksumAlgorithm = checksumAlgorithm;
        }

        // https://github.com/dotnet/roslyn/issues/40830
        public override Encoding Encoding
        {
            get { return _encoding!; }
        }

        public override SourceText ToSourceText()
        {
            var sourceText = new StringText(_builder.ToString(), _encoding, checksumAlgorithm: _checksumAlgorithm);

            // Release the pooled string builder back to the pool. At this point, callers are no
            // longer allowed to write to this object.
            _builder = null!;
            _pooledBuilder.Free();

            return sourceText;
        }

        public override void Write(char value)
        {
            _builder.Append(value);
        }

        public override void Write(string? value)
        {
            _builder.Append(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _builder.Append(buffer, index, count);
        }
    }
}
