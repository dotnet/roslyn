// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration;

public sealed class GeneratorDriverTests_Attributes_SimpleName : CSharpTestBase
{
    #region Non-Incremental tests

    // These tests just validate basic correctness of results in different scenarios, without actually validating
    // that the incremental nature of this provider works properly.

    [Theory]
    [InlineData("[X]")]
    [InlineData("[X, Y]")]
    [InlineData("[Y, X]")]
    [InlineData("[X, X]")]
    [InlineData("[X][Y]")]
    [InlineData("[Y][X]")]
    [InlineData("[X][X]")]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration1(string attribute)
    {
        var source = $$"""
            {{attribute}}
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Theory]
    [InlineData("XAttribute")]
    [InlineData("A.X")]
    [InlineData("A.XAttribute")]
    [InlineData("A.X<Y>")]
    [InlineData("global::X")]
    [InlineData("global::A.X")]
    public void FindFullAttributeNameOnTopLevelClass(string attribute)
    {
        var source = $$"""
            [{{attribute}}]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForDelegateDeclaration1()
    {
        var source = """
            [X]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<DelegateDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForDifferentName()
    {
        var source = """
            [X]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<DelegateDeclarationSyntax>("YAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForSyntaxNode1()
    {
        var source = """
            [X]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<SyntaxNode>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration1()
    {
        var source = """
            [X]
            class C { }
            [X]
            class D { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step =>
            {
                Assert.True(step.Outputs.First().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                Assert.True(step.Outputs.Last().Value is ClassDeclarationSyntax { Identifier.ValueText: "D" });
            });
    }

    [Fact]
    public void FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration2()
    {
        var source = """
            [X]
            class C { }
            [Y]
            class D { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step =>
            {
                Assert.True(step.Outputs.First().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                Assert.False(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "D" }));
            });
    }

    [Fact]
    public void FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration3()
    {
        var source = """
            [Y]
            class C { }
            [X]
            class D { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step =>
            {
                Assert.False(step.Outputs.Any(o => o.Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
                Assert.True(step.Outputs.Last().Value is ClassDeclarationSyntax { Identifier.ValueText: "D" });
            });
    }

    [Fact]
    public void FindAttributeOnNestedClasses_WhenSearchingForClassDeclaration1()
    {
        var source = """
            [X]
            class C
            {
                [X]
                class D { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step =>
            {
                Assert.True(step.Outputs.First().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                Assert.True(step.Outputs.Last().Value is ClassDeclarationSyntax { Identifier.ValueText: "D" });
            });
    }

    [Fact]
    public void FindAttributeOnClassInNamespace_WhenSearchingForClassDeclaration1()
    {
        var source = """
            namespace N
            {
                [X]
                class C { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.First().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_FullAttributeName1()
    {
        var source = """
            [X]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ShortAttributeName1()
    {
        var source = """
            [X]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("X");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindFullAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_FullAttributeName1()
    {
        var source = """
            [XAttribute]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Theory]
    [InlineData("A = XAttribute")]
    [InlineData("AAttribute = XAttribute")]
    [InlineData("A = M.XAttribute")]
    [InlineData("A = M.XAttribute<int>")]
    [InlineData("A = global::M.XAttribute<int>")]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias1(string alias)
    {
        var source = $$"""
            using {{alias}};

            [A]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias3()
    {
        var source = """
            using AAttribute = XAttribute;

            [AAttribute]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias1()
    {
        var source = """
            using AAttribute : X;

            [AAttribute]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias2()
    {
        var source = """
            using AAttribute : XAttribute;

            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases1()
    {
        var source = """
            using B = XAttribute;
            namespace N
            {
                using A = B;

                [A]
                class C { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_OuterAliasReferencesInnerAlias()
    {
        // note: this is not legal.  it's ok if this ever stops working in the futuer.
        var source = """
            using BAttribute = AAttribute;
            namespace N
            {
                using AAttribute = XAttribute;

                [B]
                class C { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases2()
    {
        var source = """
            using B = XAttribute;
            namespace N
            {
                using AAttribute = B;

                [A]
                class C { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases2()
    {
        var source = """
            using BAttribute = XAttribute;
            namespace N
            {
                using AAttribute = B;

                [A]
                class C { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias1()
    {
        var source = """
            using AAttribute = BAttribute;
            using BAttribute = AAttribute;

            [A]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias2()
    {
        var source = """
            using A = BAttribute;
            using B = AAttribute;

            [A]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias3()
    {
        var source = """
            using A = B;
            using B = A;

            [A]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_LocalAliasInDifferentFile1()
    {
        var source1 = """
            [A]
            class C { }
            """;
        var source2 = """
            using A = XAttribute;
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_LocalAliasInDifferentFile2()
    {
        var source1 = """
            [A]
            class C { }
            """;
        var source2 = """
            using AAttribute = XAttribute;
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Theory]
    [InlineData("A = XAttribute")]
    [InlineData("AAttribute = XAttribute")]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasInSameFile1(string alias)
    {
        var source = $$"""
            global using {{alias}};

            [A]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Theory]
    [InlineData("B = AAttribute")]
    [InlineData("BAttribute = AAttribute")]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasInSameFile1(string alias)
    {
        var source = $$"""
            global using AAttribute = XAttribute;
            using {{alias}};

            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Theory]
    [InlineData("A = XAttribute")]
    [InlineData("AAttribute = XAttribute")]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasDifferentFile1(string alias)
    {
        var source1 = """
            [A]
            class C { }
            """;
        var source2 = $"""
            global using {alias};
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_BothGlobalAndLocalAliasDifferentFile1()
    {
        var source1 = """
            [B]
            class C { }
            """;
        var source2 = """
            global using AAttribute = XAttribute;
            using B = AAttribute;
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasLoop1()
    {
        var source1 = """
            [A]
            class C { }
            """;
        var source2 = """
            global using AAttribute = BAttribute;
            global using BAttribute = AAttribute;
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasDifferentFile1()
    {
        var source1 = """
            using B = AAttribute;
            [B]
            class C { }
            """;
        var source2 = """
            global using AAttribute = XAttribute;
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasDifferentFile2()
    {
        var source1 = """
            using BAttribute = AAttribute;
            [B]
            class C { }
            """;
        var source2 = """
            global using AAttribute = XAttribute;
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    #endregion

    #region Incremental tests

    // These tests validate minimal recomputation performed after changes are made to the compilation.

    [Fact]
    public void RerunOnSameCompilationCachesResultFully()
    {
        var source = """
            [X]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        // re-run without changes
        driver = driver.RunGenerators(compilation);
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunOnCompilationWithReferencesChangeCachesResultFully()
    {
        var source = """
            [X]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        // re-run with just changes to references.  this helper is entirely syntactic, so nothing should change.
        driver = driver.RunGenerators(compilation.RemoveAllReferences());
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void TestSourceFileRemoved1()
    {
        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        // re-run with the file with the class removed.  this will remove the actual output.
        driver = driver.RunGenerators(compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Last()));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));

        // the per-file global aliases get changed (because the last file is removed).
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        // however, the collected global aliases stays the same.
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Removed, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void TestSourceFileChanged_AttributeRemoved1()
    {
        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Last(),
            compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("""
                class C { }
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Removed, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void TestSourceFileChanged_AttributeAdded1()
    {
        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));

        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Last(),
            compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("""
                [B]
                class C { }
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.New, o.Reason));
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void TestSourceFileChanged_NonVisibleChangeToGlobalAttributeFile()
    {
        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
                global using AAttribute = XAttribute;
                class Dummy {}
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Modified, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void TestRemoveGlobalAttributeFile1()
    {
        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.RemoveSyntaxTrees(
            compilation.SyntaxTrees.First()));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step =>
            {
                Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" });
                Assert.Equal(IncrementalStepRunReason.Removed, step.Outputs.Single().Reason);
            });

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Removed, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Modified, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Modified, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Removed, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void TestRemoveGlobalAttributeFile2()
    {
        var source0 = "";

        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source0, source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.RemoveSyntaxTrees(
            compilation.SyntaxTrees.First()));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void TestAddGlobalAttributeFile1()
    {
        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
                global using AAttribute = XAttribute;
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.New, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void TestAddGlobalAttributeFile2()
    {
        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
                global using BAttribute = XAttribute;
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.New, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
    }

    [Fact]
    public void TestAddSourceFileWithoutAttribute()
    {
        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
                class D { }
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void TestAddSourceFileWithAttribute()
    {
        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
                [A]
                class D { }
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.New, o.Reason));
        Assert.Collection(runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }),
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "D" }));
    }

    [Fact]
    public void TestReplaceSourceFileWithDifferentAttribute()
    {
        var source1 = """
            global using AAttribute = XAttribute;
            """;

        var source2 = """
            global using BAttribute = AAttribute;
            """;

        var source3 = """
            [B]
            class C { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithSimpleName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Last(),
            compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("""
                [A]
                class D { }
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Modified, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);

        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "D" }));
    }

    #endregion
}
