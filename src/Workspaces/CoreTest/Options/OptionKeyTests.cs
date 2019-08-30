// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Options
{
    public class OptionKeyTests
    {
        [Fact]
        public void ToStringForOption()
        {
            var option = new Option<bool>("FooFeature", "BarName");
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
            var optionKey = default(OptionKey);
            var toStringResult = optionKey.ToString();
            Assert.Equal("", toStringResult);
        }
    }
}
