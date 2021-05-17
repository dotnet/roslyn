// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTestService.Shim
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var shellInternal = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
                assembly => assembly.GetName().Name == "Microsoft.VisualStudio.Shell.UI.Internal");
            var shellVersion = shellInternal.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (!Version.TryParse(shellVersion, out var version))
            {
                return;
            }

            var subFolder = version switch
            {
                // Prior to Visual Studio 2022 (17.0), use the original dev11 service
                { Major: < 17 } => "dev11",

                // All of 17.x uses dev17
                { Major: 17 } => "dev17",

                _ => null,
            };

            if (subFolder is null)
            {
                return;
            }

            var baseDirectory = Path.GetDirectoryName(typeof(ModuleInitializer).Assembly.Location);
            var implementationAssembly = Path.Combine(baseDirectory, subFolder, "Microsoft.VisualStudio.IntegrationTestService.Impl.dll");
            Assembly.LoadFrom(implementationAssembly);
        }
    }
}
