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
    Public Class GeneratorDriverTests_Attributes_FullyQualifiedName
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

        <Fact>
        Public Sub FindCorrectAttributeOnTopLevelClass_WhenSearchingForClassDeclaration1()
            Dim source = "
<N1.X>
class C1
end class
<N2.X>
class C2
end class

namespace N1
    class XAttribute
        inherits System.Attribute
    end class
end namespace

namespace N2
    class XAttribute
        inherits System.Attribute
    end class
end namespace
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("N1.XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C1")))
        End Sub

        <Fact>
        Public Sub FindCorrectAttributeOnTopLevelClass_WhenSearchingForClassDeclaration2()
            Dim source = "
<N1.X>
class C1
end class
<N2.X>
class C2
end class

namespace N1
    class XAttribute
        inherits System.Attribute
    end class
end namespace

namespace N2
    class XAttribute
        inherits System.Attribute
    end class
end namespace
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("N2.XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C2")))
        End Sub

        <Fact>
        Public Sub FindNestedAttribute1()
            Dim source = "
<Outer1.Inner>
class C1
end class
<Outer2.Inner>
class C2
end class

class Outer1
    public class InnerAttribute
        inherits System.Attribute
    end class
end class

class Outer2
    public class InnerAttribute
        inherits System.Attribute
    end class
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("Outer1+InnerAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C1")))
        End Sub

        <Fact>
        Public Sub FindNestedAttribute2()
            Dim source = "
<Outer1.Inner>
class C1
end class
<Outer2.Inner>
class C2
end class

class Outer1
    public class InnerAttribute
        inherits System.Attribute
    end class
end class

class Outer2
    public class InnerAttribute
        inherits System.Attribute
    end class
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("Outer2+InnerAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
            Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C2")))
        End Sub

        <Fact>
        Public Sub FindNestedGenericAttribute1()
            Dim source = "
<Outer1.Inner(of integer)>
class C1
end class
<Outer2.Inner(of integer, string)>
class C2
end class

class Outer1
    public class InnerAttribute(of T1)
        inherits System.Attribute
    end class
end class
class Outer2
    public class InnerAttribute(of T1, T2)
        inherits System.Attribute
    end class
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("Outer1+InnerAttribute`1")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))
        End Sub

        <Fact>
        Public Sub FindNestedGenericAttribute2()
            Dim source = "
<Outer1.Inner(of integer)>
class C1
end class
<Outer2.Inner(of integer, string)>
class C2
end class

class Outer1
    public class InnerAttribute(of T1)
        inherits System.Attribute
    end class
end class
class Outer2
    public class InnerAttribute(of T1, T2)
        inherits System.Attribute
    end class
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("Outer2+InnerAttribute`2")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))
        End Sub

        <Fact>
        Public Sub DoNotFindNestedGenericAttribute1()
            Dim source = "
<Outer1.Inner(of integer)>
class C1
end class
<Outer2.Inner(of integer, string)>
class C2
end class

class Outer1
    public class InnerAttribute(of T1)
        inherits System.Attribute
    end class
end class
class Outer2
    public class InnerAttribute(of T1, T2)
        inherits System.Attribute
    end class
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("Outer1+InnerAttribute`2")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))
        End Sub

        <Fact>
        Public Sub DoNotFindNestedGenericAttribute2()
            Dim source = "
<Outer1.Inner(of integer)>
class C1
end class
<Outer2.Inner(of integer, string)>
class C2
end class

class Outer1
    public class InnerAttribute(of T1)
        inherits System.Attribute
    end class
end class

class Outer2
    public class InnerAttribute(of T1, T2)
        inherits System.Attribute
    end class
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("Outer2+InnerAttribute`1")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))
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

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            ' re-run without changes
            driver = driver.RunGenerators(compilation)
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
            Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("collectedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("groupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub RerunWithReferencesChange()
            Dim source = "
<X>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
            Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            ' re-run without changes
            driver = driver.RunGenerators(compilation.WithReferences(compilation.References.Take(compilation.References.Count() - 1)))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
            Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("collectedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("groupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("result_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub RerunWithAddedFile1()
            Dim source = "
<X>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
            Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From(""))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
            Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            Sub(s) Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason))
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("groupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("result_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub RerunWithAddedFile2()
            Dim source = "
<X>
class C
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
class XAttribute
    inherits System.Attribute
end class
"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
            Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            Sub(s) Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason))
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("groupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("result_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub RerunWithAddedFile_MultipleResults_SameFile1()
            Dim source = "
<X>
class C1
end class
<X>
class C2
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
class XAttribute
    inherits System.Attribute
end class"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
Sub(_step) Assert.Collection(_step.Outputs,
Sub(t) Assert.True(IsClassStatementWithName(t.Value, "C1")),
Sub(t) Assert.True(IsClassStatementWithName(t.Value, "C2"))))

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
                Sub(s) Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason))
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("result_ForAttribute").Single().Outputs,
            Sub(t) Assert.Equal(IncrementalStepRunReason.Cached, t.Reason),
            Sub(t) Assert.Equal(IncrementalStepRunReason.Cached, t.Reason))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("groupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName").Single().Outputs,
            Sub(t) Assert.Equal(IncrementalStepRunReason.New, t.Reason),
            Sub(t) Assert.Equal(IncrementalStepRunReason.New, t.Reason))
        End Sub

        <Fact>
        Public Sub RerunWithAddedFile_MultipleResults_MultipleFile1()
            Dim source1 = "
<X>
class C1
end class
"
            Dim source2 = "
<X>
class C2
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)("XAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)
            Console.WriteLine(runResult)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
class XAttribute
    inherits System.Attribute
end class"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
Sub(_step) Assert.Collection(_step.Outputs, Sub(t) Assert.True(IsClassStatementWithName(t.Value, "C1"))),
Sub(_step) Assert.Collection(_step.Outputs, Sub(t) Assert.True(IsClassStatementWithName(t.Value, "C2"))))

            Assert.Collection(runResult.TrackedSteps("individualFileGlobalAliases_ForAttribute"),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
                Sub(s) Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason))
            Assert.Equal(IncrementalStepRunReason.Modified, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute"),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Outputs.Single().Reason))
            Assert.Collection(runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute"),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason))
            Assert.Collection(runResult.TrackedSteps("result_ForAttribute"),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("groupedNodes_ForAttributeWithMetadataName"),
            Sub(s) Assert.Collection(s.Outputs,
                Sub(t) Assert.Equal(IncrementalStepRunReason.Cached, t.Reason),
                Sub(t) Assert.Equal(IncrementalStepRunReason.Cached, t.Reason)))
            Assert.Collection(runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName"),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason),
            Sub(s) Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason))
            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
            Sub(s) Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason),
            Sub(s) Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason))
        End Sub

#End Region
    End Class
End Namespace
