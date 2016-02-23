' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.MetadataUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class EditAndContinueTests
        Inherits EditAndContinueTestBase

        <WorkItem(962219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/962219")>
        <Fact>
        Public Sub PartialMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Partial Class C
    Private Shared Partial Sub M1()
    End Sub
    Private Shared Partial Sub M2()
    End Sub
    Private Shared Partial Sub M3()
    End Sub
    Private Shared Sub M1()
    End Sub
    Private Shared Sub M2()
    End Sub
End Class
</file>
</compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim bytes0 = compilation0.EmitToArray()
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "M1", "M2")

                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M2").PartialImplementationPart
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M2").PartialImplementationPart
                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                Dim methods = diff1.TestData.Methods
                Assert.Equal(methods.Count, 1)
                Assert.True(methods.ContainsKey("C.M2()"))

                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    Dim readers = {reader0, reader1}
                    CheckNames(readers, reader1.GetMethodDefNames(), "M2")
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default))
                    CheckEncMap(reader1,
                        Handle(6, TableIndex.TypeRef),
                        Handle(3, TableIndex.MethodDef),
                        Handle(2, TableIndex.AssemblyRef))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub AddThenModifyExplicitImplementation()
            Dim source0 =
<compilation>
    <file name="a.vb">
Interface I(Of T)
    Sub M()
End Interface
</file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Interface I(Of T)
    Sub M()
End Interface
Class A
    Implements I(Of Integer), I(Of Object)
    Public Sub New()
    End Sub
    Sub M() Implements I(Of Integer).M, I(Of Object).M
    End Sub
End Class
</file>
</compilation>
            Dim source2 = source1
            Dim source3 =
<compilation>
    <file name="a.vb">
Interface I(Of T)
    Sub M()
End Interface
Class A
    Implements I(Of Integer), I(Of Object)
    Public Sub New()
    End Sub
    Sub M() Implements I(Of Integer).M, I(Of Object).M
    End Sub
End Class
Class B
    Implements I(Of Object)
    Public Sub New()
    End Sub
    Sub M() Implements I(Of Object).M
    End Sub
End Class
</file>
</compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)
            Dim compilation2 = compilation1.WithSource(source2)
            Dim compilation3 = compilation2.WithSource(source3)

            Dim bytes0 = compilation0.EmitToArray()
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader

                Dim type1 = compilation1.GetMember(Of NamedTypeSymbol)("A")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("A.M")
                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, type1)))

                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    Dim readers = {reader0, reader1}
                    CheckNames(readers, reader1.GetMethodDefNames(), ".ctor", "M")
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                        Row(2, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default),
                        Row(1, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(2, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                        Row(1, TableIndex.InterfaceImpl, EditAndContinueOperation.Default),
                        Row(2, TableIndex.InterfaceImpl, EditAndContinueOperation.Default))
                    CheckEncMap(reader1,
                        Handle(5, TableIndex.TypeRef),
                        Handle(3, TableIndex.TypeDef),
                        Handle(2, TableIndex.MethodDef),
                        Handle(3, TableIndex.MethodDef),
                        Handle(1, TableIndex.InterfaceImpl),
                        Handle(2, TableIndex.InterfaceImpl),
                        Handle(4, TableIndex.MemberRef),
                        Handle(5, TableIndex.MemberRef),
                        Handle(6, TableIndex.MemberRef),
                        Handle(1, TableIndex.MethodImpl),
                        Handle(2, TableIndex.MethodImpl),
                        Handle(1, TableIndex.TypeSpec),
                        Handle(2, TableIndex.TypeSpec),
                        Handle(2, TableIndex.AssemblyRef))

                    Dim generation1 = diff1.NextGeneration
                    Dim method2 = compilation2.GetMember(Of MethodSymbol)("A.M")
                    Dim diff2 = compilation2.EmitDifference(
                        generation1,
                        ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method1, method2)))

                    Using md2 = diff2.GetMetadata()
                        Dim reader2 = md2.Reader
                        readers = {reader0, reader1, reader2}
                        CheckNames(readers, reader2.GetMethodDefNames(), "M")
                        CheckEncLog(reader2,
                            Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                            Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                            Row(3, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                            Row(4, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                            Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default))
                        CheckEncMap(reader2,
                            Handle(6, TableIndex.TypeRef),
                            Handle(3, TableIndex.MethodDef),
                            Handle(3, TableIndex.TypeSpec),
                            Handle(4, TableIndex.TypeSpec),
                            Handle(3, TableIndex.AssemblyRef))

                        Dim generation2 = diff2.NextGeneration
                        Dim type3 = compilation3.GetMember(Of NamedTypeSymbol)("B")
                        Dim diff3 = compilation3.EmitDifference(
                            generation1,
                            ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, type3)))

                        Using md3 = diff3.GetMetadata()
                            Dim reader3 = md3.Reader
                            readers = {reader0, reader1, reader3}
                            CheckNames(readers, reader3.GetMethodDefNames(), ".ctor", "M")
                            CheckEncLog(reader3,
                                Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                                Row(7, TableIndex.MemberRef, EditAndContinueOperation.Default),
                                Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                                Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                                Row(3, TableIndex.TypeSpec, EditAndContinueOperation.Default),
                                Row(4, TableIndex.TypeDef, EditAndContinueOperation.Default),
                                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                                Row(5, TableIndex.MethodDef, EditAndContinueOperation.Default),
                                Row(3, TableIndex.MethodImpl, EditAndContinueOperation.Default),
                                Row(3, TableIndex.InterfaceImpl, EditAndContinueOperation.Default))
                            CheckEncMap(reader3,
                                Handle(6, TableIndex.TypeRef),
                                Handle(4, TableIndex.TypeDef),
                                Handle(4, TableIndex.MethodDef),
                                Handle(5, TableIndex.MethodDef),
                                Handle(3, TableIndex.InterfaceImpl),
                                Handle(7, TableIndex.MemberRef),
                                Handle(8, TableIndex.MemberRef),
                                Handle(3, TableIndex.MethodImpl),
                                Handle(3, TableIndex.TypeSpec),
                                Handle(3, TableIndex.AssemblyRef))
                        End Using
                    End Using
                End Using
            End Using
        End Sub

        <Fact, WorkItem(930065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/930065")>
        Public Sub ModifyConstructorBodyInPresenceOfExplicitInterfaceImplementation()
            Dim source =
<compilation>
    <file name="a.vb">
Interface I
    Sub M1()
    Sub M2()
End Interface
Class C
    Implements I
    Public Sub New()
    End Sub
    Sub M() Implements I.M1, I.M2
    End Sub
End Class
</file>
</compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim bytes0 = compilation0.EmitToArray()
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader

                Dim method0 = compilation0.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()
                Dim method1 = compilation1.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single()
                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))

                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    Dim readers = {reader0, reader1}
                    CheckNames(readers, reader1.GetTypeDefNames())
                    CheckNames(readers, reader1.GetMethodDefNames(), ".ctor")
                    CheckEncLog(reader1,
                        Row(2, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(5, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(6, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default))
                    CheckEncMap(reader1,
                        Handle(6, TableIndex.TypeRef),
                        Handle(3, TableIndex.MethodDef),
                        Handle(5, TableIndex.MemberRef),
                        Handle(2, TableIndex.AssemblyRef))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub NamespacesAndOverloads()
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(options:=TestOptions.DebugDll, sources:=
<compilation>
    <file name="a.vb"><![CDATA[
Class C
End Class
Namespace N
    Class C
    End Class
End Namespace
Namespace M
    Class C
        Sub M1(o As N.C)
        End Sub
        Sub M1(o As M.C)
        End Sub
        Sub M2(a As N.C, b As M.C, c As Global.C)
            M1(a)
        End Sub
    End Class
End Namespace
]]></file>
</compilation>)

            Dim bytes = compilation0.EmitToArray()
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes), EmptyLocalsProvider)
            Dim compilation1 = compilation0.WithSource(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
End Class
Namespace N
    Class C
    End Class
End Namespace
Namespace M
    Class C
        Sub M1(o As N.C)
        End Sub
        Sub M1(o As M.C)
        End Sub
        Sub M1(o As Global.C)
        End Sub
        Sub M2(a As N.C, b As M.C, c As Global.C)
            M1(a)
            M1(b)
        End Sub
    End Class
End Namespace
]]></file>
</compilation>)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, compilation1.GetMembers("M.C.M1")(2)),
                                      New SemanticEdit(SemanticEditKind.Update, compilation0.GetMembers("M.C.M2")(0), compilation1.GetMembers("M.C.M2")(0))))

            diff1.VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  call       0x06000004
  IL_0008:  nop
  IL_0009:  ldarg.0
  IL_000a:  ldarg.2
  IL_000b:  call       0x06000005
  IL_0010:  nop
  IL_0011:  ret
}
{
  // Code size        2 (0x2)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ret
}
")

            Dim compilation2 = compilation1.WithSource(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
End Class
Namespace N
    Class C
    End Class
End Namespace
Namespace M
    Class C
        Sub M1(o As N.C)
        End Sub
        Sub M1(o As M.C)
        End Sub
        Sub M1(o As Global.C)
        End Sub
        Sub M2(a As N.C, b As M.C, c As Global.C)
            M1(a)
            M1(b)
            M1(c)
        End Sub
    End Class
End Namespace
]]></file>
</compilation>)

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, compilation1.GetMembers("M.C.M2")(0), compilation2.GetMembers("M.C.M2")(0))))

            diff2.VerifyIL("
{
  // Code size       26 (0x1a)
  .maxstack  8
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  call       0x06000004
  IL_0008:  nop
  IL_0009:  ldarg.0
  IL_000a:  ldarg.2
  IL_000b:  call       0x06000005
  IL_0010:  nop
  IL_0011:  ldarg.0
  IL_0012:  ldarg.3
  IL_0013:  call       0x06000007
  IL_0018:  nop
  IL_0019:  ret
}
")
        End Sub

        <WorkItem(829353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829353")>
        <Fact()>
        Public Sub PrivateImplementationDetails_ArrayInitializer_FromMetadata()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim a As Integer() = {1, 2, 3}
        System.Console.Write(a(0))
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim a As Integer() = {1, 2, 3}
        System.Console.Write(a(1))
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(sources0, TestOptions.DebugDll.WithModuleName("MODULE"))
            Dim compilation1 = compilation0.WithSource(sources1)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim methodData0 = testData0.GetMethodData("C.M")
            methodData0.VerifyIL("
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (Integer() V_0) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""Integer""
  IL_0007:  dup
  IL_0008:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=12 <PrivateImplementationDetails>.E429CCA3F703A39CC5954A6572FEC9086135B34E""
  IL_000d:  call       ""Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  ldelem.i4
  IL_0016:  call       ""Sub System.Console.Write(Integer)""
  IL_001b:  nop
  IL_001c:  ret
}
")
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider)

            Dim testData1 = New CompilationTestData()
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
            Dim edit = New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(edit))
            diff1.VerifyIL("C.M", "
{
  // Code size       30 (0x1e)
  .maxstack  4
  .locals init (Integer() V_0) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newarr     ""Integer""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  dup
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  dup
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.3
  IL_0012:  stelem.i4
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  ldelem.i4
  IL_0017:  call       ""Sub System.Console.Write(Integer)""
  IL_001c:  nop
  IL_001d:  ret
}
")
        End Sub

        ''' <summary>
        ''' Should not generate method for string switch since
        ''' the CLR only allows adding private members.
        ''' </summary>
        <WorkItem(834086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834086")>
        <Fact()>
        Public Sub PrivateImplementationDetails_ComputeStringHash()
            Dim sources = <compilation>
                              <file name="a.vb"><![CDATA[
Class C
    Shared Function F(s As String)
        Select Case s
            Case "1"
                Return 1
            Case "2"
                Return 2
            Case "3"
                Return 3
            Case "4"
                Return 4
            Case "5"
                Return 5
            Case "6"
                Return 6
            Case "7"
                Return 7
            Case Else
                Return 0
        End Select
    End Function
End Class
]]></file>
                          </compilation>
            Const ComputeStringHashName As String = "ComputeStringHash"
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(sources, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim methodData0 = testData0.GetMethodData("C.F")
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), methodData0.EncDebugInfoProvider)

            ' Should have generated call to ComputeStringHash and
            ' added the method to <PrivateImplementationDetails>.
            Dim actualIL0 = methodData0.GetMethodIL()
            Assert.True(actualIL0.Contains(ComputeStringHashName))

            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetMethodDefNames(), ".ctor", "F", ComputeStringHashName)

                Dim testData1 = New CompilationTestData()
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")
                Dim edit = New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)
                Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(edit))

                ' Should not have generated call to ComputeStringHash nor
                ' added the method to <PrivateImplementationDetails>.
                Dim actualIL1 = diff1.GetMethodIL("C.F")
                Assert.False(actualIL1.Contains(ComputeStringHashName))

                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    Dim readers = {reader0, reader1}
                    CheckNames(readers, reader1.GetMethodDefNames(), "F")
                End Using
            End Using
        End Sub

        ''' <summary>
        ''' Avoid adding references from method bodies
        ''' other than the changed methods.
        ''' </summary>
        <Fact>
        Public Sub ReferencesInIL()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Sub F()
        System.Console.WriteLine(1)
    End Sub
    Sub G()
        System.Console.WriteLine(2)
    End Sub
End Module
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Module M
    Sub F()
        System.Console.WriteLine(1)
    End Sub
    Sub G()
        System.Console.Write(2)
    End Sub
