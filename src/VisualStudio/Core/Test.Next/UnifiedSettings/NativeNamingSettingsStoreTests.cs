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
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.RpcContracts.UnifiedSettings;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NativeNamingSettingsStoreTests
{
    [Theory]
    [InlineData(LanguageNames.CSharp, "csharp")]
    [InlineData(LanguageNames.VisualBasic, "basic")]
    public async Task WholeGraphRoundTripsThroughThePublicBroker(string language, string segment)
    {
        var native = new Broker();
        var store = new NativeNamingSettingsStore(native, language);
        var graph = NamingSettingsGraph.Default;

        var result = await store.WriteAsync(graph, "Naming test", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, native.Commits);
        Assert.Equal(3, native.Values.Count);
        Assert.All(native.Values.Keys, moniker => Assert.StartsWith("languages." + segment + ".codeStyle.naming.", moniker));
        Assert.True(graph.ConfigurationEquals(await store.ReadAsync(CancellationToken.None)));
        Assert.Equal(ReadOptions.NoRequirements, native.LastReadOptions);
    }

    [Fact]
    public async Task AnUnregisteredGraphDoesNotBecomeSuccessfulDefaults()
    {
        var store = new NativeNamingSettingsStore(new Broker(), LanguageNames.CSharp);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ResetReadsEffectiveNativeDefaultsWithoutRecreatingOverrides()
    {
        var native = new Broker();
        var store = new NativeNamingSettingsStore(native, LanguageNames.CSharp);
        await store.WriteAsync(NamingSettingsGraph.Default, "Naming test", CancellationToken.None);
        foreach (var pair in native.Values)
            native.Defaults.Add(pair.Key, pair.Value);
        native.Values.Clear();

        var graph = await store.ReadAsync(CancellationToken.None);

        Assert.True(NamingSettingsGraph.Default.ConfigurationEquals(graph));
        Assert.Empty(native.Values);
        Assert.Equal(1, native.Commits);
    }

    [Theory]
    [InlineData(CommitResultCode.PendingApproval)]
    [InlineData(CommitResultCode.ChangeError)]
    public async Task UnpublishedCommitOutcomesArePreserved(CommitResultCode code)
    {
        var native = new Broker { Outcome = code };
        var store = new NativeNamingSettingsStore(native, LanguageNames.CSharp);

        var result = await store.WriteAsync(NamingSettingsGraph.Default, "Naming test", CancellationToken.None);

        Assert.Equal(code, result.ResultCode);
        Assert.Empty(native.Values);
        Assert.Equal(0, native.Reads);
    }

    [Fact]
    public async Task RemovingReferencedInterfaceIsRejectedBeforeRequestingACommit()
    {
        var native = new Broker();
        var store = new NativeNamingSettingsStore(native, LanguageNames.CSharp);
        var graph = NamingSettingsGraph.Default;
        var specification = graph.SymbolSpecifications.Single(item => (string)item["id"] == "interface");
        var invalid = new NamingSettingsGraph(graph.SymbolSpecifications.Remove(specification), graph.NamingStyles, graph.Rules);

        await Assert.ThrowsAsync<InvalidDataException>(() => store.WriteAsync(invalid, "Invalid edit", CancellationToken.None));

        Assert.Equal(0, native.Commits);
        Assert.Empty(native.Values);
    }

    [Fact]
    public async Task PartialPublicationDoesNotReportSuccess()
    {
        var native = new Broker { SkipRules = true };
        var store = new NativeNamingSettingsStore(native, LanguageNames.CSharp);

        await Assert.ThrowsAsync<InvalidDataException>(() => store.WriteAsync(NamingSettingsGraph.Default, "Naming test", CancellationToken.None));
    }

    [Fact]
    public async Task DifferentValidPublicationDoesNotReportSuccess()
    {
        var native = new Broker();
        var store = new NativeNamingSettingsStore(native, LanguageNames.CSharp);
        var original = NamingSettingsGraph.Default;
        await store.WriteAsync(original, "Naming test", CancellationToken.None);
        native.SkipAllWrites = true;
        var reordered = new NamingSettingsGraph(original.SymbolSpecifications, original.NamingStyles, original.Rules.Reverse().ToImmutableArray());

        await Assert.ThrowsAsync<InvalidDataException>(() => store.WriteAsync(reordered, "Reorder", CancellationToken.None));

        Assert.True(original.ConfigurationEquals(await store.ReadAsync(CancellationToken.None)));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[{\"id\":\"one\",\"id\":\"two\"}]")]
    [InlineData("[{\"id\":null}]")]
    [InlineData("[{\"symbolKinds\":[\"class\",1]}]")]
    public async Task MalformedNativeArraysAreRejected(string json)
    {
        var native = new Broker();
        var store = new NativeNamingSettingsStore(native, LanguageNames.CSharp);
        await store.WriteAsync(NamingSettingsGraph.Default, "Naming test", CancellationToken.None);
        native.Values["languages.csharp.codeStyle.naming.symbolSpecifications"] = json;

        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task BrokenNativeReferencesAreRejectedWithoutWriting()
    {
        var native = new Broker();
        var store = new NativeNamingSettingsStore(native, LanguageNames.CSharp);
        await store.WriteAsync(NamingSettingsGraph.Default, "Naming test", CancellationToken.None);
        native.Values["languages.csharp.codeStyle.naming.rules"] = "[{\"symbolSpecificationId\":\"unknown\",\"namingStyleId\":\"unknown\",\"severity\":\"warning\"}]";

        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync(CancellationToken.None));

        Assert.Equal(1, native.Commits);
    }

    [Fact]
    public async Task CancellationDoesNotRequestACommit()
    {
        var native = new Broker();
        var store = new NativeNamingSettingsStore(native, LanguageNames.CSharp);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.WriteAsync(NamingSettingsGraph.Default, "Naming test", cancellation.Token));

        Assert.Equal(0, native.Commits);
    }

    private sealed class Broker : IUnifiedSettingsService
    {
        public readonly Dictionary<string, string> Values = [];
        public readonly Dictionary<string, string> Defaults = [];
        public CommitResultCode Outcome = CommitResultCode.Success;
        public bool SkipRules;
        public bool SkipAllWrites;
        public int Commits;
        public int Reads;
        public ReadOptions LastReadOptions;

        public ValueTask<IReadOnlyCollection<ReadResult>> GetValuesAsync(IReadOnlyCollection<string> monikers, ReadOptions options, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            LastReadOptions = options;
            return new(monikers.Select(moniker => (Values.TryGetValue(moniker, out var json)
                || (!options.HasFlag(ReadOptions.NoFallbackToDefault) && Defaults.TryGetValue(moniker, out json)))
                ? ReadResult.Success(moniker, new SettingValue(SettingValueType.ObjectArray, json))
                : ReadResult.NotPersisted(moniker)).ToArray());
        }

        public ValueTask<CommitResult> RequestCommitAsync(ChangeSource source, SettingsCommit commit, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Commits++;
            if (Outcome == CommitResultCode.Success && !SkipAllWrites)
            {
                foreach (var (moniker, json) in commit.Values)
                {
                    if (!SkipRules || !moniker.EndsWith(".rules", StringComparison.Ordinal))
                        Values[moniker] = json;
                }
            }
            return new(new CommitResult(Outcome, []));
        }

        public ValueTask<ReadResult> GetValueAsync(string moniker, ReadOptions options, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask<IDisposable> SubscribeAsync(IObserver<SettingsUpdate> observer, IReadOnlyCollection<string> monikers, ReadOptions options, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
