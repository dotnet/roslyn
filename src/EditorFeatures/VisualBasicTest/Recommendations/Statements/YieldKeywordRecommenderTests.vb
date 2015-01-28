' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class YieldKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Yield")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InLambdaBody()
            Dim code =
<MethodBody>
Dim f = Function()
            |
        End Function
</MethodBody>

            VerifyRecommendationsContain(code, "Yield")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInExpression()
            Dim code =
<MethodBody>
Dim f = |
</MethodBody>

            VerifyRecommendationsMissing(code, "Yield")
        End Sub

    End Class
End Namespace
