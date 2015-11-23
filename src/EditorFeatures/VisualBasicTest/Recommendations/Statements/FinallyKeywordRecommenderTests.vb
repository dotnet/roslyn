' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class FinallyKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Finally")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyInTryBlock()
            VerifyRecommendationsContain(<MethodBody>
Try
|
End Try</MethodBody>, "Finally")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyInCatchBlock()
            VerifyRecommendationsContain(<MethodBody>
Try
Catch ex As Exception
|
End Try</MethodBody>, "Finally")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyNotBeforeCatchBlock()
            VerifyRecommendationsMissing(<MethodBody>
Try
|
Catch ex As Exception
End Try</MethodBody>, "Finally")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyNotInFinallyBlock()
            VerifyRecommendationsMissing(<MethodBody>
Try
Finally
|
End Try</MethodBody>, "Finally")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyInTryNestedInCatch1()
            VerifyRecommendationsContain(<MethodBody>
        Try
        Catch
            Try ' Type an 'E' on the next line
            |
                Throw
            End Try</MethodBody>, "Finally")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FinallyInTryNestedInCatch2()
            VerifyRecommendationsContain(<MethodBody>
        Try
        Catch
            Try ' Type an 'E' on the next line
            Catch
            |
                Throw
            End Try</MethodBody>, "Finally")
        End Sub
    End Class
End Namespace
