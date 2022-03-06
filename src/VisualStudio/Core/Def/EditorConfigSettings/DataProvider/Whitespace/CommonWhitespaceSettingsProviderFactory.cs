// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal class CommonWhitespaceSettingsProviderFactory : IWorkspaceSettingsProviderFactory<WhitespaceSetting>
    {
        private readonly Workspace _workspace;

        public CommonWhitespaceSettingsProviderFactory(Workspace workspace) => _workspace = workspace;

        public ISettingsProvider<WhitespaceSetting> GetForFile(string filePath)
            => new CommonWhitespaceSettingsProvider(filePath, new OptionUpdater(_workspace, filePath), _workspace);

    }
}
