// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EditorConfigSettings;

namespace Microsoft.CodeAnalysis.CSharp.EditorConfigSettings
{
    internal class CSharpWhitespaceSettingsProviderFactory : ILanguageSettingsProviderFactory<WhitespaceSetting>
    {
        private readonly Workspace _workspace;

        public CSharpWhitespaceSettingsProviderFactory(Workspace workspace)
        {
            _workspace = workspace;
        }

        public ISettingsProvider<WhitespaceSetting> GetForFile(string filePath)
        {
            var updaterService = new OptionUpdater(_workspace, filePath);
            return new CSharpWhitespaceSettingsProvider(filePath, updaterService, _workspace);
        }
    }
}
