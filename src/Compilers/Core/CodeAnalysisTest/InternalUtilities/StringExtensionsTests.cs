// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;

namespace Roslyn.Utilities.UnitTests.InternalUtilities
{
    public class StringExtensionsTests
    {
        [Fact]
        public void GetNumeral1()
        {
            Assert.Equal("0", StringExtensions.GetNumeral(0));
            Assert.Equal("5", StringExtensions.GetNumeral(5));
            Assert.Equal("10", StringExtensions.GetNumeral(10));
            Assert.Equal("10000000", StringExtensions.GetNumeral(10000000));
        }
    }
}
