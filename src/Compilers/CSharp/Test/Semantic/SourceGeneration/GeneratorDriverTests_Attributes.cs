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

        //Assert.Collection(runResult.TrackedSteps["Classes"],
        //    step =>
        //    {
        //        Assert.Equal("C", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
        //        Assert.Equal(IncrementalStepRunReason.New, step.Outputs[0].Reason);
        //    },
        //    step =>
        //    {
        //        Assert.Equal("D", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
        //        Assert.Equal(IncrementalStepRunReason.New, step.Outputs[0].Reason);
        //    });

        //// re-run without changes
        //driver = driver.RunGenerators(compilation);
        //runResult = driver.GetRunResult().Results[0];

        //Assert.Collection(runResult.TrackedSteps["Classes"],
        //    step =>
        //    {
        //        Assert.Equal("C", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
        //        Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
        //    },
        //    step =>
        //    {
        //        Assert.Equal("D", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
        //        Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
        //    });

        //// modify the original tree, see that the post init is still cached
        //var c2 = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), CSharpSyntaxTree.ParseText("class E{}", parseOptions));
        //driver = driver.RunGenerators(c2);
        //runResult = driver.GetRunResult().Results[0];

        //Assert.Collection(runResult.TrackedSteps["Classes"],
        //    step =>
        //    {
        //        Assert.Equal("E", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
        //        Assert.Equal(IncrementalStepRunReason.Modified, step.Outputs[0].Reason);
        //    },
        //    step =>
        //    {
        //        Assert.Equal("D", ((ClassDeclarationSyntax)step.Outputs[0].Value).Identifier.ValueText);
        //        Assert.Equal(IncrementalStepRunReason.Cached, step.Outputs[0].Reason);
        //    });

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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator((ctx) =>
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
    }
}
