// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Text
{
    internal class StringTextWriter : SourceTextWriter
    {
        private readonly StringBuilder _builder;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release the pooled string builder back to the pool. At this point _builder should no longer be used.
                _pooledBuilder.Free();
            }

            base.Dispose(disposing);
        }

        public override SourceText ToSourceText()
        {
            return new StringText(_builder.ToString(), _encoding, checksumAlgorithm: _checksumAlgorithm);
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
