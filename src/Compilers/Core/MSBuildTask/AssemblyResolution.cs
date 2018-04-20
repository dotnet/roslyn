// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    // TODO: Remove once https://github.com/Microsoft/msbuild/issues/1309 is fixed.

    /// <summary>
    /// msbuild currently doesn't support specifying binding redirects per custom task thru a config file. 
    /// Each task needs to handle version redirection of its dependencies manually. 
    /// </summary>
    internal static class AssemblyResolution
    {
        static AssemblyResolution()
        {
            CorLightup.Desktop.AddAssemblyResolveHandler(ResolveAssembly);
        }

        public static void Install()
        {
            // empty, just to trigger static ctor
        }

        internal static Assembly ResolveAssembly(string assemblyDisplayName, Assembly requestingAssemblyOpt)
        {
            var name = new AssemblyName(assemblyDisplayName);
            try
            {
                return TryRedirect(name) ? LoadAssemblyWithRedirects(name) : null;
            }
            catch
            {
                ValidateBootstrapUtil.AddFailedLoad(name);
                throw;
            }
        }

        private static readonly byte[] s_b03f5f7f11d50a3a = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a };
        private static readonly byte[] s_b77a5c561934e089 = new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };

        private static bool TryRedirect(AssemblyName name)
        {
            switch (name.Name)
            {
                case "System.IO.Compression":
                    return TryRedirect(name, s_b77a5c561934e089, 4, 1, 2, 0);

                case "System.Console":
                case "System.IO.Pipes":
                case "System.Security.AccessControl":
                case "System.Security.Cryptography.Primitives":
                case "System.Security.Principal.Windows":
                case "System.Threading.Thread":
                    return TryRedirect(name, s_b03f5f7f11d50a3a, 4, 0, 1, 0);

                case "System.IO.FileSystem":
                case "System.IO.FileSystem.Primitives":
                    return TryRedirect(name, s_b03f5f7f11d50a3a, 4, 0, 2, 0);

                case "System.Diagnostics.Process":
                case "System.Diagnostics.StackTrace":
                    return TryRedirect(name, s_b03f5f7f11d50a3a, 4, 0, 3, 0);

                case "System.Reflection":
                    return TryRedirect(name, s_b03f5f7f11d50a3a, 4, 1, 1, 0);

                // Assemblies in the runtimes directory. We're not concerned about
                // redirecting versions for these assemblies, only location, so
                // it's fine to always return true. The loading location override is
                // found in the LoadAssemblyWithRedirects method.
                case "System.IO.Pipes.AccessControl":
                    return true;
            }

            return false;
        }

        private static Assembly LoadAssemblyWithRedirects(AssemblyName name)
        {
            // Check if we need to redirect the location of this assembly. This
            // is necessary for assemblies in the runtimes/ subdirectory of the
            // build task as this directory is not searched for dependencies by
            // default. This is necessary because the CoreCLR shared loader doesn't
            // provide a mechanism for nested assembly load policy:
            // https://github.com/dotnet/coreclr/issues/15982
            switch (name.Name)
            {
                case "System.IO.Pipes.AccessControl":
                    var assembly = TryRedirectToRuntimesDir(name);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                    break;
            }

            return Assembly.Load(name);
        }

        private static bool TryRedirect(AssemblyName name, byte[] token, int major, int minor, int build, int revision)
        {
            var version = new Version(major, minor, build, revision);
            if (KeysEqual(name.GetPublicKeyToken(), token) && name.Version < version)
            {
                name.Version = version;
                return true;
            }

            return false;
        }

        private static readonly string s_assemblyLocation = Utilities.TryGetAssemblyPath(typeof(AssemblyResolution).GetTypeInfo().Assembly);
        private static Assembly TryRedirectToRuntimesDir(AssemblyName name)
        {
            CompilerServerLogger.Log($"Loading with redirect {name.Name}");
            if (s_assemblyLocation == null)
            {
                return null;
            }

            var taskDir = Path.GetDirectoryName(s_assemblyLocation);
            var osId = PlatformInformation.IsWindows ? "win" : "unix";
            var runtimeDir = Path.Combine(taskDir, "runtimes", osId, "lib", "netstandard1.3");

            var assemblyPath = Path.Combine(runtimeDir, name.Name) + ".dll";

            if (File.Exists(assemblyPath))
            {
                CompilerServerLogger.Log($"Loading from: {assemblyPath}");
                return LoadAssemblyFromPath(assemblyPath);
            }

            CompilerServerLogger.Log($"File not found: {assemblyPath}");

            return null;

            Assembly LoadAssemblyFromPath(string path)
                => (Assembly)typeof(Assembly).GetTypeInfo()
                    .GetDeclaredMethod("LoadFile")
                    ?.Invoke(null, parameters: new object[] { assemblyPath });
        }

        private static bool KeysEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
