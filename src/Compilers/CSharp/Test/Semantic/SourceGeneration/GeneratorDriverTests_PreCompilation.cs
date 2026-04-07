// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

#pragma warning disable RSEXPERIMENTAL007 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class GeneratorDriverTests_PreCompilation : CSharpTestBase
    {
        #region Basic Functionality

        [Fact]
        public void PreCompilationSource_Is_Added_To_Output_Compilation()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("a", "class D {}"))));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PreCompilationSource_Is_Visible_To_RegisterSourceOutput()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("a", "class D {}"));

                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    // The compilation should include both the original tree and the pre-compilation tree
                    Assert.Equal(2, c.SyntaxTrees.Count());
                    // The type 'D' should be visible in the compilation
                    var typeD = c.GetTypeByMetadataName("D");
                    Assert.NotNull(typeD);
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PreCompilationSource_Is_Visible_To_Other_Generators()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            // Generator 1 produces a pre-compilation source
            var generator1 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
            }));

            // Generator 2 reads the compilation and should see the type from Generator 1
            var generator2 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2((ic) =>
            {
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    var type = c.GetTypeByMetadataName("PreCompType");
                    Assert.NotNull(type);
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator1, generator2 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void Multiple_PreCompilationSources_From_Same_Generator()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) =>
                {
                    c.AddSource("type1", "class Type1 {}");
                    c.AddSource("type2", "class Type2 {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Original tree + 2 pre-compilation sources
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void Multiple_Generators_With_PreCompilationSources()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator1 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("gen1", "class Gen1Type {}"));
            }));

            var generator2 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("gen2", "class Gen2Type {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator1, generator2 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Original tree + 2 pre-compilation sources from different generators
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("Gen1Type"));
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("Gen2Type"));
        }

        #endregion

        #region Input Providers

        [Fact]
        public void PreCompilation_With_AdditionalTextsProvider()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var additionalText = new InMemoryAdditionalText("test.txt", "TypeFromFile");

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.AdditionalTextsProvider, (c, file) =>
                {
                    var content = file.GetText()!.ToString();
                    c.AddSource("fromFile", $"class {content} {{}}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText });
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("TypeFromFile"));
        }

        [Fact]
        public void PreCompilation_With_AnalyzerConfigOptionsProvider()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.AnalyzerConfigOptionsProvider, (c, options) =>
                {
                    c.AddSource("configBased", "class ConfigBased {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PreCompilation_With_Combined_Providers()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var additionalText = new InMemoryAdditionalText("test.txt", "CombinedType");

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                var combined = ic.AdditionalTextsProvider.Combine(ic.ParseOptionsProvider);
                ic.RegisterPreCompilationSourceOutput(combined, (c, pair) =>
                {
                    var (file, options) = pair;
                    c.AddSource("combined", $"class {file.GetText()!} {{}}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText });
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("CombinedType"));
        }

        [Fact]
        public void PreCompilation_With_Transformed_Provider()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var additionalText1 = new InMemoryAdditionalText("a.proto", "content1");
            var additionalText2 = new InMemoryAdditionalText("b.txt", "content2");
            var additionalText3 = new InMemoryAdditionalText("c.proto", "content3");

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                var protoFiles = ic.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".proto"));
                ic.RegisterPreCompilationSourceOutput(protoFiles, (c, file) =>
                {
                    c.AddSource(Path.GetFileNameWithoutExtension(file.Path), $"class {Path.GetFileNameWithoutExtension(file.Path)} {{}}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText1, additionalText2, additionalText3 });
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Original + 2 proto files (b.txt is filtered out)
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("a"));
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("c"));
        }

        #endregion

        #region Interaction with Other Output Kinds

        [Fact]
        public void PreCompilation_With_PostInit_In_Same_Generator()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPostInitializationOutput(c => c.AddSource("postinit", "class PostInitType {}"));
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Original + PostInit + PreCompilation
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("PostInitType"));
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("PreCompType"));
        }

        [Fact]
        public void PreCompilation_With_SourceOutput_In_Same_Generator()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    // Verify pre-compilation type is visible in the compilation
                    Assert.NotNull(c.GetTypeByMetadataName("PreCompType"));
                    ctx.AddSource("regular", "class RegularType {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Original + PreCompilation + Regular
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PreCompilation_With_ImplementationSourceOutput_In_Same_Generator()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterImplementationSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    Assert.NotNull(c.GetTypeByMetadataName("PreCompType"));
                    ctx.AddSource("impl", "class ImplType {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Original + PreCompilation + Implementation
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        #endregion

        #region Incremental Behavior

        [Fact]
        public void PreCompilation_Is_Cached_On_Second_Run()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            int callCount = 0;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) =>
                {
                    callCount++;
                    c.AddSource("precomp", "class PreCompType {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);

            // First run
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation1, out var diagnostics1);
            Assert.Equal(2, outputCompilation1.SyntaxTrees.Count());
            Assert.Empty(diagnostics1);
            Assert.Equal(1, callCount);

            // Second run with same compilation — should be cached
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out var diagnostics2);
            Assert.Equal(2, outputCompilation2.SyntaxTrees.Count());
            Assert.Empty(diagnostics2);
            Assert.Equal(1, callCount); // not called again
        }

        [Fact]
        public void PreCompilation_Reruns_When_AdditionalFile_Changes()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            int callCount = 0;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.AdditionalTextsProvider, (c, file) =>
                {
                    callCount++;
                    c.AddSource("fromFile", $"class {file.GetText()!} {{}}");
                });
            }));

            var additionalText = new InMemoryAdditionalText("test.txt", "TypeA");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText });

            // First run
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation1, out var diagnostics1);
            Assert.Equal(2, outputCompilation1.SyntaxTrees.Count());
            Assert.NotNull(outputCompilation1.GetTypeByMetadataName("TypeA"));
            Assert.Equal(1, callCount);

            // Change additional file
            var newAdditionalText = new InMemoryAdditionalText("test.txt", "TypeB");
            driver = driver.ReplaceAdditionalText(additionalText, newAdditionalText);

            // Second run — should re-run because input changed
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out var diagnostics2);
            Assert.Equal(2, outputCompilation2.SyntaxTrees.Count());
            Assert.NotNull(outputCompilation2.GetTypeByMetadataName("TypeB"));
            Assert.Equal(2, callCount); // called again
        }

        [Fact]
        public void PreCompilation_Incremental_Step_Shows_Cached()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: TestOptions.GeneratorDriverOptions);

            // First run
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            var result1 = driver.GetRunResult().Results[0];
            Assert.Contains(WellKnownGeneratorOutputs.PreCompilationSourceOutput, result1.TrackedSteps.Keys);

            var step1 = Assert.Single(result1.TrackedSteps[WellKnownGeneratorOutputs.PreCompilationSourceOutput]);
            Assert.Single(step1.Outputs);
            Assert.Equal(IncrementalStepRunReason.New, step1.Outputs[0].Reason);

            // Second run — should show cached
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            var result2 = driver.GetRunResult().Results[0];
            Assert.Contains(WellKnownGeneratorOutputs.PreCompilationSourceOutput, result2.TrackedSteps.Keys);

            var step2 = Assert.Single(result2.TrackedSteps[WellKnownGeneratorOutputs.PreCompilationSourceOutput]);
            Assert.Single(step2.Outputs);
            Assert.Equal(IncrementalStepRunReason.Cached, step2.Outputs[0].Reason);
        }

        #endregion

        #region Step Tracking

        [Fact]
        public void PreCompilation_Has_Distinct_Step_Name()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) => ctx.AddSource("regular", "class RegularType {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: TestOptions.GeneratorDriverOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            var runResult = driver.GetRunResult().Results[0];

            // Both step names should be present and distinct
            Assert.Contains(WellKnownGeneratorOutputs.PreCompilationSourceOutput, runResult.TrackedSteps.Keys);
            Assert.Contains(WellKnownGeneratorOutputs.SourceOutput, runResult.TrackedSteps.Keys);

            // Both should be in output steps
            Assert.Contains(WellKnownGeneratorOutputs.PreCompilationSourceOutput, runResult.TrackedOutputSteps.Keys);
            Assert.Contains(WellKnownGeneratorOutputs.SourceOutput, runResult.TrackedOutputSteps.Keys);
        }

        #endregion

        #region Error Handling

        [Fact]
        public void PreCompilation_Throws_Reports_Error_And_Stops_Generator()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) =>
                {
                    throw new InvalidOperationException("pre-compilation failed");
                });
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    // This should NOT run because the pre-compilation phase failed
                    ctx.AddSource("regular", "class RegularType {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // The generator is in error state
            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<InvalidOperationException>(result.Exception);

            // A diagnostic was reported for the pre-compilation failure
            Assert.NotEmpty(diagnostics);

            // The regular source output was NOT produced (generator was stopped)
            Assert.DoesNotContain(result.GeneratedSources, s => s.HintName == "regular.cs");
        }

        [Fact]
        public void PreCompilation_Throws_Other_Generators_Unaffected()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // Generator 1 throws in pre-compilation
            var generator1 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) =>
                {
                    throw new InvalidOperationException("gen1 failed");
                });
            }));

            // Generator 2 should still run fine
            var generator2 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("gen2", "class Gen2Type {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator1, generator2 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            var runResult = driver.GetRunResult();
            Assert.Equal(2, runResult.Results.Length);

            // Generator 1 is in error state
            Assert.NotNull(runResult.Results[0].Exception);
            Assert.IsType<InvalidOperationException>(runResult.Results[0].Exception);

            // Generator 2 is fine
            Assert.Null(runResult.Results[1].Exception);

            // Generator 2's pre-compilation source should still be present
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("Gen2Type"));

            // A diagnostic was reported for gen1's pre-compilation failure
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void PreCompilation_Accessing_Compilation_Throws()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // A generator that chains from CompilationProvider into pre-compilation output
            // should throw because the compilation is not available during the pre-compilation phase
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.CompilationProvider, (c, comp) =>
                {
                    c.AddSource("bad", "class Bad {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

            // The generator should be in error state due to accessing compilation during pre-compilation
            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.NotNull(result.Exception);

            // A diagnostic was reported
            Assert.NotEmpty(diagnostics);
        }

        #endregion

        #region GeneratedSources

        [Fact]
        public void PreCompilationSources_Appear_In_GeneratedSources()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.Single(result.GeneratedSources);
            Assert.Equal("precomp.cs", result.GeneratedSources[0].HintName);
        }

        [Fact]
        public void PreCompilation_And_Regular_Sources_Both_In_GeneratedSources()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPostInitializationOutput(c => c.AddSource("postinit", "class PostInitType {}"));
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) => ctx.AddSource("regular", "class RegularType {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.Equal(3, result.GeneratedSources.Length);
            Assert.Contains(result.GeneratedSources, s => s.HintName == "postinit.cs");
            Assert.Contains(result.GeneratedSources, s => s.HintName == "precomp.cs");
            Assert.Contains(result.GeneratedSources, s => s.HintName == "regular.cs");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void No_PreCompilationOutputs_Registered_Is_Noop()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) => ctx.AddSource("regular", "class RegularType {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PreCompilation_Empty_Callback_Is_Noop()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => { /* intentionally empty */ });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Single(outputCompilation.SyntaxTrees);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PreCompilation_Shared_Data_Flows_To_SourceOutput()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // This tests the key scenario: a pre-compilation provider's data flows
            // to both the pre-compilation output AND a standard source output via Combine
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                var parsed = ic.ParseOptionsProvider.Select((po, ct) => "SharedData");

                // Use the parsed data to produce a pre-compilation source
                ic.RegisterPreCompilationSourceOutput(parsed, (c, data) =>
                {
                    c.AddSource("decl", $"partial class {data} {{}}");
                });

                // Use the SAME parsed data combined with the compilation to produce an implementation
                var combined = parsed.Combine(ic.CompilationProvider);
                ic.RegisterSourceOutput(combined, (ctx, pair) =>
                {
                    var (data, comp) = pair;
                    // The type from pre-compilation should be visible
                    var type = comp.GetTypeByMetadataName(data);
                    Assert.NotNull(type);
                    ctx.AddSource("impl", $"partial class {data} {{ void Method() {{}} }}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void PreCompilation_IncrementalValuesProvider_Overload()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var additionalText1 = new InMemoryAdditionalText("file1.txt", "content1");
            var additionalText2 = new InMemoryAdditionalText("file2.txt", "content2");

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                // Using the IncrementalValuesProvider overload (multi-value)
                ic.RegisterPreCompilationSourceOutput(ic.AdditionalTextsProvider, (c, file) =>
                {
                    var name = Path.GetFileNameWithoutExtension(file.Path);
                    c.AddSource(name, $"class {name} {{}}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText1, additionalText2 });
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Original + 2 pre-compilation sources
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("file1"));
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("file2"));
        }

        #endregion
    }
}
