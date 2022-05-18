// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

// Note: Keep the namespace and class name shorter (skipping WorkspaceServiceTests namespace and using DirectMemoryReaderTests
// as opposed to DirectMemoryAccessStreamReaderTests). When we use the full names, CI fails due to a failure to upload test
// results, which seems to be caused by the fully qualified test name being too long. In this case, some tests have a lot of
// parameters, causing their full name to be quite long in combination with the longer namespace and class name.
// The issue is tracked here: https://github.com/dotnet/arcade/issues/9389
namespace Microsoft.CodeAnalysis.UnitTests
{
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public sealed class DirectMemoryReaderTests
    {
        [Fact]
        public void PeakRead()
        {
            using var _ = CreateReader("text", out var reader);
            Assert.Equal('t', reader.Read());
            Assert.Equal('e', reader.Peek());
            Assert.Equal('e', reader.Read());
            Assert.Equal('x', reader.Read());
            Assert.Equal('t', reader.Read());
            Assert.Equal(-1, reader.Peek());
            Assert.Equal(-1, reader.Read());
        }

        [Theory]
        [InlineData(5, 0, 5, "bcdef", 5, 'g')]
        [InlineData(5, 0, 2, "bc\0\0\0", 2, 'd')]
        [InlineData(10, 1, 6, "\0bcdefg\0\0\0", 6, 'h')]
        [InlineData(10, 1, 7, "\0bcdefgh\0\0", 7, -1)]
        [InlineData(10, 1, 9, "\0bcdefgh\0\0", 7, -1)]
        [InlineData(10, 3, 7, "\0\0\0bcdefgh", 7, -1)]
        [InlineData(10, 3, 0, "\0\0\0\0\0\0\0\0\0\0", 0, 'b')]
        [InlineData(10, 10, 0, "\0\0\0\0\0\0\0\0\0\0", 0, 'b')]
        [InlineData(0, 0, 0, "", 0, 'b')]
        public void ReadToArray(int bufferLength, int index, int count, string expected, int expectedResult, int expectedPeek)
        {
            TestWithMethod(reader => reader.Read);
            TestWithMethod(reader => reader.ReadBlock);

            void TestWithMethod(Func<TextReader, ReadToArrayDelegate> readMethodAccessor)
            {
                using var _ = CreateReader("abcdefgh", out var reader);
                var readMethod = readMethodAccessor(reader);

                Assert.Equal('a', reader.Read());

                var buffer = new char[bufferLength];
                Assert.Equal(expectedResult, readMethod(buffer, index, count));
                Assert.Equal(expected, new string(buffer));

                Assert.Equal(expectedPeek, reader.Peek());
            }
        }

#if NETCOREAPP
        [Theory]
        [InlineData(2, "bc", 2, 'd')]
        [InlineData(7, "bcdefgh", 7, -1)]
        [InlineData(10, "bcdefgh\0\0\0", 7, -1)]
        [InlineData(0, "", 0, 'b')]
        public void ReadToSpan(int bufferLength, string expected, int expectedResult, int expectedPeek)
        {
            TestWithMethod(reader => reader.Read);
            TestWithMethod(reader => reader.ReadBlock);

            void TestWithMethod(Func<TextReader, ReadToSpanDelegate> readMethodAccessor)
            {
                using var _ = CreateReader("abcdefgh", out var reader);
                var readMethod = readMethodAccessor(reader);

                Assert.Equal('a', reader.Read());

                var buffer = new char[bufferLength];
                Assert.Equal(expectedResult, readMethod(buffer));
                Assert.Equal(expected, new string(buffer));

                Assert.Equal(expectedPeek, reader.Peek());
            }
        }
#endif

        [Fact]
        public void ReadToArrayErrors()
        {
            TestWithMethod(reader => reader.Read);
            TestWithMethod(reader => reader.ReadBlock);

            void TestWithMethod(Func<TextReader, ReadToArrayDelegate> readMethodAccessor)
            {
                using var _ = CreateReader("abcdefgh", out var reader);
                var readMethod = readMethodAccessor(reader);

                var buffer = new char[3];
                Assert.Throws<ArgumentNullException>("buffer", () => readMethod(null!, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>("index", () => readMethod(buffer, -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>("count", () => readMethod(buffer, 0, -1));
                Assert.Throws<ArgumentException>(null, () => readMethod(buffer, 0, 4));
                Assert.Throws<ArgumentException>(null, () => readMethod(buffer, 3, 1));
            }
        }

        [Fact]
        public void ReadToEnd()
        {
            using (CreateReader("text", out var reader1))
            {
                Assert.Equal("text", reader1.ReadToEnd());
                Assert.Equal(-1, reader1.Peek());
                Assert.Equal("", reader1.ReadToEnd());
            }

            using (CreateReader("text", out var reader2))
            {
                Assert.Equal('t', reader2.Read());
                Assert.Equal("ext", reader2.ReadToEnd());
                Assert.Equal(-1, reader2.Peek());
                Assert.Equal("", reader2.ReadToEnd());
            }
        }

        private static unsafe Disposer CreateReader(string text, out TemporaryStorageServiceFactory.DirectMemoryAccessStreamReader reader)
        {
            var handle = GCHandle.Alloc(text.ToCharArray(), GCHandleType.Pinned);
            reader = new((char*)handle.AddrOfPinnedObject(), text.Length);

#pragma warning disable RS0042 // Do not copy value
            return new Disposer(handle, reader);
#pragma warning restore RS0042 // Do not copy value
        }

        private sealed class Disposer : IDisposable
        {
            private GCHandle _handle;
            private readonly TextReader _textReader;

            public Disposer(GCHandle handle, TextReader textReader)
            {
#pragma warning disable RS0042 // Do not copy value
                _handle = handle;
#pragma warning restore RS0042 // Do not copy value
                _textReader = textReader;
            }

            public void Dispose()
            {
                _textReader.Dispose();
                _handle.Free();
            }
        }

        private delegate int ReadToArrayDelegate(char[] buffer, int index, int count);
        private delegate int ReadToSpanDelegate(Span<char> buffer);
    }
}
