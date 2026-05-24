// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Shared]
[Export(typeof(IClientSettingsManager))]
[Export(typeof(RemoteClientSettingsManager))]
internal sealed class RemoteClientSettingsManager : IClientSettingsManager
{
    private ClientSettings _settings = ClientSettings.Default;

    public event EventHandler<EventArgs>? ClientSettingsChanged;

    public ClientSettings GetClientSettings() => _settings;

    public void Update(ClientSpaceSettings updatedSettings)
    {
        UpdateSettings(_settings with { ClientSpaceSettings = updatedSettings });
    }

    public void Update(ClientCompletionSettings updatedSettings)
    {
        UpdateSettings(_settings with { ClientCompletionSettings = updatedSettings });
    }

    public void Update(ClientAdvancedSettings updatedSettings)
    {
        UpdateSettings(_settings with { AdvancedSettings = updatedSettings });
    }

    internal void Update(ClientSettings settings)
    {
        UpdateSettings(settings);
    }

    private void UpdateSettings(ClientSettings settings)
    {
        if (!_settings.Equals(settings))
        {
            _settings = settings;
            ClientSettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
