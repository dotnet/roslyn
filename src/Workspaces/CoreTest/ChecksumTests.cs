// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ChecksumTests
    {
#if NET
        [Fact]
        public void ValidateChecksumFromSpanSameAsChecksumFromBytes1()
        {
            var checksum1 = Checksum.Create("Goo");
            var checksum2 = Checksum.Create("Bar");

            var checksumA = Checksum.TestAccessor.CrateUsingByteArrays(checksum1, checksum2);
            var checksumB = Checksum.TestAccessor.CreateUsingSpans(checksum1, checksum2);

            Assert.Equal(checksumA, checksumB);
        }

        [Fact]
        public void ValidateChecksumFromSpanSameAsChecksumFromBytes2()
        {
            var checksum1 = Checksum.Create("Goo");
            var checksum2 = Checksum.Create("Bar");
            var checksum3 = Checksum.Create("Baz");

            var checksumA = Checksum.TestAccessor.CrateUsingByteArrays(checksum1, checksum2, checksum3);
            var checksumB = Checksum.TestAccessor.CreateUsingSpans(checksum1, checksum2, checksum3);

            Assert.Equal(checksumA, checksumB);
        }
#endif
    }
}
