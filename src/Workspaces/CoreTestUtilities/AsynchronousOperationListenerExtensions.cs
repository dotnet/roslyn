// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Test.Utilities
{
    public static class AsynchronousOperationListenerExtensions
    {
        internal static Task WaitAllDispatcherOperationAndTasksAsync(this IAsynchronousOperationListenerProvider provider, params string[] featureNames)
        {
            return ((AsynchronousOperationListenerProvider)provider).WaitAllAsync(featureNames, eventProcessingAction: () => Dispatcher.CurrentDispatcher.DoEvents());
        }

        internal static IAsynchronousOperationWaiter GetWaiter(this IAsynchronousOperationListenerProvider provider, string featureName)
        {
            return (IAsynchronousOperationWaiter)provider.GetListener(featureName);
        }
    }
}
