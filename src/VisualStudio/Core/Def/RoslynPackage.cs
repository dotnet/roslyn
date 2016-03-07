// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Versions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.RuleSets;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interactive;
using static Microsoft.CodeAnalysis.Utilities.ForegroundThreadDataKind;

namespace Microsoft.VisualStudio.LanguageServices.Setup
{
    [Guid(Guids.RoslynPackageIdString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", version: 12)]
    internal class RoslynPackage : Package
    {
        private LibraryManager _libraryManager;
        private uint _libraryManagerCookie;
        private VisualStudioWorkspace _workspace;
        private WorkspaceFailureOutputPane _outputPane;
        private IComponentModel _componentModel;
        private RuleSetEventHandler _ruleSetEventHandler;
        private IDisposable _solutionEventMonitor;

        protected override void Initialize()
        {
            base.Initialize();

            // Assume that we are being initialized on the UI thread at this point.
            ForegroundThreadAffinitizedObject.CurrentForegroundThreadData = ForegroundThreadData.CreateDefault(defaultKind: ForcedByPackageInitialize);
            Debug.Assert(ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.Kind != Unknown);

            FatalError.Handler = FailFast.OnFatalException;
            FatalError.NonFatalHandler = WatsonReporter.Report;

            // We also must set the FailFast handler for the compiler layer as well
            var compilerAssembly = typeof(Compilation).Assembly;
            var compilerFatalError = compilerAssembly.GetType("Microsoft.CodeAnalysis.FatalError", throwOnError: true);
            var property = compilerFatalError.GetProperty(nameof(FatalError.Handler), BindingFlags.Static | BindingFlags.Public);
            var compilerFailFast = compilerAssembly.GetType(typeof(FailFast).FullName, throwOnError: true);
            var method = compilerFailFast.GetMethod(nameof(FailFast.OnFatalException), BindingFlags.Static | BindingFlags.NonPublic);
            property.SetValue(null, Delegate.CreateDelegate(property.PropertyType, method));

            InitializePortableShim(compilerAssembly);

            RegisterFindResultsLibraryManager();

            var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
            _workspace = componentModel.GetService<VisualStudioWorkspace>();

            var telemetrySetupExtensions = componentModel.GetExtensions<IRoslynTelemetrySetup>();
            foreach (var telemetrySetup in telemetrySetupExtensions)
            {
                telemetrySetup.Initialize(this);
            }

            // set workspace output pane
            _outputPane = new WorkspaceFailureOutputPane(this, _workspace);

            InitializeColors();

            // load some services that have to be loaded in UI thread
            LoadComponentsInUIContext();

            _solutionEventMonitor = new SolutionEventMonitor(_workspace);
        }

        private static void InitializePortableShim(Assembly compilerAssembly)
        {
            // We eagerly force all of the types to be loaded within the PortableShim because there are scenarios
            // in which loading types can trigger the current AppDomain's AssemblyResolve or TypeResolve events.
            // If handlers of those events do anything that would cause PortableShim to be accessed recursively,
            // bad things can happen. In particular, this fixes the scenario described in TFS bug #1185842.
            //
            // Note that the fix below is written to be as defensive as possible to do no harm if it impacts other
            // scenarios.

            // Initialize the PortableShim linked into the Workspaces layer.
            Roslyn.Utilities.PortableShim.Initialize();

            // Initialize the PortableShim linked into the Compilers layer via reflection.
            var compilerPortableShim = compilerAssembly.GetType("Roslyn.Utilities.PortableShim", throwOnError: false);
            var initializeMethod = compilerPortableShim?.GetMethod(nameof(Roslyn.Utilities.PortableShim.Initialize), BindingFlags.Static | BindingFlags.NonPublic);
            initializeMethod?.Invoke(null, null);
        }

        private void InitializeColors()
        {
            // Use VS color keys in order to support theming.
            CodeAnalysisColors.SystemCaptionTextColorKey = EnvironmentColors.SystemWindowTextColorKey;
            CodeAnalysisColors.SystemCaptionTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
            CodeAnalysisColors.CheckBoxTextBrushKey = EnvironmentColors.SystemWindowTextBrushKey;
            CodeAnalysisColors.RenameErrorTextBrushKey = VSCodeAnalysisColors.RenameErrorTextBrushKey;
            CodeAnalysisColors.RenameResolvableConflictTextBrushKey = VSCodeAnalysisColors.RenameResolvableConflictTextBrushKey;
            CodeAnalysisColors.BackgroundBrushKey = VsBrushes.CommandBarGradientBeginKey;
            CodeAnalysisColors.ButtonStyleKey = VsResourceKeys.ButtonStyleKey;
            CodeAnalysisColors.AccentBarColorKey = EnvironmentColors.FileTabInactiveDocumentBorderEdgeBrushKey;
        }

        private void LoadComponentsInUIContext()
        {
            if (KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive)
            {
                // if we are already in the right UI context, load it right away
                LoadComponents();
            }
            else
            {
                // load them when it is a right context.
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged += OnSolutionExistsAndFullyLoadedContext;
            }
        }

        private void OnSolutionExistsAndFullyLoadedContext(object sender, UIContextChangedEventArgs e)
        {
            if (e.Activated)
            {
                // unsubscribe from it
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged -= OnSolutionExistsAndFullyLoadedContext;

                // load components
                LoadComponents();
            }
        }

        private void LoadComponents()
        {
            // we need to load it as early as possible since we can have errors from
            // package from each language very early
            this.ComponentModel.GetService<VisualStudioDiagnosticListTable>();
            this.ComponentModel.GetService<VisualStudioTodoListTable>();
            this.ComponentModel.GetService<VisualStudioDiagnosticListTableCommandHandler>().Initialize(this);

            this.ComponentModel.GetService<HACK_ThemeColorFixer>();
            this.ComponentModel.GetExtensions<IReferencedSymbolsPresenter>();
            this.ComponentModel.GetExtensions<INavigableItemsPresenter>();
            this.ComponentModel.GetService<VisualStudioMetadataAsSourceFileSupportService>();
            this.ComponentModel.GetService<VirtualMemoryNotificationListener>();

            // The misc files workspace needs to be loaded on the UI thread.  This way it will have
            // the appropriate task scheduler to report events on.
            this.ComponentModel.GetService<MiscellaneousFilesWorkspace>();

            LoadAnalyzerNodeComponents();

            Task.Run(() => LoadComponentsBackground());
        }

        private void LoadComponentsBackground()
        {
            // Perf: Initialize the command handlers.
            var commandHandlerServiceFactory = this.ComponentModel.GetService<ICommandHandlerServiceFactory>();
            commandHandlerServiceFactory.Initialize(ContentTypeNames.RoslynContentType);
            LoadInteractiveMenus();

            this.ComponentModel.GetService<MiscellaneousTodoListTable>();
            this.ComponentModel.GetService<MiscellaneousDiagnosticListTable>();
        }

        private void LoadInteractiveMenus()
        {
            var menuCommandService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            var monitorSelectionService = (IVsMonitorSelection)this.GetService(typeof(SVsShellMonitorSelection));

            new CSharpResetInteractiveMenuCommand(menuCommandService, monitorSelectionService, ComponentModel)
                .InitializeResetInteractiveFromProjectCommand();

            new VisualBasicResetInteractiveMenuCommand(menuCommandService, monitorSelectionService, ComponentModel)
                .InitializeResetInteractiveFromProjectCommand();
        }

        internal IComponentModel ComponentModel
        {
            get
            {
                if (_componentModel == null)
                {
                    _componentModel = (IComponentModel)GetService(typeof(SComponentModel));
                }

                return _componentModel;
            }
        }

        protected override void Dispose(bool disposing)
        {
            UnregisterFindResultsLibraryManager();

            DisposeVisualStudioDocumentTrackingService();

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
            PersistedVersionStampLogger.LogSummary();
        }

        private void RegisterFindResultsLibraryManager()
        {
            var objectManager = this.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
            if (objectManager != null)
            {
                _libraryManager = new LibraryManager(this);

                if (ErrorHandler.Failed(objectManager.RegisterSimpleLibrary(_libraryManager, out _libraryManagerCookie)))
                {
                    _libraryManagerCookie = 0;
                }

                ((IServiceContainer)this).AddService(typeof(LibraryManager), _libraryManager, promote: true);
            }
        }

        private void UnregisterFindResultsLibraryManager()
        {
            if (_libraryManagerCookie != 0)
            {
                var objectManager = this.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
                if (objectManager != null)
                {
                    objectManager.UnregisterLibrary(_libraryManagerCookie);
                    _libraryManagerCookie = 0;
                }

                ((IServiceContainer)this).RemoveService(typeof(LibraryManager), promote: true);
                _libraryManager = null;
            }
        }

        private void DisposeVisualStudioDocumentTrackingService()
        {
            if (_workspace != null)
            {
                var documentTrackingService = _workspace.Services.GetService<IDocumentTrackingService>() as VisualStudioDocumentTrackingService;
                documentTrackingService.Dispose();
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
    }
}
