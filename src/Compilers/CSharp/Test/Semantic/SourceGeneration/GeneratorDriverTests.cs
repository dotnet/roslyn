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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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

            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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

        [ConditionalFact(typeof(MonoOrCoreClrOnly), Reason = "Desktop CLR displays argument exceptions differently")]
        public void Generator_HintName_MustBe_Unique_Across_Outputs()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview);
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
                Diagnostic("CS8785").WithArguments("PipelineCallbackGenerator", "ArgumentException", "The hintName 'test.cs' of the added source file must be unique within a generator. (Parameter 'hintName')").WithLocation(1, 1)
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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

        [ConditionalFact(typeof(MonoOrCoreClrOnly), Reason = "Desktop CLR displays argument exceptions differently")]
        public void Adding_A_Source_Text_Without_Encoding_Fails_Generation()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var generator = new CallbackGenerator((ic) => { }, (sgc) => { sgc.AddSource("a", SourceText.From("")); });

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var outputDiagnostics);

            Assert.Single(outputDiagnostics);
            outputDiagnostics.Verify(
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator", "ArgumentException", "The SourceText with hintName 'a.cs' must have an explicit encoding set. (Parameter 'source')").WithLocation(1, 1)
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            var options = new CompilerAnalyzerConfigOptionsProvider(ImmutableDictionary<object, AnalyzerConfigOptions>.Empty, new CompilerAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty.Add("a", "abc").Add("b", "def")));

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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
                Compilation compilation = CreateCompilation(source, sourceFileName: "sourcefile.cs", options: TestOptions.DebugDll, parseOptions: parseOptions);
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

        [Fact]
        public void GeneratorDriver_Prefers_Incremental_Generators()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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

        [Fact]
        public void Incremental_Generators_Exception_During_Execution_Doesnt_Produce_AnySource()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            List<Compilation> compilationsCalledFor = new List<Compilation>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var filePaths = ctx.CompilationProvider.SelectMany((c, _) => c.SyntaxTrees).Select((tree, _) => tree.FilePath);

                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) => { compilationsCalledFor.Add(c); });
            }));

            // run the generator once, and check it was passed the compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, compilationsCalledFor.Count);
            Assert.Equal(compilation, compilationsCalledFor[0]);

            // run the same compilation through again, and confirm the output wasn't called
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, compilationsCalledFor.Count);
            Assert.Equal(compilation, compilationsCalledFor[0]);
        }

        [Fact]
        public void IncrementalGenerator_Runs_Only_For_Changed_Inputs()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var text1 = new InMemoryAdditionalText("Text1", "content1");
            var text2 = new InMemoryAdditionalText("Text2", "content2");

            List<Compilation> compilationsCalledFor = new List<Compilation>();
            List<AdditionalText> textsCalledFor = new List<AdditionalText>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, c) => { compilationsCalledFor.Add(c); });

                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider, (spc, c) => { textsCalledFor.Add(c); });
            }));

            // run the generator once, and check it was passed the compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, additionalTexts: new[] { text1 }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, compilationsCalledFor.Count);
            Assert.Equal(compilation, compilationsCalledFor[0]);
            Assert.Equal(1, textsCalledFor.Count);
            Assert.Equal(text1, textsCalledFor[0]);

            // clear the results, add an additional text, but keep the compilation the same
            compilationsCalledFor.Clear();
            textsCalledFor.Clear();
            driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(text2));
            driver = driver.RunGenerators(compilation);
            Assert.Equal(0, compilationsCalledFor.Count);
            Assert.Equal(1, textsCalledFor.Count);
            Assert.Equal(text2, textsCalledFor[0]);

            // now edit the compilation
            compilationsCalledFor.Clear();
            textsCalledFor.Clear();
            var newCompilation = compilation.WithOptions(compilation.Options.WithModuleName("newComp"));
            driver = driver.RunGenerators(newCompilation);
            Assert.Equal(1, compilationsCalledFor.Count);
            Assert.Equal(newCompilation, compilationsCalledFor[0]);
            Assert.Equal(0, textsCalledFor.Count);

            // re run without changing anything
            compilationsCalledFor.Clear();
            textsCalledFor.Clear();
            driver = driver.RunGenerators(newCompilation);
            Assert.Equal(0, compilationsCalledFor.Count);
            Assert.Equal(0, textsCalledFor.Count);
        }

        [Fact]
        public void IncrementalGenerator_Can_Add_Comparer_To_Input_Node()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            List<AdditionalText> texts = new List<AdditionalText>() { new InMemoryAdditionalText("abc", "") };

            List<(Compilation, ImmutableArray<AdditionalText>)> calledFor = new List<(Compilation, ImmutableArray<AdditionalText>)>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var compilationSource = ctx.CompilationProvider.Combine(ctx.AdditionalTextsProvider.Collect())
                                                // comparer that ignores the LHS (additional texts)
                                                .WithComparer(new LambdaComparer<(Compilation, ImmutableArray<AdditionalText>)>((c1, c2) => c1.Item1 == c2.Item1, 0));
                ctx.RegisterSourceOutput(compilationSource, (spc, c) =>
                {
                    calledFor.Add(c);
                });
            }));

            // run the generator once, and check it was passed the compilation + additional texts
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: texts);
            driver = driver.RunGenerators(compilation);

            Assert.Equal(1, calledFor.Count);
            Assert.Equal(compilation, calledFor[0].Item1);
            Assert.Equal(texts[0], calledFor[0].Item2.Single());

            // edit the additional texts, and verify that the output was *not* called again on the next run
            driver = driver.RemoveAdditionalTexts(texts.ToImmutableArray());
            driver = driver.RunGenerators(compilation);

            Assert.Equal(1, calledFor.Count);

            // now edit the compilation, run the generator, and confirm that the output *was* called again this time with the new compilation and no additional texts
            Compilation newCompilation = compilation.WithOptions(compilation.Options.WithModuleName("newCompilation"));
            driver = driver.RunGenerators(newCompilation);
            Assert.Equal(2, calledFor.Count);
            Assert.Equal(newCompilation, calledFor[1].Item1);
            Assert.Empty(calledFor[1].Item2);
        }

        [Fact]
        public void IncrementalGenerator_Register_End_Node_Only_Once_Through_Combines()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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

        [Fact]
        public void IncrementalGenerator_PostInit_Source_Is_Cached()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            List<ClassDeclarationSyntax> classes = new List<ClassDeclarationSyntax>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterPostInitializationOutput(c => c.AddSource("a", "class D {}"));

                ctx.RegisterSourceOutput(ctx.SyntaxProvider.CreateSyntaxProvider(static (n, _) => n is ClassDeclarationSyntax, (gsc, _) => (ClassDeclarationSyntax)gsc.Node), (spc, node) => classes.Add(node));
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);

            Assert.Equal(2, classes.Count);
            Assert.Equal("C", classes[0].Identifier.ValueText);
            Assert.Equal("D", classes[1].Identifier.ValueText);

            // clear classes, re-run
            classes.Clear();
            driver = driver.RunGenerators(compilation);
            Assert.Empty(classes);

            // modify the original tree, see that the post init is still cached
            var c2 = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), CSharpSyntaxTree.ParseText("class E{}", parseOptions));
            classes.Clear();
            driver = driver.RunGenerators(c2);
            Assert.Single(classes);
            Assert.Equal("E", classes[0].Identifier.ValueText);
        }

        [Fact]
        public void Incremental_Generators_Can_Be_Cancelled()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            List<ParseOptions> parseOptionsCalledFor = new List<ParseOptions>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, p) => { parseOptionsCalledFor.Add(p); });
            }));

            // run the generator once, and check it was passed the parse options
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions);
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, parseOptionsCalledFor.Count);
            Assert.Equal(parseOptions, parseOptionsCalledFor[0]);

            // clear the results, and re-run
            parseOptionsCalledFor.Clear();
            driver = driver.RunGenerators(compilation);
            Assert.Empty(parseOptionsCalledFor);

            // now update the parse options
            parseOptionsCalledFor.Clear();
            var newParseOptions = parseOptions.WithDocumentationMode(DocumentationMode.Diagnose);
            driver = driver.WithUpdatedParseOptions(newParseOptions);

            // check we ran
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, parseOptionsCalledFor.Count);
            Assert.Equal(newParseOptions, parseOptionsCalledFor[0]);

            // clear the results, and re-run
            parseOptionsCalledFor.Clear();
            driver = driver.RunGenerators(compilation);
            Assert.Empty(parseOptionsCalledFor);

            // replace it with null, and check that it throws
            Assert.Throws<ArgumentNullException>(() => driver.WithUpdatedParseOptions(null!));
        }

        [Fact]
        public void AnalyzerConfig_Can_Be_Updated()
        {
            var source = @"
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);
            string? analyzerOptionsValue = string.Empty;

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.AnalyzerConfigOptionsProvider, (spc, p) => p.GlobalOptions.TryGetValue("test", out analyzerOptionsValue));
            }));

            var builder = ImmutableDictionary<string, string>.Empty.ToBuilder();
            builder.Add("test", "value1");
            var optionsProvider = new CompilerAnalyzerConfigOptionsProvider(ImmutableDictionary<object, AnalyzerConfigOptions>.Empty, new CompilerAnalyzerConfigOptions(builder.ToImmutable()));

            // run the generator once, and check it was passed the configs
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, optionsProvider: optionsProvider);
            driver = driver.RunGenerators(compilation);
            Assert.Equal("value1", analyzerOptionsValue);

            // clear the results, and re-run
            analyzerOptionsValue = null;
            driver = driver.RunGenerators(compilation);
            Assert.Null(analyzerOptionsValue);

            // now update the config
            analyzerOptionsValue = null;
            builder.Clear();
            builder.Add("test", "value2");
            var newOptionsProvider = optionsProvider.WithGlobalOptions(new CompilerAnalyzerConfigOptions(builder.ToImmutable()));
            driver = driver.WithUpdatedAnalyzerConfigOptions(newOptionsProvider);

            // check we ran
            driver = driver.RunGenerators(compilation);
            Assert.Equal("value2", analyzerOptionsValue);

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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            InMemoryAdditionalText additionalText1 = new InMemoryAdditionalText("path1.txt", "");
            InMemoryAdditionalText additionalText2 = new InMemoryAdditionalText("path2.txt", "");
            InMemoryAdditionalText additionalText3 = new InMemoryAdditionalText("path3.txt", "");


            List<string?> additionalTextPaths = new List<string?>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider.Select((t, _) => t.Path), (spc, p) => { additionalTextPaths.Add(p); });
            }));

            // run the generator once and check we saw the additional file
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText1, additionalText2, additionalText3 });
            driver = driver.RunGenerators(compilation);
            Assert.Equal(3, additionalTextPaths.Count);
            Assert.Equal("path1.txt", additionalTextPaths[0]);
            Assert.Equal("path2.txt", additionalTextPaths[1]);
            Assert.Equal("path3.txt", additionalTextPaths[2]);

            // re-run and check nothing else got added
            additionalTextPaths.Clear();
            driver = driver.RunGenerators(compilation);
            Assert.Empty(additionalTextPaths);

            // now, update the additional text, but keep the path the same
            additionalTextPaths.Clear();
            driver = driver.ReplaceAdditionalText(additionalText2, new InMemoryAdditionalText("path4.txt", ""));

            // run, and check that only the replaced file was invoked
            driver = driver.RunGenerators(compilation);
            Assert.Single(additionalTextPaths);
            Assert.Equal("path4.txt", additionalTextPaths[0]);

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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            InMemoryAdditionalText additionalText = new InMemoryAdditionalText("path.txt", "abc");

            List<string?> additionalTextPaths = new List<string?>();
            List<string?> additionalTextsContents = new List<string?>();

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var texts = ctx.AdditionalTextsProvider;
                var paths = texts.Select((t, _) => t?.Path);
                var contents = texts.Select((t, _) => t?.GetText()?.ToString());

                ctx.RegisterSourceOutput(paths, (spc, p) => { additionalTextPaths.Add(p); });
                ctx.RegisterSourceOutput(contents, (spc, p) => { additionalTextsContents.Add(p); });
            }));

            // run the generator once and check we saw the additional file
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, additionalTexts: new[] { additionalText });
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, additionalTextPaths.Count);
            Assert.Equal("path.txt", additionalTextPaths[0]);

            Assert.Equal(1, additionalTextsContents.Count);
            Assert.Equal("abc", additionalTextsContents[0]);

            // re-run and check nothing else got added
            driver = driver.RunGenerators(compilation);
            Assert.Equal(1, additionalTextPaths.Count);
            Assert.Equal(1, additionalTextsContents.Count);

            // now, update the additional text, but keep the path the same
            additionalTextPaths.Clear();
            additionalTextsContents.Clear();
            var secondText = new InMemoryAdditionalText("path.txt", "def");
            driver = driver.ReplaceAdditionalText(additionalText, secondText);

            // run, and check that only the contents got re-run
            driver = driver.RunGenerators(compilation);
            Assert.Empty(additionalTextPaths);

            Assert.Equal(1, additionalTextsContents.Count);
            Assert.Equal("def", additionalTextsContents[0]);

            // now replace the text with a different path, but the same text
            additionalTextPaths.Clear();
            additionalTextsContents.Clear();
            var thirdText = new InMemoryAdditionalText("path2.txt", "def");
            driver = driver.ReplaceAdditionalText(secondText, thirdText);

            // run, and check that only the paths got re-run
            driver = driver.RunGenerators(compilation);

            Assert.Equal(1, additionalTextPaths.Count);
            Assert.Equal("path2.txt", additionalTextPaths[0]);

            Assert.Empty(additionalTextsContents);
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
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
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
            Compilation compilation = CreateEmptyCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions, references: metadataRefs);
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
    }
}
