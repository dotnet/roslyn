// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EditorConfigSettings;

namespace Microsoft.CodeAnalysis.CSharp.EditorConfigSettings
{
    internal class CSharpCodeStyleSettingsProviderFactory : ILanguageSettingsProviderFactory<CodeStyleSetting>
    {
        private readonly Workspace _workspace;

        public CSharpCodeStyleSettingsProviderFactory(Workspace workspace) => _workspace = workspace;

        public ISettingsProvider<CodeStyleSetting> GetForFile(string filePath)
            => new CSharpCodeStyleSettingsProvider(filePath, new OptionUpdater(_workspace, filePath), _workspace);
    }
}
