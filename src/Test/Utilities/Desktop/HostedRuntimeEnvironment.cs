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
    public sealed class HostedRuntimeEnvironment : IDisposable
    {
        private struct EmitData
        {
            internal ImmutableArray<byte> Assembly { get; }
            internal ImmutableArray<byte> Pdb { get; }

            internal EmitData(ImmutableArray<byte> assembly, ImmutableArray<byte> pdb)
            {
                Assembly = assembly;
                Pdb = pdb;
            }
        }

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

            if (!MonoHelpers.IsRunningOnMono())
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

        /// <summary>
        /// Find all of the <see cref="Compilation"/> values reachable from this instance.
        /// </summary>
        /// <param name="compilation"></param>
        /// <returns></returns>
        private static List<Compilation> FindReferencedCompilations(Compilation original)
        {
            var list = new List<Compilation>();
            var toVisit = new Queue<Compilation>(FindDirectReferencedCompilations(original));

            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();
                if (list.Contains(current))
                {
                    continue;
                }

                list.Add(current);

                foreach (var other in FindDirectReferencedCompilations(current))
                {
                    toVisit.Enqueue(other);
                }
            }

            return list;
        }

        private static List<Compilation> FindDirectReferencedCompilations(Compilation compilation)
        {
            var list = new List<Compilation>();
            var previousCompilation = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
            if (previousCompilation != null)
            {
                list.Add(previousCompilation);
            }

            foreach (var reference in compilation.References.OfType<CompilationReference>())
            {
                list.Add(reference.Compilation);
            }

            return list;
        }

        /// <summary>
        /// Emit all of the references which are not directly or indirectly a <see cref="Compilation"/> value.
        /// </summary>
        private static void EmitReferences(Compilation compilation, HashSet<string> fullNameSet, List<ModuleData> dependencies, DiagnosticBag diagnostics)
        {
            var previousSubmission = compilation.ScriptCompilationInfo?.PreviousScriptCompilation;
            foreach (var metadataReference in compilation.References)
            {
                if (metadataReference is CompilationReference)
                {
                    continue;
                }

                var peRef = (PortableExecutableReference)metadataReference;
                var metadata = peRef.GetMetadata();
                var isManifestModule = peRef.Properties.Kind == MetadataImageKind.Assembly;
                var identity = isManifestModule
                    ? ((AssemblyMetadata)metadata).GetAssembly().Identity
                    : null;

                // If this is an indirect reference to a Compilation then it is already been emitted 
                // so no more work to be done.
                if (isManifestModule && fullNameSet.Contains(identity.GetDisplayName()))
                {
                    continue;
                }

                foreach (var module in EnumerateModules(metadata))
                {
                    ImmutableArray<byte> bytes = module.Module.PEReaderOpt.GetEntireImage().GetContent();
                    ModuleData moduleData;
                    if (isManifestModule)
                    {
                        moduleData = new ModuleData(identity,
                                                    OutputKind.DynamicallyLinkedLibrary,
                                                    bytes,
                                                    pdb: default(ImmutableArray<byte>),
                                                    inMemoryModule: true);
                    }
                    else
                    {
                        moduleData = new ModuleData(module.Name,
                                                    bytes,
                                                    pdb: default(ImmutableArray<byte>),
                                                    inMemoryModule: true);
                    }

                    dependencies.Add(moduleData);
                    isManifestModule = false;
                }
            }
        }

        private static IEnumerable<ModuleMetadata> EnumerateModules(Metadata metadata)
        {
            return (metadata.Kind == MetadataImageKind.Assembly) ? ((AssemblyMetadata)metadata).GetModules().AsEnumerable() : SpecializedCollections.SingletonEnumerable((ModuleMetadata)metadata);
        }

        private static EmitData? EmitCompilation(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources,
            List<ModuleData> dependencies,
            DiagnosticBag diagnostics,
            CompilationTestData testData
        )
        {
            // A Compilation can appear multiple times in a depnedency graph as both a Compilation and as a MetadataReference
            // value.  Iterate the Compilations eagerly so they are always emitted directly and later references can re-use 
            // the value.  This gives better, and consistent, diagostic information.
            var referencedCompilations = FindReferencedCompilations(compilation);
            var fullNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var referencedCompilation in referencedCompilations)
            {
                var emitData = EmitCompilationCore(referencedCompilation, null, diagnostics, null);
                if (emitData.HasValue)
                {
                    var moduleData = new ModuleData(referencedCompilation.Assembly.Identity,
                                                    OutputKind.DynamicallyLinkedLibrary,
                                                    emitData.Value.Assembly,
                                                    pdb: default(ImmutableArray<byte>),
                                                    inMemoryModule: true);
                    fullNameSet.Add(moduleData.Id.FullName);
                    dependencies.Add(moduleData);
                }
            }

            // Now that the Compilation values have been emitted, emit the non-compilation references
            foreach (var current in (new[] { compilation }).Concat(referencedCompilations))
            {
                EmitReferences(current, fullNameSet, dependencies, diagnostics);
            }

            return EmitCompilationCore(compilation, manifestResources, diagnostics, testData);
        }

        private static EmitData? EmitCompilationCore(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources,
            DiagnosticBag diagnostics,
            CompilationTestData testData
        )
        {
            using (var executableStream = new MemoryStream())
            {
                var pdb = default(ImmutableArray<byte>);
                var assembly = default(ImmutableArray<byte>);
                var pdbStream = MonoHelpers.IsRunningOnMono()
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

                if (result.Success)
                {
                    return new EmitData(assembly, pdb);
                }

                return null;
            }
        }

        public void Emit(
            Compilation mainCompilation,
            IEnumerable<ResourceDescription> manifestResources,
            bool usePdbForDebugging = false)
        {
            _testData.Methods.Clear();

            var diagnostics = DiagnosticBag.GetInstance();
            var dependencies = new List<ModuleData>();
            var mainData = EmitCompilation(mainCompilation, manifestResources, dependencies, diagnostics, _testData);

            _lazyDiagnostics = diagnostics.ToReadOnlyAndFree();

            if (mainData.HasValue)
            {
                var mainImage = mainData.Value.Assembly;
                var mainPdb = mainData.Value.Pdb;
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
                throw new EmitException(_lazyDiagnostics, dumpDir);
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
}
