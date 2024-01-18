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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;

/// <summary>
/// Implements View/Other Windows/C# Interactive command.
/// </summary>
[VisualStudioContribution]
internal class ResetInteractiveWindowFromProjectCommand(
    MefInjection<IThreadingContext> threadingContext,
    MefInjection<CSharpVsInteractiveWindowProvider> interactiveWindowProvider,
    MefInjection<VisualStudioWorkspace> workspace,
    MefInjection<IUIThreadOperationExecutor> uiThreadOperationExecutor,
    MefInjection<EditorOptionsService> editorOptionsService,
    AsyncServiceProviderInjection<SDTE, EnvDTE.DTE> dteProvider,
    AsyncServiceProviderInjection<SVsShellMonitorSelection, IVsMonitorSelection> monitorSelectionProvider,
    AsyncServiceProviderInjection<SVsSolutionBuildManager, IVsSolutionBuildManager> solutionBuildManagerProvider) : Command
{
    private IThreadingContext? _threadingContext;
    private CSharpVsInteractiveWindowProvider? _interactiveWindowProvider;
    private VisualStudioWorkspace? _workspace;
    private EditorOptionsService? _editorOptionsService;
    private IUIThreadOperationExecutor? _uiThreadOperationExecutor;

    public override CommandConfiguration CommandConfiguration => new("%CSharpLanguageServiceExtension.ResetInteractiveWindowFromProject.DisplayName%")
    {
        VisibleWhen =
            ActivationConstraint.ActiveProjectCapability(ProjectCapability.CSharp) &
            // This doesn't activate when a multi-targeted project has a netfx target. We currently do not support that scenario.
            // Ideally, we would remove this constraint entirely and support reset for .NET as well.
            ActivationConstraint.ActiveProjectBuildProperty("TargetFrameworkMoniker", "[.]NETFramework.*")
    };

    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await base.InitializeAsync(cancellationToken).ConfigureAwait(false);

        _threadingContext = await threadingContext.GetServiceAsync().ConfigureAwait(false);
        _interactiveWindowProvider = await interactiveWindowProvider.GetServiceAsync().ConfigureAwait(false);
        _workspace = await workspace.GetServiceAsync().ConfigureAwait(false);
        _editorOptionsService = await editorOptionsService.GetServiceAsync().ConfigureAwait(false);
        _uiThreadOperationExecutor = await uiThreadOperationExecutor.GetServiceAsync().ConfigureAwait(false);
    }

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_threadingContext);
        Contract.ThrowIfNull(_interactiveWindowProvider);
        Contract.ThrowIfNull(_editorOptionsService);
        Contract.ThrowIfNull(_uiThreadOperationExecutor);

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // fetch services on UI thread and stay on UI thread:
        var dte = await dteProvider.GetServiceAsync().ConfigureAwait(true);
        var monitorSelection = await monitorSelectionProvider.GetServiceAsync().ConfigureAwait(true);
        var solutionBuildManager = await solutionBuildManagerProvider.GetServiceAsync().ConfigureAwait(true);

        var resetInteractive = new VsResetInteractive(
            _workspace,
            dte,
            _editorOptionsService,
            _uiThreadOperationExecutor,
            monitorSelection,
            solutionBuildManager,
            static referenceName => $"#r \"{referenceName}\"",
            static namespaceName => $"using {namespaceName};");

        var vsInteractiveWindow = _interactiveWindowProvider.Open(instanceId: 0, focus: true);

        await resetInteractive.ExecuteAsync(vsInteractiveWindow.InteractiveWindow, CSharpVSResources.CSharp_Interactive).ConfigureAwait(true);
        resetInteractive.ExecutionCompleted += FocusWindow;

        await TaskScheduler.Default;

        void FocusWindow(object s, EventArgs e)
        {
            // We have to set focus to the Interactive Window *after* the wait indicator is dismissed.
            vsInteractiveWindow.Show(focus: true);
            resetInteractive.ExecutionCompleted -= FocusWindow;
        }
    }
}
