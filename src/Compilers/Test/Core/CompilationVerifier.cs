// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader.Tools;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class CompilationVerifier
    {
        private readonly Compilation _compilation;
        private CompilationTestData _testData;
        private readonly IEnumerable<ModuleData> _dependencies;
        private ImmutableArray<Diagnostic> _diagnostics;
        private IModuleSymbol _lazyModuleSymbol;
        private IList<ModuleData> _allModuleData;

        public ImmutableArray<byte> EmittedAssemblyData;
        public ImmutableArray<byte> EmittedAssemblyPdb;

        private readonly Func<IModuleSymbol, CompilationTestData.MethodData, IReadOnlyDictionary<int, string>, bool, string> _visualizeRealIL;

        internal CompilationVerifier(
            Compilation compilation,
            Func<IModuleSymbol, CompilationTestData.MethodData, IReadOnlyDictionary<int, string>, bool, string> visualizeRealIL = null,
            IEnumerable<ModuleData> dependencies = null)
        {
            _compilation = compilation;
            _dependencies = dependencies;
            _visualizeRealIL = visualizeRealIL;
        }

        internal CompilationTestData TestData => _testData;
        public Compilation Compilation => _compilation;
        internal ImmutableArray<Diagnostic> Diagnostics => _diagnostics;

        internal Metadata GetMetadata()
        {
            if (EmittedAssemblyData == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetAllModuleMetadata.");
            }

            if (_compilation.Options.OutputKind.IsNetModule())
            {
                var metadata = ModuleMetadata.CreateFromImage(EmittedAssemblyData);
                metadata.Module.PretendThereArentNoPiaLocalTypes();
                return metadata;
            }
            else
            {
                var images = new List<ImmutableArray<byte>>
                {
                    EmittedAssemblyData
                };

                if (_allModuleData != null)
                {
                    images.AddRange(_allModuleData.Where(m => m.Kind == OutputKind.NetModule).Select(m => m.Image));
                }

                return AssemblyMetadata.Create(images.Select(image =>
                {
                    var metadata = ModuleMetadata.CreateFromImage(image);
                    metadata.Module.PretendThereArentNoPiaLocalTypes();
                    return metadata;
                }));
            }
        }

        public string Dump(string methodName = null)
        {
            using (var testEnvironment = RuntimeEnvironmentFactory.Create(_dependencies))
            {
                string mainModuleFullName = Emit(testEnvironment, manifestResources: null, EmitOptions.Default);
                IList<ModuleData> moduleDatas = testEnvironment.GetAllModuleData();
                var mainModule = moduleDatas.Single(md => md.FullName == mainModuleFullName);
                RuntimeEnvironmentUtilities.DumpAssemblyData(moduleDatas, out var dumpDir);

                string extension = mainModule.Kind == OutputKind.ConsoleApplication ? ".exe" : ".dll";
                string modulePath = Path.Combine(dumpDir, mainModule.SimpleName + extension);

                var decompiler = new ICSharpCode.Decompiler.CSharp.CSharpDecompiler(modulePath,
                    new ICSharpCode.Decompiler.DecompilerSettings() { AsyncAwait = false });

                if (methodName != null)
                {
                    var map = new Dictionary<string, ICSharpCode.Decompiler.TypeSystem.IMethod>();
                    listMethods(decompiler.TypeSystem.MainModule.RootNamespace, map);

                    if (map.TryGetValue(methodName, out var method))
                    {
                        return decompiler.DecompileAsString(method.MetadataToken);
                    }
                    else
                    {
                        throw new Exception($"Didn't find method '{methodName}'. Available/distinguishable methods are: \r\n{string.Join("\r\n", map.Keys)}");
                    }
                }

                return decompiler.DecompileWholeModuleAsString();
            }

            void listMethods(ICSharpCode.Decompiler.TypeSystem.INamespace @namespace, Dictionary<string, ICSharpCode.Decompiler.TypeSystem.IMethod> result)
            {
                foreach (var nestedNS in @namespace.ChildNamespaces)
                {
                    if (nestedNS.FullName != "System" &&
                        nestedNS.FullName != "Microsoft")
                    {
                        listMethods(nestedNS, result);
                    }
                }

                foreach (var type in @namespace.Types)
                {
                    listMethodsInType(type, result);
                }
            }

            void listMethodsInType(ICSharpCode.Decompiler.TypeSystem.ITypeDefinition type, Dictionary<string, ICSharpCode.Decompiler.TypeSystem.IMethod> result)
            {
                foreach (var nestedType in type.NestedTypes)
                {
                    listMethodsInType(nestedType, result);
                }

                foreach (var method in type.Methods)
                {
                    if (result.ContainsKey(method.FullName))
                    {
                        // There is a bug with FullName on methods in generic types
                        result.Remove(method.FullName);
                    }
                    else
                    {
                        result.Add(method.FullName, method);
                    }
                }
            }
        }

        /// <summary>
		/// Asserts that the emitted IL for a type is the same as the expected IL.
		/// Many core library types are in different assemblies on .Net Framework, and .Net Core.
		/// Therefore this test is likely to fail unless you  only run it only only on one of these frameworks,
		/// or you run it on both, but provide a different expected output string for each.
		/// See <see cref="ExecutionConditionUtil"/>.
		/// </summary>
		/// <param name="typeName">The non-fully-qualified name of the type</param>
		/// <param name="expected">The expected IL</param>
        public void VerifyTypeIL(string typeName, string expected)
        {
            var output = new ICSharpCode.Decompiler.PlainTextOutput();
            using (var testEnvironment = RuntimeEnvironmentFactory.Create(_dependencies))
            {
                string mainModuleFullName = Emit(testEnvironment, manifestResources: null, EmitOptions.Default);
                IList<ModuleData> moduleData = testEnvironment.GetAllModuleData();
                var mainModule = moduleData.Single(md => md.FullName == mainModuleFullName);
                using (var moduleMetadata = ModuleMetadata.CreateFromImage(testEnvironment.GetMainImage()))
                {
                    var peFile = new PEFile(mainModuleFullName, moduleMetadata.Module.PEReaderOpt);
                    var metadataReader = moduleMetadata.GetMetadataReader();

                    bool found = false;
                    foreach (var typeDefHandle in metadataReader.TypeDefinitions)
                    {
                        var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                        if (metadataReader.GetString(typeDef.Name) == typeName)
                        {
                            var disassembler = new ICSharpCode.Decompiler.Disassembler.ReflectionDisassembler(output, default);
                            disassembler.DisassembleType(peFile, typeDefHandle);
                            found = true;
                            break;
                        }
                    }
                    Assert.True(found, "Could not find type named " + typeName);
                }
            }
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, output.ToString(), escapeQuotes: false);
        }

        public void Emit(
            string expectedOutput,
            bool trimOutput,
            int? expectedReturnCode,
            string[] args,
            IEnumerable<ResourceDescription> manifestResources,
            EmitOptions emitOptions,
            Verification peVerify,
            SignatureDescription[] expectedSignatures)
        {
            using var testEnvironment = RuntimeEnvironmentFactory.Create(_dependencies);

            string mainModuleName = Emit(testEnvironment, manifestResources, emitOptions);
            _allModuleData = testEnvironment.GetAllModuleData();
            testEnvironment.Verify(peVerify);
#if NETCOREAPP
            ILVerify(peVerify);
#endif

            if (expectedSignatures != null)
            {
                MetadataSignatureUnitTestHelper.VerifyMemberSignatures(testEnvironment, expectedSignatures);
            }

            if (expectedOutput != null || expectedReturnCode != null)
            {
                var returnCode = testEnvironment.Execute(mainModuleName, args, expectedOutput, trimOutput);

                if (expectedReturnCode is int exCode)
                {
                    Assert.Equal(exCode, returnCode);
                }
            }
        }

        private sealed class Resolver : ILVerify.ResolverBase
        {
            private readonly Dictionary<string, ImmutableArray<byte>> _imagesByName;

            internal Resolver(Dictionary<string, ImmutableArray<byte>> imagesByName)
            {
                _imagesByName = imagesByName;
            }

            protected override PEReader ResolveCore(string simpleName)
            {
                if (_imagesByName.TryGetValue(simpleName, out var image))
                {
                    return new PEReader(image);
                }

                throw new Exception($"ILVerify was not able to resolve a module named '{simpleName}'");
            }
        }

        private void ILVerify(Verification verification)
        {
            if (verification == Verification.Skipped)
            {
                return;
            }

            var imagesByName = new Dictionary<string, ImmutableArray<byte>>(StringComparer.OrdinalIgnoreCase);
            foreach (var module in _allModuleData)
            {
                string name = module.SimpleName;
                if (imagesByName.ContainsKey(name))
                {
                    if ((verification & Verification.FailsILVerify) != 0)
                    {
                        return;
                    }

                    throw new Exception($"Multiple modules named '{name}' were found");
                }
                imagesByName.Add(name, module.Image);
            }

            var resolver = new Resolver(imagesByName);
            var verifier = new ILVerify.Verifier(resolver);
            var mscorlibModule = _allModuleData.SingleOrDefault(m => m.IsCorLib);
            if (mscorlibModule is null)
            {
                if ((verification & Verification.FailsILVerify) != 0)
                {
                    return;
                }

                throw new Exception("No corlib found");
            }

            // Main module is the first one
            var mainModuleReader = resolver.Resolve(_allModuleData[0].SimpleName);

            var (succeeded, errorMessage) = verify(verifier, mscorlibModule.SimpleName, mainModuleReader);

            switch (succeeded, (verification & Verification.FailsILVerify) == 0)
            {
                case (true, true):
                    return;
                case (true, false):
                    throw new Exception("IL Verify succeeded unexpectedly");
                case (false, false):
                    return;
                case (false, true):
                    throw new Exception("IL Verify failed unexpectedly: \r\n" + errorMessage);
            }

            static (bool, string) verify(ILVerify.Verifier verifier, string corlibName, PEReader mainModule)
            {
                IEnumerable<ILVerify.VerificationResult> result = null;
                int errorCount = 0;
                try
                {
                    verifier.SetSystemModuleName(new AssemblyName(corlibName));
                    result = verifier.Verify(mainModule);
                    errorCount = result.Count();
                }
                catch (Exception e)
                {
                    return (false, e.Message);
                }

                if (errorCount > 0)
                {
                    var metadataReader = mainModule.GetMetadataReader();
                    return (false, printVerificationResult(result, metadataReader));
                }

                return (true, string.Empty);
            }

            static string printVerificationResult(IEnumerable<ILVerify.VerificationResult> result, MetadataReader metadataReader)
            {
                return string.Join("\r\n", result.Select(r => printMethod(r.Method, metadataReader) + r.Message + printErrorArguments(r.ErrorArguments)));
            }

            static string printMethod(MethodDefinitionHandle method, MetadataReader metadataReader)
            {
                if (method.IsNil)
                {
                    return "";
                }

                var methodName = metadataReader.GetString(metadataReader.GetMethodDefinition(method).Name);
                return $"[{methodName}]: ";
            }

            static string printErrorArguments(ILVerify.ErrorArgument[] errorArguments)
            {
                if (errorArguments is null
                    || errorArguments.Length == 0)
                {
                    return "";
                }

                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                builder.Append(" { ");
                var x = errorArguments.Select(a => printErrorArgument(a)).ToArray();
                for (int i = 0; i < x.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(x[i]);
                }
                builder.Append(" }");

                return pooledBuilder.ToStringAndFree();
            }

            static string printErrorArgument(ILVerify.ErrorArgument errorArgument)
            {
                var name = errorArgument.Name;

                string value;
                if (name == "Offset" && errorArgument.Value is int i)
                {
                    value = "0x" + Convert.ToString(i, 16);
                }
                else
                {
                    value = errorArgument.Value.ToString();
                }

                return name + " = " + value;
            }
        }

        // TODO(tomat): Fold into CompileAndVerify. 
        // Replace bool verify parameter with string[] expectedPeVerifyOutput. If null, no verification. If empty verify have to succeed. Otherwise compare errors.
        public void EmitAndVerify(params string[] expectedPeVerifyOutput)
        {
            using (var testEnvironment = RuntimeEnvironmentFactory.Create(_dependencies))
            {
                string mainModuleName = Emit(testEnvironment, null, null);
                string[] actualOutput = testEnvironment.VerifyModules(new[] { mainModuleName });
                Assert.Equal(expectedPeVerifyOutput, actualOutput);
            }
        }

        private string Emit(IRuntimeEnvironment testEnvironment, IEnumerable<ResourceDescription> manifestResources, EmitOptions emitOptions)
        {
            testEnvironment.Emit(_compilation, manifestResources, emitOptions);

            _diagnostics = testEnvironment.GetDiagnostics();
            EmittedAssemblyData = testEnvironment.GetMainImage();
            EmittedAssemblyPdb = testEnvironment.GetMainPdb();
            _testData = ((IInternalRuntimeEnvironment)testEnvironment).GetCompilationTestData();

            return _compilation.Assembly.Identity.GetDisplayName();
        }

        public CompilationVerifier VerifyIL(
            string qualifiedMethodName,
            XCData expectedIL,
            bool realIL = false,
            string sequencePoints = null,
            [CallerFilePath] string callerPath = null,
            [CallerLineNumber] int callerLine = 0)
        {
            return VerifyILImpl(qualifiedMethodName, expectedIL.Value, realIL, sequencePoints, callerPath, callerLine, escapeQuotes: false);
        }

        public CompilationVerifier VerifyIL(
            string qualifiedMethodName,
            string expectedIL,
            bool realIL = false,
            string sequencePoints = null,
            [CallerFilePath] string callerPath = null,
            [CallerLineNumber] int callerLine = 0,
            string source = null)
        {
            return VerifyILImpl(qualifiedMethodName, expectedIL, realIL, sequencePoints, callerPath, callerLine, escapeQuotes: true, source: source);
        }

        public CompilationVerifier VerifyMissing(
            string qualifiedMethodName)
        {
            Assert.False(_testData.TryGetMethodData(qualifiedMethodName, out _));
            return this;
        }

        public void VerifyLocalSignature(
            string qualifiedMethodName,
            string expectedSignature,
            [CallerLineNumber] int callerLine = 0,
            [CallerFilePath] string callerPath = null)
        {
            var ilBuilder = _testData.GetMethodData(qualifiedMethodName).ILBuilder;
            string actualSignature = ILBuilderVisualizer.LocalSignatureToString(ilBuilder);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, actualSignature, escapeQuotes: true, expectedValueSourcePath: callerPath, expectedValueSourceLine: callerLine);
        }

        /// <summary>
        /// Visualizes the IL for a given method, and ensures that it matches the expected IL.
        /// </summary>
        /// <param name="realIL">Controls whether the IL stream contains pseudo-tokens or real tokens.</param>
        private CompilationVerifier VerifyILImpl(
            string qualifiedMethodName,
            string expectedIL,
            bool realIL,
            string sequencePoints,
            string callerPath,
            int callerLine,
            bool escapeQuotes,
            string source = null)
        {
            string actualIL = VisualizeIL(qualifiedMethodName, realIL, sequencePoints, source);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, message: null, escapeQuotes, callerPath, callerLine);
            return this;
        }

        public string VisualizeIL(string qualifiedMethodName, bool realIL = false, string sequencePoints = null, string source = null)
        {
            // TODO: Currently the qualifiedMethodName is a symbol display name while PDB need metadata name.
            // So we need to pass the PDB metadata name of the method to sequencePoints (instead of just bool).

            return VisualizeIL(_testData.GetMethodData(qualifiedMethodName), realIL, sequencePoints, source);
        }

        internal string VisualizeIL(CompilationTestData.MethodData methodData, bool realIL, string sequencePoints = null, string source = null)
        {
            Dictionary<int, string> markers = null;

            if (sequencePoints != null)
            {
                if (EmittedAssemblyPdb == null)
                {
                    throw new InvalidOperationException($"{nameof(EmittedAssemblyPdb)} is not set");
                }

                if (EmittedAssemblyData == null)
                {
                    throw new InvalidOperationException($"{nameof(EmittedAssemblyData)} is not set");
                }

                var actualPdbXml = PdbToXmlConverter.ToXml(
                    pdbStream: new MemoryStream(EmittedAssemblyPdb.ToArray()),
                    peStream: new MemoryStream(EmittedAssemblyData.ToArray()),
                    options: PdbToXmlOptions.ResolveTokens |
                             PdbToXmlOptions.ThrowOnError |
                             PdbToXmlOptions.ExcludeDocuments |
                             PdbToXmlOptions.ExcludeCustomDebugInformation |
                             PdbToXmlOptions.ExcludeScopes,
                    methodName: sequencePoints);

                if (actualPdbXml.StartsWith("<error>"))
                {
                    throw new Exception($"Failed to extract PDB information for method '{sequencePoints}'. PdbToXmlConverter returned:\r\n{actualPdbXml}");
                }

                markers = ILValidation.GetSequencePointMarkers(actualPdbXml, source);
            }

            if (!realIL)
            {
                return ILBuilderVisualizer.ILBuilderToString(methodData.ILBuilder, markers: markers);
            }

            if (_lazyModuleSymbol == null)
            {
                var targetReference = LoadTestEmittedExecutableForSymbolValidation(EmittedAssemblyData, _compilation.Options.OutputKind, display: _compilation.AssemblyName);
                _lazyModuleSymbol = GetSymbolFromMetadata(targetReference, MetadataImportOptions.All);
            }

            if (_lazyModuleSymbol != null)
            {
                if (_visualizeRealIL == null)
                {
                    throw new InvalidOperationException("IL visualization function is not set");
                }

                return _visualizeRealIL(_lazyModuleSymbol, methodData, markers, _testData.Module.GetMethodBody(methodData.Method).AreLocalsZeroed);
            }

            return null;
        }

        public CompilationVerifier VerifyMemberInIL(string methodName, bool expected)
        {
            Assert.Equal(expected, _testData.GetMethodsByName().ContainsKey(methodName));
            return this;
        }

        public CompilationVerifier VerifyDiagnostics(params DiagnosticDescription[] expected)
        {
            _diagnostics.Verify(expected);
            return this;
        }

        internal IModuleSymbol GetSymbolFromMetadata(MetadataReference metadataReference, MetadataImportOptions importOptions)
        {
            var dummy = _compilation
                .RemoveAllSyntaxTrees()
                .AddReferences(metadataReference)
                .WithAssemblyName("Dummy")
                .WithOptions(_compilation.Options.WithMetadataImportOptions(importOptions));

            var symbol = dummy.GetAssemblyOrModuleSymbol(metadataReference);

            if (metadataReference.Properties.Kind == MetadataImageKind.Assembly)
            {
                return ((IAssemblySymbol)symbol).Modules.First();
            }
            else
            {
                return (IModuleSymbol)symbol;
            }
        }

        internal static MetadataReference LoadTestEmittedExecutableForSymbolValidation(
            ImmutableArray<byte> image,
            OutputKind outputKind,
            string display = null)
        {
            var moduleMetadata = ModuleMetadata.CreateFromImage(image);
            moduleMetadata.Module.PretendThereArentNoPiaLocalTypes();

            if (outputKind == OutputKind.NetModule)
            {
                return moduleMetadata.GetReference(display: display);
            }
            else
            {
                return AssemblyMetadata.Create(moduleMetadata).GetReference(display: display);
            }
        }

        public void VerifyOperationTree(string expectedOperationTree, bool skipImplicitlyDeclaredSymbols = false)
        {
            _compilation.VerifyOperationTree(expectedOperationTree, skipImplicitlyDeclaredSymbols);
        }

        public void VerifyOperationTree(string symbolToVerify, string expectedOperationTree, bool skipImplicitlyDeclaredSymbols = false)
        {
            _compilation.VerifyOperationTree(symbolToVerify, expectedOperationTree, skipImplicitlyDeclaredSymbols);
        }

        /// <summary>
        /// Useful for verifying the expected variables are hoisted for closures, async, and iterator methods.
        /// </summary>
        public void VerifySynthesizedFields(string containingTypeName, params string[] expectedFields)
        {
            var types = TestData.Module.GetAllSynthesizedMembers();
            Assert.Contains(types.Keys, t => containingTypeName == t.ToString());
            var members = TestData.Module.GetAllSynthesizedMembers()
                .Where(e => e.Key.ToString() == containingTypeName)
                .Single()
                .Value
                .Where(s => s.Kind == SymbolKind.Field)
                .Select(f => $"{((IFieldSymbol)f.GetISymbol()).Type.ToString()} {f.Name}")
                .ToList();
            AssertEx.SetEqual(expectedFields, members);
        }
    }
}
