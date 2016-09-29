' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class ForLoopTest : Inherits BasicTestBase

        <Fact>
        Public Sub SimpleForLoopsTest()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="SimpleForLoopsTest">
    <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        Dim myarray As Integer() = New Integer(2) {1, 2, 3}
        For i As Integer = 0 To myarray.Length - 1
            System.Console.WriteLine(myarray(i))
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="i", ReadInsideSymbol:="i, myarray", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="i", WrittenOutsideSymbol:="myarray",
                                             AlwaysAssignedSymbol:="i", DataFlowsInSymbol:="myarray", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)

        End Sub

        <Fact>
        Public Sub SimpleForLoopsTestConversion()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="SimpleForLoopsTest">
    <file name="a.vb">
option strict off
Public Class MyClass1
    Public Shared Sub Main()
        Dim myarray As Integer() = New Integer(1) {}
        myarray(0) = 1
        myarray(1) = 2

        Dim s as double = 1.1

        For i As Integer = 0 To "1" Step s
            System.Console.WriteLine(myarray(i))
        Next

    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "String", "Integer", "Double", "Integer")
            GetDeclareSymbolTestForLoops(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="i", ReadInsideSymbol:="i, myarray, s", ReadOutsideSymbol:="myarray",
                                             WrittenInsideSymbol:="i", WrittenOutsideSymbol:="myarray, s",
                                             AlwaysAssignedSymbol:="i", DataFlowsInSymbol:="myarray, s", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <Fact>
        Public Sub ForLoopStepIsFloatNegativeVar()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="SimpleForLoopsTest">
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        Dim s As Double = -1.1

        For i As Double = 2 To 0 Step s
            System.Console.WriteLine(i)
        Next

    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Double", "Integer", "Double", "Double", "Double")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="i", ReadInsideSymbol:="i, s", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="i", WrittenOutsideSymbol:="s",
                                             AlwaysAssignedSymbol:="i", DataFlowsInSymbol:="s", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <Fact>
        Public Sub ForLoopObject()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="SimpleForLoopsTest">
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()

        Dim ctrlVar As Object
        Dim initValue As Object = 0
        Dim limit As Object = 2
        Dim stp As Object = 1

        For ctrlVar = initValue To limit Step stp
            System.Console.WriteLine(ctrlVar)
        Next

    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Object", "Object", "Object", "Object", "Object", "Object")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="ctrlVar, initValue, limit, stp", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="ctrlVar", WrittenOutsideSymbol:="initValue, limit, stp",
                                             AlwaysAssignedSymbol:="ctrlVar", DataFlowsInSymbol:="initValue, limit, stp", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <Fact>
        Public Sub ForLoopNested()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ForLoopNested">
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For AVarName = 1 To 2
            For B = 1 To 2
                For C = 1 To 2
                    For D = 1 To 2
                    Next D
                Next C
            Next B
        Next AVarName
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "Integer", "Integer")
            GetDeclareSymbolTestForLoops(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="AVarName, B, C, D", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="AVarName, B, C, D", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="AVarName", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)

            SemanticInfoTypeTestForLoops(compilation1, 2, "Integer", "Integer", "Integer", "Integer")
            GetDeclareSymbolTestForLoops(compilation1, Nothing, 2)
            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="B, C, D", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="B, C, D", WrittenOutsideSymbol:="AVarName",
                                             AlwaysAssignedSymbol:="B", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForLoops(compilation1, 2)

            SemanticInfoTypeTestForLoops(compilation1, 3, "Integer", "Integer", "Integer", "Integer")
            GetDeclareSymbolTestForLoops(compilation1, Nothing, 3)
            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="C, D", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="C, D", WrittenOutsideSymbol:="AVarName, B",
                                             AlwaysAssignedSymbol:="C", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=3)
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=3)
            ClassfiConversionTestForLoops(compilation1, 3)

            SemanticInfoTypeTestForLoops(compilation1, 4, "Integer", "Integer", "Integer", "Integer")
            GetDeclareSymbolTestForLoops(compilation1, Nothing, 4)
            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="D", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="D", WrittenOutsideSymbol:="AVarName, B, C",
                                             AlwaysAssignedSymbol:="D", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=4)
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=4)
            ClassfiConversionTestForLoops(compilation1, 4)
        End Sub

        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <Fact>
        Public Sub ChangeOuterVarInInnerFor()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ChangeOuterVarInInnerFor">
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2
            For J = 1 To 2
                I = 3
                System.Console.WriteLine(I)
            Next
        Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="I, J", ReadInsideSymbol:="I", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="I, J", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="I", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)

            SemanticInfoTypeTestForLoops(compilation1, 2, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="J", ReadInsideSymbol:="I", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="I, J", WrittenOutsideSymbol:="I",
                                             AlwaysAssignedSymbol:="J", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForLoops(compilation1, 2)
        End Sub

        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <Fact>
        Public Sub InnerForRefOuterForVar()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="InnerForRefOuterForVar">
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2
            For J = I + 1 To 2
                System.Console.WriteLine(J)
            Next
        Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="I, J", ReadInsideSymbol:="I, J", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="I, J", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="I", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)

            SemanticInfoTypeTestForLoops(compilation1, 2, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="J", ReadInsideSymbol:="I, J", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="J", WrittenOutsideSymbol:="I",
                                             AlwaysAssignedSymbol:="J", DataFlowsInSymbol:="I", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForLoops(compilation1, 2)
        End Sub

        ' Exit for nested for loops
        <Fact>
        Public Sub ExitNestedFor()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="ExitNestedFor">
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2
            For J = 1 To 2
                Exit For
            Next
            System.Console.WriteLine(I)
        Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="I, J", ReadInsideSymbol:="I", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="I, J", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="I", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)

            SemanticInfoTypeTestForLoops(compilation1, 2, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="J", ReadInsideSymbol:="", ReadOutsideSymbol:="I",
                                             WrittenInsideSymbol:="J", WrittenOutsideSymbol:="I",
                                             AlwaysAssignedSymbol:="J", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForLoops(compilation1, 2)
        End Sub

        ' Use nothing as the start value
        <Fact>
        Public Sub NothingAsStart()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="NothingAsStart">
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        Dim x = 1
        For J = Nothing To 5 Step x
            Console.WriteLine(J)
            x = 2
        Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Object", "Object", "Integer", "Object", "Integer", "Object")
            GetDeclareSymbolTestForLoops(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="J", ReadInsideSymbol:="J, x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="J, x", WrittenOutsideSymbol:="x",
                                             AlwaysAssignedSymbol:="J", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <Fact>
        Public Sub EnumAsStart()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="EnumAsStart">
    <file name="a.vb">
Option Strict Off
Option Infer Off
Public Class MyClass1
    Public Shared Sub Main()
        For x As e1 = e1.a To e1.c
        Next
    End Sub
End Class
Enum e1
    a
    b
    c
End Enum
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "e1", "e1", "e1", "e1")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <Fact>
        Public Sub PropertyAsStart()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="PropertyAsStart">
    <file name="a.vb">
Option Strict Off
Option Infer Off
Public Class MyClass1
    Property P1(ByVal x As Long) As Byte
        Get
            Return x - 10
        End Get
        Set(ByVal Value As Byte)
        End Set
    End Property
    Public Shared Sub Main()
    End Sub
    Public Sub Foo()
        For i As Integer = P1(30 + i) To 30
        Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Byte", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="i", ReadInsideSymbol:="i, Me", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="i", WrittenOutsideSymbol:="Me",
                                             AlwaysAssignedSymbol:="i", DataFlowsInSymbol:="Me", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <Fact>
        Public Sub FieldNameAsIteration()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="FieldNameAsIteration">
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Dim global_x As Integer = 10
    Const global_y As Long = 20
    Public Shared Sub Main()
        For global_x As Integer = global_y To 10
        Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Long", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="global_x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="global_x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="global_x", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <Fact>
        Public Sub SingleLine()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="SingleLine">
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For x As Integer = 0 To 10 : Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="x", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        ' For statement is split in every possible place
        <Fact>
        Public Sub SplitForLoop()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For _
x _
As _
Integer _
= _
0 _
To _
10
        Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "Integer", "Integer")
            GetDeclareSymbolTestForLoops(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="x", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <Fact>
        Public Sub VarDeclOutOfForeach()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="VarDeclOutOfForeach">
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        Dim Y As Integer
        For Y = 1 To 2
        Next
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForLoops(compilation1, 1, "Integer", "Integer", "Integer", "Integer")

            AnalyzeRegionDataFlowTestForLoops(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="Y", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="Y", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForLoops(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForLoops(compilation1)
        End Sub

        <Fact>
        Public Sub GetDeclaredSymbolOfForStatement()
            Dim compilation1 = CreateCompilationWithMscorlib(
<compilation name="CollectionHasConvertedType">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collection

Class C1
    Public Shared Sub Main()
        For element1 = 23 to 42
        Next

        For element2 as Integer = 23 to 42
        Next
    End Sub
End Class        
    </file>
</compilation>)
            GetDeclareSymbolTestForLoops(compilation1, Nothing, 1)
            GetDeclareSymbolTestForLoops(compilation1, Nothing, 2)
        End Sub

        <WorkItem(543649, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543649")>
        <Fact()>
        Public Sub TestTypeInferenceWithGarbageTypes()
            Dim vbCompilation = CreateVisualBasicCompilation("TestTypeInferenceWithGarbageTypes",
            <![CDATA[Imports System

Module Module1
    Sub Main
        For i = New Exception() To New Exception() Step 10
        Next
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOutputKind(OutputKind.ConsoleApplication))
            vbCompilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_NoSuitableWidestType1, "i").WithArguments("i"))
        End Sub


        Private Function SemanticInfoTypeTestForLoops(compilation As VisualBasicCompilation, index As Integer, ParamArray names As String()) As List(Of SemanticInfoSummary)
            Dim node = GetForStatement(compilation, index)
            Dim model = GetModel(compilation)
            Dim expressionFrom = node.FromValue
            Dim expressionTo = node.ToValue
            Dim expressionStep = If(node.StepClause Is Nothing, Nothing, node.StepClause.StepValue)
            Dim semanticInfoFrom = CompilationUtils.GetSemanticInfoSummary(model, expressionFrom)
            Dim semanticInfoTo = CompilationUtils.GetSemanticInfoSummary(model, expressionTo)
            Dim semanticInfoStep = If(node.StepClause Is Nothing, Nothing, CompilationUtils.GetSemanticInfoSummary(model, expressionStep))
            Dim semanticInfo = New List(Of SemanticInfoSummary) From {semanticInfoFrom, semanticInfoTo, semanticInfoStep}

            For i As Integer = 0 To names.Length \ 2 - 1
                If semanticInfo(i).Type Is Nothing Then
                    Continue For
                End If
                Assert.Equal(names(i * 2), semanticInfo(i).Type.ToDisplayString())
                Assert.Equal(names(i * 2 + 1), semanticInfo(i).ConvertedType.ToDisplayString())
            Next
            Return semanticInfo
        End Function

        Private Function GetDeclareSymbolTestForLoops(compilation As VisualBasicCompilation, symName As String, Optional index As Integer = 1) As Symbol
            Dim node = GetForStatement(compilation, index)
            Dim model = GetModel(compilation)
            Dim symbol = model.GetDeclaredSymbolFromSyntaxNode(node)

            If symName Is Nothing Then
                Assert.Null(symbol)
            Else
                Assert.NotNull(symbol)
                Assert.Equal(symName.ToLowerInvariant(), symbol.Name.ToLowerInvariant())
            End If

            Return symbol
        End Function

        Private Function AnalyzeRegionDataFlowTestForLoops(compilation As VisualBasicCompilation, VariablesDeclaredSymbol As String,
                         ReadInsideSymbol As String, ReadOutsideSymbol As String, WrittenInsideSymbol As String,
                         WrittenOutsideSymbol As String, AlwaysAssignedSymbol As String,
                         DataFlowsInSymbol As String, DataFlowsOutSymbol As String,
                         Optional index As Integer = 1) As DataFlowAnalysis
            Dim node = GetForBlock(compilation, index)
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

        Private Function AnalyzeRegionControlFlowTestForLoops(
                         compilation As VisualBasicCompilation,
                         Optional EntryPoints As Integer = 0,
                         Optional ExitPoints As Integer = 0,
                         Optional EndPointIsReachable As Boolean = True,
                         Optional index As Integer = 1) As ControlFlowAnalysis
            Dim node = GetForBlock(compilation, index)
            Dim model = GetModel(compilation)
            Dim analyze = model.AnalyzeControlFlow(node, node)
            Assert.Equal(EntryPoints, analyze.EntryPoints.Count)
            Assert.Equal(ExitPoints, analyze.ExitPoints.Count)
            Assert.Equal(EndPointIsReachable, analyze.EndPointIsReachable)
            Return analyze
        End Function

        Private Function ClassfiConversionTestForLoops(compilation As VisualBasicCompilation, Optional index As Integer = 1) As List(Of Conversion)
            Dim node = GetForStatement(compilation, index)
            Dim model = GetModel(compilation)
            Dim expressionFrom = node.FromValue
            Dim expressionTo = node.ToValue
            Dim semanticInfoFrom = CompilationUtils.GetSemanticInfoSummary(model, expressionFrom)
            Dim semanticInfoTo = CompilationUtils.GetSemanticInfoSummary(model, expressionTo)
            Dim semanticInfos = New List(Of Tuple(Of SemanticInfoSummary, ExpressionSyntax)) From {Tuple.Create(semanticInfoFrom, expressionFrom), Tuple.Create(semanticInfoTo, expressionTo)}
            Dim convs = New List(Of Conversion)()

            For Each SemanticInfo In semanticInfos
                If SemanticInfo.Item1.ConvertedType Is Nothing Then
                    Return Nothing
                End If
                Dim conv = model.ClassifyConversion(SemanticInfo.Item2, SemanticInfo.Item1.ConvertedType)

                If (conv.Kind = ConversionKind.Identity) Then
                    Assert.True(conv.Exists)
                    Assert.True(conv.IsIdentity)
                End If

                If (SemanticInfo.Item1.Type IsNot Nothing AndAlso SemanticInfo.Item1.Type.ToDisplayString() <> "?" AndAlso SemanticInfo.Item1.Type.ToDisplayString() <> "Void" AndAlso SemanticInfo.Item1.ConvertedType.ToDisplayString() <> "?" AndAlso SemanticInfo.Item1.ConvertedType.ToDisplayString() <> "Void") Then
                    Assert.Equal(conv.Kind, SemanticInfo.Item1.ImplicitConversion.Kind)
                End If
                convs.Add(conv)
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

        Private Function GetForStatement(compilation As VisualBasicCompilation, index As Integer) As ForStatementSyntax
            Dim tree = compilation.SyntaxTrees.First
            Dim node = tree.FindNodeOrTokenByKind(SyntaxKind.ForStatement, index).AsNode()
            Dim forStatement = TryCast(node, ForStatementSyntax)
            Return forStatement
        End Function

        Private Function GetForBlock(compilation As VisualBasicCompilation, index As Integer) As ForBlockSyntax
            Dim tree = compilation.SyntaxTrees.First
            Dim node = tree.FindNodeOrTokenByKind(SyntaxKind.ForBlock, index).AsNode()
            Return DirectCast(node, ForBlockSyntax)
        End Function

        <Fact, WorkItem(652041, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652041")>
        Public Sub Bug652041()
            Dim compilation1 = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Option Infer On

Module Program
    Sub Main()
        For X = 1 To 10
        Next
    End Sub

    Sub Main1()
        For Each X In {1,2,3}
        Next
    End Sub

End Module
 
Module M
    Public X As Integer
End Module
 
Module N
    Public X As Integer
End Module
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation1,
<expected>
BC30562: 'X' is ambiguous between declarations in Modules 'M, N'.
        For X = 1 To 10
            ~
BC30562: 'X' is ambiguous between declarations in Modules 'M, N'.
        For Each X In {1,2,3}
                 ~
</expected>)
        End Sub

    End Class
End Namespace
