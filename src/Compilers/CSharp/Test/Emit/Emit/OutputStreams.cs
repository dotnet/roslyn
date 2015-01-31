// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    internal class NameResolver
    {
        public string GetDebugInformationFileName(SyntaxTree syntaxTree)
        {
            throw new NotImplementedException();
        }

        public Stream GetXmlInclude(SyntaxTree syntaxTree, string xmlIncludeFile)
        {
            throw new NotImplementedException();
        }
    }

    internal class BrokenStream : Stream
    {
        public int BreakHow;

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get
            {
                return 0;
            }
        }

        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
                if (BreakHow == 1)
                    throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
            if (BreakHow == 2)
                throw new IOException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (BreakHow == 0)
                throw new IOException();
        }
    }
}
