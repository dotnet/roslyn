' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class IFOperatorTest
        Inherits BasicTestBase
        ' Every argument could not be empty
        <Fact>
        Public Sub ArgumentCouldNotEmpty()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ArgumentCouldNotEmpty">
    <file name="a.vb">
Option Infer Off
Module Program
    Sub Main(args As String())
        Dim X = 1
        Dim Y = 1
        Dim S = If(True, , Y = Y + 1)
        S = If(True, X = X + 1, )
        S = If(, X = X + 1, Y = Y + 1)
        S = If(True)
        S = If()
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)
            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", Nothing, "Object")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", Nothing, "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="Y", ReadOutsideSymbol:="X, Y", WrittenInsideSymbol:="",
                                             WrittenOutsideSymbol:="args, S, X, Y", AlwaysAssignedSymbol:="", DataFlowsInSymbol:="",
                                             DataFlowsOutSymbol:="")
            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Object", Nothing)
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", Nothing)
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="X", ReadOutsideSymbol:="X, Y", WrittenInsideSymbol:="",
                                             WrittenOutsideSymbol:="args, S, X, Y", AlwaysAssignedSymbol:="", DataFlowsInSymbol:="X",
                                             DataFlowsOutSymbol:="", index:=2)
            '3
            semanticInfos = GetSemanticInfos(compilation1, 3)
            SemanticInfoTypeTest(semanticInfos, Nothing, "Object", "Object")
            SemanticInfoConvertedTypeTest(semanticInfos, Nothing, "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 3)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="X, Y", ReadOutsideSymbol:="X, Y", WrittenInsideSymbol:="",
                                             WrittenOutsideSymbol:="args, S, X, Y", AlwaysAssignedSymbol:="", DataFlowsInSymbol:="X, Y",
                                             DataFlowsOutSymbol:="", index:=3)
            '4
            semanticInfos = GetSemanticInfos(compilation1, 4)
            Assert.Null(semanticInfos)
            '5
            semanticInfos = GetSemanticInfos(compilation1, 5)
            Assert.Null(semanticInfos)

        End Sub

        ' Can't declare variable in argument
        <Fact>
        Public Sub DeclVarInArgument()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="DeclVarInArgument">
    <file name="a.vb">
Option Infer Off
Module Program
    Sub Main(args As String())
        Dim X = 1
        Dim Y = 1
        Dim S1 = If(Dim B = True, X = X + 1, Y = Y + 1)
        Dim S2 = If(True,dim x1 = 2,dim y1 =3)
        Dim S3 = If(True, X = 2,dim y1 = 3)
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)
            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, Nothing, "Object", "Object")
            SemanticInfoConvertedTypeTest(semanticInfos, Nothing, "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="X, Y", ReadOutsideSymbol:="X",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, S1, S2, S3, X, Y",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="X, Y", DataFlowsOutSymbol:="")
            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", Nothing, Nothing)
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", Nothing, Nothing)
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="X, Y",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, S1, S2, S3, X, Y",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)

            '3
            semanticInfos = GetSemanticInfos(compilation1, 3)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Object", Nothing)
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", Nothing)
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 3)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="X", ReadOutsideSymbol:="X, Y",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, S1, S2, S3, X, Y",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="X", DataFlowsOutSymbol:="", index:=3)

        End Sub

        ' Conditional operator could not as statement 
        <Fact>
        Public Sub ConditionalOperatorAsStatement()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ConditionalOperatorAsStatement">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim X = 1
        Dim y = 1
        If (1 > 2,x = x + 1,Y = Y+1) 'invalid
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            Assert.Null(semanticInfos)
        End Sub

        ' Conditional operator as parameter
        <Fact>
        Public Sub ConditionalOperatorAsParameter()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ConditionalOperatorAsParameter">
    <file name="a.vb">
