// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract class AbstractPackage : AsyncPackage
{
    internal IComponentModel ComponentModel
    {
        get
        {
            if (field is null)
            {
                // We should have been initialized asynchronously, but somebody is asking earlier than we expected, so fetch it synchronously.
                var componentModel = (IComponentModel?)GetService(typeof(SComponentModel));
                Assumes.Present(componentModel);
                field = componentModel;
                return field;
            }

            return field;
        }

        set
        {
            Assumes.Present(value);
            field = value;
        }
    }

    /// This method is called upon package creation and is the mechanism by which roslyn packages calculate and
    /// process all package initialization work. Do not override this sealed method, instead override RegisterOnAfterPackageLoadedAsyncWork
    /// to indicate the work your package needs upon initialization.
    /// Not sealed as TypeScriptPackage has IVT and derives from this class and implements this method.
    protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        => RegisterAndProcessTasksAsync(RegisterInitializeAsyncWork, cancellationToken);

    /// This method is called after package load and is the mechanism by which roslyn packages calculate and
    /// process all post load package work. Do not override this sealed method, instead override RegisterOnAfterPackageLoadedAsyncWork
    /// to indicate the work your package needs after load.
    protected sealed override Task OnAfterPackageLoadedAsync(CancellationToken cancellationToken)
        => RegisterAndProcessTasksAsync(RegisterOnAfterPackageLoadedAsyncWork, cancellationToken);

    private Task RegisterAndProcessTasksAsync(Action<PackageLoadTasks> registerTasks, CancellationToken cancellationToken)
    {
        var packageTasks = new PackageLoadTasks(JoinableTaskFactory);

        // Request all initially known work, classified into whether it should be processed on the main or
        // background thread. These lists can be modified by the work itself to add more work for subsequent processing.
        // Requesting this information is useful as it lets us batch up work on these threads, significantly
        // reducing thread switches during package load.
        registerTasks(packageTasks);

        return packageTasks.ProcessTasksAsync(cancellationToken);
    }

    protected virtual void RegisterInitializeAsyncWork(PackageLoadTasks packageInitializationTasks)
    {
        // We have a legacy property to access the ComponentModel that is used across various parts of our load; we'll fetch this on the
        // background thread as our first step so any later already has it available. If it were to be accessed by a UI-thread scheduled piece
        // of work first, it'll still fetch it on demand.
        packageInitializationTasks.AddTask(isMainThreadTask: false, task: EnsureComponentModelAsync);

        async Task EnsureComponentModelAsync(PackageLoadTasks packageInitializationTasks, CancellationToken token)
        {
            var componentModel = (IComponentModel?)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(false);
            Assumes.Present(componentModel);
            ComponentModel = componentModel;
        }
    }

    protected virtual void RegisterOnAfterPackageLoadedAsyncWork(PackageLoadTasks afterPackageLoadedTasks)
    {
        afterPackageLoadedTasks.AddTask(isMainThreadTask: false, async (packageLoadTasks, cancellationToken) =>
        {
            // UIContexts can be "zombied" if UIContexts aren't supported because we're in a command line build or in other scenarios.
            // Trying to await them will throw.
            if (!KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsZombie)
            {
                await KnownUIContexts.SolutionExistsAndFullyLoadedContext;

                // Kick off the work, but don't block
                LoadComponentsInBackgroundAfterSolutionFullyLoadedAsync(cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken).Forget();
            }
        });
    }

    /// <summary>
    /// Registers an editor factory. This is the same as <see cref="Microsoft.VisualStudio.Shell.Package.RegisterEditorFactory(IVsEditorFactory)"/> except it fetches the service async.
    /// </summary>
    protected async Task RegisterEditorFactoryAsync(IVsEditorFactory editorFactory, CancellationToken cancellationToken)
    {
        // Call with ConfigureAwait(true): if we're off the UI thread we will stay that way, but a synchronous load of our package should continue to use the UI thread
        // since the UI thread is otherwise blocked waiting for us. This method is called under JTF rules so that's fine.
        var registerEditors = await GetServiceAsync<SVsRegisterEditors, IVsRegisterEditors>(throwOnFailure: true, cancellationToken).ConfigureAwait(true);
        Assumes.Present(registerEditors);

        ErrorHandler.ThrowOnFailure(registerEditors.RegisterEditor(editorFactory.GetType().GUID, editorFactory, out _));
    }

    /// <summary>
    /// A method called in the background once the solution has fully loaded. Offers a place for initialization to happen after package load.
    /// </summary>
    protected abstract Task LoadComponentsInBackgroundAfterSolutionFullyLoadedAsync(CancellationToken cancellationToken);
}
