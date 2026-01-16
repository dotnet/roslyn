// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

using BinLogPathProvider = MSBuildProjectLoader.BinLogPathProvider;

public sealed class DefaultBinLogPathProviderTests
{
    private const string DefaultFileName = "msbuild";
    private const string DefaultExtension = ".binlog";

    private static string RelativeLogDirectory => $".{Path.DirectorySeparatorChar}logs";
    private static string LogDirectory => Path.GetFullPath(RelativeLogDirectory);
    private static string LogFileName => "mylog";
    private static string LogExtension => ".mylog";

    [Fact]
    public void DefaultBinLogPathProvider_ExpandsRelativePath()
    {
        var logPath = Path.Combine(RelativeLogDirectory, LogFileName + LogExtension);
        var provider = new BinLogPathProvider(logPath);
        AssertUniquePaths(provider, LogDirectory, LogFileName, LogExtension);
    }

    [Fact]
    public void DefaultBinLogPathProvider_UsesDefaultExtension()
    {
        var logPath = Path.Combine(LogDirectory, LogFileName);
        var provider = new BinLogPathProvider(logPath);
        AssertUniquePaths(provider, LogDirectory, LogFileName, DefaultExtension);
    }

    [Fact]
    public void DefaultBinLogPathProvider_UsesDefaultFileName()
    {
        var provider = new BinLogPathProvider(LogDirectory + Path.DirectorySeparatorChar);
        AssertUniquePaths(provider, LogDirectory, DefaultFileName, DefaultExtension);
    }

    private static void AssertUniquePaths(BinLogPathProvider provider, string expectedDirectory, string expectedFilePrefix, string expectedExtension)
    {
        var newLogPaths = Enumerable.Range(0, 10)
            .Select(_ => provider.GetNewLogPath())
            .ToImmutableHashSet();
        Assert.Equal(10, newLogPaths.Count);

        foreach (var newLogPath in newLogPaths)
        {
            var newLogDirectory = Path.GetDirectoryName(newLogPath);
            var newLogFileName = Path.GetFileNameWithoutExtension(newLogPath);
            var newLogExtension = Path.GetExtension(newLogPath);

            Assert.Equal(expectedDirectory, newLogDirectory);
            Assert.StartsWith(expectedFilePrefix, newLogFileName);
            Assert.Equal(expectedExtension, newLogExtension);
        }
    }
}
