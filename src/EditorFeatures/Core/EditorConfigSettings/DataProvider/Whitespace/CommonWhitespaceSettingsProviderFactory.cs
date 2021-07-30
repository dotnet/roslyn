// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Formatting
{
    internal class CommonFormattingSettingsProviderFactory : IWorkspaceSettingsProviderFactory<WhitespaceSetting>
    {
        private readonly Workspace _workspace;

        public CommonFormattingSettingsProviderFactory(Workspace workspace) => _workspace = workspace;

        public ISettingsProvider<WhitespaceSetting> GetForFile(string filePath)
            => new CommonFormattingSettingsProvider(filePath, new OptionUpdater(_workspace, filePath), _workspace);

    }
}
