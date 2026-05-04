// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.Whitespace;

internal sealed class CSharpWhitespaceSettingsProviderFactory(
    IThreadingContext threadingContext,
    Workspace workspace,
    IGlobalOptionService globalOptions) : ILanguageSettingsProviderFactory<Setting>
{
    public ISettingsProvider<Setting> GetForFile(string filePath)
    {
        var updaterService = new OptionUpdater(workspace, filePath);
        return new CSharpWhitespaceSettingsProvider(threadingContext, filePath, updaterService, workspace, globalOptions);
    }
}
