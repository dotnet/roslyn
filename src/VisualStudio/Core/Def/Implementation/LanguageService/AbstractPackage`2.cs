// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Packaging;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Microsoft.VisualStudio.LanguageServices.SymbolSearch;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractPackage<TPackage, TLanguageService> : AbstractPackage
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        private TLanguageService _languageService;

        private PackageInstallerService _packageInstallerService;
        private VisualStudioSymbolSearchService _symbolSearchService;

        public VisualStudioWorkspaceImpl Workspace { get; private set; }

        protected AbstractPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = (IVsShell)await GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);
            var solution = (IVsSolution)await GetServiceAsync(typeof(SVsSolution)).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            Assumes.Present(shell);
            Assumes.Present(solution);

            foreach (var editorFactory in CreateEditorFactories())
            {
                RegisterEditorFactory(editorFactory);
            }

            RegisterLanguageService(typeof(TLanguageService), async ct =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(ct);

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
            shell.LoadPackage(Guids.RoslynPackageId, out var setupPackage);

            var miscellaneousFilesWorkspace = this.ComponentModel.GetService<MiscellaneousFilesWorkspace>();
            RegisterMiscellaneousFilesWorkspaceInformation(miscellaneousFilesWorkspace);

            this.Workspace = this.CreateWorkspace();
            if (await IsInIdeModeAsync(this.Workspace, cancellationToken).ConfigureAwait(true))
            {
                // start remote host
                EnableRemoteHostClientService();

                // not every derived package support object browser and for those languages
                // this is a no op
                await RegisterObjectBrowserLibraryManagerAsync(cancellationToken).ConfigureAwait(true);
            }

            LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(cancellationToken).Forget();
        }

        protected override async Task LoadComponentsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Ensure the nuget package services are initialized after we've loaded
            // the solution.
            _packageInstallerService = Workspace.Services.GetService<IPackageInstallerService>() as PackageInstallerService;
            _symbolSearchService = Workspace.Services.GetService<ISymbolSearchService>() as VisualStudioSymbolSearchService;

            _packageInstallerService?.Connect(this.RoslynLanguageName);
            _symbolSearchService?.Connect(this.RoslynLanguageName);

            HACK_AbstractCreateServicesOnUiThread.CreateServicesOnUIThread(ComponentModel, RoslynLanguageName);
        }

        protected abstract VisualStudioWorkspaceImpl CreateWorkspace();

        internal IComponentModel ComponentModel
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return (IComponentModel)GetService(typeof(SComponentModel));
            }
        }

        protected abstract void RegisterMiscellaneousFilesWorkspaceInformation(MiscellaneousFilesWorkspace miscellaneousFilesWorkspace);

        protected abstract IEnumerable<IVsEditorFactory> CreateEditorFactories();
        protected abstract TLanguageService CreateLanguageService();

        protected void RegisterService<T>(Func<CancellationToken, Task<T>> serviceCreator)
        {
            AddService(typeof(T), async (container, cancellationToken, type) => await serviceCreator(cancellationToken).ConfigureAwait(true), promote: true);
        }

        // When registering a language service, we need to take its ComAggregate wrapper.
        protected void RegisterLanguageService(Type t, Func<CancellationToken, Task<object>> serviceCreator)
        {
            AddService(t, async (container, cancellationToken, type) => await serviceCreator(cancellationToken).ConfigureAwait(true), promote: true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    if (await IsInIdeModeAsync(this.Workspace, CancellationToken.None).ConfigureAwait(true))
                    {
                        DisableRemoteHostClientService();

                        await UnregisterObjectBrowserLibraryManagerAsync(CancellationToken.None).ConfigureAwait(true);
                    }
                });

                // If we've created the language service then tell it it's time to clean itself up now.
                if (_languageService != null)
                {
                    _languageService.TearDown();
                    _languageService = null;
                }
            }

            base.Dispose(disposing);
        }

        protected abstract string RoslynLanguageName { get; }

        protected virtual Task RegisterObjectBrowserLibraryManagerAsync(CancellationToken cancellationToken)
        {
            // it is virtual rather than abstract to not break other languages which derived from our
            // base package implementations
            return Task.CompletedTask;
        }

        protected virtual Task UnregisterObjectBrowserLibraryManagerAsync(CancellationToken cancellationToken)
        {
            // it is virtual rather than abstract to not break other languages which derived from our
            // base package implementations
            return Task.CompletedTask;
        }

        private async Task<bool> IsInIdeModeAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            return workspace != null && !await IsInCommandLineModeAsync(cancellationToken).ConfigureAwait(true);
        }

        private async Task<bool> IsInCommandLineModeAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var shell = (IVsShell)await GetServiceAsync(typeof(SVsShell)).ConfigureAwait(true);

            if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out var result)))
            {
                return (bool)result;
            }

            return false;
        }

        private void EnableRemoteHostClientService()
        {
            ((RemoteHostClientServiceFactory.RemoteHostClientService)this.Workspace.Services.GetService<IRemoteHostClientService>()).Enable();
        }

        private void DisableRemoteHostClientService()
        {
            ((RemoteHostClientServiceFactory.RemoteHostClientService)this.Workspace.Services.GetService<IRemoteHostClientService>()).Disable();
        }
    }
}
