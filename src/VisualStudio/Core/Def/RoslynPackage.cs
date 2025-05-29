// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ColorSchemes;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;
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
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Setup;

[Guid(Guids.RoslynPackageIdString)]
[ProvideToolWindow(typeof(ValueTracking.ValueTrackingToolWindow))]
[ProvideToolWindow(typeof(StackTraceExplorerToolWindow))]
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
        packageInitializationTasks.AddTask(isMainThreadTask: true, task: PackageInitializationMainThreadAsync);

        return;

        Task PackageInitializationBackgroundThreadAsync(PackageLoadTasks packageInitializationTasks, CancellationToken cancellationToken)
        {
            return ProfferServiceBrokerServicesAsync(cancellationToken);
        }

        Task PackageInitializationMainThreadAsync(PackageLoadTasks packageInitializationTasks, CancellationToken cancellationToken)
        {
            var settingsEditorFactory = SettingsEditorFactory.GetInstance();
            RegisterEditorFactory(settingsEditorFactory);

            return Task.CompletedTask;
        }
    }

    protected override void RegisterOnAfterPackageLoadedAsyncWork(PackageLoadTasks afterPackageLoadedTasks)
    {
        base.RegisterOnAfterPackageLoadedAsyncWork(afterPackageLoadedTasks);

        afterPackageLoadedTasks.AddTask(isMainThreadTask: false, task: OnAfterPackageLoadedBackgroundThreadAsync);
        afterPackageLoadedTasks.AddTask(isMainThreadTask: true, task: OnAfterPackageLoadedMainThreadAsync);

        return;

        Task OnAfterPackageLoadedBackgroundThreadAsync(PackageLoadTasks afterPackageLoadedTasks, CancellationToken cancellationToken)
        {
            // Ensure the options persisters are loaded since we have to fetch options from the shell
            _ = ComponentModel.GetService<IGlobalOptionService>();

            var colorSchemeApplier = ComponentModel.GetService<ColorSchemeApplier>();
            colorSchemeApplier.RegisterInitializationWork(afterPackageLoadedTasks);

            // We are at the VS layer, so we know we must be able to get the IGlobalOperationNotificationService here.
            var globalNotificationService = this.ComponentModel.GetService<IGlobalOperationNotificationService>();

            _solutionEventMonitor = new SolutionEventMonitor(globalNotificationService);
            TrackBulkFileOperations(globalNotificationService);

            return Task.CompletedTask;
        }

        Task OnAfterPackageLoadedMainThreadAsync(PackageLoadTasks afterPackageLoadedTasks, CancellationToken cancellationToken)
        {
            // load some services that have to be loaded in UI thread
            LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(cancellationToken).Forget();

            return Task.CompletedTask;
        }
    }

    private async Task ProfferServiceBrokerServicesAsync(CancellationToken cancellationToken)
    {
        // Proffer in-process service broker services
        var serviceBrokerContainer = await this.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>(cancellationToken).ConfigureAwait(false);

        serviceBrokerContainer.Proffer(
            WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor,
            (_, _, _, _) => ValueTaskFactory.FromResult<object?>(new WorkspaceProjectFactoryService(this.ComponentModel.GetService<IWorkspaceProjectContextFactory>())));

        // Must be profferred before any C#/VB projects are loaded and the corresponding UI context activated.
        serviceBrokerContainer.Proffer(
            ManagedHotReloadLanguageServiceDescriptor.Descriptor,
            (_, _, _, _) => ValueTaskFactory.FromResult<object?>(new ManagedEditAndContinueLanguageServiceBridge(this.ComponentModel.GetService<EditAndContinueLanguageService>())));
    }

    protected override async Task LoadComponentsAsync(CancellationToken cancellationToken)
    {
        await TaskScheduler.Default;

        await GetServiceAsync(typeof(SVsErrorList)).ConfigureAwait(false);
        await GetServiceAsync(typeof(SVsSolution)).ConfigureAwait(false);
        await GetServiceAsync(typeof(SVsShell)).ConfigureAwait(false);
        await GetServiceAsync(typeof(SVsRunningDocumentTable)).ConfigureAwait(false);
        await GetServiceAsync(typeof(SVsTextManager)).ConfigureAwait(false);

        // we need to load it as early as possible since we can have errors from
        // package from each language very early
        await this.ComponentModel.GetService<VisualStudioSuppressionFixService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
        await this.ComponentModel.GetService<VisualStudioDiagnosticListSuppressionStateService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);

        await this.ComponentModel.GetService<IVisualStudioDiagnosticAnalyzerService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
        await this.ComponentModel.GetService<RemoveUnusedReferencesCommandHandler>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
        await this.ComponentModel.GetService<SyncNamespacesCommandHandler>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
        await this.ComponentModel.GetService<SolutionExplorerSymbolTreeItemCommandHandler>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);

        await LoadAnalyzerNodeComponentsAsync(cancellationToken).ConfigureAwait(false);

        LoadComponentsBackgroundAsync(cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken).Forget();
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

    private async Task LoadComponentsBackgroundAsync(CancellationToken cancellationToken)
    {
        await TaskScheduler.Default;

        await LoadStackTraceExplorerMenusAsync(cancellationToken).ConfigureAwait(true);

        // Initialize keybinding reset detector
        await ComponentModel.DefaultExportProvider.GetExportedValue<KeybindingReset.KeybindingResetDetector>().InitializeAsync(cancellationToken).ConfigureAwait(true);
    }

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

        if (_solutionEventMonitor != null)
        {
            _solutionEventMonitor.Dispose();
            _solutionEventMonitor = null;
        }

        base.Dispose(disposing);
    }

    private void ReportSessionWideTelemetry()
    {
        AsyncCompletionLogger.ReportTelemetry();
        InheritanceMarginLogger.ReportTelemetry();
        FeaturesSessionTelemetry.Report();
        ComponentModel.GetService<VisualStudioSourceGeneratorTelemetryCollectorWorkspaceServiceFactory>().ReportOtherWorkspaceTelemetry();
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

    private static void TrackBulkFileOperations(IGlobalOperationNotificationService globalNotificationService)
    {
        // we will pause whatever ambient work loads we have that are tied to IGlobalOperationNotificationService
        // such as solution crawler, preemptive remote host synchronization and etc. any background work users
        // didn't explicitly asked for.
        //
        // this should give all resources to BulkFileOperation. we do same for things like build, debugging, wait
        // dialog and etc. BulkFileOperation is used for things like git branch switching and etc.
        Contract.ThrowIfNull(globalNotificationService);

        // BulkFileOperation can't have nested events. there will be ever only 1 events (Begin/End)
        // so we only need simple tracking.
        var gate = new object();
        IDisposable? localRegistration = null;

        BulkFileOperation.Begin += (s, a) => StartBulkFileOperationNotification();
        BulkFileOperation.End += (s, a) => StopBulkFileOperationNotification();

        return;

        void StartBulkFileOperationNotification()
        {
            lock (gate)
            {
                // this shouldn't happen, but we are using external component
                // so guarding us from them
                if (localRegistration != null)
                {
                    FatalError.ReportAndCatch(new InvalidOperationException("BulkFileOperation already exist"), ErrorSeverity.General);
                    return;
                }

                localRegistration = globalNotificationService.Start("BulkFileOperation");
            }
        }

        void StopBulkFileOperationNotification()
        {
            lock (gate)
            {
                // localRegistration may be null if BulkFileOperation was already in the middle of running.  So we
                // explicitly do not assert that is is non-null here.
                localRegistration?.Dispose();
                localRegistration = null;
            }
        }
    }
}
