// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
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

            public string MVID
            {
                get { return testData.Module.PersistentIdentifier.ToString("B"); }
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
                            ImmutableArray.CreateRange<ModuleMetadata>(netModules.Select(m => ModuleMetadata.CreateFromImage(m.Image))));
                    }
                }

                return modules;
            }

            public void Emit(string expectedOutput, IEnumerable<ResourceDescription> manifestResources, bool emitPdb, bool peVerify, SignatureDescription[] expectedSignatures)
            {
                bool doExecute = expectedOutput != null;

                using (var testEnvironment = new HostedRuntimeEnvironment(dependencies))
                {
                    string mainModuleName = Emit(testEnvironment, manifestResources, emitPdb);

                    allModuleData = testEnvironment.GetAllModuleData();

                    if (peVerify)
                    {
                        testEnvironment.PeVerify();
                    }

                    if (expectedSignatures != null)
                    {
                        MetadataSignatureUnitTestHelper.VerifyMemberSignatures(testEnvironment, expectedSignatures);
                    }

                    if (doExecute)
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
                    string mainModuleName = Emit(testEnvironment, null, emitPdb: false);
                    string[] actualOutput = testEnvironment.PeVerifyModules(new[] { mainModuleName }, throwOnError: false);
                    Assert.Equal(expectedPeVerifyOutput, actualOutput);
                }
            }

            private string Emit(HostedRuntimeEnvironment testEnvironment, IEnumerable<ResourceDescription> manifestResources, bool emitPdb)
            {
                testEnvironment.Emit(compilation, manifestResources, emitPdb);

                diagnostics = testEnvironment.GetDiagnostics();
                EmittedAssemblyData = testEnvironment.GetMainImage();
                testData = testEnvironment.GetCompilationTestData();

                return compilation.Assembly.Identity.GetDisplayName();
            }

            public CompilationVerifier VerifyIL(string methodName, XCData expectedIL, bool realIL = false)
            {
                return VerifyIL(methodName, expectedIL.Value, realIL);
            }

            public CompilationVerifier VerifyIL(string qualifiedMethodName, string expectedIL, bool realIL = false)
            {
                bool escapeQuotes = compilation is Microsoft.CodeAnalysis.CSharp.CSharpCompilation;

                expectedIL = expectedIL.Replace("{#MVID#}", MVID);

                var methodData = testData.GetMethodData(qualifiedMethodName);

                // verify IL emitted via CCI, if any:
                string actualCciIL = VisualizeIL(methodData, realIL, useRefEmitter: false);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualCciIL, escapeQuotes);

                // verify IL emitted via ReflectionEmitter, if any:
                string actualRefEmitIL = VisualizeIL(methodData, realIL, useRefEmitter: true);
                AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualRefEmitIL, escapeQuotes);

                return this;
            }

            public string VisualizeIL(string qualifiedMethodName, bool realIL = false, bool useRefEmitter = false)
            {
                return VisualizeIL(testData.GetMethodData(qualifiedMethodName), realIL, useRefEmitter);
            }

            private string VisualizeIL(CompilationTestData.MethodData methodData, bool realIL, bool useRefEmitter)
            {
                if (!realIL)
                {
                    return ILBuilderVisualizer.ILBuilderToString(methodData.ILBuilder);
                }

                var module = this.GetModuleSymbolForEmittedImage();
                return module != null ? test.VisualizeRealIL(module, methodData) : null;
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
                return GetModuleSymbolForEmittedImage(ref lazyModuleSymbol, ref EmittedAssemblyData);
            }

            private IModuleSymbol GetModuleSymbolForEmittedImage(ref IModuleSymbol moduleSymbol, ref ImmutableArray<byte> peImage)
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

            private static MetadataImageReference LoadTestEmittedExecutableForSymbolValidation(
                ImmutableArray<byte> image,
                OutputKind outputKind,
                string display = null)
            {
                var moduleMetadata = ModuleMetadata.CreateFromImage(image);
                moduleMetadata.Module.PretendThereArentNoPiaLocalTypes();

                if (outputKind == OutputKind.NetModule)
                {
                    return new MetadataImageReference(moduleMetadata, display: display);
                }
                else
                {
                    var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                    return new MetadataImageReference(assemblyMetadata, display: display);
                }
            }
        }
    }
}
