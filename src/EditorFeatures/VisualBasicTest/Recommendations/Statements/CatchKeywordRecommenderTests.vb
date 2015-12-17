' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class CatchKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CatchNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Catch")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CatchInTryBlock()
            VerifyRecommendationsContain(<MethodBody>
Try
|
Finally
End Try</MethodBody>, "Catch")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CatchInCatchBlock()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch ex As Exception
|
Finally
End Try</MethodBody>, "Catch")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CatchNotInFinallyBlock()
            VerifyRecommendationsMissing(<MethodBody>
Try
Finally
|
End Try</MethodBody>, "Catch")
        End Sub
    End Class
End Namespace
