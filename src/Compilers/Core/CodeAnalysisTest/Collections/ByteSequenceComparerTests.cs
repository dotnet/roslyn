// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;

namespace Microsoft.CodeAnalysis.Collections.UnitTests
{
    public class ByteSequenceComparerTests
    {
        [Fact]
        public void Equals1()
        {
            Assert.True(ByteSequenceComparer.Equals([], []));
            Assert.True(ByteSequenceComparer.Equals([1], [1]));
            Assert.False(ByteSequenceComparer.Equals([1], [2]));
            Assert.True(ByteSequenceComparer.Equals([1, 2], [1, 2]));
            Assert.False(ByteSequenceComparer.Equals([1, 2], [1, 3]));
        }

        [Fact]
        public void Equals2()
        {
            Assert.True(ByteSequenceComparer.Equals([], 0, [], 0, 0));
            Assert.True(ByteSequenceComparer.Equals([1], 0, [], 0, 0));
            Assert.True(ByteSequenceComparer.Equals([1], 1, [1], 1, 0));
            Assert.True(ByteSequenceComparer.Equals([1], 0, [1], 0, 1));
            Assert.False(ByteSequenceComparer.Equals([1], 0, [2], 0, 1));
            Assert.True(ByteSequenceComparer.Equals([1, 2], 1, [2], 0, 1));
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
            Assert.False(ByteSequenceComparer.Equals(null, []));
            Assert.True(ByteSequenceComparer.Equals(null, null));
        }
    }
}
