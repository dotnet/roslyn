// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    using Roslyn.Reflection.Metadata.Ecma335.Blobs;

    public class LabelHandleTests
    {
        [Fact]
        public void Equality()
        {
            var a1 = new LabelHandle(1);
            var a2 = new LabelHandle(2);
            var b1 = new LabelHandle(1);

            Assert.False(((object)a1).Equals(a2));
            Assert.False(a1.Equals(new object()));
            Assert.False(a1.Equals(a2));
            Assert.False(a1 == a2);

            Assert.True(((object)a1).Equals(b1));
            Assert.True(a1.Equals(b1));
            Assert.True(a1 == b1);

            Assert.Equal(a1.GetHashCode(), b1.GetHashCode());
        }
    }
}