End Module
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)

            ' Verify full metadata contains expected rows.
            Dim bytes0 = compilation0.EmitToArray()
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "M")
                CheckNames(reader0, reader0.GetMethodDefNames(), "F", "G")
                CheckNames(reader0, reader0.GetMemberRefNames(), ".ctor", ".ctor", ".ctor", ".ctor", "WriteLine")

                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)

                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, compilation1.GetMember("M.G"))))

                ' "Write" should be included in string table, but "WriteLine" should not.
                Assert.True(diff1.MetadataDelta.IsIncluded("Write"))
                Assert.False(diff1.MetadataDelta.IsIncluded("WriteLine"))
            End Using
        End Sub

        <Fact>
        Public Sub ExceptionFilters()
            Dim source0 = MarkedSource("
Imports System
Imports System.IO

Class C
    Shared Function filter(e As Exception)
        Return True
    End Function

    Shared Sub F()
        Try
            Throw New InvalidOperationException()
        <N:0>Catch e As IOException <N:1>When filter(e)</N:1></N:0>
            Console.WriteLine()
        <N:2>Catch e As Exception <N:3>When filter(e)</N:3></N:2>
            Console.WriteLine()
        End Try
    End Sub
End Class
")
            Dim source1 = MarkedSource("
Imports System
Imports System.IO

Class C
    Shared Function filter(e As Exception)
        Return True
    End Function

    Shared Sub F()
        Try
            Throw New InvalidOperationException()
        <N:0>Catch e As IOException <N:1>When filter(e)</N:1></N:0>
            Console.WriteLine()
        <N:2>Catch e As Exception <N:3>When filter(e)</N:3></N:2>
            Console.WriteLine()
        End Try

        Console.WriteLine()
    End Sub
End Class
")

            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime({source0.Tree}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.VerifyIL("C.F", "
{
  // Code size      118 (0x76)
  .maxstack  2
  .locals init (System.IO.IOException V_0, //e
                Boolean V_1,
                System.Exception V_2, //e
                Boolean V_3)
  IL_0000:  nop
  .try
  {
    IL_0001:  nop
    IL_0002:  newobj     ""Sub System.InvalidOperationException..ctor()""
    IL_0007:  throw
  }
  filter
  {
    IL_0008:  isinst     ""System.IO.IOException""
    IL_000d:  dup
    IL_000e:  brtrue.s   IL_0014
    IL_0010:  pop
    IL_0011:  ldc.i4.0
    IL_0012:  br.s       IL_002b
    IL_0014:  dup
    IL_0015:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  call       ""Function C.filter(System.Exception) As Object""
    IL_0021:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean""
    IL_0026:  stloc.1
    IL_0027:  ldloc.1
    IL_0028:  ldc.i4.0
    IL_0029:  cgt.un
    IL_002b:  endfilter
  }  // end filter
  {  // handler
    IL_002d:  pop
    IL_002e:  call       ""Sub System.Console.WriteLine()""
    IL_0033:  nop
    IL_0034:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0039:  leave.s    IL_006e
  }
  filter
  {
    IL_003b:  isinst     ""System.Exception""
    IL_0040:  dup
    IL_0041:  brtrue.s   IL_0047
    IL_0043:  pop
    IL_0044:  ldc.i4.0
    IL_0045:  br.s       IL_005e
    IL_0047:  dup
    IL_0048:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_004d:  stloc.2
    IL_004e:  ldloc.2
    IL_004f:  call       ""Function C.filter(System.Exception) As Object""
    IL_0054:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean""
    IL_0059:  stloc.3
    IL_005a:  ldloc.3
    IL_005b:  ldc.i4.0
    IL_005c:  cgt.un
    IL_005e:  endfilter
  }  // end filter
  {  // handler
    IL_0060:  pop
    IL_0061:  call       ""Sub System.Console.WriteLine()""
    IL_0066:  nop
    IL_0067:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_006c:  leave.s    IL_006e
  }
  IL_006e:  nop
  IL_006f:  call       ""Sub System.Console.WriteLine()""
  IL_0074:  nop
  IL_0075:  ret
}
")
        End Sub

        <Fact>
        Public Sub SymbolMatcher_TypeArguments()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class A(Of T)
    Class B(Of U)
        Shared Function M(Of V)(x As A(Of U).B(Of T), y As A(Of Object).S) As A(Of V)
            Return Nothing
        End Function
        Shared Function M(Of V)(x As A(Of U).B(Of T), y As A(Of V).S) As A(Of V)
            Return Nothing
        End Function
    End Class
    Structure S
    End Structure
End Class
]]>
                    </file>
                </compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim matcher = CreateMatcher(compilation1, compilation0)

            Dim members = compilation1.GetMember(Of NamedTypeSymbol)("A.B").GetMembers("M")
            Assert.Equal(members.Length, 2)
            For Each member In members
                Dim other = DirectCast(matcher.MapDefinition(DirectCast(member, Cci.IMethodDefinition)), MethodSymbol)
                Assert.NotNull(other)
            Next
        End Sub

        <Fact>
        Public Sub SymbolMatcher_Constraints()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Interface I(Of T As I(Of T))
End Interface
Class C
    Shared Sub M(Of T As I(Of T))(o As I(Of T))
    End Sub
End Class
]]>
                    </file>
                </compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim matcher = CreateMatcher(compilation1, compilation0)
            Dim member = compilation1.GetMember(Of MethodSymbol)("C.M")
            Dim other = DirectCast(matcher.MapDefinition(DirectCast(member, Cci.IMethodDefinition)), MethodSymbol)
            Assert.NotNull(other)
        End Sub

        <Fact>
        Public Sub SymbolMatcher_CustomModifiers()
            Dim ilSource = <![CDATA[
.class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public abstract virtual instance object modopt(A) [] F() { }
}
]]>.Value
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class B
    Inherits A
    Public Overrides Function F() As Object()
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>
            Dim metadata = CompileIL(ilSource)
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source, {metadata}, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim member1 = compilation1.GetMember(Of MethodSymbol)("B.F")
            Const nModifiers As Integer = 1
            Assert.Equal(nModifiers, DirectCast(member1.ReturnType, ArrayTypeSymbol).CustomModifiers.Length)

            Dim matcher = CreateMatcher(compilation1, compilation0)
            Dim other = DirectCast(matcher.MapDefinition(DirectCast(member1, Cci.IMethodDefinition)), MethodSymbol)
            Assert.NotNull(other)
            Assert.Equal(nModifiers, DirectCast(other.ReturnType, ArrayTypeSymbol).CustomModifiers.Length)
        End Sub

        <WorkItem(844472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844472")>
        <Fact()>
        Public Sub MethodSignatureWithNoPIAType()
            Dim sourcesPIA = <compilation>
                                 <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("35DB1A6B-D635-4320-A062-28D42920F2A3")>
<ComImport()>
<Guid("35DB1A6B-D635-4320-A062-28D42920F2A4")>
Public Interface I
End Interface
]]></file>
                             </compilation>
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M(x As I)
        Dim y As I = Nothing
        M(Nothing)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M(x As I)
        Dim y As I = Nothing
        M(x)
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilationPIA = CreateCompilationWithMscorlibAndVBRuntime(sourcesPIA)
            compilationPIA.AssertTheseDiagnostics()
            Dim referencePIA = compilationPIA.EmitToImageReference(embedInteropTypes:=True)

            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(sources0, options:=TestOptions.DebugDll, additionalRefs:={referencePIA})
            Dim compilation1 = compilation0.WithSource(sources1)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim methodData0 = testData0.GetMethodData("C.M")
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))
                diff1.VerifyIL("C.M", <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init ([unchanged] V_0,
  I V_1) //y
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.1
  IL_0003:  ldarg.0
  IL_0004:  call       "Sub C.M(I)"
  IL_0009:  nop
  IL_000a:  ret
}
]]>.Value)
            End Using
        End Sub

        ''' <summary>
        ''' Disallow edits that require NoPIA references.
        ''' </summary>
        <Fact()>
        Public Sub NoPIAReferences()
            Dim sourcesPIA = <compilation>
                                 <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("35DB1A6B-D635-4320-A062-28D42920F2B3")>
<ComImport()>
<Guid("35DB1A6B-D635-4320-A062-28D42920F2B4")>
Public Interface IA
    Sub M()
    ReadOnly Property P As Integer
    Event E As Action
End Interface
<ComImport()>
<Guid("35DB1A6B-D635-4320-A062-28D42920F2B5")>
Public Interface IB
End Interface
<ComImport()>
<Guid("35DB1A6B-D635-4320-A062-28D42920F2B6")>
Public Interface IC
End Interface
Public Structure S
    Public F As Object
End Structure
]]></file>
                             </compilation>
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C(Of T)
    Shared Private F As Object = GetType(IC)
    Shared Sub M1()
        Dim o As IA = Nothing
        o.M()
        M2(o.P)
        AddHandler o.E, AddressOf M1
        M2(C(Of IA).F)
        M2(New S())
    End Sub
    Shared Sub M2(o As Object)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1A = sources0
            Dim sources1B = <compilation>
                                <file name="a.vb"><![CDATA[
Class C(Of T)
    Shared Private F As Object = GetType(IC)
    Shared Sub M1()
        M2(Nothing)
    End Sub
    Shared Sub M2(o As Object)
    End Sub
End Class
]]></file>
                            </compilation>
            Dim compilationPIA = CreateCompilationWithMscorlibAndVBRuntime(sourcesPIA)
            compilationPIA.AssertTheseDiagnostics()
            Dim referencePIA = compilationPIA.EmitToImageReference(embedInteropTypes:=True)
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(sources0, options:=TestOptions.DebugDll, additionalRefs:={referencePIA})
            Dim compilation1A = compilation0.WithSource(sources1A)
            Dim compilation1B = compilation0.WithSource(sources1B)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim methodData0 = testData0.GetMethodData("C(Of T).M1")
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "C`1", "IA", "IC", "S")
                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, methodData0.EncDebugInfoProvider)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M1")

                ' Disallow edits that require NoPIA references.
                Dim method1A = compilation1A.GetMember(Of MethodSymbol)("C.M1")
                Dim diff1A = compilation1A.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1A, GetEquivalentNodesMap(method1A, method0), preserveLocalVariables:=True)))

                diff1A.EmitResult.Diagnostics.AssertTheseDiagnostics(<errors><![CDATA[
BC37230: Cannot continue since the edit includes a reference to an embedded type: 'IA'.
BC37230: Cannot continue since the edit includes a reference to an embedded type: 'S'.
     ]]></errors>)

                ' Allow edits that do not require NoPIA references,
                Dim method1B = compilation1B.GetMember(Of MethodSymbol)("C.M1")
                Dim diff1B = compilation1B.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1B, GetEquivalentNodesMap(method1B, method0), preserveLocalVariables:=True)))
                diff1B.VerifyIL("C(Of T).M1", <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init ([unchanged] V_0,
  [unchanged] V_1)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  call       "Sub C(Of T).M2(Object)"
  IL_0007:  nop
  IL_0008:  ret
}
]]>.Value)
                Using md1 = diff1B.GetMetadata()
                    Dim reader1 = md1.Reader
                    CheckNames({reader0, reader1}, reader1.GetTypeDefNames())
                End Using
            End Using
        End Sub

        <WorkItem(844536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844536")>
        <Fact()>
        Public Sub NoPIATypeInNamespace()
            Dim sourcesPIA = <compilation>
                                 <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
<Assembly: ImportedFromTypeLib("_.dll")>
<Assembly: Guid("35DB1A6B-D635-4320-A062-28D42920F2A5")>
Namespace N
    <ComImport()>
    <Guid("35DB1A6B-D635-4320-A062-28D42920F2A6")>
    Public Interface IA
    End Interface
End Namespace
<ComImport()>
<Guid("35DB1A6B-D635-4320-A062-28D42920F2A6")>
Public Interface IB
End Interface
]]></file>
                             </compilation>
            Dim sources = <compilation>
                              <file name="a.vb"><![CDATA[
Class C(Of T)
    Shared Sub M(o As Object)
        M(C(Of N.IA).E.X)
        M(C(Of IB).E.X)
    End Sub
    Enum E
        X
    End Enum
End Class
]]></file>
                          </compilation>
            Dim compilationPIA = CreateCompilationWithMscorlibAndVBRuntime(sourcesPIA)
            compilationPIA.AssertTheseDiagnostics()
            Dim referencePIA = AssemblyMetadata.CreateFromImage(compilationPIA.EmitToArray()).GetReference(embedInteropTypes:=True)
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(sources, options:=TestOptions.DebugDll, additionalRefs:={referencePIA})
            Dim compilation1 = compilation0.WithSource(sources)

            Dim bytes0 = compilation0.EmitToArray()
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))
                diff1.EmitResult.Diagnostics.AssertTheseDiagnostics(<errors><![CDATA[
BC37230: Cannot continue since the edit includes a reference to an embedded type: 'IA'.
BC37230: Cannot continue since the edit includes a reference to an embedded type: 'IB'.
     ]]></errors>)
                diff1.VerifyIL("C(Of T).M", <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  box        "C(Of N.IA).E"
  IL_0007:  call       "Sub C(Of T).M(Object)"
  IL_000c:  nop
  IL_000d:  ldc.i4.0
  IL_000e:  box        "C(Of IB).E"
  IL_0013:  call       "Sub C(Of T).M(Object)"
  IL_0018:  nop
  IL_0019:  ret
}
]]>.Value)
            End Using
        End Sub

        <Fact, WorkItem(1175704, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1175704")>
        Public Sub EventFields()
            Dim source0 = MarkedSource("
Imports System

Class C
    Shared Event handler As EventHandler

    Shared Function F() As Integer
        RaiseEvent handler(Nothing, Nothing)
        Return 1
    End Function
End Class
")
            Dim source1 = MarkedSource("
Imports System

Class C
    Shared Event handler As EventHandler

    Shared Function F() As Integer
        RaiseEvent handler(Nothing, Nothing)
        Return 10
    End Function
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib(source0.Tree, options:=ComSafeDebugDll)

            compilation0.AssertNoDiagnostics()

            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            Dim diff1 = compilation1.EmitDifference(
               generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.F", "
{
  // Code size       26 (0x1a)
  .maxstack  3
  .locals init (Integer V_0, //F
                [unchanged] V_1,
                System.EventHandler V_2)
  IL_0000:  nop
  IL_0001:  ldsfld     ""C.handlerEvent As System.EventHandler""
  IL_0006:  stloc.2
  IL_0007:  ldloc.2
  IL_0008:  brfalse.s  IL_0013
  IL_000a:  ldloc.2
  IL_000b:  ldnull
  IL_000c:  ldnull
  IL_000d:  callvirt   ""Sub System.EventHandler.Invoke(Object, System.EventArgs)""
  IL_0012:  nop
  IL_0013:  ldc.i4.s   10
  IL_0015:  stloc.0
  IL_0016:  br.s       IL_0018
  IL_0018:  ldloc.0
  IL_0019:  ret
}
")
        End Sub

        ''' <summary>
        ''' Should use TypeDef rather than TypeRef for unrecognized
        ''' local of a type defined in the original assembly.
        ''' </summary>
        <WorkItem(910777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910777")>
        <Fact()>
        Public Sub UnrecognizedLocalOfTypeFromAssembly()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Class E
    Inherits System.Exception
End Class
Class C
    Shared Sub M()
        Try
        Catch e As E
        End Try
    End Sub
End Class
]]></file>
</compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim bytes0 = compilation0.EmitToArray()
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetAssemblyRefNames(), "mscorlib", "Microsoft.VisualBasic")
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
                ' Use empty LocalVariableNameProvider for original locals and
                ' use preserveLocalVariables: true for the edit so that existing
                ' locals are retained even though all are unrecognized.
                Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider)
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, syntaxMap:=Function(s) Nothing, preserveLocalVariables:=True)))
                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    Dim readers = {reader0, reader1}
                    CheckNames(readers, reader1.GetAssemblyRefNames(), "mscorlib", "Microsoft.VisualBasic")
                    CheckNames(readers, reader1.GetTypeRefNames(), "Object", "ProjectData", "Exception")
                    CheckEncLog(reader1,
                        Row(3, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(4, TableIndex.AssemblyRef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.MemberRef, EditAndContinueOperation.Default),
                        Row(8, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(9, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(10, TableIndex.TypeRef, EditAndContinueOperation.Default),
                        Row(2, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                        Row(3, TableIndex.MethodDef, EditAndContinueOperation.Default))
                    CheckEncMap(reader1,
                        Handle(8, TableIndex.TypeRef),
                        Handle(9, TableIndex.TypeRef),
                        Handle(10, TableIndex.TypeRef),
                        Handle(3, TableIndex.MethodDef),
                        Handle(8, TableIndex.MemberRef),
                        Handle(9, TableIndex.MemberRef),
                        Handle(2, TableIndex.StandAloneSig),
                        Handle(3, TableIndex.AssemblyRef),
                        Handle(4, TableIndex.AssemblyRef))
                End Using
            End Using
        End Sub

        <Fact, WorkItem(837315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837315")>
        Public Sub AddingSetAccessor()
            Dim source0 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        System.Console.WriteLine("hello")
    End Sub

    Friend name As String
    Readonly Property GetName
        Get
            Return name
        End Get
    End Property
End Module
</file>
</compilation>

            Dim source1 =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        System.Console.WriteLine("hello")
    End Sub

    Friend name As String
    Property GetName
        Get
            Return name
        End Get
        Private Set(value)

        End Set
    End Property
End Module</file>
</compilation>

            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader

                Dim prop0 = compilation0.GetMember(Of PropertySymbol)("Module1.GetName")
                Dim prop1 = compilation1.GetMember(Of PropertySymbol)("Module1.GetName")
                Dim method1 = prop1.SetMethod

                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)

                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, method1, preserveLocalVariables:=True)))

                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    CheckNames({reader0, reader1}, reader1.GetMethodDefNames(), "set_GetName")
                End Using

                diff1.VerifyIL("Module1.set_GetName", "
{
  // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  nop
  IL_0001:  ret
}
")
            End Using

        End Sub

        <Fact>
        Public Sub PropertyGetterReturnValueVariable()
            Dim source0 =
<compilation>
    <file name="a.vb">
Module Module1
    ReadOnly Property P
        Get
            P = 1
        End Get
    End Property
End Module
</file>
</compilation>

            Dim source1 =
<compilation>
    <file name="a.vb">
Module Module1
    ReadOnly Property P
        Get
            P = 2
        End Get
    End Property
End Module</file>
</compilation>

            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim getter0 = compilation0.GetMember(Of PropertySymbol)("Module1.P").GetMethod
                Dim getter1 = compilation1.GetMember(Of PropertySymbol)("Module1.P").GetMethod

                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("Module1.get_P").EncDebugInfoProvider)

                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, getter0, getter1, preserveLocalVariables:=True)))

                diff1.VerifyIL("Module1.get_P", "
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (Object V_0) //P
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  box        ""Integer""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ret
}")
            End Using
        End Sub

#Region "Local Slots"
        <Fact, WorkItem(828389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828389")>
        Public Sub CatchClause()
            Dim source0 =
<compilation>
    <file name="a.vb">
Class C
    Shared Sub M()
        Try
            System.Console.WriteLine(1)
        Catch ex As System.Exception
        End Try
    End Sub
End Class
</file>
</compilation>

            Dim source1 =
<compilation>
    <file name="a.vb">
Class C
    Shared Sub M()
        Try
            System.Console.WriteLine(2)
        Catch ex As System.Exception
        End Try
    End Sub
End Class
</file>
</compilation>

            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (System.Exception V_0) //ex
  IL_0000:  nop
  .try
  {
    IL_0001:  nop
    IL_0002:  ldc.i4.2
    IL_0003:  call       ""Sub System.Console.WriteLine(Integer)""
    IL_0008:  nop
    IL_0009:  leave.s    IL_001a
  }
  catch System.Exception
  {
    IL_000b:  dup
    IL_000c:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_0011:  stloc.0
    IL_0012:  nop
    IL_0013:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0018:  leave.s    IL_001a
  }
  IL_001a:  nop
  IL_001b:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlots()
            Dim sources0 = <compilation>
                               <file><![CDATA[
Class A(Of T)
End Class
Class B
    Inherits A(Of B)
    Shared Function F() As B
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        Dim x As Object = F()
        Dim y As A(Of B) = F()
        Dim z As Object = F()
        M(x)
        M(y)
        M(z)
    End Sub
    Shared Sub N()
        Dim a As Object = F()
        Dim b As Object = F()
        M(a)
        M(b)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file><![CDATA[
Class A(Of T)
End Class
Class B
    Inherits A(Of B)
    Shared Function F() As B
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        Dim z As B = F()
        Dim y As A(Of B) = F()
        Dim w As Object = F()
        M(w)
        M(y)
    End Sub
    Shared Sub N()
        Dim a As Object = F()
        Dim b As Object = F()
        M(a)
        M(b)
    End Sub
End Class
]]></file>
                           </compilation>

            Dim sources2 = <compilation>
                               <file><![CDATA[
Class A(Of T)
End Class
Class B
    Inherits A(Of B)
    Shared Function F() As B
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        Dim x As Object = F()
        Dim z As B = F()
        M(x)
        M(z)
    End Sub
    Shared Sub N()
        Dim a As Object = F()
        Dim b As Object = F()
        M(a)
        M(b)
    End Sub
End Class
]]></file>
                           </compilation>

            Dim sources3 = <compilation>
                               <file><![CDATA[
Class A(Of T)
End Class
Class B
    Inherits A(Of B)
    Shared Function F() As B
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        Dim x As Object = F()
        Dim z As B = F()
        M(x)
        M(z)
    End Sub
    Shared Sub N()
        Dim c As Object = F()
        Dim b As Object = F()
        M(c)
        M(b)
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim compilation2 = compilation1.WithSource(sources2)
            Dim compilation3 = compilation2.WithSource(sources3)

            ' Verify full metadata contains expected rows.
            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("B.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("B.M")
            Dim methodN = compilation0.GetMember(Of MethodSymbol)("B.N")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(
                ModuleMetadata.CreateFromImage(bytes0),
                Function(m)
                    Select Case MetadataTokens.GetRowNumber(m)
                        Case 4
                            Return testData0.GetMethodData("B.M").GetEncDebugInfo()
                        Case 5
                            Return testData0.GetMethodData("B.N").GetEncDebugInfo()
                        Case Else
                            Return Nothing
                    End Select
                End Function)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

            diff1.VerifyIL("
{
  // Code size       41 (0x29)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       0x06000003
  IL_0006:  stloc.3
  IL_0007:  call       0x06000003
  IL_000c:  stloc.1
  IL_000d:  call       0x06000003
  IL_0012:  stloc.s    V_4
  IL_0014:  ldloc.s    V_4
  IL_0016:  call       0x0A000007
  IL_001b:  call       0x06000004
  IL_0020:  nop
  IL_0021:  ldloc.1
  IL_0022:  call       0x06000004
  IL_0027:  nop
  IL_0028:  ret
}
")

            diff1.VerifyPdb({&H06000001UI, &H06000002UI, &H06000003UI, &H06000004UI, &H06000005UI},
<symbols>
    <methods>
        <method token="0x6000004">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="5" endLine="8" endColumn="30"/>
                <entry offset="0x1" startLine="9" startColumn="13" endLine="9" endColumn="25"/>
                <entry offset="0x7" startLine="10" startColumn="13" endLine="10" endColumn="31"/>
                <entry offset="0xd" startLine="11" startColumn="13" endLine="11" endColumn="30"/>
                <entry offset="0x14" startLine="12" startColumn="9" endLine="12" endColumn="13"/>
                <entry offset="0x21" startLine="13" startColumn="9" endLine="13" endColumn="13"/>
                <entry offset="0x28" startLine="14" startColumn="5" endLine="14" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x29">
                <currentnamespace name=""/>
                <local name="z" il_index="3" il_start="0x0" il_end="0x29" attributes="0"/>
                <local name="y" il_index="1" il_start="0x0" il_end="0x29" attributes="0"/>
                <local name="w" il_index="4" il_start="0x0" il_end="0x29" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            Dim method2 = compilation2.GlobalNamespace.GetMember(Of NamedTypeSymbol)("B").GetMember(Of MethodSymbol)("M")

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables:=True)))

            diff2.VerifyIL("
{
  // Code size       35 (0x23)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       0x06000003
  IL_0006:  stloc.s    V_5
  IL_0008:  call       0x06000003
  IL_000d:  stloc.3
  IL_000e:  ldloc.s    V_5
  IL_0010:  call       0x0A000008
  IL_0015:  call       0x06000004
  IL_001a:  nop
  IL_001b:  ldloc.3
  IL_001c:  call       0x06000004
  IL_0021:  nop
  IL_0022:  ret
}
")

            diff2.VerifyPdb({&H06000001UI, &H06000002UI, &H06000003UI, &H06000004UI, &H06000005UI},
<symbols>
    <methods>
        <method token="0x6000004">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="5" endLine="8" endColumn="30"/>
                <entry offset="0x1" startLine="9" startColumn="13" endLine="9" endColumn="30"/>
                <entry offset="0x8" startLine="10" startColumn="13" endLine="10" endColumn="25"/>
                <entry offset="0xe" startLine="11" startColumn="9" endLine="11" endColumn="13"/>
                <entry offset="0x1b" startLine="12" startColumn="9" endLine="12" endColumn="13"/>
                <entry offset="0x22" startLine="13" startColumn="5" endLine="13" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x23">
                <currentnamespace name=""/>
                <local name="x" il_index="5" il_start="0x0" il_end="0x23" attributes="0"/>
                <local name="z" il_index="3" il_start="0x0" il_end="0x23" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            ' Modify different method. (Previous generations
            ' have not referenced method.)

            method2 = compilation2.GlobalNamespace.GetMember(Of NamedTypeSymbol)("B").GetMember(Of MethodSymbol)("N")
            Dim method3 = compilation3.GlobalNamespace.GetMember(Of NamedTypeSymbol)("B").GetMember(Of MethodSymbol)("N")
            Dim metadata3 As ImmutableArray(Of Byte) = Nothing
            Dim il3 As ImmutableArray(Of Byte) = Nothing
            Dim pdb3 As Stream = Nothing

            Dim diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method2, method3, GetEquivalentNodesMap(method3, method2), preserveLocalVariables:=True)))

            diff3.VerifyIL("
{
  // Code size       38 (0x26)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       0x06000003
  IL_0006:  stloc.2
  IL_0007:  call       0x06000003
  IL_000c:  stloc.1
  IL_000d:  ldloc.2
  IL_000e:  call       0x0A000009
  IL_0013:  call       0x06000004
  IL_0018:  nop
  IL_0019:  ldloc.1
  IL_001a:  call       0x0A000009
  IL_001f:  call       0x06000004
  IL_0024:  nop
  IL_0025:  ret
}
")

            diff3.VerifyPdb({&H06000001UI, &H06000002UI, &H06000003UI, &H06000004UI, &H06000005UI},
<symbols>
    <methods>
        <method token="0x6000005">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="5" endLine="14" endColumn="19"/>
                <entry offset="0x1" startLine="15" startColumn="13" endLine="15" endColumn="30"/>
                <entry offset="0x7" startLine="16" startColumn="13" endLine="16" endColumn="30"/>
                <entry offset="0xd" startLine="17" startColumn="9" endLine="17" endColumn="13"/>
                <entry offset="0x19" startLine="18" startColumn="9" endLine="18" endColumn="13"/>
                <entry offset="0x25" startLine="19" startColumn="5" endLine="19" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x26">
                <currentnamespace name=""/>
                <local name="c" il_index="2" il_start="0x0" il_end="0x26" attributes="0"/>
                <local name="b" il_index="1" il_start="0x0" il_end="0x26" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        ''' <summary>
        ''' Preserve locals for method added after initial compilation.
        ''' </summary>
        <Fact()>
        Public Sub PreserveLocalSlots_NewMethod()
            Dim sources0 = <compilation>
                               <file><![CDATA[
Class C
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file><![CDATA[
Class C
    Shared Sub M()
        Dim a = New Object()
        Dim b = String.Empty
    End Sub
End Class
]]></file>
                           </compilation>

            Dim sources2 = <compilation>
                               <file><![CDATA[
Class C
    Shared Sub M()
        Dim a = 1
        Dim b = String.Empty
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim compilation2 = compilation1.WithSource(sources2)

            Dim bytes0 = compilation0.EmitToArray()
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)

            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, method1, Nothing, preserveLocalVariables:=True)))

            Dim method2 = compilation2.GetMember(Of MethodSymbol)("C.M")
            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables:=True)))
            diff2.VerifyIL("C.M", <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init ([object] V_0,
                String V_1, //b
                Integer V_2) //a
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.2
  IL_0003:  ldsfld     "String.Empty As String"
  IL_0008:  stloc.1
  IL_0009:  ret
}
]]>.Value)
            diff2.VerifyPdb({&H06000002UI},
<symbols>
    <methods>
        <method token="0x6000002">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="19"/>
                <entry offset="0x1" startLine="3" startColumn="13" endLine="3" endColumn="18"/>
                <entry offset="0x3" startLine="4" startColumn="13" endLine="4" endColumn="29"/>
                <entry offset="0x9" startLine="5" startColumn="5" endLine="5" endColumn="12"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <currentnamespace name=""/>
                <local name="a" il_index="2" il_start="0x0" il_end="0xa" attributes="0"/>
                <local name="b" il_index="1" il_start="0x0" il_end="0xa" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        ''' <summary>
        ''' Local types should be retained, even if the local is no longer
        ''' used by the method body, since there may be existing
        ''' references to that slot, in a Watch window for instance.
        ''' </summary>
        <WorkItem(843320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843320")>
        <Fact>
        Public Sub PreserveLocalTypes()
            Dim sources0 = <compilation>
                               <file><![CDATA[
Class C
    Shared Sub Main()
        Dim x = True
        Dim y = x
        System.Console.WriteLine(y)
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file><![CDATA[
Class C
    Shared Sub Main()
        Dim x = "A"
        Dim y = x
        System.Console.WriteLine(y)
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.Main")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.Main")
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.Main").EncDebugInfoProvider)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))
            diff1.VerifyIL("C.Main", "
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init ([bool] V_0,
           [bool] V_1,
           String V_2, //x
           String V_3) //y
  IL_0000:  nop       
  IL_0001:  ldstr      ""A""
  IL_0006:  stloc.2   
  IL_0007:  ldloc.2   
  IL_0008:  stloc.3   
  IL_0009:  ldloc.3   
  IL_000a:  call       ""Sub System.Console.WriteLine(String)""
  IL_000f:  nop       
  IL_0010:  ret       
}")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsReferences()
            Dim sources0 = <compilation>
                               <file><![CDATA[

Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x = new system.collections.generic.stack(of Integer)
        x.Push(1)
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(sources0, XmlReferences, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (System.Collections.Generic.Stack(Of Integer) V_0) //x
  IL_0000:  nop
  IL_0001:  newobj     ""Sub System.Collections.Generic.Stack(Of Integer)..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  callvirt   ""Sub System.Collections.Generic.Stack(Of Integer).Push(Integer)""
  IL_000e:  nop
  IL_000f:  ret
}
")

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim modMeta = ModuleMetadata.CreateFromImage(bytes0)
            Dim generation0 = EmitBaseline.CreateInitialBaseline(modMeta, testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (System.Collections.Generic.Stack(Of Integer) V_0) //x
  IL_0000:  nop
  IL_0001:  newobj     "Sub System.Collections.Generic.Stack(Of Integer)..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  callvirt   "Sub System.Collections.Generic.Stack(Of Integer).Push(Integer)"
  IL_000e:  nop
  IL_000f:  ret
}
]]>.Value)
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsUsing()
            Dim sources0 = <compilation>
                               <file><![CDATA[

Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.IDisposable = nothing
        Using x
        end using
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (System.IDisposable V_0, //x
                System.IDisposable V_1)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  stloc.1
  .try
  {
    IL_0006:  leave.s    IL_0014
  }
  finally
  {
    IL_0008:  nop
    IL_0009:  ldloc.1
    IL_000a:  brfalse.s  IL_0013
    IL_000c:  ldloc.1
    IL_000d:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0012:  nop
    IL_0013:  endfinally
  }
  IL_0014:  ret
}
")

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (System.IDisposable V_0, //x
                System.IDisposable V_1)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  stloc.1
  .try
  {
    IL_0006:  leave.s    IL_0014
  }
  finally
  {
    IL_0008:  nop
    IL_0009:  ldloc.1
    IL_000a:  brfalse.s  IL_0013
    IL_000c:  ldloc.1
    IL_000d:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0012:  nop
    IL_0013:  endfinally
  }
  IL_0014:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsWithByRef()
            Dim sources0 = <compilation>
                               <file><![CDATA[

Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.Guid() = nothing
        With x(3)
            .ToString()
        end With
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (System.Guid() V_0, //x
                System.Guid& V_1) //$W0
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.3
  IL_0006:  ldelema    ""System.Guid""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""System.Guid""
  IL_0013:  callvirt   ""Function Object.ToString() As String""
  IL_0018:  pop
  IL_0019:  nop
  IL_001a:  ret
}
")

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (System.Guid() V_0, //x
                System.Guid& V_1) //$W0
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.3
  IL_0006:  ldelema    ""System.Guid""
  IL_000b:  stloc.1
  IL_000c:  ldloc.1
  IL_000d:  constrained. ""System.Guid""
  IL_0013:  callvirt   ""Function Object.ToString() As String""
  IL_0018:  pop
  IL_0019:  nop
  IL_001a:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsWithByVal()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[

Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.Guid() = nothing
        With x
            .ToString()
        end With
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (System.Guid() V_0, //x
                System.Guid() V_1) //$W0
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  callvirt   ""Function Object.ToString() As String""
  IL_000c:  pop
  IL_000d:  nop
  IL_000e:  ldnull
  IL_000f:  stloc.1
  IL_0010:  ret
}
")
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (System.Guid() V_0, //x
                System.Guid() V_1) //$W0
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  callvirt   ""Function Object.ToString() As String""
  IL_000c:  pop
  IL_000d:  nop
  IL_000e:  ldnull
  IL_000f:  stloc.1
  IL_0010:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsSyncLock()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.Guid() = nothing
        SyncLock x
            dim y as System.Guid() = nothing
            SyncLock y
                x.ToString()
            end SyncLock
        end SyncLock
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size       76 (0x4c)
  .maxstack  2
  .locals init (System.Guid() V_0, //x
                Object V_1,
                Boolean V_2,
                System.Guid() V_3, //y
                Object V_4,
                Boolean V_5)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  stloc.1
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.2
  .try
  {
    IL_0008:  ldloc.1
    IL_0009:  ldloca.s   V_2
    IL_000b:  call       ""Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)""
    IL_0010:  nop
    IL_0011:  ldnull
    IL_0012:  stloc.3
    IL_0013:  nop
    IL_0014:  ldloc.3
    IL_0015:  stloc.s    V_4
    IL_0017:  ldc.i4.0
    IL_0018:  stloc.s    V_5
    .try
    {
      IL_001a:  ldloc.s    V_4
      IL_001c:  ldloca.s   V_5
      IL_001e:  call       ""Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)""
      IL_0023:  nop
      IL_0024:  ldloc.0
      IL_0025:  callvirt   ""Function Object.ToString() As String""
      IL_002a:  pop
      IL_002b:  leave.s    IL_003b
    }
    finally
    {
      IL_002d:  ldloc.s    V_5
      IL_002f:  brfalse.s  IL_0039
      IL_0031:  ldloc.s    V_4
      IL_0033:  call       ""Sub System.Threading.Monitor.Exit(Object)""
      IL_0038:  nop
      IL_0039:  nop
      IL_003a:  endfinally
    }
    IL_003b:  nop
    IL_003c:  leave.s    IL_004a
  }
  finally
  {
    IL_003e:  ldloc.2
    IL_003f:  brfalse.s  IL_0048
    IL_0041:  ldloc.1
    IL_0042:  call       ""Sub System.Threading.Monitor.Exit(Object)""
    IL_0047:  nop
    IL_0048:  nop
    IL_0049:  endfinally
  }
  IL_004a:  nop
  IL_004b:  ret
}
")

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size       76 (0x4c)
  .maxstack  2
  .locals init (System.Guid() V_0, //x
                Object V_1,
                Boolean V_2,
                System.Guid() V_3, //y
                Object V_4,
                Boolean V_5)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldloc.0
  IL_0005:  stloc.1
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.2
  .try
  {
    IL_0008:  ldloc.1
    IL_0009:  ldloca.s   V_2
    IL_000b:  call       ""Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)""
    IL_0010:  nop
    IL_0011:  ldnull
    IL_0012:  stloc.3
    IL_0013:  nop
    IL_0014:  ldloc.3
    IL_0015:  stloc.s    V_4
    IL_0017:  ldc.i4.0
    IL_0018:  stloc.s    V_5
    .try
    {
      IL_001a:  ldloc.s    V_4
      IL_001c:  ldloca.s   V_5
      IL_001e:  call       ""Sub System.Threading.Monitor.Enter(Object, ByRef Boolean)""
      IL_0023:  nop
      IL_0024:  ldloc.0
      IL_0025:  callvirt   ""Function Object.ToString() As String""
      IL_002a:  pop
      IL_002b:  leave.s    IL_003b
    }
    finally
    {
      IL_002d:  ldloc.s    V_5
      IL_002f:  brfalse.s  IL_0039
      IL_0031:  ldloc.s    V_4
      IL_0033:  call       ""Sub System.Threading.Monitor.Exit(Object)""
      IL_0038:  nop
      IL_0039:  nop
      IL_003a:  endfinally
    }
    IL_003b:  nop
    IL_003c:  leave.s    IL_004a
  }
  finally
  {
    IL_003e:  ldloc.2
    IL_003f:  brfalse.s  IL_0048
    IL_0041:  ldloc.1
    IL_0042:  call       ""Sub System.Threading.Monitor.Exit(Object)""
    IL_0047:  nop
    IL_0048:  nop
    IL_0049:  endfinally
  }
  IL_004a:  nop
  IL_004b:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsForEach()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[

Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.Collections.Generic.List(of integer) = nothing
        for each [i] in [x]
        Next
        for each i as integer in x
        Next
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size      101 (0x65)
  .maxstack  1
  .locals init (System.Collections.Generic.List(Of Integer) V_0, //x
                System.Collections.Generic.List(Of Integer).Enumerator V_1,
                Integer V_2, //i
                Boolean V_3,
                System.Collections.Generic.List(Of Integer).Enumerator V_4,
                Integer V_5, //i
                Boolean V_6)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  .try
  {
    IL_0003:  ldloc.0
    IL_0004:  callvirt   ""Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator""
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_0015
    IL_000c:  ldloca.s   V_1
    IL_000e:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer""
    IL_0013:  stloc.2
    IL_0014:  nop
    IL_0015:  ldloca.s   V_1
    IL_0017:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean""
    IL_001c:  stloc.3
    IL_001d:  ldloc.3
    IL_001e:  brtrue.s   IL_000c
    IL_0020:  leave.s    IL_0031
  }
  finally
  {
    IL_0022:  ldloca.s   V_1
    IL_0024:  constrained. ""System.Collections.Generic.List(Of Integer).Enumerator""
    IL_002a:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_002f:  nop
    IL_0030:  endfinally
  }
  IL_0031:  nop
  .try
  {
    IL_0032:  ldloc.0
    IL_0033:  callvirt   ""Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator""
    IL_0038:  stloc.s    V_4
    IL_003a:  br.s       IL_0046
    IL_003c:  ldloca.s   V_4
    IL_003e:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer""
    IL_0043:  stloc.s    V_5
    IL_0045:  nop
    IL_0046:  ldloca.s   V_4
    IL_0048:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean""
    IL_004d:  stloc.s    V_6
    IL_004f:  ldloc.s    V_6
    IL_0051:  brtrue.s   IL_003c
    IL_0053:  leave.s    IL_0064
  }
  finally
  {
    IL_0055:  ldloca.s   V_4
    IL_0057:  constrained. ""System.Collections.Generic.List(Of Integer).Enumerator""
    IL_005d:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0062:  nop
    IL_0063:  endfinally
  }
  IL_0064:  ret
}
")

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size      101 (0x65)
  .maxstack  1
  .locals init (System.Collections.Generic.List(Of Integer) V_0, //x
                System.Collections.Generic.List(Of Integer).Enumerator V_1,
                Integer V_2, //i
                Boolean V_3,
                System.Collections.Generic.List(Of Integer).Enumerator V_4,
                Integer V_5, //i
                Boolean V_6)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  .try
  {
    IL_0003:  ldloc.0
    IL_0004:  callvirt   ""Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator""
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_0015
    IL_000c:  ldloca.s   V_1
    IL_000e:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer""
    IL_0013:  stloc.2
    IL_0014:  nop
    IL_0015:  ldloca.s   V_1
    IL_0017:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean""
    IL_001c:  stloc.3
    IL_001d:  ldloc.3
    IL_001e:  brtrue.s   IL_000c
    IL_0020:  leave.s    IL_0031
  }
  finally
  {
    IL_0022:  ldloca.s   V_1
    IL_0024:  constrained. ""System.Collections.Generic.List(Of Integer).Enumerator""
    IL_002a:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_002f:  nop
    IL_0030:  endfinally
  }
  IL_0031:  nop
  .try
  {
    IL_0032:  ldloc.0
    IL_0033:  callvirt   ""Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator""
    IL_0038:  stloc.s    V_4
    IL_003a:  br.s       IL_0046
    IL_003c:  ldloca.s   V_4
    IL_003e:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer""
    IL_0043:  stloc.s    V_5
    IL_0045:  nop
    IL_0046:  ldloca.s   V_4
    IL_0048:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean""
    IL_004d:  stloc.s    V_6
    IL_004f:  ldloc.s    V_6
    IL_0051:  brtrue.s   IL_003c
    IL_0053:  leave.s    IL_0064
  }
  finally
  {
    IL_0055:  ldloca.s   V_4
    IL_0057:  constrained. ""System.Collections.Generic.List(Of Integer).Enumerator""
    IL_005d:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0062:  nop
    IL_0063:  endfinally
  }
  IL_0064:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsForEach001()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.Collections.Generic.List(of integer) = nothing
        Dim i as integer
        for each i in x
        Next
        for each i in x
        Next
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size      100 (0x64)
  .maxstack  1
  .locals init (System.Collections.Generic.List(Of Integer) V_0, //x
                Integer V_1, //i
                System.Collections.Generic.List(Of Integer).Enumerator V_2,
                Boolean V_3,
                System.Collections.Generic.List(Of Integer).Enumerator V_4,
                Boolean V_5)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  .try
  {
    IL_0003:  ldloc.0
    IL_0004:  callvirt   ""Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator""
    IL_0009:  stloc.2
    IL_000a:  br.s       IL_0015
    IL_000c:  ldloca.s   V_2
    IL_000e:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer""
    IL_0013:  stloc.1
    IL_0014:  nop
    IL_0015:  ldloca.s   V_2
    IL_0017:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean""
    IL_001c:  stloc.3
    IL_001d:  ldloc.3
    IL_001e:  brtrue.s   IL_000c
    IL_0020:  leave.s    IL_0031
  }
  finally
  {
    IL_0022:  ldloca.s   V_2
    IL_0024:  constrained. ""System.Collections.Generic.List(Of Integer).Enumerator""
    IL_002a:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_002f:  nop
    IL_0030:  endfinally
  }
  IL_0031:  nop
  .try
  {
    IL_0032:  ldloc.0
    IL_0033:  callvirt   ""Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator""
    IL_0038:  stloc.s    V_4
    IL_003a:  br.s       IL_0045
    IL_003c:  ldloca.s   V_4
    IL_003e:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer""
    IL_0043:  stloc.1
    IL_0044:  nop
    IL_0045:  ldloca.s   V_4
    IL_0047:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean""
    IL_004c:  stloc.s    V_5
    IL_004e:  ldloc.s    V_5
    IL_0050:  brtrue.s   IL_003c
    IL_0052:  leave.s    IL_0063
  }
  finally
  {
    IL_0054:  ldloca.s   V_4
    IL_0056:  constrained. ""System.Collections.Generic.List(Of Integer).Enumerator""
    IL_005c:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0061:  nop
    IL_0062:  endfinally
  }
  IL_0063:  ret
}
")

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size      100 (0x64)
  .maxstack  1
  .locals init (System.Collections.Generic.List(Of Integer) V_0, //x
                Integer V_1, //i
                System.Collections.Generic.List(Of Integer).Enumerator V_2,
                Boolean V_3,
                System.Collections.Generic.List(Of Integer).Enumerator V_4,
                Boolean V_5)
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.0
  .try
  {
    IL_0003:  ldloc.0
    IL_0004:  callvirt   ""Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator""
    IL_0009:  stloc.2
    IL_000a:  br.s       IL_0015
    IL_000c:  ldloca.s   V_2
    IL_000e:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer""
    IL_0013:  stloc.1
    IL_0014:  nop
    IL_0015:  ldloca.s   V_2
    IL_0017:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean""
    IL_001c:  stloc.3
    IL_001d:  ldloc.3
    IL_001e:  brtrue.s   IL_000c
    IL_0020:  leave.s    IL_0031
  }
  finally
  {
    IL_0022:  ldloca.s   V_2
    IL_0024:  constrained. ""System.Collections.Generic.List(Of Integer).Enumerator""
    IL_002a:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_002f:  nop
    IL_0030:  endfinally
  }
  IL_0031:  nop
  .try
  {
    IL_0032:  ldloc.0
    IL_0033:  callvirt   ""Function System.Collections.Generic.List(Of Integer).GetEnumerator() As System.Collections.Generic.List(Of Integer).Enumerator""
    IL_0038:  stloc.s    V_4
    IL_003a:  br.s       IL_0045
    IL_003c:  ldloca.s   V_4
    IL_003e:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.get_Current() As Integer""
    IL_0043:  stloc.1
    IL_0044:  nop
    IL_0045:  ldloca.s   V_4
    IL_0047:  call       ""Function System.Collections.Generic.List(Of Integer).Enumerator.MoveNext() As Boolean""
    IL_004c:  stloc.s    V_5
    IL_004e:  ldloc.s    V_5
    IL_0050:  brtrue.s   IL_003c
    IL_0052:  leave.s    IL_0063
  }
  finally
  {
    IL_0054:  ldloca.s   V_4
    IL_0056:  constrained. ""System.Collections.Generic.List(Of Integer).Enumerator""
    IL_005c:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0061:  nop
    IL_0062:  endfinally
  }
  IL_0063:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsFor001()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As object)
        for i as double = foo() to foo() step foo()
            for j as double = foo() to foo() step foo()
            next
        next
    End Sub

    shared function foo() as double
        return 1
    end function
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size      148 (0x94)
  .maxstack  2
  .locals init (Double V_0,
                Double V_1,
                Double V_2,
                Boolean V_3,
                Double V_4, //i
                Double V_5,
                Double V_6,
                Double V_7,
                Boolean V_8,
                Double V_9) //j
  IL_0000:  nop
  IL_0001:  call       ""Function C.foo() As Double""
  IL_0006:  stloc.0
  IL_0007:  call       ""Function C.foo() As Double""
  IL_000c:  stloc.1
  IL_000d:  call       ""Function C.foo() As Double""
  IL_0012:  stloc.2
  IL_0013:  ldloc.2
  IL_0014:  ldc.r8     0
  IL_001d:  clt.un
  IL_001f:  ldc.i4.0
  IL_0020:  ceq
  IL_0022:  stloc.3
  IL_0023:  ldloc.0
  IL_0024:  stloc.s    V_4
  IL_0026:  br.s       IL_007c
  IL_0028:  call       ""Function C.foo() As Double""
  IL_002d:  stloc.s    V_5
  IL_002f:  call       ""Function C.foo() As Double""
  IL_0034:  stloc.s    V_6
  IL_0036:  call       ""Function C.foo() As Double""
  IL_003b:  stloc.s    V_7
  IL_003d:  ldloc.s    V_7
  IL_003f:  ldc.r8     0
  IL_0048:  clt.un
  IL_004a:  ldc.i4.0
  IL_004b:  ceq
  IL_004d:  stloc.s    V_8
  IL_004f:  ldloc.s    V_5
  IL_0051:  stloc.s    V_9
  IL_0053:  br.s       IL_005c
  IL_0055:  ldloc.s    V_9
  IL_0057:  ldloc.s    V_7
  IL_0059:  add
  IL_005a:  stloc.s    V_9
  IL_005c:  ldloc.s    V_8
  IL_005e:  brtrue.s   IL_006b
  IL_0060:  ldloc.s    V_9
  IL_0062:  ldloc.s    V_6
  IL_0064:  clt.un
  IL_0066:  ldc.i4.0
  IL_0067:  ceq
  IL_0069:  br.s       IL_0074
  IL_006b:  ldloc.s    V_9
  IL_006d:  ldloc.s    V_6
  IL_006f:  cgt.un
  IL_0071:  ldc.i4.0
  IL_0072:  ceq
  IL_0074:  brtrue.s   IL_0055
  IL_0076:  ldloc.s    V_4
  IL_0078:  ldloc.2
  IL_0079:  add
  IL_007a:  stloc.s    V_4
  IL_007c:  ldloc.3
  IL_007d:  brtrue.s   IL_0089
  IL_007f:  ldloc.s    V_4
  IL_0081:  ldloc.1
  IL_0082:  clt.un
  IL_0084:  ldc.i4.0
  IL_0085:  ceq
  IL_0087:  br.s       IL_0091
  IL_0089:  ldloc.s    V_4
  IL_008b:  ldloc.1
  IL_008c:  cgt.un
  IL_008e:  ldc.i4.0
  IL_008f:  ceq
  IL_0091:  brtrue.s   IL_0028
  IL_0093:  ret
}
")
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)))

            diff1.VerifyIL("C.M", "
{
  // Code size      148 (0x94)
  .maxstack  2
  .locals init (Double V_0,
                Double V_1,
                Double V_2,
                Boolean V_3,
                Double V_4, //i
                Double V_5,
                Double V_6,
                Double V_7,
                Boolean V_8,
                Double V_9) //j
  IL_0000:  nop
  IL_0001:  call       ""Function C.foo() As Double""
  IL_0006:  stloc.0
  IL_0007:  call       ""Function C.foo() As Double""
  IL_000c:  stloc.1
  IL_000d:  call       ""Function C.foo() As Double""
  IL_0012:  stloc.2
  IL_0013:  ldloc.2
  IL_0014:  ldc.r8     0
  IL_001d:  clt.un
  IL_001f:  ldc.i4.0
  IL_0020:  ceq
  IL_0022:  stloc.3
  IL_0023:  ldloc.0
  IL_0024:  stloc.s    V_4
  IL_0026:  br.s       IL_007c
  IL_0028:  call       ""Function C.foo() As Double""
  IL_002d:  stloc.s    V_5
  IL_002f:  call       ""Function C.foo() As Double""
  IL_0034:  stloc.s    V_6
  IL_0036:  call       ""Function C.foo() As Double""
  IL_003b:  stloc.s    V_7
  IL_003d:  ldloc.s    V_7
  IL_003f:  ldc.r8     0
  IL_0048:  clt.un
  IL_004a:  ldc.i4.0
  IL_004b:  ceq
  IL_004d:  stloc.s    V_8
  IL_004f:  ldloc.s    V_5
  IL_0051:  stloc.s    V_9
  IL_0053:  br.s       IL_005c
  IL_0055:  ldloc.s    V_9
  IL_0057:  ldloc.s    V_7
  IL_0059:  add
  IL_005a:  stloc.s    V_9
  IL_005c:  ldloc.s    V_8
  IL_005e:  brtrue.s   IL_006b
  IL_0060:  ldloc.s    V_9
  IL_0062:  ldloc.s    V_6
  IL_0064:  clt.un
  IL_0066:  ldc.i4.0
  IL_0067:  ceq
  IL_0069:  br.s       IL_0074
  IL_006b:  ldloc.s    V_9
  IL_006d:  ldloc.s    V_6
  IL_006f:  cgt.un
  IL_0071:  ldc.i4.0
  IL_0072:  ceq
  IL_0074:  brtrue.s   IL_0055
  IL_0076:  ldloc.s    V_4
  IL_0078:  ldloc.2
  IL_0079:  add
  IL_007a:  stloc.s    V_4
  IL_007c:  ldloc.3
  IL_007d:  brtrue.s   IL_0089
  IL_007f:  ldloc.s    V_4
  IL_0081:  ldloc.1
  IL_0082:  clt.un
  IL_0084:  ldc.i4.0
  IL_0085:  ceq
  IL_0087:  br.s       IL_0091
  IL_0089:  ldloc.s    V_4
  IL_008b:  ldloc.1
  IL_008c:  cgt.un
  IL_008e:  ldc.i4.0
  IL_008f:  ceq
  IL_0091:  brtrue.s   IL_0028
  IL_0093:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsImplicit()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[

option explicit off

Class A(Of T)
End Class
Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.Guid() = nothing
        With x
            Dim z = .ToString
            y = z
        end With
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (Object V_0, //y
                System.Guid() V_1, //x
                System.Guid() V_2, //$W0
                String V_3) //z
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.1
  IL_0003:  nop
  IL_0004:  ldloc.1
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  callvirt   ""Function Object.ToString() As String""
  IL_000c:  stloc.3
  IL_000d:  ldloc.3
  IL_000e:  stloc.0
  IL_000f:  nop
  IL_0010:  ldnull
  IL_0011:  stloc.2
  IL_0012:  ret
}
")

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim edit = New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(edit))

            diff1.VerifyIL("C.M", "
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (Object V_0, //y
                System.Guid() V_1, //x
                System.Guid() V_2, //$W0
                String V_3) //z
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.1
  IL_0003:  nop
  IL_0004:  ldloc.1
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  callvirt   ""Function Object.ToString() As String""
  IL_000c:  stloc.3
  IL_000d:  ldloc.3
  IL_000e:  stloc.0
  IL_000f:  nop
  IL_0010:  ldnull
  IL_0011:  stloc.2
  IL_0012:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsImplicitQualified()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[

option explicit off

Class A(Of T)
End Class

Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.Guid() = nothing

        goto Length         ' this does not declare Length
        Length:             ' this does not declare Length

        dim y = x.Length    ' this does not declare Length
        Length = 5          ' this does 

    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            Dim actualIL0 = testData0.GetMethodData("C.M").GetMethodIL()
            Dim expectedIL0 = "
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (Object V_0, //Length
  System.Guid() V_1, //x
  Integer V_2) //y
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.1
  IL_0003:  br.s       IL_0005
  IL_0005:  nop
  IL_0006:  ldloc.1
  IL_0007:  ldlen
  IL_0008:  conv.i4
  IL_0009:  stloc.2
  IL_000a:  ldc.i4.5
  IL_000b:  box        ""Integer""
  IL_0010:  stloc.0
  IL_0011:  ret
}
"

            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL0, actualIL0)

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim edit = New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(edit))

            diff1.VerifyIL("C.M", "
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (Object V_0, //Length
  System.Guid() V_1, //x
  Integer V_2) //y
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  stloc.1
  IL_0003:  br.s       IL_0005
  IL_0005:  nop
  IL_0006:  ldloc.1
  IL_0007:  ldlen
  IL_0008:  conv.i4
  IL_0009:  stloc.2
  IL_000a:  ldc.i4.5
  IL_000b:  box        ""Integer""
  IL_0010:  stloc.0
  IL_0011:  ret
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsImplicitXmlNs()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[

option explicit off

Imports <xmlns:Length="http:  //roslyn/F">

Class A(Of T)
End Class

Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub M(o As Object)
        dim x as System.Guid() = nothing

        GetXmlNamespace(Length).ToString()    ' this does not declare Length
        dim z as object = GetXmlNamespace(Length)    ' this does not declare Length
        Length = 5          ' this does 

        Dim aa = Length
    End Sub
End Class
]]></file>
                           </compilation>



            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(sources0, XmlReferences, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            testData0.GetMethodData("C.M").VerifyIL("
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (Object V_0, //Length
           System.Guid() V_1, //x
           Object V_2, //z
           Object V_3) //aa
  IL_0000:  nop       
  IL_0001:  ldnull    
  IL_0002:  stloc.1   
  IL_0003:  ldstr      ""http:  //roslyn/F""
  IL_0008:  call       ""Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace""
  IL_000d:  callvirt   ""Function System.Xml.Linq.XNamespace.ToString() As String""
  IL_0012:  pop       
  IL_0013:  ldstr      ""http:  //roslyn/F""
  IL_0018:  call       ""Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace""
  IL_001d:  stloc.2   
  IL_001e:  ldc.i4.5  
  IL_001f:  box        ""Integer""
  IL_0024:  stloc.0   
  IL_0025:  ldloc.0   
  IL_0026:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_002b:  stloc.3   
  IL_002c:  ret       
}
")

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim edit = New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(edit))

            diff1.VerifyIL("C.M", "
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (Object V_0, //Length
           System.Guid() V_1, //x
           Object V_2, //z
           Object V_3) //aa
  IL_0000:  nop       
  IL_0001:  ldnull    
  IL_0002:  stloc.1   
  IL_0003:  ldstr      ""http:  //roslyn/F""
  IL_0008:  call       ""Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace""
  IL_000d:  callvirt   ""Function System.Xml.Linq.XNamespace.ToString() As String""
  IL_0012:  pop       
  IL_0013:  ldstr      ""http:  //roslyn/F""
  IL_0018:  call       ""Function System.Xml.Linq.XNamespace.Get(String) As System.Xml.Linq.XNamespace""
  IL_001d:  stloc.2   
  IL_001e:  ldc.i4.5  
  IL_001f:  box        ""Integer""
  IL_0024:  stloc.0   
  IL_0025:  ldloc.0   
  IL_0026:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_002b:  stloc.3   
  IL_002c:  ret       
}
")
        End Sub

        ''' <summary>
        ''' Local slots must be preserved based on signature.
        ''' </summary>
        <Fact>
        Public Sub PreserveLocalSlotsImplicitNamedArgXml()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Option Explicit Off

Class A(Of T)
End Class

Class C
    Inherits A(Of C)
    Shared Function F() As C
        Return Nothing
    End Function
    Shared Sub F(qq As Object)
    End Sub
    Shared Sub M(o As Object)
        F(qq:=<qq a="qq"></>)        'does not declare qq

        qq = 5
        Dim aa = qq
    End Sub
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(sources0, XmlReferences, TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)

            Dim actualIL0 = testData0.GetMethodData("C.M").GetMethodIL()
            Dim expectedIL0 =
            <![CDATA[
{
  // Code size       88 (0x58)
  .maxstack  3
  .locals init (Object V_0, //qq
  Object V_1, //aa
  System.Xml.Linq.XElement V_2)
  IL_0000:  nop
  IL_0001:  ldstr      "qq"
  IL_0006:  ldstr      ""
  IL_000b:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0010:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0015:  stloc.2
  IL_0016:  ldloc.2
  IL_0017:  ldstr      "a"
  IL_001c:  ldstr      ""
  IL_0021:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0026:  ldstr      "qq"
  IL_002b:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_0030:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0035:  nop
  IL_0036:  ldloc.2
  IL_0037:  ldstr      ""
  IL_003c:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0041:  nop
  IL_0042:  ldloc.2
  IL_0043:  call       "Sub C.F(Object)"
  IL_0048:  nop
  IL_0049:  ldc.i4.5
  IL_004a:  box        "Integer"
  IL_004f:  stloc.0
  IL_0050:  ldloc.0
  IL_0051:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0056:  stloc.1
  IL_0057:  ret
}
]]>.Value

            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL0, actualIL0)

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), testData0.GetMethodData("C.M").EncDebugInfoProvider)

            Dim edit = New SemanticEdit(SemanticEditKind.Update, method0, method1, preserveLocalVariables:=True)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(edit))

            diff1.VerifyIL("C.M", <![CDATA[
{
  // Code size       88 (0x58)
  .maxstack  3
  .locals init (Object V_0, //qq
  Object V_1, //aa
  [unchanged] V_2,
  System.Xml.Linq.XElement V_3)
  IL_0000:  nop
  IL_0001:  ldstr      "qq"
  IL_0006:  ldstr      ""
  IL_000b:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0010:  newobj     "Sub System.Xml.Linq.XElement..ctor(System.Xml.Linq.XName)"
  IL_0015:  stloc.3
  IL_0016:  ldloc.3
  IL_0017:  ldstr      "a"
  IL_001c:  ldstr      ""
  IL_0021:  call       "Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName"
  IL_0026:  ldstr      "qq"
  IL_002b:  newobj     "Sub System.Xml.Linq.XAttribute..ctor(System.Xml.Linq.XName, Object)"
  IL_0030:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0035:  nop
  IL_0036:  ldloc.3
  IL_0037:  ldstr      ""
  IL_003c:  callvirt   "Sub System.Xml.Linq.XContainer.Add(Object)"
  IL_0041:  nop
  IL_0042:  ldloc.3
  IL_0043:  call       "Sub C.F(Object)"
  IL_0048:  nop
  IL_0049:  ldc.i4.5
  IL_004a:  box        "Integer"
  IL_004f:  stloc.0
  IL_0050:  ldloc.0
  IL_0051:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0056:  stloc.1
  IL_0057:  ret
}
]]>.Value)
        End Sub

        <Fact>
        Public Sub AnonymousTypes_Update()
            Dim source0 = MarkedSource("
Class C
    Shared Sub F()
        Dim <N:0>x</N:0> = New With { .A = 1 }
    End Sub
End Class
")
            Dim source1 = MarkedSource("
Class C
    Shared Sub F()
        Dim <N:0>x</N:0> = New With { .A = 2 }
    End Sub
End Class
")
            Dim source2 = MarkedSource("
Class C
    Shared Sub F()
        Dim <N:0>x</N:0> = New With { .A = 3 }
    End Sub
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib(source0.Tree, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)
            v0.VerifyIL("C.F", "
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (VB$AnonymousType_0(Of Integer) V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""Sub VB$AnonymousType_0(Of Integer)..ctor(Integer)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
")
            Dim diff1 = compilation1.EmitDifference(generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.VerifyIL("C.F", "
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (VB$AnonymousType_0(Of Integer) V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""Sub VB$AnonymousType_0(Of Integer)..ctor(Integer)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
")
            ' expect a single TypeRef for System.Object
            Dim md1 = diff1.GetMetadata()
            AssertEx.Equal({"[0x23000002] 0x0000020d.0x0000021a"}, DumpTypeRefs(md1.Reader))

            Dim diff2 = compilation2.EmitDifference(diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            diff2.VerifyIL("C.F", "
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (VB$AnonymousType_0(Of Integer) V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     ""Sub VB$AnonymousType_0(Of Integer)..ctor(Integer)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
")
            ' expect a single TypeRef for System.Object
            Dim md2 = diff2.GetMetadata()
            AssertEx.Equal({"[0x23000003] 0x00000256.0x00000263"}, DumpTypeRefs(md2.Reader))
        End Sub

        <Fact>
        Public Sub AnonymousTypes_UpdateAfterAdd()
            Dim source0 = MarkedSource("
Class C
    Shared Sub F()
    End Sub
End Class
")
            Dim source1 = MarkedSource("
Class C
    Shared Sub F()
        Dim <N:0>x</N:0> = New With { .A = 2 }
    End Sub
End Class
")
            Dim source2 = MarkedSource("
Class C
    Shared Sub F()
        Dim <N:0>x</N:0> = New With { .A = 3 }
    End Sub
End Class
")
            Dim compilation0 = CreateCompilationWithMscorlib(source0.Tree, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim diff1 = compilation1.EmitDifference(generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.VerifyIL("C.F", "
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (VB$AnonymousType_0(Of Integer) V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""Sub VB$AnonymousType_0(Of Integer)..ctor(Integer)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
")
            Dim diff2 = compilation2.EmitDifference(diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            diff2.VerifyIL("C.F", "
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (VB$AnonymousType_0(Of Integer) V_0) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     ""Sub VB$AnonymousType_0(Of Integer)..ctor(Integer)""
  IL_0007:  stloc.0
  IL_0008:  ret
}
")
            ' expect a single TypeRef for System.Object
            Dim md2 = diff2.GetMetadata()
            AssertEx.Equal({"[0x23000003] 0x00000289.0x00000296"}, DumpTypeRefs(md2.Reader))
        End Sub

        Private Shared Iterator Function DumpTypeRefs(reader As MetadataReader) As IEnumerable(Of String)
            For Each typeRefHandle In reader.TypeReferences
                Dim typeRef = reader.GetTypeReference(typeRefHandle)
                Yield $"[0x{MetadataTokens.GetToken(typeRef.ResolutionScope):x8}] 0x{MetadataTokens.GetHeapOffset(typeRef.Namespace):x8}.0x{MetadataTokens.GetHeapOffset(typeRef.Name):x8}"
            Next
        End Function

        <Fact>
        Public Sub AnonymousTypes()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Namespace N
    Class A
        Shared F As Object = New With {.A = 1, .B = 2}
    End Class
End Namespace
Namespace M
    Class B
        Shared Sub M()
            Dim x As New With {.B = 3, .A = 4}
            Dim y = x.A
            Dim z As New With {.C = 5}
        End Sub
    End Class
End Namespace
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Namespace N
    Class A
        Shared F As Object = New With {.A = 1, .B = 2}
    End Class
End Namespace
Namespace M
    Class B
        Shared Sub M()
            Dim x As New With {.B = 3, .A = 4}
            Dim y As New With {.A = x.A}
            Dim z As New With {.C = 5}
        End Sub
    End Class
End Namespace
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0),
                                                                     testData0.GetMethodData("M.B.M").EncDebugInfoProvider)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("M.B.M")
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetTypeDefNames(),
                           "<Module>",
                           "VB$AnonymousType_0`2",
                           "VB$AnonymousType_1`2",
                           "VB$AnonymousType_2`1",
                           "A",
                           "B")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("M.B.M")
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    CheckNames({reader0, reader1}, reader1.GetTypeDefNames(), "VB$AnonymousType_3`1")
                    diff1.VerifyIL("M.B.M", "
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (VB$AnonymousType_1(Of Integer, Integer) V_0, //x
  [int] V_1,
  VB$AnonymousType_2(Of Integer) V_2, //z
  VB$AnonymousType_3(Of Integer) V_3) //y
  IL_0000:  nop
  IL_0001:  ldc.i4.3
  IL_0002:  ldc.i4.4
  IL_0003:  newobj     ""Sub VB$AnonymousType_1(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  callvirt   ""Function VB$AnonymousType_1(Of Integer, Integer).get_A() As Integer""
  IL_000f:  newobj     ""Sub VB$AnonymousType_3(Of Integer)..ctor(Integer)""
  IL_0014:  stloc.3
  IL_0015:  ldc.i4.5
  IL_0016:  newobj     ""Sub VB$AnonymousType_2(Of Integer)..ctor(Integer)""
  IL_001b:  stloc.2
  IL_001c:  ret
}
")
                End Using
            End Using
        End Sub

        ''' <summary>
        ''' Update method with anonymous type that was
        ''' not directly referenced in previous generation.
        ''' </summary>
        <Fact>
        Public Sub AnonymousTypes_SkipGeneration()
            Dim source0 = MarkedSource("
Class A
End Class
Class B
    <N:3>Shared Function F() As Object</N:3>
        Dim <N:0>x</N:0> As New With {.A = 1}
        Return x.A
    End Function

    <N:4>Shared Function G() As Object</N:4>
        Dim <N:1>x</N:1> As Integer = 1
        Return x
    End Function
End Class
")

            Dim source1 = MarkedSource("
Class A
End Class
Class B
    <N:3>Shared Function F() As Object</N:3>
        Dim <N:0>x</N:0> As New With {.A = 1}
        Return x.A
    End Function

    <N:4>Shared Function G() As Object</N:4>
        Dim <N:1>x</N:1> As Integer = 1
        Return x + 1
    End Function
End Class
")
            Dim source2 = MarkedSource("
Class A
End Class
Class B
    <N:3>Shared Function F() As Object</N:3>
        Dim <N:0>x</N:0> As New With {.A = 1}
        Return x.A
    End Function

    <N:4>Shared Function G() As Object</N:4>
        Dim <N:1>x</N:1> As New With {.A = New A()}
        Dim <N:2>y</N:2> As New With {.B = 2}
        Return x.A
    End Function
End Class
")
            Dim source3 = MarkedSource("
Class A
End Class
Class B
    <N:3>Shared Function F() As Object</N:3>
        Dim <N:0>x</N:0> As New With {.A = 1}
        Return x.A
    End Function

    <N:4>Shared Function G() As Object</N:4>
        Dim <N:1>x</N:1> As New With {.A = New A()}
        Dim <N:2>y</N:2> As New With {.B = 3}
        Return y.B
    End Function
End Class
")

            Dim compilation0 = CreateCompilationWithMscorlib(source0.Tree, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)
            Dim compilation3 = compilation2.WithSource(source3.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim method0 = compilation0.GetMember(Of MethodSymbol)("B.G")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("B.G")
            Dim method2 = compilation2.GetMember(Of MethodSymbol)("B.G")
            Dim method3 = compilation3.GetMember(Of MethodSymbol)("B.G")

            Dim reader0 = md0.MetadataReader
            CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "VB$AnonymousType_0`1", "A", "B")

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader
            CheckNames({reader0, reader1}, reader1.GetTypeDefNames()) ' no additional types

            diff1.VerifyIL("B.G", "
{
  // Code size       16 (0x10)
  .maxstack  2
  .locals init (Object V_0, //G
                Integer V_1) //x
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.1
  IL_0003:  ldloc.1
  IL_0004:  ldc.i4.1
  IL_0005:  add.ovf
  IL_0006:  box        ""Integer""
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
}
")
            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method1, method2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            Dim md2 = diff2.GetMetadata()
            Dim reader2 = md2.Reader
            CheckNames({reader0, reader1, reader2}, reader2.GetTypeDefNames(), "VB$AnonymousType_1`1") ' one additional type

            diff2.VerifyIL("B.G", "
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (Object V_0, //G
                [int] V_1,
                VB$AnonymousType_0(Of A) V_2, //x
                VB$AnonymousType_1(Of Integer) V_3) //y
  IL_0000:  nop
  IL_0001:  newobj     ""Sub A..ctor()""
  IL_0006:  newobj     ""Sub VB$AnonymousType_0(Of A)..ctor(A)""
  IL_000b:  stloc.2
  IL_000c:  ldc.i4.2
  IL_000d:  newobj     ""Sub VB$AnonymousType_1(Of Integer)..ctor(Integer)""
  IL_0012:  stloc.3
  IL_0013:  ldloc.2
  IL_0014:  callvirt   ""Function VB$AnonymousType_0(Of A).get_A() As A""
  IL_0019:  stloc.0
  IL_001a:  br.s       IL_001c
  IL_001c:  ldloc.0
  IL_001d:  ret
}
")

            Dim diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method2, method3, GetSyntaxMapFromMarkers(source2, source3), preserveLocalVariables:=True)))

            Dim md3 = diff3.GetMetadata()
            Dim reader3 = md3.Reader
            CheckNames({reader0, reader1, reader2, reader3}, reader3.GetTypeDefNames()) ' no additional types
            diff3.VerifyIL("B.G", "
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Object V_0, //G
                [int] V_1,
                VB$AnonymousType_0(Of A) V_2, //x
                VB$AnonymousType_1(Of Integer) V_3) //y
  IL_0000:  nop
  IL_0001:  newobj     ""Sub A..ctor()""
  IL_0006:  newobj     ""Sub VB$AnonymousType_0(Of A)..ctor(A)""
  IL_000b:  stloc.2
  IL_000c:  ldc.i4.3
  IL_000d:  newobj     ""Sub VB$AnonymousType_1(Of Integer)..ctor(Integer)""
  IL_0012:  stloc.3
  IL_0013:  ldloc.3
  IL_0014:  callvirt   ""Function VB$AnonymousType_1(Of Integer).get_B() As Integer""
  IL_0019:  box        ""Integer""
  IL_001e:  stloc.0
  IL_001f:  br.s       IL_0021
  IL_0021:  ldloc.0
  IL_0022:  ret
}
")
        End Sub

        ''' <summary>
        ''' Update another method (without directly referencing
        ''' anonymous type) after updating method with anonymous type.
        ''' </summary>
        <Fact>
        Public Sub AnonymousTypes_SkipGeneration_2()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Function F() As Object
        Dim x As New With {.A = 1}
        Return x.A
    End Function
    Shared Function G() As Object
        Dim x As Integer = 1
        Return x
    End Function
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Function F() As Object
        Dim x As New With {.A = 2, .B = 3}
        Return x.A
    End Function
    Shared Function G() As Object
        Dim x As Integer = 1
        Return x
    End Function
End Class
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Function F() As Object
        Dim x As New With {.A = 2, .B = 3}
        Return x.A
    End Function
    Shared Function G() As Object
        Dim x As Integer = 1
        Return x + 1
    End Function
End Class
]]></file>
                           </compilation>
            Dim sources3 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Function F() As Object
        Dim x As New With {.A = 2, .B = 3}
        Return x.A
    End Function
    Shared Function G() As Object
        Dim x As New With {.A = DirectCast(Nothing, Object)}
        Dim y As New With {.A = "a"c, .B = "b"c}
        Return x
    End Function
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim compilation2 = compilation1.WithSource(sources2)
            Dim compilation3 = compilation2.WithSource(sources3)

            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim generation0 = EmitBaseline.CreateInitialBaseline(
                    ModuleMetadata.CreateFromImage(bytes0),
                    Function(m)
                        Select Case md0.MetadataReader.GetString(md0.MetadataReader.GetMethodDefinition(m).Name)
                            Case "F" : Return testData0.GetMethodData("C.F").GetEncDebugInfo()
                            Case "G" : Return testData0.GetMethodData("C.G").GetEncDebugInfo()
                        End Select

                        Return Nothing
                    End Function)
                Dim method0F = compilation0.GetMember(Of MethodSymbol)("C.F")
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetTypeDefNames(), "<Module>", "VB$AnonymousType_0`1", "C")
                Dim method1F = compilation1.GetMember(Of MethodSymbol)("C.F")
                Dim method1G = compilation1.GetMember(Of MethodSymbol)("C.G")
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0F, method1F, GetEquivalentNodesMap(method1F, method0F), preserveLocalVariables:=True)))
                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    CheckNames({reader0, reader1}, reader1.GetTypeDefNames(), "VB$AnonymousType_1`2") ' one additional type

                    Dim method2G = compilation2.GetMember(Of MethodSymbol)("C.G")
                    Dim diff2 = compilation2.EmitDifference(
                        diff1.NextGeneration,
                        ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method1G, method2G, GetEquivalentNodesMap(method2G, method1G), preserveLocalVariables:=True)))
                    Using md2 = diff2.GetMetadata()
                        Dim reader2 = md2.Reader
                        CheckNames({reader0, reader1, reader2}, reader2.GetTypeDefNames()) ' no additional types

                        Dim method3G = compilation3.GetMember(Of MethodSymbol)("C.G")
                        Dim diff3 = compilation3.EmitDifference(
                        diff2.NextGeneration,
                        ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method2G, method3G, GetEquivalentNodesMap(method3G, method2G), preserveLocalVariables:=True)))
                        Using md3 = diff3.GetMetadata()
                            Dim reader3 = md3.Reader
                            CheckNames({reader0, reader1, reader2, reader3}, reader3.GetTypeDefNames()) ' no additional types
                        End Using
                    End Using
                End Using
            End Using
        End Sub

        <WorkItem(1292, "https://github.com/dotnet/roslyn/issues/1292")>
        <Fact>
        Public Sub AnonymousTypes_Key()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As New With {.A = 1, .B = 2}
        Dim y As New With {Key .A = 3, .B = 4}
        Dim z As New With {.A = 5, Key .B = 6}
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As New With {.A = 1, .B = 2}
        Dim y As New With {Key .A = 3, Key .B = 4}
        Dim z As New With {Key .A = 5, .B = 6}
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0),
                                                                     testData0.GetMethodData("C.M").EncDebugInfoProvider)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetTypeDefNames(),
                    "<Module>",
                    "VB$AnonymousType_0`2",
                    "VB$AnonymousType_1`2",
                    "VB$AnonymousType_2`2",
                    "C")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))
                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    CheckNames({reader0, reader1}, reader1.GetTypeDefNames(), "VB$AnonymousType_3`2")
                    diff1.VerifyIL("C.M",
"{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (VB$AnonymousType_0(Of Integer, Integer) V_0, //x
                [unchanged] V_1,
                [unchanged] V_2,
                VB$AnonymousType_3(Of Integer, Integer) V_3, //y
                VB$AnonymousType_1(Of Integer, Integer) V_4) //z
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.2
  IL_0003:  newobj     ""Sub VB$AnonymousType_0(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0008:  stloc.0
  IL_0009:  ldc.i4.3
  IL_000a:  ldc.i4.4
  IL_000b:  newobj     ""Sub VB$AnonymousType_3(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0010:  stloc.3
  IL_0011:  ldc.i4.5
  IL_0012:  ldc.i4.6
  IL_0013:  newobj     ""Sub VB$AnonymousType_1(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0018:  stloc.s    V_4
  IL_001a:  ret
}")
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub AnonymousTypes_DifferentCase()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As New With {.A = 1, .B = 2}
        Dim y As New With {.a = 3, .b = 4}
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As New With {.a = 1, .B = 2}
        Dim y As New With {.AB = 3}
        Dim z As New With {.ab = 4}
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources2 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As New With {.a = 1, .B = 2}
        Dim z As New With {.Ab = 5}
    End Sub
End Class
]]></file>
                           </compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim compilation2 = compilation1.WithSource(sources2)
            Dim testData0 = New CompilationTestData()
            Dim bytes0 = compilation0.EmitToArray(testData:=testData0)
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0),
                                                                     testData0.GetMethodData("C.M").EncDebugInfoProvider)
                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
                Dim reader0 = md0.MetadataReader
                CheckNames(reader0, reader0.GetTypeDefNames(),
                    "<Module>",
                    "VB$AnonymousType_0`2",
                    "C")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))
                Using md1 = diff1.GetMetadata()
                    Dim reader1 = md1.Reader
                    CheckNames({reader0, reader1}, reader1.GetTypeDefNames(), "VB$AnonymousType_1`1")
                    diff1.VerifyIL("C.M",
"{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                VB$AnonymousType_0(Of Integer, Integer) V_2, //x
                VB$AnonymousType_1(Of Integer) V_3, //y
                VB$AnonymousType_1(Of Integer) V_4) //z
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.2
  IL_0003:  newobj     ""Sub VB$AnonymousType_0(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0008:  stloc.2
  IL_0009:  ldc.i4.3
  IL_000a:  newobj     ""Sub VB$AnonymousType_1(Of Integer)..ctor(Integer)""
  IL_000f:  stloc.3
  IL_0010:  ldc.i4.4
  IL_0011:  newobj     ""Sub VB$AnonymousType_1(Of Integer)..ctor(Integer)""
  IL_0016:  stloc.s    V_4
  IL_0018:  ret
}")
                    Dim method2 = compilation2.GetMember(Of MethodSymbol)("C.M")
                    Dim diff2 = compilation2.EmitDifference(
                        diff1.NextGeneration,
                        ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method1, method2, GetEquivalentNodesMap(method2, method1), preserveLocalVariables:=True)))
                    Using md2 = diff2.GetMetadata()
                        Dim reader2 = md2.Reader
                        CheckNames({reader0, reader1, reader2}, reader2.GetTypeDefNames())
                        diff2.VerifyIL("C.M",
"{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init ([unchanged] V_0,
                [unchanged] V_1,
                VB$AnonymousType_0(Of Integer, Integer) V_2, //x
                [unchanged] V_3,
                [unchanged] V_4,
                VB$AnonymousType_1(Of Integer) V_5) //z
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.2
  IL_0003:  newobj     ""Sub VB$AnonymousType_0(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0008:  stloc.2
  IL_0009:  ldc.i4.5
  IL_000a:  newobj     ""Sub VB$AnonymousType_1(Of Integer)..ctor(Integer)""
  IL_000f:  stloc.s    V_5
  IL_0011:  ret
}")
                    End Using
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub AnonymousTypes_Nested()
            Dim template = "
