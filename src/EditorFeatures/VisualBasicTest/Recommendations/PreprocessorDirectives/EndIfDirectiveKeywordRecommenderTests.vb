' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class EndIfDirectiveKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndIfNotInFile()
            VerifyRecommendationsMissing(<File>|</File>, "#End If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndIfInFileAfterIf()
            VerifyRecommendationsContain(<File>
#If True Then
|</File>, "#End If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndIfInFileAfterElseIf()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
|</File>, "#End If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndIfNotInFileAfterElse1()
            VerifyRecommendationsContain(<File>
#If True Then
#Else
|</File>, "#End If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashEndIfNotInFileAfterElse2()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#End If")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterHashEndIf()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
#End |</File>, "If")
        End Sub

        <WorkItem(957458)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotIfWithEndPartiallyTyped()
            VerifyRecommendationsMissing(<File>
#If True Then
#En |</File>, "If")
        End Sub
    End Class
End Namespace
