// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
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

    [Fact]
    public async Task InitialArrayMigrationsUseOneLegacySnapshotAsync()
    {
        var manager = new NamingSettingsStoreTests.SettingsManagerMock();
        var legacy = NamingStylePreferences.Default;
        var coordinator = new NamingSettingsCoordinator(new NamingSettingsStore(manager, LanguageNames.CSharp), () => legacy, _ => throw new InvalidOperationException(), s_context);
        var specifications = await coordinator.MigrateToUnifiedSettingsAsync(Prefix + "symbolSpecifications", CancellationToken.None);
        legacy = new NamingStylePreferences([], [], ImmutableArray<NamingRule>.Empty);
        var styles = await coordinator.MigrateToUnifiedSettingsAsync(Prefix + "namingStyles", CancellationToken.None);
        var rules = await coordinator.MigrateToUnifiedSettingsAsync(Prefix + "rules", CancellationToken.None);

        var graph = new NamingSettingsGraph(NamingSettingsStorageCodec.DecodeSpecifications(specifications.Items!.ToImmutableArray()), styles.Items!.ToImmutableArray(), rules.Items!.ToImmutableArray());
        Assert.Equal(NamingStylePreferences.Default, graph.ToPreferences());
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
