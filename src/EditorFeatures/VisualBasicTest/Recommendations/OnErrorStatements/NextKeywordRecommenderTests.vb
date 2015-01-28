' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class NextKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextAfterOnErrorResume()
            VerifyRecommendationsAreExactly(<MethodBody>On Error Resume |</MethodBody>, "Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextAfterResumeStatement()
            VerifyRecommendationsAreExactly(<MethodBody>Resume |</MethodBody>, "Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextNotInLambdaAfterResume()
            ' On Error statements are never allowed within lambdas
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
            Resume |
End Sub</MethodBody>, "Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NextNotInLambdaAfterOnErrorResume()
            ' On Error statements are never allowed within lambdas
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
            On Error Resume |
End Sub</MethodBody>, "Next")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>On Error Resume 
|</MethodBody>, "Next")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>On Error Resume _
|</MethodBody>, "Next")
        End Sub
    End Class
End Namespace
