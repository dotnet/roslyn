// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using static Microsoft.VisualStudio.LanguageServices.Options.NamingSettingsConstants;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class NamingSettingsStore(ISettingsManager settingsManager, string language)
{
    public string Language { get; } = language;

    private readonly string _prefix = language switch
    {
        LanguageNames.CSharp => SettingNames.CSharpPrefix,
        LanguageNames.VisualBasic => SettingNames.VisualBasicPrefix,
        _ => throw ExceptionUtilities.UnexpectedValue(language),
    };

    public bool IsUnifiedSettingsEnabled
        => settingsManager.GetReader().GetArray<IReadOnlyDictionary<string, object>>(
            _prefix + SettingNames.Rules, SettingReadOptions.RequireValidation | SettingReadOptions.NoFallbackToDefault).Outcome
            != SettingRetrievalOutcome.NotSupportedInClassicMode;

    public NamingSettingsGraph ReadGraph()
        => ReadGraphCore(null, default);

    public NamingSettingsGraph ReadGraph(string arraySettingMoniker, ImmutableArray<IReadOnlyDictionary<string, object>> items)
    {
        if (arraySettingMoniker != _prefix + SettingNames.SymbolSpecifications &&
            arraySettingMoniker != _prefix + SettingNames.NamingStyles &&
            arraySettingMoniker != _prefix + SettingNames.Rules)
        {
            throw new InvalidDataException($"Unknown naming array '{arraySettingMoniker}'.");
        }

        return ReadGraphCore(arraySettingMoniker, items);
    }

    private NamingSettingsGraph ReadGraphCore(string? arraySettingMoniker, ImmutableArray<IReadOnlyDictionary<string, object>> items)
    {
        var reader = settingsManager.GetReader();
        var graph = new NamingSettingsGraph(
            NamingSettingsStorageSerializer.DeserializeSpecifications(ReadItems(SettingNames.SymbolSpecifications)),
            ReadItems(SettingNames.NamingStyles),
            ReadItems(SettingNames.Rules));

        _ = graph.ToPreferences();
        return graph;

        ImmutableArray<IReadOnlyDictionary<string, object>> ReadItems(string name)
            => arraySettingMoniker == _prefix + name ? items : ReadArray(reader, name);
    }

    public SettingCommitResult WriteGraph(NamingSettingsGraph graph, string changeDescription, Guid eventSource)
    {
        graph = graph.NormalizeDefaultNames();

        // A writer is private to this attempt so a rejected array cannot leave queued changes for a later commit.
        var writer = settingsManager.GetWriter(nameof(NamingSettingsStore), eventSource);
        EnqueueArray(writer, SettingNames.SymbolSpecifications, NamingSettingsStorageSerializer.SerializeSpecifications(graph.SymbolSpecifications));
        EnqueueArray(writer, SettingNames.NamingStyles, NamingSettingsStorageSerializer.SerializeStyles(graph.NamingStyles));
        EnqueueArray(writer, SettingNames.Rules, NamingSettingsStorageSerializer.SerializeRules(graph.Rules));

        // Preserve PendingApproval and error outcomes; the coordinator must verify publication before updating legacy/runtime state.
        return writer.RequestCommit(changeDescription);
    }

    public SettingCommitResult Reset(Guid eventSource)
        => WriteGraph(NamingSettingsGraph.Default, ServicesVSResources.Reset_naming_preferences, eventSource);

    public IDisposable SubscribeToChanges(Action<SettingsUpdate> handler)
        => settingsManager.GetReader().SubscribeToChanges(
            handler, _prefix + SettingNames.SymbolSpecifications, _prefix + SettingNames.NamingStyles, _prefix + SettingNames.Rules);

    private ImmutableArray<IReadOnlyDictionary<string, object>> ReadArray(ISettingsReader reader, string name)
    {
        var moniker = _prefix + name;
        var retrieval = reader.GetArray<IReadOnlyDictionary<string, object>>(
            moniker, SettingReadOptions.RequireValidation | SettingReadOptions.NoFallbackToDefault);
        if (retrieval is not { Outcome: SettingRetrievalOutcome.Success, Value: { } items })
        {
            throw new InvalidDataException(string.Format(
                ServicesVSResources.Unable_to_read_naming_setting_0_1_2,
                moniker, retrieval.Outcome, retrieval.Message));
        }

        return items.ToImmutableArray();
    }

    private void EnqueueArray(ISettingsWriter writer, string name, ImmutableArray<IReadOnlyDictionary<string, object>> items)
    {
        var moniker = _prefix + name;
        // The native array writer removes values equal to registered defaults unless CustomizeUnconditionally is used.
        var change = writer.EnqueueArrayChange(moniker, items);
        if (change.Outcome is not (SettingChangeOutcome.PendingCommit or SettingChangeOutcome.NoOp))
        {
            throw new InvalidDataException(string.Format(
                ServicesVSResources.Unable_to_enqueue_naming_setting_0_1_2,
                moniker, change.Outcome, change.Message));
        }
    }
}
