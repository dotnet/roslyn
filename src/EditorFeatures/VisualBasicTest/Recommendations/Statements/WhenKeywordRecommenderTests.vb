' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class WhenKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhenAfterCatchBlock()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch x As Exception |
End Try</MethodBody>, "When")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WhenAfterCatchBlockWithoutAs()
            VerifyRecommendationsContain(<MethodBody>
Dim x
Try
Catch x |
End Try</MethodBody>, "When")
        End Sub

        <WorkItem(542803)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWhenAfterDimStatement()
            VerifyRecommendationsMissing(<MethodBody>Dim ex As Exception |</MethodBody>, "When")
        End Sub

        <WorkItem(542803)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWhenAfterLambdaInExceptionFilter()
            VerifyRecommendationsMissing(
<MethodBody>
Try
Catch ex As Exception When (Function(e As Exception) As Boolean |
                                Return False
                            End Function).Invoke(ex)
End Try
</MethodBody>,
 "When")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>
Try
Catch x As Exception 
|
End Try</MethodBody>, "When")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
Try
Catch x As Exception _
|
End Try</MethodBody>, "When")
        End Sub
    End Class
End Namespace
