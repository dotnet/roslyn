' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ExitKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInSubBody()
            VerifyRecommendationsContain(<ClassDeclaration>
Sub Foo()
|
End Sub</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInFunction()
            VerifyRecommendationsContain(<ClassDeclaration>
Sub Foo()
|
End Sub</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInPropertyGet()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<ClassDeclaration>
ReadOnly Property Foo
Get
|
End Get
End Property
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInPropertySet()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<ClassDeclaration>
WriteOnly Property Foo
Set
|
End Set
End Property
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitNotInAddHandler()
            VerifyRecommendationsMissing(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        |
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event
                                               </ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInLambdaInAddHandler()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        Dim x = Sub()
                    |
                End Sub
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event
                                               </ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitNotInRemoveHandler()
            VerifyRecommendationsMissing(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
        |
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event

</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInLambdaInRemoveHandler()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
        Dim x = Sub()
                    |
                End Sub
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event

</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitNotInRaiseEvent()
            VerifyRecommendationsMissing(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        |
    End RaiseEvent
End Event

</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInLambdaInRaiseEvent()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        Dim x = Sub()
                    |
                End Sub
    End RaiseEvent
End Event

</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInLoopInAddHandler1()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        Do
            |
        Loop
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        Dim x = Sub()
                    |
                End Sub
    End RaiseEvent
End Event
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInLoopInAddHandler2()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        Do
            |
        Loop Until True
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInLoopInAddHandler3()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        Do Until True
            |
        Loop
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInForLoopInAddHandler()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        For i = 1 To 10
            |
        Next
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInForEachLoopInAddHandler()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        For Each x In y
            |
        Next
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInTryInAddHandler()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        Try
            |
        Catch ex As Exception
        End Try
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInCatchInAddHandler()
            VerifyRecommendationsContain(<ClassDeclaration>
Custom Event Click As EventHandler
    AddHandler(ByVal value As EventHandler)
        Try
        Catch ex As Exception
            |
        End Try
    End AddHandler

    RemoveHandler(ByVal value As EventHandler)
    End RemoveHandler

    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
    End RaiseEvent
End Event
</ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitNotInOperator()
            VerifyRecommendationsMissing(<File>
Class Foo
    Public Shared Operator +(value1 As Foo, value2 as Foo) As Foo
        |
    End Operator
End Class
</File>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInWhileLoopInAddHandler()
            VerifyRecommendationsContain(<MethodBody>
While True
|
End While
                                               </MethodBody>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInLoopInClassDeclarationLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
Do
|
Loop
End Sub
                                               </ClassDeclaration>, "Exit")

        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInClassDeclarationLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitInClassDeclarationSingleLineLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub() |
                                               </ClassDeclaration>, "Exit")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ExitNotInFinallyBlock()
            Dim code =
<MethodBody>
Try
Finally
    |
</MethodBody>

            VerifyRecommendationsMissing(code, "Exit")
        End Sub

    End Class
End Namespace
