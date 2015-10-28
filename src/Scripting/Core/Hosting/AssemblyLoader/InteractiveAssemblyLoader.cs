// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Implements an assembly loader for interactive compiler and REPL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The class is thread-safe.
    /// </para>
    /// </remarks>
    public sealed partial class InteractiveAssemblyLoader : IDisposable
    {
        private class LoadedAssembly
        {
            /// <summary>
            /// The original path of the assembly before it was shadow-copied.
            /// For GAC'd assemblies, this is equal to Assembly.Location no matter what path was used to load them.
            /// </summary>
            public string OriginalPath { get; set; }
        }

        private readonly AssemblyLoaderImpl _runtimeAssemblyLoader;

        private readonly MetadataShadowCopyProvider _shadowCopyProvider;

        // Synchronizes assembly reference tracking.
        // Needs to be thread-safe since assembly loading may be triggered at any time by CLR type loader.
        private readonly object _referencesLock = new object();

        // loaded assembly -> loaded assembly info
        private readonly Dictionary<Assembly, LoadedAssembly> _assembliesLoadedFromLocation;

        // { original full path -> assembly }
        // Note, that there might be multiple entries for a single GAC'd assembly.
        private readonly Dictionary<string, AssemblyAndLocation> _assembliesLoadedFromLocationByFullPath;

        // simple name -> loaded assemblies
        private readonly Dictionary<string, List<Assembly>> _loadedAssembliesBySimpleName;

        // simple name -> identity and location of a known dependency
        private readonly Dictionary<string, List<AssemblyIdentityAndLocation>> _dependenciesWithLocationBySimpleName;

        [SuppressMessage("Performance", "RS0008", Justification = "Equality not actually implemented")]
        private struct AssemblyIdentityAndLocation
        {
            public readonly AssemblyIdentity Identity;
            public readonly string Location;

            public AssemblyIdentityAndLocation(AssemblyIdentity identity, string location)
            {
                Debug.Assert(identity != null && location != null);

                this.Identity = identity;
                this.Location = location;
            }

            public override int GetHashCode()
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override bool Equals(object obj)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override string ToString()
            {
                return Identity + " @ " + Location;
            }
        }

        public InteractiveAssemblyLoader(MetadataShadowCopyProvider shadowCopyProvider = null)
        {
            _shadowCopyProvider = shadowCopyProvider;

            _assembliesLoadedFromLocationByFullPath = new Dictionary<string, AssemblyAndLocation>();
            _assembliesLoadedFromLocation = new Dictionary<Assembly, LoadedAssembly>();
            _loadedAssembliesBySimpleName = new Dictionary<string, List<Assembly>>(AssemblyIdentityComparer.SimpleNameComparer);
            _dependenciesWithLocationBySimpleName = new Dictionary<string, List<AssemblyIdentityAndLocation>>();

            _runtimeAssemblyLoader = AssemblyLoaderImpl.Create(this);
        }

        public void Dispose()
        {
            _runtimeAssemblyLoader.Dispose();
        }

        internal Assembly LoadAssemblyFromStream(Stream peStream, Stream pdbStream)
        {
            Assembly assembly = _runtimeAssemblyLoader.LoadFromStream(peStream, pdbStream);
            RegisterDependency(assembly);
            return assembly;
        }

        private AssemblyAndLocation Load(string reference)
        {
            MetadataShadowCopy copy = null;
            try
            {
                if (_shadowCopyProvider != null)
                {
                    // All modules of the assembly has to be copied at once to keep the metadata consistent.
                    // This API copies the xml doc file and keeps the memory-maps of the metadata.
                    // We don't need that here but presumably the provider is shared with the compilation API that needs both.
                    // Ideally the CLR would expose API to load Assembly given a byte* and not create their own memory-map.
                    copy = _shadowCopyProvider.GetMetadataShadowCopy(reference, MetadataImageKind.Assembly);
                }

                var result = _runtimeAssemblyLoader.LoadFromPath((copy != null) ? copy.PrimaryModule.FullPath : reference);

                if (_shadowCopyProvider != null && result.GlobalAssemblyCache)
                {
                    _shadowCopyProvider.SuppressShadowCopy(reference);
                }

                return result;
            }
            catch (FileNotFoundException)
            {
                return default(AssemblyAndLocation);
            }
            finally
            {
                // copy holds on the file handle, we need to keep the handle 
                // open until the file is locked by the CLR assembly loader:
                copy?.DisposeFileHandles();
            }
        }

        /// <summary>
        /// Notifies the assembly loader about a dependency that might be loaded in future.
        /// </summary>
        /// <param name="dependency">Assembly identity.</param>
        /// <param name="path">Assembly location.</param>
        /// <remarks>
        /// Associates a full assembly name with its location. The association is used when an assembly 
        /// is being loaded and its name needs to be resolved to a location.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dependency"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is not an existing path.</exception>
        public void RegisterDependency(AssemblyIdentity dependency, string path)
        {
            if (dependency == null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }

            if (!PathUtilities.IsAbsolute(path))
            {
                throw new ArgumentException(ScriptingResources.AbsolutePathExpected, nameof(path));
            }

            lock (_referencesLock)
            {
                RegisterDependencyNoLock(new AssemblyIdentityAndLocation(dependency, path));
            }
        }

        /// <summary>
        /// Notifies the assembly loader about an in-memory dependency that should be available within the resolution context.
        /// </summary>
        /// <param name="dependency">Assembly identity.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dependency"/> is null.</exception>
        /// <remarks>
        /// When another in-memory assembly references the <paramref name="dependency"/> the loader 
        /// responds with the specified dependency if the assembly identity matches the requested one.
        /// </remarks>
        public void RegisterDependency(Assembly dependency)
        {
            if (dependency == null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }

            lock (_referencesLock)
            {
                RegisterLoadedAssemblySimpleNameNoLock(dependency);
            }
        }

        private void RegisterLoadedAssemblySimpleNameNoLock(Assembly assembly)
        {
            List<Assembly> sameSimpleNameAssemblies;
            string simpleName = assembly.GetName().Name;
            if (_loadedAssembliesBySimpleName.TryGetValue(simpleName, out sameSimpleNameAssemblies))
            {
                sameSimpleNameAssemblies.Add(assembly);
            }
            else
            {
                _loadedAssembliesBySimpleName.Add(simpleName, new List<Assembly> { assembly });
            }
        }

        private void RegisterDependencyNoLock(AssemblyIdentityAndLocation dependency)
        {
            List<AssemblyIdentityAndLocation> sameSimpleNameAssemblyIdentities;
            string simpleName = dependency.Identity.Name;
            if (_dependenciesWithLocationBySimpleName.TryGetValue(simpleName, out sameSimpleNameAssemblyIdentities))
            {
                sameSimpleNameAssemblyIdentities.Add(dependency);
            }
            else
            {
                _dependenciesWithLocationBySimpleName.Add(simpleName, new List<AssemblyIdentityAndLocation> { dependency });
            }
        }

        internal Assembly ResolveAssembly(string assemblyDisplayName, Assembly requestingAssemblyOpt)
        {
            AssemblyIdentity identity;
            if (!AssemblyIdentity.TryParseDisplayName(assemblyDisplayName, out identity))
            {
                return null;
            }

            string loadDirectoryOpt;
            lock (_referencesLock)
            {
                LoadedAssembly loadedAssembly;
                if (requestingAssemblyOpt != null && 
                    _assembliesLoadedFromLocation.TryGetValue(requestingAssemblyOpt, out loadedAssembly))
                {
                    loadDirectoryOpt = Path.GetDirectoryName(loadedAssembly.OriginalPath);
                }
                else
                {
                    loadDirectoryOpt = null;
                }
            }

            return ResolveAssembly(identity, loadDirectoryOpt);
        }

        internal Assembly ResolveAssembly(AssemblyIdentity identity, string loadDirectoryOpt)
        {
            // if the referring assembly is already loaded by our loader, load from its directory:
            if (loadDirectoryOpt != null)
            {
                string pathWithoutExtension = Path.Combine(loadDirectoryOpt, identity.Name);
                string originalDllPath = pathWithoutExtension + ".dll";
                string originalExePath = pathWithoutExtension + ".exe";

                lock (_referencesLock)
                {
                    AssemblyAndLocation assembly;
                    if (_assembliesLoadedFromLocationByFullPath.TryGetValue(originalDllPath, out assembly) ||
                        _assembliesLoadedFromLocationByFullPath.TryGetValue(originalExePath, out assembly))
                    {
                        return assembly.Assembly;
                    }
                }

                // Copy & load both .dll and .exe, this is not a common scenario we would need to optimize for.
                // Remember both loaded assemblies for the next time, even though their versions might not match the current request
                // they might match the next one and we don't want to load them again.
                Assembly dll = ShadowCopyAndLoadDependency(originalDllPath).Assembly;
                Assembly exe = ShadowCopyAndLoadDependency(originalExePath).Assembly;

                if (dll == null ^ exe == null)
                {
                    return dll ?? exe;
                }

                if (dll != null && exe != null)
                {
                    // .dll and an .exe of the same name might have different versions, 
                    // one of which might match the requested version.
                    // Prefer .dll if they are both the same. 
                    return FindHighestVersionOrFirstMatchingIdentity(identity, new[] { dll, exe });
                }
            }

            return GetOrLoadKnownAssembly(identity);
        }

        private Assembly GetOrLoadKnownAssembly(AssemblyIdentity identity)
        {
            Assembly assembly = null;
            string assemblyFileToLoad = null;

            // Try to find the assembly among assemblies that we loaded, comparing its identity with the requested one.
            lock (_referencesLock)
            {
                // already loaded assemblies:
                List<Assembly> sameSimpleNameAssemblies;
                if (_loadedAssembliesBySimpleName.TryGetValue(identity.Name, out sameSimpleNameAssemblies))
                {
                    assembly = FindHighestVersionOrFirstMatchingIdentity(identity, sameSimpleNameAssemblies);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                }

                // names:
                List<AssemblyIdentityAndLocation> sameSimpleNameIdentities;
                if (_dependenciesWithLocationBySimpleName.TryGetValue(identity.Name, out sameSimpleNameIdentities))
                {
                    var identityAndLocation = FindHighestVersionOrFirstMatchingIdentity(identity, sameSimpleNameIdentities);
                    if (identityAndLocation.Identity != null)
                    {
                        assemblyFileToLoad = identityAndLocation.Location;
                        AssemblyAndLocation assemblyAndLocation;
                        if (_assembliesLoadedFromLocationByFullPath.TryGetValue(assemblyFileToLoad, out assemblyAndLocation))
                        {
                            return assemblyAndLocation.Assembly;
                        }
                    }
                }
            }

            if (assemblyFileToLoad != null)
            {
                assembly = ShadowCopyAndLoadDependency(assemblyFileToLoad).Assembly;
            }

            return assembly;
        }

        private AssemblyAndLocation ShadowCopyAndLoadDependency(string originalPath)
        {
            AssemblyAndLocation assemblyAndLocation = Load(originalPath);
            if (assemblyAndLocation.IsDefault)
            {
                return default(AssemblyAndLocation);
            }

            lock (_referencesLock)
            {
                // Always remember the path. The assembly might have been loaded from another path or not loaded yet.
                _assembliesLoadedFromLocationByFullPath[originalPath] = assemblyAndLocation;

                LoadedAssembly loadedAssembly;
                if (_assembliesLoadedFromLocation.TryGetValue(assemblyAndLocation.Assembly, out loadedAssembly))
                {
                    return assemblyAndLocation;
                }

                _assembliesLoadedFromLocation.Add(
                    assemblyAndLocation.Assembly,
                    new LoadedAssembly { OriginalPath = assemblyAndLocation.GlobalAssemblyCache ? assemblyAndLocation.Location : originalPath });

                RegisterLoadedAssemblySimpleNameNoLock(assemblyAndLocation.Assembly);
            }

            return assemblyAndLocation;
        }

        private static Assembly FindHighestVersionOrFirstMatchingIdentity(AssemblyIdentity identity, IEnumerable<Assembly> assemblies)
        {
            Assembly candidate = null;
            Version candidateVersion = null;
            foreach (var assembly in assemblies)
            {
                var assemblyIdentity = AssemblyIdentity.FromAssemblyDefinition(assembly);
                if (DesktopAssemblyIdentityComparer.Default.ReferenceMatchesDefinition(identity, assemblyIdentity))
                {
                    if (candidate == null || candidateVersion < assemblyIdentity.Version)
                    {
                        candidate = assembly;
                        candidateVersion = assemblyIdentity.Version;
                    }
                }
            }

            return candidate;
        }

        private static AssemblyIdentityAndLocation FindHighestVersionOrFirstMatchingIdentity(AssemblyIdentity identity, IEnumerable<AssemblyIdentityAndLocation> assemblies)
        {
            var candidate = default(AssemblyIdentityAndLocation);
            foreach (var assembly in assemblies)
            {
                if (DesktopAssemblyIdentityComparer.Default.ReferenceMatchesDefinition(identity, assembly.Identity))
                {
                    if (candidate.Identity == null || candidate.Identity.Version < assembly.Identity.Version)
                    {
                        candidate = assembly;
                    }
                }
            }

            return candidate;
        }
    }
}
