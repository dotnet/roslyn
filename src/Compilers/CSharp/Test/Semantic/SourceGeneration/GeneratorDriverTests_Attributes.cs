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
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
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
    public class GeneratorDriverTests_Attributes : CSharpTestBase
    {
        #region Non-Incremental tests

        // These tests just validate basic correctness of results in different scenarios, without actually validating
        // that the incremental nature of this provider works properly.

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList1()
        {
            var source = @"
[X, Y]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList2()
        {
            var source = @"
[Y, X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList3()
        {
            var source = @"
[X, X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists1()
        {
            var source = @"
[X][Y]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists2()
        {
            var source = @"
[Y][X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists3()
        {
            var source = @"
[X][X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindFullAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[XAttribute]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindDottedAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[A.X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindDottedFullAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[A.XAttribute]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindDottedGenericAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[A.X<Y>]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindGlobalAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[global::X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindGlobalDottedAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[global::A.X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForDelegateDeclaration1()
        {
            var source = @"
[X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<DelegateDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForDifferentName()
        {
            var source = @"
[X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<DelegateDeclarationSyntax>("YAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForSyntaxNode1()
        {
            var source = @"
[X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<SyntaxNode>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[X]
class C { }
[X]
class D { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
                    Assert.True(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "D" }));
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration2()
        {
            var source = @"
[X]
class C { }
[Y]
class D { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
                    Assert.False(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "D" }));
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration3()
        {
            var source = @"
[Y]
class C { }
[X]
class D { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.False(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
                    Assert.True(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "D" }));
                });
        }

        [Fact]
        public void FindAttributeOnNestedClasses_WhenSearchingForClassDeclaration1()
        {
            var source = @"
[X]
class C
{
    [X]
    class D { }
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
                    Assert.True(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "D" }));
                });
        }

        [Fact]
        public void FindAttributeOnClassInNamespace_WhenSearchingForClassDeclaration1()
        {
            var source = @"
namespace N
{
    [X]
    class C { }
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_FullAttributeName1()
        {
            var source = @"
[X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ShortAttributeName1()
        {
            var source = @"
[X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("X")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindFullAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_FullAttributeName1()
        {
            var source = @"
[XAttribute]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias1()
        {
            var source = @"
using A = XAttribute;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias2()
        {
            var source = @"
using AAttribute = XAttribute;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias3()
        {
            var source = @"
using AAttribute = XAttribute;

[AAttribute]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias4()
        {
            var source = @"
using A = M.XAttribute;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias5()
        {
            var source = @"
using A = M.XAttribute<int>;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias6()
        {
            var source = @"
using A = global::M.XAttribute<int>;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias1()
        {
            var source = @"
using AAttribute : X;

[AAttribute]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias2()
        {
            var source = @"
using AAttribute : XAttribute;

[B]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases1()
        {
            var source = @"
using B = XAttribute;
namespace N
{
    using A = B;

    [A]
    class C { }
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases2()
        {
            var source = @"
using B = XAttribute;
namespace N
{
    using AAttribute = B;

    [A]
    class C { }
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases2()
        {
            var source = @"
using BAttribute = XAttribute;
namespace N
{
    using AAttribute = B;

    [A]
    class C { }
}
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias1()
        {
            var source = @"
using AAttribute = BAttribute;
using BAttribute = AAttribute;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias2()
        {
            var source = @"
using A = BAttribute;
using B = AAttribute;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias3()
        {
            var source = @"
using A = B;
using B = A;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_LocalAliasInDifferentFile1()
        {
            var source1 = @"
[A]
class C { }
";
            var source2 = @"
using A = XAttribute;
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_LocalAliasInDifferentFile2()
        {
            var source1 = @"
[A]
class C { }
";
            var source2 = @"
using AAttribute = XAttribute;
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasInSameFile1()
        {
            var source = @"
global using A = XAttribute;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasInSameFile2()
        {
            var source = @"
global using AAttribute = XAttribute;

[A]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasInSameFile1()
        {
            var source = @"
global using AAttribute = XAttribute;
using B = AAttribute;

[B]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasInSameFile2()
        {
            var source = @"
global using AAttribute = XAttribute;
using BAttribute = AAttribute;

[B]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasDifferentFile1()
        {
            var source1 = @"
[A]
class C { }
";
            var source2 = @"
global using A = XAttribute;
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasDifferentFile2()
        {
            var source1 = @"
[A]
class C { }
";
            var source2 = @"
global using AAttribute = XAttribute;
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_BothGlobalAndLocalAliasDifferentFile1()
        {
            var source1 = @"
[B]
class C { }
";
            var source2 = @"
global using AAttribute = XAttribute;
using B = AAttribute;
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasLoop1()
        {
            var source1 = @"
[A]
class C { }
";
            var source2 = @"
global using AAttribute = BAttribute;
global using BAttribute = AAttribute;
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"));
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasDifferentFile1()
        {
            var source1 = @"
using B = AAttribute;
[B]
class C { }
";
            var source2 = @"
global using AAttribute = XAttribute;
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        [Fact]
        public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasDifferentFile2()
        {
            var source1 = @"
using BAttribute = AAttribute;
[B]
class C { }
";
            var source2 = @"
global using AAttribute = XAttribute;
";

            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });
        }

        #endregion

        #region Incremental tests

        // These tests validate minimal recomputation performed after changes are made to the compilation.

        [Fact]
        public void RerunOnSameCompilationCachesResultFully()
        {
            var source = @"
[X]
class C { }
";
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute<ClassDeclarationSyntax>("XAttribute")
                    .WithTrackingName("FindX");

                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];
            Console.WriteLine(runResult);

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });

            // re-run without changes
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["FindX"],
                step =>
                {
                    Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                });

            Assert.Equal(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason, IncrementalStepRunReason.Unchanged);
            Assert.Equal(runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason, IncrementalStepRunReason.Cached);
            Assert.Equal(runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason, IncrementalStepRunReason.Cached);
            Assert.Equal(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason, IncrementalStepRunReason.Unchanged);
            Assert.Equal(runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason, IncrementalStepRunReason.Cached);
            Assert.Equal(runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason, IncrementalStepRunReason.Cached);

            foreach (var steps in runResult.TrackedSteps.Values)
            {
                Assert.Collection(steps, step =>
                {
                    Assert.Equal(IncrementalStepRunReason.Unchanged, step.Outputs[0].Reason);
                });
            }
        }

        #endregion
    }
}
