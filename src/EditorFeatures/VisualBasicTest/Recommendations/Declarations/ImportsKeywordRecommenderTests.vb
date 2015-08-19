' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ImportsKeywordRecommender
    Public Class OptionKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsInBlankFile()
            VerifyRecommendationsContain(<File>|</File>, "Imports")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsAfterAnotherImportsStatement()
            VerifyRecommendationsContain(<File>
Imports Bar
|</File>, "Imports")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsAfterXmlImports()
            VerifyRecommendationsContain(<File>
Imports &lt;xmlns:test="http://tempuri.org"&gt;
|</File>, "Imports")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsAfterBlankLineAfterImports()
            VerifyRecommendationsContain(<File>
Imports Bar

|</File>, "Imports")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsAfterBlankLineAfterXmlImports()
            VerifyRecommendationsContain(<File>
Imports &lt;xmlns:test="http://tempuri.org"&gt;

|</File>, "Imports")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsAfterOptionStatement()
            VerifyRecommendationsContain(<File>
Option Strict On
|</File>, "Imports")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsAfterBlankLineAfterOptionStatement()
            VerifyRecommendationsContain(<File>
Option Strict On

|</File>, "Imports")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsNotBeforeOptionStatement()
            VerifyRecommendationsMissing(<File>
|
Option Strict On
</File>, "Imports")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImportsNotAfterType()
            VerifyRecommendationsMissing(<File>
Class Foo
End Class
|</File>, "Imports")
        End Sub
    End Class
End Namespace
