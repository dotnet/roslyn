' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ExitKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInSubBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Sub Foo()
|
End Sub</ClassDeclaration>, "Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInFunctionTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Sub Foo()
|
End Sub</ClassDeclaration>, "Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInPropertyGetTest() As Task
            ' We can always exit a Sub/Function, so it should be there
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
ReadOnly Property Foo
Get
|
End Get
End Property
</ClassDeclaration>, "Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInPropertySetTest() As Task
            ' We can always exit a Sub/Function, so it should be there
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
WriteOnly Property Foo
Set
|
End Set
End Property
</ClassDeclaration>, "Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitNotInAddHandlerTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInLambdaInAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitNotInRemoveHandlerTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInLambdaInRemoveHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitNotInRaiseEventTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInLambdaInRaiseEventTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInLoopInAddHandler1Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInLoopInAddHandler2Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInLoopInAddHandler3Test() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInForLoopInAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInForEachLoopInAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInTryInAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInCatchInAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
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
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitNotInOperatorTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class Foo
    Public Shared Operator +(value1 As Foo, value2 as Foo) As Foo
        |
    End Operator
End Class
</File>, "Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInWhileLoopInAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
While True
|
End While
                                               </MethodBody>, "Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInLoopInClassDeclarationLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub()
Do
|
Loop
End Sub
                                               </ClassDeclaration>, "Exit")

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInClassDeclarationLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitInClassDeclarationSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub() |
                                               </ClassDeclaration>, "Exit")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ExitNotInFinallyBlockTest() As Task
            Dim code =
<MethodBody>
Try
Finally
    |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "Exit")
        End Function
    End Class
End Namespace
