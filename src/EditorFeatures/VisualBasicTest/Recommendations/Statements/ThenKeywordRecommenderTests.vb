' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ThenKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NotAfterHashIfTest()
            VerifyRecommendationsMissing(<File>#If |</File>, "Then")
        End Sub

        <Fact>
        Public Sub AfterHashIfExpressionTest()
            VerifyRecommendationsContain(<File>#If True |</File>, "Then")
        End Sub
    End Class
End Namespace
