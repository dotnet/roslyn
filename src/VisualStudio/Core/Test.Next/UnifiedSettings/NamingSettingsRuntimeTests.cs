// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Moq;
using Xunit;
using LegacySettingsManager = Microsoft.VisualStudio.Settings.ISettingsManager;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsRuntimeTests
{
    private const string LegacyKey = "TextEditor.CSharp.Specific.NamingPreferences5";

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task FallbackOnlyProfileSurvivesMigrationAndStartup(string language)
    {
        using var fixture = new Fixture(language);
        fixture.SetLegacyValue(fixture.PrimaryKey, null);
        var preferences = CreatePreferences("Older profile style");
        fixture.SetLegacyValue(fixture.FallbackKey, preferences);

        await fixture.MigrateLegacyAsync();
        await fixture.Runtime.InitializeAsync();

        Assert.Equal(preferences, fixture.Manager.Reader.Graph.ToPreferences());
        Assert.Equal(preferences, Assert.Single(fixture.Refreshes));
        Assert.Equal(preferences, fixture.Legacy);
        Assert.Equal(0, fixture.LegacyWrites);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task FallbackImportsReachNativeAndRuntime(string language)
    {
        using var fixture = new Fixture(language);
        fixture.SetLegacyValue(fixture.PrimaryKey, null);
        await fixture.Runtime.InitializeAsync();
        var preferences = CreatePreferences("Imported fallback style");
        fixture.SetLegacyValue(fixture.FallbackKey, preferences);
        fixture.NotifyLegacy(fixture.FallbackKey);
        await Task.WhenAll(fixture.Observed);

        Assert.Equal(preferences, fixture.Manager.Reader.Graph.ToPreferences());
        Assert.Equal(preferences, fixture.Refreshes[^1]);
        Assert.Equal(0, fixture.LegacyWrites);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task PrimaryPreferencesTakePrecedenceOverFallbackImports(string language)
    {
        using var fixture = new Fixture(language);
        var primary = CreatePreferences("Primary profile style");
        fixture.Legacy = primary;
        await fixture.MigrateLegacyAsync();
        await fixture.Runtime.InitializeAsync();
        fixture.SetLegacyValue(fixture.FallbackKey, CreatePreferences("Ignored fallback style"));
        fixture.NotifyLegacy(fixture.FallbackKey);
        await Task.WhenAll(fixture.Observed);

        Assert.Equal(primary, fixture.Manager.Reader.Graph.ToPreferences());
        Assert.Equal(primary, fixture.Refreshes[^1]);
        Assert.Equal(primary, fixture.Legacy);
        Assert.Equal(0, fixture.LegacyWrites);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task RemovingPrimaryReactivatesFallback(string language)
    {
        using var fixture = new Fixture(language);
        fixture.Legacy = CreatePreferences("Primary profile style");
        var fallback = CreatePreferences("Fallback profile style");
        fixture.SetLegacyValue(fixture.FallbackKey, fallback);
        await fixture.MigrateLegacyAsync();
        await fixture.Runtime.InitializeAsync();
        fixture.SetLegacyValue(fixture.PrimaryKey, null);
        fixture.NotifyLegacy();
        await Task.WhenAll(fixture.Observed);

        Assert.Equal(fallback, fixture.Manager.Reader.Graph.ToPreferences());
        Assert.Equal(fallback, fixture.Refreshes[^1]);
        Assert.Equal(fallback, fixture.Legacy);
        Assert.Equal(0, fixture.LegacyWrites);
    }

    [Fact]
    public async Task ClassicModeLeavesTheExistingLegacyPersisterInControl()
    {
        using var fixture = new Fixture();
        fixture.Manager.Reader.Outcome = SettingRetrievalOutcome.NotSupportedInClassicMode;
        await fixture.Runtime.InitializeAsync();

        Assert.Null(fixture.Manager.Reader.ChangeHandler);
        Assert.Null(fixture.Manager.LastWriter);
        Assert.Empty(fixture.Refreshes);
        Assert.Equal(0, fixture.LegacyWrites);
    }

    [Fact]
    public async Task ColdStartupRefreshesRuntimeEvenWhenLegacyXmlAlreadyMatches()
    {
        using var fixture = new Fixture();
        await fixture.Runtime.InitializeAsync();
        await fixture.Runtime.InitializeAsync();

        Assert.True(fixture.Runtime.Ready.IsCompleted);
        Assert.Single(fixture.Refreshes);
        Assert.Equal(3, fixture.Manager.Reader.SubscriptionMonikers.Length);
        Assert.Equal(0, fixture.LegacyWrites);
        Assert.Equal(1, fixture.Manager.LastWriter!.CommitCount);
    }

    [Fact]
    public async Task FailedLegacyImportRetriesAutomatically()
    {
        using var fixture = new Fixture();
        await fixture.Runtime.InitializeAsync();
        fixture.Manager.CommitOutcome = SettingCommitOutcome.InternalError;
        fixture.Manager.BeforeCommit = () => fixture.Manager.CommitOutcome = SettingCommitOutcome.Success;
        fixture.Legacy = new([], [], System.Collections.Immutable.ImmutableArray<NamingRule>.Empty);
        fixture.NotifyLegacy();
        await fixture.Observed[^1];

        Assert.Empty(fixture.Manager.Reader.Graph.Rules);
        Assert.Equal(fixture.Legacy, fixture.Refreshes[^1]);
        Assert.IsType<InvalidDataException>(Assert.Single(fixture.Failures));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task LegacyImportsDuringStartupSurviveNativePublication(string language)
    {
        using var fixture = new Fixture(language);
        var committing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Manager.BeforeCommit = () =>
        {
            fixture.Manager.BeforeCommit = null;
            committing.SetResult(true);
            resume.Task.GetAwaiter().GetResult();
        };
        var initialization = Task.Run(fixture.Runtime.InitializeAsync);
        await committing.Task;
        Task[] imports;
        try
        {
            var graph = NamingSettingsGraph.Default;
            var style = graph.NamingStyles[1].ToDictionary(pair => pair.Key, pair => pair.Value);
            style["name"] = "Imported style";
            style.Remove("defaultName");
            fixture.Legacy = new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles.SetItem(1, style), graph.Rules).ToPreferences();
            fixture.NotifyLegacy();
            fixture.Legacy = new([], [], System.Collections.Immutable.ImmutableArray<NamingRule>.Empty);
            fixture.NotifyLegacy();
            imports = fixture.Observed.ToArray();
        }
        finally
        {
            resume.SetResult(true);
        }

        await initialization;
        await Task.WhenAll(imports);

        Assert.Empty(fixture.Legacy.Rules.NamingRules);
        Assert.Empty(fixture.Manager.Reader.Graph.Rules);
        Assert.Equal(fixture.Legacy, fixture.Refreshes[^1]);
        Assert.Contains(fixture.Refreshes, preferences => preferences.NamingStyles.Any(style => style.Name == "Imported style"));
    }

    [Fact]
    public async Task FailedStartupReportsFailureAndRecoversAfterNativeCorrection()
    {
        using var fixture = new Fixture();
        fixture.Manager.Reader.Outcome = SettingRetrievalOutcome.FailedValidation;
        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.Runtime.InitializeAsync());
        Assert.Empty(fixture.Refreshes);
        var failedRead = await fixture.Runtime.Provider.GetValueAsync<IReadOnlyList<IDictionary<string, object>>>("rules", CancellationToken.None);
        Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IDictionary<string, object>>>.Failure>(failedRead);

        fixture.Manager.Reader.Outcome = SettingRetrievalOutcome.Success;
        var resolved = 0;
        fixture.Runtime.Provider.ErrorConditionResolved += (_, _) => resolved++;
        fixture.NotifyNative(Guid.NewGuid());
        await fixture.Observed[^1];

        Assert.Single(fixture.Refreshes);
        Assert.True(resolved > 0);
        Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IDictionary<string, object>>>.Success>(
            await fixture.Runtime.Provider.GetValueAsync<IReadOnlyList<IDictionary<string, object>>>("rules", CancellationToken.None));
    }

    [Fact]
    public async Task InvalidNativeNotificationRetainsRuntimeAndDoesNotClaimRecovery()
    {
        using var fixture = new Fixture();
        await fixture.Runtime.InitializeAsync();
        var resolved = 0;
        fixture.Runtime.Provider.ErrorConditionResolved += (_, _) => resolved++;
        fixture.Manager.Reader.Outcome = SettingRetrievalOutcome.FailedValidation;
        fixture.NotifyNative(Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.Observed[^1]);

        Assert.Single(fixture.Refreshes);
        Assert.Equal(0, resolved);
        Assert.Equal(NamingStylePreferences.Default, fixture.Legacy);
    }

    [Fact]
    public async Task IncomingLegacyProfileWritesReachNativeAndRuntime()
    {
        using var fixture = new Fixture();
        await fixture.Runtime.InitializeAsync();
        fixture.Legacy = new([], [], System.Collections.Immutable.ImmutableArray<NamingRule>.Empty);
        fixture.NotifyLegacy();
        await fixture.Observed[^1];

        Assert.Empty(fixture.Manager.Reader.Graph.Rules);
        Assert.Equal(fixture.Legacy, fixture.Refreshes[^1]);
        Assert.Equal(1, fixture.Manager.LastWriter!.CommitCount);
    }

    [Fact]
    public async Task NativeEditPublishesLegacyWithoutRecursiveWrite()
    {
        using var fixture = new Fixture();
        await fixture.Runtime.InitializeAsync();
        fixture.Manager.Reader.Graph = new([], [], []);
        fixture.NotifyNative(Guid.NewGuid());
        await Task.WhenAll(fixture.Observed);

        Assert.Empty(fixture.Legacy.Rules.NamingRules);
        Assert.Equal(1, fixture.LegacyWrites);
        Assert.Equal(1, fixture.Manager.LastWriter!.CommitCount);
    }

    [Fact]
    public async Task OwnNotificationsAndDisposedSubscriptionsDoNotRefresh()
    {
        using var fixture = new Fixture();
        await fixture.Runtime.InitializeAsync();
        await fixture.Runtime.Coordinator.ApplyAsync(new([], [], []), fixture.Manager.Reader.Graph, "Naming", CancellationToken.None);
        fixture.Observed.Clear();
        fixture.NotifyNative(fixture.Manager.LastEventSource);
        Assert.Empty(fixture.Observed);

        fixture.Runtime.Dispose();
        fixture.NotifyLegacy();
        Assert.Null(fixture.Manager.Reader.ChangeHandler);
        Assert.Empty(fixture.Observed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => fixture.Runtime.InitializeAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ObservedChangesRefreshAllExternalTablesAndChoicesWithoutManualReload(bool legacyChange)
    {
        using var fixture = new Fixture();
        await fixture.Runtime.InitializeAsync();
        await fixture.Runtime.Provider.GetGraphAsync(CancellationToken.None);
        var notifications = 0;
        var choicesChanged = new List<string>();
        fixture.Runtime.Provider.SettingValuesChanged += (_, _) => notifications++;
        fixture.Runtime.Provider.EnumSettingChoicesChanged += (_, e) => choicesChanged.Add(e.SettingMoniker);
        var graph = fixture.Manager.Reader.Graph;
        var specification = graph.SymbolSpecifications[1].ToDictionary(pair => pair.Key, pair => pair.Value);
        specification["name"] = "Observed specification";
        specification.Remove("defaultName");
        var style = graph.NamingStyles[1].ToDictionary(pair => pair.Key, pair => pair.Value);
        style["name"] = "Observed style";
        style.Remove("defaultName");
        var updated = new NamingSettingsGraph(graph.SymbolSpecifications.SetItem(1, specification), graph.NamingStyles.SetItem(1, style), graph.Rules);
        if (legacyChange)
        {
            fixture.Legacy = updated.ToPreferences();
            fixture.NotifyLegacy();
        }
        else
        {
            fixture.Manager.Reader.Graph = updated;
            fixture.NotifyNative(Guid.NewGuid());
        }
        await Task.WhenAll(fixture.Observed);

        Assert.True(notifications > 0);
        foreach (var moniker in new[] { "rules", "symbolSpecifications", "namingStyles" })
        {
            Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<IDictionary<string, object>>>.Success>(
                await fixture.Runtime.Provider.GetValueAsync<IReadOnlyList<IDictionary<string, object>>>(moniker, CancellationToken.None));
        }
        var refreshed = await fixture.Runtime.Provider.GetGraphAsync(CancellationToken.None);
        Assert.Equal("Observed specification", refreshed.ToPreferences().Rules.NamingRules[0].SymbolSpecification.Name);
        Assert.Equal("Observed style", refreshed.ToPreferences().Rules.NamingRules[0].NamingStyle.Name);
        foreach (var moniker in new[] { "rules.symbolSpecificationId", "rules.namingStyleId", "symbolSpecifications.symbolKinds" })
            Assert.Contains(moniker, choicesChanged);
        var choices = Assert.IsType<ExternalSettingOperationResult<IReadOnlyList<EnumChoice>>.Success>(
            await fixture.Runtime.Provider.GetEnumChoicesAsync("rules.namingStyleId", CancellationToken.None)).Value;
        Assert.Contains(choices, choice => choice.Title == "Observed style");
    }

    private static NamingStylePreferences CreatePreferences(string styleName)
    {
        var element = NamingStylePreferences.Default.CreateXElement();
        element.Element("NamingStyles")!.Element("NamingStyle")!.SetAttributeValue("Name", styleName);
        return NamingStylePreferences.FromXElement(element);
    }

    private sealed class Fixture : IDisposable
    {
        public readonly NamingSettingsStoreTests.SettingsManagerMock Manager = new();
        public readonly Mock<ISettingsSubset> Subset = new(MockBehavior.Strict);
        private readonly Mock<LegacySettingsManager> _legacyManager = new(MockBehavior.Strict);
        public readonly List<Task> Observed = [];
        public readonly List<Exception> Failures = [];
        public readonly List<NamingStylePreferences> Refreshes = [];
        public NamingStylePreferences Legacy
        {
            get => ReadLegacy();
            set => SetLegacyValue(PrimaryKey, value);
        }
        public int LegacyWrites;
        public readonly NamingSettingsRuntime Runtime;
        private readonly JoinableTaskContext _context = new();
        public readonly string PrimaryKey;
        public readonly string FallbackKey;
        private readonly string _prefix;

        public Fixture(string language = LanguageNames.CSharp)
        {
            PrimaryKey = language == LanguageNames.CSharp ? LegacyKey : "TextEditor.VisualBasic.Specific.NamingPreferences5";
            FallbackKey = $"TextEditor.{language}.Specific.NamingPreferences";
            _prefix = language == LanguageNames.CSharp ? "languages.csharp.editorConfig.naming." : "languages.basic.codeStyle.naming.";
            Legacy = NamingStylePreferences.Default;
            SetLegacyValue(FallbackKey, null);
            var store = new NamingSettingsStore(Manager, language);
            var coordinator = new NamingSettingsCoordinator(store, ReadLegacy, value =>
            {
                Legacy = value;
                LegacyWrites++;
                NotifyLegacy();
            }, _context, Refreshes.Add);
            Runtime = new(store, coordinator, Subset.Object, PrimaryKey, FallbackKey, ReadLegacy, Observed.Add, Failures.Add,
                CancellationToken.None, TimeSpan.Zero);
        }

        private NamingStylePreferences ReadLegacy() => NamingSettingsService.ReadLegacyPreferences(_legacyManager.Object, PrimaryKey, FallbackKey);

        public void SetLegacyValue(string key, NamingStylePreferences? preferences)
        {
            var xml = preferences?.CreateXElement().ToString() ?? "";
            _legacyManager.Setup(manager => manager.TryGetValue(key, out xml)).Returns(preferences is null ? GetValueResult.Missing : GetValueResult.Success);
        }

        public async Task MigrateLegacyAsync()
        {
            var specifications = await Runtime.Coordinator.MigrateToUnifiedSettingsAsync(_prefix + "symbolSpecifications", CancellationToken.None);
            var styles = await Runtime.Coordinator.MigrateToUnifiedSettingsAsync(_prefix + "namingStyles", CancellationToken.None);
            var rules = await Runtime.Coordinator.MigrateToUnifiedSettingsAsync(_prefix + "rules", CancellationToken.None);
            Manager.Reader.Graph = new(NamingSettingsStorageSerializer.DeserializeSpecifications(specifications.Items!.ToImmutableArray()),
                styles.Items!.ToImmutableArray(), rules.Items!.ToImmutableArray());
        }

        public void NotifyLegacy(string? key = null) => Subset.Raise(subset => subset.SettingChangedAsync += null, Subset.Object, new PropertyChangedEventArgs(key ?? PrimaryKey));
        public void NotifyNative(Guid source) => Manager.Reader.ChangeHandler?.Invoke(new(source, ["languages.csharp.editorConfig.naming.rules"]));
        public void Dispose()
        {
            Runtime.Dispose();
            _context.Dispose();
        }
    }
}
