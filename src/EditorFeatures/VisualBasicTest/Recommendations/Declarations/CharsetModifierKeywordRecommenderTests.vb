' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class CharsetModifierKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AutoAfterDeclareTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Auto")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AnsiAfterDeclareTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Ansi")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UnicodeAfterDeclareTest()
            VerifyRecommendationsContain(<ClassDeclaration>Declare |</ClassDeclaration>, "Unicode")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AutoNotAfterAnotherCharsetModifier1Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Ansi |</ClassDeclaration>, "Auto")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AutoNotAfterAnotherCharsetModifier2Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Auto |</ClassDeclaration>, "Auto")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AutoNotAfterAnotherCharsetModifier3Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Unicode |</ClassDeclaration>, "Auto")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterColonTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare : |</ClassDeclaration>, "Unicode")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>Declare 
 |</ClassDeclaration>, "Unicode")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare _
 |</ClassDeclaration>, "Unicode")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare _ ' Test
 |</ClassDeclaration>, "Unicode")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuationt()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare _
 |</ClassDeclaration>, "Unicode")
        End Sub
    End Class
End Namespace
