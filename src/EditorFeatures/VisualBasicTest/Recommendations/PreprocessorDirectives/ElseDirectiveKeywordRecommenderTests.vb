' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
