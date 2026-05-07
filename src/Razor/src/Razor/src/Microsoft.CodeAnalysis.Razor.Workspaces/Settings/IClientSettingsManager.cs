// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Razor.Settings;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

internal interface IClientSettingsManager
{
    ClientSettings GetClientSettings();

    void Update(ClientSpaceSettings updateSettings);
    void Update(ClientCompletionSettings updateSettings);
    void Update(ClientAdvancedSettings updateSettings);

    event EventHandler<EventArgs> ClientSettingsChanged;
}
