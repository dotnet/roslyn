' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class DelegateSubFunctionKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAndFunctionAfterDelegateTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Delegate |</ClassDeclaration>, "Sub", "Function")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(
<ClassDeclaration>Delegate _
|</ClassDeclaration>, "Sub", "Function")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTestCommentsAfterLineContinuation() As Task
            Await VerifyRecommendationsAreExactlyAsync(
<ClassDeclaration>Delegate _ ' Test
|</ClassDeclaration>, "Sub", "Function")
        End Function
    End Class
End Namespace
