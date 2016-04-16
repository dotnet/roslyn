// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
