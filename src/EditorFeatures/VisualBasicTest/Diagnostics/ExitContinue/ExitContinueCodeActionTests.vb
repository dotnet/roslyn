' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedExitKind_Sub()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedExitKind_While()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedExitKind_For()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedExitKind_Do()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitPropNot()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedExitKind_Try()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedExitKind_Function()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitSubOfFunc()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitFuncOfSub()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitDoNotWithinDo()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitDoNotWithinDo_For()
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

            Test(code, expected, compareTokens:=False, index:=0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitWhileNotWithinWhile()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitDoNotWithinDo_Try()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitTryNotWithinTry()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExitChangeToSelect()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ContinueDoNotWithinDo()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ContinueForNotWithinFor()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ContinueWhileNotWithinWhile()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedContinueKindWhile()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedContinueKindFor()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedContinueKindForEach()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedContinueKindDo()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedContinueKindDo_ReplaceFor()
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

            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedExitKindDo_UseSub()
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

            Test(code, expected, compareTokens:=False, index:=1)
        End Sub

        <WorkItem(547094)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub DoNotTryToExitFinally()
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
            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(547110)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub MissingExitTokenInNonExitableBlock()
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
            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(547100)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub NotInValidCaseElse()
            TestMissing(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n For Each a In args \n Select a \n Case Else \n [|Exit Select|] ' here \n End Select \n Next \n End Sub \n End Module"))
        End Sub

        <WorkItem(547099)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub CollapseDuplicateBlockKinds()
            TestActionCount(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Do \n Do While True \n [|Exit Function|] ' here \n Loop \n Loop \n End Sub \n End Module"),
            3)
        End Sub

        <WorkItem(547092)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ReplaceInvalidTokenExit()
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
            Test(code, expected, compareTokens:=False)
        End Sub

        <WorkItem(547092)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ReplaceInvalidTokenContinue()
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
            Test(code, expected, compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedActionDescriptions1()
            Dim code =
<File>
Class C
    Sub foo()
        [|Exit Function|]
    End Sub
End Class
</File>

            TestExactActionSetOffered(code.ConvertTestSourceTag(), {String.Format(FeaturesResources.ChangeTo, "Function", "Sub"), String.Format(VBFeaturesResources.DeleteTheStatement, "Exit Function")})
        End Sub

        <WorkItem(531354)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectExitContinue)>
        Public Sub ExpectedActionDescriptions2()
            Dim code =
<File>
Class C
    Sub foo()
        [|Exit    |]
    End Sub
End Class
</File>

            TestExactActionSetOffered(code.ConvertTestSourceTag(), {String.Format(VBFeaturesResources.Insert, "Sub"), String.Format(VBFeaturesResources.DeleteTheStatement, "Exit")})
        End Sub

    End Class
End Namespace

