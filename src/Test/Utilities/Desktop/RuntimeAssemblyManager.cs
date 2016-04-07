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
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class RuntimeAssemblyManager : MarshalByRefObject, IDisposable
    {
        private enum Kind
        {
            ModuleData,
            Assembly
        }

        private struct AssemblyData
        {
            internal ModuleData ModuleData { get; }
            internal Assembly Assembly { get; }
            internal Kind Kind => Assembly != null ? Kind.Assembly : Kind.ModuleData;
            internal ModuleDataId Id => Assembly != null ? new ModuleDataId(Assembly) : ModuleData.Id;

            internal AssemblyData(ModuleData moduleData)
            {
                ModuleData = moduleData;
                Assembly = null;
            }

            internal AssemblyData(Assembly assembly)
            {
                ModuleData = default(ModuleData);
                Assembly = assembly;
            }
        }

        private static int s_dumpCount;

        private readonly AppDomainAssemblyCache _assemblyCache = AppDomainAssemblyCache.GetOrCreate();
        private readonly Dictionary<string, AssemblyData> _fullNameToAssemblyDataMap;
        private readonly Dictionary<Guid, AssemblyData> _mvidToAssemblyDataMap;
        private readonly List<Guid> _mainMvids;

        // Assemblies loaded by this manager.
        private readonly HashSet<Assembly> _loadedAssemblies;

        /// <summary>
        /// The AppDomain we create to host the RuntimeAssemblyManager will always have the mscorlib
        /// it was compiled against.  It's possible the data we are verifying or running used a slightly
        /// different mscorlib.  Hence we can't do exact MVID matching on them.  This tracks the set of 
        /// modules loaded when we started the RuntimeAssemblyManager for which we can't do strict 
        /// comparisons.
        /// </summary>
        private readonly HashSet<string> _preloadedSet;

        private bool _containsNetModules;

        internal IEnumerable<ModuleData> ModuleDatas => _fullNameToAssemblyDataMap.Values.Where(x => x.Kind == Kind.ModuleData).Select(x => x.ModuleData);

        public RuntimeAssemblyManager()
        {
            _fullNameToAssemblyDataMap = new Dictionary<string, AssemblyData>(StringComparer.OrdinalIgnoreCase);
            _mvidToAssemblyDataMap = new Dictionary<Guid, AssemblyData>();
            _loadedAssemblies = new HashSet<Assembly>();
            _mainMvids = new List<Guid>();

            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += AssemblyResolve;
            currentDomain.AssemblyLoad += AssemblyLoad;
            CLRHelpers.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;

            _preloadedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in currentDomain.GetAssemblies())
            {
                var assemblyData = new AssemblyData(assembly);
                _preloadedSet.Add(assemblyData.Id.SimpleName);
                AddAssemblyData(assemblyData);
            }
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
            _fullNameToAssemblyDataMap.Clear();
            _mvidToAssemblyDataMap.Clear();
        }

        /// <summary>
        /// Adds given MVID into a list of module MVIDs that are considered owned by this manager.
        /// </summary>
        public void AddMainModuleMvid(Guid mvid)
        {
            if (!_mvidToAssemblyDataMap.ContainsKey(mvid))
            {
                throw new Exception($"No module with {mvid} loaded");
            }

            _mainMvids.Add(mvid);
        }

        /// <summary>
        /// True if given assembly is owned by this manager.
        /// </summary>
        private bool IsOwned(Assembly assembly)
        {
            if (assembly == null)
            {
                return false;
            }

            return _mainMvids.Count == 0
                || (assembly.ManifestModule != null && _mainMvids.Contains(assembly.ManifestModule.ModuleVersionId))
                || _loadedAssemblies.Contains(assembly);
        }

        internal bool ContainsNetModules()
        {
            return _containsNetModules;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
        /// Add this to the set of <see cref="ModuleData"/> that is managed by this instance.  It is okay to 
        /// return values that are already present. 
        /// </summary>
        /// <param name="modules"></param>
        public void AddModuleData(IEnumerable<ModuleData> modules)
        {
            foreach (var module in modules)
            {
                // If the module is already added then nothing else to do
                AssemblyData assemblyData;
                bool fullMatch;
                if (TryGetMatchingByFullName(module.Id, out assemblyData, out fullMatch))
                {
                    if (!fullMatch)
                    {
                        throw new Exception($"Two modules of name {assemblyData.Id.FullName} have different MVID");
                    }
                }
                else
                {
                    if (module.Kind == OutputKind.NetModule)
                    {
                        _containsNetModules = true;
                    }

                    AddAssemblyData(new AssemblyData(module));
                }
            }
        }

        public bool HasConflicts(IEnumerable<ModuleDataId> moduleDataIds)
        {
            foreach (var id in moduleDataIds)
            {
                AssemblyData assemblyData;
                bool fullMatch;
                if (TryGetMatchingByFullName(id, out assemblyData, out fullMatch) && !fullMatch)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddAssemblyData(AssemblyData assemblyData)
        {
            _fullNameToAssemblyDataMap.Add(assemblyData.Id.FullName, assemblyData);
            _mvidToAssemblyDataMap.Add(assemblyData.Id.Mvid, assemblyData);
        }

        /// <summary>
        /// Return the subset of IDs passed in which are not currently tracked by this instance.
        /// </summary>
        public List<ModuleDataId> GetMissing(IEnumerable<ModuleDataId> moduleIds)
        {
            var list = new List<ModuleDataId>();
            foreach (var id in moduleIds)
            {
                AssemblyData other;
                bool fullMatch;
                if (!TryGetMatchingByFullName(id, out other, out fullMatch) || !fullMatch)
                {
                    list.Add(id);
                }
            }

            return list;
        }

        private bool TryGetMatchingByFullName(ModuleDataId id, out AssemblyData assemblyData, out bool fullMatch)
        {
            if (_fullNameToAssemblyDataMap.TryGetValue(id.FullName, out assemblyData))
            {
                fullMatch = _preloadedSet.Contains(id.SimpleName) || id.Mvid == assemblyData.Id.Mvid;
                return true;
            }

            assemblyData = default(AssemblyData);
            fullMatch = false;
            return false;
        }

        private ImmutableArray<byte> GetModuleBytesByName(string moduleName)
        {
            AssemblyData data;
            if (!_fullNameToAssemblyDataMap.TryGetValue(moduleName, out data))
            {
                throw new KeyNotFoundException(String.Format("Could not find image for module '{0}'.", moduleName));
            }

            if (data.Kind != Kind.ModuleData)
            {
                throw new Exception($"Cannot get bytes for preloaded Assembly {data.Id.FullName}");
            }

            return data.ModuleData.Image;
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
        internal static Assembly LoadAsAssembly(string moduleName, ImmutableArray<byte> rawAssembly, bool reflectionOnly = false)
        {
            Debug.Assert(!rawAssembly.IsDefault);

            byte[] bytes = rawAssembly.ToArray();

            try
            {
                if (reflectionOnly)
                {
                    return Assembly.ReflectionOnlyLoad(bytes);
                }
                else
                {
                    return Assembly.Load(bytes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception loading {moduleName} reflectionOnly:{reflectionOnly}", ex);
            }
        }

        internal Assembly GetAssembly(string fullName, bool reflectionOnly)
        {
            AssemblyData data;
            if (!_fullNameToAssemblyDataMap.TryGetValue(fullName, out data))
            {
                return null;
            }

            Assembly assembly;
            switch (data.Kind)
            {
                case Kind.Assembly:
                    assembly = data.Assembly;
                    if (reflectionOnly && !assembly.ReflectionOnly)
                    {
                        assembly = Assembly.ReflectionOnlyLoad(assembly.FullName);
                    }
                    break;
                case Kind.ModuleData:
                    assembly = _assemblyCache.GetOrLoad(data.ModuleData, reflectionOnly);
                    break;
                default:
                    throw new InvalidOperationException();
            }

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

        internal SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName, IEnumerable<ModuleDataId> searchModules)
        {
            try
            {
                var signatures = new SortedSet<string>();
                foreach (var id in searchModules) // Check inside each assembly in the compilation
                {
                    var assembly = GetAssembly(id.FullName, reflectionOnly: true);
                    foreach (var signature in MetadataSignatureHelper.GetMemberSignatures(assembly, fullyQualifiedTypeName, memberName))
                    {
                        signatures.Add(signature);
                    }
                }
                return signatures;
            }
            catch (Exception ex)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Error getting signatures {fullyQualifiedTypeName}.{memberName}");
                builder.AppendLine($"Assemblies");
                foreach (var module in _fullNameToAssemblyDataMap.Values)
                {
                    builder.AppendLine($"\t{module.Id.SimpleName} {module.Id.Mvid} - {module.Kind} {_assemblyCache.GetOrDefault(module.Id, reflectionOnly: false) != null} {_assemblyCache.GetOrDefault(module.Id, reflectionOnly: true) != null}");
                }

                throw new Exception(builder.ToString(), ex);
            }
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
            Assembly assembly = LoadAsAssembly(moduleName, bytes);
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
            return DumpAssemblyData(ModuleDatas, out dumpDirectory);
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
                        var assemblyLocation = typeof(IRuntimeEnvironment).Assembly.Location;
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

            foreach (var name in modulesToVerify)
            {
                var assemblyData = _fullNameToAssemblyDataMap[name];
                if (assemblyData.Kind != Kind.ModuleData)
                {
                    continue;
                }

                var module = assemblyData.ModuleData;
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
                DumpAssemblyData(ModuleDatas, out dumpDir);
                throw new PeVerifyException(errors.ToString(), dumpDir);
            }
            return allOutput.ToArray();
        }
    }
}
