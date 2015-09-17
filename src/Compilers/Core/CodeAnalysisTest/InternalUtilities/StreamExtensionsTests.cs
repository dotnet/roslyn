// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using System;
using System.IO;
using Xunit;

namespace Roslyn.Utilities.UnitTests.InternalUtilities
{
    public class StreamExtensionsTests
    {
        [Fact]
        public void CallsReadMultipleTimes()
        {
            var firstRead = true;
            var sourceArray = new byte[] { 1, 2, 3, 4 };
            int sourceOffset = 0;

            var stream = new TestStream(readFunc: (buf, offset, count) =>
            {
                if (firstRead)
                {
                    count = count / 2;
                    firstRead = false;
                }
                Array.Copy(sourceArray, sourceOffset, buf, offset, count);
                sourceOffset += count;
                return count;
            });

            var destArray = new byte[4];
            var destCopy = destArray.AsImmutable();
            // Note: Buffer is in undefined state after receiving an exception
            Assert.Equal(sourceArray.Length, stream.TryReadAll(destArray, 0, sourceArray.Length));
            Assert.Equal(sourceArray, destArray);
        }

        [Fact]
        public void ExceptionsPropagate()
        {
            var buffer = new byte[10];

            var stream = new TestStream(readFunc: (_1, _2, _3) => { throw new IOException(); });
            Assert.Throws<IOException>(() => stream.TryReadAll(null, 0, 1));

            stream = new TestStream(readFunc: (buf, offset, count) =>
            {
                if (offset + count > buf.Length)
                {
                    throw new ArgumentException();
                }
                return 0;
            });
            Assert.Equal(0, stream.TryReadAll(buffer, 0, 1));
            Assert.Throws<ArgumentException>(() => stream.TryReadAll(buffer, 0, 100));
        }

        [Fact]
        public void ExceptionMayChangeOutput()
        {
            var firstRead = true;
            var sourceArray = new byte[] { 1, 2, 3, 4 };

            var stream = new TestStream(readFunc: (buf, offset, count) =>
            {
                if (firstRead)
                {
                    count = count / 2;
                    Array.Copy(sourceArray, 0, buf, offset, count);
                    firstRead = false;
                    return count;
                }
                throw new IOException();
            });

            var destArray = new byte[4];
            var destCopy = destArray.AsImmutable();
            // Note: Buffer is in undefined state after receiving an exception
            Assert.Throws<IOException>(() => stream.TryReadAll(destArray, 0, sourceArray.Length));
            Assert.NotEqual(destArray, destCopy);
        }

        [Fact]
        public void ExceptionMayChangePosition()
        {
            var firstRead = true;
            var sourceArray = new byte[] { 1, 2, 3, 4 };
            var backingStream = new MemoryStream(sourceArray);

            var stream = new TestStream(readFunc: (buf, offset, count) =>
            {
                if (firstRead)
                {
                    count = count / 2;
                    backingStream.Read(buf, offset, count);
                    firstRead = false;
                    return count;
                }
                throw new IOException();
            });

            var destArray = new byte[4];
            Assert.Equal(0, backingStream.Position);
            Assert.Throws<IOException>(() => stream.TryReadAll(destArray, 0, sourceArray.Length));
            Assert.Equal(2, backingStream.Position);
        }

        [Fact]
        public void PrematureEndOfStream()
        {
            var sourceArray = new byte[] { 1, 2, 3, 4 };
            var stream = new MemoryStream(sourceArray);

            var destArray = new byte[6];
            // Try to read more bytes than exist in the stream
            Assert.Equal(4, stream.TryReadAll(destArray, 0, 6));
            var expected = new byte[] { 1, 2, 3, 4, 0, 0 };
            Assert.Equal(expected, destArray);
        }
    }
}
