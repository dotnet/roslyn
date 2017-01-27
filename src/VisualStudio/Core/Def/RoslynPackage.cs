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
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Versions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interactive;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.RuleSets;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.LanguageServices.Packaging;
using Microsoft.VisualStudio.LanguageServices.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using static Microsoft.CodeAnalysis.Utilities.ForegroundThreadDataKind;
using Task = System.Threading.Tasks.Task;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Setup
{
    [Guid(Guids.RoslynPackageIdString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", version: 16)]
    internal class RoslynPackage : AbstractPackage
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

            FatalError.Handler = FailFast.OnFatalException;
            FatalError.NonFatalHandler = WatsonReporter.Report;

            // We also must set the FailFast handler for the compiler layer as well
            var compilerAssembly = typeof(Compilation).Assembly;
            var compilerFatalError = compilerAssembly.GetType("Microsoft.CodeAnalysis.FatalError", throwOnError: true);
            var property = compilerFatalError.GetProperty(nameof(FatalError.Handler), BindingFlags.Static | BindingFlags.Public);
            var compilerFailFast = compilerAssembly.GetType(typeof(FailFast).FullName, throwOnError: true);
            var method = compilerFailFast.GetMethod(nameof(FailFast.OnFatalException), BindingFlags.Static | BindingFlags.NonPublic);
            property.SetValue(null, Delegate.CreateDelegate(property.PropertyType, method));

            RegisterFindResultsLibraryManager();

            var componentModel = (IComponentModel)this.GetService(typeof(SComponentModel));
            _workspace = componentModel.GetService<VisualStudioWorkspace>();

            // Ensure the options persisters are loaded since we have to fetch options from the shell
            componentModel.GetExtensions<IOptionPersister>();

            RoslynTelemetrySetup.Initialize(this);

            // set workspace output pane
            _outputPane = new WorkspaceFailureOutputPane(this, _workspace);

            InitializeColors();

            // load some services that have to be loaded in UI thread
            LoadComponentsInUIContextOnceSolutionFullyLoaded();

            _solutionEventMonitor = new SolutionEventMonitor(_workspace);
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

        protected override void LoadComponentsInUIContext()
        {
            // we need to load it as early as possible since we can have errors from
            // package from each language very early
            this.ComponentModel.GetService<VisualStudioDiagnosticListTable>();
            this.ComponentModel.GetService<VisualStudioTodoListTable>();
            this.ComponentModel.GetService<VisualStudioDiagnosticListTableCommandHandler>().Initialize(this);

            this.ComponentModel.GetService<HACK_ThemeColorFixer>();
            this.ComponentModel.GetExtensions<IDefinitionsAndReferencesPresenter>();
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
            PersistedVersionStampLogger.LogSummary();
            LinkedFileDiffMergingLogger.ReportTelemetry();
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

        private void DisposeVisualStudioServices()
        {
            if (_workspace != null)
            {
                var documentTrackingService = _workspace.Services.GetService<IDocumentTrackingService>() as VisualStudioDocumentTrackingService;
                documentTrackingService.Dispose();

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
    }
}
