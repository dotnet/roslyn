' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Partial Public Class IOperationTests
        Inherits BasicTestBase

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_ForNull_ThrowsArgumentNullException()
            Assert.ThrowsAny(Of ArgumentNullException)(Function() OperationExtensions.GetCorrespondingOperation(Nothing))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_ForGotoBranch_ReturnsNull()
            Dim result = GetOuterOperationAndCorrespondingInnerOperation(Of LabelStatementSyntax, GoToStatementSyntax)(
            <![CDATA[
Class C
    Sub F
begin: 'BIND1:"begin:"
        For i = 0 To 1
            GoTo begin 'BIND2:"GoTo begin"
        Next
    End Sub
End Class
]]>.Value)

            Assert.IsAssignableFrom(GetType(ILabeledOperation), result.outer)
            Assert.Null(result.corresponding)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_ForLoopWithExit()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
            Exit For 'BIND2:"Exit For"
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_WhileLoopWithContinue()
            AssertOuterIsCorrespondingLoopOfInner(Of WhileBlockSyntax, ContinueStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        While True 'BIND1:"While True"
            Continue While 'BIND2:"Continue While"
        End While
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_DoWhileLoopWithExitAndContinue()
            AssertOuterIsCorrespondingLoopOfInner(Of DoLoopBlockSyntax, ContinueStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        Do 'BIND1:"Do"
            If True
                Exit Do
            Else
                Continue Do 'BIND2:"Continue Do"
            End If
        Loop While True
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_ForEachLoopWithExit()
            AssertOuterIsCorrespondingLoopOfInner(Of ForEachBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For Each i In {1,2,3} 'BIND1:"For Each i In {1,2,3}"
            If i = 2
                Exit For 'BIND2:"Exit For"
            End If
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_ForEachLoopWithExitAndContinue()
            AssertOuterIsCorrespondingLoopOfInner(Of ForEachBlockSyntax, ContinueStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For Each i In {1,2,3} 'BIND1:"For Each i In {1,2,3}"
            If i Mod 2 = 0
                Exit For
            Else
                Continue For 'BIND2:"Continue For"
            End If
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_NestedLoops()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
            For j = 0 To 1
            Next
            Exit For 'BIND2:"Exit For"
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_NestedLoops2()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 
            For j = 0 To 1 'BIND1:"For j = 0 To 1"
                Exit For 'BIND2:"Exit For"
            Next
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_SwitchLookup_ExitInCase()
            AssertOuterIsCorrespondingSwitchOfInner(
            <![CDATA[
Class C
    Sub F
        Select Case 1 'BIND1:"Select Case 1"
            Case 1
                Exit Select 'BIND2:"Exit Select"
        End Select
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_SwitchLookup_NestedSelects()
            AssertOuterIsCorrespondingSwitchOfInner(
            <![CDATA[
Class C
    Sub F
        Select Case 1 'BIND1:"Select Case 1"
            Case 1
                Select Case 2
                    Case 2
                End Select
                Exit Select 'BIND2:"Exit Select"
        End Select
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_SwitchLookup_NestedSelects2()
            AssertOuterIsCorrespondingSwitchOfInner(
            <![CDATA[
Class C
    Sub F
        Select Case 1
            Case 1
                Select Case 2 'BIND1:"Select Case 2"
                    Case 2
                        Exit Select 'BIND2:"Exit Select"
                End Select
        End Select
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_LoopInSelect()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        Select Case 1
            Case 1
                For i = 0 To 1 'BIND1:"For i = 0 To 1"
                    Exit For 'BIND2:"Exit For"
                Next
        End Select
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_SwitchLookup_SelectInLoop()
            AssertOuterIsCorrespondingSwitchOfInner(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1
            Select Case 1 'BIND1:"Select Case 1"
                Case 1
                    Exit Select 'BIND2:"Exit Select"
            End Select
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_ContinueNestedInIntermediateSelect()
            AssertOuterIsCorrespondingLoopOfInner(Of ForBlockSyntax, ContinueStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
            Select Case 1 
                Case 1
                    Continue For 'BIND2:"Continue For"
            End Select
        Next
    End Sub
End Class
]]>.Value)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_LoopLookup_ExitButNoLoop_ReturnsNull()
            Dim result = GetOuterOperationAndCorrespondingInnerOperation(Of ForBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        ' the following loop is just for utilize the common testing method (which expects 2 bindings in source)    
        For i = 0 To 1 'BIND1:"For i = 0 To 1"
        Next

        Exit For 'BIND2:"Exit For"
    End Sub
End Class
]]>.Value)

            Assert.IsAssignableFrom(GetType(ILoopOperation), result.outer)
            Assert.Null(result.corresponding)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")>
        <Fact>
        Public Sub GetCorrespondingOperation_SwitchLookup_ExitButNoSwitch_ReturnsNull()
            Dim result = GetOuterOperationAndCorrespondingInnerOperation(Of SelectBlockSyntax, ExitStatementSyntax)(
            <![CDATA[
Class C
    Sub F
        ' the following switch is just for utilize the common testing method (which expects 2 bindings in source)
        Select Case 1 'BIND1:"Select Case 1"
            Case 1
        End Select

        Exit Select 'BIND2:"Exit Select"
    End Sub
End Class
]]>.Value)

            Assert.IsAssignableFrom(GetType(ISwitchOperation), result.outer)
            Assert.Null(result.corresponding)
        End Sub

        Private Sub AssertOuterIsCorrespondingLoopOfInner(Of TOuterSyntax As SyntaxNode, TInnerSyntax As SyntaxNode)(source As string)
            Dim result As (expected As IOperation, actual As IOperation)
            result = GetOuterOperationAndCorrespondingInnerOperation(Of TOuterSyntax, TInnerSyntax)(source)

            Assert.Equal(result.expected.Syntax, result.actual.Syntax)
        End Sub

        Private Sub AssertOuterIsCorrespondingSwitchOfInner(source As string)
            Dim result As (expected As IOperation, actual As IOperation)
            result = GetOuterOperationAndCorrespondingInnerOperation(Of SelectBlockSyntax, ExitStatementSyntax)(source)

            Assert.Equal(result.expected.Syntax, result.actual.Syntax)
        End Sub

        Private Function GetOuterOperationAndCorrespondingInnerOperation(Of TOuterSyntax As SyntaxNode, TInnerSyntax As SyntaxNode)(
            source As string) As (outer As IOperation, corresponding As IOperation)

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(source, fileName)
            Dim compilation = CreateEmptyCompilation({syntaxTree})

            Dim outer = GetOperationAndSyntaxForTest(Of TOuterSyntax)(compilation, fileName, 1).operation
            Dim inner = TryCast(GetOperationAndSyntaxForTest(Of TInnerSyntax)(compilation, fileName, 2).operation, IBranchOperation)
            Dim correspondingOfInner = inner?.GetCorrespondingOperation()

            Return (outer, correspondingOfInner)

        End Function

    End Class

End Namespace
