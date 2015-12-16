' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class OnErrorKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnErrorResumeNextInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "On Error Resume Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnErrorGoToInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "On Error GoTo")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnErrorResumeNextNotInLambda()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() 
            |
End Sub
</MethodBody>, "On Error Resume Next")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnErrorGoToNotInLambda()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() 
            |
End Sub
</MethodBody>, "On Error GoTo")
        End Sub
    End Class
End Namespace
