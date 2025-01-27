// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.NamingStyles;

internal sealed class NamingStyleSettingsProviderFactory(Workspace workspace, IGlobalOptionService globalOptions) : IWorkspaceSettingsProviderFactory<NamingStyleSetting>
{
    private readonly Workspace _workspace = workspace;
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public ISettingsProvider<NamingStyleSetting> GetForFile(string filePath)
    {
        var updater = new NamingStyleSettingsUpdater(_workspace, _globalOptions, filePath);
        return new NamingStyleSettingsProvider(filePath, updater, _workspace, _globalOptions);
    }
}
