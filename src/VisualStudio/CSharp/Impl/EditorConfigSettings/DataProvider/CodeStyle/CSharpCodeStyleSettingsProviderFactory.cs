﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.CodeStyle
{
    internal sealed class CSharpCodeStyleSettingsProviderFactory : ILanguageSettingsProviderFactory<CodeStyleSetting>
    {
        private readonly Workspace _workspace;
        private readonly IGlobalOptionService _globalOptions;

        public CSharpCodeStyleSettingsProviderFactory(Workspace workspace, IGlobalOptionService globalOptions)
        {
            _workspace = workspace;
            _globalOptions = globalOptions;
        }

        public ISettingsProvider<CodeStyleSetting> GetForFile(string filePath)
            => new CSharpCodeStyleSettingsProvider(filePath, new OptionUpdater(_workspace, filePath), _workspace, _globalOptions);
    }
}
