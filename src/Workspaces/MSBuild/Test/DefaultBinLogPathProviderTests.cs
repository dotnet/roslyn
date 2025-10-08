// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;
using static Microsoft.CodeAnalysis.MSBuild.MSBuildProjectLoader;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

public sealed class DefaultBinLogPathProviderTests
{
    private const string LogDirectory = "./logs";
    private const string LogFileName = "mylog";
    private const string LogExtension = ".mylog";

    [Fact]
    public void DefaultBinLogPathProvider_ProducesUniquePaths()
    {
        var logPath = Path.Combine(LogDirectory, LogFileName + LogExtension);
        var provider = new DefaultBinLogPathProvider(logPath);

        var newLogPaths = Enumerable.Range(0, 10)
            .Select(_ => provider.GetNewLogPath())
            .ToImmutableHashSet();
        Assert.Equal(10, newLogPaths.Count);

        foreach (var newLogPath in newLogPaths)
        {
            var newLogDirectory = Path.GetDirectoryName(newLogPath);
            var newLogFileName = Path.GetFileNameWithoutExtension(newLogPath);
            var newLogExtension = Path.GetExtension(newLogPath);

            Assert.Equal(LogDirectory, newLogDirectory);
            Assert.StartsWith(LogFileName, newLogFileName);
            Assert.Equal(LogExtension, newLogExtension);
        }
    }

    [Fact]
    public void DefaultBinLogPathProvider_UsesDefaultExtension()
    {
        var logPath = Path.Combine(LogDirectory, LogFileName);
        var provider = new DefaultBinLogPathProvider(logPath);

        var newLogPaths = Enumerable.Range(0, 10)
            .Select(_ => provider.GetNewLogPath())
            .ToImmutableHashSet();
        Assert.Equal(10, newLogPaths.Count);

        foreach (var newLogPath in newLogPaths)
        {
            var newLogDirectory = Path.GetDirectoryName(newLogPath);
            var newLogFileName = Path.GetFileNameWithoutExtension(newLogPath);
            var newLogExtension = Path.GetExtension(newLogPath);

            Assert.Equal(LogDirectory, newLogDirectory);
            Assert.StartsWith(LogFileName, newLogFileName);
            Assert.Equal(DefaultBinLogPathProvider.DefaultExtension, newLogExtension);
        }
    }

    [Fact]
    public void DefaultBinLogPathProvider_UsesDefaultFileName()
    {
        var provider = new DefaultBinLogPathProvider(LogDirectory + Path.DirectorySeparatorChar);

        var newLogPaths = Enumerable.Range(0, 10)
            .Select(_ => provider.GetNewLogPath())
            .ToImmutableHashSet();
        Assert.Equal(10, newLogPaths.Count);

        foreach (var newLogPath in newLogPaths)
        {
            var newLogDirectory = Path.GetDirectoryName(newLogPath);
            var newLogFileName = Path.GetFileNameWithoutExtension(newLogPath);
            var newLogExtension = Path.GetExtension(newLogPath);

            Assert.Equal(LogDirectory, newLogDirectory);
            Assert.StartsWith(DefaultBinLogPathProvider.DefaultFileName, newLogFileName);
            Assert.Equal(DefaultBinLogPathProvider.DefaultExtension, newLogExtension);
        }
    }
}
