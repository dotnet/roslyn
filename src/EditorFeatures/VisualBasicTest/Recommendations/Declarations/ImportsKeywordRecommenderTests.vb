' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.ImportsKeywordRecommender
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class OptionKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ImportsInBlankFileTest()
            VerifyRecommendationsContain(<File>|</File>, "Imports")
        End Sub

        <Fact>
        Public Sub ImportsAfterAnotherImportsStatementTest()
            VerifyRecommendationsContain(<File>
Imports Bar
|</File>, "Imports")
        End Sub

        <Fact>
        Public Sub ImportsAfterXmlImportsTest()
            VerifyRecommendationsContain(<File>
Imports &lt;xmlns:test="http://tempuri.org"&gt;
|</File>, "Imports")
        End Sub

        <Fact>
        Public Sub ImportsAfterBlankLineAfterImportsTest()
            VerifyRecommendationsContain(<File>
Imports Bar

|</File>, "Imports")
        End Sub

        <Fact>
        Public Sub ImportsAfterBlankLineAfterXmlImportsTest()
            VerifyRecommendationsContain(<File>
Imports &lt;xmlns:test="http://tempuri.org"&gt;

|</File>, "Imports")
        End Sub

        <Fact>
        Public Sub ImportsAfterOptionStatementTest()
            VerifyRecommendationsContain(<File>
Option Strict On
|</File>, "Imports")
        End Sub

        <Fact>
        Public Sub ImportsAfterBlankLineAfterOptionStatementTest()
            VerifyRecommendationsContain(<File>
Option Strict On

|</File>, "Imports")
        End Sub

        <Fact>
        Public Sub ImportsNotBeforeOptionStatementTest()
            VerifyRecommendationsMissing(<File>
|
Option Strict On
</File>, "Imports")
        End Sub

        <Fact>
        Public Sub ImportsNotAfterTypeTest()
            VerifyRecommendationsMissing(<File>
Class Goo
End Class
|</File>, "Imports")
        End Sub
    End Class
End Namespace
