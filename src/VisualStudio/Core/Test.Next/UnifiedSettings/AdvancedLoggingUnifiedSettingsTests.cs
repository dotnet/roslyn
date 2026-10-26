// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UnifiedSettings;

public sealed class AdvancedLoggingUnifiedSettingsTests
{
    private const string Moniker = "languages.dotnet.advanced.enableFileLoggingForDiagnostics";

    [Fact]
    public void LoggingUsesUsxStorageWithFullRegistryMigration()
    {
        var option = VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics;
        Assert.False((bool)option.Definition.DefaultValue!);
        var storage = Assert.IsType<VisualStudioOptionStorage.UnifiedSettingsManagerStorage>(VisualStudioOptionStorage.Storages[option.Definition.ConfigName]);
        Assert.Equal(Moniker, storage.Key);
        using var live = Load("roslynSettings.registration.json");
        var native = live.RootElement.GetProperty("properties").GetProperty(Moniker);
        Assert.Equal("boolean", native.GetProperty("type").GetString());
        Assert.False(native.GetProperty("default").GetBoolean());
        Assert.False(native.GetProperty("transientScopesSupported").GetBoolean());
        Assert.Equal(new[] { "languages.csharp.advanced.analysis", "languages.basic.advanced.analysis" },
            native.GetProperty("placements").EnumerateArray().Select(placement => placement.GetProperty("category").GetString()));
        var rule = native.GetProperty("migration").GetProperty("custom");
        Assert.Equal("full", rule.GetProperty("mode").GetString());
        var input = Assert.Single(rule.GetProperty("inputs").EnumerateArray());
        Assert.Equal("VsUserSettingsRegistry", input.GetProperty("store").GetString());
        Assert.Equal(@"Roslyn\Internal\Diagnostics\EnableFileLoggingForDiagnostics", input.GetProperty("path").GetString());
        var maps = rule.GetProperty("map").EnumerateArray().ToArray();
        Assert.Equal(4, maps.Length);
        Assert.Equal("@default", maps[0].GetProperty("result").GetString());
        Assert.Equal(0, maps[0].GetProperty("matches")[0].GetInt32());
        Assert.True(maps[1].GetProperty("result").GetBoolean());
        Assert.Equal(1, maps[1].GetProperty("matches")[0].GetInt32());
        Assert.False(maps[2].GetProperty("result").GetBoolean());
        Assert.Equal(0, maps[2].GetProperty("matches")[0].GetInt32());
        Assert.Equal("@default", maps[^1].GetProperty("result").GetString());
        Assert.Equal("@any", maps[^1].GetProperty("matches")[0].GetString());
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1L)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void LoggingMigrationEnablesNumericOneAndDefaultsOtherIntegersToFalse(long legacy)
    {
        using var live = Load("roslynSettings.registration.json");
        var maps = live.RootElement.GetProperty("properties").GetProperty(Moniker).GetProperty("migration").GetProperty("custom").GetProperty("map");
        var firstMatch = maps.EnumerateArray().First(map => map.GetProperty("matches")[0] is var input
            && (input.ValueKind == JsonValueKind.String || input.GetInt64() == legacy));
        var output = firstMatch.GetProperty("result");
        var enabled = output.ValueKind == JsonValueKind.True;
        Assert.Equal(legacy == 1, enabled);
    }

    [Fact]
    public void LoggingMigrationWritesCanonicalIntegersForCheckboxChangesAndReset()
    {
        using var live = Load("roslynSettings.registration.json");
        var maps = live.RootElement.GetProperty("properties").GetProperty(Moniker).GetProperty("migration").GetProperty("custom").GetProperty("map").EnumerateArray().ToArray();
        foreach (var value in new[] { true, false })
        {
            var reverse = maps.First(map => map.GetProperty("result") is var result
                && result.ValueKind is JsonValueKind.True or JsonValueKind.False && result.GetBoolean() == value);
            Assert.Equal(value ? 1 : 0, Assert.Single(reverse.GetProperty("matches").EnumerateArray()).GetInt32());
        }

        var reset = maps.First(map => map.GetProperty("result").ValueKind == JsonValueKind.String
            && map.GetProperty("result").GetString() == "@default");
        Assert.Equal(0, Assert.Single(reset.GetProperty("matches").EnumerateArray()).GetInt32());
    }

    private static JsonDocument Load(string manifest)
    {
        using var stream = typeof(AdvancedLoggingUnifiedSettingsTests).Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests." + manifest);
        Assert.NotNull(stream);
        return JsonDocument.Parse(stream!, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
    }
}
