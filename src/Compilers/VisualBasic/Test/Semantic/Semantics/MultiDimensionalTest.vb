' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class MultiDimensionalTest
        Inherits BasicTestBase

        <Fact>
        Public Sub SimpleTest()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="SimpleTest">
    <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim myArray = New Integer(Number.One, Number.Two) {}'BIND:"New Integer(Number.One, Number.Two) {}"
    End Sub
End Module
Enum Number
    One
    Two
End Enum
    </file>
</compilation>, OutputKind.ConsoleApplication)

            SemanticInfoTypeTest(compilation1, 1, "Integer(*,*)")
            GetDeclareSymbolTest(compilation1, "myArray")
            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="myArray", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="myArray", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="myArray", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

        End Sub

        <Fact>
        Public Sub BadDeclareTest()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="BadDeclareTest">
    <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim m As Boolean = True
        Dim arr7 As Integer(,) = New Integer(m, 4) {}' Invalid
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            SemanticInfoTypeTest(compilation1, 1, "Integer(*,*)")
            GetDeclareSymbolTest(compilation1, "arr7", 2)
            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="arr7", ReadInsideSymbol:="m", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="arr7", WrittenOutsideSymbol:="m",
                                             AlwaysAssignedSymbol:="arr7", DataFlowsInSymbol:="m", DataFlowsOutSymbol:="", index:=2)

        End Sub

        <Fact>
        Public Sub DifferentKindsVarAsIndex()
            ' Use VBRuntime so UBound is defined. Otherwise, replyCounts does not infer its type.
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation name="DifferentKindsVarAsIndex">
    <file name="a.vb">
Option Infer On
Imports Microsoft.VisualBasic.Information
Module Program
    Property prop As Integer
    Sub Main()
        Dim arr1(3, prop) As Integer
        Dim arr2(3, fun()) As Integer
        Dim temp = fun()
        Dim arr3(temp, 1) As Integer
        Dim x() As Integer
        Dim y() As Integer
        Dim replyCounts(,) = New Short(UBound(x, 1), UBound(y, 1)) {}
    End Sub
    Function fun() As Integer
        Return 3
    End Function
    Sub foo(x As Integer)
        Dim arr1(3, x) As Integer
    End Sub
End Module
    </file>
</compilation>)

            SemanticInfoTypeTest(compilation1, 1, "Short(*,*)")
            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="arr1", ReadInsideSymbol:="", ReadOutsideSymbol:="temp, x, y",
                                             WrittenInsideSymbol:="arr1", WrittenOutsideSymbol:="arr2, arr3, replyCounts, temp",
                                             AlwaysAssignedSymbol:="arr1", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=1)

            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="arr2", ReadInsideSymbol:="", ReadOutsideSymbol:="temp, x, y",
                                             WrittenInsideSymbol:="arr2", WrittenOutsideSymbol:="arr1, arr3, replyCounts, temp",
                                             AlwaysAssignedSymbol:="arr2", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)

            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="arr3", ReadInsideSymbol:="temp", ReadOutsideSymbol:="x, y",
                                             WrittenInsideSymbol:="arr3", WrittenOutsideSymbol:="arr1, arr2, replyCounts, temp",
                                             AlwaysAssignedSymbol:="arr3", DataFlowsInSymbol:="temp", DataFlowsOutSymbol:="", index:=4)

            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="replyCounts", ReadInsideSymbol:="x, y", ReadOutsideSymbol:="temp",
                                             WrittenInsideSymbol:="replyCounts", WrittenOutsideSymbol:="arr1, arr2, arr3, temp",
                                             AlwaysAssignedSymbol:="replyCounts", DataFlowsInSymbol:="x, y", DataFlowsOutSymbol:="", index:=7)

            Dim i = 1
            For Each expectedName In {"arr1", "arr2", "temp", "arr3", "x", "y", "replyCounts"}
                GetDeclareSymbolTest(compilation1, expectedName, i)
                i += 1
            Next

        End Sub

        <Fact>
        Public Sub DifferentKindsVarAsIndex_2()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="DifferentKindsVarAsIndex">
    <file name="a.vb">
Option Infer On
Module Program
    Property prop As Integer
    Sub Main()
        Dim y = 1
        Dim arr5(3 + 2, If(True, y + 1, y + 2)) As Integer
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="arr5", ReadInsideSymbol:="y", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="arr5", WrittenOutsideSymbol:="y",
                                             AlwaysAssignedSymbol:="arr5", DataFlowsInSymbol:="y", DataFlowsOutSymbol:="", index:=2)
            GetDeclareSymbolTest(compilation1, "arr5", 2)

        End Sub

        <Fact>
        Public Sub DifferentKindsVarAsIndex_3()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="DifferentKindsVarAsIndex">
    <file name="a.vb">
Imports Microsoft.VisualBasic.Information
Public Class Class1(Of T)
    Sub foo(x As Integer(,))
        Dim y = 1
        Dim arr5(3 + 2, If(True, UBound(x, 1), UBound(arr5, 1))) As Integer
    End Sub
End Class
    </file>
</compilation>, OutputKind.ConsoleApplication)

            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="arr5", ReadInsideSymbol:="arr5, x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="arr5", WrittenOutsideSymbol:="Me, x, y",
                                             AlwaysAssignedSymbol:="arr5", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)
            GetDeclareSymbolTest(compilation1, "arr5", 2)

        End Sub

        <Fact>
        Public Sub DifferentKindsVarAsIndex_4()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="DifferentKindsVarAsIndex">
    <file name="a.vb">
