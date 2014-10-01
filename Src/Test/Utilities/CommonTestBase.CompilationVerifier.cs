// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public abstract partial class CommonTestBase
    {
        public class CompilationVerifier
        {
            private readonly CommonTestBase test;
            private readonly Compilation compilation;
            private CompilationTestData testData;
            private readonly IEnumerable<ModuleData> dependencies;
            private ImmutableArray<Diagnostic> diagnostics;
            private IModuleSymbol lazyModuleSymbol;
            private IList<ModuleData> allModuleData;

            internal ImmutableArray<byte> EmittedAssemblyData;
            internal ImmutableArray<byte> EmittedAssemblyPdb;

            public CompilationVerifier(
                CommonTestBase test, 
                Compilation compilation,
                IEnumerable<ModuleData> dependencies = null)
            {
                this.test = test;
                this.compilation = compilation;
                this.dependencies = dependencies;
            }

            public CompilationVerifier Clone()
            {
                return new CompilationVerifier(test, compilation, dependencies);
            }

            public Compilation Compilation
            {
                get { return compilation; }
            }

            public TempRoot Temp
            {
                get { return test.Temp; }
            }

            internal ImmutableArray<Diagnostic> Diagnostics
            {
                get { return diagnostics; }
            }

            internal ImmutableArray<ModuleMetadata> GetAllModuleMetadata()
            {
                if (EmittedAssemblyData == null)
                {
                    throw new InvalidOperationException("You must call Emit before calling GetAllModuleMetadata.");
                }

                ImmutableArray<ModuleMetadata> modules = ImmutableArray.Create(ModuleMetadata.CreateFromImage(EmittedAssemblyData));

                if (allModuleData != null)
                {
                    var netModules = allModuleData.Where(m => m.Kind == OutputKind.NetModule);
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
                using (var testEnvironment = new HostedRuntimeEnvironment(dependencies))
                {
                    string mainModuleName = Emit(testEnvironment, manifestResources);
                    allModuleData = testEnvironment.GetAllModuleData();

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
                using (var testEnvironment = new HostedRuntimeEnvironment(dependencies))
                {
                    string mainModuleName = Emit(testEnvironment, null);
                    string[] actualOutput = testEnvironment.PeVerifyModules(new[] { mainModuleName }, throwOnError: false);
                    Assert.Equal(expectedPeVerifyOutput, actualOutput);
                }
            }

            private string Emit(HostedRuntimeEnvironment testEnvironment, IEnumerable<ResourceDescription> manifestResources)
            {
                testEnvironment.Emit(compilation, manifestResources);

                diagnostics = testEnvironment.GetDiagnostics();
                EmittedAssemblyData = testEnvironment.GetMainImage();
                EmittedAssemblyPdb = testEnvironment.GetMainPdb();
                testData = testEnvironment.GetCompilationTestData();

                return compilation.Assembly.Identity.GetDisplayName();
            }

            public CompilationVerifier VerifyIL(
                string methodName, 
                XCData expectedIL, 
                bool realIL = false, 
                string sequencePoints = null,
                [CallerFilePath]string callerPath = null,
                [CallerLineNumber]int callerLine = 0)
            {
                return VerifyIL(methodName, expectedIL.Value, realIL, sequencePoints, callerPath, callerLine);
            }

            public CompilationVerifier VerifyIL(
                string qualifiedMethodName,
                string expectedIL, 
                bool realIL = false, 
                string sequencePoints = null,
                [CallerFilePath]string callerPath = null,
                [CallerLineNumber]int callerLine = 0)
            {
                // TODO: Currently the qualifiedMethodName is a symbol display name while PDB need metadata name.
                // So we need to pass the PDB metadata name of the method to sequencePoints (instead of just bool).

                bool escapeQuotes = compilation is CSharp.CSharpCompilation;

                var methodData = testData.GetMethodData(qualifiedMethodName);

                // verify IL emitted via CCI, if any:
                string actualCciIL = VisualizeIL(methodData, realIL, sequencePoints, useRefEmitter: false);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualCciIL, escapeQuotes, callerPath, callerLine);

                // verify IL emitted via ReflectionEmitter, if any:
                string actualRefEmitIL = VisualizeIL(methodData, realIL, sequencePoints, useRefEmitter: true);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualRefEmitIL, escapeQuotes, callerPath, callerLine);

                return this;
            }

            public CompilationVerifier VerifyPdb(string qualifiedMethodName, string expectedPdbXml)
            {
                var actualPdbXml = GetPdbXml(this.compilation, qualifiedMethodName);
                AssertXmlEqual(expectedPdbXml, actualPdbXml);

                return this;
            }

            public string VisualizeIL(string qualifiedMethodName, bool realIL = false, string sequencePoints = null, bool useRefEmitter = false)
            {
                return VisualizeIL(testData.GetMethodData(qualifiedMethodName), realIL, sequencePoints, useRefEmitter);
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

                    markers = GetSequencePointMarkers(actualPdbXml);
                }

                if (!realIL)
                {
                    return ILBuilderVisualizer.ILBuilderToString(methodData.ILBuilder, markers: markers);
                }

                var module = this.GetModuleSymbolForEmittedImage();
                return module != null ? test.VisualizeRealIL(module, methodData, markers) : null;
            }

            public CompilationVerifier VerifyMemberInIL(string methodName, bool expected)
            {
                Assert.Equal(expected, testData.Methods.ContainsKey(methodName));
                return this;
            }

            public CompilationVerifier VerifyDiagnostics(params DiagnosticDescription[] expected)
            {
                diagnostics.Verify(expected);
                return this;
            }

            public IModuleSymbol GetModuleSymbolForEmittedImage()
            {
                return GetModuleSymbolForEmittedImage(ref lazyModuleSymbol, EmittedAssemblyData);
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

                    var targetReference = LoadTestEmittedExecutableForSymbolValidation(peImage, compilation.Options.OutputKind, display: compilation.AssemblyName);
                    var references = compilation.References.Concat(new[] { targetReference });
                    var assemblies = test.ReferencesToModuleSymbols(references, compilation.Options.MetadataImportOptions);
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
