// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.NamingStyles
{
    internal class NamingStyleSettingsProviderFactory : IWorkspaceSettingsProviderFactory<NamingStyleSetting>
    {
        private readonly Workspace _workspace;

        public NamingStyleSettingsProviderFactory(Workspace workspace) => _workspace = workspace;

        public ISettingsProvider<NamingStyleSetting> GetForFile(string filePath)
        {
            var updater = new NamingStyleSettingsUpdater(_workspace, filePath);
            return new NamingStyleSettingsProvider(filePath, updater, _workspace);
        }
    }
}
