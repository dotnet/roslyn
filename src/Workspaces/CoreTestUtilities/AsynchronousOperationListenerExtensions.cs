// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Test.Utilities;

internal static class AsynchronousOperationListenerExtensions
{
    internal static async Task WaitAllDispatcherOperationAndTasksAsync(this IAsynchronousOperationListenerProvider provider, Workspace? workspace, params string[] featureNames)
    {
        await ((AsynchronousOperationListenerProvider)provider).WaitAllAsync(workspace, featureNames).ConfigureAwait(false);
    }

    internal static IAsynchronousOperationWaiter GetWaiter(this IAsynchronousOperationListenerProvider provider, string featureName)
        => (IAsynchronousOperationWaiter)provider.GetListener(featureName);
}
