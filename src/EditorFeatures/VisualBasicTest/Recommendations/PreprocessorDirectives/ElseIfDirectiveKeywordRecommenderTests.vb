' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
