// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

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

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        _componentModel_doNotAccessDirectly = (IComponentModel)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);
        Assumes.Present(_componentModel_doNotAccessDirectly);
    }

    protected override Task OnAfterPackageLoadedAsync(CancellationToken cancellationToken)
    {
        // TODO: remove, workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1985204
        var globalOptions = ComponentModel.GetService<IGlobalOptionService>();
        if (globalOptions.GetOption(SemanticSearchFeatureFlag.Enabled))
        {
            UIContext.FromUIContextGuid(new Guid(SemanticSearchFeatureFlag.UIContextId)).IsActive = true;
        }

        return base.OnAfterPackageLoadedAsync(cancellationToken);
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
