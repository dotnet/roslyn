// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.VsixInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Microsoft.VisualStudio.ExtensionManager;
    using Microsoft.VisualStudio.Settings;
    using File = System.IO.File;
    using Path = System.IO.Path;

    public static class Installer
    {
        public static void Install(IEnumerable<string> vsixFiles, string installationPath, string rootSuffix)
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;

            try
            {
                InstallImpl(vsixFiles, rootSuffix, installationPath);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= HandleAssemblyResolve;
            }

            return;

            Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
            {
                string path = Path.Combine(installationPath, @"Common7\IDE\PrivateAssemblies", new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }

                path = Path.Combine(installationPath, @"Common7\IDE", new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }

                path = Path.Combine(installationPath, @"Common7\IDE\PublicAssemblies", new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }

                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InstallImpl(IEnumerable<string> vsixFiles, string rootSuffix, string installationPath)
        {
            var vsExeFile = Path.Combine(installationPath, @"Common7\IDE\devenv.exe");

            using (var settingsManager = ExternalSettingsManager.CreateForApplication(vsExeFile, rootSuffix))
            {
                var extensionManager = new ExtensionManagerService(settingsManager);
                IVsExtensionManager vsExtensionManager = (IVsExtensionManager)(object)extensionManager;
                var extensions = vsixFiles.Select(vsExtensionManager.CreateInstallableExtension).ToArray();

                foreach (var extension in extensions)
                {
                    if (extensionManager.IsInstalled(extension))
                    {
                        extensionManager.Uninstall(extensionManager.GetInstalledExtension(extension.Header.Identifier));
                    }
                }

                foreach (var extension in extensions)
                {
                    extensionManager.Install(extension, perMachine: false);
                }
            }
        }
    }
}
