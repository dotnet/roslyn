' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class DelegateSubFunctionKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubAndFunctionAfterDelegateTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Delegate |</ClassDeclaration>, "Sub", "Function")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsAreExactly(
<ClassDeclaration>Delegate _
|</ClassDeclaration>, "Sub", "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsAreExactly(
<ClassDeclaration>Delegate _ ' Test
|</ClassDeclaration>, "Sub", "Function")
        End Sub
    End Class
End Namespace
