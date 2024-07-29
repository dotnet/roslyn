// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    public static IncrementalValuesProvider<T> ForAttributeWithSimpleName<T>(
        this IncrementalGeneratorInitializationContext context, string simpleName)
        where T : SyntaxNode
    {
        return context.SyntaxProvider.ForAttributeWithSimpleName(
            simpleName,
            (node, _) => node is T).SelectMany((t, _) => t.matches.Cast<T>()).WithTrackingName("result_ForAttribute");
    }

    public static IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
        this IncrementalGeneratorInitializationContext context, string fullyQualifiedMetadataName)
        where T : SyntaxNode
    {
        return context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName,
            (node, _) => node is T,
            (context, cancellationToken) => (T)context.TargetNode);
    }
}

public sealed class GeneratorDriverTests_Attributes_FullyQualifiedName : CSharpTestBase
{
    #region Non-Incremental tests

    // These tests just validate basic correctness of results in different scenarios, without actually validating
    // that the incremental nature of this provider works properly.

    [Fact]
    public void FindCorrectAttributeOnTopLevelClass_WhenSearchingForClassDeclaration1()
    {
        var source = """
            [N1.X]
            class C1 { }
            [N2.X]
            class C2 { }

            namespace N1
            {
                class XAttribute : System.Attribute { }
            }

            namespace N2
            {
                class XAttribute : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("N1.XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }));
    }

    [Theory]
    [InlineData("XAttribute")]
    [InlineData("X")]
    [InlineData("N1.xAttribute")]
    [InlineData("N1.x")]
    public void DoNotFindAttributeOnTopLevelClass_WhenSearchingSimpleName1(string name)
    {
        var source = """
            [N1.X]
            class C1 { }
            [N2.X]
            class C2 { }

            namespace N1
            {
                class XAttribute : System.Attribute { }
            }

            namespace N2
            {
                class XAttribute : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>(name);
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));
    }

    [Fact]
    public void FindCorrectAttributeOnTopLevelClass_WhenSearchingForClassDeclaration2()
    {
        var source = """
            [N1.X]
            class C1 { }
            [N2.X]
            class C2 { }

            namespace N1
            {
                class XAttribute : System.Attribute { }
            }

            namespace N2
            {
                class XAttribute : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("N2.XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" }));
    }

    [Theory]
    [InlineData("CLSCompliant(true)")]
    [InlineData("CLSCompliantAttribute(true)")]
    [InlineData("System.CLSCompliant(true)")]
    [InlineData("System.CLSCompliantAttribute(true)")]
    public void FindAssemblyAttribute1(string attribute)
    {
        var source = $"""
            using System;
            [assembly: {attribute}]
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<CompilationUnitSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is CompilationUnitSyntax c && c.SyntaxTree == compilation.SyntaxTrees.Single()));
    }

    [Theory]
    [InlineData("CLSCompliant(true)")]
    [InlineData("CLSCompliantAttribute(true)")]
    [InlineData("System.CLSCompliant(true)")]
    [InlineData("System.CLSCompliantAttribute(true)")]
    public void FindModuleAttribute1(string attribute)
    {
        var source = $"""
            using System;
            [module: {attribute}]
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<CompilationUnitSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is CompilationUnitSyntax c && c.SyntaxTree == compilation.SyntaxTrees.Single()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("class WithoutAttributes { }")]
    public void FindAssemblyAttribute2(string source2)
    {
        var source1 = """
            using System;
            [assembly: CLSCompliant(true)]
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<CompilationUnitSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is CompilationUnitSyntax c && c.SyntaxTree == compilation.SyntaxTrees.First()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("class WithoutAttributes { }")]
    public void FindAssemblyAttribute3(string source1)
    {
        var source2 = """
            using System;
            [assembly: CLSCompliant(true)]
            """;

        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<CompilationUnitSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is CompilationUnitSyntax c && c.SyntaxTree == compilation.SyntaxTrees.Last()));
    }

    [Fact]
    public void FindAssemblyAttribute4()
    {
        var source1 = """
            using System;
            [assembly: CLSCompliant(true)]
            """;
        var source2 = """
            using System;
            [assembly: CLSCompliant(false)]
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<CompilationUnitSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is CompilationUnitSyntax c && c.SyntaxTree == compilation.SyntaxTrees.First()),
            step => Assert.True(step.Outputs.Single().Value is CompilationUnitSyntax c && c.SyntaxTree == compilation.SyntaxTrees.Last()));
    }

    [Fact]
    public void FindTopLocalFunctionAttribute1()
    {
        var source = """
            using System;

            [CLSCompliant(true)]
            void LocalFunc()
            {
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<LocalFunctionStatementSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is LocalFunctionStatementSyntax { Identifier.ValueText: "LocalFunc" }));
    }

    [Fact]
    public void FindNestedLocalFunctionAttribute1()
    {
        var source = """
            using System;

            class C
            {
                void M()
                {
                    [CLSCompliant(true)]
                    void LocalFunc()
                    {
                    }
                }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<LocalFunctionStatementSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is LocalFunctionStatementSyntax { Identifier.ValueText: "LocalFunc" }));
    }

    [Fact]
    public void FindNestedLocalFunctionAttribute2()
    {
        var source = """
            using System;

            class C
            {
                void M()
                {
                    var v = () =>
                    {
                        [CLSCompliant(true)]
                        void LocalFunc()
                        {
                        }
                    };
                }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<LocalFunctionStatementSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is LocalFunctionStatementSyntax { Identifier.ValueText: "LocalFunc" }));
    }

    [Fact]
    public void FindTypeParameterFunctionAttribute1()
    {
        var source = """
            using System;

            class C<[CLSCompliant(true)] T>
            {
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<TypeParameterSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is TypeParameterSyntax { Identifier.ValueText: "T" }));
    }

    [Fact]
    public void FindMethodAttribute1()
    {
        var source = """
            using System;

            class C
            {
                [CLSCompliant(true)]
                void M()
                {
                }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<MethodDeclarationSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is MethodDeclarationSyntax { Identifier.ValueText: "M" }));
    }

    [Fact]
    public void FindMethodReturnAttribute1()
    {
        var source = """
            using System;

            class C
            {
                [return: CLSCompliant(true)]
                void M()
                {
                }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<MethodDeclarationSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is MethodDeclarationSyntax { Identifier.ValueText: "M" }));
    }

    [Fact]
    public void FindPartialMethodAttribute1()
    {
        var source = """
            using System;

            class C
            {
                [CLSCompliant(true)]
                internal partial void M();
                internal partial void M() { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<MethodDeclarationSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is MethodDeclarationSyntax { Identifier.ValueText: "M", Body: null, ExpressionBody: null }));
    }

    [Fact]
    public void FindPartialMethodAttribute2()
    {
        var source = """
            using System;

            class C
            {
                internal partial void M();
                [CLSCompliant(true)]
                internal partial void M() { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<MethodDeclarationSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is MethodDeclarationSyntax { Identifier.ValueText: "M", Body: not null }));
    }

    [Fact]
    public void FindFieldAttribute1()
    {
        var source = """
            using System;

            class C
            {
                [CLSCompliant(true)]
                int m;
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<VariableDeclaratorSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is VariableDeclaratorSyntax { Identifier.ValueText: "m" }));
    }

    [Fact]
    public void FindFieldAttribute2()
    {
        var source = """
            using System;

            class C
            {
                [CLSCompliant(true)]
                int m, n;
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<VariableDeclaratorSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.Collection(step.Outputs,
                v => Assert.True(v.Value is VariableDeclaratorSyntax { Identifier.ValueText: "m" }),
                v => Assert.True(v.Value is VariableDeclaratorSyntax { Identifier.ValueText: "n" })));
    }

    [Fact]
    public void FindEventFieldAttribute1()
    {
        var source = """
            using System;

            class C
            {
                [CLSCompliant(true)]
                event Action m;
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<VariableDeclaratorSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is VariableDeclaratorSyntax { Identifier.ValueText: "m" }));
    }

    [Fact]
    public void FindEventFieldAttribute2()
    {
        var source = """
            using System;

            class C
            {
                [CLSCompliant(true)]
                event Action m, n;
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<VariableDeclaratorSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.Collection(step.Outputs,
                v => Assert.True(v.Value is VariableDeclaratorSyntax { Identifier.ValueText: "m" }),
                v => Assert.True(v.Value is VariableDeclaratorSyntax { Identifier.ValueText: "n" })));
    }

    [Fact]
    public void FindParenthesizedLambdaAttribute1()
    {
        var source = """
            using System;

            Func<int, int> v = [CLSCompliant(true)] (int i) => i;
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<LambdaExpressionSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is LambdaExpressionSyntax));
    }

    [Fact]
    public void FindAccessorAttribute1()
    {
        var source = """
            using System;

            class C
            {
                int Prop
                {
                    [CLSCompliant(true)]
                    get => 0;
                }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<AccessorDeclarationSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is AccessorDeclarationSyntax { RawKind: (int)SyntaxKind.GetAccessorDeclaration }));
    }

    [Fact]
    public void FindTypeParameterAttribute1()
    {
        var source = """
            using System;

            class C<[CLSCompliant(true)]T>
            {
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<TypeParameterSyntax>("System.CLSCompliantAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is TypeParameterSyntax { Identifier.ValueText: "T" }));
    }

    [Fact]
    public void FindNestedAttribute1()
    {
        var source = """
            [Outer1.Inner]
            class C1 { }
            [Outer2.Inner]
            class C2 { }

            class Outer1
            {
                public class InnerAttribute : System.Attribute { }
            }
            class Outer2
            {
                public class InnerAttribute : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer1+InnerAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }));
    }

    [Fact]
    public void FindNestedAttribute2()
    {
        var source = """
            [Outer1.Inner]
            class C1 { }
            [Outer2.Inner]
            class C2 { }

            class Outer1
            {
                public class InnerAttribute : System.Attribute { }
            }
            class Outer2
            {
                public class InnerAttribute : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer2+InnerAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" }));
    }

    [Fact]
    public void FindNestedGenericAttribute1()
    {
        var source = """
            [Outer1.Inner<int>]
            class C1 { }
            [Outer2.Inner<int, string>]
            class C2 { }

            class Outer1
            {
                public class InnerAttribute<T1> : System.Attribute{ }
            }
            class Outer2
            {
                public class InnerAttribute<T1, T2> : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer1+InnerAttribute`1");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }));
    }

    [Fact]
    public void FindNestedGenericAttribute2()
    {
        var source = """
            [Outer1.Inner<int>]
            class C1 { }
            [Outer2.Inner<int, string>]
            class C2 { }

            class Outer1
            {
                public class InnerAttribute<T1> : System.Attribute{ }
            }
            class Outer2
            {
                public class InnerAttribute<T1, T2> : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer2+InnerAttribute`2");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" }));
    }

    [Fact]
    public void DoNotFindNestedGenericAttribute1()
    {
        var source = """
            [Outer1.Inner<int>]
            class C1 { }
            [Outer2.Inner<int, string>]
            class C2 { }

            class Outer1
            {
                public class InnerAttribute<T1> : System.Attribute{ }
            }
            class Outer2
            {
                public class InnerAttribute<T1, T2> : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer1+InnerAttribute`2");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));
    }

    [Fact]
    public void DoNotFindNestedGenericAttribute2()
    {
        var source = """
            [Outer1.Inner<int>]
            class C1 { }
            [Outer2.Inner<int, string>]
            class C2 { }

            class Outer1
            {
                public class InnerAttribute<T1> : System.Attribute{ }
            }
            class Outer2
            {
                public class InnerAttribute<T1, T2> : System.Attribute { }
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer2+InnerAttribute`1");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists1()
    {
        var source = """
            [X][X]
            class C { }

            class XAttribute : System.Attribute { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var counter = 0;
        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.SyntaxProvider.ForAttributeWithMetadataName<ClassDeclarationSyntax>(
                "XAttribute",
                (_, _) => true,
                (ctx, _) =>
                {
                    Assert.True(ctx.Attributes.Length == 2);
                    return (ClassDeclarationSyntax)ctx.TargetNode;
                });
            ctx.RegisterSourceOutput(input, (spc, node) => { counter++; });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
        Assert.Equal(1, counter);
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists1B()
    {
        var source = """
            [X, X]
            class C { }

            class XAttribute : System.Attribute { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var counter = 0;
        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.SyntaxProvider.ForAttributeWithMetadataName<ClassDeclarationSyntax>(
                "XAttribute",
                (_, _) => true,
                (ctx, _) =>
                {
                    Assert.True(ctx.Attributes.Length == 2);
                    return (ClassDeclarationSyntax)ctx.TargetNode;
                });
            ctx.RegisterSourceOutput(input, (spc, node) => { counter++; });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
        Assert.Equal(1, counter);
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists2()
    {
        var source = """
            [X][Y]
            class C { }

            class XAttribute : System.Attribute { }
            class YAttribute : System.Attribute { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var counter = 0;
        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.SyntaxProvider.ForAttributeWithMetadataName<ClassDeclarationSyntax>(
                "XAttribute",
                (_, _) => true,
                (ctx, _) =>
                {
                    Assert.True(ctx.Attributes.Length == 1);
                    return (ClassDeclarationSyntax)ctx.TargetNode;
                });
            ctx.RegisterSourceOutput(input, (spc, node) => { counter++; });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
        Assert.Equal(1, counter);
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists2B()
    {
        var source = """
            [X, Y]
            class C { }

            class XAttribute : System.Attribute { }
            class YAttribute : System.Attribute { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var counter = 0;
        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.SyntaxProvider.ForAttributeWithMetadataName<ClassDeclarationSyntax>(
                "XAttribute",
                (_, _) => true,
                (ctx, _) =>
                {
                    Assert.True(ctx.Attributes.Length == 1);
                    return (ClassDeclarationSyntax)ctx.TargetNode;
                });
            ctx.RegisterSourceOutput(input, (spc, node) => { counter++; });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
        Assert.Equal(1, counter);
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists3()
    {
        var source = """
            [Y][X]
            class C { }

            class XAttribute : System.Attribute { }
            class YAttribute : System.Attribute { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var counter = 0;
        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.SyntaxProvider.ForAttributeWithMetadataName<ClassDeclarationSyntax>(
                "XAttribute",
                (_, _) => true,
                (ctx, _) =>
                {
                    Assert.True(ctx.Attributes.Length == 1);
                    return (ClassDeclarationSyntax)ctx.TargetNode;
                });
            ctx.RegisterSourceOutput(input, (spc, node) => { counter++; });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
        Assert.Equal(1, counter);
    }

    [Fact]
    public void FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists3B()
    {
        var source = """
            [Y, X]
            class C { }

            class XAttribute : System.Attribute { }
            class YAttribute : System.Attribute { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var counter = 0;
        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.SyntaxProvider.ForAttributeWithMetadataName<ClassDeclarationSyntax>(
                "XAttribute",
                (_, _) => true,
                (ctx, _) =>
                {
                    Assert.True(ctx.Attributes.Length == 1);
                    return (ClassDeclarationSyntax)ctx.TargetNode;
                });
            ctx.RegisterSourceOutput(input, (spc, node) => { counter++; });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));
        Assert.Equal(1, counter);
    }

    [Fact, WorkItem(66451, "https://github.com/dotnet/roslyn/issues/66451")]
    public void MultipleInputs_RemoveFirst_ModifySecond()
    {
        var source0 = "public class GenerateAttribute : System.Attribute { }";
        var comp0 = CreateCompilation(source0).VerifyDiagnostics().EmitToImageReference();

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var provider = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("GenerateAttribute");
            ctx.RegisterSourceOutput(provider, static (spc, syntax) => spc.AddSource(
                $"{syntax.Identifier.Text}.g",
                $"partial class {syntax.Identifier.Text} {{ /* generated */ }}"));
        }));

        var parseOptions = TestOptions.RegularPreview;

        var source1 = """
            [Generate]
            [System.Obsolete]
            public partial class Class1 { }
            """;
        var source2 = """
            [Generate]
            [System.Obsolete]
            public partial class Class2 { }
            """;

        Compilation compilation = CreateCompilation(new[] { source1, source2 }, new[] { comp0 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, parseOptions: parseOptions);
        verify(ref driver, compilation,
            ("Class1.g.cs", "partial class Class1 { /* generated */ }"),
            ("Class2.g.cs", "partial class Class2 { /* generated */ }"));

        // Remove Class1 from the final provider via a TransformNode
        // (by removing the Generate attribute).
        replace(ref compilation, parseOptions, "Class1", """
            //[Generate]
            [System.Obsolete]
            public partial class Class1 { }
            """);
        verify(ref driver, compilation,
            ("Class2.g.cs", "partial class Class2 { /* generated */ }"));

        // Modify Class2 (make it internal).
        replace(ref compilation, parseOptions, "Class2", """
            [Generate]
            [System.Obsolete]
            internal partial class Class2 { }
            """);
        verify(ref driver, compilation,
            ("Class2.g.cs", "partial class Class2 { /* generated */ }"));

        static void verify(ref GeneratorDriver driver, Compilation compilation, params (string HintName, string SourceText)[] expectedGeneratedSources)
        {
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
            outputCompilation.VerifyDiagnostics();
            generatorDiagnostics.Verify();
            Assert.Equal(expectedGeneratedSources, driver.GetRunResult().Results.Single().GeneratedSources.Select(s => (s.HintName, s.SourceText.ToString())));
        }

        static void replace(ref Compilation compilation, CSharpParseOptions parseOptions, string className, string source)
        {
            var tree = compilation.GetMember(className).DeclaringSyntaxReferences.Single().SyntaxTree;
            compilation = compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(source, parseOptions));
        }
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

            class XAttribute : System.Attribute
            {
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        // re-run without changes
        driver = driver.RunGenerators(compilation);
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttributeInternal"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithReferencesChange()
    {
        var source = """
            [X]
            class C { }

            class XAttribute : System.Attribute
            {
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        // re-run without changes
        driver = driver.RunGenerators(compilation.RemoveAllReferences());
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttributeInternal"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithAddedFile1()
    {
        var source = """
            [X]
            class C { }

            class XAttribute : System.Attribute
            {
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From(""))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttributeInternal"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithAddedFile2()
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
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
            class XAttribute : System.Attribute
            {
            }
            """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttributeInternal"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithAddedFile_MultipleResults_SameFile1()
    {
        var source = """
            [X]
            class C1 { }
            [X]
            class C2 { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
            class XAttribute : System.Attribute
            {
            }
            """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.Collection(step.Outputs,
                t => Assert.True(t.Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }),
                t => Assert.True(t.Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" })));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason));
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["result_ForAttributeInternal"].Single().Outputs,
            t => Assert.Equal(IncrementalStepRunReason.Cached, t.Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs,
            t => Assert.Equal(IncrementalStepRunReason.New, t.Reason),
            t => Assert.Equal(IncrementalStepRunReason.New, t.Reason));
    }

    [Fact]
    public void RerunWithAddedFile_MultipleResults_MultipleFile1()
    {
        var source1 = """
            [X]
            class C1 { }
            """;
        var source2 = """
            [X]
            class C2 { }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
            class XAttribute : System.Attribute
            {
            }
            """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.Collection(step.Outputs, t => Assert.True(t.Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" })),
            step => Assert.Collection(step.Outputs, t => Assert.True(t.Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" })));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason));
        Assert.Collection(runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Collection(runResult.TrackedSteps["result_ForAttributeInternal"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Collection(runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"],
            s => Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason));
        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
    }

    [Fact]
    public void RerunWithChangedFileThatNowReferencesAttribute1()
    {
        var source = """
            class C { }

            class XAttribute : System.Attribute
            {
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));

        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
                [X]
                class C { }

                class XAttribute : System.Attribute
                {
                }
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["result_ForAttributeInternal"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithChangedFileThatNowReferencesAttribute2()
    {
        var source1 = """
            class C { }
            """;
        var source2 = """
            class XAttribute : System.Attribute
            {
            }
            """;
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));

        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("""
                [X]
                class C { }
                """))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs,
            o => Assert.Equal(IncrementalStepRunReason.New, o.Reason));
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["result_ForAttributeInternal"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    #endregion
}
