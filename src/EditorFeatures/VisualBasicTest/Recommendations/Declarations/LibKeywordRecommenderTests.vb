' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class LibKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub LibAfterNameInSubTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub goo |</ClassDeclaration>, "Lib")
        End Sub

        <Fact>
        Public Sub LibAfterNameInFunctionTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Function goo |</ClassDeclaration>, "Lib")
        End Sub

        <Fact>
        Public Sub LibNotAfterLibKeywordTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Declare Sub goo Lib |</ClassDeclaration>, "Lib")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>Declare Sub goo 
|</ClassDeclaration>, "Lib")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare Sub goo _
|</ClassDeclaration>, "Lib")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<ClassDeclaration>Declare Sub goo _ ' Test
|</ClassDeclaration>, "Lib")
        End Sub
    End Class
End Namespace
