' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class HandlesKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesAfterMethodInClass()
            VerifyRecommendationsContain(<File>
Class Foo
Sub Foo() |
|</File>, "Handles")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesAfterMethodInModule()
            VerifyRecommendationsContain(<File>
Module Foo
Sub Foo() |
|</File>, "Handles")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesAfterFunction()
            VerifyRecommendationsContain(<File>
Module Foo
Function Foo() As Integer |
|</File>, "Handles")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesNotAfterMethodInStructure()
            VerifyRecommendationsMissing(<File>
Structure Foo
Sub Foo() |
|</File>, "Handles")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesNotAfterNewLine()
            VerifyRecommendationsMissing(<File>
Class Foo
Sub Foo() 
        |
</File>, "Handles")
        End Sub

        <WorkItem(577941)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHandlesAfterIterator()
            VerifyRecommendationsMissing(<File>
Class C
    Private Iterator Function TestIterator() |
</File>, "Handles")
        End Sub

    End Class
End Namespace
