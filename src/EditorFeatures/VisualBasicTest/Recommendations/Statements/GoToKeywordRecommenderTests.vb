' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class GoToKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GoToInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "GoTo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GoToInMultiLineLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "GoTo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GoToNotInSingleLineLambda()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |</MethodBody>, "GoTo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GoToNotInFinallyBlock()
            Dim code =
<MethodBody>
Try
Finally
    |
</MethodBody>

            VerifyRecommendationsMissing(code, "GoTo")
        End Sub

    End Class
End Namespace
