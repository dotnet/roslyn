// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CommandLine.AssemblyIdentityUtils;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AbstractAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly Dictionary<string, Assembly> _pathsToAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Assembly> _namesToAssemblies = new Dictionary<string, Assembly>();
        private readonly List<string> _dependencyPaths = new List<string>();
        private readonly object _guard = new object();

        private bool _hookedAssemblyResolve;

        /// <summary>
        /// Implemented by derived types to handle the actual loading of an assembly from
        /// a file on disk, and any bookkeeping specific to the derived type.
        /// </summary>
        protected abstract Assembly LoadCore(string fullPath);

        public void AddDependencyLocation(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            lock (_guard)
            {
                if (!_dependencyPaths.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
                {
                    _dependencyPaths.Add(fullPath);
                }
            }
        }

        public Assembly LoadFromPath(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            lock (_guard)
            {
                Assembly assembly;
                if (_pathsToAssemblies.TryGetValue(fullPath, out assembly))
                {
                    return assembly;
                }

                assembly = LoadInternal(fullPath);

                if (!_hookedAssemblyResolve)
                {
                    _hookedAssemblyResolve = true;

                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                }

                return assembly;
            }
        }

        private Assembly LoadInternal(string fullPath)
        {
            Assembly assembly = LoadCore(fullPath);
            string assemblyName = assembly.FullName;

            _pathsToAssemblies[fullPath] = assembly;
            _namesToAssemblies[assemblyName] = assembly;

            return assembly;
        }

        /// <summary>
        /// Handler for <see cref="AppDomain.AssemblyResolve"/>. Delegates to <see cref="AssemblyResolveInternal(ResolveEventArgs)"/>
        /// and prevents exceptions from leaking out.
        /// </summary>
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return AssemblyResolveInternal(args);
            }
            catch
            {
                return null;
            }
        }

        private Assembly AssemblyResolveInternal(ResolveEventArgs args)
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
                        return LoadInternal(candidatePath);
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
    }
}
