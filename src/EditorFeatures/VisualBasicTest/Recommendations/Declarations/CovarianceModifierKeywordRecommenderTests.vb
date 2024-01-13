' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class CovarianceModifierKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub InAfterOfInInterfaceTypeParamTest()
            VerifyRecommendationsContain(<File>Interface IGoo(Of |</File>, "In")
        End Sub

        <Fact>
        Public Sub OutAfterOfInInterfaceTypeParamTest()
            VerifyRecommendationsContain(<File>Interface IGoo(Of |</File>, "In")
        End Sub

        <Fact>
        Public Sub InNotInClassTypeParamTest()
            VerifyRecommendationsMissing(<File>Class Goo(Of |</File>, "In")
        End Sub

        <Fact>
        Public Sub OutNotInClassTypeParamTest()
            VerifyRecommendationsMissing(<File>Class Goo(Of |</File>, "Out")
        End Sub

        <Fact>
        Public Sub InNotInStructureTypeParamTest()
            VerifyRecommendationsMissing(<File>Structure Goo(Of |</File>, "In")
        End Sub

        <Fact>
        Public Sub OutNotInStructureTypeParamTest()
            VerifyRecommendationsMissing(<File>Structure Goo(Of |</File>, "Out")
        End Sub

        <Fact>
        Public Sub InForSecondInterfaceTypeParamTest()
            VerifyRecommendationsContain(<File>Interface IGoo(Of T, |</File>, "In")
        End Sub

        <Fact>
        Public Sub OutForSecondInterfaceTypeParamTest()
            VerifyRecommendationsContain(<File>Interface IGoo(Of T, |</File>, "In")
        End Sub

        <Fact>
        Public Sub InNotInMultipleConstraintsTest()
            VerifyRecommendationsMissing(<File>Interface IGoo(Of T As {New, |</File>, "In")
        End Sub

        <Fact>
        Public Sub OutNotInMultipleConstraintsTest()
            VerifyRecommendationsMissing(<File>Interface IGoo(Of T As {New, |</File>, "In")
        End Sub

        <Fact>
        Public Sub InAfterOfInDelegateTypeParamTest()
            VerifyRecommendationsContain(<File>Delegate Sub Goo(Of |</File>, "In")
        End Sub

        <Fact>
        Public Sub OutAfterOfInDelegateTypeParamTest()
            VerifyRecommendationsContain(<File>Delegate Sub Goo(Of |</File>, "In")
        End Sub

        <Fact>
        Public Sub InForSecondDelegateTypeParamTest()
            VerifyRecommendationsContain(<File>Delegate Sub Goo(Of |</File>, "In")
        End Sub

        <Fact>
        Public Sub OutForSecondDelegateTypeParamTest()
            VerifyRecommendationsContain(<File>Delegate Sub Goo(Of |</File>, "In")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterEolTest()
            VerifyRecommendationsContain(
<File>Delegate Sub Goo(Of 
    |</File>, "In")
        End Sub
    End Class
End Namespace
