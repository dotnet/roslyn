' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class EditAndContinueClosureTests
        Inherits EditAndContinueTestBase

        <Fact>
        Public Sub MethodToMethodWithClosure()
            Dim source0 =
<compilation>
    <file name="a.vb">
Delegate Function D() As Object
Class C
    Function F(o As Object)
        Return o
    End Function
End Class
    </file>
</compilation>

            Dim source1 =
<compilation>
    <file name="a.vb">
Delegate Function D() As Object
Class C
    Function F(o As Object)
        Return (DirectCast(Function() o, D))
    End Function
End Class
    </file>
</compilation>

            Dim compilation0 = CreateCompilationWithReferences(source0, references:=LatestReferences, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)
            Dim bytes0 = compilation0.EmitToArray()
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, compilation1.GetMember(Of MethodSymbol)("C.F"), compilation1.GetMember(Of MethodSymbol)("C.F"))))

            Using md1 = diff1.GetMetadata()
                Dim reader1 = md1.Reader

                ' Field: $VB$Local_o
                ' Methods: .ctor, _Lambda$__1
                ' Type: _Closure$__1-0
                CheckEncLogDefinitions(reader1,
                    Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                    Row(1, TableIndex.Field, EditAndContinueOperation.Default),
                    Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                    Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                    Row(4, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                    Row(1, TableIndex.NestedClass, EditAndContinueOperation.Default))
            End Using
        End Sub

        <Fact>
        Public Sub MethodWithStaticLambda1()
            Dim source0 = MarkedSource("
Imports System
Class C
    Sub F()
        Dim x As Func(Of Integer) = <N:0>Function() 1</N:0>
    End Sub
End Class
")

            Dim source1 = MarkedSource("
Imports System
Class C
    Sub F()
        Dim x As Func(Of Integer) = <N:0>Function() 2</N:0>
    End Sub
End Class
")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreatePdbInfoProvider().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates
            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        <Fact>
        Public Sub MethodWithStaticLambdaGeneric1()
            Dim source0 = MarkedSource("
Imports System
Class C
    Sub F(Of T)()
        Dim x As Func(Of T) = <N:0>Function() Nothing</N:0>
    End Sub
End Class
")

            Dim source1 = MarkedSource("
Imports System
Class C
    Sub F(Of T)()
        Dim x As Func(Of T) = <N:0>Function() Nothing</N:0>
    End Sub
End Class
")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreatePdbInfoProvider().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__1}",
                "C._Closure$__1(Of $CLS0): {$I1-0, _Lambda$__1-0}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates
            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        <Fact>
        Public Sub MethodWithThisOnlyClosure1()
            Dim source0 = MarkedSource("
Imports System
Class C
    Function F(a As Integer)
        Dim x As Func(Of Integer) = <N:0>Function() F(1)</N:0>
        Return 1
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System
Class C
    Function F(a As Integer)
        Dim x As Func(Of Integer) = <N:0>Function() F(2)</N:0>
        Return 2
    End Function
End Class
")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreatePdbInfoProvider().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {_Lambda$__1-0}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates
            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
        End Sub

        <Fact>
        Public Sub MethodWithClosure1()
            Dim source0 = MarkedSource("
Imports System
Class C
    <N:0>Function F(a As Integer) As Integer
        Dim x As Func(Of Integer) = <N:1>Function() F(a + 1)</N:1>
        Return 1
    End Function</N:0>
End Class
")
            Dim source1 = MarkedSource("
Imports System
Class C
    <N:0>Function F(a As Integer) As Integer
        Dim x As Func(Of Integer) = <N:1>Function() F(a + 2)</N:1>
        Return 2
    End Function</N:0>
End Class
")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreatePdbInfoProvider().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__1-0}",
                "C._Closure$__1-0: {$VB$Me, _Lambda$__0}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates
            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        <Fact>
        Public Sub ConstructorWithClosure1()
            Dim source0 = MarkedSource("
Imports System
Class D
    Public Sub New(f As Func(Of Integer))
    End Sub
End Class
Class C
    Inherits D
    <N:0>Public Sub New(a As Integer, b As Integer)
        MyBase.New(<N:1>Function() a</N:1>)

        Dim c As Integer = 0

        Dim f1 As Func(Of Integer) = <N:2>Function() a + 1</N:2>
        Dim f2 As Func(Of Integer) = <N:3>Function() b + 2</N:3>
        Dim f3 As Func(Of Integer) = <N:4>Function() c + 3</N:4>
        Dim f4 As Func(Of Integer) = <N:5>Function() a + b + c</N:5>
        Dim f5 As Func(Of Integer) = <N:6>Function() a + c</N:6>
        Dim f6 As Func(Of Integer) = <N:7>Function() b + c</N:7>
    End Sub</N:0>
End Class
")
            Dim source1 = MarkedSource("
Imports System
Class D
    Public Sub New(f As Func(Of Integer))
    End Sub
End Class
Class C
    Inherits D
    <N:0>Public Sub New(a As Integer, b As Integer)
        MyBase.New(<N:1>Function() a * 10</N:1>)

        Dim c As Integer = 0

        Dim f1 As Func(Of Integer) = <N:2>Function() a * 10 + 1</N:2>
        Dim f2 As Func(Of Integer) = <N:3>Function() b * 10 + 2</N:3>
        Dim f3 As Func(Of Integer) = <N:4>Function() c * 10  + 3</N:4>
        Dim f4 As Func(Of Integer) = <N:5>Function() a * 10 + b + c</N:5>
        Dim f5 As Func(Of Integer) = <N:6>Function() a * 10 + c</N:6>
        Dim f6 As Func(Of Integer) = <N:7>Function() b * 10 + c</N:7>
    End Sub</N:0>
End Class
")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim ctor0 = compilation0.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()
            Dim ctor1 = compilation1.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreatePdbInfoProvider().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__0-0}",
                "C._Closure$__0-0: {_Lambda$__0, _Lambda$__1, _Lambda$__2, _Lambda$__3, _Lambda$__4, _Lambda$__5, _Lambda$__6}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates
            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(7, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        <Fact>
        Public Sub MethodWithAsyncLambda()
            Dim source0 = MarkedSource("
Imports System
Imports System.Threading.Tasks

Module C

    Sub M()
        Dim task = Async Sub()
                       <N:0>Await F(42)</N:0>
                   End Sub
        task()

        Console.ReadLine()
    End Sub

    Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.FromResult(x)
    End Function

End Module
")
            Dim source1 = MarkedSource("
Imports System
Imports System.Threading.Tasks

Module C

    Sub M()
        Dim task = Async Sub()
                       <N:0>Await F(42*42)</N:0>
                   End Sub
        task()

        Console.ReadLine()
    End Sub

    Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.FromResult(x)
    End Function

End Module
")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreatePdbInfoProvider().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {VB$StateMachine_1_F}",
                "C.VB$StateMachine_1_F: {$State, $Builder, $VB$Local_x, $A0, MoveNext, System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates
            CheckEncLogDefinitions(reader1,
                Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(6, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(16, TableIndex.CustomAttribute, EditAndContinueOperation.Default),
                Row(17, TableIndex.CustomAttribute, EditAndContinueOperation.Default))
        End Sub

    End Class
End Namespace
