// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Implements an assembly loader for interactive compiler and REPL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An assembly is loaded into CLR’s Load Context if it is in the GAC, otherwise it's loaded into No Context via <see cref="Assembly.LoadFile(string)"/>.
    /// <see cref="Assembly.LoadFile(string)"/> automatically redirects to GAC if the assembly has a strong name and there is an equivalent assembly in GAC. 
    /// </para>
    /// <para>
    /// The class is thread-safe.
    /// </para>
    /// </remarks>
    internal sealed class InteractiveAssemblyLoader : AssemblyLoader
    {
        private class LoadedAssembly
        {
            public bool LoadedExplicitly { get; set; }

            /// <summary>
            /// The original path of the assembly before it was shadow-copied.
            /// For GAC'd assemblies, this is equal to Assembly.Location no matter what path was used to load them.
            /// </summary>
            public string OriginalPath { get; set; }
        }

        private readonly MetadataShadowCopyProvider _shadowCopyProvider;

        // Synchronizes assembly reference tracking.
        // Needs to be thread-safe since assembly loading may be triggered at any time by CLR type loader.
        private object ReferencesLock { get { return _loadedAssemblies; } }

        // loaded assembly -> loaded assmbly info (the path might be different from Assembly.Location if the assembly was shadow copied).
        private readonly Dictionary<Assembly, LoadedAssembly> _loadedAssemblies;

        // { original full path -> assembly }
        // Note, that there might be multiple entries for a single GAC'd assembly.
        private readonly Dictionary<string, Assembly> _loadedAssembliesByPath;

        // simple name -> loaded assemblies
        private readonly Dictionary<string, List<Assembly>> _loadedAssembliesBySimpleName;

        // Assemblies registered for lazy load when assembly resolve event is triggered.
        // (We don't want to preload these eagerly using LoadReference API.)
        //
        // simple name -> AssemblyName that includes file path
        private readonly Dictionary<string, List<AssemblyIdentityAndLocation>> _identitiesBySimpleName;

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

            Assembly mscorlib = typeof(object).Assembly;
            _loadedAssembliesByPath = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase)
            {
                { mscorlib.Location, mscorlib }
            };

            _loadedAssemblies = new Dictionary<Assembly, LoadedAssembly>()
            {
                { mscorlib, new LoadedAssembly { LoadedExplicitly = true, OriginalPath = mscorlib.Location } }
            };

            _loadedAssembliesBySimpleName = new Dictionary<string, List<Assembly>>(StringComparer.OrdinalIgnoreCase)
            {
                { "mscorlib", new List<Assembly> { mscorlib } }
            };

            _identitiesBySimpleName = new Dictionary<string, List<AssemblyIdentityAndLocation>>();

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolve);
        }

        /// <summary>
        /// Loads assembly with given identity.
        /// </summary>
        /// <param name="identity">The assembly identity.</param>
        /// <param name="location">Location of the assembly.</param>
        /// <returns>Loaded assembly.</returns>
        public override Assembly Load(AssemblyIdentity identity, string location = null)
        {
            if (!string.IsNullOrEmpty(location))
            {
                RegisterDependency(identity, location);
            }

            // Don't let the CLR load the assembly from the CodeBase (location), 
            // which uses LoadFrom semantics (we want LoadFile):
            return Assembly.Load(identity.ToAssemblyName());
        }

        /// <summary>
        /// Loads an assembly from path.
        /// </summary>
        /// <param name="path">Absolute assembly file path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is not an exisiting assembly file path.</exception>
        /// <exception cref="System.Reflection.TargetInvocationException">The assembly resolver threw an exception.</exception>
        public AssemblyLoadResult LoadFromPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!PathUtilities.IsAbsolute(path))
            {
                throw new ArgumentException("Path must be absolute", nameof(path));
            }

            try
            {
                return LoadFromPathInternal(FileUtilities.NormalizeAbsolutePath(path));
            }
            catch (TargetInvocationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message, "path", e);
            }
        }

        /// <summary>
        /// Notifies the assembly loader about a dependency that might be loaded in future.
        /// </summary>
        /// <param name="dependency">Assembly identity.</param>
        /// <param name="location">Assembly location.</param>
        /// <remarks>
        /// Associates a full assembly name with its location. The association is used when an assembly 
        /// is being loaded and its name needs to be resolved to a location.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dependency"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="location"/> is null or empty.</exception>
        public void RegisterDependency(AssemblyIdentity dependency, string location)
        {
            if (dependency == null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }

            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentException("location");
            }

            lock (ReferencesLock)
            {
                RegisterDependencyNoLock(new AssemblyIdentityAndLocation(dependency, location));
            }
        }

        private AssemblyLoadResult LoadFromPathInternal(string fullPath)
        {
            Assembly assembly;
            lock (ReferencesLock)
            {
                if (_loadedAssembliesByPath.TryGetValue(fullPath, out assembly))
                {
                    LoadedAssembly loadedAssembly = _loadedAssemblies[assembly];
                    if (loadedAssembly.LoadedExplicitly)
                    {
                        return AssemblyLoadResult.CreateAlreadyLoaded(assembly.Location, loadedAssembly.OriginalPath);
                    }
                    else
                    {
                        loadedAssembly.LoadedExplicitly = true;
                        return AssemblyLoadResult.CreateSuccessful(assembly.Location, loadedAssembly.OriginalPath);
                    }
                }
            }

            AssemblyLoadResult result;
            assembly = ShadowCopyAndLoadAssembly(fullPath, out result);
            if (assembly == null)
            {
                throw new FileNotFoundException(message: null, fileName: fullPath);
            }

            return result;
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
            if (_identitiesBySimpleName.TryGetValue(simpleName, out sameSimpleNameAssemblyIdentities))
            {
                sameSimpleNameAssemblyIdentities.Add(dependency);
            }
            else
            {
                _identitiesBySimpleName.Add(simpleName, new List<AssemblyIdentityAndLocation> { dependency });
            }
        }

        private Assembly ShadowCopyAndLoad(string reference)
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

                return LoadAssembly((copy != null) ? copy.PrimaryModule.FullPath : reference);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            finally
            {
                // copy holds on the file handle, we need to keep the handle 
                // open until the file is locked by the CLR assembly loader:
                if (copy != null)
                {
                    copy.DisposeFileHandles();
                }
            }
        }

        private static Assembly LoadAssembly(string path)
        {
            return Assembly.LoadFile(path);
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyIdentity identity;
            if (!AssemblyIdentity.TryParseDisplayName(args.Name, out identity))
            {
                return null;
            }

            if (args.RequestingAssembly != null)
            {
                string originalDllPath = null, originalExePath = null;

                string originalPath = null;
                lock (ReferencesLock)
                {
                    LoadedAssembly loadedAssembly;
                    if (_loadedAssemblies.TryGetValue(args.RequestingAssembly, out loadedAssembly))
                    {
                        originalPath = loadedAssembly.OriginalPath;

                        string pathWithoutExtension = Path.Combine(Path.GetDirectoryName(originalPath), identity.Name);
                        originalDllPath = pathWithoutExtension + ".dll";
                        originalExePath = pathWithoutExtension + ".exe";

                        Assembly assembly;
                        if (_loadedAssembliesByPath.TryGetValue(originalDllPath, out assembly) || _loadedAssembliesByPath.TryGetValue(originalExePath, out assembly))
                        {
                            return assembly;
                        }
                    }
                }

                // if the referring assembly is already loaded by our loader, load from its directory:
                if (originalPath != null)
                {
                    // Copy & load both .dll and .exe, this is not a common scenario we would need to optimize for.
                    // Remember both loaded assemblies for the next time, even though their versions might not match the current request
                    // they might match the next one and we don't want to load them again.
                    Assembly dll = ShadowCopyAndLoadDependency(originalDllPath);
                    Assembly exe = ShadowCopyAndLoadDependency(originalExePath);

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
            }

            return GetOrLoadKnownAssembly(identity);
        }

        private Assembly GetOrLoadKnownAssembly(AssemblyIdentity identity)
        {
            Assembly assembly = null;
            string assemblyFileToLoad = null;

            // Try to find the assembly among assemblies that we loaded, comparing its identity with the requested one.
            lock (ReferencesLock)
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
                if (_identitiesBySimpleName.TryGetValue(identity.Name, out sameSimpleNameIdentities))
                {
                    var identityAndLocation = FindHighestVersionOrFirstMatchingIdentity(identity, sameSimpleNameIdentities);
                    if (identityAndLocation.Identity != null)
                    {
                        assemblyFileToLoad = identityAndLocation.Location;
                        if (_loadedAssembliesByPath.TryGetValue(assemblyFileToLoad, out assembly))
                        {
                            return assembly;
                        }
                    }
                }
            }

            if (assemblyFileToLoad != null)
            {
                assembly = ShadowCopyAndLoadDependency(assemblyFileToLoad);
            }

            return assembly;
        }

        private Assembly ShadowCopyAndLoadAssembly(string originalPath, out AssemblyLoadResult result)
        {
            return ShadowCopyAndLoadAndAddEntry(originalPath, out result, explicitLoad: true);
        }

        private Assembly ShadowCopyAndLoadDependency(string originalPath)
        {
            AssemblyLoadResult result;
            return ShadowCopyAndLoadAndAddEntry(originalPath, out result, explicitLoad: false);
        }

        private Assembly ShadowCopyAndLoadAndAddEntry(string originalPath, out AssemblyLoadResult result, bool explicitLoad)
        {
            Assembly assembly = ShadowCopyAndLoad(originalPath);
            if (assembly == null)
            {
                result = default(AssemblyLoadResult);
                return null;
            }

            lock (ReferencesLock)
            {
                // Always remember the path. The assembly might have been loaded from another path or not loaded yet.
                _loadedAssembliesByPath[originalPath] = assembly;

                LoadedAssembly loadedAssembly;
                if (_loadedAssemblies.TryGetValue(assembly, out loadedAssembly))
                {
                    // The assembly has been loaded while we were copying the file and loading the copy;
                    // Ignore the copy (unfortunately we can't unload a loaded assembly) and use the
                    // existing one.
                    if (loadedAssembly.LoadedExplicitly)
                    {
                        result = AssemblyLoadResult.CreateAlreadyLoaded(assembly.Location, loadedAssembly.OriginalPath);
                    }
                    else
                    {
                        loadedAssembly.LoadedExplicitly = explicitLoad;
                        result = AssemblyLoadResult.CreateSuccessful(assembly.Location, loadedAssembly.OriginalPath);
                    }

                    return assembly;
                }
                else
                {
                    result = AssemblyLoadResult.CreateSuccessful(assembly.Location, assembly.GlobalAssemblyCache ? assembly.Location : originalPath);
                    _loadedAssemblies.Add(assembly, new LoadedAssembly { LoadedExplicitly = explicitLoad, OriginalPath = result.OriginalPath });
                }

                RegisterLoadedAssemblySimpleNameNoLock(assembly);
            }

            if (_shadowCopyProvider != null && assembly.GlobalAssemblyCache)
            {
                _shadowCopyProvider.SuppressShadowCopy(originalPath);
            }

            return assembly;
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
