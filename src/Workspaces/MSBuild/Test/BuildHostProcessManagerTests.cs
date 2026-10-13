// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/85194")]
    [InlineData("DOTNET_ROOT")]
    [InlineData("DOTNET_ROOT(x86)")]
    [InlineData("DOTNET_ROOT_X64")]
    [InlineData("DOTNET_ROOT_X86")]
    [InlineData("DOTNET_ROOT_ARM64")]
    public void ProcessStartInfo_ForNetCore_ExplicitDotNetPathOverridesInheritedRoots(string variableName)
    {
        var originalValue = Environment.GetEnvironmentVariable(variableName);
        try
        {
            Environment.SetEnvironmentVariable(variableName, Path.GetFullPath("inherited-dotnet"));

            var dotnetDirectory = Path.GetFullPath("custom-dotnet");
            var dotnetPath = Path.Combine(dotnetDirectory, "dotnet");
            var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(BuildHostProcessKind.NetCore, pipeName: "", dotnetPath);

            Assert.Equal(dotnetPath, processStartInfo.FileName);
            Assert.Equal(dotnetDirectory, processStartInfo.Environment["DOTNET_ROOT"]);
            if (variableName != "DOTNET_ROOT")
                Assert.DoesNotContain(variableName, processStartInfo.Environment.Keys);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/85194")]
    [InlineData("DOTNET_ROOT")]
    [InlineData("DOTNET_ROOT(x86)")]
    [InlineData("DOTNET_ROOT_X64")]
    [InlineData("DOTNET_ROOT_X86")]
    [InlineData("DOTNET_ROOT_ARM64")]
    public void ProcessStartInfo_ForNetCore_DefaultDotNetPathPreservesRoot(string variableName)
    {
        var originalValue = Environment.GetEnvironmentVariable(variableName);
        try
        {
            var inheritedRoot = Path.GetFullPath("inherited-dotnet");
            Environment.SetEnvironmentVariable(variableName, inheritedRoot);

            var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(BuildHostProcessKind.NetCore, pipeName: "", dotnetPath: null);

            Assert.Equal(inheritedRoot, processStartInfo.Environment[variableName]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71019")]
    public void ProcessStartInfo_ForNetCore_ExplicitDotNetPathSetsHostPath(bool hasInheritedHostPath)
    {
        var originalValue = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        try
        {
            var inheritedHostPath = hasInheritedHostPath ? Path.Combine(Path.GetFullPath("inherited-dotnet"), "dotnet") : null;
            Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", inheritedHostPath);

            var dotnetPath = Path.Combine(Path.GetFullPath("custom-dotnet"), "dotnet");
            var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(BuildHostProcessKind.NetCore, pipeName: "", dotnetPath);

            Assert.Equal(dotnetPath, processStartInfo.FileName);
            Assert.Equal(dotnetPath, processStartInfo.Environment["DOTNET_HOST_PATH"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", originalValue);
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71019")]
    public void ProcessStartInfo_ForNetCore_DefaultDotNetPathPreservesHostPath(bool hasInheritedHostPath)
    {
        var originalValue = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        try
        {
            var inheritedHostPath = hasInheritedHostPath ? Path.Combine(Path.GetFullPath("inherited-dotnet"), "dotnet") : null;
            Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", inheritedHostPath);

            var processStartInfo = BuildHostProcessManager.CreateBuildHostStartInfo(BuildHostProcessKind.NetCore, pipeName: "", dotnetPath: null);

            Assert.Equal(hasInheritedHostPath, processStartInfo.Environment.TryGetValue("DOTNET_HOST_PATH", out var hostPath));
            Assert.Equal(inheritedHostPath, hostPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_HOST_PATH", originalValue);
        }
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
#if NET
        var args = processStartInfo.ArgumentList;
#else
        var args = processStartInfo.Arguments.Split(' ');
#endif
        Assert.True(args.Count() >= 2, $"Expected at least 2 args: '{string.Join(",", args)}'");

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
#if NET
        var args = processStartInfo.ArgumentList;
#else
        var args = processStartInfo.Arguments.Split(' ');
#endif
        Assert.True(args.Count() >= 2, $"Expected at least 2 args: '{string.Join(",", args)}'");

        Assert.Equal(PipeName, args[^2]);
        Assert.Equal("de-DE", args[^1]);
    }
}
