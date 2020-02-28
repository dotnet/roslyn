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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
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
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions);
            driver.RunFullGeneration(compilation, out var outputCompilation);

            Assert.Single(outputCompilation.SyntaxTrees);
            Assert.Equal(compilation, outputCompilation);

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
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            SingleFileTestGenerator testGenerator = new SingleFileTestGenerator(generatorSource);

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions).WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)));
            driver.RunFullGeneration(compilation, out var outputCompilation);

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
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            SingleFileTestGenerator testGenerator = new SingleFileTestGenerator(generatorSource);

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions).WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)));
            driver = driver.RunFullGeneration(compilation, out var outputCompilation1);
            driver = driver.RunFullGeneration(compilation, out var outputCompilation2);
            driver = driver.RunFullGeneration(compilation, out var outputCompilation3);

            Assert.Equal(2, outputCompilation1.SyntaxTrees.Count());
            Assert.Equal(2, outputCompilation2.SyntaxTrees.Count());
            Assert.Equal(2, outputCompilation3.SyntaxTrees.Count());

            Assert.NotEqual(compilation, outputCompilation1);
            Assert.NotEqual(compilation, outputCompilation2);
            Assert.NotEqual(compilation, outputCompilation3);
            Assert.Equal(outputCompilation1, outputCompilation2);
            Assert.Equal(outputCompilation2, outputCompilation3);
            Assert.Equal(outputCompilation3, outputCompilation1);
        }

        [Fact]
        public void TryApply_Edits_Fails_If_FullGeneration_Has_Not_Run()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator() { CanApplyChanges = false };

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions)
                .WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)));

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
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions)
                .WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)));

            // run an initial generation pass
            driver = driver.RunFullGeneration(compilation, out var outputCompilation);
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
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator() { CanApplyChanges = false };

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions)
                .WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)));

            driver.RunFullGeneration(compilation, out var outputCompilation);
            Assert.Single(outputCompilation.SyntaxTrees);

            // create an edit
            AdditionalFileAddedEdit edit = new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file2.cs", ""));
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
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions)
                .WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)));

            // run initial generation pass
            driver = driver.RunFullGeneration(compilation, out var outputCompilation);
            Assert.Single(outputCompilation.SyntaxTrees);

            // create an edit
            AdditionalFileAddedEdit edit = new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file1.cs", ""));
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
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions)
                .WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)))
                .WithAdditionalTexts(ImmutableArray.Create<AdditionalText>(new InMemoryAdditionalText("c:\\a\\file1.cs", "")));

            // run initial generation pass
            driver = driver.RunFullGeneration(compilation, out var outputCompilation);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // create an edit
            AdditionalFileAddedEdit edit = new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file2.cs", ""));
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(edit));

            // now try apply edits
            driver = driver.TryApplyEdits(compilation, out var editedCompilation, out var succeeded);
            Assert.True(succeeded);
            Assert.Equal(3, editedCompilation.SyntaxTrees.Count());

            // if we run a full compilation again, we should still get 3 syntax trees
            driver = driver.RunFullGeneration(compilation, out outputCompilation);
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());

            // lets add multiple edits   
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file3.cs", "")),
                                                                                new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file4.cs", "")),
                                                                                new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file5.cs", ""))));
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
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();
            var text = new InMemoryAdditionalText("c:\\a\\file1.cs", "");


            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions)
                .WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)))
                .WithAdditionalTexts(ImmutableArray.Create<AdditionalText>(text));

            driver = driver.RunFullGeneration(compilation, out var outputCompilation);

            // we should have the a single extra file for the additional texts
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // even if we run a full gen, or partial, nothing should change yet
            driver = driver.TryApplyEdits(outputCompilation, out var editedCompilation, out var succeeded);
            Assert.True(succeeded);
            Assert.Equal(2, editedCompilation.SyntaxTrees.Count());

            driver = driver.RunFullGeneration(compilation, out outputCompilation);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // create an edit
            AdditionalFileAddedEdit edit = new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file2.cs", ""));
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(edit));

            // now try apply edits
            driver = driver.TryApplyEdits(compilation, out editedCompilation, out succeeded);
            Assert.True(succeeded);
            Assert.Equal(3, editedCompilation.SyntaxTrees.Count());

            // if we run a full compilation again, we should still get 3 syntax trees
            driver = driver.RunFullGeneration(compilation, out outputCompilation);
            Assert.Equal(3, outputCompilation.SyntaxTrees.Count());
        }

        [Fact]
        public void Edits_Are_Applied_During_Full_Generation()
        {
            var source = @"
class C { }
";

            var parseOptions = TestOptions.Regular;
            Compilation compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, parseOptions: parseOptions);
            compilation.VerifyDiagnostics();
            Assert.Single(compilation.SyntaxTrees);

            AdditionalFileAddedGenerator testGenerator = new AdditionalFileAddedGenerator();
            var text = new InMemoryAdditionalText("c:\\a\\file1.cs", "");

            GeneratorDriver driver = new CSharpGeneratorDriver(compilation, parseOptions)
                .WithGeneratorProviders(ImmutableArray.Create<GeneratorProvider>(new InMemoryGeneratorProvider(testGenerator)))
                .WithAdditionalTexts(ImmutableArray.Create<AdditionalText>(text));

            driver.RunFullGeneration(compilation, out var outputCompilation);
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());

            // add multiple edits   
            driver = driver.WithPendingEdits(ImmutableArray.Create<PendingEdit>(new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file2.cs", "")),
                                                                                new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file3.cs", "")),
                                                                                new AdditionalFileAddedEdit(new InMemoryAdditionalText("c:\\a\\file4.cs", ""))));

            // but just do a full generation (don't try apply)
            driver.RunFullGeneration(compilation, out outputCompilation);
            Assert.Equal(5, outputCompilation.SyntaxTrees.Count());
        }
    }

    internal class InMemoryGeneratorProvider : GeneratorProvider
    {
        private ISourceGenerator _generator;

        public InMemoryGeneratorProvider(ISourceGenerator generator)
        {
            this._generator = generator;
        }

        public override ISourceGenerator GetGenerator()
        {
            return _generator;
        }
    }

    internal class SingleFileTestGenerator : ISourceGenerator
    {
        private readonly string _content;
        private readonly string _hintName;

        public SingleFileTestGenerator(string content, string hintName = "generatedFile")
        {
            _content = content;
            _hintName = hintName;
        }

        public void Execute(SourceGeneratorContext context)
        {
            context.AdditionalSources.Add(this._hintName, SourceText.From(_content, Encoding.UTF8));
        }
    }

    internal class AdditionalFileAddedGenerator : ISourceGenerator, ITriggeredByAdditionalFileGenerator
    {
        public bool CanApplyChanges { get; set; } = true;

        public void Execute(SourceGeneratorContext context)
        {
            foreach (var file in context.AnalyzerOptions.AdditionalFiles)
            {
                AddSourceForAdditionalFile(context.AdditionalSources, file);
            }
        }

        public bool UpdateContext(UpdateContext context, AdditionalFileEdit edit)
        {
            if (edit is AdditionalFileAddedEdit add && CanApplyChanges)
            {
                AddSourceForAdditionalFile(context.AdditionalSources, add.AddedText);
                return true;
            }
            return false;
        }

        private void AddSourceForAdditionalFile(AdditionalSourcesCollection sources, AdditionalText file) => sources.Add(GetGeneratedFileName(GetGeneratedFileName(file.Path)), SourceText.From("", Encoding.UTF8));

        private string GetGeneratedFileName(string path) => $"{Path.GetFileNameWithoutExtension(path)}.generated";
    }

    internal class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _content;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _content = SourceText.From(content);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _content;

    }
}

