// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#if NETCOREAPP2_1
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities.CoreClr
{
    internal sealed class TestExecutionLoadContext : AssemblyLoadContext
    {
        private readonly static ImmutableDictionary<string, string> s_platformAssemblyPaths = GetPlatformAssemblyPaths();
        private readonly static Dictionary<string, Assembly> s_loadedPlatformAssemblies = new Dictionary<string, Assembly>(StringComparer.Ordinal);

        private readonly Dictionary<string, ModuleData> _dependencies;

        public TestExecutionLoadContext(IList<ModuleData> dependencies)
        {
            _dependencies = new Dictionary<string, ModuleData>(dependencies.Count, StringComparer.Ordinal);
            foreach (var dep in dependencies)
            {
                _dependencies.Add(dep.FullName, dep);
            }
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            lock (s_platformAssemblyPaths)
            {
                Assembly assembly;
                if (s_platformAssemblyPaths.TryGetValue(assemblyName.Name, out var assemblyPath))
                {
                    assembly = LoadPlatformAssembly(assemblyName, assemblyPath);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                }

                if (_dependencies.TryGetValue(assemblyName.FullName, out var moduleData))
                {
                    return LoadImageAsAssembly(moduleData.Image);
                }

                return null;
            }
        }

        private Assembly LoadPlatformAssembly(AssemblyName assemblyName, string assemblyPath)
        {
            lock (s_loadedPlatformAssemblies)
            {
                if (s_loadedPlatformAssemblies.TryGetValue(assemblyPath, out var assembly))
                {
                    return assembly;
                }
                else
                {
                    try
                    {
                        assembly = Default.LoadFromAssemblyName(assemblyName);
                    }
                    catch (FileNotFoundException)
                    {
                        // The assembly wasn't in the TPA list we can try to load
                        // it from the dependencies list of the assembly. However,
                        // if the assembly is mscorlib we won't be able to reload it,
                        // no matter what
                        if (assemblyName.Name == "mscorlib")
                        {
                            throw;
                        }
                        assembly = null;
                    }

                    return assembly;
                }
            }
        }

        private Assembly LoadImageAsAssembly(ImmutableArray<byte> mainImage)
        {
            using (var assemblyStream = new MemoryStream(mainImage.ToArray()))
            {
                return LoadFromStream(assemblyStream);
            }
        }

        internal (int ExitCode, string Output) Execute(ImmutableArray<byte> mainImage, string[] mainArgs, int? expectedOutputLength)
        {
            var mainAssembly = LoadImageAsAssembly(mainImage);
            var entryPoint = mainAssembly.EntryPoint;

            AssertEx.NotNull(entryPoint, "Attempting to execute an assembly that has no entrypoint; is your test trying to execute a DLL?");

            int exitCode = 0;
            SharedConsole.CaptureOutput(() =>
            {
                var count = entryPoint.GetParameters().Length;
                object[] args;
                if (count == 0)
                {
                    args = Array.Empty<object>();
                }
                else if (count == 1)
                {
                    args = new[] { mainArgs ?? Array.Empty<string>() };
                }
                else
                {
                    throw new Exception("Unrecognized entry point");
                }

                exitCode = entryPoint.Invoke(null, args) is int exit ? exit : 0;
            }, expectedOutputLength ?? 0, out var stdOut, out var stdErr);

            var output = stdOut + stdErr;
            return (exitCode, output);
        }

        private static ImmutableDictionary<string, string> GetPlatformAssemblyPaths()
        {
            var assemblyNames = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

            // AppContext.GetData returns a string containing a separated list
            // of paths to the Trusted Platform Assemblies for this program. The TPA is the
            // set of assemblies we will always load from this location, regardless
            // of whether or not a load from another location is requested.
            var platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
            foreach (var assemblyPath in platformAssemblies)
            {
                if (!String.IsNullOrEmpty(assemblyPath) && TryGetAssemblyName(assemblyPath, out string assemblyName))
                {
                    assemblyNames.Add(assemblyName, assemblyPath);
                }
            }

            return assemblyNames.ToImmutable();
        }

        private static bool TryGetAssemblyName(string filePath, out string name)
        {
            try
            {
                using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var peReader = new PEReader(fileStream))
                {
                    if (peReader.HasMetadata)
                    {
                        var mdReader = peReader.GetMetadataReader();
                        if (mdReader.IsAssembly)
                        {
                            var assemblyDef = mdReader.GetAssemblyDefinition();
                            name = mdReader.GetString(assemblyDef.Name);

                            return true;
                        }
                    }
                }
            }
            catch (BadImageFormatException) { } // Fall through

            name = null;
            return false;
        }

        public SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName, IEnumerable<ModuleDataId> searchModules)
        {
            try
            {
                var signatures = new SortedSet<string>();
                foreach (var id in searchModules)
                {
                    var name = new AssemblyName(id.FullName);
                    var assembly = LoadFromAssemblyName(name);
                    foreach (var signature in MetadataSignatureHelper.GetMemberSignatures(assembly, fullyQualifiedTypeName, memberName))
                    {
                        signatures.Add(signature);
                    }
                }
                return signatures;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting signatures {fullyQualifiedTypeName}.{memberName}", ex);
            }
        }
    }
}
#endif
