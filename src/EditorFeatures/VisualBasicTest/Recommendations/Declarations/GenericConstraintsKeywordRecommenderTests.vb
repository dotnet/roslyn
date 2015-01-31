' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class GenericConstraintsKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterAsInSingleConstraint()
            VerifyRecommendationsContain(<File>Class Foo(Of T As |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterInMultipleConstraint()
            VerifyRecommendationsContain(<File>Class Foo(Of T As {|</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterExplicitType()
            VerifyRecommendationsContain(<File>Class Foo(Of T As {OtherType, |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterStructureConstraint()
            VerifyRecommendationsMissing(<File>Class Foo(Of T As {Structure, |</File>, "Class", "Structure", "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ClassOnlyAfterNew()
            VerifyRecommendationsContain(<File>Class Foo(Of T As {New, |</File>, "Class")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NewOnlyAfterClass()
            VerifyRecommendationsContain(<File>Class Foo(Of T As {Class, |</File>, "New")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneAfterClassAndNew()
            VerifyRecommendationsMissing(<File>Class Foo(Of T As {Class, New,|</File>, "Class", "Structure", "New")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<File>Class Foo(Of T As 
|</File>, "New")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<File>Class Foo(Of T As _
|</File>, "New")
        End Sub
    End Class
End Namespace
