// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class ShadowCopyAnalyzerPathResolver : IAnalyzerPathResolver
    {
        private enum DirectoryCleanupState
        {
            InProgress,
            Completed
        }

        private static readonly ConcurrentDictionary<string, DirectoryCleanupState> s_directoryCleanupStates = new(AnalyzerAssemblyLoader.OriginalPathComparer);

        /// <summary>
        /// The base directory for shadow copies. Each instance of
        /// <see cref="ShadowCopyAnalyzerPathResolver"/> gets its own
        /// subdirectory under this directory. This is also the starting point
        /// for scavenge operations.
        /// </summary>
        internal string BaseDirectory { get; }

        internal string ShadowDirectory { get; }

        internal string CacheDirectory { get; }

        /// <summary>
        /// As long as this mutex is alive, other instances of this type will not try to clean
        /// up the shadow directory.
        /// </summary>
        private Mutex Mutex { get; }

        internal Task DeleteLeftoverDirectoriesTask { get; }

        /// <summary>
        /// This is a counter that is incremented each time a new shadow sub directory is created to ensure they 
        /// have unique names.
        /// </summary>
        private int _directoryCount;

        /// <summary>
        /// This is a map from the original directory name to the numbered directory name it 
        /// occupies in the shadow directory.
        /// </summary>
        private ConcurrentDictionary<string, int> OriginalDirectoryMap { get; } = new(AnalyzerAssemblyLoader.OriginalPathComparer);

        /// <summary>
        /// This interface can be called from multiple threads for the same original assembly path. This
        /// is a map between the original path and the Task that completes when the shadow copy for that
        /// original path completes.
        /// </summary>
        private ConcurrentDictionary<string, Task<string>> CopyMap { get; } = new(AnalyzerAssemblyLoader.OriginalPathComparer);

        /// <summary>
        /// This is the number of shadow copies that have occurred in this instance.
        /// </summary>
        /// <remarks>
        /// This is used for testing, it should not be used for any other purpose.
        /// </remarks>
        internal int CopyCount => CopyMap.Count;

        public ShadowCopyAnalyzerPathResolver(string baseDirectory)
        {
            if (baseDirectory is null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            // The shadow copy analyzer should only be created on Windows. To create on Linux we cannot use 
            // GetTempPath as it's not per-user. Generally there is no need as LoadFromStream achieves the same
            // effect
            if (!Path.IsPathRooted(baseDirectory))
            {
                throw new ArgumentException($"Must be a full path: {baseDirectory}", nameof(baseDirectory));
            }

            BaseDirectory = baseDirectory;
            var shadowDirectoryName = Guid.NewGuid().ToString("N").ToLowerInvariant();

            // The directory is deliberately _not_ created at this point. It will only be created when the first
            // request comes in. This avoids creating unnecessary directories when no analyzers are loaded 
            // via the shadow layer.
            ShadowDirectory = Path.Combine(BaseDirectory, shadowDirectoryName);
            CacheDirectory = Path.Combine(BaseDirectory, "cache");
            Mutex = new Mutex(initiallyOwned: false, name: shadowDirectoryName);
            DeleteLeftoverDirectoriesTask = Task.Run(DeleteLeftoverDirectories);
        }

        private void DeleteLeftoverDirectories()
        {
            if (!s_directoryCleanupStates.TryAdd(BaseDirectory, DirectoryCleanupState.InProgress))
            {
                // Someone else is already cleaning up this directory. Wait until it's completed
                SpinWait.SpinUntil(() => s_directoryCleanupStates[BaseDirectory] == DirectoryCleanupState.Completed, millisecondsTimeout: -1);
                return;
            }

            try
            {
                // Avoid first chance exception
                if (!Directory.Exists(BaseDirectory))
                    return;

                IEnumerable<string> subDirectories;
                try
                {
                    subDirectories = Directory.EnumerateDirectories(BaseDirectory);
                }
                catch (DirectoryNotFoundException)
                {
                    return;
                }

                foreach (var subDirectory in subDirectories)
                {
                    if (subDirectory == CacheDirectory)
                        continue;

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
                                //
                                // It's possible for us to race with multiple shadow copy instances trying to delete the
                                // same directory. If that happens, we may get a DirectoryNotFoundException. We
                                // explicitly ignore that exception as it means our work is already done.
                                // This isn't a perfect check, as two processes could race here, but it's close enough.
                                if (Directory.Exists(subDirectory))
                                {
                                    try
                                    {
                                        Directory.Delete(subDirectory, recursive: true);
                                    }
                                    catch (DirectoryNotFoundException)
                                    {
                                        // Another process beat us to it. Nothing to do.
                                    }
                                }
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
                        mutex?.Dispose();
                    }
                }

                pruneCacheIfNeeded();
            }
            finally
            {
                s_directoryCleanupStates[BaseDirectory] = DirectoryCleanupState.Completed;
            }

            void pruneCacheIfNeeded()
            {
                if (!PlatformInformation.IsWindows)
                    return;

                using var cacheMutex = new Mutex(initiallyOwned: false, name: $"RoslynShadowCopyCache-{HashToHex(CacheDirectory)}");
                bool lockTaken = false;
                try
                {
                    try
                    {
                        lockTaken = cacheMutex.WaitOne(millisecondsTimeout: 0);
                    }
                    catch (AbandonedMutexException)
                    {
                        lockTaken = true;
                    }

                    if (lockTaken)
                    {
                        // Permit up to 200 unlinked files (not hard-linked to a specific shadow loader directory).
                        // Delete the oldest files which exceed this limit.
                        const int maxUnlinkedCount = 200;
                        var filesToEvict = Directory.EnumerateFiles(CacheDirectory)
                            .Where(file =>
                            {
                                Debug.Assert(PlatformInformation.IsWindows);
                                return FileUtilities.CountHardLinks(file) == 1;
                            })
                            .OrderByDescending(File.GetLastWriteTimeUtc)
                            .Skip(maxUnlinkedCount);

                        foreach (var file in filesToEvict)
                        {
                            File.Delete(file);
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
                    if (lockTaken)
                        cacheMutex.ReleaseMutex();
                }
            }
        }

        public bool IsAnalyzerPathHandled(string analyzerFilePath) => true;

        public string GetResolvedAnalyzerPath(string originalAnalyzerPath)
        {
            var analyzerShadowDir = GetAnalyzerShadowDirectory(originalAnalyzerPath);
            var analyzerShadowPath = Path.Combine(analyzerShadowDir, Path.GetFileName(originalAnalyzerPath));
            ShadowCopyFile(originalAnalyzerPath, analyzerShadowPath);
            return analyzerShadowPath;
        }

        public string? GetResolvedSatellitePath(string originalAnalyzerPath, CultureInfo cultureInfo)
        {
            var satelliteFilePath = AnalyzerAssemblyLoader.GetSatelliteAssemblyPath(originalAnalyzerPath, cultureInfo);
            if (satelliteFilePath is null)
            {
                return null;
            }

            var analyzerShadowDir = GetAnalyzerShadowDirectory(originalAnalyzerPath);
            var satelliteFileName = Path.GetFileName(satelliteFilePath);
            var satelliteDirectoryName = Path.GetFileName(Path.GetDirectoryName(satelliteFilePath));
            var shadowSatellitePath = Path.Combine(analyzerShadowDir, satelliteDirectoryName!, satelliteFileName);
            ShadowCopyFile(satelliteFilePath, shadowSatellitePath);
            return shadowSatellitePath;
        }

        /// <summary>
        /// Get the shadow directory for the given original analyzer file path.
        /// </summary>
        private string GetAnalyzerShadowDirectory(string analyzerFilePath)
        {
            var originalDirName = Path.GetDirectoryName(analyzerFilePath)!;
            var shadowDirName = OriginalDirectoryMap.GetOrAdd(originalDirName, _ => Interlocked.Increment(ref _directoryCount)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Path.Combine(ShadowDirectory, shadowDirName);
        }

        /// <summary>
        /// This type has to account for multiple threads calling into the various resolver APIs. To avoid two threads
        /// writing at the same time this method is used to ensure only one thread _wins_ and both can wait for 
        /// that thread to complete the copy.
        /// </summary>
        private void ShadowCopyFile(string originalFilePath, string shadowCopyPath)
        {
            if (CopyMap.TryGetValue(originalFilePath, out var copyTask))
            {
                copyTask.Wait();
                return;
            }

            var tcs = new TaskCompletionSource<string>();
            var task = CopyMap.GetOrAdd(originalFilePath, tcs.Task);
            if (object.ReferenceEquals(task, tcs.Task))
            {
                // This thread won and we need to do the copy.
                try
                {
                    copyFile(originalFilePath, shadowCopyPath);
                    tcs.SetResult(shadowCopyPath);
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
                task.Wait();
                Debug.Assert(AnalyzerAssemblyLoader.GeneratedPathComparer.Equals(shadowCopyPath, task.Result));
            }

            void copyFile(string originalPath, string shadowCopyPath)
            {
                var directory = Path.GetDirectoryName(shadowCopyPath);
                if (directory is null)
                {
                    throw new ArgumentException($"Shadow copy path '{shadowCopyPath}' must not be the root directory");
                }

                _ = Directory.CreateDirectory(directory);

                // The shadow copy should only copy files that exist. For files that don't exist, this best
                // emulates not having the shadow copy layer
                if (File.Exists(originalPath))
                {
                    linkFromCacheOrFallbackToCopy(originalFilePath, shadowCopyPath);
                    ClearReadOnlyFlagOnFile(new FileInfo(shadowCopyPath));
                }
            }

            // Optimization for antivirus scanning on Windows:
            // - Shadow copied files are hard-linked to/from a cache directory if possible.
            // - We continue to use per-session 'ShadowDirectory' for ease of implementing correct loading semantics and cleanup.
            // - Hard linking a file from the cache instead of copying it is empirically observed to reduce time spent running AV scans when loading assemblies.
            void linkFromCacheOrFallbackToCopy(string originalPath, string shadowCopyPath)
            {
                if (!PlatformInformation.IsWindows)
                {
                    File.Copy(originalPath, shadowCopyPath);
                    return;
                }

                var cachePath = TryGetCacheKey(originalPath) is { } cacheKey
                    ? Path.Combine(CacheDirectory, cacheKey)
                    : null;
                if (File.Exists(cachePath))
                {
                    // File is already present in cache. If it matches original, then hard-link from cache to shadow copy path. Failing that just copy from the original path.
                    if (!cacheEntryMatches(originalPath, cachePath))
                    {
                        try { File.Delete(cachePath); } catch { }
                        File.Copy(originalPath, shadowCopyPath);
                    }
                    else if (!FileUtilities.TryCreateHardLink(cachePath, shadowCopyPath))
                    {
                        File.Copy(originalPath, shadowCopyPath);
                    }
                }
                else
                {
                    // File not in cache. Copy it to the shadow copy path, then try to hard link it to the cache.
                    // If the hard linking fails for some reason, that isn't a functional problem.
                    // It usually means we lost a race to cache the same file, or that the current volume doesn't support hard links (e.g. Dev Drive).
                    File.Copy(originalPath, shadowCopyPath);
                    if (cachePath is not null)
                    {
                        Directory.CreateDirectory(CacheDirectory);
                        FileUtilities.TryCreateHardLink(shadowCopyPath, cachePath);
                    }
                }
            }

            bool cacheEntryMatches(string originalPath, string cachePath)
            {
                var originalInfo = new FileInfo(originalPath);
                var cacheInfo = new FileInfo(cachePath);
                try
                {
                    if (originalInfo.Length != cacheInfo.Length)
                        return false;

                    return AssemblyUtilities.ReadMvid(originalPath) == AssemblyUtilities.ReadMvid(cachePath);
                }
                catch
                {
                    return false;
                }
            }
        }

        private static string HashToHex(string value)
        {
            Span<byte> hash = stackalloc byte[16];
            int bytesWritten = XxHash128.Hash(MemoryMarshal.AsBytes(value.AsSpan()), hash);
            Debug.Assert(bytesWritten == hash.Length);

            return hashToHex(hash);

            // See also 'PrivateImplementationDetails.HashToHex'
            static string hashToHex(ReadOnlySpan<byte> hash)
            {
#if NET10_0_OR_GREATER
                return string.Create(hash.Length * 2, hash, (destination, hash) => toHex(hash, destination));
#else
                char[] c = new char[hash.Length * 2];
                toHex(hash, c);
                return new string(c);
#endif

                static void toHex(ReadOnlySpan<byte> source, Span<char> destination)
                {
                    int i = 0;
                    foreach (var b in source)
                    {
                        destination[i++] = hexchar(b >> 4);
                        destination[i++] = hexchar(b & 0xF);
                    }
                }

                static char hexchar(int x) => (char)((x <= 9) ? (x + '0') : (x + ('a' - 10)));
            }
        }

        private static string? TryGetCacheKey(string originalPath)
        {
            // Key format: (original filename) + (file path hash) + (mvid) + (original file length)
            var hexHash = HashToHex(originalPath);
            try
            {
                var mvid = AssemblyUtilities.ReadMvid(originalPath);
                var length = new FileInfo(originalPath).Length;
                return $"{Path.GetFileNameWithoutExtension(originalPath)}-{hexHash}-{mvid:N}-{length}-{Path.GetExtension(originalPath)}";
            }
            catch
            {
                return null;
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
    }
}
