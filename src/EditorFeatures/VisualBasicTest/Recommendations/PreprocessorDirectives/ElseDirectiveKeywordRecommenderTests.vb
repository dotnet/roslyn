' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.PreprocessorDirectives
    Public Class ElseDirectiveKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseNotInFile()
            VerifyRecommendationsMissing(<File>|</File>, "#Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseInFileAfterIf()
            VerifyRecommendationsContain(<File>
#If True Then
|</File>, "#Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseInFileAfterElseIf()
            VerifyRecommendationsContain(<File>
#If True Then
#ElseIf True Then
|</File>, "#Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseNotInFileAfterElse1()
            VerifyRecommendationsMissing(<File>
#If True Then
#Else
|</File>, "#Else")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HashElseNotInFileAfterElse2()
            VerifyRecommendationsMissing(<File>
#If True Then
#ElseIf True Then
#Else
|</File>, "#Else")
        End Sub
    End Class
End Namespace
