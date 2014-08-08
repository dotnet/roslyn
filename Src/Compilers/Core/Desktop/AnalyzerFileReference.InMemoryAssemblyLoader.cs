// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public partial class AnalyzerFileReference
    {
        /// <summary>
        /// Handles loading assemblies without locking the corresponding DLL on disk.
        /// 
        /// This is achieved by copying the DLL into a byte array, and then calling
        /// <see cref="Assembly.Load(byte[])"/> to load the assembly from the byte array.
        /// 
        /// Does not handle multi-module assemblies.
        /// </summary>
        /// 
        /// <remarks>
        /// The interesting bit is that <see cref="Assembly"/> objects loaded in this way
        /// are not placed in the Load or Load-From binding contexts. If one of these
        /// needs a dependency to be resolved and it isn't already loaded or available in
        /// the GAC, the runtime will not do any probing to find it. Since it doesn't know
        /// where the assembly came from, it doesn't assume it knows how to resolve its
        /// dependencies.
        /// 
        /// This means we also need to hook the <see cref="AppDomain.AssemblyResolve"/>
        /// event and handle finding and loading dependencies ourselves. We also need to
        /// handle loading the dependencies' dependencies, and so on.
        /// </remarks>
        private static class InMemoryAssemblyLoader
        {
            /// <summary>
            /// Maps from a full path to a file to a corresponding <see cref="Assembly"/>
            /// that we've already loaded.
            /// </summary>
            private static readonly Dictionary<string, Assembly> assembliesFromFiles =
                new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Maps from an assembly full name to the directory where we found the
            /// corresponding file.
            /// </summary>
            private static readonly Dictionary<string, string> directoriesFromAssemblyNames =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Maps from an assembly full name to the corresponding <see cref="Assembly"/>.
            /// </summary>
            private static readonly Dictionary<string, Assembly> assembliesFromNames =
                new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Controls access to the loader's data structures.
            /// </summary>
            private static readonly object guard = new object();

            private static bool hookedAssemblyResolve = false;

            /// <summary>
            /// Loads the <see cref="Assembly"/> at the given path without locking the file.
            /// </summary>
            public static Assembly Load(string fullPath)
            {
                CompilerPathUtilities.RequireAbsolutePath(fullPath, "fullPath");

                try
                {
                    fullPath = Path.GetFullPath(fullPath);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(e.Message, "fullPath");
                }

                lock (guard)
                {
                    Assembly assembly;
                    if (assembliesFromFiles.TryGetValue(fullPath, out assembly))
                    {
                        return assembly;
                    }

                    assembly = LoadCore(fullPath);

                    if (!hookedAssemblyResolve)
                    {
                        hookedAssemblyResolve = true;

                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                    }

                    return assembly;
                }
            }

            /// <summary>
            /// Performs the actual loading of the assembly, updates data structures, and
            /// fires the <see cref="AssemblyLoad"/> event.
            /// </summary>
            private static Assembly LoadCore(string fullPath)
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                Assembly assembly = Assembly.Load(bytes);

                string directory = Path.GetDirectoryName(fullPath);
                string assemblyName = assembly.FullName;

                assembliesFromFiles[fullPath] = assembly;
                directoriesFromAssemblyNames[assemblyName] = directory;
                assembliesFromNames[assemblyName] = assembly;

                EventHandler<AnalyzerAssemblyLoadEventArgs> handler = AnalyzerFileReference.AssemblyLoad;
                if (handler != null)
                {
                    handler(null, new AnalyzerAssemblyLoadEventArgs(fullPath, assembly));
                }

                return assembly;
            }

            /// <summary>
            /// Handles the <see cref="AppDomain.AssemblyResolve"/> event when the requesting
            /// assembly is one that we've loaded.
            /// 
            /// We assume that an assembly's dependencies can be found next to it in the file
            /// system.
            /// </summary>
            private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            {
                if (args.RequestingAssembly == null)
                {
                    // We don't know who is requesting the load; don't try to satisfy the request.
                    return null;
                }

                lock (guard)
                {
                    string requestingAssemblyName = args.RequestingAssembly.FullName;

                    string directoryPath;
                    if (!directoriesFromAssemblyNames.TryGetValue(requestingAssemblyName, out directoryPath))
                    {
                        // The requesting assembly is not one of ours; don't try to satisfy the request.
                        return null;
                    }

                    Assembly assembly;
                    if (assembliesFromNames.TryGetValue(args.Name, out assembly))
                    {
                        // We've already loaded an assembly by this name; use that.
                        return assembly;
                    }

                    AssemblyIdentity assemblyIdentity;
                    if (!AssemblyIdentity.TryParseDisplayName(args.Name, out assemblyIdentity))
                    {
                        return null;
                    }

                    string assemblyFullPath = Path.Combine(directoryPath, assemblyIdentity.Name + ".dll");

                    assembly = LoadCore(assemblyFullPath);

                    return assembly;
                }
            }
        }
    }
}