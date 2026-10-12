// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    public void LoggingHasNativeUiWithTypeAwareLegacyStorageSynchronization()
    {
        var option = VisualStudioLoggingOptionsStorage.EnableFileLoggingForDiagnostics;
        Assert.False((bool)option.Definition.DefaultValue!);
        var storage = Assert.IsType<VisualStudioOptionStorage.LocalUserProfileStorage>(VisualStudioOptionStorage.Storages[option.Definition.ConfigName]);
        Assert.Equal(@"Roslyn\Internal\Diagnostics", storage.Path);
        Assert.Equal("EnableFileLoggingForDiagnostics", storage.Key);
        using var live = Load("roslynSettings.registration.json");
        var native = live.RootElement.GetProperty("properties").GetProperty(Moniker);
        Assert.Equal("boolean", native.GetProperty("type").GetString());
        Assert.False(native.GetProperty("default").GetBoolean());
        Assert.False(native.TryGetProperty("migration", out _));
        Assert.Equal(new[] { "languages.csharp.advanced.analysis", "languages.basic.advanced.analysis" },
            native.GetProperty("placements").EnumerateArray().Select(placement => placement.GetProperty("category").GetString()));
        using var draft = Load("roslynAdvancedSettings.registration.draft.json");
        var setting = draft.RootElement.GetProperty("properties").GetProperty(Moniker);
        Assert.Equal("boolean", setting.GetProperty("type").GetString());
        Assert.False(setting.GetProperty("default").GetBoolean());
        var placements = setting.GetProperty("placements").EnumerateArray().Select(placement => placement.GetProperty("category").GetString());
        Assert.Equal(new[] { "languages.csharp.advanced.analysis", "languages.basic.advanced.analysis" }, placements);
        var rule = setting.GetProperty("migration").GetProperty("custom");
        Assert.Equal("full", rule.GetProperty("mode").GetString());
        var input = Assert.Single(rule.GetProperty("inputs").EnumerateArray());
        Assert.Equal("VsUserSettingsRegistry", input.GetProperty("store").GetString());
        Assert.Equal(storage.Path + "\\" + storage.Key, input.GetProperty("path").GetString());
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
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void LoggingDraftPreservesTheDwordCodecIncludingUnknownIntegers(int legacy)
    {
        using var draft = Load("roslynAdvancedSettings.registration.draft.json");
        var maps = draft.RootElement.GetProperty("properties").GetProperty(Moniker).GetProperty("migration").GetProperty("custom").GetProperty("map");
        var firstMatch = maps.EnumerateArray().First(map => map.GetProperty("matches")[0] is var input
            && (input.ValueKind == JsonValueKind.String || input.GetInt32() == legacy));
        var output = firstMatch.GetProperty("result");
        var enabled = output.ValueKind == JsonValueKind.True;
        Assert.Equal(((object)legacy).Equals(1), enabled);
    }

    [Fact]
    public void NumericManifestLiteralDoesNotRepresentTheLegacyDwordQwordDistinction()
    {
        object dword = 1;
        object qword = 1L;
        Assert.True(dword.Equals(1));
        Assert.False(qword.Equals(1));
        // IntegerStoreValue.Equals also equates these via decimal conversion. A public numeric
        // match cannot request DWORD-only equality; this serialization check is not a native migration test.
        Assert.Equal(Convert.ToDecimal(dword), Convert.ToDecimal(qword));
        Assert.Equal(JsonSerializer.Serialize(dword), JsonSerializer.Serialize(qword));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(2, false)]
    [InlineData(1L, false)]
    [InlineData(0L, false)]
    [InlineData(1.0, false)]
    [InlineData("1", false)]
    [InlineData("true", false)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ProductionLoggingRegistryCodecPreservesBoxedValueType(object value, bool expected)
        => Assert.Equal(expected, LocalUserRegistryOptionPersister.ReadBooleanRegistryValue(value));

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void ProductionLoggingRegistryCodecRoundTripsCanonicalDefaultsAndWrites(bool value, int expected)
    {
        var storedValue = LocalUserRegistryOptionPersister.WriteBooleanRegistryValue(value);
        Assert.Equal(expected, storedValue);
        Assert.Equal(value, LocalUserRegistryOptionPersister.ReadBooleanRegistryValue(storedValue));
    }

    private static JsonDocument Load(string manifest)
    {
        using var stream = typeof(AdvancedLoggingUnifiedSettingsTests).Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests." + manifest);
        Assert.NotNull(stream);
        return JsonDocument.Parse(stream!, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
    }
}
