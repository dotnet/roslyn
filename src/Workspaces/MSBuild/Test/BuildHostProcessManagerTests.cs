// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

using BuildHostProcessKind = BuildHostProcessManager.BuildHostProcessKind;

public sealed class BuildHostProcessManagerTests
{
    [Fact]
    public void ProcessStartInfo_ForNetCore_RollsForwardToLatestPreview()
    {
        var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(BuildHostProcessKind.NetCore, pipeName: "", dotnetPath: null);

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
        var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(BuildHostProcessKind.NetCore, pipeName: "", dotnetPath: null);

        Assert.StartsWith("dotnet", processStartInfo.FileName);
    }

    [Fact]
    public void ProcessStartInfo_ForMono_LaunchesMono()
    {
        var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(BuildHostProcessKind.Mono, pipeName: "", dotnetPath: null);

        Assert.Equal("mono", processStartInfo.FileName);
    }

    [Fact]
    public void ProcessStartInfo_ForNetFramework_LaunchesBuildHost()
    {
        var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(BuildHostProcessKind.NetFramework, pipeName: "", dotnetPath: null);

        Assert.EndsWith("Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.exe", processStartInfo.FileName);
    }

    [Theory]
    [InlineData(BuildHostProcessKind.NetFramework)]
    [InlineData(BuildHostProcessKind.NetCore)]
    [InlineData(BuildHostProcessKind.Mono)]
    internal void ProcessStartInfo_PassesPipeName(BuildHostProcessKind buildHostKind)
    {
        const string PipeName = "TestPipe";

        var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(buildHostKind, PipeName, dotnetPath: null);

        var args = processStartInfo.Arguments.Split(' ');
        Assert.True(args.Length >= 2, $"Expected at least 2 args: '{processStartInfo.Arguments}'");

        Assert.Equal(PipeName, args[^2]);
        Assert.Equal(System.Globalization.CultureInfo.CurrentUICulture.Name, args[^1]);
    }

    [Theory]
    [InlineData(BuildHostProcessKind.NetFramework)]
    [InlineData(BuildHostProcessKind.NetCore)]
    [InlineData(BuildHostProcessKind.Mono)]
    [UseCulture("de-DE", "de-DE")]
    internal void ProcessStartInfo_PassesLocale(BuildHostProcessKind buildHostKind)
    {
        const string PipeName = "TestPipe";

        var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(buildHostKind, PipeName, dotnetPath: null);

        var args = processStartInfo.Arguments.Split(' ');
        Assert.True(args.Length >= 2, $"Expected at least 2 args: '{processStartInfo.Arguments}'");

        Assert.Equal(PipeName, args[^2]);
        Assert.Equal("de-DE", args[^1]);
    }
}
