// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration;

public class GeneratorDriverTests_Attributes_FullyQualifiedName : CSharpTestBase
{
    #region Non-Incremental tests

    // These tests just validate basic correctness of results in different scenarios, without actually validating
    // that the incremental nature of this provider works properly.

    [Fact]
    public void FindCorrectAttributeOnTopLevelClass_WhenSearchingForClassDeclaration1()
    {
        var source = @"
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
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("N1.XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }));
    }

    [Fact]
    public void FindCorrectAttributeOnTopLevelClass_WhenSearchingForClassDeclaration2()
    {
        var source = @"
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
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("N2.XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" }));
    }

    [Fact]
    public void FindNestedAttribute1()
    {
        var source = @"
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
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer1+InnerAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }));
    }

    [Fact]
    public void FindNestedAttribute2()
    {
        var source = @"
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
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer2+InnerAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" }));
    }

    [Fact]
    public void FindNestedGenericAttribute1()
    {
        var source = @"
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
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer1+InnerAttribute`1");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }));
    }

    [Fact]
    public void FindNestedGenericAttribute2()
    {
        var source = @"
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
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer2+InnerAttribute`2");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" }));
    }

    [Fact]
    public void DoNotFindNestedGenericAttribute1()
    {
        var source = @"
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
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer1+InnerAttribute`2");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));
    }

    [Fact]
    public void DoNotFindNestedGenericAttribute2()
    {
        var source = @"
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
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("Outer2+InnerAttribute`1");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));
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

class XAttribute : System.Attribute
{
}
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        // re-run without changes
        driver = driver.RunGenerators(compilation);
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["groupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithReferencesChange()
    {
        var source = @"
[X]
class C { }

class XAttribute : System.Attribute
{
}
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        // re-run without changes
        driver = driver.RunGenerators(compilation.RemoveAllReferences());
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["collectedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["groupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithAddedFile1()
    {
        var source = @"
[X]
class C { }

class XAttribute : System.Attribute
{
}
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From(""))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["groupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithAddedFile2()
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
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From(@"
class XAttribute : System.Attribute
{
}"))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["groupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }

    [Fact]
    public void RerunWithAddedFile_MultipleResults_SameFile1()
    {
        var source = @"
[X]
class C1 { }
[X]
class C2 { }
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: parseOptions);

        Assert.Single(compilation.SyntaxTrees);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From(@"
class XAttribute : System.Attribute
{
}"))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.Collection(step.Outputs,
                t => Assert.True(t.Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }),
                t => Assert.True(t.Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" })));

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"].Single().Outputs,
            t => Assert.Equal(IncrementalStepRunReason.Cached, t.Reason),
            t => Assert.Equal(IncrementalStepRunReason.Cached, t.Reason));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["groupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs,
            t => Assert.Equal(IncrementalStepRunReason.Modified, t.Reason),
            t => Assert.Equal(IncrementalStepRunReason.Modified, t.Reason));
    }

    [Fact]
    public void RerunWithAddedFile_MultipleResults_MultipleFile1()
    {
        var source1 = @"
[X]
class C1 { }
";
        var source2 = @"
[X]
class C2 { }
";
        var parseOptions = TestOptions.RegularPreview;
        Compilation compilation = CreateCompilation(new[] { source1, source2 }, options: TestOptions.DebugDll, parseOptions: parseOptions);

        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
        {
            var input = ctx.ForAttributeWithMetadataName<ClassDeclarationSyntax>("XAttribute");
            ctx.RegisterSourceOutput(input, (spc, node) => { });
        }));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult().Results[0];
        Console.WriteLine(runResult);

        Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"));

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From(@"
class XAttribute : System.Attribute
{
}"))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.Collection(step.Outputs, t => Assert.True(t.Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" })),
            step => Assert.Collection(step.Outputs, t => Assert.True(t.Value is ClassDeclarationSyntax { Identifier.ValueText: "C2" })));

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["compilationUnit_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason));
        Assert.Collection(runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Collection(runResult.TrackedSteps["result_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Collection(runResult.TrackedSteps["groupedNodes_ForAttributeWithMetadataName"],
            s => Assert.Collection(s.Outputs,
                t => Assert.Equal(IncrementalStepRunReason.Cached, t.Reason),
                t => Assert.Equal(IncrementalStepRunReason.Cached, t.Reason)));
        Assert.Collection(runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"],
            s => Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason));
        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            s => Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason));
    }

    #endregion
}
