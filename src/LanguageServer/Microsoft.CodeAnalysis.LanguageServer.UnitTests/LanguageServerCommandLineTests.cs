// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LanguageServerCommandLineTests
{
    private static async Task<ServerConfiguration?> ParseAsync(params string[] args)
    {
        ServerConfiguration? configuration = null;
        var command = LanguageServerCommandLine.CreateCommand(async (c, _) =>
        {
            configuration = c;
        });

        var exitCode = await command.Parse(args).InvokeAsync(cancellationToken: CancellationToken.None);
        return configuration;
    }

    [Fact]
    public async Task AutoLoadProjects_NotSpecified_IsNull()
    {
        var configuration = await ParseAsync();
        Assert.NotNull(configuration);
        Assert.Null(configuration.AutoLoadProjects);
    }

    [Fact]
    public async Task AutoLoadProjects_SpecifiedWithoutValue_UsesNonZeroDefault()
    {
        var configuration = await ParseAsync("--autoLoadProjects");

        Assert.NotNull(configuration);
        Assert.True(configuration.AutoLoadProjects > 0,
            $"Expected default --autoLoadProjects value to be greater than zero, but was {configuration.AutoLoadProjects}.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(1000)]
    public async Task AutoLoadProjects_SpecifiedWithValue_UsesProvidedValue(int value)
    {
        var configuration = await ParseAsync("--autoLoadProjects", value.ToString());

        Assert.NotNull(configuration);
        Assert.Equal(value, configuration.AutoLoadProjects);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("notanumber")]
    public async Task AutoLoadProjects_SpecifiedWithNonPositiveValue_FailsParse(string value)
    {
        var configuration = await ParseAsync("--autoLoadProjects", value);
        Assert.Null(configuration);
    }
}
