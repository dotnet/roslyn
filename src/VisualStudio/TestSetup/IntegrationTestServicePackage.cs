// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.VisualStudio.Test.Setup
{
    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", version: 1)]
    public sealed class IntegrationTestServicePackage : Package
    {
        public const string PackageGuidString = "D02DAC01-DDD0-4ECC-8687-79A554852B14";

        protected override void Initialize()
        {
            IntegrationTestServiceCommands.Initialize(this);
            base.Initialize();
        }
    }
}
