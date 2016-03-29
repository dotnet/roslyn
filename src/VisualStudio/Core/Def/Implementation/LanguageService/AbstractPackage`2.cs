// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using static Microsoft.CodeAnalysis.Utilities.ForegroundThreadDataKind;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractPackage<TPackage, TLanguageService> : Package
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        private ForegroundThreadAffinitizedObject _foregroundObject;
        private TLanguageService _languageService;
        private MiscellaneousFilesWorkspace _miscellaneousFilesWorkspace;

        public VisualStudioWorkspaceImpl Workspace { get; private set; }

        protected AbstractPackage()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            // Assume that we are being initialized on the UI thread at this point.
            var defaultForegroundThreadData = ForegroundThreadData.CreateDefault(defaultKind: ForcedByPackageInitialize);
            ForegroundThreadAffinitizedObject.CurrentForegroundThreadData = defaultForegroundThreadData;
            _foregroundObject = new ForegroundThreadAffinitizedObject(defaultForegroundThreadData);

            foreach (var editorFactory in CreateEditorFactories())
            {
                RegisterEditorFactory(editorFactory);
            }

            RegisterLanguageService(typeof(TLanguageService), () =>
            {
                // Create the language service, tell it to set itself up, then store it in a field
                // so we can notify it that it's time to clean up.
                _languageService = CreateLanguageService();
                _languageService.Setup();
                return _languageService.ComAggregate;
            });

            // Okay, this is also a bit strange.  We need to get our Interop dll into our process,
            // but we're in the GAC.  Ask the base Roslyn Package to load, and it will take care of
            // it for us.
            // * NOTE * workspace should never be created before loading roslyn package since roslyn package
            //          installs a service roslyn visual studio workspace requires
            IVsPackage setupPackage;
            var shell = (IVsShell)this.GetService(typeof(SVsShell));
            shell.LoadPackage(Guids.RoslynPackageId, out setupPackage);

            _miscellaneousFilesWorkspace = this.ComponentModel.GetService<MiscellaneousFilesWorkspace>();
            if (_miscellaneousFilesWorkspace != null)
            {
                // make sure solution crawler start once everything has been setup.
                _miscellaneousFilesWorkspace.StartSolutionCrawler();
            }

            RegisterMiscellaneousFilesWorkspaceInformation(_miscellaneousFilesWorkspace);

            this.Workspace = this.CreateWorkspace();
            if (this.Workspace != null)
            {
                // make sure solution crawler start once everything has been setup.
                // this also should be started before any of workspace events start firing
                this.Workspace.StartSolutionCrawler();
            }

            // Ensure services that must be created on the UI thread have been.
            HACK_AbstractCreateServicesOnUiThread.CreateServicesOnUIThread(ComponentModel, RoslynLanguageName);
        }

        protected abstract VisualStudioWorkspaceImpl CreateWorkspace();

        internal IComponentModel ComponentModel
        {
            get
            {
                _foregroundObject.AssertIsForeground();

                return (IComponentModel)GetService(typeof(SComponentModel));
            }
        }

        protected abstract void RegisterMiscellaneousFilesWorkspaceInformation(MiscellaneousFilesWorkspace miscellaneousFilesWorkspace);

        protected abstract IEnumerable<IVsEditorFactory> CreateEditorFactories();
        protected abstract TLanguageService CreateLanguageService();

        protected void RegisterService<T>(Func<T> serviceCreator)
        {
            ((IServiceContainer)this).AddService(typeof(T), (container, type) => serviceCreator(), promote: true);
        }

        // When registering a language service, we need to take its ComAggregate wrapper.
        protected void RegisterLanguageService(Type t, Func<object> serviceCreator)
        {
            ((IServiceContainer)this).AddService(t, (container, type) => serviceCreator(), promote: true);
        }

        protected override void Dispose(bool disposing)
        {
            if (_miscellaneousFilesWorkspace != null)
            {
                _miscellaneousFilesWorkspace.StopSolutionCrawler();
            }

            if (this.Workspace != null)
            {
                this.Workspace.StopSolutionCrawler();
            }

            // If we've created the language service then tell it it's time to clean itself up now.
            if (_languageService != null)
            {
                _languageService.TearDown();
                _languageService = null;
            }

            base.Dispose(disposing);
        }

        protected abstract string RoslynLanguageName { get; }
    }
}