Option Infer Off
Module Program
    Sub Main(args As String())
        Dim a0 As Boolean = False
        Dim a1 As Integer = 0
        Dim a2 As Long = 1
        Dim b0 = a0
        Dim b1 = a1
        Dim b2 = a2
        Console.WriteLine((If(b0, b1, b2)) &lt;&gt; (If(a0, a1, a2)))
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Object", "Object", "Object")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.NarrowingValue, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="b0, b1, b2", ReadOutsideSymbol:="a0, a1, a2",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="a0, a1, a2, args, b0, b1, b2",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="b0, b1, b2", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Integer", "Long")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Long", "Long")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="a0, a1, a2", ReadOutsideSymbol:="a0, a1, a2, b0, b1, b2",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="a0, a1, a2, args, b0, b1, b2",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="a0, a1, a2", DataFlowsOutSymbol:="", index:=2)
        End Sub

        ' 'Goto' is Invalid in expression
        <Fact>
        Public Sub GotoInConditionalOperator()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="GotoInConditionalOperator">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim s = If(True, GoTo lab1, GoTo lab2)
lab1:
        s = 1
lab2:
        s = 2
        Dim s1 = If(True, return, return)
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", Nothing, Nothing)
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", Nothing, Nothing)
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, s1",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", Nothing, Nothing)
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", Nothing, Nothing)
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, s1",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)
        End Sub

        ' Function call in return expression
        <Fact>
        Public Sub FunctionCallAsArgument()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
imports System
Module Program
    Sub Main(args As String())
        Dim x = If(True, Console.WriteLine(0), Console.WriteLine(1))
        Dim y = If(True, fun_void(), fun_int(1))
        Dim z = If(True, fun_Exception(1), fun_int(1))
        Dim r = If(True, fun_long(0), fun_int(1))
        Dim s = If(False, fun_long(0), fun_int(1))
    End Sub
    Private Sub fun_void()
        Return
    End Sub
    Private Function fun_int(x As Integer) As Integer
        Return x
    End Function
    Private Function fun_long(x As Integer) As Long
        Return CLng(x)
    End Function
    Private Function fun_Exception(x As Integer) As Exception
        Return New Exception()
    End Function
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Void", "Void")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(0, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(0, semanticInfos(2).ImplicitConversion.Kind)
            'ClassfiConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, r, s, x, y, z",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Void", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Integer", "Integer")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(0, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            'ClassfiConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                                         WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, r, s, x, y, z",
                                                         AlwaysAssignedSymbol:="", DataFlowsInSymbol:="",
                                                         DataFlowsOutSymbol:="", index:=2)

            '3
            semanticInfos = GetSemanticInfos(compilation1, 3)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "System.Exception", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 3)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                                         WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, r, s, x, y, z",
                                                         AlwaysAssignedSymbol:="", DataFlowsInSymbol:="",
                                                         DataFlowsOutSymbol:="", index:=3)

            '4
            semanticInfos = GetSemanticInfos(compilation1, 4)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Long", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Long", "Long")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 4)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                                         WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, r, s, x, y, z",
                                                         AlwaysAssignedSymbol:="", DataFlowsInSymbol:="",
                                                         DataFlowsOutSymbol:="", index:=4)
            '5
            semanticInfos = GetSemanticInfos(compilation1, 5)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Long", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Long", "Long")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 5)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                                         WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, r, s, x, y, z",
                                                         AlwaysAssignedSymbol:="", DataFlowsInSymbol:="",
                                                         DataFlowsOutSymbol:="", index:=5)
        End Sub

        ' Query works  in return argument 
        <Fact>
        Public Sub QueryAsArgument()

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
<compilation name="QueryAsArgument">
    <file name="a.vb">
Option Infer On
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim arr As String() = New String() {"aaa", "bbb", "ccc"}
        Dim arr_int As Integer() = New Integer() {111, 222, 333}
        Dim s = If(True, (From x In arr Select x).ToList(), From y As Integer In arr_int Select y)
    End Sub
End Module
    </file>
