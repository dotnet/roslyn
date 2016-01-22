' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
