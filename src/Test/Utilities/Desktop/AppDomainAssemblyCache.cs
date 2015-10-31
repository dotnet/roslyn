// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// This is a singleton per AppDomain which manages all of the assemblies which were ever loaded into it.  
    /// </summary>
    internal sealed class AppDomainAssemblyCache
    {
        private static AppDomainAssemblyCache s_singleton;
        private static readonly object s_guard = new object();

        // Per-domain cache, contains all assemblies loaded to this app domain since the first manager was created.
        // The key is the manifest module MVID, which is unique for each distinct assembly. 
        private readonly Dictionary<Guid, Assembly> _assemblyCache = new Dictionary<Guid, Assembly>();
        private readonly Dictionary<Guid, Assembly> _reflectionOnlyAssemblyCache = new Dictionary<Guid, Assembly>();

        internal static AppDomainAssemblyCache GetOrCreate()
        {
            lock (s_guard)
            {
                if (s_singleton == null)
                {
                    s_singleton = new AppDomainAssemblyCache();
                    var currentDomain = AppDomain.CurrentDomain;
                    currentDomain.AssemblyLoad += OnAssemblyLoad;
                }

                return s_singleton;
            }
        }

        internal Assembly GetOrLoad(ModuleData moduleData, bool reflectionOnly)
        {
            var cache = reflectionOnly ? _reflectionOnlyAssemblyCache : _assemblyCache;

            lock (s_guard)
            {
                Assembly assembly;
                if (cache.TryGetValue(moduleData.Mvid, out assembly))
                {
                    return assembly;
                }

                var loadedAssembly = RuntimeAssemblyManager.LoadAsAssembly(moduleData.Image, reflectionOnly);

                // Validate the loaded assembly matches the value that we now have in the cache. 
                if (!cache.TryGetValue(moduleData.Mvid, out assembly))
                {
                    throw new Exception("Explicit assembly load didn't update the proper cache");
                }

                if (loadedAssembly != assembly)
                {
                    throw new Exception("Cache entry doesn't match result of load");
                }

                return assembly;
            }
        }

        private void OnAssemblyLoad(Assembly assembly)
        {
            // We need to add loaded assemblies to the cache in order to avoid loading them twice.
            // This is not just optimization. CLR isn't able to load the same assembly from multiple "locations".
            // Location for byte[] assemblies is the location of the assembly that invokes Assembly.Load. 
            // PE verifier invokes load directly for the assembly being verified. If this assembly is also a dependency 
            // of another assembly we verify our AssemblyResolve is invoked. If we didn't reuse the assembly already loaded 
            // by PE verifier we would get an error from Assembly.Load.
            var cache = assembly.ReflectionOnly ? _reflectionOnlyAssemblyCache : _assemblyCache;

            lock (s_guard)
            {
                var mvid = assembly.ManifestModule.ModuleVersionId;
                if (!cache.ContainsKey(mvid))
                {
                    cache.Add(mvid, assembly);
                }
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            GetOrCreate().OnAssemblyLoad(args.LoadedAssembly);
        }
    }
}
