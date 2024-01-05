' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class CharsetModifierKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub AutoAfterDeclareTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Auto")
        End Sub

        <Fact>
        Public Sub AnsiAfterDeclareTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Ansi")
        End Sub

        <Fact>
        Public Sub UnicodeAfterDeclareTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Unicode")
        End Sub

        <Fact>
        Public Sub AutoNotAfterAnotherCharsetModifier1Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Ansi |</ClassDeclaration>, "Auto")
        End Sub

        <Fact>
        Public Sub AutoNotAfterAnotherCharsetModifier2Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Auto |</ClassDeclaration>, "Auto")
        End Sub

        <Fact>
        Public Sub AutoNotAfterAnotherCharsetModifier3Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Unicode |</ClassDeclaration>, "Auto")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterColonTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare : |</ClassDeclaration>, "Unicode")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>Declare 
 |</ClassDeclaration>, "Unicode")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare _
 |</ClassDeclaration>, "Unicode")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare _ ' Test
 |</ClassDeclaration>, "Unicode")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuationt()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare _
 |</ClassDeclaration>, "Unicode")
        End Sub
    End Class
End Namespace
