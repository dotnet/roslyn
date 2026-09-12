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
            dotnetRootUser: "/user/dotnet");

        Assert.Equal("/user/dotnet", environmentVariables["DOTNET_ROOT"]);
        Assert.Null(environmentVariables["DOTNET_ROOT_X64"]);
        Assert.Null(environmentVariables["dotnet_root_arm64"]);
        Assert.DoesNotContain("UNRELATED", environmentVariables);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EMPTY")]
    public void CreateEnvironmentVariablesClearsDotnetRootWithoutUserValue(string? dotnetRootUser)
    {
        var environmentVariables = TestRunnerEnvironment.CreateEnvironmentVariables(
            ["DOTNET_ROOT_X64"],
            dotnetRootUser);

        Assert.Equal(string.Empty, environmentVariables["DOTNET_ROOT"]);
    }
}
