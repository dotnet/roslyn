﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    Public Class RemoveHandlerKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RemoveHandlerHelpTextTest() As Task
            Await VerifyRecommendationDescriptionTextIsAsync(<MethodBody>|</MethodBody>, "RemoveHandler",
$"{VBFeaturesResources.RemoveHandler_statement}
{VBWorkspaceResources.Removes_the_association_between_an_event_and_an_event_handler_or_delegate_at_run_time}
RemoveHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RemoveHandlerInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "RemoveHandler")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RemoveHandlerAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x 
|</MethodBody>, "RemoveHandler")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RemoveHandlerMissingInClassBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "RemoveHandler")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RemoveHandlerInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Sub() |</MethodBody>, "RemoveHandler")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RemoveHandlerInSingleLineFunctionLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = Function() |</MethodBody>, "RemoveHandler")
        End Function

        <Fact>
        <WorkItem(808406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RemoveHandlerInCustomEventTest() As Task
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
       |
    End Event
End Class</File>

            Await VerifyRecommendationsContainAsync(code, "RemoveHandler")
        End Function

        <Fact>
        <WorkItem(808406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotRemoveHandlerInCustomEventWithRemoveHandlerTest() As Task
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
        RemoveHandler(z as Action)
        End RemoveHandler
       |
    End Event
End Class</File>

            Await VerifyRecommendationsMissingAsync(code, "RemoveHandler")
        End Function
    End Class
End Namespace
