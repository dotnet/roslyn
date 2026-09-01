// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
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
    public void LineEndingsAreConfiguredForGitNormalization()
    {
        var gitAttributes = File.ReadAllLines(GetRepositoryFilePath(".gitattributes"));

        Assert.Contains(gitAttributes, line => HasGitAttributes(line, "*", "text=auto", "encoding=UTF-8"));
        Assert.Contains(gitAttributes, line => HasGitAttributes(line, "*.sh", "text", "eol=lf"));
        Assert.Contains(gitAttributes, line => HasGitAttributes(line, "*.cs", "diff=csharp", "text"));
        Assert.Contains(gitAttributes, line => HasGitAttributes(line, "*.vb", "text"));
    }

    private static bool HasGitAttributes(string line, string pattern, params string[] expectedAttributes)
    {
        var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == expectedAttributes.Length + 1 &&
            parts[0] == pattern &&
            expectedAttributes.All(attribute => parts.Contains(attribute, StringComparer.Ordinal));
    }

    private static string GetRepositoryFilePath(string fileName, [CallerFilePath] string sourceFilePath = "")
        => Path.Combine(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", "..", "..", "..")), fileName);
}
