' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AsyncKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub KeywordsAfterAsync()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Async |</ClassDeclaration>,
                                            "Friend", "Function", "Private", "Protected", "Protected Friend", "Public", "Sub")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInMethodStatement()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Async")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InMethodExpression()
            VerifyRecommendationsContain(<MethodBody>Dim z = |</MethodBody>, "Async")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Async")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AlreadyAsyncFunctionDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>| Async</ClassDeclaration>, "Async")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubDeclaration()
            VerifyRecommendationsContain(<ClassDeclaration>| Sub bar()</ClassDeclaration>, "Async")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub FunctionDeclarationInInterface()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Async")
        End Sub

        <WorkItem(547254)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterAsync()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Async")
        End Sub

        <WorkItem(645060)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterConstInClass()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Async")
        End Sub

        <WorkItem(645060)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterConstInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>Const |</ModuleDeclaration>, "Async")
        End Sub

        <WorkItem(645060)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWithEventsInClass()
            VerifyRecommendationsMissing(<ClassDeclaration>WithEvents |</ClassDeclaration>, "Async")
        End Sub

        <WorkItem(645060)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterWithEventsInModule()
            VerifyRecommendationsMissing(<ModuleDeclaration>WithEvents |</ModuleDeclaration>, "Async")
        End Sub

        <WorkItem(674791)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Async")
        End Sub

    End Class
End Namespace
