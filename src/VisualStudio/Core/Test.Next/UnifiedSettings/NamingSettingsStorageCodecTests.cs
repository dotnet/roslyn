// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsStorageCodecTests
{
    [Theory]
    [InlineData("csharpNamingSettings.registration.json", "languages.csharp.editorConfig.naming.")]
    [InlineData("visualBasicNamingSettings.registration.json", "languages.basic.codeStyle.naming.")]
    public void ReorderedDefaultObjectsSerializeExactlyLikeRegisteredDefaults(string file, string prefix)
    {
        using var manifest = NamingSettingsManifestTests.Load(file);
        var graph = NamingSettingsGraph.Default;
        var arrays = new[]
        {
            ("symbolSpecifications", NamingSettingsStorageCodec.EncodeSpecifications(Reorder(graph.SymbolSpecifications))),
            ("namingStyles", NamingSettingsStorageCodec.EncodeStyles(Reorder(graph.NamingStyles))),
            ("rules", NamingSettingsStorageCodec.EncodeRules(Reorder(graph.Rules))),
        };
        foreach (var (name, items) in arrays)
        {
            var defaults = NamingSettingsManifestTests.ReadDefaults(manifest.RootElement.GetProperty("properties").GetProperty(prefix + name));
            Assert.Equal(JsonSerializer.Serialize(defaults), JsonSerializer.Serialize(items));
        }

        static ImmutableArray<IReadOnlyDictionary<string, object>> Reorder(ImmutableArray<IReadOnlyDictionary<string, object>> items)
            => items.Select(item => (IReadOnlyDictionary<string, object>)item.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value)).ToImmutableArray();
    }

    [Fact]
    public void OrderingPreservesUnknownMetadataAndDoesNotMutateInput()
    {
        var style = NamingSettingsGraph.Default.NamingStyles[0].Reverse().ToDictionary(pair => pair.Key, pair => pair.Value);
        style["additionalMetadata"] = "preserved";
        var keys = style.Keys.ToArray();

        var encoded = NamingSettingsStorageCodec.EncodeStyles([style]);

        Assert.Equal("preserved", encoded[0]["additionalMetadata"]);
        Assert.Equal(keys, style.Keys);
        Assert.NotSame(style, encoded[0]);
    }

    [Fact]
    public void NativeScalarTokenListsRoundTripWithoutMutatingTheGraph()
    {
        var graph = NamingSettingsGraph.Default;
        var encoded = NamingSettingsStorageCodec.EncodeSpecifications(graph.SymbolSpecifications);
        Assert.All(encoded, item => Assert.IsType<string>(item["symbolKinds"]));
        Assert.All(graph.SymbolSpecifications, item => Assert.IsType<string[]>(item["symbolKinds"]));
        var decoded = new NamingSettingsGraph(NamingSettingsStorageCodec.DecodeSpecifications(encoded), graph.NamingStyles, graph.Rules);
        Assert.True(graph.ConfigurationEquals(decoded));
    }

    [Theory]
    [InlineData("class,class")]
    [InlineData("class,")]
    [InlineData("unknown")]
    public void MalformedTokenListsCannotProduceSuccessfulPreferences(string tokens)
    {
        var graph = NamingSettingsGraph.Default;
        var encoded = NamingSettingsStorageCodec.EncodeSpecifications(graph.SymbolSpecifications);
        var first = encoded[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        first["symbolKinds"] = tokens;
        var decoded = new NamingSettingsGraph(NamingSettingsStorageCodec.DecodeSpecifications(encoded.SetItem(0, first)), graph.NamingStyles, graph.Rules);
        Assert.Throws<InvalidDataException>(() => decoded.ToPreferences());
    }

    [Fact]
    public void MissingCompatibilityIdRemainsMissingAcrossStorage()
    {
        var graph = NamingSettingsGraph.FromPreferences(NamingSettingsGraph.Default.ToPreferences());
        var first = graph.SymbolSpecifications[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        first.Remove("legacyId");
        var specifications = graph.SymbolSpecifications.SetItem(0, first);
        var decoded = NamingSettingsStorageCodec.DecodeSpecifications(NamingSettingsStorageCodec.EncodeSpecifications(specifications));
        Assert.DoesNotContain("legacyId", decoded[0].Keys);
        Assert.Equal(new NamingSettingsGraph(specifications, graph.NamingStyles, graph.Rules).ToPreferences(), new NamingSettingsGraph(decoded, graph.NamingStyles, graph.Rules).ToPreferences());
    }
}
