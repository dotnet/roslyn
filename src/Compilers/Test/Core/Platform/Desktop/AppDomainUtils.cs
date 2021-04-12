// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NET472
using System;
using System.IO;
using System.Reflection;

namespace Roslyn.Test.Utilities.Desktop
{
    public static class AppDomainUtils
    {
        private static readonly object s_lock = new object();
        private static bool s_hookedResolve;

        public static AppDomain Create(string name = null, string basePath = null)
        {
            name = name ?? "Custom AppDomain";
            basePath = basePath ?? Path.GetDirectoryName(typeof(AppDomainUtils).Assembly.Location);

            lock (s_lock)
            {
                if (!s_hookedResolve)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
                    s_hookedResolve = true;
                }
            }

            return AppDomain.CreateDomain(name, null, new AppDomainSetup()
            {
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ApplicationBase = basePath
            });
        }

        /// <summary>
        /// When run under xunit without AppDomains all DLLs get loaded via the AssemblyResolve
        /// event.  In some cases the xunit, AppDomain marshalling, xunit doesn't fully hook
        /// the event and we need to do it for our assemblies.
        /// </summary>
        private static Assembly OnResolve(object sender, ResolveEventArgs e)
        {
            var assemblyName = new AssemblyName(e.Name);
            var fullPath = Path.Combine(
                Path.GetDirectoryName(typeof(AppDomainUtils).Assembly.Location),
                assemblyName.Name + ".dll");
            if (File.Exists(fullPath))
            {
                return Assembly.LoadFrom(fullPath);
            }

            return null;
        }
    }
}
#endif
