' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ThenKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashIf()
            VerifyRecommendationsMissing(<File>#If |</File>, "Then")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterHashIfExpression()
            VerifyRecommendationsContain(<File>#If True |</File>, "Then")
        End Sub

    End Class
End Namespace
