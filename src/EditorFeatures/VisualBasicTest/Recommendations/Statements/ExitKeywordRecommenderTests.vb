' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ExitKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ExitInSubBodyTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Sub Goo()
|
End Sub</ClassDeclaration>, "Exit")
        End Sub

        <Fact>
        Public Sub ExitInFunctionTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Sub Goo()
|
End Sub</ClassDeclaration>, "Exit")
        End Sub

        <Fact>
        Public Sub ExitInPropertyGetTest()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<ClassDeclaration>
ReadOnly Property Goo
Get
|
End Get
End Property
</ClassDeclaration>, "Exit")
        End Sub

        <Fact>
        Public Sub ExitInPropertySetTest()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsContain(<ClassDeclaration>
WriteOnly Property Goo
Set
|
End Set
End Property
</ClassDeclaration>, "Exit")
        End Sub

        <Fact>
        Public Sub ExitNotInAddHandlerTest()
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

        <Fact>
        Public Sub ExitInLambdaInAddHandlerTest()
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

        <Fact>
        Public Sub ExitNotInRemoveHandlerTest()
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

        <Fact>
        Public Sub ExitInLambdaInRemoveHandlerTest()
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

        <Fact>
        Public Sub ExitNotInRaiseEventTest()
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

        <Fact>
        Public Sub ExitInLambdaInRaiseEventTest()
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

        <Fact>
        Public Sub ExitInLoopInAddHandler1Test()
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

        <Fact>
        Public Sub ExitInLoopInAddHandler2Test()
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

        <Fact>
        Public Sub ExitInLoopInAddHandler3Test()
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

        <Fact>
        Public Sub ExitInForLoopInAddHandlerTest()
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

        <Fact>
        Public Sub ExitInForEachLoopInAddHandlerTest()
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

        <Fact>
        Public Sub ExitInTryInAddHandlerTest()
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

        <Fact>
        Public Sub ExitInCatchInAddHandlerTest()
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

        <Fact>
        Public Sub ExitNotInOperatorTest()
            VerifyRecommendationsMissing(<File>
Class Goo
    Public Shared Operator +(value1 As Goo, value2 as Goo) As Goo
        |
    End Operator
End Class
</File>, "Exit")
        End Sub

        <Fact>
        Public Sub ExitInWhileLoopInAddHandlerTest()
            VerifyRecommendationsContain(<MethodBody>
While True
|
End While
                                               </MethodBody>, "Exit")
        End Sub

        <Fact>
        Public Sub ExitInLoopInClassDeclarationLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
Do
|
Loop
End Sub
                                               </ClassDeclaration>, "Exit")

        End Sub

        <Fact>
        Public Sub ExitInClassDeclarationLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Exit")
        End Sub

        <Fact>
        Public Sub ExitInClassDeclarationSingleLineLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub() |
                                               </ClassDeclaration>, "Exit")
        End Sub

        <Fact>
        Public Sub ExitNotInFinallyBlockTest()
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
