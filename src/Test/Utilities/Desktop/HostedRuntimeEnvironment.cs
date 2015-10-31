// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class HostedRuntimeEnvironment : IDisposable
    {
        private static readonly Dictionary<string, Guid> s_allModuleNames = new Dictionary<string, Guid>();

        private bool _disposed;
        private AppDomain _domain;
        private RuntimeAssemblyManager _assemblyManager;
        private ImmutableArray<Diagnostic> _lazyDiagnostics;
        private ModuleData _mainModule;
        private ImmutableArray<byte> _mainModulePdb;
        private List<ModuleData> _allModuleData;
        private readonly CompilationTestData _testData = new CompilationTestData();
        private readonly IEnumerable<ModuleData> _additionalDependencies;
        private bool _executeRequested;
        private bool _peVerifyRequested;

        public HostedRuntimeEnvironment(IEnumerable<ModuleData> additionalDependencies = null)
        {
            _additionalDependencies = additionalDependencies;
        }

        private void CreateAssemblyManager(IEnumerable<ModuleData> compilationDependencies, ModuleData mainModule)
        {
            var allModules = compilationDependencies;
            if (_additionalDependencies != null)
            {
                allModules = allModules.Concat(_additionalDependencies);
            }

            // We need to add the main module so that it gets checked against already loaded assembly names.
            // If an assembly is loaded directly via PEVerify(image) another assembly of the same full name
            // can't be loaded as a dependency (via Assembly.ReflectionOnlyLoad) in the same domain.
            if (mainModule != null)
            {
                allModules = allModules.Concat(new[] { mainModule });
            }

            allModules = allModules.ToArray();

            string conflict = DetectNameCollision(allModules);
            if (conflict != null && !MonoHelpers.IsRunningOnMono())
            {
                var appDomainProxyType = typeof(RuntimeAssemblyManager);
                var thisAssembly = appDomainProxyType.Assembly;

                AppDomain appDomain = null;
                RuntimeAssemblyManager manager;
                try
                {
                    appDomain = AppDomainUtils.Create("HostedRuntimeEnvironment");
                    manager = (RuntimeAssemblyManager)appDomain.CreateInstanceAndUnwrap(thisAssembly.FullName, appDomainProxyType.FullName);
                }
                catch
                {
                    if (appDomain != null)
                    {
                        AppDomain.Unload(appDomain);
                    }
                    throw;
                }

                _domain = appDomain;
                _assemblyManager = manager;
            }
            else
            {
                _assemblyManager = new RuntimeAssemblyManager();
            }

            _assemblyManager.AddModuleData(allModules);

            if (mainModule != null)
            {
                _assemblyManager.AddMainModuleMvid(mainModule.Mvid);
            }
        }

        // Determines if any of the given dependencies has the same name as already loaded assembly with different content.
        private static string DetectNameCollision(IEnumerable<ModuleData> modules)
        {
            lock (s_allModuleNames)
            {
                foreach (var module in modules)
                {
                    Guid mvid;
                    if (s_allModuleNames.TryGetValue(module.FullName, out mvid))
                    {
                        if (mvid != module.Mvid)
                        {
                            return module.FullName;
                        }
                    }
                }

                // only add new modules if there is no collision:
                foreach (var module in modules)
                {
                    s_allModuleNames[module.FullName] = module.Mvid;
                }
            }

            return null;
        }

        private static void EmitDependentCompilation(Compilation compilation,
                                                     List<ModuleData> dependencies,
                                                     DiagnosticBag diagnostics,
                                                     bool usePdbForDebugging = false)
        {
            ImmutableArray<byte> assembly, pdb;
            if (EmitCompilation(compilation, null, dependencies, diagnostics, null, out assembly, out pdb))
            {
                dependencies.Add(new ModuleData(compilation.Assembly.Identity,
                                                OutputKind.DynamicallyLinkedLibrary,
                                                assembly,
                                                pdb: usePdbForDebugging ? pdb : default(ImmutableArray<byte>),
                                                inMemoryModule: true));
            }
        }

        internal static void EmitReferences(Compilation compilation, List<ModuleData> dependencies, DiagnosticBag diagnostics)
        {
            var previousSubmission = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
            if (previousSubmission != null)
            {
                EmitDependentCompilation(previousSubmission, dependencies, diagnostics);
            }

            foreach (MetadataReference r in compilation.References)
            {
                CompilationReference compilationRef;
                PortableExecutableReference peRef;

                if ((compilationRef = r as CompilationReference) != null)
                {
                    EmitDependentCompilation(compilationRef.Compilation, dependencies, diagnostics);
                }
                else if ((peRef = r as PortableExecutableReference) != null)
                {
                    var metadata = peRef.GetMetadata();
                    bool isManifestModule = peRef.Properties.Kind == MetadataImageKind.Assembly;
                    foreach (var module in EnumerateModules(metadata))
                    {
                        ImmutableArray<byte> bytes = module.Module.PEReaderOpt.GetEntireImage().GetContent();
                        if (isManifestModule)
                        {
                            dependencies.Add(new ModuleData(((AssemblyMetadata)metadata).GetAssembly().Identity,
                                                            OutputKind.DynamicallyLinkedLibrary,
                                                            bytes,
                                                            pdb: default(ImmutableArray<byte>),
                                                            inMemoryModule: true));
                        }
                        else
                        {
                            dependencies.Add(new ModuleData(module.Name,
                                                            bytes,
                                                            pdb: default(ImmutableArray<byte>),
                                                            inMemoryModule: true));
                        }

                        isManifestModule = false;
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static IEnumerable<ModuleMetadata> EnumerateModules(Metadata metadata)
        {
            return (metadata.Kind == MetadataImageKind.Assembly) ? ((AssemblyMetadata)metadata).GetModules().AsEnumerable() : SpecializedCollections.SingletonEnumerable((ModuleMetadata)metadata);
        }

        internal static bool EmitCompilation(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources,
            List<ModuleData> dependencies,
            DiagnosticBag diagnostics,
            CompilationTestData testData,
            out ImmutableArray<byte> assembly,
            out ImmutableArray<byte> pdb
        )
        {
            assembly = default(ImmutableArray<byte>);
            pdb = default(ImmutableArray<byte>);

            EmitReferences(compilation, dependencies, diagnostics);

            using (var executableStream = new MemoryStream())
            {
                MemoryStream pdbStream = MonoHelpers.IsRunningOnMono()
                    ? null
                    : new MemoryStream();

                EmitResult result;
                try
                {
                    result = compilation.Emit(
                        executableStream,
                        pdbStream: pdbStream,
                        xmlDocumentationStream: null,
                        win32Resources: null,
                        manifestResources: manifestResources,
                        options: EmitOptions.Default,
                        debugEntryPoint: null,
                        testData: testData,
                        getHostDiagnostics: null,
                        cancellationToken: default(CancellationToken));
                }
                finally
                {
                    if (pdbStream != null)
                    {
                        pdb = pdbStream.ToImmutable();
                        pdbStream.Dispose();
                    }
                }

                diagnostics.AddRange(result.Diagnostics);
                assembly = executableStream.ToImmutable();

                return result.Success;
            }
        }

        public void Emit(
            Compilation mainCompilation,
            IEnumerable<ResourceDescription> manifestResources,
            bool usePdbForDebugging = false)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var dependencies = new List<ModuleData>();

            _testData.Methods.Clear();

            ImmutableArray<byte> mainImage, mainPdb;
            bool succeeded = EmitCompilation(mainCompilation, manifestResources, dependencies, diagnostics, _testData, out mainImage, out mainPdb);

            _lazyDiagnostics = diagnostics.ToReadOnlyAndFree();

            if (succeeded)
            {
                _mainModule = new ModuleData(mainCompilation.Assembly.Identity,
                                                 mainCompilation.Options.OutputKind,
                                                 mainImage,
                                                 pdb: usePdbForDebugging ? mainPdb : default(ImmutableArray<byte>),
                                                 inMemoryModule: true);
                _mainModulePdb = mainPdb;
                _allModuleData = dependencies;
                _allModuleData.Insert(0, _mainModule);
                CreateAssemblyManager(dependencies, _mainModule);
            }
            else
            {
                string dumpDir;
                RuntimeAssemblyManager.DumpAssemblyData(dependencies, out dumpDir);

                // This method MUST throw if compilation did not succeed.  If compilation succeeded and there were errors, that is bad.
                // Please see KevinH if you intend to change this behavior as many tests expect the Exception to indicate failure.
                throw new EmitException(_lazyDiagnostics, dumpDir); // ToArray for serializability.
            }
        }

        public int Execute(string moduleName, int expectedOutputLength, out string processOutput)
        {
            _executeRequested = true;

            try
            {
                return _assemblyManager.Execute(moduleName, expectedOutputLength, out processOutput);
            }
            catch (TargetInvocationException tie)
            {
                string dumpDir;
                _assemblyManager.DumpAssemblyData(out dumpDir);
                throw new ExecutionException(tie.InnerException, dumpDir);
            }
        }

        public int Execute(string moduleName, string expectedOutput)
        {
            string actualOutput;
            int exitCode = Execute(moduleName, expectedOutput.Length, out actualOutput);

            if (expectedOutput.Trim() != actualOutput.Trim())
            {
                string dumpDir;
                _assemblyManager.DumpAssemblyData(out dumpDir);
                throw new ExecutionException(expectedOutput, actualOutput, dumpDir);
            }

            return exitCode;
        }

        internal ImmutableArray<Diagnostic> GetDiagnostics()
        {
            if (_lazyDiagnostics.IsDefault)
            {
                throw new InvalidOperationException("You must call Emit before calling GetBuffer.");
            }

            return _lazyDiagnostics;
        }

        public ImmutableArray<byte> GetMainImage()
        {
            if (_mainModule == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetMainImage.");
            }

            return _mainModule.Image;
        }

        public ImmutableArray<byte> GetMainPdb()
        {
            if (_mainModule == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetMainPdb.");
            }

            return _mainModulePdb;
        }

        internal IList<ModuleData> GetAllModuleData()
        {
            if (_allModuleData == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetAllModuleData.");
            }

            return _allModuleData;
        }

        public void PeVerify()
        {
            _peVerifyRequested = true;

            if (_assemblyManager == null)
            {
                throw new InvalidOperationException("You must call Emit before calling PeVerify.");
            }

            _assemblyManager.PeVerifyModules(new[] { _mainModule.FullName });
        }

        internal string[] PeVerifyModules(string[] modulesToVerify, bool throwOnError = true)
        {
            _peVerifyRequested = true;

            if (_assemblyManager == null)
            {
                CreateAssemblyManager(new ModuleData[0], null);
            }

            return _assemblyManager.PeVerifyModules(modulesToVerify, throwOnError);
        }

        internal SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
        {
            if (_assemblyManager == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetMemberSignaturesFromMetadata.");
            }

            return _assemblyManager.GetMemberSignaturesFromMetadata(fullyQualifiedTypeName, memberName);
        }

        // A workaround for known bug DevDiv 369979 - don't unload the AppDomain if we may have loaded a module
        private bool IsSafeToUnloadDomain
        {
            get
            {
                if (_assemblyManager == null)
                {
                    return true;
                }

                return !(_assemblyManager.ContainsNetModules() && (_peVerifyRequested || _executeRequested));
            }
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_domain == null)
            {
                if (_assemblyManager != null)
                {
                    _assemblyManager.Dispose();
                    _assemblyManager = null;
                }
            }
            else
            {
                Debug.Assert(_assemblyManager != null);
                _assemblyManager.Dispose();

                if (IsSafeToUnloadDomain)
                {
                    AppDomain.Unload(_domain);
                }

                _assemblyManager = null;
                _domain = null;
            }

            _disposed = true;
        }

        internal CompilationTestData GetCompilationTestData()
        {
            if (_testData.Module == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetCompilationTestData.");
            }
            return _testData;
        }
    }

    internal sealed class RuntimeAssemblyManager : MarshalByRefObject, IDisposable
    {
        // Per-domain cache, contains all assemblies loaded to this app domain since the first manager was created.
        // The key is the manifest module MVID, which is unique for each distinct assembly. 
        private static readonly ConcurrentDictionary<Guid, Assembly> s_domainAssemblyCache;
        private static readonly ConcurrentDictionary<Guid, Assembly> s_domainReflectionOnlyAssemblyCache;
        private static int s_dumpCount;

        // Modules managed by this manager. All such modules must have unique simple name.
        private readonly Dictionary<string, ModuleData> _modules;
        // Assemblies loaded by this manager.
        private readonly HashSet<Assembly> _loadedAssemblies;
        private readonly List<Guid> _mainMvids;

        private bool _containsNetModules;

        static RuntimeAssemblyManager()
        {
            s_domainAssemblyCache = new ConcurrentDictionary<Guid, Assembly>();
            s_domainReflectionOnlyAssemblyCache = new ConcurrentDictionary<Guid, Assembly>();
            AppDomain.CurrentDomain.AssemblyLoad += DomainAssemblyLoad;
        }

        public RuntimeAssemblyManager()
        {
            _modules = new Dictionary<string, ModuleData>(StringComparer.OrdinalIgnoreCase);
            _loadedAssemblies = new HashSet<Assembly>();
            _mainMvids = new List<Guid>();

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoad;
            CLRHelpers.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
        }

        public void Dispose()
        {
            // clean up our handlers, so that they don't accumulate

            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoad;
            CLRHelpers.ReflectionOnlyAssemblyResolve -= ReflectionOnlyAssemblyResolve;

            foreach (var assembly in _loadedAssemblies)
            {
                if (!MonoHelpers.IsRunningOnMono())
                {
                    assembly.ModuleResolve -= ModuleResolve;
                }
            }

            //EDMAURER Some RuntimeAssemblyManagers are created via reflection in an AppDomain of our creation.
            //Sometimes those AppDomains are not released. I don't fully understand how that appdomain roots
            //a RuntimeAssemblyManager, but according to heap dumps, it does. Even though the appdomain is not
            //unloaded, its RuntimeAssemblyManager is explicitly disposed. So make sure that it cleans up this
            //memory hog - the modules dictionary.
            _modules.Clear();
        }


        /// <summary>
        /// Adds given MVID into a list of module MVIDs that are considered owned by this manager.
        /// </summary>
        public void AddMainModuleMvid(Guid mvid)
        {
            _mainMvids.Add(mvid);
        }

        /// <summary>
        /// True if given assembly is owned by this manager.
        /// </summary>
        private bool IsOwned(Assembly assembly)
        {
            return _mainMvids.Count == 0 || _mainMvids.Contains(assembly.ManifestModule.ModuleVersionId) || _loadedAssemblies.Contains(assembly);
        }

        internal bool ContainsNetModules()
        {
            return _containsNetModules;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void AddModuleData(IEnumerable<ModuleData> modules)
        {
            foreach (var module in modules)
            {
                if (!_modules.ContainsKey(module.FullName))
                {
                    if (module.Kind == OutputKind.NetModule)
                    {
                        _containsNetModules = true;
                    }
                    _modules.Add(module.FullName, module);
                }
            }
        }

        private ImmutableArray<byte> GetModuleBytesByName(string moduleName)
        {
            ModuleData data;
            if (!_modules.TryGetValue(moduleName, out data))
            {
                throw new KeyNotFoundException(String.Format("Could not find image for module '{0}'.", moduleName));
            }

            return data.Image;
        }

        private static void DomainAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            // We need to add loaded assemblies to the cache in order to avoid loading them twice.
            // This is not just optimization. CLR isn't able to load the same assembly from multiple "locations".
            // Location for byte[] assemblies is the location of the assembly that invokes Assembly.Load. 
            // PE verifier invokes load directly for the assembly being verified. If this assembly is also a dependency 
            // of another assembly we verify our AssemblyResolve is invoked. If we didn't reuse the assembly already loaded 
            // by PE verifier we would get an error from Assembly.Load.

            var assembly = args.LoadedAssembly;
            var cache = assembly.ReflectionOnly ? s_domainReflectionOnlyAssemblyCache : s_domainAssemblyCache;
            cache.TryAdd(assembly.ManifestModule.ModuleVersionId, assembly);
        }

        private void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assembly = args.LoadedAssembly;

            // ModuleResolve needs to be hooked up for the main assembly once its loaded.
            // We won't get an AssemblyResolve event for the main assembly so we need to do it here.
            if (_mainMvids.Contains(assembly.ManifestModule.ModuleVersionId) && _loadedAssemblies.Add(assembly))
            {
                if (!MonoHelpers.IsRunningOnMono())
                {
                    assembly.ModuleResolve += ModuleResolve;
                }
            }
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return AssemblyResolve(args, reflectionOnly: false);
        }

        private Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return AssemblyResolve(args, reflectionOnly: true);
        }

        private Assembly AssemblyResolve(ResolveEventArgs args, bool reflectionOnly)
        {
            // only respond to requests for dependencies of assemblies owned by this manager:
            if (IsOwned(args.RequestingAssembly))
            {
                return GetAssembly(args.Name, reflectionOnly);
            }

            return null;
        }

        /// <summary>
        /// Loads given array of bytes as an assembly image using <see cref="System.Reflection.Assembly.Load"/> or <see cref="System.Reflection.Assembly.ReflectionOnlyLoad"/>.
        /// </summary>
        internal static Assembly LoadAsAssembly(ImmutableArray<byte> rawAssembly, bool reflectionOnly = false)
        {
            Debug.Assert(!rawAssembly.IsDefault);

            byte[] bytes = rawAssembly.ToArray();

            if (reflectionOnly)
            {
                return System.Reflection.Assembly.ReflectionOnlyLoad(bytes);
            }
            else
            {
                return System.Reflection.Assembly.Load(bytes);
            }
        }

        internal Assembly GetAssembly(string fullName, bool reflectionOnly)
        {
            ModuleData data;
            if (!_modules.TryGetValue(fullName, out data))
            {
                return null;
            }

            ConcurrentDictionary<Guid, Assembly> cache = reflectionOnly ? s_domainReflectionOnlyAssemblyCache : s_domainAssemblyCache;

            var assembly = cache.GetOrAdd(data.Mvid, _ => LoadAsAssembly(data.Image, reflectionOnly));

            if (!MonoHelpers.IsRunningOnMono())
            {
                assembly.ModuleResolve += ModuleResolve;
            }

            _loadedAssemblies.Add(assembly);
            return assembly;
        }

        private Module ModuleResolve(object sender, ResolveEventArgs args)
        {
            var assembly = args.RequestingAssembly;
            var rawModule = GetModuleBytesByName(args.Name);

            Debug.Assert(assembly != null);
            Debug.Assert(!rawModule.IsDefault);

            return assembly.LoadModule(args.Name, rawModule.ToArray());
        }

        internal SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
        {
            var signatures = new SortedSet<string>();
            foreach (var module in _modules) // Check inside each assembly in the compilation
            {
                foreach (var signature in MetadataSignatureHelper.GetMemberSignatures(GetAssembly(module.Key, true),
                                                                                      fullyQualifiedTypeName, memberName))
                {
                    signatures.Add(signature);
                }
            }
            return signatures;
        }

        internal SortedSet<string> GetFullyQualifiedTypeNames(string assemblyName)
        {
            var typeNames = new SortedSet<string>();
            Assembly assembly = GetAssembly(assemblyName, true);
            foreach (var typ in assembly.GetTypes())
                typeNames.Add(typ.FullName);
            return typeNames;
        }

        public int Execute(string moduleName, int expectedOutputLength, out string output)
        {
            ImmutableArray<byte> bytes = GetModuleBytesByName(moduleName);
            Assembly assembly = LoadAsAssembly(bytes);
            MethodInfo entryPoint = assembly.EntryPoint;
            Debug.Assert(entryPoint != null, "Attempting to execute an assembly that has no entrypoint; is your test trying to execute a DLL?");

            object result = null;
            string stdOut, stdErr;
            ConsoleOutput.Capture(() =>
            {
                var count = entryPoint.GetParameters().Length;
                object[] args;
                if (count == 0)
                {
                    args = new object[0];
                }
                else if (count == 1)
                {
                    args = new object[] { new string[0] };
                }
                else
                {
                    throw new Exception("Unrecognized entry point");
                }

                result = entryPoint.Invoke(null, args);
            }, expectedOutputLength, out stdOut, out stdErr);

            output = stdOut + stdErr;
            return result is int ? (int)result : 0;
        }

        public string DumpAssemblyData(out string dumpDirectory)
        {
            return DumpAssemblyData(_modules.Values, out dumpDirectory);
        }

        public static string DumpAssemblyData(IEnumerable<ModuleData> modules, out string dumpDirectory)
        {
            dumpDirectory = null;

            StringBuilder sb = new StringBuilder();
            foreach (var module in modules)
            {
                // Limit the number of dumps to 10.  After 10 we're likely in a bad state and are 
                // dumping lots of unnecessary data.
                if (s_dumpCount > 10)
                {
                    break; 
                }

                if (module.InMemoryModule)
                {
                    Interlocked.Increment(ref s_dumpCount);

                    if (dumpDirectory == null)
                    {
                        var assemblyLocation = typeof(HostedRuntimeEnvironment).Assembly.Location;
                        dumpDirectory = Path.Combine(
                            Path.GetDirectoryName(assemblyLocation),
                            "Dumps");
                        try
                        {
                             Directory.CreateDirectory(dumpDirectory);
                        }
                        catch
                        {
                            // Okay if directory already exists
                        }
                    }

                    string fileName;
                    if (module.Kind == OutputKind.NetModule)
                    {
                        fileName = module.FullName;
                    }
                    else
                    {
                        AssemblyIdentity identity;
                        AssemblyIdentity.TryParseDisplayName(module.FullName, out identity);
                        fileName = identity.Name;
                    }

                    string pePath = Path.Combine(dumpDirectory, fileName + module.Kind.GetDefaultExtension());
                    string pdbPath = (module.Pdb != null) ? pdbPath = Path.Combine(dumpDirectory, fileName + ".pdb") : null;
                    try
                    {
                        module.Image.WriteToFile(pePath);
                        if (pdbPath != null)
                        {
                            module.Pdb.WriteToFile(pdbPath);
                        }
                    }
                    catch (IOException)
                    {
                        pePath = "<unable to write file>";
                        if (pdbPath != null)
                        {
                            pdbPath = "<unable to write file>";
                        }
                    }
                    sb.Append("PE(" + module.Kind + "): ");
                    sb.AppendLine(pePath);
                    if (pdbPath != null)
                    {
                        sb.Append("PDB: ");
                        sb.AppendLine(pdbPath);
                    }
                }
            }
            return sb.ToString();
        }

        public string[] PeVerifyModules(string[] modulesToVerify, bool throwOnError = true)
        {
            // For Windows RT (ARM) THE CLRHelper.Peverify appears to not work and will exclude this 
            // for ARM testing at present.
            StringBuilder errors = new StringBuilder();
            List<string> allOutput = new List<string>();

// Disable all PEVerification due to https://github.com/dotnet/roslyn/issues/6190
#if false

            foreach (var name in modulesToVerify)
            {
                var module = _modules[name];
                string[] output = CLRHelpers.PeVerify(module.Image);
                if (output.Length > 0)
                {
                    if (modulesToVerify.Length > 1)
                    {
                        errors.AppendLine();
                        errors.AppendLine("<<" + name + ">>");
                        errors.AppendLine();
                    }

                    foreach (var error in output)
                    {
                        errors.AppendLine(error);
                    }
                }

                if (!throwOnError)
                {
                    allOutput.AddRange(output);
                }
            }

            if (throwOnError && errors.Length > 0)
            {
                string dumpDir;
                DumpAssemblyData(_modules.Values, out dumpDir);
                throw new PeVerifyException(errors.ToString(), dumpDir);
            }
#endif
            return allOutput.ToArray();
        }
    }

    public static class ModuleExtension
    {
        public static readonly string EXE = ".exe";
        public static readonly string DLL = ".dll";
        public static readonly string NETMODULE = ".netmodule";
    }

    [Serializable, DebuggerDisplay("{GetDebuggerDisplay()}")]
    public sealed class ModuleData : ISerializable
    {
        // Simple assembly name  ("foo") or module name ("bar.netmodule").
        public readonly string FullName;

        public readonly OutputKind Kind;
        public readonly ImmutableArray<byte> Image;
        public readonly ImmutableArray<byte> Pdb;
        public readonly bool InMemoryModule;
        private Guid? _mvid;

        public ModuleData(string netModuleName, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule)
        {
            this.FullName = netModuleName;
            this.Kind = OutputKind.NetModule;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
        }

        public ModuleData(AssemblyIdentity identity, OutputKind kind, ImmutableArray<byte> image, ImmutableArray<byte> pdb, bool inMemoryModule)
        {
            this.FullName = identity.GetDisplayName();
            this.Kind = kind;
            this.Image = image;
            this.Pdb = pdb;
            this.InMemoryModule = inMemoryModule;
        }

        public Guid Mvid
        {
            get
            {
                if (_mvid == null)
                {
                    using (var metadata = ModuleMetadata.CreateFromImage(Image))
                    {
                        _mvid = metadata.GetModuleVersionId();
                    }
                }

                return _mvid.Value;
            }
        }

        private string GetDebuggerDisplay()
        {
            return FullName + " {" + Mvid + "}";
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //public readonly string FullName;
            info.AddValue("FullName", this.FullName);

            //public readonly OutputKind Kind;
            info.AddValue("kind", (int)this.Kind);

            //public readonly ImmutableArray<byte> Image;
            info.AddByteArray("Image", this.Image);

            //public readonly ImmutableArray<byte> PDB;
            info.AddByteArray("PDB", this.Pdb);

            //public readonly bool InMemoryModule;
            info.AddValue("InMemoryModule", this.InMemoryModule);

            //private Guid? mvid;
            info.AddValue("mvid", _mvid, typeof(Guid?));
        }

        private ModuleData(SerializationInfo info, StreamingContext context)
        {
            //public readonly string FullName;
            this.FullName = info.GetString("FullName");

            //public readonly OutputKind Kind;
            this.Kind = (OutputKind)info.GetInt32("kind");

            //public readonly ImmutableArray<byte> Image;
            this.Image = info.GetByteArray("Image");

            //public readonly ImmutableArray<byte> PDB;
            this.Pdb = info.GetByteArray("PDB");

            //public readonly bool InMemoryModule;
            this.InMemoryModule = info.GetBoolean("InMemoryModule");

            //private Guid? mvid;
            _mvid = (Guid?)info.GetValue("mvid", typeof(Guid?));
        }
    }

    [Serializable]
    public class EmitException : Exception
    {
        public IEnumerable<Diagnostic> Diagnostics { get; }

        protected EmitException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public EmitException(IEnumerable<Diagnostic> diagnostics, string directory)
            : base(GetMessageFromResult(diagnostics, directory))
        {
            this.Diagnostics = diagnostics;
        }

        private static string GetMessageFromResult(IEnumerable<Diagnostic> diagnostics, string directory)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Emit Failed, binaries saved to: ");
            sb.AppendLine(directory);
            foreach (var d in diagnostics)
            {
                sb.AppendLine(d.ToString());
            }
            return sb.ToString();
        }
    }

    [Serializable]
    public class PeVerifyException : Exception
    {
        protected PeVerifyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public PeVerifyException(string output, string exePath) : base(GetMessageFromResult(output, exePath)) { }

        private static string GetMessageFromResult(string output, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("PeVerify failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("':");
            sb.AppendLine(output);
            return sb.ToString();
        }
    }

    public class ExecutionException : Exception
    {
        public ExecutionException(string expectedOutput, string actualOutput, string exePath) : base(GetMessageFromResult(expectedOutput, actualOutput, exePath)) { }

        public ExecutionException(Exception innerException, string exePath) : base(GetMessageFromException(innerException, exePath), innerException) { }

        private static string GetMessageFromException(Exception executionException, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Execution failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("'.");
            sb.Append("Exception: " + executionException);
            return sb.ToString();
        }

        private static string GetMessageFromResult(string expectedOutput, string actualOutput, string exePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Execution failed for assembly '");
            sb.Append(exePath);
            sb.AppendLine("'.");
            if (expectedOutput != null)
            {
                sb.Append("Expected: ");
                sb.AppendLine(expectedOutput);
                sb.Append("Actual:   ");
                sb.AppendLine(actualOutput);
            }
            else
            {
                sb.Append("Output: ");
                sb.AppendLine(actualOutput);
            }
            return sb.ToString();
        }
    }

    internal static class SerializationInfoExtensions
    {
        public static void AddArray<T>(this SerializationInfo info, string name, ImmutableArray<T> value) where T : class
        {
            // we will copy the content into an array and serialize the copy
            // we could serialize element-wise, but that would require serializing
            // name and type for every serialized element which seems worse than creating a copy.
            info.AddValue(name, value.IsDefault ? null : value.ToArray(), typeof(T[]));
        }

        public static ImmutableArray<T> GetArray<T>(this SerializationInfo info, string name) where T : class
        {
            var arr = (T[])info.GetValue(name, typeof(T[]));
            return ImmutableArray.Create<T>(arr);
        }

        public static void AddByteArray(this SerializationInfo info, string name, ImmutableArray<byte> value)
        {
            // we will copy the content into an array and serialize the copy
            // we could serialize element-wise, but that would require serializing
            // name and type for every serialized element which seems worse than creating a copy.
            info.AddValue(name, value.IsDefault ? null : value.ToArray(), typeof(byte[]));
        }

        public static ImmutableArray<byte> GetByteArray(this SerializationInfo info, string name)
        {
            var arr = (byte[])info.GetValue(name, typeof(byte[]));
            return ImmutableArray.Create<byte>(arr);
        }
    }
}
