// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace EditorconfigResourcePackage
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase)]
    [Guid(PackageGuidString)]
    public sealed class EditorconfigResourcePackage : AsyncPackage
    {
        public const string PackageGuidString = "A435156A-8EA1-4A32-BC2E-118681C4DEE8";

        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
            => base.InitializeAsync(cancellationToken, progress);
    }
}