</compilation>, {SystemCoreRef})

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "System.Collections.Generic.List(Of String)", "System.Collections.Generic.IEnumerable(Of Integer)")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="arr, arr_int, x, y", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x, x, y, y", WrittenOutsideSymbol:="args, arr, arr_int, s",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="arr, arr_int", DataFlowsOutSymbol:="", variablesDeclared:="x, x, y, y")

        End Sub

        ' Lambda works  in return argument 
        <WorkItem(528700, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528700")>
        <Fact>
        Public Sub LambdaAsArgument()

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
<compilation name="QueryAsArgument">
    <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Module Program
    Sub Main(args As String())
        Dim Y = 2
        Dim S = If(True, DirectCast(Function(z As Integer) As Integer
                                        System.Console.WriteLine("SUB")
                                        Return z * z
                                    End Function, Func(Of Integer, Integer)), Y + 1)
        S = If(False, _
Sub(Z As Integer)
    System.Console.WriteLine("SUB")
End Sub, Y + 1)
        System.Console.WriteLine(S)
    End Sub
End Module 
    </file>
</compilation>, {SystemCoreRef})

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "System.Func(Of Integer, Integer)", "Integer")
            'SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Integer")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, variablesDeclared:="z", ReadInsideSymbol:="Y, z", ReadOutsideSymbol:="S, Y",
                                             WrittenInsideSymbol:="z", WrittenOutsideSymbol:="args, S, Y, Z",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", Nothing, "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, variablesDeclared:="Z", ReadInsideSymbol:="Y", ReadOutsideSymbol:="S, Y, z",
                                             WrittenInsideSymbol:="Z", WrittenOutsideSymbol:="args, S, Y, z",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)

        End Sub

        ' Conflict variable name declare in IF 
        <Fact>
        Public Sub VariableNameConflict_1()

            Dim compilation1 = CreateCompilationWithMscorlibAndReferences(
<compilation name="VariableNameConflict">
    <file name="a.vb">
Option Infer On
Imports System.Linq
Imports System
Module Program
    Sub Main(args As String())
        Dim arr As String() = New String() {"aaa", "bbb", "ccc"}
        Dim arr_int As Integer() = New Integer() {111, 222, 333}
        Dim x = 1
        Dim s = If(True, (From x In arr Select x).ToList(), From y As Integer In arr_int Select y)
    End Sub
End Module
    </file>
</compilation>, {SystemCoreRef})

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "System.Collections.Generic.List(Of String)", "System.Collections.Generic.IEnumerable(Of Integer)")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="arr, arr_int, x, y", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x, x, y, y", WrittenOutsideSymbol:="args, arr, arr_int, s, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="arr, arr_int", DataFlowsOutSymbol:="", variablesDeclared:="x, x, y, y")

        End Sub

        ' Conflict variable name declare in IF 
        <WorkItem(528700, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528700")>
        <Fact>
        Public Sub VariableNameConflict_2()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="VariableNameConflict">
    <file name="a.vb">
Option Infer Off
Imports System
Module Program
    Sub Main(args As String())
        Dim X = 1
        Dim Y = 1
        Dim S = If(True, DirectCast(Function(x As Integer) As Integer
                                        Return 0
                                    End Function, Func(Of Integer, Integer)), Y = Y + 1)
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "System.Func(Of Integer, Integer)", "Object")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, variablesDeclared:="x", ReadInsideSymbol:="Y", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="args, S, X, Y",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

        End Sub

        ' Object initializer's in a ternary expression 
        <Fact>
        Public Sub ObjInit()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ObjInit">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim f1 As New Foo(), f2 As New Foo(), f3 As New Foo()
        Dim b As Boolean = True
        f3 = If(b, f1 = New Foo(), f2 = New Foo())
        b = False
        f3 = If(b, f1 = New Foo(), f2 = New Foo())
    End Sub
End Module
Class Foo
    Public i As Integer
