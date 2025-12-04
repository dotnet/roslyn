// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTestService
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;

    [Guid("78d5a8b5-1634-434b-802d-e3e4a46b1aa6")]
    public sealed class IntegrationTestServicePackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            IntegrationTestServiceCommands.Initialize(this);
        }
    }
}
