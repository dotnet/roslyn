' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class HandlesKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesAfterMethodInClassTest()
            VerifyRecommendationsContain(<File>
Class Goo
Sub Goo() |
|</File>, "Handles")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesAfterMethodInModuleTest()
            VerifyRecommendationsContain(<File>
Module Goo
Sub Goo() |
|</File>, "Handles")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesAfterFunctionTest()
            VerifyRecommendationsContain(<File>
Module Goo
Function Goo() As Integer |
|</File>, "Handles")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesNotAfterMethodInStructureTest()
            VerifyRecommendationsMissing(<File>
Structure Goo
Sub Goo() |
|</File>, "Handles")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub HandlesNotAfterNewLineTest()
            VerifyRecommendationsMissing(<File>
Class Goo
Sub Goo() 
        |
</File>, "Handles")
        End Sub

        <WorkItem(577941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577941")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoHandlesAfterIteratorTest()
            VerifyRecommendationsMissing(<File>
Class C
    Private Iterator Function TestIterator() |
</File>, "Handles")
        End Sub
    End Class
End Namespace
