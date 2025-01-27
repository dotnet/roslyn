// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities.UnifiedSettings;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

[TestService]
internal partial class WorkaroundsInProcess
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

    private static bool s_gitHubCopilotWorkaroundApplied = false;

    /// <summary>
    /// GitHub Copilot opens it's output window and steals focus randomly after a file is open. This forces that to happen sooner.
    /// </summary>
    public async Task WaitForGitHubCoPilotAsync(CancellationToken cancellationToken)
    {
        if (s_gitHubCopilotWorkaroundApplied)
            return;

        await JoinableTaskFactory.SwitchToMainThreadAsync();

        var shell = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsShell, IVsShell>(cancellationToken);
        var packageGuid = new Guid("{22818076-b98c-4525-b959-c9e12ff2433c}");

        if (ErrorHandler.Succeeded(shell.IsPackageInstalled(packageGuid, out var fInstalled)) && fInstalled != 0)
        {
            shell.LoadPackage(packageGuid, out _);

            var tempFile = Path.Combine(Path.GetTempPath(), "GitHubCopilotWorkaround.txt");
            File.WriteAllText(tempFile, "");
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, tempFile, VSConstants.LOGVIEWID.Code_guid, out _, out _, out var windowFrame, out _);

            await Task.Delay(TimeSpan.FromSeconds(10));

            windowFrame.CloseFrame(grfSaveOptions: 0);

            // Opening a file implicitly created a "solution" so close it so other tests don't care
            await TestServices.SolutionExplorer.CloseSolutionAsync(cancellationToken);

            s_gitHubCopilotWorkaroundApplied = true;
        }
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
