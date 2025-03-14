// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract class AbstractPackage : AsyncPackage
{
    private IComponentModel? _componentModel_doNotAccessDirectly;

    internal IComponentModel ComponentModel
    {
        get
        {
            Assumes.Present(_componentModel_doNotAccessDirectly);
            return _componentModel_doNotAccessDirectly;
        }
    }

    protected virtual void RegisterInitializationWork(List<Func<IProgress<ServiceProgressData>, CancellationToken, Task>> bgThreadWorkTasks, List<Func<IProgress<ServiceProgressData>, CancellationToken, Task>> mainThreadWorkTasks)
    {
        // This treatment of registering work on the bg/main threads is a bit unique as we want the component model initialized at the beginning
        // of whichever context is invoked first.
        bgThreadWorkTasks.Add(EnsureComponentModelAsync);
        mainThreadWorkTasks.Add(EnsureComponentModelAsync);

        async Task EnsureComponentModelAsync(IProgress<ServiceProgressData> progress, CancellationToken token)
        {
            if (_componentModel_doNotAccessDirectly == null)
            {
                _componentModel_doNotAccessDirectly = (IComponentModel?)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(false);
                Assumes.Present(_componentModel_doNotAccessDirectly);
            }
        }
    }

    protected sealed override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        var bgThreadWorkTasks = new List<Func<IProgress<ServiceProgressData>, CancellationToken, Task>>();
        var mainThreadWorkTasks = new List<Func<IProgress<ServiceProgressData>, CancellationToken, Task>>();

        // Request all initially known work, classified into whether it should be processed on the main or
        // background thread. These lists can be modified by the work itself to add more work for subsequent processing.
        // Requesting this information is useful as it lets us batch up work on these threads, significantly
        // reducing thread switches during package load.
        RegisterInitializationWork(bgThreadWorkTasks, mainThreadWorkTasks);

        // prime the pump by doing the first group of bg thread work if the initiating thread is not the main thread
        if (!JoinableTaskFactory.Context.IsOnMainThread)
            await PerformWorkAsync(useMainThread: false).ConfigureAwait(false);

        // Continue processing work until everything is completed, switching between main and bg threads as needed.
        while (mainThreadWorkTasks.Count > 0 || bgThreadWorkTasks.Count > 0)
        {
            await PerformWorkAsync(useMainThread: true).ConfigureAwait(false);
            await PerformWorkAsync(useMainThread: false).ConfigureAwait(false);
        }

        return;

        async Task PerformWorkAsync(bool useMainThread)
        {
            var workTasks = useMainThread ? mainThreadWorkTasks : bgThreadWorkTasks;

            // Ensure we're invoking the task on the right thread
            if (useMainThread)
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            else if (JoinableTaskFactory.Context.IsOnMainThread)
                await TaskScheduler.Default;

            for (var i = 0; i < workTasks.Count; i++)
            {
                var work = workTasks[i];

                // CA(true) is important here, as we want to ensure that each iteration is done in the same
                // captured context. Thus, even poorly behaving tasks (ie, those that do their own thread switching)
                // don't effect the next loop iteration.
                await work(progress, cancellationToken).ConfigureAwait(true);
            }

            workTasks.Clear();
        }
    }

    protected override async Task OnAfterPackageLoadedAsync(CancellationToken cancellationToken)
    {
        // TODO: remove, workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1985204
        var globalOptions = ComponentModel.GetService<IGlobalOptionService>();
        if (globalOptions.GetOption(SemanticSearchFeatureFlag.Enabled))
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            UIContext.FromUIContextGuid(new Guid(SemanticSearchFeatureFlag.UIContextId)).IsActive = true;
        }
    }

    protected async Task LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(CancellationToken cancellationToken)
    {
        // UIContexts can be "zombied" if UIContexts aren't supported because we're in a command line build or in other scenarios.
        // Trying to await them will throw.
        if (!KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsZombie)
        {
            await KnownUIContexts.SolutionExistsAndFullyLoadedContext;
            await LoadComponentsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    protected abstract Task LoadComponentsAsync(CancellationToken cancellationToken);
}
