// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Logging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Versions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Experimentation;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interactive;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.RuleSets;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Setup
{
    [Guid(Guids.RoslynPackageIdString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", version: 17)]
    internal class RoslynPackage : AbstractPackage
    {
        private VisualStudioWorkspace _workspace;
        private WorkspaceFailureOutputPane _outputPane;
        private IComponentModel _componentModel;
        private RuleSetEventHandler _ruleSetEventHandler;
        private IDisposable _solutionEventMonitor;
        private SolutionUserOptionsProvider _solutionUserOptionsProvider;
        private AnalyzerConfigDocumentAsSolutionItemHandler _analyzerConfigDocumentAsSolutionItemHandler;

        public RoslynPackage()
        {
            foreach (var optionName in SolutionUserOptionNames.AllOptionNames)
            {
                AddOptionKey(optionName);
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            Assumes.Present(_componentModel);

            FatalError.Handler = FailFast.OnFatalException;
            FatalError.NonFatalHandler = WatsonReporter.Report;

            // We also must set the FailFast handler for the compiler layer as well
            var compilerAssembly = typeof(Compilation).Assembly;
            var compilerFatalError = compilerAssembly.GetType("Microsoft.CodeAnalysis.FatalError", throwOnError: true);
            var property = compilerFatalError.GetProperty(nameof(FatalError.Handler), BindingFlags.Static | BindingFlags.Public);
            var compilerFailFast = compilerAssembly.GetType(typeof(FailFast).FullName, throwOnError: true);
            var method = compilerFailFast.GetMethod(nameof(FailFast.OnFatalException), BindingFlags.Static | BindingFlags.NonPublic);
            property.SetValue(null, Delegate.CreateDelegate(property.PropertyType, method));

            _workspace = _componentModel.GetService<VisualStudioWorkspace>();
            _workspace.Services.GetService<IExperimentationService>();

            // Ensure the options persisters are loaded since we have to fetch options from the shell
            _componentModel.GetExtensions<IOptionPersister>();

            _analyzerConfigDocumentAsSolutionItemHandler = this.ComponentModel.GetService<AnalyzerConfigDocumentAsSolutionItemHandler>();

            RoslynTelemetrySetup.Initialize(this);

            // set workspace output pane
            _outputPane = new WorkspaceFailureOutputPane(_componentModel.GetService<IThreadingContext>(), this, _workspace);

            InitializeColors();

            // load some services that have to be loaded in UI thread
            LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(cancellationToken).Forget();

            _solutionEventMonitor = new SolutionEventMonitor(_workspace);

            TrackBulkFileOperations();
        }

        private void InitializeColors()
        {
            // Use VS color keys in order to support theming.
            CodeAnalysisColors.SystemCaptionTextColorKey = EnvironmentColors.SystemWindowTextColorKey;
            CodeAnalysisColors.SystemCaptionTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
            CodeAnalysisColors.CheckBoxTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
            CodeAnalysisColors.BackgroundBrushKey = VsBrushes.CommandBarGradientBeginKey;
            CodeAnalysisColors.ButtonStyleKey = VsResourceKeys.ButtonStyleKey;
            CodeAnalysisColors.AccentBarColorKey = EnvironmentColors.FileTabInactiveDocumentBorderEdgeBrushKey;
        }

        protected override async Task LoadComponentsAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await GetServiceAsync(typeof(SVsTaskStatusCenterService)).ConfigureAwait(true);
            await GetServiceAsync(typeof(SVsErrorList)).ConfigureAwait(true);
            await GetServiceAsync(typeof(SVsSolution)).ConfigureAwait(true);
            await GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
            await GetServiceAsync(typeof(SVsRunningDocumentTable)).ConfigureAwait(true);
            await GetServiceAsync(typeof(SVsTextManager)).ConfigureAwait(true);

            // we need to load it as early as possible since we can have errors from
            // package from each language very early
            this.ComponentModel.GetService<TaskCenterSolutionAnalysisProgressReporter>();
            this.ComponentModel.GetService<VisualStudioDiagnosticListTableCommandHandler>().Initialize(this);

            this.ComponentModel.GetService<VisualStudioMetadataAsSourceFileSupportService>();
            this.ComponentModel.GetService<VirtualMemoryNotificationListener>();

            // The misc files workspace needs to be loaded on the UI thread.  This way it will have
            // the appropriate task scheduler to report events on.
            this.ComponentModel.GetService<MiscellaneousFilesWorkspace>();

            LoadAnalyzerNodeComponents();

            LoadComponentsBackgroundAsync(cancellationToken).Forget();
        }

        private async Task LoadComponentsBackgroundAsync(CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;

            await LoadInteractiveMenusAsync(cancellationToken).ConfigureAwait(true);

            // Initialize any experiments async
            var experiments = this.ComponentModel.DefaultExportProvider.GetExportedValues<IExperiment>();
            foreach (var experiment in experiments)
            {
                await experiment.InitializeAsync().ConfigureAwait(true);
            }
        }

        private async Task LoadInteractiveMenusAsync(CancellationToken cancellationToken)
        {
            // Obtain services and QueryInterface from the main thread
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var menuCommandService = (OleMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);
            var monitorSelectionService = (IVsMonitorSelection)await GetServiceAsync(typeof(SVsShellMonitorSelection)).ConfigureAwait(true);

            // Switch to the background object for constructing commands
            await TaskScheduler.Default;

            await new CSharpResetInteractiveMenuCommand(menuCommandService, monitorSelectionService, ComponentModel)
                .InitializeResetInteractiveFromProjectCommandAsync()
                .ConfigureAwait(true);

            await new VisualBasicResetInteractiveMenuCommand(menuCommandService, monitorSelectionService, ComponentModel)
                .InitializeResetInteractiveFromProjectCommandAsync()
                .ConfigureAwait(true);
        }

        internal IComponentModel ComponentModel
        {
            get
            {
                return _componentModel ?? throw new InvalidOperationException($"Cannot use {nameof(RoslynPackage)}.{nameof(ComponentModel)} prior to initialization.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            DisposeVisualStudioServices();

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
            PersistedVersionStampLogger.ReportTelemetry();
            LinkedFileDiffMergingLogger.ReportTelemetry();
            SolutionLogger.ReportTelemetry();
            AsyncCompletionLogger.ReportTelemetry();
            CompletionProvidersLogger.ReportTelemetry();
            SyntacticLspLogger.ReportTelemetry();
        }

        private void DisposeVisualStudioServices()
        {
            if (_workspace != null)
            {
                _workspace.Services.GetService<VisualStudioMetadataReferenceManager>().DisconnectFromVisualStudioNativeServices();
            }
        }

        private void LoadAnalyzerNodeComponents()
        {
            this.ComponentModel.GetService<IAnalyzerNodeSetup>().Initialize(this);

            _ruleSetEventHandler = this.ComponentModel.GetService<RuleSetEventHandler>();
            if (_ruleSetEventHandler != null)
            {
                _ruleSetEventHandler.Register();
            }
        }

        private void UnregisterAnalyzerTracker()
        {
            this.ComponentModel.GetService<IAnalyzerNodeSetup>().Unregister();
        }

        private void UnregisterRuleSetEventHandler()
        {
            if (_ruleSetEventHandler != null)
            {
                _ruleSetEventHandler.Unregister();
                _ruleSetEventHandler = null;
            }
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            if (_solutionUserOptionsProvider == null)
            {
                // NOTE: This method is called before Initialize, so we cannot use "this.ComponentModel"
                var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
                _solutionUserOptionsProvider = componentModel.GetService<SolutionUserOptionsProvider>();
                _solutionUserOptionsProvider.Initialize((IVsSolution)this.GetService(typeof(SVsSolution)));
            }

            _solutionUserOptionsProvider?.OnLoadOption(key, stream);
            base.OnLoadOptions(key, stream);
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            _solutionUserOptionsProvider?.OnSaveOption(key, stream);
            base.OnSaveOptions(key, stream);
        }

        private void TrackBulkFileOperations()
        {
            // we will pause whatever ambient work loads we have that are tied to IGlobalOperationNotificationService
            // such as solution crawler, pre-emptive remote host synchronization and etc. any background work users didn't
            // explicitly asked for.
            //
            // this should give all resources to BulkFileOperation. we do same for things like build, 
            // debugging, wait dialog and etc. BulkFileOperation is used for things like git branch switching and etc.
            var globalNotificationService = _workspace.Services.GetService<IGlobalOperationNotificationService>();

            // BulkFileOperation can't have nested events. there will be ever only 1 events (Begin/End)
            // so we only need simple tracking.
            var gate = new object();
            GlobalOperationRegistration localRegistration = null;

            BulkFileOperation.End += (s, a) =>
            {
                StopBulkFileOperationNotification();
            };

            BulkFileOperation.Begin += (s, a) =>
            {
                StartBulkFileOperationNotification();
            };

            void StartBulkFileOperationNotification()
            {
                lock (gate)
                {
                    // this shouldn't happen, but we are using external component
                    // so guarding us from them
                    if (localRegistration != null)
                    {
                        WatsonReporter.Report(new Exception("BulkFileOperation already exist"));
                        return;
                    }

                    localRegistration = globalNotificationService.Start("BulkFileOperation");
                }
            }

            void StopBulkFileOperationNotification()
            {
                lock (gate)
                {
                    // this can happen if BulkFileOperation was already in the middle
                    // of running. to make things simpler, decide to not use IsInProgress
                    // which we need to worry about race case.
                    if (localRegistration == null)
                    {
                        return;
                    }

                    localRegistration.Dispose();
                    localRegistration = null;
                }
            }
        }
    }
}
