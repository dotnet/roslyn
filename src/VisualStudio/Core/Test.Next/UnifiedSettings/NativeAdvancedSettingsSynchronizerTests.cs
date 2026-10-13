// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NativeAdvancedSettingsSynchronizerTests
{
    [WpfFact]
    public async Task MissingLoggingValueKeepsTheFalseDefaultWithoutRegistryOrNativeWrites()
    {
        using var host = new Host();
        await host.InitializeAsync();
        Assert.Single(host.Bindings);
        Assert.Empty(host.Native.Values);
        Assert.False(host.Logging.ReadLegacy().HasValue);
        Assert.Equal(0, host.Native.CommitCount);
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

    [WpfFact]
    public async Task NativeLoggingOverrideWinsOverRegistryValue()
    {
        using var host = new Host();
        using var key = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics");
        key.SetValue("EnableFileLoggingForDiagnostics", 0, RegistryValueKind.DWord);
        host.Native.Values[host.Logging.Moniker] = "true";
        await host.InitializeAsync();
        Assert.Equal(1, key.GetValue("EnableFileLoggingForDiagnostics"));
        Assert.Equal(true, host.Refreshed[host.Logging.Moniker]);
        Assert.Equal(0, host.Native.CommitCount);
    }

    [WpfFact]
    public async Task RegistryChangesAndDeletionUpdateNativeWithoutRecreatingTheValue()
    {
        using var host = new Host();
        await host.InitializeAsync();
        using var key = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics");
        foreach (var value in new[] { 1, 0, 1 })
        {
            key.SetValue("EnableFileLoggingForDiagnostics", value, RegistryValueKind.DWord);
            host.Synchronizer.OnRegistryChanged();
            await host.DrainAsync();
            Assert.Equal(value == 1, host.Logging.Deserialize(host.Native.Effective(host.Logging.Moniker)));
            Assert.Equal(value == 1, host.Refreshed[host.Logging.Moniker]);
        }
        key.DeleteValue("EnableFileLoggingForDiagnostics");
        host.Synchronizer.OnRegistryChanged();
        await host.DrainAsync();
        Assert.Empty(key.GetValueNames());
        Assert.Empty(host.Native.Values);
        Assert.Equal(false, host.Refreshed[host.Logging.Moniker]);
    }

    [WpfFact]
    public async Task ImportDuringBootstrapWinsOverTheLegacySnapshot()
    {
        using var host = new Host();
        using var key = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics");
        key.SetValue("EnableFileLoggingForDiagnostics", 1, RegistryValueKind.DWord);
        host.Native.BeforeSecondRead = moniker =>
        {
            if (moniker == host.Logging.Moniker)
                host.Native.Values[moniker] = "false";
        };
        await host.InitializeAsync();
        Assert.Equal(0, key.GetValue("EnableFileLoggingForDiagnostics"));
        Assert.Equal(0, host.Native.CommitCount);
    }

    [WpfFact]
    public async Task RegistryDeletionDuringBootstrapDoesNotSeedANativeDefault()
    {
        using var host = new Host();
        using var key = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics");
        key.SetValue("EnableFileLoggingForDiagnostics", 1, RegistryValueKind.DWord);
        host.Native.BeforeSecondRead = moniker =>
        {
            if (moniker == host.Logging.Moniker)
                key.DeleteValue("EnableFileLoggingForDiagnostics");
        };
        await host.InitializeAsync();
        Assert.Equal(0, host.Native.CommitCount);
        Assert.Empty(key.GetValueNames());
        Assert.Equal(false, host.Refreshed[host.Logging.Moniker]);
    }

    [WpfFact]
    public async Task FailedNativeCommitIsReportedAndDoesNotPoisonSubsequentRegistryUpdates()
    {
        using var host = new Host();
        await host.InitializeAsync();
        using var key = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics");
        key.SetValue("EnableFileLoggingForDiagnostics", 1, RegistryValueKind.DWord);
        host.Native.FailCommit = true;
        host.Synchronizer.OnRegistryChanged();
        await Assert.ThrowsAsync<InvalidOperationException>(host.DrainAsync);
        Assert.Empty(host.Native.Values);
        host.Native.FailCommit = false;
        host.Synchronizer.OnRegistryChanged();
        await host.DrainAsync();
        Assert.Equal("true", host.Native.Effective(host.Logging.Moniker));
    }

    [WpfTheory]
    [InlineData("\"unsupported\"")]
    [InlineData("null")]
    [InlineData("1")]
    public async Task InvalidNativeLoggingValueCannotOverwriteRegistryData(string invalid)
    {
        using var host = new Host();
        using var key = host.RegistryRoot.CreateSubKey(@"Roslyn\Internal\Diagnostics");
        key.SetValue("EnableFileLoggingForDiagnostics", 1, RegistryValueKind.DWord);
        host.Native.Values[host.Logging.Moniker] = invalid;
        await Assert.ThrowsAsync<InvalidOperationException>(host.InitializeAsync);
        Assert.Equal(1, key.GetValue("EnableFileLoggingForDiagnostics"));
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
        public readonly RegistryKey RegistryRoot;
        public readonly LocalUserRegistryOptionPersister Registry;
        public readonly ImmutableArray<NativeAdvancedSettingsSynchronizer.Binding> Bindings;
        public readonly NativeStore Native;
        public readonly NativeAdvancedSettingsSynchronizer Synchronizer;
        public readonly ConcurrentDictionary<string, object?> Refreshed = new();
        public Action<string, object?>? OnRefresh;
        public NativeAdvancedSettingsSynchronizer.Binding Logging => Bindings.Single();

        public Host()
        {
            RegistryRoot = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_registryPath);
            Registry = new(RegistryRoot);
            Bindings = NativeAdvancedSettingsService.CreateBindings(Registry);
            Native = new(Bindings);
            Synchronizer = new(Native, Bindings, (key, value) =>
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
}
