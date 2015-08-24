// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Xunit;

namespace Microsoft.CodeAnalysis.Collections.UnitTests
{
    public class ByteSequenceComparerTests
    {
        [Fact]
        public void Equals1()
        {
            Assert.True(ByteSequenceComparer.Equals(new byte[] { }, new byte[] { }));
            Assert.True(ByteSequenceComparer.Equals(new byte[] { 1 }, new byte[] { 1 }));
            Assert.False(ByteSequenceComparer.Equals(new byte[] { 1 }, new byte[] { 2 }));
            Assert.True(ByteSequenceComparer.Equals(new byte[] { 1, 2 }, new byte[] { 1, 2 }));
            Assert.False(ByteSequenceComparer.Equals(new byte[] { 1, 2 }, new byte[] { 1, 3 }));
        }

        [Fact]
        public void Equals2()
        {
            Assert.True(ByteSequenceComparer.Equals(new byte[] { }, 0, new byte[] { }, 0, 0));
            Assert.True(ByteSequenceComparer.Equals(new byte[] { 1 }, 0, new byte[] { }, 0, 0));
            Assert.True(ByteSequenceComparer.Equals(new byte[] { 1 }, 1, new byte[] { 1 }, 1, 0));
            Assert.True(ByteSequenceComparer.Equals(new byte[] { 1 }, 0, new byte[] { 1 }, 0, 1));
            Assert.False(ByteSequenceComparer.Equals(new byte[] { 1 }, 0, new byte[] { 2 }, 0, 1));
            Assert.True(ByteSequenceComparer.Equals(new byte[] { 1, 2 }, 1, new byte[] { 2 }, 0, 1));
        }

        [Fact]
        public void Equals3()
        {
            var b = new byte[] { 1, 2, 1 };

            Assert.True(ByteSequenceComparer.Equals(b, b));
            Assert.True(ByteSequenceComparer.Equals(b, 0, b, 0, 1));
            Assert.True(ByteSequenceComparer.Equals(b, 2, b, 2, 1));
            Assert.True(ByteSequenceComparer.Equals(b, 0, b, 2, 1));
            Assert.False(ByteSequenceComparer.Equals(b, 0, b, 1, 1));

            Assert.False(ByteSequenceComparer.Equals(null, b));
            Assert.False(ByteSequenceComparer.Equals(null, new byte[] { }));
            Assert.True(ByteSequenceComparer.Equals(null, null));
        }
    }
}
