' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities.TestGenerators

Namespace Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests.SourceGeneration
    Public Class GeneratorDriverTests_Attributes_SimpleName
        Inherits BasicTestBase

        Private Function IsClassStatementWithName(value As Object, name As String) As Boolean
            If TypeOf value IsNot ClassStatementSyntax Then
                Return False
            End If

            Return DirectCast(value, ClassStatementSyntax).Identifier.ValueText = name
        End Function

#Region "Non-Incremental tests"

        ' These tests just validate basic correctness of results in different scenarios, without actually validating
        ' that the incremental nature of this provider works properly.

        <Theory>
        <InlineData("<X>")>
        <InlineData("<X, Y>")>
        <InlineData("<Y, X>")>
        <InlineData("<X, X>")>
        <InlineData("<X><Y>")>
        <InlineData("<Y><X>")>
        <InlineData("<X><X>")>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration1(attribute As String)
            Dim source = $"
{attribute}
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(
                                                        Sub(ctx)
                                                            Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                            ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                            End Sub)
                                                        End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Theory>
        <InlineData("XAttribute")>
        <InlineData("A.X")>
        <InlineData("A.XAttribute")>
        <InlineData("A.X(Of Y)")>
        <InlineData("global.X")>
        <InlineData("global.A.X")>
        Public Sub FindFullAttributeNameOnTopLevelClass(attribute As String)
            Dim source = $"
<{attribute}>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForDelegateDeclaration1()
            Dim source = "
<X>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of DelegateStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForDifferentName()
            Dim source = "
<X>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of DelegateStatementSyntax)("YAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForSyntaxNode1()
            Dim source = "
<X>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of SyntaxNode)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration1()
            Dim source = "
<X>
class C
end class
<X>
class D
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step)
                    Assert.True(IsClassStatementWithName(_step.Outputs.First().Value, "C"))
                    Assert.True(IsClassStatementWithName(_step.Outputs.Last().Value, "D"))
                End Sub)
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration2()
            Dim source = "
<X>
class C
end class
<Y>
class D
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step)
                    Assert.True(IsClassStatementWithName(_step.Outputs.First().Value, "C"))
                    Assert.False(_step.Outputs.Any(Function(o) IsClassStatementWithName(o.Value, "D")))
                End Sub)
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClasses_WhenSearchingForClassDeclaration3()
            Dim source = "
<Y>
class C
end class
<X>
class D
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step)
                    Assert.False(_step.Outputs.Any(Function(o) IsClassStatementWithName(o.Value, "C")))
                    Assert.True(IsClassStatementWithName(_step.Outputs.Last().Value, "D"))
                End Sub)
        End Sub

        <Fact>
        Public Sub FindAttributeOnNestedClasses_WhenSearchingForClassDeclaration1()
            Dim source = "
<X>
class C
    <X>
    class D
    end class
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step)
                    Assert.True(IsClassStatementWithName(_step.Outputs.First().Value, "C"))
                    Assert.True(IsClassStatementWithName(_step.Outputs.Last().Value, "D"))
                End Sub)
        End Sub

        <Fact>
        Public Sub FindAttributeOnClassInNamespace_WhenSearchingForClassDeclaration1()
            Dim source = "
namespace N
    <X>
    class C
    end class
end namespace
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.First().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_FullAttributeName1()
            Dim source = "
<X>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ShortAttributeName1()
            Dim source = "
<X>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("X")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindFullAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_FullAttributeName1()
            Dim source = "
<XAttribute>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Theory>
        <InlineData("A = XAttribute")>
        <InlineData("AAttribute = XAttribute")>
        <InlineData("A = M.XAttribute")>
        <InlineData("A = M.XAttribute(of integer)")>
        <InlineData("A = global.M.XAttribute(of integer)")>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias1(text As String)
            Dim source = $"
imports {text}

<A>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias3()
            Dim source = "
imports AAttribute = XAttribute

<AAttribute>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias1()
            Dim source = "
