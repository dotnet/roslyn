' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class EndKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub EndInMethodBodyTest()
            ' It's always a statement (or at least for now)
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "End")
        End Sub

        <Fact>
        Public Sub EndAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "End")
        End Sub

        <Fact>
        Public Sub EndMissingInClassBlockTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "End")
        End Sub

        <Fact>
        Public Sub EndInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "End")
        End Sub

        <Fact>
        Public Sub EndNotInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "End")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530599")>
        Public Sub EndNotOutsideOfMethodBodyTest()
            VerifyRecommendationsMissing(<MethodBody>Class C
 |</MethodBody>, "End")
        End Sub
    End Class
End Namespace
