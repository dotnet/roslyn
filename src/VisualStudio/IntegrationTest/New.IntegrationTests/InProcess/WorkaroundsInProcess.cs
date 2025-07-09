// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

[TestService]
internal sealed partial class WorkaroundsInProcess
{
    public async Task RemoveConflictingKeyBindingsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE80.DTE2>(cancellationToken);
        EnvDTE.Command command;
        try
        {
            command = dte.Commands.Item("Edit.IntelliCode.APIUsageExamples");
        }
        catch
        {
            // Ignore if the command doesn't exist
            return;
        }

        command.Bindings = Array.Empty<object>();
    }

    public async Task WaitForNavigationAsync(CancellationToken cancellationToken)
    {
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.NavigateTo], cancellationToken);
        await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);

        // It's not clear why this delay is necessary. Navigation operations are expected to fully complete as part
        // of one of the above waiters, but GetActiveWindowCaptionAsync appears to return the previous window
        // caption for a short delay after the above complete.
        await Task.Delay(2000, cancellationToken);
    }

    /// <summary>
    /// Background operations appear to have the ability to dismiss a light bulb session "at random". This method
    /// waits for known background work to complete and reduce the likelihood that the light bulb dismisses itself.
    /// </summary>
    public async Task WaitForLightBulbAsync(CancellationToken cancellationToken)
    {
        // Wait for workspace (including project system, file change notifications, and EditorPackage operations),
        // as well as Roslyn's solution crawler and diagnostic service that report light bulb session changes.
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService],
            cancellationToken);

        // Wait for operations dispatched to the main thread without other tracking
        await WaitForApplicationIdleAsync(cancellationToken);
    }

    public async Task DisableAutoSurroundAsync(CancellationToken cancellationToken)
    {
        // Disable auto surround because it will exit the completion session
        // Tracking issue: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1940994
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var settingManager = await GetRequiredGlobalServiceAsync<SVsUnifiedSettingsManager, ISettingsManager>(cancellationToken);
        var reader = settingManager.GetReader();
        var settingName = "textEditor.general.display.autoBraceSurround";
        var settingRetrieval = reader.GetValue<bool>(settingName);
        if (settingRetrieval.Value)
        {
            var writer = settingManager.GetWriter("Roslyn Integration test");
            writer.EnqueueChange(settingName, false);
            var result = writer.RequestCommit("Integration test workaround");
            Assert.True(result.Outcome is SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued);
        }

        // 17.9 P2 used a different name for the same setting
        settingName = "textEditor.general.autoBraceSurround";
        settingRetrieval = reader.GetValue<bool>(settingName);
        if (settingRetrieval.Value)
        {
            var writer = settingManager.GetWriter("Roslyn Integration test");
            writer.EnqueueChange(settingName, false);
            var result = writer.RequestCommit("Integration test workaround");
            Assert.True(result.Outcome is SettingCommitOutcome.Success or SettingCommitOutcome.NoChangesQueued);
        }
    }
}
