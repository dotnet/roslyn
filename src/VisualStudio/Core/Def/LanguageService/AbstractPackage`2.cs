// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Packaging;
using Microsoft.VisualStudio.LanguageServices.SymbolSearch;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract partial class AbstractPackage<TPackage, TLanguageService> : AbstractPackage
    where TPackage : AbstractPackage<TPackage, TLanguageService>
    where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
{
    private PackageInstallerService? _packageInstallerService;
    private VisualStudioSymbolSearchService? _symbolSearchService;

    /// <summary>
    /// Set to 1 if we've already preloaded project system components. Should be updated with <see cref="Interlocked.CompareExchange{T}(ref T, T, T)" />
    /// </summary>
    private int _projectSystemComponentsPreloaded;

    private bool _objectBrowserLibraryManagerRegistered = false;

    protected AbstractPackage()
    {
    }

    protected override void RegisterInitializeAsyncWork(PackageLoadTasks packageInitializationTasks)
    {
        base.RegisterInitializeAsyncWork(packageInitializationTasks);

        packageInitializationTasks.AddTask(isMainThreadTask: false, task: PackageInitializationBackgroundThreadAsync);
    }

    private async Task PackageInitializationBackgroundThreadAsync(PackageLoadTasks packageInitializationTasks, CancellationToken cancellationToken)
    {
        // We still need to ensure the RoslynPackage is loaded, since it's OnAfterPackageLoaded will hook up event handlers in RoslynPackage.LoadComponentsInBackgroundAfterSolutionFullyLoadedAsync.
        // Once that method has been replaced, then this package load can be removed.
        //
        // Rather than triggering a package load via IVsShell (which requires a transition to the UI thread), we request an async service that is free-threaded;
        // this let's us stay on the background and goes through the full background package load path.
        _ = await GetServiceAsync<RoslynPackageLoadService, RoslynPackageLoadService>(throwOnFailure: true, cancellationToken).ConfigureAwait(false);

        AddService(typeof(TLanguageService), async (_, cancellationToken, _) =>
            {
                // Ensure we're on the BG when creating the language service.
                await TaskScheduler.Default;

                var languageService = CreateLanguageService();
                languageService.Setup(cancellationToken);

                // DevDiv 753309:
                // We've redefined some VS interfaces that had incorrect PIAs. When 
                // we interop with native parts of VS, they always QI, so everything
                // works. However, Razor is now managed, but assumes that the C#
                // language service is native. When setting breakpoints, they
                // get the language service from its GUID and cast it to IVsLanguageDebugInfo.
                // This would QI the native lang service. Since we're managed and
                // we've redefined IVsLanguageDebugInfo, the cast
                // fails. To work around this, we put the LS inside a ComAggregate object,
                // which always force a QueryInterface and allow their cast to succeed.
                // 
                // This also fixes 752331, which is a similar problem with the 
                // exception assistant.

                // Creating the com aggregate has to happen on the UI thread.
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                return Interop.ComAggregate.CreateAggregatedObject(languageService);
            },
            promote: true);

        // Misc workspace has to be up and running by the time our package is usable so that it can track running
        // doc events and appropriately map files to/from it and other relevant workspaces (like the
        // metadata-as-source workspace).
        var miscellaneousFilesWorkspace = this.ComponentModel.GetService<MiscellaneousFilesWorkspace>();

        RegisterMiscellaneousFilesWorkspaceInformation(miscellaneousFilesWorkspace);

        var devenv = Path.Combine(AppContext.BaseDirectory, "devenv.exe");
        var version = FileVersionInfo.GetVersionInfo(devenv);
        if (version.FileMajorPart < 18 || (version.FileMajorPart == 18 && version.FileBuildPart < 11304))
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        foreach (var editorFactory in CreateEditorFactories())
        {
            await RegisterEditorFactoryAsync(editorFactory, cancellationToken).ConfigureAwait(true);
        }
    }

    protected override void RegisterOnAfterPackageLoadedAsyncWork(PackageLoadTasks afterPackageLoadedTasks)
    {
        base.RegisterOnAfterPackageLoadedAsyncWork(afterPackageLoadedTasks);

        afterPackageLoadedTasks.AddTask(
            isMainThreadTask: true,
            task: async (packageLoadedTasks, cancellationToken) =>
            {
                if (!await CommandLineMode.IsInCommandLineModeAsync(AsyncServiceProvider.GlobalProvider, cancellationToken).ConfigureAwait(true))
                {
                    // not every derived package support object browser and for those languages
                    // this is a no op
                    RegisterObjectBrowserLibraryManager();

                    _objectBrowserLibraryManagerRegistered = true;
                }
            });
    }

    protected override async Task LoadComponentsInBackgroundAfterSolutionFullyLoadedAsync(CancellationToken cancellationToken)
    {
        // Ensure the nuget package services are initialized. This initialization pass will only run
        // once our package is loaded indirectly through a legacy COM service we proffer (like the legacy project systems
        // loading us) or through something like the IVsEditorFactory or a debugger service. Right now it's fine
        // we only load this there, because we only use these to provide code fixes. But we only show code fixes in
        // open files, and so you would have had to open a file, which loads the editor factory, which loads our package,
        // which will run this.
        //
        // This code will have to be moved elsewhere once any of that load path is changed such that the package
        // no longer loads if a file is opened.
        var workspace = ComponentModel.GetService<VisualStudioWorkspace>();
        _packageInstallerService = workspace.Services.GetService<IPackageInstallerService>() as PackageInstallerService;
        _symbolSearchService = workspace.Services.GetService<ISymbolSearchService>() as VisualStudioSymbolSearchService;

        _packageInstallerService?.RegisterLanguage(this.RoslynLanguageName);
        _symbolSearchService?.RegisterLanguage(this.RoslynLanguageName);
    }

    protected abstract void RegisterMiscellaneousFilesWorkspaceInformation(MiscellaneousFilesWorkspace miscellaneousFilesWorkspace);

    protected abstract IEnumerable<IVsEditorFactory> CreateEditorFactories();
    protected abstract TLanguageService CreateLanguageService();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Per VS core team, Package.Dispose is called on the UI thread.
            Contract.ThrowIfFalse(JoinableTaskFactory.Context.IsOnMainThread);

            if (_objectBrowserLibraryManagerRegistered)
            {
                UnregisterObjectBrowserLibraryManager();
            }
        }

        base.Dispose(disposing);
    }

    protected abstract string RoslynLanguageName { get; }

    protected virtual void RegisterObjectBrowserLibraryManager()
    {
        // it is virtual rather than abstract to not break other languages which derived from our
        // base package implementations
    }

    protected virtual void UnregisterObjectBrowserLibraryManager()
    {
        // it is virtual rather than abstract to not break other languages which derived from our
        // base package implementations
    }

    protected void PreloadProjectSystemComponents()
    {
        if (Interlocked.CompareExchange(ref _projectSystemComponentsPreloaded, value: 1, comparand: 0) == 1)
            return;

        // Preload some components so later uses don't block. This is specifically to help out csproj and msvbprj project systems. They push changes
        // to us on the UI thread as fundamental part of their design. This causes blocking on the UI thread as we create MEF components and JIT code
        // for the first time, even though those components could have been loaded on the background thread first. This method is called from the two places
        // we expose a service for the project systems to create us; this can be called by VS's preloading support on a background thread to ensure this is ran
        // on a background thread so the later calls on the UI thread will block less. For CPS projects, we don't need this, as CPS already creates us on
        // background threads, so we can just let things load as they're pulled in.
        //
        // The expectation is no thread switching should happen here; if we're being called on a background thread that means we're being pulled in
        // by the preloading logic. If we're being called on the UI thread, that means we might be getting created directly by the project systems
        // and the UI thread is already blocked, so a switch to the background thread won't unblock the UI thread and might delay us even further.
        //
        // As long as there's something that's not cheap to load later, and it'll definitely be used in all csproj/msvbprj scenarios, then it's worth
        // putting here to preload.
        var workspace = this.ComponentModel.GetService<VisualStudioWorkspaceImpl>();
        workspace.PreloadProjectSystemComponents(this.RoslynLanguageName);
    }
}
