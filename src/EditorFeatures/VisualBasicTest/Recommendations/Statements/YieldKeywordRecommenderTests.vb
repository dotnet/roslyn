' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class YieldKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Yield")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InLambdaBodyTest() As Task
            Dim code =
<MethodBody>
Dim f = Function()
            |
        End Function
</MethodBody>

            Await VerifyRecommendationsContainAsync(code, "Yield")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInExpressionTest() As Task
            Dim code =
<MethodBody>
Dim f = |
</MethodBody>

            Await VerifyRecommendationsMissingAsync(code, "Yield")
        End Function
    End Class
End Namespace
