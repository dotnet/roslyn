' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        <WorkItem(542531, "DevDiv")>
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

    End Class
End Namespace