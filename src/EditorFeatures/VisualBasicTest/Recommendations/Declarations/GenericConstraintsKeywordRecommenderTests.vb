' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class GenericConstraintsKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub AllAfterAsInSingleConstraintTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        Public Sub AllAfterInMultipleConstraintTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As {|</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        Public Sub AllAfterExplicitTypeTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As {OtherType, |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        Public Sub NoneAfterStructureConstraintTest()
            VerifyRecommendationsMissing(<File>Class Goo(Of T As {Structure, |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        Public Sub ClassOnlyAfterNewTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As {New, |</File>, "Class")
        End Sub

        <Fact>
        Public Sub NewOnlyAfterClassTest()
            VerifyRecommendationsContain(<File>Class Goo(Of T As {Class, |</File>, "New")
        End Sub

        <Fact>
        Public Sub NoneAfterClassAndNewTest()
            VerifyRecommendationsMissing(<File>Class Goo(Of T As {Class, New,|</File>, "Class", "Structure", "New")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<File>Class Goo(Of T As 
|</File>, "New")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<File>Class Goo(Of T As _
|</File>, "New")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<File>Class Goo(Of T As _ ' Test
|</File>, "New")
        End Sub
    End Class
End Namespace
