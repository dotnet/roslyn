' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities.TestGenerators

Namespace Microsoft.CodeAnalysis.VisualBasic.Semantic.UnitTests.SourceGeneration
    Friend Module IncrementalGeneratorInitializationContextExtensions
        <Extension>
        Public Function ForAttributeWithSimpleName(Of T As SyntaxNode)(
        context As IncrementalGeneratorInitializationContext, simpleName As String) As IncrementalValuesProvider(Of T)

            Return context.SyntaxProvider.ForAttributeWithSimpleName(
            simpleName,
            Function(node, c) TypeOf node Is T).SelectMany(Function(tuple, c) tuple.matches.Cast(Of T)).WithTrackingName("result_ForAttribute")
        End Function

        <Extension>
        Public Function ForAttributeWithMetadataName(Of T As SyntaxNode)(
           context As IncrementalGeneratorInitializationContext, fullyQualifiedMetadataName As String) As IncrementalValuesProvider(Of T)

            Return context.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName,
                Function(node, c) TypeOf node Is T,
                Function(ctx, c) DirectCast(ctx.TargetNode, T))
        End Function
    End Module

    Public Class GeneratorDriverTests_Attributes_FullyQualifiedName
        Inherits BasicTestBase

        Private Shared Function IsClassStatementWithName(value As Object, name As String) As Boolean
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

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C2")))
        End Sub

        <Theory>
        <InlineData("X")>
        <InlineData("XAttribute")>
        Public Sub DoNotAttributeOnTopLevelClass_WhenSearchingForSimpleName1(name As String)
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
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ClassStatementSyntax)(name)
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))
        End Sub

        <Theory>
        <InlineData("CLSCompliant(true)")>
        <InlineData("CLSCompliantAttribute(true)")>
        <InlineData("System.CLSCompliant(true)")>
        <InlineData("System.CLSCompliantAttribute(true)")>
        Public Sub FindAssemblyAttribute1(attribute As String)
            Dim source = $"
Imports System
<Assembly: {attribute}>
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of CompilationUnitSyntax)("System.CLSCompliantAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(DirectCast(_step.Outputs.Single().Value, CompilationUnitSyntax).SyntaxTree Is compilation.SyntaxTrees.First))
        End Sub

        <Theory>
        <InlineData("CLSCompliant(true)")>
        <InlineData("CLSCompliantAttribute(true)")>
        <InlineData("System.CLSCompliant(true)")>
        <InlineData("System.CLSCompliantAttribute(true)")>
        Public Sub FindModuleAttribute1(attribute As String)
            Dim source = $"
Imports System
<Module: {attribute}>
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of CompilationUnitSyntax)("System.CLSCompliantAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(DirectCast(_step.Outputs.Single().Value, CompilationUnitSyntax).SyntaxTree Is compilation.SyntaxTrees.First))
        End Sub

        <Fact>
        Public Sub FindAssemblyAttribute4()
            Dim source1 = "
Imports System
<Assembly: CLSCompliant(true)>
"
            Dim source2 = "
Imports System
<Assembly: CLSCompliant(false)>"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation({source1, source2}, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of CompilationUnitSyntax)("System.CLSCompliantAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(DirectCast(_step.Outputs.Single().Value, CompilationUnitSyntax).SyntaxTree Is compilation.SyntaxTrees.First),
                Sub(_step) Assert.True(DirectCast(_step.Outputs.Single().Value, CompilationUnitSyntax).SyntaxTree Is compilation.SyntaxTrees.Last))
        End Sub

        <Fact>
        Public Sub FindMethodStatementAttribute1()
            Dim source = "
Imports System

Class C
    <CLSCompliant(true)>
    Public Sub M()
    End Sub
