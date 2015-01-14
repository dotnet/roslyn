// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
