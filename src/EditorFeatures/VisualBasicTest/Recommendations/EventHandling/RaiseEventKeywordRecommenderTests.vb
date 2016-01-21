' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    Public Class RaiseEventKeywordRecommenderTests
        <WorkItem(808406)>
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

        <WorkItem(899057)>
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
        <WorkItem(808406)>
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