Imports Microsoft.VisualBasic.Information
Public Class Class1
    Sub foo(x As Integer(,))
        Dim myArray As Integer(,) = New Integer(UBound(myArray, 1), UBound(x, 1)) {}
    End Sub
End Class
    </file>
</compilation>, OutputKind.ConsoleApplication)

            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="myArray", ReadInsideSymbol:="myArray, x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="myArray", WrittenOutsideSymbol:="Me, x",
                                             AlwaysAssignedSymbol:="myArray", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=1)
            GetDeclareSymbolTest(compilation1, "myArray")

        End Sub

        <Fact>
        Public Sub MultiDimensionalInArrayAnonymous()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="MultiDimensionalInArrayAnonymous">
    <file name="a.vb">
Option Infer On
Module Program
    Sub Main()
        Dim x As Integer = 1
        Dim a0 = New With {
         Key.b4 = New Integer(1, 2) {}, _
         Key.b5 = New Integer(1, P1(x)) {{1, 2}, {2, 3}},
         Key.b6 = New Integer()() {New Integer(x) {}, New Integer(2) {}},
        }
    End Sub
    Property P1(ByVal x As Integer) As Integer
        Get
            Return x + 5
        End Get
        Set(ByVal Value As Integer)
        End Set
    End Property
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            SemanticInfoTypeTest(compilation1, 1, "Integer(*,*)")

            AnalyzeRegionDataFlowFieldTest(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="a0, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=1)

            AnalyzeRegionDataFlowFieldTest(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="a0, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)

            AnalyzeRegionDataFlowFieldTest(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="a0, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=3)

        End Sub

        <Fact>
        Public Sub GenericAsArrayType()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="GenericAsArrayType">
    <file name="a.vb">
Public Class Class1(Of T)
    Private Sub Foo()
        Dim x As T(,) = New T(1, 2) {}
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
    End Sub
End Class
    </file>
</compilation>, OutputKind.ConsoleApplication)

            For i As Integer = 1 To 2
                SemanticInfoTypeTest(compilation1, i, "T(*,*)")
            Next
            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="Me, Y",
                                             AlwaysAssignedSymbol:="x", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="Y", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="Y", WrittenOutsideSymbol:="Me, x",
                                             AlwaysAssignedSymbol:="Y", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)

        End Sub

        <Fact>
        Public Sub MixedArray()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="MixedArray">
    <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim x = New Integer(,)() {}
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            SemanticInfoTypeTest(compilation1, 1, "Integer(*,*)()")
            GetDeclareSymbolTest(compilation1, "x")
            AnalyzeRegionDataFlowTest(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="x", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

        End Sub

        <WorkItem(542531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542531")>
        <Fact>
        Public Sub AssignMultiDimArrayToArrayWithExplicitBounds()
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Option Explicit Off

Module ArExtInitErr001
    Sub Main()
        Dim a5(1, 1) As Integer
        Dim b5(8, ) As Integer = a5
    End Sub

End Module
    </file>
</compilation>).
VerifyDiagnostics(
            Diagnostic(ERRID.ERR_ExpectedExpression, ""),
            Diagnostic(ERRID.ERR_InitWithExplicitArraySizes, "b5(8, )"))
        End Sub

#Region "HelpMethod"

        Private Function SemanticInfoTypeTest(compilation As VisualBasicCompilation, index As Integer, ParamArray names As String()) As SemanticInfoSummary
            Dim expression = GetSyntaxNode(Of ArrayCreationExpressionSyntax)(compilation, SyntaxKind.ArrayCreationExpression, index)
            Dim model = GetModel(compilation)
            Dim semanticInfo = model.GetSemanticInfoSummary(expression)

            If "<nothing>" = names(0) Then
                Assert.Null(semanticInfo.Type)
            Else
                Assert.Equal(names(0), semanticInfo.Type.ToDisplayString())
            End If

            If names.Count > 1 Then
                Assert.Equal(names(1), semanticInfo.ConvertedType.ToDisplayString())
                Assert.Equal(semanticInfo.ImplicitConversion.Kind, ConversionKind.DelegateRelaxationLevelNone)
            Else
                Assert.Equal(names(0), semanticInfo.ConvertedType.ToDisplayString())
                Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            End If

            Return semanticInfo
        End Function

        Private Function GetDeclareSymbolTest(compilation As VisualBasicCompilation, expectedSymbolName As String, Optional index As Integer = 1) As ISymbol
            Dim node = GetSyntaxNode(Of VariableDeclaratorSyntax)(compilation, SyntaxKind.VariableDeclarator, index).Names.First()
            Dim model = GetModel(compilation)
            Dim symbol = model.GetDeclaredSymbol(node)
            Assert.NotNull(symbol)
            Assert.Equal(expectedSymbolName, symbol.Name)
            Return symbol
        End Function

        Private Function AnalyzeRegionDataFlowTest(compilation As VisualBasicCompilation, VariablesDeclaredSymbol As String,
                         ReadInsideSymbol As String, ReadOutsideSymbol As String, WrittenInsideSymbol As String,
                         WrittenOutsideSymbol As String, AlwaysAssignedSymbol As String,
                         DataFlowsInSymbol As String, DataFlowsOutSymbol As String,
                         Optional index As Integer = 1) As DataFlowAnalysis
            Dim node = DirectCast(GetSyntaxNode(Of VariableDeclaratorSyntax)(compilation, SyntaxKind.VariableDeclarator, index).Parent, StatementSyntax)
            Dim model = GetModel(compilation)
            Dim analyze = model.AnalyzeDataFlow(node, node)
            Assert.Equal(VariablesDeclaredSymbol, GetSymbolNamesSortedAndJoined(analyze.VariablesDeclared))
            Assert.Equal(ReadInsideSymbol, GetSymbolNamesSortedAndJoined(analyze.ReadInside))
            Assert.Equal(ReadOutsideSymbol, GetSymbolNamesSortedAndJoined(analyze.ReadOutside))
            Assert.Equal(WrittenInsideSymbol, GetSymbolNamesSortedAndJoined(analyze.WrittenInside))
            Assert.Equal(WrittenOutsideSymbol, GetSymbolNamesSortedAndJoined(analyze.WrittenOutside))
            Assert.Equal(AlwaysAssignedSymbol, GetSymbolNamesSortedAndJoined(analyze.AlwaysAssigned))
            Assert.Equal(DataFlowsInSymbol, GetSymbolNamesSortedAndJoined(analyze.DataFlowsIn))
            Assert.Equal(DataFlowsOutSymbol, GetSymbolNamesSortedAndJoined(analyze.DataFlowsOut))
            Return analyze
        End Function

        Private Function AnalyzeRegionDataFlowFieldTest(compilation As VisualBasicCompilation, VariablesDeclaredSymbol As String,
                         ReadInsideSymbol As String, ReadOutsideSymbol As String, WrittenInsideSymbol As String,
                         WrittenOutsideSymbol As String, AlwaysAssignedSymbol As String,
                         DataFlowsInSymbol As String, DataFlowsOutSymbol As String,
                         Optional index As Integer = 1) As DataFlowAnalysis
            Dim node = GetSyntaxNode(Of NamedFieldInitializerSyntax)(compilation, SyntaxKind.NamedFieldInitializer, index).Expression
            Dim model = GetModel(compilation)
            Dim analyze = model.AnalyzeDataFlow(node)
            Assert.Equal(VariablesDeclaredSymbol, GetSymbolNamesSortedAndJoined(analyze.VariablesDeclared))
            Assert.Equal(ReadInsideSymbol, GetSymbolNamesSortedAndJoined(analyze.ReadInside))
            Assert.Equal(ReadOutsideSymbol, GetSymbolNamesSortedAndJoined(analyze.ReadOutside))
            Assert.Equal(WrittenInsideSymbol, GetSymbolNamesSortedAndJoined(analyze.WrittenInside))
            Assert.Equal(WrittenOutsideSymbol, GetSymbolNamesSortedAndJoined(analyze.WrittenOutside))
            Assert.Equal(AlwaysAssignedSymbol, GetSymbolNamesSortedAndJoined(analyze.AlwaysAssigned))
            Assert.Equal(DataFlowsInSymbol, GetSymbolNamesSortedAndJoined(analyze.DataFlowsIn))
            Assert.Equal(DataFlowsOutSymbol, GetSymbolNamesSortedAndJoined(analyze.DataFlowsOut))
            Return analyze
        End Function

        Private Function GetSymbolNamesSortedAndJoined(Of T As ISymbol)(symbols As IEnumerable(Of T)) As String
            Return String.Join(", ", symbols.Select(Function(symbol) symbol.Name).OrderBy(Function(name) name))
        End Function

        Private Function GetModel(compilation As VisualBasicCompilation) As SemanticModel
            Dim tree = compilation.SyntaxTrees.First
            Dim model = compilation.GetSemanticModel(tree)
            Return model
        End Function

        Private Function GetSyntaxNode(Of T As VisualBasicSyntaxNode)(compilation As VisualBasicCompilation, syntaxKind As SyntaxKind, index As Integer) As T
            Dim tree = compilation.SyntaxTrees.First
            Dim node = tree.FindNodeOrTokenByKind(syntaxKind, index).AsNode()
            Dim arrayCreationExpression = TryCast(node, T)
            Return arrayCreationExpression
        End Function

#End Region

        Private Shared arraysOfRank1IlSource As String =
        <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance float64[0...] Test1() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test1"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
                ldc.i4.0
                ldc.i4.1
                newobj instance void float64[...]::.ctor(int32, int32)
                dup
                ldc.i4.0
                ldc.r8 -100
                call instance void float64[...]::Set(int32, float64)
      IL_000a:  ret
    } // end of method Test::Test1

    .method public hidebysig newslot virtual 
            instance float64 Test2(float64[0...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  2
      IL_0000:  ldstr      "Test2"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
                ldarg.1
                ldc.i4.0
                call instance float64 float64[...]::Get(int32)
      IL_000a:  ret
    } // end of method Test::Test2

    .method public hidebysig newslot virtual 
            instance void Test3(float64[0...] x) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  2
      IL_000a:  ret
    } // end of method Test::Test3

    .method public hidebysig static void  M1<T>(!!T[0...] a) cil managed
    {
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static void  M2<T>(!!T[] a, !!T[0...] b) cil managed
    {
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M2

} // end of class Test
]]>.Value


        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_GetElement()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        System.Console.WriteLine(t.Test1()(0))
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
-100
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000a:  ldc.i4.0
  IL_000b:  call       "Double(*).Get"
  IL_0010:  call       "Sub System.Console.WriteLine(Double)"
  IL_0015:  ret
}
]]>)
        End Sub


        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_SetElement()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a = t.Test1()
        a(0) = 123
        System.Console.WriteLine(t.Test2(a))
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(Compilation, expectedOutput:=
            <![CDATA[
Test1
Test2
123
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (Double(*) V_0) //a
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  dup
  IL_0006:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.r8     123
  IL_0017:  call       "Double(*).Set"
  IL_001c:  ldloc.0
  IL_001d:  callvirt   "Function Test.Test2(Double(*)) As Double"
  IL_0022:  call       "Sub System.Console.WriteLine(Double)"
  IL_0027:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_ElementAddress()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a = t.Test1()
        TestRef(a(0))
        System.Console.WriteLine(t.Test2(a))
    End Sub

    Shared Sub TestRef(ByRef val As Double)
        val = 123
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(Compilation, expectedOutput:=
            <![CDATA[
Test1
Test2
123
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (Double(*) V_0) //a
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  dup
  IL_0006:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  call       "Double(*).Address"
  IL_0013:  call       "Sub C.TestRef(ByRef Double)"
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Function Test.Test2(Double(*)) As Double"
  IL_001e:  call       "Sub System.Console.WriteLine(Double)"
  IL_0023:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_Overriding01()

            Dim source =
<compilation>
    <file name="a.vb">
class C 
    Inherits Test
    public overrides Function Test1() As double()
        return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(
<expected>
BC30437: 'Public Overrides Function Test1() As Double()' cannot override 'Public Overridable Overloads Function Test1() As Double(*)' because they differ by their return types.
    public overrides Function Test1() As double()
                              ~~~~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_Overriding02()

            Dim source =
<compilation>
    <file name="a.vb">
class C 
    Inherits Test
    public overrides Function Test2(x As double()) As Double
        return x(0)
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(
<expected>
BC30284: function 'Test2' cannot be declared 'Overrides' because it does not override a function in a base class.
    public overrides Function Test2(x As double()) As Double
                              ~~~~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_ArrayConversions()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a1 As double() = t.Test1()
        Dim a2 As double()= CType(t.Test1(), double())
        Dim a3 As System.Collections.Generic.IList(Of Double) = t.Test1()
        Dim a4 As double() = Nothing
        t.Test2(a4)
        Dim a5 = DirectCast(t.Test1(), System.Collections.Generic.IList(Of Double))
        Dim ilist As System.Collections.Generic.IList(Of Double) = new double () {}
        Dim mdarray = t.Test1()
        mdarray = ilist
        mdarray = t.Test1()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            compilation.AssertTheseDiagnostics(
<expected>
BC30311: Value of type 'Double(*)' cannot be converted to 'Double()'.
        Dim a1 As double() = t.Test1()
                             ~~~~~~~~~
BC30311: Value of type 'Double(*)' cannot be converted to 'Double()'.
        Dim a2 As double()= CType(t.Test1(), double())
                                  ~~~~~~~~~
BC30311: Value of type 'Double(*)' cannot be converted to 'IList(Of Double)'.
        Dim a3 As System.Collections.Generic.IList(Of Double) = t.Test1()
                                                                ~~~~~~~~~
BC30311: Value of type 'Double()' cannot be converted to 'Double(*)'.
        t.Test2(a4)
                ~~
BC30311: Value of type 'Double(*)' cannot be converted to 'IList(Of Double)'.
        Dim a5 = DirectCast(t.Test1(), System.Collections.Generic.IList(Of Double))
                            ~~~~~~~~~
BC30311: Value of type 'IList(Of Double)' cannot be converted to 'Double(*)'.
        mdarray = ilist
                  ~~~~~
</expected>
            )

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.Off))
            compilation.AssertTheseDiagnostics(
<expected>
BC30311: Value of type 'Double(*)' cannot be converted to 'Double()'.
        Dim a1 As double() = t.Test1()
                             ~~~~~~~~~
BC30311: Value of type 'Double(*)' cannot be converted to 'Double()'.
        Dim a2 As double()= CType(t.Test1(), double())
                                  ~~~~~~~~~
BC30311: Value of type 'Double(*)' cannot be converted to 'IList(Of Double)'.
        Dim a3 As System.Collections.Generic.IList(Of Double) = t.Test1()
                                                                ~~~~~~~~~
BC30311: Value of type 'Double()' cannot be converted to 'Double(*)'.
        t.Test2(a4)
                ~~
BC30311: Value of type 'Double(*)' cannot be converted to 'IList(Of Double)'.
        Dim a5 = DirectCast(t.Test1(), System.Collections.Generic.IList(Of Double))
                            ~~~~~~~~~
BC30311: Value of type 'IList(Of Double)' cannot be converted to 'Double(*)'.
        mdarray = ilist
                  ~~~~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_StringConversions()
            Dim ilSource As String =
            <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance char[0...] Test1() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test1"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
                ldc.i4.0
                ldc.i4.1
                newobj instance void char[...]::.ctor(int32, int32)
      IL_000a:  ret
    } // end of method Test::Test1

    .method public hidebysig newslot virtual 
            instance void Test2(char[0...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  2
      IL_0000:  ldstr      "Test2"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } // end of method Test::Test2
} // end of class Test
]]>.Value


            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a1 As String = t.Test1()
        Dim a2 As String= CType(t.Test1(), String)
        Dim a4 As String = Nothing
        t.Test2(a4)
        Dim mdarray = t.Test1()
        mdarray = a4
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            compilation.AssertTheseDiagnostics(
<expected>
BC30311: Value of type 'Char(*)' cannot be converted to 'String'.
        Dim a1 As String = t.Test1()
                           ~~~~~~~~~
BC30311: Value of type 'Char(*)' cannot be converted to 'String'.
        Dim a2 As String= CType(t.Test1(), String)
                                ~~~~~~~~~
BC30311: Value of type 'String' cannot be converted to 'Char(*)'.
        t.Test2(a4)
                ~~
BC30311: Value of type 'String' cannot be converted to 'Char(*)'.
        mdarray = a4
                  ~~
</expected>
            )

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.Off))
            compilation.AssertTheseDiagnostics(
<expected>
BC30311: Value of type 'Char(*)' cannot be converted to 'String'.
        Dim a1 As String = t.Test1()
                           ~~~~~~~~~
BC30311: Value of type 'Char(*)' cannot be converted to 'String'.
        Dim a2 As String= CType(t.Test1(), String)
                                ~~~~~~~~~
BC30311: Value of type 'String' cannot be converted to 'Char(*)'.
        t.Test2(a4)
                ~~
BC30311: Value of type 'String' cannot be converted to 'Char(*)'.
        mdarray = a4
                  ~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_TypeArgumentInference01()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim md = t.Test1()
        Dim sz = new double () {}
        
        M1(sz)
        M1(md)
        M2(sz, sz)
        M2(md, md)
        M2(sz, md)
        M2(md, sz)
        M3(sz)
        M3(md)

        Test.M1(sz)
        Test.M1(md)
        Test.M2(sz, sz)
        Test.M2(md, md)
        Test.M2(sz, md)
        Test.M2(md, sz)
    End Sub

    Shared Sub M1(of T)(a As T ())
    End Sub
    Shared Sub M2(of T)(a As T, b As T)
    End Sub
    Shared Sub M3(of T)(a As System.Collections.Generic.IList(Of T))
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(
<expected>
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Sub M1(Of T)(a As T())' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        M1(md)
        ~~
BC36657: Data type(s) of the type parameter(s) in method 'Public Shared Sub M2(Of T)(a As T, b As T)' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        M2(sz, md)
        ~~
BC36657: Data type(s) of the type parameter(s) in method 'Public Shared Sub M2(Of T)(a As T, b As T)' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        M2(md, sz)
        ~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Sub M3(Of T)(a As IList(Of T))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        M3(md)
        ~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Overloads Sub M1(Of T)(a As T(*))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test.M1(sz)
             ~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Overloads Sub M2(Of T)(a As T(), b As T(*))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test.M2(sz, sz)
             ~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Overloads Sub M2(Of T)(a As T(), b As T(*))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test.M2(md, md)
             ~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Overloads Sub M2(Of T)(a As T(), b As T(*))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test.M2(md, sz)
             ~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_TypeArgumentInference02()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim md = t.Test1()
        Dim sz = new double () {}
        
        M2(md, md)

        Test.M1(md)
        Test.M2(sz, md)
    End Sub

    Shared Sub M2(Of T)(a As T, b As T)
        System.Console.WriteLine(GetType(T))
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
System.Double[*]
System.Double
System.Double
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_TypeArgumentInference03()


            Dim ilSource As String =
            <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig static void  M1<T>(!!T[0...][] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static void  M2<T>(!!T[][0...] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static void  M3<T>(!!T[0...][0...] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static float64[0...][] Test1() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldnull
      IL_000a:  ret
    } // end of method Test::Test1

    .method public hidebysig static float64[][0...] Test2() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldnull
      IL_000a:  ret
    } // end of method Test::Test2
} // end of class Test
]]>.Value

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Test.M1(new Double()() {})
        Test.M2(new Double()() {})
        Test.M3(new Double()() {})

        Test.M2(Test.Test1())
        Test.M3(Test.Test1())

        Test.M3(Test.Test2())
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource, options:=TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(
<expected>
BC32050: Type parameter 'T' for 'Public Shared Overloads Sub M1(Of T)(ParamArray a As T()(*))' cannot be inferred.
        Test.M1(new Double()() {})
             ~~
BC32050: Type parameter 'T' for 'Public Shared Overloads Sub M2(Of T)(ParamArray a As T(*)())' cannot be inferred.
        Test.M2(new Double()() {})
             ~~
BC32050: Type parameter 'T' for 'Public Shared Overloads Sub M3(Of T)(ParamArray a As T(*)(*))' cannot be inferred.
        Test.M3(new Double()() {})
             ~~
BC32050: Type parameter 'T' for 'Public Shared Overloads Sub M2(Of T)(ParamArray a As T(*)())' cannot be inferred.
        Test.M2(Test.Test1())
             ~~
BC32050: Type parameter 'T' for 'Public Shared Overloads Sub M3(Of T)(ParamArray a As T(*)(*))' cannot be inferred.
        Test.M3(Test.Test1())
             ~~
BC32050: Type parameter 'T' for 'Public Shared Overloads Sub M3(Of T)(ParamArray a As T(*)(*))' cannot be inferred.
        Test.M3(Test.Test2())
             ~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_TypeArgumentInference04()

            Dim ilSource As String =
            <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig static void  M1<T>(!!T[0...][] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static void  M2<T>(!!T[][0...] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static float64[0...][] Test1() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldnull
      IL_000a:  ret
    } // end of method Test::Test1

    .method public hidebysig static float64[][0...] Test2() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldnull
      IL_000a:  ret
    } // end of method Test::Test2
} // end of class Test
]]>.Value

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Test.M1(Test.Test1())

        Test.M1(Test.Test2())
        Test.M2(Test.Test2())
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
System.Double
System.Double[]
System.Double
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_ForEach()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        for each d in t.Test1()
            System.Console.WriteLine(d)
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe, includeVbRuntime:=True)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
-100
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  1
  .locals init (System.Collections.IEnumerator V_0)
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000a:  callvirt   "Function System.Array.GetEnumerator() As System.Collections.IEnumerator"
  IL_000f:  stloc.0
  IL_0010:  br.s       IL_0022
  IL_0012:  ldloc.0
  IL_0013:  callvirt   "Function System.Collections.IEnumerator.get_Current() As Object"
  IL_0018:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToDouble(Object) As Double"
  IL_001d:  call       "Sub System.Console.WriteLine(Double)"
  IL_0022:  ldloc.0
  IL_0023:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
  IL_0028:  brtrue.s   IL_0012
  IL_002a:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_Length()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        System.Console.WriteLine(t.Test1().Length)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
