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
using System.Reflection.PortableExecutable;
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
        internal abstract IEnumerable<IModuleSymbol> ReferencesToModuleSymbols(IEnumerable<MetadataReference> references, MetadataImportOptions importOptions = MetadataImportOptions.Public);

        #region Emit

        protected abstract Compilation GetCompilationForEmit(
            IEnumerable<string> source,
            IEnumerable<MetadataReference> additionalRefs,
            CompilationOptions options,
            ParseOptions parseOptions);

        protected abstract CompilationOptions CompilationOptionsReleaseDll { get; }

        internal CompilationVerifier CompileAndVerify(
            string source,
            IEnumerable<MetadataReference> additionalRefs = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<IModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<IModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            CompilationOptions options = null,
            ParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            bool verify = true)
        {
            return CompileAndVerify(
                sources: new string[] { source },
                additionalRefs: additionalRefs,
                dependencies: dependencies,
                sourceSymbolValidator: sourceSymbolValidator,
                assemblyValidator: assemblyValidator,
                symbolValidator: symbolValidator,
                expectedSignatures: expectedSignatures,
                expectedOutput: expectedOutput,
                options: options,
                parseOptions: parseOptions,
                emitOptions: emitOptions,
                verify: verify);
        }

        internal CompilationVerifier CompileAndVerify(
            string[] sources,
            IEnumerable<MetadataReference> additionalRefs = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<IModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<IModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            CompilationOptions options = null,
            ParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            bool verify = true)
        {
            if (options == null)
            {
                options = CompilationOptionsReleaseDll.WithOutputKind((expectedOutput != null) ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary);
            }

            var compilation = GetCompilationForEmit(sources, additionalRefs, options, parseOptions);

            return this.CompileAndVerify(
                compilation,
                null,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                emitOptions,
                verify);
        }

        internal CompilationVerifier CompileAndVerify(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<IModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<IModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            EmitOptions emitOptions = null,
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
                sourceSymbolValidator(module);
            }

            CompilationVerifier result = null;

            var verifier = Emit(compilation,
                                dependencies,
                                manifestResources,
                                expectedSignatures,
                                expectedOutput,
                                assemblyValidator,
                                symbolValidator,
                                emitOptions,
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

            // If this fails, it means that more that all emitters failed to return a validator
            // (i.e. none thought that they were applicable for the given input parameters).
            Assert.NotNull(result);

            return result;
        }

        internal CompilationVerifier CompileAndVerifyFieldMarshal(string source, Dictionary<string, byte[]> expectedBlobs, bool isField = true)
        {
            return CompileAndVerifyFieldMarshal(
                source,
                (s, _omitted1) =>
                {
                    Assert.True(expectedBlobs.ContainsKey(s), "Expecting marshalling blob for " + (isField ? "field " : "parameter ") + s);
                    return expectedBlobs[s];
                },
                isField);
        }

        internal CompilationVerifier CompileAndVerifyFieldMarshal(string source, Func<string, PEAssembly, byte[]> getExpectedBlob, bool isField = true)
        {
            return CompileAndVerify(source, options: CompilationOptionsReleaseDll, assemblyValidator: (assembly) => MetadataValidation.MarshalAsMetadataValidator(assembly, getExpectedBlob, isField));
        }

        static internal void RunValidators(CompilationVerifier verifier, Action<PEAssembly> assemblyValidator, Action<IModuleSymbol> symbolValidator)
        {
            if (assemblyValidator != null)
            {
                using (var emittedMetadata = AssemblyMetadata.Create(verifier.GetAllModuleMetadata()))
                {
                    assemblyValidator(emittedMetadata.GetAssembly());
                }
            }

            if (symbolValidator != null)
            {
                var peModuleSymbol = verifier.GetModuleSymbolForEmittedImage();
                Debug.Assert(peModuleSymbol != null);
                symbolValidator(peModuleSymbol);
            }
        }

        internal CompilationVerifier Emit(
            Compilation compilation,
            IEnumerable<ModuleData> dependencies,
            IEnumerable<ResourceDescription> manifestResources,
            SignatureDescription[] expectedSignatures,
            string expectedOutput,
            Action<PEAssembly> assemblyValidator,
            Action<IModuleSymbol> symbolValidator,
            EmitOptions emitOptions,
            bool verify)
        {
            CompilationVerifier verifier = null;

            verifier = new CompilationVerifier(this, compilation, dependencies);

            verifier.Emit(expectedOutput, manifestResources, emitOptions, verify, expectedSignatures);

            // We're dual-purposing emitters here.  In this context, it
            // tells the validator the version of Emit that is calling it. 
            RunValidators(verifier, assemblyValidator, symbolValidator);

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
            IlasmUtilities.IlasmTempAssembly(ilSource, appendDefaultHeader, includePdb, out assemblyPath, out pdbPath);

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

        internal static Cci.ModulePropertiesForSerialization GetDefaultModulePropertiesForSerialization()
        {
            return new Cci.ModulePropertiesForSerialization(
                persistentIdentifier: default(Guid),
                fileAlignment: Cci.ModulePropertiesForSerialization.DefaultFileAlignment32Bit,
                sectionAlignment: Cci.ModulePropertiesForSerialization.DefaultSectionAlignment,
                targetRuntimeVersion: "v4.0.30319",
                machine: 0,
                prefer32Bit: false,
                trackDebugData: false,
                baseAddress: Cci.ModulePropertiesForSerialization.DefaultExeBaseAddress32Bit,
                sizeOfHeapReserve: Cci.ModulePropertiesForSerialization.DefaultSizeOfHeapReserve32Bit,
                sizeOfHeapCommit: Cci.ModulePropertiesForSerialization.DefaultSizeOfHeapCommit32Bit,
                sizeOfStackReserve: Cci.ModulePropertiesForSerialization.DefaultSizeOfStackReserve32Bit,
                sizeOfStackCommit: Cci.ModulePropertiesForSerialization.DefaultSizeOfStackCommit32Bit,
                enableHighEntropyVA: true,
                strongNameSigned: false,
                configureToExecuteInAppContainer: false,
                subsystem: Subsystem.WindowsCui,
                imageCharacteristics: Characteristics.Dll,
                majorSubsystemVersion: 0,
                minorSubsystemVersion: 0,
                linkerMajorVersion: 0,
                linkerMinorVersion: 0);
        }

        #endregion

        #region Metadata Validation

        /// <summary>
        /// Creates instance of SignatureDescription for a specified member
        /// </summary>
        /// <param name="fullyQualifiedTypeName">
        /// Fully qualified type name for member
        /// Names must be in format recognized by reflection
        /// e.g. MyType{T}.MyNestedType{T, U} => MyType`1+MyNestedType`2
        /// </param>
        /// <param name="memberName">
        /// Name of member on specified type whose signature needs to be verified
        /// Names must be in format recognized by reflection
        /// e.g. For explicitly implemented member - I1{string}.Method => I1{System.String}.Method
        /// </param>
        /// <param name="expectedSignature">
        /// Baseline string for signature of specified member
        /// Skip this argument to get an error message that shows all available signatures for specified member
        /// </param>
        /// <returns>Instance of SignatureDescription for specified member</returns>
        internal static SignatureDescription Signature(string fullyQualifiedTypeName, string memberName, string expectedSignature = "")
        {
            return new SignatureDescription()
            {
                FullyQualifiedTypeName = fullyQualifiedTypeName,
                MemberName = memberName,
                ExpectedSignature = expectedSignature
            };
        }

        #endregion
    }
}