Imports System
Imports System.Linq

Class C
    Sub F(args As String())
        Dim <N:4>result</N:4> =
            From a in args
            Let <N:0>x = a.Reverse()</N:0>
            Let <N:1>y = x.Reverse()</N:1>
            <N:2>Where x.SequenceEqual(y)</N:2>
            Select <N:3>Value = a</N:3>, Length = a.Length

        Console.WriteLine(<<VALUE>>)
    End Sub
End Class
"
            Dim source0 = MarkedSource(template.Replace("<<VALUE>>", "0"))
            Dim source1 = MarkedSource(template.Replace("<<VALUE>>", "1"))
            Dim source2 = MarkedSource(template.Replace("<<VALUE>>", "2"))

            Dim compilation0 = CreateCompilationWithMscorlib45({source0.Tree}, {SystemCoreRef}, options:=ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation0.WithSource(source2.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim expectedIL = "
{
  // Code size      175 (0xaf)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable(Of <anonymous type: Key Value As String, Key Length As Integer>) V_0) //result
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldsfld     ""C._Closure$__.$I1-0 As System.Func(Of String, <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_0007:  brfalse.s  IL_0010
  IL_0009:  ldsfld     ""C._Closure$__.$I1-0 As System.Func(Of String, <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_000e:  br.s       IL_0026
  IL_0010:  ldsfld     ""C._Closure$__.$I As C._Closure$__""
  IL_0015:  ldftn      ""Function C._Closure$__._Lambda$__1-0(String) As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>""
  IL_001b:  newobj     ""Sub System.Func(Of String, <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>)..ctor(Object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""C._Closure$__.$I1-0 As System.Func(Of String, <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_0026:  call       ""Function System.Linq.Enumerable.Select(Of String, <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>)(System.Collections.Generic.IEnumerable(Of String), System.Func(Of String, <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_002b:  ldsfld     ""C._Closure$__.$I1-1 As System.Func(Of <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_0030:  brfalse.s  IL_0039
  IL_0032:  ldsfld     ""C._Closure$__.$I1-1 As System.Func(Of <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_0037:  br.s       IL_004f
  IL_0039:  ldsfld     ""C._Closure$__.$I As C._Closure$__""
  IL_003e:  ldftn      ""Function C._Closure$__._Lambda$__1-1(<anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>) As <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>""
  IL_0044:  newobj     ""Sub System.Func(Of <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)..ctor(Object, System.IntPtr)""
  IL_0049:  dup
  IL_004a:  stsfld     ""C._Closure$__.$I1-1 As System.Func(Of <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_004f:  call       ""Function System.Linq.Enumerable.Select(Of <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)(System.Collections.Generic.IEnumerable(Of <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>), System.Func(Of <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_0054:  ldsfld     ""C._Closure$__.$I1-2 As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, Boolean)""
  IL_0059:  brfalse.s  IL_0062
  IL_005b:  ldsfld     ""C._Closure$__.$I1-2 As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, Boolean)""
  IL_0060:  br.s       IL_0078
  IL_0062:  ldsfld     ""C._Closure$__.$I As C._Closure$__""
  IL_0067:  ldftn      ""Function C._Closure$__._Lambda$__1-2(<anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>) As Boolean""
  IL_006d:  newobj     ""Sub System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, Boolean)..ctor(Object, System.IntPtr)""
  IL_0072:  dup
  IL_0073:  stsfld     ""C._Closure$__.$I1-2 As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, Boolean)""
  IL_0078:  call       ""Function System.Linq.Enumerable.Where(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)(System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>), System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, Boolean)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>)""
  IL_007d:  ldsfld     ""C._Closure$__.$I1-3 As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key Value As String, Key Length As Integer>)""
  IL_0082:  brfalse.s  IL_008b
  IL_0084:  ldsfld     ""C._Closure$__.$I1-3 As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key Value As String, Key Length As Integer>)""
  IL_0089:  br.s       IL_00a1
  IL_008b:  ldsfld     ""C._Closure$__.$I As C._Closure$__""
  IL_0090:  ldftn      ""Function C._Closure$__._Lambda$__1-3(<anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>) As <anonymous type: Key Value As String, Key Length As Integer>""
  IL_0096:  newobj     ""Sub System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key Value As String, Key Length As Integer>)..ctor(Object, System.IntPtr)""
  IL_009b:  dup
  IL_009c:  stsfld     ""C._Closure$__.$I1-3 As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key Value As String, Key Length As Integer>)""
  IL_00a1:  call       ""Function System.Linq.Enumerable.Select(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key Value As String, Key Length As Integer>)(System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>), System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key a As String, Key x As System.Collections.Generic.IEnumerable(Of Char)>, Key y As System.Collections.Generic.IEnumerable(Of Char)>, <anonymous type: Key Value As String, Key Length As Integer>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key Value As String, Key Length As Integer>)""
  IL_00a6:  stloc.0
  IL_00a7:  ldc.i4.<<VALUE>>
  IL_00a8:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_00ad:  nop
  IL_00ae:  ret
}"

            v0.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "0"))

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, $I1-1, $I1-2, $I1-3, _Lambda$__1-0, _Lambda$__1-1, _Lambda$__1-2, _Lambda$__1-3}")

            diff1.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "1"))

            Dim diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables:=True)))

            diff2.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I1-0, $I1-1, $I1-2, $I1-3, _Lambda$__1-0, _Lambda$__1-1, _Lambda$__1-2, _Lambda$__1-3}")

            diff2.VerifyIL("C.F", expectedIL.Replace("<<VALUE>>", "2"))
        End Sub

        ''' <summary>
        ''' Should not re-use locals with custom modifiers.
        ''' </summary>
        <Fact(Skip:="TODO")>
        Public Sub LocalType_CustomModifiers()
            ' Equivalent method signature to VB, but
            ' with optional modifiers on locals.
            Dim ilSource = <![CDATA[
.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly '<<GeneratedFileName>>' { }
.class public C
{
  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .method public static object F(class [mscorlib]System.IDisposable d)
  {
    .locals init ([0] object F,
             [1] class C modopt(int32) c,
             [2] class [mscorlib]System.IDisposable modopt(object) VB$Using,
             [3] bool V_3)
    ldnull
    ret
  }
}
]]>.Value
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Class C
    Shared Function F(d As System.IDisposable) As Object
        Dim c As C
        Using d
            c = DirectCast(d, C)
        End Using
        Return c
    End Function
End Class
]]>
                    </file>
                </compilation>
            Dim metadata0 = DirectCast(CompileIL(ilSource, appendDefaultHeader:=False), MetadataImageReference)
            ' Still need a compilation with source for the initial
            ' generation - to get a MethodSymbol and syntax map.
            Dim compilation0 = CreateCompilationWithMscorlib(source, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.Clone()

            Dim moduleMetadata0 = DirectCast(metadata0.GetMetadata(), AssemblyMetadata).GetModules(0)
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.F")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(
                moduleMetadata0,
                Function(m) Nothing)
            Dim testData1 = New CompilationTestData()
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim edit = New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)
            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(edit))

            diff1.VerifyIL("C.F", "
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init ([object] V_0,
           [unchanged] V_1,
           [unchanged] V_2,
           [bool] V_3,
           Object V_4, //F
           C V_5, //c
           System.IDisposable V_6, //VB$Using
           Boolean V_7)
  IL_0000:  nop       
  IL_0001:  nop       
  IL_0002:  ldarg.0   
  IL_0003:  stloc.s    V_6
  .try
  {
    IL_0005:  ldarg.0   
    IL_0006:  castclass  ""C""
    IL_000b:  stloc.s    V_5
    IL_000d:  leave.s    IL_0024
  }
  finally
  {
    IL_000f:  nop       
    IL_0010:  ldloc.s    V_6
    IL_0012:  ldnull    
    IL_0013:  ceq       
    IL_0015:  stloc.s    V_7
    IL_0017:  ldloc.s    V_7
    IL_0019:  brtrue.s   IL_0023
    IL_001b:  ldloc.s    V_6
    IL_001d:  callvirt   ""Sub System.IDisposable.Dispose()""
    IL_0022:  nop       
    IL_0023:  endfinally
  }
  IL_0024:  ldloc.s    V_5
  IL_0026:  stloc.s    V_4
  IL_0028:  br.s       IL_002a
  IL_002a:  ldloc.s    V_4
  IL_002c:  ret       
}
")
        End Sub

        <WorkItem(839414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/839414")>
        <Fact>
        Public Sub Bug839414()
            Dim source0 =
<compilation>
    <file name="a.vb">
Module M
    Function F() As Object
        Static x = 1
        Return x
    End Function
End Module
</file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Module M
    Function F() As Object
        Static x = "2"
        Return x
    End Function
End Module
</file>
</compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)
            Dim bytes0 = compilation0.EmitToArray()
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("M.F")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("M.F")
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
            compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))
        End Sub

        <WorkItem(849649, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849649")>
        <Fact>
        Public Sub Bug849649()
            Dim source0 =
<compilation>
    <file name="a.vb">
Module M
    Sub F()
        Dim x(5) As Integer
        x(3) = 2
    End Sub
End Module
</file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb">
Module M
    Sub F()
        Dim x(5) As Integer
        x(3) = 3
    End Sub
End Module
</file>
</compilation>
            Dim compilation0 = CreateCompilationWithMscorlibAndVBRuntime(source0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)
            Dim bytes0 = compilation0.EmitToArray()
            Dim method0 = compilation0.GetMember(Of MethodSymbol)("M.F")
            Dim method1 = compilation1.GetMember(Of MethodSymbol)("M.F")
            Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)

            Dim diff0 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1, GetEquivalentNodesMap(method1, method0), preserveLocalVariables:=True)))

            diff0.VerifyIL("
{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  nop
  IL_0001:  ldc.i4.6
  IL_0002:  newarr     0x0100000A
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.3
  IL_000a:  ldc.i4.3
  IL_000b:  stelem.i4
  IL_000c:  ret
}
")
        End Sub

#End Region

        <Fact>
        Public Sub SymWriterErrors()
            Dim source0 =
<compilation>
    <file name="a.vb"><![CDATA[
        Class C
        End Class
]]></file>
</compilation>
            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
        Class C
            Sub Main()
            End Sub
        End Class
]]></file>
</compilation>
            Dim compilation0 = CreateCompilationWithMscorlib(source0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(source1)

            ' Verify full metadata contains expected rows.
            Dim bytes0 = compilation0.EmitToArray()
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)

                Dim diff1 = compilation1.EmitDifference(
                            EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider),
                            ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Insert, Nothing, compilation1.GetMember(Of MethodSymbol)("C.Main"))),
                            testData:=New CompilationTestData With {.SymWriterFactory = Function() New MockSymUnmanagedWriter()})

                diff1.EmitResult.Diagnostics.Verify(
                    Diagnostic(ERRID.ERR_PDBWritingFailed).WithArguments(New NotImplementedException().Message))

                Assert.False(diff1.EmitResult.Success)
            End Using
        End Sub

        <WorkItem(1003274, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003274")>
        <Fact>
        Public Sub ConditionalAttribute()
            Const source0 = "
Imports System.Diagnostics

Class C
    Sub M()
        ' Body
    End Sub
            
    <Conditional(""Defined"")>
    Sub N1()
    End Sub

    <Conditional(""Undefined"")>
    Sub N2()
    End Sub
End Class
"
            Dim parseOptions As New VisualBasicParseOptions(preprocessorSymbols:={New KeyValuePair(Of String, Object)("Defined", True)})
            Dim tree0 = VisualBasicSyntaxTree.ParseText(source0, parseOptions)
            Dim tree1 = VisualBasicSyntaxTree.ParseText(source0.Replace("' Body", "N1(): N2()"), parseOptions)
            Dim compilation0 = CreateCompilationWithMscorlib({tree0}, options:=TestOptions.DebugDll)
            Dim compilation1 = compilation0.ReplaceSyntaxTree(tree0, tree1)

            Dim bytes0 = compilation0.EmitToArray()
            Using md0 = ModuleMetadata.CreateFromImage(bytes0)
                Dim reader0 = md0.MetadataReader

                Dim method0 = compilation0.GetMember(Of MethodSymbol)("C.M")
                Dim method1 = compilation1.GetMember(Of MethodSymbol)("C.M")
                Dim generation0 = EmitBaseline.CreateInitialBaseline(ModuleMetadata.CreateFromImage(bytes0), EmptyLocalsProvider)
                Dim diff1 = compilation1.EmitDifference(
                    generation0,
                    ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, method0, method1)))
                diff1.EmitResult.Diagnostics.AssertNoErrors()

                diff1.VerifyIL("C.M", "
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""Sub C.N1()""
  IL_0007:  nop
  IL_0008:  ret
}
")
            End Using
        End Sub

        <Fact>
        Public Sub ReferenceToMemberAddedToAnotherAssembly1()
            Dim sourceA0 = "
Public Class A
End Class
"
            Dim sourceA1 = "
Public Class A
    Public Sub M() 
        System.Console.WriteLine(1)
    End Sub
End Class

Public Class X 
End Class
"
            Dim sourceB0 = "
Public Class B
    Public Shared Sub F()
    End Sub
End Class"
            Dim sourceB1 = "
Public Class B
    Public Shared Sub F() 
        Dim a = New A()
        a.M()
    End Sub
End Class

Public Class Y 
    Inherits X 
End Class
"

            Dim compilationA0 = CreateCompilationWithMscorlib({sourceA0}, options:=TestOptions.DebugDll, assemblyName:="LibA")
            Dim compilationA1 = compilationA0.WithSource(sourceA1)
            Dim compilationB0 = CreateCompilationWithMscorlib({sourceB0}, {compilationA0.ToMetadataReference()}, options:=TestOptions.DebugDll, assemblyName:="LibB")
            Dim compilationB1 = CreateCompilationWithMscorlib({sourceB1}, {compilationA1.ToMetadataReference()}, options:=TestOptions.DebugDll, assemblyName:="LibB")

            Dim bytesA0 = compilationA0.EmitToArray()
            Dim bytesB0 = compilationB0.EmitToArray()
            Dim mdA0 = ModuleMetadata.CreateFromImage(bytesA0)
            Dim mdB0 = ModuleMetadata.CreateFromImage(bytesB0)
            Dim generationA0 = EmitBaseline.CreateInitialBaseline(mdA0, EmptyLocalsProvider)
            Dim generationB0 = EmitBaseline.CreateInitialBaseline(mdB0, EmptyLocalsProvider)
            Dim mA1 = compilationA1.GetMember(Of MethodSymbol)("A.M")
            Dim mX1 = compilationA1.GetMember(Of TypeSymbol)("X")

            Dim allAddedSymbols = New ISymbol() {mA1, mX1}

            Dim diffA1 = compilationA1.EmitDifference(
                generationA0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Insert, Nothing, mA1),
                    New SemanticEdit(SemanticEditKind.Insert, Nothing, mX1)),
                allAddedSymbols)

            diffA1.EmitResult.Diagnostics.Verify()

            Dim diffB1 = compilationB1.EmitDifference(
                generationB0,
                ImmutableArray.Create(
                    New SemanticEdit(SemanticEditKind.Update, compilationB0.GetMember(Of MethodSymbol)("B.F"), compilationB1.GetMember(Of MethodSymbol)("B.F")),
                    New SemanticEdit(SemanticEditKind.Insert, Nothing, compilationB1.GetMember(Of TypeSymbol)("Y"))),
                allAddedSymbols)

            diffB1.EmitResult.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_EncReferenceToAddedMember, "X").WithArguments("X", "LibA").WithLocation(8, 14),
                Diagnostic(ERRID.ERR_EncReferenceToAddedMember, "M").WithArguments("M", "LibA").WithLocation(3, 16))
        End Sub

        <Fact>
        Public Sub ReferenceToMemberAddedToAnotherAssembly2()
            Dim sourceA = "
Public Class A
    Public Sub M()
    End Sub
End Class"
            Dim sourceB0 = "
Public Class B
    Public Shared Sub F() 
        Dim a = New A()
    End Sub
End Class"
            Dim sourceB1 = "
Public Class B
    Public Shared Sub F() 
        Dim a = New A()
        a.M()
    End Sub
End Class"
            Dim sourceB2 = "
Public Class B
    Public Shared Sub F() 
        Dim a = New A()
    End Sub
End Class"

            Dim compilationA = CreateCompilationWithMscorlib({sourceA}, options:=TestOptions.DebugDll, assemblyName:="AssemblyA")
            Dim aRef = compilationA.ToMetadataReference()

            Dim compilationB0 = CreateCompilationWithMscorlib({sourceB0}, {aRef}, options:=TestOptions.DebugDll, assemblyName:="AssemblyB")
            Dim compilationB1 = compilationB0.WithSource(sourceB1)
            Dim compilationB2 = compilationB1.WithSource(sourceB2)

            Dim testDataB0 = New CompilationTestData()
            Dim bytesB0 = compilationB0.EmitToArray(testData:=testDataB0)
            Dim mdB0 = ModuleMetadata.CreateFromImage(bytesB0)
            Dim generationB0 = EmitBaseline.CreateInitialBaseline(mdB0, testDataB0.GetMethodData("B.F").EncDebugInfoProvider())

            Dim f0 = compilationB0.GetMember(Of MethodSymbol)("B.F")
            Dim f1 = compilationB1.GetMember(Of MethodSymbol)("B.F")
            Dim f2 = compilationB2.GetMember(Of MethodSymbol)("B.F")

            Dim diffB1 = compilationB1.EmitDifference(
                generationB0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetEquivalentNodesMap(f1, f0), preserveLocalVariables:=True)))

            diffB1.VerifyIL("B.F", "
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (A V_0) //a
  IL_0000:  nop
  IL_0001:  newobj     ""Sub A..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  callvirt   ""Sub A.M()""
  IL_000d:  nop
  IL_000e:  ret
}
")

            Dim diffB2 = compilationB2.EmitDifference(
               diffB1.NextGeneration,
               ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2, GetEquivalentNodesMap(f2, f1), preserveLocalVariables:=True)))

            diffB2.VerifyIL("B.F", "
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (A V_0) //a
  IL_0000:  nop
  IL_0001:  newobj     ""Sub A..ctor()""
  IL_0006:  stloc.0
  IL_0007:  ret
}
")
        End Sub

        <Fact>
        Public Sub ForStatement()
            Dim source0 = MarkedSource("
Imports System

Class C
    Sub F()
        <N:0><N:1>For a = G(0) To G(1) Step G(2)</N:1>
            Console.WriteLine(1)
        Next</N:0>
    End Sub

    Function G(a As Integer) As Integer
        Return 10
    End Function
End Class
")

            Dim source1 = MarkedSource("
Imports System

Class C
    Sub F()
        <N:0><N:1>For a = G(0) To G(1) Step G(2)</N:1>
            Console.WriteLine(2)
        Next</N:0>
    End Sub

    Function G(a As Integer) As Integer
        Return 10
    End Function
End Class
")

            Dim compilation0 = CreateCompilationWithMscorlib(source0.Tree, {MsvbRef}, ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            v0.VerifyIL("C.F", "
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                Integer V_3) //a
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""Function C.G(Integer) As Integer""
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""Function C.G(Integer) As Integer""
  IL_0010:  stloc.1
  IL_0011:  ldarg.0
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""Function C.G(Integer) As Integer""
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  stloc.3
  IL_001b:  br.s       IL_0028
  IL_001d:  ldc.i4.1
  IL_001e:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0023:  nop
  IL_0024:  ldloc.3
  IL_0025:  ldloc.2
  IL_0026:  add.ovf
  IL_0027:  stloc.3
  IL_0028:  ldloc.2
  IL_0029:  ldc.i4.s   31
  IL_002b:  shr
  IL_002c:  ldloc.3
  IL_002d:  xor
  IL_002e:  ldloc.2
  IL_002f:  ldc.i4.s   31
  IL_0031:  shr
  IL_0032:  ldloc.1
  IL_0033:  xor
  IL_0034:  ble.s      IL_001d
  IL_0036:  ret
}
")
            ' Note that all variables are mapped to their previous slots
            diff1.VerifyIL("C.F", "
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                Integer V_2,
                Integer V_3) //a
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""Function C.G(Integer) As Integer""
  IL_0008:  stloc.0
  IL_0009:  ldarg.0
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""Function C.G(Integer) As Integer""
  IL_0010:  stloc.1
  IL_0011:  ldarg.0
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""Function C.G(Integer) As Integer""
  IL_0018:  stloc.2
  IL_0019:  ldloc.0
  IL_001a:  stloc.3
  IL_001b:  br.s       IL_0028
  IL_001d:  ldc.i4.2
  IL_001e:  call       ""Sub System.Console.WriteLine(Integer)""
  IL_0023:  nop
  IL_0024:  ldloc.3
  IL_0025:  ldloc.2
  IL_0026:  add.ovf
  IL_0027:  stloc.3
  IL_0028:  ldloc.2
  IL_0029:  ldc.i4.s   31
  IL_002b:  shr
  IL_002c:  ldloc.3
  IL_002d:  xor
  IL_002e:  ldloc.2
  IL_002f:  ldc.i4.s   31
  IL_0031:  shr
  IL_0032:  ldloc.1
  IL_0033:  xor
  IL_0034:  ble.s      IL_001d
  IL_0036:  ret
}
")
        End Sub

        <Fact>
        Public Sub ForStatement_LateBound()
            Dim source0 = MarkedSource("
Option Strict On
Public Class C
    Public Shared Sub F()
        Dim <N:0>a</N:0> As Object = 0
        Dim <N:1>b</N:1> As Object = 0
        Dim <N:2>c</N:2> As Object = 0
        Dim <N:3>d</N:3> As Object = 0

        <N:4>For a = b To c Step d
            System.Console.Write(a)
        Next</N:4>
    End Sub
End Class")

            Dim source1 = MarkedSource("
Option Strict On
Public Class C
    Public Shared Sub F()
        Dim <N:0>a</N:0> As Object = 0
        Dim <N:1>b</N:1> As Object = 0
        Dim <N:2>c</N:2> As Object = 0
        Dim <N:3>d</N:3> As Object = 0

        <N:4>For a = b To c Step d
            System.Console.WriteLine(a)
        Next</N:4>
    End Sub
End Class")

            Dim compilation0 = CreateCompilationWithMscorlib(source0.Tree, {MsvbRef}, ComSafeDebugDll)
            Dim compilation1 = compilation0.WithSource(source1.Tree)

            Dim v0 = CompileAndVerify(compilation0)
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")

            Dim diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables:=True)))

            Dim md1 = diff1.GetMetadata()
            Dim reader1 = md1.Reader

            v0.VerifyIL("C.F", "
{
  // Code size       77 (0x4d)
  .maxstack  6
  .locals init (Object V_0, //a
                Object V_1, //b
                Object V_2, //c
                Object V_3, //d
                Object V_4,
                Boolean V_5,
                Boolean V_6)
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  box        ""Integer""
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  box        ""Integer""
  IL_000e:  stloc.1
  IL_000f:  ldc.i4.0
  IL_0010:  box        ""Integer""
  IL_0015:  stloc.2
  IL_0016:  ldc.i4.0
  IL_0017:  box        ""Integer""
  IL_001c:  stloc.3
  IL_001d:  ldloc.0
  IL_001e:  ldloc.1
  IL_001f:  ldloc.2
  IL_0020:  ldloc.3
  IL_0021:  ldloca.s   V_4
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       ""Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean""
  IL_002a:  stloc.s    V_5
  IL_002c:  ldloc.s    V_5
  IL_002e:  brfalse.s  IL_004c
  IL_0030:  ldloc.0
  IL_0031:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0036:  call       ""Sub System.Console.Write(Object)""
  IL_003b:  nop
  IL_003c:  ldloc.0
  IL_003d:  ldloc.s    V_4
  IL_003f:  ldloca.s   V_0
  IL_0041:  call       ""Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean""
  IL_0046:  stloc.s    V_6
  IL_0048:  ldloc.s    V_6
  IL_004a:  brtrue.s   IL_0030
  IL_004c:  ret
}
")
            ' Note that all variables are mapped to their previous slots
            diff1.VerifyIL("C.F", "
{
  // Code size       77 (0x4d)
  .maxstack  6
  .locals init (Object V_0, //a
                Object V_1, //b
                Object V_2, //c
                Object V_3, //d
                Object V_4,
                Boolean V_5,
                Boolean V_6)
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  box        ""Integer""
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  box        ""Integer""
  IL_000e:  stloc.1
  IL_000f:  ldc.i4.0
  IL_0010:  box        ""Integer""
  IL_0015:  stloc.2
  IL_0016:  ldc.i4.0
  IL_0017:  box        ""Integer""
  IL_001c:  stloc.3
  IL_001d:  ldloc.0
  IL_001e:  ldloc.1
  IL_001f:  ldloc.2
  IL_0020:  ldloc.3
  IL_0021:  ldloca.s   V_4
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       ""Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean""
  IL_002a:  stloc.s    V_5
  IL_002c:  ldloc.s    V_5
  IL_002e:  brfalse.s  IL_004c
  IL_0030:  ldloc.0
  IL_0031:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0036:  call       ""Sub System.Console.WriteLine(Object)""
  IL_003b:  nop
  IL_003c:  ldloc.0
  IL_003d:  ldloc.s    V_4
  IL_003f:  ldloca.s   V_0
  IL_0041:  call       ""Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean""
  IL_0046:  stloc.s    V_6
  IL_0048:  ldloc.s    V_6
  IL_004a:  brtrue.s   IL_0030
  IL_004c:  ret
}
")
        End Sub
    End Class
End Namespace