End Class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of MethodStatementSyntax)("System.CLSCompliantAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(DirectCast(_step.Outputs.Single().Value, MethodStatementSyntax).Identifier.ValueText = "M"))
        End Sub

        <Fact>
        Public Sub FindMethodStatementAttribute2()
            Dim source = "
Imports System

MustInherit Class C
    <CLSCompliant(true)>
    Public MustOverride Sub M()
End Class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of MethodStatementSyntax)("System.CLSCompliantAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(DirectCast(_step.Outputs.Single().Value, MethodStatementSyntax).Identifier.ValueText = "M"))
        End Sub

        <Fact>
        Public Sub FindFieldAttribute1()
            Dim source = "
Imports System

Class C
    <CLSCompliant(true)>
    dim a as integer
End Class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ModifiedIdentifierSyntax)("System.CLSCompliantAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(DirectCast(_step.Outputs.Single().Value, ModifiedIdentifierSyntax).Identifier.ValueText = "a"))
        End Sub

        <Fact>
        Public Sub FindFieldAttribute3()
            Dim source = "
Imports System

Class C
    <CLSCompliant(true)>
    dim a, b as integer
End Class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ModifiedIdentifierSyntax)("System.CLSCompliantAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.Collection(_step.Outputs,
                    Sub(v) Assert.True(DirectCast(v.Value, ModifiedIdentifierSyntax).Identifier.ValueText = "a"),
                    Sub(v) Assert.True(DirectCast(v.Value, ModifiedIdentifierSyntax).Identifier.ValueText = "b")))
        End Sub

        <Fact>
        Public Sub FindFieldAttribute2()
            Dim source = "
Imports System

Class C
    <CLSCompliant(true)>
    dim a as string, b as integer
End Class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(Sub(ctx)
                                                                                              Dim input = ctx.ForAttributeWithMetadataName(Of ModifiedIdentifierSyntax)("System.CLSCompliantAttribute")
                                                                                              ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                                                                                              End Sub)
                                                                                          End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.Collection(_step.Outputs,
                    Sub(v) Assert.True(DirectCast(v.Value, ModifiedIdentifierSyntax).Identifier.ValueText = "a"),
                    Sub(v) Assert.True(DirectCast(v.Value, ModifiedIdentifierSyntax).Identifier.ValueText = "b")))
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

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList1()
            Dim source = "
<X><X>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(
                Sub(ctx)
                    Dim input = ctx.SyntaxProvider.ForAttributeWithMetadataName(Of ClassStatementSyntax)(
                        "XAttribute",
                        Function(a, b) True,
                        Function(ctx1, c)
                            Assert.Equal(2, ctx1.Attributes.Length)
                            Return DirectCast(ctx1.TargetNode, ClassStatementSyntax)
                        End Function)
                    ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                    End Sub)
                End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList1B()
            Dim source = "
<X, X>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(
                Sub(ctx)
                    Dim input = ctx.SyntaxProvider.ForAttributeWithMetadataName(Of ClassStatementSyntax)(
                        "XAttribute",
                        Function(a, b) True,
                        Function(ctx1, c)
                            Assert.Equal(2, ctx1.Attributes.Length)
                            Return DirectCast(ctx1.TargetNode, ClassStatementSyntax)
                        End Function)
                    ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                    End Sub)
                End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList2()
            Dim source = "
<X><Y>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(
                Sub(ctx)
                    Dim input = ctx.SyntaxProvider.ForAttributeWithMetadataName(Of ClassStatementSyntax)(
                        "XAttribute",
                        Function(a, b) True,
                        Function(ctx1, c)
                            Assert.Equal(1, ctx1.Attributes.Length)
                            Return DirectCast(ctx1.TargetNode, ClassStatementSyntax)
                        End Function)
                    ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                    End Sub)
                End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList2B()
            Dim source = "
<X, Y>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(
                Sub(ctx)
                    Dim input = ctx.SyntaxProvider.ForAttributeWithMetadataName(Of ClassStatementSyntax)(
                        "XAttribute",
                        Function(a, b) True,
                        Function(ctx1, c)
                            Assert.Equal(1, ctx1.Attributes.Length)
                            Return DirectCast(ctx1.TargetNode, ClassStatementSyntax)
                        End Function)
                    ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                    End Sub)
                End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList3()
            Dim source = "
<Y><X>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(
                Sub(ctx)
                    Dim input = ctx.SyntaxProvider.ForAttributeWithMetadataName(Of ClassStatementSyntax)(
                        "XAttribute",
                        Function(a, b) True,
                        Function(ctx1, c)
                            Assert.Equal(1, ctx1.Attributes.Length)
                            Return DirectCast(ctx1.TargetNode, ClassStatementSyntax)
                        End Function)
                    ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                    End Sub)
                End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))
        End Sub

        <Fact>
        Public Sub FindAttributeOnTopLevelClass_WhenSearchingForClassDeclaration_MultipleAttributesInList3B()
            Dim source = "
