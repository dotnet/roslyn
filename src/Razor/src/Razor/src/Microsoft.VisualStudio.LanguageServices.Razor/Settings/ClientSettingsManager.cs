// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.Settings;

[Export(typeof(IClientSettingsManager))]
internal sealed class ClientSettingsManager : IClientSettingsManager
{
    public event EventHandler<EventArgs>? ClientSettingsChanged;

    private readonly object _settingsUpdateLock = new();
    private readonly IAdvancedSettingsStorage? _advancedSettingsStorage;
    private readonly RazorGlobalOptions? _globalOptions;
    private ClientSettings _settings;

    [ImportingConstructor]
    public ClientSettingsManager(
        [ImportMany] IEnumerable<IClientSettingsChangedTrigger> changeTriggers,
        [Import(AllowDefault = true)] IAdvancedSettingsStorage? advancedSettingsStorage = null,
        RazorGlobalOptions? globalOptions = null)
    {
        _settings = ClientSettings.Default;

        // update Roslyn's global options (null in tests):
        if (globalOptions is not null)
        {
            globalOptions.TabSize = _settings.ClientSpaceSettings.IndentSize;
            globalOptions.UseTabs = _settings.ClientSpaceSettings.IndentWithTabs;
        }

        foreach (var changeTrigger in changeTriggers)
        {
            changeTrigger.Initialize(this);
        }

        _advancedSettingsStorage = advancedSettingsStorage;
        _globalOptions = globalOptions;

        if (_advancedSettingsStorage is not null)
        {
            Update(_advancedSettingsStorage.GetAdvancedSettings());
            _advancedSettingsStorage.OnChangedAsync(Update).Forget();
        }
    }

    public ClientSettings GetClientSettings() => _settings;

    public void Update(ClientSpaceSettings updatedSettings)
    {
        if (updatedSettings is null)
        {
            throw new ArgumentNullException(nameof(updatedSettings));
        }

        // update Roslyn's global options (null in tests):
        if (_globalOptions is not null)
        {
            _globalOptions.TabSize = updatedSettings.IndentSize;
            _globalOptions.UseTabs = updatedSettings.IndentWithTabs;
        }

        lock (_settingsUpdateLock)
        {
            UpdateSettings_NoLock(_settings with { ClientSpaceSettings = updatedSettings });
        }
    }

    public void Update(ClientCompletionSettings updatedSettings)
    {
        if (updatedSettings is null)
        {
            throw new ArgumentNullException(nameof(updatedSettings));
        }

        lock (_settingsUpdateLock)
        {
            UpdateSettings_NoLock(_settings with { ClientCompletionSettings = updatedSettings });
        }
    }

    public void Update(ClientAdvancedSettings advancedSettings)
    {
        if (advancedSettings is null)
        {
            throw new ArgumentNullException(nameof(advancedSettings));
        }

        lock (_settingsUpdateLock)
        {
            UpdateSettings_NoLock(_settings with { AdvancedSettings = advancedSettings });
        }
    }

    private void UpdateSettings_NoLock(ClientSettings settings)
    {
        if (!_settings.Equals(settings))
        {
            _settings = settings;

            ClientSettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
