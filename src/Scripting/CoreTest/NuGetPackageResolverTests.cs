// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    public class NuGetPackageResolverTests : TestBase
    {
        [Fact]
        public void ParsePackageNameAndVersion()
        {
            ParseInvalidPackageReference("A");
            ParseInvalidPackageReference("A/1");
            ParseInvalidPackageReference("nuget");
            ParseInvalidPackageReference("nuget:");
            ParseInvalidPackageReference("NUGET:");
            ParseInvalidPackageReference("nugetA/1");

            ParseValidPackageReference("nuget:A", "A", "");
            ParseValidPackageReference("nuget:A.B", "A.B", "");
            ParseValidPackageReference("nuget:  ", "  ", "");

            ParseInvalidPackageReference("nuget:A/");
            ParseInvalidPackageReference("nuget:A//1.0");
            ParseInvalidPackageReference("nuget:/1.0.0");
            ParseInvalidPackageReference("nuget:A/B/2.0.0");

            ParseValidPackageReference("nuget::nuget/1", ":nuget", "1");
            ParseValidPackageReference("nuget:A/1", "A", "1");
            ParseValidPackageReference("nuget:A.B/1.0.0", "A.B", "1.0.0");
            ParseValidPackageReference("nuget:A/B.C", "A", "B.C");
            ParseValidPackageReference("nuget:  /1", "  ", "1");
            ParseValidPackageReference("nuget:A\t/\n1.0\r ", "A\t", "\n1.0\r ");
        }

        private static void ParseValidPackageReference(string reference, string expectedName, string expectedVersion)
        {
            string name;
            string version;
            Assert.True(NuGetPackageResolver.TryParsePackageReference(reference, out name, out version));
            Assert.Equal(expectedName, name);
            Assert.Equal(expectedVersion, version);
        }

        private static void ParseInvalidPackageReference(string reference)
        {
            string name;
            string version;
            Assert.False(NuGetPackageResolver.TryParsePackageReference(reference, out name, out version));
            Assert.Null(name);
            Assert.Null(version);
        }
    }
}
