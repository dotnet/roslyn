// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Reflection;

#if NET
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

        private readonly ConcurrentDictionary<Guid, Task<string>> _mvidPathMap = new ConcurrentDictionary<Guid, Task<string>>();
        private readonly ConcurrentDictionary<(Guid, string), Task<string>> _mvidSatelliteAssemblyPathMap = new ConcurrentDictionary<(Guid, string), Task<string>>();

        internal string BaseDirectory => _baseDirectory;

        internal int CopyCount => _mvidPathMap.Count;

#if NET
        public ShadowCopyAnalyzerAssemblyLoader(string baseDirectory, ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers = default, ImmutableArray<IAnalyzerAssemblyRedirector> externalRedirectors = default)
            : this(null, baseDirectory, externalResolvers, externalRedirectors)
        {
        }

        public ShadowCopyAnalyzerAssemblyLoader(AssemblyLoadContext? compilerLoadContext, string baseDirectory, ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers = default, ImmutableArray<IAnalyzerAssemblyRedirector> externalRedirectors = default)
            : base(compilerLoadContext, AnalyzerLoadOption.LoadFromDisk, externalResolvers, externalRedirectors)
#else
        public ShadowCopyAnalyzerAssemblyLoader(string baseDirectory, ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers = default, ImmutableArray<IAnalyzerAssemblyRedirector> externalRedirectors = default)
            : base(externalResolvers, externalRedirectors)
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

        protected override string PreparePathToLoad(string originalAnalyzerPath)
        {
            var mvid = AssemblyUtilities.ReadMvid(originalAnalyzerPath);

            return PrepareLoad(_mvidPathMap, mvid, copyAnalyzerContents);

            string copyAnalyzerContents()
            {
                var analyzerFileName = Path.GetFileName(originalAnalyzerPath);
                var shadowDirectory = Path.Combine(_shadowCopyDirectoryAndMutex.Value.directory, mvid.ToString());
                var shadowAnalyzerPath = Path.Combine(shadowDirectory, analyzerFileName);
                CopyFile(originalAnalyzerPath, shadowAnalyzerPath);

                return shadowAnalyzerPath;
            }
        }

        protected override string PrepareSatelliteAssemblyToLoad(string originalAnalyzerPath, string cultureName)
        {
            var mvid = AssemblyUtilities.ReadMvid(originalAnalyzerPath);

            return PrepareLoad(_mvidSatelliteAssemblyPathMap, (mvid, cultureName), copyAnalyzerContents);

            string copyAnalyzerContents()
            {
                var analyzerFileName = Path.GetFileName(originalAnalyzerPath);
                var shadowDirectory = Path.Combine(_shadowCopyDirectoryAndMutex.Value.directory, mvid.ToString());
                var shadowAnalyzerPath = Path.Combine(shadowDirectory, analyzerFileName);

                var originalDirectory = Path.GetDirectoryName(originalAnalyzerPath)!;
                var satelliteFileName = GetSatelliteFileName(analyzerFileName);

                var originalSatellitePath = Path.Combine(originalDirectory, cultureName, satelliteFileName);
                var shadowSatellitePath = Path.Combine(shadowDirectory, cultureName, satelliteFileName);
                CopyFile(originalSatellitePath, shadowSatellitePath);

                return shadowSatellitePath;
            }
        }

        private static string PrepareLoad<TKey>(ConcurrentDictionary<TKey, Task<string>> mvidPathMap, TKey mvidKey, Func<string> copyContents)
            where TKey : notnull
        {
            if (mvidPathMap.TryGetValue(mvidKey, out Task<string>? copyTask))
            {
                return copyTask.Result;
            }

            var tcs = new TaskCompletionSource<string>();
            var task = mvidPathMap.GetOrAdd(mvidKey, tcs.Task);
            if (object.ReferenceEquals(task, tcs.Task))
            {
                // This thread won and we need to do the copy.
                try
                {
                    var shadowAnalyzerPath = copyContents();
                    tcs.SetResult(shadowAnalyzerPath);
                    return shadowAnalyzerPath;
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    throw;
                }
            }
            else
            {
                // This thread lost and we need to wait for the winner to finish the copy.
                return task.Result;
            }
        }

        private static void CopyFile(string originalPath, string shadowCopyPath)
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
