' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ArrayStatements
    Public Class PreserveKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PreserveNotInMethodBody()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Preserve")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PreserveAfterReDimStatement()
            VerifyRecommendationsContain(<MethodBody>ReDim | </MethodBody>, "Preserve")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PreserveNotAfterReDimPreserve()
            VerifyRecommendationsMissing(<ClassDeclaration>ReDim Preserve |</ClassDeclaration>, "Preserve")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PreserveNotAfterWeirdBrokenReDim()
            VerifyRecommendationsMissing(<MethodBody>ReDim x, ReDim |</MethodBody>, "Preserve")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub PreserveInSingleLineLambda()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() ReDim |</MethodBody>, "Preserve")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>ReDim 
| </MethodBody>, "Preserve")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>ReDim _
| </MethodBody>, "Preserve")
        End Sub
    End Class
End Namespace
