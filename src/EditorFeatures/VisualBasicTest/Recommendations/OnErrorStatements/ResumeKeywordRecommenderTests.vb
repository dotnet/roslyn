' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class ResumeKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ResumeNextAfterOnError()
            VerifyRecommendationsContain(<MethodBody>On Error |</MethodBody>, "Resume Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ResumeInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Resume")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ResumeNotInLambda()
            ' On Error statements are never allowed within lambdas
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub()
            |
End Sub</MethodBody>, "Resume")
        End Sub
    End Class
End Namespace