End Class
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "?", "?")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(0, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(0, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="b, f1, f2", ReadOutsideSymbol:="b, f1, f2",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, b, f1, f2, f3",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="b, f1, f2", DataFlowsOutSymbol:="")
            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "?", "?")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(0, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(0, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="b, f1, f2", ReadOutsideSymbol:="b, f1, f2",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, b, f1, f2, f3",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="b, f1, f2", DataFlowsOutSymbol:="", index:=2)

        End Sub

        ' Multiple conditional  operator in expression
        <Fact>
        Public Sub MultipleConditional()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="MultipleConditional">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim S = If(False, 1, If(True, 2, 3))
        Console.Write(S)
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Integer", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Integer", "Integer")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="S",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, S",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Integer", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Integer", "Integer")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="S",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, S",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

        End Sub

        ' Arguments are of types: bool, constant, enum
        <Fact>
        Public Sub EnumAsArguments()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="EnumAsArguments">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim testResult = If(True, 0, color.Blue)
        Console.WriteLine(testResult)
        testResult = If(True, 5, color.Blue)
        Console.WriteLine(testResult)
    End Sub
End Module
Enum color
    Red
    Green
    Blue
End Enum
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Integer", "color")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Integer", "Integer")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Numeric Or ConversionKind.Widening Or ConversionKind.InvolvesEnumTypeConversions, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="testResult",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, testResult",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Integer", "color")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Integer", "Integer")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Numeric Or ConversionKind.Widening Or ConversionKind.InvolvesEnumTypeConversions, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="testResult",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, testResult",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

        End Sub

        ' Arguments are of types: bool, IEnumerable<int> and int[]
        <Fact()>
        Public Sub IEnumerableAsArguments()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="IEnumerableAsArguments">
    <file name="a.vb">
Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim testResult = If(True, Enumerable.Empty(Of Integer)(), {1})
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "?", Nothing)
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Integer()", "Integer()")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(Nothing, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Widening, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, testResult",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

        End Sub

        ' Implicit conversion between Enum and null
        <WorkItem(542116, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542116")>
        <Fact()>
        Public Sub ImplicitConversion()
            Dim compilationDef =
<compilation name="ImplicitConversion">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim x = foo(0)
        System.Console.WriteLine(x)
        'Blue
        Dim y = foo(1)
        System.Console.WriteLine(y)
        'Red
    End Sub
    Private Function foo(x As color) As color
        Dim dd = Function()
                     Return If((x = color.Red), color.Blue, Nothing)
                 End Function
        Return (dd())
    End Function
End Module
Enum color
    Red
    Green
    Blue
End Enum
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
2
0
]]>)
            Dim compilation1 = DirectCast(verifier.Compilation, VisualBasicCompilation)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "color", Nothing)
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "color", "color")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningNothingLiteral, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x", ReadOutsideSymbol:="dd",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="dd, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

        End Sub

        ' Implicit type conversion on conditional
        <Fact>
        Public Sub ImplicitConversionForConditional()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ImplicitConversionForConditional">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim valueFromDatabase As Object
        Dim result As Decimal
        valueFromDatabase = DBNull.Value
        result = CDec(If(valueFromDatabase IsNot DBNull.Value, valueFromDatabase, 0))
        result = (If(valueFromDatabase IsNot DBNull.Value, CDec(valueFromDatabase), CDec(0)))
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Object", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="valueFromDatabase", ReadOutsideSymbol:="valueFromDatabase",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, result, valueFromDatabase",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="valueFromDatabase", DataFlowsOutSymbol:="")
            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Decimal", "Decimal")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Decimal", "Decimal")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, index:=2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="valueFromDatabase", ReadOutsideSymbol:="valueFromDatabase",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, result, valueFromDatabase",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="valueFromDatabase", DataFlowsOutSymbol:="", index:=2)

        End Sub

        ' Implicitly typed arrays used in arguments
        <Fact()>
        Public Sub ImplicitlyTypedArraysUsedAsarguments()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ImplicitlyTypedArraysUsedAsarguments">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim a As Integer()
        a = Nothing
        a = If(True, {1, 2, 3}, {2, 3, 4})
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", Nothing, Nothing)
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Integer()", "Integer()")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Widening, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Widening, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="a, args",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

        End Sub

        ' Nullable bool type can be conditional-argument
        <Fact()>
        Public Sub NullableAsarguments()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="NullableAsarguments">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim b As Boolean? = Nothing
        Dim result = If(b, 0, 1)
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean?", "Integer", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean?", "Integer", "Integer")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="b", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, b, result",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="b", DataFlowsOutSymbol:="")

        End Sub

        ' Not boolean type as conditional-argument
        <Fact>
        Public Sub NotBooleanAsConditionalArgument()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="NotBooleanAsConditionalArgument">
    <file name="a.vb">
