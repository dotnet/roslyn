using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Roslyn.Compilers;
using Roslyn.Utilities;

namespace Roslyn.Scripting
{
    /// <summary>
    /// Implements assembly loader for interactive compiler and REPL.
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
    public class AssemblyLoader : IAssemblyLoader
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

        private readonly MetadataShadowCopyProvider shadowCopyProvider;

        // Synchronizes assembly reference tracking.
        // Needs to be thread-safe since assembly loading may be triggered at any time by CLR type loader.
        private object ReferencesLock { get { return loadedAssemblies; } }

        // loaded assembly -> loaded assmbly info (the path might be different from Assembly.Location if the assembly was shadow copied).
        private readonly Dictionary<Assembly, LoadedAssembly> loadedAssemblies;

        // { original full path -> assembly }
        // Note, that there might be multiple entries for a single GAC'd assembly.
        private readonly Dictionary<string, Assembly> loadedAssembliesByPath;

        // simple name -> loaded assemblies
        private readonly Dictionary<string, List<Assembly>> loadedAssembliesBySimpleName;

        // Assemblies registered for lazy load when assembly resolve event is triggered.
        // (We don't want to preload these eagerly using LoadReference API.)
        //
        // simple name -> AssemblyName that includes file path
        private readonly Dictionary<string, List<AssemblyIdentity>> identitiesBySimpleName;

        public AssemblyLoader(MetadataShadowCopyProvider shadowCopyProvider = null)
        {
            this.shadowCopyProvider = shadowCopyProvider;

            Assembly mscorlib = typeof(object).Assembly;
            loadedAssembliesByPath = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase)
            {
                { mscorlib.Location, mscorlib }
            };

            loadedAssemblies = new Dictionary<Assembly, LoadedAssembly>()
            {
                { mscorlib, new LoadedAssembly { LoadedExplicitly = true, OriginalPath = mscorlib.Location } }
            };

            loadedAssembliesBySimpleName = new Dictionary<string, List<Assembly>>(StringComparer.OrdinalIgnoreCase)
            {
                { "mscorlib", new List<Assembly> { mscorlib } }
            };

