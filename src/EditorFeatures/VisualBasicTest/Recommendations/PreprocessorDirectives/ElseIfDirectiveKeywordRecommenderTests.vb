' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ElseIfDirectiveKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfNotInFile()
            VerifyRecommendationsMissing(<File>|</File>, "#ElseIf")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfInFileAfterIf()
            VerifyRecommendationsContain(<File>
#If True Then
|</File>, "#ElseIf")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfInFileAfterElseIf()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
|</File>, "#ElseIf")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfNotInFileAfterElseIf1()
            VerifyRecommendationsMissing(<File>
#If True Then
#Else
|</File>, "#ElseIf")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseIfNotInFileAfterElseIf2()
            VerifyRecommendationsMissing(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#ElseIf")
        End Sub
    End Class
End Namespace
