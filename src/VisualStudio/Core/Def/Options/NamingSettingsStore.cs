// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class NamingSettingsStore(ISettingsManager settingsManager, string language)
{
    public string Language { get; } = language;

    private readonly string _prefix = language switch
    {
        LanguageNames.CSharp => "languages.csharp.codeStyle.naming.",
        LanguageNames.VisualBasic => "languages.basic.codeStyle.naming.",
        _ => throw ExceptionUtilities.UnexpectedValue(language),
    };

    public bool IsUnifiedSettingsEnabled
        => settingsManager.GetReader().GetArray<IReadOnlyDictionary<string, object>>(
            _prefix + "rules", SettingReadOptions.RequireValidation | SettingReadOptions.NoFallbackToDefault).Outcome
            != SettingRetrievalOutcome.NotSupportedInClassicMode;

    public NamingSettingsGraph ReadGraph()
    {
        var reader = settingsManager.GetReader();
        var graph = new NamingSettingsGraph(
            NamingSettingsStorageCodec.DecodeSpecifications(ReadArray(reader, "symbolSpecifications")),
            ReadArray(reader, "namingStyles"),
            ReadArray(reader, "rules"));

        _ = graph.ToPreferences();
        return graph;
    }

    public SettingCommitResult WriteGraph(NamingSettingsGraph graph, string changeDescription, Guid eventSource)
    {
        graph = graph.NormalizeDefaultNames();

        // A writer is private to this attempt so a rejected array cannot leave queued changes for a later commit.
        var writer = settingsManager.GetWriter(nameof(NamingSettingsStore), eventSource);
        EnqueueArray(writer, "symbolSpecifications", NamingSettingsStorageCodec.EncodeSpecifications(graph.SymbolSpecifications));
        EnqueueArray(writer, "namingStyles", NamingSettingsStorageCodec.EncodeStyles(graph.NamingStyles));
        EnqueueArray(writer, "rules", NamingSettingsStorageCodec.EncodeRules(graph.Rules));

        // Preserve PendingApproval and error outcomes; the coordinator must verify publication before updating legacy/runtime state.
        return writer.RequestCommit(changeDescription);
    }

    public IDisposable SubscribeToChanges(Action<SettingsUpdate> handler)
        => settingsManager.GetReader().SubscribeToChanges(
            handler, _prefix + "symbolSpecifications", _prefix + "namingStyles", _prefix + "rules");

    private ImmutableArray<IReadOnlyDictionary<string, object>> ReadArray(ISettingsReader reader, string name)
    {
        var moniker = _prefix + name;
        var retrieval = reader.GetArray<IReadOnlyDictionary<string, object>>(
            moniker, SettingReadOptions.RequireValidation | SettingReadOptions.NoFallbackToDefault);
        if (retrieval is not { Outcome: SettingRetrievalOutcome.Success, Value: { } items })
            throw new InvalidDataException($"Unable to read naming setting '{moniker}': {retrieval.Outcome}. {retrieval.Message}");

        return items.ToImmutableArray();
    }

    private void EnqueueArray(ISettingsWriter writer, string name, ImmutableArray<IReadOnlyDictionary<string, object>> items)
    {
        var moniker = _prefix + name;
        // The native array writer removes values equal to registered defaults unless CustomizeUnconditionally is used.
        var change = writer.EnqueueArrayChange(moniker, items);
        if (change.Outcome is not (SettingChangeOutcome.PendingCommit or SettingChangeOutcome.NoOp))
            throw new InvalidDataException($"Unable to enqueue validated naming setting '{moniker}': {change.Outcome}. {change.Message}");
    }
}
