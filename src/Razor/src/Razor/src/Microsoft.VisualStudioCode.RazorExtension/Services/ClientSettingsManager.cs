// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

namespace Microsoft.VisualStudioCode.RazorExtension.Configuration;

[Shared]
[Export(typeof(IClientSettingsManager))]
internal class ClientSettingsManager : IClientSettingsManager
{
    private ClientSettings _currentSettings = ClientSettings.Default;

    public event EventHandler<EventArgs>? ClientSettingsChanged;

    public ClientSettings GetClientSettings()
    {
        return _currentSettings;
    }

    public void Update(ClientAdvancedSettings updateSettings)
    {
        _currentSettings = _currentSettings with
        {
            AdvancedSettings = updateSettings
        };

        ClientSettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Update(ClientSpaceSettings updateSettings)
    {
        _currentSettings = _currentSettings with
        {
            ClientSpaceSettings = updateSettings
        };

        ClientSettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Update(ClientCompletionSettings updateSettings)
    {
        _currentSettings = _currentSettings with
        {
            ClientCompletionSettings = updateSettings
        };

        ClientSettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
