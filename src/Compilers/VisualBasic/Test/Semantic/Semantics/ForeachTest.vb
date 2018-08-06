' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class ForeachTest : Inherits BasicTestBase

        <Fact>
        Public Sub SimpleForeachTest()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="SimpleForeachTest">
    <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        Dim arr As String() = New String(1) {}
        arr(0) = "one"
        arr(1) = "two"
        For Each s As String In arr
            Console.WriteLine(s)
        Next
    End Sub

End Class
    </file>
</compilation>, OutputKind.ConsoleApplication)

            SemanticInfoTypeTestForeach(compilation1, 1, "String()", "String()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="s", ReadInsideSymbol:="arr, s", ReadOutsideSymbol:="arr",
                                             WrittenInsideSymbol:="s", WrittenOutsideSymbol:="arr",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="arr", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        ' Narrowing conversions from the elements in group to element are evaluated and performed at run time
        <Fact>
        Public Sub NarrowConversions()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="NarrowConversions">
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Shared Sub Main()
        For Each number As Integer In New Long() {45, 3}
            Console.WriteLine(number)
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Long()", "Long()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="number", ReadInsideSymbol:="number", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="number", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            Dim verifyItem As ForEachStatementInfo = Me.VerifyForeachSemanticInfo(compilation1, 1)
            Assert.Equal("Public Overloads Function GetEnumerator() As System.Collections.IEnumerator", verifyItem.GetEnumeratorMethod.ToString)
            Assert.Equal("Function MoveNext() As Boolean", verifyItem.MoveNextMethod.ToString)
            Assert.Equal("ReadOnly Property Current As Object", verifyItem.CurrentProperty.ToString)
            Assert.Equal("Sub Dispose()", verifyItem.DisposeMethod.ToString)
        End Sub

        ' Narrowing conversions from the elements in group to element are evaluated and performed at run time
        <Fact>
        Public Sub NarrowConversions_2()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="NarrowConversions">
    <file name="a.vb">
Option Strict On
Imports System
Class C
    Shared Sub Main()
        For Each number As Integer In New Long() {9876543210}
            Console.WriteLine(number)
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Long()", "Long()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="number", ReadInsideSymbol:="number", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="number", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        ' Using the iteration variable in the collection expression
        <WorkItem(542234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542234")>
        <Fact>
        Public Sub IterationVarInCollectionExpression()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="IterationVarInCollectionExpression">
    <file name="a.vb">
Option Infer On
Class C
    Shared Sub Main()
        For Each x As S In If(True, x, 1)
        Next
    End Sub
End Class
Public Structure S
End Structure
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Object", "System.Collections.IEnumerable")
            GetDeclareSymbolTestForeach(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
            Me.ClassfiConversionTestForeach(compilation1, 1)

            Dim verifyItem As ForEachStatementInfo = Me.VerifyForeachSemanticInfo(compilation1, 1)
            Assert.Equal("Function GetEnumerator() As System.Collections.IEnumerator", verifyItem.GetEnumeratorMethod.ToString)
            Assert.Equal("Function MoveNext() As Boolean", verifyItem.MoveNextMethod.ToString)
            Assert.Equal("ReadOnly Property Current As Object", verifyItem.CurrentProperty.ToString)
            Assert.Equal("Sub Dispose()", verifyItem.DisposeMethod.ToString)

        End Sub

        ' Using the iteration variable in the collection expression
        <Fact>
        Public Sub IterationVarInCollectionExpression_1()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="IterationVarInCollectionExpression">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        For Each x As Integer In New Integer() {x + 5, x + 6, x + 7}
            System.Console.WriteLine(x)
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()", "Integer()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        ' Traversing items in 'Nothing'
        <Fact>
        Public Sub TraversingNothingStrictOn()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="TraversingNothing">
    <file name="a.vb">
Option Infer Off
Option Strict On
Class C
    Shared Sub Main()
        For Each item In Nothing
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "<nothing>", "Object")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)

            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        ' Traversing items in 'Nothing'
        <Fact()>
        Public Sub TraversingNothingStrictOff()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="TraversingNothing">
    <file name="a.vb">
Option Strict Off
Class C
    Shared Sub Main()
        For Each item In Nothing
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "<nothing>", "System.Collections.IEnumerable")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="item", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="item", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)

            Dim verifyItem = VerifyForeachSemanticInfo(compilation1)
            Assert.NotNull(verifyItem.GetEnumeratorMethod)
            Assert.NotNull(verifyItem.MoveNextMethod)
            Assert.NotNull(verifyItem.CurrentProperty)
            Assert.NotNull(verifyItem.DisposeMethod)
        End Sub

        ' Nested ForEach can use a var declared in the outer ForEach
        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <WorkItem(542234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542234")>
        <Fact>
        Public Sub NestedForeach()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="NestedForeach">
    <file name="a.vb">
Class C
    Shared Sub Main()
        Dim c(3)() As Integer
        For Each x As Integer() In c
            ReDim x(3)
            For i As Integer = 0 To 3
                x(i) = i
            Next
            For Each y As Integer In x
                System.Console.WriteLine (y)
            Next
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()()", "Integer()()")
            GetDeclareSymbolTestForeach(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="i, x, y", ReadInsideSymbol:="c, i, x, y", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="i, x, y", WrittenOutsideSymbol:="c",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="c", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)

            SemanticInfoTypeTestForeach(compilation1, 2, "Integer()", "Integer()")
            GetDeclareSymbolTestForeach(compilation1, Nothing, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="y", ReadInsideSymbol:="x, y", ReadOutsideSymbol:="c, i, x",
                                             WrittenInsideSymbol:="y", WrittenOutsideSymbol:="c, i, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForeach(compilation1, 2)
            VerifyForeachSemanticInfo(compilation1, 2)
        End Sub

        ' Inner foreach loop referencing the outer foreach loop iteration variable
        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <Fact>
        Public Sub NestedForeach_1()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="NestedForeach_1">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S
            For Each y As Char In x
                System.Console.WriteLine(y)
            Next 
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "String()", "String()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x, y", ReadInsideSymbol:="S, x, y", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x, y", WrittenOutsideSymbol:="S",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="S", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)

            SemanticInfoTypeTestForeach(compilation1, 2, "String", "String")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="y", ReadInsideSymbol:="x, y", ReadOutsideSymbol:="S",
                                             WrittenInsideSymbol:="y", WrittenOutsideSymbol:="S, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForeach(compilation1, 2)
            VerifyForeachSemanticInfo(compilation1, 2)

        End Sub

        ' Breaking from nested Loops
        <Fact>
        Public Sub BreakFromForeach()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="BreakFromForeach">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S
            For Each y As Char In x
                If y = "B"c Then
                    Exit For
                Else
                    System.Console.WriteLine(y)
                End If
            Next
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "String()", "String()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x, y", ReadInsideSymbol:="S, x, y", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x, y", WrittenOutsideSymbol:="S",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="S", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)

            SemanticInfoTypeTestForeach(compilation1, 2, "String", "String")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="y", ReadInsideSymbol:="x, y", ReadOutsideSymbol:="S",
                                             WrittenInsideSymbol:="y", WrittenOutsideSymbol:="S, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForeach(compilation1, 2)
            VerifyForeachSemanticInfo(compilation1, 2)
        End Sub

        ' Continuing for nested Loops
        <WorkItem(542234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542234")>
        <Fact>
        Public Sub ContinueInForeach()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="ContinueInForeach">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S
            For Each y As Char In x
                If y = "B"c Then
                    Continue For
                End If
                System.Console.WriteLine(y)
            Next y
        Next x
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "String()", "String()")
            GetDeclareSymbolTestForeach(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x, y", ReadInsideSymbol:="S, x, y", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x, y", WrittenOutsideSymbol:="S",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="S", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)

            SemanticInfoTypeTestForeach(compilation1, 2, "String", "String")
            GetDeclareSymbolTestForeach(compilation1, Nothing, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="y", ReadInsideSymbol:="x, y", ReadOutsideSymbol:="S",
                                             WrittenInsideSymbol:="y", WrittenOutsideSymbol:="S, x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="x", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForeach(compilation1, 2)
            VerifyForeachSemanticInfo(compilation1, 2)
        End Sub

        ' Query expression works in foreach
        <Fact()>
        Public Sub QueryExpressionInForeach()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="QueryExpressionInForeach">
    <file name="a.vb">
Imports System
Imports System.Linq

Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In From w In New Integer() {1, 2, 3} Select z = w.ToString()
            System.Console.WriteLine(x.ToLower())
        Next
    End Sub
End Class
    </file>
</compilation>, references:={TestBase.LinqAssemblyRef})

            SemanticInfoTypeTestForeach(compilation1, 1, "System.Collections.Generic.IEnumerable(Of String)", "System.Collections.Generic.IEnumerable(Of String)")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="w, x, z", ReadInsideSymbol:="w, x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="w, x, z", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        ' No confusion in a foreach statement when from is a value type
        <Fact>
        Public Sub ReDimFrom()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="ReDimFrom">
    <file name="a.vb">
Option Infer On
Class C
    Public Shared Sub Main()
        Dim src = New System.Collections.ArrayList()
        src.Add(New from(1))
        For Each x As from In src
            System.Console.WriteLine(x.X)
        Next
    End Sub
End Class
Public Structure from
    Dim X As Integer
    Public Sub New(x as Integer)
        X = x
    End Sub
End Structure
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "System.Collections.ArrayList", "System.Collections.ArrayList")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="src, x", ReadOutsideSymbol:="src",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="src",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="src", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub BC30302ERR_TypeCharWithType1()
            Dim compilation1 = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="c.vb">
Option Infer On
Class C
    Shared Sub Main()
        For Each x% In New Integer() {1, 1}
        Next
        For Each x&amp; In New Long() {1, 1}
        Next
        For Each x! In New Double() {1, 1}
        Next
        For Each x# In New Double() {1, 1}
        Next
        For Each x@ In New Decimal() {1, 1}
        Next
        'COMPILEERROR: BC30302
        For Each x% As Long In New Long() {1, 1, 1}
        Next
        For Each x# As Single In New Double() {1, 1, 1}
        Next
        For Each x@ As Decimal In New Decimal() {1, 1, 1}
        Next
        For Each x! As Object In New Long() {1, 1, 1}
        Next
    End Sub
End Class
        </file>
    </compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()", "Integer()")
            SemanticInfoTypeTestForeach(compilation1, 2, "Long()", "Long()")
            SemanticInfoTypeTestForeach(compilation1, 3, "Double()", "Double()")
            SemanticInfoTypeTestForeach(compilation1, 4, "Double()", "Double()")
            SemanticInfoTypeTestForeach(compilation1, 5, "Decimal()", "Decimal()")
            SemanticInfoTypeTestForeach(compilation1, 6, "Long()", "Long()")
            SemanticInfoTypeTestForeach(compilation1, 7, "Double()", "Double()")
            SemanticInfoTypeTestForeach(compilation1, 8, "Decimal()", "Decimal()")
            SemanticInfoTypeTestForeach(compilation1, 9, "Long()", "Long()")
            For i As Integer = 1 To 9

                AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                                 WrittenInsideSymbol:="x", WrittenOutsideSymbol:="x, x, x, x, x, x, x, x",
                                                 AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=i)
                AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                    EndPointIsReachable:=True, index:=i)
                ClassfiConversionTestForeach(compilation1, i)
                VerifyForeachSemanticInfo(compilation1, i)
            Next

        End Sub

        <Fact>
        Public Sub BC30039ERR_LoopControlMustNotBeProperty()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="LoopControlMustNotBeProperty">
    <file name="a.vb">
