// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static readonly Dictionary<string, Guid> allModuleNames = new Dictionary<string, Guid>();

        private bool disposed;
        private AppDomain domain;
        private RuntimeAssemblyManager assemblyManager;
        private ImmutableArray<Diagnostic> lazyDiagnostics;
        private ModuleData mainModule;
        private List<ModuleData> allModuleData;
        private readonly CompilationTestData testData = new CompilationTestData();
        private readonly IEnumerable<ModuleData> additionalDependencies;
        private bool executeRequested;
        private bool peVerifyRequested;

        public HostedRuntimeEnvironment(IEnumerable<ModuleData> additionalDependencies = null)
        {
            this.additionalDependencies = additionalDependencies;
        }

        private RuntimeAssemblyManager CreateAssemblyManager(IEnumerable<ModuleData> compilationDependencies, ModuleData mainModule)
        {
            var allModules = compilationDependencies;
            if (additionalDependencies != null)
            {
                allModules = allModules.Concat(additionalDependencies);
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
            RuntimeAssemblyManager manager;

            if (conflict != null)
            {
                Type appDomainProxyType = typeof(RuntimeAssemblyManager);
                Assembly thisAssembly = appDomainProxyType.Assembly;
                this.domain = AppDomain.CreateDomain("HostedRuntimeEnvironment", null, Environment.CurrentDirectory, null, false);
                manager = (RuntimeAssemblyManager)domain.CreateInstanceAndUnwrap(thisAssembly.FullName, appDomainProxyType.FullName);
            }
            else
            {
                manager = new RuntimeAssemblyManager();
            }

            manager.AddModuleData(allModules);

            if (mainModule != null)
            {
                manager.AddMainModuleMvid(mainModule.Mvid);
            }

            return manager;
        }

        // Determines if any of the given dependencies has the same name as already loaded assembly with different content.
        private static string DetectNameCollision(IEnumerable<ModuleData> modules)
        {
            lock (allModuleNames)
            {
                foreach (var module in modules)
                {
                    Guid mvid;
                    if (allModuleNames.TryGetValue(module.FullName, out mvid))
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
                    allModuleNames[module.FullName] = module.Mvid;
                }
            }

            return null;
        }

        private static void EmitDependentCompilation(Compilation compilation, List<ModuleData> dependencies, DiagnosticBag diagnostics)
        {
            ImmutableArray<byte> assembly, pdb;
            if (EmitCompilation(compilation, null, dependencies, diagnostics, null, out assembly, out pdb))
            {
                dependencies.Add(new ModuleData(compilation.Assembly.Identity, OutputKind.DynamicallyLinkedLibrary, assembly, pdb, inMemoryModule: true));
            }
        }

        internal static void EmitReferences(Compilation compilation, List<ModuleData> dependencies, DiagnosticBag diagnostics)
        {
            if (compilation.PreviousSubmission != null)
            {
                EmitDependentCompilation(compilation.PreviousSubmission, dependencies, diagnostics);
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
                            dependencies.Add(new ModuleData(((AssemblyMetadata)metadata).GetAssembly().Identity, OutputKind.DynamicallyLinkedLibrary, bytes, pdb: default(ImmutableArray<byte>), inMemoryModule: true));
                        }
                        else
                        {
                            dependencies.Add(new ModuleData(module.Name, bytes, pdb: default(ImmutableArray<byte>), inMemoryModule: true));
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
                MemoryStream pdbStream = new MemoryStream();

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
                        testData: testData,
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

        public void Emit(Compilation mainCompilation, IEnumerable<ResourceDescription> manifestResources)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var dependencies = new List<ModuleData>();

            testData.Methods.Clear();

            ImmutableArray<byte> mainImage, mainPdb;
            bool succeeded = EmitCompilation(mainCompilation, manifestResources, dependencies, diagnostics, testData, out mainImage, out mainPdb);

            this.lazyDiagnostics = diagnostics.ToReadOnlyAndFree();

            if (succeeded)
            {
                this.mainModule = new ModuleData(mainCompilation.Assembly.Identity, mainCompilation.Options.OutputKind, mainImage, mainPdb, inMemoryModule: true);
                this.allModuleData = dependencies;
                this.allModuleData.Insert(0, mainModule);
                this.assemblyManager = CreateAssemblyManager(dependencies, mainModule);
            }
            else
            {
                string dumpDir;
                RuntimeAssemblyManager.DumpAssemblyData(dependencies, out dumpDir);

                // This method MUST throw if compilation did not succeed.  If compilation succeeded and there were errors, that is bad.
                // Please see KevinH if you intend to change this behavior as many tests expect the Exception to indicate failure.
                throw new EmitException(this.lazyDiagnostics, dumpDir); // ToArray for serializability.
            }
        }

        public int Execute(string moduleName, int expectedOutputLength, out string processOutput)
        {
            executeRequested = true;

            try
            {
                return this.assemblyManager.Execute(moduleName, expectedOutputLength, out processOutput);
            }
            catch (TargetInvocationException tie)
            {
                string dumpDir;
                assemblyManager.DumpAssemblyData(out dumpDir);
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
                assemblyManager.DumpAssemblyData(out dumpDir);
                throw new ExecutionException(expectedOutput, actualOutput, dumpDir);
            }

            return exitCode;
        }

        internal ImmutableArray<Diagnostic> GetDiagnostics()
        {
            if (this.lazyDiagnostics.IsDefault)
            {
                throw new InvalidOperationException("You must call Emit before calling GetBuffer.");
            }

            return this.lazyDiagnostics;
        }

        public ImmutableArray<byte> GetMainImage()
        {
            if (mainModule == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetMainImage.");
            }

            return mainModule.Image;
        }

        public ImmutableArray<byte> GetMainPdb()
        {
            if (mainModule == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetMainPdb.");
            }

            return mainModule.Pdb;
        }

        internal IList<ModuleData> GetAllModuleData()
        {
            if (allModuleData == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetAllModuleData.");
            }

            return allModuleData;
        }

        public void PeVerify()
        {
            peVerifyRequested = true;

            if (assemblyManager == null)
            {
                throw new InvalidOperationException("You must call Emit before calling PeVerify.");
            }

            this.assemblyManager.PeVerifyModules(new[] { this.mainModule.FullName });
        }

        internal string[] PeVerifyModules(string[] modulesToVerify, bool throwOnError = true)
        {
            peVerifyRequested = true;

            if (assemblyManager == null)
            {
                assemblyManager = CreateAssemblyManager(new ModuleData[0], null);
            }

            return assemblyManager.PeVerifyModules(modulesToVerify, throwOnError);
        }

        internal SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
        {
            if (assemblyManager == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetMemberSignaturesFromMetadata.");
            }

            return assemblyManager.GetMemberSignaturesFromMetadata(fullyQualifiedTypeName, memberName);
        }

        // A workaround for known bug DevDiv 369979 - don't unload the AppDomain if we may have loaded a module
        private bool IsSafeToUnloadDomain
        {
            get
            {
                if (assemblyManager == null)
                {
                    return true;
                }

                return !(assemblyManager.ContainsNetModules() && (peVerifyRequested || executeRequested));
            }
        }

        void IDisposable.Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (domain == null)
            {
                if (assemblyManager != null)
                {
                    assemblyManager.Dispose();

                    assemblyManager = null;
                }
            }
            else
            {
                // KevinH - I'm adding this for debugging...we seem to be getting to AppDomain.Unload when we shouldn't
                // (causing intermittant failures on the build machine).  We should never be creating a separate
                // AppDomain without its own assemblyManager.
                if (assemblyManager == null)
                {
                    throw new InvalidOperationException("assemblyManager should never be null if a remote domain was created");
                }
                else
                {
                    assemblyManager.Dispose();

                    if (IsSafeToUnloadDomain)
                    {
                        AppDomain.Unload(domain);
                    }

                    assemblyManager = null;
                }

                domain = null;
            }

            disposed = true;
        }

        internal CompilationTestData GetCompilationTestData()
        {
            if (testData.Module == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetCompilationTestData.");
            }
            return testData;
        }
    }

    internal sealed class RuntimeAssemblyManager : MarshalByRefObject, IDisposable
    {
        // Per-domain cache, contains all assemblies loaded to this app domain since the first manager was created.
        // The key is the manifest module MVID, which is unique for each distinct assembly. 
        private static readonly ConcurrentDictionary<Guid, Assembly> domainAssemblyCache;
        private static readonly ConcurrentDictionary<Guid, Assembly> domainReflectionOnlyAssemblyCache;

        // Modules managed by this manager. All such modules must have unique simple name.
        private readonly Dictionary<string, ModuleData> modules;
        // Assemblies loaded by this manager.
        private readonly HashSet<Assembly> loadedAssemblies;
        private readonly List<Guid> mainMvids;

        private bool containsNetModules;

        static RuntimeAssemblyManager()
        {
            domainAssemblyCache = new ConcurrentDictionary<Guid, Assembly>();
            domainReflectionOnlyAssemblyCache = new ConcurrentDictionary<Guid, Assembly>();
            AppDomain.CurrentDomain.AssemblyLoad += DomainAssemblyLoad;
        }

        public RuntimeAssemblyManager()
        {
            modules = new Dictionary<string, ModuleData>(StringComparer.OrdinalIgnoreCase);
            loadedAssemblies = new HashSet<Assembly>();
            mainMvids = new List<Guid>();

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

            foreach (var assembly in loadedAssemblies)
            {
                assembly.ModuleResolve -= ModuleResolve;
            }

            //EDMAURER Some RuntimeAssemblyManagers are created via reflection in an AppDomain of our creation.
            //Sometimes those AppDomains are not released. I don't fully understand how that appdomain roots
            //a RuntimeAssemblyManager, but according to heap dumps, it does. Even though the appdomain is not
            //unloaded, its RuntimeAssemblyManager is explicitly disposed. So make sure that it cleans up this
            //memory hog - the modules dictionary.
            modules.Clear();
        }


        /// <summary>
        /// Adds given MVID into a list of module MVIDs that are considered owned by this manager.
        /// </summary>
        public void AddMainModuleMvid(Guid mvid)
        {
            mainMvids.Add(mvid);
        }

        /// <summary>
        /// True if given assembly is owned by this manager.
        /// </summary>
        private bool IsOwned(Assembly assembly)
        {
            return mainMvids.Count == 0 || mainMvids.Contains(assembly.ManifestModule.ModuleVersionId) || loadedAssemblies.Contains(assembly);
        }

        internal bool ContainsNetModules()
        {
            return containsNetModules;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void AddModuleData(IEnumerable<ModuleData> modules)
        {
            foreach (var module in modules)
            {
                if (!this.modules.ContainsKey(module.FullName))
                {
                    if (module.Kind == OutputKind.NetModule)
                    {
                        containsNetModules = true;
                    }
                    this.modules.Add(module.FullName, module);
                }
            }
        }

        private ImmutableArray<byte> GetModuleBytesByName(string moduleName)
        {
            ModuleData data;
            if (!modules.TryGetValue(moduleName, out data))
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
            var cache = assembly.ReflectionOnly ? domainReflectionOnlyAssemblyCache : domainAssemblyCache;
            cache.TryAdd(assembly.ManifestModule.ModuleVersionId, assembly);
        }

        private void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assembly = args.LoadedAssembly;

            // ModuleResolve needs to be hooked up for the main assembly once its loaded.
            // We won't get an AssemblyResolve event for the main assembly so we need to do it here.
            if (this.mainMvids.Contains(assembly.ManifestModule.ModuleVersionId) && loadedAssemblies.Add(assembly))
            {
                assembly.ModuleResolve += ModuleResolve;
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
            if (!modules.TryGetValue(fullName, out data))
            {
                return null;
            }

            ConcurrentDictionary<Guid, Assembly> cache = reflectionOnly ? domainReflectionOnlyAssemblyCache : domainAssemblyCache;

            var assembly = cache.GetOrAdd(data.Mvid, _ => LoadAsAssembly(data.Image, reflectionOnly));

            assembly.ModuleResolve += ModuleResolve;
            loadedAssemblies.Add(assembly);
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
            foreach (var module in modules) // Check inside each assembly in the compilation
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
                result = entryPoint.Invoke(null, new object[entryPoint.GetParameters().Length]);
            }, expectedOutputLength, out stdOut, out stdErr);

            output = stdOut + stdErr;
            return result is int ? (int)result : 0;
        }

        public string DumpAssemblyData(out string dumpDirectory)
        {
            return DumpAssemblyData(modules.Values, out dumpDirectory);
        }

        public static string DumpAssemblyData(IEnumerable<ModuleData> modules, out string dumpDirectory)
        {
            dumpDirectory = null;

            StringBuilder sb = new StringBuilder();
            foreach (var module in modules)
            {
                if (module.InMemoryModule)
                {
                    if (dumpDirectory == null)
                    {
                        dumpDirectory = Path.Combine(Path.GetTempPath(), "RoslynTestFailureDump", Guid.NewGuid().ToString());
                        Directory.CreateDirectory(dumpDirectory);
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
#if !(ARM)

            foreach (var name in modulesToVerify)
            {
                var module = modules[name];
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
                DumpAssemblyData(this.modules.Values, out dumpDir);
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
        private Guid? mvid;

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
                if (mvid == null)
                {
                    using (var metadata = ModuleMetadata.CreateFromImage(Image))
                    {
                        mvid = metadata.GetModuleVersionId();
                    }
                }

                return mvid.Value;
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
            info.AddValue("mvid", this.mvid, typeof(Guid?));
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
            this.mvid = (Guid?)info.GetValue("mvid", typeof(Guid?));
        }
    }

    [Serializable]
    public class EmitException : Exception
    {
        public IEnumerable<Diagnostic> Diagnostics { get; private set; }

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
}
