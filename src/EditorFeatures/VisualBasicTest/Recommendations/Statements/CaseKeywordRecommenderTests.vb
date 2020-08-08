' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class CaseKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseAfterSelectTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Select |</MethodBody>, "Case")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoCaseAfterQuerySelectTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim q = From x in "abc" Select |</MethodBody>, "Case")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoCaseElseAfterQuerySelectTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim q = From x in "abc" Select |</MethodBody>, "Case Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseNotByItselfTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Case")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseInSelectBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Select Case goo
|
End Select</MethodBody>, "Case")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseElseInSelectBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Select Case goo
|
End Select</MethodBody>, "Case Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseElseNotInSelectBlockThatAlreadyHasCaseElseTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Select Case goo
Case Else
|
End Select</MethodBody>, "Case Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseElseNotInSelectBlockIfBeforeCaseTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Select Case goo
|
Case
End Select</MethodBody>, "Case Else")
        End Function

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoCaseInSelectBlockIfAfterCaseElseTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Select Case goo
    Case Else
        Dim i = 3
    |
End Select</MethodBody>, "Case")
        End Function

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseInSelectBlockBeforeCaseElseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Select Case goo
    |
    Case Else
        Dim i = 3
End Select</MethodBody>, "Case")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoCaseIsInSelectBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Select Case goo
|
End Select</MethodBody>, "Case Is")
        End Function
    End Class
End Namespace
