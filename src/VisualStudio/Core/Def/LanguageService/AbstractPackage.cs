// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

    protected virtual void RegisterInitializationWork(PackageRegistrationTasks packageRegistrationTasks)
    {
        // This treatment of registering work on the bg/main threads is a bit unique as we want the component model initialized at the beginning
        // of whichever context is invoked first.
        packageRegistrationTasks.AddTask(isMainThreadTask: false, task: EnsureComponentModelAsync);
        packageRegistrationTasks.AddTask(isMainThreadTask: true, task: EnsureComponentModelAsync);

        async Task EnsureComponentModelAsync(IProgress<ServiceProgressData> progress, PackageRegistrationTasks packageRegistrationTasks, CancellationToken token)
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
        var packageRegistrationTasks = new PackageRegistrationTasks(JoinableTaskFactory);

        // Request all initially known work, classified into whether it should be processed on the main or
        // background thread. These lists can be modified by the work itself to add more work for subsequent processing.
        // Requesting this information is useful as it lets us batch up work on these threads, significantly
        // reducing thread switches during package load.
        RegisterInitializationWork(packageRegistrationTasks);

        await packageRegistrationTasks.ProcessTasksAsync(progress, cancellationToken).ConfigureAwait(false);
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
