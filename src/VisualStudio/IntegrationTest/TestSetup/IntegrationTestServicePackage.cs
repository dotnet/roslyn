// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    [Guid("D02DAC01-DDD0-4ECC-8687-79A554852B14")]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", version: 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class IntegrationTestServicePackage : AsyncPackage
    {
        private static readonly Guid s_compilerPackage = new Guid("31C0675E-87A4-4061-A0DD-A4E510FCCF97");

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

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
