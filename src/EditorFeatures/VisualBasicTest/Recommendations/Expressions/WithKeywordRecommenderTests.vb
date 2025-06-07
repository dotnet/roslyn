' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class WithKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "With")
        End Sub

        <Fact>
        Public Sub NoneAfterFromTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Goo From |</ClassDeclaration>, "With")
        End Sub

        <Fact>
        Public Sub NoneAfterWith1Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New With |</ClassDeclaration>, "With")
        End Sub

        <Fact>
        Public Sub NoneAfterWith2Test()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Goo With |</ClassDeclaration>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterDimEqualsNewTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = New |</MethodBody>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterDimEqualsNewTypeNameTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = New Goo |</MethodBody>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterDimEqualsNewTypeNameAndParensTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = New Goo() |</MethodBody>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterDimAsNewTest()
            VerifyRecommendationsContain(<MethodBody>Dim x As New |</MethodBody>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterDimAsNewTypeNameTest()
            VerifyRecommendationsContain(<MethodBody>Dim x As New Goo |</MethodBody>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterDimAsNewTypeNameAndParensTest()
            VerifyRecommendationsContain(<MethodBody>Dim x As New Goo() |</MethodBody>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterAssignmentNewTest()
            VerifyRecommendationsContain(<MethodBody>x = New |</MethodBody>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterAssignmentNewTypeNameTest()
            VerifyRecommendationsContain(<MethodBody>x = New Goo |</MethodBody>, "With")
        End Sub

        <Fact>
        Public Sub WithAfterAssignmentNewTypeNameAndParensTest()
            VerifyRecommendationsContain(<MethodBody>x = New Goo() |</MethodBody>, "With")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543291")>
        Public Sub NoWithAfterDotTest()
            Dim code = <File>
Class C
    Sub M()
        Dim c As New C.|
    End Sub
End Class
                       </File>

            VerifyRecommendationsMissing(code, "With")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>Dim x = New Goo 
|</ClassDeclaration>, "With")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<ClassDeclaration>Dim x = New Goo _
|</ClassDeclaration>, "With")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>Dim x = New Goo _ ' Test
|</ClassDeclaration>, "With")
        End Sub
    End Class
End Namespace
