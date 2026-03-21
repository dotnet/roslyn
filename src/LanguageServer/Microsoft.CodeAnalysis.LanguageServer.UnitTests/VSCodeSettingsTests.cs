// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class VSCodeSettingsTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public void TryRead_ReadsDotnetDefaultSolution()
    {
        var settings = TryReadSettings("""
            {
              "dotnet.defaultSolution": "src/App.sln"
            }
            """);

        Assert.Equal("src/App.sln", settings.TryGetStringSetting(VSCodeSettings.Names.DefaultSolution));
    }

    [Fact]
    public void TryRead_AllowsJsonCommentsAndTrailingCommas()
    {
        var settings = TryReadSettings("""
            {
              // comment
              "dotnet.defaultSolution": "src/App.sln",
            }
            """);

        Assert.Equal("src/App.sln", settings.TryGetStringSetting(VSCodeSettings.Names.DefaultSolution));
    }

    [Fact]
    public void TryRead_ReadsDisableDefaultSolutionValue()
    {
        var settings = TryReadSettings("""
            {
              "dotnet.defaultSolution": "disable"
            }
            """);

        Assert.Equal("disable", settings.TryGetStringSetting(VSCodeSettings.Names.DefaultSolution));
    }

    private string WriteSettingsFile(string settingsJson)
    {
        var folder = _tempRoot.CreateDirectory();
        var settingsDirectory = folder.CreateDirectory(".vscode");
        var settingsPath = Path.Combine(settingsDirectory.Path, "settings.json");
        File.WriteAllText(settingsPath, settingsJson);
        return settingsPath;
    }

    private VSCodeSettings TryReadSettings(string settingsJson)
    {
        var settingsPath = WriteSettingsFile(settingsJson);
        Assert.True(VSCodeSettings.TryRead(settingsPath, NullLogger.Instance, out var settings));
        return settings;
    }
}
