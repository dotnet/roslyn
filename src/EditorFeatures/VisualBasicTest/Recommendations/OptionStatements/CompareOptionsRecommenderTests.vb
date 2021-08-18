' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OptionStatements
    Public Class CompareOptionsRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionsAfterOptionCompareTest()
            VerifyRecommendationsAreExactly(<File>Option Compare |</File>, "Binary", "Text")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<File>Option Compare 
|</File>, "Binary", "Text")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<File>Option Compare _
|</File>, "Binary", "Text")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<File>Option Compare _ ' Test
|</File>, "Binary", "Text")
        End Sub
    End Class
End Namespace
