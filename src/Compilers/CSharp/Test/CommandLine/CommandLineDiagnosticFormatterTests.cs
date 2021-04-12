// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class CommandLineDiagnosticFormatterTests
    {
        [ConditionalFact(typeof(WindowsOnly))]
        public void GetPathNameRelativeToBaseDirectory()
        {
            var formatter = new CommandLineDiagnosticFormatter(
                baseDirectory: @"X:\rootdir\dir",
                displayFullPaths: true,
                displayEndLocations: true);

            Assert.Equal(@"a.cs", formatter.RelativizeNormalizedPath(@"X:\rootdir\dir\a.cs"));
            Assert.Equal(@"temp\a.cs", formatter.RelativizeNormalizedPath(@"X:\rootdir\dir\temp\a.cs"));
            Assert.Equal(@"Y:\rootdir\dir\a.cs", formatter.RelativizeNormalizedPath(@"Y:\rootdir\dir\a.cs"));

            formatter = new CommandLineDiagnosticFormatter(
                baseDirectory: @"X:\rootdir\..\rootdir\dir",
                displayFullPaths: true,
                displayEndLocations: true);

            Assert.Equal(@"a.cs", formatter.RelativizeNormalizedPath(@"X:\rootdir\dir\a.cs"));
            Assert.Equal(@"temp\a.cs", formatter.RelativizeNormalizedPath(@"X:\rootdir\dir\temp\a.cs"));
            Assert.Equal(@"Y:\rootdir\dir\a.cs", formatter.RelativizeNormalizedPath(@"Y:\rootdir\dir\a.cs"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        [WorkItem(14725, "https://github.com/dotnet/roslyn/issues/14725")]
        public void RelativizeNormalizedPathShouldHandleRootPaths_1()
        {
            var formatter = new CommandLineDiagnosticFormatter(
                baseDirectory: @"C:\",
                displayFullPaths: false,
                displayEndLocations: false);

            Assert.Equal(@"temp.cs", formatter.RelativizeNormalizedPath(@"c:\temp.cs"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        [WorkItem(14725, "https://github.com/dotnet/roslyn/issues/14725")]
        public void RelativizeNormalizedPathShouldHandleRootPaths_2()
        {
            var formatter = new CommandLineDiagnosticFormatter(
                baseDirectory: @"C:\A\B",
                displayFullPaths: false,
                displayEndLocations: false);

            Assert.Equal(@"C\D\temp.cs", formatter.RelativizeNormalizedPath(@"c:\A\B\C\D\temp.cs"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        [WorkItem(14725, "https://github.com/dotnet/roslyn/issues/14725")]
        public void RelativizeNormalizedPathShouldHandleDirectoriesWithSamePrefix_1()
        {
            var formatter = new CommandLineDiagnosticFormatter(
                baseDirectory: @"C:\AB",
                displayFullPaths: false,
                displayEndLocations: false);

            Assert.Equal(@"c:\ABCD\file.cs", formatter.RelativizeNormalizedPath(@"c:\ABCD\file.cs"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        [WorkItem(14725, "https://github.com/dotnet/roslyn/issues/14725")]
        public void RelativizeNormalizedPathShouldHandleDirectoriesWithSamePrefix_2()
        {
            var formatter = new CommandLineDiagnosticFormatter(
                baseDirectory: @"C:\ABCD",
                displayFullPaths: false,
                displayEndLocations: false);

            Assert.Equal(@"c:\AB\file.cs", formatter.RelativizeNormalizedPath(@"c:\AB\file.cs"));
        }
    }
}
