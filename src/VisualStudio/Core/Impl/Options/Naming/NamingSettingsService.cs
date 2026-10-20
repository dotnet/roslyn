// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using LegacySettingsManager = Microsoft.VisualStudio.Settings.ISettingsManager;
using UnifiedSettingsManager = Microsoft.VisualStudio.Utilities.UnifiedSettings.ISettingsManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

[Export(typeof(INamingSettingsService))]
internal sealed class NamingSettingsService : INamingSettingsService, IDisposable
{
    private readonly IThreadingContext _threading;
    private readonly IVsService<UnifiedSettingsManager> _nativeManager;
    private readonly IVsService<LegacySettingsManager> _legacyManager;
    private readonly Lazy<ILegacyGlobalOptionService> _globalOptions;
    private readonly Dictionary<string, AsyncLazy<NamingSettingsRuntime>> _runtimes;
    private readonly object _lifetimeGate = new();
    private readonly List<NamingSettingsRuntime> _createdRuntimes = [];
    private readonly CancellationTokenRegistration _disposalRegistration;
    private int _started;
    private bool _disposed;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NamingSettingsService(IThreadingContext threading,
        IVsService<SVsUnifiedSettingsManager, UnifiedSettingsManager> nativeManager,
        IVsService<SVsSettingsPersistenceManager, LegacySettingsManager> legacyManager,
        Lazy<ILegacyGlobalOptionService> globalOptions)
    {
        _threading = threading;
        _nativeManager = nativeManager;
        _legacyManager = legacyManager;
        _globalOptions = globalOptions;
        _runtimes = new(StringComparer.Ordinal);
        foreach (var language in new[] { LanguageNames.CSharp, LanguageNames.VisualBasic })
            _runtimes.Add(language, new(() => CreateRuntimeAsync(language), threading.JoinableTaskFactory));
        _disposalRegistration = threading.DisposalToken.Register(DisposeRuntimes);
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
            return;
        foreach (var lazy in _runtimes.Values)
        {
            _threading.JoinableTaskFactory.RunAsync(async () =>
            {
                await Task.Yield();
                var runtime = await lazy.GetValueAsync(_threading.DisposalToken).ConfigureAwait(false);
                await runtime.InitializeAsync().ConfigureAwait(false);
            }).Task.ReportNonFatalErrorUnlessCancelledAsync(_threading.DisposalToken);
        }
    }

    private async Task<NamingSettingsRuntime> CreateRuntimeAsync(string language)
    {
        var token = _threading.DisposalToken;
        var native = await _nativeManager.GetValueAsync(token).ConfigureAwait(false);
        var manager = await _legacyManager.GetValueAsync(token).ConfigureAwait(false);
        var key = new OptionKey2(NamingStyleOptions.NamingPreferences, language);
        var storage = (VisualStudioOptionStorage.RoamingProfileStorage)VisualStudioOptionStorage.Storages[key.Option.Definition.ConfigName];
        var legacyKey = storage.GetKey(language);
        var fallbackKey = NamingPreferencesReadFallback.GetStorageKey(language);
        NamingStylePreferences ReadLegacy() => ReadLegacyPreferences(manager, legacyKey, fallbackKey);
        void WriteLegacy(NamingStylePreferences value)
        {
            _threading.JoinableTaskFactory.Run(() => manager.SetValueAsync(legacyKey, value.CreateXElement().ToString(), isMachineLocal: false));
        }
        var store = new NamingSettingsStore(native, language);
        var coordinator = new NamingSettingsCoordinator(store, ReadLegacy, WriteLegacy, _threading.JoinableTaskContext,
            value => Refresh(key, value));
        var runtime = new NamingSettingsRuntime(store, coordinator, manager.GetSubset("*"), legacyKey, fallbackKey, ReadLegacy,
            task => task.ReportNonFatalErrorUnlessCancelledAsync(token),
            exception => FatalError.ReportAndCatchUnlessCanceled(exception, token), token);
        lock (_lifetimeGate)
        {
            if (_disposed)
            {
                runtime.Dispose();
                throw new ObjectDisposedException(nameof(NamingSettingsService));
            }
            _createdRuntimes.Add(runtime);
        }
        return runtime;
    }

    internal static NamingStylePreferences ReadLegacyPreferences(LegacySettingsManager manager, string key, string fallbackKey)
    {
        var result = manager.TryGetValue(key, out string xml);
        if (result == GetValueResult.Missing)
            result = manager.TryGetValue(fallbackKey, out xml);
        if (result == GetValueResult.Missing)
            return NamingStylePreferences.Default;
        if (result != GetValueResult.Success)
            throw new InvalidDataException($"Unable to read legacy Naming preferences: {result}.");
        try
        {
            return NamingStylePreferences.FromXElement(XElement.Parse(xml));
        }
        catch (Exception e) when (e is System.Xml.XmlException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidDataException("Unable to import legacy Naming preferences.", e);
        }
    }

    private void Refresh(OptionKey2 key, object? value)
    {
        if (_globalOptions.Value.GlobalOptions.RefreshOption(key, value))
            _globalOptions.Value.UpdateRegisteredWorkspaces();
    }

    public async Task<NamingSettingsCoordinator> GetCoordinatorAsync(string language, CancellationToken cancellationToken)
        => (await _runtimes[language].GetValueAsync(cancellationToken).ConfigureAwait(false)).Coordinator;

    public async Task<NamingSettingsProvider> GetProviderAsync(string language, CancellationToken cancellationToken)
    {
        Start();
        var runtime = await _runtimes[language].GetValueAsync(cancellationToken).ConfigureAwait(false);
        await runtime.Ready.WithCancellation(cancellationToken).ConfigureAwait(false);
        return runtime.Provider;
    }

    public async Task ResetAsync(string language, CancellationToken cancellationToken)
    {
        var provider = await GetProviderAsync(language, cancellationToken).ConfigureAwait(false);
        var result = await provider.ResetAsync(cancellationToken).ConfigureAwait(false);
        if (result is ExternalSettingOperationResult.Failure failure)
            throw new InvalidDataException(failure.ErrorMessage);
    }

    private void DisposeRuntimes()
    {
        lock (_lifetimeGate)
        {
            _disposed = true;
            foreach (var runtime in _createdRuntimes)
                runtime.Dispose();
            _createdRuntimes.Clear();
        }
    }

    public void Dispose()
    {
        _disposalRegistration.Dispose();
        DisposeRuntimes();
    }
}
