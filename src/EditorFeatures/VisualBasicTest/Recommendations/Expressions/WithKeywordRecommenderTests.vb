' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class WithKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterFrom()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Foo From |</ClassDeclaration>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterWith1()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New With |</ClassDeclaration>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterWith2()
            VerifyRecommendationsMissing(<ClassDeclaration>Dim x = New Foo With |</ClassDeclaration>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterDimEqualsNew()
            VerifyRecommendationsContain(<MethodBody>Dim x = New |</MethodBody>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterDimEqualsNewTypeName()
            VerifyRecommendationsContain(<MethodBody>Dim x = New Foo |</MethodBody>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterDimEqualsNewTypeNameAndParens()
            VerifyRecommendationsContain(<MethodBody>Dim x = New Foo() |</MethodBody>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterDimAsNew()
            VerifyRecommendationsContain(<MethodBody>Dim x As New |</MethodBody>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterDimAsNewTypeName()
            VerifyRecommendationsContain(<MethodBody>Dim x As New Foo |</MethodBody>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterDimAsNewTypeNameAndParens()
            VerifyRecommendationsContain(<MethodBody>Dim x As New Foo() |</MethodBody>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterAssignmentNew()
            VerifyRecommendationsContain(<MethodBody>x = New |</MethodBody>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterAssignmentNewTypeName()
            VerifyRecommendationsContain(<MethodBody>x = New Foo |</MethodBody>, "With")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub WithAfterAssignmentNewTypeNameAndParens()
            VerifyRecommendationsContain(<MethodBody>x = New Foo() |</MethodBody>, "With")
        End Sub

        <WorkItem(543291)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoWithAfterDot()
            Dim code = <File>
Class C
    Sub M()
        Dim c As New C.|
    End Sub
End Class
                       </File>

            VerifyRecommendationsMissing(code, "With")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<ClassDeclaration>Dim x = New Foo 
|</ClassDeclaration>, "With")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>Dim x = New Foo _
|</ClassDeclaration>, "With")
        End Sub
    End Class
End Namespace
