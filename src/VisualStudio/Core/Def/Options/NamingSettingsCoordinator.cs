// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using static Microsoft.VisualStudio.LanguageServices.Options.NamingSettingsConstants;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class NamingSettingsCoordinator(
    NamingSettingsStore store,
    Func<NamingStylePreferences> readLegacy,
    Action<NamingStylePreferences> writeLegacy,
    JoinableTaskContext joinableTaskContext,
    Action<NamingStylePreferences>? refreshRuntime = null) : IArraySettingMigrator
{
    private readonly ReentrantSemaphore _gate = ReentrantSemaphore.Create(1, joinableTaskContext, ReentrantSemaphore.ReentrancyMode.NotRecognized);
    private readonly Guid _eventSource = Guid.NewGuid();
    private NamingSettingsGraph? _graph;
    private NamingStylePreferences? _publishingLegacy;

    public event EventHandler? Changed;

    public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public bool IsOwnChange(SettingsUpdate update) => update.EventSource == _eventSource;

    public bool IsOwnLegacyChange(NamingStylePreferences preferences)
        => preferences.Equals(Volatile.Read(ref _publishingLegacy));

    public NamingSettingsGraph ReadForValidation() => store.ReadGraph();

    public Task<NamingSettingsGraph> ReadAsync(CancellationToken cancellationToken)
        => _gate.ExecuteAsync(() => new ValueTask<NamingSettingsGraph>(_graph = store.ReadGraph()), cancellationToken).AsTask();

    public Task<SettingCommitResult> ApplyAsync(NamingSettingsGraph graph, NamingSettingsGraph expected, string description, CancellationToken cancellationToken)
    {
        graph = graph.NormalizeDefaultNames();
        return _gate.ExecuteAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = store.ReadGraph();
            if (!current.ConfigurationEquals(expected))
                throw new InvalidDataException("Naming settings changed while the editor was open. Reload the settings before applying this edit.");

            var result = store.WriteGraph(graph, description, _eventSource);
            if (result.Outcome is SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued)
            {
                var published = store.ReadGraph();
                if (!published.ConfigurationEquals(graph))
                    throw new InvalidDataException("The complete naming configuration was not published. Legacy naming preferences were not changed.");

                PublishLegacy(published);
            }

            return new ValueTask<SettingCommitResult>(result);
        }, cancellationToken).AsTask();
    }

    public Task SynchronizeFromUnifiedSettingsAsync(CancellationToken cancellationToken)
        => _gate.ExecuteAsync(() =>
        {
            var graph = store.ReadGraph().NormalizeDefaultNames();
            var result = store.WriteGraph(graph, nameof(NamingSettingsCoordinator), _eventSource);
            if (result.Outcome is not (SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued))
                throw new InvalidDataException($"Unable to normalize naming settings: {result.Outcome}. {result.Message}");

            var published = store.ReadGraph();
            if (!published.ConfigurationEquals(graph))
                throw new InvalidDataException("The complete naming configuration was not published. Legacy naming preferences were not changed.");

            PublishLegacy(published);
            return Task.CompletedTask;
        }, cancellationToken);

    public Task SynchronizeFromLegacyAsync(CancellationToken cancellationToken)
        => SynchronizeFromLegacyAsync(readLegacy(), cancellationToken);

    public Task SynchronizeFromLegacyAsync(NamingStylePreferences preferences, CancellationToken cancellationToken)
        => _gate.ExecuteAsync(() =>
        {
            var previous = _graph ?? store.ReadGraph();
            var graph = preferences.Equals(NamingStylePreferences.Default)
                ? NamingSettingsGraph.Default
                : NamingSettingsGraph.FromPreferences(preferences, previous).NormalizeDefaultNames();
            var result = store.WriteGraph(graph, nameof(NamingSettingsCoordinator), _eventSource);
            if (result.Outcome is not (SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued))
                throw new InvalidDataException($"Unable to synchronize legacy naming settings: {result.Outcome}. {result.Message}");

            var published = store.ReadGraph();
            if (!published.ConfigurationEquals(graph))
                throw new InvalidDataException("The complete legacy naming configuration was not published.");

            PublishLegacy(published);
            return Task.CompletedTask;
        }, cancellationToken);

    private void PublishLegacy(NamingSettingsGraph graph)
    {
        var preferences = graph.ToPreferences();
        try
        {
            Volatile.Write(ref _publishingLegacy, preferences);
            if (!preferences.Equals(readLegacy()))
                writeLegacy(preferences);
            _graph = graph;
            refreshRuntime?.Invoke(preferences);
        }
        finally
        {
            Volatile.Write(ref _publishingLegacy, null);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task<ArraySettingContent> MigrateToUnifiedSettingsAsync(string arraySettingMoniker, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var preferences = readLegacy();
        var graph = preferences.Equals(NamingStylePreferences.Default)
            ? NamingSettingsGraph.Default : NamingSettingsGraph.FromPreferences(preferences);
        return Task.FromResult(new ArraySettingContent(GetItems(graph, arraySettingMoniker)));
    }

    public Task MigrateFromUnifiedSettingsAsync(string arraySettingMoniker, ArraySettingContent arrayContent, CancellationToken cancellationToken)
    {
        _ = GetItems(NamingSettingsGraph.Default, arraySettingMoniker);
        // Re-read the whole graph, including effective defaults for reset, instead of overwriting XML with a single array.
        return SynchronizeFromUnifiedSettingsAsync(cancellationToken);
    }

    public bool SupportsIncompleteItems(string arraySettingMoniker) => true;

    private static ImmutableArray<IReadOnlyDictionary<string, object>> GetItems(NamingSettingsGraph graph, string moniker)
        => moniker.Substring(moniker.LastIndexOf('.') + 1) switch
        {
            SettingNames.SymbolSpecifications => NamingSettingsStorageCodec.EncodeSpecifications(graph.SymbolSpecifications),
            SettingNames.NamingStyles => NamingSettingsStorageCodec.EncodeStyles(graph.NamingStyles),
            SettingNames.Rules => NamingSettingsStorageCodec.EncodeRules(graph.Rules),
            _ => throw new InvalidDataException($"Unknown naming array '{moniker}'."),
        };
}