imports AAttribute : X

<AAtribute>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_WithLocalAlias2()
            Dim source = "
imports AAttribute : XAttribute

<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases1()
            Dim source = "
imports B = XAttribute
namespace N
    imports A = B

    <A>
    class C
    end class
end namespace
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_OuterAliasReferencesInnerAlias()
            ' note: this is not legal.
            Dim source = "
imports BAttribute = AAttribute
namespace N
    imports AAttribute = XAttribute

    <B>
    class C
    end class
end namespace
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases2()
            Dim source = "
imports B = XAttribute
namespace N
    imports AAttribute = B

    <A>
    class C
    end class
end namespace
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_ThroughMultipleAliases2()
            Dim source = "
imports BAttribute = XAttribute
namespace N
    imports AAttribute = B

    <A>
    class C
    end class
end namespace
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias1()
            Dim source = "
imports AAttribute = BAttribute
imports BAttribute = AAttribute

<A>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias2()
            Dim source = "
imports A = BAttribute
imports B = AAttribute

<A>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_RecursiveAlias3()
            Dim source = "
imports A = B
imports B = A

<A>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_LocalAliasInDifferentFile1()
            Dim source1 = "
<A>
class C
end class
"
            Dim source2 = "
imports A = XAttribute
"

            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub DoNotFindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_LocalAliasInDifferentFile2()
            Dim source1 = "
<A>
class C
end class
"
            Dim source2 = "
imports AAttribute = XAttribute
"

            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasInSameFile1()
            Dim source = "
<A>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source,
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("A = XAttribute")),
                parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasInSameFile2()
            Dim source = "
<A>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source,
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("AAttribute = XAttribute")), parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasInSameFile1()
            Dim source = "
imports B = AAttribute

<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source,
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("AAttribute = XAttribute")), parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasInSameFile2()
            Dim source = "
imports BAttribute = AAttribute

<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source,
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("AAttribute = XAttribute")), parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasDifferentFile1()
            Dim source1 = "
<A>
class C
end class
"

            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source1},
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("A = XAttribute")),
                parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasDifferentFile2()
            Dim source1 = "
<A>
class C
end class
"

            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source1},
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("AAttribute = XAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_BothGlobalAndLocalAliasDifferentFile1()
            Dim source1 = "
<B>
class C
end class
"
            Dim source2 = "
global imports AAttribute = XAttribute
imports B = AAttribute
"

            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAliasLoop1()
            Dim source1 = "
<A>
class C
end class
"
            Dim source2 = "
global imports AAttribute = BAttribute
global imports BAttribute = AAttribute
"

            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasDifferentFile1()
            Dim source1 = "
imports B = AAttribute
<B>
class C
end class
"

            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source1},
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("AAttribute = XAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_GlobalAndLocalAliasDifferentFile2()
            Dim source1 = "
imports BAttribute = AAttribute
<B>
class C
end class
"

            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source1},
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("AAttribute = XAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

#End Region

#Region "Incremental tests"

        ' These tests validate minimal recomputation performed after changes are made to the compilation.

        <Fact>
        Public Sub RerunOnSameCompilationCachesResultFully()
            Dim source = "
<X>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            ' re-run without changes
            driver = driver.RunGenerators(compilation)
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub RerunOnCompilationWithReferencesChangeCachesResultFully()
            Dim source = "
<X>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            ' re-run with just changes to references.  this helper is entirely syntactic, so nothing should change.
            driver = driver.RunGenerators(compilation.RemoveAllReferences())
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestSourceFileRemoved1()
            Dim source3 = "
<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3},
                options:=TestOptions.DebugDll.WithGlobalImports(
                    GlobalImport.Parse("AAttribute = XAttribute"),
                    GlobalImport.Parse("BAttribute = AAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            ' re-run with the file with the class removed.  this will remove the actual output.
            driver = driver.RunGenerators(compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Last()))
            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))

            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestSourceFileChanged_AttributeRemoved1()
            Dim source3 = "
