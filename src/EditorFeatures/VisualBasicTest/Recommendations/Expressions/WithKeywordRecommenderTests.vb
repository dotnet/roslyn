﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class WithKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInClassDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterFromTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Dim x = New Goo From |</ClassDeclaration>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterWith1Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Dim x = New With |</ClassDeclaration>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneAfterWith2Test() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Dim x = New Goo With |</ClassDeclaration>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterDimEqualsNewTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = New |</MethodBody>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterDimEqualsNewTypeNameTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = New Goo |</MethodBody>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterDimEqualsNewTypeNameAndParensTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = New Goo() |</MethodBody>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterDimAsNewTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x As New |</MethodBody>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterDimAsNewTypeNameTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x As New Goo |</MethodBody>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterDimAsNewTypeNameAndParensTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x As New Goo() |</MethodBody>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterAssignmentNewTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>x = New |</MethodBody>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterAssignmentNewTypeNameTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>x = New Goo |</MethodBody>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function WithAfterAssignmentNewTypeNameAndParensTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>x = New Goo() |</MethodBody>, "With")
        End Function

        <WorkItem(543291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543291")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoWithAfterDotTest() As Task
            Dim code = <File>
Class C
    Sub M()
        Dim c As New C.|
    End Sub
End Class
                       </File>

            Await VerifyRecommendationsMissingAsync(code, "With")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterEolTest() As Task
            Await VerifyRecommendationsMissingAsync(
<ClassDeclaration>Dim x = New Goo 
|</ClassDeclaration>, "With")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>Dim x = New Goo _
|</ClassDeclaration>, "With")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsContainAsync(
<ClassDeclaration>Dim x = New Goo _ ' Test
|</ClassDeclaration>, "With")
        End Function
    End Class
End Namespace
