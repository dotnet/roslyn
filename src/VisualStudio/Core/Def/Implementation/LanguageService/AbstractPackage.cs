// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract class AbstractPackage : AsyncPackage
    {
        private IComponentModel _componentModel;

        protected internal IComponentModel ComponentModel
        {
            get
            {
                return _componentModel ?? throw new InvalidOperationException($"Cannot use {nameof(AbstractPackage)}.{nameof(ComponentModel)} prior to initialization.");
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            _componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            Assumes.Present(_componentModel);
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
}
