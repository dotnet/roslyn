﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
Class Goo
End Class
|</File>, "Imports")
        End Function
    End Class
End Namespace
