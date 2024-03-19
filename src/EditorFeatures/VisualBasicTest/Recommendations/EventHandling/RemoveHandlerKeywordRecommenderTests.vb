' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class RemoveHandlerKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub RemoveHandlerHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>|</MethodBody>, "RemoveHandler",
$"{VBFeaturesResources.RemoveHandler_statement}
{VBWorkspaceResources.Removes_the_association_between_an_event_and_an_event_handler_or_delegate_at_run_time}
RemoveHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}")
        End Sub

        <Fact>
        Public Sub RemoveHandlerInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "RemoveHandler")
        End Sub

        <Fact>
        Public Sub RemoveHandlerAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "RemoveHandler")
        End Sub

        <Fact>
        Public Sub RemoveHandlerMissingInClassBlockTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "RemoveHandler")
        End Sub

        <Fact>
        Public Sub RemoveHandlerInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "RemoveHandler")
        End Sub

        <Fact>
        Public Sub RemoveHandlerInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "RemoveHandler")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        Public Sub RemoveHandlerInCustomEventTest()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
       |
    End Event
End Class</File>

            VerifyRecommendationsContain(code, "RemoveHandler")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        Public Sub NotRemoveHandlerInCustomEventWithRemoveHandlerTest()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
        RemoveHandler(z as Action)
        End RemoveHandler
       |
    End Event
End Class</File>

            VerifyRecommendationsMissing(code, "RemoveHandler")
        End Sub
    End Class
End Namespace
