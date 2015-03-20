// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Manages cache of the following information for Portable Executables loaded from files.
    ///
    /// For assemblies - a map from file name and timestamp to: 
    ///     1) A weak reference to the corresponding PEAssembly object;
    ///     2) A list of weak references to instances of VB/CS AssemblySymbols based on the PEAssembly object.
    ///
    /// For modules - a map from file name and timestamp to a weak reference to the corresponding PEModule object
    /// </summary>
    [Obsolete("To be removed", error: false)]
    internal static class MetadataCache
    {
        /// <summary>
        /// Global cache for assemblies imported from files.
        /// </summary>
        private static Dictionary<FileKey, CachedAssembly> s_assembliesFromFiles =
            new Dictionary<FileKey, CachedAssembly>();

        private static List<FileKey> s_assemblyKeys = new List<FileKey>();

        /// <summary>
        /// Global cache for net-modules imported from files.
        /// </summary>
        private static Dictionary<FileKey, CachedModule> s_modulesFromFiles =
            new Dictionary<FileKey, CachedModule>();

        private static List<FileKey> s_moduleKeys = new List<FileKey>();

        /// <summary>
        /// Timer triggering compact operation for metadata cache.
        /// </summary>
        private static readonly Timer s_compactTimer = new Timer(CompactCache);

        /// <summary>
        /// Period at which the timer is firing (30 seconds).
        /// </summary>
        private const int compactTimerPeriod = 30000;

        private const int yes = 1;
        private const int no = 0;

        /// <summary>
        /// compactTimer's procedure is in progress.
        /// Used to prevent multiple instances running in parallel.
        /// </summary>
        private static int s_compactInProgress;

        /// <summary>
        /// compactTimer is on, i.e. will fire.
        /// 
        /// This field is changed to 'yes' only by EnableCompactTimer(),
        /// and is changed to 'no' only by CompactCache().
        /// </summary>
        private static int s_compactTimerIsOn;

        /// <summary>
        /// Collection count last time the cache was compacted.
        /// </summary>
        private static int s_compactCollectionCount;

        internal struct CachedAssembly
        {
            public readonly WeakReference<AssemblyMetadata> Metadata;

            // Save a reference to the cached symbols so that they don't get collected 
            // if the metadata object gets collected.
            public readonly WeakList<IAssemblySymbol> CachedSymbols;

            public CachedAssembly(AssemblyMetadata metadata)
            {
                Debug.Assert(metadata != null);

                // Save a reference to the cached symbols so that they don't get collected 
                // if the metadata object gets collected.
                this.CachedSymbols = metadata.CachedSymbols;

                this.Metadata = new WeakReference<AssemblyMetadata>(metadata);
            }
        }

        internal struct CachedModule
        {
            public readonly WeakReference<ModuleMetadata> Metadata;

            public CachedModule(ModuleMetadata metadata)
            {
                Debug.Assert(metadata != null);
                this.Metadata = new WeakReference<ModuleMetadata>(metadata);
            }
        }

        /// <summary>
        /// Lock that must be acquired for the duration of read/write operations on MetadataCache.
        /// 
        /// Internal for testing.
        /// </summary>
        internal static readonly object Guard = new object();

        /// <summary>
        /// Return amount of GC collections occurred so far.
        /// </summary>
        private static int GetCollectionCount()
        {
            int count = 0;

            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                unchecked
                {
                    count += GC.CollectionCount(i);
                }
            }

            return count;
        }

        /// <summary>
        /// Called by compactTimer to compact the cache.
        /// </summary>
        private static void CompactCache(Object state)
        {
            if (s_compactTimerIsOn == no)
            {
                return;
            }

            // Prevent execution in parallel.
            if (Interlocked.CompareExchange(ref s_compactInProgress, yes, no) != no)
            {
                return;
            }

            try
            {
                int currentCollectionCount = GetCollectionCount();

                if (currentCollectionCount == s_compactCollectionCount)
                {
                    // Nothing was collected since we compacted caches last time.
                    return;
                }

                CompactCacheOfAssemblies();
                CompactCacheOfModules();

                s_compactCollectionCount = currentCollectionCount;

                lock (Guard)
                {
                    if (!(AnyAssembliesCached() || AnyModulesCached()))
                    {
                        // Stop the timer
                        s_compactTimerIsOn = no;
                        s_compactTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
            finally
            {
                System.Diagnostics.Debug.Assert(s_compactInProgress == yes);
                s_compactInProgress = no;
            }
        }

        /// <summary>
        /// Trigger timer every 30 seconds.
        /// Cache must be locked before calling this method.
        /// </summary>
        internal static void EnableCompactTimer()
        {
            if (Interlocked.CompareExchange(ref s_compactTimerIsOn, yes, no) == no)
            {
                s_compactCollectionCount = GetCollectionCount();
                s_compactTimer.Change(compactTimerPeriod, compactTimerPeriod);
            }
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal static bool CompactTimerIsOn
        {
            get
            {
                return s_compactTimerIsOn == yes;
            }
        }


        /// <summary>
        /// Trigger compact operation for the cache, meant to be used for test purpose only.
        /// Locking the cache prior to calling this method is a good way to get into a deadlock.
        /// 
        /// For test purposes only!!!
        /// </summary>
        internal static void TriggerCacheCompact()
        {
            if (Thread.VolatileRead(ref s_compactTimerIsOn) == yes)
            {
                // If CompactCache procedure is in progress, wait for it to complete.
                // If the cache is locked by this thread, we might wait forever because 
                // CompactCache might be deadlocked.
                while (Interlocked.CompareExchange(ref s_compactInProgress, yes, no) != no)
                {
                    Thread.Sleep(10);
                }

                if (Thread.VolatileRead(ref s_compactTimerIsOn) == yes)
                {
                    s_compactInProgress = 0;

                    // Force the timer to fire now.
                    s_compactTimer.Change(0, compactTimerPeriod);
                }
                else
                {
                    // Timer was disabled while we were waiting for CompactCache to complete.
                    // Do not enable it.
                    s_compactInProgress = 0;
                }
            }
        }

        /// <summary>
        /// Called by compactTimer.
        /// </summary>
        private static void CompactCacheOfAssemblies()
        {
            // Do one pass through the assemblyKeys list    
            int originalCount = -1;

            for (int current = 0; ; current++)
            {
                // Compact assemblies, one assembly per lock

                // Lock our cache
                lock (Guard)
                {
                    if (originalCount == -1)
                    {
                        originalCount = s_assemblyKeys.Count;
                    }

                    if (s_assemblyKeys.Count > current)
                    {
                        CachedAssembly cachedAssembly;
                        FileKey key = s_assemblyKeys[current];

                        if (s_assembliesFromFiles.TryGetValue(key, out cachedAssembly))
                        {
                            if (cachedAssembly.Metadata.IsNull())
                            {
                                // Assembly has been collected
                                s_assembliesFromFiles.Remove(key);
                                s_assemblyKeys.RemoveAt(current);
                                current--;
                            }
                        }
                        else
                        {
                            // Key is not found. Shouldn't ever get here!
                            System.Diagnostics.Debug.Assert(false);
                            s_assemblyKeys.RemoveAt(current);
                            current--;
                        }
                    }

                    if (s_assemblyKeys.Count <= current + 1)
                    {
                        // no more assemblies to process
                        if (originalCount > s_assemblyKeys.Count)
                        {
                            s_assemblyKeys.TrimExcess();
                        }

                        return;
                    }
                }

                Thread.Yield();
            }
        }

        private static bool AnyAssembliesCached()
        {
            return s_assemblyKeys.Count > 0;
        }

        /// <summary>
        /// Called by compactTimer.
        /// </summary>
        private static void CompactCacheOfModules()
        {
            // Do one pass through the moduleKeys list    
            int originalCount = -1;

            for (int current = 0; ; current++)
            {
                // Compact modules, one module per lock

                // Lock our cache
                lock (Guard)
                {
                    if (originalCount == -1)
                    {
                        originalCount = s_moduleKeys.Count;
                    }

                    if (s_moduleKeys.Count > current)
                    {
                        CachedModule cachedModule;
                        FileKey key = s_moduleKeys[current];

                        if (s_modulesFromFiles.TryGetValue(key, out cachedModule))
                        {
                            if (cachedModule.Metadata.IsNull())
                            {
                                // Module has been collected
                                s_modulesFromFiles.Remove(key);
                                s_moduleKeys.RemoveAt(current);
                                current--;
                            }
                        }
                        else
                        {
                            // Key is not found. Shouldn't ever get here!
                            System.Diagnostics.Debug.Assert(false);
                            s_moduleKeys.RemoveAt(current);
                            current--;
                        }
                    }

                    if (s_moduleKeys.Count <= current + 1)
                    {
                        // no more modules to process
                        if (originalCount > s_moduleKeys.Count)
                        {
                            s_moduleKeys.TrimExcess();
                        }

                        return;
                    }
                }

                Thread.Yield();
            }
        }

        private static bool AnyModulesCached()
        {
            return s_moduleKeys.Count > 0;
        }

        /// <summary>
        /// Global cache for assemblies imported from files.
        /// Internal accessibility is for test purpose only.
        /// </summary>
        internal static Dictionary<FileKey, CachedAssembly> AssembliesFromFiles
        {
            get
            {
                return s_assembliesFromFiles;
            }
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal static List<FileKey> AssemblyKeys
        {
            get
            {
                return s_assemblyKeys;
            }
        }

        /// <summary>
        /// Global cache for net-modules imported from files.
        /// Internal accessibility is for test purpose only.
        /// </summary>
        /// <remarks></remarks>
        internal static Dictionary<FileKey, CachedModule> ModulesFromFiles
        {
            get
            {
                return s_modulesFromFiles;
            }
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal static List<FileKey> ModuleKeys
        {
            get
            {
                return s_moduleKeys;
            }
        }

        // for testing
        internal static CleaningCacheLock LockAndClean()
        {
            return CleaningCacheLock.LockAndCleanCaches();
        }

        /// <summary>
        /// This class is meant to be used for test purpose only.
        /// It locks MetadataCache until the instance is disposed.
        /// Upon locking, the cache is swapped with an empty cache,
        /// original cache is restored before the cache is unlocked.
        /// </summary>
        internal class CleaningCacheLock : IDisposable
        {
            private Dictionary<FileKey, CachedAssembly> _saveAssembliesFromFiles;
            private List<FileKey> _saveAssemblyKeys;
            private Dictionary<FileKey, CachedModule> _saveModulesFromFiles;
            private List<FileKey> _saveModuleKeys;

            private bool _cacheIsLocked;

            // helpers to diagnose lock leaks.
            private CleaningCacheLock _next;
            private static CleaningCacheLock s_last;
            private string _stackTrace;
            private int _threadId;

            private CleaningCacheLock()
            { }

            public static CleaningCacheLock LockAndCleanCaches()
            {
                CleaningCacheLock result = new CleaningCacheLock();

                try
                {
                    Monitor.Enter(Guard, ref result._cacheIsLocked);

                    result._saveAssembliesFromFiles = s_assembliesFromFiles;
                    result._saveAssemblyKeys = s_assemblyKeys;
                    result._saveModulesFromFiles = s_modulesFromFiles;
                    result._saveModuleKeys = s_moduleKeys;

                    var newAssembliesFromFiles = new Dictionary<FileKey, CachedAssembly>();
                    var newAssemblyKeys = new List<FileKey>();
                    var newModulesFromFiles = new Dictionary<FileKey, CachedModule>();
                    var newModuleKeys = new List<FileKey>();

                    s_assembliesFromFiles = newAssembliesFromFiles;
                    s_assemblyKeys = newAssemblyKeys;
                    s_modulesFromFiles = newModulesFromFiles;
                    s_moduleKeys = newModuleKeys;

                    result._threadId = Thread.CurrentThread.ManagedThreadId;
                    result._stackTrace = Environment.StackTrace;

                    result._next = s_last;
                    s_last = result;
                }
                catch
                {
                    if (result._cacheIsLocked)
                    {
                        result._cacheIsLocked = false;
                        Monitor.Exit(Guard);
                    }

                    throw;
                }

                return result;
            }

            public void FreeAndRestore()
            {
                if (!_cacheIsLocked)
                {
                    throw new InvalidOperationException();
                }

                Dispose();
            }

            /// <summary>
            /// Clean global metadata caches, meant to be used for test purpose only.
            /// </summary>
            public void CleanCaches()
            {
                if (!_cacheIsLocked)
                {
                    throw new InvalidOperationException();
                }

                s_assembliesFromFiles.Clear();
                s_assemblyKeys.Clear();
                s_modulesFromFiles.Clear();
                s_moduleKeys.Clear();
            }

            public void Dispose()
            {
                if (_cacheIsLocked)
                {
                    DisposeCachedMetadata();

                    s_assembliesFromFiles = _saveAssembliesFromFiles;
                    s_assemblyKeys = _saveAssemblyKeys;
                    s_modulesFromFiles = _saveModulesFromFiles;
                    s_moduleKeys = _saveModuleKeys;

                    Debug.Assert(ReferenceEquals(s_last, this));
                    Debug.Assert(_threadId == Thread.CurrentThread.ManagedThreadId);

                    s_last = _next;
                    _cacheIsLocked = false;
                    Monitor.Exit(Guard);
                }
            }
        }

        internal static void DisposeCachedMetadata()
        {
            foreach (var cachedAssembly in s_assembliesFromFiles.Values)
            {
                AssemblyMetadata metadata;
                if (cachedAssembly.Metadata.TryGetTarget(out metadata))
                {
                    metadata.Dispose();
                }
            }

            foreach (var cachedModule in s_modulesFromFiles.Values)
            {
                ModuleMetadata metadata;
                if (cachedModule.Metadata.TryGetTarget(out metadata))
                {
                    metadata.Dispose();
                }
            }
        }

        /// <exception cref="IOException"/>
        internal static Metadata GetOrCreateFromFile(string fullPath, MetadataImageKind kind)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            lock (Guard)
            {
                if (kind == MetadataImageKind.Assembly)
                {
                    return GetOrCreateAssemblyFromFile(fullPath);
                }
                else
                {
                    return GetOrCreateModuleFromFile(fullPath);
                }
            }
        }

        /// <exception cref="IOException"/>
        private static AssemblyMetadata GetOrCreateAssemblyFromFile(string fullPath)
        {
            AssemblyMetadata assembly = null;

            // may throw:
            FileKey key = FileKey.Create(fullPath);

            CachedAssembly cachedAssembly;
            bool existingKey = s_assembliesFromFiles.TryGetValue(key, out cachedAssembly);
            if (existingKey && cachedAssembly.Metadata.TryGetTarget(out assembly))
            {
                return assembly;
            }

            // memory-map all modules of the assembly:
            assembly = AssemblyMetadata.CreateFromFile(fullPath);

            // refresh the timestamp (the file may have changed just before we memory-mapped it):
            bool fault = true;
            try
            {
                key = FileKey.Create(fullPath);
                fault = false;
            }
            finally
            {
                if (fault)
                {
                    assembly.Dispose();
                }
            }

            cachedAssembly = new CachedAssembly(assembly);
            s_assembliesFromFiles[key] = cachedAssembly;

            if (!existingKey)
            {
                s_assemblyKeys.Add(key);
                EnableCompactTimer();
            }

            return assembly;
        }

        /// <exception cref="IOException"/>
        private static ModuleMetadata GetOrCreateModuleFromFile(string fullPath)
        {
            ModuleMetadata module = null;

            // may throw
            FileKey key = FileKey.Create(fullPath);

            CachedModule cachedModule;
            bool existingKey = s_modulesFromFiles.TryGetValue(key, out cachedModule);
            if (existingKey && cachedModule.Metadata.TryGetTarget(out module))
            {
                return module;
            }

            // memory-map the module:
            module = ModuleMetadata.CreateFromFile(fullPath);

            // refresh the timestamp (the file may have changed just before we memory-mapped it):
            bool fault = true;
            try
            {
                key = FileKey.Create(fullPath);
                fault = false;
            }
            finally
            {
                if (fault)
                {
                    module.Dispose();
                }
            }

            cachedModule = new CachedModule(module);
            s_modulesFromFiles[key] = cachedModule;

            if (!existingKey)
            {
                s_moduleKeys.Add(key);
                EnableCompactTimer();
            }

            return module;
        }
    }
}
