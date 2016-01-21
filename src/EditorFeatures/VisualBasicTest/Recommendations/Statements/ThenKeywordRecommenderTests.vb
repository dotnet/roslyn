' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
