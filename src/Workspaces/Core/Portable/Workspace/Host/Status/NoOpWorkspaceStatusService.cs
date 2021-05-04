// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed class NoOpWorkspaceStatusService : IWorkspaceStatusService
    {
        public static readonly IWorkspaceStatusService Instance = new NoOpWorkspaceStatusService();

        private NoOpWorkspaceStatusService()
        {
        }

        event EventHandler IWorkspaceStatusService.StatusChanged
        {
            add { }
            remove { }
        }

        public Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return Task.CompletedTask;
        }

        public Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return SpecializedTasks.True;
        }
    }
}
