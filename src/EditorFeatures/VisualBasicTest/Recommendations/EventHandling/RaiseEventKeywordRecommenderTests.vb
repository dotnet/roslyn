' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    Public Class RaiseEventKeywordRecommenderTests
        <Fact>
        <WorkItem(808406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RaiseEventInCustomEventTest() As Task
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
       |
    End Event
End Class</File>

            Await VerifyRecommendationsContainAsync(code, "RaiseEvent")
        End Function

        <Fact>
        <WorkItem(899057, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899057")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function RaiseEventInSingleLineLambdaTest() As Task
            Dim code = <File>
Public Class Z
    Public Sub Main()
        Dim c = Sub() |
    End Sub
End Class</File>

            Await VerifyRecommendationsContainAsync(code, "RaiseEvent")
        End Function

        <Fact>
        <WorkItem(808406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotRaiseEventInCustomEventWithRaiseEventTest() As Task
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
        RaiseEvent()
        End RaiseEvent
       |
    End Event
End Class</File>

            Await VerifyRecommendationsMissingAsync(code, "RaiseEvent")
        End Function
    End Class
End Namespace