<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3},
                options:=TestOptions.DebugDll.WithGlobalImports(
                    GlobalImport.Parse("AAttribute = XAttribute"),
                    GlobalImport.Parse("BAttribute = AAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Last(),
            compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("
class C
end class
"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestSourceFileChanged_AttributeAdded1()
            Dim source3 = "
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3},
                options:=TestOptions.DebugDll.WithGlobalImports(
                    GlobalImport.Parse("AAttribute = XAttribute"),
                    GlobalImport.Parse("BAttribute = AAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))

            driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
                compilation.SyntaxTrees.Last(),
                compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("
<B>
class C
end class
"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub TestRemoveGlobalAttributeFile1()
            Dim source3 = "
<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3},
                options:=TestOptions.DebugDll.WithGlobalImports(
                    GlobalImport.Parse("AAttribute = XAttribute"),
                    GlobalImport.Parse("BAttribute = AAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            driver = driver.RunGenerators(compilation.WithOptions(compilation.Options.WithGlobalImports(
                GlobalImport.Parse("BAttribute = AAttribute"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Removed, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestAddGlobalAttributeFile1()
            Dim source3 = "
<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3},
                options:=TestOptions.DebugDll.WithGlobalImports(GlobalImport.Parse("BAttribute = AAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))

            driver = driver.RunGenerators(
                compilation.WithOptions(compilation.Options.WithGlobalImports(
                                        compilation.Options.GlobalImports.Add(GlobalImport.Parse("AAttribute = XAttribute")))))

            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub TestAddGlobalAttributeFile2()
            Dim source3 = "
<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttribute"))

            driver = driver.RunGenerators(compilation.WithOptions(compilation.Options.WithGlobalImports(
                compilation.Options.GlobalImports.Add(GlobalImport.Parse("BAttribute = XAttribute")))))
            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub TestAddSourceFileWithoutAttribute()
            Dim source3 = "
<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3},
                options:=TestOptions.DebugDll.WithGlobalImports(
                    GlobalImport.Parse("AAttribute = XAttribute"),
                    GlobalImport.Parse("BAttribute = AAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
class D
end class"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs,
                Sub(o) Assert.Equal(IncrementalStepRunReason.Cached, o.Reason))
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub TestAddSourceFileWithAttribute()
            Dim source3 = "
<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3},
                options:=TestOptions.DebugDll.WithGlobalImports(
                    GlobalImport.Parse("AAttribute = XAttribute"),
                    GlobalImport.Parse("BAttribute = AAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(
                compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
<A>
class D
end class"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute"),
                Sub(_step) Assert.Equal(IncrementalStepRunReason.Cached, _step.Outputs.Single().Reason),
                Sub(_step) Assert.Equal(IncrementalStepRunReason.New, _step.Outputs.Single().Reason))
            Assert.Collection(runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute"),
                Sub(_step) Assert.Equal(IncrementalStepRunReason.Cached, _step.Outputs.Single().Reason),
                Sub(_step) Assert.Equal(IncrementalStepRunReason.New, _step.Outputs.Single().Reason))
            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.Equal(IncrementalStepRunReason.Cached, _step.Outputs.Single().Reason),
                Sub(_step) Assert.Equal(IncrementalStepRunReason.New, _step.Outputs.Single().Reason))

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "D")))
        End Sub

        <Fact>
        Public Sub TestReplaceSourceFileWithDifferentAttribute()
            Dim source3 = "
<B>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation({source3},
                options:=TestOptions.DebugDll.WithGlobalImports(
                    GlobalImport.Parse("AAttribute = XAttribute"),
                    GlobalImport.Parse("BAttribute = AAttribute")), parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithSimpleName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Last(),
            compilation.SyntaxTrees.Last().WithChangedText(SourceText.From("
<A>
class D
end class"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)

            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "D")))
        End Sub

#End Region

    End Class
End Namespace
