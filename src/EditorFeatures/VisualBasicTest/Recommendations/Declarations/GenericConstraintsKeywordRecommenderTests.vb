' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class GenericConstraintsKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterAsInSingleConstraintTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterInMultipleConstraintTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As {|</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterExplicitTypeTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As {OtherType, |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterStructureConstraintTest()
            VerifyRecommendationsMissing(<File>Class Goo(Of T As {Structure, |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassOnlyAfterNewTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As {New, |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NewOnlyAfterClassTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As {Class, |</File>, "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterClassAndNewTest()
            VerifyRecommendationsMissing(<File>Class Goo(Of T As {Class, New,|</File>, "Class", "Structure", "New")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<File>Class Goo(Of T As 
|</File>, "New")
        End Sub

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<File>Class Goo(Of T As _
|</File>, "New")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<File>Class Goo(Of T As _ ' Test
|</File>, "New")
        End Sub
    End Class
End Namespace
