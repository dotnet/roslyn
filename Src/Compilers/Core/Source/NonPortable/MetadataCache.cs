// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
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
    /// 
    /// For analyzer assemblies - a map from file name and timestamp to a weak reference to the diagnostic analyzers defined in the assembly.
    /// </summary>
    internal static class MetadataCache
    {
        /// <summary>
        /// Global cache for assemblies imported from files.
        /// The cache must be locked for the duration of read/write operations, see CacheLockObject property.
        /// </summary>
        private static Dictionary<FileKey, CachedAssembly> assembliesFromFiles =
            new Dictionary<FileKey, CachedAssembly>();

        private static List<FileKey> assemblyKeys = new List<FileKey>();

        /// <summary>
        /// Global cache for net-modules imported from files.
        /// The cache must be locked for the duration of read/write operations, see CacheLockObject property.
        /// </summary>
        private static Dictionary<FileKey, CachedModule> modulesFromFiles =
            new Dictionary<FileKey, CachedModule>();

        private static List<FileKey> moduleKeys = new List<FileKey>();

        /// <summary>
        /// Global cache for diagnostic analyzers imported from analyzer assembly files.
        /// The cache must be locked for the duration of read/write operations, see CacheLockObject property.
        /// </summary>
        private static Dictionary<FileKey, CachedAnalyzers> analyzersFromFiles =
            new Dictionary<FileKey, CachedAnalyzers>();

        private static List<FileKey> analyzerAssemblyKeys = new List<FileKey>();

        /// <summary>
        /// Timer triggering compact operation for metadata cache.
        /// </summary>
        private static readonly Timer compactTimer = new Timer(CompactCache);

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
        private static int compactInProgress;

        /// <summary>
        /// compactTimer is on, i.e. will fire.
        /// 
        /// This field is changed to 'yes' only by EnableCompactTimer(),
        /// and is changed to 'no' only by CompactCache().
        /// </summary>
        private static int compactTimerIsOn;

        /// <summary>
        /// Collection count last time the cache was compacted.
        /// </summary>
        private static int compactCollectionCount;

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

        internal struct CachedAnalyzers
        {
            // Save a reference to the cached analyzers so that they don't get collected 
            // if the metadata object gets collected.
            public readonly WeakReference Analyzers;

            public CachedAnalyzers(ImmutableArray<IDiagnosticAnalyzer> analyzers)
            {
                Debug.Assert(analyzers != null);

                this.Analyzers = new WeakReference(analyzers);
            }
        }

        /// <summary>
        /// Lock that must be acquired for the duration of read/write operations on MetadataCache.
        /// 
        /// Internal for testing.
        /// </summary>
        internal static readonly object Guard = new CommonLock();

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
            if (compactTimerIsOn == no)
            {
                return;
            }

            // Prevent execution in parallel.
            if (Interlocked.CompareExchange(ref compactInProgress, yes, no) != no)
            {
                return;
            }

            try
            {
                int currentCollectionCount = GetCollectionCount();

                if (currentCollectionCount == compactCollectionCount)
                {
                    // Nothing was collected since we compacted caches last time.
                    return;
                }

                CompactCacheOfAssemblies();
                CompactCacheOfModules();
                CompactCacheOfAnalyzers();

                compactCollectionCount = currentCollectionCount;

                DebuggerUtilities.CallBeforeAcquiringLock(); //see method comment
                lock (Guard)
                {
                    if (!(AnyAssembliesCached() || AnyModulesCached() || AnyAnalyzerAssembliesCached()))
                    {
                        // Stop the timer
                        compactTimerIsOn = no;
                        compactTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
            finally
            {
                System.Diagnostics.Debug.Assert(compactInProgress == yes);
                compactInProgress = no;
            }
        }

        /// <summary>
        /// Trigger timer every 30 seconds.
        /// Cache must be locked before calling this method.
        /// </summary>
        internal static void EnableCompactTimer()
        {
            if (Interlocked.CompareExchange(ref compactTimerIsOn, yes, no) == no)
            {
                compactCollectionCount = GetCollectionCount();
                compactTimer.Change(compactTimerPeriod, compactTimerPeriod);
            }
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal static bool CompactTimerIsOn
        {
            get
            {
                return compactTimerIsOn == yes;
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
            if (Thread.VolatileRead(ref compactTimerIsOn) == yes)
            {
                // If CompactCache procedure is in progress, wait for it to complete.
                // If the cache is locked by this thread, we might wait forever because 
                // CompactCache might be deadlocked.
                while (Interlocked.CompareExchange(ref compactInProgress, yes, no) != no)
                {
                    Thread.Sleep(10);
                }

                if (Thread.VolatileRead(ref compactTimerIsOn) == yes)
                {
                    compactInProgress = 0;

                    // Force the timer to fire now.
                    compactTimer.Change(0, compactTimerPeriod);
                }
                else
                {
                    // Timer was disabled while we were waiting for CompactCache to complete.
                    // Do not enable it.
                    compactInProgress = 0;
                }
            }
        }

        /// <summary>
        /// Called by compactTimer.
        /// </summary>
        private static void CompactCacheOfAssemblies()
        {
            DebuggerUtilities.CallBeforeAcquiringLock(); //see method comment

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
                        originalCount = assemblyKeys.Count;
                    }

                    if (assemblyKeys.Count > current)
                    {
                        CachedAssembly cachedAssembly;
                        FileKey key = assemblyKeys[current];

                        if (assembliesFromFiles.TryGetValue(key, out cachedAssembly))
                        {
                            if (cachedAssembly.Metadata.IsNull())
                            {
                                // Assembly has been collected
                                assembliesFromFiles.Remove(key);
                                assemblyKeys.RemoveAt(current);
                                current--;
                            }
                        }
                        else
                        {
                            // Key is not found. Shouldn't ever get here!
                            System.Diagnostics.Debug.Assert(false);
                            assemblyKeys.RemoveAt(current);
                            current--;
                        }
                    }

                    if (assemblyKeys.Count <= current + 1)
                    {
                        // no more assemblies to process
                        if (originalCount > assemblyKeys.Count)
                        {
                            assemblyKeys.TrimExcess();
                        }

                        return;
                    }
                }

                Thread.Yield();
            }
        }

        private static bool AnyAssembliesCached()
        {
            return assemblyKeys.Count > 0;
        }

        /// <summary>
        /// Called by compactTimer.
        /// </summary>
        private static void CompactCacheOfModules()
        {
            DebuggerUtilities.CallBeforeAcquiringLock(); //see method comment

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
                        originalCount = moduleKeys.Count;
                    }

                    if (moduleKeys.Count > current)
                    {
                        CachedModule cachedModule;
                        FileKey key = moduleKeys[current];

                        if (modulesFromFiles.TryGetValue(key, out cachedModule))
                        {
                            if (cachedModule.Metadata.IsNull())
                            {
                                // Module has been collected
                                modulesFromFiles.Remove(key);
                                moduleKeys.RemoveAt(current);
                                current--;
                            }
                        }
                        else
                        {
                            // Key is not found. Shouldn't ever get here!
                            System.Diagnostics.Debug.Assert(false);
                            moduleKeys.RemoveAt(current);
                            current--;
                        }
                    }

                    if (moduleKeys.Count <= current + 1)
                    {
                        // no more modules to process
                        if (originalCount > moduleKeys.Count)
                        {
                            moduleKeys.TrimExcess();
                        }

                        return;
                    }
                }

                Thread.Yield();
            }
        }

        private static bool AnyModulesCached()
        {
            return moduleKeys.Count > 0;
        }

        /// <summary>
        /// Called by compactTimer.
        /// </summary>
        private static void CompactCacheOfAnalyzers()
        {
            DebuggerUtilities.CallBeforeAcquiringLock(); //see method comment

            // Do one pass through the analyzerAssemblyKeys list    
            int originalCount = -1;

            for (int current = 0; ; current++)
            {
                // Compact analyzer assemblies, one assembly per lock

                // Lock our cache
                lock (Guard)
                {
                    if (originalCount == -1)
                    {
                        originalCount = analyzerAssemblyKeys.Count;
                    }

                    if (analyzerAssemblyKeys.Count > current)
                    {
                        CachedAnalyzers cahedAnalyzers;
                        FileKey key = analyzerAssemblyKeys[current];

                        if (analyzersFromFiles.TryGetValue(key, out cahedAnalyzers))
                        {
                            if (!cahedAnalyzers.Analyzers.IsAlive)
                            {
                                // Analyzers has been collected
                                analyzersFromFiles.Remove(key);
                                analyzerAssemblyKeys.RemoveAt(current);
                                current--;
                            }
                        }
                        else
                        {
                            // Key is not found. Shouldn't ever get here!
                            System.Diagnostics.Debug.Assert(false);
                            analyzerAssemblyKeys.RemoveAt(current);
                            current--;
                        }
                    }

                    if (analyzerAssemblyKeys.Count <= current + 1)
                    {
                        // no more assemblies to process
                        if (originalCount > analyzerAssemblyKeys.Count)
                        {
                            analyzerAssemblyKeys.TrimExcess();
                        }

                        return;
                    }
                }

                Thread.Yield();
            }
        }

        private static bool AnyAnalyzerAssembliesCached()
        {
            return analyzerAssemblyKeys.Count > 0;
        }

        /// <summary>
        /// Global cache for assemblies imported from files.
        /// The cache must be locked for the duration of read/write operations, see CacheLockObject property.
        /// Internal accessibility is for test purpose only.
        /// </summary>
        internal static Dictionary<FileKey, CachedAssembly> AssembliesFromFiles
        {
            get
            {
                return assembliesFromFiles;
            }
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal static List<FileKey> AssemblyKeys
        {
            get
            {
                return assemblyKeys;
            }
        }

        /// <summary>
        /// Global cache for net-modules imported from files.
        /// The cache must be locked for the duration of read/write operations, see CacheLockObject property.
        /// Internal accessibility is for test purpose only.
        /// </summary>
        /// <remarks></remarks>
        internal static Dictionary<FileKey, CachedModule> ModulesFromFiles
        {
            get
            {
                return modulesFromFiles;
            }
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal static List<FileKey> ModuleKeys
        {
            get
            {
                return moduleKeys;
            }
        }

        /// <summary>
        /// Global cache for analyzers imported from files.
        /// The cache must be locked for the duration of read/write operations, see CacheLockObject property.
        /// Internal accessibility is for test purpose only.
        /// </summary>
        internal static Dictionary<FileKey, CachedAnalyzers> AnalyzersFromFiles
        {
            get
            {
                return analyzersFromFiles;
            }
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal static List<FileKey> AnalyzerAssemblyKeys
        {
            get
            {
                return analyzerAssemblyKeys;
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
            private Dictionary<FileKey, CachedAssembly> saveAssembliesFromFiles;
            private List<FileKey> saveAssemblyKeys;
            private Dictionary<FileKey, CachedModule> saveModulesFromFiles;
            private List<FileKey> saveModuleKeys;
            private Dictionary<FileKey, CachedAnalyzers> saveAnalyzersFromFiles;
            private List<FileKey> saveAnalyzerAssemblyKeys;

            private bool cacheIsLocked;

            // helpers to diagnose lock leaks.
            private CleaningCacheLock next;
            private static CleaningCacheLock last;
            private string stackTrace;
            private int threadId;

            private CleaningCacheLock()
            { }

            public static CleaningCacheLock LockAndCleanCaches()
            {
                CleaningCacheLock result = new CleaningCacheLock();

                try
                {
                    Monitor.Enter(Guard, ref result.cacheIsLocked);

                    result.saveAssembliesFromFiles = assembliesFromFiles;
                    result.saveAssemblyKeys = assemblyKeys;
                    result.saveModulesFromFiles = modulesFromFiles;
                    result.saveModuleKeys = moduleKeys;
                    result.saveAnalyzersFromFiles = analyzersFromFiles;
                    result.saveAnalyzerAssemblyKeys = analyzerAssemblyKeys;

                    var newAssembliesFromFiles = new Dictionary<FileKey, CachedAssembly>();
                    var newAssemblyKeys = new List<FileKey>();
                    var newModulesFromFiles = new Dictionary<FileKey, CachedModule>();
                    var newModuleKeys = new List<FileKey>();
                    var newAnalyzersFromFiles = new Dictionary<FileKey, CachedAnalyzers>();
                    var newAnalyzerAssemblyKeys = new List<FileKey>();

                    assembliesFromFiles = newAssembliesFromFiles;
                    assemblyKeys = newAssemblyKeys;
                    modulesFromFiles = newModulesFromFiles;
                    moduleKeys = newModuleKeys;
                    analyzersFromFiles = newAnalyzersFromFiles;
                    analyzerAssemblyKeys = newAnalyzerAssemblyKeys;

                    result.threadId = Thread.CurrentThread.ManagedThreadId;
                    result.stackTrace = Environment.StackTrace;

                    result.next = last;
                    last = result;
                }
                catch
                {
                    if (result.cacheIsLocked)
                    {
                        result.cacheIsLocked = false;
                        Monitor.Exit(Guard);
                    }

                    throw;
                }

                return result;
            }

            public void FreeAndRestore()
            {
                if (!cacheIsLocked)
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
                if (!cacheIsLocked)
                {
                    throw new InvalidOperationException();
                }

                assembliesFromFiles.Clear();
                assemblyKeys.Clear();
                modulesFromFiles.Clear();
                moduleKeys.Clear();
                analyzersFromFiles.Clear();
                analyzerAssemblyKeys.Clear();
            }

            public void Dispose()
            {
                if (cacheIsLocked)
                {
                    DisposeCachedMetadata();

                    assembliesFromFiles = this.saveAssembliesFromFiles;
                    assemblyKeys = this.saveAssemblyKeys;
                    modulesFromFiles = this.saveModulesFromFiles;
                    moduleKeys = this.saveModuleKeys;
                    analyzersFromFiles = this.saveAnalyzersFromFiles;
                    analyzerAssemblyKeys = this.saveAnalyzerAssemblyKeys;

                    Debug.Assert(ReferenceEquals(last, this));
                    Debug.Assert(this.threadId == Thread.CurrentThread.ManagedThreadId);

                    last = this.next;
                    cacheIsLocked = false;
                    Monitor.Exit(Guard);
                }
            }
        }

        internal static void DisposeCachedMetadata()
        {
            foreach (var cachedAssembly in assembliesFromFiles.Values)
            {
                AssemblyMetadata metadata;
                if (cachedAssembly.Metadata.TryGetTarget(out metadata))
                {
                    metadata.Dispose();
                }
            }

            foreach (var cachedModule in modulesFromFiles.Values)
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

        internal static ImmutableArray<IDiagnosticAnalyzer> GetOrCreateAnalyzersFromFile(AnalyzerFileReference analyzerReference)
        {
            string fullPath = analyzerReference.FullPath;
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            lock (Guard)
            {
                // may throw:
                FileKey key = FileKey.Create(fullPath);

                CachedAnalyzers cachedAnalyzers;
                if (analyzersFromFiles.TryGetValue(key, out cachedAnalyzers) && cachedAnalyzers.Analyzers.IsAlive)
                {
                    return (ImmutableArray<IDiagnosticAnalyzer>)cachedAnalyzers.Analyzers.Target;
                }

                // get all analyzers in the assembly:
                var builder = ImmutableArray.CreateBuilder<IDiagnosticAnalyzer>();
                analyzerReference.AddAnalyzers(builder, null, null);
                var analyzers = builder.ToImmutable();

                // refresh the timestamp (the file may have changed just before we memory-mapped it):
                key = FileKey.Create(fullPath);

                analyzersFromFiles[key] = new CachedAnalyzers(analyzers);
                analyzerAssemblyKeys.Add(key);
                EnableCompactTimer();

                return analyzers;
            }
        }

        /// <exception cref="IOException"/>
        private static AssemblyMetadata GetOrCreateAssemblyFromFile(string fullPath)
        {
            AssemblyMetadata assembly = null;

            // may throw:
            FileKey key = FileKey.Create(fullPath);

            CachedAssembly cachedAssembly;
            bool existingKey = assembliesFromFiles.TryGetValue(key, out cachedAssembly);
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
            assembliesFromFiles[key] = cachedAssembly;

            if (!existingKey)
            {
                assemblyKeys.Add(key);
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
            bool existingKey = modulesFromFiles.TryGetValue(key, out cachedModule);
            if (existingKey && cachedModule.Metadata.TryGetTarget(out module))
            {
                return module;
            }

            // mempy-map the module:
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
            modulesFromFiles[key] = cachedModule;

            if (!existingKey)
            {
                moduleKeys.Add(key);
                EnableCompactTimer();
            }

            return module;
        }
    }
}