// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

#pragma warning disable RSEXPERIMENTAL007 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    [WorkItem("https://github.com/dotnet/roslyn/issues/83089")]
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
            outputCompilation.VerifyDiagnostics();
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

            int callCount = 0;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("a", "class D {}"));

                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    callCount++;
                    // The compilation should include both the original tree and the pre-compilation tree
                    Assert.Equal(2, c.SyntaxTrees.Count());
                    // The type 'D' should be visible in the compilation
                    var typeD = c.GetTypeByMetadataName("D");
                    Assert.NotNull(typeD);
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(1, callCount);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
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

            int callCount = 0;
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
                    callCount++;
                    var type = c.GetTypeByMetadataName("PreCompType");
                    Assert.NotNull(type);
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator1, generator2 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(1, callCount);
            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
        }

        [Fact]
        public void PreCompilationSource_Is_Visible_To_Other_Generators_ReversedOrder()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            int callCount = 0;
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
                    callCount++;
                    var type = c.GetTypeByMetadataName("PreCompType");
                    Assert.NotNull(type);
                });
            }));

            // Register the consuming generator before the producing generator to verify
            // ordering of generator registration does not affect pre-compilation visibility.
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator2, generator1 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(1, callCount);
            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
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
            outputCompilation.VerifyDiagnostics();
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
            outputCompilation.VerifyDiagnostics();
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
            outputCompilation.VerifyDiagnostics();
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
            outputCompilation.VerifyDiagnostics();
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
            outputCompilation.VerifyDiagnostics();
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

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText1, additionalText2, additionalText3 }, driverOptions: TestOptions.GeneratorDriverOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Original + 2 proto files (b.txt is filtered out)
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("a"));
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("c"));
            outputCompilation.VerifyDiagnostics(
                // Microsoft.CodeAnalysis.Test.Utilities\Roslyn.Test.Utilities.TestGenerators.PipelineCallbackGenerator\c.cs(1,7): warning CS8981: The type name 'c' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class c {}
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "c").WithArguments("c").WithLocation(1, 7),
                // Microsoft.CodeAnalysis.Test.Utilities\Roslyn.Test.Utilities.TestGenerators.PipelineCallbackGenerator\a.cs(1,7): warning CS8981: The type name 'a' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class a {}
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "a").WithArguments("a").WithLocation(1, 7));

            // Add another .proto file: pipeline re-evaluates; the filter accepts the new file and a
            // fresh source output step runs for it. The previously-emitted a and c remain cached
            // because their inputs (their own AdditionalText entries) are reference-stable.
            var additionalText4 = new InMemoryAdditionalText("d.proto", "content4");
            driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText4));
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

            Assert.Equal(4, outputCompilation.SyntaxTrees.Count());
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("a"));
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("c"));
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("d"));

            var afterProtoSteps = driver.GetRunResult().Results[0].TrackedSteps[WellKnownGeneratorOutputs.PreCompilationSourceOutput];
            Assert.Equal(3, afterProtoSteps.Length);
            // Two of the per-element source-output steps were already produced last run and stayed
            // cached; one is fresh for d.proto.
            Assert.Equal(2, afterProtoSteps.Count(s => s.Outputs[0].Reason == IncrementalStepRunReason.Cached));
            Assert.Equal(1, afterProtoSteps.Count(s => s.Outputs[0].Reason == IncrementalStepRunReason.New));

            // Add another .txt file: the filter rejects it, so no new element reaches the
            // downstream source-output step. Every existing per-element step stays cached and no
            // new tree is added to the output compilation.
            var additionalText5 = new InMemoryAdditionalText("e.txt", "content5");
            driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText5));
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

            Assert.Equal(4, outputCompilation.SyntaxTrees.Count());
            Assert.Null(outputCompilation.GetTypeByMetadataName("e"));

            var afterTxtSteps = driver.GetRunResult().Results[0].TrackedSteps[WellKnownGeneratorOutputs.PreCompilationSourceOutput];
            Assert.Equal(3, afterTxtSteps.Length);
            Assert.All(afterTxtSteps, step => Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason));
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
            outputCompilation.VerifyDiagnostics();
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

            int callCount = 0;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    callCount++;
                    // Verify pre-compilation type is visible in the compilation
                    Assert.NotNull(c.GetTypeByMetadataName("PreCompType"));
                    ctx.AddSource("regular", "class RegularType {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(1, callCount);
            // Original + PreCompilation + Regular
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
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

            int callCount = 0;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterImplementationSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    callCount++;
                    Assert.NotNull(c.GetTypeByMetadataName("PreCompType"));
                    ctx.AddSource("impl", "class ImplType {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(1, callCount);
            // Original + PreCompilation + Implementation
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
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
            outputCompilation1.VerifyDiagnostics();
            Assert.Equal(1, callCount);

            // Second run with same compilation — should be cached
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out var diagnostics2);
            Assert.Equal(2, outputCompilation2.SyntaxTrees.Count());
            Assert.Empty(diagnostics2);
            outputCompilation2.VerifyDiagnostics();
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
            outputCompilation1.VerifyDiagnostics();
            Assert.Equal(1, callCount);

            // Change additional file
            var newAdditionalText = new InMemoryAdditionalText("test.txt", "TypeB");
            driver = driver.ReplaceAdditionalText(additionalText, newAdditionalText);

            // Second run — should re-run because input changed
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out var diagnostics2);
            Assert.Equal(2, outputCompilation2.SyntaxTrees.Count());
            Assert.NotNull(outputCompilation2.GetTypeByMetadataName("TypeB"));
            outputCompilation2.VerifyDiagnostics();
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
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation1, out _);
            outputCompilation1.VerifyDiagnostics();
            var result1 = driver.GetRunResult().Results[0];
            Assert.Contains(WellKnownGeneratorOutputs.PreCompilationSourceOutput, result1.TrackedSteps.Keys);

            var step1 = Assert.Single(result1.TrackedSteps[WellKnownGeneratorOutputs.PreCompilationSourceOutput]);
            Assert.Single(step1.Outputs);
            Assert.Equal(IncrementalStepRunReason.New, step1.Outputs[0].Reason);

            // Second run — should show cached
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out _);
            outputCompilation2.VerifyDiagnostics();
            var result2 = driver.GetRunResult().Results[0];
            Assert.Contains(WellKnownGeneratorOutputs.PreCompilationSourceOutput, result2.TrackedSteps.Keys);

            var step2 = Assert.Single(result2.TrackedSteps[WellKnownGeneratorOutputs.PreCompilationSourceOutput]);
            Assert.Single(step2.Outputs);
            Assert.Equal(IncrementalStepRunReason.Cached, step2.Outputs[0].Reason);
        }

        [Fact]
        public void PreCompilation_And_Standard_Both_Cached_On_Second_Run()
        {
            // Verifies that a generator with both RegisterPreCompilationSourceOutput and
            // RegisterSourceOutput (consuming CompilationProvider) is fully cached on a second
            // run with the same compilation. Without the augmented-compilation cache, the
            // pre-compilation phase would produce a fresh Compilation reference every run,
            // invalidating the standard phase's CompilationProvider for every generator.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (c, _) => c.AddSource("standard", "class StandardType {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: TestOptions.GeneratorDriverOptions);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation1, out _);
            outputCompilation1.VerifyDiagnostics();
            var result1 = driver.GetRunResult().Results[0];
            var preCompStep1 = Assert.Single(result1.TrackedSteps[WellKnownGeneratorOutputs.PreCompilationSourceOutput]);
            var standardStep1 = Assert.Single(result1.TrackedSteps[WellKnownGeneratorOutputs.SourceOutput]);
            Assert.Equal(IncrementalStepRunReason.New, preCompStep1.Outputs[0].Reason);
            Assert.Equal(IncrementalStepRunReason.New, standardStep1.Outputs[0].Reason);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out _);
            outputCompilation2.VerifyDiagnostics();
            var result2 = driver.GetRunResult().Results[0];
            var preCompStep2 = Assert.Single(result2.TrackedSteps[WellKnownGeneratorOutputs.PreCompilationSourceOutput]);
            var standardStep2 = Assert.Single(result2.TrackedSteps[WellKnownGeneratorOutputs.SourceOutput]);
            Assert.Equal(IncrementalStepRunReason.Cached, preCompStep2.Outputs[0].Reason);
            Assert.Equal(IncrementalStepRunReason.Cached, standardStep2.Outputs[0].Reason);
        }

        [Fact]
        public void PreCompilation_Generator_Does_Not_Invalidate_Other_Generators_CompilationProvider()
        {
            // Cross-generator regression test: a generator that uses
            // RegisterPreCompilationSourceOutput must not invalidate CompilationProvider for
            // other generators in the same driver when nothing has changed between runs.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generatorA = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) => c.AddSource("a", "class TypeFromGenA {}"))));

            var generatorB = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2((ic) =>
                ic.RegisterSourceOutput(ic.CompilationProvider, (c, _) => c.AddSource("b", "class TypeFromGenB {}"))));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generatorA, generatorB }, parseOptions: parseOptions, driverOptions: TestOptions.GeneratorDriverOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            var resultB = driver.GetRunResult().Results[1];
            var standardStep = Assert.Single(resultB.TrackedSteps[WellKnownGeneratorOutputs.SourceOutput]);
            Assert.Equal(IncrementalStepRunReason.Cached, standardStep.Outputs[0].Reason);
        }

        [Fact]
        public void PreCompilation_Generator_Filtered_Other_Generator_Stays_Cached()
        {
            // When generator A (which uses pre-comp) is filtered out on a re-run, generator B
            // must still see a stable compilation -- otherwise filtering one generator would
            // silently invalidate cached state for everyone else.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generatorA = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) => c.AddSource("a", "class TypeFromGenA {}"))));

            var generatorB = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2((ic) =>
                ic.RegisterSourceOutput(ic.CompilationProvider, (c, _) => c.AddSource("b", "class TypeFromGenB {}"))));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generatorA, generatorB }, parseOptions: parseOptions, driverOptions: TestOptions.GeneratorDriverOptions);
            driver = driver.RunGenerators(compilation);

            driver = driver.RunGenerators(compilation, ctx => ctx.Generator != generatorA);
            var resultB = driver.GetRunResult().Results[1];
            var standardStep = Assert.Single(resultB.TrackedSteps[WellKnownGeneratorOutputs.SourceOutput]);
            Assert.Equal(IncrementalStepRunReason.Cached, standardStep.Outputs[0].Reason);
        }

        [Fact]
        public void PreCompilation_RunResult_Trees_Are_In_Output_Compilation()
        {
            // Within-run consistency: trees surfaced in runResult.GeneratedSources for a
            // pre-compilation source must be present in outputCompilation by reference (not
            // just by content) so that user code can call outputCompilation.GetSemanticModel(tree)
            // on those trees.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) => c.AddSource("precomp", "class PreCompType {}"))));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: TestOptions.GeneratorDriverOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            var result = driver.GetRunResult().Results[0];
            var preCompTree = Assert.Single(result.GeneratedSources, s => s.HintName == "precomp.cs").SyntaxTree;
            Assert.Contains(preCompTree, outputCompilation.SyntaxTrees);
            var model = outputCompilation.GetSemanticModel(preCompTree);
            Assert.NotNull(model);
        }

        [Fact]
        public void PreCompilation_Cached_Standard_Diagnostic_Tree_Is_In_Output_Compilation()
        {
            // A standard source output can report diagnostics against pre-compilation trees it
            // observes through CompilationProvider. If the standard output is cached on a later
            // run, the cached diagnostic must still point at a tree in that run's output compilation.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var descriptor = new DiagnosticDescriptor(
                "PCSG001",
                "Test diagnostic",
                "Test diagnostic",
                "Generators",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            int standardCallCount = 0;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    standardCallCount++;
                    var type = c.GetTypeByMetadataName("PreCompType");
                    var location = Assert.Single(type!.Locations);
                    ctx.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(descriptor, location));
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: TestOptions.GeneratorDriverOptions);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation1, out var diagnostics1);
            outputCompilation1.VerifyDiagnostics();
            var diagnostic1 = Assert.Single(diagnostics1);
            var diagnosticTree1 = diagnostic1.Location.SourceTree;
            Assert.NotNull(diagnosticTree1);
            Assert.Contains(diagnosticTree1, outputCompilation1.SyntaxTrees);
            Assert.Equal(1, standardCallCount);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out var diagnostics2);
            outputCompilation2.VerifyDiagnostics();
            var diagnostic2 = Assert.Single(diagnostics2);
            var diagnosticTree2 = diagnostic2.Location.SourceTree;
            Assert.NotNull(diagnosticTree2);
            Assert.Contains(diagnosticTree2, outputCompilation2.SyntaxTrees);
            Assert.Equal(1, standardCallCount);
        }

        [Fact]
        public void PreCompilation_Cached_SyntaxTree_Reference_Is_Stable_Across_Runs()
        {
            // The underlying invariant behind PreCompilation_Cached_Standard_Diagnostic_Tree_Is_In_Output_Compilation:
            // when the upstream pre-compilation callback is cached on a re-run, the parsed
            // SyntaxTree must be reference-equal to the previous run's tree, otherwise
            // anything that captured a reference (Locations, semantic-model lookups, etc.)
            // becomes inconsistent with the run's output compilation.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) => c.AddSource("precomp", "class PreCompType {}"))));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: TestOptions.GeneratorDriverOptions);

            driver = driver.RunGenerators(compilation);
            var preCompTree1 = Assert.Single(driver.GetRunResult().Results[0].GeneratedSources, s => s.HintName == "precomp.cs").SyntaxTree;

            driver = driver.RunGenerators(compilation);
            var preCompTree2 = Assert.Single(driver.GetRunResult().Results[0].GeneratedSources, s => s.HintName == "precomp.cs").SyntaxTree;

            Assert.Same(preCompTree1, preCompTree2);
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
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.VerifyDiagnostics();
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
                    // This should NOT run because the pre-compilation phase failed.
                    Assert.Fail("Standard source output ran even though pre-compilation failed.");
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

            outputCompilation.VerifyDiagnostics();
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
            outputCompilation.VerifyDiagnostics();
        }

        [Fact]
        public void PreCompilation_Trees_Preserved_When_Standard_Phase_Throws()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // A generator whose pre-compilation phase succeeds (adding PreCompType) but whose
            // standard source output throws. The pre-compilation tree was already added to the
            // compilation that other generators may have observed, so it must remain present in
            // the output compilation and the generator's GeneratedSources even though the standard
            // phase failed.
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) => c.AddSource("precomp", "class PreCompType {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    throw new InvalidOperationException("standard phase failed");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);

            // The generator is in error state from the standard phase failure
            Assert.NotNull(result.Exception);
            Assert.IsType<InvalidOperationException>(result.Exception);
            Assert.NotEmpty(diagnostics);

            // But the pre-compilation tree must still be present in the output compilation
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("PreCompType"));
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // And the pre-compilation source must still appear in GeneratedSources for the failing generator
            Assert.Single(result.GeneratedSources);
            Assert.Equal("precomp.cs", result.GeneratedSources[0].HintName);

            outputCompilation.VerifyDiagnostics();
        }

        [Fact]
        [UseCulture("en-US")]
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
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.VerifyDiagnostics();

            // The generator should be in error state due to accessing compilation during pre-compilation
            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<InvalidOperationException>(result.Exception);
            Assert.Contains("pre-compilation", result.Exception!.Message);

            // A diagnostic was reported
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        [UseCulture("en-US")]
        public void PreCompilation_Using_SyntaxProvider_Throws()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // A generator that chains a SyntaxProvider into a pre-compilation source output should
            // surface a clear generator error, because syntax-based providers depend on a built
            // compilation that does not yet exist during the pre-compilation phase.
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                var syntax = ic.SyntaxProvider.CreateSyntaxProvider((n, _) => true, (c, _) => c.Node);
                ic.RegisterPreCompilationSourceOutput(syntax.Collect(), (c, nodes) =>
                {
                    c.AddSource("bad", "class Bad {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.VerifyDiagnostics();

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);

            // Generator should be in error state with a useful InvalidOperationException
            Assert.NotNull(result.Exception);
            Assert.IsType<InvalidOperationException>(result.Exception);
            Assert.Contains("pre-compilation", result.Exception!.Message);

            // The pre-compilation source must NOT have been produced
            Assert.Empty(result.GeneratedSources);
            Assert.Null(outputCompilation.GetTypeByMetadataName("Bad"));

            // A diagnostic was reported for the failure
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void PreCompilation_Failure_Skips_Standard_But_Recovers_On_Next_Run()
        {
            // A pre-compilation failure must skip the standard phase for that generator in the
            // same run, since its PreCompilationTrees were dropped and the standard phase has
            // nothing consistent to run against. On a subsequent run with no other input changes
            // the generator must still get another chance: incremental generator exceptions are
            // recoverable, not sticky.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            int preCompCallCount = 0;
            int standardCallCount = 0;
            bool shouldThrow = true;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) =>
                {
                    preCompCallCount++;
                    if (shouldThrow)
                    {
                        throw new InvalidOperationException("pre-compilation failed");
                    }
                    c.AddSource("precomp", "class PreCompType {}");
                });
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) => standardCallCount++);
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);

            // Run 1: pre-comp throws, standard skipped this run.
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation1, out _);
            Assert.Equal(1, preCompCallCount);
            Assert.Equal(0, standardCallCount);
            var run1Result = driver.GetRunResult().Results[0];
            Assert.NotNull(run1Result.Exception);

            // Run 2: pre-comp now succeeds. Standard must run, generator must recover.
            shouldThrow = false;
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out _);
            Assert.Equal(2, preCompCallCount);
            Assert.Equal(1, standardCallCount);
            var run2Result = driver.GetRunResult().Results[0];
            Assert.Null(run2Result.Exception);
        }

        [Fact]
        public void V1_Generator_Recovers_From_Standard_Phase_Exception_With_No_Input_Changes()
        {
            // Incremental generator exceptions are recoverable: a generator that throws on one
            // run must get another chance on the next run, even if no inputs changed. This test
            // guards that contract specifically for v1-style generators that have no
            // pre-compilation output nodes -- the standard-phase skip-on-failure logic must not
            // inadvertently make their exceptions sticky.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            int callCount = 0;
            bool shouldThrow = true;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    callCount++;
                    if (shouldThrow)
                    {
                        throw new InvalidOperationException("standard failed");
                    }
                    ctx.AddSource("ok", "class Ok {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);

            // Run 1: throws.
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            Assert.Equal(1, callCount);
            Assert.NotNull(driver.GetRunResult().Results[0].Exception);

            // Run 2: same compilation, same inputs, but the generator no longer throws. Must recover.
            shouldThrow = false;
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            Assert.Equal(2, callCount);
            Assert.Null(driver.GetRunResult().Results[0].Exception);
            Assert.Single(driver.GetRunResult().Results[0].GeneratedSources);
        }

        [Fact]
        public void PreCompilation_Throws_With_Warning_Suppressed_Still_Stops_Generator()
        {
            // Even when the source generator failure warning (CS8785) is suppressed via NoWarn,
            // a pre-compilation phase failure must still be recorded on GeneratorState so the
            // standard phase skips this generator. Otherwise the standard phase would proceed
            // with stale/missing pre-comp trees -- silent corruption.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            var options = ((CSharpCompilationOptions)TestOptions.DebugDll)
                .WithSpecificDiagnosticOptions("CS8785", ReportDiagnostic.Suppress);
            Compilation compilation = CreateCompilation(source, options: options, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, t) =>
                {
                    throw new InvalidOperationException("pre-compilation failed");
                });
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    Assert.Fail("Standard source output ran even though pre-compilation failed.");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // The failure is still observable via the run result, even though the warning was suppressed.
            var result = Assert.Single(driver.GetRunResult().Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<InvalidOperationException>(result.Exception);
            // The per-generator run result still carries the failure diagnostic (invariant of GeneratorRunResult).
            Assert.Single(result.Diagnostics);

            // No diagnostic flows to the driver bag because the warning was suppressed by the compilation options.
            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
        }

        [Fact]
        public void Standard_Phase_Throws_With_Warning_Suppressed_Exception_Still_Observable()
        {
            // When the source generator failure warning (CS8785) is suppressed via NoWarn,
            // a standard-phase exception must still be recorded on GeneratorRunResult.Exception
            // so callers (and incremental recovery logic) can observe it.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            var options = ((CSharpCompilationOptions)TestOptions.DebugDll)
                .WithSpecificDiagnosticOptions("CS8785", ReportDiagnostic.Suppress);
            Compilation compilation = CreateCompilation(source, options: options, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterSourceOutput(ic.CompilationProvider, (ctx, c) =>
                {
                    throw new InvalidOperationException("standard phase failed");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            var result = Assert.Single(driver.GetRunResult().Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<InvalidOperationException>(result.Exception);
            Assert.Single(result.Diagnostics);

            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
        }

        [Fact]
        public void Init_Phase_Throws_With_Warning_Suppressed_Exception_Still_Observable()
        {
            // When the source generator init failure warning (CS8784) is suppressed via NoWarn,
            // an init-phase exception must still be recorded on GeneratorRunResult.Exception.
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            var options = ((CSharpCompilationOptions)TestOptions.DebugDll)
                .WithSpecificDiagnosticOptions("CS8784", ReportDiagnostic.Suppress);
            Compilation compilation = CreateCompilation(source, options: options, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                throw new InvalidOperationException("init failed");
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            var result = Assert.Single(driver.GetRunResult().Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<InvalidOperationException>(result.Exception);
            Assert.Single(result.Diagnostics);

            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
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
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.VerifyDiagnostics();

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
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.VerifyDiagnostics();

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
            outputCompilation.VerifyDiagnostics();
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
            outputCompilation.VerifyDiagnostics();
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

            int callCount = 0;
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
                    callCount++;
                    var (data, comp) = pair;
                    // The type from pre-compilation should be visible
                    var type = comp.GetTypeByMetadataName(data);
                    Assert.NotNull(type);
                    ctx.AddSource("impl", $"partial class {data} {{ void Method() {{}} }}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(1, callCount);
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
            outputCompilation.VerifyDiagnostics();
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
            outputCompilation.VerifyDiagnostics();
        }

        [Fact]
        public void PreCompilation_And_Standard_Output_Same_HintName()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // Hint names must be unique across all phases for a single generator. The standard phase
            // tries to emit "shared" but the pre-compilation phase already reserved it, so the
            // standard phase fails. The pre-compilation tree is preserved (other generators saw it).
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) => c.AddSource("shared", "class PreCompClass {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (c, _) => c.AddSource("shared", "class StandardClass {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.VerifyDiagnostics();

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<ArgumentException>(result.Exception);
            Assert.NotEmpty(diagnostics);

            // The pre-compilation tree was committed to the compilation before the standard phase
            // failed, so PreCompClass remains; StandardClass was never committed.
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("PreCompClass"));
            Assert.Null(outputCompilation.GetTypeByMetadataName("StandardClass"));

            // The pre-compilation tree is also surfaced in GeneratedSources so the generator's
            // observable state matches what other generators saw.
            var generatedSource = Assert.Single(result.GeneratedSources);
            Assert.Equal("shared.cs", generatedSource.HintName);
            Assert.Contains("PreCompClass", generatedSource.SourceText.ToString());
        }

        [Fact]
        public void PostInit_And_PreCompilation_Output_Same_HintName()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // Hint names must be unique across all phases for a single generator. The pre-compilation
            // phase tries to emit "shared" but the PostInit phase already reserved it.
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPostInitializationOutput(c => c.AddSource("shared", "class PostInitClass {}"));
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) => c.AddSource("shared", "class PreCompClass {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.VerifyDiagnostics();

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<ArgumentException>(result.Exception);
            Assert.NotEmpty(diagnostics);

            // The PostInit tree is preserved, the colliding pre-compilation tree was not committed.
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("PostInitClass"));
            Assert.Null(outputCompilation.GetTypeByMetadataName("PreCompClass"));
        }

        [Fact]
        public void PostInit_And_Standard_Output_Same_HintName()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // Hint names must be unique across all phases for a single generator. The standard
            // phase tries to emit "shared" but the PostInit phase already reserved it.
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPostInitializationOutput(c => c.AddSource("shared", "class PostInitClass {}"));
                ic.RegisterSourceOutput(ic.CompilationProvider, (c, _) => c.AddSource("shared", "class StandardClass {}"));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.VerifyDiagnostics();

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<ArgumentException>(result.Exception);
            Assert.NotEmpty(diagnostics);

            // The PostInit tree is preserved; the colliding standard tree was not committed.
            Assert.NotNull(outputCompilation.GetTypeByMetadataName("PostInitClass"));
            Assert.Null(outputCompilation.GetTypeByMetadataName("StandardClass"));
        }

        [Fact]
        public void PreCompilation_HintName_Conflict_Within_PreCompilation_Phase_Throws()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            // Two pre-compilation outputs in the same generator using the same hint name should
            // surface a generator error from the in-phase AdditionalSourcesCollection.
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.ParseOptionsProvider, (c, _) =>
                {
                    c.AddSource("dup", "class A {}");
                    c.AddSource("dup", "class B {}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.VerifyDiagnostics();

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.NotNull(result.Exception);
            Assert.IsType<ArgumentException>(result.Exception);

            // No pre-compilation source was committed, so neither type made it into the compilation
            Assert.Null(outputCompilation.GetTypeByMetadataName("A"));
            Assert.Null(outputCompilation.GetTypeByMetadataName("B"));
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void PreCompilation_Can_Access_CompilationOptions()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            OutputKind? observedKind = null;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.CompilationOptionsProvider, (c, opts) =>
                {
                    observedKind = opts.OutputKind;
                    c.AddSource("opts", $"// kind: {opts.OutputKind}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.VerifyDiagnostics();
            Assert.Empty(diagnostics);

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.Null(result.Exception);
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, observedKind);
            Assert.Single(result.GeneratedSources);
            Assert.Equal("opts.cs", result.GeneratedSources[0].HintName);
        }

        [Fact]
        public void PreCompilation_Can_Access_MetadataReferences()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            var expectedReferenceCount = compilation.ExternalReferences.Length;

            int observedReferenceCount = -1;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) =>
            {
                ic.RegisterPreCompilationSourceOutput(ic.MetadataReferencesProvider.Collect(), (c, refs) =>
                {
                    observedReferenceCount = refs.Length;
                    c.AddSource("refs", $"// refs: {refs.Length}");
                });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
            outputCompilation.VerifyDiagnostics();
            Assert.Empty(diagnostics);

            var runResult = driver.GetRunResult();
            var result = Assert.Single(runResult.Results);
            Assert.Null(result.Exception);
            Assert.Equal(expectedReferenceCount, observedReferenceCount);
            Assert.Single(result.GeneratedSources);
        }

        #endregion
    }
}
