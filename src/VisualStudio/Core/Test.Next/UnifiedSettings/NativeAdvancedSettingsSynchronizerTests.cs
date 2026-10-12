// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.RpcContracts.UnifiedSettings;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NativeAdvancedSettingsSynchronizerTests
{
    [WpfFact]
    public async Task MissingValuesKeepTheTrueNullableDefaultsWithoutLegacyOrNativeWrites()
    {
        using var host = new Host();
        await host.InitializeAsync();
        Assert.Equal(4, host.Bindings.Length);
        Assert.Empty(host.Native.Values);
        Assert.Empty(host.Legacy.Values);
        Assert.Equal(0, host.Native.CommitCount);
        Assert.Equal(0, host.Legacy.WriteCount);
        foreach (var binding in host.Bindings)
            Assert.Equal(binding.Default, host.Refreshed[binding.Moniker]);
    }

    [WpfFact]
    public async Task ExistingDiagnosticsSubkeyWithoutLoggingValueDoesNotSeedANativeDefault()
    {
        using var host = new Host();
        using var diagnostics = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics");
        await host.InitializeAsync();
        Assert.False(host.Logging.ReadLegacy().HasValue);
        Assert.Equal(0, host.Native.CommitCount);
        Assert.Empty(diagnostics.GetValueNames());
    }

    [WpfTheory]
    [InlineData(null, "automatic")]
    [InlineData(false, "off")]
    [InlineData(true, "on")]
    public async Task BootstrapPreservesExplicitNullAndBooleansForEveryNullableOption(bool? value, string token)
    {
        using var host = new Host();
        foreach (var binding in host.NullableBindings)
            host.Legacy.Values[binding.LegacyKey!] = value;
        await host.InitializeAsync();
        foreach (var binding in host.NullableBindings)
        {
            Assert.Equal(token, JsonConvert.DeserializeObject<string>(host.Native.Effective(binding.Moniker)));
            Assert.Equal(value, host.Refreshed[binding.Moniker]);
            Assert.Equal(value, host.Legacy.Values[binding.LegacyKey!]);
        }
        Assert.Equal(0, host.Legacy.WriteCount);
    }

    [WpfFact]
    public async Task NativeOverridesWinOverLegacyValuesAndAutomaticWritesRealNull()
    {
        using var host = new Host();
        foreach (var binding in host.NullableBindings)
        {
            host.Legacy.Values[binding.LegacyKey!] = false;
            host.Native.Values[binding.Moniker] = "\"automatic\"";
        }
        await host.InitializeAsync();
        Assert.Equal(3, host.Legacy.WriteCount);
        foreach (var binding in host.NullableBindings)
            Assert.Null(host.Legacy.Values[binding.LegacyKey!]);
        Assert.Equal(0, host.Native.CommitCount);
    }

    [WpfFact]
    public async Task NativeEditsAutomaticAndResetPreserveValuesAcrossRestart()
    {
        using var host = new Host();
        await host.InitializeAsync();
        foreach (var binding in host.NullableBindings)
        {
            foreach (var (token, expected) in new (string, bool?)[] { ("off", false), ("on", true), ("automatic", null), ("on", true) })
            {
                host.Native.Change(binding.Moniker, JsonConvert.SerializeObject(token));
                await host.DrainAsync();
                Assert.Equal(expected, host.Legacy.Values[binding.LegacyKey!]);
                Assert.Equal(expected, host.Refreshed[binding.Moniker]);
            }
        }
        Assert.Empty(host.Native.Values);
        using var restarted = new Host();
        foreach (var pair in host.Legacy.Values)
            restarted.Legacy.Values.Add(pair.Key, pair.Value);
        await restarted.InitializeAsync();
        Assert.Empty(restarted.Native.Values);
        Assert.Equal(0, restarted.Legacy.WriteCount);
        Assert.All(restarted.NullableBindings, binding => Assert.Equal(true, restarted.Refreshed[binding.Moniker]));
    }

    [WpfFact]
    public async Task LegacyImportsRoamingAndDeletionUpdateNativeWhilePageIsClosedWithoutEcho()
    {
        using var host = new Host();
        await host.InitializeAsync();
        foreach (var binding in host.NullableBindings)
        {
            foreach (var value in new bool?[] { false, null, true })
            {
                await host.Legacy.SetValueAsync(binding.LegacyKey!, value, isMachineLocal: false);
                await host.DrainAsync();
                Assert.Equal(value, binding.Deserialize(host.Native.Effective(binding.Moniker)));
            }
            host.Legacy.Values.Remove(binding.LegacyKey!);
            await host.Legacy.NotifyAsync(binding.LegacyKey!);
            await host.DrainAsync();
            Assert.Equal("\"on\"", host.Native.Effective(binding.Moniker));
        }
        Assert.Equal(9, host.Legacy.WriteCount);
        Assert.Empty(host.Legacy.Values);
        Assert.Empty(host.Native.Values);
    }

    [WpfFact]
    public async Task ImportDuringBootstrapWinsOverTheLegacySnapshot()
    {
        using var host = new Host();
        var binding = host.NullableBindings.First();
        host.Legacy.Values[binding.LegacyKey!] = false;
        host.Native.BeforeSecondRead = moniker =>
        {
            if (moniker == binding.Moniker)
                host.Native.Values[moniker] = "\"automatic\"";
        };
        await host.InitializeAsync();
        Assert.Null(host.Legacy.Values[binding.LegacyKey!]);
        Assert.Equal(0, host.Native.CommitCount);
    }

    [WpfFact]
    public async Task LegacyDeletionDuringBootstrapDoesNotSeedANativeDefault()
    {
        using var host = new Host();
        var binding = host.NullableBindings.First();
        host.Legacy.Values[binding.LegacyKey!] = false;
        host.Native.BeforeSecondRead = moniker =>
        {
            if (moniker == binding.Moniker)
                host.Legacy.Values.Remove(binding.LegacyKey!);
        };
        await host.InitializeAsync();
        Assert.Equal(0, host.Native.CommitCount);
        Assert.Equal(0, host.Legacy.WriteCount);
        Assert.Equal(true, host.Refreshed[binding.Moniker]);
    }

    [WpfFact]
    public async Task FailedNativeCommitIsReportedAndDoesNotPoisonSubsequentLegacyUpdates()
    {
        using var host = new Host();
        await host.InitializeAsync();
        var binding = host.NullableBindings.First();
        host.Native.FailCommit = true;
        await host.Legacy.SetValueAsync(binding.LegacyKey!, false, false);
        await Assert.ThrowsAsync<InvalidOperationException>(host.DrainAsync);
        Assert.Empty(host.Native.Values);
        host.Native.FailCommit = false;
        await host.Legacy.NotifyAsync(binding.LegacyKey!);
        await host.DrainAsync();
        Assert.Equal("\"off\"", host.Native.Effective(binding.Moniker));
    }

    [WpfTheory]
    [InlineData("\"unsupported\"")]
    [InlineData("null")]
    [InlineData("true")]
    public async Task InvalidNativeNullableValueCannotOverwriteLegacyData(string invalid)
    {
        using var host = new Host();
        var binding = host.NullableBindings.First();
        host.Legacy.Values[binding.LegacyKey!] = false;
        host.Native.Values[binding.Moniker] = invalid;
        await Assert.ThrowsAsync<InvalidOperationException>(host.InitializeAsync);
        Assert.Equal(false, host.Legacy.Values[binding.LegacyKey!]);
        Assert.Equal(0, host.Legacy.WriteCount);
    }

    [WpfTheory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(2, false)]
    [InlineData(1L, false)]
    [InlineData("1", false)]
    public async Task LoggingBootstrapUsesTheProductionRegistryCodecWithoutRewritingTheRegistry(object stored, bool expected)
    {
        using var host = new Host();
        using (var key = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics"))
            key.SetValue("EnableFileLoggingForDiagnostics", stored, stored is long ? RegistryValueKind.QWord : stored is int ? RegistryValueKind.DWord : RegistryValueKind.String);
        await host.InitializeAsync();
        Assert.Equal(expected, host.Logging.Deserialize(host.Native.Effective(host.Logging.Moniker)));
        Assert.Equal(expected, host.Refreshed[host.Logging.Moniker]);
        using var preserved = host.RegistryRoot.OpenSubKey(@"Roslyn\Internal\Diagnostics");
        Assert.Equal(stored, preserved!.GetValue("EnableFileLoggingForDiagnostics"));
    }

    [WpfFact]
    public async Task NativeLoggingChangesAndResetWriteDwordsAndRefreshRuntime()
    {
        using var host = new Host();
        await host.InitializeAsync();
        foreach (var value in new[] { true, false })
        {
            host.Native.Change(host.Logging.Moniker, JsonConvert.SerializeObject(value));
            await host.DrainAsync();
            Assert.Equal(value, host.Refreshed[host.Logging.Moniker]);
            using var key = host.RegistryRoot.OpenSubKey(@"Roslyn\Internal\Diagnostics");
            Assert.Equal(RegistryValueKind.DWord, key!.GetValueKind("EnableFileLoggingForDiagnostics"));
            Assert.Equal(value ? 1 : 0, key.GetValue("EnableFileLoggingForDiagnostics"));
        }
        Assert.Empty(host.Native.Values);
    }

    [WpfFact]
    public async Task NativeLoggingBridgeRefreshesTheCachedFileEventSink()
    {
        var composition = EditorTestCompositions.EditorFeatures.GetCompositionConfiguration();
        var factory = RuntimeComposition.CreateRuntimeComposition(composition).CreateExportProviderFactory(joinableTaskFactory: null);
        await using var exportProvider = factory.CreateExportProvider();
        var options = exportProvider.GetExportedValue<IGlobalOptionService>();
        var threading = exportProvider.GetExportedValue<IThreadingContext>();
        var sink = new FileEventSink(options, threading);
        using var host = new Host();
        host.OnRefresh = (moniker, value) =>
        {
            var binding = host.Bindings.Single(binding => binding.Moniker == moniker);
            options.RefreshOption(binding.Key, value);
        };
        await host.InitializeAsync();
        Assert.False(sink.IsEnabled(FunctionId.Workspace_Project_GetCompilation));
        host.Native.Change(host.Logging.Moniker, "true");
        await host.DrainAsync();
        Assert.True(sink.IsEnabled(FunctionId.Workspace_Project_GetCompilation));
        host.Native.Change(host.Logging.Moniker, "false");
        await host.DrainAsync();
        Assert.False(sink.IsEnabled(FunctionId.Workspace_Project_GetCompilation));
    }

    [WpfFact]
    public async Task RegistryNotificationsObserveExternalChangesAndStopOnCancellation()
    {
        using var host = new Host();
        await host.InitializeAsync();
        using var cancellation = new CancellationTokenSource();
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var updated = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.OnRefresh = (moniker, value) =>
        {
            if (moniker == host.Logging.Moniker && value is true)
                updated.TrySetResult(true);
        };
        var watch = host.Registry.WatchChangesAsync(() =>
        {
            host.Synchronizer.OnRegistryChanged();
            ready.TrySetResult(true);
        }, cancellation.Token);
        try
        {
            Assert.Same(ready.Task, await Task.WhenAny(ready.Task, Task.Delay(TimeSpan.FromSeconds(30))));
            using (var key = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics"))
                key.SetValue("EnableFileLoggingForDiagnostics", 1, RegistryValueKind.DWord);
            Assert.Same(updated.Task, await Task.WhenAny(updated.Task, Task.Delay(TimeSpan.FromSeconds(30))));
            await host.DrainAsync();
            Assert.Equal("true", host.Native.Effective(host.Logging.Moniker));
        }
        finally
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => watch);
            await host.DrainAsync();
        }
    }

    private sealed class Host : IDisposable
    {
        private readonly JoinableTaskContext _context = new();
        private readonly ConcurrentQueue<Task> _tasks = new();
        private readonly string _registryPath = @"Software\Roslyn\UnifiedSettingsTests\" + Guid.NewGuid().ToString("N");
        public readonly LegacyStore Legacy = new();
        public readonly RegistryKey RegistryRoot;
        public readonly LocalUserRegistryOptionPersister Registry;
        public readonly ImmutableArray<NativeAdvancedSettingsSynchronizer.Binding> Bindings;
        public readonly NativeStore Native;
        public readonly NativeAdvancedSettingsSynchronizer Synchronizer;
        public readonly ConcurrentDictionary<string, object?> Refreshed = new();
        public Action<string, object?>? OnRefresh;
        public IEnumerable<NativeAdvancedSettingsSynchronizer.Binding> NullableBindings => Bindings.Where(binding => binding.IsNullable);
        public NativeAdvancedSettingsSynchronizer.Binding Logging => Bindings.Single(binding => !binding.IsNullable);

        public Host()
        {
            RegistryRoot = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_registryPath);
            Registry = new(RegistryRoot);
            var legacy = new VisualStudioSettingsOptionPersister((_, _) => { }, ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>>.Empty, Legacy);
            Bindings = NativeAdvancedSettingsService.CreateBindings(legacy, Registry);
            Native = new(Bindings);
            Synchronizer = new(Native, Legacy, Bindings, (key, value) =>
            {
                var moniker = Bindings.Single(binding => binding.Key == key).Moniker;
                Refreshed[moniker] = value;
                OnRefresh?.Invoke(moniker, value);
            }, _tasks.Enqueue, _context, CancellationToken.None);
        }

        public async Task InitializeAsync()
        {
            await Synchronizer.InitializeAsync();
            await DrainAsync();
        }

        public async Task DrainAsync()
        {
            while (_tasks.TryDequeue(out var task))
                await task;
        }

        public void Dispose()
        {
            Synchronizer.Dispose();
            _context.Dispose();
            RegistryRoot.Dispose();
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(_registryPath, throwOnMissingSubKey: false);
        }
    }

    private sealed class NativeStore : IUnifiedSettingsService, IDisposable
    {
        public readonly Dictionary<string, string> Values = [];
        private readonly Dictionary<string, string> _defaults;
        private readonly Dictionary<string, int> _reads = [];
        private IObserver<SettingsUpdate>? _observer;
        public Action<string>? BeforeSecondRead;
        public int CommitCount;
        public bool FailCommit;

        public NativeStore(IEnumerable<NativeAdvancedSettingsSynchronizer.Binding> bindings)
            => _defaults = bindings.ToDictionary(binding => binding.Moniker, binding => binding.Serialize(binding.Default));

        public string Effective(string moniker) => Values.TryGetValue(moniker, out var value) ? value : _defaults[moniker];
        private ReadResult Read(string moniker)
            => Values.TryGetValue(moniker, out var value) ? ReadResult.Success(moniker, new SettingValue(value.StartsWith("\"") ? SettingValueType.String : SettingValueType.Boolean, value)) : ReadResult.NotPersisted(moniker);

        public ValueTask<ReadResult> GetValueAsync(string moniker, ReadOptions options, CancellationToken cancellationToken)
        {
            Assert.True(options.HasFlag(ReadOptions.NoFallbackToDefault));
            Assert.True(options.HasFlag(ReadOptions.RequireValidation));
            Assert.True(options.HasFlag(ReadOptions.RequireRegistration));
            _reads.TryGetValue(moniker, out var reads);
            _reads[moniker] = ++reads;
            if (reads == 2)
                BeforeSecondRead?.Invoke(moniker);
            return new(Read(moniker));
        }

        public ValueTask<IReadOnlyCollection<ReadResult>> GetValuesAsync(IReadOnlyCollection<string> patterns, ReadOptions options, CancellationToken cancellationToken)
            => new(patterns.Select(Read).ToArray());

        public ValueTask<IDisposable> SubscribeAsync(IObserver<SettingsUpdate> observer, IReadOnlyCollection<string> patterns, ReadOptions options, CancellationToken cancellationToken)
        {
            _observer = observer;
            Notify();
            return new(this);
        }

        public ValueTask<CommitResult> RequestCommitAsync(ChangeSource source, SettingsCommit commit, CancellationToken cancellationToken)
        {
            if (FailCommit)
                throw new InvalidOperationException("Native commit unavailable");
            CommitCount++;
            foreach (var (moniker, json) in commit.Values)
                Change(moniker, json);
            return new(CommitResult.Success([]));
        }

        public void Change(string moniker, string json)
        {
            if (json == _defaults[moniker])
                Values.Remove(moniker);
            else
                Values[moniker] = json;
            Notify();
        }

        private void Notify() => _observer?.OnNext(new SettingsUpdate(_defaults.Keys.Select(Read).ToArray(), ReadOptions.NoFallbackToDefault));
        public void Dispose() => _observer = null;
    }

    private sealed class LegacyStore : ISettingsManager, ISettingsSubset
    {
        public readonly Dictionary<string, object?> Values = [];
        public int WriteCount;
        public event PropertyChangedAsyncEventHandler? SettingChangedAsync;
        public ISettingsSubset GetSubset(string pattern) => this;

        public GetValueResult TryGetValue<T>(string name, out T value)
        {
            var present = Values.TryGetValue(name, out var stored);
            value = stored is null ? default! : (T)stored;
            return present ? GetValueResult.Success : GetValueResult.Missing;
        }

        public async Task SetValueAsync(string name, object? value, bool isMachineLocal)
        {
            Assert.False(isMachineLocal);
            Values[name] = value;
            WriteCount++;
            await NotifyAsync(name);
        }

        public async Task NotifyAsync(string name)
        {
            if (SettingChangedAsync is { } handlers)
            {
                foreach (PropertyChangedAsyncEventHandler handler in handlers.GetInvocationList())
                    await handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public ISettingsList GetOrCreateList(string name, bool isMachineLocal) => throw new NotImplementedException();
        public T GetValueOrDefault<T>(string name, T defaultValue = default!) => throw new NotImplementedException();
        public string[] NamesStartingWith(string prefix) => throw new NotImplementedException();
        public void SetOnlineStore(IAsyncStringStorage store) => throw new NotImplementedException();
        public void SetSharedStore(IAsyncStringStorage store) => throw new NotImplementedException();
    }
}
