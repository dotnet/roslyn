using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Roslyn.Compilers.AssemblyManager;
using Roslyn.Compilers.CSharp.Metadata.PE;
using Roslyn.Compilers.MetadataReader;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    public partial class Compilation
    {
        /// <summary>
        /// The list of RetargetingAssemblySymbol objects created for this Compilation. 
        /// RetargetingAssemblySymbols are created when some other compilation references this one, 
        /// but the other references provided are incompatible with it. For example, compilation C1 
        /// references v1 of Lib.dll and compilation C2 references C1 and v2 of Lib.dll. In this
        /// case, in context of C2, all types from v1 of Lib.dll leaking through C1 (through method 
        /// signatures, etc.) must be retargeted to the types from v2 of Lib.dll. This is what 
        /// RetargetingAssemblySymbol is responsible for. In the example above, modules in C2 do not 
        /// reference C1.m_AssemblySymbol, but reference a special RetargetingAssemblySymbol created
        /// for C1 by AssemblyManager.
        ///  
        /// WeakReference is used to allow RetargetingAssemblySymbols to be collected when they become unused.
        /// 
        /// The cache must be locked for the duration of read/write operations, 
        /// see AssemblyManager.CacheLockObject property.
        /// 
        /// Internal accesibility is for test purpose only.
        /// </summary>
        /// <remarks></remarks>
        internal readonly List<WeakReference<Retargeting.RetargetingAssemblySymbol>> OtherAssemblySymbols =
            new List<WeakReference<Retargeting.RetargetingAssemblySymbol>>();

        /// <summary>
        /// AssemblySymbol to represent missing, for whatever reason, CorLibrary.
        /// The symbol is created by AssemblyManager on as needed basis and is shared by all compilations
        /// with missing CorLibraries.
        /// </summary>
        private static MissingCorLibrarySymbol MissingCorLibrary;

        private bool sourceAssemblyCreationReentranceFlag;

        /// <summary>
        /// AssemblyManager encapsulates functionality to create an underlying SourceAssemblySymbol 
        /// (with underlying ModuleSymbols) for Compilation and AssemblySymbols for referenced
        /// assemblies (with underlying ModuleSymbols) all properly linked together based on
        /// reference resolution between them.
        /// 
        /// AssemblyManager is also responsible for reuse of metadata readers for imported modules
        /// and assemblies as well as existing AssemblySymbols for referenced assemblies. In order
        /// to do that, it maintains global cache for metadata readers and AssemblySymbols
        /// associated with them. The cache uses WeakReferences to refer to the metadata readers and
        /// AssemblySymbols to allow memory and resources being reclaimed once they are no longer
        /// used. The tricky part about reusing existing AssemblySymbols is to find a set of
        /// AssemblySymbols that are created for the referenced assemblies, which (the
        /// AssemblySymbols from the set) are linked in a way, consistent with the reference
        /// resolution between the referenced assemblies.
        /// 
        /// When existing Compilation is used as a metadata reference, there are scenarios when its
        /// underlying SourceAssemblySymbol cannot be used to provide symbols in context of the new
        /// Compilation. Consider classic multi-targeting scenario: compilation C1 references v1 of
        /// Lib.dll and compilation C2 references C1 and v2 of Lib.dll. In this case,
        /// SourceAssemblySymbol for C1 is linked to AssemblySymbol for v1 of Lib.dll. However,
        /// given the set of references for C2, the same reference for C1 should be resolved against
        /// v2 of Lib.dll. In other words, in context of C2, all types from v1 of Lib.dll leaking
        /// through C1 (through method signatures, etc.) must be retargeted to the types from v2 of
        /// Lib.dll. In this case, AssemblyManager creates a special RetargetingAssemblySymbol for
        /// C1, which is responsible for the type retargeting. The RetargetingAssemblySymbols could
        /// also be reused for different Compilations, AssemblyManager maintains a cache of
        /// RetargetingAssemblySymbols (WeakReferences) for each Compilation.
        /// 
        /// The only public entry point of this class is CreateSourceAssembly() method.
        /// 
        /// TODO: Comment on CorLibrary.
        /// </summary>
        internal static class AssemblyManager
        {
            /// <summary>
            /// List of compilations that should be compacted.
            /// 
            /// The cache must be locked for the duration of read/write operations, 
            /// see AssemblyManager.CacheLockObject property.
            /// 
            /// Internal accesibility is for test purpose only.
            /// </summary>
            internal static readonly List<WeakReference<Compilation>> CompilationsToCompact;

            static AssemblyManager()
            {
                CompilationsToCompact = new List<WeakReference<Compilation>>();
                MetadataCache.SetCSCallbacks(CompactCacheOfRetargetingAssemblies, AnyRetargetingAssembliesCached);
            }

            /// <summary>
            /// For test purposes.
            /// </summary>
            internal static bool CompactTimerIsOn
            {
                get
                {
                    return MetadataCache.CompactTimerIsOn;
                }
            }

            internal static void TriggerCacheCompact()
            {
                MetadataCache.TriggerCacheCompact();
            }

            /// <summary>
            /// Called by compactTimer.
            /// </summary>
            private static void CompactCacheOfRetargetingAssemblies()
            {
                DebuggerUtilities.CallBeforeAcquiringLock(); //see method comment

                // Do one pass through the compilationsToCompact list    
                int originalCount = -1;

                for (int current = 0; ; current++)
                {
                    // Compact compilations, one compilation per lock

                    // Lock our cache
                    lock (CacheLockObject)
                    {
                        if (originalCount == -1)
                        {
                            originalCount = CompilationsToCompact.Count;
                        }

                        if (CompilationsToCompact.Count > current)
                        {
                            Compilation compilation = CompilationsToCompact[current].GetTarget();

                            if (compilation == null)
                            {
                                // Compilation has been collected
                                CompilationsToCompact.RemoveAt(current);
                                current--;
                            }
                            else
                            {
                                // Compact cache of retargeting assemblies for this compilation.
                                var cache = compilation.OtherAssemblySymbols;
                                int count = cache.Count;

                                for (int i = 0; i < cache.Count; i++)
                                {
                                    if (cache[i].IsNull())
                                    {
                                        cache.RemoveAt(i);
                                        i--;
                                    }
                                }

                                if (cache.Count < count)
                                {
                                    cache.TrimExcess();
                                }

                                if (cache.Count == 0)
                                {
                                    // Cache for this compilation is empty,
                                    // remove it from the compilationsToCompact list
                                    CompilationsToCompact.RemoveAt(current);
                                    current--;
                                }
                            }
                        }

                        if (CompilationsToCompact.Count <= current + 1)
                        {
                            // no more compilations to process
                            if (originalCount > CompilationsToCompact.Count)
                            {
                                CompilationsToCompact.TrimExcess();
                            }

                            return;
                        }
                    }

                    Thread.Yield();
                }
            }

            private static bool AnyRetargetingAssembliesCached()
            {
                return CompilationsToCompact.Count > 0;
            }

            /// <summary>
            /// Lock and clean global Metadata caches, meant to be used for test purpose only.
            /// </summary>
            /// <remarks></remarks>
            internal static MetadataCache.CleaningCacheLock LockAndCleanCaches()
            {
                return MetadataCache.CleaningCacheLock.LockAndCleanCaches();
            }

            /// <summary>
            /// The object that must be locked for the duration of read/write operations on AssemblyManager's caches.
            /// </summary>
            internal static CommonLock CacheLockObject
            {
                get
                {
                    return MetadataCache.CacheLockObject;
                }
            }

            public static void CreateSourceAssemblyForCompilation(Compilation compilation)
            {
                DebuggerUtilities.CallBeforeAcquiringLock(); //see method comment

                // Lock our cache
                lock (CacheLockObject)
                {
                    if (compilation.lazyAssemblySymbol == null)
                    {
                        Contract.ThrowIfFalse(compilation.lazyReferencedAssembliesMap == null);
                        Contract.ThrowIfFalse(compilation.lazyReferencedModulesMap == null);

                        Contract.ThrowIfTrue(compilation.sourceAssemblyCreationReentranceFlag,
                            "CreateSourceAssemblyForCompilation - illegal reentrance");

                        compilation.sourceAssemblyCreationReentranceFlag = true;

                        try
                        {
                            Dictionary<MetadataReference, AssemblySymbol> referencedAssembliesMap;
                            Dictionary<ModuleFileReference, ModuleSymbol> referencedModulesMap;
                            SourceAssemblySymbol assemblySymbol;
                            DiagnosticBag diagnostics;

                            CreateSourceAssemblyForCompilation(
                                compilation,
                                new Version(0, 0, 0, 0),
                                out referencedAssembliesMap,
                                out referencedModulesMap,
                                out assemblySymbol,
                                out diagnostics);

                            // Make sure all three fields for Compilation are initialized at once.
                            Contract.ThrowIfNull(assemblySymbol);
                            Contract.ThrowIfNull(referencedAssembliesMap);
                            Contract.ThrowIfNull(referencedModulesMap);
                            Contract.ThrowIfNull(diagnostics);

                            compilation.lazyAssemblySymbol = assemblySymbol;
                            compilation.lazyReferencedAssembliesMap = referencedAssembliesMap;
                            compilation.lazyReferencedModulesMap = referencedModulesMap;
                            compilation.lazyAssemblyManagerDiagnostics = diagnostics;
                        }
                        finally
                        {
                            compilation.sourceAssemblyCreationReentranceFlag = false;
                        }
                    }
                    else
                    {
                        Contract.ThrowIfNull(compilation.lazyReferencedAssembliesMap);
                        Contract.ThrowIfNull(compilation.lazyReferencedModulesMap);
                    }
                }
            }

            private static void CreateSourceAssemblyForCompilation(
                Compilation compilation,
                Version assemblyVersion,
                out Dictionary<MetadataReference, AssemblySymbol> referencedAssembliesMap,
                out Dictionary<ModuleFileReference, ModuleSymbol> referencedModulesMap,
                out SourceAssemblySymbol assemblySymbol,
                out DiagnosticBag diagnostics)
            {
                var assemblies = new List<AssemblyData<AssemblySymbol>>();
                diagnostics = new DiagnosticBag();

                // Let's process our references and separate Assembly refs from addmodule.
                var moduleInfos = new List<MetadataCache.CachedModule>();
                var netModules = new List<Module>(); // To make sure the modules are not collected ahead of time.

                int compilationReferencesCount = compilation.references.Count;

                List<SourceLocation> referenceDirectiveLocations;
                var references = GetCompilationReferences(compilation, diagnostics, out referenceDirectiveLocations);
                int referencesCount = references.Count;

                // References originating from #r directives follow references supplied as arguments of the compilation.
                Debug.Assert((referenceDirectiveLocations != null ? referenceDirectiveLocations.Count : 0) == referencesCount - compilationReferencesCount);

                // value >= 0 is an index of an assembly reference
                // value < 0  is a negative value of an index for added module
                const int SkipReference = Int32.MaxValue;
                int[] referenceMap = new int[referencesCount];

                // Used to filter out duplicate references that reference the same file (resolve to the same full normalized path).
                HashSet<string> assemblyReferencesByFullPath = null;
                HashSet<string> moduleReferencesByFullPath = null;

                // Used to filter out assemblies that have exactly the same identity (CS1703 is reported if the compilation isn't interactive)
                // Maps simple name to a list of full names.
                Dictionary<string, List<AssemblyIdentity>> assemblyReferencesBySimpleName = null;
                
                for (int i = 0; i < referencesCount; i++)
                {
                    Location location = (i < compilationReferencesCount) ? NoLocation.Singleton : referenceDirectiveLocations[i - compilationReferencesCount];

                    MetadataReference reference = ResolveAssemblyNameReference(references[i], location, compilation, diagnostics);
                    if (reference == null)
                    {
                        referenceMap[i] = SkipReference;
                        continue;
                    }

                    switch (reference.Kind)
                    {
                        case ReferenceKind.AssemblyBytes:
                            {
                                var bytesAssembly = (AssemblyBytesReference)reference;

                                MetadataCache.CachedAssembly cachedAssembly;
                                Assembly assembly = MetadataCache.CreateAssemblyFromBytes(bytesAssembly, out cachedAssembly);
                                if (!TryAddAssemblyName(assembly.Identity, diagnostics, location, ref assemblyReferencesBySimpleName)) 
                                {
                                    referenceMap[i] = SkipReference;
                                    continue;
                                }

                                var asmData = new AssemblyDataForFile(cachedAssembly, bytesAssembly.EmbedInteropTypes, bytesAssembly.DocumentationProvider);

                                referenceMap[i] = assemblies.Count;
                                assemblies.Add(asmData);

                                // asmData keeps strong ref after this point
                                GC.KeepAlive(assembly);
                            }

                            break;

                        case ReferenceKind.AssemblyFile:
                            {
                                var fileAssembly = (AssemblyFileReference)reference;

                                string resolvedPath, metadataFile;
                                if (!GetMetadataFile(fileAssembly.Path, location, compilation, diagnostics, ref assemblyReferencesByFullPath,
                                    out resolvedPath, out metadataFile))
                                {
                                    referenceMap[i] = SkipReference;
                                    continue;
                                }
                                
                                try
                                {
                                    MetadataCache.CachedAssembly cachedAssembly;
                                    Assembly assembly = MetadataCache.CreateAssemblyFromFile(metadataFile, resolvedPath, compilation.MetadataFileProvider, out cachedAssembly);

                                    if (!TryAddAssemblyName(assembly.Identity, diagnostics, location, ref assemblyReferencesBySimpleName))
                                    {
                                        referenceMap[i] = SkipReference;
                                        continue;
                                    } 
                                    
                                    var asmData = new AssemblyDataForFile(cachedAssembly, fileAssembly.EmbedInteropTypes, fileAssembly.DocumentationProvider);

                                    referenceMap[i] = assemblies.Count;
                                    assemblies.Add(asmData);

                                    // asmData keeps strong ref after this point
                                    GC.KeepAlive(assembly);
                                }
                                catch (MetadataReaderException ex)
                                {
                                    if (ex.ErrorKind == MetadataReaderErrorKind.InvalidPEKind)
                                    {
                                        diagnostics.Add(ErrorCode.ERR_ImportNonAssembly, location, resolvedPath);
                                    }
                                    else 
                                    {
                                        diagnostics.Add(ErrorCode.FTL_MetadataCantOpenFile, location, resolvedPath, ex.Message);
                                    }

                                    referenceMap[i] = SkipReference;
                                }
                                catch (FileNotFoundException ex)
                                {
                                    diagnostics.Add(ErrorCode.ERR_NoMetadataFile, location, ex.FileName, location);
                                    referenceMap[i] = SkipReference;
                                }
                                catch (IOException ex)
                                {
                                    diagnostics.Add(ErrorCode.FTL_MetadataCantOpenFile, location, resolvedPath, ex.Message);
                                    referenceMap[i] = SkipReference;
                                }
                            }

                            break;

                        case ReferenceKind.ModuleFile:
                            {
                                var fileModule = (ModuleFileReference)reference;

                                Contract.ThrowIfTrue(fileModule.EmbedInteropTypes);

                                if (fileModule.Snapshot)
                                {
                                    throw new NotImplementedException();
                                }

                                string resolvedPath, metadataFile;
                                if (!GetMetadataFile(fileModule.Path, location, compilation, diagnostics, ref moduleReferencesByFullPath,
                                    out resolvedPath, out metadataFile))
                                {
                                    referenceMap[i] = SkipReference;
                                    continue;
                                }

                                try
                                {
                                    MetadataCache.CachedModule moduleInfo;
                                    Module module = MetadataCache.CreateModuleFromFile(metadataFile, out moduleInfo);

                                    referenceMap[i] = -(netModules.Count + 1);
                                    moduleInfos.Add(moduleInfo);
                                    netModules.Add(module);
                                }
                                catch (MetadataReaderException ex)
                                {
                                    if (ex.ErrorKind == MetadataReaderErrorKind.InvalidPEKind) 
                                    {
                                        diagnostics.Add(ErrorCode.ERR_AddModuleAssembly, location, resolvedPath);
                                    }
                                    else
                                    {
                                        diagnostics.Add(ErrorCode.FTL_MetadataCantOpenFile, location, resolvedPath, ex.Message);
                                    }

                                    referenceMap[i] = SkipReference;
                                }
                                catch (FileNotFoundException ex)
                                {
                                    diagnostics.Add(ErrorCode.ERR_NoMetadataFile, location, ex.FileName);
                                    referenceMap[i] = SkipReference;
                                }
                                catch (IOException ex)
                                {
                                    diagnostics.Add(ErrorCode.FTL_MetadataCantOpenFile, location, resolvedPath, ex.Message);
                                    referenceMap[i] = SkipReference;
                                }
                            }

                            break;

                        case ReferenceKind.CSharpCompilation:
                            {
                                var compilationAssembly = (CompilationReference)reference;

                                // Note, if SourceAssemblySymbol hasn't been created for 
                                // compilationAssembly.Compilation yet, we want this to happen 
                                // right now. Conveniently, this constructor will trigger creation of the 
                                // SourceAssemblySymbol.
                                var asmData = new AssemblyDataForCompilation(compilationAssembly.Compilation, compilationAssembly.EmbedInteropTypes);
                                Debug.Assert(compilationAssembly.Compilation.lazyAssemblySymbol != null);

                                if (!TryAddAssemblyName(compilationAssembly.Compilation.Assembly.Identity, diagnostics, location, ref assemblyReferencesBySimpleName))
                                {
                                    referenceMap[i] = SkipReference;
                                    continue;
                                } 

                                referenceMap[i] = assemblies.Count;
                                assemblies.Add(asmData);
                            }

                            break;

                        case ReferenceKind.AssemblyObject:
                            throw new NotImplementedException();

                        default:
                        case ReferenceKind.AssemblyName:
                            throw Contract.Unreachable;
                    }
                }

                // Produce the assembly symbol (out parameter) for the compilation
                // We need new SourceAssemblySymbol in case one of the
                // references refers back to the assembly we are building.
                assemblySymbol = new SourceAssemblySymbol(compilation, compilation.outputName, netModules, assemblyVersion);
                var assemblyBeingBuiltData = new AssemblyDataForAssemblyBeingBuilt(assemblySymbol, assemblies, moduleInfos);
                assemblies.Insert(0, assemblyBeingBuiltData);

                // Let's bind all the references
                var binder = new AssemblyBinder(assemblySymbol);
                Roslyn.Compilers.AssemblyManager.Binder<AssemblySymbol>.Binding[] bindingResult;

                bindingResult = binder.Bind(assemblySymbol, assemblies.ToArray());
                Contract.ThrowIfFalse(ReferenceEquals(bindingResult[0].AssemblySymbol, assemblySymbol));

                // Create AssemblySymbols for assemblies that can't use any existing symbols.
                var newSymbols = new List<int>();
                int corLibraryIndex = -1;

                for (int i = 0; i < bindingResult.Length; i++)
                {
                    if (bindingResult[i].IsCorLibrary)
                    {
                        Debug.Assert(corLibraryIndex == -1);
                        corLibraryIndex = i;
                    }

                    if (bindingResult[i].AssemblySymbol == null)
                    {
                        var compilationData = assemblies[i] as AssemblyDataForCompilation;

                        if (compilationData != null)
                        {
                            bindingResult[i].AssemblySymbol = new Retargeting.RetargetingAssemblySymbol(
                                                        (SourceAssemblySymbol)compilationData.Compilation.Assembly, compilationData.IsLinked);
                        }
                        else
                        {
                            var fileData = (AssemblyDataForFile)assemblies[i];
                            bindingResult[i].AssemblySymbol =
                                new PEAssemblySymbol(fileData.Assembly, fileData.DocumentationProvider, fileData.IsLinked);
                        }

                        newSymbols.Add(i);
                    }
                }

                // Setup bound references for newly created AssemblySymbols
                // This should be done after we created/found all AssemblySymbols 
                Dictionary<AssemblyIdentity, MissingAssemblySymbol> missingAssemblies = null;

                foreach (int i in newSymbols)
                {
                    var compilationData = assemblies[i] as AssemblyDataForCompilation;

                    if (compilationData != null)
                    {
                        SetupReferencesForRetargetingAssembly(bindingResult, i, ref missingAssemblies, assemblySymbol);
                    }
                    else
                    {
                        var fileData = (AssemblyDataForFile)assemblies[i];
                        SetupReferencesForFileAssembly(fileData, bindingResult, i, ref missingAssemblies, assemblySymbol);
                    }
                }

                // Setup bound references for newly created SourceAssemblySymbol
                SetupReferencesForSourceAssembly(assemblyBeingBuiltData, bindingResult, ref missingAssemblies);

                List<AssemblySymbol> linkedReferencedAssemblies = new List<AssemblySymbol>();

                // Setup CorLibrary and NoPia stuff for newly created assemblies
                AssemblySymbol corLibrary;

                if (corLibraryIndex != -1)
                {
                    corLibrary = bindingResult[corLibraryIndex].AssemblySymbol;
                }
                else
                {
                    if (MissingCorLibrary == null)
                    {
                        MissingCorLibrary = new MissingCorLibrarySymbol();
                    }

                    corLibrary = MissingCorLibrary;
                }

                foreach (int i in newSymbols)
                {
                    if (assemblies[i].ContainsNoPiaLocalTypes)
                    {
                        bindingResult[i].AssemblySymbol.SetNoPiaResolutionAssemblies(
                            assemblySymbol.Modules[0].GetReferencedAssemblySymbols());
                    }

                    // Setup linked referenced assemblies.
                    linkedReferencedAssemblies.Clear();

                    if (assemblies[i].IsLinked)
                    {
                        linkedReferencedAssemblies.Add(bindingResult[i].AssemblySymbol);
                    }

                    for (int k = 0; k < bindingResult[i].ReferenceBinding.Length; k++)
                    {
                        if (bindingResult[i].ReferenceBinding[k] > -1 &&
                            assemblies[bindingResult[i].ReferenceBinding[k]].IsLinked)
                        {
                            linkedReferencedAssemblies.Add(
                                bindingResult[bindingResult[i].ReferenceBinding[k]].AssemblySymbol);
                        }
                    }

                    if (linkedReferencedAssemblies.Count > 0)
                    {
                        bindingResult[i].AssemblySymbol.SetLinkedReferencedAssemblies(
                            ReadOnlyArray<AssemblySymbol>.CreateFrom(linkedReferencedAssemblies.Distinct()));
                    }

                    bindingResult[i].AssemblySymbol.SetCorLibrary(corLibrary);
                }

                assemblySymbol.SetCorLibrary(corLibrary);

                if (missingAssemblies != null)
                {
                    foreach (var missingAssembly in missingAssemblies.Values)
                    {
                        missingAssembly.SetCorLibrary(corLibrary);
                    }
                }

                // Add new assemblies into the cache
                foreach (int i in newSymbols)
                {
                    var compilationData = assemblies[i] as AssemblyDataForCompilation;

                    if (compilationData != null)
                    {
                        CacheRetargetingAssemblySymbol(compilationData.Compilation,
                                (Retargeting.RetargetingAssemblySymbol)bindingResult[i].AssemblySymbol);
                    }
                    else
                    {
                        var fileData = (AssemblyDataForFile)assemblies[i];
                        fileData.CachedAssembly.AssemblySymbols.Add(
                            new WeakReference(
                                (PEAssemblySymbol)bindingResult[i].AssemblySymbol));
                    }
                }

                // Setup references for the compilation (out parameters)
                referencedAssembliesMap = new Dictionary<MetadataReference, AssemblySymbol>(referencesCount);
                referencedModulesMap = new Dictionary<ModuleFileReference, ModuleSymbol>(netModules.Count);

                for (int i = 0; i < referenceMap.Length; i++)
                {
                    if (referenceMap[i] == SkipReference)
                    {
                        continue;
                    }

                    if (referenceMap[i] < 0)
                    {
                        referencedModulesMap.Add(
                            (ModuleFileReference)references[i],
                            assemblySymbol.Modules[-referenceMap[i]]);
                        referencedAssembliesMap.Add(references[i], assemblySymbol);
                    }
                    else
                    {
                        referencedAssembliesMap.Add(
                            references[i],
                            assemblySymbol.Modules[0].GetReferencedAssemblySymbols()[referenceMap[i]]);
                    }
                }
            }

            private static bool TryAddPath(string fullPath, ref HashSet<string> paths)
            {
                if (paths == null)
                {
                    paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                return paths.Add(fullPath);
            }

            // Returns false if an assembly of an equivalent identity has been added previously.
            // - Both assembly names are strong (have keys) and are either equal or FX unified 
            // - Both assembly names are weak (no keys) and have the same simple name.
            private static bool TryAddAssemblyName(AssemblyIdentity identity, DiagnosticBag diagnostics, Location location,
                ref Dictionary<string, List<AssemblyIdentity>> referencesBySimpleName)
            {
                if (referencesBySimpleName == null)
                {
                    referencesBySimpleName = new Dictionary<string, List<AssemblyIdentity>>(StringComparer.OrdinalIgnoreCase);
                }

                List<AssemblyIdentity> sameSimpleNameIdentities;
                string simpleName = identity.Name;
                if (!referencesBySimpleName.TryGetValue(simpleName, out sameSimpleNameIdentities))
                {
                    referencesBySimpleName.Add(simpleName, new List<AssemblyIdentity> { identity });
                    return true;
                }

                AssemblyIdentity equivalentName = null;
                foreach (var otherName in sameSimpleNameIdentities)
                {    
                    if (identity.IsEquivalent(otherName, applyFrameworkAssemblyUnification: true))
                    {
                        equivalentName = otherName;
                        break;
                    }
                }

                if (equivalentName != null)
                {
                    if (identity.IsStrongName)
                    {
                        Debug.Assert(equivalentName.IsStrongName);

                        // TODO (tomat): We want to ignore duplicate w/o reporting errors in interactive and script.
                        // Should this be reported by csc? We have two signed assemblies whose identities are the same.
                        // Why don't we just ignore the duplicate?
                        // diagnostics.Add(ErrorCode.ERR_DuplicateImport, location, fullName);
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateImportSimple, location, identity);
                    }

                    return false;
                }
                else
                {
                    sameSimpleNameIdentities.Add(identity);
                    return true;
                }
            }

            private static IList<MetadataReference> GetCompilationReferences(Compilation compilation, DiagnosticBag diagnostics, 
                out List<SourceLocation> locations)
            {
                List<MetadataReference> references = null;
                locations = null;
                
                foreach (var referenceDirective in compilation.Declarations.ReferenceDirectives)
                {
                    if (references == null)
                    {
                        references = new List<MetadataReference>(compilation.references);
                        locations = new List<SourceLocation>();
                    }
            
                    references.Add(MetadataReference.Create(referenceDirective.File));
                    locations.Add(referenceDirective.Location);
                }

                return (IList<MetadataReference>)references ?? compilation.references;
            }

            /// <summary>
            /// If the given <paramref name="metadataReference"/> is an <see cref="AssemblyNameReference"/> 
            /// uses the <paramref name="compilation"/>'s reference resolver to translate it to a reference 
            /// of another kind that we load. Otherwise returns the <paramref name="metadataReference"/> itself.
            /// </summary>
            private static MetadataReference ResolveAssemblyNameReference(MetadataReference metadataReference, Location location, Compilation compilation, 
                DiagnosticBag diagnostics)
            {
                var assemblyName = metadataReference as AssemblyNameReference;
                if (assemblyName == null)
                {
                    return metadataReference;
                }

                MetadataReference result;
                try
                {
                    result = compilation.ReferenceResolver.ResolveAssemblyName(assemblyName);
                }
                catch (Exception)
                {
                    result = null;
                }

                if (result == null || result.Kind == ReferenceKind.AssemblyName)
                {
                    diagnostics.Add(ErrorCode.ERR_NoMetadataFile, location, assemblyName.Name);
                    return null;
                }

                return result;
            }

            /// <summary>
            /// Uses the <paramref name="compilation"/>'s reference resolver to normalize given <paramref name="path"/>.
            /// If the path returned by the resolver is relative converts it into an absolute path using the current working directory.
            /// </summary>
            private static string ResolvePath(string path, Location location, Compilation compilation, DiagnosticBag diagnostics)
            {
                string result;
                try
                {
                    result = compilation.ReferenceResolver.ResolvePath(path);
                    if (result != null)
                    {
                        result = Path.GetFullPath(result);
                    }
                }
                catch (Exception)
                {
                    result = null;
                }

                if (result == null)
                {
                    diagnostics.Add(ErrorCode.ERR_NoMetadataFile, location, path);
                    return null;
                }

                return result;
            }

            private static bool GetMetadataFile(string path, Location location, Compilation compilation, DiagnosticBag diagnostics, 
                ref HashSet<string> paths, out string resolvedPath, out string metadataFile)
            {
                resolvedPath = ResolvePath(path, location, compilation, diagnostics);
                if (resolvedPath == null)
                {
                    metadataFile = null;
                    return false;
                }

                if (!TryAddPath(resolvedPath, ref paths))
                {
                    metadataFile = null;
                    return false;
                }

                try
                {
                    metadataFile = compilation.MetadataFileProvider.ProvideFile(resolvedPath);
                }
                catch (Exception)
                {
                    diagnostics.Add(ErrorCode.ERR_NoMetadataFile, location, resolvedPath);
                    metadataFile = null;
                    return false;
                }

                return true;
            }

            private static void CacheRetargetingAssemblySymbol(Compilation compilation, Retargeting.RetargetingAssemblySymbol assembly)
            {
                compilation.OtherAssemblySymbols.Add(new WeakReference<Retargeting.RetargetingAssemblySymbol>(assembly));

                // Add compilation to the compilationsToCompact list if the OtherAssemblySymbols cache
                // was empty
                if (compilation.OtherAssemblySymbols.Count == 1)
                {
                    CompilationsToCompact.Add(new WeakReference<Compilation>(compilation));
                    MetadataCache.EnableCompactTimer();
                }
            }

            private static void SetupReferencesForRetargetingAssembly(
                Roslyn.Compilers.AssemblyManager.Binder<AssemblySymbol>.Binding[] bindingResult,
                int bindingIndex,
                ref Dictionary<AssemblyIdentity, MissingAssemblySymbol> missingAssemblies,
                SourceAssemblySymbol sourceAssembly)
            {
                var retargetingAssemblySymbol = (Retargeting.RetargetingAssemblySymbol)bindingResult[bindingIndex].AssemblySymbol;
                IList<ModuleSymbol> modules = retargetingAssemblySymbol.Modules;
                int moduleCount = modules.Count;
                int refsUsed = 0;

                for (int j = 0; j < moduleCount; j++)
                {
                    ReadOnlyArray<AssemblyIdentity> referencedAssemblies =
                        retargetingAssemblySymbol.UnderlyingAssembly.Modules[j].GetReferencedAssemblies();

                    // For source module skip underlying linked references
                    if (j == 0)
                    {
                        ReadOnlyArray<AssemblySymbol> underlyingReferencedAssemblySymbols =
                            retargetingAssemblySymbol.UnderlyingAssembly.Modules[0].GetReferencedAssemblySymbols();

                        int linkedUnderlyingReferences = 0;
                        foreach (AssemblySymbol asm in underlyingReferencedAssemblySymbols)
                        {
                            if (asm.IsLinked)
                            {
                                linkedUnderlyingReferences++;
                            }
                        }

                        if (linkedUnderlyingReferences > 0)
                        {
                            var filteredReferencedAssemblies = new AssemblyIdentity[referencedAssemblies.Count - linkedUnderlyingReferences];
                            int newIndex = 0;

                            for (int k = 0; k < underlyingReferencedAssemblySymbols.Count; k++)
                            {
                                if (!underlyingReferencedAssemblySymbols[k].IsLinked)
                                {
                                    filteredReferencedAssemblies[newIndex] = referencedAssemblies[k];
                                    newIndex++;
                                }
                            }

                            Debug.Assert(newIndex == filteredReferencedAssemblies.Length);
                            referencedAssemblies = filteredReferencedAssemblies.AsReadOnlyWrap();
                        }
                    }

                    int refsCount = referencedAssemblies.Count;
                    AssemblySymbol[] symbols = new AssemblySymbol[refsCount];

                    for (int k = 0; k < refsCount; k++)
                    {
                        int boundReference = bindingResult[bindingIndex].ReferenceBinding[refsUsed + k];
                        if (boundReference > -1)
                        {
                            symbols[k] = bindingResult[boundReference].AssemblySymbol;
                        }
                        else
                        {
                            symbols[k] = GetOrAddMissingAssemblySymbol(referencedAssemblies[k], ref missingAssemblies);
                        }
                    }

                    modules[j].SetReferences(referencedAssemblies, symbols.AsReadOnlyWrap(), sourceAssembly);

                    refsUsed += refsCount;
                }
            }

            private static MissingAssemblySymbol GetOrAddMissingAssemblySymbol(
                AssemblyIdentity assemblyIdentity,
                ref Dictionary<AssemblyIdentity, MissingAssemblySymbol> missingAssemblies)
            {
                MissingAssemblySymbol missingAssembly;

                if (missingAssemblies == null)
                {
                    missingAssemblies = new Dictionary<AssemblyIdentity, MissingAssemblySymbol>();
                }
                else if (missingAssemblies.TryGetValue(assemblyIdentity, out missingAssembly))
                {
                    return missingAssembly;
                }

                missingAssembly = new MissingAssemblySymbol(assemblyIdentity);
                missingAssemblies.Add(assemblyIdentity, missingAssembly);

                return missingAssembly;
            }

            private static void SetupReferencesForFileAssembly(
                AssemblyDataForFile fileData,
                Roslyn.Compilers.AssemblyManager.Binder<AssemblySymbol>.Binding[] bindingResult,
                int bindingIndex,
                ref Dictionary<AssemblyIdentity, 
                MissingAssemblySymbol> missingAssemblies,
                SourceAssemblySymbol sourceAssembly)
            {
                var portableExecutableAssemblySymbol = (PEAssemblySymbol)bindingResult[bindingIndex].AssemblySymbol;

                IList<ModuleSymbol> modules = portableExecutableAssemblySymbol.Modules;
                int moduleCount = modules.Count;
                int refsUsed = 0;

                for (int j = 0; j < moduleCount; j++)
                {
                    int moduleReferenceCount = fileData.CachedAssembly.ModuleReferenceCounts[j];
                    var identities = new AssemblyIdentity[moduleReferenceCount];
                    var symbols = new AssemblySymbol[moduleReferenceCount];

                    fileData.AssemblyReferences.CopyTo(refsUsed, identities, 0, moduleReferenceCount);

                    for (int k = 0; k < moduleReferenceCount; k++)
                    {
                        int boundReference = bindingResult[bindingIndex].ReferenceBinding[refsUsed + k];
                        if (boundReference > -1)
                        {
                            symbols[k] = bindingResult[boundReference].AssemblySymbol;
                        }
                        else
                        {
                            symbols[k] = GetOrAddMissingAssemblySymbol(identities[k], ref missingAssemblies);
                        }
                    }

                    modules[j].SetReferences(identities.AsReadOnlyWrap(), symbols.AsReadOnlyWrap(), sourceAssembly);

                    refsUsed += moduleReferenceCount;
                }
            }

            private static void SetupReferencesForSourceAssembly(
                AssemblyDataForAssemblyBeingBuilt assemblyBeingBuiltData,
                Roslyn.Compilers.AssemblyManager.Binder<AssemblySymbol>.Binding[] bindingResult,
                ref Dictionary<AssemblyIdentity, MissingAssemblySymbol> missingAssemblies)
            {
                var sourceAssemblySymbol = (SourceAssemblySymbol)bindingResult[0].AssemblySymbol;

                IList<ModuleSymbol> modules = sourceAssemblySymbol.Modules;
                int moduleCount = modules.Count;
                int refsUsed = 0;

                for (int j = 0; j < moduleCount; j++)
                {
                    int refsCount = assemblyBeingBuiltData.ReferencesCountForModule[j];
                    var identities = new AssemblyIdentity[refsCount];
                    var symbols = new AssemblySymbol[refsCount];

                    assemblyBeingBuiltData.AssemblyReferences.CopyTo(refsUsed, identities, 0, refsCount);

                    for (int k = 0; k < refsCount; k++)
                    {
                        int boundReference = bindingResult[0].ReferenceBinding[refsUsed + k];
                        if (boundReference > -1)
                        {
                            symbols[k] = bindingResult[boundReference].AssemblySymbol;
                        }
                        else
                        {
                            symbols[k] = GetOrAddMissingAssemblySymbol(identities[k], ref missingAssemblies);
                        }
                    }

                    modules[j].SetReferences(identities.AsReadOnlyWrap(), symbols.AsReadOnlyWrap(), sourceAssemblySymbol);

                    refsUsed += refsCount;
                }
            }

            private static int[] BindAssemblyReferences(
                ReadOnlyArray<AssemblyIdentity> referencedAssemblies,
                AssemblyData<AssemblySymbol>[] assemblies)
            {
                var boundReferences = new int[referencedAssemblies.Count];

                for (int j = 0; j < referencedAssemblies.Count; j++)
                {
                    boundReferences[j] = ResolveReferencedAssembly(referencedAssemblies[j], assemblies);
                }

                return boundReferences;
            }

            // Returns an index the reference is bound to:
            //   Index >= 0 when reference is bound to an assembly with index (I)
            //   Index = -1 When reference cannot be resolved
            private static int ResolveReferencedAssembly(
                AssemblyIdentity referencedAssembly,
                AssemblyData<AssemblySymbol>[] assemblies)
            {
                int result = -1;

                // TODO: Compiler has rather complex code to disambiguate references,
                //      see MetaDataFile:DisambiguateIdenticalReferences

                for (int i = 0; i < assemblies.Length; i++)
                {
                    AssemblyIdentity definition = assemblies[i].Identity;

                    if (AssemblyIdentity.ReferenceMatchesDefinition(referencedAssembly, definition))
                    {
                        if (result != -1)
                        {
                            throw new ArgumentException(String.Format("Two metadata references found with the same identity {0}. Did you include the file extension as part of the name you gave the compilation?", 
                                definition.GetDisplayName()));
                        }

                        result = i;
                    }
                }

                return result;
            }

            private class AssemblyDataForAssemblyBeingBuilt : AssemblyData<AssemblySymbol>
            {
                private readonly AssemblyIdentity assemblyIdentity;
                private readonly AssemblySymbol assemblySymbol;

                // assemblies referenced directly by the assembly:
                private readonly AssemblyData<AssemblySymbol>[] referencedAssemblyData;

                // all referenced assembly names including assemblies referenced by modules:
                private readonly ReadOnlyArray<AssemblyIdentity> referencedAssemblies;

                // [0] is the number of assembly names in referencedAssemblies that are direct references specified in referencedAssemblyData.
                // [i] is the number of references coming from module moduleInfo[i-1]. These names are also added to referencedAssemblies.
                private readonly int[] moduleReferenceCounts;

                public AssemblyDataForAssemblyBeingBuilt(
                    AssemblySymbol assemblySymbol,
                    List<AssemblyData<AssemblySymbol>> referencedAssemblyData,
                    List<MetadataCache.CachedModule> moduleInfos)
                {
                    Contract.ThrowIfNull(assemblySymbol);
                    Contract.ThrowIfNull(referencedAssemblyData);

                    assemblyIdentity = new AssemblyIdentity(name: assemblySymbol.BaseName);

                    this.assemblySymbol = assemblySymbol;
                    this.referencedAssemblyData = referencedAssemblyData.ToArray();

                    var refs = referencedAssemblyData.ConvertAll(asmData => asmData.Identity);

                    int count = moduleInfos.Count;
                    Contract.ThrowIfFalse(assemblySymbol.Modules.Count == count + 1);

                    moduleReferenceCounts = new int[count + 1]; // Plus one for the source module.
                    moduleReferenceCounts[0] = refs.Count;

                    // add assembly names from modules:
                    for (int i = 1; i <= count; i++)
                    {
                        Contract.ThrowIfFalse(ReferenceEquals(moduleInfos[i - 1].Module,
                                ((PEModuleSymbol)assemblySymbol.Modules[i]).Module));

                        moduleReferenceCounts[i] = moduleInfos[i - 1].AssemblyReferences.Count;
                        refs.AddRange(moduleInfos[i - 1].AssemblyReferences);
                    }

                    referencedAssemblies = ReadOnlyArray<AssemblyIdentity>.CreateFrom(refs);
                }

                public int[] ReferencesCountForModule
                {
                    get
                    {
                        return moduleReferenceCounts;
                    }
                }

                public override AssemblyIdentity Identity
                {
                    get
                    {
                        return assemblyIdentity;
                    }
                }

                public override ReadOnlyArray<AssemblyIdentity> AssemblyReferences
                {
                    get
                    {
                        return referencedAssemblies;
                    }
                }

                public override IEnumerable<AssemblySymbol> AvailableSymbols
                {
                    get
                    {
                        return new AssemblySymbol[] { assemblySymbol };
                    }
                }

                public override int[] BindAssemblyReferences(AssemblyData<AssemblySymbol>[] assemblies)
                {
                    int[] boundReferences = new int[referencedAssemblies.Count];

                    for (int i = 0; i < referencedAssemblyData.Length; i++)
                    {
                        Contract.ThrowIfFalse(ReferenceEquals(referencedAssemblyData[i], assemblies[i + 1]));
                        boundReferences[i] = i + 1;
                    }

                    for (int i = referencedAssemblyData.Length; i < referencedAssemblies.Count; i++)
                    {
                        boundReferences[i] = ResolveReferencedAssembly(referencedAssemblies[i], assemblies);
                    }

                    return boundReferences;
                }

                public override bool IsMatchingAssembly(AssemblySymbol assembly)
                {
                    return ReferenceEquals(assembly, assemblySymbol);
                }

                public override bool ContainsNoPiaLocalTypes
                {
                    get
                    {
                        return false;
                    }
                }

                public override bool IsLinked
                {
                    get
                    {
                        return false;
                    }
                }

                public override bool DeclaresTheObjectClass
                {
                    get
                    {
                        return false;
                    }
                }
            }

            private abstract class AssemblyDataForMetadataOrCompilation : AssemblyData<AssemblySymbol>
            {
                private List<AssemblySymbol> assemblies;
                protected AssemblyIdentity assemblyIdentity;
                protected ReadOnlyArray<AssemblyIdentity> referencedAssemblies;
                protected readonly bool EmbedInteropTypes;

                protected AssemblyDataForMetadataOrCompilation(bool embedInteropTypes)
                {
                    this.EmbedInteropTypes = embedInteropTypes;
                }

                public override AssemblyIdentity Identity
                {
                    get
                    {
                        return assemblyIdentity;
                    }
                }

                public override IEnumerable<AssemblySymbol> AvailableSymbols
                {
                    get
                    {
                        if (assemblies == null)
                        {
                            assemblies = new List<AssemblySymbol>();

                            // This should be done lazy because while we creating
                            // instances of this type, creation of new SourceAssembly symbols
                            // might change the set of available AssemblySymbols.
                            PopulateAssembliesList(assemblies);
                        }

                        return assemblies;
                    }
                }

                protected abstract void PopulateAssembliesList(List<AssemblySymbol> assemblies);

                public override ReadOnlyArray<AssemblyIdentity> AssemblyReferences
                {
                    get
                    {
                        return referencedAssemblies;
                    }
                }

                public override int[] BindAssemblyReferences(AssemblyData<AssemblySymbol>[] assemblies)
                {
                    return AssemblyManager.BindAssemblyReferences(referencedAssemblies, assemblies);
                }

                public sealed override bool IsLinked
                {
                    get
                    {
                        return EmbedInteropTypes;
                    }
                }
            }

            private class AssemblyDataForFile : AssemblyDataForMetadataOrCompilation
            {
                private readonly Assembly assembly;
                private readonly MetadataCache.CachedAssembly cachedAssembly;
                private readonly IMetadataDocumentationProvider documentationProvider;

                public Assembly Assembly
                {
                    get
                    {
                        return this.assembly;
                    }
                }

                public MetadataCache.CachedAssembly CachedAssembly
                {
                    get
                    {
                        return this.cachedAssembly;
                    }
                }

                public IMetadataDocumentationProvider DocumentationProvider
                {
                    get
                    {
                        return this.documentationProvider;
                    }
                }

                public AssemblyDataForFile(MetadataCache.CachedAssembly cachedAssembly, bool embedInteropTypes, IMetadataDocumentationProvider documentationProvider)
                    : base(embedInteropTypes)
                {
                    Contract.ThrowIfNull(cachedAssembly);
                    Contract.ThrowIfNull(documentationProvider);
                    this.cachedAssembly = cachedAssembly;
                    this.assembly = cachedAssembly.Assembly;
                    this.documentationProvider = documentationProvider;
                    Contract.ThrowIfNull(this.assembly);

                    assemblyIdentity = cachedAssembly.Identity;
                    referencedAssemblies = cachedAssembly.AssemblyReferences;
                }

                protected override void PopulateAssembliesList(List<AssemblySymbol> assemblies)
                {
                    List<WeakReference> weakAssemblies = cachedAssembly.AssemblySymbols;
                    int count = weakAssemblies.Count;
                    int i = 0;

                    while (i < count)
                    {
                        object target = weakAssemblies[i].Target;

                        if (target == null)
                        {
                            // The AssemblySymbol has been collected 
                            weakAssemblies.RemoveAt(i);
                            count -= 1;
                        }
                        else
                        {
                            PEAssemblySymbol assembly = target as PEAssemblySymbol;

                            if (assembly != null && 
                                assembly.DocumentationProvider.Equals(DocumentationProvider))
                            {
                                assemblies.Add(assembly);
                            }

                            i += 1;
                        }
                    }
                }

                public override bool IsMatchingAssembly(AssemblySymbol candidateAssembly)
                {
                    var asm = candidateAssembly as PEAssemblySymbol;

                    return asm != null && ReferenceEquals(asm.Assembly, this.assembly);
                }

                public override bool ContainsNoPiaLocalTypes
                {
                    get
                    {
                        return assembly.ContainsNoPiaLocalTypes();
                    }
                }

                public override bool DeclaresTheObjectClass
                {
                    get
                    {
                        return assembly.DeclaresTheObjectClass;
                    }
                }
            }

            private class AssemblyDataForCompilation : AssemblyDataForMetadataOrCompilation
            {
                private readonly Compilation compilation;
                public Compilation Compilation
                {
                    get
                    {
                        return compilation;
                    }
                }

                public AssemblyDataForCompilation(Compilation compilation, bool embedInteropTypes)
                    : base(embedInteropTypes)
                {
                    Contract.ThrowIfNull(compilation);
                    this.compilation = compilation;

                    // Force creation of the SourceAssemblySymbol
                    AssemblySymbol assembly = compilation.Assembly;
                    assemblyIdentity = assembly.Identity;

                    // Collect information about references
                    var refs = ArrayBuilder<AssemblyIdentity>.GetInstance();

                    var modules = assembly.Modules;
                    int mCount = modules.Count;
                    int i;

                    // Filter out linked assemblies referenced by the source module.
                    var sourceReferencedAssemblies = modules[0].GetReferencedAssemblies();
                    var sourceReferencedAssemblySymbols = modules[0].GetReferencedAssemblySymbols();
                    int rCount = sourceReferencedAssemblies.Count;

                    Debug.Assert(rCount == sourceReferencedAssemblySymbols.Count);

                    for (i = 0; i < rCount; i++)
                    {
                        if (!sourceReferencedAssemblySymbols[i].IsLinked)
                        {
                            refs.Add(sourceReferencedAssemblies[i]);
                        }
                    }

                    for (i = 1; i < mCount; i++)
                    {
                        refs.AddRange(modules[i].GetReferencedAssemblies());
                    }

                    referencedAssemblies = refs.ToReadOnlyAndFree();
                }

                protected override void PopulateAssembliesList(List<AssemblySymbol> assemblies)
                {
                    assemblies.Add(compilation.Assembly);

                    List<WeakReference<Retargeting.RetargetingAssemblySymbol>> weakAssemblies = compilation.OtherAssemblySymbols;
                    int count = weakAssemblies.Count;
                    bool trim = false;
                    int i = 0;

                    while (i < count)
                    {
                        AssemblySymbol assembly = weakAssemblies[i].GetTarget();
                        if (assembly == null)
                        {
                            // The AssemblySymbol has been collected 
                            weakAssemblies.RemoveAt(i);
                            trim = true;
                            count -= 1;
                        }
                        else
                        {
                            assemblies.Add(assembly);
                            i += 1;
                        }
                    }

                    if (trim)
                    {
                        weakAssemblies.TrimExcess();
                    }
                }

                public override bool IsMatchingAssembly(AssemblySymbol candidateAssembly)
                {
                    var retargeting = candidateAssembly as Retargeting.RetargetingAssemblySymbol;
                    AssemblySymbol asm;

                    if (retargeting != null)
                    {
                        asm = retargeting.UnderlyingAssembly;
                    }
                    else
                    {
                        asm = candidateAssembly as SourceAssemblySymbol;
                    }

                    Contract.ThrowIfTrue(asm is Retargeting.RetargetingAssemblySymbol);

                    return ReferenceEquals(asm, compilation.Assembly);
                }

                public override bool ContainsNoPiaLocalTypes
                {
                    get
                    {
                        return compilation.MightContainNoPiaLocalTypes();
                    }
                }

                public override bool DeclaresTheObjectClass
                {
                    get
                    {
                        return compilation.DeclaresTheObjectClass;
                    }
                }
            }

            private class AssemblyBinder
                : Roslyn.Compilers.AssemblyManager.Binder<AssemblySymbol>
            {
                private readonly AssemblySymbol assemblyBeingBuilt;

                public AssemblyBinder(AssemblySymbol assemblyBeingBuilt)
                {
                    this.assemblyBeingBuilt = assemblyBeingBuilt;
                }

                protected override AssemblySymbol[] GetActualBoundReferencesUsedBy(AssemblySymbol assemblySymbol)
                {
                    Contract.ThrowIfTrue(ReferenceEquals(assemblySymbol, assemblyBeingBuilt));

                    var refs = new List<AssemblySymbol>();

                    foreach (var module in assemblySymbol.Modules)
                    {
                        refs.AddRange(module.GetReferencedAssemblySymbols());
                    }

                    for (int i = 0; i < refs.Count; i++)
                    {
                        if (refs[i].IsMissing)
                        {
                            refs[i] = null; // Do not expose missing assembly symbols to AssemblyManager.Binder
                        }
                    }

                    return refs.ToArray();
                }

                protected override ReadOnlyArray<AssemblySymbol> GetNoPiaResolutionAssemblies(AssemblySymbol candidateAssembly)
                {
                    return candidateAssembly.GetNoPiaResolutionAssemblies();
                }

                protected override bool IsLinked(AssemblySymbol candidateAssembly)
                {
                    return candidateAssembly.IsLinked;
                }

                protected override AssemblySymbol GetCorLibrary(AssemblySymbol candidateAssembly)
                {
                    AssemblySymbol corLibrary = candidateAssembly.CorLibrary;

                    // Do not expose missing assembly symbols to AssemblyManager.Binder
                    return corLibrary.IsMissing ? null : corLibrary;
                }
            }

            /// <summary>
            /// For testing purposes only.
            /// </summary>
            /// <param name="compilation"></param>
            /// <returns></returns>
            internal static bool IsSourceAssemblySymbolCreated(Compilation compilation)
            {
                return compilation.lazyAssemblySymbol != null;
            }

            /// <summary>
            /// For testing purposes only.
            /// </summary>
            /// <param name="compilation"></param>
            /// <returns></returns>
            internal static bool IsReferencedAssembliesMapCreated(Compilation compilation)
            {
                return compilation.lazyReferencedAssembliesMap != null;
            }

            /// <summary>
            /// For testing purposes only.
            /// </summary>
            /// <param name="compilation"></param>
            /// <returns></returns>
            internal static bool IsReferencedModulesMapCreated(Compilation compilation)
            {
                return compilation.lazyReferencedModulesMap != null;
            }
        }
    }
}
