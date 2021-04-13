// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.Cci;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    /// <summary>
    /// Test <see cref="MetadataWriter.IsTooLongInternal"/>.
    /// </summary>
    public class MetadataNameLimitsTests
    {
        [Fact]
        public void TestEmpty()
        {
            CheckIsTooLong("", 5, true);
        }

        [Fact]
        public void TestSingleByte()
        {
            CheckIsTooLong("a", 5, true);
            CheckIsTooLong("abc", 5, true);

            CheckIsTooLong("abcdef", 5, false);
        }

        [Fact]
        public void TestDoubleByte()
        {
            CheckIsTooLong("\u070F", 5, true);
            CheckIsTooLong("\u070Fxyz", 5, true);

            CheckIsTooLong("abc\u070Fxyz", 5, false);
        }

        [Fact]
        public void TestTripleByte()
        {
            CheckIsTooLong("\uFFFF", 5, true);
            CheckIsTooLong("\uFFFFyz", 5, true);

            CheckIsTooLong("abc\uFFFFxyz", 5, false);
        }

        private static void CheckIsTooLong(string fullName, int maxLength, bool withinLimit)
        {
            Assert.NotEqual(withinLimit, MetadataWriter.IsTooLongInternal(fullName, maxLength));
        }
    }
}