Option Infer Off
Imports System
Module Program
    Sub Main(args As String())
        Dim x = 1
        Dim s = If("", x, 2)
        'invalid
        s = If("True", x, 2)
        'valid
        s = If("1", x, 2)
        'valid
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "String", "Object", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.NarrowingString, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "String", "Object", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.NarrowingString, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)

            '3
            semanticInfos = GetSemanticInfos(compilation1, 3)
            SemanticInfoTypeTest(semanticInfos, "String", "Object", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.NarrowingString, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 3)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=3)

        End Sub

        ' Not boolean type as conditional-argument
        <Fact>
        Public Sub NotBooleanAsConditionalArgument_1()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="NotBooleanAsConditionalArgument">
    <file name="a.vb">
Option Infer Off
Imports System
Module Program
    Sub Main(args As String())
        Dim x = 1
        Dim s = If(color.Green, x, 2)
        '1
        s = If(color.Red, x, 2)
        '2
    End Sub
End Module
Public Enum color
    Red
    Blue
    Green
End Enum
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "color", "Object", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.NarrowingBoolean Or ConversionKind.InvolvesEnumTypeConversions, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "color", "Object", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.NarrowingBoolean Or ConversionKind.InvolvesEnumTypeConversions, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)

        End Sub

        <Fact>
        Public Sub FunctionWithNoReturnType()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="FunctionWithNoReturnType">
    <file name="a.vb">
Option Infer Off
Imports System
Module Program
    Sub Main(args As String())
        Dim x = 1
        Dim s = If(fun1(), x, 2)
        s = If(sub1(), x, 2)
    End Sub
    Private Sub sub1()
    End Sub
    Private Function fun1()
    End Function
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Object", "Object", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.NarrowingValue, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="")
            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Void", "Object", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x", ReadOutsideSymbol:="x",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)

        End Sub

        ' General type as argument
        <Fact>
        Public Sub GeneralTypeAsArgument()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="GeneralTypeAsArgument">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
    End Sub
    Private Function fun(Of T)(Parm1 As T) As T
        Dim temp As T
        Return If(temp, temp, 1)
    End Function
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "T", "T", "Integer")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningTypeParameter, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="temp", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="Parm1",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="temp", DataFlowsOutSymbol:="")
        End Sub

        ' General Method as argument
        <Fact>
        Public Sub GeneralMethodAsArgument()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="GeneralMethodAsArgument">
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim x As Integer = 1
        Dim y As Object = 0
        Dim s = If(True, fun(x), y)
        Dim s1 = If(False, sub1(x), y)
    End Sub
    Private Function fun(Of T)(Parm1 As T) As T
        Return Parm1
    End Function
    Private Sub sub1(Of T)(Parm1 As T)
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Integer", "Object")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x, y", ReadOutsideSymbol:="x, y",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, s1, x, y",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Void", "Object")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Object", "Object")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(0, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="x, y", ReadOutsideSymbol:="x, y",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, s, s1, x, y",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="y", DataFlowsOutSymbol:="", index:=2)

        End Sub

        ' IF operator on Try Catch
        <Fact()>
        Public Sub TryCatch()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="TryCatch">
    <file name="a.vb">
Option Infer Off
Imports System
Module Program
    Sub Main(args As String())
        Dim inside = 0
        Try
            Throw New Exception
        Catch ex As Exception When If(False, CType(Nothing, Boolean?), True)
            inside = 1
        End Try
    End Sub
    Sub foo()
        Try
            Dim s_a As Exception
            Dim s_b As New InvalidCastException
            Throw If(False, s_a, s_b)
        Catch ex As InvalidCastException
        End Try
    End Sub
End Module
    </file>
