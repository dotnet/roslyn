// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Options
{
    public class OptionKeyTests
    {
        [Fact]
        [Obsolete]
        public void ToStringForObsoleteOption()
        {
            var option = new Option<bool>("FooFeature", "BarName");
            var optionKey = new OptionKey(option);

            var toStringResult = optionKey.ToString();

            Assert.Equal("FooFeature - BarName", toStringResult);
        }

        [Fact]
        public void ToStringForOption()
        {
            var option = new Option<bool>("FooFeature", "BarName", false);
            var optionKey = new OptionKey(option);

            var toStringResult = optionKey.ToString();

            Assert.Equal("FooFeature - BarName", toStringResult);
        }

        [Fact]
        public void ToStringForPerLanguageOption()
        {
            var option = new PerLanguageOption<bool>("FooFeature", "BarName", false);
            var optionKey = new OptionKey(option, "BazLanguage");

            var toStringResult = optionKey.ToString();

            Assert.Equal("(BazLanguage) FooFeature - BarName", toStringResult);
        }
        [Fact]
        public void ToStringForDefaultOption()
        {
            // Verify ToString works gracefully for default option key.
            var optionKey = default(OptionKey);
            var toStringResult = optionKey.ToString();
            Assert.Equal("", toStringResult);

            // Also verify GetHashCode does not throw.
            _ = optionKey.GetHashCode();
        }
    }
}
