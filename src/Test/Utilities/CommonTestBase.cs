// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Base class for all language specific tests.
    /// </summary>
    public abstract partial class CommonTestBase : TestBase
    {
        private static ImmutableArray<Emitter> LoadEmitters()
        {
            var configFileName = Path.GetFileName(Assembly.GetExecutingAssembly().Location) + ".config";
            var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), configFileName);

            if (File.Exists(configFilePath))
            {
                var assemblyConfig = XDocument.Load(configFilePath);

                var roslynUnitTestsSection = assemblyConfig.Root.Element("roslyn.unittests");

                if (roslynUnitTestsSection != null)
                {
                    var emitSection = roslynUnitTestsSection.Element("emit");

                    if (emitSection != null)
                    {
                        var methodElements = emitSection.Elements("method");
                        var builder = ImmutableArray.CreateBuilder<Emitter>(methodElements.Count());

                        foreach (var method in methodElements)
                        {
                            Emitter emitter;
                            try
                            {
                                var asm = Assembly.Load(method.Attribute("assembly").Value);
                                emitter = (Emitter)Delegate.CreateDelegate(typeof(Emitter),
                                    asm.GetType(method.Attribute("type").Value),
                                    method.Attribute("name").Value);
                            }
                            catch
                            {
                                // It is possible and OK for an emitter to fail to load.  This is in fact expected
                                // when only the Open directory is built (as is the case in Github).  When this happens
                                // the ReflectionEmitter won't be present.  In that case we use the single available
                                // Emitter
                                continue;
                            }

                            builder.Add(emitter);
                        }

                        if (builder.Count == 0)
                        {
                            throw new Exception("Unable to load any emitter");
                        }

                        return builder.ToImmutableArray();
                    }
                }
            }

            return ImmutableArray<Emitter>.Empty;
        }

        internal abstract IEnumerable<IModuleSymbol> ReferencesToModuleSymbols(IEnumerable<MetadataReference> references, MetadataImportOptions importOptions = MetadataImportOptions.Public);

        #region Emit

        protected abstract Compilation GetCompilationForEmit(
            IEnumerable<string> source,
            IEnumerable<MetadataReference> additionalRefs,
            CompilationOptions options);

        protected abstract CompilationOptions CompilationOptionsReleaseDll { get; }

        internal delegate CompilationVerifier Emitter(
            CommonTestBase test,
            Compilation compilation,
            IEnumerable<ModuleData> dependencies,
            TestEmitters emitters,
            IEnumerable<ResourceDescription> manifestResources,
            SignatureDescription[] expectedSignatures,
            string expectedOutput,
            Action<PEAssembly, TestEmitters> assemblyValidator,
            Action<IModuleSymbol, TestEmitters> symbolValidator,
            bool collectEmittedAssembly,
            bool verify);

        private static ImmutableArray<Emitter> s_emitters;

        private static ImmutableArray<Emitter> Emitters
        {
            get
            {
                if (s_emitters.IsDefault)
                {
                    s_emitters = LoadEmitters();
                }

                return s_emitters;
            }
        }

        internal CompilationVerifier CompileAndVerify(
            string source,
            IEnumerable<MetadataReference> additionalRefs = null,
            IEnumerable<ModuleData> dependencies = null,
            TestEmitters emitters = TestEmitters.All,
            Action<IModuleSymbol, TestEmitters> sourceSymbolValidator = null,
            Action<PEAssembly, TestEmitters> assemblyValidator = null,
            Action<IModuleSymbol, TestEmitters> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            CompilationOptions options = null,
            bool collectEmittedAssembly = true,
            bool verify = true)
        {
            return CompileAndVerify(
                sources: new string[] { source },
                additionalRefs: additionalRefs,
                dependencies: dependencies,
                emitters: emitters,
                sourceSymbolValidator: sourceSymbolValidator,
                assemblyValidator: assemblyValidator,
                symbolValidator: symbolValidator,
                expectedSignatures: expectedSignatures,
                expectedOutput: expectedOutput,
                options: options,
                collectEmittedAssembly: collectEmittedAssembly,
                verify: verify);
        }

        internal CompilationVerifier CompileAndVerify(
            string[] sources,
            IEnumerable<MetadataReference> additionalRefs = null,
            IEnumerable<ModuleData> dependencies = null,
            TestEmitters emitters = TestEmitters.All,
            Action<IModuleSymbol, TestEmitters> sourceSymbolValidator = null,
            Action<PEAssembly, TestEmitters> assemblyValidator = null,
            Action<IModuleSymbol, TestEmitters> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            CompilationOptions options = null,
            bool collectEmittedAssembly = true,
            bool verify = true)
        {
            if (options == null)
            {
                options = CompilationOptionsReleaseDll.WithOutputKind((expectedOutput != null) ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary);
            }

            var compilation = GetCompilationForEmit(sources, additionalRefs, options);

            return this.CompileAndVerify(
                compilation,
                null,
                dependencies,
                emitters,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                collectEmittedAssembly,
                verify);
        }

        internal CompilationVerifier CompileAndVerify(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            TestEmitters emitters = TestEmitters.All,
            Action<IModuleSymbol, TestEmitters> sourceSymbolValidator = null,
            Action<PEAssembly, TestEmitters> assemblyValidator = null,
            Action<IModuleSymbol, TestEmitters> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool collectEmittedAssembly = true,
            bool verify = true)
        {
            Assert.NotNull(compilation);

            Assert.True(expectedOutput == null ||
                (compilation.Options.OutputKind == OutputKind.ConsoleApplication || compilation.Options.OutputKind == OutputKind.WindowsApplication),
                "Compilation must be executable if output is expected.");

            if (verify)
            {
                // Unsafe code might not verify, so don't try.
                var csharpOptions = compilation.Options as CSharp.CSharpCompilationOptions;
                verify = (csharpOptions == null || !csharpOptions.AllowUnsafe);
            }

            if (sourceSymbolValidator != null)
            {
                var module = compilation.Assembly.Modules.First();
                sourceSymbolValidator(module, emitters);
            }

            if (Emitters.IsDefaultOrEmpty)
            {
                throw new InvalidOperationException(
                    @"You must specify at least one Emitter.

Example app.config:

<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <roslyn.unittests>
    <emit>
      <method assembly=""SomeAssembly"" type=""SomeClass"" name= ""SomeEmitMethod"" />
    </emit>
  </roslyn.unittests>
</configuration>");
            }

            CompilationVerifier result = null;

            foreach (var emit in Emitters)
            {
                var verifier = emit(this,
                                    compilation,
                                    dependencies,
                                    emitters,
                                    manifestResources,
                                    expectedSignatures,
                                    expectedOutput,
                                    assemblyValidator,
                                    symbolValidator,
                                    collectEmittedAssembly,
                                    verify);

                if (result == null)
                {
                    result = verifier;
                }
                else
                {
                    // only one emitter should return a verifier
                    Assert.Null(verifier);
                }
            }

            // If this fails, it means that more that all emitters failed to return a validator
            // (i.e. none thought that they were applicable for the given input parameters).
            Assert.NotNull(result);

            return result;
        }

        private static Action<T, TestEmitters> Translate<T>(Action<T> action)
        {
            if (action != null)
            {
                return (module, _) => action(module);
            }
            else
            {
                return null;
            }
        }

        internal CompilationVerifier CompileAndVerifyFieldMarshal(string source, Dictionary<string, byte[]> expectedBlobs, bool isField = true, TestEmitters emitters = TestEmitters.All)
        {
            return CompileAndVerifyFieldMarshal(
                source,
                (s, _omitted1, _omitted2) =>
                {
                    Assert.True(expectedBlobs.ContainsKey(s), "Expecting marshalling blob for " + (isField ? "field " : "parameter ") + s);
                    return expectedBlobs[s];
                },
                isField,
                emitters);
        }

        internal CompilationVerifier CompileAndVerifyFieldMarshal(string source, Func<string, PEAssembly, TestEmitters, byte[]> getExpectedBlob, bool isField = true, TestEmitters emitters = TestEmitters.All)
        {
            return CompileAndVerify(source, emitters: emitters, options: CompilationOptionsReleaseDll, assemblyValidator: (assembly, options) => MarshalAsMetadataValidator(assembly, getExpectedBlob, options, isField));
        }

        static internal void RunValidators(CompilationVerifier verifier, TestEmitters emitters, Action<PEAssembly, TestEmitters> assemblyValidator, Action<IModuleSymbol, TestEmitters> symbolValidator)
        {
            if (assemblyValidator != null)
            {
                using (var emittedMetadata = AssemblyMetadata.Create(verifier.GetAllModuleMetadata()))
                {
                    assemblyValidator(emittedMetadata.GetAssembly(), emitters);
                }
            }

            if (symbolValidator != null)
            {
                var peModuleSymbol = verifier.GetModuleSymbolForEmittedImage();
                Debug.Assert(peModuleSymbol != null);
                symbolValidator(peModuleSymbol, emitters);
            }
        }

        // The purpose of this method is simply to check that the signature of the 'Emit' method 
        // matches the 'Emitter' delegate type that it will be dynamically assigned to...
        // That is, we will catch mismatches due to changes in the signatures at compile time instead
        // of getting an opaque ArgumentException from the call to CreateDelegate.
        private static void TestEmitSignature()
        {
            Emitter emitter = Emit;
        }

        static internal CompilationVerifier Emit(
            CommonTestBase test,
            Compilation compilation,
            IEnumerable<ModuleData> dependencies,
            TestEmitters emitters,
            IEnumerable<ResourceDescription> manifestResources,
            SignatureDescription[] expectedSignatures,
            string expectedOutput,
            Action<PEAssembly, TestEmitters> assemblyValidator,
            Action<IModuleSymbol, TestEmitters> symbolValidator,
            bool collectEmittedAssembly,
            bool verify)
        {
            CompilationVerifier verifier = null;

            // We only handle CCI emit here for now...
            if (emitters != TestEmitters.RefEmit)
            {
                verifier = new CompilationVerifier(test, compilation, dependencies);

                verifier.Emit(expectedOutput, manifestResources, verify, expectedSignatures);

                // We're dual-purposing emitters here.  In this context, it
                // tells the validator the version of Emit that is calling it. 
                RunValidators(verifier, TestEmitters.CCI, assemblyValidator, symbolValidator);
            }

            return verifier;
        }

        /// <summary>
        /// Reads content of the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>Read-only binary data read from the file.</returns>
        public static ImmutableArray<byte> ReadFromFile(string path)
        {
            return ImmutableArray.Create<byte>(File.ReadAllBytes(path));
        }

        internal static void EmitILToArray(
            string ilSource,
            bool appendDefaultHeader,
            bool includePdb,
            out ImmutableArray<byte> assemblyBytes,
            out ImmutableArray<byte> pdbBytes)
        {
            string assemblyPath;
            string pdbPath;
            SharedCompilationUtils.IlasmTempAssembly(ilSource, appendDefaultHeader, includePdb, out assemblyPath, out pdbPath);

            Assert.NotNull(assemblyPath);
            Assert.Equal(pdbPath != null, includePdb);

            using (new DisposableFile(assemblyPath))
            {
                assemblyBytes = ReadFromFile(assemblyPath);
            }

            if (pdbPath != null)
            {
                using (new DisposableFile(pdbPath))
                {
                    pdbBytes = ReadFromFile(pdbPath);
                }
            }
            else
            {
                pdbBytes = default(ImmutableArray<byte>);
            }
        }

        internal static MetadataReference CompileIL(string ilSource, bool appendDefaultHeader = true, bool embedInteropTypes = false)
        {
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            EmitILToArray(ilSource, appendDefaultHeader, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            return AssemblyMetadata.CreateFromImage(assemblyBytes).GetReference(embedInteropTypes: embedInteropTypes);
        }

        internal static MetadataReference CreateReflectionEmitAssembly(Action<ModuleBuilder> create)
        {
            using (var file = new DisposableFile(extension: ".dll"))
            {
                var name = Path.GetFileName(file.Path);
                var appDomain = AppDomain.CurrentDomain;
                var assembly = appDomain.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Save, Path.GetDirectoryName(file.Path));
                var module = assembly.DefineDynamicModule(CommonTestBase.GetUniqueName(), name);
                create(module);
                assembly.Save(name);

                var image = CommonTestBase.ReadFromFile(file.Path);
                return MetadataReference.CreateFromImage(image);
            }
        }

        #endregion

        #region Compilation Creation Helpers

        protected CSharp.CSharpCompilation CreateCSharpCompilation(
            XCData code,
            CSharp.CSharpParseOptions parseOptions = null,
            CSharp.CSharpCompilationOptions compilationOptions = null,
            string assemblyName = null,
            IEnumerable<MetadataReference> referencedAssemblies = null)
        {
            return CreateCSharpCompilation(assemblyName, code, parseOptions, compilationOptions, referencedAssemblies, referencedCompilations: null);
        }

        protected CSharp.CSharpCompilation CreateCSharpCompilation(
            string assemblyName,
            XCData code,
            CSharp.CSharpParseOptions parseOptions = null,
            CSharp.CSharpCompilationOptions compilationOptions = null,
            IEnumerable<MetadataReference> referencedAssemblies = null,
            IEnumerable<Compilation> referencedCompilations = null)
        {
            return CreateCSharpCompilation(
                assemblyName,
                code.Value,
                parseOptions,
                compilationOptions,
                referencedAssemblies,
                referencedCompilations);
        }

        protected VisualBasic.VisualBasicCompilation CreateVisualBasicCompilation(
            XCData code,
            VisualBasic.VisualBasicParseOptions parseOptions = null,
            VisualBasic.VisualBasicCompilationOptions compilationOptions = null,
            string assemblyName = null,
            IEnumerable<MetadataReference> referencedAssemblies = null)
        {
            return CreateVisualBasicCompilation(assemblyName, code, parseOptions, compilationOptions, referencedAssemblies, referencedCompilations: null);
        }

        protected VisualBasic.VisualBasicCompilation CreateVisualBasicCompilation(
            string assemblyName,
            XCData code,
            VisualBasic.VisualBasicParseOptions parseOptions = null,
            VisualBasic.VisualBasicCompilationOptions compilationOptions = null,
            IEnumerable<MetadataReference> referencedAssemblies = null,
            IEnumerable<Compilation> referencedCompilations = null)
        {
            return CreateVisualBasicCompilation(
                assemblyName,
                code.Value,
                parseOptions,
                compilationOptions,
                referencedAssemblies,
                referencedCompilations);
        }

        protected CSharp.CSharpCompilation CreateCSharpCompilation(
            string code,
            CSharp.CSharpParseOptions parseOptions = null,
            CSharp.CSharpCompilationOptions compilationOptions = null,
            string assemblyName = null,
            IEnumerable<MetadataReference> referencedAssemblies = null)
        {
            return CreateCSharpCompilation(assemblyName, code, parseOptions, compilationOptions, referencedAssemblies, referencedCompilations: null);
        }

        protected CSharp.CSharpCompilation CreateCSharpCompilation(
            string assemblyName,
            string code,
            CSharp.CSharpParseOptions parseOptions = null,
            CSharp.CSharpCompilationOptions compilationOptions = null,
            IEnumerable<MetadataReference> referencedAssemblies = null,
            IEnumerable<Compilation> referencedCompilations = null)
        {
            if (assemblyName == null)
            {
                assemblyName = GetUniqueName();
            }

            if (parseOptions == null)
            {
                parseOptions = CSharp.CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None);
            }

            if (compilationOptions == null)
            {
                compilationOptions = new CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            var references = new List<MetadataReference>();
            if (referencedAssemblies == null)
            {
                references.Add(MscorlibRef);
                references.Add(SystemRef);
                references.Add(SystemCoreRef);
                //TODO: references.Add(MsCSRef);
                references.Add(SystemXmlRef);
                references.Add(SystemXmlLinqRef);
            }
            else
            {
                references.AddRange(referencedAssemblies);
            }

            AddReferencedCompilations(referencedCompilations, references);

            var tree = CSharp.SyntaxFactory.ParseSyntaxTree(code, options: parseOptions);

            return CSharp.CSharpCompilation.Create(assemblyName, new[] { tree }, references, compilationOptions);
        }

        protected VisualBasic.VisualBasicCompilation CreateVisualBasicCompilation(
            string code,
            VisualBasic.VisualBasicParseOptions parseOptions = null,
            VisualBasic.VisualBasicCompilationOptions compilationOptions = null,
            string assemblyName = null,
            IEnumerable<MetadataReference> referencedAssemblies = null)
        {
            return CreateVisualBasicCompilation(assemblyName, code, parseOptions, compilationOptions, referencedAssemblies, referencedCompilations: null);
        }

        protected VisualBasic.VisualBasicCompilation CreateVisualBasicCompilation(
            string assemblyName,
            string code,
            VisualBasic.VisualBasicParseOptions parseOptions = null,
            VisualBasic.VisualBasicCompilationOptions compilationOptions = null,
            IEnumerable<MetadataReference> referencedAssemblies = null,
            IEnumerable<Compilation> referencedCompilations = null)
        {
            if (assemblyName == null)
            {
                assemblyName = GetUniqueName();
            }

            if (parseOptions == null)
            {
                parseOptions = VisualBasic.VisualBasicParseOptions.Default;
            }

            if (compilationOptions == null)
            {
                compilationOptions = new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            var references = new List<MetadataReference>();
            if (referencedAssemblies == null)
            {
                references.Add(MscorlibRef);
                references.Add(SystemRef);
                references.Add(SystemCoreRef);
                references.Add(MsvbRef);
                references.Add(SystemXmlRef);
                references.Add(SystemXmlLinqRef);
            }
            else
            {
                references.AddRange(referencedAssemblies);
            }

            AddReferencedCompilations(referencedCompilations, references);

            var tree = VisualBasic.VisualBasicSyntaxTree.ParseText(code, options: parseOptions);

            return VisualBasic.VisualBasicCompilation.Create(assemblyName, new[] { tree }, references, compilationOptions);
        }

        private void AddReferencedCompilations(IEnumerable<Compilation> referencedCompilations, List<MetadataReference> references)
        {
            if (referencedCompilations != null)
            {
                foreach (var referencedCompilation in referencedCompilations)
                {
                    references.Add(referencedCompilation.EmitToImageReference());
                }
            }
        }

        /// <summary>
        /// Creates a reference to a single-module assembly or a standalone module stored in memory
        /// from a hex-encoded byte stream representing a gzipped assembly image.
        /// </summary>
        /// <param name="image">
        /// A string containing a hex-encoded byte stream representing a gzipped assembly image. 
        /// Hex digits are case-insensitive and can be separated by spaces or newlines.
        /// Cannot be null.
        /// </param>
        /// <param name="properties">Reference properties (extern aliases, type embedding, <see cref="MetadataImageKind"/>).</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="filePath">Optional path that describes the location of the metadata. The file doesn't need to exist on disk. The path is opaque to the compiler.</param>
        protected internal PortableExecutableReference CreateMetadataReferenceFromHexGZipImage(
            string image,
            MetadataReferenceProperties properties = default(MetadataReferenceProperties),
            DocumentationProvider documentation = null,
            string filePath = null)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            using (var compressed = new MemoryStream(SoapHexBinary.Parse(image).Value))
            using (var gzipStream = new GZipStream(compressed, CompressionMode.Decompress))
            using (var uncompressed = new MemoryStream())
            {
                gzipStream.CopyTo(uncompressed);
                uncompressed.Position = 0;
                return MetadataReference.CreateFromStream(uncompressed, properties, documentation, filePath);
            }
        }

        #endregion

        #region IL Verification

        internal abstract string VisualizeRealIL(IModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers);

        #endregion

        #region Other Helpers
        internal static ModulePropertiesForSerialization GetDefaultModulePropertiesForSerialization()
        {
            return new ModulePropertiesForSerialization(
                persistentIdentifier: default(Guid),
                fileAlignment: ModulePropertiesForSerialization.DefaultFileAlignment32Bit,
                targetRuntimeVersion: "v4.0.30319",
                platform: Platform.AnyCpu,
                trackDebugData: false,
                baseAddress: ModulePropertiesForSerialization.DefaultExeBaseAddress32Bit,
                sizeOfHeapReserve: ModulePropertiesForSerialization.DefaultSizeOfHeapReserve32Bit,
                sizeOfHeapCommit: ModulePropertiesForSerialization.DefaultSizeOfHeapCommit32Bit,
                sizeOfStackReserve: ModulePropertiesForSerialization.DefaultSizeOfStackReserve32Bit,
                sizeOfStackCommit: ModulePropertiesForSerialization.DefaultSizeOfStackCommit32Bit,
                enableHighEntropyVA: true,
                strongNameSigned: false,
                configureToExecuteInAppContainer: false,
                subsystemVersion: default(SubsystemVersion));
        }
        #endregion
    }
}
