// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class WorkspaceInProcess
    {
        public Task WaitForProjectSystemAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
