// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
