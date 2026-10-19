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
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsCoordinatorTests
{
    private const string Prefix = "languages.csharp.editorConfig.naming.";
    private static readonly JoinableTaskContext s_context = new();

    [Fact]
    public async Task SuccessfulCommitPublishesCompleteGraphToLegacyAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var legacy = NamingStylePreferences.Default;
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp), () => legacy, value => legacy = value, s_context);
        var original = await coordinator.ReadAsync(CancellationToken.None);
        var empty = new NamingSettingsGraph([], [], []);

        var result = await coordinator.ApplyAsync(empty, original, "Naming settings", CancellationToken.None);

        Assert.Equal(SettingCommitOutcome.Success, result.Outcome);
        Assert.Empty(legacy.SymbolSpecifications);
        Assert.Empty(legacy.NamingStyles);
        Assert.Empty(legacy.Rules.NamingRules);
    }

    [Theory]
    [InlineData(SettingCommitOutcome.PendingApproval)]
    [InlineData(SettingCommitOutcome.InternalError)]
    public async Task UnpublishedCommitDoesNotChangeLegacyAsync(SettingCommitOutcome outcome)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock { CommitOutcome = outcome };
        var writes = 0;
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp), () => NamingStylePreferences.Default, _ => writes++, s_context);
        var original = await coordinator.ReadAsync(CancellationToken.None);

        var result = await coordinator.ApplyAsync(new NamingSettingsGraph([], [], []), original, "Naming settings", CancellationToken.None);

        Assert.Equal(outcome, result.Outcome);
        Assert.Equal(0, writes);
    }

    [Fact]
    public async Task SuccessWithoutCompletePublicationIsRejectedAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock { PublishCommit = false };
        var writes = 0;
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp), () => NamingStylePreferences.Default, _ => writes++, s_context);
        var original = await coordinator.ReadAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidDataException>(() => coordinator.ApplyAsync(new NamingSettingsGraph([], [], []), original, "Naming settings", CancellationToken.None));

        Assert.Equal(0, writes);
    }

    [Fact]
    public async Task InvalidJsonRetainsLastValidLegacyPreferencesAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var writes = 0;
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp), () => NamingStylePreferences.Default, _ => writes++, s_context);
        manager.Reader.Graph = new NamingSettingsGraph([], manager.Reader.Graph.NamingStyles, manager.Reader.Graph.Rules);

        await Assert.ThrowsAsync<InvalidDataException>(() => coordinator.SynchronizeFromUnifiedSettingsAsync(CancellationToken.None));

        Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task ArrayMigrationCallbacksReadCurrentLegacyPreferencesAsync(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var legacy = NamingStylePreferences.Default;
        var prefix = language == LanguageNames.CSharp ? Prefix : "languages.basic.codeStyle.naming.";
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, _ => throw new InvalidOperationException(), s_context);
        Assert.Equal(legacy, (await ReadGraphAsync()).ToPreferences());
        legacy = new NamingStylePreferences([], [], ImmutableArray<NamingRule>.Empty);
        Assert.Equal(legacy, (await ReadGraphAsync()).ToPreferences());
        legacy = NamingStylePreferences.Default;
        Assert.Equal(legacy, (await ReadGraphAsync()).ToPreferences());

        var graph = NamingSettingsGraph.Default;
        var style = graph.NamingStyles[1].ToDictionary(pair => pair.Key, pair => pair.Value);
        style["name"] = "Imported style";
        style.Remove("defaultName");
        legacy = new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles.SetItem(1, style), graph.Rules).ToPreferences();
        Assert.Equal(legacy, (await ReadGraphAsync()).ToPreferences());

        async Task<NamingSettingsGraph> ReadGraphAsync()
        {
            var specifications = await coordinator.MigrateToUnifiedSettingsAsync(prefix + "symbolSpecifications", CancellationToken.None);
            var repeated = await coordinator.MigrateToUnifiedSettingsAsync(prefix + "symbolSpecifications", CancellationToken.None);
            Assert.Equal(specifications.Items, repeated.Items);
            var styles = await coordinator.MigrateToUnifiedSettingsAsync(prefix + "namingStyles", CancellationToken.None);
            var rules = await coordinator.MigrateToUnifiedSettingsAsync(prefix + "rules", CancellationToken.None);
            return new NamingSettingsGraph(NamingSettingsStorageCodec.DecodeSpecifications(specifications.Items!.ToImmutableArray()), styles.Items!.ToImmutableArray(), rules.Items!.ToImmutableArray());
        }
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, "symbolSpecifications", false)]
    [InlineData(LanguageNames.CSharp, "namingStyles", false)]
    [InlineData(LanguageNames.CSharp, "rules", false)]
    [InlineData(LanguageNames.CSharp, "symbolSpecifications", true)]
    [InlineData(LanguageNames.CSharp, "namingStyles", true)]
    [InlineData(LanguageNames.CSharp, "rules", true)]
    [InlineData(LanguageNames.VisualBasic, "symbolSpecifications", false)]
    [InlineData(LanguageNames.VisualBasic, "namingStyles", false)]
    [InlineData(LanguageNames.VisualBasic, "rules", false)]
    [InlineData(LanguageNames.VisualBasic, "symbolSpecifications", true)]
    [InlineData(LanguageNames.VisualBasic, "namingStyles", true)]
    [InlineData(LanguageNames.VisualBasic, "rules", true)]
    public async Task ReverseMigrationUsesCompletedPayloadOrDefaultsAndPreservesSiblingsAsync(string language, string name, bool reset)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var defaults = NamingSettingsGraph.Default;
        var current = new NamingSettingsGraph(
            ChangeFirstItem(defaults.SymbolSpecifications, "name", "Reader specification"),
            ChangeFirstItem(defaults.NamingStyles, "name", "Reader style"),
            ChangeFirstItem(defaults.Rules, "severity", "warning"));
        manager.Reader.Graph = current;
        var items = name switch
        {
            "symbolSpecifications" => reset ? defaults.SymbolSpecifications : ChangeFirstItem(current.SymbolSpecifications, "name", "Payload specification"),
            "namingStyles" => reset ? defaults.NamingStyles : ChangeFirstItem(current.NamingStyles, "prefix", "payload_"),
            "rules" => reset ? defaults.Rules : ChangeFirstItem(current.Rules, "severity", "error"),
            _ => throw new InvalidOperationException(name),
        };
        var expected = new NamingSettingsGraph(
            name == "symbolSpecifications" ? items : current.SymbolSpecifications,
            name == "namingStyles" ? items : current.NamingStyles,
            name == "rules" ? items : current.Rules);
        var prefix = language == LanguageNames.CSharp ? Prefix : "languages.basic.codeStyle.naming.";
        var storedItems = name == "symbolSpecifications" ? NamingSettingsStorageCodec.EncodeSpecifications(items) : items;
        if (!reset)
        {
            using var manifest = NamingSettingsManifestTests.Load(language == LanguageNames.CSharp
                ? "csharpNamingSettings.registration.json" : "visualBasicNamingSettings.registration.json");
            var propertyDefaults = manifest.RootElement.GetProperty("properties").GetProperty(prefix + name)
                .GetProperty("items").GetProperty("properties").EnumerateObject().ToDictionary(
                    property => property.Name, property => NamingSettingsManifestTests.ReadScalar(property.Value.GetProperty("default")), StringComparer.Ordinal);
            storedItems = storedItems.Select(item =>
            {
                var completed = new Dictionary<string, object>(propertyDefaults, StringComparer.Ordinal);
                foreach (var property in item)
                    completed[property.Key] = property.Value;
                return (IReadOnlyDictionary<string, object>)completed;
            }).ToImmutableArray();
        }
        var legacy = current.ToPreferences();
        NamingStylePreferences? refreshed = null;
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, value => legacy = value, s_context, value => refreshed = value);

        await coordinator.MigrateFromUnifiedSettingsAsync(prefix + name, new ArraySettingContent(reset ? null : storedItems), CancellationToken.None);

        Assert.Equal(expected.ToPreferences(), legacy);
        Assert.Equal(legacy, refreshed);
        Assert.True(current.ConfigurationEquals(manager.Reader.Graph));
        Assert.Null(manager.LastWriter);
        Assert.False(coordinator.SupportsIncompleteItems(prefix + name));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task ReverseMigrationCanReplaceAnInvalidStaleArrayAsync(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var defaults = NamingSettingsGraph.Default;
        manager.Reader.Graph = new(defaults.SymbolSpecifications, defaults.NamingStyles, ChangeFirstItem(defaults.Rules, "severity", "invalid"));
        var rules = ChangeFirstItem(defaults.Rules, "severity", "warning");
        var legacy = NamingStylePreferences.Default;
        var prefix = language == LanguageNames.CSharp ? Prefix : "languages.basic.codeStyle.naming.";
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, value => legacy = value, s_context);

        await coordinator.MigrateFromUnifiedSettingsAsync(prefix + "rules", new ArraySettingContent(rules), CancellationToken.None);

        Assert.Equal(new NamingSettingsGraph(defaults.SymbolSpecifications, defaults.NamingStyles, rules).ToPreferences(), legacy);
        Assert.Equal("invalid", manager.Reader.Graph.Rules[0]["severity"]);
        Assert.Null(manager.LastWriter);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task InvalidReverseMigrationPayloadDoesNotOverwriteLegacyAsync(string language)
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var legacy = NamingStylePreferences.Default;
        var refreshes = 0;
        var prefix = language == LanguageNames.CSharp ? Prefix : "languages.basic.codeStyle.naming.";
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, language), () => legacy, value => legacy = value, s_context, _ => refreshes++);
        var rules = ChangeFirstItem(NamingSettingsGraph.Default.Rules, "namingStyleId", "missing-style");

        await Assert.ThrowsAsync<InvalidDataException>(() => coordinator.MigrateFromUnifiedSettingsAsync(prefix + "rules", new ArraySettingContent(rules), CancellationToken.None));

        Assert.Equal(NamingStylePreferences.Default, legacy);
        Assert.Equal(0, refreshes);
        Assert.Null(manager.LastWriter);
    }

    [Fact]
    public async Task StaleEditorCannotOverwriteIncomingNativeConfigurationAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp), () => NamingStylePreferences.Default, _ => throw new InvalidOperationException(), s_context);
        var original = await coordinator.ReadAsync(CancellationToken.None);
        manager.Reader.Graph = new NamingSettingsGraph([], [], []);

        await Assert.ThrowsAsync<InvalidDataException>(() => coordinator.ApplyAsync(original, original, "Naming settings", CancellationToken.None));

        Assert.Null(manager.LastWriter);
    }

    [Fact]
    public void RegisteredDefaultsRoundTripWithoutFreezingLocalizedNames()
    {
        var graph = NamingSettingsGraph.Default;
        Assert.Equal(NamingStylePreferences.Default, graph.ToPreferences());
        var roundTrip = NamingSettingsGraph.FromPreferences(graph.ToPreferences(), graph);
        foreach (var item in roundTrip.SymbolSpecifications.AddRange(roundTrip.NamingStyles))
        {
            Assert.Equal("", item["name"]);
            Assert.Equal(true, item["defaultName"]);
        }
    }

    private static ImmutableArray<IReadOnlyDictionary<string, object>> ChangeFirstItem(
        ImmutableArray<IReadOnlyDictionary<string, object>> items, string property, object value)
    {
        var item = items[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        item[property] = value;
        if (property == "name")
            item.Remove("defaultName");
        return items.SetItem(0, item);
    }
}
