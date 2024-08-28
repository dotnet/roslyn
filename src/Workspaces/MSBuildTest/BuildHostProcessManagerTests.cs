// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

using BuildHostProcessKind = BuildHostProcessManager.BuildHostProcessKind;

public class BuildHostProcessManagerTests
{
    [Fact]
    public void ProcessStartInfo_ForNetCore_RollsForwardToLatestPreview()
    {
        var processStartInfo = new BuildHostProcessManager()
            .CreateBuildHostStartInfo(BuildHostProcessKind.NetCore);

#if NET
        var rollForwardIndex = processStartInfo.ArgumentList.IndexOf("--roll-forward");
        var latestMajorIndex = processStartInfo.ArgumentList.IndexOf("LatestMajor");
        Assert.True(rollForwardIndex >= 0);
        Assert.True(latestMajorIndex >= 0);
        Assert.Equal(latestMajorIndex, rollForwardIndex + 1);
#else
        Assert.Contains("--roll-forward LatestMajor", processStartInfo.Arguments);
#endif
    }

    [Fact]
    public void ProcessStartInfo_ForNetCore_LaunchesDotNetCLI()
    {
        var processStartInfo = new BuildHostProcessManager()
            .CreateBuildHostStartInfo(BuildHostProcessKind.NetCore);

        Assert.StartsWith("dotnet", processStartInfo.FileName);
    }

    [Fact]
    public void ProcessStartInfo_ForMono_LaunchesMono()
    {
        var processStartInfo = new BuildHostProcessManager()
            .CreateBuildHostStartInfo(BuildHostProcessKind.Mono);

        Assert.Equal("mono", processStartInfo.FileName);
    }

    [Fact]
    public void ProcessStartInfo_ForNetFramework_LaunchesBuildHost()
    {
        var processStartInfo = new BuildHostProcessManager()
            .CreateBuildHostStartInfo(BuildHostProcessKind.NetFramework);

        Assert.EndsWith("Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.exe", processStartInfo.FileName);
    }

    [Theory]
    [InlineData(BuildHostProcessKind.NetFramework)]
    [InlineData(BuildHostProcessKind.NetCore)]
    [InlineData(BuildHostProcessKind.Mono)]
    internal void ProcessStartInfo_PassesBinLogPath(BuildHostProcessKind buildHostKind)
    {
        const string BinaryLogPath = "test.binlog";

        var processStartInfo = new BuildHostProcessManager(binaryLogPath: BinaryLogPath)
            .CreateBuildHostStartInfo(buildHostKind);

#if NET
        var binlogIndex = processStartInfo.ArgumentList.IndexOf("--binlog");
        Assert.True(binlogIndex >= 0);
        Assert.Equal(BinaryLogPath, processStartInfo.ArgumentList[binlogIndex + 1]);
#else
        Assert.Contains($"--binlog {BinaryLogPath}", processStartInfo.Arguments);
#endif
    }

    [Theory]
    [InlineData(BuildHostProcessKind.NetFramework)]
    [InlineData(BuildHostProcessKind.NetCore)]
    [InlineData(BuildHostProcessKind.Mono)]
    [UseCulture("de-DE", "de-DE")]
    internal void ProcessStartInfo_PassesLocale(BuildHostProcessKind buildHostKind)
    {
        const string Locale = "de-DE";

        var processStartInfo = new BuildHostProcessManager()
            .CreateBuildHostStartInfo(buildHostKind);

#if NET
        var localeIndex = processStartInfo.ArgumentList.IndexOf("--locale");
        Assert.True(localeIndex >= 0);
        Assert.Equal(Locale, processStartInfo.ArgumentList[localeIndex + 1]);
#else
        Assert.Contains($"--locale {Locale}", processStartInfo.Arguments);
#endif
    }

    [Theory]
    [InlineData(BuildHostProcessKind.NetFramework)]
    [InlineData(BuildHostProcessKind.NetCore)]
    [InlineData(BuildHostProcessKind.Mono)]
    internal void ProcessStartInfo_PassesProperties(BuildHostProcessKind buildHostKind)
    {
        var globalMSBuildProperties = new Dictionary<string, string>
        {
            ["TestKey1"] = "TestValue1",
            ["TestKey2"] = "TestValue2",
        }.ToImmutableDictionary();

        var buildHostProcessManager = new BuildHostProcessManager(globalMSBuildProperties);

        var processStartInfo = buildHostProcessManager.CreateBuildHostStartInfo(buildHostKind);

#if NET
        foreach (var kvp in globalMSBuildProperties)
        {
            var propertyArgument = GetPropertyArgument(kvp);
            var argumentIndex = processStartInfo.ArgumentList.IndexOf(propertyArgument);
            Assert.True(argumentIndex >= 0);
            Assert.Equal("--property", processStartInfo.ArgumentList[argumentIndex - 1]);
        }
#else
        foreach (var kvp in globalMSBuildProperties)
        {
            var propertyArgument = GetPropertyArgument(kvp);
            Assert.Contains($"--property {propertyArgument}", processStartInfo.Arguments);
        }
#endif

        static string GetPropertyArgument(KeyValuePair<string, string> property)
        {
            return $"{property.Key}={property.Value}";
        }
    }
}
