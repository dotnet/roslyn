' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ElseIfDirectiveKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseIfNotInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "#ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseIfInFileAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
|</File>, "#ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseIfInFileAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
#ElseIf True Then
|</File>, "#ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseIfNotInFileAfterElseIf1Test() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If True Then
#Else
|</File>, "#ElseIf")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseIfNotInFileAfterElseIf2Test() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#ElseIf")
        End Function
    End Class
End Namespace