Option Infer On
Imports System
Class C
    Inherits B
    Property P() As Integer
    Shadows F As Integer
    Sub Method1(A As Integer, ByRef B As Integer)
        ' error
        For Each P In new Integer(){1}
        Next
        ' warning
        For Each F In Integer(){2}
        Next
        For Each Me.F In Integer(){3}
        Next
        For Each MyBase.F In {4}
        Next
        For Each A In {5}
        Next
        For Each B In {6}
        Next
    End Sub
    Shared Sub Main()
    End Sub
End Class
Class B
    Public F As Integer
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()", "Integer()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="Me", ReadOutsideSymbol:="B, Me",
                                             WrittenInsideSymbol:="", WrittenOutsideSymbol:="A, B, Me",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="Me", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub BC30277ERR_TypecharNoMatch2_2()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="TypecharNoMatch2">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        'declare with explicit type, use in next with a type char")
        For Each x As Integer In New Integer() {1, 1, 1}
            'COMPILEERROR: BC30277, "x#"
        Next x#
        For Each [me] As Integer In New Integer() {1, 1, 1}
        Next me%
    End Sub
End Class
    </file>
</compilation>)
            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()", "Integer()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="me",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)

            SemanticInfoTypeTestForeach(compilation1, 2, "Integer()", "Integer()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="me", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="me", WrittenOutsideSymbol:="x",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForeach(compilation1, 2)
            VerifyForeachSemanticInfo(compilation1, 2)
        End Sub

        <Fact>
        Public Sub BC30288ERR_DuplicateLocals1_1()
            Dim compilation1 = CreateCompilationWithMscorlib40(
    <compilation name="DuplicateLocals1">
        <file name="a.vb">
Class C
    Shared Sub Main()
        For Each x As Integer In New Integer() {1, 2, 3}
            Dim x As Integer
        Next
    End Sub
End Class
        </file>
    </compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()", "Integer()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x, x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <WorkItem(542234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542234")>
        <Fact>
        Public Sub BC30290ERR_LocalSameAsFunc_1()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="LocalSameAsFunc">
    <file name="a.vb">
Class C
    Shared Sub Main()
    End Sub
    Function goo()
        'COMPILEERROR: BC30290, 
        For Each goo As Integer In New Integer() {1, 2, 3}
        Next
    End Function
    Sub goo1()
        For Each goo1 As Integer In New Integer() {1, 2, 3}
        Next
    End SUB
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()", "Integer()")
            GetDeclareSymbolTestForeach(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="goo", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="goo", WrittenOutsideSymbol:="Me",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)

            SemanticInfoTypeTestForeach(compilation1, 2, "Integer()", "Integer()")
            GetDeclareSymbolTestForeach(compilation1, Nothing, 2)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="goo1", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="goo1", WrittenOutsideSymbol:="Me",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="", index:=2)
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True, index:=2)
            ClassfiConversionTestForeach(compilation1, 2)
            VerifyForeachSemanticInfo(compilation1, 2)
        End Sub

        <Fact>
        Public Sub BC30311ERR_TypeMismatch2_1()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Shared Sub Main()
        For Each x As Integer In New Exception() {Nothing, Nothing}
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Exception()", "Exception()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub BC30311ERR_TypeMismatch2_2()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim numbers2D As Integer()() = New Integer()() {New Integer() {1, 2}, New Integer() {1, 2}}
        For Each x As Integer In numbers2D
            System.Console.Write("{0} ", x)
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()()", "Integer()()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="numbers2D, x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="numbers2D",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="numbers2D", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub BC30369ERR_BadInstanceMemberAccess_3()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Class C
            Shared Sub Main()
                For Each x As Integer In F(x)
                Next
            End Sub
            Private Sub F(x As Integer)
            End Sub
        End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Void", "Void")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub BC30369ERR_BadInstanceMemberAccess_4()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Class C
            Shared Sub Main()
                For Each x As Integer In F(x)
                Next
            End Sub
            Private Function F(x As Integer) As Object
                Return New Object()
            End Function
        End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Object", "Object")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <WorkItem(542083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542083")>
        <Fact>
        Public Sub BC30369ERR_BadInstanceMemberAccess_5()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Property P1(ByVal x As Integer) As integer
        Get
            Return x +5
        End Get
        Set(ByVal Value As integer)
        End Set
    End Property
    Public Shared Sub Main()
        For Each x As integer In New integer() {P1(x), P1(x), P1(x)}
        Next
    End Sub
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()", "Integer()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub BC30369ERR_BadInstanceMemberAccess_6()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        For Each x As Integer In New Integer() {goo(x), goo(x), goo(x)}
        Next
    End Sub
    Function goo(ByRef x As Integer) As Integer
        x = 10
        Return x + 10
    End Function
