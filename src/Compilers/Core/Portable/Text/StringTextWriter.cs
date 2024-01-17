// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal sealed class StringTextWriter : SourceTextWriter
    {
        private readonly StringBuilder _builder;
        private readonly Encoding? _encoding;
        private readonly SourceHashAlgorithm _checksumAlgorithm;

#if DEBUG
        private readonly int _length;
#endif

        public StringTextWriter(Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, int length)
        {
            _builder = new StringBuilder(length);
            _encoding = encoding;
            _checksumAlgorithm = checksumAlgorithm;
#if DEBUG
            _length = length;
#endif
        }

        // https://github.com/dotnet/roslyn/issues/40830
        public override Encoding Encoding
        {
            get { return _encoding!; }
        }

        public override SourceText ToSourceText()
        {
#if DEBUG
            RoslynDebug.Assert(_builder.Length == _length);
#endif
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
