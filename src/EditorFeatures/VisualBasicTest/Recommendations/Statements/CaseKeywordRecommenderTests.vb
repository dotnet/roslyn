' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
Select Case foo
|
End Select</MethodBody>, "Case")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseElseInSelectBlockTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Select Case foo
|
End Select</MethodBody>, "Case Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseElseNotInSelectBlockThatAlreadyHasCaseElseTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Select Case foo
Case Else
|
End Select</MethodBody>, "Case Else")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CaseElseNotInSelectBlockIfBeforeCaseTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Select Case foo
|
Case
End Select</MethodBody>, "Case Else")
        End Function

        <Fact>
        <WorkItem(543384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543384")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoCaseInSelectBlockIfAfterCaseElseTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Select Case foo
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
Select Case foo
    |
    Case Else
        Dim i = 3
End Select</MethodBody>, "Case")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoCaseIsInSelectBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Select Case foo
|
End Select</MethodBody>, "Case Is")
        End Function
    End Class
End Namespace
