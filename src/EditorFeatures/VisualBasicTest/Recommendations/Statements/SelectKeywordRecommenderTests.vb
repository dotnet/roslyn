' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class SelectKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Select")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectInMultiLineLambdaTest()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Select")

        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectNotInSingleLineLambdaTest()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Select")
        End Sub

        <WorkItem(543396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543396")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectInSingleLineIfTest()
            VerifyRecommendationsContain(<MethodBody>If True Then S|</MethodBody>, "Select")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectAfterExitInsideCaseTest()
            Dim code =
<MethodBody>
Dim i As Integer = 1
Select Case i
    Case 0
        Exit |
</MethodBody>

            VerifyRecommendationsContain(code, "Select")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectNotAfterExitInsideCaseInsideFinallyBlockTest()
            Dim code =
<MethodBody>
Try
Finally
    Dim i As Integer = 1
    Select Case i
        Case 0
            Exit |
</MethodBody>

            VerifyRecommendationsMissing(code, "Select")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectNotAfterExitInsideFinallyBlockInsideCaseTest()
            Dim code =
<MethodBody>
Select Case i
    Case 0
        Try
        Finally
            Dim i As Integer = 1
                    Exit |
</MethodBody>

            VerifyRecommendationsMissing(code, "Select")
        End Sub
    End Class
End Namespace
