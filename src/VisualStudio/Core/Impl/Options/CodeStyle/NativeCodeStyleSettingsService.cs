// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.RpcContracts;
using Microsoft.VisualStudio.RpcContracts.UnifiedSettings;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

[Export(typeof(INativeCodeStyleSettingsService))]
internal sealed class NativeCodeStyleSettingsService : INativeCodeStyleSettingsService
{
    private readonly IThreadingContext _threadingContext;
    private readonly IVsService<IBrokeredServiceContainer> _container;
    private readonly IVsService<ISettingsManager> _legacyManager;
    private readonly Lazy<EditorConfigOptionsEnumerator> _enumerator;
    private readonly Lazy<ILegacyGlobalOptionService> _globalOptions;
    private readonly object _startGate = new();
    private readonly HashSet<string> _startedLanguages = [];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NativeCodeStyleSettingsService(
        IThreadingContext threadingContext,
        IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> container,
        IVsService<SVsSettingsPersistenceManager, ISettingsManager> legacyManager,
        Lazy<EditorConfigOptionsEnumerator> enumerator,
        Lazy<ILegacyGlobalOptionService> globalOptions)
    {
        _threadingContext = threadingContext;
        _container = container;
        _legacyManager = legacyManager;
        _enumerator = enumerator;
        _globalOptions = globalOptions;
    }

    public void Start(string language)
    {
        lock (_startGate)
        {
            if (!_startedLanguages.Add(language))
                return;
        }

        var cancellationToken = _threadingContext.DisposalToken;
        _threadingContext.JoinableTaskFactory.RunAsync(() => RunAsync(SynchronizeAsync,
            task => task.ReportNonFatalErrorUnlessCancelledAsync(cancellationToken), cancellationToken))
            .Task.ReportNonFatalErrorUnlessCancelledAsync(cancellationToken);

        async Task SynchronizeAsync()
        {
            // Do not re-enter the option persister's Lazy while it is being created.
            await System.Threading.Tasks.Task.Yield();
            var container = await _container.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var broker = container.GetFullAccessServiceBroker();
            using var client = new ServiceBrokerClient(broker, _threadingContext.JoinableTaskFactory);
            using var rental = await client.GetProxyAsync<IUnifiedSettingsService>(VisualStudioServices.VS2022_10.UnifiedSettingsService, cancellationToken).ConfigureAwait(false);
            var native = rental.Proxy;
            if (native is null)
                throw new InvalidOperationException("Unified settings service is unavailable.");
            var manager = await _legacyManager.GetValueAsync(cancellationToken).ConfigureAwait(false);
            using var legacy = new VisualStudioSettingsOptionPersister(Refresh,
                ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>>.Empty, manager);
            using var synchronizer = new NativeCodeStyleSettingsSynchronizer(native, legacy, CreateBindings(language), Refresh,
                task => task.ReportNonFatalErrorUnlessCancelledAsync(cancellationToken), _threadingContext.JoinableTaskContext, cancellationToken);
            await synchronizer.InitializeAsync().ConfigureAwait(false);
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static async Task RunAsync(Func<Task> synchronizeAsync, Action<Task> observe, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await synchronizeAsync().ConfigureAwait(false);
                return;
            }
            catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                observe(Task.FromException(e));
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        }
    }

    private IEnumerable<NativeCodeStyleSettingsSynchronizer.Binding> CreateBindings(string language)
    {
        var names = NativeCodeStyleSettingsDefinitions.GetConfigNames(language).ToImmutableHashSet();
        var options = _enumerator.Value.GetOptions(language).SelectMany(group => group.options);
        foreach (var option in options.Where(option => names.Contains(option.Definition.ConfigName)))
        {
            var key = new OptionKey2(option, option.IsPerLanguage ? language : null);
            var storage = (VisualStudioOptionStorage.RoamingProfileStorage)VisualStudioOptionStorage.Storages[option.Definition.ConfigName];
            var moniker = NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, option.Definition.ConfigName);
            yield return new(new CodeStyleSetting(option.Definition.ConfigName, option, language, string.Empty), storage.GetKey(key.Language), moniker);
        }
    }

    private void Refresh(OptionKey2 key, object? value)
    {
        if (_globalOptions.Value.GlobalOptions.RefreshOption(key, value))
            _globalOptions.Value.UpdateRegisteredWorkspaces();
    }
}
