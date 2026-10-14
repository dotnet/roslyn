// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsGraphTests
{
    [Fact]
    public void DefaultPreferencesRoundTripWithoutChangingLegacyIdentities()
    {
        var preferences = NamingStylePreferences.Default;
        var graph = NamingSettingsGraph.FromPreferences(preferences);
        Assert.Equal(preferences, graph.ToPreferences());
        Assert.All(graph.SymbolSpecifications, item => Assert.True(NamingSettingsIdentity.IsValidId((string)item["id"])));
        Assert.All(graph.NamingStyles, item => Assert.True(NamingSettingsIdentity.IsValidId((string)item["id"])));
    }

    [Fact]
    public void EmptyPreferencesRoundTrip()
    {
        var graph = new NamingSettingsGraph([], [], []);
        var preferences = graph.ToPreferences();
        Assert.Empty(preferences.SymbolSpecifications);
        Assert.Empty(preferences.NamingStyles);
        Assert.Empty(preferences.Rules.NamingRules);
    }

    [Theory]
    [InlineData("silent", ReportDiagnostic.Hidden)]
    [InlineData("suggestion", ReportDiagnostic.Info)]
    [InlineData("warning", ReportDiagnostic.Warn)]
    [InlineData("error", ReportDiagnostic.Error)]
    public void SeveritiesRoundTripThroughLegacyStorage(string token, ReportDiagnostic expected)
    {
        var original = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var rule = new Dictionary<string, object>(original.Rules[0].ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            ["severity"] = token,
        };
        var graph = new NamingSettingsGraph(original.SymbolSpecifications, original.NamingStyles, original.Rules.SetItem(0, rule));
        var preferences = graph.ToPreferences();
        var restored = NamingStylePreferences.FromXElement(preferences.CreateXElement());

        Assert.Equal(expected, restored.Rules.NamingRules[0].EnforcementLevel);
        Assert.Equal(preferences, restored);
        Assert.Equal(token, NamingSettingsGraph.FromPreferences(restored, graph).Rules[0]["severity"]);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("default")]
    public void SeveritiesNotRepresentableInLegacyStorageAreRejected(string token)
    {
        var original = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var rule = new Dictionary<string, object>(original.Rules[0].ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            ["severity"] = token,
        };
        var graph = new NamingSettingsGraph(original.SymbolSpecifications, original.NamingStyles, original.Rules.SetItem(0, rule));

        Assert.Throws<InvalidDataException>(() => graph.ToPreferences());
    }

    [Fact]
    public void RemovingLastRuleDoesNotSilentlyDiscardDefinitions()
    {
        var original = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var graph = new NamingSettingsGraph(original.SymbolSpecifications, original.NamingStyles, []);

        var restored = NamingSettingsGraph.FromPreferences(graph.ToPreferences(), graph);
        Assert.Empty(restored.Rules);
        Assert.Equal(original.SymbolSpecifications.Length, restored.SymbolSpecifications.Length);
        Assert.Equal(original.NamingStyles.Length, restored.NamingStyles.Length);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DefinitionsWithoutRulesRoundTrip(bool includeSpecifications, bool includeStyles)
    {
        var defaults = NamingSettingsGraph.Default;
        var graph = new NamingSettingsGraph(
            includeSpecifications ? defaults.SymbolSpecifications : [],
            includeStyles ? defaults.NamingStyles : [], []);

        var restored = NamingSettingsGraph.FromPreferences(graph.ToPreferences(), graph);

        Assert.True(graph.ConfigurationEquals(restored));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandAuthoredItemsDoNotRequireGuidMetadata(bool includeEmptyCompatibilityIds)
    {
        var original = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var graph = new NamingSettingsGraph(
            original.SymbolSpecifications.Select(PrepareItem).ToImmutableArray(),
            original.NamingStyles.Select(PrepareItem).ToImmutableArray(),
            original.Rules);

        var first = graph.ToPreferences();
        var second = graph.ToPreferences();
        var restored = NamingSettingsGraph.FromPreferences(first, graph);
        Assert.Equal(original.ToPreferences(), first);
        Assert.Equal(first, second);
        Assert.Equal(original.NamingStyles.Select(item => item["id"]), restored.NamingStyles.Select(item => item["id"]));
        Assert.All(restored.SymbolSpecifications, item => Assert.False(item.ContainsKey("legacyId")));
        Assert.All(restored.NamingStyles, item => Assert.False(item.ContainsKey("legacyId")));

        IReadOnlyDictionary<string, object> PrepareItem(IReadOnlyDictionary<string, object> item)
        {
            var prepared = item.Where(pair => pair.Key != "legacyId").ToDictionary(pair => pair.Key, pair => pair.Value);
            if (includeEmptyCompatibilityIds)
                prepared["legacyId"] = "";
            return prepared;
        }
    }

    [Fact]
    public void BuiltInIdsAreReservedBeforeCustomDisplayNames()
    {
        var preferences = NamingStylePreferences.Default;
        var template = preferences.SymbolSpecifications[0];
        var custom = new SymbolSpecification(Guid.NewGuid(), "interface", template.ApplicableSymbolKindList, template.ApplicableAccessibilityList, template.RequiredModifierList);
        var extended = new NamingStylePreferences(preferences.SymbolSpecifications.Insert(0, custom), preferences.NamingStyles, preferences.Rules.NamingRules);
        var graph = NamingSettingsGraph.FromPreferences(extended);
        Assert.Equal("interface_2", graph.SymbolSpecifications[0]["id"]);
        Assert.Equal("interface", graph.SymbolSpecifications[2]["id"]);
    }

    [Fact]
    public void RenamingADisplayNameDoesNotChangeReadableIdentity()
    {
        var original = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var first = new Dictionary<string, object>(original.NamingStyles[0].ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            ["name"] = "Renamed display name",
        };
        var renamed = new NamingSettingsGraph(original.SymbolSpecifications, original.NamingStyles.SetItem(0, first), original.Rules);
        var restored = NamingSettingsGraph.FromPreferences(renamed.ToPreferences(), renamed);
        Assert.Equal(original.NamingStyles[0]["id"], restored.NamingStyles[0]["id"]);
        Assert.Equal("Renamed display name", restored.NamingStyles[0]["name"]);
        Assert.Equal(original.ToPreferences().NamingStyles[0].ID, restored.ToPreferences().NamingStyles[0].ID);
    }

    [Fact]
    public void RuleOrderAndUnreferencedDefinitionsArePreserved()
    {
        var original = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var reversed = new NamingSettingsGraph(original.SymbolSpecifications, original.NamingStyles, original.Rules.Reverse().ToImmutableArray());
        var restored = NamingSettingsGraph.FromPreferences(reversed.ToPreferences(), reversed);
        Assert.Equal(original.SymbolSpecifications.Length, restored.SymbolSpecifications.Length);
        Assert.Equal(original.NamingStyles.Length, restored.NamingStyles.Length);
        Assert.Equal(reversed.Rules.Select(item => item["symbolSpecificationId"]), restored.Rules.Select(item => item["symbolSpecificationId"]));
    }

    [Fact]
    public void DuplicateReadableIdsAreRejected()
    {
        var graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var duplicate = new Dictionary<string, object>(graph.SymbolSpecifications[1].ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            ["id"] = graph.SymbolSpecifications[0]["id"],
        };
        var invalid = new NamingSettingsGraph(graph.SymbolSpecifications.SetItem(1, duplicate), graph.NamingStyles, graph.Rules);
        Assert.Throws<InvalidDataException>(() => invalid.ToPreferences());
    }

    [Fact]
    public void BrokenReferencesAreRejected()
    {
        var graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var broken = new Dictionary<string, object>(graph.Rules[0].ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            ["namingStyleId"] = "unknown_style",
        };
        var invalid = new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles, graph.Rules.SetItem(0, broken));
        Assert.Throws<InvalidDataException>(() => invalid.ToPreferences());
    }

    [Theory]
    [InlineData("capitalization", "not_a_capitalization")]
    [InlineData("legacyId", "not_a_guid")]
    [InlineData("legacyId", "00000000-0000-0000-0000-000000000000")]
    [InlineData("legacyId", true)]
    [InlineData("legacyId", 42)]
    public void InvalidStylePropertiesAreRejected(string property, object value)
    {
        var graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var broken = new Dictionary<string, object>(graph.NamingStyles[0].ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            [property] = value,
        };
        var invalid = new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles.SetItem(0, broken), graph.Rules);
        Assert.Throws<InvalidDataException>(() => invalid.ToPreferences());
    }
}
