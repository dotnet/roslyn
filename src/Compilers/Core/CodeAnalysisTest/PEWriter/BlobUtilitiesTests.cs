// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.PEWriter
{
    public unsafe class BlobUtilitiesTests
    {
        private void TestGetUTF8ByteCount(int expectedCount, string expectedRemainder, string str, int charCount, int byteLimit)
        {
            fixed (char* ptr = str)
            {
                char* remainderPtr;
                Assert.Equal(expectedCount, BlobUtilities.GetUTF8ByteCount(ptr, charCount, byteLimit, out remainderPtr));

                string remainder = new string(remainderPtr);
                Assert.Equal(expectedRemainder, remainder);
            }
        }

        [Fact]
        public void GetUTF8ByteCount()
        {
            TestGetUTF8ByteCount(0, "", str: "", charCount: 0, byteLimit: int.MaxValue);
            TestGetUTF8ByteCount(2, "c", str: "abc", charCount: 2, byteLimit: int.MaxValue);
            TestGetUTF8ByteCount(2, "", str: "\u0123", charCount: 1, byteLimit: int.MaxValue);
            TestGetUTF8ByteCount(3, "", str: "\u1234", charCount: 1, byteLimit: int.MaxValue);
            TestGetUTF8ByteCount(3, "", str: "\ud800", charCount: 1, byteLimit: int.MaxValue);
            TestGetUTF8ByteCount(3, "", str: "\udc00", charCount: 1, byteLimit: int.MaxValue);
            TestGetUTF8ByteCount(3, "\udc00", str: "\ud800\udc00", charCount: 1, byteLimit: int.MaxValue); // surrogate pair
            TestGetUTF8ByteCount(4, "", str: "\ud800\udc00", charCount: 2, byteLimit: int.MaxValue); // surrogate pair

            TestGetUTF8ByteCount(0, "", str: "", charCount: 0, byteLimit: 0);
            TestGetUTF8ByteCount(0, "abc", str: "abc", charCount: 2, byteLimit: 0);
            TestGetUTF8ByteCount(0, "\u0123", str: "\u0123", charCount: 1, byteLimit: 0);
            TestGetUTF8ByteCount(0, "\u1234", str: "\u1234", charCount: 1, byteLimit: 0);
            TestGetUTF8ByteCount(0, "\ud800", str: "\ud800", charCount: 1, byteLimit: 0);
            TestGetUTF8ByteCount(0, "\udc00", str: "\udc00", charCount: 1, byteLimit: 0);
            TestGetUTF8ByteCount(0, "\ud800\udc00", str: "\ud800\udc00", charCount: 1, byteLimit: 0);
            TestGetUTF8ByteCount(0, "\ud800\udc00", str: "\ud800\udc00", charCount: 2, byteLimit: 0); // surrogate pair

            TestGetUTF8ByteCount(0, "", str: "", charCount: 0, byteLimit: 1);
            TestGetUTF8ByteCount(1, "bc", str: "abc", charCount: 2, byteLimit: 1);
            TestGetUTF8ByteCount(0, "\u0123", str: "\u0123", charCount: 1, byteLimit: 1);
            TestGetUTF8ByteCount(0, "\u1234", str: "\u1234", charCount: 1, byteLimit: 1);
            TestGetUTF8ByteCount(0, "\ud800", str: "\ud800", charCount: 1, byteLimit: 1);
            TestGetUTF8ByteCount(0, "\udc00", str: "\udc00", charCount: 1, byteLimit: 1);
            TestGetUTF8ByteCount(0, "\ud800\udc00", str: "\ud800\udc00", charCount: 1, byteLimit: 1);
            TestGetUTF8ByteCount(0, "\ud800\udc00", str: "\ud800\udc00", charCount: 2, byteLimit: 1); // surrogate pair

            TestGetUTF8ByteCount(0, "", str: "", charCount: 0, byteLimit: 2);
            TestGetUTF8ByteCount(2, "c", str: "abc", charCount: 2, byteLimit: 2);
            TestGetUTF8ByteCount(2, "", str: "\u0123", charCount: 1, byteLimit: 2);
            TestGetUTF8ByteCount(0, "\u1234", str: "\u1234", charCount: 1, byteLimit: 2);
            TestGetUTF8ByteCount(0, "\ud800", str: "\ud800", charCount: 1, byteLimit: 2);
            TestGetUTF8ByteCount(0, "\udc00", str: "\udc00", charCount: 1, byteLimit: 2);
            TestGetUTF8ByteCount(0, "\ud800\udc00", str: "\ud800\udc00", charCount: 1, byteLimit: 2);
            TestGetUTF8ByteCount(0, "\ud800\udc00", str: "\ud800\udc00", charCount: 2, byteLimit: 2); // surrogate pair

            TestGetUTF8ByteCount(0, "", str: "", charCount: 0, byteLimit: 3);
            TestGetUTF8ByteCount(2, "c", str: "abc", charCount: 2, byteLimit: 3);
            TestGetUTF8ByteCount(2, "", str: "\u0123", charCount: 1, byteLimit: 3);
            TestGetUTF8ByteCount(3, "", str: "\u1234", charCount: 1, byteLimit: 3);
            TestGetUTF8ByteCount(3, "", str: "\ud800", charCount: 1, byteLimit: 3);
            TestGetUTF8ByteCount(3, "", str: "\udc00", charCount: 1, byteLimit: 3);
            TestGetUTF8ByteCount(3, "\udc00", str: "\ud800\udc00", charCount: 1, byteLimit: 3);
            TestGetUTF8ByteCount(0, "\ud800\udc00", str: "\ud800\udc00", charCount: 2, byteLimit: 3); // surrogate pair

            TestGetUTF8ByteCount(0, "", str: "", charCount: 0, byteLimit: 4);
            TestGetUTF8ByteCount(2, "c", str: "abc", charCount: 2, byteLimit: 4);
            TestGetUTF8ByteCount(2, "", str: "\u0123", charCount: 1, byteLimit: 4);
            TestGetUTF8ByteCount(3, "", str: "\u1234", charCount: 1, byteLimit: 4);
            TestGetUTF8ByteCount(3, "", str: "\ud800", charCount: 1, byteLimit: 4);
            TestGetUTF8ByteCount(3, "", str: "\udc00", charCount: 1, byteLimit: 4);
            TestGetUTF8ByteCount(3, "\udc00", str: "\ud800\udc00", charCount: 1, byteLimit: 4);
            TestGetUTF8ByteCount(4, "", str: "\ud800\udc00", charCount: 2, byteLimit: 4); // surrogate pair
        }
    }
}
