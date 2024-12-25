' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class AddHandlerKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub AddHandlerHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>|</MethodBody>, "AddHandler",
$"{VBFeaturesResources.AddHandler_statement}
{VBWorkspaceResources.Associates_an_event_with_an_event_handler_delegate_or_lambda_expression_at_run_time}
AddHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}")
        End Sub

        <Fact>
        Public Sub AddHandlerInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "AddHandler")
        End Sub

        <Fact>
        Public Sub AddHandlerAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "AddHandler")
        End Sub

        <Fact>
        Public Sub AddHandlerMissingInClassBlockTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "AddHandler")
        End Sub

        <Fact>
        Public Sub AddHandlerInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "AddHandler")
        End Sub

        <Fact>
        Public Sub AddHandlerInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "AddHandler")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        Public Sub AddHandlerInCustomEventTest()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
       |
    End Event
End Class</File>

            VerifyRecommendationsContain(code, "AddHandler")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        Public Sub NotAddHandlerInCustomEventWithAddHandlerTest()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
        AddHandler(z as Action)
        End AddHandler
       |
    End Event
End Class</File>

            VerifyRecommendationsMissing(code, "AddHandler")
        End Sub
    End Class
End Namespace