1
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000a:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_000f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0014:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_LongLength()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        System.Console.WriteLine(t.Test1().LongLength)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
1
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000a:  callvirt   "Function System.Array.get_LongLength() As Long"
  IL_000f:  call       "Sub System.Console.WriteLine(Long)"
  IL_0014:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_ParamArray()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim d as double = 1.1
        t.Test3(d)
        t.Test3(new double () { d })
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(
<expected>
BC31092: ParamArray parameters must have an array type.
        t.Test3(d)
          ~~~~~
BC31092: ParamArray parameters must have an array type.
        t.Test3(new double () { d })
          ~~~~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_Redim01()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a = t.Test1()
        System.Console.WriteLine(a.GetType())
        System.Console.WriteLine(a.Length)
        Redim a(1)
        System.Console.WriteLine(a.GetType())
        System.Console.WriteLine(a.Length)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
System.Double[]
1
System.Double[]
2
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  2
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000a:  dup
  IL_000b:  callvirt   "Function Object.GetType() As System.Type"
  IL_0010:  call       "Sub System.Console.WriteLine(Object)"
  IL_0015:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_001a:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001f:  ldc.i4.2
  IL_0020:  newobj     "Double(*)..ctor"
  IL_0025:  dup
  IL_0026:  callvirt   "Function Object.GetType() As System.Type"
  IL_002b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0030:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_0035:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003a:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_Redim02()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a = t.Test1()
        System.Console.WriteLine(a.GetType())
        System.Console.WriteLine(a.Length)
        System.Console.WriteLine(a(0))
        Redim Preserve a(1)
        System.Console.WriteLine(a.GetType())
        System.Console.WriteLine(a.Length)
        System.Console.WriteLine(a(0))
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe, includeVbRuntime:=True)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
System.Double[]
1
-100
System.Double[]
2
-100
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       94 (0x5e)
  .maxstack  3
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000a:  dup
  IL_000b:  callvirt   "Function Object.GetType() As System.Type"
  IL_0010:  call       "Sub System.Console.WriteLine(Object)"
  IL_0015:  dup
  IL_0016:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_001b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0020:  dup
  IL_0021:  ldc.i4.0
  IL_0022:  call       "Double(*).Get"
  IL_0027:  call       "Sub System.Console.WriteLine(Double)"
  IL_002c:  ldc.i4.2
  IL_002d:  newobj     "Double(*)..ctor"
  IL_0032:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_0037:  castclass  "Double(*)"
  IL_003c:  dup
  IL_003d:  callvirt   "Function Object.GetType() As System.Type"
  IL_0042:  call       "Sub System.Console.WriteLine(Object)"
  IL_0047:  dup
  IL_0048:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_004d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0052:  ldc.i4.0
  IL_0053:  call       "Double(*).Get"
  IL_0058:  call       "Sub System.Console.WriteLine(Double)"
  IL_005d:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_Redim03()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a = t.Test1()
        Redim a(1, 2)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(
<expected>
BC30415: 'ReDim' cannot change the number of dimensions of an array.
        Redim a(1, 2)
              ~~~~~~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_Literals01()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a = t.Test1()
        a = {0, 1}
        Print(a)
    End Sub

    Shared Sub Print(a as System.Array)
        for each d in a
            System.Console.WriteLine(d)
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe, includeVbRuntime:=True)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
0
1
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  4
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000a:  pop
  IL_000b:  ldc.i4.2
  IL_000c:  newobj     "Double(*)..ctor"
  IL_0011:  dup
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.r8     1
  IL_001c:  call       "Double(*).Set"
  IL_0021:  call       "Sub C.Print(System.Array)"
  IL_0026:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_Literals02()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a = t.Test1()
        a = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 , 11, 12, 13, 14}
        Print(a)
    End Sub

    Shared Sub Print(a as System.Array)
        for each d in a
            System.Console.WriteLine(d)
        Next
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe, includeVbRuntime:=True)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
0
1
2
3
4
5
6
7
8
9
10
11
12
13
14
]]>)

            verifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  newobj     "Sub Test..ctor()"
  IL_0005:  callvirt   "Function Test.Test1() As Double(*)"
  IL_000a:  pop
  IL_000b:  ldc.i4.s   15
  IL_000d:  newobj     "Double(*)..ctor"
  IL_0012:  dup
  IL_0013:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=120 <PrivateImplementationDetails>.198EB999C0C49843F4C649E4F9C8292C86A60DC7"
  IL_0018:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_001d:  call       "Sub C.Print(System.Array)"
  IL_0022:  ret
}
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_Literals03()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Dim t = new Test()
        Dim a = t.Test1()
        a = ({1})
        a = ({1.0})
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseDll)
            compilation.AssertTheseDiagnostics(
<expected>
BC30311: Value of type 'Integer()' cannot be converted to 'Double(*)'.
        a = ({1})
            ~~~~~
BC30311: Value of type 'Double()' cannot be converted to 'Double(*)'.
        a = ({1.0})
            ~~~~~~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_Literals04()

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Test.M1({1})
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, arraysOfRank1IlSource, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
System.Int32
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_Literals05()

            Dim ilSource As String =
            <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig static void  M1<T>(!!T[0...] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M1
} // end of class Test
]]>.Value

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Test.M1({1})
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
System.Int32
]]>)
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <Fact>
        Public Sub ArraysOfRank1_Literals06()

            Dim ilSource As String =
            <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig static void  M1<T>(!!T[0...][] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static void  M2<T>(!!T[][0...] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static void  M3<T>(!!T[0...][0...] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      IL_0011:  ret
    } // end of method M1
} // end of class Test
]]>.Value

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Test.M1({({1})})
        Test.M2({({1})})
        Test.M3({({1})})
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource, options:=TestOptions.ReleaseExe)
            compilation.AssertTheseDiagnostics(
<expected>
BC32050: Type parameter 'T' for 'Public Shared Overloads Sub M3(Of T)(ParamArray a As T(*)(*))' cannot be inferred.
        Test.M3({({1})})
             ~~
</expected>
            )
        End Sub

        <WorkItem(1211526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1211526"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub ArraysOfRank1_Literals07()

            Dim ilSource As String =
            <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig static void  M1<T>(!!T[0...][] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static void  M2<T>(!!T[][0...] a) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M1
} // end of class Test
]]>.Value

            Dim source =
