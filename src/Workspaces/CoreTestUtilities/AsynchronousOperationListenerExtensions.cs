// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Test.Utilities;

public static class AsynchronousOperationListenerExtensions
{
    extension(IAsynchronousOperationListenerProvider provider)
    {
        internal async Task WaitAllDispatcherOperationAndTasksAsync(Workspace? workspace, params string[] featureNames)
        {
            await ((AsynchronousOperationListenerProvider)provider).WaitAllAsync(workspace, featureNames).ConfigureAwait(false);
        }

        internal IAsynchronousOperationWaiter GetWaiter(string featureName)
            => (IAsynchronousOperationWaiter)provider.GetListener(featureName);
    }
}
