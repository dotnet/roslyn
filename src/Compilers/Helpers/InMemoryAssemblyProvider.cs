// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Roslyn.Utilities
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
    internal static class InMemoryAssemblyProvider 
    {
        public sealed class AssemblyLoadEventArgs : EventArgs
        {
            private readonly string _path;
            private readonly Assembly _loadedAssembly;

            public AssemblyLoadEventArgs(string path, Assembly loadedAssembly)
            {
                _path = path;
                _loadedAssembly = loadedAssembly;
            }

            public string Path
            {
                get { return _path; }
            }

            public Assembly LoadedAssembly
            {
                get { return _loadedAssembly; }
            }
        }

        /// <summary>
        /// Maps from a full path to a file to a corresponding <see cref="Assembly"/>
        /// that we've already loaded.
        /// </summary>
        private static readonly Dictionary<string, Assembly> s_assembliesFromFiles =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps from an assembly full name to the directory where we found the
        /// corresponding file.
        /// </summary>
        private static readonly Dictionary<string, string> s_filesFromAssemblyNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps from an assembly full name to the corresponding <see cref="Assembly"/>.
        /// </summary>
        private static readonly Dictionary<string, Assembly> s_assembliesFromNames =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps from the full path to an assembly to the full path of the assembly
        /// that requested it.
        /// </summary>
        private static readonly Dictionary<string, string> s_requestingFilesFromFiles =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Controls access to the loader's data structures.
        /// </summary>
        private static readonly object s_guard = new object();

        private static bool s_hookedAssemblyResolve = false;

        public static event EventHandler<AssemblyLoadEventArgs> AssemblyLoad;

        /// <summary>
        /// Loads the <see cref="Assembly"/> at the given path without locking the file.
        /// </summary>
        public static Assembly GetAssembly(string fullPath)
        {
            if (fullPath == null || !PathUtilities.IsAbsolute(fullPath))
            {
                throw new ArgumentException(nameof(fullPath));
            }

            try
            {
                fullPath = Path.GetFullPath(fullPath);
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, "fullPath");
            }

            lock (s_guard)
            {
                Assembly assembly;
                if (s_assembliesFromFiles.TryGetValue(fullPath, out assembly))
                {
                    return assembly;
                }

                assembly = LoadCore(fullPath);

                if (!s_hookedAssemblyResolve)
                {
                    s_hookedAssemblyResolve = true;

                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                }

                return assembly;
            }
        }

        public static string TryGetRequestingAssembly(string fullPath)
        {
            lock (s_guard)
            {
                string requestingAssemblyFullPath;
                if (s_requestingFilesFromFiles.TryGetValue(fullPath, out requestingAssemblyFullPath))
                {
                    return requestingAssemblyFullPath;
                }

                return null;
            }
        }

        public static string GetCandidatePath(string baseDirectory, AssemblyIdentity assemblyIdentity)
        {
            if (!string.IsNullOrEmpty(assemblyIdentity.CultureName))
            {
                baseDirectory = Path.Combine(baseDirectory, assemblyIdentity.CultureName);
            }

            return Path.Combine(baseDirectory, assemblyIdentity.Name + ".dll");
        }

        /// <summary>
        /// Performs the actual loading of the assembly, updates data structures, and
        /// fires the <see cref="AssemblyLoad"/> event.
        /// </summary>
        private static Assembly LoadCore(string fullPath)
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            Assembly assembly = Assembly.Load(bytes);

            string assemblyName = assembly.FullName;

            s_assembliesFromFiles[fullPath] = assembly;
            s_filesFromAssemblyNames[assemblyName] = fullPath;
            s_assembliesFromNames[assemblyName] = assembly;

            AssemblyLoad?.Invoke(null, new AssemblyLoadEventArgs(fullPath, assembly));

            return assembly;
        }

        /// <summary>
        /// Handles the <see cref="AppDomain.AssemblyResolve"/> event.
        /// </summary>
        /// <remarks>
        /// This handler catches and swallow any and all exceptions that
        /// arise, and simply returns null when they do. Leaking an exception
        /// from the event handler may interrupt the entire assembly
        /// resolution process, which is undesirable.
        /// </remarks>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                if (args.RequestingAssembly == null)
                {
                    return ResolveForUnknownRequestor(args.Name);
                }
                else
                {
                    return ResolveForKnownRequestor(args.Name, args.RequestingAssembly);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Attempts to find and load an <see cref="Assembly"/> when the requesting <see cref="Assembly"/>
        /// is unknown.
        /// </summary>
        /// <remarks>
        /// In this case we simply look next to all the assemblies we have previously loaded for one with the
        /// correct name and a matching <see cref="AssemblyIdentity"/>.
        /// </remarks>
        private static Assembly ResolveForUnknownRequestor(string requestedAssemblyName)
        {
            lock (s_guard)
            {
                string requestedNameWithPolicyApplied = AppDomain.CurrentDomain.ApplyPolicy(requestedAssemblyName);

                Assembly assembly;
                if (s_assembliesFromNames.TryGetValue(requestedNameWithPolicyApplied, out assembly))
                {
                    // We've already loaded an assembly by this name; use that.
                    return assembly;
                }

                AssemblyIdentity requestedAssemblyIdentity;
                if (!AssemblyIdentity.TryParseDisplayName(requestedNameWithPolicyApplied, out requestedAssemblyIdentity))
                {
                    return null;
                }

                foreach (string loadedAssemblyFullPath in s_assembliesFromFiles.Keys)
                {
                    string directoryPath = Path.GetDirectoryName(loadedAssemblyFullPath);

                    string candidateAssemblyFullPath = GetCandidatePath(directoryPath, requestedAssemblyIdentity);

                    AssemblyIdentity candidateAssemblyIdentity = TryGetAssemblyIdentity(candidateAssemblyFullPath);

                    if (requestedAssemblyIdentity.Equals(candidateAssemblyIdentity))
                    {
                        return LoadCore(candidateAssemblyFullPath);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Attempts to find and load an <see cref="Assembly"/> when the requesting <see cref="Assembly"/>
        /// is known.
        /// </summary>
        /// <remarks>
        /// This method differs from <see cref="ResolveForUnknownRequestor(string)"/> in a couple of ways.
        /// First, we only attempt to handle the load if the requesting assembly is one we've loaded.
        /// If it isn't one of ours, then presumably some other component is hooking <see cref="AppDomain.AssemblyResolve"/>
        /// and will have a better idea of how to load the assembly.
        /// Second, we only look immediately next to the requesting assembly, instead of next to all the assemblies
        /// we've previously loaded. An analyzer needs to ship with all of its dependencies, and if it doesn't we don't
        /// want to mask the problem.
        /// </remarks>
        private static Assembly ResolveForKnownRequestor(string requestedAssemblyName, Assembly requestingAssembly)
        {
            lock (s_guard)
            {
                string requestingAssemblyName = requestingAssembly.FullName;

                string requestingAssemblyFullPath;
                if (!s_filesFromAssemblyNames.TryGetValue(requestingAssemblyName, out requestingAssemblyFullPath))
                {
                    // The requesting assembly is not one of ours; don't try to satisfy the request.
                    return null;
                }

                string nameWithPolicyApplied = AppDomain.CurrentDomain.ApplyPolicy(requestedAssemblyName);

                Assembly assembly;
                if (s_assembliesFromNames.TryGetValue(nameWithPolicyApplied, out assembly))
                {
                    // We've already loaded an assembly by this name; use that.
                    return assembly;
                }

                AssemblyIdentity assemblyIdentity;
                if (!AssemblyIdentity.TryParseDisplayName(nameWithPolicyApplied, out assemblyIdentity))
                {
                    return null;
                }

                string directoryPath = Path.GetDirectoryName(requestingAssemblyFullPath);
                string assemblyFullPath = GetCandidatePath(directoryPath, assemblyIdentity);
                if (!File.Exists(assemblyFullPath))
                {
                    return null;
                }

                assembly = LoadCore(assemblyFullPath);

                s_requestingFilesFromFiles[assemblyFullPath] = requestingAssemblyFullPath;

                return assembly;
            }
        }

        private static AssemblyIdentity TryGetAssemblyIdentity(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                using (var peReader = new PEReader(stream))
                {
                    var metadataReader = peReader.GetMetadataReader();

                    AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();

                    string name = metadataReader.GetString(assemblyDefinition.Name);
                    Version version = assemblyDefinition.Version;

                    StringHandle cultureHandle = assemblyDefinition.Culture;
                    string cultureName = (!cultureHandle.IsNil) ? metadataReader.GetString(cultureHandle) : null;
                    AssemblyFlags flags = assemblyDefinition.Flags;

                    bool hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;
                    BlobHandle publicKeyHandle = assemblyDefinition.PublicKey;
                    ImmutableArray<byte> publicKeyOrToken = !publicKeyHandle.IsNil
                        ? metadataReader.GetBlobBytes(publicKeyHandle).AsImmutableOrNull()
                        : default(ImmutableArray<byte>);
                    return new AssemblyIdentity(name, version, cultureName, publicKeyOrToken, hasPublicKey);
                }
            }
            catch { }

            return null;
        }
    }
}
