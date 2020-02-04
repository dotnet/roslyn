' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ThenKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashIfTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>#If |</File>, "Then")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterHashIfExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<File>#If True |</File>, "Then")
        End Function
    End Class
End Namespace
