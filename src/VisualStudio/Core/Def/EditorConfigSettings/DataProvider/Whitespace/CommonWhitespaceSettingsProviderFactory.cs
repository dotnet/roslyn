// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider.Whitespace;

internal sealed class CommonWhitespaceSettingsProviderFactory(
    IThreadingContext threadingContext, Workspace workspace, IGlobalOptionService globalOptions) : IWorkspaceSettingsProviderFactory<Setting>
{
    public ISettingsProvider<Setting> GetForFile(string filePath)
        => new CommonWhitespaceSettingsProvider(threadingContext, filePath, new OptionUpdater(workspace, filePath), workspace, globalOptions);
}
