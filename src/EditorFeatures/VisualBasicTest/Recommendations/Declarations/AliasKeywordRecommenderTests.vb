' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class AliasKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub AliasAfterLibNameInSubTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub goo Lib "Goo" |</ClassDeclaration>, "Alias")
        End Sub

        <Fact>
        Public Sub AliasAfterLibNameInFunctionTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Function goo Lib "Goo" |</ClassDeclaration>, "Alias")
        End Sub

        <Fact>
        Public Sub AliasNotAfterLibKeywordTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub goo Lib |</ClassDeclaration>, Array.Empty(Of String)())
        End Sub

        <Fact>
        Public Sub NothingAfterBrokenAliasTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Declare Sub goo Lib "Goo" Alais |</ClassDeclaration>, Array.Empty(Of String)())
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NoAliasAfterEolTest()
            VerifyRecommendationsMissing(
<ClassDeclaration>Declare Function goo Lib "Goo" 
    |</ClassDeclaration>, "Alias")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AliasAfterExplicitLineContinuationTest()
            VerifyRecommendationsAreExactly(
<ClassDeclaration>Declare Function goo Lib "Goo" _
|</ClassDeclaration>, "Alias")
        End Sub
        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AliasAfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsAreExactly(
<ClassDeclaration>Declare Function goo Lib "Goo" _ ' Test
|</ClassDeclaration>, "Alias")
        End Sub
    End Class
End Namespace
