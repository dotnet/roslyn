﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.CodeStyle
{
    [ExportLanguageServiceFactory(typeof(ILanguageSettingsProviderFactory<CodeStyleSetting>), LanguageNames.CSharp), Shared]
    internal sealed class CSharpCodeStyleSettingsLanguageServiceFactory : ILanguageServiceFactory
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeStyleSettingsLanguageServiceFactory(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            var workspace = languageServices.WorkspaceServices.Workspace;
            return new CSharpCodeStyleSettingsProviderFactory(workspace, _globalOptions);
        }
    }
}
