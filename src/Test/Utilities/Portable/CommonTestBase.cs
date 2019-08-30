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
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public enum Verification
    {
        Passes = 0,
        Fails,
        Skipped
    }

    /// <summary>
    /// Base class for all language specific tests.
    /// </summary>
    public abstract partial class CommonTestBase : TestBase
    {
        #region Emit

        internal CompilationVerifier CompileAndVerifyCommon(
            Compilation compilation,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<IModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<IModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            int? expectedReturnCode = null,
            string[] args = null,
            EmitOptions emitOptions = null,
            Verification verify = Verification.Passes)
        {
            Assert.NotNull(compilation);

            Assert.True(expectedOutput == null ||
                (compilation.Options.OutputKind == OutputKind.ConsoleApplication || compilation.Options.OutputKind == OutputKind.WindowsApplication),
                "Compilation must be executable if output is expected.");

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
                                expectedReturnCode,
                                args ?? Array.Empty<string>(),
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

        internal CompilationVerifier CompileAndVerifyFieldMarshalCommon(Compilation compilation, Dictionary<string, byte[]> expectedBlobs, bool isField = true)
        {
            return CompileAndVerifyFieldMarshalCommon(
                compilation,
                (s, _omitted1) =>
                {
                    Assert.True(expectedBlobs.ContainsKey(s), "Expecting marshalling blob for " + (isField ? "field " : "parameter ") + s);
                    return expectedBlobs[s];
                },
                isField);
        }

        internal CompilationVerifier CompileAndVerifyFieldMarshalCommon(Compilation compilation, Func<string, PEAssembly, byte[]> getExpectedBlob, bool isField = true)
        {
            return CompileAndVerifyCommon(compilation, assemblyValidator: (assembly) => MetadataValidation.MarshalAsMetadataValidator(assembly, getExpectedBlob, isField));
        }

        static internal void RunValidators(CompilationVerifier verifier, Action<PEAssembly> assemblyValidator, Action<IModuleSymbol> symbolValidator)
        {
            Assert.True(assemblyValidator != null || symbolValidator != null);

            var emittedMetadata = verifier.GetMetadata();

            if (assemblyValidator != null)
            {
                Assert.Equal(MetadataImageKind.Assembly, emittedMetadata.Kind);

                var assembly = ((AssemblyMetadata)emittedMetadata).GetAssembly();
                assemblyValidator(assembly);
            }

            if (symbolValidator != null)
            {
                var reference = emittedMetadata.Kind == MetadataImageKind.Assembly
                    ? ((AssemblyMetadata)emittedMetadata).GetReference()
                    : ((ModuleMetadata)emittedMetadata).GetReference();

                var moduleSymbol = verifier.GetSymbolFromMetadata(reference, verifier.Compilation.Options.MetadataImportOptions);
                symbolValidator(moduleSymbol);
            }
        }

        internal CompilationVerifier Emit(
            Compilation compilation,
            IEnumerable<ModuleData> dependencies,
            IEnumerable<ResourceDescription> manifestResources,
            SignatureDescription[] expectedSignatures,
            string expectedOutput,
            int? expectedReturnCode,
            string[] args,
            Action<PEAssembly> assemblyValidator,
            Action<IModuleSymbol> symbolValidator,
            EmitOptions emitOptions,
            Verification verify)
        {
            var verifier = new CompilationVerifier(compilation, VisualizeRealIL, dependencies);

            verifier.Emit(expectedOutput, expectedReturnCode, args, manifestResources, emitOptions, verify, expectedSignatures);

            if (assemblyValidator != null || symbolValidator != null)
            {
                // We're dual-purposing emitters here.  In this context, it
                // tells the validator the version of Emit that is calling it. 
                RunValidators(verifier, assemblyValidator, symbolValidator);
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
            IlasmUtilities.IlasmTempAssembly(ilSource, appendDefaultHeader, includePdb, out var assemblyPath, out var pdbPath);

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

        internal static MetadataReference CompileIL(string ilSource, bool prependDefaultHeader = true, bool embedInteropTypes = false)
        {
            EmitILToArray(ilSource, prependDefaultHeader, includePdb: false, assemblyBytes: out var assemblyBytes, pdbBytes: out var pdbBytes);
            return AssemblyMetadata.CreateFromImage(assemblyBytes).GetReference(embedInteropTypes: embedInteropTypes);
        }

        internal static MetadataReference GetILModuleReference(string ilSource, bool prependDefaultHeader = true)
        {
            EmitILToArray(ilSource, prependDefaultHeader, includePdb: false, assemblyBytes: out var assemblyBytes, pdbBytes: out var pdbBytes);
            return ModuleMetadata.CreateFromImage(assemblyBytes).GetReference();
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
                parseOptions = CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Default).WithDocumentationMode(DocumentationMode.None);
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

        #endregion

        #region IL Verification

        internal abstract string VisualizeRealIL(IModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers, bool areLocalsZeroed);

        #endregion

        #region Other Helpers

        internal static Cci.ModulePropertiesForSerialization GetDefaultModulePropertiesForSerialization()
        {
            return new Cci.ModulePropertiesForSerialization(
                persistentIdentifier: default(Guid),
                corFlags: CorFlags.ILOnly,
                fileAlignment: Cci.ModulePropertiesForSerialization.DefaultFileAlignment32Bit,
                sectionAlignment: Cci.ModulePropertiesForSerialization.DefaultSectionAlignment,
                targetRuntimeVersion: "v4.0.30319",
                machine: 0,
                baseAddress: Cci.ModulePropertiesForSerialization.DefaultExeBaseAddress32Bit,
                sizeOfHeapReserve: Cci.ModulePropertiesForSerialization.DefaultSizeOfHeapReserve32Bit,
                sizeOfHeapCommit: Cci.ModulePropertiesForSerialization.DefaultSizeOfHeapCommit32Bit,
                sizeOfStackReserve: Cci.ModulePropertiesForSerialization.DefaultSizeOfStackReserve32Bit,
                sizeOfStackCommit: Cci.ModulePropertiesForSerialization.DefaultSizeOfStackCommit32Bit,
                dllCharacteristics: Compilation.GetDllCharacteristics(enableHighEntropyVA: true, configureToExecuteInAppContainer: false),
                subsystem: Subsystem.WindowsCui,
                imageCharacteristics: Characteristics.Dll,
                majorSubsystemVersion: 0,
                minorSubsystemVersion: 0,
                linkerMajorVersion: 0,
                linkerMinorVersion: 0);
        }

        internal void AssertDeclaresType(PEModuleSymbol peModule, WellKnownType type, Accessibility expectedAccessibility)
        {
            var name = MetadataTypeName.FromFullName(type.GetMetadataName());
            Assert.Equal(expectedAccessibility, peModule.LookupTopLevelMetadataType(ref name).DeclaredAccessibility);
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

        #region Operation Test Helpers
        internal static void VerifyParentOperations(SemanticModel model)
        {
            var parentMap = GetParentOperationsMap(model);

            // check parent for each child
            foreach (var (child, parent) in parentMap)
            {
                // check parent property returns same parent we gathered by walking down operation tree
                Assert.Equal(child.Parent, parent);

                // check SearchparentOperation return same parent
                Assert.Equal(((Operation)child).SearchParentOperation(), parent);

                if (parent == null)
                {
                    // this is root of operation tree
                    VerifyOperationTreeContracts(child);
                }
            }
        }

        private static Dictionary<IOperation, IOperation> GetParentOperationsMap(SemanticModel model)
        {
            // get top operations first
            var topOperations = new HashSet<IOperation>();
            var root = model.SyntaxTree.GetRoot();

            CollectTopOperations(model, root, topOperations);

            // dig down the each operation tree to create the parent operation map
            var map = new Dictionary<IOperation, IOperation>();
            foreach (var topOperation in topOperations)
            {
                // this is top of the operation tree
                map.Add(topOperation, null);

                CollectParentOperations(topOperation, map);
            }

            return map;
        }

        private static void CollectParentOperations(IOperation operation, Dictionary<IOperation, IOperation> map)
        {
            // walk down to collect all parent operation map for this tree
            foreach (var child in operation.Children.WhereNotNull())
            {
                map.Add(child, operation);

                CollectParentOperations(child, map);
            }
        }

        private static void CollectTopOperations(SemanticModel model, SyntaxNode node, HashSet<IOperation> topOperations)
        {
            foreach (var child in node.ChildNodes())
            {
                var operation = model.GetOperation(child);
                if (operation != null)
                {
                    // found top operation
                    topOperations.Add(operation);

                    // don't dig down anymore
                    continue;
                }

                // sub tree might have the top operation
                CollectTopOperations(model, child, topOperations);
            }
        }

        internal static void VerifyClone(SemanticModel model)
        {
            foreach (var node in model.SyntaxTree.GetRoot().DescendantNodes())
            {
                var operation = model.GetOperation(node);
                if (operation == null)
                {
                    continue;
                }

                var clonedOperation = OperationCloner.CloneOperation(operation);

                Assert.Same(model, operation.SemanticModel);
                Assert.Same(model, clonedOperation.SemanticModel);
                Assert.NotSame(model, ((Operation)operation).OwningSemanticModel);
                Assert.Same(((Operation)operation).OwningSemanticModel, ((Operation)clonedOperation).OwningSemanticModel);

                // check whether cloned IOperation is same as original one
                var original = OperationTreeVerifier.GetOperationTree(model.Compilation, operation);
                var cloned = OperationTreeVerifier.GetOperationTree(model.Compilation, clonedOperation);

                Assert.Equal(original, cloned);

                // make sure cloned operation is value equal but doesn't share any IOperations
                var originalSet = new HashSet<IOperation>(operation.DescendantsAndSelf());
                var clonedSet = new HashSet<IOperation>(clonedOperation.DescendantsAndSelf());

                Assert.Equal(originalSet.Count, clonedSet.Count);
                Assert.Equal(0, originalSet.Intersect(clonedSet).Count());
            }
        }

        private static void VerifyOperationTreeContracts(IOperation root)
        {
            var semanticModel = ((Operation)root).OwningSemanticModel;
            var set = new HashSet<IOperation>(root.DescendantsAndSelf());

            foreach (var child in root.DescendantsAndSelf())
            {
                // all operations from spine should belong to the operation tree set
                VerifyOperationTreeSpine(semanticModel, set, child.Syntax);

                // operation tree's node must be part of root of semantic model which is 
                // owner of operation's lifetime
                Assert.True(semanticModel.Root.FullSpan.Contains(child.Syntax.FullSpan));
            }
        }

        private static void VerifyOperationTreeSpine(
            SemanticModel semanticModel, HashSet<IOperation> set, SyntaxNode node)
        {
            while (node != semanticModel.Root)
            {
                var operation = semanticModel.GetOperation(node);
                if (operation != null)
                {
                    Assert.True(set.Contains(operation));
                }

                node = node.Parent;
            }
        }

        #endregion

        #region Theory Helpers

        public static IEnumerable<object[]> ExternalPdbFormats
        {
            get
            {
                if (ExecutionConditionUtil.IsWindows)
                {
                    return new List<object[]>()
                    {
                        new object[] { DebugInformationFormat.Pdb },
                        new object[] { DebugInformationFormat.PortablePdb }
                    };
                }
                else
                {
                    return new List<object[]>()
                    {
                        new object[] { DebugInformationFormat.PortablePdb }
                    };
                }
            }
        }

        public static IEnumerable<object[]> PdbFormats =>
            new List<object[]>(ExternalPdbFormats)
            {
                new object[] { DebugInformationFormat.Embedded }
            };

        #endregion
    }
}
