' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ElseDirectiveKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseNotInFileTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>|</File>, "#Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseInFileAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
|</File>, "#Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseInFileAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
#If True Then
#ElseIf True Then
|</File>, "#Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseNotInFileAfterElse1Test() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If True Then
#Else
|</File>, "#Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function HashElseNotInFileAfterElse2Test() As Task
            Await VerifyRecommendationsMissingAsync(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#Else")
        End Function
    End Class
End Namespace
