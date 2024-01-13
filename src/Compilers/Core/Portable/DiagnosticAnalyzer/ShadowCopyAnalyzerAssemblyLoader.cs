// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;

#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis
{
    internal sealed class ShadowCopyAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        /// <summary>
        /// The base directory for shadow copies. Each instance of
        /// <see cref="ShadowCopyAnalyzerAssemblyLoader"/> gets its own
        /// subdirectory under this directory. This is also the starting point
        /// for scavenge operations.
        /// </summary>
        private readonly string _baseDirectory;

        internal readonly Task DeleteLeftoverDirectoriesTask;

        /// <summary>
        /// The directory where this instance of <see cref="ShadowCopyAnalyzerAssemblyLoader"/>
        /// will shadow-copy assemblies, and the mutex created to mark that the owner of it is still active.
        /// </summary>
        private readonly Lazy<(string directory, Mutex)> _shadowCopyDirectoryAndMutex;

        /// <summary>
        /// Used to generate unique names for per-assembly directories. Should be updated with <see cref="Interlocked.Increment(ref int)"/>.
        /// </summary>
        private int _assemblyDirectoryId;

        internal string BaseDirectory => _baseDirectory;

        internal int CopyCount => _assemblyDirectoryId;

#if NETCOREAPP
        public ShadowCopyAnalyzerAssemblyLoader(string baseDirectory)
            : this(null, baseDirectory)
        {
        }

        public ShadowCopyAnalyzerAssemblyLoader(AssemblyLoadContext? compilerLoadContext, string baseDirectory)
            : base(compilerLoadContext, AnalyzerLoadOption.LoadFromDisk)
#else
        public ShadowCopyAnalyzerAssemblyLoader(string baseDirectory)
#endif
        {
            if (baseDirectory is null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            _baseDirectory = baseDirectory;
            _shadowCopyDirectoryAndMutex = new Lazy<(string directory, Mutex)>(
                () => CreateUniqueDirectoryForProcess(), LazyThreadSafetyMode.ExecutionAndPublication);

            DeleteLeftoverDirectoriesTask = Task.Run(DeleteLeftoverDirectories);
        }

        private void DeleteLeftoverDirectories()
        {
            // Avoid first chance exception
            if (!Directory.Exists(_baseDirectory))
                return;

            IEnumerable<string> subDirectories;
            try
            {
                subDirectories = Directory.EnumerateDirectories(_baseDirectory);
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            foreach (var subDirectory in subDirectories)
            {
                string name = Path.GetFileName(subDirectory).ToLowerInvariant();
                Mutex? mutex = null;
                try
                {
                    // We only want to try deleting the directory if no-one else is currently
                    // using it. That is, if there is no corresponding mutex.
                    if (!Mutex.TryOpenExisting(name, out mutex))
                    {
                        try
                        {
                            // Avoid calling ClearReadOnlyFlagOnFiles before calling Directory.Delete. In general, files
                            // created by the shadow copy should not be marked read-only (CopyFile also clears the
                            // read-only flag), and clearing the read-only flag for the entire directory requires
                            // significant disk access.
                            //
                            // If the deletion fails for an IOException, it may have been the result of a file being
                            // marked read-only. We catch that exception and perform an explicit clear before trying
                            // again.
                            Directory.Delete(subDirectory, recursive: true);
                        }
                        catch (IOException)
                        {
                            // Retry after clearing the read-only flag
                            ClearReadOnlyFlagOnFiles(subDirectory);
                            Directory.Delete(subDirectory, recursive: true);
                        }
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

        protected override string PreparePathToLoad(string originalAnalyzerPath, ImmutableHashSet<string> cultureNames)
        {
            var analyzerFileName = Path.GetFileName(originalAnalyzerPath);
            var shadowDirectory = CreateUniqueDirectoryForAssembly();
            var shadowAnalyzerPath = Path.Combine(shadowDirectory, analyzerFileName);
            copyFile(originalAnalyzerPath, shadowAnalyzerPath);

            if (cultureNames.IsEmpty)
            {
                return shadowAnalyzerPath;
            }

            var originalDirectory = Path.GetDirectoryName(originalAnalyzerPath)!;
            var satelliteFileName = GetSatelliteFileName(analyzerFileName);
            foreach (var cultureName in cultureNames)
            {
                var originalSatellitePath = Path.Combine(originalDirectory, cultureName, satelliteFileName);
                var shadowSatellitePath = Path.Combine(shadowDirectory, cultureName, satelliteFileName);
                copyFile(originalSatellitePath, shadowSatellitePath);
            }

            return shadowAnalyzerPath;

            static void copyFile(string originalPath, string shadowCopyPath)
            {
                var directory = Path.GetDirectoryName(shadowCopyPath);
                if (directory is null)
                {
                    throw new ArgumentException($"Shadow copy path '{shadowCopyPath}' must not be the root directory");
                }

                _ = Directory.CreateDirectory(directory);
                File.Copy(originalPath, shadowCopyPath);
                ClearReadOnlyFlagOnFile(new FileInfo(shadowCopyPath));
            }
        }

        private static void ClearReadOnlyFlagOnFiles(string directoryPath)
        {
            DirectoryInfo directory = new DirectoryInfo(directoryPath);

            foreach (var file in directory.EnumerateFiles(searchPattern: "*", searchOption: SearchOption.AllDirectories))
            {
                ClearReadOnlyFlagOnFile(file);
            }
        }

        private static void ClearReadOnlyFlagOnFile(FileInfo fileInfo)
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
            int directoryId = Interlocked.Increment(ref _assemblyDirectoryId);

            string directory = Path.Combine(_shadowCopyDirectoryAndMutex.Value.directory, directoryId.ToString());

            Directory.CreateDirectory(directory);
            return directory;
        }

        private (string directory, Mutex mutex) CreateUniqueDirectoryForProcess()
        {
            string guid = Guid.NewGuid().ToString("N").ToLowerInvariant();
            string directory = Path.Combine(_baseDirectory, guid);

            var mutex = new Mutex(initiallyOwned: false, name: guid);

            Directory.CreateDirectory(directory);

            return (directory, mutex);
        }
    }
}
