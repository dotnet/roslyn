// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using RunTests;

namespace RunTests.UnitTests;

public sealed class ProcessTestExecutorTests
{
    [Fact]
    public void AddDotNetRootEnvironmentVariablesPreservesInheritedX86Root()
    {
        var dotnetPath = Path.Combine("dotnet", "dotnet.exe");
        var inheritedX86Root = Path.Combine("dotnet", "x86");
        var environmentVariables = new Dictionary<string, string>();

        ProcessTestExecutor.AddDotNetRootEnvironmentVariables(
            environmentVariables,
            dotnetPath,
            "x86",
            name => name == "DOTNET_ROOT_X86" ? inheritedX86Root : null);

        Assert.Equal(Path.GetDirectoryName(dotnetPath), environmentVariables["DOTNET_ROOT"]);
        Assert.DoesNotContain("DOTNET_ROOT_X86", environmentVariables);
    }

    [Fact]
    public void AddDotNetRootEnvironmentVariablesPreservesConfiguredX86Root()
    {
        var configuredX86Root = Path.Combine("custom", "x86");
        var environmentVariables = new Dictionary<string, string>
        {
            ["DOTNET_ROOT_X86"] = configuredX86Root,
        };

        ProcessTestExecutor.AddDotNetRootEnvironmentVariables(
            environmentVariables,
            Path.Combine("dotnet", "dotnet.exe"),
            "x86",
            _ => null);

        Assert.Equal(configuredX86Root, environmentVariables["DOTNET_ROOT_X86"]);
    }

    [Fact]
    public void AddDotNetRootEnvironmentVariablesUsesDotNetDirectoryWhenX86RootIsNotSet()
    {
        var dotnetPath = Path.Combine("dotnet", "dotnet.exe");
        var environmentVariables = new Dictionary<string, string>();

        ProcessTestExecutor.AddDotNetRootEnvironmentVariables(
            environmentVariables,
            dotnetPath,
            "x86",
            _ => null);

        Assert.Equal(Path.GetDirectoryName(dotnetPath), environmentVariables["DOTNET_ROOT_X86"]);
    }
}