            identitiesBySimpleName = new Dictionary<string, List<AssemblyIdentity>>();

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolve);
        }

        /// <summary>
        /// Loads assembly with given identity.
        /// </summary>
        /// <param name="identity">The assembly identity.</param>
        /// <returns>Loaded assembly.</returns>
        public Assembly Load(AssemblyIdentity identity)
        {
            if (!string.IsNullOrEmpty(identity.Location))
            {
                RegisterDependency(identity);
            }

            // don't let the CLR load the assembly from the CodeBase, which uses LoadFrom semantics (we want LoadFile):
            return Assembly.Load(identity.ToAssemblyName(setCodeBase: false));
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
                throw new ArgumentNullException("path");
            }

            if (!FileUtilities.IsAbsolute(path))
            {
                throw new ArgumentException("Path must be absolute", "path");
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
        /// <param name="dependency">Assembly identity with location.</param>
        /// <remarks>
        /// Associates a full assembly name with its location. The association is used when an assembly 
        /// is being loaded and its name needs to be resolved to a location.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="dependency"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="dependency"/>.<see cref="AssemblyName.CodeBase"/> is null or empty.</exception>
        public void RegisterDependency(AssemblyIdentity dependency)
        {
            if (dependency == null)
            {
                throw new ArgumentNullException("dependency");
            }

            if (string.IsNullOrEmpty(dependency.Location))
            {
                throw new ArgumentException("dependency");
            }

            lock (ReferencesLock)
            {
                RegisterDependencyNoLock(dependency);
            }
        }

        private AssemblyLoadResult LoadFromPathInternal(string fullPath)
        {
            Assembly assembly;
            lock (ReferencesLock)
            {
                if (loadedAssembliesByPath.TryGetValue(fullPath, out assembly))
                {
                    LoadedAssembly loadedAssembly = loadedAssemblies[assembly];
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
            if (loadedAssembliesBySimpleName.TryGetValue(simpleName, out sameSimpleNameAssemblies))
            {
                sameSimpleNameAssemblies.Add(assembly);
            }
            else
            {
                loadedAssembliesBySimpleName.Add(simpleName, new List<Assembly> { assembly });
            }
        }

        private void RegisterDependencyNoLock(AssemblyIdentity identity)
        {
            List<AssemblyIdentity> sameSimpleNameAssemblyIdentities;
            string simpleName = identity.Name;
            if (identitiesBySimpleName.TryGetValue(simpleName, out sameSimpleNameAssemblyIdentities))
            {
                sameSimpleNameAssemblyIdentities.Add(identity);
            }
            else
            {
                identitiesBySimpleName.Add(simpleName, new List<AssemblyIdentity> { identity });
            }
        }

        private Assembly ShadowCopyAndLoad(string reference)
        {
            MetadataShadowCopy copy = null;
            try
            {
                if (shadowCopyProvider != null)
                {
                    // All modules of the assembly has to be copied at once to keep the metadata consistent.
                    // This API copies the xml doc file and keeps the memory-maps of the metadata.
                    // We don't need that here but presumably the provider is shared with the compilation API that needs both.
                    // Ideally the CLR would expose API to load Assembly given a byte* and not create their own memory-map.
                    copy = shadowCopyProvider.GetMetadataShadowCopy(reference, MetadataImageKind.Assembly);
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
            string fullName = args.Name;
            string simpleName;
            if (!AssemblyIdentity.TryParseSimpleName(fullName, out simpleName))
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
                    if (loadedAssemblies.TryGetValue(args.RequestingAssembly, out loadedAssembly))
                    {
                        originalPath = loadedAssembly.OriginalPath;

                        string pathWithoutExtension = Path.Combine(Path.GetDirectoryName(originalPath), simpleName);
                        originalDllPath = pathWithoutExtension + ".dll";
                        originalExePath = pathWithoutExtension + ".exe";

                        Assembly assembly;
                        if (loadedAssembliesByPath.TryGetValue(originalDllPath, out assembly) || loadedAssembliesByPath.TryGetValue(originalExePath, out assembly))
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
                        return FindHighestVersionOrFirstMatchingIdentity(fullName, new[] { dll, exe });
                    }
                }
            }

            return GetOrLoadKnownAssembly(fullName, simpleName);
        }

        private Assembly GetOrLoadKnownAssembly(string fullName, string simpleName)
        {
            Assembly assembly = null;
            string assemblyFileToLoad = null;

            // Try to find the assembly among assemblies that we loaded, comparing its identity with the requested one.
            lock (ReferencesLock)
            {
                // already loaded assemblies:
                List<Assembly> sameSimpleNameAssemblies;
                if (loadedAssembliesBySimpleName.TryGetValue(simpleName, out sameSimpleNameAssemblies))
                {
                    assembly = FindHighestVersionOrFirstMatchingIdentity(fullName, sameSimpleNameAssemblies);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                }

                // names:
                List<AssemblyIdentity> sameSimpleNameIdentities;
                if (identitiesBySimpleName.TryGetValue(simpleName, out sameSimpleNameIdentities))
                {
                    var identity = FindHighestVersionOrFirstMatchingIdentity(fullName, sameSimpleNameIdentities);
                    if (identity != null)
                    {
                        Debug.Assert(identity.Location != null);
                        assemblyFileToLoad = identity.Location;

                        if (loadedAssembliesByPath.TryGetValue(assemblyFileToLoad, out assembly))
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
                loadedAssembliesByPath[originalPath] = assembly;

                LoadedAssembly loadedAssembly;
                if (loadedAssemblies.TryGetValue(assembly, out loadedAssembly))
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
                    loadedAssemblies.Add(assembly, new LoadedAssembly { LoadedExplicitly = explicitLoad, OriginalPath = result.OriginalPath });
                }

                RegisterLoadedAssemblySimpleNameNoLock(assembly);
            }

            if (shadowCopyProvider != null && assembly.GlobalAssemblyCache)
            {
                shadowCopyProvider.SuppressShadowCopy(originalPath);
            }

            return assembly;
        }

        // TODO (tomat): use AssemblyIdentity.GetbestMatch
        private static Assembly FindHighestVersionOrFirstMatchingIdentity(string name, IEnumerable<Assembly> assemblies)
        {
            // Name can be partial or full. In both cases multiple assemblies might match.
            // If the name is partial and doesn't specify version we pick the highest available version.

            Assembly candidate = null;
            foreach (var assembly in assemblies)
            {
                if (AssemblyIdentity.ReferenceMatchesDefinition(name, assembly.FullName))
                {
                    if (candidate == null || candidate.GetName().Version < assembly.GetName().Version)
                    {
                        candidate = assembly;
                    }
                }
            }

            return candidate;
        }

        // TODO (tomat): use AssemblyIdentity.GetBestMatch
        private static AssemblyIdentity FindHighestVersionOrFirstMatchingIdentity(string name, IEnumerable<AssemblyIdentity> identities)
        {
            // Name can be partial or full. In both cases multiple assemblies might match.
            // If the name is partial and doesn't specify version we pick the highest available version.

            AssemblyIdentity candidateIdentity = null;
            foreach (var identity in identities)
            {
                if (AssemblyIdentity.ReferenceMatchesDefinition(name, identity))
                {
                    if (candidateIdentity == null || candidateIdentity.Version < identity.Version)
                    {
                        candidateIdentity = identity;
                    }
                }
            }

            return candidateIdentity;
        }
    }
}
