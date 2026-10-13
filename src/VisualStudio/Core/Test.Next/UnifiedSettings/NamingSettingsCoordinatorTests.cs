// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    private const string Prefix = "languages.csharp.codeStyle.naming.";
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
        manager.Reader.Graph = new NamingSettingsGraph(manager.Reader.Graph.SymbolSpecifications, manager.Reader.Graph.NamingStyles, []);

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

    [Fact]
    public async Task ReverseResetUsesRegisteredWholeGraphDefaultsAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        manager.Reader.Graph = NamingSettingsGraph.Default;
        var legacy = new NamingStylePreferences([], [], ImmutableArray<NamingRule>.Empty);
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp), () => legacy, value => legacy = value, s_context);

        await coordinator.MigrateFromUnifiedSettingsAsync(Prefix + "rules", new ArraySettingContent(null), CancellationToken.None);

        Assert.Equal(NamingStylePreferences.Default, legacy);
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
}
