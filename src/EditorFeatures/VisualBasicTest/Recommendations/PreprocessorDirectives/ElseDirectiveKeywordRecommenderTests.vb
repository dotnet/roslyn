' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ElseDirectiveKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub HashElseNotInFileTest()
            VerifyRecommendationsMissing(<File>|</File>, "#Else")
        End Sub

        <Fact>
        Public Sub HashElseInFileAfterIfTest()
            VerifyRecommendationsContain(<File>
#If True Then
|</File>, "#Else")
        End Sub

        <Fact>
        Public Sub HashElseInFileAfterElseIfTest()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
|</File>, "#Else")
        End Sub

        <Fact>
        Public Sub HashElseNotInFileAfterElse1Test()
            VerifyRecommendationsMissing(<File>
#If True Then
#Else
|</File>, "#Else")
        End Sub

        <Fact>
        Public Sub HashElseNotInFileAfterElse2Test()
            VerifyRecommendationsMissing(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#Else")
        End Sub
    End Class
End Namespace
