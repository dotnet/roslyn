// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public abstract class TextReaderTestBase
    {
        [Fact]
        public void PeakRead()
        {
            using var _ = CreateReaders("text", out var referenceReader, out var reader);
            AssertAllEqual('t', referenceReader.Read(), reader.Read());
            AssertAllEqual('e', referenceReader.Peek(), reader.Peek());
            AssertAllEqual('e', referenceReader.Read(), reader.Read());
            AssertAllEqual('x', referenceReader.Read(), reader.Read());
            AssertAllEqual('t', referenceReader.Read(), reader.Read());
            AssertAllEqual(-1, referenceReader.Peek(), reader.Peek());
            AssertAllEqual(-1, referenceReader.Read(), reader.Read());
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
                using var _ = CreateReaders("abcdefgh", out var referenceReader, out var reader);
                var referenceReadMethod = readMethodAccessor(referenceReader);
                var readMethod = readMethodAccessor(reader);

                AssertAllEqual('a', referenceReader.Read(), reader.Read());

                var referenceBuffer = new char[bufferLength];
                var buffer = new char[bufferLength];
                AssertAllEqual(expectedResult, referenceReadMethod(referenceBuffer, index, count), readMethod(buffer, index, count));
                AssertAllEqual(expected, new string(referenceBuffer), new string(buffer));

                AssertAllEqual(expectedPeek, referenceReader.Peek(), reader.Peek());
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
                using var _ = CreateReaders("abcdefgh", out var referenceReader, out var reader);
                var referenceReadMethod = readMethodAccessor(referenceReader);
                var readMethod = readMethodAccessor(reader);

                AssertAllEqual('a', referenceReader.Read(), reader.Read());

                var referenceBuffer = new char[bufferLength];
                var buffer = new char[bufferLength];
                AssertAllEqual(expectedResult, referenceReadMethod(referenceBuffer), readMethod(buffer));
                AssertAllEqual(expected, new string(referenceBuffer), new string(buffer));

                AssertAllEqual(expectedPeek, referenceReader.Peek(), reader.Peek());
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
                using var _ = CreateReaders("abcdefgh", out var _, out var reader);
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
            using (CreateReaders("text", out var referenceReader, out var reader))
            {
                AssertAllEqual("text", referenceReader.ReadToEnd(), reader.ReadToEnd());
                AssertAllEqual(-1, referenceReader.Peek(), reader.Peek());
                AssertAllEqual("", referenceReader.ReadToEnd(), reader.ReadToEnd());
            }

            using (CreateReaders("text", out var referenceReader, out var reader))
            {
                AssertAllEqual('t', referenceReader.Read(), reader.Read());
                AssertAllEqual("ext", referenceReader.ReadToEnd(), reader.ReadToEnd());
                AssertAllEqual(-1, referenceReader.Peek(), reader.Peek());
                AssertAllEqual("", referenceReader.ReadToEnd(), reader.ReadToEnd());
            }
        }

        private static void AssertAllEqual<T>(T expected1, T expected2, T actual)
        {
            Assert.Equal(expected1, actual);
            Assert.Equal(expected2, actual);
        }

        private IDisposable CreateReaders(string text, out TextReader referenceReader, out TextReader reader)
        {
            referenceReader = new StringReader(text);
            (var disposer, reader) = CreateReader(text);

            return new CombinedDisposable(referenceReader, disposer, reader);
        }

        protected abstract (IDisposable? disposer, TextReader reader) CreateReader(string text);

        private sealed class CombinedDisposable : IDisposable
        {
            private ImmutableArray<IDisposable?> _values;

            public CombinedDisposable(params IDisposable?[] values)
            {
                _values = values.ToImmutableArray();
            }

            public void Dispose()
            {
                for (var i = _values.Length - 1; i >= 0; --i)
                    _values[i]?.Dispose();

                _values = _values.Clear();
            }
        }

        private delegate int ReadToArrayDelegate(char[] buffer, int index, int count);
        private delegate int ReadToSpanDelegate(Span<char> buffer);
    }
}
