' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class CaseKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseAfterSelectTest()
            VerifyRecommendationsContain(<MethodBody>Select |</MethodBody>, "Case")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCaseAfterQuerySelectTest()
            VerifyRecommendationsMissing(<MethodBody>Dim q = From x in "abc" Select |</MethodBody>, "Case")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCaseElseAfterQuerySelectTest()
            VerifyRecommendationsMissing(<MethodBody>Dim q = From x in "abc" Select |</MethodBody>, "Case Else")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseNotByItselfTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Case")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseInSelectBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Select Case goo
|
End Select</MethodBody>, "Case")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseElseInSelectBlockTest()
            VerifyRecommendationsContain(<MethodBody>
Select Case goo
|
End Select</MethodBody>, "Case Else")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseElseNotInSelectBlockThatAlreadyHasCaseElseTest()
            VerifyRecommendationsMissing(<MethodBody>
Select Case goo
Case Else
|
End Select</MethodBody>, "Case Else")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseElseNotInSelectBlockIfBeforeCaseTest()
            VerifyRecommendationsMissing(<MethodBody>
Select Case goo
|
Case
End Select</MethodBody>, "Case Else")
        End Sub

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCaseInSelectBlockIfAfterCaseElseTest()
            VerifyRecommendationsMissing(<MethodBody>
Select Case goo
    Case Else
        Dim i = 3
    |
End Select</MethodBody>, "Case")
        End Sub

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CaseInSelectBlockBeforeCaseElseTest()
            VerifyRecommendationsContain(<MethodBody>
Select Case goo
    |
    Case Else
        Dim i = 3
End Select</MethodBody>, "Case")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoCaseIsInSelectBlockTest()
            VerifyRecommendationsMissing(<MethodBody>
Select Case goo
|
End Select</MethodBody>, "Case Is")
        End Sub
    End Class
End Namespace
