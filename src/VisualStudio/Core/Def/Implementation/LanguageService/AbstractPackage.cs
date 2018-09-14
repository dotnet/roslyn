// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract class AbstractPackage : AsyncPackage
    {
        protected ForegroundThreadAffinitizedObject ForegroundObject
        {
            get;
            private set;
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel));
            ForegroundObject = new ForegroundThreadAffinitizedObject(componentModel.GetService<IThreadingContext>());
        }

        protected async Task LoadComponentsInUIContextOnceSolutionFullyLoadedAsync(CancellationToken cancellationToken)
        {
            await KnownUIContexts.SolutionExistsAndFullyLoadedContext;
            await LoadComponentsAsync(cancellationToken);
        }

        protected abstract Task LoadComponentsAsync(CancellationToken cancellationToken);
    }
}
