' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class CustomEventKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CustomEventInClassDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Custom Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CustomEventInStructureDeclarationTest()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Custom Event")
        End Sub

        <Fact>
        <WorkItem(544999, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544999")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CustomEventNotInInterfaceDeclarationTest()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Custom Event")
        End Sub

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Custom Event")
        End Sub
    End Class
End Namespace
