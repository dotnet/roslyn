// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings.DataProvider.CodeStyle;

internal sealed class CSharpCodeStyleSettingsProviderFactory(
    IThreadingContext threadingContext,
    Workspace workspace,
    IGlobalOptionService globalOptions) : ILanguageSettingsProviderFactory<CodeStyleSetting>
{
    public ISettingsProvider<CodeStyleSetting> GetForFile(string filePath)
        => new CSharpCodeStyleSettingsProvider(threadingContext, filePath, new OptionUpdater(workspace, filePath), workspace, globalOptions);
}
