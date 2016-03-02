// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class VersionHelperTests
    {
        [Fact]
        public void ParseGood()
        {
            Version version;

            Assert.True(VersionHelper.TryParseAssemblyVersion("3.2.*", allowWildcard: true, version: out version));
            Assert.Equal(3, version.Major);
            Assert.Equal(2, version.Minor);

            Assert.Equal(65535, version.Build);
            Assert.Equal(65535, version.Revision);

            Assert.True(VersionHelper.TryParseAssemblyVersion("1.2.3.*", allowWildcard: true, version: out version));
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Build);
            Assert.Equal(65535, version.Revision);
        }

        [Fact]
        public void TimeBased()
        {
            var now = DateTime.Now;
            int days, seconds;
            VersionTestHelpers.GetDefautVersion(now, out days, out seconds);

            var version = VersionHelper.GenerateVersionFromPatternAndCurrentTime(now, new Version(3, 2, 65535, 65535));
            Assert.Equal(3, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(days, version.Build); 
            Assert.Equal(seconds, version.Revision);

            version = VersionHelper.GenerateVersionFromPatternAndCurrentTime(now, new Version(1, 2, 3, 65535));

            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Build);
            Assert.Equal(seconds, version.Revision);

            version = VersionHelper.GenerateVersionFromPatternAndCurrentTime(now, new Version(1, 2, 3, 4));
            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(3, version.Build);
            Assert.Equal(4, version.Revision);

            Assert.Null(VersionHelper.GenerateVersionFromPatternAndCurrentTime(now, null));
        }

        [Fact]
        public void ParseGood2()
        {
            Version version;
            Assert.True(VersionHelper.TryParse("1.234.56.7", out version));
            Assert.Equal(1, version.Major);
            Assert.Equal(234, version.Minor);
            Assert.Equal(56, version.Build);
            Assert.Equal(7, version.Revision);

            Assert.True(VersionHelper.TryParse("3.2.1", out version));
            Assert.Equal(3, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(1, version.Build);
            Assert.Equal(0, version.Revision);

            Assert.True(VersionHelper.TryParse("3.2", out version));
            Assert.Equal(3, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(0, version.Build);
            Assert.Equal(0, version.Revision);

            Assert.True(VersionHelper.TryParse("3", out version));
            Assert.Equal(3, version.Major);
            Assert.Equal(0, version.Minor);
            Assert.Equal(0, version.Build);
            Assert.Equal(0, version.Revision);
        }

        [Fact]
        public void ParseBad()
        {
            Version version;
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.234.56.7.*", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.234.56.7.1", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("*", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.2. *", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.2.* ", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.*", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.1.*.*", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("   ", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion(null, allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("a", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("********", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("...", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion(".a.b.", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion(".0.1.", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("65535.65535.65535.65535", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("65535.65535.65535.65535", allowWildcard: false, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion(" 1.2.3.4", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1 .2.3.4", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.2.3.4 ", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.2.3. 4", allowWildcard: true, version: out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParseAssemblyVersion("1.2. 3.4", allowWildcard: true, version: out version));
            Assert.Null(version);

            // U+FF11 FULLWIDTH DIGIT ONE 
            Assert.False(VersionHelper.TryParseAssemblyVersion("\uFF11.\uFF10.\uFF10.\uFF10", allowWildcard: true, version: out version));
            Assert.Null(version);
        }

        [Fact]
        public void ParseBad2()
        {
            Version version;
            Assert.False(VersionHelper.TryParse("", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse(null, out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("a", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("********", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("...", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse(".a.b.", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse(".1.2.", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("1.234.56.7.8", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("*", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("1.2. 3", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("1.2.3 ", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("1.a", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("1.2.a.b", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("-1.2.3.4", out version));
            Assert.Null(version);
            Assert.False(VersionHelper.TryParse("1.-2.3.4", out version));
            Assert.Null(version);

            // U+FF11 FULLWIDTH DIGIT ONE 
            Assert.False(VersionHelper.TryParse("\uFF11.\uFF10.\uFF10.\uFF10", out version));
            Assert.Null(version);
        }
    }
}
