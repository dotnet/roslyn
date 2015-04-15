// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Loads analyzer assemblies from their original locations in the file system.
    /// Assemblies will only be loaded from the locations specified when the loader
    /// is instantiated.
    /// </summary>
    /// <remarks>
    /// This type is meant to be used in scenarios where it is OK for the analyzer
    /// assemblies to be locked on disk for the lifetime of the host; for example,
    /// csc.exe and vbc.exe. In scenarios where support for updating or deleting
    /// the analyzer on disk is required a different loader should be used.
    /// </remarks>
    public sealed class SimpleAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly List<string> _dependencyPaths = new List<string>();
        private readonly Dictionary<string, Assembly> _pathsToAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Assembly> _namesToAssemblies = new Dictionary<string, Assembly>();
        private readonly object _guard = new object();

        private bool _hookedAssemblyResolve = false;

        /// <summary>
        /// Add the path to an assembly to be considered while handling the
        /// <see cref="AppDomain.AssemblyResolve"/> event.
        /// </summary>
        public void AddDependencyLocation(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            if (!_dependencyPaths.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
            {
                _dependencyPaths.Add(fullPath);
            }
        }

        /// <summary>
        /// Given a path to an assembly on disk, loads and returns the
        /// corresponding <see cref="Assembly"/> object.
        /// </summary>
        /// <remarks>
        /// Multiple calls with the same path will return the same 
        /// <see cref="Assembly"/> instance.
        /// 
        /// Also hooks the <see cref="AppDomain.AssemblyResolve"/> event in
        /// case it needs to load a dependency.
        /// </remarks>
        public Assembly LoadFromPath(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));

            lock (_guard)
            {
                Assembly assembly;
                if (_pathsToAssemblies.TryGetValue(fullPath, out assembly))
                {
                    return assembly;
                }

                assembly = LoadCore(fullPath);

                if (!_hookedAssemblyResolve)
                {
                    _hookedAssemblyResolve = true;

                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                }

                return assembly;
            }
        }

        private Assembly LoadCore(string fullPath)
        {
            Assembly assembly = Assembly.LoadFrom(fullPath);
            string assemblyName = assembly.FullName;

            _pathsToAssemblies[fullPath] = assembly;
            _namesToAssemblies[assemblyName] = assembly;

            return assembly;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string requestedNameWithPolicyApplied = AppDomain.CurrentDomain.ApplyPolicy(args.Name);

            lock (_guard)
            {
                Assembly assembly;
                if (_namesToAssemblies.TryGetValue(requestedNameWithPolicyApplied, out assembly))
                {
                    return assembly;
                }

                AssemblyIdentity requestedAssemblyIdentity;
                if (!AssemblyIdentity.TryParseDisplayName(requestedNameWithPolicyApplied, out requestedAssemblyIdentity))
                {
                    return null;
                }

                foreach (string candidatePath in _dependencyPaths)
                {
                    if (AssemblyAlreadyLoaded(candidatePath) ||
                        !FileMatchesAssemblyName(candidatePath, requestedAssemblyIdentity.Name))
                    {
                        continue;
                    }

                    AssemblyIdentity candidateIdentity = TryGetAssemblyIdentity(candidatePath);

                    if (requestedAssemblyIdentity.Equals(candidateIdentity))
                    {
                        return LoadCore(candidatePath);
                    }
                }

                return null;
            }
        }

        private bool AssemblyAlreadyLoaded(string path)
        {
            return _pathsToAssemblies.ContainsKey(path);
        }

        private bool FileMatchesAssemblyName(string path, string assemblySimpleName)
        {
            return Path.GetFileNameWithoutExtension(path).Equals(assemblySimpleName, StringComparison.OrdinalIgnoreCase);
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
