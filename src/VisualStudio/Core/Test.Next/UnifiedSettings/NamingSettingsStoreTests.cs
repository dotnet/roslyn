// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsStoreTests
{
    [Theory]
    [InlineData(LanguageNames.CSharp, "languages.csharp.editorConfig.naming.")]
    [InlineData(LanguageNames.VisualBasic, "languages.basic.codeStyle.naming.")]
    public void WritesAllValidatedCollectionsUsingOnePrivateWriter(string language, string prefix)
    {
        var manager = new SettingsManagerMock();
        var store = new NamingSettingsStore(manager, language);
        var graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var eventSource = Guid.NewGuid();

        var result = store.WriteGraph(graph, "Naming settings change", eventSource);

        Assert.Equal(SettingCommitOutcome.Success, result.Outcome);
        Assert.Equal(eventSource, manager.LastEventSource);
        Assert.Equal(1, manager.LastWriter!.CommitCount);
        Assert.Equal(
            new[] { prefix + "symbolSpecifications", prefix + "namingStyles", prefix + "rules" },
            manager.LastWriter.QueuedMonikers);
    }

    [Theory]
    [InlineData(SettingCommitOutcome.PendingApproval)]
    [InlineData(SettingCommitOutcome.InternalError)]
    [InlineData(SettingCommitOutcome.NoChangesQueued)]
    public void CommitOutcomeIsNotReplacedBySuccess(SettingCommitOutcome outcome)
    {
        var manager = new SettingsManagerMock { CommitOutcome = outcome };
        var store = new NamingSettingsStore(manager, LanguageNames.CSharp);

        var result = store.WriteGraph(NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default), "Naming settings change", Guid.NewGuid());

        Assert.Equal(outcome, result.Outcome);
        Assert.Equal("commit detail", result.Message);
    }

    [Theory]
    [InlineData(SettingChangeOutcome.InvalidValue)]
    [InlineData(SettingChangeOutcome.Denied)]
    [InlineData(SettingChangeOutcome.PendingCommitWithoutValidation)]
    public void RejectedArrayDoesNotCommitEarlierQueuedArrays(SettingChangeOutcome outcome)
    {
        var manager = new SettingsManagerMock { RejectedArrayOutcome = outcome };
        var store = new NamingSettingsStore(manager, LanguageNames.CSharp);
        var graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);

        var error = Assert.Throws<InvalidDataException>(() => store.WriteGraph(graph, "Naming settings change", Guid.NewGuid()));

        Assert.Contains("namingStyles", error.Message);
        Assert.Equal(0, manager.LastWriter!.CommitCount);
        var firstWriter = manager.LastWriter;
        manager.RejectedArrayOutcome = null;
        store.WriteGraph(graph, "Naming settings change", Guid.NewGuid());
        Assert.NotSame(firstWriter, manager.LastWriter);
        Assert.Equal(0, firstWriter.CommitCount);
    }

    [Fact]
    public void UnchangedArrayDoesNotRejectOtherQueuedChanges()
    {
        var manager = new SettingsManagerMock { RejectedArrayOutcome = SettingChangeOutcome.NoOp };
        var store = new NamingSettingsStore(manager, LanguageNames.CSharp);
        var graph = NamingSettingsGraph.Default;
        var rules = graph.Rules.Select(item => item.ToDictionary(pair => pair.Key, pair => pair.Value)).ToArray();
        rules[0]["severity"] = "warning";
        var updated = new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles, rules.Cast<IReadOnlyDictionary<string, object>>().ToImmutableArray());

        Assert.Equal(SettingCommitOutcome.Success, store.WriteGraph(updated, "Naming", Guid.NewGuid()).Outcome);
        Assert.Equal(ReportDiagnostic.Warn, manager.Reader.Graph.ToPreferences().Rules.NamingRules[0].EnforcementLevel);
    }

    [Fact]
    public void InvalidGraphIsRejectedBeforeCreatingWriter()
    {
        var manager = new SettingsManagerMock();
        var store = new NamingSettingsStore(manager, LanguageNames.CSharp);
        var original = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);
        var invalid = new NamingSettingsGraph([], original.NamingStyles, original.Rules);

        Assert.Throws<InvalidDataException>(() => store.WriteGraph(invalid, "Naming settings change", Guid.NewGuid()));
        Assert.Null(manager.LastWriter);
    }

    [Fact]
    public void ReadsValidatedWholeGraphWithoutInvalidValueFallback()
    {
        var manager = new SettingsManagerMock();
        var store = new NamingSettingsStore(manager, LanguageNames.CSharp);

        Assert.Equal(NamingStylePreferences.Default, store.ReadGraph().ToPreferences());
        Assert.All(manager.Reader.ReadOptions, options => Assert.Equal(
            SettingReadOptions.RequireValidation | SettingReadOptions.NoFallbackToDefault, options));
    }

    [Theory]
    [InlineData(SettingRetrievalOutcome.FailedValidation)]
    [InlineData(SettingRetrievalOutcome.NotRegistered)]
    [InlineData(SettingRetrievalOutcome.InvalidType)]
    [InlineData(SettingRetrievalOutcome.NotSupportedInClassicMode)]
    public void ReadFailuresAreExplicitInsteadOfReturningDefaults(SettingRetrievalOutcome outcome)
    {
        var manager = new SettingsManagerMock();
        manager.Reader.Outcome = outcome;
        var store = new NamingSettingsStore(manager, LanguageNames.CSharp);

        var error = Assert.Throws<InvalidDataException>(() => store.ReadGraph());

        Assert.Contains(outcome.ToString(), error.Message);
        Assert.Contains("reader detail", error.Message);
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, "languages.csharp.editorConfig.naming.rules")]
    [InlineData(LanguageNames.VisualBasic, "languages.basic.codeStyle.naming.rules")]
    public void DefaultCollectionsAreNotCustomizedAndRevertingDeletesTheOverride(string language, string rulesMoniker)
    {
        var manager = new SettingsManagerMock();
        var store = new NamingSettingsStore(manager, language);
        store.WriteGraph(NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default), "Unchanged defaults", Guid.NewGuid());
        Assert.Empty(manager.CustomizedArrays);

        var graph = NamingSettingsGraph.Default;
        var rule = graph.Rules[0].ToDictionary(pair => pair.Key, pair => pair.Value);
        rule["severity"] = "warning";
        var edited = new NamingSettingsGraph(graph.SymbolSpecifications, graph.NamingStyles, graph.Rules.SetItem(0, rule));
        store.WriteGraph(edited, "Edit one rule", Guid.NewGuid());
        Assert.Equal(rulesMoniker, Assert.Single(manager.CustomizedArrays).Key);

        store.WriteGraph(NamingSettingsGraph.Default, "Revert to defaults", Guid.NewGuid());
        Assert.Empty(manager.CustomizedArrays);
        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(manager.Reader.Graph));
    }

    internal sealed class SettingsManagerMock : ISettingsManager
    {
        public readonly SettingsReaderMock Reader = new();
        public SettingsWriterMock? LastWriter;
        public Guid LastEventSource;
        public SettingCommitOutcome CommitOutcome = SettingCommitOutcome.Success;
        public SettingChangeOutcome? RejectedArrayOutcome;
        public bool PublishCommit = true;
        public Action? BeforeCommit;
        public readonly Dictionary<string, ImmutableArray<IReadOnlyDictionary<string, object>>> CustomizedArrays = [];

        public ISettingsReader GetReader() => Reader;

        public ISettingsWriter GetWriter(string callerName) => GetWriter(callerName, Guid.Empty);

        public ISettingsWriter GetWriter(string callerName, Guid eventSource)
        {
            LastEventSource = eventSource;
            return LastWriter = new SettingsWriterMock(CommitOutcome, RejectedArrayOutcome, PublishCommit ? Reader : null, CustomizedArrays, () => BeforeCommit?.Invoke());
        }
    }

    internal class SettingsReaderMock : ISettingsReader
    {
        public Action<SettingsUpdate>? ChangeHandler;
        public string[] SubscriptionMonikers = [];
        public SettingRetrievalOutcome Outcome = SettingRetrievalOutcome.Success;
        public readonly List<SettingReadOptions> ReadOptions = [];
        public NamingSettingsGraph Graph = NamingSettingsGraph.FromPreferences(NamingStylePreferences.Default);

        public SettingRetrieval<IReadOnlyList<T>> GetArray<T>(string moniker, SettingReadOptions readOptions = SettingReadOptions.RequireValidation) where T : notnull
        {
            ReadOptions.Add(readOptions);
            if (Outcome != SettingRetrievalOutcome.Success)
                return new(Outcome, "reader detail", null);

            var graph = Graph;
            var items = moniker.Substring(moniker.LastIndexOf('.') + 1) switch
            {
                "symbolSpecifications" => graph.SymbolSpecifications.Select(item =>
                {
                    var stored = item.ToDictionary(pair => pair.Key, pair => pair.Value);
                    foreach (var name in new[] { "symbolKinds", "accessibilities", "requiredModifiers" })
                    {
                        if (stored[name] is IEnumerable<string> tokens)
                            stored[name] = string.Join(",", tokens);
                    }
                    return (IReadOnlyDictionary<string, object>)stored;
                }).ToImmutableArray(),
                "namingStyles" => graph.NamingStyles,
                "rules" => graph.Rules,
                _ => throw new InvalidOperationException(moniker),
            };
            return new(Outcome, null, items.Cast<T>().ToArray());
        }

        public IReadOnlyList<T> GetArrayOrThrow<T>(string moniker) where T : notnull => throw new NotImplementedException();
        public SettingRetrieval<T> GetValue<T>(string moniker, SettingReadOptions readOptions = SettingReadOptions.RequireValidation) where T : notnull => throw new NotImplementedException();
        public SettingRetrieval<object> GetValue(string moniker, Type targetType, SettingReadOptions readOptions = SettingReadOptions.RequireValidation) => throw new NotImplementedException();
        public T GetValueOrThrow<T>(string moniker) where T : notnull => throw new NotImplementedException();
        public object GetValueOrThrow(string moniker, Type targetType) => throw new NotImplementedException();
        public IDisposable SubscribeToChanges(Action<SettingsUpdate> handler, params string[] monikerPatterns)
        {
            ChangeHandler += handler;
            SubscriptionMonikers = monikerPatterns;
            return new Subscription(() => ChangeHandler -= handler);
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }

    internal sealed class SettingsWriterMock(SettingCommitOutcome commitOutcome, SettingChangeOutcome? rejectedArrayOutcome, SettingsReaderMock? backingReader,
        Dictionary<string, ImmutableArray<IReadOnlyDictionary<string, object>>> customizedArrays, Action beforeCommit) : SettingsReaderMock, ISettingsWriter
    {
        public readonly List<string> QueuedMonikers = [];
        private readonly Dictionary<string, ImmutableArray<IReadOnlyDictionary<string, object>>> _queuedArrays = [];
        public int CommitCount;

        public SettingChangeResult EnqueueArrayChange<T>(string moniker, IReadOnlyList<T> value) where T : notnull
        {
            if (moniker.EndsWith(".namingStyles", StringComparison.Ordinal) && rejectedArrayOutcome is { } outcome)
                return new(outcome, false, "enqueue detail");

            QueuedMonikers.Add(moniker);
            _queuedArrays.Add(moniker.Substring(moniker.LastIndexOf('.') + 1), value.Cast<IReadOnlyDictionary<string, object>>().ToImmutableArray());
            return new(SettingChangeOutcome.PendingCommit, true, null);
        }

        public SettingCommitResult RequestCommit(string changeDescription)
        {
            beforeCommit();
            CommitCount++;
            if (commitOutcome == SettingCommitOutcome.Success && backingReader is not null)
            {
                foreach (var moniker in QueuedMonikers)
                {
                    var name = moniker.Substring(moniker.LastIndexOf('.') + 1);
                    using var manifest = NamingSettingsManifestTests.Load(moniker.StartsWith("languages.csharp.", StringComparison.Ordinal)
                        ? "csharpUnifiedSettings.registration.json" : "visualBasicUnifiedSettings.registration.json");
                    var defaults = NamingSettingsManifestTests.ReadDefaults(manifest.RootElement.GetProperty("properties").GetProperty(moniker));
                    var value = _queuedArrays[name];
                    // Native JSON-value comparison preserves object-property order when reserializing values.
                    if (JsonSerializer.Serialize(value) == JsonSerializer.Serialize(defaults))
                    {
                        customizedArrays.Remove(moniker);
                    }
                    else
                    {
                        customizedArrays[moniker] = value;
                    }
                }
                backingReader.Graph = new NamingSettingsGraph(NamingSettingsStorageSerializer.DeserializeSpecifications(_queuedArrays["symbolSpecifications"]),
                    _queuedArrays.TryGetValue("namingStyles", out var styles) ? styles : backingReader.Graph.NamingStyles, _queuedArrays["rules"]);
            }
            return new(commitOutcome, "commit detail");
        }

        public SettingCommitResult Commit(string changeDescription) => throw new NotImplementedException();
        public SettingChangeResult EnqueueChange<T>(string moniker, T value) where T : notnull => throw new NotImplementedException();
        public SettingChangeResult EnqueueChange<T>(string moniker, T value, SettingWriteOptions options) where T : notnull => throw new NotImplementedException();
    }
}
