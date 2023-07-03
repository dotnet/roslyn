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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class GeneratorDriverTests
         : CSharpTestBase
    {
        [Fact]
        public void Running_With_No_Changes_Is_NoOp()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(ImmutableArray<ISourceGenerator>.Empty, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Empty(diagnostics);
            Assert.Single(outputCompilation.SyntaxTrees);
            Assert.Equal(compilation, outputCompilation);
        }

        [Fact]
        public void Generator_Is_Initialized_Before_Running()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            int initCount = 0, executeCount = 0;
            var generator = new CallbackGenerator((ic) => initCount++, (sgc) => executeCount++);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(1, initCount);
            Assert.Equal(1, executeCount);
        }

        [Fact]
        public void Generator_Is_Not_Initialized_If_Not_Run()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            int initCount = 0, executeCount = 0;
            var generator = new CallbackGenerator((ic) => initCount++, (sgc) => executeCount++);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);

            Assert.Equal(0, initCount);
            Assert.Equal(0, executeCount);
        }

        [Fact]
        public void Generator_Is_Only_Initialized_Once()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            int initCount = 0, executeCount = 0;
            var generator = new CallbackGenerator((ic) => initCount++, (sgc) => executeCount++, source: "public class C { }");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            driver = driver.RunGeneratorsAndUpdateCompilation(outputCompilation, out outputCompilation, out _);
            driver.RunGeneratorsAndUpdateCompilation(outputCompilation, out outputCompilation, out _);

            Assert.Equal(1, initCount);
            Assert.Equal(3, executeCount);
        }

        [Fact]
        public void Single_File_Is_Added()
        {
            var source = @"
class C { }
";

            var generatorSource = @"
class GeneratedClass { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            SingleFileTestGenerator testGenerator = new SingleFileTestGenerator(generatorSource);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.NotEqual(compilation, outputCompilation);

            var generatedClass = outputCompilation.GlobalNamespace.GetTypeMembers("GeneratedClass").Single();
            Assert.True(generatedClass.Locations.Single().IsInSource);
        }

        [Fact]
        public void Analyzer_Is_Run()
        {
            var source = @"
class C { }
";

            var generatorSource = @"
class GeneratedClass { }
";

            var parseOptions = TestOptions.Regular;
            var analyzer = new Analyzer_Is_Run_Analyzer();

            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            compilation.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(0, analyzer.GeneratedClassCount);

            SingleFileTestGenerator testGenerator = new SingleFileTestGenerator(generatorSource);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            outputCompilation.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.GeneratedClassCount);
        }

        private class Analyzer_Is_Run_Analyzer : DiagnosticAnalyzer
        {
            public int GeneratedClassCount;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(Handle, SymbolKind.NamedType);
            }

            private void Handle(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "GeneratedClass":
                        Interlocked.Increment(ref GeneratedClassCount);
                        break;
                    case "C":
                    case "System.Runtime.CompilerServices.IsExternalInit":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void Single_File_Is_Added_OnlyOnce_For_Multiple_Calls()
        {
            var source = @"
class C { }
";

            var generatorSource = @"
class GeneratedClass { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            SingleFileTestGenerator testGenerator = new SingleFileTestGenerator(generatorSource);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation1, out _);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation2, out _);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation3, out _);

            Assert.Equal(2, outputCompilation1.SyntaxTrees.Count());
            Assert.Equal(2, outputCompilation2.SyntaxTrees.Count());
            Assert.Equal(2, outputCompilation3.SyntaxTrees.Count());

            Assert.NotEqual(compilation, outputCompilation1);
            Assert.NotEqual(compilation, outputCompilation2);
            Assert.NotEqual(compilation, outputCompilation3);
        }

        [Fact]
        public void User_Source_Can_Depend_On_Generated_Source()
        {
            var source = @"
#pragma warning disable CS0649
class C 
{
    public D d;
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics(
                // (5,12): error CS0246: The type or namespace name 'D' could not be found (are you missing a using directive or an assembly reference?)
                //     public D d;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("D").WithLocation(5, 12)
                );

            Assert.Single(compilation.SyntaxTrees);

            var generator = new SingleFileTestGenerator("public class D { }");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
            generatorDiagnostics.Verify();
        }

        [Fact]
        public void Error_During_Initialization_Is_Reported()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var exception = new InvalidOperationException("init error");

            var generator = new CallbackGenerator((ic) => throw exception, (sgc) => { });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
            generatorDiagnostics.Verify(
                    // warning CS8784: Generator 'CallbackGenerator' failed to initialize. It will not contribute to the output and compilation errors may occur as a result. Exception was 'InvalidOperationException' with message 'init error'
                    Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringInitialization).WithArguments("CallbackGenerator", "InvalidOperationException", "init error").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Error_During_Initialization_Generator_Does_Not_Run()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var exception = new InvalidOperationException("init error");
            var generator = new CallbackGenerator((ic) => throw exception, (sgc) => { }, source: "class D { }");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            Assert.Single(outputCompilation.SyntaxTrees);
        }

        [Fact]
        public void Error_During_Generation_Is_Reported()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var exception = new InvalidOperationException("generate error");

            var generator = new CallbackGenerator((ic) => { }, (sgc) => throw exception);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
            generatorDiagnostics.Verify(
                 // warning CS8785: Generator 'CallbackGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was 'InvalidOperationException' with message 'generate error'
                 Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator", "InvalidOperationException", "generate error").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Error_During_Generation_Does_Not_Affect_Other_Generators()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var exception = new InvalidOperationException("generate error");

            var generator = new CallbackGenerator((ic) => { }, (sgc) => throw exception);
            var generator2 = new CallbackGenerator2((ic) => { }, (sgc) => { }, source: "public class D { }");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator, generator2 }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            generatorDiagnostics.Verify(
                 // warning CS8785: Generator 'CallbackGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was 'InvalidOperationException' with message 'generate error'
                 Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator", "InvalidOperationException", "generate error").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Error_During_Generation_With_Dependent_Source()
        {
            var source = @"
#pragma warning disable CS0649
class C 
{
    public D d;
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics(
                    // (5,12): error CS0246: The type or namespace name 'D' could not be found (are you missing a using directive or an assembly reference?)
                    //     public D d;
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("D").WithLocation(5, 12)
                    );

            Assert.Single(compilation.SyntaxTrees);

            var exception = new InvalidOperationException("generate error");

            var generator = new CallbackGenerator((ic) => { }, (sgc) => throw exception, source: "public class D { }");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics(
                // (5,12): error CS0246: The type or namespace name 'D' could not be found (are you missing a using directive or an assembly reference?)
                //     public D d;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "D").WithArguments("D").WithLocation(5, 12)
                );
            generatorDiagnostics.Verify(
                // warning CS8785: Generator 'CallbackGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was 'InvalidOperationException' with message 'generate error'
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator", "InvalidOperationException", "generate error").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Error_During_Generation_Has_Exception_In_Description()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var exception = new InvalidOperationException("generate error");

            var generator = new CallbackGenerator((ic) => { }, (sgc) => throw exception);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();

            // Since translated description strings can have punctuation that differs based on locale, simply ensure the
            // exception message is contains in the diagnostic description.
            Assert.Contains(exception.ToString(), generatorDiagnostics.Single().Descriptor.Description.ToString());
        }

        [Fact]
        public void Generator_Can_Report_Diagnostics()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            string description = "This is a test diagnostic";
            DiagnosticDescriptor generatorDiagnostic = new DiagnosticDescriptor("TG001", "Test Diagnostic", description, "Generators", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);
            var diagnostic = Microsoft.CodeAnalysis.Diagnostic.Create(generatorDiagnostic, Location.None);

            var generator = new CallbackGenerator((ic) => { }, (sgc) => sgc.ReportDiagnostic(diagnostic));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
            generatorDiagnostics.Verify(
                Diagnostic("TG001").WithLocation(1, 1)
                );
        }

        [Fact]
        public void Generator_HintName_MustBe_Unique()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new CallbackGenerator((ic) => { }, (sgc) =>
            {
                sgc.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8));

                // the assert should swallow the exception, so we'll actually successfully generate
                Assert.Throws<ArgumentException>("hintName", () => sgc.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8)));

                // also throws for <name> vs <name>.cs
                Assert.Throws<ArgumentException>("hintName", () => sgc.AddSource("test.cs", SourceText.From("public class D{}", Encoding.UTF8)));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
            outputCompilation.VerifyDiagnostics();
            generatorDiagnostics.Verify();
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void Generator_HintName_MustBe_Unique_Across_Outputs()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview);
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) =>
                {
                    spc.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8));

                    // throws immediately, because we're within the same output node
                    Assert.Throws<ArgumentException>("hintName", () => spc.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8)));

                    // throws for .cs too
                    Assert.Throws<ArgumentException>("hintName", () => spc.AddSource("test.cs", SourceText.From("public class D{}", Encoding.UTF8)));
                });

                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) =>
                {
                    // will not throw at this point, because we have no way of knowing what the other outputs added
                    // we *will* throw later in the driver when we combine them however (this is a change for V2, but not visible from V1)
                    spc.AddSource("test", SourceText.From("public class D{}", Encoding.UTF8));
                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
            outputCompilation.VerifyDiagnostics();
            generatorDiagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(PipelineCallbackGenerator), "The hintName 'test.cs' of the added source file must be unique within a generator.", "hintName").WithLocation(1, 1)
                );
            Assert.Equal(1, outputCompilation.SyntaxTrees.Count());
        }

        [Fact]
        public void Generator_HintName_Is_Appended_With_GeneratorName()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new SingleFileTestGenerator("public class D {}", "source.cs");
            var generator2 = new SingleFileTestGenerator2("public class E {}", "source.cs");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator, generator2 }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
            outputCompilation.VerifyDiagnostics();
            generatorDiagnostics.Verify();
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());

            var filePaths = outputCompilation.SyntaxTrees.Skip(1).Select(t => t.FilePath).ToArray();
            Assert.Equal(new[] {
                Path.Combine(generator.GetType().Assembly.GetName().Name!, generator.GetType().FullName!, "source.cs"),
                Path.Combine(generator2.GetType().Assembly.GetName().Name!, generator2.GetType().FullName!, "source.cs")
            }, filePaths);
        }

        [Fact]
        public void RunResults_Are_Empty_Before_Generation()
        {
            GeneratorDriver driver = CSharpGeneratorDriver.Create(ImmutableArray<ISourceGenerator>.Empty, parseOptions: TestOptions.Regular);
            var results = driver.GetRunResult();

            Assert.Empty(results.GeneratedTrees);
            Assert.Empty(results.Diagnostics);
            Assert.Empty(results.Results);
        }

        [Fact]
        public void RunResults_Are_Available_After_Generation()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new CallbackGenerator((ic) => { }, (sgc) => { sgc.AddSource("test", SourceText.From("public class D {}", Encoding.UTF8)); });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();

            Assert.Single(results.GeneratedTrees);
            Assert.Single(results.Results);
            Assert.Empty(results.Diagnostics);

            var result = results.Results.Single();

            Assert.Null(result.Exception);
            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedSources);
            Assert.Equal(results.GeneratedTrees.Single(), result.GeneratedSources.Single().SyntaxTree);
        }

        [Fact]
        public void RunResults_Combine_SyntaxTrees()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new CallbackGenerator((ic) => { }, (sgc) => { sgc.AddSource("test", SourceText.From("public class D {}", Encoding.UTF8)); sgc.AddSource("test2", SourceText.From("public class E {}", Encoding.UTF8)); });
            var generator2 = new SingleFileTestGenerator("public class F{}");
            var generator3 = new SingleFileTestGenerator2("public class G{}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator, generator2, generator3 }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();

            Assert.Equal(4, results.GeneratedTrees.Length);
            Assert.Equal(3, results.Results.Length);
            Assert.Empty(results.Diagnostics);

            var result1 = results.Results[0];
            var result2 = results.Results[1];
            var result3 = results.Results[2];

            Assert.Null(result1.Exception);
            Assert.Empty(result1.Diagnostics);
            Assert.Equal(2, result1.GeneratedSources.Length);
            Assert.Equal(results.GeneratedTrees[0], result1.GeneratedSources[0].SyntaxTree);
            Assert.Equal(results.GeneratedTrees[1], result1.GeneratedSources[1].SyntaxTree);

            Assert.Null(result2.Exception);
            Assert.Empty(result2.Diagnostics);
            Assert.Single(result2.GeneratedSources);
            Assert.Equal(results.GeneratedTrees[2], result2.GeneratedSources[0].SyntaxTree);

            Assert.Null(result3.Exception);
            Assert.Empty(result3.Diagnostics);
            Assert.Single(result3.GeneratedSources);
            Assert.Equal(results.GeneratedTrees[3], result3.GeneratedSources[0].SyntaxTree);
        }

        [Fact]
        public void RunResults_Combine_Diagnostics()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            string description = "This is a test diagnostic";
            DiagnosticDescriptor generatorDiagnostic1 = new DiagnosticDescriptor("TG001", "Test Diagnostic", description, "Generators", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);
            DiagnosticDescriptor generatorDiagnostic2 = new DiagnosticDescriptor("TG002", "Test Diagnostic", description, "Generators", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);
            DiagnosticDescriptor generatorDiagnostic3 = new DiagnosticDescriptor("TG003", "Test Diagnostic", description, "Generators", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);

            var diagnostic1 = Microsoft.CodeAnalysis.Diagnostic.Create(generatorDiagnostic1, Location.None);
            var diagnostic2 = Microsoft.CodeAnalysis.Diagnostic.Create(generatorDiagnostic2, Location.None);
            var diagnostic3 = Microsoft.CodeAnalysis.Diagnostic.Create(generatorDiagnostic3, Location.None);

            var generator = new CallbackGenerator((ic) => { }, (sgc) => { sgc.ReportDiagnostic(diagnostic1); sgc.ReportDiagnostic(diagnostic2); });
            var generator2 = new CallbackGenerator2((ic) => { }, (sgc) => { sgc.ReportDiagnostic(diagnostic3); });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator, generator2 }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();

            Assert.Equal(2, results.Results.Length);
            Assert.Equal(3, results.Diagnostics.Length);
            Assert.Empty(results.GeneratedTrees);

            var result1 = results.Results[0];
            var result2 = results.Results[1];

            Assert.Null(result1.Exception);
            Assert.Equal(2, result1.Diagnostics.Length);
            Assert.Empty(result1.GeneratedSources);
            Assert.Equal(results.Diagnostics[0], result1.Diagnostics[0]);
            Assert.Equal(results.Diagnostics[1], result1.Diagnostics[1]);

            Assert.Null(result2.Exception);
            Assert.Single(result2.Diagnostics);
            Assert.Empty(result2.GeneratedSources);
            Assert.Equal(results.Diagnostics[2], result2.Diagnostics[0]);
        }

        [Fact]
        public void FullGeneration_Diagnostics_AreSame_As_RunResults()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            string description = "This is a test diagnostic";
            DiagnosticDescriptor generatorDiagnostic1 = new DiagnosticDescriptor("TG001", "Test Diagnostic", description, "Generators", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);
            DiagnosticDescriptor generatorDiagnostic2 = new DiagnosticDescriptor("TG002", "Test Diagnostic", description, "Generators", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);
            DiagnosticDescriptor generatorDiagnostic3 = new DiagnosticDescriptor("TG003", "Test Diagnostic", description, "Generators", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: description);

            var diagnostic1 = Microsoft.CodeAnalysis.Diagnostic.Create(generatorDiagnostic1, Location.None);
            var diagnostic2 = Microsoft.CodeAnalysis.Diagnostic.Create(generatorDiagnostic2, Location.None);
            var diagnostic3 = Microsoft.CodeAnalysis.Diagnostic.Create(generatorDiagnostic3, Location.None);

            var generator = new CallbackGenerator((ic) => { }, (sgc) => { sgc.ReportDiagnostic(diagnostic1); sgc.ReportDiagnostic(diagnostic2); });
            var generator2 = new CallbackGenerator2((ic) => { }, (sgc) => { sgc.ReportDiagnostic(diagnostic3); });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator, generator2 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var fullDiagnostics);

            var results = driver.GetRunResult();

            Assert.Equal(3, results.Diagnostics.Length);
            Assert.Equal(3, fullDiagnostics.Length);
            AssertEx.Equal(results.Diagnostics, fullDiagnostics);
        }

        [Fact]
        public void Cancellation_During_Execution_Doesnt_Report_As_Generator_Error()
        {
            var source = @"
class C 
{
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            CancellationTokenSource cts = new CancellationTokenSource();

            var testGenerator = new CallbackGenerator(
                onInit: (i) => { },
                onExecute: (e) => { cts.Cancel(); }
                );

            // test generator cancels the token. Check that the call to this generator doesn't make it look like it errored.
            var testGenerator2 = new CallbackGenerator2(
                onInit: (i) => { },
                onExecute: (e) =>
                {
                    e.AddSource("a", SourceText.From("public class E {}", Encoding.UTF8));
                    e.CancellationToken.ThrowIfCancellationRequested();
                });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator, testGenerator2 }, parseOptions: parseOptions);
            var oldDriver = driver;

            Assert.Throws<OperationCanceledException>(() =>
               driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics, cts.Token)
               );
            Assert.Same(oldDriver, driver);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void Adding_A_Source_Text_Without_Encoding_Fails_Generation()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new CallbackGenerator((ic) => { }, (sgc) => { sgc.AddSource("a", SourceText.From("")); });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var outputDiagnostics);

            Assert.Single(outputDiagnostics);
            outputDiagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(CallbackGenerator), "The SourceText with hintName 'a.cs' must have an explicit encoding set.", "source").WithLocation(1, 1)
                );
        }

        [Fact]
        public void ParseOptions_Are_Passed_To_Generator()
        {
            var source = @"
class C 
{
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            ParseOptions? passedOptions = null;
            var testGenerator = new CallbackGenerator(
                onInit: (i) => { },
                onExecute: (e) => { passedOptions = e.ParseOptions; }
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.Same(parseOptions, passedOptions);
        }

        [Fact]
        public void AdditionalFiles_Are_Passed_To_Generator()
        {
            var source = @"
class C 
{
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var texts = ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("a", "abc"), new InMemoryAdditionalText("b", "def"));

            ImmutableArray<AdditionalText> passedIn = default;
            var testGenerator = new CallbackGenerator(
                onInit: (i) => { },
                onExecute: (e) => passedIn = e.AdditionalFiles
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions, additionalTexts: texts);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.Equal(2, passedIn.Length);
            Assert.Equal<AdditionalText>(texts, passedIn);
        }

        [Fact]
        public void AnalyzerConfigOptions_Are_Passed_To_Generator()
        {
            var source = @"
class C 
{
}
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var options = new CompilerAnalyzerConfigOptionsProvider(ImmutableDictionary<object, AnalyzerConfigOptions>.Empty, new DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty.Add("a", "abc").Add("b", "def")));

            AnalyzerConfigOptionsProvider? passedIn = null;
            var testGenerator = new CallbackGenerator(
                onInit: (i) => { },
                onExecute: (e) => passedIn = e.AnalyzerConfigOptions
                );

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions, optionsProvider: options);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

            Assert.NotNull(passedIn);

            Assert.True(passedIn!.GlobalOptions.TryGetValue("a", out var item1));
            Assert.Equal("abc", item1);

            Assert.True(passedIn!.GlobalOptions.TryGetValue("b", out var item2));
            Assert.Equal("def", item2);
        }

        [Fact]
        public void Generator_Can_Provide_Source_In_PostInit()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            static void postInit(GeneratorPostInitializationContext context)
            {
                context.AddSource("postInit", "public class D {} ");
            }

            var generator = new CallbackGenerator((ic) => ic.RegisterForPostInitialization(postInit), (sgc) => { });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            outputCompilation.VerifyDiagnostics();
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
        }

        [Fact]
        public void PostInit_Source_Is_Available_During_Execute()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            static void postInit(GeneratorPostInitializationContext context)
            {
                context.AddSource("postInit", "public class D {} ");
            }

            INamedTypeSymbol? dSymbol = null;
            var generator = new CallbackGenerator((ic) => ic.RegisterForPostInitialization(postInit), (sgc) => { dSymbol = sgc.Compilation.GetTypeByMetadataName("D"); }, source = "public class E : D {}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            outputCompilation.VerifyDiagnostics();
            Assert.NotNull(dSymbol);
        }

        [Fact]
        public void PostInit_Source_Is_Available_To_Other_Generators_During_Execute()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            static void postInit(GeneratorPostInitializationContext context)
            {
                context.AddSource("postInit", "public class D {} ");
            }

            INamedTypeSymbol? dSymbol = null;
            var generator = new CallbackGenerator((ic) => ic.RegisterForPostInitialization(postInit), (sgc) => { });
            var generator2 = new CallbackGenerator2((ic) => { }, (sgc) => { dSymbol = sgc.Compilation.GetTypeByMetadataName("D"); }, source = "public class E : D {}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator, generator2 }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            outputCompilation.VerifyDiagnostics();
            Assert.NotNull(dSymbol);
        }

        [Fact]
        public void PostInit_Is_Only_Called_Once()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);
            int postInitCount = 0;
            int executeCount = 0;

            void postInit(GeneratorPostInitializationContext context)
            {
                context.AddSource("postInit", "public class D {} ");
                postInitCount++;
            }

            var generator = new CallbackGenerator((ic) => ic.RegisterForPostInitialization(postInit), (sgc) => executeCount++, source = "public class E : D {}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            outputCompilation.VerifyDiagnostics();
            Assert.Equal(1, postInitCount);
            Assert.Equal(3, executeCount);
        }

        [Fact]
        public void Error_During_PostInit_Is_Reported()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            static void postInit(GeneratorPostInitializationContext context)
            {
                context.AddSource("postInit", "public class D {} ");
                throw new InvalidOperationException("post init error");
            }

            var generator = new CallbackGenerator((ic) => ic.RegisterForPostInitialization(postInit), (sgc) => Assert.True(false, "Should not execute"), source = "public class E : D {}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            generatorDiagnostics.Verify(
                 // warning CS8784: Generator 'CallbackGenerator' failed to initialize. It will not contribute to the output and compilation errors may occur as a result. Exception was 'InvalidOperationException' with message 'post init error'
                 Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringInitialization).WithArguments("CallbackGenerator", "InvalidOperationException", "post init error").WithLocation(1, 1)
             );
        }

        [Fact]
        public void Error_During_Initialization_PostInit_Does_Not_Run()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            static void init(GeneratorInitializationContext context)
            {
                context.RegisterForPostInitialization(postInit);
                throw new InvalidOperationException("init error");
            }

            static void postInit(GeneratorPostInitializationContext context)
            {
                context.AddSource("postInit", "public class D {} ");
                Assert.True(false, "Should not execute");
            }

            var generator = new CallbackGenerator(init, (sgc) => Assert.True(false, "Should not execute"), source = "public class E : D {}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            outputCompilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            generatorDiagnostics.Verify(
                 // warning CS8784: Generator 'CallbackGenerator' failed to initialize. It will not contribute to the output and compilation errors may occur as a result. Exception was 'InvalidOperationException' with message 'init error'
                 Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringInitialization).WithArguments("CallbackGenerator", "InvalidOperationException", "init error").WithLocation(1, 1)
             );
        }

        [Fact]
        public void PostInit_SyntaxTrees_Are_Available_In_RunResults()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new CallbackGenerator((ic) => ic.RegisterForPostInitialization(pic => pic.AddSource("postInit", "public class D{}")), (sgc) => { }, "public class E{}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();

            Assert.Single(results.Results);
            Assert.Empty(results.Diagnostics);

            var result = results.Results[0];
            Assert.Null(result.Exception);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(2, result.GeneratedSources.Length);
        }

        [Fact]
        public void PostInit_SyntaxTrees_Are_Combined_In_RunResults()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new CallbackGenerator((ic) => ic.RegisterForPostInitialization(pic => pic.AddSource("postInit", "public class D{}")), (sgc) => { }, "public class E{}");
            var generator2 = new SingleFileTestGenerator("public class F{}");
            var generator3 = new SingleFileTestGenerator2("public class G{}");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator, generator2, generator3 }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();

            Assert.Equal(4, results.GeneratedTrees.Length);
            Assert.Equal(3, results.Results.Length);
            Assert.Empty(results.Diagnostics);

            var result1 = results.Results[0];
            var result2 = results.Results[1];
            var result3 = results.Results[2];

            Assert.Null(result1.Exception);
            Assert.Empty(result1.Diagnostics);
            Assert.Equal(2, result1.GeneratedSources.Length);
            Assert.Equal(results.GeneratedTrees[0], result1.GeneratedSources[0].SyntaxTree);
            Assert.Equal(results.GeneratedTrees[1], result1.GeneratedSources[1].SyntaxTree);

            Assert.Null(result2.Exception);
            Assert.Empty(result2.Diagnostics);
            Assert.Single(result2.GeneratedSources);
            Assert.Equal(results.GeneratedTrees[2], result2.GeneratedSources[0].SyntaxTree);

            Assert.Null(result3.Exception);
            Assert.Empty(result3.Diagnostics);
            Assert.Single(result3.GeneratedSources);
            Assert.Equal(results.GeneratedTrees[3], result3.GeneratedSources[0].SyntaxTree);
        }

        [Fact]
        public void SyntaxTrees_Are_Lazy()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new SingleFileTestGenerator("public class D {}", "source.cs");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            var results = driver.GetRunResult();

            var tree = Assert.Single(results.GeneratedTrees);

            Assert.False(tree.TryGetRoot(out _));
            var rootFromGetRoot = tree.GetRoot();
            Assert.NotNull(rootFromGetRoot);
            Assert.True(tree.TryGetRoot(out var rootFromTryGetRoot));
            Assert.Same(rootFromGetRoot, rootFromTryGetRoot);
        }

        [Fact]
        public void Diagnostics_Respect_Suppression()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);
            CallbackGenerator gen = new CallbackGenerator((c) => { }, (c) =>
            {
                c.ReportDiagnostic(CSDiagnostic.Create("GEN001", "generators", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 2));
                c.ReportDiagnostic(CSDiagnostic.Create("GEN002", "generators", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 3));
            });

            var options = ((CSharpCompilationOptions)compilation.Options);

            // generator driver diagnostics are reported separately from the compilation
            verifyDiagnosticsWithOptions(options,
                Diagnostic("GEN001").WithLocation(1, 1),
                Diagnostic("GEN002").WithLocation(1, 1));

            // warnings can be individually suppressed
            verifyDiagnosticsWithOptions(options.WithSpecificDiagnosticOptions("GEN001", ReportDiagnostic.Suppress),
                Diagnostic("GEN002").WithLocation(1, 1));

            verifyDiagnosticsWithOptions(options.WithSpecificDiagnosticOptions("GEN002", ReportDiagnostic.Suppress),
                Diagnostic("GEN001").WithLocation(1, 1));

            // warning level is respected
            verifyDiagnosticsWithOptions(options.WithWarningLevel(0));

            verifyDiagnosticsWithOptions(options.WithWarningLevel(2),
                Diagnostic("GEN001").WithLocation(1, 1));

            verifyDiagnosticsWithOptions(options.WithWarningLevel(3),
                Diagnostic("GEN001").WithLocation(1, 1),
                Diagnostic("GEN002").WithLocation(1, 1));

            // warnings can be upgraded to errors
            verifyDiagnosticsWithOptions(options.WithSpecificDiagnosticOptions("GEN001", ReportDiagnostic.Error),
                Diagnostic("GEN001").WithLocation(1, 1).WithWarningAsError(true),
                Diagnostic("GEN002").WithLocation(1, 1));

            verifyDiagnosticsWithOptions(options.WithSpecificDiagnosticOptions("GEN002", ReportDiagnostic.Error),
                Diagnostic("GEN001").WithLocation(1, 1),
                Diagnostic("GEN002").WithLocation(1, 1).WithWarningAsError(true));

            void verifyDiagnosticsWithOptions(CompilationOptions options, params DiagnosticDescription[] expected)
            {
                GeneratorDriver driver = CSharpGeneratorDriver.Create(ImmutableArray.Create(gen), parseOptions: parseOptions);
                var updatedCompilation = compilation.WithOptions(options);

                driver.RunGeneratorsAndUpdateCompilation(updatedCompilation, out var outputCompilation, out var diagnostics);
                outputCompilation.VerifyDiagnostics();
                diagnostics.Verify(expected);
            }
        }

        [Fact]
        public void Diagnostics_Respect_Pragma_Suppression()
        {
            var gen001 = CSDiagnostic.Create("GEN001", "generators", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 2);

            // reported diagnostics can have a location in source
            verifyDiagnosticsWithSource("//comment",
                new[] { (gen001, TextSpan.FromBounds(2, 5)) },
                Diagnostic("GEN001", "com").WithLocation(1, 3));

            // diagnostics are suppressed via #pragma
            verifyDiagnosticsWithSource(
@"#pragma warning disable
//comment",
                new[] { (gen001, TextSpan.FromBounds(27, 30)) },
                Diagnostic("GEN001", "com", isSuppressed: true).WithLocation(2, 3));

            // but not when they don't have a source location
            verifyDiagnosticsWithSource(
@"#pragma warning disable
//comment",
                new[] { (gen001, new TextSpan(0, 0)) },
                Diagnostic("GEN001").WithLocation(1, 1));

            // can be suppressed explicitly
            verifyDiagnosticsWithSource(
@"#pragma warning disable GEN001
//comment",
                new[] { (gen001, TextSpan.FromBounds(34, 37)) },
                Diagnostic("GEN001", "com", isSuppressed: true).WithLocation(2, 3));

            // suppress + restore
            verifyDiagnosticsWithSource(
@"#pragma warning disable GEN001
//comment
#pragma warning restore GEN001
//another",
                new[] { (gen001, TextSpan.FromBounds(34, 37)), (gen001, TextSpan.FromBounds(77, 80)) },
                Diagnostic("GEN001", "com", isSuppressed: true).WithLocation(2, 3),
                Diagnostic("GEN001", "ano").WithLocation(4, 3));

            void verifyDiagnosticsWithSource(string source, (Diagnostic, TextSpan)[] reportDiagnostics, params DiagnosticDescription[] expected)
            {
                var parseOptions = TestOptions.Regular;
                source = source.Replace(Environment.NewLine, "\r\n");
                Compilation compilation = CreateCompilation(source, sourceFileName: "sourcefile.cs", options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
                compilation.VerifyDiagnostics();
                Assert.Single(compilation.SyntaxTrees);

                CallbackGenerator gen = new CallbackGenerator((c) => { }, (c) =>
                {
                    foreach ((var d, var l) in reportDiagnostics)
                    {
                        if (l.IsEmpty)
                        {
                            c.ReportDiagnostic(d);
                        }
                        else
                        {
                            c.ReportDiagnostic(d.WithLocation(Location.Create(c.Compilation.SyntaxTrees.First(), l)));
                        }
                    }
                });
                GeneratorDriver driver = CSharpGeneratorDriver.Create(ImmutableArray.Create(gen), parseOptions: parseOptions);

                driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
                outputCompilation.VerifyDiagnostics();
                diagnostics.Verify(expected);
            }
        }

        [Fact, WorkItem(66337, "https://github.com/dotnet/roslyn/issues/66337")]
        public void Diagnostics_Respect_SuppressMessageAttribute()
        {
            var gen001 = CSDiagnostic.Create("GEN001", "generators", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, isEnabledByDefault: true, warningLevel: 2);

            // reported diagnostics can have a location in source
            verify("""
                class C
                {
                    //comment
                }
                """,
                new[] { (gen001, "com") },
                Diagnostic("GEN001", "com").WithLocation(3, 7));

            // diagnostics are suppressed via SuppressMessageAttribute
            verify("""
                [System.Diagnostics.CodeAnalysis.SuppressMessage("", "GEN001")]
                class C
                {
                    //comment
                }
                """,
                new[] { (gen001, "com") },
                Diagnostic("GEN001", "com", isSuppressed: true).WithLocation(4, 7));

            // but not when they don't have a source location
            verify("""
                [System.Diagnostics.CodeAnalysis.SuppressMessage("", "GEN001")]
                class C
                {
                    //comment
                }
                """,
                new[] { (gen001, "") },
                Diagnostic("GEN001").WithLocation(1, 1));

            // different ID suppressed + multiple diagnostics
            verify("""
                [System.Diagnostics.CodeAnalysis.SuppressMessage("", "GEN002")]
                class C
                {
                    //comment
                    //another
                }
                """,
                new[] { (gen001, "com"), (gen001, "ano") },
                Diagnostic("GEN001", "com").WithLocation(4, 7),
                Diagnostic("GEN001", "ano").WithLocation(5, 7));

            // diagnostics are suppressed via SuppressMessageAttribute on a primary constructor
            verify("""
                [method: System.Diagnostics.CodeAnalysis.SuppressMessage("", "GEN001")]
                class C(int i)
                {
                    public int I { get; } = i;
                }
                """,
                new[] { (gen001, "int") },
                Diagnostic("GEN001", "int", isSuppressed: true).WithLocation(2, 9));

            static void verify(string source, IReadOnlyList<(Diagnostic Diagnostic, string Location)> reportDiagnostics, params DiagnosticDescription[] expected)
            {
                var parseOptions = TestOptions.RegularPreview;
                source = source.Replace(Environment.NewLine, "\r\n");
                var compilation = CreateCompilation(source, parseOptions: parseOptions);
                compilation.VerifyDiagnostics();
                var syntaxTree = compilation.SyntaxTrees.Single();
                var actualDiagnostics = reportDiagnostics.SelectAsArray(x =>
                    {
                        if (string.IsNullOrEmpty(x.Location))
                        {
                            return x.Diagnostic;
                        }
                        var start = source.IndexOf(x.Location);
                        Assert.True(start >= 0, $"Not found in source: '{x.Location}'");
                        var end = start + x.Location.Length;
                        return x.Diagnostic.WithLocation(Location.Create(syntaxTree, TextSpan.FromBounds(start, end)));
                    });

                var gen = new CallbackGenerator(c => { }, c =>
                {
                    foreach (var d in actualDiagnostics)
                    {
                        c.ReportDiagnostic(d);
                    }
                });

                var driver = CSharpGeneratorDriver.Create(new[] { gen }, parseOptions: parseOptions);
                driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
                outputCompilation.VerifyDiagnostics();
                diagnostics.Verify(expected);
            }
        }

        [Fact]
        public void GeneratorDriver_Prefers_Incremental_Generators()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            int initCount = 0, executeCount = 0;
            var generator = new CallbackGenerator((ic) => initCount++, (sgc) => executeCount++);

            int incrementalInitCount = 0;
            var generator2 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) => incrementalInitCount++));

            int dualInitCount = 0, dualExecuteCount = 0, dualIncrementalInitCount = 0;
            var generator3 = new IncrementalAndSourceCallbackGenerator((ic) => dualInitCount++, (sgc) => dualExecuteCount++, (ic) => dualIncrementalInitCount++);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator, generator2, generator3 }, parseOptions: parseOptions);
            driver.RunGenerators(compilation);

            // ran individual incremental and source generators
            Assert.Equal(1, initCount);
            Assert.Equal(1, executeCount);
            Assert.Equal(1, incrementalInitCount);

            // ran the combined generator only as an IIncrementalGenerator
            Assert.Equal(0, dualInitCount);
            Assert.Equal(0, dualExecuteCount);
            Assert.Equal(1, dualIncrementalInitCount);
        }

        [Fact]
        public void GeneratorDriver_Initializes_Incremental_Generators()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            int incrementalInitCount = 0;
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) => incrementalInitCount++));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver.RunGenerators(compilation);

            // ran the incremental generator
            Assert.Equal(1, incrementalInitCount);
        }

        [Fact]
        public void Incremental_Generators_Exception_During_Initialization()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var e = new InvalidOperationException("abc");
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) => throw e));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            var runResults = driver.GetRunResult();

            Assert.Single(runResults.Diagnostics);
            Assert.Single(runResults.Results);
            Assert.Empty(runResults.GeneratedTrees);
            Assert.Equal(e, runResults.Results[0].Exception);
        }

        [Fact]
        public void Incremental_Generators_Exception_During_Execution()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var e = new InvalidOperationException("abc");
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) => ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) => throw e)));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            var runResults = driver.GetRunResult();

            Assert.Single(runResults.Diagnostics);
            Assert.Single(runResults.Results);
            Assert.Empty(runResults.GeneratedTrees);
            Assert.Equal(e, runResults.Results[0].Exception);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67386")]
        public void Incremental_Generators_Exception_In_Comparer()
        {
            var source = """
                class Attr : System.Attribute { }
                [Attr] class C { }
                """;
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var syntaxTree = compilation.SyntaxTrees.Single();

            var e = new InvalidOperationException("abc");
            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                var name = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("Attr")
                    .Select((c, _) => c.Identifier.ValueText)
                    .WithComparer(new LambdaComparer<string>((_, _) => throw e));
                ctx.RegisterSourceOutput(name, (spc, n) => spc.AddSource(n, "// generated"));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            var runResults = driver.GetRunResult();

            Assert.Empty(runResults.Diagnostics);
            Assert.Equal("// generated", runResults.Results.Single().GeneratedSources.Single().SourceText.ToString());

            compilation = compilation.ReplaceSyntaxTree(syntaxTree, CSharpSyntaxTree.ParseText("""
                class Attr : System.Attribute { }
                [Attr] class D { }
                """, parseOptions));
            compilation.VerifyDiagnostics();

            driver = driver.RunGenerators(compilation);
            runResults = driver.GetRunResult();

            AssertEx.Equal(
                "warning CS8785: Generator 'PipelineCallbackGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type 'InvalidOperationException' with message 'abc'",
                runResults.Diagnostics.Single().ToString());
            Assert.Empty(runResults.GeneratedTrees);
            Assert.Equal(e, runResults.Results.Single().Exception);
        }

        [Fact]
        public void Incremental_Generators_Exception_During_Execution_Doesnt_Produce_AnySource()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var e = new InvalidOperationException("abc");
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) => spc.AddSource("test", ""));
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) => throw e);
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            var runResults = driver.GetRunResult();

            Assert.Single(runResults.Diagnostics);
            Assert.Single(runResults.Results);
            Assert.Empty(runResults.GeneratedTrees);
            Assert.Equal(e, runResults.Results[0].Exception);
        }

        [Fact]
        public void Incremental_Generators_Exception_During_Execution_Doesnt_Stop_Other_Generators()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var e = new InvalidOperationException("abc");
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) => throw e);
            }));

            var generator2 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) => spc.AddSource("test", ""));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator, generator2 }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            var runResults = driver.GetRunResult();

            Assert.Single(runResults.Diagnostics);
            Assert.Equal(2, runResults.Results.Length);
            Assert.Single(runResults.GeneratedTrees);
            Assert.Equal(e, runResults.Results[0].Exception);
        }

        [Fact]
        public void IncrementalGenerator_With_No_Pipeline_Callback_Is_Valid()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) => { }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            outputCompilation.VerifyDiagnostics();
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void IncrementalGenerator_Can_Add_PostInit_Source()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ic) => ic.RegisterPostInitializationOutput(c => c.AddSource("a", "class D {}"))));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void User_WrappedFunc_Throw_Exceptions()
        {
            Func<int, CancellationToken, int> func = (input, _) => input;
            Func<int, CancellationToken, int> throwsFunc = (input, _) => throw new InvalidOperationException("user code exception");
            Func<int, CancellationToken, int> timeoutFunc = (input, ct) => { ct.ThrowIfCancellationRequested(); return input; };
            Func<int, CancellationToken, int> otherTimeoutFunc = (input, _) => throw new OperationCanceledException();

            var userFunc = func.WrapUserFunction();
            var userThrowsFunc = throwsFunc.WrapUserFunction();
            var userTimeoutFunc = timeoutFunc.WrapUserFunction();
            var userOtherTimeoutFunc = otherTimeoutFunc.WrapUserFunction();

            // user functions return same values when wrapped
            var result = userFunc(10, CancellationToken.None);
            var userResult = userFunc(10, CancellationToken.None);
            Assert.Equal(10, result);
            Assert.Equal(result, userResult);

            // exceptions thrown in user code are wrapped
            Assert.Throws<InvalidOperationException>(() => throwsFunc(20, CancellationToken.None));
            Assert.Throws<UserFunctionException>(() => userThrowsFunc(20, CancellationToken.None));

            try
            {
                userThrowsFunc(20, CancellationToken.None);
            }
            catch (UserFunctionException e)
            {
                Assert.IsType<InvalidOperationException>(e.InnerException);
            }

            // cancellation is not wrapped, and is bubbled up
            Assert.Throws<OperationCanceledException>(() => timeoutFunc(30, new CancellationToken(true)));
            Assert.Throws<OperationCanceledException>(() => userTimeoutFunc(30, new CancellationToken(true)));

            // unless it wasn't *our* cancellation token, in which case it still gets wrapped
            Assert.Throws<OperationCanceledException>(() => otherTimeoutFunc(30, CancellationToken.None));
            Assert.Throws<UserFunctionException>(() => userOtherTimeoutFunc(30, CancellationToken.None));
        }

        [Fact]
        public void IncrementalGenerator_Doesnt_Run_For_Same_Input()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider.Select((c, ct) => c).WithTrackingName("IdentityTransform"), (spc, c) => { });
            }));

            // run the generator once, and check it was passed the compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["IdentityTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });

            // run the same compilation through again, and confirm the output wasn't called
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["IdentityTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
                });
        }

        [Fact]
        public void IncrementalGenerator_Runs_Only_For_Changed_Inputs()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var text1 = new InMemoryAdditionalText("Text1", "content1");
            var text2 = new InMemoryAdditionalText("Text2", "content2");

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider.Select((c, ct) => c).WithTrackingName("CompilationTransform"), (spc, c) => { });

                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider.Select((at, ct) => at).WithTrackingName("AdditionalTextsTransform"), (spc, at) => { });
            }));

            // run the generator once, and check it was passed the compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, additionalTexts: new[] { text1 }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["CompilationTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(compilation, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(compilation, output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });
            Assert.Collection(runResult.TrackedSteps["AdditionalTextsTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(text1, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(text1, output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });

            // add an additional text, but keep the compilation the same
            driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(text2));
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["CompilationTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(compilation, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(compilation, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });
            Assert.Collection(runResult.TrackedSteps["AdditionalTextsTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(text1, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(text1, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                },
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(text2, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(text2, output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });

            // now edit the compilation
            var newCompilation = compilation.WithOptions(compilation.Options.WithModuleName("newComp"));
            driver = driver.RunGenerators(newCompilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["CompilationTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(newCompilation, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(newCompilation, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                        });
                });
            Assert.Collection(runResult.TrackedSteps["AdditionalTextsTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(text1, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(text1, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                },
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(text2, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(text2, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });

            // re run without changing anything
            driver = driver.RunGenerators(newCompilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["CompilationTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(newCompilation, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(newCompilation, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });
            Assert.Collection(runResult.TrackedSteps["AdditionalTextsTransform"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(text1, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(text1, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                },
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(text2, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(text2, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });
        }

        [Fact]
        public void IncrementalGenerator_Can_Add_Comparer_To_Input_Node()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            List<Compilation> compilationsCalledFor = new List<Compilation>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var compilationSource = ctx.CompilationProvider.WithComparer(new LambdaComparer<Compilation>((c1, c2) => true, 0));
                ctx.RegisterSourceOutput(compilationSource, (spc, c) =>
                {
                    compilationsCalledFor.Add(c);
                });
            }));

            // run the generator once, and check it was passed the compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, compilationsCalledFor.Count);
            Assert.Equal(compilation, compilationsCalledFor[0]);

            // now edit the compilation, run the generator, and confirm that the output was not called again this time
            Compilation newCompilation = compilation.WithOptions(compilation.Options.WithModuleName("newCompilation"));
            driver = driver.RunGenerators(newCompilation);
            Assert.Equal(1, compilationsCalledFor.Count);
            Assert.Equal(compilation, compilationsCalledFor[0]);
        }

        [Fact]
        public void IncrementalGenerator_Can_Add_Comparer_To_Combine_Node()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            List<AdditionalText> texts = new List<AdditionalText>() { new InMemoryAdditionalText("abc", "") };

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var compilationSource = ctx.CompilationProvider.Combine(ctx.AdditionalTextsProvider.Collect())
                                                // comparer that ignores the LHS (additional texts)
                                                .WithComparer(new LambdaComparer<(Compilation, ImmutableArray<AdditionalText>)>((c1, c2) => c1.Item1 == c2.Item1, 0))
                                                .WithTrackingName("Step")
                                                .Select((x, ct) => x)
                                                .WithTrackingName("Step2");
                ctx.RegisterSourceOutput(compilationSource, (spc, c) =>
                {
                });
            }));

            // run the generator once, and check it was passed the compilation + additional texts
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: texts, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["Step"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(compilation, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        },
                        source =>
                        {
                            Assert.Equal(texts[0], ((ImmutableArray<AdditionalText>)source.Source.Outputs[source.OutputIndex].Value)[0]);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            var value = ((Compilation, ImmutableArray<AdditionalText>))output.Value;
                            Assert.Equal(compilation, value.Item1);
                            Assert.Equal(texts[0], value.Item2.Single());
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });

            // edit the additional texts, and verify that the step output is considered "unchanged" and that the value is the same as the previous value.
            driver = driver.RemoveAdditionalTexts(texts.ToImmutableArray());
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["Step"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(compilation, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        },
                        source =>
                        {
                            Assert.Empty((ImmutableArray<AdditionalText>)source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            var value = ((Compilation, ImmutableArray<AdditionalText>))output.Value;
                            Assert.Equal(compilation, value.Item1);
                            Assert.Equal(texts[0], value.Item2.Single());
                            Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason);
                        });
                });

            // Verify that a step that consumes the result of the Combine step gets the old value as an input
            // and considers the value cached.
            Assert.Collection(runResult.TrackedSteps["Step2"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            var value = ((Compilation, ImmutableArray<AdditionalText>))source.Source.Outputs[source.OutputIndex].Value;
                            Assert.Equal(compilation, value.Item1);
                            Assert.Equal(texts[0], value.Item2.Single());
                            Assert.Equal(IncrementalStepRunReason.Unchanged, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            var value = ((Compilation, ImmutableArray<AdditionalText>))output.Value;
                            Assert.Equal(compilation, value.Item1);
                            Assert.Equal(texts[0], value.Item2.Single());
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });

            // now edit the compilation, run the generator, and confirm that the output *was* called again this time with the new compilation and no additional texts
            Compilation newCompilation = compilation.WithOptions(compilation.Options.WithModuleName("newCompilation"));
            driver = driver.RunGenerators(newCompilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["Step"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(newCompilation, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason);
                        },
                        source =>
                        {
                            Assert.Empty((ImmutableArray<AdditionalText>)source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Unchanged, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            var value = ((Compilation, ImmutableArray<AdditionalText>))output.Value;
                            Assert.Equal(newCompilation, value.Item1);
                            Assert.Empty(value.Item2);
                            Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                        });
                });
        }

        [Fact, WorkItem(61162, "https://github.com/dotnet/roslyn/issues/61162")]
        public void IncrementalGenerator_Collect_SyntaxProvider_01()
        {
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(static ctx =>
            {
                var invokedMethodsProvider = ctx.SyntaxProvider
                    .CreateSyntaxProvider(
                        static (node, _) => node is InvocationExpressionSyntax,
                        static (ctx, ct) => ctx.SemanticModel.GetSymbolInfo(ctx.Node, ct).Symbol?.Name ?? "(method not found)")
                    .Collect();

                ctx.RegisterSourceOutput(invokedMethodsProvider, static (spc, invokedMethods) =>
                {
                    spc.AddSource("InvokedMethods.g.cs", string.Join(Environment.NewLine,
                        invokedMethods.Select(m => $"// {m}")));
                });
            }));

            var source = """
                System.Console.WriteLine();
                System.Console.WriteLine();
                System.Console.WriteLine();
                System.Console.WriteLine();
                """;
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugExeThrowing, parseOptions: parseOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            verify(ref driver, compilation, """
                // WriteLine
                // WriteLine
                // WriteLine
                // WriteLine
                """);

            replace(ref compilation, parseOptions, """
                System.Console.WriteLine();
                System.Console.WriteLine();
                """);
            verify(ref driver, compilation, """
                // WriteLine
                // WriteLine
                """);

            replace(ref compilation, parseOptions, "_ = 0;");
            verify(ref driver, compilation, "");

            static void verify(ref GeneratorDriver driver, Compilation compilation, string generatedContent)
            {
                driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
                outputCompilation.VerifyDiagnostics();
                generatorDiagnostics.Verify();
                var generatedTree = driver.GetRunResult().GeneratedTrees.Single();
                AssertEx.EqualOrDiff(generatedContent, generatedTree.ToString());
            }

            static void replace(ref Compilation compilation, CSharpParseOptions parseOptions, string source)
            {
                compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(source, parseOptions));
            }
        }

        [Fact, WorkItem(61162, "https://github.com/dotnet/roslyn/issues/61162")]
        public void IncrementalGenerator_Collect_SyntaxProvider_02()
        {
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(static ctx =>
            {
                var invokedMethodsProvider = ctx.SyntaxProvider
                    .CreateSyntaxProvider(
                        static (node, _) => node is InvocationExpressionSyntax,
                        static (ctx, ct) => ctx.SemanticModel.GetSymbolInfo(ctx.Node, ct).Symbol?.Name ?? "(method not found)")
                    .Select((n, _) => n);

                ctx.RegisterSourceOutput(invokedMethodsProvider, static (spc, invokedMethod) =>
                {
                    spc.AddSource(invokedMethod, "// " + invokedMethod);
                });
            }));

            var source = """
                System.Console.WriteLine();
                System.Console.ReadLine();
                """;
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugExeThrowing, parseOptions: parseOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            verify(ref driver, compilation, new[]
            {
                "// WriteLine",
                "// ReadLine"
            });

            replace(ref compilation, parseOptions, """
                System.Console.WriteLine();
                """);

            verify(ref driver, compilation, new[]
            {
                "// WriteLine"
            });

            replace(ref compilation, parseOptions, "_ = 0;");
            verify(ref driver, compilation, Array.Empty<string>());

            static void verify(ref GeneratorDriver driver, Compilation compilation, string[] generatedContent)
            {
                driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
                outputCompilation.VerifyDiagnostics();
                generatorDiagnostics.Verify();
                var trees = driver.GetRunResult().GeneratedTrees;
                Assert.Equal(generatedContent.Length, trees.Length);
                for (int i = 0; i < generatedContent.Length; i++)
                {
                    AssertEx.EqualOrDiff(generatedContent[i], trees[i].ToString());
                }
            }

            static void replace(ref Compilation compilation, CSharpParseOptions parseOptions, string source)
            {
                compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(source, parseOptions));
            }
        }

        [Fact]
        public void IncrementalGenerator_Register_End_Node_Only_Once_Through_Combines()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            List<Compilation> compilationsCalledFor = new List<Compilation>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var source = ctx.CompilationProvider;
                var source2 = ctx.CompilationProvider.Combine(source);
                var source3 = ctx.CompilationProvider.Combine(source2);
                var source4 = ctx.CompilationProvider.Combine(source3);
                var source5 = ctx.CompilationProvider.Combine(source4);

                ctx.RegisterSourceOutput(source5, (spc, c) =>
                {
                    compilationsCalledFor.Add(c.Item1);
                });
            }));

            // run the generator and check that we didn't multiple register the generate source node through the combine
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, compilationsCalledFor.Count);
            Assert.Equal(compilation, compilationsCalledFor[0]);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67633")]
        public void IncrementalGenerator_SyntaxProvider_InputRemoved()
        {
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(static ctx =>
            {
                var invokedMethodsProvider = ctx.SyntaxProvider
                    .CreateSyntaxProvider(
                        static (node, _) => node is InvocationExpressionSyntax,
                        static (ctx, ct) => ctx.SemanticModel.GetSymbolInfo(ctx.Node, ct).Symbol?.Name ?? "(method not found)")
                    .Select((n, _) => n)
                    .WithTrackingName("Select");

                ctx.RegisterSourceOutput(invokedMethodsProvider, static (spc, invokedMethod) =>
                {
                    spc.AddSource(invokedMethod, "// " + invokedMethod);
                });
            }));

            var source1 = """
                System.Console.WriteLine();
                System.Console.ReadLine();
                """;

            var source2 = """
                class C {
                    public void M()
                    {
                        System.Console.Clear();
                        System.Console.Beep();
                    }
                }
                """;

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugExeThrowing, parseOptions: parseOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            verify(ref driver, compilation, new[]
            {
                "// WriteLine",
                "// ReadLine",
                "// Clear",
                "// Beep"
            });

            // edit part of source 1
            replace(ref compilation, parseOptions, """
                System.Console.WriteLine();
                System.Console.Write(' ');
                """);

            verify(ref driver, compilation, new[]
            {
                "// WriteLine",
                "// Write",
                "// Clear",
                "// Beep"
            });

            Assert.Equal(new (object, IncrementalStepRunReason)[]
            {
                ("WriteLine", IncrementalStepRunReason.Cached),
                ("Write", IncrementalStepRunReason.Modified),
                ("Clear", IncrementalStepRunReason.Cached),
                ("Beep", IncrementalStepRunReason.Cached)
            },
            driver.GetRunResult().Results.Single().TrackedSteps["Select"].Select(r => r.Outputs.Single()));

            // remove second line of source 1
            replace(ref compilation, parseOptions, """
                System.Console.WriteLine();
                """);

            verify(ref driver, compilation, new[]
            {
                "// WriteLine",
                "// Clear",
                "// Beep"
            });

            Assert.Equal(new (object, IncrementalStepRunReason)[]
            {
                ("WriteLine", IncrementalStepRunReason.Cached),
                ("Write", IncrementalStepRunReason.Removed),
                ("Clear", IncrementalStepRunReason.Cached),
                ("Beep", IncrementalStepRunReason.Cached)
            },
            driver.GetRunResult().Results.Single().TrackedSteps["Select"].Select(r => r.Outputs.Single()));

            static void verify(ref GeneratorDriver driver, Compilation compilation, string[] generatedContent)
            {
                driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
                outputCompilation.VerifyDiagnostics();
                generatorDiagnostics.Verify();
                var trees = driver.GetRunResult().GeneratedTrees;
                Assert.Equal(generatedContent.Length, trees.Length);
                for (int i = 0; i < generatedContent.Length; i++)
                {
                    AssertEx.EqualOrDiff(generatedContent[i], trees[i].ToString());
                }
            }

            static void replace(ref Compilation compilation, CSharpParseOptions parseOptions, string source)
            {
                compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(source, parseOptions));
            }
        }

        [Fact]
        public void IncrementalGenerator_PostInit_Source_Is_Cached()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterPostInitializationOutput(c => c.AddSource("a", "class D {}"));

                var input = ctx.SyntaxProvider.CreateSyntaxProvider(static (n, _) => n is ClassDeclarationSyntax, (gsc, _) => (ClassDeclarationSyntax)gsc.Node)
                .Select((c, ct) => c).WithTrackingName("Classes");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["Classes"],
                step =>
                {
                    Assert.Equal("C", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
                    Assert.Equal(IncrementalStepRunReason.New, step.Outputs[0].Reason);
                },
                step =>
                {
                    Assert.Equal("D", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
                    Assert.Equal(IncrementalStepRunReason.New, step.Outputs[0].Reason);
                });

            // re-run without changes
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["Classes"],
                step =>
                {
                    Assert.Equal("C", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
                    Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
                },
                step =>
                {
                    Assert.Equal("D", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
                    Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
                });

            // modify the original tree, see that the post init is still cached
            var c2 = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), CSharpSyntaxTree.ParseText("class E{}", parseOptions));
            driver = driver.RunGenerators(c2);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["Classes"],
                step =>
                {
                    Assert.Equal("E", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
                    Assert.Equal(IncrementalStepRunReason.Modified, step.Outputs[0].Reason);
                },
                step =>
                {
                    Assert.Equal("D", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
                    Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
                });
        }

        [Fact]
        public void Incremental_Generators_Can_Be_Cancelled()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            CancellationTokenSource cts = new CancellationTokenSource();
            bool generatorCancelled = false;

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
            {
                var step1 = ctx.CompilationProvider.Select((c, ct) => { generatorCancelled = true; cts.Cancel(); return c; });
                var step2 = step1.Select((c, ct) => { ct.ThrowIfCancellationRequested(); return c; });

                ctx.RegisterSourceOutput(step2, (spc, c) => spc.AddSource("a", ""));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            Assert.Throws<OperationCanceledException>(() => driver = driver.RunGenerators(compilation, cancellationToken: cts.Token));
            Assert.True(generatorCancelled);
        }

        [Fact]
        public void ParseOptions_Can_Be_Updated()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, p) => { });
            }));

            // run the generator once, and check it was passed the parse options
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(disabledOutputs: IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            Assert.Single(runResult.TrackedSteps["ParseOptions"]);
            var output = runResult.TrackedSteps["ParseOptions"][0].Outputs[0].Value;
            Assert.Equal(parseOptions, output);

            // re-run without changes
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["ParseOptions"],
                step =>
                {
                    Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
                });

            // now update the parse options
            var newParseOptions = parseOptions.WithDocumentationMode(DocumentationMode.Diagnose);
            driver = driver.WithUpdatedParseOptions(newParseOptions);

            // check we ran
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Single(runResult.TrackedSteps["ParseOptions"]);
            output = runResult.TrackedSteps["ParseOptions"][0].Outputs[0].Value;
            Assert.Equal(newParseOptions, output);

            // re-run without changes
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["ParseOptions"],
                step =>
                {
                    Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
                });

            // replace it with null, and check that it throws
            Assert.Throws<ArgumentNullException>(() => driver.WithUpdatedParseOptions(null!));
        }

        [Fact, WorkItem(57455, "https://github.com/dotnet/roslyn/issues/57455")]
        public void RemoveTriggeringSyntaxAndVerifySyntaxTreeConsistentWithCompilation()
        {
            var source = @"
[System.Obsolete]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = ctx.SyntaxProvider
                    .CreateSyntaxProvider(static (s, t) => isSyntaxTargetForGeneration(s), static (context, ct) => getSemanticTargetForGeneration(context, ct))
                    .Where(static c => c is not null)!;

                IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
                    ctx.CompilationProvider.Combine(classDeclarations.Collect());

                ctx.RegisterSourceOutput(compilationAndClasses, (context, ct) => validate(ct.Item1, ct.Item2));
            }));

            // run the generator once
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(disabledOutputs: IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            Assert.True(driver.GetRunResult().Diagnostics.IsEmpty);

            // now update the source 
            var newSource = @"
class C { }
";
            Compilation newCompilation = CreateCompilation(newSource, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

            // check we ran
            driver = driver.RunGenerators(newCompilation);
            Assert.True(driver.GetRunResult().Diagnostics.IsEmpty);
            return;

            static void validate(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> nodes)
            {
                foreach (var node in nodes)
                {
                    Assert.True(compilation.SyntaxTrees.Contains(node.SyntaxTree));
                }
            }

            static bool isSyntaxTargetForGeneration(SyntaxNode node)
                => node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } };

            static ClassDeclarationSyntax? getSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
            {
                var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
                foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
                {
                    return classDeclarationSyntax;
                }
                return null;
            }
        }

        [Fact]
        public void AnalyzerConfig_Can_Be_Updated()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.AnalyzerConfigOptionsProvider.Select((p, ct) =>
                {
                    p.GlobalOptions.TryGetValue("test", out var analyzerOptionsValue);
                    return analyzerOptionsValue;
                }).WithTrackingName("AnalyzerConfig"),
                (spc, p) => { });
            }));

            var builder = ImmutableDictionary<string, string>.Empty.ToBuilder();
            builder.Add("test", "value1");
            var optionsProvider = new CompilerAnalyzerConfigOptionsProvider(ImmutableDictionary<object, AnalyzerConfigOptions>.Empty, new DictionaryAnalyzerConfigOptions(builder.ToImmutable()));

            // run the generator once, and check it was passed the configs
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, optionsProvider: optionsProvider, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["AnalyzerConfig"],
                step =>
                {
                    Assert.Equal("AnalyzerConfig", step.Name);
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("value1", output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });

            // re-run without changes.
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["AnalyzerConfig"],
                step =>
                {
                    Assert.Equal("AnalyzerConfig", step.Name);
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("value1", output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });

            // now update the config
            builder.Clear();
            builder.Add("test", "value2");
            var newOptionsProvider = optionsProvider.WithGlobalOptions(new DictionaryAnalyzerConfigOptions(builder.ToImmutable()));
            driver = driver.WithUpdatedAnalyzerConfigOptions(newOptionsProvider);

            // check we ran
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["AnalyzerConfig"],
                step =>
                {
                    Assert.Equal("AnalyzerConfig", step.Name);
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("value2", output.Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                        });
                });

            // replace it with null, and check that it throws
            Assert.Throws<ArgumentNullException>(() => driver.WithUpdatedAnalyzerConfigOptions(null!));
        }

        [Fact]
        public void AdditionalText_Can_Be_Replaced()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            InMemoryAdditionalText additionalText1 = new InMemoryAdditionalText("path1.txt", "");
            InMemoryAdditionalText additionalText2 = new InMemoryAdditionalText("path2.txt", "");
            InMemoryAdditionalText additionalText3 = new InMemoryAdditionalText("path3.txt", "");

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider.Select((t, _) => t.Path).WithTrackingName("Paths"), (spc, p) => { });
            }));

            // run the generator once and check we saw the additional file
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText1, additionalText2, additionalText3 }, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["Paths"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path1.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.New, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("path1.txt", output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                },
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path2.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.New, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("path2.txt", output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                },
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path3.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.New, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("path3.txt", output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });

            // re-run and check nothing else got added
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["Paths"],
               step =>
               {
                   Assert.Collection(step.Inputs,
                       input =>
                       {
                           var consumedInput = input.Source.Outputs[input.OutputIndex];
                           Assert.Equal("path1.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                           Assert.Equal(IncrementalStepRunReason.Cached, consumedInput.Reason);
                       });
                   Assert.Collection(step.Outputs,
                       output =>
                       {
                           Assert.Equal("path1.txt", output.Value);
                           Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                       });
               },
               step =>
               {
                   Assert.Collection(step.Inputs,
                       input =>
                       {
                           var consumedInput = input.Source.Outputs[input.OutputIndex];
                           Assert.Equal("path2.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                           Assert.Equal(IncrementalStepRunReason.Cached, consumedInput.Reason);
                       });
                   Assert.Collection(step.Outputs,
                       output =>
                       {
                           Assert.Equal("path2.txt", output.Value);
                           Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                       });
               },
               step =>
               {
                   Assert.Collection(step.Inputs,
                       input =>
                       {
                           var consumedInput = input.Source.Outputs[input.OutputIndex];
                           Assert.Equal("path3.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                           Assert.Equal(IncrementalStepRunReason.Cached, consumedInput.Reason);
                       });
                   Assert.Collection(step.Outputs,
                       output =>
                       {
                           Assert.Equal("path3.txt", output.Value);
                           Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                       });
               });

            // now, update the additional text with a new path
            driver = driver.ReplaceAdditionalText(additionalText2, new InMemoryAdditionalText("path4.txt", ""));

            // run, and check that only the replaced file was invoked
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["Paths"],
               step =>
               {
                   Assert.Collection(step.Inputs,
                       input =>
                       {
                           var consumedInput = input.Source.Outputs[input.OutputIndex];
                           Assert.Equal("path1.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                           Assert.Equal(IncrementalStepRunReason.Cached, consumedInput.Reason);
                       });
                   Assert.Collection(step.Outputs,
                       output =>
                       {
                           Assert.Equal("path1.txt", output.Value);
                           Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                       });
               },
               step =>
               {
                   Assert.Collection(step.Inputs,
                       input =>
                       {
                           var consumedInput = input.Source.Outputs[input.OutputIndex];
                           Assert.Equal("path4.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                           Assert.Equal(IncrementalStepRunReason.Modified, consumedInput.Reason);
                       });
                   Assert.Collection(step.Outputs,
                       output =>
                       {
                           Assert.Equal("path4.txt", output.Value);
                           Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                       });
               },
               step =>
               {
                   Assert.Collection(step.Inputs,
                       input =>
                       {
                           var consumedInput = input.Source.Outputs[input.OutputIndex];
                           Assert.Equal("path3.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                           Assert.Equal(IncrementalStepRunReason.Cached, consumedInput.Reason);
                       });
                   Assert.Collection(step.Outputs,
                       output =>
                       {
                           Assert.Equal("path3.txt", output.Value);
                           Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                       });
               });

            // replace it with null, and check that it throws
            Assert.Throws<ArgumentNullException>(() => driver.ReplaceAdditionalText(additionalText1, null!));
        }

        [Fact]
        public void Replaced_Input_Is_Treated_As_Modified()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            InMemoryAdditionalText additionalText = new InMemoryAdditionalText("path.txt", "abc");

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var texts = ctx.AdditionalTextsProvider;
                var paths = texts.Select((t, _) => t?.Path).WithTrackingName("Path");
                var contents = texts.Select((t, ct) => t?.GetText(ct)?.ToString()).WithTrackingName("Content");

                ctx.RegisterSourceOutput(paths, (spc, p) => { });
                ctx.RegisterSourceOutput(contents, (spc, p) => { });
            }));

            // run the generator once and check we saw the additional file
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText }, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["Path"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.New, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("path.txt", output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });
            Assert.Collection(runResult.TrackedSteps["Content"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.New, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("abc", output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });

            // re-run and check nothing else got added
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["Path"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.Cached, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("path.txt", output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });
            Assert.Collection(runResult.TrackedSteps["Content"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.Cached, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("abc", output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });

            // now, update the additional text, but keep the path the same
            var secondText = new InMemoryAdditionalText("path.txt", "def");
            driver = driver.ReplaceAdditionalText(additionalText, secondText);

            // run, and check that only the contents are marked as modified
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["Path"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.Modified, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("path.txt", output.Value);
                            Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason);
                        });
                });
            Assert.Collection(runResult.TrackedSteps["Content"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.Modified, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("def", output.Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                        });
                });

            // now replace the text with a different path, but the same text
            var thirdText = new InMemoryAdditionalText("path2.txt", "def");
            driver = driver.ReplaceAdditionalText(secondText, thirdText);

            // run, and check that only the paths got re-run
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps["Path"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path2.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.Modified, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("path2.txt", output.Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                        });
                });
            Assert.Collection(runResult.TrackedSteps["Content"],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        input =>
                        {
                            var consumedInput = input.Source.Outputs[input.OutputIndex];
                            Assert.Equal("path2.txt", Assert.IsType<InMemoryAdditionalText>(consumedInput.Value).Path);
                            Assert.Equal(IncrementalStepRunReason.Modified, consumedInput.Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal("def", output.Value);
                            Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason);
                        });
                });
        }

        [Theory]
        [CombinatorialData]
        [InlineData(IncrementalGeneratorOutputKind.Source | IncrementalGeneratorOutputKind.Implementation)]
        [InlineData(IncrementalGeneratorOutputKind.Source | IncrementalGeneratorOutputKind.PostInit)]
        [InlineData(IncrementalGeneratorOutputKind.Implementation | IncrementalGeneratorOutputKind.PostInit)]
        [InlineData(IncrementalGeneratorOutputKind.Source | IncrementalGeneratorOutputKind.Implementation | IncrementalGeneratorOutputKind.PostInit)]
        public void Generator_Output_Kinds_Can_Be_Disabled(IncrementalGeneratorOutputKind disabledOutput)
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterPostInitializationOutput((context) => context.AddSource("PostInit", ""));
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, ct) => context.AddSource("Source", ""));
                ctx.RegisterImplementationSourceOutput(ctx.CompilationProvider, (context, ct) => context.AddSource("Implementation", ""));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, driverOptions: new GeneratorDriverOptions(disabledOutput), parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            var result = driver.GetRunResult();

            Assert.Single(result.Results);
            Assert.Empty(result.Results[0].Diagnostics);

            // verify the expected outputs were generated
            // NOTE: adding new output types will cause this test to fail. Update above as needed.
            foreach (IncrementalGeneratorOutputKind kind in Enum.GetValues(typeof(IncrementalGeneratorOutputKind)))
            {
                if (kind == IncrementalGeneratorOutputKind.None)
                    continue;

                if (disabledOutput.HasFlag((IncrementalGeneratorOutputKind)kind))
                {
                    Assert.DoesNotContain(result.Results[0].GeneratedSources, isTextForKind);
                }
                else
                {
                    Assert.Contains(result.Results[0].GeneratedSources, isTextForKind);
                }

                bool isTextForKind(GeneratedSourceResult s) => s.HintName == Enum.GetName(typeof(IncrementalGeneratorOutputKind), kind) + ".cs";
            }
        }

        [Fact]
        public void IncrementalGeneratorInputSourcesHaveNames()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider((node, ct) => node is ClassDeclarationSyntax c, (context, ct) => context.Node).WithTrackingName("Syntax"), (context, ct) => { });
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, ct) => { });
                ctx.RegisterSourceOutput(ctx.AnalyzerConfigOptionsProvider, (context, ct) => { });
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (context, ct) => { });
                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider, (context, ct) => { });
                ctx.RegisterImplementationSourceOutput(ctx.MetadataReferencesProvider, (context, ct) => { });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: parseOptions, additionalTexts: new[] { new InMemoryAdditionalText("text.txt", "") }, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];

            // Assert that the well-named providers recorded steps with well known names.
            Assert.Contains(WellKnownGeneratorInputs.Compilation, runResult.TrackedSteps.Keys);
            Assert.Contains(WellKnownGeneratorInputs.AnalyzerConfigOptions, runResult.TrackedSteps.Keys);
            Assert.Contains(WellKnownGeneratorInputs.ParseOptions, runResult.TrackedSteps.Keys);
            Assert.Contains(WellKnownGeneratorInputs.AdditionalTexts, runResult.TrackedSteps.Keys);
            Assert.Contains(WellKnownGeneratorInputs.MetadataReferences, runResult.TrackedSteps.Keys);

            // Assert that a syntax provider records itself.
            Assert.Contains("Syntax", runResult.TrackedSteps.Keys);

            // Source output steps have the well-defined SourceOutputStep name
            Assert.Contains(WellKnownGeneratorOutputs.SourceOutput, runResult.TrackedSteps.Keys);
            Assert.Contains(WellKnownGeneratorOutputs.ImplementationSourceOutput, runResult.TrackedSteps.Keys);
            // Source output steps should also be in the TrackedOutputSteps collection
            Assert.Contains(WellKnownGeneratorOutputs.SourceOutput, runResult.TrackedOutputSteps.Keys);
            Assert.Contains(WellKnownGeneratorOutputs.ImplementationSourceOutput, runResult.TrackedOutputSteps.Keys);

            Assert.Equal(8, runResult.TrackedSteps.Count);
            Assert.Equal(2, runResult.TrackedOutputSteps.Count);
        }

        [Fact]
        public void Steps_From_Common_Input_Nodes_Recorded_In_All_Generators_Steps()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            InMemoryAdditionalText additionalText = new InMemoryAdditionalText("path.txt", "abc");

            var generator1 = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider, (context, ct) => { });
            });
            var generator2 = new PipelineCallbackGenerator2(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider, (context, ct) => { });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator1.AsSourceGenerator(), generator2.AsSourceGenerator() }, parseOptions: parseOptions, additionalTexts: new[] { additionalText }, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            GeneratorDriverRunResult runResult = driver.GetRunResult();
            Assert.All(runResult.Results,
                result => Assert.Contains(WellKnownGeneratorInputs.AdditionalTexts, result.TrackedSteps.Keys));
            Assert.Equal(2, runResult.Results.Length);
        }

        [Fact]
        public void Metadata_References_Provider()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            var metadataRefs = new[] {
                MetadataReference.CreateFromAssemblyInternal(this.GetType().Assembly),
                MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly)
            };
            Compilation compilation = CreateEmptyCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions, references: metadataRefs);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            List<string?> referenceList = new List<string?>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.MetadataReferencesProvider, (spc, r) => { referenceList.Add(r.Display); });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            Assert.Equal(referenceList[0], metadataRefs[0].Display);
            Assert.Equal(referenceList[1], metadataRefs[1].Display);

            // re-run and check we didn't see anything new
            referenceList.Clear();

            driver = driver.RunGenerators(compilation);
            Assert.Empty(referenceList);

            // Modify the reference
            var modifiedRef = metadataRefs[0].WithAliases(new[] { "Alias " });
            metadataRefs[0] = modifiedRef;
            compilation = compilation.WithReferences(metadataRefs);

            driver = driver.RunGenerators(compilation);
            Assert.Single(referenceList, modifiedRef.Display);
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        [WorkItem(59190, "https://github.com/dotnet/roslyn/issues/59190")]
        public void LongBinaryExpression()
        {
            var source = @"
class C {
public static readonly string F = ""a""
";

            for (int i = 0; i < 7000; i++)
            {
                source += @" + ""a""
";
            }

            source += @";
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider((node, ct) => node is ClassDeclarationSyntax c, (context, ct) => context.Node).WithTrackingName("Syntax"), (context, ct) => { });
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, ct) => { });
                ctx.RegisterSourceOutput(ctx.AnalyzerConfigOptionsProvider, (context, ct) => { });
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (context, ct) => { });
                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider, (context, ct) => { });
                ctx.RegisterImplementationSourceOutput(ctx.MetadataReferencesProvider, (context, ct) => { });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: parseOptions, additionalTexts: new[] { new InMemoryAdditionalText("text.txt", "") }, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            driver.GetRunResult();
        }

        [Fact]
        [WorkItem(59209, "https://github.com/dotnet/roslyn/issues/59209")]
        public void Binary_Additional_Files_Do_Not_Throw_When_Compared()
        {
            var source = "class C{}";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider, (context, text) =>
                {
                    context.AddSource(Path.GetFileName(text.Path), "");
                });
            });

            var additionalText1 = new InMemoryAdditionalText.BinaryText("file1");
            var additionalText2 = new InMemoryAdditionalText.BinaryText("file2");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() },
                parseOptions: parseOptions,
                additionalTexts: new[] { additionalText1, additionalText2 },
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);
            var result = driver.GetRunResult();

            Assert.Equal(2, result.GeneratedTrees.Length);
            driver = driver.RunGenerators(compilation);
        }

        [Fact]
        [WorkItem(58625, "https://github.com/dotnet/roslyn/issues/58625")]
        public void Incremental_Generators_Can_Recover_From_Exceptions()
        {
            var source = "class C{}";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            bool shouldThrow = true;

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, text) =>
                {
                    if (shouldThrow)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        context.AddSource("generated", "");
                    }

                });
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() },
                parseOptions: parseOptions,
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);
            var result = driver.GetRunResult();

            var diag = Assert.Single(result.Diagnostics);

            // update the compilation
            compilation = compilation.WithOptions(compilation.Options.WithModuleName("newName"));
            shouldThrow = false;

            driver = driver.RunGenerators(compilation);
            result = driver.GetRunResult();

            Assert.Single(result.GeneratedTrees);
        }

        [Fact]
        public void Timing_Info_Is_Empty_If_Not_Run()
        {
            var source = "class C{}";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, text) =>
                {
                    context.AddSource("generated", "");
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

            var timing = driver.GetTimingInfo();

            Assert.Equal(TimeSpan.Zero, timing.ElapsedTime);

            var generatorTiming = Assert.Single(timing.GeneratorTimes);
            Assert.Equal(generator, generatorTiming.Generator);
            Assert.Equal(TimeSpan.Zero, generatorTiming.ElapsedTime);
        }

        [Fact]
        public void Can_Get_Timing_Info()
        {
            var source = "class C{}";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, text) =>
                {
                    context.AddSource("generated", "");
                    Thread.Sleep(1);
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);
            var timing = driver.GetTimingInfo();

            Assert.NotEqual(TimeSpan.Zero, timing.ElapsedTime);

            var generatorTiming = Assert.Single(timing.GeneratorTimes);
            Assert.Equal(generator, generatorTiming.Generator);
            Assert.NotEqual(TimeSpan.Zero, generatorTiming.ElapsedTime);
            Assert.True(timing.ElapsedTime >= generatorTiming.ElapsedTime);
        }

        [Fact]
        public void Can_Get_Timing_Info_From_Multiple_Generators()
        {
            var source = "class C{}";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, text) =>
                {
                    context.AddSource("generated", "");
                    Thread.Sleep(1);
                });
            }).AsSourceGenerator();

            var generator2 = new PipelineCallbackGenerator2(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, text) =>
                {
                    context.AddSource("generated", "");
                    Thread.Sleep(1);
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator, generator2 }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

            driver = driver.RunGenerators(compilation);
            var timing = driver.GetTimingInfo();

            Assert.NotEqual(TimeSpan.Zero, timing.ElapsedTime);
            Assert.Equal(2, timing.GeneratorTimes.Length);

            var timing1 = timing.GeneratorTimes[0];
            Assert.Equal(generator, timing1.Generator);
            Assert.NotEqual(TimeSpan.Zero, timing1.ElapsedTime);
            Assert.True(timing.ElapsedTime >= timing1.ElapsedTime);

            var timing2 = timing.GeneratorTimes[1];
            Assert.Equal(generator2, timing2.Generator);
            Assert.NotEqual(TimeSpan.Zero, timing2.ElapsedTime);
            Assert.True(timing.ElapsedTime >= timing2.ElapsedTime);

            Assert.True(timing.ElapsedTime >= timing1.ElapsedTime + timing2.ElapsedTime);
        }

        [Fact]
        public void Timing_Info_Only_Includes_Last_Run()
        {
            var source = "class C{}";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (context, text) =>
                {
                    Thread.Sleep(50);
                    context.AddSource("generated", "");
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

            // run once
            driver = driver.RunGenerators(compilation);
            var timing = driver.GetTimingInfo();

            Assert.NotEqual(TimeSpan.Zero, timing.ElapsedTime);

            var generatorTiming = Assert.Single(timing.GeneratorTimes);
            Assert.Equal(generator, generatorTiming.Generator);
            Assert.NotEqual(TimeSpan.Zero, generatorTiming.ElapsedTime);
            Assert.True(timing.ElapsedTime >= generatorTiming.ElapsedTime);

            // run a second time. No steps should be performed, so overall time should be less 
            driver = driver.RunGenerators(compilation);
            var timing2 = driver.GetTimingInfo();

            Assert.NotEqual(TimeSpan.Zero, timing2.ElapsedTime);
            Assert.True(timing.ElapsedTime > timing2.ElapsedTime);

            var generatorTiming2 = Assert.Single(timing2.GeneratorTimes);
            Assert.Equal(generator, generatorTiming2.Generator);
            Assert.NotEqual(TimeSpan.Zero, generatorTiming2.ElapsedTime);
            Assert.True(generatorTiming.ElapsedTime > generatorTiming2.ElapsedTime);
        }

        [Fact]
        public void Returning_Null_From_SelectMany_Gives_Empty_Array()
        {
            var source = "class C{}";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                var nullArray = ctx.CompilationProvider.Select((c, _) => null as object[]);
                var flatArray = nullArray.SelectMany((a, _) => a!);
                ctx.RegisterSourceOutput(flatArray, (_, _) => { });

            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult();

            Assert.Empty(runResult.GeneratedTrees);
            Assert.Empty(runResult.Diagnostics);
            var result = Assert.Single(runResult.Results);
            Assert.Empty(result.GeneratedSources);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void Post_Init_Trees_Are_Reparsed_When_ParseOptions_Change()
        {
            var source = "class C{}";
            var postInitSource = @"
#pragma warning disable CS0169
class D {  (int, bool) _field; }";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterPostInitializationOutput(c => c.AddSource("D", postInitSource));
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            compilation.VerifyDiagnostics();
            Assert.Empty(diagnostics);

            // change the parse options so that the tree is no longer accepted
            parseOptions = parseOptions.WithLanguageVersion(LanguageVersion.CSharp2);
            compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            driver = driver.WithUpdatedParseOptions(parseOptions);

            // change some other options to ensure the parseOption change tracking flows correctly
            driver = driver.AddAdditionalTexts(ImmutableArray<AdditionalText>.Empty);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out diagnostics);
            diagnostics.Verify();
            compilation.VerifyDiagnostics(
                    // Microsoft.CodeAnalysis.Test.Utilities\Roslyn.Test.Utilities.TestGenerators.PipelineCallbackGenerator\D.cs(3,12): error CS8022: Feature 'tuples' is not available in C# 2. Please use language version 7.0 or greater.
                    // class D {  (int, bool) _field; }
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "(int, bool)").WithArguments("tuples", "7.0").WithLocation(3, 12)
                );

            // change them back to something where it is supported
            parseOptions = parseOptions.WithLanguageVersion(LanguageVersion.CSharp8);
            compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            driver = driver.WithUpdatedParseOptions(parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out diagnostics);
            diagnostics.Verify();
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_DetachedSyntaxTree_Incremental()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (ctx, _) =>
                {
                    var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "/detached");
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        warningLevel: 1,
                        location: Location.Create(syntaxTree, TextSpan.FromBounds(0, 2))));
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(PipelineCallbackGenerator), "Reported diagnostic 'TEST0001' has a source location in file '/detached', which is not part of the compilation being analyzed.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_DetachedSyntaxTree_Incremental_AdditionalLocations()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (ctx, comp) =>
                {
                    var validSyntaxTree = comp.SyntaxTrees.Single();
                    var invalidSyntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "/detached");
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        warningLevel: 1,
                        location: Location.Create(validSyntaxTree, TextSpan.FromBounds(0, 2)),
                        additionalLocations: new[] { Location.Create(invalidSyntaxTree, TextSpan.FromBounds(0, 2)) }));
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(PipelineCallbackGenerator), "Reported diagnostic 'TEST0001' has a source location in file '/detached', which is not part of the compilation being analyzed.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_DetachedSyntaxTree_Execute()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new CallbackGenerator(ctx => { }, ctx =>
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "/detached");
                ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                    "TEST0001",
                    "Test",
                    "Test diagnostic",
                    DiagnosticSeverity.Warning,
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 1,
                    location: Location.Create(syntaxTree, TextSpan.FromBounds(0, 2))));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(CallbackGenerator), "Reported diagnostic 'TEST0001' has a source location in file '/detached', which is not part of the compilation being analyzed.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_DetachedSyntaxTree_Execute_AdditionalLocations()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new CallbackGenerator(ctx => { }, ctx =>
            {
                var validSyntaxTree = ctx.Compilation.SyntaxTrees.Single();
                var invalidSyntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "/detached");
                ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                    "TEST0001",
                    "Test",
                    "Test diagnostic",
                    DiagnosticSeverity.Warning,
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 1,
                    location: Location.Create(validSyntaxTree, TextSpan.FromBounds(0, 2)),
                    additionalLocations: new[] { Location.Create(invalidSyntaxTree, TextSpan.FromBounds(0, 2)) }));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(CallbackGenerator), "Reported diagnostic 'TEST0001' has a source location in file '/detached', which is not part of the compilation being analyzed.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_SpanOutsideRange_Incremental()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions, sourceFileName: "/original");
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (ctx, comp) =>
                {
                    var syntaxTree = comp.SyntaxTrees.Single();
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        warningLevel: 1,
                        location: Location.Create(syntaxTree, TextSpan.FromBounds(0, 100))));
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(PipelineCallbackGenerator), "Reported diagnostic 'TEST0001' has a source location '[0..100)' in file '/original', which is outside of the given file.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_SpanOutsideRange_Incremental_AdditionalLocations()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions, sourceFileName: "/original");
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (ctx, comp) =>
                {
                    var syntaxTree = comp.SyntaxTrees.Single();
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        warningLevel: 1,
                        location: Location.Create(syntaxTree, TextSpan.FromBounds(0, 2)),
                        additionalLocations: new[] { Location.Create(syntaxTree, TextSpan.FromBounds(0, 100)) }));
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(PipelineCallbackGenerator), "Reported diagnostic 'TEST0001' has a source location '[0..100)' in file '/original', which is outside of the given file.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_SpanOutsideRange_Execute()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions, sourceFileName: "/original");
            compilation.VerifyDiagnostics();

            var generator = new CallbackGenerator(ctx => { }, ctx =>
            {
                var syntaxTree = ctx.Compilation.SyntaxTrees.Single();
                ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                    "TEST0001",
                    "Test",
                    "Test diagnostic",
                    DiagnosticSeverity.Warning,
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 1,
                    location: Location.Create(syntaxTree, TextSpan.FromBounds(0, 100))));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(CallbackGenerator), "Reported diagnostic 'TEST0001' has a source location '[0..100)' in file '/original', which is outside of the given file.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_SpanOutsideRange_Execute_AdditionalLocations()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions, sourceFileName: "/original");
            compilation.VerifyDiagnostics();

            var generator = new CallbackGenerator(ctx => { }, ctx =>
            {
                var syntaxTree = ctx.Compilation.SyntaxTrees.Single();
                ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                    "TEST0001",
                    "Test",
                    "Test diagnostic",
                    DiagnosticSeverity.Warning,
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 1,
                    location: Location.Create(syntaxTree, TextSpan.FromBounds(0, 2)),
                    additionalLocations: new[] { Location.Create(syntaxTree, TextSpan.FromBounds(0, 100)) }));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(CallbackGenerator), "Reported diagnostic 'TEST0001' has a source location '[0..100)' in file '/original', which is outside of the given file.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_SpaceInIdentifier_Incremental()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (ctx, comp) =>
                {
                    var syntaxTree = comp.SyntaxTrees.Single();
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST 0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        warningLevel: 1,
                        location: Location.Create(syntaxTree, TextSpan.FromBounds(0, 2))));
                });
            }).AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(PipelineCallbackGenerator), "Reported diagnostic has an ID 'TEST 0001', which is not a valid identifier.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")]
        public void Diagnostic_SpaceInIdentifier_Execute()
        {
            var source = "class C {}";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            var generator = new CallbackGenerator(ctx => { }, ctx =>
            {
                var syntaxTree = ctx.Compilation.SyntaxTrees.Single();
                ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                    "TEST 0001",
                    "Test",
                    "Test diagnostic",
                    DiagnosticSeverity.Warning,
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 1,
                    location: Location.Create(syntaxTree, TextSpan.FromBounds(0, 2))));
            });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);
            diagnostics.Verify(
                ArgumentExceptionDiagnostic(nameof(CallbackGenerator), "Reported diagnostic has an ID 'TEST 0001', which is not a valid identifier.", "diagnostic").WithLocation(1, 1));
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void IncrementalGenerator_Add_New_Generator_After_Generation()
        {
            // 1. run a generator, smuggling out some inputs from context
            // 2. add a second generator, re-using the inputs from the first step and using a Combine node
            // 3. run the new graph

            var source = @"
class C { }
";
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing);
            compilation.VerifyDiagnostics();

            IncrementalValueProvider<ParseOptions> parseOptionsProvider = default;
            IncrementalValueProvider<AnalyzerConfigOptionsProvider> configOptionsProvider = default;

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var source = parseOptionsProvider = ctx.ParseOptionsProvider;
                var source2 = configOptionsProvider = ctx.AnalyzerConfigOptionsProvider;
                var combine = source.Combine(source2);
                ctx.RegisterSourceOutput(combine, (spc, c) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator });
            driver = driver.RunGenerators(compilation);

            // parse options and analyzer options are now cached
            // add a new generator that depends on them
            bool wasCalled = false;
            var generator2 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2(ctx =>
            {
                var source = parseOptionsProvider;
                var source2 = configOptionsProvider;
                // this call should always be made, even though the above inputs are cached
                var transform = source.Select((a, _) => { wasCalled = true; return new object(); });
                // now combine source2 with the transform. Combine will call single on transform, and we'll crash if it wasn't called
                var combine = source2.Combine(transform);
                ctx.RegisterSourceOutput(combine, (spc, c) => { });
            }));

            driver = driver.AddGenerators(ImmutableArray.Create<ISourceGenerator>(generator2));
            driver = driver.RunGenerators(compilation);
            Assert.True(wasCalled);
        }

        [Fact]
        public void IncrementalGenerator_Add_New_Generator_After_Generation_SourceOutputNode()
        {
            // 1. run a generator, smuggling out some inputs from context
            // 2. add a second generator, re-using the inputs from the first step
            // 3. run the new graph

            var source = @"
class C { }
";
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing);
            compilation.VerifyDiagnostics();

            IncrementalValueProvider<ParseOptions> parseOptionsProvider = default;

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var source = parseOptionsProvider = ctx.ParseOptionsProvider;
                ctx.RegisterSourceOutput(source, (spc, c) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator });
            driver = driver.RunGenerators(compilation);

            // parse options are now cached
            // add a new generator that depends on them
            bool wasCalled = false;
            var generator2 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2(ctx =>
            {
                var source = parseOptionsProvider;
                ctx.RegisterSourceOutput(source, (spc, c) => { wasCalled = true; });
            }));

            driver = driver.AddGenerators(ImmutableArray.Create<ISourceGenerator>(generator2));
            driver = driver.RunGenerators(compilation);
            Assert.True(wasCalled);
        }

        [Fact]
        public void IncrementalGenerator_Add_New_Generator_With_Syntax_After_Generation()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            bool gen1Called = false;

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var syntax = ctx.SyntaxProvider.CreateSyntaxProvider((s, _) => true, (s, _) => s.Node);
                ctx.RegisterSourceOutput(syntax, (spc, c) =>
                {
                    gen1Called = true;
                });
            }));

            // run the generator and make sure the first node is cached
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            Assert.True(gen1Called);

            // now, add another syntax node from another generator
            var gen2Called = false;
            var generator2 = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator2(ctx =>
            {
                var syntax = ctx.SyntaxProvider.CreateSyntaxProvider((s, _) => true, (s, _) => s.Node);
                ctx.RegisterSourceOutput(syntax, (spc, c) =>
                {
                    gen2Called = true;
                });
            }));
            driver = driver.AddGenerators(ImmutableArray.Create<ISourceGenerator>(generator2));

            // ensure it runs successfully
            gen1Called = false;
            driver = driver.RunGenerators(compilation);

            Assert.False(gen1Called); // Generator 1 did not re-run
            Assert.True(gen2Called);
        }

        [Fact, WorkItem(66451, "https://github.com/dotnet/roslyn/issues/66451")]
        public void Transform_MultipleInputs_RemoveFirst_ModifySecond()
        {
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var provider = ctx.SyntaxProvider
                    .CreateSyntaxProvider(
                        static (node, _) => node is ClassDeclarationSyntax c,
                        static (gsc, _) => gsc.Node)
                    .Select(static (node, _) => (ClassDeclarationSyntax)node)
                    .Where(static (node) => node.Modifiers.Any(SyntaxKind.PartialKeyword))
                    .WithTrackingName("MyTransformNode");
                ctx.RegisterSourceOutput(provider, static (spc, syntax) =>
                {
                    spc.AddSource(
                        $"{syntax.Identifier.Text}.g",
                        $"partial class {syntax.Identifier.Text} {{ /* generated */ }}");
                });
            }));

            var parseOptions = TestOptions.RegularPreview;

            var source1 = """
                public partial class Class1 { }
                """;
            var source2 = """
                public partial class Class2 { }
                """;

            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            verify(ref driver, compilation);

            // Remove Class1 from the final provider via a TransformNode
            // (by removing the partial keyword).
            replace(ref compilation, parseOptions, "Class1", """
                public class Class1 { }
                """);
            verify(ref driver, compilation);

            // Modify Class2 (make it internal).
            replace(ref compilation, parseOptions, "Class2", """
                internal partial class Class2 { }
                """);
            verify(ref driver, compilation);

            static void verify(ref GeneratorDriver driver, Compilation compilation)
            {
                driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
                outputCompilation.VerifyDiagnostics();
                generatorDiagnostics.Verify();
            }

            static void replace(ref Compilation compilation, CSharpParseOptions parseOptions, string className, string source)
            {
                var tree = compilation.GetMember(className).DeclaringSyntaxReferences.Single().SyntaxTree;
                compilation = compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(source, parseOptions));
            }
        }

        private static DiagnosticDescription ArgumentExceptionDiagnostic(string generatorName, string message, string parameterName)
        {
            return Diagnostic("CS8785").WithArguments(generatorName, nameof(ArgumentException),
#if NETCOREAPP
                $"{message} (Parameter '{parameterName}')"
#else
                $"{message}{Environment.NewLine}Parameter name: {parameterName}"
#endif
                );
        }
    }
}
