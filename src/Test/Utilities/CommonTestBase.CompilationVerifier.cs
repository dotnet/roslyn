// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PDB;


using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DiaSymReader;
using PDB::Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public abstract partial class CommonTestBase
    {
        public class CompilationVerifier
        {
            private readonly CommonTestBase _test;
            private readonly Compilation _compilation;
            private CompilationTestData _testData;
            private readonly IEnumerable<ModuleData> _dependencies;
            private ImmutableArray<Diagnostic> _diagnostics;
            private IModuleSymbol _lazyModuleSymbol;
            private IList<ModuleData> _allModuleData;

            internal ImmutableArray<byte> EmittedAssemblyData;
            internal ImmutableArray<byte> EmittedAssemblyPdb;

            public CompilationVerifier(
                CommonTestBase test,
                Compilation compilation,
                IEnumerable<ModuleData> dependencies = null)
            {
                _test = test;
                _compilation = compilation;
                _dependencies = dependencies;
            }

            public CompilationVerifier Clone()
            {
                return new CompilationVerifier(_test, _compilation, _dependencies);
            }

            internal CompilationTestData TestData
            {
                get { return _testData; }
            }

            public Compilation Compilation
            {
                get { return _compilation; }
            }

            public TempRoot Temp
            {
                get { return _test.Temp; }
            }

            internal ImmutableArray<Diagnostic> Diagnostics
            {
                get { return _diagnostics; }
            }

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

            public void Emit(string expectedOutput, IEnumerable<ResourceDescription> manifestResources, bool peVerify, SignatureDescription[] expectedSignatures)
            {
                using (var testEnvironment = new HostedRuntimeEnvironment(_dependencies))
                {
                    string mainModuleName = Emit(testEnvironment, manifestResources);
                    _allModuleData = testEnvironment.GetAllModuleData();

                    if (peVerify)
                    {
                        testEnvironment.PeVerify();
                    }

                    if (expectedSignatures != null)
                    {
                        MetadataSignatureUnitTestHelper.VerifyMemberSignatures(testEnvironment, expectedSignatures);
                    }

                    if (expectedOutput != null)
                    {
                        testEnvironment.Execute(mainModuleName, expectedOutput);
                    }
                }
            }

            // TODO(tomat): Fold into CompileAndVerify. 
            // Replace bool verify parameter with string[] expectedPeVerifyOutput. If null, no verification. If empty verify have to succeed. Otherwise compare errors.
            public void EmitAndVerify(params string[] expectedPeVerifyOutput)
            {
                using (var testEnvironment = new HostedRuntimeEnvironment(_dependencies))
                {
                    string mainModuleName = Emit(testEnvironment, null);
                    string[] actualOutput = testEnvironment.PeVerifyModules(new[] { mainModuleName }, throwOnError: false);
                    Assert.Equal(expectedPeVerifyOutput, actualOutput);
                }
            }

            private string Emit(HostedRuntimeEnvironment testEnvironment, IEnumerable<ResourceDescription> manifestResources)
            {
                testEnvironment.Emit(_compilation, manifestResources);

                _diagnostics = testEnvironment.GetDiagnostics();
                EmittedAssemblyData = testEnvironment.GetMainImage();
                EmittedAssemblyPdb = testEnvironment.GetMainPdb();
                _testData = testEnvironment.GetCompilationTestData();

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
                [CallerLineNumber]int callerLine = 0)
            {
                return VerifyILImpl(qualifiedMethodName, expectedIL, realIL, sequencePoints, callerPath, callerLine, escapeQuotes: true);
            }

            private CompilationVerifier VerifyILImpl(
                string qualifiedMethodName,
                string expectedIL,
                bool realIL,
                string sequencePoints,
                string callerPath,
                int callerLine,
                bool escapeQuotes)
            {
                // TODO: Currently the qualifiedMethodName is a symbol display name while PDB need metadata name.
                // So we need to pass the PDB metadata name of the method to sequencePoints (instead of just bool).

                var methodData = _testData.GetMethodData(qualifiedMethodName);

                // verify IL emitted via CCI, if any:
                string actualCciIL = VisualizeIL(methodData, realIL, sequencePoints, useRefEmitter: false);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualCciIL, escapeQuotes, callerPath, callerLine);

                // verify IL emitted via ReflectionEmitter, if any:
                string actualRefEmitIL = VisualizeIL(methodData, realIL, sequencePoints, useRefEmitter: true);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualRefEmitIL, escapeQuotes, callerPath, callerLine);

                return this;
            }

            public CompilationVerifier VerifyPdb(
                XElement expectedPdb,
                [CallerLineNumber]int expectedValueSourceLine = 0,
                [CallerFilePath]string expectedValueSourcePath = null)
            {
                _compilation.VerifyPdb(expectedPdb, expectedValueSourceLine, expectedValueSourcePath);
                return this;
            }

            public CompilationVerifier VerifyPdb(
                string expectedPdb,
                [CallerLineNumber]int expectedValueSourceLine = 0,
                [CallerFilePath]string expectedValueSourcePath = null)
            {
                _compilation.VerifyPdb(expectedPdb, expectedValueSourceLine, expectedValueSourcePath);
                return this;
            }

            public CompilationVerifier VerifyPdb(
                string qualifiedMethodName,
                string expectedPdb,
                [CallerLineNumber]int expectedValueSourceLine = 0,
                [CallerFilePath]string expectedValueSourcePath = null)
            {
                _compilation.VerifyPdb(qualifiedMethodName, expectedPdb, expectedValueSourceLine, expectedValueSourcePath);
                return this;
            }

            public CompilationVerifier VerifyPdb(
               string qualifiedMethodName,
               XElement expectedPdb,
               [CallerLineNumber]int expectedValueSourceLine = 0,
               [CallerFilePath]string expectedValueSourcePath = null)
            {
                _compilation.VerifyPdb(qualifiedMethodName, expectedPdb, expectedValueSourceLine, expectedValueSourcePath);
                return this;
            }

            public ISymUnmanagedReader CreateSymReader()
            {
                return new SymReader(new MemoryStream(EmittedAssemblyPdb.ToArray()));
            }

            public string VisualizeIL(string qualifiedMethodName, bool realIL = false, string sequencePoints = null, bool useRefEmitter = false)
            {
                return VisualizeIL(_testData.GetMethodData(qualifiedMethodName), realIL, sequencePoints, useRefEmitter);
            }

            private string VisualizeIL(CompilationTestData.MethodData methodData, bool realIL, string sequencePoints, bool useRefEmitter)
            {
                Dictionary<int, string> markers = null;

                if (sequencePoints != null)
                {
                    var actualPdbXml = PdbToXmlConverter.ToXml(
                        pdbStream: new MemoryStream(EmittedAssemblyPdb.ToArray()),
                        peStream: new MemoryStream(EmittedAssemblyData.ToArray()),
                        options: PdbToXmlOptions.ResolveTokens | PdbToXmlOptions.ThrowOnError,
                        methodName: sequencePoints);

                    markers = GetMarkers(actualPdbXml);
                }

                if (!realIL)
                {
                    return ILBuilderVisualizer.ILBuilderToString(methodData.ILBuilder, markers: markers);
                }

                var module = this.GetModuleSymbolForEmittedImage();
                return module != null ? _test.VisualizeRealIL(module, methodData, markers) : null;
            }

            public CompilationVerifier VerifyMemberInIL(string methodName, bool expected)
            {
                Assert.Equal(expected, _testData.Methods.ContainsKey(methodName));
                return this;
            }

            public CompilationVerifier VerifyDiagnostics(params DiagnosticDescription[] expected)
            {
                _diagnostics.Verify(expected);
                return this;
            }

            public IModuleSymbol GetModuleSymbolForEmittedImage()
            {
                return GetModuleSymbolForEmittedImage(ref _lazyModuleSymbol, EmittedAssemblyData);
            }

            private IModuleSymbol GetModuleSymbolForEmittedImage(ref IModuleSymbol moduleSymbol, ImmutableArray<byte> peImage)
            {
                if (peImage.IsDefault)
                {
                    return null;
                }

                if (moduleSymbol == null)
                {
                    Debug.Assert(!peImage.IsDefault);

                    var targetReference = LoadTestEmittedExecutableForSymbolValidation(peImage, _compilation.Options.OutputKind, display: _compilation.AssemblyName);
                    var references = _compilation.References.Concat(new[] { targetReference });
                    var assemblies = _test.ReferencesToModuleSymbols(references, _compilation.Options.MetadataImportOptions);
                    var module = assemblies.Last();
                    moduleSymbol = module;
                }

                return moduleSymbol;
            }

            private static MetadataReference LoadTestEmittedExecutableForSymbolValidation(
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
        }
    }
}
