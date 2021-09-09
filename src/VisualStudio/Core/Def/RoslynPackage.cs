﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Logging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.ColorSchemes;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interactive;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.RuleSets;
using Microsoft.VisualStudio.LanguageServices.Implementation.SyncNamespaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Setup
{
    [Guid(Guids.RoslynPackageIdString)]

    // The option page configuration is duplicated in PackageRegistration.pkgdef
    [ProvideToolWindow(typeof(ValueTracking.ValueTrackingToolWindow))]
    internal sealed class RoslynPackage : AbstractPackage
    {
        // The randomly-generated key name is used for serializing the Background Analysis Scope preference to the .SUO
        // file. It doesn't have any semantic meaning, but is intended to not conflict with any other extension that
        // might be saving an "AnalysisScope" named stream to the same file.
        // note: must be <= 31 characters long
        private const string BackgroundAnalysisScopeOptionKey = "AnalysisScope-DCE33A29A768";
        private const byte BackgroundAnalysisScopeOptionVersion = 1;

        private static RoslynPackage? _lazyInstance;

        private VisualStudioWorkspace? _workspace;
        private IComponentModel? _componentModel;
        private RuleSetEventHandler? _ruleSetEventHandler;
        private ColorSchemeApplier? _colorSchemeApplier;
        private IDisposable? _solutionEventMonitor;

        private BackgroundAnalysisScope? _analysisScope;

        public RoslynPackage()
        {
            // We need to register an option in order for OnLoadOptions/OnSaveOptions to be called
            AddOptionKey(BackgroundAnalysisScopeOptionKey);
        }

        public BackgroundAnalysisScope? AnalysisScope
        {
            get
            {
                return _analysisScope;
            }

            set
            {
                if (_analysisScope == value)
                    return;

                _analysisScope = value;
                AnalysisScopeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? AnalysisScopeChanged;

        internal static async ValueTask<RoslynPackage?> GetOrLoadAsync(IThreadingContext threadingContext, IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            if (_lazyInstance is null)
            {
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var shell = (IVsShell7?)await serviceProvider.GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
                Assumes.Present(shell);
                await shell.LoadPackageAsync(typeof(RoslynPackage).GUID);

                if (ErrorHandler.Succeeded(((IVsShell)shell).IsPackageLoaded(typeof(RoslynPackage).GUID, out var package)))
                {
                    _lazyInstance = (RoslynPackage)package;
                }
            }

            return _lazyInstance;
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            if (key == BackgroundAnalysisScopeOptionKey)
            {
                if (stream.ReadByte() == BackgroundAnalysisScopeOptionVersion)
                {
                    var hasValue = stream.ReadByte() == 1;
                    AnalysisScope = hasValue ? (BackgroundAnalysisScope)stream.ReadByte() : null;
                }
                else
                {
                    AnalysisScope = null;
                }
            }

            base.OnLoadOptions(key, stream);
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            if (key == BackgroundAnalysisScopeOptionKey)
            {
                stream.WriteByte(BackgroundAnalysisScopeOptionVersion);
                stream.WriteByte(AnalysisScope.HasValue ? (byte)1 : (byte)0);
                stream.WriteByte((byte)AnalysisScope.GetValueOrDefault());
            }

            base.OnSaveOptions(key, stream);
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            Assumes.Present(_componentModel);

            // Ensure the options persisters are loaded since we have to fetch options from the shell
            LoadOptionPersistersAsync(_componentModel, cancellationToken).Forget();

            _workspace = _componentModel.GetService<VisualStudioWorkspace>();

            // Fetch the session synchronously on the UI thread; if this doesn't happen before we try using this on
            // the background thread then we will experience hangs like we see in this bug:
            // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=190808 or
            // https://devdiv.visualstudio.com/DevDiv/_workitems?id=296981&_a=edit
            var telemetryService = (VisualStudioWorkspaceTelemetryService)_workspace.Services.GetRequiredService<IWorkspaceTelemetryService>();
            telemetryService.InitializeTelemetrySession(TelemetryService.DefaultSession);

            Logger.Log(FunctionId.Run_Environment,
                KeyValueLogMessage.Create(m => m["Version"] = FileVersionInfo.GetVersionInfo(typeof(VisualStudioWorkspace).Assembly.Location).FileVersion));

            InitializeColors();

            // load some services that have to be loaded in UI thread
            LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(cancellationToken).Forget();

            _solutionEventMonitor = new SolutionEventMonitor(_workspace);

            TrackBulkFileOperations();

            var settingsEditorFactory = _componentModel.GetService<SettingsEditorFactory>();
            RegisterEditorFactory(settingsEditorFactory);
        }

        private async Task LoadOptionPersistersAsync(IComponentModel componentModel, CancellationToken cancellationToken)
        {
            var listenerProvider = componentModel.GetService<IAsynchronousOperationListenerProvider>();
            using var token = listenerProvider.GetListener(FeatureAttribute.Workspace).BeginAsyncOperation(nameof(LoadOptionPersistersAsync));

            // Switch to a background thread to ensure assembly loads don't show up as UI delays attributed to
            // InitializeAsync.
            await TaskScheduler.Default;

            var persisterProviders = componentModel.GetExtensions<IOptionPersisterProvider>().ToImmutableArray();

            foreach (var provider in persisterProviders)
            {
                _ = await provider.GetOrCreatePersisterAsync(cancellationToken).ConfigureAwait(true);
            }
        }

        private void InitializeColors()
        {
            // Initialize ColorScheme support
            _colorSchemeApplier = ComponentModel.GetService<ColorSchemeApplier>();
            _colorSchemeApplier.Initialize();
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

            // The misc files workspace needs to be loaded on the UI thread.  This way it will have
            // the appropriate task scheduler to report events on.
            this.ComponentModel.GetService<MiscellaneousFilesWorkspace>();

            // Load and initialize the add solution item service so ConfigurationUpdater can use it to create editorconfig files.
            this.ComponentModel.GetService<VisualStudioAddSolutionItemService>().Initialize(this);

            this.ComponentModel.GetService<IVisualStudioDiagnosticAnalyzerService>().Initialize(this);
            this.ComponentModel.GetService<RemoveUnusedReferencesCommandHandler>().Initialize(this);
            this.ComponentModel.GetService<SyncNamespacesCommandHandler>().Initialize(this);

            LoadAnalyzerNodeComponents();

            LoadComponentsBackgroundAsync(cancellationToken).Forget();
        }

        // Overrides for VSSDK003 fix 
        // See https://github.com/Microsoft/VSSDK-Analyzers/blob/main/doc/VSSDK003.md
        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
            => toolWindowType == typeof(ValueTracking.ValueTrackingToolWindow).GUID
                ? this
                : base.GetAsyncToolWindowFactory(toolWindowType);

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
                => base.GetToolWindowTitle(toolWindowType, id);

        protected override Task<object?> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
            => Task.FromResult((object?)null);

        private async Task LoadComponentsBackgroundAsync(CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;

            await LoadInteractiveMenusAsync(cancellationToken).ConfigureAwait(true);

            // Initialize keybinding reset detector
            await ComponentModel.DefaultExportProvider.GetExportedValue<KeybindingReset.KeybindingResetDetector>().InitializeAsync().ConfigureAwait(true);
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
            SolutionLogger.ReportTelemetry();
            AsyncCompletionLogger.ReportTelemetry();
            CompletionProvidersLogger.ReportTelemetry();
            ChangeSignatureLogger.ReportTelemetry();
        }

        private void DisposeVisualStudioServices()
        {
            if (_workspace != null)
            {
                _workspace.Services.GetRequiredService<VisualStudioMetadataReferenceManager>().DisconnectFromVisualStudioNativeServices();
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
            => this.ComponentModel.GetService<IAnalyzerNodeSetup>().Unregister();

        private void UnregisterRuleSetEventHandler()
        {
            if (_ruleSetEventHandler != null)
            {
                _ruleSetEventHandler.Unregister();
                _ruleSetEventHandler = null;
            }
        }

        private void TrackBulkFileOperations()
        {
            RoslynDebug.AssertNotNull(_workspace);

            // we will pause whatever ambient work loads we have that are tied to IGlobalOperationNotificationService
            // such as solution crawler, pre-emptive remote host synchronization and etc. any background work users didn't
            // explicitly asked for.
            //
            // this should give all resources to BulkFileOperation. we do same for things like build, 
            // debugging, wait dialog and etc. BulkFileOperation is used for things like git branch switching and etc.
            var globalNotificationService = _workspace.Services.GetRequiredService<IGlobalOperationNotificationService>();

            // BulkFileOperation can't have nested events. there will be ever only 1 events (Begin/End)
            // so we only need simple tracking.
            var gate = new object();
            GlobalOperationRegistration? localRegistration = null;

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
                RoslynDebug.Assert(gate != null);
                RoslynDebug.Assert(globalNotificationService != null);

                lock (gate)
                {
                    // this shouldn't happen, but we are using external component
                    // so guarding us from them
                    if (localRegistration != null)
                    {
                        FatalError.ReportAndCatch(new InvalidOperationException("BulkFileOperation already exist"));
                        return;
                    }

                    localRegistration = globalNotificationService.Start("BulkFileOperation");
                }
            }

            void StopBulkFileOperationNotification()
            {
                RoslynDebug.Assert(gate != null);

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
