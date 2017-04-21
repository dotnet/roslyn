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

            var shell = (IVsShell)GetService(typeof(SVsShell));
            if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out var installDirectoryObject)))
            {
                if (installDirectoryObject is string installDirectory)
                {
                    if (!File.Exists(Path.Combine(installDirectory, @"PublicAssemblies\Microsoft.VisualStudio.CodingConventions.dll")))
                    {
                        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyForCurrentDomain;
                    }
                }
            }
        }

        private Assembly ResolveAssemblyForCurrentDomain(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("Microsoft.VisualStudio.CodingConventions"))
            {
                // Load the one in our package
                var thisPackage = Path.GetDirectoryName(typeof(IntegrationTestServicePackage).Assembly.Location);
                var assembly = Path.Combine(thisPackage, "Microsoft.VisualStudio.CodingConventions.dll");
                return Assembly.LoadFile(assembly);
            }

            return null;
        }
    }
}
