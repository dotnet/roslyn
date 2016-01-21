' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        <WorkItem(957458)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotIfWithEndPartiallyTypedTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If True Then
#En |</File>, "If")
        End Function
    End Class
End Namespace
