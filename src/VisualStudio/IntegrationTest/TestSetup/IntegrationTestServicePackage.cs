// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    [Guid("D02DAC01-DDD0-4ECC-8687-79A554852B14")]
    public sealed class IntegrationTestServicePackage : AsyncPackage
    {
        private static readonly Guid s_compilerPackage = new Guid("31C0675E-87A4-4061-A0DD-A4E510FCCF97");

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = (IVsShell)await GetServiceAsync(typeof(SVsShell));
            ErrorHandler.ThrowOnFailure(shell.IsPackageInstalled(s_compilerPackage, out var installed));
            if (installed != 0)
            {
                await ((IVsShell7)shell).LoadPackageAsync(s_compilerPackage);
            }

            IntegrationTestServiceCommands.Initialize(this);
        }
    }
}
