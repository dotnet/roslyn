// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.Whitespace
{
    internal sealed class CSharpWhitespaceSettingsProviderFactory : ILanguageSettingsProviderFactory<Setting>
    {
        private readonly Workspace _workspace;
        private readonly IGlobalOptionService _globalOptions;

        public CSharpWhitespaceSettingsProviderFactory(Workspace workspace, IGlobalOptionService globalOptions)
        {
            _workspace = workspace;
            _globalOptions = globalOptions;
        }

        public ISettingsProvider<Setting> GetForFile(string filePath)
        {
            var updaterService = new OptionUpdater(_workspace, filePath);
            return new CSharpWhitespaceSettingsProvider(filePath, updaterService, _workspace, _globalOptions);
        }
    }
}