End Class
    </file>
</compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer()", "Integer()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)

            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub BC30532ERR_DateToDoubleConversion_1()
            Dim compilation1 = CreateCompilationWithMscorlib40(
    <compilation name="DateToDoubleConversion">
        <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        For Each x As Double In New Date() {#12:00:00 AM#}
        Next
    End Sub
End Class
    </file>
    </compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Date()", "Date()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <WorkItem(542234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542234")>
        <Fact()>
        Public Sub BC32006ERR_CharToIntegralTypeMismatch1_1()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="CharToIntegralTypeMismatch1">
        <file name="a.vb">
        Imports System
        Imports System.Linq

        Class C
            Shared Sub Main()
                For Each x As Integer In From c In "abc" Select c
                Next
            End Sub
        End Class
        Public Structure S
        End Structure
    </file>
    </compilation>, references:={TestBase.LinqAssemblyRef})

            SemanticInfoTypeTestForeach(compilation1, 1, "System.Collections.Generic.IEnumerable(Of Char)", "System.Collections.Generic.IEnumerable(Of Char)")
            GetDeclareSymbolTestForeach(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="c, c, x", ReadInsideSymbol:="c", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="c, c, x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub BC32023ERR_ForEachCollectionDesignPattern1_1()
            Dim compilation1 = CreateCompilationWithMscorlib40(
    <compilation name="ForEachCollectionDesignPattern1">
        <file name="a.vb">
        Class C
            Shared Sub Main()
                For Each x As Integer In If(x, x, x)
                Next
            End Sub
        End Class
    </file>
    </compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "Integer", "Integer")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="x", ReadInsideSymbol:="x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <WorkItem(542234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542234")>
        <Fact>
        Public Sub VarDeclOutOfForeach()
            Dim compilation1 = CreateCompilationWithMscorlib40(
    <compilation name="VarDeclOutOfForeach">
        <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
            Dim z As String
            For Each z In ""
            Next
    End Sub
End Class
    </file>
    </compilation>)

            SemanticInfoTypeTestForeach(compilation1, 1, "String", "String")
            GetDeclareSymbolTestForeach(compilation1, Nothing)
            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="z", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        ' No confusion in a foreach statement when from is a value type
        <WorkItem(542081, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542081")>
        <Fact>
        Public Sub LambdaAsIteration()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="LambdaAsIteration">
    <file name="a.vb">
Imports System
Class C
    Public Shared Sub Main()
        Dim x As Action
        For Each x In New Action() {Sub() Console.WriteLine("hello")}
            x.Invoke()
        Next
    End Sub
End Class
    </file>
</compilation>)
            compilation1.VerifyDiagnostics()
            SemanticInfoTypeTestForeach(compilation1, 1, "System.Action()", "System.Action()")

            AnalyzeRegionDataFlowTestForeach(compilation1, VariablesDeclaredSymbol:="", ReadInsideSymbol:="x", ReadOutsideSymbol:="",
                                             WrittenInsideSymbol:="x", WrittenOutsideSymbol:="",
                                             AlwaysAssignedSymbol:="", DataFlowsInSymbol:="", DataFlowsOutSymbol:="")
            AnalyzeRegionControlFlowTestForeach(compilation1, EntryPoints:=0, ExitPoints:=0,
                                                EndPointIsReachable:=True)
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub


        <Fact>
        Public Sub CollectionHasNoDifferentConvertedTypeForDesignPatternMatch()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="CollectionHasConvertedType">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections

Public Class SomethingEnumerable
    Implements IEnumerable

    Public Function GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C1
    Public Shared Sub Main()
        Dim myCollection As SomethingEnumerable = nothing
        For Each element In myCollection 
            Console.WriteLine("goo")
        Next
    End Sub
End Class
    </file>
</compilation>)
            compilation1.VerifyDiagnostics()
            SemanticInfoTypeTestForeach(compilation1, 1, "SomethingEnumerable", "SomethingEnumerable")
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub CollectionHasConvertedTypeIEnumerable()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="CollectionHasConvertedType">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections

Public Class SomethingEnumerable
    Implements IEnumerable

    Public Function GetEnumerator2() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C1
    Public Shared Sub Main()
        Dim myCollection As SomethingEnumerable = nothing
        For Each element In myCollection 
            Console.WriteLine("goo")
        Next
    End Sub
End Class
    </file>
</compilation>)
            compilation1.VerifyDiagnostics()
            SemanticInfoTypeTestForeach(compilation1, 1, "SomethingEnumerable", "System.Collections.IEnumerable")
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub CollectionHasConvertedTypeGenericIEnumerable()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="CollectionHasConvertedType">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections.Generic

