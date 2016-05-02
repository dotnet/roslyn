// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.VisualStudio.Test.Setup
{
    [Guid("D02DAC01-DDD0-4ECC-8687-79A554852B14")]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", version: 1)]
    public sealed class IntegrationTestServicePackage : Package
    {
        protected override void Initialize()
        {
            base.Initialize();
            IntegrationTestServiceCommands.Initialize(this);
        }
    }
}
