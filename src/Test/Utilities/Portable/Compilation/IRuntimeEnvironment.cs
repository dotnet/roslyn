﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    public static class RuntimeEnvironmentFactory
    {
        private static readonly Lazy<IRuntimeEnvironmentFactory> s_lazyFactory = new Lazy<IRuntimeEnvironmentFactory>(GetFactoryImplementation);

        internal static IRuntimeEnvironment Create(IEnumerable<ModuleData> additionalDependencies = null)
        {
            return s_lazyFactory.Value.Create(additionalDependencies);
        }

        private static IRuntimeEnvironmentFactory GetFactoryImplementation()
        {
            string assemblyName;
            string typeName;
            if (CoreClrShim.IsRunningOnCoreClr)
            {
                assemblyName = "Roslyn.Test.Utilities.CoreClr";
                typeName = "Microsoft.CodeAnalysis.Test.Utilities.CodeRuntime.CoreCLRRuntimeEnvironmentFactory";
            }
            else
            {
                assemblyName = "Roslyn.Test.Utilities.Desktop";
                typeName = "Microsoft.CodeAnalysis.Test.Utilities.CodeRuntime.DesktopRuntimeEnvironmentFactory";
            }

            return RuntimeUtilities.GetFactoryImplementation<IRuntimeEnvironmentFactory>(assemblyName, typeName);
        }

        public static void CaptureOutput(Action action, int expectedLength, out string output, out string errorOutput)
        {
            using (var runtimeEnvironment = Create())
            {
                runtimeEnvironment.CaptureOutput(action, expectedLength, out output, out errorOutput);
            }
        }
    }

    internal struct EmitOutput
    {
        internal ImmutableArray<byte> Assembly { get; }
        internal ImmutableArray<byte> Pdb { get; }

        internal EmitOutput(ImmutableArray<byte> assembly, ImmutableArray<byte> pdb)
        {
            Assembly = assembly;
            Pdb = pdb;
        }
    }

    internal static class RuntimeUtilities
    {
        private static int s_dumpCount;

        private static IEnumerable<ModuleMetadata> EnumerateModules(Metadata metadata)
        {
            return (metadata.Kind == MetadataImageKind.Assembly) ? ((AssemblyMetadata)metadata).GetModules().AsEnumerable() : SpecializedCollections.SingletonEnumerable((ModuleMetadata)metadata);
        }

        /// <summary>
        /// Loads the given assembly name, assuming the same public key, culture, version,
        /// and architecture as this assembly, and uses reflection to instantiate the given
        /// type and return the value.
        /// </summary>
        internal static T GetFactoryImplementation<T>(string assemblyName, string typeName)
        {
            var thisAssemblyName = typeof(RuntimeUtilities).GetTypeInfo().Assembly.GetName();
            var name = new AssemblyName();
            name.Name = assemblyName;
            name.Version = thisAssemblyName.Version;
            name.SetPublicKey(thisAssemblyName.GetPublicKey());
            name.CultureName = thisAssemblyName.CultureName;
            name.ProcessorArchitecture = thisAssemblyName.ProcessorArchitecture;

            var assembly = Assembly.Load(name);
            var type = assembly.GetType(typeName);
            return (T)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Emit all of the references which are not directly or indirectly a <see cref="Compilation"/> value.
        /// </summary>
        internal static void EmitReferences(Compilation compilation, HashSet<string> fullNameSet, List<ModuleData> dependencies, DiagnosticBag diagnostics)
        {
            // NOTE: specifically don't need to consider previous submissions since they will always be compilations.
            foreach (var metadataReference in compilation.References)
            {
                if (metadataReference is CompilationReference)
                {
                    continue;
                }

                var peRef = (PortableExecutableReference)metadataReference;
                var metadata = peRef.GetMetadataNoCopy();
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
                        fullNameSet.Add(identity.GetDisplayName());
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

        /// <summary>
        /// Find all of the <see cref="Compilation"/> values reachable from this instance.
        /// </summary>
        /// <param name="original"></param>
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

        internal static EmitOutput? EmitCompilation(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources,
            List<ModuleData> dependencies,
            DiagnosticBag diagnostics,
            CompilationTestData testData,
            EmitOptions emitOptions
        )
        {
            // A Compilation can appear multiple times in a depnedency graph as both a Compilation and as a MetadataReference
            // value.  Iterate the Compilations eagerly so they are always emitted directly and later references can re-use 
            // the value.  This gives better, and consistent, diagostic information.
            var referencedCompilations = FindReferencedCompilations(compilation);
            var fullNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var referencedCompilation in referencedCompilations)
            {
                var emitData = EmitCompilationCore(referencedCompilation, null, diagnostics, null, emitOptions);
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

            return EmitCompilationCore(compilation, manifestResources, diagnostics, testData, emitOptions);
        }

        internal static EmitOutput? EmitCompilationCore(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources,
            DiagnosticBag diagnostics,
            CompilationTestData testData,
            EmitOptions emitOptions
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
                        metadataPEStream: null,
                        pdbStream: pdbStream,
                        xmlDocumentationStream: null,
                        win32Resources: null,
                        manifestResources: manifestResources,
                        options: emitOptions,
                        debugEntryPoint: null,
                        sourceLinkStream: null,
                        embeddedTexts: null,
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

                if (result.Success)
                {
                    return new EmitOutput(assembly, pdb);
                }

                return null;
            }
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
                        dumpDirectory = Path.GetTempPath();
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
    }

    public interface IRuntimeEnvironmentFactory
    {
        IRuntimeEnvironment Create(IEnumerable<ModuleData> additionalDependencies);
    }

    public interface IRuntimeEnvironment : IDisposable
    {
        void Emit(Compilation mainCompilation, IEnumerable<ResourceDescription> manifestResources, EmitOptions emitOptions, bool usePdbForDebugging = false);
        int Execute(string moduleName, string[] args, string expectedOutput);
        ImmutableArray<byte> GetMainImage();
        ImmutableArray<byte> GetMainPdb();
        ImmutableArray<Diagnostic> GetDiagnostics();
        SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName);
        IList<ModuleData> GetAllModuleData();
        void PeVerify();
        string[] PeVerifyModules(string[] modulesToVerify, bool throwOnError = true);
        void CaptureOutput(Action action, int expectedLength, out string output, out string errorOutput);
    }

    internal interface IInternalRuntimeEnvironment
    {
        CompilationTestData GetCompilationTestData();
    }
}
