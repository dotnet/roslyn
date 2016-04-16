// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class AppDomainUtils
    {
        private static readonly object s_lock = new object();
        private static bool s_hookedResolve;

        public static AppDomain Create(string name = null, string basePath = null)
        {
            name = name ?? "Custtom AppDomain";
            basePath = basePath ?? Path.GetDirectoryName(typeof(AppDomainUtils).Assembly.Location);

            lock (s_lock)
            {
                if (!s_hookedResolve)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
                    s_hookedResolve = true;
                }
            }

            return AppDomain.CreateDomain(
                name,
                null,
                appBasePath: basePath,
                appRelativeSearchPath: null,
                shadowCopyFiles: false);
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
