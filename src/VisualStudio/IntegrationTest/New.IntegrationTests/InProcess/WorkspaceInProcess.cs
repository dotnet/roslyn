// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Threading;

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
    }
}
