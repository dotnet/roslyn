// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.VSSdkCompatibility;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;

/// <summary>
/// Implements View/Other Windows/C# Interactive command.
/// </summary>
[VisualStudioContribution]
internal class ResetInteractiveWindowFromProjectCommand(
    MefInjection<IThreadingContext> mefThreadingContext,
    MefInjection<CSharpVsInteractiveWindowProvider> mefInteractiveWindowProvider,
    MefInjection<VisualStudioWorkspace> mefWorkspace,
    MefInjection<IUIThreadOperationExecutor> mefUIThreadOperationExecutor,
    MefInjection<EditorOptionsService> mefEditorOptionsService,
    AsyncServiceProviderInjection<SDTE, EnvDTE.DTE> dteProvider,
    AsyncServiceProviderInjection<SVsShellMonitorSelection, IVsMonitorSelection> monitorSelectionProvider,
    AsyncServiceProviderInjection<SVsSolutionBuildManager, IVsSolutionBuildManager> solutionBuildManagerProvider) : Command
{
    public override CommandConfiguration CommandConfiguration => new("%CSharpLanguageServiceExtension.ResetInteractiveWindowFromProject.DisplayName%")
    {
        VisibleWhen =
            ActivationConstraint.ActiveProjectCapability(ProjectCapability.CSharp) &
            // This doesn't activate when a multi-targeted project has a netfx target. We currently do not support that scenario.
            // Ideally, we would remove this constraint entirely and support reset for .NET as well.
            ActivationConstraint.ActiveProjectBuildProperty("TargetFrameworkMoniker", "[.]NETFramework.*")
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var threadingContext = await mefThreadingContext.GetServiceAsync().ConfigureAwait(false);
        var interactiveWindowProvider = await mefInteractiveWindowProvider.GetServiceAsync().ConfigureAwait(false);
        var workspace = await mefWorkspace.GetServiceAsync().ConfigureAwait(false);
        var editorOptionsService = await mefEditorOptionsService.GetServiceAsync().ConfigureAwait(false);
        var uiThreadOperationExecutor = await mefUIThreadOperationExecutor.GetServiceAsync().ConfigureAwait(false);

        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // fetch services on UI thread and stay on UI thread:
        var dte = await dteProvider.GetServiceAsync().ConfigureAwait(true);
        var monitorSelection = await monitorSelectionProvider.GetServiceAsync().ConfigureAwait(true);
        var solutionBuildManager = await solutionBuildManagerProvider.GetServiceAsync().ConfigureAwait(true);

        var resetInteractive = new VsResetInteractive(
            workspace,
            dte,
            editorOptionsService,
            uiThreadOperationExecutor,
            monitorSelection,
            solutionBuildManager,
            static referenceName => $"#r \"{referenceName}\"",
            static namespaceName => $"using {namespaceName};");

        var vsInteractiveWindow = interactiveWindowProvider.Open(instanceId: 0, focus: true);

        // TODO: potential race condition (https://github.com/dotnet/roslyn/issues/71871):
        await resetInteractive.ExecuteAsync(vsInteractiveWindow.InteractiveWindow, CSharpVSResources.CSharp_Interactive).ConfigureAwait(true);
        resetInteractive.ExecutionCompleted += FocusWindow;

        void FocusWindow(object s, EventArgs e)
        {
            // We have to set focus to the Interactive Window *after* the wait indicator is dismissed.
            vsInteractiveWindow.Show(focus: true);
            resetInteractive.ExecutionCompleted -= FocusWindow;
        }
    }
}
