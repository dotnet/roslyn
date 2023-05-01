// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
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
        private static readonly Lazy<IRuntimeEnvironmentFactory> s_lazyFactory = new Lazy<IRuntimeEnvironmentFactory>(RuntimeUtilities.GetRuntimeEnvironmentFactory);

        internal static IRuntimeEnvironment Create(IEnumerable<ModuleData> additionalDependencies = null)
        {
            return s_lazyFactory.Value.Create(additionalDependencies);
        }

        public static void CaptureOutput(Action action, int expectedLength, out string output, out string errorOutput)
        {
            using (var runtimeEnvironment = Create())
            {
                runtimeEnvironment.CaptureOutput(action, expectedLength, out output, out errorOutput);
            }
        }
    }

    internal readonly struct EmitOutput
    {
        internal ImmutableArray<byte> Assembly { get; }
        internal ImmutableArray<byte> Pdb { get; }

        internal EmitOutput(ImmutableArray<byte> assembly, ImmutableArray<byte> pdb)
        {
            Assembly = assembly;

            if (pdb.IsDefault)
            {
                // We didn't emit a discrete PDB file, so we'll look for an embedded PDB instead.
                using (var peReader = new PEReader(Assembly))
                {
                    DebugDirectoryEntry portablePdbEntry = peReader.ReadDebugDirectory().FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                    if (portablePdbEntry.DataSize != 0)
                    {
                        using (var embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(portablePdbEntry))
                        {
                            var mdReader = embeddedMetadataProvider.GetMetadataReader();
                            pdb = readMetadata(mdReader);
                        }
                    }
                }
            }

            Pdb = pdb;

            unsafe ImmutableArray<byte> readMetadata(MetadataReader mdReader)
            {
                var length = mdReader.MetadataLength;
                var bytes = new byte[length];
                Marshal.Copy((IntPtr)mdReader.MetadataPointer, bytes, 0, length);
                return ImmutableArray.Create(bytes);
            }
        }
    }

    internal static class RuntimeEnvironmentUtilities
    {
        private static int s_dumpCount;

        private static IEnumerable<ModuleMetadata> EnumerateModules(Metadata metadata)
        {
            return (metadata.Kind == MetadataImageKind.Assembly) ? ((AssemblyMetadata)metadata).GetModules().AsEnumerable() : SpecializedCollections.SingletonEnumerable((ModuleMetadata)metadata);
        }

        /// <summary>
        /// Emit all of the references which are not directly or indirectly a <see cref="Compilation"/> value.
        /// </summary>
        internal static void EmitReferences(Compilation compilation, HashSet<string> fullNameSet, List<ModuleData> dependencies, AssemblyIdentity corLibIdentity)
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

                var isCorLib = isManifestModule && corLibIdentity == identity;
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
                                                    inMemoryModule: true,
                                                    isCorLib);
                    }
                    else
                    {
                        moduleData = new ModuleData(module.Name,
                                                    bytes,
                                                    pdb: default(ImmutableArray<byte>),
                                                    inMemoryModule: true,
                                                    isCorLib: false);
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
            EmitOptions emitOptions)
        {
            var corLibIdentity = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly.Identity;

            // A Compilation can appear multiple times in a dependency graph as both a Compilation and as a MetadataReference
            // value.  Iterate the Compilations eagerly so they are always emitted directly and later references can re-use 
            // the value.  This gives better, and consistent, diagnostic information.
            var referencedCompilations = FindReferencedCompilations(compilation);
            var fullNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var referencedCompilation in referencedCompilations)
            {
                var emitData = EmitCompilationCore(referencedCompilation, null, diagnostics, null, emitOptions);
                if (emitData.HasValue)
                {
                    var identity = referencedCompilation.Assembly.Identity;
                    var moduleData = new ModuleData(identity,
                                                    OutputKind.DynamicallyLinkedLibrary,
                                                    emitData.Value.Assembly,
                                                    pdb: default(ImmutableArray<byte>),
                                                    inMemoryModule: true,
                                                    isCorLib: corLibIdentity == identity);
                    fullNameSet.Add(moduleData.Id.FullName);
                    dependencies.Add(moduleData);
                }
            }

            // Now that the Compilation values have been emitted, emit the non-compilation references
            foreach (var current in (new[] { compilation }).Concat(referencedCompilations))
            {
                EmitReferences(current, fullNameSet, dependencies, corLibIdentity);
            }

            return EmitCompilationCore(compilation, manifestResources, diagnostics, testData, emitOptions);
        }

        internal static EmitOutput? EmitCompilationCore(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources,
            DiagnosticBag diagnostics,
            CompilationTestData testData,
            EmitOptions emitOptions)
        {
            emitOptions ??= EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded);

            using var executableStream = new MemoryStream();

            var pdb = default(ImmutableArray<byte>);
            var assembly = default(ImmutableArray<byte>);
            var pdbStream = (emitOptions.DebugInformationFormat != DebugInformationFormat.Embedded) ? new MemoryStream() : null;

            // Note: don't forget to name the source inputs to get them embedded for debugging
            var embeddedTexts = compilation.SyntaxTrees
                .Select(t => (filePath: t.FilePath, text: t.GetText()))
                .Where(t => t.text.CanBeEmbedded && !string.IsNullOrEmpty(t.filePath))
                .Select(t => EmbeddedText.FromSource(t.filePath, t.text))
                .ToImmutableArray();

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
                    embeddedTexts,
                    rebuildData: null,
                    testData: testData,
                    cancellationToken: default);
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

        public static string DumpAssemblyData(IEnumerable<ModuleData> modules, out string dumpDirectory)
        {
            dumpDirectory = null;

            var sb = new StringBuilder();
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
                        dumpDirectory = TempRoot.Root;
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
                        AssemblyIdentity.TryParseDisplayName(module.FullName, out var identity);
                        fileName = identity.Name;
                    }

                    string pePath = Path.Combine(dumpDirectory, fileName + module.Kind.GetDefaultExtension());
                    try
                    {
                        module.Image.WriteToFile(pePath);
                    }
                    catch (ArgumentException e)
                    {
                        pePath = $"<unable to write file: '{pePath}' -- {e.Message}>";
                    }
                    catch (IOException e)
                    {
                        pePath = $"<unable to write file: '{pePath}' -- {e.Message}>";
                    }

                    string pdbPath;
                    if (!module.Pdb.IsDefaultOrEmpty)
                    {
                        pdbPath = Path.Combine(dumpDirectory, fileName + ".pdb");

                        try
                        {
                            module.Pdb.WriteToFile(pdbPath);
                        }
                        catch (ArgumentException e)
                        {
                            pdbPath = $"<unable to write file: '{pdbPath}' -- {e.Message}>";
                        }
                        catch (IOException e)
                        {
                            pdbPath = $"<unable to write file: '{pdbPath}' -- {e.Message}>";
                        }
                    }
                    else
                    {
                        pdbPath = null;
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
        int Execute(string moduleName, string[] args, string expectedOutput, bool trimOutput = true);
        ImmutableArray<byte> GetMainImage();
        ImmutableArray<byte> GetMainPdb();
        ImmutableArray<Diagnostic> GetDiagnostics();
        SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName);
        IList<ModuleData> GetAllModuleData();
        void Verify(Verification verification);
        string[] VerifyModules(string[] modulesToVerify);
        void CaptureOutput(Action action, int expectedLength, out string output, out string errorOutput);
    }

    internal interface IInternalRuntimeEnvironment
    {
        CompilationTestData GetCompilationTestData();
    }
}
