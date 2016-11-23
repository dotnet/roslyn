// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.IO;
using System.Reflection;

namespace Roslyn.VisualStudio.Test.Setup
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
            object installDirectoryObject;
            if (ErrorHandler.Succeeded(shell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out installDirectoryObject)))
            {
                string installDirectory = installDirectoryObject as string;

                if (installDirectory != null)
                {
                    if (!File.Exists(Path.Combine(installDirectory, @"PublicAssemblies\Microsoft.VisualStudio.CodingConventions.dll")))
                    {
                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                    }
                }
            }
        }

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
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
