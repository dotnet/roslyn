// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration;

public class GeneratorDriverTests_Attributes_FullyQualifiedName : CSharpTestBase
{
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
    public void RerunWithAddedFile()
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
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From(""))));
        runResult = driver.GetRunResult().Results[0];

        Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
            step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C" }));

        Assert.Collection(runResult.TrackedSteps["individualFileGlobalAliases_ForAttribute"],
            s => Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            s => Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason));
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["collectedGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["allUpGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["compilationUnit_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["compilationUnitAndGlobalAliases_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["result_ForAttribute"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["collectedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps["groupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps["compilationAndGroupedNodes_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
        Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps["result_ForAttributeWithMetadataName"].Single().Outputs.Single().Reason);
    }
}