</compilation>, OutputKind.ConsoleApplication)

            '1
            Dim semanticInfos = GetSemanticInfos(compilation1, 1)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "Boolean?", "Boolean")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "Boolean?", "Boolean?")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningNullable, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="args, ex, inside",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")

            '2
            semanticInfos = GetSemanticInfos(compilation1, 2)
            SemanticInfoTypeTest(semanticInfos, "Boolean", "System.Exception", "System.InvalidCastException")
            SemanticInfoConvertedTypeTest(semanticInfos, "Boolean", "System.Exception", "System.Exception")
            Assert.Equal(ConversionKind.Identity, semanticInfos(0).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.Identity, semanticInfos(1).ImplicitConversion.Kind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfos(2).ImplicitConversion.Kind)
            ClassifyConversionTest(compilation1, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, ReadInsideSymbol:="s_a, s_b", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="ex, s_b",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)

        End Sub

        Private Function GetSemanticInfos(compilation As VisualBasicCompilation, index As Integer) As List(Of SemanticInfoSummary)

            Dim model = GetModel(compilation)
            Dim ternaryExpression = GetTernaryConditionalExpression(compilation, index)
            If (ternaryExpression Is Nothing) Then
                Return Nothing
            End If
            Dim conditionExpression = ternaryExpression.Condition
            Dim whenTrueExpression = ternaryExpression.WhenTrue
            Dim whenFalseExpression = ternaryExpression.WhenFalse
            Dim ConditionSemanticInfo = model.GetSemanticInfoSummary(conditionExpression)
            Dim whenTrueSemanticInfo = model.GetSemanticInfoSummary(whenTrueExpression)
            Dim whenFalseSemanticInfo = model.GetSemanticInfoSummary(whenFalseExpression)

            Dim semanticInfos As List(Of SemanticInfoSummary) = New List(Of SemanticInfoSummary)()
            semanticInfos.Add(ConditionSemanticInfo)
            semanticInfos.Add(whenTrueSemanticInfo)
            semanticInfos.Add(whenFalseSemanticInfo)
            Return semanticInfos

        End Function

        Private Sub SemanticInfoTypeTest(semanticInfos As List(Of SemanticInfoSummary), ParamArray names As String())
            Dim i = 0
            For Each semanticInfo In semanticInfos
                If names(i) Is Nothing Then
                    Assert.Null(semanticInfo.Type)
                Else
                    If names(i) Is Nothing Then
                        Assert.Null(semanticInfo.Type)
                    Else
                        Assert.Equal(names(i), semanticInfo.Type.ToDisplayString())
                    End If
                End If

                i += 1
            Next
        End Sub

        Private Sub SemanticInfoConvertedTypeTest(semanticInfos As List(Of SemanticInfoSummary), ParamArray names As String())
            Dim i = 0

            If names(0) Is Nothing Then
                Assert.Null(semanticInfos(0).Type)
            Else
                If semanticInfos(0).Type IsNot Nothing AndAlso (semanticInfos(0).Type.ToDisplayString() <> "Void" AndAlso
                    semanticInfos(1).ConvertedType IsNot Nothing AndAlso
                    semanticInfos(2).ConvertedType IsNot Nothing AndAlso
                    semanticInfos(1).ConvertedType.ToDisplayString() <> "Void" AndAlso
                    semanticInfos(2).ConvertedType.ToDisplayString() <> "Void" AndAlso
                    semanticInfos(1).ConvertedType.TypeKind <> TypeKind.TypeParameter AndAlso
                    semanticInfos(2).ConvertedType.TypeKind <> TypeKind.TypeParameter) Then
                    Assert.Equal(semanticInfos(1).ConvertedType.ToDisplayString(), semanticInfos(2).ConvertedType.ToDisplayString())
                End If
            End If

            For Each semanticInfo In semanticInfos
                If names(i) Is Nothing Then
                    Assert.Null(semanticInfo.Type)
                Else
                    Assert.Equal(names(i), semanticInfo.ConvertedType.ToDisplayString())
                End If

                i += 1
            Next
        End Sub

        Private Function AnalyzeRegionDataFlowTestForeach(compilation As VisualBasicCompilation,
                         ReadInsideSymbol As String, ReadOutsideSymbol As String, WrittenInsideSymbol As String,
                         WrittenOutsideSymbol As String, AlwaysAssignedSymbol As String,
                         DataFlowsInSymbol As String, DataFlowsOutSymbol As String, Optional variablesDeclared As String = "",
                         Optional index As Integer = 1) As DataFlowAnalysis
            Dim node = GetTernaryConditionalExpression(compilation, index)
            Dim model = GetModel(compilation)
            Dim analyze = model.AnalyzeDataFlow(node)
            Assert.Equal(variablesDeclared, GetSymbolNamesSortedAndJoined(analyze.VariablesDeclared))
            Assert.Equal(ReadInsideSymbol, GetSymbolNamesSortedAndJoined(analyze.ReadInside))
            Assert.Equal(ReadOutsideSymbol, GetSymbolNamesSortedAndJoined(analyze.ReadOutside))
            Assert.Equal(WrittenInsideSymbol, GetSymbolNamesSortedAndJoined(analyze.WrittenInside))
            Assert.Equal(WrittenOutsideSymbol, GetSymbolNamesSortedAndJoined(analyze.WrittenOutside))
            Assert.Equal(AlwaysAssignedSymbol, GetSymbolNamesSortedAndJoined(analyze.AlwaysAssigned))
            Assert.Equal(DataFlowsOutSymbol, GetSymbolNamesSortedAndJoined(analyze.DataFlowsOut))
            Return analyze
        End Function

        Private Function ClassifyConversionTest(compilation As VisualBasicCompilation, Optional index As Integer = 1) As List(Of Conversion)

            Dim model = GetModel(compilation)
            Dim ternaryExpression = GetTernaryConditionalExpression(compilation, index)
            If (ternaryExpression Is Nothing) Then
                Return Nothing
            End If
            Dim conditionExpression = ternaryExpression.Condition
            Dim whenTrueExpression = ternaryExpression.WhenTrue
            Dim whenFalseExpression = ternaryExpression.WhenFalse

            Dim expressions = New List(Of ExpressionSyntax)()
            expressions.Add(conditionExpression)
            expressions.Add(whenTrueExpression)
            expressions.Add(whenFalseExpression)
            expressions = expressions.Where(Function(x) x.IsMissing = False).ToList()

            Dim convs = New List(Of Conversion)()

            For Each expression In expressions
                Dim semanticInfo = model.GetSemanticInfoSummary(expression)
                Dim conv = model.ClassifyConversion(expression, semanticInfo.ConvertedType)
                convs.Add(conv)
                If (conv.Kind = ConversionKind.Identity) Then
                    Assert.True(conv.Exists)
                    Assert.True(conv.IsIdentity)
                End If

                If semanticInfo.Type Is Nothing Then
                    Assert.True(Conversions.IsWideningConversion(semanticInfo.ImplicitConversion.Kind))
                ElseIf (semanticInfo.Type.ToDisplayString() <> "?" And semanticInfo.Type.ToDisplayString() <> "Void" And semanticInfo.ConvertedType.ToDisplayString() <> "?" And semanticInfo.ConvertedType.ToDisplayString() <> "Void") Then
                    Assert.Equal(conv.Kind, semanticInfo.ImplicitConversion.Kind)
                End If
            Next
            Return convs
        End Function

        Private Function GetSymbolNamesSortedAndJoined(Of T As ISymbol)(symbols As IEnumerable(Of T)) As String
            Return String.Join(", ", symbols.Select(Function(symbol) symbol.Name).OrderBy(Function(name) name))
        End Function

        Private Function GetModel(compilation As VisualBasicCompilation) As SemanticModel
            Dim tree = compilation.SyntaxTrees.First
            Dim model = compilation.GetSemanticModel(tree)
            Return model
        End Function

        Private Function GetTernaryConditionalExpression(compilation As VisualBasicCompilation, index As Integer) As TernaryConditionalExpressionSyntax

            Dim tree = compilation.SyntaxTrees.First
            Dim node = tree.FindNodeOrTokenByKind(SyntaxKind.TernaryConditionalExpression, index).AsNode()
            'Dim expressions = node.ChildNodes.Select(Function(x) TryCast(x, ExpressionSyntax)).Where(Function(x) x IsNot Nothing)
            Dim ternaryExpression = TryCast(node, TernaryConditionalExpressionSyntax)
            Return ternaryExpression
        End Function

    End Class
End Namespace
