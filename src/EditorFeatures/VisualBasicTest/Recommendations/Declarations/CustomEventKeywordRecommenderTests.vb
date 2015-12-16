' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class CustomEventKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CustomEventInClassDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Custom Event")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CustomEventInStructureDeclaration()
            VerifyRecommendationsContain(<StructureDeclaration>|</StructureDeclaration>, "Custom Event")
        End Sub

        <Fact>
        <WorkItem(544999)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CustomEventNotInInterfaceDeclaration()
            VerifyRecommendationsMissing(<InterfaceDeclaration>|</InterfaceDeclaration>, "Custom Event")
        End Sub

        <WorkItem(674791)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Custom Event")
        End Sub
    End Class
End Namespace
