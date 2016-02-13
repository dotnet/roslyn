' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class SelectKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Select")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectInMultiLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Select")

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectNotInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Select")
        End Function

        <WorkItem(543396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543396")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectInSingleLineIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If True Then S|</MethodBody>, "Select")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectAfterExitInsideCaseTest() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 1
Select Case i
    Case 0
        Exit |
</MethodBody>

            Await VerifyRecommendationsContainAsync(code, "Select")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectNotAfterExitInsideCaseInsideFinallyBlockTest() As Task
            Dim code =
<MethodBody>
Try
Finally
    Dim i As Integer = 1
    Select Case i
        Case 0
            Exit |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "Select")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SelectNotAfterExitInsideFinallyBlockInsideCaseTest() As Task
            Dim code =
<MethodBody>
Select Case i
    Case 0
        Try
        Finally
            Dim i As Integer = 1
                    Exit |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "Select")
        End Function
    End Class
End Namespace
