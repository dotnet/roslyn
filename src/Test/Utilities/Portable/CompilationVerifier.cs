﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
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

        private readonly Func<IModuleSymbol, CompilationTestData.MethodData, IReadOnlyDictionary<int, string>, string> _visualizeRealIL;

        internal CompilationVerifier(
            Compilation compilation,
            Func<IModuleSymbol, CompilationTestData.MethodData, IReadOnlyDictionary<int, string>, string> visualizeRealIL = null,
            IEnumerable<ModuleData> dependencies = null)
        {
            _compilation = compilation;
            _dependencies = dependencies;
            _visualizeRealIL = visualizeRealIL;
        }

        internal CompilationTestData TestData => _testData;
        public Compilation Compilation => _compilation;
        internal ImmutableArray<Diagnostic> Diagnostics => _diagnostics;

        internal ImmutableArray<ModuleMetadata> GetAllModuleMetadata()
        {
            if (EmittedAssemblyData == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetAllModuleMetadata.");
            }

            ImmutableArray<ModuleMetadata> modules = ImmutableArray.Create(ModuleMetadata.CreateFromImage(EmittedAssemblyData));

            if (_allModuleData != null)
            {
                var netModules = _allModuleData.Where(m => m.Kind == OutputKind.NetModule);
                if (netModules.Any())
                {
                    modules = modules.Concat(
                        ImmutableArray.CreateRange(netModules.Select(m => ModuleMetadata.CreateFromImage(m.Image))));
                }
            }

            return modules;
        }

        public void Emit(string expectedOutput, int? expectedReturnCode, string[] args, IEnumerable<ResourceDescription> manifestResources, EmitOptions emitOptions, bool peVerify, SignatureDescription[] expectedSignatures)
        {
            using (var testEnvironment = RuntimeEnvironmentFactory.Create(_dependencies))
            {
                string mainModuleName = Emit(testEnvironment, manifestResources, emitOptions);
                _allModuleData = testEnvironment.GetAllModuleData();

                if (peVerify)
                {
                    testEnvironment.PeVerify();
                }

                if (expectedSignatures != null)
                {
                    MetadataSignatureUnitTestHelper.VerifyMemberSignatures(testEnvironment, expectedSignatures);
                }

                if (expectedOutput != null || expectedReturnCode != null)
                {
                    var returnCode = testEnvironment.Execute(mainModuleName, args, expectedOutput);

                    if (expectedReturnCode is int exCode)
                    {
                        Assert.Equal(exCode, returnCode);
                    }
                }
            }
        }

        // TODO(tomat): Fold into CompileAndVerify. 
        // Replace bool verify parameter with string[] expectedPeVerifyOutput. If null, no verification. If empty verify have to succeed. Otherwise compare errors.
        public void EmitAndVerify(params string[] expectedPeVerifyOutput)
        {
            using (var testEnvironment = RuntimeEnvironmentFactory.Create(_dependencies))
            {
                string mainModuleName = Emit(testEnvironment, null, null);
                string[] actualOutput = testEnvironment.PeVerifyModules(new[] { mainModuleName }, throwOnError: false);
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
            [CallerFilePath]string callerPath = null,
            [CallerLineNumber]int callerLine = 0)
        {
            return VerifyILImpl(qualifiedMethodName, expectedIL.Value, realIL, sequencePoints, callerPath, callerLine, escapeQuotes: false);
        }

        public CompilationVerifier VerifyIL(
            string qualifiedMethodName,
            string expectedIL,
            bool realIL = false,
            string sequencePoints = null,
            [CallerFilePath]string callerPath = null,
            [CallerLineNumber]int callerLine = 0,
            string source = null)
        {
            return VerifyILImpl(qualifiedMethodName, expectedIL, realIL, sequencePoints, callerPath, callerLine, escapeQuotes: true, source: source);
        }

        public void VerifyLocalSignature(
            string qualifiedMethodName,
            string expectedSignature,
            [CallerLineNumber]int callerLine = 0,
            [CallerFilePath]string callerPath = null)
        {
            var ilBuilder = _testData.GetMethodData(qualifiedMethodName).ILBuilder;
            string actualSignature = ILBuilderVisualizer.LocalSignatureToString(ilBuilder);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedSignature, actualSignature, escapeQuotes: true, expectedValueSourcePath: callerPath, expectedValueSourceLine: callerLine);
        }

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
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL, escapeQuotes, callerPath, callerLine);
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
                var actualPdbXml = PdbToXmlConverter.ToXml(
                    pdbStream: new MemoryStream(EmittedAssemblyPdb.ToArray()),
                    peStream: new MemoryStream(EmittedAssemblyData.ToArray()),
                    options: PdbToXmlOptions.ResolveTokens |
                             PdbToXmlOptions.ThrowOnError |
                             PdbToXmlOptions.ExcludeDocuments |
                             PdbToXmlOptions.ExcludeCustomDebugInformation |
                             PdbToXmlOptions.ExcludeScopes,
                    methodName: sequencePoints);

                markers = ILValidation.GetSequencePointMarkers(actualPdbXml, source);
            }

            if (!realIL)
            {
                return ILBuilderVisualizer.ILBuilderToString(methodData.ILBuilder, markers: markers);
            }

            if (_lazyModuleSymbol == null)
            {
                _lazyModuleSymbol = GetModuleSymbolForEmittedImage(EmittedAssemblyData, MetadataImportOptions.All);
            }

            return _lazyModuleSymbol != null ? _visualizeRealIL(_lazyModuleSymbol, methodData, markers) : null;
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

        public IModuleSymbol GetModuleSymbolForEmittedImage()
        {
            return GetModuleSymbolForEmittedImage(EmittedAssemblyData, _compilation.Options.MetadataImportOptions);
        }

        private IModuleSymbol GetModuleSymbolForEmittedImage(ImmutableArray<byte> peImage, MetadataImportOptions importOptions)
        {
            if (peImage.IsDefault)
            {
                return null;
            }
            var targetReference = LoadTestEmittedExecutableForSymbolValidation(peImage, _compilation.Options.OutputKind, display: _compilation.AssemblyName);
            var references = _compilation.References.Concat(new[] { targetReference });
            var assemblies = GetReferencesToModuleSymbols(references, importOptions);
            return assemblies.Last();
        }

        private IEnumerable<IModuleSymbol> GetReferencesToModuleSymbols(IEnumerable<MetadataReference> references, MetadataImportOptions importOptions)
        {
            var dummy = _compilation
                .RemoveAllSyntaxTrees()
                .WithReferences(references)
                .WithAssemblyName("Dummy")
                .WithOptions(_compilation.Options.WithMetadataImportOptions(importOptions));

            return references.Select(reference =>
            {
                if (reference.Properties.Kind == MetadataImageKind.Assembly)
                {
                    return ((IAssemblySymbol)dummy.GetAssemblyOrModuleSymbol(reference))?.Modules.First();
                }
                else
                {
                    return (IModuleSymbol)dummy.GetAssemblyOrModuleSymbol(reference);
                }
            });
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
    }
}
