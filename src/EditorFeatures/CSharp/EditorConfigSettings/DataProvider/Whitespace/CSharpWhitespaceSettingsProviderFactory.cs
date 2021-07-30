// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.Whitespace
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
