// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LanguageVersionTests
    {
        [Fact]
        public void CurrentVersion()
        {
            var highest = Enum.
                GetValues(typeof(LanguageVersion)).
                Cast<LanguageVersion>().
                Where(x => x != LanguageVersion.Latest && x != LanguageVersion.Preview && x != LanguageVersion.LatestMajor).
                Max();

            Assert.Equal(LanguageVersionFacts.CurrentVersion, highest);
        }
    }
}
