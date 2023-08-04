// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal class StringTextWriter : SourceTextWriter
    {
        private readonly StringBuilder _builder;
        private readonly Encoding? _encoding;
        private readonly SourceHashAlgorithm _checksumAlgorithm;

        public StringTextWriter(Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, int capacity)
        {
            _builder = new StringBuilder(capacity);
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
            ValidateWriteArguments(buffer, index, count);

            _builder.Append(buffer, index, count);
        }
    }
}
