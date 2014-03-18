// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal class CancellableReader : TextReader
    {
        private readonly TextReader textReader;
        private readonly CancellationToken cancellationToken;

        public CancellableReader(TextReader textReader, CancellationToken cancellationToken)
        {
            this.textReader = textReader;
            this.cancellationToken = cancellationToken;
        }

        public override int Peek()
        {
            cancellationToken.ThrowIfCancellationRequested();
            return textReader.Peek();
        }

        public override int Read()
        {
            cancellationToken.ThrowIfCancellationRequested();
            return textReader.Read();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return textReader.Read(buffer, index, count);
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return textReader.ReadBlock(buffer, index, count);
        }

        public override string ReadLine()
        {
            cancellationToken.ThrowIfCancellationRequested();
            return textReader.ReadLine();
        }

        public override string ReadToEnd()
        {
            cancellationToken.ThrowIfCancellationRequested();
            return textReader.ReadToEnd();
        }
    }
}