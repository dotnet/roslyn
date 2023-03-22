' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class RaiseEventKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        Public Sub RaiseEventInCustomEventTest()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
       |
    End Event
End Class</File>

            VerifyRecommendationsContain(code, "RaiseEvent")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899057")>
        Public Sub RaiseEventInSingleLineLambdaTest()
            Dim code = <File>
Public Class Z
    Public Sub Main()
        Dim c = Sub() |
    End Sub
End Class</File>

            VerifyRecommendationsContain(code, "RaiseEvent")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808406")>
        Public Sub NotRaiseEventInCustomEventWithRaiseEventTest()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
        RaiseEvent()
        End RaiseEvent
       |
    End Event
End Class</File>

            VerifyRecommendationsMissing(code, "RaiseEvent")
        End Sub
    End Class
End Namespace
