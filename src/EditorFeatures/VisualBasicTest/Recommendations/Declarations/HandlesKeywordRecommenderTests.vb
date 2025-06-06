' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class HandlesKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub HandlesAfterMethodInClassTest()
            VerifyRecommendationsContain(<File>
Class Goo
Sub Goo() |
|</File>, "Handles")
        End Sub

        <Fact>
        Public Sub HandlesAfterMethodInModuleTest()
            VerifyRecommendationsContain(<File>
Module Goo
Sub Goo() |
|</File>, "Handles")
        End Sub

        <Fact>
        Public Sub HandlesAfterFunctionTest()
            VerifyRecommendationsContain(<File>
Module Goo
Function Goo() As Integer |
|</File>, "Handles")
        End Sub

        <Fact>
        Public Sub HandlesNotAfterMethodInStructureTest()
            VerifyRecommendationsMissing(<File>
Structure Goo
Sub Goo() |
|</File>, "Handles")
        End Sub

        <Fact>
        Public Sub HandlesNotAfterNewLineTest()
            VerifyRecommendationsMissing(<File>
Class Goo
Sub Goo() 
        |
</File>, "Handles")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577941")>
        Public Sub NoHandlesAfterIteratorTest()
            VerifyRecommendationsMissing(<File>
Class C
    Private Iterator Function TestIterator() |
</File>, "Handles")
        End Sub
    End Class
End Namespace
