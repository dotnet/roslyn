' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class CustomEventKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CustomEventInClassDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Custom Event")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CustomEventInStructureDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<StructureDeclaration>|</StructureDeclaration>, "Custom Event")
        End Function

        <Fact>
        <WorkItem(544999, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544999")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CustomEventNotInInterfaceDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Custom Event")
        End Function

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Custom Event")
        End Function
    End Class
End Namespace