<Y, X>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"
            Dim parseOptions = TestOptions.RegularLatest
            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)

            Assert.Single(compilation.SyntaxTrees)

            Dim generator = New IncrementalGeneratorWrapper(New PipelineCallbackGenerator(
                Sub(ctx)
                    Dim input = ctx.SyntaxProvider.ForAttributeWithMetadataName(Of ClassStatementSyntax)(
                        "XAttribute",
                        Function(a, b) True,
                        Function(ctx1, c)
                            Assert.Equal(1, ctx1.Attributes.Length)
                            Return DirectCast(ctx1.TargetNode, ClassStatementSyntax)
                        End Function)
                    ctx.RegisterSourceOutput(input, Sub(spc, node)
                                                    End Sub)
                End Sub))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(generator), parseOptions:=parseOptions, driverOptions:=New GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps:=True))
            driver = driver.RunGenerators(compilation)
            Dim runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
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

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            ' re-run without changes
            driver = driver.RunGenerators(compilation)
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttributeInternal").Single().Outputs.Single().Reason)
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

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            ' re-run without changes
            driver = driver.RunGenerators(compilation.WithReferences(compilation.References.Take(compilation.References.Count() - 1)))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttributeInternal").Single().Outputs.Single().Reason)
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

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From(""))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs,
                Sub(o) Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason))
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttributeInternal").Single().Outputs.Single().Reason)
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

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
class XAttribute
    inherits System.Attribute
end class
"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs,
                Sub(o) Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason))
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("result_ForAttributeInternal").Single().Outputs.Single().Reason)
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

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs,
                Sub(o) Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason))
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("result_ForAttributeInternal").Single().Outputs,
                Sub(t) Assert.Equal(IncrementalStepRunReason.Cached, t.Reason))
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

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))

            driver = driver.RunGenerators(compilation.AddSyntaxTrees(compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
class XAttribute
    inherits System.Attribute
end class"))))
            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
Sub(_step) Assert.Collection(_step.Outputs, Sub(t) Assert.True(IsClassStatementWithName(t.Value, "C1"))),
Sub(_step) Assert.Collection(_step.Outputs, Sub(t) Assert.True(IsClassStatementWithName(t.Value, "C2"))))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs,
                Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Reason),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Unchanged, s.Reason))
            Assert.Collection(runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute"),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason))
            Assert.Collection(runResult.TrackedSteps("result_ForAttributeInternal"),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Cached, s.Outputs.Single().Reason))
            Assert.Collection(runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName"),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason),
                Sub(s) Assert.Equal(IncrementalStepRunReason.Modified, s.Outputs.Single().Reason))
            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(s) Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason),
                Sub(s) Assert.Equal(IncrementalStepRunReason.New, s.Outputs.Single().Reason))
        End Sub

        <Fact>
        Public Sub RerunWithChangedFileThatNowReferencesAttribute1()
            Dim source = "
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

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))

            driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
<X>
class C
end class

class XAttribute
    inherits System.Attribute
end class
"))))

            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("result_ForAttributeInternal").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("result_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
        End Sub

        <Fact>
        Public Sub RerunWithChangedFileThatNowReferencesAttribute2()
            Dim source1 = "
class C
end class
"
            Dim source2 = "
class XAttribute
    inherits System.Attribute
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

            Assert.False(runResult.TrackedSteps.ContainsKey("result_ForAttributeWithMetadataName"))

            driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            compilation.SyntaxTrees.First().WithChangedText(SourceText.From("
<X>
class C
end class
"))))

            runResult = driver.GetRunResult().Results(0)

            Assert.Collection(runResult.TrackedSteps("result_ForAttributeWithMetadataName"),
                Sub(_step) Assert.True(IsClassStatementWithName(_step.Outputs.Single().Value, "C")))

            Assert.False(runResult.TrackedSteps.ContainsKey("individualFileGlobalAliases_ForAttribute"))
            Assert.Equal(IncrementalStepRunReason.Unchanged, runResult.TrackedSteps("collectedGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("compilationGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.Cached, runResult.TrackedSteps("allUpGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Collection(runResult.TrackedSteps("compilationUnit_ForAttribute").Single().Outputs,
                Sub(o) Assert.Equal(IncrementalStepRunReason.New, o.Reason))
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("compilationUnitAndGlobalAliases_ForAttribute").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("result_ForAttributeInternal").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("compilationAndGroupedNodes_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
            Assert.Equal(IncrementalStepRunReason.New, runResult.TrackedSteps("result_ForAttributeWithMetadataName").Single().Outputs.Single().Reason)
        End Sub

#End Region
    End Class
End Namespace
