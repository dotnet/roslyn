// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.RpcContracts.UnifiedSettings;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NativeCodeStyleSettingsSynchronizerTests
{
    [WpfTheory]
    [InlineData(LanguageNames.CSharp, "legacyWrite")]
    [InlineData(LanguageNames.CSharp, "seedCommit")]
    [InlineData(LanguageNames.CSharp, "invalidScalar")]
    [InlineData(LanguageNames.VisualBasic, "legacyWrite")]
    [InlineData(LanguageNames.VisualBasic, "seedCommit")]
    [InlineData(LanguageNames.VisualBasic, "invalidScalar")]
    public async Task ServiceLifetimeRecoversFromFailedBootstrap(string language, string failure)
    {
        using var cancellation = new CancellationTokenSource();
        var ready = new TaskCompletionSource<Host>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failures = new List<Task>();
        Host? failedHost = null;
        var attempts = 0;
        var lifetime = NativeCodeStyleSettingsService.RunAsync(async () =>
        {
            using var host = new Host(language);
            if (++attempts == 1)
            {
                failedHost = host;
                switch (failure)
                {
                    case "legacyWrite":
                        host.Native.Values[host.Binding.Moniker] = "true";
                        host.Legacy.FailNextWrite = true;
                        break;
                    case "seedCommit":
                        host.Legacy.Value = new CodeStyleOption2<bool>(true, NotificationOption2.Warning).ToXElement().ToString();
                        host.Native.FailNextCommit = true;
                        break;
                    case "invalidScalar":
                        host.Native.Values[host.Binding.SeverityMoniker] = "invalid";
                        break;
                    default:
                        throw new InvalidOperationException(failure);
                }
            }
            await host.InitializeAsync();
            ready.SetResult(host);
            await Task.Delay(Timeout.Infinite, cancellation.Token);
        }, failures.Add, cancellation.Token);

        Host recovered;
        try
        {
            recovered = await ready.Task;
            Assert.Equal(2, attempts);
            await Assert.ThrowsAnyAsync<Exception>(() => Assert.Single(failures));
            Assert.False(failedHost!.Native.HasSubscription);
            recovered.Native.Change(recovered.Binding.Moniker, "true");
            await recovered.DrainAsync();
            Assert.True(recovered.ReadLegacy().Value);
            await recovered.Legacy.ImportAsync(new CodeStyleOption2<bool>(false, NotificationOption2.Error).ToXElement().ToString());
            await recovered.DrainAsync();
            Assert.Equal("false", recovered.Native.Effective(recovered.Binding.Moniker));
            Assert.Equal("error", recovered.Native.Effective(recovered.Binding.SeverityMoniker));
        }
        finally
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lifetime);
        }
        Assert.False(recovered.Native.HasSubscription);
        _ = Assert.Single(failures);
    }

    [WpfFact]
    public async Task ServiceDisposalCancelsBootstrapRetry()
    {
        using var cancellation = new CancellationTokenSource();
        var failed = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var lifetime = NativeCodeStyleSettingsService.RunAsync(() =>
        {
            attempts++;
            throw new InvalidOperationException("Unavailable settings service.");
        }, task => failed.SetResult(task), cancellation.Token);

        var failure = await failed.Task;
        await Assert.ThrowsAsync<InvalidOperationException>(() => failure);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lifetime);
        Assert.Equal(1, attempts);
    }

    [WpfTheory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task BootstrapMigratesCompoundXmlUsingExistingHiddenSeverityContract(string language)
    {
        using var host = new Host(language);
        host.Legacy.Value = new CodeStyleOption2<bool>(true, NotificationOption2.None).ToXElement().ToString();
        await host.InitializeAsync();
        Assert.Equal("true", host.Native.Effective(host.Binding.Moniker));
        Assert.Equal("silent", host.Native.Effective(host.Binding.SeverityMoniker));
        Assert.Equal(0, host.Legacy.WriteCount);
    }

    [WpfTheory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task ExistingNativePreferenceWinsWithoutResettingLegacySeverity(string language)
    {
        using var host = new Host(language);
        host.Legacy.Value = new CodeStyleOption2<bool>(true, NotificationOption2.Warning).ToXElement().ToString();
        host.Native.Values[host.Binding.Moniker] = "true";
        await host.InitializeAsync();
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Warning), host.ReadLegacy());
        Assert.Equal("warning", host.Native.Effective(host.Binding.SeverityMoniker));
    }

    [WpfFact]
    public async Task MissingStoresUseNativeProfileDefaultsWithoutPersistingDefaults()
    {
        using var host = new Host(LanguageNames.CSharp);
        host.Native.Defaults[host.Binding.Moniker] = "true";
        host.Native.Defaults[host.Binding.SeverityMoniker] = "warning";
        await host.InitializeAsync();
        Assert.Empty(host.Native.Values);
        Assert.Null(host.Legacy.Value);
        Assert.Equal(0, host.Legacy.WriteCount);
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Warning), host.Refreshed);
    }

    [WpfFact]
    public async Task NativePreferenceAndSeverityEditsAndResetPreserveTheSibling()
    {
        using var host = new Host(LanguageNames.CSharp);
        host.Legacy.Value = new CodeStyleOption2<bool>(true, NotificationOption2.Warning).ToXElement().ToString();
        await host.InitializeAsync();
        host.Native.Change(host.Binding.SeverityMoniker, "error");
        await host.DrainAsync();
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Error), host.ReadLegacy());
        host.Native.Change(host.Binding.Moniker, "false");
        await host.DrainAsync();
        Assert.Equal(new CodeStyleOption2<bool>(false, NotificationOption2.Error), host.ReadLegacy());
        Assert.False(host.Native.Values.ContainsKey(host.Binding.Moniker));
        host.Native.Change(host.Binding.SeverityMoniker, "silent");
        await host.DrainAsync();
        Assert.Equal(new CodeStyleOption2<bool>(false, NotificationOption2.Silent), host.ReadLegacy());
    }

    [WpfFact]
    public async Task IncomingLegacyImportUpdatesBothNativeScalarsWhileThePageIsClosed()
    {
        using var host = new Host(LanguageNames.VisualBasic);
        await host.InitializeAsync();
        await host.Legacy.ImportAsync(new CodeStyleOption2<bool>(true, NotificationOption2.Error).ToXElement().ToString());
        await host.DrainAsync();
        Assert.Equal("true", host.Native.Effective(host.Binding.Moniker));
        Assert.Equal("error", host.Native.Effective(host.Binding.SeverityMoniker));
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Error), host.Refreshed);
    }

    [WpfFact]
    public async Task FailedLegacyImportCommitRetriesAutomatically()
    {
        using var host = new Host(LanguageNames.CSharp);
        await host.InitializeAsync();
        var imported = new CodeStyleOption2<bool>(true, NotificationOption2.Error).ToXElement().ToString();
        host.Native.FailNextCommit = true;

        await host.Legacy.ImportAsync(imported);
        await host.DrainAsync();
        Assert.Equal("true", host.Native.Effective(host.Binding.Moniker));
        Assert.Equal("error", host.Native.Effective(host.Binding.SeverityMoniker));
        Assert.Equal(2, host.Native.CommitCount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Assert.Single(host.Failures));
    }

    [WpfTheory]
    [InlineData(LanguageNames.CSharp, true, false)]
    [InlineData(LanguageNames.CSharp, false, true)]
    [InlineData(LanguageNames.CSharp, true, true)]
    [InlineData(LanguageNames.VisualBasic, true, false)]
    [InlineData(LanguageNames.VisualBasic, false, true)]
    [InlineData(LanguageNames.VisualBasic, true, true)]
    public async Task FailedNativeUpdateCanBeRetriedWithoutLosingEitherComponent(string language, bool changePreference, bool changeSeverity)
    {
        using var host = new Host(language);
        var initial = new CodeStyleOption2<bool>(false, NotificationOption2.Warning);
        host.Legacy.Value = initial.ToXElement().ToString();
        await host.InitializeAsync();
        var commits = host.Native.CommitCount;
        if (changePreference)
            host.Native.Values[host.Binding.Moniker] = "true";
        if (changeSeverity)
            host.Native.Values[host.Binding.SeverityMoniker] = "error";
        host.Legacy.FailNextWrite = true;

        host.Native.Notify();
        await Assert.ThrowsAsync<InvalidOperationException>(host.DrainAsync);
        Assert.Equal(initial, host.ReadLegacy());
        Assert.Null(host.Refreshed);
        Assert.Equal(0, host.Legacy.WriteCount);

        host.Native.Notify();
        await host.DrainAsync();
        var expected = new CodeStyleOption2<bool>(changePreference, changeSeverity ? NotificationOption2.Error : NotificationOption2.Warning);
        Assert.Equal(expected, host.ReadLegacy());
        Assert.Equal(expected, host.Refreshed);
        Assert.Equal(1, host.Legacy.WriteCount);

        host.Native.Change(changePreference ? host.Binding.SeverityMoniker : host.Binding.Moniker, changePreference ? "suggestion" : "true");
        await host.DrainAsync();
        var afterSiblingEdit = new CodeStyleOption2<bool>(true, changePreference ? NotificationOption2.Suggestion : NotificationOption2.Error);
        Assert.Equal(afterSiblingEdit, host.ReadLegacy());
        Assert.Equal(afterSiblingEdit, host.Refreshed);
        Assert.Equal(2, host.Legacy.WriteCount);
        Assert.Equal(commits, host.Native.CommitCount);
    }

    [WpfFact]
    public async Task BatchedNativeImportWritesTheXmlPairOnceAndDoesNotEcho()
    {
        using var host = new Host(LanguageNames.CSharp);
        await host.InitializeAsync();
        host.Native.Values[host.Binding.Moniker] = "true";
        host.Native.Values[host.Binding.SeverityMoniker] = "error";
        host.Native.Notify();
        await host.DrainAsync();
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Error), host.ReadLegacy());
        Assert.Equal(1, host.Legacy.WriteCount);
        Assert.Equal(0, host.Native.CommitCount);
    }

    [WpfTheory]
    [InlineData(LanguageNames.CSharp)]
    [InlineData(LanguageNames.VisualBasic)]
    public async Task LegacyImportsDuringBootstrapSurviveNativePublication(string language)
    {
        using var host = new Host(language);
        host.Legacy.Value = new CodeStyleOption2<bool>(false, NotificationOption2.Silent).ToXElement().ToString();
        host.Native.Values[host.Binding.Moniker] = "true";
        host.Native.Values[host.Binding.SeverityMoniker] = "warning";
        var reading = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Native.BeforeEffectiveRead = async () =>
        {
            reading.SetResult(true);
            await resume.Task;
        };
        var initialization = host.Synchronizer.InitializeAsync();
        await reading.Task;
        var imported = new CodeStyleOption2<bool>(false, NotificationOption2.Suggestion);
        try
        {
            await host.Legacy.ImportAsync(new CodeStyleOption2<bool>(true, NotificationOption2.Error).ToXElement().ToString());
            await host.Legacy.ImportAsync(imported.ToXElement().ToString());
        }
        finally
        {
            resume.SetResult(true);
        }

        await initialization;
        await host.DrainAsync();

        Assert.Equal(imported, host.ReadLegacy());
        Assert.Equal(imported, host.Refreshed);
        Assert.Equal("false", host.Native.Effective(host.Binding.Moniker));
        Assert.Equal("suggestion", host.Native.Effective(host.Binding.SeverityMoniker));
        Assert.Equal(2, host.Native.CommitCount);
    }

    [WpfFact]
    public async Task LegacyImportBeforeNativeEditKeepsTheLatestSiblingAndAcceptsXmlWhitespace()
    {
        using var host = new Host(LanguageNames.CSharp);
        await host.InitializeAsync();
        var xml = new CodeStyleOption2<bool>(false, NotificationOption2.Warning).ToXElement();
        await host.Legacy.ImportAsync("\n  " + xml + "\n");
        await host.DrainAsync();
        host.Native.Change(host.Binding.Moniker, "true");
        await host.DrainAsync();
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Warning), host.ReadLegacy());
    }

    [WpfFact]
    public async Task NativeSuppressedSeverityDoesNotEchoTheNormalizedLegacySeverity()
    {
        using var host = new Host(LanguageNames.CSharp);
        await host.InitializeAsync();
        host.Native.Change(host.Binding.SeverityMoniker, "none");
        await host.DrainAsync();
        Assert.Equal("none", host.Native.Effective(host.Binding.SeverityMoniker));
        Assert.Equal(new CodeStyleOption2<bool>(false, NotificationOption2.None), host.Refreshed);
        Assert.Equal(NotificationOption2.Silent, host.ReadLegacy().Notification);
    }

    [WpfFact]
    public async Task RestartAfterResetDoesNotRestoreThePreviousNativeOverride()
    {
        using var first = new Host(LanguageNames.CSharp);
        await first.InitializeAsync();
        first.Native.Change(first.Binding.Moniker, "true");
        await first.DrainAsync();
        first.Native.Change(first.Binding.Moniker, "false");
        await first.DrainAsync();
        using var restarted = new Host(LanguageNames.CSharp);
        restarted.Legacy.Value = first.Legacy.Value;
        await restarted.InitializeAsync();
        Assert.Equal("false", restarted.Native.Effective(restarted.Binding.Moniker));
        Assert.Empty(restarted.Native.Values);
        Assert.Equal(0, restarted.Legacy.WriteCount);
    }

    [WpfFact]
    public async Task NativeImportDuringBootstrapWinsBeforeLegacySeeding()
    {
        using var host = new Host(LanguageNames.CSharp);
        host.Legacy.Value = new CodeStyleOption2<bool>(false, NotificationOption2.Warning).ToXElement().ToString();
        host.Native.BeforeSeedRead = () =>
        {
            host.Native.Values[host.Binding.Moniker] = "true";
            host.Native.Values[host.Binding.SeverityMoniker] = "error";
            host.Native.Notify();
        };
        await host.InitializeAsync();
        Assert.Equal(new CodeStyleOption2<bool>(true, NotificationOption2.Error), host.ReadLegacy());
        Assert.Equal("true", host.Native.Effective(host.Binding.Moniker));
        Assert.Equal("error", host.Native.Effective(host.Binding.SeverityMoniker));
        Assert.Equal(0, host.Native.CommitCount);
    }

    private sealed class Host : IDisposable
    {
        private readonly JoinableTaskContext _context = new();
        private readonly List<Task> _tasks = [];
        public readonly List<Task> Failures = [];
        public readonly LegacyStore Legacy = new();
        public readonly NativeStore Native;
        public readonly NativeCodeStyleSettingsSynchronizer.Binding Binding;
        public readonly NativeCodeStyleSettingsSynchronizer Synchronizer;
        private readonly VisualStudioSettingsOptionPersister _persister;
        public object? Refreshed;

        public Host(string language)
        {
            var setting = new CodeStyleSetting("dotnet_style_qualification_for_field", CodeStyleOptions2.QualifyFieldAccess, language, "Field");
            var storage = (VisualStudioOptionStorage.RoamingProfileStorage)VisualStudioOptionStorage.Storages[setting.Moniker];
            Binding = new(setting, storage.GetKey(language), NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, setting.Moniker));
            Legacy.Key = Binding.LegacyKey;
            Native = new NativeStore(Binding);
            _persister = new VisualStudioSettingsOptionPersister((_, _) => { }, ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>>.Empty, Legacy);
            Synchronizer = new(Native, _persister, [Binding], (_, value) => Refreshed = value, _tasks.Add,
                _context, CancellationToken.None, TimeSpan.Zero, Failures.Add);
        }

        public async Task InitializeAsync()
        {
            await Synchronizer.InitializeAsync();
            await DrainAsync();
        }

        public async Task DrainAsync()
        {
            while (_tasks.Count > 0)
            {
                var tasks = _tasks.ToArray();
                _tasks.Clear();
                await Task.WhenAll(tasks);
            }
        }

        public CodeStyleOption2<bool> ReadLegacy() => CodeStyleOption2<bool>.FromXElement(XElement.Parse(Legacy.Value!));
        public void Dispose()
        {
            Synchronizer.Dispose();
            _persister.Dispose();
            _context.Dispose();
        }
    }

    private sealed class NativeStore : IUnifiedSettingsService, IDisposable
    {
        public readonly Dictionary<string, string> Values = [];
        public readonly Dictionary<string, string> Defaults = [];
        private IObserver<SettingsUpdate>? _observer;
        public bool HasSubscription => _observer is not null;
        private int _persistedReads;
        public Action? BeforeSeedRead;
        public Func<Task>? BeforeEffectiveRead;
        public int CommitCount;
        public bool FailNextCommit;

        public NativeStore(NativeCodeStyleSettingsSynchronizer.Binding binding)
        {
            var style = (ICodeStyleOption2)binding.Setting.Key.Option.DefaultValue!;
            Defaults[binding.Moniker] = binding.Setting.SerializePreference(style);
            Defaults[binding.SeverityMoniker] = CodeStyleSetting.SerializeSeverity(style.Notification);
        }

        public string Effective(string moniker) => Values.TryGetValue(moniker, out var value) ? value : Defaults[moniker];
        private ReadResult Read(string moniker, ReadOptions options)
            => !Values.ContainsKey(moniker) && options.HasFlag(ReadOptions.NoFallbackToDefault)
                ? ReadResult.NotPersisted(moniker)
                : ReadResult.Success(moniker, new SettingValue(SettingValueType.String, JsonSerializer.Serialize(Effective(moniker))));

        public ValueTask<ReadResult> GetValueAsync(string moniker, ReadOptions options, CancellationToken cancellationToken)
            => new(Read(moniker, options));
        public async ValueTask<IReadOnlyCollection<ReadResult>> GetValuesAsync(IReadOnlyCollection<string> patterns, ReadOptions options, CancellationToken cancellationToken)
        {
            if (options.HasFlag(ReadOptions.NoFallbackToDefault) && ++_persistedReads == 2)
                BeforeSeedRead?.Invoke();
            if (!options.HasFlag(ReadOptions.NoFallbackToDefault) && BeforeEffectiveRead is { } beforeRead)
                await beforeRead();
            return patterns.Select(moniker => Read(moniker, options)).ToArray();
        }
        public ValueTask<IDisposable> SubscribeAsync(IObserver<SettingsUpdate> observer, IReadOnlyCollection<string> patterns, ReadOptions options, CancellationToken cancellationToken)
        {
            _observer = observer;
            observer.OnNext(new SettingsUpdate(patterns.Select(moniker => Read(moniker, options)).ToArray(), options));
            return new(this);
        }
        public ValueTask<CommitResult> RequestCommitAsync(ChangeSource source, SettingsCommit commit, CancellationToken cancellationToken)
        {
            CommitCount++;
            if (FailNextCommit)
            {
                FailNextCommit = false;
                return new(new CommitResult(CommitResultCode.ChangeError, []));
            }
            foreach (var (moniker, json) in commit.Values)
            {
                var value = JsonSerializer.Deserialize<string>(json)!;
                if (value == Defaults[moniker])
                {
                    Values.Remove(moniker);
                }
                else
                {
                    Values[moniker] = value;
                }
            }
            Notify(source.Id);
            return new(CommitResult.Success([]));
        }
        public void Change(string moniker, string value)
        {
            if (value == Defaults[moniker])
            {
                Values.Remove(moniker);
            }
            else
            {
                Values[moniker] = value;
            }
            Notify();
        }
        public void Notify(Guid source = default)
            => _observer?.OnNext(new SettingsUpdate(Defaults.Keys.Select(moniker => Values.TryGetValue(moniker, out var value)
                ? ReadResult.Success(moniker, new SettingValue(SettingValueType.String, JsonSerializer.Serialize(value)), new SettingValueMetaData(source))
                : ReadResult.NotPersisted(moniker)).ToArray(), ReadOptions.NoFallbackToDefault));
        public void Dispose() => _observer = null;
    }

    private sealed class LegacyStore : ISettingsManager, ISettingsSubset
    {
        public string? Value;
        public string Key = string.Empty;
        public int WriteCount;
        public bool FailNextWrite;
        public event PropertyChangedAsyncEventHandler? SettingChangedAsync;
        public ISettingsSubset GetSubset(string pattern) => this;
        public GetValueResult TryGetValue<T>(string name, out T value)
        {
            value = Value is null ? default! : (T)(object)Value;
            return Value is null ? GetValueResult.Missing : GetValueResult.Success;
        }
        public async Task SetValueAsync(string name, object? value, bool isMachineLocal)
        {
            Assert.False(isMachineLocal);
            if (FailNextWrite)
            {
                FailNextWrite = false;
                throw new InvalidOperationException("Legacy settings write failed.");
            }
            Value = (string?)value;
            WriteCount++;
            if (SettingChangedAsync is { } handlers)
            {
                foreach (PropertyChangedAsyncEventHandler handler in handlers.GetInvocationList())
                    await handler(this, new PropertyChangedEventArgs(name));
            }
        }
        public Task ImportAsync(string xml)
            => SetValueAsync(Key, xml, false);
        public ISettingsList GetOrCreateList(string name, bool isMachineLocal) => throw new NotImplementedException();
        public T GetValueOrDefault<T>(string name, T defaultValue = default!) => throw new NotImplementedException();
        public string[] NamesStartingWith(string prefix) => throw new NotImplementedException();
        public void SetOnlineStore(IAsyncStringStorage store) => throw new NotImplementedException();
        public void SetSharedStore(IAsyncStringStorage store) => throw new NotImplementedException();
    }
}