Public Interface IBetterEnumerable(Of T)
    Inherits IEnumerable(Of T)
End Interface

Public Class SomethingEnumerable(Of T)
    Implements IEnumerable(Of T)

    Public Function GetEnumerator1() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
        Return Nothing
    End Function

    Public Function GetEnumerator2() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C1
    Public Shared Sub Main()
        Dim myCollection3 As SomethingEnumerable(Of String) = nothing
        For Each element In myCollection3
            Console.WriteLine("goo")
        Next
    End Sub
End Class        
    </file>
</compilation>)

            compilation1.VerifyDiagnostics()
            SemanticInfoTypeTestForeach(compilation1, 1, "SomethingEnumerable(Of String)", "System.Collections.Generic.IEnumerable(Of String)")
            ClassfiConversionTestForeach(compilation1)
            VerifyForeachSemanticInfo(compilation1)
        End Sub

        <Fact>
        Public Sub GetDeclaredSymbolOfForEachStatement()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation name="CollectionHasConvertedType">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collection

Class C1
    Public Shared Sub Main()
        For Each element1 In new List()
        Next

        For Each element2 as Object In new List()
        Next
    End Sub
End Class        
    </file>
</compilation>)
            GetDeclareSymbolTestForeach(compilation1, Nothing, 1)
            GetDeclareSymbolTestForeach(compilation1, Nothing, 2)
        End Sub

        <WorkItem(667616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667616")>
        <Fact>
        Public Sub PortableLibraryStringForEach()
            Dim comp = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb">
Public Class C
    Public Sub Test(s As String)
        For Each c In s
        Next
    End Sub
End Class
    </file>
</compilation>, {MscorlibRefPortable})

            comp.VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim loopSyntax = tree.GetRoot().DescendantNodes().OfType(Of ForEachStatementSyntax)().Single()

            Dim loopInfo = model.GetForEachStatementInfo(loopSyntax)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator), loopInfo.GetEnumeratorMethod)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current), loopInfo.CurrentProperty)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), loopInfo.MoveNextMethod)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), loopInfo.DisposeMethod)

            ' The spec says that the element type is object.
            ' Therefore, we should infer object for "var".
            Assert.Equal(SpecialType.System_Object, loopInfo.CurrentProperty.Type.SpecialType)

            ' However, to match dev11, we actually infer "char" for "var".
            Dim typeInfo = model.GetTypeInfo(DirectCast(loopSyntax.ControlVariable, IdentifierNameSyntax))
            Assert.Equal(SpecialType.System_Char, typeInfo.Type.SpecialType)
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType)
            Dim conv = model.GetConversion(loopSyntax.ControlVariable)
            Assert.Equal(ConversionKind.Identity, conv.Kind)
        End Sub

        <WorkItem(667616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667616")>
        <Fact>
        Public Sub PortableLibraryStringForEach_ExplicitCast()
            Dim comp = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb">
Public Class C
    Public Sub Test(s As Object)
        For Each c In DirectCast(s, String)
        Next
    End Sub
End Class
    </file>
</compilation>, {MscorlibRefPortable})

            comp.VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim loopSyntax = tree.GetRoot().DescendantNodes().OfType(Of ForEachStatementSyntax)().Single()

            Dim loopInfo = model.GetForEachStatementInfo(loopSyntax)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator), loopInfo.GetEnumeratorMethod)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current), loopInfo.CurrentProperty)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), loopInfo.MoveNextMethod)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), loopInfo.DisposeMethod)

            ' The spec says that the element type is object.
            ' Therefore, we should infer object for "var".
            Assert.Equal(SpecialType.System_Object, loopInfo.CurrentProperty.Type.SpecialType)

            ' However, to match dev11, we actually infer "char" for "var".
            Dim typeInfo = model.GetTypeInfo(DirectCast(loopSyntax.ControlVariable, IdentifierNameSyntax))
            Assert.Equal(SpecialType.System_Char, typeInfo.Type.SpecialType)
            Assert.Equal(typeInfo.Type, typeInfo.ConvertedType)
            Dim conv = model.GetConversion(loopSyntax.ControlVariable)
            Assert.Equal(ConversionKind.Identity, conv.Kind)
        End Sub

        <WorkItem(529956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529956")>
        <Fact>
        Public Sub CastArrayToIEnumerable()
            Dim source =
                <compilation>
                    <file name="a.vb">
Imports System
Imports System.Collections

Public Class C
    Public Shared Widening Operator CType(s As String) As C
        Return New C()
    End Operator
End Class

Module Program
    Sub Main(args As String())
        For Each x As C In args
        Next
        For Each x As C In DirectCast(args, IEnumerable)
        Next
    End Sub
End Module
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            comp.AssertNoDiagnostics()

            Dim udc = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)(WellKnownMemberNames.ImplicitConversionName)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim loopSyntaxes = tree.GetRoot().DescendantNodes().OfType(Of ForEachStatementSyntax)().ToArray()
            Assert.Equal(2, loopSyntaxes.Length)

            Dim loopInfo0 = model.GetForEachStatementInfo(loopSyntaxes(0))
            Assert.Equal(comp.GetSpecialType(SpecialType.System_Array), loopInfo0.GetEnumeratorMethod.ContainingType) ' Unlike C#, the spec doesn't say that arrays use IEnumerable
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current), loopInfo0.CurrentProperty)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), loopInfo0.MoveNextMethod)
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), loopInfo0.DisposeMethod)
            Assert.Equal(SpecialType.System_String, loopInfo0.ElementType.SpecialType)
            Assert.Equal(udc, loopInfo0.ElementConversion.Method)
            Assert.Equal(ConversionKind.NarrowingReference, loopInfo0.CurrentConversion.Kind)

            Dim loopInfo1 = model.GetForEachStatementInfo(loopSyntaxes(1))
            Assert.Equal(Of ISymbol)(comp.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator), loopInfo1.GetEnumeratorMethod) ' No longer using System.Array method.
            Assert.Equal(loopInfo0.CurrentProperty, loopInfo1.CurrentProperty)
            Assert.Equal(loopInfo0.MoveNextMethod, loopInfo1.MoveNextMethod)
            Assert.Equal(loopInfo0.DisposeMethod, loopInfo1.DisposeMethod)
            Assert.Equal(SpecialType.System_Object, loopInfo1.ElementType.SpecialType) ' No longer string.
            Assert.Null(loopInfo1.ElementConversion.Method) ' No longer using UDC.
            Assert.Equal(ConversionKind.Identity, loopInfo1.CurrentConversion.Kind) ' Now identity
        End Sub

        Private Function SemanticInfoTypeTestForeach(compilation As VisualBasicCompilation, index As Integer, ParamArray names As String()) As SemanticInfoSummary
            Dim node = GetForEachStatement(compilation, index)
            Dim expression = node.Expression
            Dim model = GetModel(compilation)
            Dim semanticInfo = model.GetSemanticInfoSummary(expression)

            If "<nothing>" = names(0) Then
                Assert.Null(semanticInfo.Type)
            Else
                Assert.Equal(names(0), semanticInfo.Type.ToDisplayString())
            End If

            If names.Count > 1 Then
                Assert.Equal(names(1), semanticInfo.ConvertedType.ToDisplayString())
                If names(0) = "Object" Then
                    If names(1) = "Object" Then
                        Assert.True(Conversions.IsIdentityConversion(semanticInfo.ImplicitConversion.Kind))
                    Else
                        Assert.True(Conversions.IsNarrowingConversion(semanticInfo.ImplicitConversion.Kind))
                    End If
                Else
                    Assert.True(Conversions.IsWideningConversion(semanticInfo.ImplicitConversion.Kind))
                End If
            Else
                Assert.Equal(names(0), semanticInfo.ConvertedType.ToDisplayString())
                Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            End If

            Return semanticInfo
        End Function

        Private Function GetDeclareSymbolTestForeach(compilation As VisualBasicCompilation, symName As String, Optional index As Integer = 1) As Symbol
            Dim node = GetForEachStatement(compilation, index)
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

        Private Function AnalyzeRegionDataFlowTestForeach(compilation As VisualBasicCompilation, VariablesDeclaredSymbol As String,
                         ReadInsideSymbol As String, ReadOutsideSymbol As String, WrittenInsideSymbol As String,
                         WrittenOutsideSymbol As String, AlwaysAssignedSymbol As String,
                         DataFlowsInSymbol As String, DataFlowsOutSymbol As String,
                         Optional index As Integer = 1) As DataFlowAnalysis
            Dim node = GetForEachBlock(compilation, index)
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

        Private Function AnalyzeRegionControlFlowTestForeach(
                         compilation As VisualBasicCompilation,
                         Optional EntryPoints As Integer = 0,
                         Optional ExitPoints As Integer = 0,
                         Optional EndPointIsReachable As Boolean = True,
                         Optional index As Integer = 1) As ControlFlowAnalysis
            Dim node = GetForEachBlock(compilation, index)
            Dim model = GetModel(compilation)
            Dim analyze = model.AnalyzeControlFlow(node, node)
            Assert.Equal(EntryPoints, analyze.EntryPoints.Count)
            Assert.Equal(ExitPoints, analyze.ExitPoints.Count)
            Assert.Equal(EndPointIsReachable, analyze.EndPointIsReachable)
            Return analyze
        End Function

        Private Function ClassfiConversionTestForeach(compilation As VisualBasicCompilation, Optional index As Integer = 1) As Conversion
            Dim node = GetForEachStatement(compilation, index)
            Dim expression = node.Expression
            Dim model = GetModel(compilation)
            Dim semanticInfo = model.GetSemanticInfoSummary(expression)
            If semanticInfo.ConvertedType Is Nothing Then
                Return Nothing
            End If
            Dim conv = model.ClassifyConversion(expression, semanticInfo.ConvertedType)

            If (conv.Kind = ConversionKind.Identity) Then
                Assert.True(conv.Exists)
                Assert.True(conv.IsIdentity)
            End If

            If (semanticInfo.Type IsNot Nothing AndAlso
                semanticInfo.ConvertedType IsNot Nothing AndAlso
                semanticInfo.Type.ToDisplayString() <> "?" AndAlso
                semanticInfo.Type.ToDisplayString() <> "Void" AndAlso
                semanticInfo.ConvertedType.ToDisplayString() <> "?" AndAlso
                semanticInfo.ConvertedType.ToDisplayString() <> "Void") Then
                Assert.Equal(conv.Kind, semanticInfo.ImplicitConversion.Kind)
            End If

            Return conv
        End Function

        Private Function GetSymbolNamesSortedAndJoined(Of T As ISymbol)(symbols As IEnumerable(Of T)) As String
            Return String.Join(", ", symbols.Select(Function(symbol) symbol.Name).OrderBy(Function(name) name))
        End Function

        Private Function GetModel(compilation As VisualBasicCompilation) As SemanticModel
            Dim tree = compilation.SyntaxTrees.First
            Dim model = compilation.GetSemanticModel(tree)
            Return model
        End Function

        Private Function GetForEachStatement(compilation As VisualBasicCompilation, index As Integer) As ForEachStatementSyntax
            Dim tree = compilation.SyntaxTrees.First
            Dim node = tree.FindNodeOrTokenByKind(SyntaxKind.ForEachStatement, index).AsNode()
            Dim ForEachStatement = TryCast(node, ForEachStatementSyntax)
            Return ForEachStatement
        End Function

        Private Function GetForEachBlock(compilation As VisualBasicCompilation, index As Integer) As ForEachBlockSyntax
            Dim tree = compilation.SyntaxTrees.First
            Dim node = tree.FindNodeOrTokenByKind(SyntaxKind.ForEachBlock, index).AsNode()
            Return DirectCast(node, ForEachBlockSyntax)
        End Function

        Private Function VerifyForeachSemanticInfo(compilation As VisualBasicCompilation, Optional index As Integer = 1) As ForEachStatementInfo
            Dim node = GetForEachBlock(compilation, index)
            Dim semanticModel = GetModel(compilation)
            Dim foreachStatementInfo = semanticModel.GetForEachStatementInfo(node)

            'Assert.Null(foreachStatementInfo.CurrentProperty)
            'Assert.Null(foreachStatementInfo.DisposeMethod)
            'Assert.Null(foreachStatementInfo.GetEnumeratorMethod)
            'Assert.Null(foreachStatementInfo.MoveNextMethod)
            Return (foreachStatementInfo)
        End Function

        'Private Function GetVariableDeclarator(compilation As VisualBasicCompilation, index As Integer) As VariableDeclaratorSyntax
        '    Dim node = GetForEachStatement(compilation, index)
        '    Dim VariableDeclarator = node.DescendantNodes.Select(Function(x) TryCast(x, VariableDeclaratorSyntax)).Where(Function(x) x IsNot Nothing).ToList()
        '    Assert.True(VariableDeclarator.Count() <= 1)
        '    Return If(VariableDeclarator.Count = 1, VariableDeclarator.First(), Nothing)
        'End Function

    End Class
End Namespace

