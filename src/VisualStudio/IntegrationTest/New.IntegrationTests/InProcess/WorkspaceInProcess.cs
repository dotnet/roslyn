// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    internal partial class WorkspaceInProcess
    {
        internal static void EnableAsynchronousOperationTracking()
        {
            AsynchronousOperationListenerProvider.Enable(true);
        }

        public async Task<bool> IsPrettyListingOnAsync(string languageName, CancellationToken cancellationToken)
        {
            var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            return globalOptions.GetOption(FeatureOnOffOptions.PrettyListing, languageName);
        }

        public async Task SetPrettyListingAsync(string languageName, bool value, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            globalOptions.SetGlobalOption(new OptionKey(FeatureOnOffOptions.PrettyListing, languageName), value);
        }

        public Task WaitForAsyncOperationsAsync(string featuresToWaitFor, CancellationToken cancellationToken)
            => WaitForAsyncOperationsAsync(featuresToWaitFor, waitForWorkspaceFirst: true, cancellationToken);

        public async Task WaitForAsyncOperationsAsync(string featuresToWaitFor, bool waitForWorkspaceFirst, CancellationToken cancellationToken)
        {
            if (waitForWorkspaceFirst || featuresToWaitFor == FeatureAttribute.Workspace)
            {
                await WaitForProjectSystemAsync(cancellationToken);
                await TestServices.Shell.WaitForFileChangeNotificationsAsync(cancellationToken);
                await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
            }

            var listenerProvider = await GetComponentModelServiceAsync<AsynchronousOperationListenerProvider>(cancellationToken);

            if (waitForWorkspaceFirst)
            {
                var workspaceWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace);
                await workspaceWaiter.ExpeditedWaitAsync().WithCancellation(cancellationToken);
            }

            var featureWaiter = listenerProvider.GetWaiter(featuresToWaitFor);
            await featureWaiter.ExpeditedWaitAsync().WithCancellation(cancellationToken);
        }

        public async Task WaitForAllAsyncOperationsAsync(string[] featureNames, CancellationToken cancellationToken)
        {
            if (featureNames.Contains(FeatureAttribute.Workspace))
            {
                await WaitForProjectSystemAsync(cancellationToken);
                await TestServices.Shell.WaitForFileChangeNotificationsAsync(cancellationToken);
                await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
            }

            var listenerProvider = await GetComponentModelServiceAsync<AsynchronousOperationListenerProvider>(cancellationToken);
            var workspace = await GetComponentModelServiceAsync<VisualStudioWorkspace>(cancellationToken);

            if (featureNames.Contains(FeatureAttribute.NavigateTo))
            {
                var statusService = workspace.Services.GetRequiredService<IWorkspaceStatusService>();
                Contract.ThrowIfFalse(await statusService.IsFullyLoadedAsync(cancellationToken));

                // Make sure the "priming" operation has started for Nav To
                var threadingContext = await GetComponentModelServiceAsync<IThreadingContext>(cancellationToken);
                var asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
                var searchHost = new DefaultNavigateToSearchHost(workspace.CurrentSolution, asyncListener, threadingContext.DisposalToken);

                // Calling DefaultNavigateToSearchHost.IsFullyLoadedAsync starts the fire-and-forget asynchronous
                // operation to populate the remote host. The call to WaitAllAsync below will wait for that operation to
                // complete.
                await searchHost.IsFullyLoadedAsync(cancellationToken);
            }

            await listenerProvider.WaitAllAsync(workspace, featureNames).WithCancellation(cancellationToken);
        }
    }
}
