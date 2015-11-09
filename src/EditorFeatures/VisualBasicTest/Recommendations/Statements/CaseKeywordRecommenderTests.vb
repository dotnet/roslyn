' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class CaseKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseAfterSelect()
            VerifyRecommendationsContain(<MethodBody>Select |</MethodBody>, "Case")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCaseAfterQuerySelect()
            VerifyRecommendationsMissing(<MethodBody>Dim q = From x in "abc" Select |</MethodBody>, "Case")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCaseElseAfterQuerySelect()
            VerifyRecommendationsMissing(<MethodBody>Dim q = From x in "abc" Select |</MethodBody>, "Case Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseNotByItself()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Case")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseInSelectBlock()
            VerifyRecommendationsContain(<MethodBody>
Select Case foo
|
End Select</MethodBody>, "Case")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseElseInSelectBlock()
            VerifyRecommendationsContain(<MethodBody>
Select Case foo
|
End Select</MethodBody>, "Case Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseElseNotInSelectBlockThatAlreadyHasCaseElse()
            VerifyRecommendationsMissing(<MethodBody>
Select Case foo
Case Else
|
End Select</MethodBody>, "Case Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseElseNotInSelectBlockIfBeforeCase()
            VerifyRecommendationsMissing(<MethodBody>
Select Case foo
|
Case
End Select</MethodBody>, "Case Else")
        End Sub

        <WpfFact>
        <WorkItem(543384)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCaseInSelectBlockIfAfterCaseElse()
            VerifyRecommendationsMissing(<MethodBody>
Select Case foo
    Case Else
        Dim i = 3
    |
End Select</MethodBody>, "Case")
        End Sub

        <WpfFact>
        <WorkItem(543384)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseInSelectBlockBeforeCaseElse()
            VerifyRecommendationsContain(<MethodBody>
Select Case foo
    |
    Case Else
        Dim i = 3
End Select</MethodBody>, "Case")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCaseIsInSelectBlock()
            VerifyRecommendationsMissing(<MethodBody>
Select Case foo
|
End Select</MethodBody>, "Case Is")
        End Sub
    End Class
End Namespace
