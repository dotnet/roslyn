// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    [Guid("D02DAC01-DDD0-4ECC-8687-79A554852B14")]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", version: 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class IntegrationTestServicePackage : Package
    {
        protected override void Initialize()
        {
            base.Initialize();
            IntegrationTestServiceCommands.Initialize(this);
        }
    }
}
