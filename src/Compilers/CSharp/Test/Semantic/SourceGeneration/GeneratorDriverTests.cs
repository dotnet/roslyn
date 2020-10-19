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
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

#nullable enable
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
        public void TryApply_Edits_Fails_If_FullGeneration_Has_Not_Run()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator() { CanApplyChanges = false };

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);

            // try apply edits should fail if we've not run a full compilation yet
            driver = driver.TryApplyEdits(compilation, out var outputCompilation, out var succeeded);
            Assert.False(succeeded);
            Assert.Equal(compilation, outputCompilation);
        }

        [Fact]
        public void TryApply_Edits_Does_Nothing_When_Nothing_Pending()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);

            // run an initial generation pass
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            Assert.Single(outputCompilation.SyntaxTrees);

            // now try apply edits (which should succeed, but do nothing)
            driver = driver.TryApplyEdits(compilation, out var editedCompilation, out var succeeded);
            Assert.True(succeeded);
            Assert.Equal(outputCompilation, editedCompilation);
        }

        [Fact]
        public void Failed_Edit_Does_Not_Change_Compilation()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator() { CanApplyChanges = false };

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            Assert.Single(outputCompilation.SyntaxTrees);

            // create an edit
            AdditionalFileAddedEdit edit = new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file2.cs", ""));
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(edit));

            // now try apply edits (which will fail)
            driver = driver.TryApplyEdits(compilation, out var editedCompilation, out var succeeded);
            Assert.False(succeeded);
            Assert.Single(editedCompilation.SyntaxTrees);
            Assert.Equal(compilation, editedCompilation);
        }

        [Fact]
        public void Added_Additional_File()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: parseOptions);

            // run initial generation pass
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            Assert.Single(outputCompilation.SyntaxTrees);

            // create an edit
            AdditionalFileAddedEdit edit = new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file1.cs", ""));
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(edit));

            // now try apply edits
            driver = driver.TryApplyEdits(compilation, out outputCompilation, out var succeeded);
            Assert.True(succeeded);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
        }

        [Fact]
        public void Multiple_Added_Additional_Files()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(parseOptions: parseOptions,
                                                                  generators: ImmutableArray.Create<ISourceGenerator>(testGenerator),
                                                                  optionsProvider: CompilerAnalyzerConfigOptionsProvider.Empty,
                                                                  additionalTexts: ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("a\\file1.cs", "")));

            // run initial generation pass
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // create an edit
            AdditionalFileAddedEdit edit = new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file2.cs", ""));
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(edit));

            // now try apply edits
            driver = driver.TryApplyEdits(compilation, out var editedCompilation, out var succeeded);
            Assert.True(succeeded);
            Assert.Equal(3, editedCompilation.SyntaxTrees.Count());

            // if we run a full compilation again, we should still get 3 syntax trees
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());

            // lets add multiple edits   
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file3.cs", "")),
                                                                                new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file4.cs", "")),
                                                                                new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file5.cs", ""))));
            // now try apply edits
            driver = driver.TryApplyEdits(compilation, out editedCompilation, out succeeded);
            Assert.True(succeeded);
            Assert.Equal(6, editedCompilation.SyntaxTrees.Count());
        }

        [Fact]
        public void Added_Additional_File_With_Full_Generation()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();
            var text = new InMemoryAdditionalText("a\\file1.cs", "");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(parseOptions: parseOptions,
                                                                  generators: ImmutableArray.Create<ISourceGenerator>(testGenerator),
                                                                  optionsProvider: CompilerAnalyzerConfigOptionsProvider.Empty,
                                                                  additionalTexts: ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("a\\file1.cs", "")));

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            // we should have a single extra file for the additional texts
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // even if we run a full gen, or partial, nothing should change yet
            driver = driver.TryApplyEdits(outputCompilation, out var editedCompilation, out var succeeded);
            Assert.True(succeeded);
            Assert.Equal(2, editedCompilation.SyntaxTrees.Count());

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // create an edit
            AdditionalFileAddedEdit edit = new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file2.cs", ""));
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(edit));

            // now try apply edits
            driver = driver.TryApplyEdits(compilation, out editedCompilation, out succeeded);
            Assert.True(succeeded);
            Assert.Equal(3, editedCompilation.SyntaxTrees.Count());

            // if we run a full compilation again, we should still get 3 syntax trees
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
        }

        [Fact]
        public void Edits_Are_Applied_During_Full_Generation()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();
            var text = new InMemoryAdditionalText("a\\file1.cs", "");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(parseOptions: parseOptions,
                                                                  generators: ImmutableArray.Create<ISourceGenerator>(testGenerator),
                                                                  optionsProvider: CompilerAnalyzerConfigOptionsProvider.Empty,
                                                                  additionalTexts: ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("a\\file1.cs", "")));

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // add multiple edits   
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file2.cs", "")),
                                                                                new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file3.cs", "")),
                                                                                new AdditionalFileAddedEdit(new InMemoryAdditionalText("a\\file4.cs", ""))));

            // but just do a full generation (don't try apply)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
            Assert.Equal(5, outputCompilation.SyntaxTrees.Count());
        }

        [Fact]
        public void Adding_Another_Generator_Makes_TryApplyEdits_Fail()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            SingleFileTestGenerator testGenerator1 = new SingleFileTestGenerator("public class D { }");
            SingleFileTestGenerator2 testGenerator2 = new SingleFileTestGenerator2("public class E { }");

            GeneratorDriver driver = CSharpGeneratorDriver.Create(parseOptions: parseOptions,
                                                                  generators: ImmutableArray.Create<ISourceGenerator>(testGenerator1),
                                                                  optionsProvider: CompilerAnalyzerConfigOptionsProvider.Empty,
                                                                  additionalTexts: ImmutableArray<AdditionalText>.Empty);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // try apply edits
            driver = driver.TryApplyEdits(compilation, out _, out bool success);
            Assert.True(success);

            // add another generator
            driver = driver.AddGenerators(ImmutableArray.Create<ISourceGenerator>(testGenerator2));

            // try apply changes should now fail
            driver = driver.TryApplyEdits(compilation, out _, out success);
            Assert.False(success);

            // full generation
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());

            // try apply changes should now succeed
            driver.TryApplyEdits(compilation, out _, out success);
            Assert.True(success);
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
                onExecute: (e) => { e.AddSource("a", SourceText.From("public class E {}", Encoding.UTF8)); }
                );


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
                Diagnostic("CS" + (int)ErrorCode.WRN_GeneratorFailedDuringGeneration).WithArguments("CallbackGenerator", "ArgumentException", "The provided SourceText must have an explicit encoding set. (Parameter 'source')").WithLocation(1, 1)
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
    }
}
