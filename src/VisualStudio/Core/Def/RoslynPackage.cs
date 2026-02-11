// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.UnitTesting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.RuleSets;
using Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;
using Microsoft.VisualStudio.LanguageServices.Implementation.SyncNamespaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem.BrokeredService;
using Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Setup;

[Guid(Guids.RoslynPackageIdString)]
[ProvideToolWindow(typeof(ValueTracking.ValueTrackingToolWindow))]
[ProvideToolWindow(typeof(StackTraceExplorerToolWindow))]
[ProvideService(typeof(RoslynPackageLoadService), IsAsyncQueryable = true, IsCacheable = true, IsFreeThreaded = true)]
internal sealed class RoslynPackage : AbstractPackage
{
    private static RoslynPackage? s_lazyInstance;

    private RuleSetEventHandler? _ruleSetEventHandler;
    private SolutionEventMonitor? _solutionEventMonitor;

    internal static async ValueTask<RoslynPackage?> GetOrLoadAsync(IThreadingContext threadingContext, IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (s_lazyInstance is null)
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = (IVsShell7?)await serviceProvider.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
            Assumes.Present(shell);
            await shell.LoadPackageAsync(typeof(RoslynPackage).GUID);

            if (ErrorHandler.Succeeded(((IVsShell)shell).IsPackageLoaded(typeof(RoslynPackage).GUID, out var package)))
            {
                s_lazyInstance = (RoslynPackage)package;
            }
        }

        return s_lazyInstance;
    }

    protected override void RegisterInitializeAsyncWork(PackageLoadTasks packageInitializationTasks)
    {
        base.RegisterInitializeAsyncWork(packageInitializationTasks);

        packageInitializationTasks.AddTask(isMainThreadTask: false, task: PackageInitializationBackgroundThreadAsync);

        return;

        async Task PackageInitializationBackgroundThreadAsync(PackageLoadTasks packageInitializationTasks, CancellationToken cancellationToken)
        {
            AddService(typeof(RoslynPackageLoadService), (_, _, _) => Task.FromResult((object?)new RoslynPackageLoadService()), promote: true);

            await RegisterEditorFactoryAsync(new SettingsEditorFactory(), cancellationToken).ConfigureAwait(true);
            await ProfferServiceBrokerServicesAsync(cancellationToken).ConfigureAwait(true);
        }
    }

    private async Task ProfferServiceBrokerServicesAsync(CancellationToken cancellationToken)
    {
        // Proffer in-process service broker services
        var serviceBrokerContainer = await this.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>(cancellationToken).ConfigureAwait(false);

        serviceBrokerContainer.Proffer(
            WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor,
            (_, _, _, _) => ValueTask.FromResult<object?>(new WorkspaceProjectFactoryService(ComponentModel.GetService<IWorkspaceProjectContextFactory>())));

        // Must be profferred before any C#/VB projects are loaded and the corresponding UI context activated.
        serviceBrokerContainer.Proffer(
            ManagedHotReloadLanguageServiceDescriptor.Descriptor,
            (_, _, _, _) => ValueTask.FromResult<object?>(ComponentModel.GetService<EditAndContinueLanguageService>()));
    }

    protected override async Task LoadComponentsInBackgroundAfterSolutionFullyLoadedAsync(CancellationToken cancellationToken)
    {
        // we need to load it as early as possible since we can have errors from
        // package from each language very early
        await this.ComponentModel.GetService<VisualStudioSuppressionFixService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
        await this.ComponentModel.GetService<VisualStudioDiagnosticListSuppressionStateService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);

        await this.ComponentModel.GetService<IVisualStudioDiagnosticAnalyzerService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
        await this.ComponentModel.GetService<RemoveUnusedReferencesCommandHandler>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
        await this.ComponentModel.GetService<SyncNamespacesCommandHandler>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);

        await LoadAnalyzerNodeComponentsAsync(cancellationToken).ConfigureAwait(false);

        await LoadStackTraceExplorerMenusAsync(cancellationToken).ConfigureAwait(false);

        // Initialize keybinding reset detector
        await ComponentModel.DefaultExportProvider.GetExportedValue<KeybindingReset.KeybindingResetDetector>().InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    // Overrides for VSSDK003 fix 
    // See https://github.com/Microsoft/VSSDK-Analyzers/blob/main/doc/VSSDK003.md
    public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
    {
        if (toolWindowType == typeof(ValueTracking.ValueTrackingToolWindow).GUID)
        {
            return this;
        }

        if (toolWindowType == typeof(StackTraceExplorerToolWindow).GUID)
        {
            return this;
        }

        return base.GetAsyncToolWindowFactory(toolWindowType);
    }

    protected override string GetToolWindowTitle(Type toolWindowType, int id)
            => base.GetToolWindowTitle(toolWindowType, id);

    protected override Task<object?> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        => Task.FromResult((object?)null);

    private async Task LoadStackTraceExplorerMenusAsync(CancellationToken cancellationToken)
    {
        // Obtain services and QueryInterface from the main thread
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var menuCommandService = (OleMenuCommandService?)await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);
        Assumes.Present(menuCommandService);
        StackTraceExplorerCommandHandler.Initialize(menuCommandService, this);
    }

    protected override void Dispose(bool disposing)
    {
        UnregisterAnalyzerTracker();
        UnregisterRuleSetEventHandler();

        ReportSessionWideTelemetry();

        _solutionEventMonitor?.Dispose();
        _solutionEventMonitor = null;

        base.Dispose(disposing);
    }

    private static void ReportSessionWideTelemetry()
    {
        AsyncCompletionLogger.ReportTelemetry();
        InheritanceMarginLogger.ReportTelemetry();
        FeaturesSessionTelemetry.Report();
    }

    private async Task LoadAnalyzerNodeComponentsAsync(CancellationToken cancellationToken)
    {
        await this.ComponentModel.GetService<IAnalyzerNodeSetup>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);

        _ruleSetEventHandler = this.ComponentModel.GetService<RuleSetEventHandler>();
        if (_ruleSetEventHandler != null)
            await _ruleSetEventHandler.RegisterAsync(this, cancellationToken).ConfigureAwait(false);
    }

    private void UnregisterAnalyzerTracker()
        => this.ComponentModel.GetService<IAnalyzerNodeSetup>().Unregister();

    private void UnregisterRuleSetEventHandler()
    {
        if (_ruleSetEventHandler != null)
        {
            _ruleSetEventHandler.Unregister();
            _ruleSetEventHandler = null;
        }
    }
}
