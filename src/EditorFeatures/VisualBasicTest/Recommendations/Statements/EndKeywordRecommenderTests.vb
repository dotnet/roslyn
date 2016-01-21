' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class EndKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndInMethodBodyTest() As Task
            ' It's always a statement (or at least for now)
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "End")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x 
|</MethodBody>, "End")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndMissingInClassBlockTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "End")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndInSingleLineLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Sub() |</MethodBody>, "End")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndNotInSingleLineFunctionLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim x = Function() |</MethodBody>, "End")
        End Function

        <WorkItem(530599)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EndNotOutsideOfMethodBodyTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Class C
 |</MethodBody>, "End")
        End Function
    End Class
End Namespace
