// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    internal partial class WorkspaceInProcess
    {
        public Task WaitForProjectSystemAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Visual Studio 2019 version 16.0 includes SVsOperationProgress, but does not include IVsOperationProgressStatusService. Update Microsoft.VisualStudio.Shell.Framework to 16.1 or newer to support waiting for project system.");
        }
    }
}
