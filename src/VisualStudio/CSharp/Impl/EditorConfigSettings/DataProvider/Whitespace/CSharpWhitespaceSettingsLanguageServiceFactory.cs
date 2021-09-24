// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.Whitespace
{
    [ExportLanguageServiceFactory(typeof(ILanguageSettingsProviderFactory<WhitespaceSetting>), LanguageNames.CSharp), Shared]
    internal class CSharpWhitespaceSettingsLanguageServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpWhitespaceSettingsLanguageServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            var workspace = languageServices.WorkspaceServices.Workspace;
            return new CSharpWhitespaceSettingsProviderFactory(workspace);
        }
    }
}
