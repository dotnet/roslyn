' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class EndIfDirectiveKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEndIfNotInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "#End If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEndIfInFileAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
|</File>, "#End If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEndIfInFileAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
#ElseIf True Then
|</File>, "#End If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEndIfNotInFileAfterElse1Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
#Else
|</File>, "#End If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashEndIfNotInFileAfterElse2Test() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#End If")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterHashEndIfTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
#ElseIf True Then
#End |</File>, "If")
        End Function

        <WorkItem(957458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/957458")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotIfWithEndPartiallyTypedTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If True Then
#En |</File>, "If")
        End Function
    End Class
End Namespace
