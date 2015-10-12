' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    Public Class RaiseEventKeywordRecommenderTests
        <WorkItem(808406)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RaiseEventInCustomEvent()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
       |
    End Event
End Class</File>

            VerifyRecommendationsContain(code, "RaiseEvent")
        End Sub

        <WorkItem(899057)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RaiseEventInSingleLineLambda()
            Dim code = <File>
Public Class Z
    Public Sub Main()
        Dim c = Sub() |
    End Sub
End Class</File>

            VerifyRecommendationsContain(code, "RaiseEvent")
        End Sub

        <WpfFact>
        <WorkItem(808406)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotRaiseEventInCustomEventWithRaiseEvent()
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
