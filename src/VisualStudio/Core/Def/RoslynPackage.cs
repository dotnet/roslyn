// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.ColorSchemes;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Logging;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interactive;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.RuleSets;
using Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;
using Microsoft.VisualStudio.LanguageServices.Implementation.SyncNamespaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;
using Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Setup
{
    [Guid(Guids.RoslynPackageIdString)]

    // The option page configuration is duplicated in PackageRegistration.pkgdef
    [ProvideToolWindow(typeof(ValueTracking.ValueTrackingToolWindow))]
    [ProvideToolWindow(typeof(StackTraceExplorerToolWindow))]
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

            cancellationToken.ThrowIfCancellationRequested();

            // Ensure the options persisters are loaded since we have to fetch options from the shell
            LoadOptionPersistersAsync(this.ComponentModel, cancellationToken).Forget();

            _workspace = this.ComponentModel.GetService<VisualStudioWorkspace>();

            await InitializeColorsAsync(cancellationToken).ConfigureAwait(true);

            // load some services that have to be loaded in UI thread
            LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(cancellationToken).Forget();

            _solutionEventMonitor = new SolutionEventMonitor(_workspace);

            TrackBulkFileOperations();

            var settingsEditorFactory = this.ComponentModel.GetService<SettingsEditorFactory>();
            RegisterEditorFactory(settingsEditorFactory);

            // Misc workspace has to be up and running by the time our package is usable so that it can track running
            // doc events and appropriately map files to/from it and other relevant workspaces (like the
            // metadata-as-source workspace).
            await this.ComponentModel.GetService<MiscellaneousFilesWorkspace>().InitializeAsync(this).ConfigureAwait(false);
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

        private async Task InitializeColorsAsync(CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            _colorSchemeApplier = ComponentModel.GetService<ColorSchemeApplier>();
            await _colorSchemeApplier.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override async Task LoadComponentsAsync(CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;

            await GetServiceAsync(typeof(SVsTaskStatusCenterService)).ConfigureAwait(false);
            await GetServiceAsync(typeof(SVsErrorList)).ConfigureAwait(false);
            await GetServiceAsync(typeof(SVsSolution)).ConfigureAwait(false);
            await GetServiceAsync(typeof(SVsShell)).ConfigureAwait(false);
            await GetServiceAsync(typeof(SVsRunningDocumentTable)).ConfigureAwait(false);
            await GetServiceAsync(typeof(SVsTextManager)).ConfigureAwait(false);

            // we need to load it as early as possible since we can have errors from
            // package from each language very early
            await this.ComponentModel.GetService<TaskCenterSolutionAnalysisProgressReporter>().InitializeAsync(this).ConfigureAwait(false);
            await this.ComponentModel.GetService<VisualStudioSuppressionFixService>().InitializeAsync(this).ConfigureAwait(false);
            await this.ComponentModel.GetService<VisualStudioDiagnosticListTableCommandHandler>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
            await this.ComponentModel.GetService<VisualStudioDiagnosticListSuppressionStateService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);

            await this.ComponentModel.GetService<VisualStudioMetadataAsSourceFileSupportService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);

            // Load and initialize the add solution item service so ConfigurationUpdater can use it to create editorconfig files.
            await this.ComponentModel.GetService<VisualStudioAddSolutionItemService>().InitializeAsync(this).ConfigureAwait(false);

            await this.ComponentModel.GetService<IVisualStudioDiagnosticAnalyzerService>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
            await this.ComponentModel.GetService<RemoveUnusedReferencesCommandHandler>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);
            await this.ComponentModel.GetService<SyncNamespacesCommandHandler>().InitializeAsync(this, cancellationToken).ConfigureAwait(false);

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

            await LoadInteractiveMenusAsync(cancellationToken).ConfigureAwait(true);
            await LoadStackTraceExplorerMenusAsync(cancellationToken).ConfigureAwait(true);

            // Initialize keybinding reset detector
            await ComponentModel.DefaultExportProvider.GetExportedValue<KeybindingReset.KeybindingResetDetector>().InitializeAsync().ConfigureAwait(true);
        }

        private async Task LoadInteractiveMenusAsync(CancellationToken cancellationToken)
        {
            // Obtain services and QueryInterface from the main thread
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var menuCommandService = (OleMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);
            var monitorSelectionService = (IVsMonitorSelection)await GetServiceAsync(typeof(SVsShellMonitorSelection)).ConfigureAwait(true);

            // Switch to the background object for constructing commands
            await TaskScheduler.Default;

            var threadingContext = ComponentModel.GetService<IThreadingContext>();

            await new CSharpResetInteractiveMenuCommand(menuCommandService, monitorSelectionService, ComponentModel, threadingContext)
                .InitializeResetInteractiveFromProjectCommandAsync()
                .ConfigureAwait(true);

            await new VisualBasicResetInteractiveMenuCommand(menuCommandService, monitorSelectionService, ComponentModel, threadingContext)
                .InitializeResetInteractiveFromProjectCommandAsync()
                .ConfigureAwait(true);
        }

        private async Task LoadStackTraceExplorerMenusAsync(CancellationToken cancellationToken)
        {
            // Obtain services and QueryInterface from the main thread
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var menuCommandService = (OleMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);
            StackTraceExplorerCommandHandler.Initialize(menuCommandService, this);
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

        private static void ReportSessionWideTelemetry()
        {
            SolutionLogger.ReportTelemetry();
            AsyncCompletionLogger.ReportTelemetry();
            CompletionProvidersLogger.ReportTelemetry();
            ChangeSignatureLogger.ReportTelemetry();
            InheritanceMarginLogger.ReportTelemetry();
        }

        private void DisposeVisualStudioServices()
        {
            if (_workspace != null)
            {
                _workspace.Services.GetRequiredService<VisualStudioMetadataReferenceManager>().DisconnectFromVisualStudioNativeServices();
            }
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
            IDisposable? localRegistration = null;

            BulkFileOperation.Begin += (s, a) => StartBulkFileOperationNotification();
            BulkFileOperation.End += (s, a) => StopBulkFileOperationNotification();

            return;

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
                        FatalError.ReportAndCatch(new InvalidOperationException("BulkFileOperation already exist"), ErrorSeverity.General);
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
