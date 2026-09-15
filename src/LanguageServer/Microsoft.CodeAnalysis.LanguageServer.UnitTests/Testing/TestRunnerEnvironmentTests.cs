// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Testing;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Testing;

public sealed class TestRunnerEnvironmentTests
{
    [Fact]
    public void CreateEnvironmentVariablesClearsDotnetRootVariants()
    {
        var environmentVariables = TestRunnerEnvironment.CreateEnvironmentVariables(
            ["DOTNET_ROOT", "DOTNET_ROOT_X64", "dotnet_root_arm64", "UNRELATED"],
            dotnetRootUser: "/user/dotnet",
            dotnetRoot: "/inherited/dotnet");

        Assert.Equal("/user/dotnet", environmentVariables["DOTNET_ROOT"]);
        Assert.Null(environmentVariables["DOTNET_ROOT_X64"]);
        Assert.Null(environmentVariables["dotnet_root_arm64"]);
        Assert.DoesNotContain("UNRELATED", environmentVariables);
    }

    [Fact]
    public void CreateEnvironmentVariablesClearsInheritedDiagnosticPorts()
    {
        var environmentVariables = TestRunnerEnvironment.CreateEnvironmentVariables(
            ["DOTNET_DiagnosticPorts", "DOTNET_DefaultDiagnosticPortSuspend"],
            dotnetRootUser: null,
            dotnetRoot: null);

        Assert.Null(environmentVariables["DOTNET_DiagnosticPorts"]);
        Assert.Null(environmentVariables["DOTNET_DefaultDiagnosticPortSuspend"]);
    }

    [Fact]
    public void CreateEnvironmentVariablesUsesInheritedDotnetRootWithoutUserValue()
    {
        var environmentVariables = TestRunnerEnvironment.CreateEnvironmentVariables(
            ["DOTNET_ROOT", "DOTNET_ROOT_X64"],
            dotnetRootUser: null,
            dotnetRoot: "/inherited/dotnet");

        Assert.Equal("/inherited/dotnet", environmentVariables["DOTNET_ROOT"]);
        Assert.Null(environmentVariables["DOTNET_ROOT_X64"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EMPTY")]
    public void CreateEnvironmentVariablesClearsExplicitlyEmptyUserRoot(string dotnetRootUser)
    {
        var environmentVariables = TestRunnerEnvironment.CreateEnvironmentVariables(
            ["DOTNET_ROOT_X64"],
            dotnetRootUser,
            dotnetRoot: "/inherited/dotnet");

        Assert.Equal(string.Empty, environmentVariables["DOTNET_ROOT"]);
    }

    [Fact]
    public void CreateEnvironmentVariablesClearsDotnetRootWithoutAnyValue()
    {
        var environmentVariables = TestRunnerEnvironment.CreateEnvironmentVariables(
            ["DOTNET_ROOT_X64"],
            dotnetRootUser: null,
            dotnetRoot: null);

        Assert.Equal(string.Empty, environmentVariables["DOTNET_ROOT"]);
    }
}
