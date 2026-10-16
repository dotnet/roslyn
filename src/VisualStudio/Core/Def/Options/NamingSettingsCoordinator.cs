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
            {
                if (!current.ConfigurationEquals(graph))
                    throw new InvalidDataException(ServicesVSResources.Naming_settings_changed_while_the_editor_was_open);

                PublishLegacy(current);
                return new ValueTask<SettingCommitResult>(new SettingCommitResult(SettingCommitOutcome.NoChangesQueued, null));
            }

            var result = store.WriteGraph(graph, description, _eventSource);
            if (result.Outcome is SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued)
            {
                var published = store.ReadGraph();
                if (!published.ConfigurationEquals(graph))
                {
                    throw new InvalidDataException(ServicesVSResources.Complete_naming_configuration_was_not_published);
                }

                PublishLegacy(published);
            }

            return new ValueTask<SettingCommitResult>(result);
        }, cancellationToken).AsTask();
    }

    public Task<SettingCommitResult> ResetAsync(CancellationToken cancellationToken)
        => _gate.ExecuteAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = store.Reset(_eventSource);
            if (result.Outcome is SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued)
            {
                var published = store.ReadGraph();
                if (!published.ConfigurationEquals(NamingSettingsGraph.Default))
                {
                    throw new InvalidDataException(ServicesVSResources.Reset_naming_preferences_were_not_published);
                }

                PublishLegacy(published);
            }

            return new ValueTask<SettingCommitResult>(result);
        }, cancellationToken).AsTask();

    public Task SynchronizeFromUnifiedSettingsAsync(CancellationToken cancellationToken)
        => _gate.ExecuteAsync(() =>
        {
            var graph = store.ReadGraph().NormalizeDefaultNames();
            var result = store.WriteGraph(graph, nameof(NamingSettingsCoordinator), _eventSource);
            if (result.Outcome is not (SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued))
            {
                throw new InvalidDataException(string.Format(
                    ServicesVSResources.Unable_to_normalize_naming_settings_0_1,
                    result.Outcome, result.Message));
            }

            var published = store.ReadGraph();
            if (!published.ConfigurationEquals(graph))
            {
                throw new InvalidDataException(ServicesVSResources.Complete_naming_configuration_was_not_published);
            }

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
            {
                throw new InvalidDataException(string.Format(
                    ServicesVSResources.Unable_to_synchronize_legacy_naming_settings_0_1,
                    result.Outcome, result.Message));
            }

            var published = store.ReadGraph();
            if (!published.ConfigurationEquals(graph))
            {
                throw new InvalidDataException(ServicesVSResources.Complete_legacy_naming_configuration_was_not_published);
            }

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
        => _gate.ExecuteAsync(() =>
        {
            var items = arrayContent.Items?.ToImmutableArray() ?? GetItems(NamingSettingsGraph.Default, arraySettingMoniker);
            // The callback payload may not yet be visible to the settings reader.
            var graph = store.ReadGraph(arraySettingMoniker, items).NormalizeDefaultNames();
            PublishLegacy(graph);
            return Task.CompletedTask;
        }, cancellationToken);

    public bool SupportsIncompleteItems(string arraySettingMoniker) => false;

    private static ImmutableArray<IReadOnlyDictionary<string, object>> GetItems(NamingSettingsGraph graph, string moniker)
        => moniker.Substring(moniker.LastIndexOf('.') + 1) switch
        {
            SettingNames.SymbolSpecifications => NamingSettingsStorageCodec.EncodeSpecifications(graph.SymbolSpecifications),
            SettingNames.NamingStyles => NamingSettingsStorageCodec.EncodeStyles(graph.NamingStyles),
            SettingNames.Rules => NamingSettingsStorageCodec.EncodeRules(graph.Rules),
            _ => throw new InvalidDataException($"Unknown naming array '{moniker}'."),
        };
}
