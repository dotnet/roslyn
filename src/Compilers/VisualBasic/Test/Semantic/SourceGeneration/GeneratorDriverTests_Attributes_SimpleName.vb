' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities.TestGenerators
Imports Roslyn.Utilities
Imports Xunit
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests.SourceGeneration

    Public Class GeneratorDriverTests_Attributes_SimpleName
        Inherits BasicTestBase

#Region "Non-Incremental tests"

        ' These tests just validate basic correctness of results in different scenarios, without actually validating
        ' that the incremental nature of this provider works properly.

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration1()
            Dim source = "
[X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(
                                                        Sub(ctx)
                                                            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                            ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                            End Sub)
                                                        End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create({generator}, parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:   "C" }))
    End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList1()
            Dim source = "
[X, Y]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, (spc, node) >= {})
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create({generator}, parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:       "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList2()
            Dim source = "
[Y, X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList3()
    {
        Dim source = "
[X, X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists1()
            Dim source = "
[X][Y]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists2()
            Dim source = "
[Y][X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributeLists3()
            Dim source = "
[X][X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindFullAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
            Dim source = "
[XAttribute]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindDottedAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
    {
        Dim source = "
[A.X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindDottedFullAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
            Dim source = "
[A.XAttribute]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindDottedGenericAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
            Dim source = "
[A.X<Y>]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindGlobalAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
        Dim source = "
[global::X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindGlobalDottedAttributeNameOnTopLevelClass_WhenSearchingForClassDeclaration1()
        Dim source = "
[global::A.X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForDelegateDeclaration1()
            Dim source = "
[X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute < DelegateDeclarationSyntax > ("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForDifferentName()
            Dim source = "
[X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute < DelegateDeclarationSyntax > ("YAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForSyntaxNode1()
        Dim source = "
[X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute (of SyntaxNode ) ("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration1()
            Dim source = "
[X]
class C { }
[X]
class D { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step)
                    Assert.True(_step.Outputs.Any(o >= o.Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
                Assert.True(_step.Outputs.Any(o >= o.Value Is ClassDeclarationSyntax { Identifier.ValueText:  "D" }))
        End Sub)
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration2()
            Dim source = "
[X]
class C { }
[Y]
class D { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step)
                    Assert.True(_step.Outputs.Any(o >= o.Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
                Assert.False(_step.Outputs.Any(o >= o.Value Is ClassDeclarationSyntax { Identifier.ValueText:  "D" }))
        End Sub)
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration3()
            Dim source = "
[Y]
class C { }
[X]
class D { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step)
                    Assert.False(_step.Outputs.Any(o >= o.Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
                Assert.True(_step.Outputs.Any(o >= o.Value Is ClassDeclarationSyntax { Identifier.ValueText:  "D" }))
        End Sub)
        End Sub

        <Fact>
        Public Sub FindAttributeOnNestedClasses_WhenSearchingForClassDeclaration1()
            Dim source = "
[X]
class C
{
    [X]
    class D { }
}
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step)
                    Assert.True(_step.Outputs.Any(o >= o.Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
                Assert.True(_step.Outputs.Any(o >= o.Value Is ClassDeclarationSyntax { Identifier.ValueText:  "D" }))
        End Sub)
        End Sub

        <Fact>
        Public Sub FindAttributeOnClassInNamespace_WhenSearchingForClassDeclaration1()
        Dim source = "
namespace N
{
    [X]
    class C { }
}
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Any(o => o.Value Is ClassDeclarationSyntax { Identifier.ValueText: "C" })))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_FullAttributeName1()
        Dim source = "
[X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ShortAttributeName1()
        Dim source = "
[X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("X")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindFullAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_FullAttributeName1()
        Dim source = "
[XAttribute]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias1()
        Dim source = "
imports A = XAttribute

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias2()
        Dim source = "
imports AAttribute = XAttribute

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias3()
        Dim source = "
imports AAttribute = XAttribute

[AAttribute]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias4()
        Dim source = "
imports A = M.XAttribute

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias5()
        Dim source = "
imports A = M.XAttribute<int>

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias6()
        Dim source = "
imports A = global::M.XAttribute<int>

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias1()
            Dim source = "
imports AAttribute : X

[AAttribute]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias2()
            Dim source = "
imports AAttribute : XAttribute

[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases1()
        Dim source = "
imports B = XAttribute
namespace N
{
    imports A = B

    [A]
    class C { }
}
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_OuterAliasReferencesInnerAlias()
        ' note: this is not legal.  it's ok if this ever stops working in the futuer.
        Dim source = "
imports BAttribute = AAttribute
namespace N
{
    imports AAttribute = XAttribute

    [B]
    class C { }
}
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases2()
        Dim source = "
imports B = XAttribute
namespace N
{
    imports AAttribute = B

    [A]
    class C { }
}
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases2()
            Dim source = "
imports BAttribute = XAttribute
namespace N
{
    imports AAttribute = B

    [A]
    class C { }
}
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias1()
            Dim source = "
imports AAttribute = BAttribute
imports BAttribute = AAttribute

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias2()
            Dim source = "
imports A = BAttribute
imports B = AAttribute

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias3()
            Dim source = "
imports A = B
imports B = A

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_LocalAliasInDifferentFile1()
            Dim source1 = "
[A]
class C { }
"
            Dim source2 = "
imports A = XAttribute
"

            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_LocalAliasInDifferentFile2()
            Dim source1 = "
[A]
class C { }
"
            Dim source2 = "
imports AAttribute = XAttribute
"

            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasInSameFile1()
        Dim source = "
global imports A = XAttribute

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasInSameFile2()
        Dim source = "
global imports AAttribute = XAttribute

[A]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasInSameFile1()
        Dim source = "
global imports AAttribute = XAttribute
imports B = AAttribute

[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasInSameFile2()
        Dim source = "
global imports AAttribute = XAttribute
imports BAttribute = AAttribute

[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasDifferentFile1()
        Dim source1 = "
[A]
class C { }
"
            Dim source2 = "
global imports A = XAttribute
"

            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasDifferentFile2()
        Dim source1 = "
[A]
class C { }
"
            Dim source2 = "
global imports AAttribute = XAttribute
"

            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_BothGlobalAndLocalAliasDifferentFile1()
            Dim source1 = "
[B]
class C { }
"
            Dim source2 = "
global imports AAttribute = XAttribute
imports B = AAttribute
"

            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasLoop1()
            Dim source1 = "
[A]
class C { }
"
            Dim source2 = "
global imports AAttribute = BAttribute
global imports BAttribute = AAttribute
"

            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("FindX"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasDifferentFile1()
        Dim source1 = "
imports B = AAttribute
[B]
class C { }
"
            Dim source2 = "
global imports AAttribute = XAttribute
"

            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasDifferentFile2()
        Dim source1 = "
imports BAttribute = AAttribute
[B]
class C { }
"
            Dim source2 = "
global imports AAttribute = XAttribute
"

            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

#end region

#Region "Incremental tests"

        ' These tests validate minimal recomputation performed after changes are made to the compilation.

        <Fact>
        Public Sub RerunOnSameCompilationCachesResultFully()
        Dim source = "
[X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        ' re-run without changes
        driver = driver.RunGenerators(compilation)
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub RerunOnCompilationWithReferencesChangeCachesResultFully()
        Dim source = "
[X]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        ' re-run with just changes to references.  this helper is entirely syntactic, so nothing should change.
        driver = driver.RunGenerators(compilation.RemoveAllReferences())
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestSourceFileRemoved1()
        Dim source1 = "
global imports AAttribute = XAttribute"

            Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
        {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        ' re-run with the file with the class removed.  this will remove the actual output.
        driver = driver.RunGenerators(compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Last()))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Removed, s.Outputs.Single().Reason))

        ' the per-file global aliases get changed (because the last file is removed).
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        ' however, the collected global aliases stays the same.
        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Removed, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Removed, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Removed, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestSourceFileChanged_AttributeRemoved1()
        Dim source1 = "
global imports AAttribute = XAttribute"

            Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
        {
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Last(),
            compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("
class C { }
"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Removed, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestSourceFileChanged_AttributeAdded1()
        Dim source1 = "
global imports AAttribute = XAttribute"

            Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))

            driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.Last(),
                compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("
[B]
class C { }
"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.New, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub TestSourceFileChanged_NonVisibleChangeToGlobalAttributeFile()
        Dim source1 = "
global imports AAttribute = XAttribute"

            Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
global imports AAttribute = XAttribute
class Dummy {}
"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestRemoveGlobalAttributeFile1()
        Dim source1 = "
global imports AAttribute = XAttribute"

            Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        driver = driver.RunGenerators(compilation.RemoveSyntaxTrees(
            compilation.SyntaxTrees.First()))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Removed, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Removed, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestAddGlobalAttributeFile1()
        Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(
                compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
global imports AAttribute = XAttribute"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.New, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.New, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub TestAddGlobalAttributeFile2()
        Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(
                compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
global imports BAttribute = XAttribute"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.New, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.New, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))
End Sub

        <Fact>
        Public Sub TestAddSourceFileWithoutAttribute()
        Dim source1 = "
global imports AAttribute = XAttribute"

            Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
class D { }"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.New, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestAddSourceFileWithAttribute()
        Dim source1 = "
global imports AAttribute = XAttribute"

            Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
            Dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(Of ClassStatementSyntax)("XAttribute")
                                                                                          ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                          End Sub)
                                                                                      End Sub))

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "C" }))

        driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
[A]
class D { }"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.New, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Unchanged, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.New, s.Outputs.Single().Reason))
        Assert.Collection(runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Cached, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.New, s.Outputs.Single().Reason))
        Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Cached, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.New, s.Outputs.Single().Reason))

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:    "C" }),
            Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText: "D" }))
End Sub

        <Fact>
        Public Sub TestReplaceSourceFileWithDifferentAttribute()
        Dim source1 = "
global imports AAttribute = XAttribute"

            Dim source2 = "
global imports BAttribute = AAttribute"

            Dim source3 = "
[B]
class C { }
"
            Dim parseOptions = TestOptions.RegularPreview
            Dim compilation = CreateCompilation({ source1, source2, source3 }, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

        Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(sub(ctx)
            dim input = ctx.SyntaxProvider.CreateSyntaxProviderForAttribute(of classstatementsyntax)("XAttribute")
            ctx.RegisterSourceOutput(input, sub(spc, node)
                                            End Sub)
        End Sub))

        Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=ParseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
        driver = driver.RunGenerators(compilation)
        Dim runResult = driver.GetRunResult().Results(0)
        Console.WriteLine(runResult)

        Assert.Collection(runResult.trackedsteps("result_ForAttribute"),
            Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText: "C" }))

        driver = driver.RunGenerators(Compilation.ReplaceSyntaxTree(
            Compilation.SyntaxTrees.Last(),
            Compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("
[A]
class D { }"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.trackedsteps("individualFileGlobalAliases_ForAttribute"),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason),
            sub(_step) Assert.Equal(IncrementalstepRunReason.Unchanged, s.Outputs.Single().Reason))
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.trackedsteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Cached, runResult.trackedsteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

        Assert.Equal(IncrementalstepRunReason.Modified, runResult.trackedsteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.trackedsteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
        Assert.Equal(IncrementalstepRunReason.Modified, runResult.trackedsteps("result_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.trackedsteps("result_ForAttribute"),
                Sub(_step) Assert.True(_step.Outputs.Single().Value Is ClassDeclarationSyntax { Identifier.ValueText:  "D" }))
End Sub

#End Region
    End Class
End Namespace
