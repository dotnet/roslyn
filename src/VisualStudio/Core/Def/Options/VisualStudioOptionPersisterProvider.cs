// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using UnifiedSettingsManager = Microsoft.VisualStudio.Utilities.UnifiedSettings.ISettingsManager;

namespace Microsoft.VisualStudio.LanguageServices.Options;

[Export(typeof(IOptionPersisterProvider))]
[Export(typeof(VisualStudioOptionPersisterProvider))]
internal sealed class VisualStudioOptionPersisterProvider : IOptionPersisterProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<ILegacyGlobalOptionService> _legacyGlobalOptions;

    // maps config name to a read fallback:
    private readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> _readFallbacks;

    // Ensure only one persister instance is created (even in the face of parallel requests for the value)
    // because the constructor registers global event handler callbacks.
    private readonly Lazy<IOptionPersister> _lazyPersister;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioOptionPersisterProvider(
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        [ImportMany] IEnumerable<Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> readFallbacks,
        Lazy<ILegacyGlobalOptionService> legacyGlobalOptions)
    {
        _serviceProvider = serviceProvider;
        _legacyGlobalOptions = legacyGlobalOptions;
        _readFallbacks = readFallbacks.ToImmutableDictionary(item => item.Metadata.ConfigName, item => item);
        _lazyPersister = new Lazy<IOptionPersister>(() => CreatePersister());
    }

    public IOptionPersister GetOrCreatePersister()
        => _lazyPersister.Value;

    private IOptionPersister CreatePersister()
    {
        var settingsManager = GetFreeThreadedService<SVsSettingsPersistenceManager, ISettingsManager>();
        Assumes.Present(settingsManager);

        var localRegistry = GetFreeThreadedService<SLocalRegistry, ILocalRegistry4>();
        Assumes.Present(localRegistry);

        var featureFlags = GetFreeThreadedService<SVsFeatureFlags, IVsFeatureFlags>();

        var unifiedSettingsManager = GetFreeThreadedService<SVsUnifiedSettingsManager, UnifiedSettingsManager>();

        return new VisualStudioOptionPersister(
            new VisualStudioSettingsOptionPersister(RefreshOption, _readFallbacks, settingsManager, unifiedSettingsManager),
            LocalUserRegistryOptionPersister.Create(localRegistry),
            new FeatureFlagPersister(featureFlags));
    }

    private void RefreshOption(OptionKey2 optionKey, object? newValue)
    {
        if (_legacyGlobalOptions.Value.GlobalOptions.RefreshOption(optionKey, newValue))
        {
            // We may be updating the values of internally defined public options.
            // Update solution snapshots of all workspaces to reflect the new values.
            _legacyGlobalOptions.Value.UpdateRegisteredWorkspaces();
        }
    }

    /// <summary>
    /// Returns a service without doing a transition to the UI thread to cast the service to the interface type. This should only be called for services that are
    /// well-understood to be castable off the UI thread, either because they are managed or free-threaded COM.
    /// </summary>
    private I? GetFreeThreadedService<T, I>() where I : class
    {
        try
        {
            return (I?)_serviceProvider.GetService(typeof(T));
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
