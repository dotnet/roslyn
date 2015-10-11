' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class SelectKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Select")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectInMultiLineLambda()
            VerifyRecommendationsContain(<ClassDeclaration>
Private _member = Sub()
|
End Sub
                                         </ClassDeclaration>, "Select")

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectNotInSingleLineLambda()
            VerifyRecommendationsMissing(<ClassDeclaration>
Private _member = Sub() |
                                         </ClassDeclaration>, "Select")
        End Sub

        <WorkItem(543396)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectInSingleLineIf()
            VerifyRecommendationsContain(<MethodBody>If True Then S|</MethodBody>, "Select")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectAfterExitInsideCase()
            Dim code =
<MethodBody>
Dim i As Integer = 1
Select Case i
    Case 0
        Exit |
</MethodBody>

            VerifyRecommendationsContain(code, "Select")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectNotAfterExitInsideCaseInsideFinallyBlock()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SelectNotAfterExitInsideFinallyBlockInsideCase()
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
