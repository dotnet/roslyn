// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class RuntimeAssemblyManager : MarshalByRefObject, IDisposable
    {
        private static int s_dumpCount;

        private readonly AppDomainAssemblyCache _assemblyCache = AppDomainAssemblyCache.GetOrCreate();

        // Modules managed by this manager. All such modules must have unique simple name.
        private readonly Dictionary<string, ModuleData> _modules;
        // Assemblies loaded by this manager.
        private readonly HashSet<Assembly> _loadedAssemblies;
        private readonly List<Guid> _mainMvids;

        private bool _containsNetModules;

        public RuntimeAssemblyManager()
        {
            _modules = new Dictionary<string, ModuleData>(StringComparer.OrdinalIgnoreCase);
            _loadedAssemblies = new HashSet<Assembly>();
            _mainMvids = new List<Guid>();

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoad;
            CLRHelpers.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
        }

        public void Dispose()
        {
            // clean up our handlers, so that they don't accumulate

            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoad;
            CLRHelpers.ReflectionOnlyAssemblyResolve -= ReflectionOnlyAssemblyResolve;

            foreach (var assembly in _loadedAssemblies)
            {
                if (!MonoHelpers.IsRunningOnMono())
                {
                    assembly.ModuleResolve -= ModuleResolve;
                }
            }

            //EDMAURER Some RuntimeAssemblyManagers are created via reflection in an AppDomain of our creation.
            //Sometimes those AppDomains are not released. I don't fully understand how that appdomain roots
            //a RuntimeAssemblyManager, but according to heap dumps, it does. Even though the appdomain is not
            //unloaded, its RuntimeAssemblyManager is explicitly disposed. So make sure that it cleans up this
            //memory hog - the modules dictionary.
            _modules.Clear();
        }


        /// <summary>
        /// Adds given MVID into a list of module MVIDs that are considered owned by this manager.
        /// </summary>
        public void AddMainModuleMvid(Guid mvid)
        {
            _mainMvids.Add(mvid);
        }

        /// <summary>
        /// True if given assembly is owned by this manager.
        /// </summary>
        private bool IsOwned(Assembly assembly)
        {
            return _mainMvids.Count == 0 || _mainMvids.Contains(assembly.ManifestModule.ModuleVersionId) || _loadedAssemblies.Contains(assembly);
        }

        internal bool ContainsNetModules()
        {
            return _containsNetModules;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void AddModuleData(IEnumerable<ModuleData> modules)
        {
            foreach (var module in modules)
            {
                if (!_modules.ContainsKey(module.FullName))
                {
                    if (module.Kind == OutputKind.NetModule)
                    {
                        _containsNetModules = true;
                    }
                    _modules.Add(module.FullName, module);
                }
            }
        }

        private ImmutableArray<byte> GetModuleBytesByName(string moduleName)
        {
            ModuleData data;
            if (!_modules.TryGetValue(moduleName, out data))
            {
                throw new KeyNotFoundException(String.Format("Could not find image for module '{0}'.", moduleName));
            }

            return data.Image;
        }

        private void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assembly = args.LoadedAssembly;

            // ModuleResolve needs to be hooked up for the main assembly once its loaded.
            // We won't get an AssemblyResolve event for the main assembly so we need to do it here.
            if (_mainMvids.Contains(assembly.ManifestModule.ModuleVersionId) && _loadedAssemblies.Add(assembly))
            {
                if (!MonoHelpers.IsRunningOnMono())
                {
                    assembly.ModuleResolve += ModuleResolve;
                }
            }
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return AssemblyResolve(args, reflectionOnly: false);
        }

        private Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return AssemblyResolve(args, reflectionOnly: true);
        }

        private Assembly AssemblyResolve(ResolveEventArgs args, bool reflectionOnly)
        {
            // only respond to requests for dependencies of assemblies owned by this manager:
            if (IsOwned(args.RequestingAssembly))
            {
                return GetAssembly(args.Name, reflectionOnly);
            }

            return null;
        }

        /// <summary>
        /// Loads given array of bytes as an assembly image using <see cref="System.Reflection.Assembly.Load"/> or <see cref="System.Reflection.Assembly.ReflectionOnlyLoad"/>.
        /// </summary>
        internal static Assembly LoadAsAssembly(ImmutableArray<byte> rawAssembly, bool reflectionOnly = false)
        {
            Debug.Assert(!rawAssembly.IsDefault);

            byte[] bytes = rawAssembly.ToArray();

            if (reflectionOnly)
            {
                return System.Reflection.Assembly.ReflectionOnlyLoad(bytes);
            }
            else
            {
                return System.Reflection.Assembly.Load(bytes);
            }
        }

        internal Assembly GetAssembly(string fullName, bool reflectionOnly)
        {
            ModuleData data;
            if (!_modules.TryGetValue(fullName, out data))
            {
                return null;
            }

            var assembly = _assemblyCache.GetOrLoad(data, reflectionOnly);
            if (!MonoHelpers.IsRunningOnMono())
            {
                assembly.ModuleResolve += ModuleResolve;
            }

            _loadedAssemblies.Add(assembly);
            return assembly;
        }

        private Module ModuleResolve(object sender, ResolveEventArgs args)
        {
            var assembly = args.RequestingAssembly;
            var rawModule = GetModuleBytesByName(args.Name);

            Debug.Assert(assembly != null);
            Debug.Assert(!rawModule.IsDefault);

            return assembly.LoadModule(args.Name, rawModule.ToArray());
        }

        internal SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
        {
            var signatures = new SortedSet<string>();
            foreach (var module in _modules) // Check inside each assembly in the compilation
            {
                foreach (var signature in MetadataSignatureHelper.GetMemberSignatures(GetAssembly(module.Key, true),
                                                                                      fullyQualifiedTypeName, memberName))
                {
                    signatures.Add(signature);
                }
            }
            return signatures;
        }

        internal SortedSet<string> GetFullyQualifiedTypeNames(string assemblyName)
        {
            var typeNames = new SortedSet<string>();
            Assembly assembly = GetAssembly(assemblyName, true);
            foreach (var typ in assembly.GetTypes())
                typeNames.Add(typ.FullName);
            return typeNames;
        }

        public int Execute(string moduleName, int expectedOutputLength, out string output)
        {
            ImmutableArray<byte> bytes = GetModuleBytesByName(moduleName);
            Assembly assembly = LoadAsAssembly(bytes);
            MethodInfo entryPoint = assembly.EntryPoint;
            Debug.Assert(entryPoint != null, "Attempting to execute an assembly that has no entrypoint; is your test trying to execute a DLL?");

            object result = null;
            string stdOut, stdErr;
            ConsoleOutput.Capture(() =>
            {
                var count = entryPoint.GetParameters().Length;
                object[] args;
                if (count == 0)
                {
                    args = new object[0];
                }
                else if (count == 1)
                {
                    args = new object[] { new string[0] };
                }
                else
                {
                    throw new Exception("Unrecognized entry point");
                }

                result = entryPoint.Invoke(null, args);
            }, expectedOutputLength, out stdOut, out stdErr);

            output = stdOut + stdErr;
            return result is int ? (int)result : 0;
        }

        public string DumpAssemblyData(out string dumpDirectory)
        {
            return DumpAssemblyData(_modules.Values, out dumpDirectory);
        }

        public static string DumpAssemblyData(IEnumerable<ModuleData> modules, out string dumpDirectory)
        {
            dumpDirectory = null;

            StringBuilder sb = new StringBuilder();
            foreach (var module in modules)
            {
                // Limit the number of dumps to 10.  After 10 we're likely in a bad state and are 
                // dumping lots of unnecessary data.
                if (s_dumpCount > 10)
                {
                    break; 
                }

                if (module.InMemoryModule)
                {
                    Interlocked.Increment(ref s_dumpCount);

                    if (dumpDirectory == null)
                    {
                        var assemblyLocation = typeof(HostedRuntimeEnvironment).Assembly.Location;
                        dumpDirectory = Path.Combine(
                            Path.GetDirectoryName(assemblyLocation),
                            "Dumps");
                        try
                        {
                             Directory.CreateDirectory(dumpDirectory);
                        }
                        catch
                        {
                            // Okay if directory already exists
                        }
                    }

                    string fileName;
                    if (module.Kind == OutputKind.NetModule)
                    {
                        fileName = module.FullName;
                    }
                    else
                    {
                        AssemblyIdentity identity;
                        AssemblyIdentity.TryParseDisplayName(module.FullName, out identity);
                        fileName = identity.Name;
                    }

                    string pePath = Path.Combine(dumpDirectory, fileName + module.Kind.GetDefaultExtension());
                    string pdbPath = (module.Pdb != null) ? pdbPath = Path.Combine(dumpDirectory, fileName + ".pdb") : null;
                    try
                    {
                        module.Image.WriteToFile(pePath);
                        if (pdbPath != null)
                        {
                            module.Pdb.WriteToFile(pdbPath);
                        }
                    }
                    catch (IOException)
                    {
                        pePath = "<unable to write file>";
                        if (pdbPath != null)
                        {
                            pdbPath = "<unable to write file>";
                        }
                    }
                    sb.Append("PE(" + module.Kind + "): ");
                    sb.AppendLine(pePath);
                    if (pdbPath != null)
                    {
                        sb.Append("PDB: ");
                        sb.AppendLine(pdbPath);
                    }
                }
            }
            return sb.ToString();
        }

        public string[] PeVerifyModules(string[] modulesToVerify, bool throwOnError = true)
        {
            // For Windows RT (ARM) THE CLRHelper.Peverify appears to not work and will exclude this 
            // for ARM testing at present.
            StringBuilder errors = new StringBuilder();
            List<string> allOutput = new List<string>();

// Disable all PEVerification due to https://github.com/dotnet/roslyn/issues/6190
#if false

            foreach (var name in modulesToVerify)
            {
                var module = _modules[name];
                string[] output = CLRHelpers.PeVerify(module.Image);
                if (output.Length > 0)
                {
                    if (modulesToVerify.Length > 1)
                    {
                        errors.AppendLine();
                        errors.AppendLine("<<" + name + ">>");
                        errors.AppendLine();
                    }

                    foreach (var error in output)
                    {
                        errors.AppendLine(error);
                    }
                }

                if (!throwOnError)
                {
                    allOutput.AddRange(output);
                }
            }

            if (throwOnError && errors.Length > 0)
            {
                string dumpDir;
                DumpAssemblyData(_modules.Values, out dumpDir);
                throw new PeVerifyException(errors.ToString(), dumpDir);
            }
#endif
            return allOutput.ToArray();
        }
    }
}
