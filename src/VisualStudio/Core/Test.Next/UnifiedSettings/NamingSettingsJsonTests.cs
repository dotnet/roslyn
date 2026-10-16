// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsJsonTests
{
    private static readonly JoinableTaskContext s_context = new();

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task LegacyImportsRekeyEveryReferenceAndRepeatedImportsKeepTheSameIds(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var imported = CreateImportedPreferences();
        var legacy = imported;
        var writes = 0;
        var refreshes = new List<NamingStylePreferences>();
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, value =>
        {
            legacy = value;
            writes++;
        }, s_context, refreshes.Add);

        await coordinator.SynchronizeFromLegacyAsync(CancellationToken.None);
        var first = manager.Reader.Graph;
        var published = first.ToPreferences();
        Assert.NotEqual(imported.SymbolSpecifications[0].ID, published.SymbolSpecifications[0].ID);
        Assert.NotEqual(imported.NamingStyles[0].ID, published.NamingStyles[0].ID);
        Assert.Equal(imported.SymbolSpecifications[0].Name, published.SymbolSpecifications[0].Name);
        Assert.Equal(imported.SymbolSpecifications[0].ApplicableSymbolKindList.Select(NamingSettingsGraph.SerializeKind),
            published.SymbolSpecifications[0].ApplicableSymbolKindList.Select(NamingSettingsGraph.SerializeKind));
        Assert.Equal(imported.NamingStyles[0].Name, published.NamingStyles[0].Name);
        Assert.Equal(imported.NamingStyles[0].Prefix, published.NamingStyles[0].Prefix);
        var rule = Assert.Single(published.Rules.NamingRules);
        Assert.Equal(published.SymbolSpecifications[0].ID, rule.SymbolSpecification.ID);
        Assert.Equal(published.NamingStyles[0].ID, rule.NamingStyle.ID);
        Assert.Equal(ReportDiagnostic.Warn, rule.EnforcementLevel);
        Assert.Equal(published, NamingStylePreferences.FromXElement(legacy.CreateXElement()));
        Assert.Equal(published, Assert.Single(refreshes));
        AssertCleanOverrides(manager);

        legacy = imported;
        await coordinator.SynchronizeFromLegacyAsync(CancellationToken.None);
        Assert.True(first.ConfigurationEquals(manager.Reader.Graph));
        Assert.Equal(2, writes);
        await coordinator.SynchronizeFromLegacyAsync(CancellationToken.None);
        await coordinator.SynchronizeFromUnifiedSettingsAsync(CancellationToken.None);
        Assert.Equal(2, writes);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task ExistingGuidMetadataIsUpgradedWithoutLosingNamesOrAdditionalMetadata(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var legacy = CreateImportedPreferences();
        var graph = NamingSettingsGraph.FromPreferences(legacy);
        var specification = graph.SymbolSpecifications[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        specification["legacyId"] = legacy.SymbolSpecifications[0].ID.ToString("D");
        var style = graph.NamingStyles[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        style["legacyId"] = legacy.NamingStyles[0].ID.ToString("D");
        style["additionalMetadata"] = "preserved";
        manager.Reader.Graph = new([specification], [style], graph.Rules);
        Assert.Equal(legacy, manager.Reader.Graph.ToPreferences());
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, value => legacy = value, s_context);

        await coordinator.SynchronizeFromUnifiedSettingsAsync(CancellationToken.None);

        AssertCleanOverrides(manager);
        Assert.Equal("preserved", manager.Reader.Graph.NamingStyles[0]["additionalMetadata"]);
        Assert.Equal(graph.ToPreferences(), legacy);
        Assert.True(graph.ConfigurationEquals(manager.Reader.Graph));
    }

    [Fact]
    public void CustomizedArraysCaptureNamesAndRegisteredDefaultsStillElide()
    {
        var graph = NamingSettingsGraph.Default;
        var style = graph.NamingStyles[1].ToDictionary(pair => pair.Key, pair => pair.Value);
        style["prefix"] = "X";
        var specification = graph.SymbolSpecifications[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        specification["symbolKinds"] = new[] { "class", "interface" };
        var styles = NamingSettingsStorageCodec.EncodeStyles(graph.NamingStyles.SetItem(1, style));
        var specifications = NamingSettingsStorageCodec.EncodeSpecifications(graph.SymbolSpecifications.SetItem(0, specification));
        AssertCleanDefinitions(styles);
        AssertCleanDefinitions(specifications);
        Assert.Equal(graph.ToPreferences().NamingStyles[1].Name, styles[1]["name"]);
        Assert.Equal(graph.ToPreferences().SymbolSpecifications[0].Name, specifications[0]["name"]);
        Assert.Equal("X", styles[1]["prefix"]);

        var captured = styles[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        captured["name"] = "Name captured in another UI language";
        var encodedAgain = NamingSettingsStorageCodec.EncodeStyles(styles.SetItem(0, captured));
        Assert.Equal(captured["name"], encodedAgain[0]["name"]);
        Assert.All(NamingSettingsStorageCodec.EncodeStyles(graph.NamingStyles), item =>
        {
            Assert.Equal("", item["name"]);
            Assert.Equal(true, item["defaultName"]);
            Assert.False(item.ContainsKey("legacyId"));
        });
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task OldBuiltInNameMarkersBecomeLiteralNamesOnlyInCustomizedArrays(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var graph = NamingSettingsGraph.Default;
        var styles = graph.NamingStyles.Select((item, index) =>
        {
            var properties = item.ToDictionary(pair => pair.Key, pair => pair.Value);
            properties["legacyId"] = graph.ToPreferences().NamingStyles[index].ID.ToString("D");
            return (IReadOnlyDictionary<string, object>)properties;
        }).ToImmutableArray();
        var changed = styles[1].ToDictionary(pair => pair.Key, pair => pair.Value);
        changed["prefix"] = "X";
        manager.Reader.Graph = new(graph.SymbolSpecifications, styles.SetItem(1, changed), graph.Rules);
        var legacy = manager.Reader.Graph.ToPreferences();
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, value => legacy = value, s_context);

        await coordinator.SynchronizeFromUnifiedSettingsAsync(CancellationToken.None);

        Assert.EndsWith(".namingStyles", Assert.Single(manager.CustomizedArrays).Key);
        AssertCleanOverrides(manager);
        Assert.Equal(graph.ToPreferences().NamingStyles[1].Name, manager.Reader.Graph.NamingStyles[1]["name"]);
        Assert.Equal(graph.ToPreferences().NamingStyles[1].ID, legacy.NamingStyles[1].ID);
        await coordinator.ApplyAsync(graph, manager.Reader.Graph, "Reset", CancellationToken.None);
        Assert.Empty(manager.CustomizedArrays);
        Assert.Equal(NamingStylePreferences.Default, legacy);
    }

    [Theory]
    [InlineData(SettingCommitOutcome.PendingApproval)]
    [InlineData(SettingCommitOutcome.InternalError)]
    public async Task FailedNormalizationDoesNotRewriteLegacyIdentities(SettingCommitOutcome outcome)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock { CommitOutcome = outcome };
        manager.Reader.Graph = NamingSettingsGraph.FromPreferences(CreateImportedPreferences());
        var writes = 0;
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp),
            () => NamingStylePreferences.Default, _ => writes++, s_context);

        await Assert.ThrowsAsync<InvalidDataException>(() => coordinator.SynchronizeFromUnifiedSettingsAsync(CancellationToken.None));

        Assert.Equal(0, writes);
    }

    [Fact]
    public async Task RenamedCustomDefinitionsConvergeAfterAFreshLegacyOnlyProfileImport()
    {
        var imported = CreateImportedPreferences();
        var firstManager = new NamingSettingsStoreTests.SettingsManagerMock();
        var firstLegacy = imported;
        var first = new NamingSettingsCoordinator(new NamingSettingsStore(firstManager, LanguageNames.CSharp), () => firstLegacy, value => firstLegacy = value, s_context);
        await first.SynchronizeFromLegacyAsync(CancellationToken.None);
        var style = firstManager.Reader.Graph.NamingStyles[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        style["name"] = "Renamed style";
        firstManager.Reader.Graph = new(firstManager.Reader.Graph.SymbolSpecifications, [style], firstManager.Reader.Graph.Rules);
        await first.SynchronizeFromUnifiedSettingsAsync(CancellationToken.None);
        var originalGuid = firstLegacy.NamingStyles[0].ID;

        var secondManager = new NamingSettingsStoreTests.SettingsManagerMock();
        var secondLegacy = NamingStylePreferences.FromXElement(firstLegacy.CreateXElement());
        var second = new NamingSettingsCoordinator(new NamingSettingsStore(secondManager, LanguageNames.CSharp), () => secondLegacy, value => secondLegacy = value, s_context);
        await second.SynchronizeFromLegacyAsync(CancellationToken.None);
        Assert.NotEqual(originalGuid, secondLegacy.NamingStyles[0].ID);
        firstLegacy = secondLegacy;
        await first.SynchronizeFromLegacyAsync(CancellationToken.None);
        Assert.Equal(secondLegacy, firstLegacy);
        Assert.True(firstManager.Reader.Graph.ConfigurationEquals(secondManager.Reader.Graph));
        await second.SynchronizeFromLegacyAsync(CancellationToken.None);
        Assert.Equal(firstLegacy, secondLegacy);
        AssertCleanOverrides(firstManager);
        AssertCleanOverrides(secondManager);
    }

    [Fact]
    public void BuiltInNamesAreReservedEvenWhenTheBuiltInDefinitionIsAbsent()
    {
        var preferences = CreateImportedPreferences();
        var style = preferences.NamingStyles[0];
        var renamed = new NamingStyle(style.ID, "pascal_case", style.Prefix, style.Suffix, style.WordSeparator, style.CapitalizationScheme);
        var graph = NamingSettingsGraph.FromPreferences(new(preferences.SymbolSpecifications, [renamed],
            [new NamingRule(preferences.SymbolSpecifications[0], renamed, ReportDiagnostic.Warn)]));
        Assert.Equal("pascal_case_2", graph.NamingStyles[0]["id"]);
        Assert.NotEqual(NamingStylePreferences.Default.NamingStyles[0].ID, graph.ToPreferences().NamingStyles[0].ID);
    }

    [Fact]
    public void OlderSuffixedBuiltInIdsCanBeImportedWithoutKeepingInvalidDefaultNameMarkers()
    {
        var graph = NamingSettingsGraph.Default;
        var style = graph.NamingStyles[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        style["id"] = "pascal_case_2";
        style["legacyId"] = graph.ToPreferences().NamingStyles[0].ID.ToString("D");
        var rules = graph.Rules.Select(item =>
        {
            var rule = item.ToDictionary(pair => pair.Key, pair => pair.Value);
            if ((string)rule["namingStyleId"] == "pascal_case")
                rule["namingStyleId"] = "pascal_case_2";
            return (IReadOnlyDictionary<string, object>)rule;
        }).ToImmutableArray();
        var previous = new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles.SetItem(0, style), rules);

        var imported = NamingSettingsGraph.FromPreferences(previous.ToPreferences(), previous).NormalizeDefaultNames();

        Assert.Equal("pascal_case_2", imported.NamingStyles[0]["id"]);
        Assert.False(imported.NamingStyles[0].ContainsKey("defaultName"));
        Assert.Equal(previous.ToPreferences().NamingStyles[0].Name, imported.ToPreferences().NamingStyles[0].Name);
        Assert.Equal(imported.ToPreferences().NamingStyles[0].ID, imported.ToPreferences().Rules.NamingRules[1].NamingStyle.ID);
    }

    private static NamingStylePreferences CreateImportedPreferences()
    {
        var template = NamingStylePreferences.Default.SymbolSpecifications[0];
        var specification = new SymbolSpecification(Guid.NewGuid(), "Imported specification", template.ApplicableSymbolKindList,
            template.ApplicableAccessibilityList, template.RequiredModifierList);
        var styleTemplate = NamingStylePreferences.Default.NamingStyles[0];
        var style = new NamingStyle(Guid.NewGuid(), "Imported style", "Custom", styleTemplate.Suffix, styleTemplate.WordSeparator, styleTemplate.CapitalizationScheme);
        return new([specification], [style], [new NamingRule(specification, style, ReportDiagnostic.Warn)]);
    }

    private static void AssertCleanOverrides(NamingSettingsStoreTests.SettingsManagerMock manager)
    {
        foreach (var pair in manager.CustomizedArrays.Where(pair => !pair.Key.EndsWith(".rules", StringComparison.Ordinal)))
            AssertCleanDefinitions(pair.Value);
    }

    private static void AssertCleanDefinitions(ImmutableArray<IReadOnlyDictionary<string, object>> items)
    {
        Assert.NotEmpty(items);
        Assert.All(items, item =>
        {
            Assert.False(item.ContainsKey("legacyId"));
            Assert.False(item.ContainsKey("defaultName"));
            Assert.False(string.IsNullOrWhiteSpace((string)item["name"]));
        });
    }
}
