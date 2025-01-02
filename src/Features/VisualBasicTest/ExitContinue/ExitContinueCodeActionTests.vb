' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.ExitContinue
    <Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
    Public Class ExitContinueCodeActionTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New IncorrectExitContinueCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestExpectedExitKind_Sub() As Task
            Dim code =
<File>
Class C
    Sub goo()
        [|Exit|]
    End Sub
End Class
</File>

            Dim expected =
<File>
Class C
    Sub goo()
        Exit Sub
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedExitKind_While() As Task
            Dim code =
    <File>
Class C
    Sub goo()
        While True
            [|Exit|]
        End While
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub goo()
        While True
            Exit While
        End While
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedExitKind_For() As Task
            Dim code =
    <File>
Class C
    Sub goo()
        For x as Integer = 1 to 10
            [|Exit|]
        Next
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub goo()
        For x as Integer = 1 to 10
            Exit For
        Next
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedExitKind_Do() As Task
            Dim code =
    <File>
Class C
    Sub goo()
        Do While True
            [|Exit|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub goo()
        Do While True
            Exit Do
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExitPropNot() As Task
            Dim code =
    <File>
Class C
    Property P as Integer
        Get
            [|Exit|]
        End Get
        Set
        End Set
    End Property
Exit Class
</File>

            Dim expected =
    <File>
Class C
    Property P as Integer
        Get
            Exit Property
        End Get
        Set
        End Set
    End Property
Exit Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedExitKind_Try() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        Try
            [|Exit|]
        Catch ex As Exception

        End Try
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo()
        Try
            Exit Try
        Catch ex As Exception

        End Try
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedExitKind_Function() As Task
            Dim code =
    <File>
Class C
    Function x() as Integer
        [|Exit|]
    End Function
End Class
</File>

            Dim expected =
    <File>
Class C
    Function x() as Integer
        Exit Function
    End Function
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExitSubOfFunc() As Task
            Dim code =
    <File>
Class C
    Function x() as Integer
        [|Exit Sub|]
    End Function
End Class
</File>

            Dim expected =
    <File>
Class C
    Function x() as Integer
        Exit Function
    End Function
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExitFuncOfSub() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        [|Exit Function|]
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo()
        Exit Sub
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExitDoNotWithinDo() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        While True
            [|Exit Do|]
        End While
    End Sub
End Class
</File>

            Dim expected =
     <File>
Class C
    Sub Goo()
        While True
            Exit While
        End While
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExitDoNotWithinDo_For() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        For x as Integer = 1 to 10
            [|Exit Do|]
        Next
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo()
        For x as Integer = 1 to 10
            Exit For
        Next
    End Sub
End Class
</File>

            Await TestAsync(code, expected, index:=0)
        End Function

        <Fact>
        Public Async Function TestExitWhileNotWithinWhile() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        Do While True
            [|Exit While|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo()
        Do While True
            Exit Do
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExitDoNotWithinDo_Try() As Task
            Dim code =
    <File>
Imports System
Class C
    Sub Goo()
        Try
            [|Exit Do|]
        Catch ex As Exception

        End Try
    End Sub
End Class
</File>

            Dim expected =
    <File>
Imports System
Class C
    Sub Goo()
        Try
            Exit Try
        Catch ex As Exception

        End Try
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExitTryNotWithinTry() As Task
            Dim code =
    <File>
Class C
    Sub Goo
        [|Exit Try|]
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo
        Exit Sub
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExitChangeToSelect() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        Dim i as Integer = 0
        Select Case i
            Case 0
                [|Exit Do|]
        End Select
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo()
        Dim i as Integer = 0
        Select Case i
            Case 0
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestContinueDoNotWithinDo() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        Dim i as Integer = 0
        Select Case i
            Case 0
                [|Continue Do|]
        End Select
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo()
        Dim i as Integer = 0
        Select Case i
            Case 0
        End Select
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestContinueForNotWithinFor() As Task
            Dim code =
    <File>
Class C
    Sub Goo
        Dim i as Integer = 0
        Select Case i
            Case 0
                [|Continue For|]
        End Select
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo
        Dim i as Integer = 0
        Select Case i
            Case 0
        End Select
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestContinueWhileNotWithinWhile() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        Dim i as Integer = 0
        Select Case i
            Case 0
                [|Continue While|]
        End Select
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo()
        Dim i as Integer = 0
        Select Case i
            Case 0
        End Select
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedContinueKindWhile() As Task
            Dim code =
    <File>
Class C
    Sub Goo()
        While True
            [|Continue|]
        End While
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo()
        While True
            Continue While
        End While
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedContinueKindFor() As Task
            Dim code =
    <File>
Class C
    Sub Goo
        For x as integer = 1 to 10
            [|Continue|]
        Next
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo
        For x as integer = 1 to 10
            Continue For
        Next
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedContinueKindForEach() As Task
            Dim code =
    <File>
Class C
    Sub Goo
        For Each x in {1}
            [|Continue|]
        Next
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo
        For Each x in {1}
            Continue For
        Next
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedContinueKindDo() As Task
            Dim code =
    <File>
Class C
    Sub Goo
        Do While True
            [|Continue|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo
        Do While True
            Continue Do
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedContinueKindDo_ReplaceFor() As Task
            Dim code =
    <File>
Class C
    Sub Goo
        Do While True
            [|Continue For|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo
        Do While True
            Continue Do
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedExitKindDo_UseSub() As Task
            Dim code =
    <File>
Class C
    Sub Goo
        Do While True
            [|Exit|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Goo
        Do While True
            Exit Sub
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected, index:=1)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547094")>
        Public Async Function TestDoNotTryToExitFinally() As Task
            Dim code =
    <File>
Imports System
Class C
    Function Goo() As Integer
        Try

        Catch ex As Exception

        Finally
            [|Exit|]
        End Try
    End Function
End Class
</File>

            Dim expected =
    <File>
Imports System
Class C
    Function Goo() As Integer
        Try

        Catch ex As Exception

        Finally
        End Try
    End Function
End Class
</File>
            Await TestAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547110")>
        Public Async Function TestMissingExitTokenInNonExitableBlock() As Task
            Dim code =
    <File>
Imports System
Class C
    Function Goo() As Integer
        Try
            If True Then
                [|Exit|]
            End If
        Catch ex As Exception

        Finally
        End Try
    End Function
End Class
</File>

            Dim expected =
    <File>
Imports System
Class C
    Function Goo() As Integer
        Try
            If True Then
                Exit Try
            End If
        Catch ex As Exception

        Finally
        End Try
    End Function
End Class
</File>
            Await TestAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547100")>
        Public Async Function TestNotInValidCaseElse() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        For Each a In args
            Select a
                Case Else
                    [|Exit Select|] ' here 
            End Select
        Next
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547099")>
        Public Async Function TestCollapseDuplicateBlockKinds() As Task
            Await TestActionCountAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Do
            Do While True
                [|Exit Function|] ' here 
            Loop
        Loop
    End Sub
End Module",
            3)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547092")>
        Public Async Function TestReplaceInvalidTokenExit() As Task
            Dim code =
    <File>
Imports System
Class C
    Function Goo() As Integer
        Try
            If True Then
                [|Exit |]blah
            End If
        Catch ex As Exception

        Finally
        End Try
    End Function
End Class
</File>

            Dim expected =
    <File>
Imports System
Class C
    Function Goo() As Integer
        Try
            If True Then
                Exit Try
            End If
        Catch ex As Exception

        Finally
        End Try
    End Function
End Class
</File>
            Await TestAsync(code, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547092")>
        Public Async Function TestReplaceInvalidTokenContinue() As Task
            Dim code =
    <File>
Imports System
Class C
    Function Goo() As Integer
        Do
            [|Continue |]blah
        Loop
    End Function
End Class
</File>

            Dim expected =
    <File>
Imports System
Class C
    Function Goo() As Integer
        Do
            Continue Do
        Loop
    End Function
End Class
</File>
            Await TestAsync(code, expected)
        End Function

        <Fact>
        Public Async Function TestExpectedActionDescriptions1() As Task
            Dim code =
<File>
Class C
    Sub goo()
        [|Exit Function|]
    End Sub
End Class
</File>

            Await TestExactActionSetOfferedAsync(code.ConvertTestSourceTag(), {String.Format(FeaturesResources.Change_0_to_1, "Function", "Sub"), String.Format(VBFeaturesResources.Delete_the_0_statement1, "Exit Function")})
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531354")>
        Public Async Function TestExpectedActionDescriptions2() As Task
            Dim code =
<File>
Class C
    Sub goo()
        [|Exit    |]
    End Sub
End Class
</File>

            Await TestExactActionSetOfferedAsync(code.ConvertTestSourceTag(), {String.Format(VBFeaturesResources.Insert_0, "Sub"), String.Format(VBFeaturesResources.Delete_the_0_statement1, "Exit")})
        End Function
    End Class
End Namespace
