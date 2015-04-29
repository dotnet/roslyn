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

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

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

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

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

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

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

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

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

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

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

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

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

        <WorkItem(1067140)>
        <Fact>
        Public Sub AnonymousDelegates()
            Dim sources0a = <compilation>
                                <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim s = Sub() Return
        Dim t = Sub(o As C) o.M() 
    End Sub
    Shared Sub N()
        Dim x = New With {.P = 0}
        Dim s = Function(o As Object) o
        Dim t = Sub(o As Object) Return
        Dim u = Sub(c As C) c.GetHashCode() 
    End Sub
End Class
]]></file>
                            </compilation>
            Dim sources1a = <compilation>
                                <file name="a.vb"><![CDATA[
Class C
    Sub M()
        Dim s = Sub() Return
        Dim t = Sub(o As C) o.M() 
    End Sub
    Shared Sub N()
        Dim x = New With {.Q = 1}
        Dim s = Function(c As Object) c
        Dim t = Sub(c as Object) Return
        Dim u = Sub(c As C) c.GetHashCode() 
    End Sub
End Class
]]></file>
                            </compilation>

            Dim source0 = MarkedSource("
Class C
    Sub M()
        Dim s = Sub() Return
        Dim t = Sub(o As C) o.M() 
    End Sub
    Shared Sub N()
        Dim x = <N:0>New With {.P = 0}</N:0>
        Dim s = <N:1>Function(o As Object) o</N:1>
        Dim t = <N:2>Sub(o As Object) Return</N:2>
        Dim u = <N:3>Sub(c As C) c.GetHashCode()</N:3>
    End Sub
End Class
")

            Dim source1 = MarkedSource("
Class C
    Sub M()
        Dim s = Sub() Return
        Dim t = Sub(o As C) o.M() 
    End Sub
    Shared Sub N()
        Dim x = <N:0>New With {.Q = 1}</N:0>
        Dim s = <N:1>Function(c As Object) c</N:1>
        Dim t = <N:2>Sub(c as Object) Return</N:2>
        Dim u = <N:3>Sub(c As C) c.GetHashCode()</N:3>
    End Sub
End Class
")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.N")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.N")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))
            Dim reader0 = md0.MetadataReader
            CheckNamesSorted({reader0}, reader0.GetTypeDefNames(), "_Closure$__", "<Module>", "C", "VB$AnonymousDelegate_0", "VB$AnonymousDelegate_1`1", "VB$AnonymousDelegate_2`2", "VB$AnonymousDelegate_3`1", "VB$AnonymousType_0`1")

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            CheckNamesSorted({reader0, reader1}, reader1.GetTypeDefNames(), "VB$AnonymousDelegate_4`2", "VB$AnonymousType_1`1")
            diff1.VerifyIL("C.N", <![CDATA[
{
  // Code size      124 (0x7c)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                [unchanged] V_2,
                [unchanged] V_3,
                VB$AnonymousType_1(Of Integer) V_4, //x
                VB$AnonymousDelegate_4(Of Object, Object) V_5, //s
                VB$AnonymousDelegate_3(Of Object) V_6, //t
                VB$AnonymousDelegate_3(Of C) V_7) //u
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     "Sub VB$AnonymousType_1(Of Integer)..ctor(Integer)"
  IL_0007:  stloc.s    V_4
  IL_0009:  ldsfld     "C._Closure$__.$I2-0 As <generated method>"
  IL_000e:  brfalse.s  IL_0017
  IL_0010:  ldsfld     "C._Closure$__.$I2-0 As <generated method>"
  IL_0015:  br.s       IL_002d
  IL_0017:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_001c:  ldftn      "Function C._Closure$__._Lambda$__2-0(Object) As Object"
  IL_0022:  newobj     "Sub VB$AnonymousDelegate_4(Of Object, Object)..ctor(Object, System.IntPtr)"
  IL_0027:  dup
  IL_0028:  stsfld     "C._Closure$__.$I2-0 As <generated method>"
  IL_002d:  stloc.s    V_5
  IL_002f:  ldsfld     "C._Closure$__.$I2-1 As <generated method>"
  IL_0034:  brfalse.s  IL_003d
  IL_0036:  ldsfld     "C._Closure$__.$I2-1 As <generated method>"
  IL_003b:  br.s       IL_0053
  IL_003d:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_0042:  ldftn      "Sub C._Closure$__._Lambda$__2-1(Object)"
  IL_0048:  newobj     "Sub VB$AnonymousDelegate_3(Of Object)..ctor(Object, System.IntPtr)"
  IL_004d:  dup
  IL_004e:  stsfld     "C._Closure$__.$I2-1 As <generated method>"
  IL_0053:  stloc.s    V_6
  IL_0055:  ldsfld     "C._Closure$__.$I2-2 As <generated method>"
  IL_005a:  brfalse.s  IL_0063
  IL_005c:  ldsfld     "C._Closure$__.$I2-2 As <generated method>"
  IL_0061:  br.s       IL_0079
  IL_0063:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_0068:  ldftn      "Sub C._Closure$__._Lambda$__2-2(C)"
  IL_006e:  newobj     "Sub VB$AnonymousDelegate_3(Of C)..ctor(Object, System.IntPtr)"
  IL_0073:  dup
  IL_0074:  stsfld     "C._Closure$__.$I2-2 As <generated method>"
  IL_0079:  stloc.s    V_7
  IL_007b:  ret
}
]]>.Value)
        End Sub

        <Fact>
        Public Sub PartialClass()
            Dim source0 = MarkedSource("
Imports System
Class C
    Public m1 As Func(Of Integer) = <N:0>Function() 1</N:0>
End Class

Partial Class C
    Public m2 As Func(Of Integer) = <N:1>Function() 1</N:1>
End Class
")

            Dim source1 = MarkedSource("
Imports System
Class C
    Public m1 As Func(Of Integer) = <N:0>Function() 10</N:0>
End Class

Partial Class C
    Public m2 As Func(Of Integer) = <N:1>Function() 10</N:1>
End Class
")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, references:=LatestReferences, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim ctor0 = compilation0.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()
            Dim ctor1 = compilation1.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, Function(handle) v0.CreateSymReader().GetEncMethodDebugInfo(handle))

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, ctor0, ctor1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names):
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I0-0, $I0-1, _Lambda$__0-0, _Lambda$__0-1}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates
            CheckEncLogDefinitions(reader1,
                Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        <Fact>
        Public Sub JoinAndGroupByClauses()
            ' In the following markup we use <N> tag to denote only those matching syntax nodes that represent emitted lambdas.
            ' The true match produced by the IDE includes more matches that are needed for matching active statements and detection of rude edits, 
            ' but the compiler doesn't need them.

            Dim source0 = MarkedSource("
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In { 1 }
                     <N:7>Join b In { 5 } On <N:0>a + 1 Equals b - 1</N:0></N:7>
                     <N:8>Group <N:2>a = 100</N:2>, b = a + 5 By <N:3>c = a + 4</N:3> Into d = <N:4>Count(Q(1))</N:4></N:8>
                     Select <N:1>z = d + 0</N:1>, y = d + 1
   End Sub
    
    Shared Function Q(a As Integer) As Boolean
        Return True
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In { 1 }
                     <N:7>Join b In { 5 } On <N:0>a + 1 Equals b - 1</N:0></N:7>
                     <N:8>Group <N:2>a = 100</N:2>, b = a + 6 By <N:3>c = a + 4</N:3> Into d = <N:4>Count(Q(1))</N:4></N:8>
                     Select <N:1>z = d + 0</N:1>, y = d + 1
    End Sub
    
    Shared Function Q(a As Integer) As Boolean
        Return True
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, {MscorlibRef, SystemCoreRef}, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, $I1-1, $I1-2, $I1-3, $I1-4, $I1-6, $I1-5, $I1-7, _Lambda$__1-0, _Lambda$__1-1, _Lambda$__1-2, _Lambda$__1-3, _Lambda$__1-4, _Lambda$__1-5, _Lambda$__1-6, _Lambda$__1-7}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates for lambdas
            CheckEncLogDefinitions(reader1,
                Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(23, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(27, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(28, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(29, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(30, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(31, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(32, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(33, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(34, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        <Fact>
        Public Sub SelectClauseWithIdentifierOnly()
            ' In the following markup we use <N> tag to denote only those matching syntax nodes that represent emitted lambdas.
            ' The true match produced by the IDE includes more matches that are needed for matching active statements and detection of rude edits, 
            ' but the compiler doesn't need them.

            Dim source0 = MarkedSource("
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In { 1 }
                     Select <N:0:ExpressionRangeVariable>a</N:0>, b = a + 1
   End Sub
End Class
")
            Dim source1 = MarkedSource("
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In { 1 }
                     Select <N:0:ExpressionRangeVariable>a</N:0>, b = a + 2
    End Sub
End Class
")
            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, {MscorlibRef, SystemCoreRef}, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates for lambdas
            CheckEncLogDefinitions(reader1,
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        ''' <summary>
        ''' We need to handle case when an old node that represents a lambda body with multiple nodes 
        ''' of the same kind is mapped to a new node that belongs to the lambda body but is 
        ''' different from the one that represents the new body.
        ''' 
        ''' This handling is done in <see cref="LambdaUtilities.GetCorrespondingLambdaBody(SyntaxNode, SyntaxNode)"/>
        ''' </summary>
        <Fact>
        Public Sub SelectClauseCrossMatch()
            ' In the following markup we use <N> tag to denote only those matching syntax nodes that represent emitted lambdas.
            ' The true match produced by the IDE includes more matches that are needed for matching active statements and detection of rude edits, 
            ' but the compiler doesn't need them.

            Dim source0 = MarkedSource("
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In { 1 }
                     Select <N:0>a = a + 1</N:0>, <N:1>b = 1000</N:1>
   End Sub
End Class
")
            Dim source1 = MarkedSource("
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In { 1 }
                     Select <N:1>a = 1000</N:1>, <N:0>b = a + 1</N:0>
    End Sub
End Class
")
            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, {MscorlibRef, SystemCoreRef}, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, _Lambda$__1-0}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates for lambdas
            CheckEncLogDefinitions(reader1,
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        ''' <summary>
        ''' We need to handle case when an old node that represents a lambda body with multiple nodes 
        ''' of the same kind is mapped to a new node that belongs to the lambda body but is 
        ''' different from the one that represents the new body.
        ''' 
        ''' This handling is done in <see cref="LambdaUtilities.GetCorrespondingLambdaBody(SyntaxNode, SyntaxNode)"/>
        ''' </summary>
        <Fact>
        Public Sub JoinClauseCrossMatch()
            ' In the following markup we use <N> tag to denote only those matching syntax nodes that represent emitted lambdas.
            ' The true match produced by the IDE includes more matches that are needed for matching active statements and detection of rude edits, 
            ' but the compiler doesn't need them.

            Dim source0 = MarkedSource("
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In { 1 }
                     <N:3>Join b in { 2 } On <N:0>a Equals b</N:0> And <N:1>a + 1 Equals b + 1</N:1></N:3>
                     Select <N:2>z = 1</N:2>
   End Sub
End Class
")
            Dim source1 = MarkedSource("
Imports System.Linq

Class C
    Sub F()
        Dim result = From a In { 1 }
                     <N:3>Join b in { 2 } On <N:1>a Equals b</N:1> And <N:0>a + 1 Equals b + 1</N:0></N:3>
                     Select <N:2>z = 1</N:2>
    End Sub
End Class
")
            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, {MscorlibRef, SystemCoreRef}, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            ' no new synthesized members generated (with #1 in names)
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, $I1-1, $I1-2, _Lambda$__1-0, _Lambda$__1-1, _Lambda$__1-2}")

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' Method updates for lambdas
            CheckEncLogDefinitions(reader1,
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(14, TableIndex.MethodDef, EditAndContinueOperation.Default))
        End Sub

        ' TODO: AggregateClauseCrossMatch
        ' TODO: port C# tests, add more VB specific tests

        <Fact>
        Public Sub LambdasMultipleGenerations1()
            Dim source0 = MarkedSource("
Imports System
Class C
    Shared Function G(f As Func(Of Integer, Integer)) As Integer
    End Function

    Shared Function F() As Object
        Return G(<N:0>Function(a) a + 1</N:0>)
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System
Class C
    Shared Function G(f As Func(Of Integer, Integer)) As Integer
    End Function

    Shared Function F() As Object
        Return G(<N:0>Function(a) a + 2</N:0>) + G(<N:1>Function(b) b + 20</N:1>)
    End Function
End Class
")

            Dim source2 = MarkedSource("
Imports System
Class C
    Shared Function G(f As Func(Of Integer, Integer)) As Integer
    End Function

    Shared Function F() As Object
        Return G(<N:0>Function(a) a + 3</N:0>) + G(<N:1>Function(b) b + 30</N:1>) + G(<N:2>Function(b) b + &H300</N:2>)
    End Function
End Class
")

            Dim source3 = MarkedSource("
Imports System
Class C
    Shared Function G(f As Func(Of Integer, Integer)) As Integer
    End Function

    Shared Function F() As Object
        Return G(<N:0>Function(a) a + 4</N:0>) + G(<N:1>Function(b) b + 40</N:1>) + G(<N:2>Function(b) b + &H400</N:2>)
    End Function
End Class
")

            Dim compilation0 = CreateCompilationWithMscorlib(source0.Tree, ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)
            Dim compilation3 = compilation2.WithSource(source3.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")
            Dim f3 = compilation3.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' New lambda "_Lambda$__2-1#1" has been added
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I2-0, $I2-1#1, _Lambda$__2-0, _Lambda$__2-1#1}")

            ' updated
            diff1.VerifyIL("C._Closure$__._Lambda$__2-0", "
{
  // Code size        9 (0x9)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.2
  IL_0003:  add.ovf
  IL_0004:  stloc.0
  IL_0005:  br.s       IL_0007
  IL_0007:  ldloc.0
  IL_0008:  ret
}
")
            ' added
            diff1.VerifyIL("C._Closure$__._Lambda$__2-1#1", "
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.s   20
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.0
  IL_0009:  ret
}
")
            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            diff2.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I2-0, $I2-1#1, $I2-2#2, _Lambda$__2-0, _Lambda$__2-1#1, _Lambda$__2-2#2}")

            ' updated
            diff2.VerifyIL("C._Closure$__._Lambda$__2-0", "
{
  // Code size        9 (0x9)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.3
  IL_0003:  add.ovf
  IL_0004:  stloc.0
  IL_0005:  br.s       IL_0007
  IL_0007:  ldloc.0
  IL_0008:  ret
}
")
            ' updated
            diff2.VerifyIL("C._Closure$__._Lambda$__2-1#1", "
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.s   30
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.0
  IL_0009:  ret
}
")
            ' added
            diff2.VerifyIL("C._Closure$__._Lambda$__2-2#2", "
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4     0x300
  IL_0007:  add.ovf
  IL_0008:  stloc.0
  IL_0009:  br.s       IL_000b
  IL_000b:  ldloc.0
  IL_000c:  ret
}
")
            Dim diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f2, f3, GetSyntaxMapFromMarkers(source2, source3), preserveLocalVariables:=True)))

            diff3.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I2-0, $I2-1#1, $I2-2#2, _Lambda$__2-0, _Lambda$__2-1#1, _Lambda$__2-2#2}")

            ' updated
            diff3.VerifyIL("C._Closure$__._Lambda$__2-0", "
{
  // Code size        9 (0x9)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.4
  IL_0003:  add.ovf
  IL_0004:  stloc.0
  IL_0005:  br.s       IL_0007
  IL_0007:  ldloc.0
  IL_0008:  ret
}
")

            ' updated
            diff3.VerifyIL("C._Closure$__._Lambda$__2-1#1", "
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4.s   40
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  IL_0006:  br.s       IL_0008
  IL_0008:  ldloc.0
  IL_0009:  ret
}
")
            ' added
            diff3.VerifyIL("C._Closure$__._Lambda$__2-2#2", "
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldc.i4     0x400
  IL_0007:  add.ovf
  IL_0008:  stloc.0
  IL_0009:  br.s       IL_000b
  IL_000b:  ldloc.0
  IL_000c:  ret
}
")
        End Sub

        <Fact, WorkItem(2284, "https://github.com/dotnet/roslyn/issues/2284")>
        Public Sub LambdasMultipleGenerations2()
            Dim source0 = MarkedSource("
Imports System
Imports System.Linq

Class C
    Private _titles As Integer() = New Integer() {1, 2}
    Dim A As Action

    Private Sub F()
        ' edit 1
        ' Dim z = From title In _titles
        '         Where title > 0 
        '         Select title

        A = <N:0>Sub ()
            Console.WriteLine(1)

            ' edit 2
            ' Console.WriteLine(2)
        End Sub</N:0>
    End Sub
End Class
")
            Dim source1 = MarkedSource("
Imports System
Imports System.Linq

Class C
    Private _titles As Integer() = New Integer() {1, 2}
    Dim A As Action

    Private Sub F()
        ' edit 1
        Dim <N:3>z</N:3> = From title In _titles
                           <N:2>Where title > 0</N:2>
                           Select <N:1:ExpressionRangeVariable>title</N:1>

        A = <N:0>Sub ()
            Console.WriteLine(1)

            ' edit 2
            ' Console.WriteLine(2)
        End Sub</N:0>
    End Sub
End Class")

            Dim source2 = MarkedSource("
Imports System
Imports System.Linq

Class C
    Private _titles As Integer() = New Integer() {1, 2}
    Dim A As Action

    Private Sub F()
        ' edit 1
        Dim <N:3>z</N:3> = From title In _titles
                           <N:2>Where title > 0</N:2>
                           Select <N:1:ExpressionRangeVariable>title</N:1>

        A = <N:0>Sub ()
            Console.WriteLine(1)

            ' edit 2
            Console.WriteLine(2)
        End Sub</N:0>
    End Sub
End Class")

            Dim compilation0 = CreateCompilationWithReferences(source0.Tree, {MscorlibRef, SystemCoreRef}, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            ' new lambda "_Lambda$__3-0#1" has been added
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I3-0#1, $I3-1#1, $I3-0, _Lambda$__3-0#1, _Lambda$__3-1#1, _Lambda$__3-0}")

            ' lambda body unchanged
            diff1.VerifyIL("C._Closure$__._Lambda$__3-0", "
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0007:  nop
  IL_0008:  ret
}")

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            ' no new members
            diff2.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I3-0#1, $I3-1#1, $I3-0, _Lambda$__3-0#1, _Lambda$__3-1#1, _Lambda$__3-0}")

            ' lambda body updated
            diff2.VerifyIL("C._Closure$__._Lambda$__3-0", "
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0007:  nop
  IL_0008:  ldc.i4.2
  IL_0009:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_000e:  nop
  IL_000f:  ret
}")
        End Sub
    End Class
End Namespace
