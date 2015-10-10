' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class CovarianceModifierKeywordRecommenderTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InAfterOfInInterfaceTypeParam()
            VerifyRecommendationsContain(<File>Interface IFoo(Of |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OutAfterOfInInterfaceTypeParam()
            VerifyRecommendationsContain(<File>Interface IFoo(Of |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InNotInClassTypeParam()
            VerifyRecommendationsMissing(<File>Class Foo(Of |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OutNotInClassTypeParam()
            VerifyRecommendationsMissing(<File>Class Foo(Of |</File>, "Out")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InNotInStructureTypeParam()
            VerifyRecommendationsMissing(<File>Structure Foo(Of |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OutNotInStructureTypeParam()
            VerifyRecommendationsMissing(<File>Structure Foo(Of |</File>, "Out")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InForSecondInterfaceTypeParam()
            VerifyRecommendationsContain(<File>Interface IFoo(Of T, |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OutForSecondInterfaceTypeParam()
            VerifyRecommendationsContain(<File>Interface IFoo(Of T, |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InNotInMultipleConstraints()
            VerifyRecommendationsMissing(<File>Interface IFoo(Of T As {New, |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OutNotInMultipleConstraints()
            VerifyRecommendationsMissing(<File>Interface IFoo(Of T As {New, |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InAfterOfInDelegateTypeParam()
            VerifyRecommendationsContain(<File>Delegate Sub Foo(Of |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OutAfterOfInDelegateTypeParam()
            VerifyRecommendationsContain(<File>Delegate Sub Foo(Of |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InForSecondDelegateTypeParam()
            VerifyRecommendationsContain(<File>Delegate Sub Foo(Of |</File>, "In")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OutForSecondDelegateTypeParam()
            VerifyRecommendationsContain(<File>Delegate Sub Foo(Of |</File>, "In")
        End Sub

        <WorkItem(530953)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterEol()
            VerifyRecommendationsContain(
<File>Delegate Sub Foo(Of 
    |</File>, "In")
        End Sub
    End Class
End Namespace
