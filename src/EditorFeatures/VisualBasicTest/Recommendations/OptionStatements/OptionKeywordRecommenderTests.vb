' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OptionStatements
    Public Class OptionKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionInBlankFile()
            VerifyRecommendationsContain(<File>|</File>, "Option")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionAfterAnotherOptionStatement()
            VerifyRecommendationsContain(<File>
Option Strict On
|</File>, "Option")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionAfterBlankLine()
            VerifyRecommendationsContain(<File>
Option Strict On

|</File>, "Option")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionNotAfterImports()
            VerifyRecommendationsMissing(<File>
Imports Foo
|</File>, "Option")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionNotAfterType()
            VerifyRecommendationsMissing(<File>
Class Foo
End Class
|</File>, "Option")
        End Sub

        <WorkItem(543008)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionNotAfterRegionKeyword()
            VerifyRecommendationsMissing(<File>
#Region |
</File>, "Option")
        End Sub
    End Class
End Namespace
