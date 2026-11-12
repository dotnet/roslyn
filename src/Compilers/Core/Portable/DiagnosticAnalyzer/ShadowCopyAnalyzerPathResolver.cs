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
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class ShadowCopyAnalyzerPathResolver : IAnalyzerPathResolver
    {
        private const string DirectoryVersion = "v1";

        private enum DirectoryCleanupState
        {
            InProgress,
            Completed
        }

        private static readonly ConcurrentDictionary<string, DirectoryCleanupState> s_directoryCleanupStates = new(AnalyzerAssemblyLoader.OriginalPathComparer);

        /// <summary>
        /// The base directory for shadow copies.
        /// Layout:
        ///   baseDirectory/
        ///   └── v1/
        ///       ├── shadow/
        ///       │   └── (per-session directories)
        ///       └── cache/
        ///           └── (cached dlls)
        /// </summary>
        internal string BaseDirectory { get; }

        /// <summary>Directory denoted by '$BaseDirectory/$DirectoryVersion/shadow/'.</summary>
        internal string ShadowDirectory { get; }

        /// <summary>Directory for the current session. Directory denoted by '$BaseDirectory/$DirectoryVersion/shadow/$sessionId/'.</summary>
        internal string SessionDirectory { get; }

        /// <summary>
        /// Shared cache for shadow copied assemblies.
        /// Used to amortize cost of antivirus scans.
        /// Denoted by '$BaseDirectory/$DirectoryVersion/cache/'.
        /// </summary>
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

        /// <summary>Whether to hard link assemblies to/from <see cref="CacheDirectory"/>.</summary>
        /// <remarks>Internal for testing only.</remarks>
        internal bool EnableHardLinks = true;

        /// <summary>
        /// This is the number of shadow copies that have occurred in this instance.
        /// </summary>
        /// <remarks>
        /// This is used for testing, it should not be used for any other purpose.
        /// </remarks>
        internal int CopyCount => CopyMap.Count;

#if NET
        [SupportedOSPlatform("windows")]
#endif
        public ShadowCopyAnalyzerPathResolver(string baseDirectory)
        {
            if (!PlatformInformation.IsWindows)
            {
                throw new InvalidOperationException("ShadowCopyAnalyzerPathResolver is only supported on Windows.");
            }

            if (baseDirectory is null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            if (!Path.IsPathRooted(baseDirectory))
            {
                throw new ArgumentException($"Must be a full path: {baseDirectory}", nameof(baseDirectory));
            }

            // The directories are deliberately _not_ created at this point. They will only be created when the first
            // request comes in. This avoids creating unnecessary directories when no analyzers are loaded 
            // via the shadow layer.
            BaseDirectory = baseDirectory;

            var versionDirectory = Path.Combine(baseDirectory, DirectoryVersion);
            ShadowDirectory = Path.Combine(versionDirectory, "shadow");
            CacheDirectory = Path.Combine(versionDirectory, "cache");

            var sessionDirectoryName = Guid.NewGuid().ToString("N").ToLowerInvariant();
            SessionDirectory = Path.Combine(ShadowDirectory, sessionDirectoryName);
            Mutex = new Mutex(initiallyOwned: false, name: sessionDirectoryName);

            DeleteLeftoverDirectoriesTask = Task.Run(DeleteLeftoverDirectories);
        }

        internal static Task CleanLegacyShadowDirectoryAsync(string legacyShadowDirectory)
        {
            if (Directory.Exists(legacyShadowDirectory))
            {
                return Task.Run(() =>
                {
                    DeleteLeftoverDirectories(baseDirectory: legacyShadowDirectory, shadowDirectory: legacyShadowDirectory, cacheDirectory: null);
                    // Delete the legacyShadowDirectory if empty, otherwise do nothing.
                    try { Directory.Delete(legacyShadowDirectory, recursive: false); }
                    catch { }
                });
            }

            return Task.CompletedTask;
        }

        private void DeleteLeftoverDirectories()
        {
            DeleteLeftoverDirectories(BaseDirectory, ShadowDirectory, CacheDirectory);
        }

        private static void DeleteLeftoverDirectories(string baseDirectory, string shadowDirectory, string? cacheDirectory)
        {
            if (!s_directoryCleanupStates.TryAdd(baseDirectory, DirectoryCleanupState.InProgress))
            {
                // Someone else is already cleaning up this directory. Wait until it's completed
                SpinWait.SpinUntil(() => s_directoryCleanupStates[baseDirectory] == DirectoryCleanupState.Completed, millisecondsTimeout: -1);
                return;
            }

            try
            {
                // Avoid first chance exception
                if (!Directory.Exists(shadowDirectory))
                    return;

                IEnumerable<string> subDirectories;
                try
                {
                    subDirectories = Directory.EnumerateDirectories(shadowDirectory);
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
                s_directoryCleanupStates[baseDirectory] = DirectoryCleanupState.Completed;
            }

            void pruneCacheIfNeeded()
            {
                // Avoid first chance exception
                if (!Directory.Exists(cacheDirectory))
                    return;

                using var cacheMutex = new Mutex(initiallyOwned: false, name: $"RoslynShadowCopyCache-{HashToHex(cacheDirectory)}");
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
                        // Note: this value was chosen arbitrarily, based on speculation that ordinary solutions use perhaps a few dozen analyzer assemblies,
                        // and that a user would likely be working on several different solutions/worktrees regularly.
                        // The value can and should be adjusted in future based on empirical measurements.
                        const int maxUnlinkedCount = 200;
                        var filesToEvict = Directory.EnumerateFiles(cacheDirectory)
                            .Select(static file => (file, fileInformationOpt: TryGetWindowsFileInformation(file)))
                            .Where(static pair => pair.fileInformationOpt is { NumberOfLinks: 1 })
                            .OrderByDescending(static pair =>
                            {
                                var creationTime = pair.fileInformationOpt!.Value.CreationTime;
                                return (long)creationTime.dwHighDateTime << 32 | (uint)creationTime.dwLowDateTime;
                            })
                            .Skip(maxUnlinkedCount);

                        // Note: it's expected that 'pruneCacheIfNeeded()' and 'linkFromCacheOrFallbackToCopy()' can run concurrently.
                        // If pruning deletes a file that linking was attempting to use, linking is expected to fall back gracefully to copying.
                        foreach (var pair in filesToEvict)
                        {
                            File.Delete(pair.file);
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
            return Path.Combine(SessionDirectory, shadowDirName);
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
                    copyFile(this, originalFilePath, shadowCopyPath);
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

            static void copyFile(ShadowCopyAnalyzerPathResolver @this, string originalPath, string shadowCopyPath)
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
                    linkFromCacheOrFallbackToCopy(@this, originalPath, shadowCopyPath);
                    ClearReadOnlyFlagOnFile(new FileInfo(shadowCopyPath));
                }
            }

            // Optimization for antivirus scanning on Windows:
            // - Shadow copied files are hard-linked to/from a cache directory if possible.
            // - We continue to load from 'SessionDirectory' for ease of implementing correct loading semantics and cleanup.
            // - Hard linking a file from the cache instead of copying it is empirically observed to reduce time spent running AV scans when loading assemblies.
            static void linkFromCacheOrFallbackToCopy(ShadowCopyAnalyzerPathResolver @this, string originalPath, string shadowCopyPath)
            {
                var cachePath = TryGetCacheKey(originalPath) is { } cacheKey
                    ? Path.Combine(@this.CacheDirectory, cacheKey)
                    : null;
                if (File.Exists(cachePath))
                {
                    // File is already present in cache. If it matches original, then hard-link from cache to shadow copy path. Failing that just copy from the original path.
                    if (!cacheEntryMatches(originalPath, cachePath))
                    {
                        try { File.Delete(cachePath); } catch { }
                        File.Copy(originalPath, shadowCopyPath);
                    }
                    else if (!@this.TryCreateHardLink(cachePath, shadowCopyPath))
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
                        Directory.CreateDirectory(@this.CacheDirectory);
                        @this.TryCreateHardLink(shadowCopyPath, cachePath);
                    }
                }
            }

            static bool cacheEntryMatches(string originalPath, string cachePath)
            {
                var originalInfo = new FileInfo(originalPath);
                var cacheInfo = new FileInfo(cachePath);
                try
                {
                    // Sometimes differing assemblies will have same mvid, e.g. before and after Ready2Run compilation.
                    // To defend against this, we require both length and mvid match in order to use the cached file.
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

        private static string HashToHex(ReadOnlySpan<char> value)
        {
            Span<byte> hash = stackalloc byte[16];
            int bytesWritten = XxHash128.Hash(MemoryMarshal.AsBytes(value), hash);
            Debug.Assert(bytesWritten == hash.Length);

            return HexUtilities.ToHexStringLower(hash);
        }

        private static string? TryGetCacheKey(string originalPath)
        {
            // Key format: (original filename) + (file path hash) + (mvid) + (original file length) + (original file extension)
            var hexHash = HashToHex(originalPath);
            try
            {
                var mvid = AssemblyUtilities.ReadMvid(originalPath);
                var length = new FileInfo(originalPath).Length;
                return $"{Path.GetFileNameWithoutExtension(originalPath)}-{hexHash}-{mvid:N}-{length}{Path.GetExtension(originalPath)}";
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

        /// <summary>Create a hard link to a file.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.file.createhardlink?view=net-11.0" />
        private bool TryCreateHardLink(string path, string pathToTarget)
        {
            return EnableHardLinks && CreateHardLink(pathToTarget, path, IntPtr.Zero);

            // https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createhardlinkw
            [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
        }

        /// <summary>Get number of hard links to a file.</summary>
        private static ByHandleFileInformation? TryGetWindowsFileInformation(string path)
        {
            // https://learn.microsoft.com/en-us/windows/win32/fileio/file-access-rights-constants
            const uint FILE_READ_ATTRIBUTES = 0x0080;

            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
            const uint FILE_SHARE_READ = 0x00000001;
            const uint FILE_SHARE_WRITE = 0x00000002;
            const uint FILE_SHARE_DELETE = 0x00000004;
            const uint OPEN_EXISTING = 3;

            using var handle = CreateFileW(
                lpFileName: path,
                dwDesiredAccess: FILE_READ_ATTRIBUTES,
                dwShareMode: FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                lpSecurityAttributes: IntPtr.Zero,
                dwCreationDisposition: OPEN_EXISTING,
                dwFlagsAndAttributes: 0,
                hTemplateFile: IntPtr.Zero);

            if (!GetFileInformationByHandle(handle, out var fileInformation))
                return null;

            return fileInformation;

            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern SafeFileHandle CreateFileW(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            // https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfileinformationbyhandle
            [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern bool GetFileInformationByHandle(SafeFileHandle handle, out ByHandleFileInformation fileInformation);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ByHandleFileInformation
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }
    }
}
