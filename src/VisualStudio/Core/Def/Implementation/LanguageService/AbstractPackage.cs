// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract class AbstractPackage : AsyncPackage
    {
        protected ForegroundThreadAffinitizedObject ForegroundObject;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Assume that we are being initialized on the UI thread at this point, and setup our foreground state
            var kind = ForegroundThreadDataInfo.CreateDefault(ForegroundThreadDataKind.ForcedByPackageInitialize);

            // None of the work posted to the foregroundTaskScheduler should block pending keyboard/mouse input from the user.
            // So instead of using the default priority which is above user input, we use Background priority which is 1 level
            // below user input.
            var taskScheduler = new SynchronizationContextTaskScheduler(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher, DispatcherPriority.Background));

            ForegroundThreadAffinitizedObject.CurrentForegroundThreadData = new ForegroundThreadData(Thread.CurrentThread, taskScheduler, kind);
            ForegroundObject = new ForegroundThreadAffinitizedObject(ThreadingContext.Invalid);
        }

        protected void LoadComponentsInUIContextOnceSolutionFullyLoaded(CancellationToken cancellationToken)
        {
            ForegroundObject.AssertIsForeground();

            if (KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive)
            {
                // if we are already in the right UI context, load it right away
                LoadComponentsInUIContext(cancellationToken);
            }
            else
            {
                // load them when it is a right context.
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged += OnSolutionExistsAndFullyLoadedContext;
            }
        }

        private void OnSolutionExistsAndFullyLoadedContext(object sender, UIContextChangedEventArgs e)
        {
            ForegroundObject.AssertIsForeground();

            if (e.Activated)
            {
                // unsubscribe from it
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged -= OnSolutionExistsAndFullyLoadedContext;

                // load components
                LoadComponentsInUIContext(CancellationToken.None);
            }
        }

        protected abstract void LoadComponentsInUIContext(CancellationToken cancellationToken);
    }
}
