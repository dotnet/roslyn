// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.OperationProgress;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    [Export]
    [Shared]
    internal class ProjectOperationProgressTracker
    {
        private readonly IAsynchronousOperationListenerProvider _asynchronousOperationListenerProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsyncServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectOperationProgressTracker(
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider)
        {
            _asynchronousOperationListenerProvider = asynchronousOperationListenerProvider;
            _threadingContext = threadingContext;
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        internal async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var operationProgressStatus = (IVsOperationProgressStatusService)await _serviceProvider.GetServiceAsync(typeof(SVsOperationProgress));
            var stageStatus = operationProgressStatus.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            stageStatus.InProgressChanged += (_, e) => HandleInProgressChanged(stageStatus, e);
        }

        private void HandleInProgressChanged(IVsOperationProgressStageStatus stageStatus, OperationProgressStatusChangedEventArgs e)
        {
            if (!e.Status.IsInProgress)
            {
                return;
            }

            var listener = _asynchronousOperationListenerProvider.GetListener(FeatureAttribute.Workspace);
            var token = listener.BeginAsyncOperation(nameof(CommonOperationProgressStageIds.Intellisense));

            stageStatus.WaitForCompletionAsync().CompletesAsyncOperation(token);
        }
    }
}
