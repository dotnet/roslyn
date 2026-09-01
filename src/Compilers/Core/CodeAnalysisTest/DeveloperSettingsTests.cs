// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class DeveloperSettingsTests
{
    [ConditionalFact(typeof(WindowsOnly))]
    public void LongPathsAreEnabledOnWindows()
    {
        using var fileSystemKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem");

        Assert.Equal(1, fileSystemKey?.GetValue("LongPathsEnabled"));
    }

    [Fact]
    public void SourceFileLineEndingsMatchPlatform()
    {
        var sourceText = File.ReadAllText(GetThisSourceFilePath());

        if (Path.DirectorySeparatorChar == '\\')
        {
            Assert.Contains("\r\n", sourceText);
            Assert.False(ContainsBareLineFeed(sourceText));
        }
        else
        {
            Assert.Contains("\n", sourceText);
            Assert.DoesNotContain("\r\n", sourceText);
        }
    }

    private static bool ContainsBareLineFeed(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n' && (i == 0 || text[i - 1] != '\r'))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetThisSourceFilePath([CallerFilePath] string sourceFilePath = "")
        => sourceFilePath;
}