<compilation>
    <file name="a.vb">
class C
    Shared Sub Main()
        Test.M1({({-1})})
        Test.M2({({-1})})
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource, options:=TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
System.Int32[]
System.Int32
]]>)
        End Sub

        <WorkItem(4954, "https://github.com/dotnet/roslyn/issues/4954")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub SizesAndLowerBounds_01()

            Dim ilSource As String =
            <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance float64[,] Test1() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test1"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[...,] Test2() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test2"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[...,...] Test3() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test3"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,] Test4() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test4"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,...] Test5() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test5"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,5] Test6() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test6"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,2...] Test7() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test7"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,2...8] Test8() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test8"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,] Test9() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test9"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,...] Test10() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test10"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,5] Test11() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test11"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,2...] Test12() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test12"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,2...8] Test13() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test13"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...,] Test14() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test14"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...,...] Test15() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test15"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...,2...] Test16() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test16"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 
} // end of class Test
]]>.Value

            Dim source =
<compilation>
    <file name="a.vb">
class C 
    Inherits Test

    Shared Sub Main()
        Dim a As double(,)

        Dim t = new Test()
        a = t.Test1()
        a = t.Test2()
        a = t.Test3()
        a = t.Test4()
        a = t.Test5()
        a = t.Test6()
        a = t.Test7()
        a = t.Test8()
        a = t.Test9()
        a = t.Test10()
        a = t.Test11()
        a = t.Test12()
        a = t.Test13()
        a = t.Test14()
        a = t.Test15()
        a = t.Test16()

        t = new C()
        a = t.Test1()
        a = t.Test2()
        a = t.Test3()
        a = t.Test4()
        a = t.Test5()
        a = t.Test6()
        a = t.Test7()
        a = t.Test8()
        a = t.Test9()
        a = t.Test10()
        a = t.Test11()
        a = t.Test12()
        a = t.Test13()
        a = t.Test14()
        a = t.Test15()
        a = t.Test16()
    End Sub

    public overrides Function Test1() As Double(,)
        System.Console.WriteLine("Overriden 1")
        return Nothing
    End Function
    public overrides Function Test2() As Double(,)
        System.Console.WriteLine("Overriden 2")
        return Nothing
    End Function
    public overrides Function Test3() As Double(,)
        System.Console.WriteLine("Overriden 3")
        return Nothing
    End Function
    public overrides Function Test4() As Double(,)
        System.Console.WriteLine("Overriden 4")
        return Nothing
    End Function
    public overrides Function Test5() As Double(,)
        System.Console.WriteLine("Overriden 5")
        return Nothing
    End Function
    public overrides Function Test6() As Double(,)
        System.Console.WriteLine("Overriden 6")
        return Nothing
    End Function
    public overrides Function Test7() As Double(,)
        System.Console.WriteLine("Overriden 7")
        return Nothing
    End Function
    public overrides Function Test8() As Double(,)
        System.Console.WriteLine("Overriden 8")
        return Nothing
    End Function
    public overrides Function Test9() As Double(,)
        System.Console.WriteLine("Overriden 9")
        return Nothing
    End Function
    public overrides Function Test10() As Double(,)
        System.Console.WriteLine("Overriden 10")
        return Nothing
    End Function
    public overrides Function Test11() As Double(,)
        System.Console.WriteLine("Overriden 11")
        return Nothing
    End Function
    public overrides Function Test12() As Double(,)
        System.Console.WriteLine("Overriden 12")
        return Nothing
    End Function
    public overrides Function Test13() As Double(,)
        System.Console.WriteLine("Overriden 13")
        return Nothing
    End Function
    public overrides Function Test14() As Double(,)
        System.Console.WriteLine("Overriden 14")
        return Nothing
    End Function
    public overrides Function Test15() As Double(,)
        System.Console.WriteLine("Overriden 15")
        return Nothing
    End Function
    public overrides Function Test16() As Double(,)
        System.Console.WriteLine("Overriden 16")
        return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[Test1
Test2
Test3
Test4
Test5
Test6
Test7
Test8
Test9
Test10
Test11
Test12
Test13
Test14
Test15
Test16
Overriden 1
Overriden 2
Overriden 3
Overriden 4
Overriden 5
Overriden 6
Overriden 7
Overriden 8
Overriden 9
Overriden 10
Overriden 11
Overriden 12
Overriden 13
Overriden 14
Overriden 15
Overriden 16
]]>)
        End Sub

        <WorkItem(4954, "https://github.com/dotnet/roslyn/issues/4954")>
        <ClrOnlyFact(ClrOnlyReason.Ilasm)>
        Public Sub SizesAndLowerBounds_02()

            Dim ilSource As String =
            <![CDATA[
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance void Test1(float64[,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test1"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test2(float64[...,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test2"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test3(float64[...,...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test3"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test4(float64[5,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test4"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test5(float64[5,...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test5"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test6(float64[5,5] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test6"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test7(float64[5,2...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test7"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test8(float64[5,2...8] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test8"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test9(float64[1...5,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test9"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test10(float64[1...5,...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test10"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test11(float64[1...5,5] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test11"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test12(float64[1...5,2...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test12"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test13(float64[1...5,2...8] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test13"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test14(float64[1...,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test14"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test15(float64[1...,...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test15"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test16(float64[1...,2...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      "Test16"
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 
} // end of class Test
]]>.Value

            Dim source =
<compilation>
    <file name="a.vb">
Class C 
    Inherits Test

    Shared Sub Main()
    
        Dim a As double(,) = New Double(,) {}

        Dim t = new Test()
        t.Test1(a)
        t.Test2(a)
        t.Test3(a)
        t.Test4(a)
        t.Test5(a)
        t.Test6(a)
        t.Test7(a)
        t.Test8(a)
        t.Test9(a)
        t.Test10(a)
        t.Test11(a)
        t.Test12(a)
        t.Test13(a)
        t.Test14(a)
        t.Test15(a)
        t.Test16(a)

        t = new C()
        t.Test1(a)
        t.Test2(a)
        t.Test3(a)
        t.Test4(a)
        t.Test5(a)
        t.Test6(a)
        t.Test7(a)
        t.Test8(a)
        t.Test9(a)
        t.Test10(a)
        t.Test11(a)
        t.Test12(a)
        t.Test13(a)
        t.Test14(a)
        t.Test15(a)
        t.Test16(a)
    End Sub

    public overrides Sub Test1(x As double(,))
        System.Console.WriteLine("Overriden 1")
    End Sub
    public overrides Sub Test2(x As double(,))
        System.Console.WriteLine("Overriden 2")
    End Sub
    public overrides Sub Test3(x As double(,))
        System.Console.WriteLine("Overriden 3")
    End Sub
    public overrides Sub Test4(x As double(,))
        System.Console.WriteLine("Overriden 4")
    End Sub
    public overrides Sub Test5(x As double(,))
        System.Console.WriteLine("Overriden 5")
    End Sub
    public overrides Sub Test6(x As double(,))
        System.Console.WriteLine("Overriden 6")
    End Sub
    public overrides Sub Test7(x As double(,))
        System.Console.WriteLine("Overriden 7")
    End Sub
    public overrides Sub Test8(x As double(,))
        System.Console.WriteLine("Overriden 8")
    End Sub
    public overrides Sub Test9(x As double(,))
        System.Console.WriteLine("Overriden 9")
    End Sub
    public overrides Sub Test10(x As double(,))
        System.Console.WriteLine("Overriden 10")
    End Sub
    public overrides Sub Test11(x As double(,))
        System.Console.WriteLine("Overriden 11")
    End Sub
    public overrides Sub Test12(x As double(,))
        System.Console.WriteLine("Overriden 12")
    End Sub
    public overrides Sub Test13(x As double(,))
        System.Console.WriteLine("Overriden 13")
    End Sub
    public overrides Sub Test14(x As double(,))
        System.Console.WriteLine("Overriden 14")
    End Sub
    public overrides Sub Test15(x As double(,))
        System.Console.WriteLine("Overriden 15")
    End Sub
    public overrides Sub Test16(x As double(,))
        System.Console.WriteLine("Overriden 16")
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, ilSource, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[Test1
Test2
Test3
Test4
Test5
Test6
Test7
Test8
Test9
Test10
Test11
Test12
Test13
Test14
Test15
Test16
Overriden 1
Overriden 2
Overriden 3
Overriden 4
Overriden 5
Overriden 6
Overriden 7
Overriden 8
Overriden 9
Overriden 10
Overriden 11
Overriden 12
Overriden 13
Overriden 14
Overriden 15
Overriden 16
]]>)
        End Sub

    End Class
End Namespace
