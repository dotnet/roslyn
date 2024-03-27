// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer;

[Export(typeof(VisualStudioCopilotOptionService)), Shared]
internal sealed class VisualStudioCopilotOptionService
{
    private const string CopilotOptionNamePrefix = "Microsoft.VisualStudio.Conversations";

    private readonly Task<ISettingsManager> _settingsManagerTask;

    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioCopilotOptionService(
        IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService,
        IThreadingContext threadingContext)
    {
        _settingsManagerTask = settingsManagerService.GetValueAsync(threadingContext.DisposalToken);
    }

    public async Task<bool> IsCopilotOptionEnabledAsync(string optionName)
    {
        var settingManager = await _settingsManagerTask.ConfigureAwait(false);
        // The bool setting is persisted as 0=None, 1=True, 2=False, so it needs to be retrieved as an int.
        return settingManager.TryGetValue($"{CopilotOptionNamePrefix}.{optionName}", out int isEnabled) == GetValueResult.Success
            && isEnabled == 1;
    }
}
