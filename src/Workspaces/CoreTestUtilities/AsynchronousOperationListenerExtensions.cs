// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Test.Utilities
{
    public static class AsynchronousOperationListenerExtensions
    {
        internal static async Task WaitAllAsync(this AsynchronousOperationListenerProvider provider, Workspace? workspace, params string[] featureNames)
        {
            await provider.WaitAllAsync(workspace, featureNames).ConfigureAwait(false);
        }
    }
}
