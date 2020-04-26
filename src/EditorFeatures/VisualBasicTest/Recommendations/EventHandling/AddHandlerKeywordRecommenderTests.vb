﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    Public Class AddHandlerKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddHandlerHelpTextTest() As Task
            Await VerifyRecommendationDescriptionTextIsAsync(<MethodBody>|</MethodBody>, "AddHandler",
$"{VBFeaturesResources.AddHandler_statement}
{VBWorkspaceResources.Associates_an_event_with_an_event_handler_delegate_or_lambda_expression_at_run_time}
AddHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddHandlerInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "AddHandler")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddHandlerAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x 
|</MethodBody>, "AddHandler")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddHandlerMissingInClassBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "AddHandler")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddHandlerInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Sub() |</MethodBody>, "AddHandler")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddHandlerInSingleLineFunctionLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = Function() |</MethodBody>, "AddHandler")
        End Function

        <Fact>
        <WorkItem(808406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddHandlerInCustomEventTest() As Task
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
       |
    End Event
End Class</File>

            Await VerifyRecommendationsContainAsync(code, "AddHandler")
        End Function

        <Fact>
        <WorkItem(808406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAddHandlerInCustomEventWithAddHandlerTest() As Task
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
        AddHandler(z as Action)
        End AddHandler
       |
    End Event
End Class</File>

            Await VerifyRecommendationsMissingAsync(code, "AddHandler")
        End Function
    End Class
End Namespace
