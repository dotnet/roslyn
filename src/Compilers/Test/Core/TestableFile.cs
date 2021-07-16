// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// This works with <see cref="TestableFileSystem"/> to have an "in memory" file that can 
    /// be manipulated by consumers of <see cref="ICommonCompilerFileSystem"/>
    /// 
    /// This isn't meant to handle complex file system interactions but the basic cases of open,
    /// close, create, read and write.
    /// </summary>
    public sealed class TestableFile
    {
        private sealed class TestableFileStream : MemoryStream
        {
            public TestableFile MemoryFile { get; }
            public bool CopyBack { get; }

            public TestableFileStream(TestableFile memoryFile)
            {
                Debug.Assert(!memoryFile.Exists);
                MemoryFile = memoryFile;
                MemoryFile.Exists = true;
                CopyBack = true;
            }

            public TestableFileStream(TestableFile memoryFile, byte[] bytes, bool writable)
                : base(bytes, writable)
            {
                Debug.Assert(memoryFile.Exists);
                MemoryFile = memoryFile;
                CopyBack = writable;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (CopyBack)
                    {
                        MemoryFile.Contents.Clear();
                        MemoryFile.Contents.AddRange(this.ToArray());
                    }
                }

                base.Dispose(disposing);
            }
        }

        public bool Exists { get; private set; }
        public List<byte> Contents { get; } = new List<byte>();

        public TestableFile()
        {
        }

        public TestableFile(string contents)
        {
            Exists = true;
            Contents.AddRange(Encoding.UTF8.GetBytes(contents));
        }

        public TestableFile(byte[] contents)
        {
            Exists = true;
            Contents.AddRange(contents);
        }

        public MemoryStream GetStream(FileAccess access = FileAccess.ReadWrite)
        {
            var writable = access is FileAccess.Write or FileAccess.ReadWrite;
            if (!Exists)
            {
                if (!writable)
                {
                    throw new InvalidOperationException();
                }

                return new TestableFileStream(this);
            }

            return new TestableFileStream(this, Contents.ToArray(), writable);
        }
    }
}
