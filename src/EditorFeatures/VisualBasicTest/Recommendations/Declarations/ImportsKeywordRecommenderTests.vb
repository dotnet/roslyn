' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ImportsKeywordRecommender
    Public Class OptionKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsInBlankFileTest() As Task
            Await VerifyRecommendationsContainAsync(<File>|</File>, "Imports")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsAfterAnotherImportsStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Imports Bar
|</File>, "Imports")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsAfterXmlImportsTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Imports &lt;xmlns:test="http://tempuri.org"&gt;
|</File>, "Imports")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsAfterBlankLineAfterImportsTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Imports Bar

|</File>, "Imports")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsAfterBlankLineAfterXmlImportsTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Imports &lt;xmlns:test="http://tempuri.org"&gt;

|</File>, "Imports")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsAfterOptionStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Option Strict On
|</File>, "Imports")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsAfterBlankLineAfterOptionStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<File>
Option Strict On

|</File>, "Imports")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsNotBeforeOptionStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
|
Option Strict On
</File>, "Imports")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImportsNotAfterTypeTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class Foo
End Class
|</File>, "Imports")
        End Function
    End Class
End Namespace
