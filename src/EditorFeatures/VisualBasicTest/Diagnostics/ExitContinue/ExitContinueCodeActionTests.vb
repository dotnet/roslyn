' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.ExitContinue

    Public Class ExitContinueCodeActionTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing, New IncorrectExitContinueCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedExitKind_Sub() As Task
            Dim code =
<File>
Class C
    Sub foo()
        [|Exit|]
    End Sub
End Class
</File>

            Dim expected =
<File>
Class C
    Sub foo()
        Exit Sub
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedExitKind_While() As Task
            Dim code =
    <File>
Class C
    Sub foo()
        While True
            [|Exit|]
        End While
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub foo()
        While True
            Exit While
        End While
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedExitKind_For() As Task
            Dim code =
    <File>
Class C
    Sub foo()
        For x as Integer = 1 to 10
            [|Exit|]
        Next
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub foo()
        For x as Integer = 1 to 10
            Exit For
        Next
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedExitKind_Do() As Task
            Dim code =
    <File>
Class C
    Sub foo()
        Do While True
            [|Exit|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub foo()
        Do While True
            Exit Do
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
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

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedExitKind_Try() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
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
    Sub Foo()
        Try
            Exit Try
        Catch ex As Exception

        End Try
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
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

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
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

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExitFuncOfSub() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
        [|Exit Function|]
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo()
        Exit Sub
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExitDoNotWithinDo() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
        While True
            [|Exit Do|]
        End While
    End Sub
End Class
</File>

            Dim expected =
     <File>
Class C
    Sub Foo()
        While True
            Exit While
        End While
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExitDoNotWithinDo_For() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
        For x as Integer = 1 to 10
            [|Exit Do|]
        Next
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo()
        For x as Integer = 1 to 10
            Exit For
        Next
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExitWhileNotWithinWhile() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
        Do While True
            [|Exit While|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo()
        Do While True
            Exit Do
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExitDoNotWithinDo_Try() As Task
            Dim code =
    <File>
Imports System
Class C
    Sub Foo()
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
    Sub Foo()
        Try
            Exit Try
        Catch ex As Exception

        End Try
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExitTryNotWithinTry() As Task
            Dim code =
    <File>
Class C
    Sub Foo
        [|Exit Try|]
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo
        Exit Sub
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExitChangeToSelect() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
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
    Sub Foo()
        Dim i as Integer = 0
        Select Case i
            Case 0
                Exit Select
        End Select
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestContinueDoNotWithinDo() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
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
    Sub Foo()
        Dim i as Integer = 0
        Select Case i
            Case 0
        End Select
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestContinueForNotWithinFor() As Task
            Dim code =
    <File>
Class C
    Sub Foo
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
    Sub Foo
        Dim i as Integer = 0
        Select Case i
            Case 0
        End Select
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestContinueWhileNotWithinWhile() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
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
    Sub Foo()
        Dim i as Integer = 0
        Select Case i
            Case 0
        End Select
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedContinueKindWhile() As Task
            Dim code =
    <File>
Class C
    Sub Foo()
        While True
            [|Continue|]
        End While
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo()
        While True
            Continue While
        End While
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedContinueKindFor() As Task
            Dim code =
    <File>
Class C
    Sub Foo
        For x as integer = 1 to 10
            [|Continue|]
        Next
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo
        For x as integer = 1 to 10
            Continue For
        Next
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedContinueKindForEach() As Task
            Dim code =
    <File>
Class C
    Sub Foo
        For Each x in {1}
            [|Continue|]
        Next
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo
        For Each x in {1}
            Continue For
        Next
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedContinueKindDo() As Task
            Dim code =
    <File>
Class C
    Sub Foo
        Do While True
            [|Continue|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo
        Do While True
            Continue Do
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedContinueKindDo_ReplaceFor() As Task
            Dim code =
    <File>
Class C
    Sub Foo
        Do While True
            [|Continue For|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo
        Do While True
            Continue Do
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedExitKindDo_UseSub() As Task
            Dim code =
    <File>
Class C
    Sub Foo
        Do While True
            [|Exit|]
        Loop
    End Sub
End Class
</File>

            Dim expected =
    <File>
Class C
    Sub Foo
        Do While True
            Exit Sub
        Loop
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False, index:=1)
        End Function

        <WorkItem(547094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547094")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestDoNotTryToExitFinally() As Task
            Dim code =
    <File>
Imports System
Class C
    Function Foo() As Integer
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
    Function Foo() As Integer
        Try

        Catch ex As Exception

        Finally
        End Try
    End Function
End Class
</File>
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(547110, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547110")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestMissingExitTokenInNonExitableBlock() As Task
            Dim code =
    <File>
Imports System
Class C
    Function Foo() As Integer
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
    Function Foo() As Integer
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
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(547100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547100")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestNotInValidCaseElse() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n For Each a In args \n Select a \n Case Else \n [|Exit Select|] ' here \n End Select \n Next \n End Sub \n End Module"))
        End Function

        <WorkItem(547099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547099")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestCollapseDuplicateBlockKinds() As Task
            Await TestActionCountAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Do \n Do While True \n [|Exit Function|] ' here \n Loop \n Loop \n End Sub \n End Module"),
            3)
        End Function

        <WorkItem(547092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547092")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestReplaceInvalidTokenExit() As Task
            Dim code =
    <File>
Imports System
Class C
    Function Foo() As Integer
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
    Function Foo() As Integer
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
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(547092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547092")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestReplaceInvalidTokenContinue() As Task
            Dim code =
    <File>
Imports System
Class C
    Function Foo() As Integer
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
    Function Foo() As Integer
        Do
            Continue Do
        Loop
    End Function
End Class
</File>
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedActionDescriptions1() As Task
            Dim code =
<File>
Class C
    Sub foo()
        [|Exit Function|]
    End Sub
End Class
</File>

            Await TestExactActionSetOfferedAsync(code.ConvertTestSourceTag(), {String.Format(FeaturesResources.ChangeTo, "Function", "Sub"), String.Format(VBFeaturesResources.DeleteTheStatement, "Exit Function")})
        End Function

        <WorkItem(531354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531354")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Async Function TestExpectedActionDescriptions2() As Task
            Dim code =
<File>
Class C
    Sub foo()
        [|Exit    |]
    End Sub
End Class
</File>

            Await TestExactActionSetOfferedAsync(code.ConvertTestSourceTag(), {String.Format(VBFeaturesResources.Insert, "Sub"), String.Format(VBFeaturesResources.DeleteTheStatement, "Exit")})
        End Function
    End Class
End Namespace