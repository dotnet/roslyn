// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    public sealed class ShadowCopyAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly List<string> _dependencyPaths = new List<string>();
        private readonly Dictionary<string, Assembly> _pathsToAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Assembly> _namesToAssemblies = new Dictionary<string, Assembly>();
        private readonly object _guard = new object();

        /// <summary>
        /// The base directory for shadow copies. Each instance of
        /// <see cref="ShadowCopyAnalyzerAssemblyLoader"/> gets its own
        /// subdirectory under this directory. This is also the starting point
        /// for scavenge operations.
        /// </summary>
        private readonly string _baseDirectory;

        /// <summary>
        /// The directory where this instance of <see cref="ShadowCopyAnalyzerAssemblyLoader"/>
        /// will shadow-copy assemblies.
        /// </summary>
        private string _shadowCopyDirectory;
        private Mutex _shadowCopyDirectoryMutex;

        /// <summary>
        /// Used to generate unique names for per-assembly directories.
        /// </summary>
        private int _assemblyDirectoryId = 0;

        private bool _hookedAssemblyResolve = false;

        public ShadowCopyAnalyzerAssemblyLoader(string baseDirectory = null)
        {
            if (baseDirectory != null)
            {
                _baseDirectory = baseDirectory;
            }
            else
            {
                _baseDirectory = Path.Combine(Path.GetTempPath(), "CodeAnalysis", "AnalyzerShadowCopies");
            }

            Task.Run((Action)DeleteLeftoverDirectories);
        }

        private void DeleteLeftoverDirectories()
        {
            foreach (var subDirectory in Directory.EnumerateDirectories(_baseDirectory))
            {
                string name = Path.GetFileName(subDirectory).ToLowerInvariant();
                Mutex mutex = null;
                try
                {
                    // We only want to try deleting the directory if no one else is currently
                    // using it. That is, if there is no corresponding mutex.
                    if (!Mutex.TryOpenExisting(name, out mutex))
                    {
                        ClearReadOnlyFlagOnFiles(subDirectory);
                        Directory.Delete(subDirectory, recursive: true);
                    }
                }
                catch
                {
                    // If something goes wrong we will leave it to the next run to clean up.
                    // Just swallow the exception and move on.
                }
                finally
                {
                    if (mutex != null)
                    {
                        mutex.Dispose();
                    }
                }
            }
        }

        public void AddDependencyLocation(string fullPath)
        {
            lock (_guard)
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
        }

        public Assembly LoadFromPath(string fullPath)
        {
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
            if (_shadowCopyDirectory == null)
            {
                _shadowCopyDirectory = CreateUniqueDirectoryForProcess();
            }

            string assemblyDirectory = CreateUniqueDirectoryForAssembly();
            string shadowCopyPath = CopyFileAndResources(fullPath, assemblyDirectory);

            Assembly assembly = Assembly.LoadFrom(shadowCopyPath);
            string assemblyName = assembly.FullName;

            _pathsToAssemblies[fullPath] = assembly;
            _namesToAssemblies[assemblyName] = assembly;

            return assembly;
        }

        private string CopyFileAndResources(string fullPath, string assemblyDirectory)
        {
            string fileNameWithExtension = Path.GetFileName(fullPath);
            string shadowCopyPath = Path.Combine(assemblyDirectory, fileNameWithExtension);

            CopyFile(fullPath, shadowCopyPath);

            string originalDirectory = Path.GetDirectoryName(fullPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);
            string resourcesNameWithoutExtension = fileNameWithoutExtension + ".resources";
            string resourcesNameWithExtension = resourcesNameWithoutExtension + ".dll";

            foreach (var directory in Directory.EnumerateDirectories(originalDirectory))
            {
                string directoryName = Path.GetFileName(directory);

                string resourcesPath = Path.Combine(directory, resourcesNameWithExtension);
                if (File.Exists(resourcesPath))
                {
                    string resourcesShadowCopyPath = Path.Combine(assemblyDirectory, directoryName, resourcesNameWithExtension);
                    CopyFile(resourcesPath, resourcesShadowCopyPath);
                }

                resourcesPath = Path.Combine(directory, resourcesNameWithoutExtension, resourcesNameWithExtension);
                if (File.Exists(resourcesPath))
                {
                    string resourcesShadowCopyPath = Path.Combine(assemblyDirectory, directoryName, resourcesNameWithoutExtension, resourcesNameWithExtension);
                    CopyFile(resourcesPath, resourcesShadowCopyPath);
                }
            }

            return shadowCopyPath;
        }

        private void CopyFile(string originalPath, string shadowCopyPath)
        {
            var directory = Path.GetDirectoryName(shadowCopyPath);
            Directory.CreateDirectory(directory);

            File.Copy(originalPath, shadowCopyPath);

            ClearReadOnlyFlagOnFile(new FileInfo(shadowCopyPath));
        }

        private void ClearReadOnlyFlagOnFiles(string directoryPath)
        {
            DirectoryInfo directory = new DirectoryInfo(directoryPath);

            foreach (var file in directory.EnumerateFiles(searchPattern: "*", searchOption: SearchOption.AllDirectories))
            {
                ClearReadOnlyFlagOnFile(file);
            }
        }

        private void ClearReadOnlyFlagOnFile(FileInfo fileInfo)
        {
            try
            {
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                }
            }
            catch
            {
                // There are many reasons this could fail. Ignore it and keep going.
            }
        }

        private string CreateUniqueDirectoryForAssembly()
        {
            int directoryId = _assemblyDirectoryId++;

            string directory = Path.Combine(_shadowCopyDirectory, directoryId.ToString());

            Directory.CreateDirectory(directory);
            return directory;
        }

        private string CreateUniqueDirectoryForProcess()
        {
            string guid = Guid.NewGuid().ToString("N").ToLowerInvariant();
            string directory = Path.Combine(_baseDirectory, guid);

            _shadowCopyDirectoryMutex = new Mutex(initiallyOwned: false, name: guid);

            Directory.CreateDirectory(directory);

            return directory;
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
