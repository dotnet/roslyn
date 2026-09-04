// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.Win32;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class DeveloperSettingsTests
{
    [ConditionalFact(typeof(WindowsOnly))]
    public void LongPathsAreEnabledOnWindows()
    {
        Debug.Assert(PlatformInformation.IsWindows);

        using var fileSystemKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");
        var longPathsEnabled = fileSystemKey?.GetValue("LongPathsEnabled") as int? ?? 0;

        Assert.Equal(1, longPathsEnabled);
    }

    [Fact]
    public void SourceFileLineEndingsMatchPlatform()
    {
        // The line break inside this literal is taken verbatim from this source file, so it
        // reflects the line endings the file was checked out with.
        const string twoLines = """
            first
            second
            """;

        var lineEnding = twoLines["first".Length..^"second".Length];

        Assert.Equal(Environment.NewLine, lineEnding);
    }
}
