// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Test.Utilities
{
    public static class AsynchronousOperationListenerExtensions
    {
        internal static Task WaitAllDispatcherOperationAndTasksAsync(this IAsynchronousOperationListenerProvider provider, params string[] featureNames)
            => ((AsynchronousOperationListenerProvider)provider).WaitAllAsync(featureNames);

        internal static IAsynchronousOperationWaiter GetWaiter(this IAsynchronousOperationListenerProvider provider, string featureName)
            => (IAsynchronousOperationWaiter)provider.GetListener(featureName);
    }
}
